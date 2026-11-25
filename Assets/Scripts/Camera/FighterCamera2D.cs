using System;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Combat
{
    public enum CameraState
    {
        BaseCombatFrame,
        IntroEstablish,
        FighterReveal,
        RoundStart,
        HitAccent,
        CounterHitAccent,
        LaunchTrack,
        WallAccent,
        ThrowCinematic,
        UltimateEntry,
        MP4Takeover,
        UltimateExit,
        KO,
        Results
    }

    /// <summary>
    /// Master 2.5D Cinematic Physical Camera Director for Demon Idol.
    /// Implements authentic Street Fighter 6 camera optics and handling:
    /// 1. Cinematic Physical Camera simulation (Full-Frame Super 35, 50mm portrait prime, lens shift).
    /// 2. Exact 40% character screen height calibration in neutral combat with dynamic close/wide scaling.
    /// 3. SF6 grounded eye-level (1.25m) with heroic low pitch (2.8 deg) and decoupled vertical jumping.
    /// 4. Asymmetric aggression lead tracking (camera biases toward advancing/attacking fighter).
    /// 5. Snappy impact zooms on Counter Hits, Wall Splats, and Ultras with trauma-based screen shake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FighterCamera2D : MonoBehaviour
    {
        private static FighterCamera2D _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        public static FighterCamera2D Instance
        {
            get
            {
                if (_instance == null) return null;
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Target Fighters")]
        public Transform fighter1;
        public Transform fighter2;

        [Header("Cinematic Physical Camera (SF6 Optical Spec)")]
        [Tooltip("Enable Unity Physical Camera simulation for authentic cinema optics")]
        public bool usePhysicalCamera = true;
        [Tooltip("Sensor size in mm (Full Frame 16:9 Super 35 = 36mm x 20.25mm)")]
        public Vector2 sensorSize = new Vector2(36.0f, 20.25f);
        [Tooltip("Cinematic focal length in mm (50mm standard prime)")]
        [Range(10.0f, 150.0f)]
        public float focalLength = 50.0f;
        [Tooltip("Heroic lens shift (Y offset raises horizon line to anchor grounded fighters)")]
        public Vector2 lensShift = new Vector2(0f, 0.035f);

        [Header("Character Height Framing Calibration (55-65% Screen Size)")]
        [Tooltip("Standard character height in meters")]
        [Range(0.5f, 3.0f)]
        public float standardFighterHeight = 1.35f;
        [Tooltip("Target fraction of vertical viewport occupied by characters in neutral combat (0.59 = 59%)")]
        [Range(0.25f, 0.75f)]
        public float targetCharacterScreenFraction = 0.59f;
        [Tooltip("Close combat zoom push-in fraction (0.64 = 64% screen size at throw range)")]
        [Range(0.25f, 0.90f)]
        public float closeCombatScreenFraction = 0.64f;
        [Tooltip("Max spacing pull-out fraction (0.54 = 54% screen size corner-to-corner)")]
        [Range(0.20f, 0.80f)]
        public float wideSpacingScreenFraction = 0.54f;
        [Tooltip("Zoom sensitivity relative to fighter distance (legacy compatibility)")]
        [Range(0.01f, 2.0f)]
        public float zoomFactor = 0.45f;

        [Header("Dynamic Zoom Curve Distances & Padding")]
        [Tooltip("Distance in meters where extreme close-range push-in begins")]
        [Range(0.5f, 3.0f)]
        public float closeDistanceMin = 1.2f;
        [Tooltip("Distance in meters where neutral transition begins")]
        [Range(1.0f, 4.0f)]
        public float closeDistanceMax = 2.2f;
        [Tooltip("Screen fraction boost at point-blank range")]
        [Range(0.0f, 0.2f)]
        public float closeCombatFractionBoost = 0.05f;
        [Tooltip("Neutral combat spacing distance in meters")]
        [Range(1.5f, 5.0f)]
        public float neutralDistance = 3.0f;
        [Tooltip("Max spacing distance in meters for full pull-out")]
        [Range(4.0f, 15.0f)]
        public float wideDistanceMax = 7.0f;
        [Tooltip("Horizontal padding margin ensuring both fighters remain comfortably in-frame")]
        [Range(0.0f, 2.0f)]
        public float horizontalFramingPadding = 0.35f;

        [Header("SF6 Dynamic Handling & Grounded Pitch")]
        [Tooltip("Grounded camera offset relative to midpoint")]
        public Vector3 offset = new Vector3(-0.05f, 1.05f, -3.55f);
        [Tooltip("Heroic upward pitch angle in degrees")]
        [Range(-20.0f, 20.0f)]
        public float pitchAngle = 2.8f;
        [Tooltip("Asymmetric aggression lead bias towards the advancing/attacking fighter")]
        [Range(0.0f, 2.0f)]
        public float aggressionLeadAmount = 0.35f;
        [Tooltip("Threshold vertical launch tracking height (normal jumps stay decoupled below this)")]
        [Range(0.5f, 6.0f)]
        public float verticalLaunchThresholdY = 2.5f;
        [Tooltip("Tracking ratio for vertical launches above threshold")]
        [Range(0.0f, 1.0f)]
        public float verticalLaunchFollowFactor = 0.38f;

        [Header("Camera Distance Limits & Boundary Padding")]
        [Tooltip("Minimum camera distance Z (tight framing when close)")]
        [Range(-15.0f, -1.0f)]
        public float minDistanceZ = -3.2f;
        [Tooltip("Maximum camera distance Z (widest framing at max spacing)")]
        [Range(-20.0f, -2.0f)]
        public float maxDistanceZ = -7.0f;
        [Tooltip("Hard limit on closest Z distance during cinematic accent zooms")]
        [Range(-6.0f, -1.0f)]
        public float maxCinematicZoomInZ = -2.4f;
        [Tooltip("Buffer added to near limit for cinematic zoom clamping")]
        [Range(0.0f, 2.0f)]
        public float cinematicZoomBufferZ = 0.6f;
        [Tooltip("Background depth offset used for camera boundary clamping")]
        [Range(0.0f, 10.0f)]
        public float backgroundDistanceOffset = 2.5f;
        [Tooltip("Viewport edge padding for cornered fighters")]
        [Range(0.1f, 3.0f)]
        public float cornerViewportEdgePadding = 1.2f;

        [Header("Damping & Interpolation Speeds")]
        [Range(0.1f, 30.0f)]
        public float smoothSpeed = 8.0f;
        [Range(0.1f, 30.0f)]
        public float smoothSpeedY = 3.5f;
        [Range(0.1f, 30.0f)]
        public float smoothSpeedZ = 5.5f;
        [Tooltip("Fast follow speed during impact accents and KO cinematic")]
        [Range(1.0f, 30.0f)]
        public float accentFollowSpeed = 14.0f;

        [Header("Deadzone Filtering")]
        [Range(0.0f, 0.5f)]
        public float deadzoneX = 0.06f;
        [Range(0.0f, 0.5f)]
        public float deadzoneY = 0.10f;
        [Range(0.0f, 0.5f)]
        public float deadzoneZ = 0.12f;

        [Header("Cinematic Accent Inward Zoom Deltas (Relative to baseZ)")]
        [Tooltip("Inward push-in distance on standard Hit Confirm")]
        [Range(0.0f, 2.0f)]
        public float hitAccentZoomDelta = 0.45f;
        [Tooltip("Inward push-in distance on Counter Hit / Fatal Counter")]
        [Range(0.0f, 2.5f)]
        public float counterHitZoomDelta = 0.85f;
        [Tooltip("Inward push-in distance on Wall Bounce / Wall Splat")]
        [Range(0.0f, 2.5f)]
        public float wallAccentZoomDelta = 0.65f;
        [Tooltip("Inward push-in distance during Throw Cinematic")]
        [Range(0.0f, 2.5f)]
        public float throwAccentZoomDelta = 0.70f;
        [Tooltip("Inward push-in distance during Ultimate / Distortion Startup")]
        [Range(0.0f, 3.0f)]
        public float ultimateEntryZoomDelta = 1.10f;
        [Tooltip("Inward push-in distance on Round KO")]
        [Range(0.0f, 3.5f)]
        public float koZoomDelta = 1.25f;
        [Tooltip("Vertical target focus offset during KO zoom")]
        [Range(0.0f, 3.0f)]
        public float koFocusOffsetY = 1.2f;
        [Tooltip("Vertical target focus offset during Match Results")]
        [Range(0.0f, 3.0f)]
        public float resultsFocusOffsetY = 1.1f;
        [Tooltip("Camera distance Z during Match Results")]
        [Range(-10.0f, -2.0f)]
        public float resultsCameraZ = -4.5f;
        [Tooltip("Minimum Y height for launch tracking")]
        [Range(0.0f, 5.0f)]
        public float launchTrackMinY = 1.4f;
        [Tooltip("Maximum Y height for launch tracking")]
        [Range(2.0f, 10.0f)]
        public float launchTrackMaxY = 5.0f;
        [Tooltip("Vertical padding margin during launch tracking")]
        [Range(0.2f, 3.0f)]
        public float launchTrackPaddingY = 1.2f;

        [Header("Legacy Accent Zoom Offsets (Maintained for Serialization)")]
        [Range(-15.0f, -1.0f)] public float hitAccentZoomZ = -9.2f;
        [Range(-15.0f, -1.0f)] public float counterHitZoomZ = -7.5f;
        [Range(0.0f, 5.0f)] public float launchTrackLiftY = 1.4f;
        [Range(-15.0f, -1.0f)] public float wallAccentZoomZ = -7.8f;
        [Range(-15.0f, -1.0f)] public float throwAccentZoomZ = -7.2f;
        [Range(-15.0f, -1.0f)] public float ultimateEntryZoomZ = -5.8f;
        [Range(-15.0f, -1.0f)] public float koCinematicZoomZ = -5.5f;

        [Header("State Tracking")]
        public CameraState CurrentState { get; private set; } = CameraState.BaseCombatFrame;
        private float _stateDurationRemaining = 0f;
        private Transform _focusedTarget;

        [Header("Screen Shake Layers")]
        [Range(0, 1)] private float _traumaHit = 0f;
        [Range(0, 1)] private float _traumaCounterHit = 0f;
        [Range(0, 1)] private float _traumaWall = 0f;
        [Range(0, 1)] private float _traumaExplosion = 0f;
        [Range(0.1f, 20.0f)] public float traumaDecay = 6.0f;
        [Range(0.0f, 0.2f)] public float maxShakeOffset = 0.035f;
        [Range(0.0f, 15.0f)] public float maxShakeAngle = 0.0f;
        [Range(0.0f, 2.0f)] public float masterShakeScale = 0.25f;
        [Range(1.0f, 60.0f)] public float shakeFrequency = 24.0f;
        [Range(0.001f, 0.1f)] public float microImpulseFocalPunchScale = 0.02f;

        [Header("Event Trauma & State Durations")]
        [Range(0.0f, 1.0f)] public float hitTraumaIntensity = 0.25f;
        [Range(0.02f, 1.0f)] public float hitAccentDuration = 0.12f;
        [Range(0.0f, 1.0f)] public float counterHitTraumaIntensity = 0.50f;
        [Range(0.02f, 1.0f)] public float counterHitAccentDuration = 0.18f;
        [Range(0.0f, 1.0f)] public float blockTraumaIntensity = 0.08f;
        [Range(0.0f, 1.0f)] public float rapidCancelTraumaIntensity = 0.25f;
        [Range(0.0f, 1.0f)] public float burstTraumaIntensity = 0.45f;
        [Range(0.0f, 1.0f)] public float overdriveTraumaIntensity = 0.35f;
        [Range(0.0f, 1.0f)] public float wallTraumaIntensity = 0.12f;
        [Range(0.05f, 1.0f)] public float wallAccentDuration = 0.28f;
        [Range(0.05f, 1.0f)] public float throwAccentDuration = 0.35f;
        [Range(0.0f, 1.0f)] public float ultimateEntryTraumaIntensity = 0.15f;
        [Range(0.1f, 2.0f)] public float ultimateEntryDuration = 0.60f;
        [Range(0.05f, 1.0f)] public float ultimateExitDuration = 0.20f;
        [Range(0.0f, 1.0f)] public float koTraumaIntensity = 0.20f;
        [Range(0.5f, 5.0f)] public float koStateDuration = 2.5f;
        [Range(0.1f, 2.0f)] public float launchTrackDuration = 0.55f;

        public struct CameraMicroImpulse
        {
            public Vector3 direction;
            public float magnitude;
            public int totalTicks;
            public int ticksRemaining;
            public float fovPunch;
        }

        private CameraMicroImpulse _activeImpulse;
        private float _baseFieldOfView = 32f;
        private Vector3 _lastTargetPosition;
        private Camera _cam;
        private float _stageArenaLimitX = 10.5f;
        private float _lastCameraFighterSearchTime = -10f;

        // Cached state & lifecycle hardening
        private Vector3 _smoothedBasePosition;
        private bool _hasSmoothedBasePosition;
        private float _baseFocalLength = 50f;
        private float _impulseDurationRemaining;
        private float _impulseTotalDuration;
        private FighterController _cachedF1Controller;
        private FighterController _cachedF2Controller;
        private float _stageLeftWallX = -10.5f;
        private float _stageRightWallX = 10.5f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _cam = GetComponent<Camera>();

            ConfigurePhysicalCamera();
            if (_cam != null)
            {
                _baseFieldOfView = _cam.fieldOfView;
                _baseFocalLength = Mathf.Max(1f, focalLength);
            }

            _smoothedBasePosition = transform.position;
            _hasSmoothedBasePosition = true;
            _lastTargetPosition = transform.position;
            ResetTrauma();
        }

        private void OnEnable()
        {
            ConfigurePhysicalCamera();
            ResetTrauma();
            SubscribeMatchSim();
        }

        private void OnDisable()
        {
            UnsubscribeMatchSim();
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
                sim.OnRapidCanceled -= HandleSimRapidCanceled;
                sim.OnRapidCanceled += HandleSimRapidCanceled;
                sim.OnBurstExecuted -= HandleSimBurstExecuted;
                sim.OnBurstExecuted += HandleSimBurstExecuted;
                sim.OnOverdriveActivated -= HandleSimOverdriveActivated;
                sim.OnOverdriveActivated += HandleSimOverdriveActivated;
                sim.OnRoundEnded -= HandleSimRoundEnded;
                sim.OnRoundEnded += HandleSimRoundEnded;
            }
        }

        private void UnsubscribeMatchSim()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                var sim = DemonIdol.BlazBlue.BBMatchSim.Instance;
                sim.OnHitConfirmed -= HandleSimHitConfirmed;
                sim.OnBlockLanded -= HandleSimBlockLanded;
                sim.OnRapidCanceled -= HandleSimRapidCanceled;
                sim.OnBurstExecuted -= HandleSimBurstExecuted;
                sim.OnOverdriveActivated -= HandleSimOverdriveActivated;
                sim.OnRoundEnded -= HandleSimRoundEnded;
            }
        }

        private void HandleSimHitConfirmed(int attacker, int defender, float damage, Vector3 pt, bool isCounterHit, bool isFatal)
        {
            if (isCounterHit || isFatal)
                TriggerCounterHitImpactZoom(0.5f);
            else
                TriggerHitImpactZoom(0.25f);
        }

        private void HandleSimBlockLanded(int attacker, int defender, Vector3 pt, bool isBarrier, bool isChip)
        {
            AddTrauma(blockTraumaIntensity);
        }

        private void HandleSimRapidCanceled(int playerIdx, Vector3 pt)
        {
            AddTrauma(rapidCancelTraumaIntensity);
        }

        private void HandleSimBurstExecuted(int playerIdx, DemonIdol.BlazBlue.BBBurstType burstType, Vector3 pt)
        {
            AddTrauma(burstTraumaIntensity);
        }

        private void HandleSimOverdriveActivated(int playerIdx, Vector3 pt)
        {
            AddTrauma(overdriveTraumaIntensity);
        }

        private void HandleSimRoundEnded(int winnerIdx, string reason)
        {
            TriggerKO();
        }

        private void OnValidate()
        {
            closeDistanceMax = Mathf.Max(closeDistanceMax, closeDistanceMin + 0.05f);
            neutralDistance = Mathf.Max(neutralDistance, closeDistanceMax + 0.05f);
            wideDistanceMax = Mathf.Max(wideDistanceMax, neutralDistance + 0.1f);

            if (maxDistanceZ > minDistanceZ)
            {
                float nearZ = Mathf.Max(minDistanceZ, maxDistanceZ);
                float farZ = Mathf.Min(minDistanceZ, maxDistanceZ);
                minDistanceZ = nearZ;
                maxDistanceZ = farZ;
            }

            ConfigurePhysicalCamera();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void ConfigurePhysicalCamera()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            if (usePhysicalCamera)
            {
                _cam.usePhysicalProperties = true;
                _cam.sensorSize = sensorSize;
                _cam.focalLength = Mathf.Max(1f, focalLength);
                _cam.gateFit = Camera.GateFitMode.Horizontal;
                _cam.lensShift = lensShift;
                _baseFocalLength = _cam.focalLength;
                _baseFieldOfView = _cam.fieldOfView;
            }
            else
            {
                _cam.usePhysicalProperties = false;
                _baseFieldOfView = _cam.fieldOfView;
            }
        }

        public void SetTargets(Transform f1, Transform f2)
        {
            fighter1 = f1;
            fighter2 = f2;
            _cachedF1Controller = f1 != null ? f1.GetComponent<FighterController>() : null;
            _cachedF2Controller = f2 != null ? f2.GetComponent<FighterController>() : null;
        }

        public void BindFighterTransforms(Transform f1, Transform f2)
        {
            SetTargets(f1, f2);
        }

        #region State Priority Machine

        public static int GetPriority(CameraState state)
        {
            switch (state)
            {
                case CameraState.KO:
                case CameraState.Results:
                    return 90;
                case CameraState.MP4Takeover:
                case CameraState.UltimateEntry:
                case CameraState.UltimateExit:
                    return 80;
                case CameraState.ThrowCinematic:
                    return 70;
                case CameraState.WallAccent:
                    return 60;
                case CameraState.LaunchTrack:
                    return 50;
                case CameraState.CounterHitAccent:
                    return 40;
                case CameraState.HitAccent:
                    return 30;
                case CameraState.IntroEstablish:
                case CameraState.FighterReveal:
                case CameraState.RoundStart:
                    return 20;
                case CameraState.BaseCombatFrame:
                default:
                    return 10;
            }
        }

        public bool RequestState(CameraState newState, float duration = 0.25f, Transform focusTarget = null)
        {
            int currentPri = GetPriority(CurrentState);
            int newPri = GetPriority(newState);

            if (newPri >= currentPri || _stateDurationRemaining <= 0f)
            {
                CurrentState = newState;
                _stateDurationRemaining = duration;
                _focusedTarget = focusTarget;
                return true;
            }
            return false;
        }

        public void TriggerHitImpactZoom(float intensity = 0.25f, float duration = -1f)
        {
            AddHitTrauma(intensity * 0.25f);
            RequestState(CameraState.HitAccent, duration > 0f ? duration : hitAccentDuration);
        }

        public void TriggerHitAccent(float duration = -1f)
        {
            TriggerHitImpactZoom(0.25f, duration);
        }

        public void SetClutchSlowMo(bool active)
        {
            if (active)
            {
                RequestState(CameraState.KO, 1.5f);
            }
            else
            {
                ReturnToBaseCombatFrame();
            }
        }

        public void TriggerCounterHitImpactZoom(float intensity = 0.5f, float duration = -1f)
        {
            AddCounterHitTrauma(intensity * 0.35f);
            RequestState(CameraState.CounterHitAccent, duration > 0f ? duration : counterHitAccentDuration);
        }

        public void TriggerLaunchTrack(float duration = -1f)
        {
            RequestState(CameraState.LaunchTrack, duration > 0f ? duration : launchTrackDuration);
        }

        public void TriggerWallAccent(float duration = -1f)
        {
            AddWallTrauma(wallTraumaIntensity);
            RequestState(CameraState.WallAccent, duration > 0f ? duration : wallAccentDuration);
        }

        public void TriggerThrowCinematic(float duration = -1f)
        {
            RequestState(CameraState.ThrowCinematic, duration > 0f ? duration : throwAccentDuration);
        }

        public void TriggerUltimateEntry(float duration = -1f)
        {
            AddExplosionTrauma(ultimateEntryTraumaIntensity);
            RequestState(CameraState.UltimateEntry, duration > 0f ? duration : ultimateEntryDuration);
        }

        public void TriggerUltimateExit(float duration = -1f)
        {
            RequestState(CameraState.UltimateExit, duration > 0f ? duration : ultimateExitDuration);
        }

        public void TriggerKO()
        {
            AddExplosionTrauma(koTraumaIntensity);
            RequestState(CameraState.KO, koStateDuration);
        }

        public void TriggerResults()
        {
            RequestState(CameraState.Results, 999f);
        }

        public void ReturnToBaseCombatFrame()
        {
            CurrentState = CameraState.BaseCombatFrame;
            _stateDurationRemaining = 0f;
            _focusedTarget = null;
            ResetTrauma();
        }

        public void ResetTrauma()
        {
            _traumaHit = 0f;
            _traumaCounterHit = 0f;
            _traumaWall = 0f;
            _traumaExplosion = 0f;
            transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        #endregion

        #region Camera Framing & Update

        private void LateUpdate()
        {
            // Frame rate independent delta time with game pause safety
            float dt = Time.timeScale <= 0.0001f ? 0f : Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            // Screen Shake Trauma decay — always decayed even without targets
            float decay = traumaDecay * dt;
            _traumaHit = Mathf.Max(0f, _traumaHit - decay);
            _traumaCounterHit = Mathf.Max(0f, _traumaCounterHit - decay);
            _traumaWall = Mathf.Max(0f, _traumaWall - decay);
            _traumaExplosion = Mathf.Max(0f, _traumaExplosion - decay);

            float totalTrauma = Mathf.Clamp01(_traumaHit + _traumaCounterHit + _traumaWall + _traumaExplosion) * SettingsPanel.ScreenShakeIntensity * masterShakeScale;
            if (SettingsPanel.ReducedMotion || SettingsPanel.QualityTier == 0)
            {
                totalTrauma = 0f;
            }

            Vector3 shakeOffset = Vector3.zero;
            if (totalTrauma > 0.005f)
            {
                float shake = totalTrauma * totalTrauma;
                shakeOffset.x = (Mathf.PerlinNoise(Time.unscaledTime * shakeFrequency, 0f) - 0.5f) * 2f * maxShakeOffset * shake;
                shakeOffset.y = (Mathf.PerlinNoise(0f, Time.unscaledTime * shakeFrequency) - 0.5f) * 2f * maxShakeOffset * shake;
            }

            // Micro-impulse duration decay & pure post-offset
            Vector3 impulseOffset = Vector3.zero;
            if (_impulseDurationRemaining > 0f && _impulseTotalDuration > 0.0001f)
            {
                _impulseDurationRemaining = Mathf.Max(0f, _impulseDurationRemaining - dt);
                float progress = Mathf.Clamp01(_impulseDurationRemaining / _impulseTotalDuration);
                impulseOffset = _activeImpulse.direction * (_activeImpulse.magnitude * progress);

                if (_cam != null && _activeImpulse.fovPunch > 0f)
                {
                    if (usePhysicalCamera && _cam.usePhysicalProperties)
                    {
                        _cam.focalLength = _baseFocalLength * (1f + (_activeImpulse.fovPunch * microImpulseFocalPunchScale * progress));
                    }
                    else
                    {
                        _cam.fieldOfView = _baseFieldOfView - (_activeImpulse.fovPunch * progress);
                    }
                }
            }
            else if (_cam != null)
            {
                if (usePhysicalCamera && _cam.usePhysicalProperties)
                {
                    if (!Mathf.Approximately(_cam.focalLength, _baseFocalLength))
                    {
                        _cam.focalLength = _baseFocalLength;
                    }
                }
                else
                {
                    if (!Mathf.Approximately(_cam.fieldOfView, _baseFieldOfView))
                    {
                        _cam.fieldOfView = _baseFieldOfView;
                    }
                }
            }

            // Tick state timer
            if (_stateDurationRemaining > 0f)
            {
                _stateDurationRemaining = Mathf.Max(0f, _stateDurationRemaining - dt);
                if (_stateDurationRemaining <= 0f && CurrentState != CameraState.KO && CurrentState != CameraState.Results)
                {
                    CurrentState = CameraState.BaseCombatFrame;
                    _focusedTarget = null;
                }
            }

            if (fighter1 == null || fighter2 == null)
            {
                FindFightersIfMissing();
                if (fighter1 == null || fighter2 == null)
                {
                    if (_hasSmoothedBasePosition)
                    {
                        transform.position = _smoothedBasePosition + shakeOffset + impulseOffset;
                        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
                    }
                    return;
                }
            }

            // Calculate Target Position based on active state & 40% calibration
            Vector3 targetPosition = CalculateFramingPosition();

            // Deadzone filtering on neutral micro-jitter
            if (CurrentState == CameraState.BaseCombatFrame)
            {
                if (Mathf.Abs(targetPosition.x - _lastTargetPosition.x) < deadzoneX)
                {
                    targetPosition.x = _lastTargetPosition.x;
                }
                if (Mathf.Abs(targetPosition.y - _lastTargetPosition.y) < deadzoneY)
                {
                    targetPosition.y = _lastTargetPosition.y;
                }
                if (Mathf.Abs(targetPosition.z - _lastTargetPosition.z) < deadzoneZ)
                {
                    targetPosition.z = _lastTargetPosition.z;
                }
            }

            _lastTargetPosition = targetPosition;

            // Interpolate Position with independent axis exponential damping
            if (!_hasSmoothedBasePosition)
            {
                _smoothedBasePosition = transform.position;
                _hasSmoothedBasePosition = true;
            }

            float followSpeedX = smoothSpeed;
            float followSpeedY = smoothSpeedY;
            float followSpeedZ = smoothSpeedZ;

            if (CurrentState == CameraState.CounterHitAccent || CurrentState == CameraState.KO)
            {
                followSpeedX = accentFollowSpeed;
                followSpeedY = accentFollowSpeed;
                followSpeedZ = accentFollowSpeed;
            }

            float alphaX = 1f - Mathf.Exp(-followSpeedX * dt);
            float alphaY = 1f - Mathf.Exp(-followSpeedY * dt);
            float alphaZ = 1f - Mathf.Exp(-followSpeedZ * dt);

            _smoothedBasePosition.x = Mathf.Lerp(_smoothedBasePosition.x, targetPosition.x, alphaX);
            _smoothedBasePosition.y = Mathf.Lerp(_smoothedBasePosition.y, targetPosition.y, alphaY);
            _smoothedBasePosition.z = Mathf.Lerp(_smoothedBasePosition.z, targetPosition.z, alphaZ);

            // Pure post-offset application (never fed back into smoothed base lerp)
            Vector3 finalPosition = _smoothedBasePosition + shakeOffset + impulseOffset;
            transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            transform.position = finalPosition;
        }

        public void ApplyStageCameraLimits(StageDefinition stage)
        {
            if (stage == null) return;
            float nearZ = Mathf.Max(stage.cameraMinZ, stage.cameraMaxZ);
            float farZ = Mathf.Min(stage.cameraMinZ, stage.cameraMaxZ);
            minDistanceZ = Mathf.Clamp(nearZ, -8.0f, -3.8f);
            maxDistanceZ = Mathf.Clamp(farZ, -14.0f, -7.5f);
            _stageLeftWallX = -stage.ArenaLimitX;
            _stageRightWallX = stage.ArenaLimitX;
            _stageArenaLimitX = stage.ArenaLimitX;
        }

        public void ApplyStageCameraLimits(DemonIdol.BlazBlue.BBStagePrefabConfig stage)
        {
            if (stage == null) return;
            float nearZ = Mathf.Max(stage.cameraDefaultZ, stage.cameraDefaultZ - 5.0f);
            float farZ = Mathf.Min(stage.cameraDefaultZ, stage.cameraDefaultZ - 5.0f);
            minDistanceZ = Mathf.Clamp(nearZ, -8.0f, -3.8f);
            maxDistanceZ = Mathf.Clamp(farZ, -14.0f, -7.5f);
            _stageLeftWallX = stage.leftWallX;
            _stageRightWallX = stage.rightWallX;
            _stageArenaLimitX = Mathf.Max(Mathf.Abs(stage.leftWallX), Mathf.Abs(stage.rightWallX));
        }

        public Vector3 CalculateFramingPosition()
        {
            Vector3 p1 = fighter1 != null ? fighter1.position : new Vector3(-2.5f, 0, 0);
            Vector3 p2 = fighter2 != null ? fighter2.position : new Vector3(2.5f, 0, 0);

            float minFighterX = Mathf.Min(p1.x, p2.x);
            float maxFighterX = Mathf.Max(p1.x, p2.x);
            float fighterDistance = Mathf.Max(0.5f, maxFighterX - minFighterX);
            Vector3 midPoint = (p1 + p2) * 0.5f;

            if (_cam == null) _cam = GetComponent<Camera>();

            // Calculate Optical Half FOV Tangents correctly under GateFitMode.Horizontal
            float aspect = (_cam != null && _cam.aspect > 0.01f) ? _cam.aspect : (16f / 9f);
            float tanFovX;
            float tanFovY;
            if (_cam != null && _cam.usePhysicalProperties && _cam.focalLength > 0.001f)
            {
                tanFovX = (_cam.sensorSize.x * 0.5f) / _cam.focalLength;
                tanFovY = tanFovX / aspect;
            }
            else
            {
                float fovRad = (_cam != null ? _cam.fieldOfView : _baseFieldOfView) * 0.5f * Mathf.Deg2Rad;
                tanFovY = Mathf.Tan(fovRad);
                tanFovX = tanFovY * aspect;
            }

            // Safe near / far Z ordering
            float nearLimitZ = Mathf.Max(minDistanceZ, maxDistanceZ);
            float farLimitZ = Mathf.Min(minDistanceZ, maxDistanceZ);
            float zoomInLimitZ = Mathf.Min(maxCinematicZoomInZ, nearLimitZ + cinematicZoomBufferZ);

            // 1. Street Fighter 6 Dynamic Zoom Curve & Screen Height Calibration:
            // At close range (< closeDistanceMax): gentle footsies push-in
            // At neutral spacing (~neutralDistance): calibrated target screen height
            // At corner/wide spacing (>= wideDistanceMax): pulls out smoothly for jumpers/zoners
            float targetFraction;
            if (fighterDistance < closeDistanceMax)
            {
                float t = Mathf.Clamp01((fighterDistance - closeDistanceMin) / Mathf.Max(0.001f, closeDistanceMax - closeDistanceMin));
                targetFraction = Mathf.Lerp(closeCombatScreenFraction + closeCombatFractionBoost, closeCombatScreenFraction, t);
            }
            else if (fighterDistance <= neutralDistance)
            {
                float t = Mathf.Clamp01((fighterDistance - closeDistanceMax) / Mathf.Max(0.001f, neutralDistance - closeDistanceMax));
                targetFraction = Mathf.Lerp(closeCombatScreenFraction, targetCharacterScreenFraction, t);
            }
            else
            {
                float t = Mathf.Clamp01((fighterDistance - neutralDistance) / Mathf.Max(0.001f, wideDistanceMax - neutralDistance));
                targetFraction = Mathf.Lerp(targetCharacterScreenFraction, wideSpacingScreenFraction, t);
            }

            // Required vertical viewport height in world space
            float reqFrustumHeight = standardFighterHeight / Mathf.Max(0.1f, targetFraction);
            float verticalCalibratedZ = -(reqFrustumHeight * 0.5f / Mathf.Max(0.001f, tanFovY));

            // Horizontal bounds requirement: both fighters + snug margin fit inside view
            float requiredHalfWidth = (fighterDistance * 0.5f) + horizontalFramingPadding;
            float horizontalRequiredZ = -(requiredHalfWidth / Mathf.Max(0.001f, tanFovX));

            // Dynamic depth satisfying both vertical height and full horizontal visibility
            float baseZ = Mathf.Min(verticalCalibratedZ, horizontalRequiredZ);
            baseZ = Mathf.Clamp(baseZ, farLimitZ, nearLimitZ);

            // 2. SF6 Asymmetric Aggression Lead Tracking (using cached controllers):
            // Camera slightly leads the attacking or advancing fighter, giving momentum breathing space
            float leadBiasX = 0f;
            if (_cachedF1Controller == null && fighter1 != null) _cachedF1Controller = fighter1.GetComponent<FighterController>();
            if (_cachedF2Controller == null && fighter2 != null) _cachedF2Controller = fighter2.GetComponent<FighterController>();

            FighterController fc1 = _cachedF1Controller;
            FighterController fc2 = _cachedF2Controller;

            if (fc1 != null && fc2 != null)
            {
                bool f1Attacking = fc1.State == FighterState.AttackStartup || fc1.State == FighterState.AttackActive;
                bool f2Attacking = fc2.State == FighterState.AttackStartup || fc2.State == FighterState.AttackActive;

                if (f1Attacking && !f2Attacking)
                {
                    leadBiasX = fc1.FacingDirection * aggressionLeadAmount;
                }
                else if (f2Attacking && !f1Attacking)
                {
                    leadBiasX = fc2.FacingDirection * aggressionLeadAmount;
                }
            }

            float targetX = midPoint.x + offset.x + leadBiasX;

            // 3. SF6 Grounded Eye Level & Decoupled Vertical Motion:
            // Normal jumps stay inside the upper frame (zero bobbing).
            // Only aerial launches above threshold track vertically upwards.
            float targetY = offset.y;
            float airbornePeak = Mathf.Max(p1.y, p2.y);
            if (airbornePeak > verticalLaunchThresholdY)
            {
                targetY = offset.y + (airbornePeak - verticalLaunchThresholdY) * verticalLaunchFollowFactor;
            }

            float targetZ = baseZ;

            // 4. Stage Arena Boundary Clamping:
            // Prevent camera frustum from ever showing out-of-bounds void
            float distToBg = Mathf.Abs(targetZ) + backgroundDistanceOffset;
            float halfWidthAtBg = distToBg * tanFovX;
            float maxCamX = Mathf.Max(0f, _stageArenaLimitX - halfWidthAtBg);

            var activeStage = DemonIdol.BlazBlue.BBStagePrefabConfig.ActiveConfig;
            if (activeStage != null)
            {
                maxCamX = Mathf.Max(Mathf.Abs(activeStage.cameraMinX), Mathf.Abs(activeStage.cameraMaxX));
                targetX = Mathf.Clamp(targetX, activeStage.cameraMinX, activeStage.cameraMaxX);
                targetY = Mathf.Clamp(targetY, activeStage.cameraMinY, activeStage.cameraMaxY);
            }
            else
            {
                targetX = Mathf.Clamp(targetX, -maxCamX, maxCamX);
            }

            // Frustum Safety Invariant: Ensure cornered fighters remain inside viewport
            float currentHalfViewX = Mathf.Abs(targetZ) * tanFovX;
            if (targetX + currentHalfViewX < maxFighterX + cornerViewportEdgePadding)
            {
                float neededHalfView = (maxFighterX + cornerViewportEdgePadding) - targetX;
                targetZ = Mathf.Min(targetZ, -(neededHalfView / tanFovX));
            }
            if (targetX - currentHalfViewX > minFighterX - cornerViewportEdgePadding)
            {
                float neededHalfView = targetX - (minFighterX - cornerViewportEdgePadding);
                targetZ = Mathf.Min(targetZ, -(neededHalfView / tanFovX));
            }
            targetZ = Mathf.Clamp(targetZ, farLimitZ, nearLimitZ);

            // 5. Cinematic Accent Overrides (all zoom inward closer to z=0 than baseZ)
            switch (CurrentState)
            {
                case CameraState.HitAccent:
                    targetZ = Mathf.Clamp(baseZ + hitAccentZoomDelta, farLimitZ, zoomInLimitZ);
                    break;

                case CameraState.CounterHitAccent:
                    targetZ = Mathf.Clamp(baseZ + counterHitZoomDelta, farLimitZ, zoomInLimitZ);
                    break;

                case CameraState.LaunchTrack:
                    float airborneHighest = Mathf.Max(p1.y, p2.y);
                    float airborneLowest = Mathf.Min(p1.y, p2.y, 0f);
                    targetY = Mathf.Clamp((airborneHighest + airborneLowest) * 0.5f, launchTrackMinY, launchTrackMaxY);
                    float reqHalfHeight = (airborneHighest - airborneLowest) * 0.5f + launchTrackPaddingY;
                    float reqZ = -(reqHalfHeight / Mathf.Max(0.001f, tanFovY));
                    targetZ = Mathf.Min(baseZ, reqZ);
                    targetZ = Mathf.Clamp(targetZ, farLimitZ, nearLimitZ);
                    break;

                case CameraState.WallAccent:
                    targetZ = Mathf.Clamp(baseZ + wallAccentZoomDelta, farLimitZ, zoomInLimitZ);
                    break;

                case CameraState.ThrowCinematic:
                    targetZ = Mathf.Clamp(baseZ + throwAccentZoomDelta, farLimitZ, zoomInLimitZ);
                    break;

                case CameraState.UltimateEntry:
                    targetZ = Mathf.Clamp(baseZ + ultimateEntryZoomDelta, farLimitZ, zoomInLimitZ);
                    break;

                case CameraState.KO:
                    Transform focus = _focusedTarget != null ? _focusedTarget : (p1.y < p2.y ? fighter1 : fighter2);
                    if (focus != null)
                    {
                        targetX = focus.position.x;
                        targetY = focus.position.y + koFocusOffsetY;
                    }
                    if (activeStage != null)
                    {
                        targetX = Mathf.Clamp(targetX, activeStage.cameraMinX, activeStage.cameraMaxX);
                        targetY = Mathf.Clamp(targetY, activeStage.cameraMinY, activeStage.cameraMaxY);
                    }
                    else
                    {
                        targetX = Mathf.Clamp(targetX, -maxCamX, maxCamX);
                    }
                    targetZ = Mathf.Clamp(baseZ + koZoomDelta, farLimitZ, zoomInLimitZ);
                    break;

                case CameraState.Results:
                    Transform winFocus = _focusedTarget != null ? _focusedTarget : (p1.y < p2.y ? fighter2 : fighter1);
                    if (winFocus != null)
                    {
                        targetX = winFocus.position.x;
                        targetY = winFocus.position.y + resultsFocusOffsetY;
                        if (activeStage != null)
                        {
                            targetX = Mathf.Clamp(targetX, activeStage.cameraMinX, activeStage.cameraMaxX);
                            targetY = Mathf.Clamp(targetY, activeStage.cameraMinY, activeStage.cameraMaxY);
                        }
                        else
                        {
                            targetX = Mathf.Clamp(targetX, -maxCamX, maxCamX);
                        }
                        targetZ = Mathf.Clamp(resultsCameraZ, farLimitZ, zoomInLimitZ);
                    }
                    break;

                case CameraState.BaseCombatFrame:
                default:
                    break;
            }

            return new Vector3(targetX, targetY, targetZ);
        }

        private void FindFightersIfMissing()
        {
            if (fighter1 != null && fighter2 != null) return;

            if (Time.unscaledTime - _lastCameraFighterSearchTime < 2.0f) return;
            _lastCameraFighterSearchTime = Time.unscaledTime;

            SubscribeMatchSim();

            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                var sim = DemonIdol.BlazBlue.BBMatchSim.Instance;
                if (sim.p1Brain != null && fighter1 == null)
                    fighter1 = sim.p1Brain.transform;
                if (sim.p2Brain != null && fighter2 == null)
                    fighter2 = sim.p2Brain.transform;
                if (fighter1 != null && fighter2 != null && fighter1 != fighter2)
                {
                    SetTargets(fighter1, fighter2);
                    return;
                }
            }

            var controllers = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            FighterController p1 = null;
            FighterController p2 = null;
            foreach (var f in controllers)
            {
                if (f == null) continue;
                if (f.isPlayerControlled && p1 == null)
                {
                    p1 = f;
                }
                else if (p2 == null)
                {
                    p2 = f;
                }
            }
            if (p1 == null && controllers.Length >= 2)
            {
                p1 = controllers[0];
                p2 = controllers[1];
            }
            if (p1 != null && p2 != null && p1 != p2)
            {
                SetTargets(p1.transform, p2.transform);
            }
        }

        public bool AreFightersInFrustum(Camera cam, Bounds p1Bounds, Bounds p2Bounds)
        {
            if (cam == null) return false;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            return GeometryUtility.TestPlanesAABB(planes, p1Bounds) && GeometryUtility.TestPlanesAABB(planes, p2Bounds);
        }

        public static bool IsBoundsCompletelyInsideFrustum(Plane[] planes, Bounds bounds)
        {
            if (planes == null || planes.Length < 6) return false;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };

            for (int p = 0; p < planes.Length; p++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (planes[p].GetDistanceToPoint(corners[c]) < -0.05f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region Shake Layer Helpers

        public void AddHitTrauma(float amount) => _traumaHit = Mathf.Clamp01(_traumaHit + amount * 0.35f);
        public void AddCounterHitTrauma(float amount) => _traumaCounterHit = Mathf.Clamp01(_traumaCounterHit + amount * 0.35f);
        public void AddWallTrauma(float amount) => _traumaWall = Mathf.Clamp01(_traumaWall + amount * 0.35f);
        public void AddExplosionTrauma(float amount) => _traumaExplosion = Mathf.Clamp01(_traumaExplosion + amount * 0.35f);
        public void AddTrauma(float amount) => AddHitTrauma(amount);

        public void ApplyMicroImpulse(Vector3 direction, float magnitude, int ticks, float fovPunch = 0f)
        {
            if (magnitude <= 0.001f || ticks <= 0) return;
            _impulseTotalDuration = ticks / 60f;
            _impulseDurationRemaining = _impulseTotalDuration;
            _activeImpulse = new CameraMicroImpulse
            {
                direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.back,
                magnitude = magnitude,
                totalTicks = ticks,
                ticksRemaining = ticks,
                fovPunch = fovPunch
            };
        }

        #endregion

        #region Context Menu Helpers for Unity Editor

        [ContextMenu("Trigger Test Hit Accent")]
        public void TestHitAccent() => TriggerHitImpactZoom(hitTraumaIntensity, hitAccentDuration);

        [ContextMenu("Trigger Test Counter Hit Accent")]
        public void TestCounterHitAccent() => TriggerCounterHitImpactZoom(counterHitTraumaIntensity, counterHitAccentDuration);

        [ContextMenu("Trigger Test Wall Accent")]
        public void TestWallAccent() => TriggerWallAccent(wallAccentDuration);

        [ContextMenu("Trigger Test KO Cinematic")]
        public void TestKO() => TriggerKO();

        [ContextMenu("Return To Base Combat Frame")]
        public void TestResetToBase() => ReturnToBaseCombatFrame();

        #endregion
    }
}
