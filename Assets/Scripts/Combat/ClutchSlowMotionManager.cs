using System;
using System.Collections;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Tekken 7/8 Dynamic Clutch Slow-Motion System.
    /// When both fighters are at critical health and execute simultaneous strikes in close proximity,
    /// triggers cinematic slow-motion (Time.timeScale = 0.2f) with dynamic camera zoom
    /// until the decisive blow lands or whiffs.
    /// </summary>
    public class ClutchSlowMotionManager : MonoBehaviour
    {
        public static ClutchSlowMotionManager Instance { get; private set; }

        [Header("Fighter Targets")]
        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;

        [Header("Threshold Settings")]
        [SerializeField] private float _criticalHealthThreshold = 250f;
        [SerializeField] private float _lethalHealthThreshold = 150f;
        [SerializeField] private float _triggerDistance = 2.6f;
        [SerializeField] private float _maxSlowMoRealtimeSeconds = 0.95f;
        [SerializeField] private float _cooldownSeconds = 6.0f;

        public bool IsSlowMoActive { get; private set; } = false;
        private float _lastTriggerTime = -99f;
        private Coroutine _slowMoRoutine;

        public event Action OnClutchSlowMoStarted;
        public event Action OnClutchSlowMoEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            SubscribeMatchSim();
        }

        private void OnDisable()
        {
            UnsubscribeMatchSim();
        }

        private void Start()
        {
            SubscribeMatchSim();
        }

        private void SubscribeMatchSim()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                var sim = DemonIdol.BlazBlue.BBMatchSim.Instance;
                sim.OnHitConfirmed -= HandleSimHitConfirmed;
                sim.OnHitConfirmed += HandleSimHitConfirmed;
                sim.OnBlockLanded -= HandleSimBlockLanded;
                sim.OnBlockLanded += HandleSimBlockLanded;
                sim.OnSimTickCompleted -= HandleSimTickCompleted;
                sim.OnSimTickCompleted += HandleSimTickCompleted;
            }
        }

        private void UnsubscribeMatchSim()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                var sim = DemonIdol.BlazBlue.BBMatchSim.Instance;
                sim.OnHitConfirmed -= HandleSimHitConfirmed;
                sim.OnBlockLanded -= HandleSimBlockLanded;
                sim.OnSimTickCompleted -= HandleSimTickCompleted;
            }
        }

        private void HandleSimHitConfirmed(int attackerIndex, int defenderIndex, float damage, Vector3 hitPoint, bool isCounterHit, bool isFatal)
        {
            if (!IsSlowMoActive) return;

            if (isCounterHit && FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.TriggerCounterHitImpactZoom(0.8f);
            }

            RestoreNormalSpeed();
        }

        private void HandleSimBlockLanded(int attackerIndex, int defenderIndex, Vector3 contactPt, bool isBarrier, bool isChip)
        {
            if (!IsSlowMoActive) return;
            RestoreNormalSpeed();
        }

        private void HandleSimTickCompleted(DemonIdol.BlazBlue.BBFighterSnapshot p1Snap, DemonIdol.BlazBlue.BBFighterSnapshot p2Snap)
        {
            if (IsSlowMoActive) return;
            if (Time.unscaledTime - _lastTriggerTime < _cooldownSeconds) return;

            float h1Ratio = p1Snap.maxHealth > 0 ? (p1Snap.health / p1Snap.maxHealth) : 1f;
            float h2Ratio = p2Snap.maxHealth > 0 ? (p2Snap.health / p2Snap.maxHealth) : 1f;

            bool f1Low = h1Ratio <= 0.25f || p1Snap.health <= _criticalHealthThreshold;
            bool f2Low = h2Ratio <= 0.25f || p2Snap.health <= _criticalHealthThreshold;
            bool eitherLethal = h1Ratio <= 0.15f || h2Ratio <= 0.15f || p1Snap.health <= _lethalHealthThreshold || p2Snap.health <= _lethalHealthThreshold;

            if (!(f1Low && f2Low) && !eitherLethal) return;

            float dist = Mathf.Abs(p1Snap.position.x - p2Snap.position.x);
            if (dist > _triggerDistance) return;

            bool f1Attacking = p1Snap.state == DemonIdol.BlazBlue.BBState.AttackStartup || p1Snap.state == DemonIdol.BlazBlue.BBState.AttackActive;
            bool f2Attacking = p2Snap.state == DemonIdol.BlazBlue.BBState.AttackStartup || p2Snap.state == DemonIdol.BlazBlue.BBState.AttackActive;

            if (f1Attacking && f2Attacking)
            {
                TriggerClutchSlowMo();
            }
        }

        public void BindFighters(FighterController f1, FighterController f2)
        {
            if (_fighter1 != null) _fighter1.OnHitProcessed -= OnFighterHitProcessed;
            if (_fighter2 != null) _fighter2.OnHitProcessed -= OnFighterHitProcessed;

            _fighter1 = f1;
            _fighter2 = f2;

            if (_fighter1 != null) _fighter1.OnHitProcessed += OnFighterHitProcessed;
            if (_fighter2 != null) _fighter2.OnHitProcessed += OnFighterHitProcessed;
        }

        private void Update()
        {
            if (IsSlowMoActive) return;
            if (Time.unscaledTime - _lastTriggerTime < _cooldownSeconds) return;

            FindFightersIfMissing();
            if (_fighter1 == null || _fighter2 == null) return;

            // Only during active fighting state
            if (MatchRoundReferee.Instance != null && MatchRoundReferee.Instance.State != MatchState.Fighting)
            {
                return;
            }

            CheckClutchConditions();
        }

        private float _lastFighterSearchTime = -10f;

        private void FindFightersIfMissing()
        {
            SubscribeMatchSim();
            if (_fighter1 != null && _fighter2 != null) return;
            if (Time.unscaledTime - _lastFighterSearchTime < 2.0f) return;
            _lastFighterSearchTime = Time.unscaledTime;

            var fighters = FindObjectsByType<FighterController>();
            foreach (var f in fighters)
            {
                if (f.isPlayerControlled && _fighter1 == null) _fighter1 = f;
                else if (_fighter2 == null) _fighter2 = f;
            }
        }

        private void CheckClutchConditions()
        {
            // 1. Health check: Either both fighters <= critical, OR one is <= lethal
            float h1Ratio = _fighter1.maxHealth > 0 ? (_fighter1.currentHealth / _fighter1.maxHealth) : 1f;
            float h2Ratio = _fighter2.maxHealth > 0 ? (_fighter2.currentHealth / _fighter2.maxHealth) : 1f;
            bool f1Low = h1Ratio <= 0.25f || _fighter1.currentHealth <= _criticalHealthThreshold;
            bool f2Low = h2Ratio <= 0.25f || _fighter2.currentHealth <= _criticalHealthThreshold;
            bool eitherLethal = h1Ratio <= 0.15f || h2Ratio <= 0.15f || _fighter1.currentHealth <= _lethalHealthThreshold || _fighter2.currentHealth <= _lethalHealthThreshold;

            if (!(f1Low && f2Low) && !eitherLethal) return;

            // 2. Proximity check
            float dist = Vector3.Distance(_fighter1.transform.position, _fighter2.transform.position);
            if (dist > _triggerDistance) return;

            // 3. Attack collision check: Both attacking simultaneously or close to impact
            bool f1Attacking = _fighter1.State == FighterState.AttackStartup || _fighter1.State == FighterState.AttackActive;
            bool f2Attacking = _fighter2.State == FighterState.AttackStartup || _fighter2.State == FighterState.AttackActive;

            if (f1Attacking && f2Attacking)
            {
                TriggerClutchSlowMo();
            }
        }

        public void TriggerClutchSlowMo()
        {
            if (IsSlowMoActive) return;

            _lastTriggerTime = Time.unscaledTime;
            if (_slowMoRoutine != null) StopCoroutine(_slowMoRoutine);
            _slowMoRoutine = StartCoroutine(RunClutchSlowMo());
        }

        private IEnumerator RunClutchSlowMo()
        {
            IsSlowMoActive = true;
            Time.timeScale = 0.20f;

            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.SetClutchSlowMo(true);
            }

            OnClutchSlowMoStarted?.Invoke();

            float elapsed = 0f;
            while (elapsed < _maxSlowMoRealtimeSeconds && IsSlowMoActive)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            RestoreNormalSpeed();
        }

        private void OnFighterHitProcessed(HitResult result, AttackData attack, Vector3 hitPoint)
        {
            if (!IsSlowMoActive) return;

            // Decisive hit landed!
            if (result == HitResult.CounterHit && FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.TriggerCounterHitImpactZoom(0.8f);
            }

            RestoreNormalSpeed();
        }

        private void RestoreNormalSpeed()
        {
            if (!IsSlowMoActive) return;

            IsSlowMoActive = false;
            Time.timeScale = 1.0f;

            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.SetClutchSlowMo(false);
            }

            OnClutchSlowMoEnded?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeMatchSim();
            if (_fighter1 != null) _fighter1.OnHitProcessed -= OnFighterHitProcessed;
            if (_fighter2 != null) _fighter2.OnHitProcessed -= OnFighterHitProcessed;
            Time.timeScale = 1.0f;
        }
    }
}
