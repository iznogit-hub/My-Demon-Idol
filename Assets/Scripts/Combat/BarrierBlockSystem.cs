using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// BlazBlue Barrier Guard & Pushblock System.
    /// Holding Barrier while guarding deploys an energy shield:
    /// - Completely negates chip damage (0 chip damage)
    /// - Multiplies attacker pushback by 2.2x to safely reset neutral
    /// - Consumes a small amount of Super Meter per second while active
    /// </summary>
    public class BarrierBlockSystem : MonoBehaviour
    {
        public const float METER_DRAIN_PER_SECOND = 15.0f;
        public const float BARRIER_PUSHBACK_MULTIPLIER = 2.2f;

        [SerializeField] private FighterController _fighter;
        public FighterController Fighter
        {
            get
            {
                if (_fighter == null) _fighter = GetComponent<FighterController>();
                return _fighter;
            }
            set => _fighter = value;
        }

        [SerializeField] private GameObject _barrierVisualEffect;

        public bool IsBarrierActive { get; private set; } = false;

        public event Action<bool> OnBarrierStateChanged;

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

        private void HandleCombatTick(int currentTick)
        {
            // Simulation superfreeze guard: halt meter drain during domain cinematics
            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen) return;

            if (IsBarrierActive && Fighter != null)
            {
                if (Fighter.currentSuperMeter <= 0f)
                {
                    DeactivateBarrier();
                    return;
                }

                Fighter.SpendSuperMeter(METER_DRAIN_PER_SECOND * CombatTickEngine.TICK_DELTA_TIME);
            }
        }

        public bool TryActivateBarrier()
        {
            if (Fighter == null) return false;

            // Only allowed in defensive neutral or block states
            bool canGuard = Fighter.State == FighterState.Idle ||
                            Fighter.State == FighterState.WalkCreep ||
                            Fighter.State == FighterState.Crouch ||
                            Fighter.State == FighterState.Blockstun;

            if (!canGuard || Fighter.currentSuperMeter <= 2f)
            {
                DeactivateBarrier();
                return false;
            }

            if (!IsBarrierActive)
            {
                IsBarrierActive = true;
                if (_barrierVisualEffect != null) _barrierVisualEffect.SetActive(true);
                OnBarrierStateChanged?.Invoke(true);
            }

            return true;
        }

        public void ActivateBarrier()
        {
            TryActivateBarrier();
        }

        public void DeactivateBarrier()
        {
            if (IsBarrierActive)
            {
                IsBarrierActive = false;
                if (_barrierVisualEffect != null) _barrierVisualEffect.SetActive(false);
                OnBarrierStateChanged?.Invoke(false);
            }
        }

        public void SetVisualEffect(GameObject visual)
        {
            _barrierVisualEffect = visual;
            if (_barrierVisualEffect != null) _barrierVisualEffect.SetActive(false);
        }
    }
}
