using System;
using UnityEngine;
using DemonIdol.Audio;

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
        AstralHeat = 10
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
        public static BBAudioBridge Instance => _instance;

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
                case BBSoundEffect.HitHeavy:
                    if (director != null) director.PlayHitHeavy();
                    else PlayClipDirect(_hitHeavyClip);
                    break;

                case BBSoundEffect.FatalCounter:
                    if (director != null) director.PlayHitHeavy();
                    else PlayClipDirect(_fatalCounterClip ?? _hitHeavyClip);
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

        private void PlayClipDirect(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
