using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using DemonIdol.Combat;
using DemonIdol.Input;
using DemonIdol.Launch;

namespace DemonIdol.UI
{
    [Serializable]
    public class SettingsData
    {
        // Audio
        public float masterVolume = 1.0f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1.0f;
        public float voiceVolume = 0.9f;

        // Display & Framerate
        public bool highFrameRate = true; // 120Hz preference vs 60Hz
        public int targetFramerate = 120; // 30, 60, 120
        public int qualityTier = 1; // 0 = Low, 1 = Balanced, 2 = High, 3 = Custom
        public float resolutionScale = 1.0f;
        public string languageCode = "en";
        public bool bloomEnabled = true;
        public bool shadowsEnabled = true;

        // Combat Feedback & Camera
        public float screenShakeIntensity = 1.0f;
        public bool showDamageNumbers = true;

        // Touch Virtual Controls
        public float touchControlsOpacity = 0.85f;
        public float touchControlsScale = 1.0f;
        public bool leftHandedMode = false;

        // Accessibility & Comfort
        public bool hapticsEnabled = true;
        public bool reducedMotion = false;
        public bool reducedFlashing = false;
        public bool captionsEnabled = true;

        // Fighting Game Specific Gameplay & Combat Options
        public int roundTimer = 99; // 30, 60, 99, 0 (Infinite)
        public int roundsToWin = 2; // 1, 2, 3 (Best of 1, 3, 5)
        public int aiDifficulty = 1; // 0 = Easy, 1 = Normal, 2 = Hard, 3 = Expert, 4 = Demon Lord
        public float damageScale = 1.0f; // 0.5x, 0.75x, 1.0x, 1.25x, 1.5x
        public int inputBufferFrames = 4; // 3F (Strict 3rd Strike), 4F (Standard), 5F (Lenient), 6F (Casual)
        public bool simpleSpecials = false;
        public bool autoTechThrows = false;
        public string hitSoundProfile = "3rdStrike"; // "3rdStrike", "AnimeImpact", "HeavyMetal"
        public int colorblindMode = 0; // 0 = None, 1 = Protanopia, 2 = Deuteranopia, 3 = Tritanopia
        public bool highContrastHitboxes = false;
        public bool reducedGore = false;
        public int touchControlsMode = 0; // 0 = Auto-Detect, 1 = Always On, 2 = Off
        public int controlScheme = 1; // 0 = Classic3Button, 1 = BlazBlue4Button (Default: BlazBlue4Button)
    }

    /// <summary>
    /// Master settings panel: controls audio volume, 120 FPS iPhone ProMotion,
    /// virtual touch controls tuning (opacity, scale, left-handed layout),
    /// combat visual feedback (screen shake, damage numbers, bloom/shadows),
    /// haptic feedback toggles, accessibility, and instant persistence.
    /// Features dynamic UI fallback generation for guaranteed scene-independence.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private const string PREFS_KEY = "mdi_settings";
        private const string PREFS_HAPTICS = "mdi_haptics";

        public static SettingsPanel Instance { get; private set; }

        public static float MasterVolume => Instance != null ? Instance._current.masterVolume : 1.0f;
        public static float MusicVolume => Instance != null ? Instance._current.musicVolume : 0.8f;
        public static float SfxVolume => Instance != null ? Instance._current.sfxVolume : 1.0f;
        public static float VoiceVolume => Instance != null ? Instance._current.voiceVolume : 0.9f;

        public static float ScreenShakeIntensity => Instance != null ? (Instance._current.reducedMotion ? 0f : Instance._current.screenShakeIntensity) : 1.0f;
        public static float TouchControlsOpacity => Instance != null ? Instance._current.touchControlsOpacity : 0.85f;
        public static float TouchControlsScale => Instance != null ? Instance._current.touchControlsScale : 1.0f;
        public static bool LeftHandedMode => Instance != null && Instance._current.leftHandedMode;

        public static bool ShowDamageNumbers => Instance == null || Instance._current.showDamageNumbers;
        public static bool BloomEnabled => Instance == null || Instance._current.bloomEnabled;
        public static bool ShadowsEnabled => Instance == null || Instance._current.shadowsEnabled;

        public static bool HapticsEnabled => Instance != null ? Instance._current.hapticsEnabled : PlayerPrefs.GetInt(PREFS_HAPTICS, 1) == 1;
        public static bool ReducedMotion => Instance != null && Instance._current.reducedMotion;
        public static bool ReducedFlashing => Instance != null && Instance._current.reducedFlashing;
        public static bool CaptionsEnabled => Instance != null ? Instance._current.captionsEnabled : true;
        public static int QualityTier => Instance != null ? Instance._current.qualityTier : 1;
        public static bool IsHighFrameRatePreferred => Instance == null || Instance._current.highFrameRate;

