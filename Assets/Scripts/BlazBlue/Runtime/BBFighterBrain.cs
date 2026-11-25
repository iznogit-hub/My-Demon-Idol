using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Audio;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// BlazBlue fighter brain component. Mounted on each fighter when ControlScheme == BlazBlue4Button.
    /// Manages 4-button input sampling, history, command interpretation, discrete state machine,
    /// gauges, combat subsystems, and zero-allocation collision box generation for BBMatchSim.
    /// </summary>
    public class BBFighterBrain : MonoBehaviour
    {
        [Header("Profile & Tuning")]
        public BBCharacterProfile profile;
        public BBSystemTuning tuning;
        public BBBodyBoxes bodyBoxes;

        [Header("Components")]
        public FighterController fighter;
        public BBInputSampler sampler;
        public BBRapidCancel rapidCancel;
        public BBBarrier barrierSys;
        public BBOverdrive overdrive;
        public BBAnimationDriver animDriver;
        public IBBDrive drive;

        public int FacingDirection => fighter != null ? fighter.FacingDirection : 1;

        [Header("Sim State")]
        public BBState bbState = BBState.Neutral;
        public int stateTick = 0;
        public Vector2 simVelocity = Vector2.zero;
        public bool isGrounded = true;
        public int hitstopRemaining = 0;
        public int hitstunRemaining = 0;
        public int blockstunRemaining = 0;
        public int throwTechWindow = 0;
        public float throwPendingDamage = 0f;
        public BBFighterBrain throwAttacker = null;
        public BBInvuln currentInvuln = BBInvuln.None;
        public int burstInvulnTicksRemaining = 0;
        public BBWakeupAction wakeupAction = BBWakeupAction.Natural;
        public Func<int, BBInputFrame> customInputProvider = null;

        [Header("Pillar Combat Feel & Invulnerability Timers")]
        public int guardEnteredTick = -999;
        public int ticksSinceExitedStun = 999;
        public int wakeupThrowInvulnTicks = 0;
        public int techRollStrikeInvulnTicks = 0;
        private bool _wasGuardingPrevFrame = false;
        private Renderer[] _cachedRenderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _RimColorProp = Shader.PropertyToID("_RimColor");
        private static readonly int _EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private int _rimFlashTicksRemaining = 0;

        [Header("Active Attack")]
        public BBAttackData currentAttack;
        public int attackTick = 0;
        public BBHitMask attackHitMask;
        public float ActiveDamageScale = 1f;
        public float ActiveReachScale = 1f;
        public int SignatureHitstopBonus;
        public event System.Action<BBAttackData> OnAttackStarted;
        public static event System.Action<BBFighterBrain, BBAttackData> OnAnyAttackStarted;

        [System.NonSerialized] public BBFighterBrain opponentBrain;

        public BBInputHistory History { get; private set; } = new BBInputHistory();
        public BBCommandInterpreter Interpreter { get; private set; } = new BBCommandInterpreter();
        public BBEvaluatedCommand CurrentCommand { get; private set; }

        [Header("Gauges & Combos")]
        public float health = 10000f;
        public float maxHealth = 10000f;
        public BBGauges gauges;
        public BBChainState chainState;
        public BBComboState comboState;

        // Convenience properties forwarding to gauges
        public float heat { get => gauges.heat; set => gauges.heat = value; }
        public float barrier { get => gauges.barrier; set => gauges.barrier = value; }
        public float burst { get => gauges.burst; set => gauges.burst = value; }
        public float currentHealth { get => health; set => health = value; }
        public float heatMeter { get => gauges.heat; set => gauges.heat = value; }
        public float barrierMeter { get => gauges.barrier; set => gauges.barrier = value; }
        public float burstMeter { get => gauges.burst; set => gauges.burst = value; }
        public bool inDanger { get => gauges.isDanger; set => gauges.isDanger = value; }
        public bool inOverdrive { get => gauges.isOverdriveActive; set => gauges.isOverdriveActive = value; }
        public int hitstunRemainingTicks => hitstunRemaining;
        public bool isOverdriveActive { get => gauges.isOverdriveActive; set => gauges.isOverdriveActive = value; }
        public int overdriveTicksRemaining { get => gauges.overdriveTicksRemaining; set => gauges.overdriveTicksRemaining = value; }

        private void Awake()
        {
            if (fighter == null) fighter = GetComponent<FighterController>();
            if (sampler == null) sampler = GetComponent<BBInputSampler>();
            if (tuning == null) tuning = BBSystemTuning.Default;

            if (rapidCancel == null) rapidCancel = GetComponent<BBRapidCancel>() ?? gameObject.AddComponent<BBRapidCancel>();
            if (barrierSys == null) barrierSys = GetComponent<BBBarrier>() ?? gameObject.AddComponent<BBBarrier>();
            if (overdrive == null) overdrive = GetComponent<BBOverdrive>() ?? gameObject.AddComponent<BBOverdrive>();
            if (animDriver == null) animDriver = GetComponent<BBAnimationDriver>() ?? gameObject.AddComponent<BBAnimationDriver>();
            if (drive == null) drive = GetComponent<IBBDrive>();

            if (bodyBoxes == null)
            {
                if (profile != null && profile.characterId.Contains("Aika")) bodyBoxes = BBBodyBoxes.CreateAikaPreset();
                else if (profile != null && profile.characterId.Contains("UnityChan")) bodyBoxes = BBBodyBoxes.CreateUnityChanPreset();
                else if (profile != null && profile.characterId.Contains("PicoChan")) bodyBoxes = BBBodyBoxes.CreatePicoChanPreset();
                else bodyBoxes = BBBodyBoxes.CreateAkioPreset();
            }

            maxHealth = profile != null ? profile.maxHealth : 10000f;
            health = maxHealth;
            _cachedRenderers = GetComponentsInChildren<Renderer>(true);
            _propBlock = new MaterialPropertyBlock();
            gauges.Initialize(tuning);
            chainState.Reset();
            comboState.Reset();
        }

        private void OnEnable()
        {
            // If BBMatchSim is not present or not active, self-subscribe to tick
            if (BBMatchSim.Instance == null)
            {
                if (CombatTickEngine.Instance != null)
                {
                    CombatTickEngine.Instance.OnTick += HandleCombatTick;
                }
            }
        }

        private void OnDisable()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        public void ReleaseSoloTick()
        {
            if (CombatTickEngine.Instance != null)
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
        }

        public void StepInput(int tick)
        {
            if (fighter == null) return;

            BBInputFrame frame;
            if (customInputProvider != null)
            {
                frame = customInputProvider(tick);
            }
            else if (fighter != null && !fighter.isPlayerControlled)
            {
                frame = SampleCpuFrame(tick);
            }
            else if (sampler != null && sampler.isPlayerControlled)
            {
                frame = sampler.SampleFrameForTick(tick, fighter.FacingDirection);
            }
            else
            {
                frame = SampleCpuFrame(tick);
            }
            History.PushFrame(frame, tick, fighter.FacingDirection);

            bool inAttack = bbState == BBState.AttackStartup || bbState == BBState.AttackActive || bbState == BBState.AttackRecovery;
            bool inBlockstun = bbState == BBState.Blockstun || bbState == BBState.BarrierBlockstun;
            bool inHitstun = bbState == BBState.Hitstun;
            bool isGuarding = inBlockstun || BBInputFrame.IsBack(BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection));

            CurrentCommand = Interpreter.Evaluate(
                History,
                tick,
                fighter.FacingDirection,
                isGrounded,
                inAttack,
                inBlockstun,
                isGuarding,
                inHitstun);

            if (CurrentCommand.commandType == BBCommandType.Barrier && barrierSys != null)
            {
                barrierSys.NotifyBarrierPressed(tick);
            }

            bool isPressingGuard = BBInputFrame.IsBack(BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection)) || (CurrentCommand.commandType == BBCommandType.Barrier);
            if (isPressingGuard && !_wasGuardingPrevFrame)
            {
                guardEnteredTick = tick;
            }
            _wasGuardingPrevFrame = isPressingGuard;
        }

        BBInputFrame SampleCpuFrame(int tick)
        {
            var ai = GetComponent<BBAIController>();
            if (ai == null) ai = gameObject.AddComponent<BBAIController>();
            if (opponentBrain == null)
            {
                if (fighter != null && fighter.opponent != null)
                {
                    opponentBrain = fighter.opponent.GetComponent<BBFighterBrain>();
                }
                else if (BBMatchSim.Instance != null)
                {
                    opponentBrain = (BBMatchSim.Instance.p1Brain == this) ? BBMatchSim.Instance.p2Brain : BBMatchSim.Instance.p1Brain;
                }
            }
            return ai.GenerateInput(this, opponentBrain, tick);
        }

        public void HandleCombatTick(int tick)
        {
            ticksSinceExitedStun++;
            StepInput(tick);
            if (hitstopRemaining <= 0 && bbState != BBState.Hitstun)
            {
                ExecuteCommands(tick);
            }
            AdvanceState(tick);
            MirrorHealthToFighter();
        }

        public bool TryStartMoveId(string moveId)
        {
            var attack = FindAttackByMoveId(moveId);
            Debug.Log($"[TryStartMoveId] thisId={GetHashCode()} pIdx={sampler?.playerIndex} moveId={moveId} hasSubs={(OnAttackStarted != null)} found={(attack != null ? attack.name : "null")} canExec={(attack != null ? CanExecuteMove(attack).ToString() : "false")} bbState={bbState}");
            if (attack == null || !CanExecuteMove(attack)) return false;
            StartAttack(attack);
            return currentAttack == attack;
        }

        public void SetAuthoritativeHealth(float value)
        {
            health = Mathf.Max(0f, value);
            MirrorHealthToFighter();
        }

        public void ReceiveAuthoritativeDamage(float amount)
        {
            if (amount <= 0f) return;
            SetAuthoritativeHealth(health - amount);
        }

        public void MirrorHealthToFighter()
        {
            if (fighter == null) return;
            if (maxHealth > 0f) fighter.SetMaxHealthDirect(maxHealth);
            float previous = fighter.currentHealth;
            fighter.SetHealthDirect(health);
            if (!Mathf.Approximately(previous, health))
            {
                fighter.RaiseHealthChanged();
            }
            if (health <= 0f && fighter.State != FighterState.Dead)
            {
                fighter.SetState(FighterState.Dead);
            }
        }

        public void SetHealthDirect(float value)
        {
            health = value;
            MirrorHealthToFighter();
        }

        /// <summary>
        /// Resets the fighter to pristine round-start conditions according to fighting-game rules.
        /// Restores max health, returns state to Neutral, clears hitstop, hitstun, blockstun, combos,
        /// resets meters (heat, burst, barrier) according to tuning, and purges input buffers and latches.
        /// </summary>
        public void ResetForRound()
        {
            maxHealth = profile != null ? profile.maxHealth : 10000f;
            health = maxHealth;
            bbState = BBState.Neutral;
            stateTick = 0;
            simVelocity = Vector2.zero;
            isGrounded = true;
            hitstopRemaining = 0;
            hitstunRemaining = 0;
            blockstunRemaining = 0;
            throwTechWindow = 0;
            throwPendingDamage = 0f;
            throwAttacker = null;
            currentInvuln = BBInvuln.None;
            burstInvulnTicksRemaining = 0;
            wakeupAction = BBWakeupAction.Natural;
            currentAttack = null;
            attackTick = 0;
            attackHitMask = new BBHitMask();
            ActiveDamageScale = 1f;
            ActiveReachScale = 1f;
            SignatureHitstopBonus = 0;
            guardEnteredTick = -999;
            ticksSinceExitedStun = 999;
            wakeupThrowInvulnTicks = 0;
            techRollStrikeInvulnTicks = 0;
            _wasGuardingPrevFrame = false;
            _rimFlashTicksRemaining = 0;
            if (_cachedRenderers != null)
            {
                for (int i = 0; i < _cachedRenderers.Length; i++)
                {
                    if (_cachedRenderers[i] != null) _cachedRenderers[i].SetPropertyBlock(null);
                }
            }

            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = new Vector3(transform.position.x, 0f, 0f);
            if (cc != null) cc.enabled = true;

            chainState.Reset();
            comboState.Reset();

            if (tuning != null)
            {
                gauges.Initialize(tuning);
            }
            else
            {
                gauges.heat = 0f;
                gauges.barrier = 100f;
                gauges.burst = 100f;
                gauges.isDanger = false;
                gauges.isOverdriveActive = false;
                gauges.overdriveTicksRemaining = 0;
            }

            History?.Clear();

            if (sampler != null)
            {
                sampler.ResetLatches();
            }

            var ai = GetComponent<BBAIController>();
            if (ai != null)
            {
                ai.ResetAI();
            }

            if (animDriver != null)
            {
                animDriver.ResetAttackLayer(immediate: true);
                animDriver.UpdateLocomotionState(BBState.Neutral, 0, true);
            }

            MirrorHealthToFighter();
        }

        private static readonly System.Collections.Generic.HashSet<string> s_LoggedMissingMoves = new System.Collections.Generic.HashSet<string>();
        private sbyte jumpDirection = 8;

        public void ExecuteCommands(int tick)
        {
            if (!CurrentCommand.IsValid || profile == null) return;

            // 1. Psychic Burst & Overdrive Macro (A+B+C+D) - Can break combos in hitstun/blockstun!
            if (CurrentCommand.commandType == BBCommandType.BurstOrOverdrive)
            {
                if (bbState == BBState.Hitstun || bbState == BBState.Blockstun || bbState == BBState.BarrierBlockstun)
                {
                    if (TryExecuteDefensiveBurst(tick))
                    {
                        ConsumeCurrentCommand();
                        return;
                    }
                }
                else if (bbState == BBState.Neutral || bbState == BBState.WalkForward || bbState == BBState.WalkBackward || bbState == BBState.Crouching)
                {
                    if (TryExecuteOffensiveBurstOrOverdrive(tick))
                    {
                        ConsumeCurrentCommand();
                        return;
                    }
                }
            }

            // 2. Rapid Cancel Macro (A+B+C) - Cancels active attack on contact into neutral
            if (CurrentCommand.commandType == BBCommandType.RapidCancel)
            {
                if (TryExecuteRapidCancel(tick))
                {
                    ConsumeCurrentCommand();
                    return;
                }
            }

            // 3. Counter Assault Macro (6+A+B in blockstun) - Spends 50% Heat, breaks blockstun with invincible counter strike
            if (CurrentCommand.commandType == BBCommandType.CounterAssault)
            {
                if (bbState == BBState.Blockstun || bbState == BBState.BarrierBlockstun)
                {
                    float cost = tuning != null ? tuning.costCA : 50f;
                    if (gauges.TrySpendHeat(cost))
                    {
                        blockstunRemaining = 0;
                        hitstopRemaining = 0;
                        string caMoveId = MoveIdFor(CurrentCommand);
                        string caFallback = FallbackMoveId(CurrentCommand);
                        if (TryStartMoveId(caMoveId) || (!string.IsNullOrEmpty(caFallback) && TryStartMoveId(caFallback)))
                        {
                            currentInvuln |= BBInvuln.Strike | BBInvuln.Throw | BBInvuln.Projectile;
                            ConsumeCurrentCommand();
                            return;
                        }
                    }
                }
            }

            if (bbState == BBState.Hitstun || bbState == BBState.Blockstun || bbState == BBState.BarrierBlockstun || bbState == BBState.Thrown)
                return;

            if (CurrentCommand.commandType == BBCommandType.ForwardDash)
            {
                if (isGrounded && CanDash())
                {
                    StartDash(BBState.ForwardDash);
                    ConsumeCurrentCommand();
                }
                return;
            }
            if (CurrentCommand.commandType == BBCommandType.Backdash)
            {
                if (isGrounded && CanDash())
                {
                    StartDash(BBState.Backdash);
                    ConsumeCurrentCommand();
                }
                return;
            }

            if (IsDriveCommand(CurrentCommand.commandType))
            {
                if (drive != null && drive.TryStartDrive(this, CurrentCommand.facingNumpad, tick))
                {
                    ConsumeCurrentCommand();
                }
                return;
            }

            string moveId = MoveIdFor(CurrentCommand);
            if (CurrentCommand.commandType == BBCommandType.Special236 || (moveId != null && moveId.Contains("236")))
            {
                Debug.Log($"[CmdSpecial] pIdx={sampler?.playerIndex} tick={tick} cmd={CurrentCommand.commandType} btn={CurrentCommand.button} moveId={moveId}");
            }
            if (string.IsNullOrEmpty(moveId)) return;
            string fallback = FallbackMoveId(CurrentCommand);

            bool moveExists = FindAttackByMoveId(moveId) != null || (!string.IsNullOrEmpty(fallback) && FindAttackByMoveId(fallback) != null);

            if (TryStartMoveId(moveId) || (!string.IsNullOrEmpty(fallback) && TryStartMoveId(fallback)))
            {
                if (ticksSinceExitedStun <= 1)
                {
                    BBContactPresentation.OnReversal(transform.position + Vector3.up * 1f);
                    BBBattleHUDController.Instance?.ShowAnnouncement("REVERSAL!", 45);
                }
                ConsumeCurrentCommand();
            }
            else
            {
                if (!moveExists)
                {
                    string charId = fighter != null && fighter.appliedDefinition != null ? fighter.appliedDefinition.id : (profile != null ? profile.characterId : "unknown");
                    string key = $"{charId}:{moveId}";
                    if (s_LoggedMissingMoves.Add(key))
                    {
                        UnityEngine.Debug.LogWarning($"[BBFighterBrain] Missing move '{moveId}' for fighter '{charId}'. No action taken.");
                    }
                    ConsumeCurrentCommand();
                }
                else
                {
                    // Move exists in profile but cannot start yet (e.g. during hitstop or recovery before cancel window).
                    // Keep alive up to 5 ticks (5-tick reversal buffer).
                    if (tick - CurrentCommand.tick > 5)
                    {
                        ConsumeCurrentCommand();
                    }
                }
            }
        }

        bool CanDash()
        {
            return bbState == BBState.Neutral || bbState == BBState.WalkForward || bbState == BBState.WalkBackward || bbState == BBState.Crouching;
        }

        void StartDash(BBState dashState)
        {
            bbState = dashState;
            stateTick = 0;
            if (animDriver != null)
            {
                sbyte facingNumpad = fighter != null ? BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection) : (sbyte)5;
                animDriver.UpdateLocomotionState(bbState, facingNumpad, isGrounded);
            }
        }

        static bool IsDriveCommand(BBCommandType type)
        {
            return type == BBCommandType.Normal5D
                || type == BBCommandType.Normal2D
                || type == BBCommandType.Normal6D
                || type == BBCommandType.Normal4D
                || type == BBCommandType.NormalAirD;
        }

        static string MoveIdFor(BBEvaluatedCommand command)
        {
            string button = ButtonLetter(command.button);
            switch (command.commandType)
            {
                case BBCommandType.Normal5A: return "5A";
                case BBCommandType.Normal5B: return "5B";
                case BBCommandType.Normal5C: return "5C";
                case BBCommandType.Normal2A: return "2A";
                case BBCommandType.Normal2B: return "2B";
                case BBCommandType.Normal2C: return "2C";
                case BBCommandType.Normal6A: return "6A";
                case BBCommandType.Normal6B: return "6B";
                case BBCommandType.Normal6C: return "6C";
                case BBCommandType.Normal3C: return "3C";
                case BBCommandType.NormalAirA: return "j.A";
                case BBCommandType.NormalAirB: return "j.B";
                case BBCommandType.NormalAirC: return "j.C";
                case BBCommandType.Special236: return "236" + button;
                case BBCommandType.Special214: return "214" + button;
                case BBCommandType.Special623: return "623" + button;
                case BBCommandType.Special421: return "421" + button;
                case BBCommandType.Special22: return "22" + button;
                case BBCommandType.Special41236: return "41236" + button;
                case BBCommandType.Special63214: return "63214" + button;
                case BBCommandType.Special360: return "360" + button;
                case BBCommandType.SpecialChargeBF: return "46" + button;
                case BBCommandType.SpecialChargeDU: return "28" + button;
                case BBCommandType.Distortion236236: return "236236" + button;
                case BBCommandType.Distortion214214: return "214214" + button;
                case BBCommandType.Distortion632146: return "632146" + button;
                case BBCommandType.Throw: return command.facingNumpad == 4 ? "4BC" : "BC";
                case BBCommandType.CrushTrigger: return "CT";
                case BBCommandType.CounterAssault: return "CA";
                default: return null;
            }
        }

        static string FallbackMoveId(BBEvaluatedCommand command)
        {
            switch (command.commandType)
            {
                case BBCommandType.Normal6A: return "5A";
                case BBCommandType.Normal6B: return "5B";
                case BBCommandType.Normal6C: return "5C";
                case BBCommandType.Normal3C: return "5C";
                case BBCommandType.Normal2A: return "5A";
                case BBCommandType.Normal2B: return "5B";
                case BBCommandType.Normal2C: return "5C";
                case BBCommandType.Distortion236236:
                case BBCommandType.Distortion214214:
                case BBCommandType.Distortion632146:
                    return "236236C";
                case BBCommandType.Throw:
                    return "BC";
                case BBCommandType.CrushTrigger:
                    return "5C";
                case BBCommandType.CounterAssault:
                    return "5C";
                default:
                    return null;
            }
        }

        static string ButtonLetter(BBButton button)
        {
            if ((button & BBButton.D) != 0) return "D";
            if ((button & BBButton.C) != 0) return "C";
            if ((button & BBButton.B) != 0) return "B";
            return "A";
        }

        public void AdvanceState(int tick)
        {
            if (hitstopRemaining > 0)
            {
                hitstopRemaining--;
                if (CurrentCommand.IsValid)
                {
                    var cmd = CurrentCommand;
                    cmd.tick++;
                    CurrentCommand = cmd;
                }
                if (History != null)
                {
                    History.FreezeBufferAging(1);
                }
                return; // Frozen during hitstop
            }

            stateTick++;

            if (_rimFlashTicksRemaining > 0)
            {
                _rimFlashTicksRemaining--;
                if (_rimFlashTicksRemaining <= 0 && _cachedRenderers != null)
                {
                    for (int i = 0; i < _cachedRenderers.Length; i++)
                    {
                        if (_cachedRenderers[i] != null) _cachedRenderers[i].SetPropertyBlock(null);
                    }
                }
            }

            // Tick gauges (passive regen, low-HP regen, OD countdown, barrier drain)
            bool isBarriering = CurrentCommand.commandType == BBCommandType.Barrier && !gauges.isDanger;
            gauges.Tick(tuning, health / maxHealth, isBarriering);

            if (drive != null)
            {
                drive.InOverdrive = isOverdriveActive;
                drive.TickDrive(this, tick);
            }

            // Update invulnerability mask for current tick
            currentInvuln = BBInvuln.None;
            if (wakeupThrowInvulnTicks > 0)
            {
                wakeupThrowInvulnTicks--;
                currentInvuln |= BBInvuln.Throw;
            }
            if (techRollStrikeInvulnTicks > 0)
            {
                techRollStrikeInvulnTicks--;
                currentInvuln |= BBInvuln.Strike;
            }
            if (burstInvulnTicksRemaining > 0)
            {
                burstInvulnTicksRemaining--;
                currentInvuln |= BBInvuln.Strike | BBInvuln.Throw | BBInvuln.Projectile;
            }
            if (currentAttack != null && currentAttack.invuln != null && currentAttack.invuln.Length > 0)
            {
                for (int i = 0; i < currentAttack.invuln.Length; i++)
                {
                    if (currentAttack.invuln[i].IsActiveAt(attackTick))
                    {
                        currentInvuln |= currentAttack.invuln[i].mask;
                    }
                }
            }

            // State-specific timers
            switch (bbState)
            {
                case BBState.Thrown:
                    currentInvuln |= BBInvuln.Throw;
                    if (throwTechWindow > 0)
                    {
                        throwTechWindow--;
                        bool techInput = CurrentCommand.commandType == BBCommandType.Throw ||
                                         (History != null && History.IsChordBuffered(BBButton.B, BBButton.C, 3, 3)) ||
                                         ((History != null && (History.LatestFrame.pressed & ((byte)BBButton.B | (byte)BBButton.C)) == ((byte)BBButton.B | (byte)BBButton.C)));
                        if (techInput)
                        {
                            throwPendingDamage = 0f;
                            PerformThrowTech();
                            break;
                        }
                    }

                    if (throwPendingDamage > 0f && throwTechWindow <= 0)
                    {
                        ReceiveAuthoritativeDamage(throwPendingDamage);
                        throwPendingDamage = 0f;
                    }

                    if (hitstunRemaining > 0) hitstunRemaining--;
                    if (hitstunRemaining <= 0)
                    {
                        if (throwPendingDamage > 0f)
                        {
                            ReceiveAuthoritativeDamage(throwPendingDamage);
                            throwPendingDamage = 0f;
                        }
                        bbState = BBState.DownWait; // knockdown
                        stateTick = 0;
                        hitstunRemaining = 24;
                        throwAttacker = null;
                        if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                    }
                    break;

                case BBState.Blockstun:
                case BBState.BarrierBlockstun:
                    if (blockstunRemaining > 0)
                    {
                        blockstunRemaining--;
                    }
                    else
                    {
                        bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                        stateTick = 0;
                        ticksSinceExitedStun = 0;
                        chainState.Reset();
                        comboState.Reset();
                        if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                    }
                    break;

                case BBState.Hitstun:
                    if (hitstunRemaining > 0)
                    {
                        hitstunRemaining--;
                    }
                    else
                    {
                        bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                        stateTick = 0;
                        ticksSinceExitedStun = 0;
                        chainState.Reset();
                        comboState.Reset();
                        if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                    }
                    break;

                case BBState.Crumple:
                    if (hitstunRemaining > 0)
                    {
                        hitstunRemaining--;
                    }
                    else
                    {
                        bbState = BBState.DownWait;
                        stateTick = 0;
                        hitstunRemaining = 32;
                        ticksSinceExitedStun = 0;
                        if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                    }
                    break;

                case BBState.DownWait:
                    currentInvuln |= BBInvuln.Strike | BBInvuln.Throw;
                    if (hitstunRemaining > 0) hitstunRemaining--;

                    // Okizeme directional wakeup choices
                    if (History != null)
                    {
                        sbyte facingDir = fighter != null ? (sbyte)fighter.FacingDirection : (sbyte)1;
                        sbyte facingNum = BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, facingDir);
                        byte pressed = History.LatestFrame.pressed;

                        // Wakeup Low/Mid Kick (2/1/3 + Button)
                        if ((facingNum == 1 || facingNum == 2 || facingNum == 3) && pressed != 0)
                        {
                            bbState = BBState.Wakeup;
                            stateTick = 0;
                            hitstunRemaining = 20;
                            currentInvuln = BBInvuln.None; // Wakeup kicks are counterable
                            wakeupAction = (pressed & (byte)BBButton.B) != 0 ? BBWakeupAction.WakeupMidKick : BBWakeupAction.WakeupLowKick;
                            if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                            break;
                        }
                        // Tech Roll Forward (6 or 9 held)
                        else if ((facingNum == 6 || facingNum == 9) && hitstunRemaining <= 12)
                        {
                            bbState = BBState.Wakeup;
                            stateTick = 0;
                            hitstunRemaining = 16;
                            simVelocity.x = facingDir * 4.5f;
                            wakeupAction = BBWakeupAction.QuickTechRoll;
                            techRollStrikeInvulnTicks = 8;
                            if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                            break;
                        }
                        // Stand Up Back (4 or 7 held)
                        else if ((facingNum == 4 || facingNum == 7) && hitstunRemaining <= 12)
                        {
                            bbState = BBState.Wakeup;
                            stateTick = 0;
                            hitstunRemaining = 18;
                            simVelocity.x = -facingDir * 3.0f;
                            wakeupAction = BBWakeupAction.StandUpBack;
                            if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                            break;
                        }
                    }

                    if (hitstunRemaining <= 0)
                    {
                        bbState = BBState.Wakeup;
                        stateTick = 0;
                        hitstunRemaining = 24; // Wakeup 24f
                        wakeupAction = BBWakeupAction.Natural;
                        if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                    }
                    break;

                case BBState.Wakeup:
                    if (wakeupAction != BBWakeupAction.WakeupLowKick && wakeupAction != BBWakeupAction.WakeupMidKick)
                    {
                        currentInvuln |= BBInvuln.Strike | BBInvuln.Throw;
                    }

                    simVelocity.x *= 0.85f;

                    if (hitstunRemaining > 0) hitstunRemaining--;
                    if (hitstunRemaining <= 0)
                    {
                        simVelocity.x = 0f;
                        bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                        stateTick = 0;
                        ticksSinceExitedStun = 0;
                        wakeupThrowInvulnTicks = 6;
                        chainState.Reset();
                        comboState.Reset();
                        wakeupAction = BBWakeupAction.Natural;
                        if (animDriver != null) animDriver.UpdateLocomotionState(bbState, 5, isGrounded);
                    }
                    break;

                case BBState.AttackStartup:
                case BBState.AttackActive:
                case BBState.AttackRecovery:
                    attackTick++;
                    if (currentAttack != null)
                    {
                        if (attackTick < currentAttack.startupFrames)
                        {
                            bbState = BBState.AttackStartup;
                        }
                        else if (attackTick < currentAttack.startupFrames + currentAttack.activeFrames)
                        {
                            if (bbState != BBState.AttackActive)
                            {
                                BBContactPresentation.OnAttackSwing(this);
                            }
                            bbState = BBState.AttackActive;
                        }
                        else if (attackTick < currentAttack.TotalTicks)
                        {
                            bbState = BBState.AttackRecovery;
                        }
                        else
                        {
                            // Move finished
                            currentAttack = null;
                            attackTick = 0;
                            attackHitMask.Clear();
                            chainState.Reset();
                            currentInvuln = BBInvuln.None;
                            bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                            stateTick = 0;
                        }
                    }
                    else
                    {
                        bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                        stateTick = 0;
                    }
                    break;

                case BBState.ForwardDash:
                    int dashDuration = profile != null ? profile.forwardDashDurationTicks : (tuning != null ? tuning.dashDurationTicks : 12);
                    if (stateTick >= dashDuration)
                    {
                        bbState = BBState.Neutral;
                        stateTick = 0;
                    }
                    break;

                case BBState.Backdash:
                    int bdashDuration = profile != null ? profile.backdashDurationTicks : (tuning != null ? tuning.backdashDurationTicks : 14);
                    int bdashInvuln = profile != null ? profile.backdashInvulnTicks : (tuning != null ? tuning.backdashInvulnTicks : 5);
                    currentInvuln = stateTick < bdashInvuln ? BBInvuln.Strike | BBInvuln.Throw : BBInvuln.None;
                    if (stateTick >= bdashDuration)
                    {
                        bbState = BBState.Neutral;
                        stateTick = 0;
                        currentInvuln = BBInvuln.None;
                    }
                    break;

                case BBState.JumpStartup:
                    int prejump = tuning != null ? tuning.jumpPrejumpTicks : 4;
                    if (stateTick >= prejump)
                    {
                        bbState = BBState.Airborne;
                        isGrounded = false;
                        stateTick = 0;
                        float vLaunch = profile != null ? profile.jumpVerticalLaunch : (tuning != null ? tuning.jumpVerticalLaunch : 8.0f);
                        float hSpeed = profile != null ? profile.jumpHorizontalSpeed : (tuning != null ? tuning.jumpHorizontalSpeed : 3.2f);
                        int facing = fighter != null ? fighter.FacingDirection : 1;
                        simVelocity.y = vLaunch;
                        if (jumpDirection == 9) simVelocity.x = facing * hSpeed;
                        else if (jumpDirection == 7) simVelocity.x = -facing * hSpeed;
                        else simVelocity.x = 0f;
                        if (animDriver != null)
                        {
                            sbyte facingNumpad = fighter != null ? BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection) : (sbyte)5;
                            animDriver.UpdateLocomotionState(bbState, facingNumpad, isGrounded);
                        }
                    }
                    break;
            }

            if (isGrounded &&
                bbState != BBState.AttackStartup && bbState != BBState.AttackActive && bbState != BBState.AttackRecovery &&
                bbState != BBState.Hitstun && bbState != BBState.Blockstun && bbState != BBState.BarrierBlockstun &&
                bbState != BBState.Thrown && bbState != BBState.DownWait && bbState != BBState.Wakeup &&
                bbState != BBState.GuardCrush && bbState != BBState.Dead &&
                bbState != BBState.ForwardDash && bbState != BBState.Backdash &&
                bbState != BBState.JumpStartup && (fighter == null || fighter.State != FighterState.Dead))
            {
                sbyte facingNumpad = fighter != null ? BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection) : (sbyte)5;
                UpdateGroundLocomotion(facingNumpad);
            }
        }

        void UpdateGroundLocomotion(sbyte facingNumpad)
        {
            if (facingNumpad == 7 || facingNumpad == 8 || facingNumpad == 9)
            {
                bbState = BBState.JumpStartup;
                stateTick = 0;
                jumpDirection = facingNumpad;
                if (animDriver != null)
                {
                    animDriver.UpdateLocomotionState(bbState, facingNumpad, isGrounded);
                }
                return;
            }

            BBState targetState;
            if (facingNumpad == 1 || facingNumpad == 2 || facingNumpad == 3)
            {
                targetState = BBState.Crouching;
            }
            else if (facingNumpad == 6)
            {
                targetState = BBState.WalkForward;
            }
            else if (facingNumpad == 4)
            {
                targetState = BBState.WalkBackward;
            }
            else
            {
                targetState = BBState.Neutral;
            }

            if (bbState != targetState)
            {
                bbState = targetState;
                stateTick = 0;
            }
            if (animDriver != null)
            {
                animDriver.UpdateLocomotionState(bbState, facingNumpad, isGrounded);
            }
        }

        public void StartAttack(BBAttackData attack)
        {
            if (attack == null) return;
            currentAttack = attack;
            attackTick = 0;
            attackHitMask.Clear();
            OnAttackStarted?.Invoke(attack);
            OnAnyAttackStarted?.Invoke(this, attack);
            chainState.madeContact = false;
            ActiveDamageScale = 1f;
            ActiveReachScale = 1f;
            SignatureHitstopBonus = 0;
            string charId = fighter != null && fighter.appliedDefinition != null ? fighter.appliedDefinition.id : null;
            if (BBSignatureTable.TryGet(charId, attack.moveId, out var signature))
            {
                ActiveDamageScale = signature.damageScale;
                ActiveReachScale = signature.reachScale;
                SignatureHitstopBonus = signature.hitstopBonus;
                BBContactPresentation.RaisePortraitHook(charId, "signature:" + signature.clipToken);
            }
            if (BBProjectileCaster.ShouldSpawn(charId, attack.moveId))
            {
                BBProjectileCaster.Spawn(fighter, attack.moveId);
            }
            if (attack.kind == BBMoveKind.DistortionDrive || attack.kind == BBMoveKind.AstralHeat || attack.moveId == "236236C")
            {
                gauges.TrySpendHeat(tuning != null ? tuning.costDD : 50f);
                BBSuperFlash.Raise(charId, fighter != null ? fighter.transform : null);
            }
            else if (attack.moveId == "CT" || attack.moveId == "AB" || (CurrentCommand.IsValid && CurrentCommand.commandType == BBCommandType.CrushTrigger))
            {
                gauges.TrySpendHeat(tuning != null ? tuning.costCT : 25f);
            }
            bbState = attack.startupFrames > 0 ? BBState.AttackStartup : BBState.AttackActive;
            stateTick = 0;
            currentInvuln = BBInvuln.None;

            if (animDriver != null)
            {
                var signatureClip = BBSignatureClipFallback.ClipFor(profile, charId, attack.moveId);
                if (signatureClip != null)
                    animDriver.PlayAttackClip(signatureClip, attack.clipFirstFrame, attack.clipLastFrame, attack.TotalTicks, 0);
                else
                    animDriver.PlayAttack(attack);
            }
            if (fighter != null && fighter.animator != null && !string.IsNullOrEmpty(attack.animationTrigger))
            {
                fighter.animator.ResetTrigger(attack.animationTrigger);
                fighter.animator.SetTrigger(attack.animationTrigger);
            }

            if ((attack.button & BBButton.A) != 0)
            {
                chainState.aRepeatCount++;
            }
            else
            {
                chainState.aRepeatCount = 0;
            }
        }

        public BBAttackData FindAttackByMoveId(string targetMoveId)
        {
            if (string.IsNullOrEmpty(targetMoveId)) return null;

            if (profile != null && profile.moveSet is BBMoveSet ms)
            {
                // 1. Direct System Move mappings
                if (targetMoveId == "CT" || targetMoveId == "AB")
                {
                    if (ms.crushTrigger != null) return ms.crushTrigger;
                }
                else if (targetMoveId == "CA" || targetMoveId == "6AB")
                {
                    if (ms.counterAssault != null) return ms.counterAssault;
                }
                else if (targetMoveId == "BC" || targetMoveId == "6BC")
                {
                    if (ms.throwForward != null) return ms.throwForward;
                }
                else if (targetMoveId == "4BC")
                {
                    if (ms.throwBack != null) return ms.throwBack;
                    if (ms.throwForward != null) return ms.throwForward;
                }
                else if (targetMoveId == "j.BC")
                {
                    if (ms.throwAir != null) return ms.throwAir;
                    if (ms.throwForward != null) return ms.throwForward;
                }
                else if (targetMoveId == "EA")
                {
                    if (ms.exceedAccel != null) return ms.exceedAccel;
                }

                // 2. Linear search in moves array
                if (ms.moves != null)
                {
                    for (int i = 0; i < ms.moves.Length; i++)
                    {
                        if (ms.moves[i] != null && ms.moves[i].moveId == targetMoveId)
                            return ms.moves[i];
                    }
                }

                // 2b. If move is 236A special and not in primary 7-move kit, check tuned assets
                if (targetMoveId == "236A" && profile != null)
                {
#if UNITY_EDITOR
                    string charFolder = profile.characterId;
                    if (profile.characterId.Contains("C12") || profile.characterId.Contains("Fina")) charFolder = "Fina";
                    else if (profile.characterId.Contains("C11") || profile.characterId.Contains("Casual")) charFolder = "Casual1";
                    var extraAtk = UnityEditor.AssetDatabase.LoadAssetAtPath<BBAttackData>($"Assets/CombatData/BlazBlue/{charFolder}/BB_{charFolder}_236A.asset");
                    if (extraAtk != null) return extraAtk;
#endif
                }

                // 3. Check serialized system move properties by moveId
                if (ms.crushTrigger != null && ms.crushTrigger.moveId == targetMoveId) return ms.crushTrigger;
                if (ms.counterAssault != null && ms.counterAssault.moveId == targetMoveId) return ms.counterAssault;
                if (ms.throwForward != null && ms.throwForward.moveId == targetMoveId) return ms.throwForward;
                if (ms.throwBack != null && ms.throwBack.moveId == targetMoveId) return ms.throwBack;
                if (ms.throwAir != null && ms.throwAir.moveId == targetMoveId) return ms.throwAir;
                if (ms.burst != null && ms.burst.moveId == targetMoveId) return ms.burst;
                if (ms.overdriveStart != null && ms.overdriveStart.moveId == targetMoveId) return ms.overdriveStart;
                if (ms.exceedAccel != null && ms.exceedAccel.moveId == targetMoveId) return ms.exceedAccel;
                if (ms.astralHeat != null && ms.astralHeat.moveId == targetMoveId) return ms.astralHeat;
            }

            return null;
        }

        public bool CanExecuteMove(BBAttackData move)
        {
            if (move == null) return false;

            // Check Heat requirements for super moves and system mechanics
            float requiredHeat = 0f;
            if (move.kind == BBMoveKind.DistortionDrive || move.moveId == "236236C")
            {
                requiredHeat = tuning != null ? tuning.costDD : 50f;
            }
            else if (move.moveId == "CT" || move.moveId == "AB" || (CurrentCommand.IsValid && CurrentCommand.commandType == BBCommandType.CrushTrigger))
            {
                requiredHeat = tuning != null ? tuning.costCT : 25f;
            }
            else if (move.moveId == "CA" || move.moveId == "6AB" || (CurrentCommand.IsValid && CurrentCommand.commandType == BBCommandType.CounterAssault))
            {
                requiredHeat = tuning != null ? tuning.costCA : 50f;
            }

            if (requiredHeat > 0f && gauges.heat < requiredHeat)
            {
                return false;
            }

            if (isGrounded && !move.groundOk) return false;
            if (!isGrounded && !move.airOk) return false;

            if (bbState == BBState.Neutral || bbState == BBState.Crouching ||
                bbState == BBState.WalkForward || bbState == BBState.WalkBackward ||
                bbState == BBState.Airborne)
            {
                return true;
            }

            if (bbState == BBState.AttackStartup || bbState == BBState.AttackActive || bbState == BBState.AttackRecovery)
            {
                if (currentAttack != null)
                {
                    return BBCancelRules.CanCancel(in chainState, currentAttack, move, 0, attackTick, onHit: true, onBlock: false, isGrounded: isGrounded);
                }
            }

            return false;
        }

        /// <summary>
        /// Populates world-space hitboxes, hurtboxes, and pushboxes into a zero-allocation BBBoxBuffer.
        /// </summary>
        public void PopulateWorldBoxes(BBBoxBuffer buffer)
        {
            PopulateWorldBoxes(buffer, transform.position);
        }

        public void PopulateWorldBoxes(BBBoxBuffer buffer, Vector2 position)
        {
            if (buffer == null) return;
            float posX = position.x;
            float posY = position.y;
            int facing = fighter != null ? fighter.FacingDirection : 1;

            // 1. Pushbox
            BBWorldBox push = GetPushBox();
            buffer.Add(in push);

            // 2. Active attack boxes
            if (currentAttack != null && currentAttack.boxes != null)
            {
                int beforeCount = buffer.count;
                currentAttack.boxes.GetBoxes(attackTick, buffer, posX, posY, facing);
                if (!Mathf.Approximately(ActiveReachScale, 1f))
                {
                    for (int i = beforeCount; i < buffer.count; i++)
                    {
                        ref BBWorldBox hit = ref buffer.boxes[i];
                        if (hit.kind != BBBoxKind.Hit) continue;
                        float hx = hit.HalfWidth * ActiveReachScale;
                        float cx = hit.CenterX;
                        hit.minX = cx - hx;
                        hit.maxX = cx + hx;
                    }
                }

                // Spec 6.4: During active ticks, add extended hurtbox over attacking limb (70% size, 0.04m behind leading edge) unless disjoint
                if (bbState == BBState.AttackActive)
                {
                    int boxCount = buffer.count;
                    for (int i = beforeCount; i < boxCount; i++)
                    {
                        ref BBWorldBox hit = ref buffer.boxes[i];
                        if (hit.kind == BBBoxKind.Hit && (hit.flags & BBBox.FLAG_DISJOINT) == 0)
                        {
                            float halfW = hit.HalfWidth * 0.70f;
                            float halfH = hit.HalfHeight * 0.70f;
                            float centerX = hit.CenterX - (facing * 0.04f);
                            float centerY = hit.CenterY;
                            buffer.Add(new BBWorldBox(
                                centerX - halfW,
                                centerY - halfH,
                                centerX + halfW,
                                centerY + halfH,
                                BBBoxKind.Hurt,
                                hit.hitIndex,
                                hit.attribute,
                                0
                            ));
                        }
                    }
                }
            }

            // 3. Body hurtboxes (Head, Body, Foot)
            if (bodyBoxes != null)
            {
                if (!isGrounded)
                {
                    for (int i = 0; i < bodyBoxes.airHurtBoxes.Length; i++)
                    {
                        buffer.AddLocal(in bodyBoxes.airHurtBoxes[i], posX, posY, facing);
                    }
                }
                else if (bbState == BBState.Crouching)
                {
                    for (int i = 0; i < bodyBoxes.crouchHurtBoxes.Length; i++)
                    {
                        buffer.AddLocal(in bodyBoxes.crouchHurtBoxes[i], posX, posY, facing);
                    }
                    // Throw hurtbox
                    buffer.AddLocal(in bodyBoxes.crouchThrowHurtBox, posX, posY, facing);
                }
                else
                {
                    for (int i = 0; i < bodyBoxes.standHurtBoxes.Length; i++)
                    {
                        buffer.AddLocal(in bodyBoxes.standHurtBoxes[i], posX, posY, facing);
                    }
                    // Throw hurtbox (only when standing / neutral / blockstun)
                    if (bbState != BBState.Hitstun && bbState != BBState.Thrown)
                    {
                        buffer.AddLocal(in bodyBoxes.standThrowHurtBox, posX, posY, facing);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current world-space pushbox.
        /// </summary>
        public BBWorldBox GetPushBox()
        {
            return GetPushBox(transform.position);
        }

        public BBWorldBox GetPushBox(Vector2 position)
        {
            float posX = position.x;
            float posY = position.y;
            int facing = fighter != null ? fighter.FacingDirection : 1;

            if (bodyBoxes != null)
            {
                if (!isGrounded) return BBWorldBox.FromLocal(in bodyBoxes.airPushBox, posX, posY, facing);
                if (bbState == BBState.Crouching) return BBWorldBox.FromLocal(in bodyBoxes.crouchPushBox, posX, posY, facing);
                return BBWorldBox.FromLocal(in bodyBoxes.standPushBox, posX, posY, facing);
            }

            // Fallback pushbox
            return new BBWorldBox(posX - 0.35f, posY, posX + 0.35f, posY + 1.8f, BBBoxKind.Push);
        }

        public void PerformThrowTech()
        {
            var attacker = throwAttacker;
            throwAttacker = null;
            throwTechWindow = 0;
            throwPendingDamage = 0f;

            bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
            stateTick = 0;
            hitstopRemaining = 0;
            hitstunRemaining = 0;
            currentAttack = null;
            attackTick = 0;
            CurrentCommand = default;
            if (History != null) History.Clear();

            if (attacker != null)
            {
                attacker.throwAttacker = null;
                attacker.throwTechWindow = 0;
                attacker.throwPendingDamage = 0f;
                attacker.bbState = attacker.isGrounded ? BBState.Neutral : BBState.Airborne;
                attacker.stateTick = 0;
                attacker.attackTick = 0;
                attacker.hitstopRemaining = 0;
                attacker.currentAttack = null;
                attacker.CurrentCommand = default;
                if (attacker.History != null) attacker.History.Clear();

                // Push apart: neutral, no advantage
                float sepDist = 0.85f;
                var cc1 = GetComponent<CharacterController>();
                var cc2 = attacker.GetComponent<CharacterController>();
                if (cc1 != null) cc1.enabled = false;
                if (cc2 != null) cc2.enabled = false;

                if (transform.position.x < attacker.transform.position.x)
                {
                    transform.position += new Vector3(-sepDist, 0, 0);
                    attacker.transform.position += new Vector3(sepDist, 0, 0);
                    if (BBMatchSim.Instance != null)
                    {
                        if (this == BBMatchSim.Instance.p1Brain)
                        {
                            BBMatchSim.Instance.p1SimPosition.x -= sepDist;
                            BBMatchSim.Instance.p2SimPosition.x += sepDist;
                        }
                        else
                        {
                            BBMatchSim.Instance.p2SimPosition.x -= sepDist;
                            BBMatchSim.Instance.p1SimPosition.x += sepDist;
                        }
                        BBMatchSim.Instance.p1SimPosition.x = BBMatchSim.ClampToArena(BBMatchSim.Instance.p1SimPosition.x);
                        BBMatchSim.Instance.p2SimPosition.x = BBMatchSim.ClampToArena(BBMatchSim.Instance.p2SimPosition.x);
                    }
                }
                else
                {
                    transform.position += new Vector3(sepDist, 0, 0);
                    attacker.transform.position += new Vector3(-sepDist, 0, 0);
                    if (BBMatchSim.Instance != null)
                    {
                        if (this == BBMatchSim.Instance.p1Brain)
                        {
                            BBMatchSim.Instance.p1SimPosition.x += sepDist;
                            BBMatchSim.Instance.p2SimPosition.x -= sepDist;
                        }
                        else
                        {
                            BBMatchSim.Instance.p2SimPosition.x += sepDist;
                            BBMatchSim.Instance.p1SimPosition.x -= sepDist;
                        }
                        BBMatchSim.Instance.p1SimPosition.x = BBMatchSim.ClampToArena(BBMatchSim.Instance.p1SimPosition.x);
                        BBMatchSim.Instance.p2SimPosition.x = BBMatchSim.ClampToArena(BBMatchSim.Instance.p2SimPosition.x);
                    }
                }

                if (cc1 != null) cc1.enabled = true;
                if (cc2 != null) cc2.enabled = true;
            }

            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayBlock();
            }
            Vector3 midPt = attacker != null ? (transform.position + attacker.transform.position) * 0.5f + Vector3.up * 1.0f : transform.position + Vector3.up * 1.0f;
            BBContactPresentation.OnGuard(midPt, false, false);
            ConsumeCurrentCommand();
        }

        public bool TryExecuteDefensiveBurst(int tick)
        {
            if (health <= 0f || bbState == BBState.Dead || bbState == BBState.Thrown) return false;
            if (!gauges.TrySpendBurst()) return false;

            burstInvulnTicksRemaining = 26;
            currentInvuln = BBInvuln.Strike | BBInvuln.Throw | BBInvuln.Projectile;
            hitstopRemaining = 0;
            hitstunRemaining = 0;
            blockstunRemaining = 0;
            currentAttack = null;
            attackTick = 0;
            chainState.Reset();
            comboState.Reset();
            bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
            stateTick = 0;

            if (animDriver != null)
            {
                sbyte facingNumpad = fighter != null ? BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection) : (sbyte)5;
                animDriver.UpdateLocomotionState(bbState, facingNumpad, isGrounded);
            }

            int pIdx = (BBMatchSim.Instance != null && BBMatchSim.Instance.p2Brain == this) ? 1 : 0;
            Vector3 burstPos = transform.position + Vector3.up * 1f;

            if (BBMatchSim.Instance != null)
            {
                BBMatchSim.Instance.RaiseBurstExecuted(pIdx, BBBurstType.BlueDefensive, burstPos);
            }

            // Knock back and knock down opponent within 3.5m burst shockwave
            if (opponentBrain != null)
            {
                float dx = opponentBrain.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) < 3.5f)
                {
                    float dir = dx >= 0 ? 1f : -1f;
                    opponentBrain.bbState = BBState.DownWait;
                    opponentBrain.stateTick = 0;
                    opponentBrain.hitstunRemaining = 30; // knockdown
                    opponentBrain.simVelocity = new Vector2(dir * 8.5f, 5.5f);
                    opponentBrain.isGrounded = false;
                    opponentBrain.currentAttack = null;
                    opponentBrain.attackTick = 0;
                    opponentBrain.chainState.Reset();
                    opponentBrain.comboState.Reset();
                    if (opponentBrain.animDriver != null)
                    {
                        opponentBrain.animDriver.UpdateLocomotionState(opponentBrain.bbState, 5, false);
                    }
                }
            }

            BBContactPresentation.OnGuard(burstPos, false, false);
            return true;
        }

        public bool TryExecuteOffensiveBurstOrOverdrive(int tick)
        {
            if (health <= 0f || bbState == BBState.Dead) return false;

            if (overdrive != null)
            {
                bool isComboActive = opponentBrain != null && opponentBrain.comboState.hitCount > 0;
                if (overdrive.TryTriggerOverdrive(opponentBrain, isComboActive))
                {
                    int pIdx = (BBMatchSim.Instance != null && BBMatchSim.Instance.p2Brain == this) ? 1 : 0;
                    if (BBMatchSim.Instance != null)
                    {
                        BBMatchSim.Instance.RaiseOverdriveActivated(pIdx, transform.position + Vector3.up * 1f);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool TryExecuteRapidCancel(int tick)
        {
            if (currentAttack == null) return false;
            if (!chainState.madeContact) return false;
            if (bbState != BBState.AttackActive && bbState != BBState.AttackRecovery) return false;

            if (rapidCancel != null && rapidCancel.TryExecuteRapidCancel(opponentBrain))
            {
                int pIdx = (BBMatchSim.Instance != null && BBMatchSim.Instance.p2Brain == this) ? 1 : 0;
                if (BBMatchSim.Instance != null)
                {
                    BBMatchSim.Instance.RaiseRapidCancel(pIdx, transform.position + Vector3.up * 1f);
                }
                if (animDriver != null)
                {
                    sbyte facingNumpad = fighter != null ? BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection) : (sbyte)5;
                    animDriver.UpdateLocomotionState(bbState, facingNumpad, isGrounded);
                }
                return true;
            }
            return false;
        }

        public void TriggerRimFlash(Color color, int ticks = 2)
        {
            if (_cachedRenderers == null || _cachedRenderers.Length == 0)
            {
                _cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _propBlock.SetColor(_RimColorProp, color);
            _propBlock.SetColor(_EmissionColorProp, color);
            _rimFlashTicksRemaining = ticks;
            if (_cachedRenderers != null)
            {
                for (int i = 0; i < _cachedRenderers.Length; i++)
                {
                    if (_cachedRenderers[i] != null) _cachedRenderers[i].SetPropertyBlock(_propBlock);
                }
            }
        }

        public void ConsumeCurrentCommand()
        {
            if (CurrentCommand.button != BBButton.None)
            {
                History.ConsumeButton(CurrentCommand.button);
            }
            CurrentCommand = default;
        }
    }
}
