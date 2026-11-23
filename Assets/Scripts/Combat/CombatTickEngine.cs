using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Deterministic 60Hz combat simulation ticker.
    /// Decouples fighting game logic (startup, active, recovery, hitstop, blockstun)
    /// from variable mobile rendering framerates (60Hz, 90Hz, 120Hz).
    /// </summary>
    [System.Obsolete("Superseded by BlazBlue deterministic simulation loop. Retained as 60Hz tick driver clock for presentation components.", false)]
    public class CombatTickEngine : MonoBehaviour
    {
        public static CombatTickEngine Instance { get; private set; }

        public const float TICK_RATE = 60f;
        public const float TICK_DELTA_TIME = 1f / TICK_RATE; // 16.666ms

        public int CurrentTick { get; private set; } = 0;
        public int SuperfreezeTicksRemaining { get; private set; } = 0;
        public bool IsSuperfrozen => SuperfreezeTicksRemaining > 0;

        /// <summary>
        /// Fraction of tick elapsed between [0, 1] for render/camera interpolation.
        /// </summary>
        public float InterpolationAlpha => Mathf.Clamp01(_timeAccumulator / TICK_DELTA_TIME);

        public event Action<int> OnTick;
        public event Action<int> OnSuperfreezeTick;

        private float _timeAccumulator = 0f;
        private bool _isPaused = false;

        private void Awake()
        {
            if (GameplaySimulationAuthority.IsPackageNative)
            {
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                DemonIdol.Audio.CombatAudioDirector.DestroyDuplicateComponent(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Returns the live clock, re-enabling a disabled one or creating a dedicated object
        /// when the match host was destroyed.
        /// </summary>
        public static CombatTickEngine EnsureRunning()
        {
            if (Instance != null)
            {
                if (!Instance.gameObject.activeSelf)
                    Instance.gameObject.SetActive(true);
                if (!Instance.enabled)
                    Instance.enabled = true;
                return Instance;
            }

            if (!Application.isPlaying)
                return null;

            var existing = FindAnyObjectByType<CombatTickEngine>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf)
                    existing.gameObject.SetActive(true);
                existing.enabled = true;
                Instance = existing;
                return existing;
            }

            var go = new GameObject("CombatTickEngine");
            return go.AddComponent<CombatTickEngine>();
        }

        private void Update()
        {
            if (_isPaused) return;

            // Use unscaled delta time so combat tick remains on 60Hz wall-clock
            // independent of presentation slow-mo or timeScale variations
            _timeAccumulator += Time.unscaledDeltaTime;

            // Prevent spiral of death if a huge frame hitch happens
            if (_timeAccumulator > 0.2f)
            {
                _timeAccumulator = 0.2f;
            }

            while (_timeAccumulator >= TICK_DELTA_TIME)
            {
                _timeAccumulator -= TICK_DELTA_TIME;
                CurrentTick++;

                if (SuperfreezeTicksRemaining > 0)
                {
                    OnSuperfreezeTick?.Invoke(SuperfreezeTicksRemaining);
                    OnTick?.Invoke(CurrentTick);
                    SuperfreezeTicksRemaining--;
                }
                else
                {
                    OnTick?.Invoke(CurrentTick);
                }
            }
        }

        public void TriggerSuperfreeze(int ticks)
        {
            SuperfreezeTicksRemaining = Mathf.Max(SuperfreezeTicksRemaining, ticks);
        }

        public void PauseSimulation(bool pause)
        {
            _isPaused = pause;
        }

        public void AdvanceTicksManual(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                CurrentTick++;
                if (SuperfreezeTicksRemaining > 0)
                {
                    OnSuperfreezeTick?.Invoke(SuperfreezeTicksRemaining);
                    OnTick?.Invoke(CurrentTick);
                    SuperfreezeTicksRemaining--;
                }
                else
                {
                    OnTick?.Invoke(CurrentTick);
                }
            }
        }

        public void ResetTicks()
        {
            CurrentTick = 0;
            _timeAccumulator = 0f;
        }
    }
}
