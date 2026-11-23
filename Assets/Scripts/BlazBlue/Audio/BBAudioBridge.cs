using System;
using UnityEngine;
using DemonIdol.Audio;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue
{
    public enum BBSoundEffect
    {
        HitLight = 0,
        HitMedium = 1,
        HitHeavy = 2,
        FatalCounter = 3,
        BlockNormal = 4,
        BlockBarrier = 5,
        BarrierShatter = 6,
        RapidCancel = 7,
        OverdriveActivate = 8,
        DistortionSuperFlash = 9,
        AstralHeat = 10,
        WeaponSwingLight = 11,
        WeaponSwingHeavy = 12,
        WeaponSwingColossal = 13,
        WeaponBowFire = 14,
        WeaponStaffHum = 15
    }

    /// <summary>
    /// Audio & VFX Subsystem Bridge for BlazBlue 4-Button (Spec §11.3, M13).
    /// Dispatches audio cues and visual impact events for hits, blocks,
    /// Rapid Cancels, Overdrive bursts, and Distortion super flashes.
    /// Integrated with CombatAudioDirector with robust zero-crash fallbacks.
    /// </summary>
    public class BBAudioBridge : MonoBehaviour
    {
        private static BBAudioBridge _instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        public static BBAudioBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<BBAudioBridge>(FindObjectsInactive.Include);
                }
                return _instance;
            }
        }

        [Header("Audio Source")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Audio Clips (Optional Overrides)")]
        [SerializeField] private AudioClip _hitLightClip;
        [SerializeField] private AudioClip _hitHeavyClip;
        [SerializeField] private AudioClip _fatalCounterClip;
        [SerializeField] private AudioClip _barrierBlockClip;
        [SerializeField] private AudioClip _barrierShatterClip;
        [SerializeField] private AudioClip _rapidCancelClip;
        [SerializeField] private AudioClip _overdriveClip;

        [Header("Impact VFX Prefabs")]
        [SerializeField] private GameObject _slashVFXPrefab;
        [SerializeField] private GameObject _bluntVFXPrefab;
        [SerializeField] private GameObject _arcaneVFXPrefab;
        [SerializeField] private GameObject _groundCrushVFXPrefab;

        private void Awake()
        {
            _instance = this;
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void PlayCombatSound(BBSoundEffect effect, Vector3 worldPosition)
        {
            // First attempt routing to CombatAudioDirector if active
            var director = CombatAudioDirector.Instance;

            switch (effect)
            {
                case BBSoundEffect.HitLight:
                    if (director != null) director.PlayHitLight();
                    else PlayClipDirect(_hitLightClip);
                    break;

                case BBSoundEffect.HitMedium:
                    if (director != null) director.PlayHitMedium();
                    else PlayClipDirect(_hitLightClip);
                    break;

                case BBSoundEffect.HitHeavy:
                    if (director != null) director.PlayHitHeavy();
                    else PlayClipDirect(_hitHeavyClip);
                    break;

                case BBSoundEffect.FatalCounter:
                    if (director != null) director.PlayHitHeavy();
                    else PlayClipDirect((_fatalCounterClip != null) ? _fatalCounterClip : _hitHeavyClip);
                    break;

                case BBSoundEffect.BlockNormal:
                    if (director != null) director.PlayBlock();
                    else PlayClipDirect(_hitLightClip);
                    break;

                case BBSoundEffect.BlockBarrier:
                    if (director != null) director.PlayParry();
                    else PlayClipDirect(_barrierBlockClip);
                    break;

                case BBSoundEffect.BarrierShatter:
                    if (director != null) director.PlayDomainActivation();
                    else PlayClipDirect(_barrierShatterClip);
                    break;

                case BBSoundEffect.RapidCancel:
                    if (director != null) director.PlayRapidCancel();
                    else PlayClipDirect(_rapidCancelClip);
                    break;

                case BBSoundEffect.OverdriveActivate:
                    if (director != null) director.PlayDomainActivation();
                    else PlayClipDirect(_overdriveClip);
                    break;

                case BBSoundEffect.DistortionSuperFlash:
                    if (director != null) director.PlayDomainActivation();
                    else PlayClipDirect(_overdriveClip);
                    break;

                case BBSoundEffect.AstralHeat:
                    if (director != null) director.PlayVoiceDomainSuper();
                    else PlayClipDirect(_overdriveClip);
                    break;
            }
        }

        public void PlayWeaponSwing(BBWarriorType warrior, Vector3 worldPosition)
        {
            var director = CombatAudioDirector.Instance;
            switch (warrior)
            {
                case BBWarriorType.TwoHanded:
                case BBWarriorType.Hammer:
                case BBWarriorType.Brute:
                    if (director != null) director.PlaySwingHeavy();
                    else PlayClipDirect(_hitHeavyClip);
                    break;

                case BBWarriorType.Mage:
                case BBWarriorType.Sorceress:
                    if (director != null) director.PlayParry();
                    else PlayClipDirect(_barrierBlockClip);
                    break;

                case BBWarriorType.Swordsman:
                case BBWarriorType.Knight:
                case BBWarriorType.Ninja:
                case BBWarriorType.Karate:
                case BBWarriorType.Spearman:
                case BBWarriorType.Archer:
                case BBWarriorType.Crossbow:
                default:
                    if (director != null) director.PlaySwingLight();
                    else PlayClipDirect(_hitLightClip);
                    break;
            }
        }

        public void PlayImpactVFX(Vector3 worldPosition, BBWarriorType warrior, bool isFatal, bool isBlock)
        {
            GameObject targetPrefab = _slashVFXPrefab;
            if (isBlock)
            {
                targetPrefab = (_bluntVFXPrefab != null) ? _bluntVFXPrefab : _slashVFXPrefab;
            }
            else
            {
                switch (warrior)
                {
                    case BBWarriorType.Hammer:
                    case BBWarriorType.Brute:
                        targetPrefab = isFatal ? ((_groundCrushVFXPrefab != null) ? _groundCrushVFXPrefab : _bluntVFXPrefab) : _bluntVFXPrefab;
                        break;
                    case BBWarriorType.Mage:
                    case BBWarriorType.Sorceress:
                        targetPrefab = (_arcaneVFXPrefab != null) ? _arcaneVFXPrefab : _slashVFXPrefab;
                        break;
                    default:
                        targetPrefab = _slashVFXPrefab;
                        break;
                }
            }

            if (targetPrefab != null)
            {
                var instance = Instantiate(targetPrefab, worldPosition, Quaternion.identity);
                Destroy(instance, 1.5f);
            }
        }

        private void PlayClipDirect(AudioClip clip)
        {
            if (clip != null && clip.loadState != AudioDataLoadState.Failed && _audioSource != null && _audioSource.isActiveAndEnabled)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
