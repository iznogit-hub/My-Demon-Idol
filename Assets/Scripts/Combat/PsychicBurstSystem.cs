using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// BlazBlue / Guilty Gear Psychic Burst System.
    /// Manages the 100% Burst Gauge, Blue Defensive Burst (combo-breaker while in hitstun),
    /// and Gold Offensive Burst (neutral blast that launches opponent and maxes Super Meter to 100%).
    /// </summary>
    [System.Obsolete("Superseded by BBGauges and BBFighterBrain burst logic in Assets/Scripts/BlazBlue/. Do not use in new code.", false)]
    public class PsychicBurstSystem : MonoBehaviour
    {
        public const float MAX_BURST_METER = 100f;
        public const float PASSIVE_RECHARGE_RATE = 2.0f; // Percent per second
        public const float DAMAGE_RECHARGE_FACTOR = 0.08f; // Gauge gained per damage taken
        public const float BURST_RADIUS = 3.5f;

        [SerializeField] private FighterController _fighter;
        [SerializeField] private float _currentBurstMeter = 100f;
        public float CurrentBurstMeter => _currentBurstMeter;
        public bool IsBurstReady => _currentBurstMeter >= MAX_BURST_METER;

        public event Action<BurstType> OnBurstTriggered;
        public event Action<float, float> OnBurstMeterChanged;

        private void Awake()
        {
            if (_fighter == null) _fighter = GetComponent<FighterController>();
            _currentBurstMeter = MAX_BURST_METER;
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
            // Simulation superfreeze guard: halt burst meter recharge during domain cinematics
            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen) return;

            if (_currentBurstMeter < MAX_BURST_METER)
            {
                _currentBurstMeter = Mathf.Min(MAX_BURST_METER, _currentBurstMeter + PASSIVE_RECHARGE_RATE * CombatTickEngine.TICK_DELTA_TIME);
                OnBurstMeterChanged?.Invoke(_currentBurstMeter, MAX_BURST_METER);
            }
        }

        public void OnTakeDamage(float damage)
        {
            if (_currentBurstMeter < MAX_BURST_METER)
            {
                _currentBurstMeter = Mathf.Min(MAX_BURST_METER, _currentBurstMeter + (damage * DAMAGE_RECHARGE_FACTOR));
                OnBurstMeterChanged?.Invoke(_currentBurstMeter, MAX_BURST_METER);
            }
        }

        public bool CanExecuteBurst()
        {
            if (!IsBurstReady) return false;
            if (_fighter == null || _fighter.State == FighterState.Dead || _fighter.State == FighterState.Wakeup) return false;

            // Defensive burst: during hitstun, blockstun, or airborne juggle
            bool isDefensive = _fighter.State == FighterState.Hitstun ||
                               _fighter.State == FighterState.Blockstun ||
                               _fighter.State == FighterState.AirborneJuggle;

            // Offensive burst: neutral idle or creep
            bool isOffensive = _fighter.State == FighterState.Idle ||
                               _fighter.State == FighterState.WalkCreep;

            return isDefensive || isOffensive;
        }

        public BurstType GetApplicableBurstType()
        {
            if (_fighter == null) return BurstType.None;

            if (_fighter.State == FighterState.Hitstun ||
                _fighter.State == FighterState.Blockstun ||
                _fighter.State == FighterState.AirborneJuggle)
            {
                return BurstType.BlueDefensive;
            }

            if (_fighter.State == FighterState.Idle ||
                _fighter.State == FighterState.WalkCreep)
            {
                return BurstType.GoldOffensive;
            }

            return BurstType.None;
        }

        public bool ExecuteBurst()
        {
            if (!CanExecuteBurst()) return false;

            BurstType burstType = GetApplicableBurstType();
            if (burstType == BurstType.None) return false;

            // Consume full meter
            _currentBurstMeter = 0f;
            OnBurstMeterChanged?.Invoke(_currentBurstMeter, MAX_BURST_METER);

            FighterController opponent = _fighter.opponent;
            float dist = opponent != null ? Vector3.Distance(_fighter.transform.position, opponent.transform.position) : 999f;
            bool opponentInRange = dist <= BURST_RADIUS;

            if (burstType == BurstType.BlueDefensive)
            {
                // Break combo! Reset self to neutral
                _fighter.ForceCancelToNeutral();
                _fighter.TriggerAnimation("Burst");

                if (opponent != null && opponentInRange)
                {
                    // Blast opponent away across arena
                    opponent.ApplyHorizontalDisplacement(-_fighter.FacingDirection * 8.5f);
                    opponent.ReceiveIncomingStrike(_fighter, CreateBurstAttackData(isOffensive: false), null);
                }
            }
            else if (burstType == BurstType.GoldOffensive)
            {
                // Gold Burst: Launcher + full super meter!
                _fighter.AddSuperMeter(_fighter.maxSuperMeter);
                _fighter.TriggerAnimation("Burst");

                if (opponent != null && opponentInRange)
                {
                    opponent.ReceiveIncomingStrike(_fighter, CreateBurstAttackData(isOffensive: true), null);
                }
            }

            // Screen Trauma & Audio-Visual Call
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.5f);
            }

            OnBurstTriggered?.Invoke(burstType);
            return true;
        }

        private AttackData CreateBurstAttackData(bool isOffensive)
        {
            var data = ScriptableObject.CreateInstance<AttackData>();
            data.attackName = isOffensive ? "Gold Burst" : "Blue Burst";
            data.damage = isOffensive ? 30f : 10f;
            data.hitLevel = HitLevel.SpecialMid;
            data.isLauncher = isOffensive;
            data.causesKnockdown = !isOffensive;
            data.launchVelocity = isOffensive ? new Vector2(1.5f, 7.5f) : Vector2.zero;
            data.pushbackOnHit = isOffensive ? 2.5f : 6.0f;
            data.hitstopAttacker = 3;
            data.hitstopDefender = 14;
            data.screenShakeTrauma = 0.35f;
            return data;
        }

        public void ResetMeter()
        {
            _currentBurstMeter = MAX_BURST_METER;
            OnBurstMeterChanged?.Invoke(_currentBurstMeter, MAX_BURST_METER);
        }
    }
}
