using System;
using UnityEngine;
using DemonIdol.Combat;

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

        [Header("Sim State")]
        public BBState bbState = BBState.Neutral;
        public int stateTick = 0;
        public Vector2 simVelocity = Vector2.zero;
        public bool isGrounded = true;
        public int hitstopRemaining = 0;
        public int hitstunRemaining = 0;
        public int blockstunRemaining = 0;
        public BBInvuln currentInvuln = BBInvuln.None;

        [Header("Active Attack")]
        public BBAttackData currentAttack;
        public int attackTick = 0;
        public BBHitMask attackHitMask;

        public BBInputHistory History { get; private set; } = new BBInputHistory();
        public BBCommandInterpreter Interpreter { get; private set; } = new BBCommandInterpreter();
        public BBEvaluatedCommand CurrentCommand { get; private set; }

        [Header("Gauges & Combos")]
        public float health = 11000f;
        public float maxHealth = 11000f;
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

            maxHealth = profile != null ? profile.maxHealth : 11000f;
            health = maxHealth;
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

        public void StepInput(int tick)
        {
            if (fighter == null) return;

            if (sampler != null)
            {
                BBInputFrame frame = sampler.SampleFrameForTick(tick, fighter.FacingDirection);
                History.PushFrame(frame, tick, fighter.FacingDirection);
            }

            bool inAttack = bbState == BBState.AttackStartup || bbState == BBState.AttackActive || bbState == BBState.AttackRecovery;
            bool inBlockstun = bbState == BBState.Blockstun || bbState == BBState.BarrierBlockstun;
            bool isGuarding = inBlockstun || BBInputFrame.IsBack(BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection));

            CurrentCommand = Interpreter.Evaluate(
                History,
                tick,
                fighter.FacingDirection,
                isGrounded,
                inAttack,
                inBlockstun,
                isGuarding);

            if (CurrentCommand.commandType == BBCommandType.Barrier && barrierSys != null)
            {
                barrierSys.NotifyBarrierPressed(tick);
            }
        }

        public void HandleCombatTick(int tick)
        {
            StepInput(tick);
            if (hitstopRemaining <= 0)
            {
                TryExecuteCommand(tick);
            }
            AdvanceState(tick);
            MirrorHealthToFighter();
        }

        public bool TryStartMoveId(string moveId)
        {
            var attack = FindAttackByMoveId(moveId);
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
            if (maxHealth > 0f) fighter.maxHealth = maxHealth;
            float previous = fighter.currentHealth;
            fighter.currentHealth = health;
            if (!Mathf.Approximately(previous, health))
            {
                fighter.OnHealthChanged?.Invoke(fighter.currentHealth, fighter.maxHealth);
            }
            if (health <= 0f && fighter.State != FighterState.Dead)
            {
                fighter.SetState(FighterState.Dead);
            }
        }

        void TryExecuteCommand(int tick)
        {
            if (!CurrentCommand.IsValid || profile == null) return;
            if (bbState == BBState.Hitstun || bbState == BBState.Blockstun || bbState == BBState.BarrierBlockstun || bbState == BBState.Thrown)
                return;

            if (IsDriveCommand(CurrentCommand.commandType))
            {
                if (drive != null && drive.TryStartDrive(this, CurrentCommand.facingNumpad, tick))
                {
                    ConsumeCurrentCommand();
                }
                return;
            }

            string moveId = MoveIdFor(CurrentCommand);
            if (string.IsNullOrEmpty(moveId)) return;
            if (TryStartMoveId(moveId) || TryStartMoveId(FallbackMoveId(CurrentCommand)))
            {
                ConsumeCurrentCommand();
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
                case BBCommandType.Special236:
                case BBCommandType.Special214:
                case BBCommandType.Special623:
                case BBCommandType.Special22:
                    return "236A";
                case BBCommandType.Distortion236236:
                case BBCommandType.Distortion214214:
                case BBCommandType.Distortion632146:
                    return "236236C";
                case BBCommandType.Throw:
                    return "BC";
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
                return; // Frozen during hitstop
            }

            stateTick++;

            // Tick gauges (passive regen, low-HP regen, OD countdown, barrier drain)
            bool isBarriering = CurrentCommand.commandType == BBCommandType.Barrier && !gauges.isDanger;
            gauges.Tick(tuning, health / maxHealth, isBarriering);

            if (drive != null)
            {
                drive.InOverdrive = isOverdriveActive;
                drive.TickDrive(this, tick);
            }

            // Update invulnerability mask for current attack frame
            if (currentAttack != null && currentAttack.invuln != null && currentAttack.invuln.Length > 0)
            {
                currentInvuln = BBInvuln.None;
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
                case BBState.Blockstun:
                case BBState.BarrierBlockstun:
                    if (blockstunRemaining > 0) blockstunRemaining--;
                    if (blockstunRemaining <= 0)
                    {
                        bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                        stateTick = 0;
                        chainState.Reset();
                        comboState.Reset();
                    }
                    break;

                case BBState.Hitstun:
                    if (hitstunRemaining > 0) hitstunRemaining--;
                    if (hitstunRemaining <= 0)
                    {
                        bbState = isGrounded ? BBState.Neutral : BBState.Airborne;
                        stateTick = 0;
                        chainState.Reset();
                        comboState.Reset();
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
            }
        }

        public void StartAttack(BBAttackData attack)
        {
            if (attack == null) return;
            currentAttack = attack;
            attackTick = 0;
            attackHitMask.Clear();
            bbState = attack.startupFrames > 0 ? BBState.AttackStartup : BBState.AttackActive;
            stateTick = 0;
            currentInvuln = BBInvuln.None;

            if (animDriver != null)
            {
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

            if (profile != null && profile.moveSet is BBMoveSet ms && ms.moves != null)
            {
                for (int i = 0; i < ms.moves.Length; i++)
                {
                    if (ms.moves[i] != null && ms.moves[i].moveId == targetMoveId)
                        return ms.moves[i];
                }
            }

            return null;
        }

        public bool CanExecuteMove(BBAttackData move)
        {
            if (move == null) return false;

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
            if (buffer == null) return;
            float posX = transform.position.x;
            float posY = transform.position.y;
            int facing = fighter != null ? fighter.FacingDirection : 1;

            // 1. Pushbox
            BBWorldBox push = GetPushBox();
            buffer.Add(in push);

            // 2. Active attack boxes
            if (currentAttack != null && currentAttack.boxes != null)
            {
                int beforeCount = buffer.count;
                currentAttack.boxes.GetBoxes(attackTick, buffer, posX, posY, facing);

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
            float posX = transform.position.x;
            float posY = transform.position.y;
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
