using System;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Audio
{
    /// <summary>
    /// 128 BPM combat rhythm conductor & master sound director.
    /// Drives match soundtrack tempo, audio quantizing, and dynamic sound effects.
    /// Features 100% placeholder fulfillment with procedural synthesis fallbacks
    /// and automatic discovery of project audio assets.
    /// </summary>
    public class CombatAudioDirector : MonoBehaviour
    {
        private static CombatAudioDirector _instance;
        public static CombatAudioDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<CombatAudioDirector>(FindObjectsInactive.Include);
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("CombatAudioDirector");
                        _instance = go.AddComponent<CombatAudioDirector>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public const float COMBAT_BPM = 128.0f;
        public const float SECONDS_PER_BEAT = 60.0f / COMBAT_BPM; // ~0.46875s

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Combat SFX Clips")]
        [SerializeField] private AudioClip _hitLightClip;
        [SerializeField] private AudioClip _hitMediumClip;
        [SerializeField] private AudioClip _hitHeavyClip;
        [SerializeField] private AudioClip _parryClip;
        [SerializeField] private AudioClip _blockClip;
        [SerializeField] private AudioClip _rapidCancelClip;
        [SerializeField] private AudioClip _domainActivationClip;
        [SerializeField] private AudioClip _footstepClip;
        [SerializeField] private AudioClip _swingLightClip;
        [SerializeField] private AudioClip _swingHeavyClip;
        [SerializeField] private AudioClip _landClip;

        [Header("Deadly Kombat Round Call Clips")]
        [SerializeField] private AudioClip _round1Clip;
        [SerializeField] private AudioClip _round2Clip;
        [SerializeField] private AudioClip _finalRoundClip;
        [SerializeField] private AudioClip _fightClip;
        [SerializeField] private AudioClip _victoryClip;

        [Header("Cyberpunk Sci-Fi Soundtrack Loops")]
        [SerializeField] private AudioClip _bgmFullTrack;
        [SerializeField] private AudioClip _loopIntro;
        [SerializeField] private AudioClip _loopBridge;
        [SerializeField] private AudioClip _loopDrop;
        [SerializeField] private AudioClip _loopChorus;
        [SerializeField] private AudioClip _loopOutro;

        [Header("Projectile SFX Clips")]
        [SerializeField] private AudioClip _projFireClip;
        [SerializeField] private AudioClip _projImpactClip;

        [Header("Menu Soundtrack")]
        [SerializeField] private AudioClip _bgmMenuTrack;

        [Header("UI SFX Clips")]
        [SerializeField] private AudioClip _btnClickClip;
        [SerializeField] private AudioClip _btnHoverClip;

        public int CurrentBeat { get; private set; } = 0;
        public int CurrentBar { get; private set; } = 0;

        public event Action<int> OnBeat;
        public event Action<int> OnBar;

        private float _dspStartTime = 0f;
        private bool _isPlaying = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // The main menu spawns a dedicated audio object and marks it DontDestroyOnLoad.
                // This component also lives on Combat_Managers next to the tick engine.
                // Destroying the whole GameObject here deletes the match clock.
                DestroyDuplicateComponent(this);
                return;
            }
            Instance = this;
            if (Application.isPlaying && !SharesMatchHost(gameObject))
                DontDestroyOnLoad(gameObject);

            EnsureSources();
            FulfillMissingClips();
            ApplyInitialVolumes();
        }

        private void OnEnable()
        {
            SubscribeSim();
        }

        private void OnDisable()
        {
            UnsubscribeSim();
        }

        public void SubscribeSim()
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

        public void UnsubscribeSim()
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

        private void HandleSimHitConfirmed(int attackerIndex, int defenderIndex, float damage, Vector3 hitPoint, bool isCounterHit, bool isFatal)
        {
            if (isCounterHit || isFatal || damage >= 800f) PlayHitHeavy();
            else if (damage >= 400f) PlayHitMedium();
            else PlayHitLight();
        }

        private void HandleSimBlockLanded(int attackerIndex, int defenderIndex, Vector3 hitPoint, bool isBarrier, bool isChip)
        {
            PlayBlock();
        }

        private void HandleSimRapidCanceled(int playerIndex, Vector3 pos)
        {
            PlayRapidCancel();
        }

        private void HandleSimBurstExecuted(int playerIndex, DemonIdol.BlazBlue.BBBurstType burstType, Vector3 pos)
        {
            PlayBurst();
        }

        private void HandleSimOverdriveActivated(int playerIndex, Vector3 pos)
        {
            PlayDomainActivation();
        }

        private void HandleSimRoundEnded(int winnerIndex, string reason)
        {
            PlayVictorySound();
        }

        private void OnDestroy()
        {
            UnsubscribeSim();
            if (Instance == this)
                Instance = null;
        }

        public static bool SharesMatchHost(GameObject host)
        {
            if (host == null) return false;
            return host.GetComponent<DemonIdol.Combat.CombatTickEngine>() != null
                || host.GetComponent<DemonIdol.Combat.HomegrownCombatDirector>() != null
                || host.GetComponent<DemonIdol.Combat.MatchRoundReferee>() != null;
        }

        public static void DestroyDuplicateComponent(Component component)
        {
            if (component == null) return;
            if (component is Behaviour behaviour) behaviour.enabled = false;
            if (Application.isPlaying) Destroy(component);
            else DestroyImmediate(component);
        }

        private void Start()
        {
            if (Instance != this) return;
            StartConductor();
            SubscribeSim();
        }

        public AudioSource MusicSource => _musicSource;
        public AudioSource SfxSource => _sfxSource;
        public AudioClip HitLightClip => _hitLightClip;
        public AudioClip HitMediumClip => _hitMediumClip;
        public AudioClip HitHeavyClip => _hitHeavyClip;
        public AudioClip BlockClip => _blockClip;
        public AudioClip ParryClip => _parryClip;
        public AudioClip RapidCancelClip => _rapidCancelClip;
        public AudioClip DomainActivationClip => _domainActivationClip;
        public AudioClip SwingLightClip => _swingLightClip;
        public AudioClip SwingHeavyClip => _swingHeavyClip;
        public AudioClip ProjFireClip => _projFireClip;
        public AudioClip ProjImpactClip => _projImpactClip;
        public AudioClip Round1Clip => _round1Clip;
        public AudioClip Round2Clip => _round2Clip;
        public AudioClip FinalRoundClip => _finalRoundClip;
        public AudioClip FightClip => _fightClip;
        public AudioClip VictoryClip => _victoryClip;
        public AudioClip BgmFightTrack => _bgmFullTrack;
        public AudioClip BgmMenuTrack => _bgmMenuTrack;

        public void EnsureSources()
        {
            if (this == null) return;
            if (_musicSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                if (sources.Length > 0 && sources[0] != null) _musicSource = sources[0];
                else _musicSource = gameObject.AddComponent<AudioSource>();

                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
                _musicSource.spatialBlend = 0f; // Pure 2D
            }

            if (_sfxSource == null || _sfxSource == _musicSource)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null && sources[i] != _musicSource)
                    {
                        _sfxSource = sources[i];
                        break;
                    }
                }

                if (_sfxSource == null || _sfxSource == _musicSource)
                {
                    _sfxSource = gameObject.AddComponent<AudioSource>();
                }

                _sfxSource.loop = false;
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f; // Pure 2D
            }
        }

        public void FulfillMissingClips()
        {
            // Runtime loading outside editor via Resources
            if (_bgmFullTrack == null)
            {
                _bgmFullTrack = Resources.Load<AudioClip>("Audio/BGM/bgm_fight")
                    ?? Resources.Load<AudioClip>("Audio/01 - Go To Miami")
                    ?? Resources.Load<AudioClip>("Audio/Soundtrack/01 - Go To Miami");
            }
            if (_bgmMenuTrack == null)
            {
                _bgmMenuTrack = Resources.Load<AudioClip>("Audio/BGM/bgm_menu");
            }
            if (_round1Clip == null) _round1Clip = Resources.Load<AudioClip>("Audio/SFX/sfx_round1") ?? Resources.Load<AudioClip>("Audio/round1") ?? Resources.Load<AudioClip>("Audio/Announcer/round1");
            if (_round2Clip == null) _round2Clip = Resources.Load<AudioClip>("Audio/SFX/sfx_round2") ?? Resources.Load<AudioClip>("Audio/round2") ?? Resources.Load<AudioClip>("Audio/Announcer/round2");
            if (_finalRoundClip == null) _finalRoundClip = Resources.Load<AudioClip>("Audio/SFX/sfx_round2") ?? Resources.Load<AudioClip>("Audio/finalRound") ?? Resources.Load<AudioClip>("Audio/Announcer/finalRound");
            if (_fightClip == null) _fightClip = Resources.Load<AudioClip>("Audio/SFX/sfx_fight") ?? Resources.Load<AudioClip>("Audio/fight") ?? Resources.Load<AudioClip>("Audio/Announcer/fight");
            if (_victoryClip == null) _victoryClip = Resources.Load<AudioClip>("Audio/SFX/sfx_ko") ?? Resources.Load<AudioClip>("Audio/victory") ?? Resources.Load<AudioClip>("Audio/Announcer/victory");

#if UNITY_EDITOR
            // Auto-load project audio assets from Assets/Audio/
            if (_hitLightClip == null) _hitLightClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_hit_light.wav");
            if (_hitMediumClip == null) _hitMediumClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_hit_light.wav");
            if (_hitHeavyClip == null) _hitHeavyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_hit_heavy.wav");
            if (_parryClip == null) _parryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_guard.wav");
            if (_blockClip == null) _blockClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_guard.wav");
            if (_rapidCancelClip == null) _rapidCancelClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_whiff_light.wav");
            if (_domainActivationClip == null) _domainActivationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_thunder_close.wav");
            if (_swingLightClip == null) _swingLightClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_whiff_light.wav");
            if (_swingHeavyClip == null) _swingHeavyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_whiff_heavy.wav");
            if (_projFireClip == null) _projFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_proj_fire.wav");
            if (_projImpactClip == null) _projImpactClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_proj_impact.wav");
            if (_round1Clip == null) _round1Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_round1.wav");
            if (_round2Clip == null) _round2Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_round2.wav");
            if (_finalRoundClip == null) _finalRoundClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_round2.wav");
            if (_fightClip == null) _fightClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_fight.wav");
            if (_victoryClip == null) _victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_ko.wav");
            if (_btnClickClip == null) _btnClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_menu_select.wav");
            if (_btnHoverClip == null) _btnHoverClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_menu_select.wav");
            if (_bgmMenuTrack == null) _bgmMenuTrack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_menu.wav");
            if (_bgmFullTrack == null) _bgmFullTrack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_fight.wav");
#endif

            if (_musicSource != null && _musicSource.clip == null)
            {
                _musicSource.clip = _bgmFullTrack ?? _loopBridge;
            }

            // Procedural waveform synthesis for any remaining null clips (guarantees 100% coverage, 0 dead sounds)
            if (_hitLightClip == null) _hitLightClip = GenerateImpactTone("SFX_HitLight", 0.08f, 520f, 150f, 0.35f, 4.0f);
            if (_hitMediumClip == null) _hitMediumClip = GenerateImpactTone("SFX_HitMedium", 0.14f, 380f, 90f, 0.45f, 3.0f);
            if (_hitHeavyClip == null) _hitHeavyClip = GenerateImpactTone("SFX_HitHeavy", 0.28f, 220f, 45f, 0.50f, 2.2f);
            if (_parryClip == null) _parryClip = GenerateChime("SFX_Parry", 0.38f, 1280f, 2560f, 2.0f);
            if (_blockClip == null) _blockClip = GenerateImpactTone("SFX_Block", 0.10f, 160f, 60f, 0.20f, 4.5f);
            if (_rapidCancelClip == null) _rapidCancelClip = GenerateWhoosh("SFX_RapidCancel", 0.20f, 350f, 1800f);
            if (_swingLightClip == null) _swingLightClip = GenerateWhoosh("SFX_SwingLight", 0.12f, 250f, 800f);
            if (_swingHeavyClip == null) _swingHeavyClip = GenerateWhoosh("SFX_SwingHeavy", 0.22f, 180f, 500f);
            if (_projFireClip == null) _projFireClip = GenerateWhoosh("SFX_ProjFire", 0.18f, 400f, 1400f);
            if (_projImpactClip == null) _projImpactClip = GenerateImpactTone("SFX_ProjImpact", 0.24f, 320f, 60f, 0.45f, 3.2f);
            if (_landClip == null) _landClip = GenerateImpactTone("SFX_Land", 0.15f, 120f, 40f, 0.30f, 3.5f);
            if (_footstepClip == null) _footstepClip = GenerateImpactTone("SFX_Footstep", 0.06f, 300f, 80f, 0.25f, 5.0f);
            if (_domainActivationClip == null) _domainActivationClip = GenerateImpactTone("SFX_DomainActivation", 0.8f, 80f, 40f, 0.6f, 1.2f);
            if (_btnClickClip == null) _btnClickClip = GenerateImpactTone("SFX_BtnClick", 0.05f, 800f, 300f, 0.1f, 5.0f);
            if (_btnHoverClip == null) _btnHoverClip = GenerateChime("SFX_BtnHover", 0.06f, 900f, 1400f, 4.0f);

            // Procedural/no-op fallbacks for missing round/fight announcer clips so nothing null-refs
            if (_round1Clip == null) _round1Clip = GenerateChime("SFX_Round1_Fallback", 0.25f, 440f, 554f, 3f);
            if (_round2Clip == null) _round2Clip = GenerateChime("SFX_Round2_Fallback", 0.25f, 554f, 659f, 3f);
            if (_finalRoundClip == null) _finalRoundClip = GenerateChime("SFX_FinalRound_Fallback", 0.35f, 659f, 880f, 2.5f);
            if (_fightClip == null) _fightClip = GenerateImpactTone("SFX_Fight_Fallback", 0.30f, 400f, 150f, 0.4f, 3.5f);
            if (_victoryClip == null) _victoryClip = GenerateChime("SFX_Victory_Fallback", 0.5f, 523f, 1046f, 2f);

            // If music source still has no clip, generate ambient fight drone
            if (_musicSource != null && _musicSource.clip == null)
            {
                _musicSource.clip = _bgmFullTrack ?? GenerateAmbientDrone("Procedural_AmbientDrone", 4.0f, 65f);
            }
        }

        private void ApplyInitialVolumes()
        {
            float master = SettingsPanel.MasterVolume;
            float music = SettingsPanel.MusicVolume;
            float sfx = SettingsPanel.SfxVolume;

            AudioListener.volume = master;
            SetMusicVolume(music * master);
            SetSFXVolume(sfx * master);
        }

        #region Procedural Audio Synthesizers

        private AudioClip GenerateImpactTone(string name, float duration, float startFreq, float endFreq, float noiseAmount, float decayRate)
        {
            int sampleRate = 44100;
            int numSamples = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float env = Mathf.Exp(-decayRate * progress);

                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * noiseAmount;

                samples[i] = (tone * (1f - noiseAmount) + noise) * env;
            }

            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateChime(string name, float duration, float f1, float f2, float decayRate)
        {
            int sampleRate = 44100;
            int numSamples = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float env = Mathf.Exp(-decayRate * progress);

                float tone = 0.6f * Mathf.Sin(2f * Mathf.PI * f1 * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f2 * t);
                samples[i] = tone * env;
            }

            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateWhoosh(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int numSamples = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float env = Mathf.Sin(progress * Mathf.PI);

                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.65f;

                samples[i] = (tone + noise) * env;
            }

            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateAmbientDrone(string name, float duration, float baseFreq)
        {
            int sampleRate = 44100;
            int numSamples = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float tone1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t) * 0.5f;
                float tone3 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.0f) * t) * 0.25f;
                samples[i] = (tone1 + tone2 + tone3) * 0.3f;
            }

            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion

        public void StartConductor()
        {
            EnsureSources();
            _dspStartTime = (float)AudioSettings.dspTime;
            _isPlaying = true;
            if (_musicSource != null && _musicSource.clip != null && !_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }

        public void SetMusicVolume(float volume)
        {
            if (_musicSource != null)
            {
                _musicSource.volume = Mathf.Clamp01(volume);
            }
        }

        public void SetSFXVolume(float volume)
        {
            if (_sfxSource != null)
            {
                _sfxSource.volume = Mathf.Clamp01(volume);
            }
        }

        private void Update()
        {
            // Sync dynamic volume with SettingsPanel
            float master = SettingsPanel.MasterVolume;
            SetMusicVolume(SettingsPanel.MusicVolume * master);
            SetSFXVolume(SettingsPanel.SfxVolume * master);

            if (!_isPlaying) return;

            float songPosition = (float)(AudioSettings.dspTime - _dspStartTime);
            int beat = Mathf.FloorToInt(songPosition / SECONDS_PER_BEAT);

            if (beat > CurrentBeat)
            {
                CurrentBeat = beat;
                OnBeat?.Invoke(CurrentBeat);

                int bar = CurrentBeat / 4;
                if (bar > CurrentBar)
                {
                    CurrentBar = bar;
                    OnBar?.Invoke(CurrentBar);
                }
            }
        }

        [Header("Character Voice Acting & Grunts")]
        [SerializeField] private AudioClip _voiceGruntLight;
        [SerializeField] private AudioClip _voiceGruntHeavy;
        [SerializeField] private AudioClip _voiceDomainSuper;
        [SerializeField] private AudioClip _voiceKODefeat;

        public void PlayVoiceGruntLight() => PlayWithRandomPitch(_voiceGruntLight ?? _hitLightClip, 0.95f, 1.05f);
        public void PlayVoiceGruntHeavy() => PlayWithRandomPitch(_voiceGruntHeavy ?? _hitHeavyClip, 0.9f, 1.02f);
        public void PlayVoiceDomainSuper() => PlayOneShotDirect(_voiceDomainSuper ?? _domainActivationClip);
        public void PlayVoiceKODefeat() => PlayOneShotDirect(_voiceKODefeat ?? _hitHeavyClip);

        #region SFX Triggers with Dynamic Pitch Modulation

        public void PlayHitLight()
        {
            Debug.Log("[CombatAudioDirector] PlayHitLight SFX");
            PlayWithRandomPitch(_hitLightClip, 0.95f, 1.05f);
        }
        public void PlayHitMedium()
        {
            Debug.Log("[CombatAudioDirector] PlayHitMedium SFX");
            PlayWithRandomPitch(_hitMediumClip, 0.92f, 1.03f);
        }
        public void PlayHitHeavy()
        {
            Debug.Log("[CombatAudioDirector] PlayHitHeavy SFX");
            PlayWithRandomPitch(_hitHeavyClip, 0.88f, 1.0f);
        }
        public void PlayParry()
        {
            Debug.Log("[CombatAudioDirector] PlayParry SFX");
            PlayWithRandomPitch(_parryClip, 1.0f, 1.1f);
        }
        public void PlayBlock()
        {
            Debug.Log("[CombatAudioDirector] PlayBlock SFX");
            PlayWithRandomPitch(_blockClip, 0.9f, 1.0f);
        }
        public void PlayRapidCancel() => PlayWithRandomPitch(_rapidCancelClip, 1.05f, 1.15f);
        public void PlayBurst() => PlayWithRandomPitch(_domainActivationClip, 1.1f, 1.2f);
        public void PlayDomainActivation() => PlayWithRandomPitch(_domainActivationClip, 1.0f, 1.0f);
        public void PlayFootstep() => PlayWithRandomPitch(_footstepClip, 0.9f, 1.1f);
        public void PlaySwingLight() => PlayWithRandomPitch(_swingLightClip, 0.95f, 1.05f);
        public void PlaySwingHeavy() => PlayWithRandomPitch(_swingHeavyClip, 0.9f, 1.02f);
        public void PlayLand() => PlayWithRandomPitch(_landClip, 0.85f, 0.95f);

        public void PlayButtonClick() => PlayWithRandomPitch(_btnClickClip, 0.98f, 1.02f);
        public void PlayButtonHover() => PlayWithRandomPitch(_btnHoverClip, 0.98f, 1.02f);

        // Projectile Audio Hooks
        public void PlayProjectileFire() => PlayWithRandomPitch(_projFireClip, 0.95f, 1.05f);
        public void PlayProjectileImpact() => PlayWithRandomPitch(_projImpactClip, 0.95f, 1.05f);

        // Deadly Kombat Round Announcements
        public void PlayRound1() => PlayOneShotDirect(_round1Clip);
        public void PlayRound2() => PlayOneShotDirect(_round2Clip);
        public void PlayFinalRound() => PlayOneShotDirect(_finalRoundClip);
        public void PlayFight() => PlayOneShotDirect(_fightClip);
        public void PlayVictorySound() => PlayOneShotDirect(_victoryClip);

        // Menu and Battle Music Tracks
        public void PlayMenuMusic() => SwitchToMusicLoop(_bgmMenuTrack ?? _bgmFullTrack);
        public void PlayCombatMusic() => SwitchToMusicLoop(_bgmFullTrack);

        // Cyberpunk Dynamic Momentum Music Loops
        public void PlayIntroLoop() => SwitchToMusicLoop(_loopIntro ?? _bgmFullTrack);
        public void PlayDropLoop() => SwitchToMusicLoop(_loopDrop ?? _bgmFullTrack);
        public void PlayChorusLoop() => SwitchToMusicLoop(_loopChorus ?? _bgmFullTrack);
        public void PlayOutroLoop() => SwitchToMusicLoop(_loopOutro ?? _bgmFullTrack);
        public void PlayDefaultMatchMusic() => SwitchToMusicLoop(_bgmFullTrack ?? _loopBridge);

        public void SwitchToMusicLoop(AudioClip newLoop)
        {
            EnsureSources();
            if (newLoop == null || _musicSource == null) return;
            if (_musicSource.clip == newLoop && _musicSource.isPlaying) return;

            _musicSource.clip = newLoop;
            _musicSource.loop = true;
            _isPlaying = true;
            _musicSource.Play();
        }

        private void PlayOneShotDirect(AudioClip clip)
        {
            if (this == null || clip == null) return;
            EnsureSources();
            if (_sfxSource != null)
            {
                _sfxSource.pitch = 1.0f;
                _sfxSource.PlayOneShot(clip);
            }
        }

        private void PlayWithRandomPitch(AudioClip clip, float minPitch, float maxPitch)
        {
            if (this == null) return;
            if (clip == null) return;
            EnsureSources();
            if (_sfxSource != null)
            {
                _sfxSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                _sfxSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}