        // Fighting Game Static Accessors
        public static int RoundTimer => Instance != null ? Instance._current.roundTimer : 99;
        public static int RoundsToWin => Instance != null ? Instance._current.roundsToWin : 2;
        public static int AiDifficulty => Instance != null ? Instance._current.aiDifficulty : 1;
        public static float DamageScale => Instance != null ? Instance._current.damageScale : 1.0f;
        public static int InputBufferFrames => Instance != null ? Instance._current.inputBufferFrames : 4;
        public static bool SimpleSpecials => Instance != null && Instance._current.simpleSpecials;
        public static bool AutoTechThrows => Instance != null && Instance._current.autoTechThrows;
        public static string HitSoundProfile => Instance != null ? Instance._current.hitSoundProfile : "3rdStrike";
        public static int ColorblindMode => Instance != null ? Instance._current.colorblindMode : 0;
        public static bool HighContrastHitboxes => Instance != null && Instance._current.highContrastHitboxes;
        public static bool ReducedGore => Instance != null && Instance._current.reducedGore;
        public static int TouchControlsMode => Instance != null ? Instance._current.touchControlsMode : 0;
        public static DemonIdol.BlazBlue.ControlScheme ActiveControlScheme =>
            (Instance == null || Instance._current.controlScheme == 1)
                ? DemonIdol.BlazBlue.ControlScheme.BlazBlue4Button
                : DemonIdol.BlazBlue.ControlScheme.Classic3Button;

        [Header("Master Audio")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _voiceSlider;
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Controls & Combat Feel")]
        [SerializeField] private Slider _screenShakeSlider;
        [SerializeField] private Slider _touchOpacitySlider;
        [SerializeField] private Slider _touchScaleSlider;
        [SerializeField] private Toggle _leftHandedToggle;

        [Header("Display & Graphics")]
        [SerializeField] private Toggle _highFpsToggle;
        [SerializeField] private Toggle _damageNumbersToggle;
        [SerializeField] private Toggle _bloomToggle;
        [SerializeField] private Toggle _shadowsToggle;
        [SerializeField] private Dropdown _qualityDropdown;
        [SerializeField] private Text _qualityTierLabel;

        [Header("Accessibility & Comfort")]
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Toggle _reducedMotionToggle;
        [SerializeField] private Toggle _reducedFlashingToggle;
        [SerializeField] private Toggle _reducedGoreToggle;
        [SerializeField] private Toggle _captionsToggle;

        [Header("Actions")]
        [SerializeField] private Button _btnResetDefaults;
        [SerializeField] private Button _btnClose;

        [Header("Feedback Toast")]
        [SerializeField] private Text _toastText;
        [SerializeField] private CanvasGroup _toastGroup;

        private SettingsData _current = new SettingsData();
        private Coroutine _toastRoutine;
        private bool _isSyncingUI = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSettings();
            EnsureSettingsUI();
            BindUI();
        }

