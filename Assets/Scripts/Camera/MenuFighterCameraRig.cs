using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DemonIdol.Launch;
using DemonIdol.UI;
using DemonIdol.BlazBlue;

namespace DemonIdol.Cinematics
{
    /// <summary>
    /// Phase 2: Telephoto Close-Up Menu Camera Rig & Dynamic Focus Controller.
    /// Hardened: explicit non-physical FOV ownership, framerate-independent damping,
    /// safe runtime VolumeProfile lifecycle, and null/disable-safe chromatic aberration pulses.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class MenuFighterCameraRig : MonoBehaviour
    {
        public static MenuFighterCameraRig Instance { get; private set; }

        [Header("Camera Optics")]
        [Range(24f, 45f)] public float targetFOV = 30f;
        public float framingDamping = 8.0f;
        public Vector3 cameraOffsetFromHead = new Vector3(0.0f, 0.05f, -2.6f);

        [Header("URP Post-Processing Volume")]
        public Volume postProcessVolume;

        private UnityEngine.Camera _cam;
        private Transform _targetModel;
        private Transform _headBone;
        private string _currentCharacterId = "C01";
        private Vector3 _calculatedHeadPosition;
        private Coroutine _chromaPulseRoutine;

        private DepthOfField _dof;
        private ChromaticAberration _chroma;
        private bool _ownsVolumeObject;
        private VolumeProfile _runtimeProfile;
        private bool _isDuplicate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                _isDuplicate = true;
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = GetComponent<UnityEngine.Camera>();
            ApplyMenuOptics();
            FindOrCreateVolume();
        }

        private void OnEnable()
        {
            if (_isDuplicate) return;
            if (Instance == null) Instance = this;
            ApplyMenuOptics();
        }

        private void Start()
        {
            if (_isDuplicate) return;
            ApplyQualitySettings();
        }

        private void OnDisable()
        {
            if (_isDuplicate) return;
            if (_chromaPulseRoutine != null)
            {
                StopCoroutine(_chromaPulseRoutine);
                _chromaPulseRoutine = null;
            }

            if (_chroma != null)
            {
                _chroma.intensity.Override(0f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (_isDuplicate) return;

            if (_runtimeProfile != null)
            {
                Destroy(_runtimeProfile);
                _runtimeProfile = null;
            }

            if (_ownsVolumeObject && postProcessVolume != null)
            {
                Destroy(postProcessVolume.gameObject);
                postProcessVolume = null;
                _ownsVolumeObject = false;
            }
        }

        private void ApplyMenuOptics()
        {
            if (_cam == null) _cam = GetComponent<UnityEngine.Camera>();
            if (_cam == null) return;

            // Ensure combat physical-camera mode cannot lock out menu FOV updates.
            _cam.usePhysicalProperties = false;
            _cam.fieldOfView = targetFOV;
        }

        public void FindOrCreateVolume()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindAnyObjectByType<Volume>();
            }

            if (postProcessVolume == null)
            {
                var volObj = new GameObject("Menu_PostProcessVolume");
                postProcessVolume = volObj.AddComponent<Volume>();
                postProcessVolume.isGlobal = true;
                _ownsVolumeObject = true;
            }

            if (postProcessVolume.sharedProfile == null)
            {
                _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                postProcessVolume.profile = _runtimeProfile;
            }
            else if (Application.isPlaying && _runtimeProfile == null)
            {
                // Clone profile at runtime so menu overrides never mutate serialized project assets.
                _runtimeProfile = Instantiate(postProcessVolume.sharedProfile);
                postProcessVolume.profile = _runtimeProfile;
            }

            var prof = postProcessVolume.profile;
            if (prof == null) return;

            if (!prof.Has<DepthOfField>())
            {
                var d = prof.Add<DepthOfField>(true);
                d.mode.Override(DepthOfFieldMode.Bokeh);
                d.aperture.Override(3.2f);
                d.focalLength.Override(65f);
                d.focusDistance.Override(2.6f);
            }

            if (!prof.Has<Tonemapping>())
            {
                var tm = prof.Add<Tonemapping>(true);
                tm.mode.Override(TonemappingMode.ACES);
            }

            if (!prof.Has<ColorAdjustments>())
            {
                var ca = prof.Add<ColorAdjustments>(true);
                ca.contrast.Override(12f);
            }

            if (!prof.Has<Bloom>())
            {
                var bl = prof.Add<Bloom>(true);
                bl.threshold.Override(1.05f);
                bl.intensity.Override(0.35f);
            }

            if (!prof.Has<Vignette>())
            {
                var vg = prof.Add<Vignette>(true);
                vg.intensity.Override(0.32f);
                vg.color.Override(new Color(0.035f, 0.043f, 0.055f, 1f));
            }

            if (!prof.Has<ChromaticAberration>())
            {
                var ca = prof.Add<ChromaticAberration>(true);
                ca.intensity.Override(0.0f);
            }

            prof.TryGet(out _dof);
            prof.TryGet(out _chroma);
        }

