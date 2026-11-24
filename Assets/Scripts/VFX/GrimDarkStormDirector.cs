using System;
using System.Collections;
using UnityEngine;

namespace DemonIdol.VFX
{
    /// <summary>
    /// Dynamic atmospheric storm director for College Raimon (ST07).
    /// Orchestrates:
    /// - Multi-stage stochastic lightning strikes (pre-strike flicker, main flash, secondary discharges).
    /// - Real-time global shader illumination (_GlobalLightningIntensity, _GlobalLightningColor, _GlobalLightningDirection).
    /// - Dynamic key light & accent light surges.
    /// - Camera rumble / trauma shake during close lightning strikes.
    /// - Thunder sound timing (instant violent crack vs. delayed atmospheric rolling rumbles).
    /// - Looping torrential rain audio and dynamic rain particle systems.
    /// - Deep midnight atmospheric fog and horror ambience.
    /// </summary>
    [ExecuteAlways]
    public class GrimDarkStormDirector : MonoBehaviour
    {
        public static GrimDarkStormDirector Instance { get; private set; }

        [Header("Lightning Timing & Frequency")]
        [Tooltip("Minimum seconds between random lightning strikes")]
        [SerializeField] private float _minStrikeInterval = 4.5f;

        [Tooltip("Maximum seconds between random lightning strikes")]
        [SerializeField] private float _maxStrikeInterval = 9.5f;

        [Tooltip("Probability (0..1) that a strike is a violent close thunderbolt")]
        [SerializeField] [Range(0f, 1f)] private float _closeStrikeProbability = 0.40f;

        [Header("Shader Lightning Appearance")]
        [SerializeField] private Color _lightningColor = new Color(0.85f, 0.92f, 1.35f, 1f);
        [SerializeField] private Vector3 _lightningDirection = new Vector3(0.28f, 0.94f, 0.18f);
        [SerializeField] [Range(0f, 1f)] private float _globalWetness = 0.88f;

        [Header("Scene Light Overrides")]
        [SerializeField] private Light _keyMoonLight;
        [SerializeField] private Light _accentLight;
        [SerializeField] private float _baseMoonIntensity = 0.95f;
        [SerializeField] private Color _baseMoonColor = new Color(0.38f, 0.45f, 0.70f, 1f);

        [Header("Audio SFX")]
        [SerializeField] private AudioSource _rainAudioSource;
        [SerializeField] private AudioSource _thunderAudioSource;
        [SerializeField] private AudioClip _rainLoopClip;
        [SerializeField] private AudioClip _thunderCloseClip;
        [SerializeField] private AudioClip _thunderDistantClip;
        [SerializeField] [Range(0f, 1f)] private float _rainVolume = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float _thunderVolume = 0.85f;

        [Header("Atmospheric Fog & Weather")]
        [SerializeField] private bool _enableMidnightFog = true;
        [SerializeField] private Color _midnightFogColor = new Color(0.035f, 0.050f, 0.095f, 1f);
        [SerializeField] private float _fogDensity = 0.012f;

        [Header("Rain Particle System")]
        [SerializeField] private ParticleSystem _rainParticleSystem;

        [Header("Debug Toggles")]
        [Tooltip("Toggle streetlamp yellow glow circles in Game view. When disabled (default), hides the yellow discs on the College Raimon streetlamps.")]
        [SerializeField] private bool _enableStreetlampGlow = false;
        public bool EnableStreetlampGlow
        {
            get => _enableStreetlampGlow;
            set
            {
                _enableStreetlampGlow = value;
                ApplyStreetlampGlow();
            }
        }

        private float _nextStrikeTime = 5.0f;
        private Coroutine _strikeRoutine;
        private float _currentLightningIntensity = 0f;

        private void Awake()
        {
            Instance = this;
            EnsureReferences();
            if (Application.isPlaying)
            {
                ApplyStreetlampGlow();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) ApplyStreetlampGlow();
                };
#endif
            }
        }

        private void OnEnable()
        {
            Instance = this;
            ApplyAtmosphericSettings();
            ResetGlobalShaderVariables();
            if (Application.isPlaying)
            {
                ApplyStreetlampGlow();
                StartRainAudio();
                ScheduleNextStrike();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) ApplyStreetlampGlow();
                };
#endif
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyStreetlampGlow();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) ApplyStreetlampGlow();
                };
