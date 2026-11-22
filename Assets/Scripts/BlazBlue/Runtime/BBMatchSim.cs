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

                // Step 1: Read BBInputFrame for P1 and P2
                p1Brain.StepInput(tick);
                p2Brain.StepInput(tick);

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
                    BBCollision.StrikePass(_p1Boxes, _p2Boxes, ref p1Brain.attackHitMask, _contactResults, ref _contactCount, 0, 1, p2Brain.currentInvuln);
                    BBCollision.StrikePass(_p2Boxes, _p1Boxes, ref p2Brain.attackHitMask, _contactResults, ref _contactCount, 1, 0, p1Brain.currentInvuln);
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
                // Standard movement based on state
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

                    case BBState.Airborne:
                        float grav = brain.tuning != null ? brain.tuning.gravity : 20.0f;
                        brain.simVelocity.y -= grav * CombatTickEngine.TICK_DELTA_TIME;
                        pos.x += brain.simVelocity.x * CombatTickEngine.TICK_DELTA_TIME;
                        pos.y += brain.simVelocity.y * CombatTickEngine.TICK_DELTA_TIME;
                        if (pos.y <= 0f)
                        {
                            pos.y = 0f;
                            brain.simVelocity = Vector2.zero;
                            brain.isGrounded = true;
                            brain.bbState = BBState.Neutral;
                            brain.stateTick = 0;
                            if (brain.animDriver != null)
                            {
                                sbyte numpad = brain.fighter != null ? BBInputFrame.WorldToFacingNumpad(brain.History.LatestFrame.numpad, brain.fighter.FacingDirection) : (sbyte)5;
                                brain.animDriver.UpdateLocomotionState(brain.bbState, numpad, brain.isGrounded);
                            }
                        }
                        break;
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
                if (contact.contactType != BBContactType.Throw) continue;

                BBFighterBrain attacker = contact.attackerId == 0 ? p1Brain : p2Brain;
                BBFighterBrain defender = contact.defenderId == 0 ? p1Brain : p2Brain;

                // Successful throw
                defender.bbState = BBState.Thrown;
                defender.ReceiveAuthoritativeDamage(800f);
                if (BBMatchScreenDirector.Instance != null && BBMatchScreenDirector.Instance.Stats != null)
                {
                    BBMatchScreenDirector.Instance.Stats.RegisterHit(attacker == p1Brain, 800, false);
                }
                attacker.heat = Mathf.Min(tuning.heatMax, attacker.heat + 5f);
                attacker.hitstopRemaining = 10;
                defender.hitstopRemaining = 10;
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

                // Strike contact
                bool isGuarding = defender.bbState == BBState.Blockstun ||
                                  defender.bbState == BBState.BarrierBlockstun ||
                                  (defender.fighter != null && BBInputFrame.IsBack(BBInputFrame.WorldToFacingNumpad(defender.History.LatestFrame.numpad, defender.fighter.FacingDirection)));
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
                    BBContactPresentation.OnGuard(defender.transform.position, chip > 0, crush);

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

                    bool isCounterHit = defender.bbState == BBState.AttackStartup || defender.bbState == BBState.AttackActive;
                    bool isFatal = attacker.currentAttack != null && attacker.currentAttack.fatalCounter && isCounterHit;

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
                    if (BBMatchScreenDirector.Instance != null && BBMatchScreenDirector.Instance.Stats != null)
                    {
                        BBMatchScreenDirector.Instance.Stats.RegisterHit(attacker == p1Brain, scaledDmg, moveKind == BBMoveKind.DistortionDrive);
                    }

                    // Hitstun with combo duration decay (spec 4.15)
                    int decayedHitstun = attacker.comboState.CalculateDecayedHitstun(hitstun, defender.isGrounded);
                    if (isCounterHit) decayedHitstun = Mathf.RoundToInt(decayedHitstun * 1.5f);

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
                    BBContactPresentation.OnCleanHit(defender.transform.position, attacker, isCounterHit, moveKind == BBMoveKind.DistortionDrive);

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
