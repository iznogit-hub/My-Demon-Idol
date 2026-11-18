using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// BlazBlue fighter brain component. Mounted on each fighter when ControlScheme == BlazBlue4Button.
    /// Manages 4-button input sampling, history, command interpretation, and gauge state.
    /// </summary>
    public class BBFighterBrain : MonoBehaviour
    {
        [Header("Profile & Configuration")]
        public BBCharacterProfile profile;
        public BBSystemTuning tuning;

        [Header("Components")]
        public FighterController fighter;
        public BBInputSampler sampler;

        public BBInputHistory History { get; private set; } = new BBInputHistory();
        public BBCommandInterpreter Interpreter { get; private set; } = new BBCommandInterpreter();

        public BBEvaluatedCommand CurrentCommand { get; private set; }

        [Header("Gauges (0..Max)")]
        public float heat = 0f;
        public float barrier = 1000f;
        public float burst = 1000f;
        public bool isOverdriveActive = false;
        public int overdriveTicksRemaining = 0;

        private void Awake()
        {
            if (fighter == null) fighter = GetComponent<FighterController>();
            if (sampler == null) sampler = GetComponent<BBInputSampler>();
            if (tuning == null) tuning = BBSystemTuning.Default;

            heat = 0f;
            barrier = tuning.barrierMax;
            burst = tuning.burstMax;
        }

        private void OnEnable()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
        }

        private void OnDisable()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        public void HandleCombatTick(int tick)
        {
            if (fighter == null) return;

            // 1. Sample input for this tick
            if (sampler != null)
            {
                BBInputFrame frame = sampler.SampleFrameForTick(tick, fighter.FacingDirection);
                History.PushFrame(frame, tick, fighter.FacingDirection);
            }

            // 2. Query fighter state context
            bool isGrounded = fighter.State != FighterState.AirborneJuggle && fighter.transform.position.y <= 0.05f;
            bool isInAttack = fighter.State == FighterState.AttackStartup ||
                              fighter.State == FighterState.AttackActive ||
                              fighter.State == FighterState.AttackRecovery;
            bool isInBlockstun = fighter.State == FighterState.Blockstun;
            bool isGuarding = fighter.State == FighterState.Blockstun ||
                              fighter.State == FighterState.BarrierActive ||
                              BBInputFrame.IsBack(BBInputFrame.WorldToFacingNumpad(History.LatestFrame.numpad, fighter.FacingDirection));

            // 3. Evaluate highest-priority command
            CurrentCommand = Interpreter.Evaluate(
                History,
                tick,
                fighter.FacingDirection,
                isGrounded,
                isInAttack,
                isInBlockstun,
                isGuarding);

            // 4. Tick gauge timers
            if (isOverdriveActive && overdriveTicksRemaining > 0)
            {
                overdriveTicksRemaining--;
                if (overdriveTicksRemaining <= 0)
                {
                    isOverdriveActive = false;
                }
            }

            // Passive barrier regeneration when not actively blocking with barrier
            if (CurrentCommand.commandType != BBCommandType.Barrier && barrier < tuning.barrierMax)
            {
                barrier = Mathf.Min(tuning.barrierMax, barrier + (tuning.barrierDrainPerSecond * 0.5f * CombatTickEngine.TICK_DELTA_TIME));
            }
        }

        /// <summary>
        /// Consumes the button associated with the current command.
        /// </summary>
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