#endif
            }
        }

        public void ApplyStreetlampGlow()
        {
            float targetIntensity = _enableStreetlampGlow ? 1.8f : 0.0f;
            string[] lampMatNames = new string[]
            {
                "M_00003_ao016_streetlight_1_",
                "M_00002_ao016_streetlight_light_1",
                "M_00001_ao017_polelight_1_",
                "M_00000_ao017_lighton_",
                "M_00038_ao018_lighton_",
                "M_00041_ao018b_lighton_"
            };

#if UNITY_EDITOR
            try
            {
                foreach (var matName in lampMatNames)
                {
                    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>($"Assets/CollegeRaimon/Materials/{matName}.mat");
                    if (mat != null && mat.HasProperty("_HorrorGlowIntensity"))
                    {
                        mat.SetFloat("_HorrorGlowIntensity", targetIntensity);
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore if asset pipeline is busy during scene restore
            }
#endif
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            ResetGlobalShaderVariables();
            if (_rainAudioSource != null && _rainAudioSource.isPlaying)
            {
                _rainAudioSource.Stop();
            }
        }

        private void EnsureReferences()
        {
            // Find key moonlight if not assigned
            if (_keyMoonLight == null)
            {
                var dirLight = GameObject.Find("Directional Light");
                if (dirLight != null) _keyMoonLight = dirLight.GetComponent<Light>();
                if (_keyMoonLight == null)
                {
                    Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                    foreach (var l in lights)
                    {
                        if (l.type == LightType.Directional) { _keyMoonLight = l; break; }
                    }
                }
            }

            // Find accent light
            if (_accentLight == null)
            {
                var accentGo = GameObject.Find("Stage_AccentLight");
                if (accentGo != null) _accentLight = accentGo.GetComponent<Light>();
            }

            // Ensure AudioSources
            if (_rainAudioSource == null)
            {
                var rainObj = transform.Find("Audio_RainLoop");
                if (rainObj == null)
                {
                    GameObject go = new GameObject("Audio_RainLoop");
                    go.transform.SetParent(transform, false);
                    _rainAudioSource = go.AddComponent<AudioSource>();
                }
                else
                {
                    _rainAudioSource = rainObj.GetComponent<AudioSource>();
                }
                _rainAudioSource.loop = true;
                _rainAudioSource.playOnAwake = false;
                _rainAudioSource.spatialBlend = 0f; // 2D ambient downpour
                _rainAudioSource.volume = _rainVolume;
            }

            if (_thunderAudioSource == null)
            {
                var thunderObj = transform.Find("Audio_Thunder");
                if (thunderObj == null)
                {
                    GameObject go = new GameObject("Audio_Thunder");
                    go.transform.SetParent(transform, false);
                    _thunderAudioSource = go.AddComponent<AudioSource>();
                }
                else
                {
                    _thunderAudioSource = thunderObj.GetComponent<AudioSource>();
                }
                _thunderAudioSource.loop = false;
                _thunderAudioSource.playOnAwake = false;
                _thunderAudioSource.spatialBlend = 0.15f;
                _thunderAudioSource.volume = _thunderVolume;
            }

            // Auto-load audio clips if null
            #if UNITY_EDITOR
            if (!Application.isBatchMode)
            {
                if (Application.isPlaying)
                {
                    LoadAudioClipsImmediate();
                }
                else
                {
                    // In EditMode, schedule via delayCall to avoid VirtualArtifacts load failure during scene restoration
                    UnityEditor.EditorApplication.delayCall += LoadAudioClipsSafe;
                }
            }
            #endif

            // Ensure Rain Particle System
            if (_rainParticleSystem == null)
            {
                var rainPs = GetComponentInChildren<ParticleSystem>();
                if (rainPs != null) _rainParticleSystem = rainPs;
                else CreateRainParticles();
            }
        }

#if UNITY_EDITOR
        private void LoadAudioClipsImmediate()
        {
            try
            {
                if (_rainLoopClip == null)
                    _rainLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_rain_loop.wav");
                if (_thunderCloseClip == null)
                    _thunderCloseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_thunder_close.wav");
                if (_thunderDistantClip == null)
                    _thunderDistantClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_thunder_distant.wav");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GrimDarkStormDirector] Audio clip deferred load: {ex.Message}");
            }
        }

        private void LoadAudioClipsSafe()
        {
            if (this == null) return;
            LoadAudioClipsImmediate();
        }
#endif

        private void CreateRainParticles()
        {
            var rainGo = new GameObject("VFX_RainDownpour");
            rainGo.transform.SetParent(transform, false);
            rainGo.transform.localPosition = new Vector3(0f, 14f, 0f);

            var ps = rainGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.75f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startColor = new Color(0.85f, 0.92f, 1.0f, 0.45f);
            main.maxParticles = 2500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 1200f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 1f, 16f);

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.space = ParticleSystemSimulationSpace.World;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-3.5f); // Angled wind-driven rainfall
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(-24f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f);

            var psRenderer = rainGo.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            psRenderer.cameraVelocityScale = 0f;
            psRenderer.velocityScale = 0.04f;
            psRenderer.lengthScale = 2.5f;

            _rainParticleSystem = ps;
        }

        private void ApplyAtmosphericSettings()
        {
            if (_enableMidnightFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = _fogDensity;
                RenderSettings.fogColor = _midnightFogColor;
            }

            Shader.SetGlobalFloat("_GlobalWetness", _globalWetness);
            Shader.SetGlobalVector("_GlobalLightningDirection", _lightningDirection.normalized);
            Shader.SetGlobalColor("_GlobalLightningColor", _lightningColor);
        }

        private void ResetGlobalShaderVariables()
        {
            _currentLightningIntensity = 0f;
            Shader.SetGlobalFloat("_GlobalLightningIntensity", 0f);
            if (_keyMoonLight != null)
            {
                _keyMoonLight.intensity = _baseMoonIntensity;
                _keyMoonLight.color = _baseMoonColor;
            }
        }

        private void StartRainAudio()
        {
            if (_rainAudioSource != null && _rainLoopClip != null)
            {
                _rainAudioSource.clip = _rainLoopClip;
                _rainAudioSource.volume = _rainVolume;
                _rainAudioSource.Play();
            }
        }

        private void ScheduleNextStrike()
        {
            _nextStrikeTime = Time.time + UnityEngine.Random.Range(_minStrikeInterval, _maxStrikeInterval);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (Time.time >= _nextStrikeTime)
            {
                bool isClose = UnityEngine.Random.value < _closeStrikeProbability;
                TriggerLightningStrike(isClose);
                ScheduleNextStrike();
            }
        }

        /// <summary>
        /// Public API to trigger a dynamic multi-burst lightning storm strike.
        /// </summary>
        public void TriggerLightningStrike(bool closeStrike = true)
        {
            if (_strikeRoutine != null) StopCoroutine(_strikeRoutine);
            _strikeRoutine = StartCoroutine(LightningStrikeSequence(closeStrike));
        }

        private IEnumerator LightningStrikeSequence(bool closeStrike)
        {
            float maxIntensity = closeStrike ? UnityEngine.Random.Range(3.2f, 4.8f) : UnityEngine.Random.Range(1.6f, 2.5f);

            // Phase 1: Pre-discharge step leader flicker (rapid short flash)
            SetLightning(maxIntensity * 0.45f);
            yield return new WaitForSeconds(0.04f);

            SetLightning(maxIntensity * 0.12f);
            yield return new WaitForSeconds(0.03f);

            // Phase 2: Blinding main return stroke
            SetLightning(maxIntensity);

            // Trigger audio
            if (closeStrike)
            {
                // Immediate massive crack + camera rumble
                StartCoroutine(PlayThunderWithDelay(_thunderCloseClip, 0.05f, 1.0f));
                TriggerCameraRumble(0.45f);
            }
            else
            {
                // Delayed resonant distant roll
                float delay = UnityEngine.Random.Range(0.45f, 0.90f);
                StartCoroutine(PlayThunderWithDelay(_thunderDistantClip, delay, 0.85f));
            }

            yield return new WaitForSeconds(0.09f);

            // Phase 3: Secondary dart leader discharge
            SetLightning(maxIntensity * 0.70f);
            yield return new WaitForSeconds(0.05f);

            // Phase 4: Smooth realistic atmospheric decay
            float decayDuration = closeStrike ? 0.28f : 0.42f;
            float elapsed = 0f;
            float peak = maxIntensity * 0.70f;

            while (elapsed < decayDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / decayDuration;
                float current = Mathf.Lerp(peak, 0f, t * t);
                SetLightning(current);
                yield return null;
            }

            ResetGlobalShaderVariables();
            _strikeRoutine = null;
        }

        private void SetLightning(float intensity)
        {
            _currentLightningIntensity = intensity;
            Shader.SetGlobalFloat("_GlobalLightningIntensity", intensity);

            if (_keyMoonLight != null)
            {
                _keyMoonLight.intensity = _baseMoonIntensity + intensity * 1.35f;
                _keyMoonLight.color = Color.Lerp(_baseMoonColor, _lightningColor, Mathf.Clamp01(intensity / 3.0f));
            }

            if (_accentLight != null)
            {
                _accentLight.intensity = 1.35f + intensity * 0.85f;
            }
        }

        private IEnumerator PlayThunderWithDelay(AudioClip clip, float delay, float volumeMultiplier)
        {
            if (delay > 0.001f) yield return new WaitForSeconds(delay);
            if (_thunderAudioSource != null && clip != null)
            {
                _thunderAudioSource.PlayOneShot(clip, _thunderVolume * volumeMultiplier);
            }
        }

        private void TriggerCameraRumble(float amount)
        {
            if (DemonIdol.Combat.FighterCamera2D.Instance != null)
            {
                DemonIdol.Combat.FighterCamera2D.Instance.AddExplosionTrauma(amount);
            }
        }
    }
}
