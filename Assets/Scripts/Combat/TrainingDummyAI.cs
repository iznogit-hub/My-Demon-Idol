using UnityEngine;

namespace DemonIdol.Combat
{
    public enum DummyMode
    {
        StandStill,          // Punching bag - neutral auto-block
        Crouch,              // Ducking - tests mids vs ducked highs
        AutoBlock,           // Guards immediately when in range or after first hit
        CounterHitPractice,  // Attacks periodically with Light attack to test Counter-Hits and Parries
        Aggressive           // Basic AI: closes distance, mixes Light and Low attacks
    }

    /// <summary>
    /// Training dummy NPC controller.
    /// Provides dedicated training behaviors to test high/low mixups,
    /// counter-hits, parry reads, and frame traps.
    /// </summary>
    [System.Obsolete("Superseded by BBAIController in Assets/Scripts/BlazBlue/AI/. Do not use in new code.", false)]
    public class TrainingDummyAI : MonoBehaviour
    {
        [Header("Dummy Behavior")]
        public DummyMode mode = DummyMode.StandStill;
        [Tooltip("Interval between attacks in CounterHitPractice mode (in ticks)")]
        public int attackIntervalTicks = 90; // ~1.5 seconds

        [SerializeField] private FighterController _fighter;

        private int _tickTimer = 0;

        private void Awake()
        {
            if (_fighter == null) _fighter = GetComponent<FighterController>();
        }

        private void Start()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
        }

        private void OnDestroy()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        private void OnDisable()
        {
            _tickTimer = 0;
            if (_fighter != null)
            {
                _fighter.SetMoveInput(Vector2.zero);
                if (_fighter.InputBuf != null) _fighter.InputBuf.Clear();
                if (_fighter.Barrier != null) _fighter.Barrier.DeactivateBarrier();
            }
        }

        private void HandleCombatTick(int currentTick)
        {
            if (!enabled) return;
            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen) return;

            if (_fighter == null || _fighter.isPlayerControlled || _fighter.State == FighterState.Dead)
            {
                return;
            }

            switch (mode)
            {
                case DummyMode.StandStill:
                    _fighter.SetMoveInput(Vector2.zero);
                    break;

                case DummyMode.Crouch:
                    // Force down input to crouch
                    _fighter.SetMoveInput(new Vector2(0, -1f));
                    break;

                case DummyMode.AutoBlock:
                    // If opponent is attacking and in proximity, hold back
                    if (_fighter.opponent != null && _fighter.opponent.State == FighterState.AttackStartup)
                    {
                        _fighter.SetMoveInput(new Vector2(-_fighter.FacingDirection, 0));
                    }
                    else
                    {
                        _fighter.SetMoveInput(Vector2.zero);
                    }
                    break;

                case DummyMode.CounterHitPractice:
                    _tickTimer++;
                    if (_tickTimer >= attackIntervalTicks)
                    {
                        _tickTimer = 0;
                        // Fire a light attack to give player a counter-hit / parry window!
                        _fighter.InputBuf.Enqueue(InputButton.Light, currentTick, Vector2.zero);
                    }
                    break;

                case DummyMode.Aggressive:
                    HandleAggressiveAI(currentTick);
                    break;
            }
        }

        private void HandleAggressiveAI(int currentTick)
        {
            if (_fighter.opponent == null) return;

            float distance = Mathf.Abs(_fighter.opponent.transform.position.x - transform.position.x);

            if (distance > 3.0f)
            {
                // Advance forward
                _fighter.SetMoveInput(new Vector2(_fighter.FacingDirection * 0.5f, 0));
            }
            else
            {
                _fighter.SetMoveInput(Vector2.zero);
                _tickTimer++;
                if (_tickTimer >= 60)
                {
                    _tickTimer = 0;
                    // Mix up between Light and Medium
                    InputButton btn = (Random.value > 0.5f) ? InputButton.Light : InputButton.Medium;
                    _fighter.InputBuf.Enqueue(btn, currentTick, Vector2.zero);
                }
            }
        }
    }
}