        private void LoadSettings()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _current = JsonUtility.FromJson<SettingsData>(json);
                }
                catch
                {
                    _current = new SettingsData();
                }
            }
            else
            {
                _current = new SettingsData();
            }

            if (_current == null)
                _current = new SettingsData();
            if (_current.resolutionScale < 0.50f)
                _current.resolutionScale = GraphicsProfiles.ScaleForTier(_current.qualityTier, 1f);
            if (string.IsNullOrEmpty(_current.languageCode))
                _current.languageCode = "en";

            ApplyFramerate();
            ApplyAudioSettings();
            ApplyQualityTier();
            ApplyResolutionScale();
        }

        public void ApplyResolutionScale()
        {
            float scale = GraphicsProfiles.ScaleForTier(_current.qualityTier, _current.resolutionScale);
            _current.resolutionScale = scale;
            ScalableBufferManager.ResizeBuffers(scale, scale);
        }

        public string LanguageCode => _current != null ? _current.languageCode : "en";

        public void SaveSettings()
        {
            string json = JsonUtility.ToJson(_current);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.SetInt(PREFS_HAPTICS, _current.hapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            _current = new SettingsData();
            SaveSettings();
            ApplyFramerate();
            ApplyAudioSettings();
            ApplyQualityTier();

            if (TouchVirtualControls.Instance != null)
            {
                TouchVirtualControls.Instance.ApplySettings(_current.touchControlsOpacity, _current.touchControlsScale, _current.leftHandedMode);
            }

            SyncUIFromData();
            ShowToast("SETTINGS RESET TO DEFAULTS");
        }

        public void ApplyFramerate()
        {
            Application.targetFrameRate = _current.highFrameRate ? 120 : 60;
        }

        public void ApplyAudioSettings()
        {
            AudioListener.volume = Mathf.Clamp01(_current.masterVolume);

            if (DemonIdol.Audio.CombatAudioDirector.Instance != null)
            {
                DemonIdol.Audio.CombatAudioDirector.Instance.SetMusicVolume(_current.musicVolume * _current.masterVolume);
                DemonIdol.Audio.CombatAudioDirector.Instance.SetSFXVolume(_current.sfxVolume * _current.masterVolume);
            }

            if (_audioMixer != null)
            {
                float masterDb = Mathf.Log10(Mathf.Max(0.0001f, _current.masterVolume)) * 20f;
                float musicDb = Mathf.Log10(Mathf.Max(0.0001f, _current.musicVolume * _current.masterVolume)) * 20f;
                float sfxDb = Mathf.Log10(Mathf.Max(0.0001f, _current.sfxVolume * _current.masterVolume)) * 20f;
                float voiceDb = Mathf.Log10(Mathf.Max(0.0001f, _current.voiceVolume * _current.masterVolume)) * 20f;

                _audioMixer.SetFloat("MasterVolume", masterDb);
                _audioMixer.SetFloat("MusicVolume", musicDb);
                _audioMixer.SetFloat("SFXVolume", sfxDb);
                _audioMixer.SetFloat("VoiceVolume", voiceDb);
            }
        }

        public void ApplyQualityTier()
        {
            string[] qualityNames = QualitySettings.names;
            int levelCount = qualityNames != null && qualityNames.Length > 0 ? qualityNames.Length : 1;
            int level = _current.qualityTier >= 3 ? levelCount - 1 : Mathf.Clamp(_current.qualityTier, 0, levelCount - 1);
            QualitySettings.SetQualityLevel(level, true);
            QualitySettings.shadows = _current.shadowsEnabled ? ShadowQuality.All : ShadowQuality.Disable;
            ApplyResolutionScale();
            UpdateQualityLabel();
        }

        private void UpdateQualityLabel()
        {
            if (_qualityTierLabel != null)
            {
                _qualityTierLabel.text = $"GRAPHICS: {GetQualityTierName(_current.qualityTier)}";
            }
        }

        private string GetQualityTierName(int tier)
        {
            switch (tier)
            {
                case 0: return "LOW";
                case 1: return "BALANCED";
                case 2: return "HIGH";
                default: return "CUSTOM";
            }
        }

        private void BindUI()
        {
            _isSyncingUI = true;

            // Audio Sliders
            if (_masterSlider != null)
            {
                _masterSlider.onValueChanged.RemoveAllListeners();
                _masterSlider.value = _current.masterVolume;
                _masterSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.masterVolume = val;
                    ApplyAudioSettings();
                    SaveSettings();
                    ShowToast($"MASTER VOLUME: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveAllListeners();
                _musicSlider.value = _current.musicVolume;
                _musicSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.musicVolume = val;
                    ApplyAudioSettings();
                    SaveSettings();
                    ShowToast($"MUSIC VOLUME: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveAllListeners();
                _sfxSlider.value = _current.sfxVolume;
                _sfxSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.sfxVolume = val;
                    ApplyAudioSettings();
                    SaveSettings();
                    ShowToast($"SFX VOLUME: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            if (_voiceSlider != null)
            {
                _voiceSlider.onValueChanged.RemoveAllListeners();
                _voiceSlider.value = _current.voiceVolume;
                _voiceSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.voiceVolume = val;
                    ApplyAudioSettings();
                    SaveSettings();
                    ShowToast($"VOICE VOLUME: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            // Controls & Combat Feel
            if (_screenShakeSlider != null)
            {
                _screenShakeSlider.onValueChanged.RemoveAllListeners();
                _screenShakeSlider.value = _current.screenShakeIntensity;
                _screenShakeSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.screenShakeIntensity = val;
                    SaveSettings();
                    ShowToast($"SCREEN SHAKE: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            if (_touchOpacitySlider != null)
            {
                _touchOpacitySlider.onValueChanged.RemoveAllListeners();
                _touchOpacitySlider.value = _current.touchControlsOpacity;
                _touchOpacitySlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.touchControlsOpacity = val;
                    TouchVirtualControls.Instance?.ApplySettings(_current.touchControlsOpacity, _current.touchControlsScale, _current.leftHandedMode);
                    SaveSettings();
                    ShowToast($"TOUCH OPACITY: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            if (_touchScaleSlider != null)
            {
                _touchScaleSlider.onValueChanged.RemoveAllListeners();
                _touchScaleSlider.value = _current.touchControlsScale;
                _touchScaleSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.touchControlsScale = val;
                    TouchVirtualControls.Instance?.ApplySettings(_current.touchControlsOpacity, _current.touchControlsScale, _current.leftHandedMode);
                    SaveSettings();
                    ShowToast($"TOUCH SCALE: {Mathf.RoundToInt(val * 100f)}%");
                });
            }

            if (_leftHandedToggle != null)
            {
                _leftHandedToggle.onValueChanged.RemoveAllListeners();
                _leftHandedToggle.isOn = _current.leftHandedMode;
                _leftHandedToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.leftHandedMode = val;
                    TouchVirtualControls.Instance?.ApplySettings(_current.touchControlsOpacity, _current.touchControlsScale, _current.leftHandedMode);
                    SaveSettings();
                    ShowToast(val ? "LEFT-HANDED CONTROLS ENABLED" : "STANDARD CONTROLS ENABLED");
                });
            }

            // Display & Graphics
            if (_highFpsToggle != null)
            {
                _highFpsToggle.onValueChanged.RemoveAllListeners();
                _highFpsToggle.isOn = _current.highFrameRate;
                _highFpsToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.highFrameRate = val;
                    _current.targetFramerate = val ? 120 : 60;
                    ApplyFramerate();
                    SaveSettings();
                    ShowToast(val ? "120 FPS PROMOTION ENABLED" : "60 FPS ECO MODE ENABLED");
                });
            }

            if (_damageNumbersToggle != null)
            {
                _damageNumbersToggle.onValueChanged.RemoveAllListeners();
                _damageNumbersToggle.isOn = _current.showDamageNumbers;
                _damageNumbersToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.showDamageNumbers = val;
                    SaveSettings();
                    ShowToast(val ? "DAMAGE NUMBERS: ON" : "DAMAGE NUMBERS: OFF");
                });
            }

            if (_bloomToggle != null)
            {
                _bloomToggle.onValueChanged.RemoveAllListeners();
                _bloomToggle.isOn = _current.bloomEnabled;
                _bloomToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.bloomEnabled = val;
                    SaveSettings();
                    ShowToast(val ? "BLOOM: ENABLED" : "BLOOM: DISABLED");
                });
            }

            if (_shadowsToggle != null)
            {
                _shadowsToggle.onValueChanged.RemoveAllListeners();
                _shadowsToggle.isOn = _current.shadowsEnabled;
                _shadowsToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.shadowsEnabled = val;
                    ApplyQualityTier();
                    SaveSettings();
                    ShowToast(val ? "DYNAMIC SHADOWS: ON" : "DYNAMIC SHADOWS: OFF");
                });
            }

            if (_qualityDropdown != null)
            {
                _qualityDropdown.onValueChanged.RemoveAllListeners();
                _qualityDropdown.value = _current.qualityTier;
                _qualityDropdown.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.qualityTier = val;
                    ApplyQualityTier();
                    SaveSettings();
                    ShowToast($"GRAPHICS: {GetQualityTierName(val)}");
                });
            }

            // Accessibility
            if (_hapticsToggle != null)
            {
                _hapticsToggle.onValueChanged.RemoveAllListeners();
                _hapticsToggle.isOn = _current.hapticsEnabled;
                _hapticsToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.hapticsEnabled = val;
                    SaveSettings();
                    ShowToast(val ? "HAPTIC FEEDBACK: ON" : "HAPTIC FEEDBACK: OFF");
                });
            }

            if (_reducedMotionToggle != null)
            {
                _reducedMotionToggle.onValueChanged.RemoveAllListeners();
                _reducedMotionToggle.isOn = _current.reducedMotion;
                _reducedMotionToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.reducedMotion = val;
                    SaveSettings();
                    ShowToast(val ? "REDUCED MOTION: ENABLED" : "REDUCED MOTION: DISABLED");
                });
            }

            if (_reducedFlashingToggle != null)
            {
                _reducedFlashingToggle.onValueChanged.RemoveAllListeners();
                _reducedFlashingToggle.isOn = _current.reducedFlashing;
                _reducedFlashingToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.reducedFlashing = val;
                    SaveSettings();
                    ShowToast(val ? "REDUCED FLASHING: ENABLED" : "REDUCED FLASHING: DISABLED");
                });
            }

            if (_reducedGoreToggle != null)
            {
                _reducedGoreToggle.onValueChanged.RemoveAllListeners();
                _reducedGoreToggle.isOn = _current.reducedGore;
                _reducedGoreToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.reducedGore = val;
                    SaveSettings();
                    ShowToast(val ? "REDUCED GORE: ON" : "REDUCED GORE: OFF");
                });
            }

            if (_captionsToggle != null)
            {
                _captionsToggle.onValueChanged.RemoveAllListeners();
                _captionsToggle.isOn = _current.captionsEnabled;
                _captionsToggle.onValueChanged.AddListener(val =>
                {
                    if (_isSyncingUI) return;
                    _current.captionsEnabled = val;
                    SaveSettings();
                    ShowToast(val ? "SUBTITLES: ON" : "SUBTITLES: OFF");
                });
            }

            if (_btnResetDefaults != null)
            {
                _btnResetDefaults.onClick.RemoveAllListeners();
                _btnResetDefaults.onClick.AddListener(ResetToDefaults);
            }

            if (_btnClose != null)
            {
                _btnClose.onClick.RemoveAllListeners();
                _btnClose.onClick.AddListener(OnClickBack);
            }

            UpdateQualityLabel();
            _isSyncingUI = false;
        }

        public void SyncUIFromData()
        {
            _isSyncingUI = true;

            if (_masterSlider != null) _masterSlider.value = _current.masterVolume;
            if (_musicSlider != null) _musicSlider.value = _current.musicVolume;
            if (_sfxSlider != null) _sfxSlider.value = _current.sfxVolume;
            if (_voiceSlider != null) _voiceSlider.value = _current.voiceVolume;

            if (_screenShakeSlider != null) _screenShakeSlider.value = _current.screenShakeIntensity;
            if (_touchOpacitySlider != null) _touchOpacitySlider.value = _current.touchControlsOpacity;
            if (_touchScaleSlider != null) _touchScaleSlider.value = _current.touchControlsScale;
            if (_leftHandedToggle != null) _leftHandedToggle.isOn = _current.leftHandedMode;

            if (_highFpsToggle != null) _highFpsToggle.isOn = _current.highFrameRate;
            if (_damageNumbersToggle != null) _damageNumbersToggle.isOn = _current.showDamageNumbers;
            if (_bloomToggle != null) _bloomToggle.isOn = _current.bloomEnabled;
            if (_shadowsToggle != null) _shadowsToggle.isOn = _current.shadowsEnabled;
            if (_qualityDropdown != null) _qualityDropdown.value = _current.qualityTier;

            if (_hapticsToggle != null) _hapticsToggle.isOn = _current.hapticsEnabled;
            if (_reducedMotionToggle != null) _reducedMotionToggle.isOn = _current.reducedMotion;
            if (_reducedFlashingToggle != null) _reducedFlashingToggle.isOn = _current.reducedFlashing;
            if (_captionsToggle != null) _captionsToggle.isOn = _current.captionsEnabled;

            UpdateQualityLabel();
            _isSyncingUI = false;
        }

        #region Dynamic UI Generation Fallback

        public void EnsureSettingsUI()
        {
            // If already wired, return
            if (_masterSlider != null && _screenShakeSlider != null && _btnResetDefaults != null) return;

            // 1. Search existing children for sliders, toggles, buttons by name
            FindExistingElements();

            // 2. If essential sliders are still missing, dynamically construct settings layout
            if (_musicSlider == null || _masterSlider == null)
            {
                ConstructDynamicSettingsUI();
            }
        }

        private void FindExistingElements()
        {
            if (_masterSlider == null) _masterSlider = FindComponentRecursive<Slider>("Slider_Master");
            if (_musicSlider == null) _musicSlider = FindComponentRecursive<Slider>("Slider_Music");
            if (_sfxSlider == null) _sfxSlider = FindComponentRecursive<Slider>("Slider_SFX");
            if (_voiceSlider == null) _voiceSlider = FindComponentRecursive<Slider>("Slider_Voice");

            if (_screenShakeSlider == null) _screenShakeSlider = FindComponentRecursive<Slider>("Slider_ScreenShake");
            if (_touchOpacitySlider == null) _touchOpacitySlider = FindComponentRecursive<Slider>("Slider_TouchOpacity");
            if (_touchScaleSlider == null) _touchScaleSlider = FindComponentRecursive<Slider>("Slider_TouchScale");
            if (_leftHandedToggle == null) _leftHandedToggle = FindComponentRecursive<Toggle>("Toggle_LeftHanded");

            if (_highFpsToggle == null) _highFpsToggle = FindComponentRecursive<Toggle>("Toggle_HighFPS", "Toggle_120FPS");
            if (_damageNumbersToggle == null) _damageNumbersToggle = FindComponentRecursive<Toggle>("Toggle_DamageNumbers");
            if (_bloomToggle == null) _bloomToggle = FindComponentRecursive<Toggle>("Toggle_Bloom");
            if (_shadowsToggle == null) _shadowsToggle = FindComponentRecursive<Toggle>("Toggle_Shadows");
            if (_qualityDropdown == null) _qualityDropdown = FindComponentRecursive<Dropdown>("Dropdown_Quality");
            if (_qualityTierLabel == null) _qualityTierLabel = FindComponentRecursive<Text>("Label_QualityTier", "Text_Quality");

            if (_hapticsToggle == null) _hapticsToggle = FindComponentRecursive<Toggle>("Toggle_Haptics");
            if (_reducedMotionToggle == null) _reducedMotionToggle = FindComponentRecursive<Toggle>("Toggle_ReducedMotion");
            if (_reducedFlashingToggle == null) _reducedFlashingToggle = FindComponentRecursive<Toggle>("Toggle_ReducedFlashing");
            if (_captionsToggle == null) _captionsToggle = FindComponentRecursive<Toggle>("Toggle_Captions", "Toggle_Subtitles");

            if (_btnResetDefaults == null) _btnResetDefaults = FindComponentRecursive<Button>("Btn_ResetDefaults", "Button_Reset");
            if (_btnClose == null) _btnClose = FindComponentRecursive<Button>("Btn_Close", "Btn_Back", "Button_Close");

            if (_toastText == null) _toastText = FindComponentRecursive<Text>("Text_Toast");
            if (_toastGroup == null) _toastGroup = FindComponentRecursive<CanvasGroup>("Group_Toast", "ToastGroup");
        }

        private T FindComponentRecursive<T>(params string[] candidateNames) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            foreach (var c in components)
            {
                foreach (var name in candidateNames)
                {
                    if (string.Equals(c.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
            }
            return null;
        }

        private void ConstructDynamicSettingsUI()
        {
            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Look for or create SettingsBox container
            Transform boxTrans = transform.Find("SettingsBox");
            GameObject boxObj;
            if (boxTrans != null)
            {
                boxObj = boxTrans.gameObject;
            }
            else
            {
                boxObj = new GameObject("SettingsBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                boxObj.transform.SetParent(transform, false);
                RectTransform brt = boxObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0.5f);
                brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(960f, 620f);
                brt.anchoredPosition = Vector2.zero;

                Image boxImg = boxObj.GetComponent<Image>();
                if (atlas != null) atlas.StylePanel(boxImg, false);
                else boxImg.color = new Color(0.06f, 0.06f, 0.09f, 0.96f);
            }

            // Header Title
            CreateLabel(boxObj.transform, "Title", "SYSTEM SETTINGS & COMFORT", new Vector2(0f, 270f), new Vector2(800f, 40f), 26, FontStyle.Bold, SumiStyleTokens.PaperWhite, font);
            CreateLabel(boxObj.transform, "Subtitle", "COMBAT TUNING // ACCESSIBILITY // AUDIO & VIDEO", new Vector2(0f, 238f), new Vector2(800f, 24f), 13, FontStyle.Normal, SumiStyleTokens.ParryGold, font);

            // Left Column: Audio & Controls (-230 X)
            float colLeftX = -230f;
            float startY = 190f;
            float stepY = 46f;

            CreateLabel(boxObj.transform, "H_Audio", "-- AUDIO & CONTROLS --", new Vector2(colLeftX, startY), new Vector2(400f, 22f), 14, FontStyle.Bold, SumiStyleTokens.Vermilion, font);
            _masterSlider = CreateSliderControl(boxObj.transform, "Slider_Master", "Master Volume", new Vector2(colLeftX, startY - stepY * 1f), font);
            _musicSlider = CreateSliderControl(boxObj.transform, "Slider_Music", "Music Volume", new Vector2(colLeftX, startY - stepY * 2f), font);
            _sfxSlider = CreateSliderControl(boxObj.transform, "Slider_SFX", "SFX Volume", new Vector2(colLeftX, startY - stepY * 3f), font);
            _screenShakeSlider = CreateSliderControl(boxObj.transform, "Slider_ScreenShake", "Screen Shake", new Vector2(colLeftX, startY - stepY * 4f), font);
            _touchOpacitySlider = CreateSliderControl(boxObj.transform, "Slider_TouchOpacity", "Touch Opacity", new Vector2(colLeftX, startY - stepY * 5f), font, 0.2f, 1.0f);
            _touchScaleSlider = CreateSliderControl(boxObj.transform, "Slider_TouchScale", "Touch Scale", new Vector2(colLeftX, startY - stepY * 6f), font, 0.7f, 1.4f);
            _leftHandedToggle = CreateToggleControl(boxObj.transform, "Toggle_LeftHanded", "Left-Handed Touch Layout", new Vector2(colLeftX, startY - stepY * 7f), font);

            // Right Column: Display & Accessibility (+230 X)
            float colRightX = 230f;
            CreateLabel(boxObj.transform, "H_Display", "-- DISPLAY & ACCESSIBILITY --", new Vector2(colRightX, startY), new Vector2(400f, 22f), 14, FontStyle.Bold, SumiStyleTokens.Vermilion, font);
            _highFpsToggle = CreateToggleControl(boxObj.transform, "Toggle_HighFPS", "120 FPS ProMotion Target", new Vector2(colRightX, startY - stepY * 1f), font);
            _damageNumbersToggle = CreateToggleControl(boxObj.transform, "Toggle_DamageNumbers", "Show Damage Floating Numbers", new Vector2(colRightX, startY - stepY * 2f), font);
            _bloomToggle = CreateToggleControl(boxObj.transform, "Toggle_Bloom", "Bloom Post-Processing", new Vector2(colRightX, startY - stepY * 3f), font);
            _shadowsToggle = CreateToggleControl(boxObj.transform, "Toggle_Shadows", "Dynamic Character Shadows", new Vector2(colRightX, startY - stepY * 4f), font);
            _hapticsToggle = CreateToggleControl(boxObj.transform, "Toggle_Haptics", "Haptic Impact Feedback", new Vector2(colRightX, startY - stepY * 5f), font);
            _reducedMotionToggle = CreateToggleControl(boxObj.transform, "Toggle_ReducedMotion", "Reduced Motion (Safe Camera)", new Vector2(colRightX, startY - stepY * 6f), font);
            _reducedFlashingToggle = CreateToggleControl(boxObj.transform, "Toggle_ReducedFlashing", "Reduced Flashing (Photosafety)", new Vector2(colRightX, startY - stepY * 7f), font);
            _reducedGoreToggle = CreateToggleControl(boxObj.transform, "Toggle_ReducedGore", "Reduced Gore", new Vector2(colRightX, startY - stepY * 9f), font);
            _captionsToggle = CreateToggleControl(boxObj.transform, "Toggle_Captions", "Story Captions & Subtitles", new Vector2(colRightX, startY - stepY * 8f), font);

            // Legal Compliance & App Store Policy Buttons
            Button btnTerms = CreateSimpleButton(boxObj.transform, "Btn_Terms", "TERMS & PRIVACY", new Vector2(-260f, -215f), new Vector2(190f, 36f), new Color(0.18f, 0.22f, 0.30f, 1f), font);
            btnTerms.onClick.AddListener(OnClickTermsAndPrivacy);

            Button btnCredits = CreateSimpleButton(boxObj.transform, "Btn_Credits", "CREDITS", new Vector2(-40f, -215f), new Vector2(140f, 36f), new Color(0.18f, 0.22f, 0.30f, 1f), font);
            btnCredits.onClick.AddListener(OnClickCredits);

            Button btnDeleteAcc = CreateSimpleButton(boxObj.transform, "Btn_DeleteAcc", "DATA DELETION", new Vector2(190f, -215f), new Vector2(180f, 36f), new Color(0.40f, 0.12f, 0.12f, 1f), font);
            btnDeleteAcc.onClick.AddListener(OnClickAccountDeletion);

            // Footer Action Buttons
            _btnResetDefaults = CreateSimpleButton(boxObj.transform, "Btn_ResetDefaults", "RESET TO DEFAULTS", new Vector2(-150f, -260f), new Vector2(240f, 44f), SumiStyleTokens.CarbonInkWash, font);
            _btnClose = CreateSimpleButton(boxObj.transform, "Btn_Close", "SAVE & CLOSE", new Vector2(150f, -260f), new Vector2(240f, 44f), SumiStyleTokens.ToriiRed, font);

            // Toast overlay
            if (_toastGroup == null)
            {
                GameObject toastObj = new GameObject("ToastGroup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                toastObj.transform.SetParent(boxObj.transform, false);
                RectTransform trt = toastObj.GetComponent<RectTransform>();
                trt.anchoredPosition = new Vector2(0f, -200f);
                trt.sizeDelta = new Vector2(480f, 36f);
                toastObj.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
                _toastGroup = toastObj.GetComponent<CanvasGroup>();
                _toastGroup.alpha = 0f;
                toastObj.SetActive(false);

                _toastText = CreateLabel(toastObj.transform, "Text_Toast", "", Vector2.zero, new Vector2(460f, 30f), 14, FontStyle.Bold, SumiStyleTokens.ParryGold, font);
            }
        }

        private Text CreateLabel(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, Font font)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text txt = obj.GetComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            return txt;
        }

        private Slider CreateSliderControl(Transform parent, string name, string labelText, Vector2 pos, Font font, float min = 0f, float max = 1f)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchoredPosition = pos;
            rowRt.sizeDelta = new Vector2(400f, 36f);

            // Label
            CreateLabel(row.transform, "Label", labelText, new Vector2(-110f, 0f), new Vector2(170f, 30f), 14, FontStyle.Normal, Color.white, font);

            // Slider Background
            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform srt = sliderObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(80f, 0f);
            srt.sizeDelta = new Vector2(200f, 16f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgrt = bg.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.sizeDelta = Vector2.zero;
            Image bgImg = bg.GetComponent<Image>();
            if (atlas != null && atlas.SliderBg != null)
            {
                bgImg.sprite = atlas.SliderBg;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            }

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fart = fillArea.GetComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform frt = fill.GetComponent<RectTransform>();
            frt.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            if (atlas != null && atlas.SliderFill != null)
            {
                fillImg.sprite = atlas.SliderFill;
                fillImg.type = Image.Type.Sliced;
                fillImg.color = Color.white;
            }
            else
            {
                fillImg.color = SumiStyleTokens.ToriiRed;
            }

            slider.fillRect = frt;
            slider.targetGraphic = fillImg;

            return slider;
        }

        private Toggle CreateToggleControl(Transform parent, string name, string labelText, Vector2 pos, Font font)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchoredPosition = pos;
            rowRt.sizeDelta = new Vector2(400f, 36f);

            Toggle toggle = row.GetComponent<Toggle>();

            // Checkbox Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(row.transform, false);
            RectTransform bgrt = bg.GetComponent<RectTransform>();
            bgrt.anchoredPosition = new Vector2(-160f, 0f);
            bgrt.sizeDelta = new Vector2(24f, 24f);
            Image bgImg = bg.GetComponent<Image>();
            if (atlas != null && atlas.InputFrameGray != null)
            {
                bgImg.sprite = atlas.InputFrameGray;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.22f, 0.22f, 0.28f, 1f);
            }

            // Checkmark
            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            RectTransform crt = check.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.15f, 0.15f);
            crt.anchorMax = new Vector2(0.85f, 0.85f);
            crt.sizeDelta = Vector2.zero;
            Image checkImg = check.GetComponent<Image>();
            if (atlas != null && atlas.CheckRect1 != null)
            {
                checkImg.sprite = atlas.CheckRect1;
                checkImg.color = Color.white;
            }
            else
            {
                checkImg.color = SumiStyleTokens.ParryGold;
            }

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;

            // Label Text
            CreateLabel(row.transform, "Label", labelText, new Vector2(30f, 0f), new Vector2(320f, 30f), 14, FontStyle.Normal, Color.white, font);

            return toggle;
        }

        private Button CreateSimpleButton(Transform parent, string name, string labelText, Vector2 pos, Vector2 size, Color color, Font font)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;

            bool isRed = name.Contains("Close") || name.Contains("Delete") || color == SumiStyleTokens.ToriiRed;
            if (atlas != null) atlas.StyleButton(btn, 2, isRed);
            else img.color = color;

            CreateLabel(btnObj.transform, "Text", labelText, Vector2.zero, size, 16, FontStyle.Bold, Color.white, font);

            return btn;
        }

        #endregion

        public void ShowToast(string message)
        {
            if (_toastText == null || _toastGroup == null) return;
            _toastText.text = message;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            _toastGroup.alpha = 1f;
            _toastGroup.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.8f);

            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                _toastGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
                yield return null;
            }
            _toastGroup.alpha = 0f;
            _toastGroup.gameObject.SetActive(false);
        }

        public void OnClickTermsAndPrivacy()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (LegalComplianceUI.Instance != null)
            {
                LegalComplianceUI.Instance.Open(LegalTab.TermsOfService);
            }
        }

        public void OnClickCredits()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (CreditsUI.Instance != null)
            {
                CreditsUI.Instance.Open();
            }
        }

        public void OnClickAccountDeletion()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (LegalComplianceUI.Instance != null)
            {
                LegalComplianceUI.Instance.Open(LegalTab.DataDeletion);
            }
        }

        public void Open()
        {
            EnsureSettingsUI();
            gameObject.SetActive(true);
            SyncUIFromData();
        }

        public void Close()
        {
            OnClickBack();
        }

        public void OnClickBack()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.OnClickCloseSettingsModal();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