        public void ApplyQualitySettings()
        {
            int quality = PlayerPrefs.GetInt("MDI_QualityPreset", 1);
            if (postProcessVolume != null)
            {
                GraphicsProfiles.ApplyToVolume(postProcessVolume, quality);
            }
        }

        /// <summary>
        /// Registers a new active fighter model and dynamically resolves their head/chest bone for eye-level framing.
        /// </summary>
        public void SetTargetFighter(GameObject model, string characterId)
        {
            if (model == null)
            {
                _targetModel = null;
                _headBone = null;
                return;
            }

            _targetModel = model.transform;
            _currentCharacterId = string.IsNullOrEmpty(characterId) ? "C01" : characterId;

            // 1. Try finding Humanoid head bone
            Animator anim = model.GetComponentInChildren<Animator>();
            _headBone = null;
            if (anim != null && anim.isHuman)
            {
                _headBone = anim.GetBoneTransform(HumanBodyBones.Head);
                if (_headBone == null) _headBone = anim.GetBoneTransform(HumanBodyBones.Neck);
                if (_headBone == null) _headBone = anim.GetBoneTransform(HumanBodyBones.Chest);
            }

            // 2. Search hierarchy for head or neck bone
            if (_headBone == null)
            {
                Transform[] allChildren = model.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    string n = t.name.ToLowerInvariant();
                    if (n.Contains("head") && !n.Contains("hair") && !n.Contains("mesh"))
                    {
                        _headBone = t;
                        break;
                    }
                }
            }

            // 3. Fallback based on BBRosterArchetype hipHeight
            UpdateCalculatedHeadPosition();
        }

        private void UpdateCalculatedHeadPosition()
        {
            if (_headBone != null)
            {
                _calculatedHeadPosition = _headBone.position;
            }
            else if (_targetModel != null)
            {
                var info = BBRosterArchetype.Get(_currentCharacterId);
                float estimatedHeadY = _targetModel.position.y + (info.hipHeight > 0 ? info.hipHeight + 0.65f : 1.55f);
                _calculatedHeadPosition = new Vector3(_targetModel.position.x, estimatedHeadY, _targetModel.position.z);
            }
        }

        private void LateUpdate()
        {
            if (_targetModel == null || _cam == null) return;
            UpdateCalculatedHeadPosition();

            // Framerate-independent exponential damping for FOV
            if (Mathf.Abs(_cam.fieldOfView - targetFOV) > 0.05f)
            {
                if (_cam.usePhysicalProperties) _cam.usePhysicalProperties = false;
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                float alpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, framingDamping) * dt);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, alpha);
            }

            // Dynamically lock Bokeh DOF focus distance to head/face
            if (_dof != null && _dof.active)
            {
                float distToFace = Vector3.Distance(_cam.transform.position, _calculatedHeadPosition);
                _dof.focusDistance.Override(Mathf.Max(0.1f, distToFace));
            }
        }

        /// <summary>
        /// Pulses Chromatic Aberration on beat-drops, lock-ins, or counter-hits.
        /// Respects SettingsPanel.ReducedMotion and GraphicsProfiles.
        /// </summary>
        public void PulseChromaticAberration(float intensity = 0.35f, float duration = 0.15f)
        {
            if (!isActiveAndEnabled || _chroma == null) return;

            if (SettingsPanel.ReducedMotion)
            {
                intensity = 0.05f;
                duration = 0.05f;
            }

            int quality = PlayerPrefs.GetInt("MDI_QualityPreset", 1);
            if (!GraphicsProfiles.IsChromaticAberrationEnabled(quality))
            {
                return;
            }

            if (_chromaPulseRoutine != null)
            {
                StopCoroutine(_chromaPulseRoutine);
                _chromaPulseRoutine = null;
            }

            if (duration <= 0.001f || intensity <= 0f)
            {
                _chroma.intensity.Override(0f);
                return;
            }

            _chromaPulseRoutine = StartCoroutine(DoChromaPulse(intensity, duration));
        }

        private IEnumerator DoChromaPulse(float maxIntensity, float duration)
        {
            if (_chroma == null || duration <= 0.001f)
            {
                _chromaPulseRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float current = Mathf.Lerp(maxIntensity, 0f, t);
                _chroma.intensity.Override(current);
                yield return null;
            }

            _chroma.intensity.Override(0f);
            _chromaPulseRoutine = null;
        }
    }
}
