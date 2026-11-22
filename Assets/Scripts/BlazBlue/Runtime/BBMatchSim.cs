using System;
using Unity.Profiling;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Deterministic 60 Hz simulation authority for BlazBlue 4-Button matches.
    /// Orchestrates both fighters in a strict, deterministic sequence:
    /// 1. Sample Inputs -> 2. Advance States -> 3. Movement Integration -> 4. Pushbox Resolution ->
    /// 5. Build World Boxes -> 6. Throw Pass -> 7. Strike Pass -> 8. Projectile Pass ->
    /// 9. Apply Damage/Stun/Gauges -> 10. Facing Latch -> 11. State Hash.
    ///
    /// Zero allocations per tick; profiling via markers "BB.Tick" and "BB.Collide".
    /// </summary>
    [ExecuteAlways]
    public class BBMatchSim : MonoBehaviour
    {
        public static BBMatchSim Instance { get; private set; }

        private static readonly ProfilerMarker s_SimTickMarker = new ProfilerMarker("BB.Tick");
        private static readonly ProfilerMarker s_CollideMarker = new ProfilerMarker("BB.Collide");

        [Header("Fighters")]
        public BBFighterBrain p1Brain;
        public BBFighterBrain p2Brain;

        [Header("Tuning")]
        public BBSystemTuning tuning;

        [Header("Simulation Counters")]
        public int currentTick = 0;
        public int comboHitCount = 0;
        public uint lastTickHash = 0;

        // Zero-allocation preallocated collision buffers
        private readonly BBBoxBuffer _p1Boxes = new BBBoxBuffer();
        private readonly BBBoxBuffer _p2Boxes = new BBBoxBuffer();
        private readonly BBContact[] _contactResults = new BBContact[16];
        private int _contactCount = 0;
        private int _sideSwitchLeniencyTicks = 0;
        private int _lastP1FacingDir = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (tuning == null) tuning = BBSystemTuning.Default;
        }

        bool _boundToClock;

        private void OnEnable()
        {
            TryBindClock();
        }

        private void Update()
        {
            // The clock may not exist yet when this component is enabled.
            if (!_boundToClock) TryBindClock();
        }

        private void OnDisable()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= Tick;
            }
            _boundToClock = false;
        }

        void TryBindClock()
        {
            if (_boundToClock && CombatTickEngine.Instance == null)
                _boundToClock = false;
            if (_boundToClock) return;
            CombatTickEngine clock = CombatTickEngine.EnsureRunning();
            if (clock == null) return;
            clock.OnTick -= Tick;
            clock.OnTick += Tick;
            _boundToClock = true;
        }

        public void RegisterFighters(BBFighterBrain p1, BBFighterBrain p2)
        {
            if (Instance == null) Instance = this;
            p1Brain = p1;
            p2Brain = p2;
            if (p1 != null)
            {
                p1.opponentBrain = p2;
                p1.ReleaseSoloTick();
            }
            if (p2 != null)
            {
                p2.opponentBrain = p1;
                p2.ReleaseSoloTick();
            }
        }

        /// <summary>
        /// Fixed 60 Hz simulation step executed deterministically for both fighters.
        /// </summary>
        public void Tick(int tick)
        {
            if (p1Brain == null || p2Brain == null) return;

            using (s_SimTickMarker.Auto())
            {
                currentTick = tick;

                // Super Flash freeze: freeze match sim for 20 ticks during super startup
                if (BBSuperFlash.FreezeTicks > 0)
                {
                    BBSuperFlash.Tick();
                    return;
                }

                if (_sideSwitchLeniencyTicks > 0) _sideSwitchLeniencyTicks--;
                int currentP1Facing = (p1Brain.transform.position.x < p2Brain.transform.position.x) ? 1 : -1;
                if (currentP1Facing != _lastP1FacingDir)
                {
                    _sideSwitchLeniencyTicks = 3; // 3 frames of side-switch leniency on cross-ups
                    _lastP1FacingDir = currentP1Facing;
                }

                // Step 1: Read BBInputFrame for P1 and P2
                p1Brain.StepInput(tick);
                p2Brain.StepInput(tick);

                // Step 1.5: Execute commands (if not in hitstop/hitstun)
                if (p1Brain.hitstopRemaining <= 0 && p1Brain.bbState != BBState.Hitstun)
                {
                    p1Brain.ExecuteCommands(tick);
                }
                if (p2Brain.hitstopRemaining <= 0 && p2Brain.bbState != BBState.Hitstun)
                {
                    p2Brain.ExecuteCommands(tick);
                }

                // Step 2: Advance fighter state timers and evaluate actions (P1 then P2)
                p1Brain.AdvanceState(tick);
                p2Brain.AdvanceState(tick);

                // Step 3: Movement integration
                IntegrateMovement(p1Brain);
                IntegrateMovement(p2Brain);

                // Step 4: Pushbox resolution (Z=0 plane)
                float p1x = p1Brain.transform.position.x;
                float p2x = p2Brain.transform.position.x;
                BBWorldBox p1Push = p1Brain.GetPushBox();
                BBWorldBox p2Push = p2Brain.GetPushBox();

                float arenaLimit = PushbackSolver.ActiveArenaLimitX;
                BBCollision.ResolvePush(
                    ref p1x,
                    ref p2x,
                    in p1Push,
                    in p2Push,
                    -arenaLimit,
                    arenaLimit,
                    p1Brain.isGrounded,
                    p2Brain.isGrounded
                );

                p1Brain.transform.position = new Vector3(p1x, p1Brain.transform.position.y, 0f);
                p2Brain.transform.position = new Vector3(p2x, p2Brain.transform.position.y, 0f);

                // Step 5: Build world-space box lists for this tick
                _p1Boxes.Clear();
                _p2Boxes.Clear();
                _contactCount = 0;

                p1Brain.PopulateWorldBoxes(_p1Boxes);
                p2Brain.PopulateWorldBoxes(_p2Boxes);

                // Step 6 & 7: Physics-free collision pass
                using (s_CollideMarker.Auto())
                {
                    // Step 6: Throw checks (both directions)
                    int p1ThrowContacts = BBCollision.ThrowPass(_p1Boxes, _p2Boxes, _contactResults, ref _contactCount, 0, 1, p2Brain.currentInvuln);
                    int p2ThrowContacts = BBCollision.ThrowPass(_p2Boxes, _p1Boxes, _contactResults, ref _contactCount, 1, 0, p1Brain.currentInvuln);

                    // Check for simultaneous throw tech
                    bool simultaneousThrow = p1ThrowContacts > 0 && p2ThrowContacts > 0;
                    if (simultaneousThrow)
                    {
                        ResolveThrowTech();
                        _contactCount = 0; // Throws neutralized
                    }

                    // Step 7: Strike checks (P1 hits P2, P2 hits P1)
                    BBInvuln p2Invuln = p2Brain.currentInvuln;
                    if (p1Brain.currentAttack != null && p1Brain.currentAttack.otg)
                    {
                        p2Invuln &= ~BBInvuln.Strike;
                    }
                    BBInvuln p1Invuln = p1Brain.currentInvuln;
                    if (p2Brain.currentAttack != null && p2Brain.currentAttack.otg)
                    {
                        p1Invuln &= ~BBInvuln.Strike;
                    }

                    BBCollision.StrikePass(_p1Boxes, _p2Boxes, ref p1Brain.attackHitMask, _contactResults, ref _contactCount, 0, 1, p2Invuln);
                    BBCollision.StrikePass(_p2Boxes, _p1Boxes, ref p2Brain.attackHitMask, _contactResults, ref _contactCount, 1, 0, p1Invuln);
                }

                // Step 8: Projectiles (handled when projectiles are active)

                // Step 9: Apply hits (damage, hitstop, hitstun, blockstun, heat)
                if (_contactCount > 0)
                {
                    ApplyContacts();
                }

                // Step 10: Facing latch and round condition checks
                LatchFacing();

                // Step 11: Compute state hash
                lastTickHash = BBSimHash.ComputeTickHash(
                    tick,
                    p1Brain.transform.position.x,
                    p1Brain.transform.position.y,
                    p1Brain.fighter != null ? p1Brain.fighter.FacingDirection : 1,
                    (byte)p1Brain.bbState,
                    (int)p1Brain.health,
                    (int)p1Brain.heat,
                    p2Brain.transform.position.x,
                    p2Brain.transform.position.y,
                    p2Brain.fighter != null ? p2Brain.fighter.FacingDirection : -1,
                    (byte)p2Brain.bbState,
                    (int)p2Brain.health,
                    (int)p2Brain.heat,
                    comboHitCount
                );

                // Step 12: Presentation driver (Playables animation seeking)
                if (p1Brain.animDriver != null) p1Brain.animDriver.TickAnimation(tick);
                if (p2Brain.animDriver != null) p2Brain.animDriver.TickAnimation(tick);
            }
        }

        private void IntegrateMovement(BBFighterBrain brain)
        {
            if (brain == null || brain.fighter == null) return;
            if (brain.hitstopRemaining > 0) return;

            Vector3 pos = brain.transform.position;
            int facing = brain.fighter.FacingDirection;

            // If active attack has authored motion track, use it
            if (brain.currentAttack != null && brain.currentAttack.motionTrack != null)
            {
                Vector2 delta = brain.currentAttack.motionTrack.GetDelta(brain.attackTick, facing);
                pos.x += delta.x;
                pos.y += delta.y;
            }
            else
            {
                // Airborne gravity and trajectory integration (applies in ALL airborne states)
                if (!brain.isGrounded || pos.y > 0f)
                {
                    if (brain.bbState != BBState.Thrown)
                    {
                        float grav = brain.tuning != null ? brain.tuning.gravity : 20.0f;
                        brain.simVelocity.y -= grav * CombatTickEngine.TICK_DELTA_TIME;
                        pos.x += brain.simVelocity.x * CombatTickEngine.TICK_DELTA_TIME;
                        pos.y += brain.simVelocity.y * CombatTickEngine.TICK_DELTA_TIME;
                        if (pos.y <= 0f)
                        {
                            pos.y = 0f;
                            brain.simVelocity.y = 0f;
                            brain.isGrounded = true;
                            if (brain.bbState == BBState.Airborne)
                            {
                                brain.bbState = BBState.Neutral;
                                brain.stateTick = 0;
                                if (brain.animDriver != null)
                                {
                                    sbyte numpad = brain.fighter != null ? BBInputFrame.WorldToFacingNumpad(brain.History.LatestFrame.numpad, brain.fighter.FacingDirection) : (sbyte)5;
                                    brain.animDriver.UpdateLocomotionState(brain.bbState, numpad, brain.isGrounded);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Standard ground movement based on state
                    switch (brain.bbState)
                    {
                        case BBState.WalkForward:
                            float walkF = brain.profile != null ? brain.profile.forwardWalkSpeed : 3.6f;
                            pos.x += facing * walkF * CombatTickEngine.TICK_DELTA_TIME;
                            break;

                        case BBState.WalkBackward:
                            float walkB = brain.profile != null ? brain.profile.backwardWalkSpeed : 2.8f;
                            pos.x -= facing * walkB * CombatTickEngine.TICK_DELTA_TIME;
                            break;

                        case BBState.ForwardDash:
                            float dashSpeed = brain.profile != null ? brain.profile.forwardDashSpeed : 6.5f;
                            pos.x += facing * dashSpeed * CombatTickEngine.TICK_DELTA_TIME;
                            break;

                        case BBState.Backdash:
                            float bdashSpeed = brain.profile != null ? brain.profile.backdashSpeed : 7.0f;
                            pos.x -= facing * bdashSpeed * CombatTickEngine.TICK_DELTA_TIME;
                            break;
                    }
                }
            }

            // Ground clamping
            if (pos.y <= 0f)
            {
                pos.y = 0f;
                brain.isGrounded = true;
            }

            // Arena boundary clamp
            pos.x = PushbackSolver.ClampToArena(pos.x);
            brain.transform.position = pos;
        }

        private void ResolveThrowTech()
        {
            // Both fighters teched throw: push apart and set neutral recovery
            float p1x = p1Brain.transform.position.x;
            float p2x = p2Brain.transform.position.x;
            float separation = 1.6f;

            if (p1x <= p2x)
            {
                p1Brain.transform.position = new Vector3(p1x - (separation * 0.5f), p1Brain.transform.position.y, 0f);
                p2Brain.transform.position = new Vector3(p2x + (separation * 0.5f), p2Brain.transform.position.y, 0f);
            }
            else
            {
                p1Brain.transform.position = new Vector3(p1x + (separation * 0.5f), p1Brain.transform.position.y, 0f);
                p2Brain.transform.position = new Vector3(p2x - (separation * 0.5f), p2Brain.transform.position.y, 0f);
            }

            p1Brain.bbState = BBState.Neutral;
            p2Brain.bbState = BBState.Neutral;
            p1Brain.hitstopRemaining = 12;
            p2Brain.hitstopRemaining = 12;
        }

        private void ApplyContacts()
        {
            // Pass 1: Resolve Throws first (Throw beats strike priority rule)
            for (int i = 0; i < _contactCount; i++)
            {
                ref BBContact contact = ref _contactResults[i];
                BBFighterBrain attacker = contact.attackerId == 0 ? p1Brain : p2Brain;
                bool isThrow = contact.contactType == BBContactType.Throw ||
                    (attacker.currentAttack != null && (attacker.currentAttack.kind == BBMoveKind.Throw ||
                                                        attacker.currentAttack.kind == BBMoveKind.CommandThrow ||
                                                        attacker.currentAttack.moveId == "BC" ||
                                                        attacker.currentAttack.moveId == "4BC" ||
                                                        attacker.currentAttack.moveId == "6BC" ||
                                                        attacker.currentAttack.moveId == "j.BC"));
                if (!isThrow) continue;
                contact.contactType = BBContactType.Throw;

                BBFighterBrain defender = contact.defenderId == 0 ? p1Brain : p2Brain;
                if (defender.bbState == BBState.Thrown || defender.bbState == BBState.DownWait || defender.bbState == BBState.Wakeup) continue;
                if ((defender.currentInvuln & BBInvuln.Throw) != 0) continue;
                if (attacker.attackHitMask.HasHit(contact.hitIndex)) continue;
                attacker.attackHitMask.MarkHit(contact.hitIndex);
                attacker.chainState.madeContact = true;

                bool wasInStun = defender.bbState == BBState.Hitstun ||
                                 defender.bbState == BBState.Blockstun ||
                                 defender.bbState == BBState.BarrierBlockstun;

                // Successful throw
                defender.bbState = BBState.Thrown;
                defender.throwTechWindow = wasInStun ? 27 : 15;
                defender.throwAttacker = attacker;
                attacker.throwAttacker = defender;
                defender.hitstunRemaining = 30; // 30f duration then knockdown

                float throwDmg = (attacker.currentAttack != null && attacker.currentAttack.damage > 0f) ? attacker.currentAttack.damage : 1300f;
                defender.throwPendingDamage = throwDmg;

                Vector3 throwPt = (contact.contactPointX != 0f || contact.contactPointY != 0f)
                    ? new Vector3(contact.contactPointX, contact.contactPointY, 0f)
                    : (defender.transform.position + Vector3.up * 1.0f);
                BBContactPresentation.OnCleanHit(throwPt, attacker, false, false);
                if (BBMatchScreenDirector.Instance != null && BBMatchScreenDirector.Instance.Stats != null)
                {
                    BBMatchScreenDirector.Instance.Stats.RegisterHit(attacker == p1Brain, (int)throwDmg, false);
                }
                attacker.heat = Mathf.Min(tuning.heatMax, attacker.heat + 5f);
                // Throws do not incur strike hitstop; authoritative 30f Thrown duration
                attacker.hitstopRemaining = 0;
                defender.hitstopRemaining = 0;
                comboHitCount++;
            }

            // Pass 2: Resolve Strikes (nullified if attacker was thrown)
            for (int i = 0; i < _contactCount; i++)
            {
                ref BBContact contact = ref _contactResults[i];
                if (contact.contactType != BBContactType.Strike) continue;

                BBFighterBrain attacker = contact.attackerId == 0 ? p1Brain : p2Brain;
                BBFighterBrain defender = contact.defenderId == 0 ? p1Brain : p2Brain;

                // Priority check: if attacker was grabbed by a throw on this tick, their strike is beaten
                if (attacker.bbState == BBState.Thrown) continue;
                if (defender.bbState == BBState.Thrown) continue;

                // Check if defender can block in their current state
                bool canBlockState = defender.bbState == BBState.Neutral ||
                                     defender.bbState == BBState.WalkForward ||
                                     defender.bbState == BBState.WalkBackward ||
                                     defender.bbState == BBState.Crouching ||
                                     defender.bbState == BBState.Blockstun ||
                                     defender.bbState == BBState.BarrierBlockstun ||
                                     defender.bbState == BBState.Airborne;

                bool inBlockstun = defender.bbState == BBState.Blockstun || defender.bbState == BBState.BarrierBlockstun;
                bool isGuarding = false;

                if (canBlockState)
                {
                    if (inBlockstun)
                    {
                        // Auto-guard during blockstun (block carryover)
                        isGuarding = true;
                    }
                    else
                    {
                        int facing = defender.fighter != null ? defender.fighter.FacingDirection : 1;
                        sbyte facingNumpad = BBInputFrame.WorldToFacingNumpad(defender.History.LatestFrame.numpad, facing);

                        // 3 frames side-switch leniency on cross-ups: allow either left or right as back
                        bool isSideSwitchActive = _sideSwitchLeniencyTicks > 0;
                        if (isSideSwitchActive)
                        {
                            sbyte rawNum = defender.History.LatestFrame.numpad;
                            if (rawNum == 4 || rawNum == 6 || rawNum == 1 || rawNum == 3 || rawNum == 7 || rawNum == 9)
                            {
                                if (rawNum == 1 || rawNum == 3) facingNumpad = 1;
                                else if (rawNum == 7 || rawNum == 9) facingNumpad = 7;
                                else facingNumpad = 4;
                            }
                        }

                        BBGuard guardReq = attacker.currentAttack != null ? attacker.currentAttack.guard : BBGuard.Mid;

                        if (!defender.isGrounded || defender.bbState == BBState.Airborne)
                        {
                            // Any back blocks in the air (7, 4, 1)
                            bool anyBackAir = facingNumpad == 7 || facingNumpad == 4 || facingNumpad == 1;
                            isGuarding = anyBackAir && guardReq != BBGuard.Unblockable;
                        }
                        else
                        {
                            bool isStandingBack = facingNumpad == 4 || facingNumpad == 7;
                            bool isCrouchingBack = facingNumpad == 1 || (facingNumpad == 2 && isSideSwitchActive);

                            switch (guardReq)
                            {
                                case BBGuard.Mid:
                                    isGuarding = isStandingBack || isCrouchingBack;
                                    break;
                                case BBGuard.Low:
                                    isGuarding = isCrouchingBack; // Crouching blocks Mid+Low, standing whiffs
                                    break;
                                case BBGuard.Overhead:
                                    isGuarding = isStandingBack; // Standing blocks High+Mid, crouching whiffs
                                    break;
                                case BBGuard.Unblockable:
                                    isGuarding = false;
                                    break;
                                default:
                                    isGuarding = isStandingBack || isCrouchingBack;
                                    break;
                            }
                        }
                    }
                }

                bool isBarrier = defender.CurrentCommand.commandType == BBCommandType.Barrier && defender.barrier > 0f && !defender.gauges.isDanger;

                float baseDamage = (attacker.currentAttack != null ? attacker.currentAttack.damage : 300f) * Mathf.Max(0.1f, attacker.ActiveDamageScale);
                int hitstun = attacker.currentAttack != null ? attacker.currentAttack.hitstunTicks : 14;
                int blockstun = attacker.currentAttack != null ? attacker.currentAttack.blockstunTicks : 9;
                int hitstopA = (attacker.currentAttack != null ? attacker.currentAttack.hitstopAttacker : 8) + attacker.SignatureHitstopBonus;
                int hitstopD = (attacker.currentAttack != null ? attacker.currentAttack.hitstopDefender : 8) + attacker.SignatureHitstopBonus;

                BBMoveKind moveKind = attacker.currentAttack != null ? attacker.currentAttack.kind : BBMoveKind.Normal;
                BBButton moveButton = attacker.currentAttack != null ? attacker.currentAttack.button : BBButton.A;
                int moveHash = attacker.currentAttack != null ? attacker.currentAttack.moveId.GetHashCode() : 1;

                if (isGuarding)
                {
                    int chip = 0;
                    if (isBarrier && defender.barrierSys != null)
                    {
                        defender.barrierSys.ApplyBarrierBlock(baseDamage, currentTick, out float pushbackMult, out int blockstunReduction);
                        defender.bbState = BBState.BarrierBlockstun;
                        defender.blockstunRemaining = Mathf.Max(1, blockstun - blockstunReduction);
                        PushbackSolver.ApplyPushback(attacker.fighter, defender.fighter, 0.4f, attacker.fighter.FacingDirection, pushbackMult);
                    }
                    else
                    {
                        // Regular block takes chip damage (spec 4.19)
                        float authoredChip = attacker.currentAttack != null ? attacker.currentAttack.chipPct : 0f;
                        chip = BBDamageModel.CalculateChipDamage(baseDamage, moveKind, false, authoredChip);
                        float minHp = BBDamageModel.CanChipKO(moveKind) ? 0f : 1f;
                        defender.SetAuthoritativeHealth(Mathf.Max(minHp, defender.health - chip));

                        defender.bbState = BBState.Blockstun;
                        defender.blockstunRemaining = blockstun;
                        if (defender.animDriver != null) defender.animDriver.UpdateLocomotionState(defender.bbState, 4, defender.isGrounded);
                        PushbackSolver.ApplyPushback(attacker.fighter, defender.fighter, 0.3f, attacker.fighter.FacingDirection, 1.0f);
                    }

                    attacker.hitstopRemaining = hitstopA;
                    defender.hitstopRemaining = hitstopD;
                    attacker.gauges.AddHeatOnHit(moveButton, moveKind, true, tuning);
                    bool crush = moveButton == BBButton.C || moveKind == BBMoveKind.DistortionDrive;
                    Vector3 guardContactPt = (contact.contactPointX != 0f || contact.contactPointY != 0f)
                        ? new Vector3(contact.contactPointX, contact.contactPointY, 0f)
                        : (defender.transform.position + Vector3.up * 1.0f);
                    BBContactPresentation.OnGuard(guardContactPt, chip > 0, crush);

                    // Mark contact on attacker chain state
                    attacker.chainState.madeContact = true;
                    attacker.chainState.contactTick = attacker.attackTick;
                    if (attacker.drive != null && attacker.currentAttack != null)
                    {
                        attacker.drive.OnDriveHit(attacker, attacker.currentAttack, true);
                    }
                }
                else
                {
                    // Clean strike hit!
                    // Get proration parameters
                    BBDamageModel.GetDefaultProration(moveKind, moveButton, out float p1, out float p2, out float minDamagePct);
                    if (attacker.currentAttack != null && attacker.currentAttack.minDamagePct > 0f)
                    {
                        minDamagePct = attacker.currentAttack.minDamagePct;
                    }

                    bool isRecoveryOfSpecialOrSuper = defender.bbState == BBState.AttackRecovery &&
                        defender.currentAttack != null &&
                        (defender.currentAttack.kind == BBMoveKind.Special || defender.currentAttack.kind == BBMoveKind.DistortionDrive);

                    bool isCounterHit = defender.bbState == BBState.AttackStartup ||
                                        defender.bbState == BBState.AttackActive ||
                                        isRecoveryOfSpecialOrSuper;

                    bool isFatal = attacker.currentAttack != null && attacker.currentAttack.fatalCounter && isCounterHit;

                    int hitLevel = 1;
                    if (attacker.currentAttack is BBAttackData bbAtk)
                    {
                        hitLevel = bbAtk.AttackLevel;
                    }
                    else if (attacker.currentAttack != null)
                    {
                        hitLevel = Mathf.Clamp((int)attacker.currentAttack.hitLevel, 0, 5);
                    }
                    int extraHitstop = 0;
                    switch (hitLevel)
                    {
                        case 0: extraHitstop = 0; break;
                        case 1: extraHitstop = 0; break;
                        case 2: extraHitstop = 1; break;
                        case 3: extraHitstop = 2; break;
                        case 4: extraHitstop = 5; break;
                        case 5: extraHitstop = 8; break;
                    }

                    int extraHitstun = 0;
                    switch (hitLevel)
                    {
                        case 0:
                        case 1:
                        case 2:
                            extraHitstun = 4;
                            break;
                        case 3:
                        case 4:
                            extraHitstun = 5;
                            break;
                        case 5:
                            extraHitstun = 6;
                            break;
                    }

                    if (isCounterHit)
                    {
                        hitstopA += extraHitstop;
                        hitstopD += extraHitstop;
                    }

                    // Register hit in combo state
                    attacker.comboState.RegisterHit(moveHash, p1, p2, isAirHit: !defender.isGrounded, isFatalStarter: isFatal);

                    // Calculate scaled damage via BBDamageModel
                    int scaledDmg = BBDamageModel.CalculateDamage(
                        baseDamage,
                        attacker.comboState.comboStarterP1,
                        attacker.comboState.accumulatedP2,
                        attacker.comboState.GetMoveUsageCount(moveHash),
                        defender.health,
                        defender.maxHealth,
                        minDamagePct,
                        isDefenderInDanger: defender.gauges.isDanger
                    );

                    defender.ReceiveAuthoritativeDamage(scaledDmg);
                    Debug.Log($"[BBMatchSim] Hit landed: move={attacker.currentAttack?.moveId}, damage={scaledDmg}, P1 HP={p1Brain.health:F0}, P2 HP={p2Brain.health:F0}");
                    if (BBMatchScreenDirector.Instance != null && BBMatchScreenDirector.Instance.Stats != null)
                    {
                        BBMatchScreenDirector.Instance.Stats.RegisterHit(attacker == p1Brain, scaledDmg, moveKind == BBMoveKind.DistortionDrive);
                    }

                    // Hitstun with combo duration decay (spec 4.15)
                    int decayedHitstun = attacker.comboState.CalculateDecayedHitstun(hitstun, defender.isGrounded);
                    if (isCounterHit) decayedHitstun += extraHitstun;

                    sbyte defFacingNumpad = defender.fighter != null ? BBInputFrame.WorldToFacingNumpad(defender.History.LatestFrame.numpad, defender.fighter.FacingDirection) : (sbyte)5;
                    bool defenderIsCrouching = defender.bbState == BBState.Crouching || defFacingNumpad == 1 || defFacingNumpad == 2 || defFacingNumpad == 3;
                    if (defenderIsCrouching) decayedHitstun += 2;

                    if (attacker.currentAttack != null && (attacker.currentAttack.causesKnockdown || attacker.currentAttack.hardKnockdown))
                    {
                        defender.bbState = BBState.DownWait;
                        defender.hitstunRemaining = attacker.currentAttack.knockdownDurationTicks > 0 ? attacker.currentAttack.knockdownDurationTicks : 36;
                    }
                    else
                    {
                        defender.bbState = BBState.Hitstun;
                        defender.hitstunRemaining = decayedHitstun;
                    }
                    if (defender.animDriver != null) defender.animDriver.UpdateLocomotionState(defender.bbState, 5, defender.isGrounded);

                    attacker.hitstopRemaining = hitstopA;
                    defender.hitstopRemaining = hitstopD;
                    Vector3 hitContactPt = (contact.contactPointX != 0f || contact.contactPointY != 0f)
                        ? new Vector3(contact.contactPointX, contact.contactPointY, 0f)
                        : (defender.transform.position + Vector3.up * 1.0f);
                    BBContactPresentation.OnCleanHit(hitContactPt, attacker, isCounterHit, moveKind == BBMoveKind.DistortionDrive);

                    attacker.gauges.AddHeatOnHit(moveButton, moveKind, false, tuning);
                    defender.gauges.AddHeatOnDamageTaken(scaledDmg, tuning);

                    PushbackSolver.ApplyPushback(attacker.fighter, defender.fighter, 0.35f, attacker.fighter.FacingDirection, 1.0f);
                    comboHitCount++;

                    // Mark contact on attacker chain state
                    attacker.chainState.madeContact = true;
                    attacker.chainState.contactTick = attacker.attackTick;
                    if (attacker.drive != null && attacker.currentAttack != null)
                    {
                        attacker.drive.OnDriveHit(attacker, attacker.currentAttack, false);
                    }
                }
            }

            _contactCount = 0;
        }

        private void LatchFacing()
        {
            if (p1Brain == null || p2Brain == null || p1Brain.fighter == null || p2Brain.fighter == null) return;

            // Only update facing in neutral or when landing
            bool p1CanTurn = p1Brain.isGrounded && (p1Brain.bbState == BBState.Neutral || p1Brain.bbState == BBState.Crouching);
            bool p2CanTurn = p2Brain.isGrounded && (p2Brain.bbState == BBState.Neutral || p2Brain.bbState == BBState.Crouching);

            float p1x = p1Brain.transform.position.x;
            float p2x = p2Brain.transform.position.x;

            if (p1CanTurn && p2CanTurn)
            {
                if (p1x < p2x)
                {
                    p1Brain.fighter.SetFacing(1);
                    p2Brain.fighter.SetFacing(-1);
                }
                else if (p1x > p2x)
                {
                    p1Brain.fighter.SetFacing(-1);
                    p2Brain.fighter.SetFacing(1);
                }
            }
        }
    }
}
