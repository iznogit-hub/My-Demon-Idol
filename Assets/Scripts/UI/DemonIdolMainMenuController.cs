using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DemonIdol.Launch;
using DemonIdol.Audio;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master UI Controller for Assets/Scenes/MainMenu.unity.
    /// Handles Title branding, mode selection (Arcade, Local Versus, Training),
    /// Controls modal, Settings modal, and Mac Quit button.
    /// Manages keyboard, gamepad, and touch navigation.
    /// Spawns the single DontDestroyOnLoad CombatAudioDirector.
    /// </summary>
    public class DemonIdolMainMenuController : MonoBehaviour
    {
        public static DemonIdolMainMenuController Instance { get; private set; }

        [Header("Mode Buttons")]
        public Button btnArcade;
        public Button btnVersus;
        public Button btnTraining;
        public Button btnControls;
        public Button btnSettings;
        public Button btnQuit;

        [Header("Modals")]
        public GameObject controlsModal;
        public GameObject settingsModal;
        public Button btnCloseControls;
        public Button btnCloseSettings;

        [Header("Audio Settings")]
        public Slider sliderBGM;
        public Slider sliderSFX;

        private void Awake()
        {
            Instance = this;
            EnsureAudioDirector();
        }

        private void Start()
        {
            SetupButtonListeners();
            SetupPlatformQuit();

            // Set default button selection for keyboard/gamepad navigation
            if (btnArcade != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(btnArcade.gameObject);
            }
        }

        private void EnsureAudioDirector()
        {
            if (CombatAudioDirector.Instance == null)
            {
                var existing = FindAnyObjectByType<CombatAudioDirector>();
                if (existing == null)
                {
                    GameObject audioObj = new GameObject("CombatAudio_Manager");
                    audioObj.AddComponent<CombatAudioDirector>();
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (btnArcade != null) btnArcade.onClick.AddListener(OnArcadeClicked);
            if (btnVersus != null) btnVersus.onClick.AddListener(OnVersusClicked);
            if (btnTraining != null) btnTraining.onClick.AddListener(OnTrainingClicked);
            if (btnControls != null) btnControls.onClick.AddListener(OnControlsClicked);
            if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);

            if (btnCloseControls != null) btnCloseControls.onClick.AddListener(() =>
            {
                if (controlsModal != null) controlsModal.SetActive(false);
                if (btnControls != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btnControls.gameObject);
            });

            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(() =>
            {
                if (settingsModal != null) settingsModal.SetActive(false);
                if (btnSettings != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btnSettings.gameObject);
            });

            if (sliderBGM != null)
            {
                sliderBGM.value = PlayerPrefs.GetFloat("MDI_BGM_Volume", 0.8f);
                sliderBGM.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MDI_BGM_Volume", val);
                    if (CombatAudioDirector.Instance != null)
                        CombatAudioDirector.Instance.SetMusicVolume(val);
                });
            }

            if (sliderSFX != null)
            {
                sliderSFX.value = PlayerPrefs.GetFloat("MDI_SFX_Volume", 1.0f);
                sliderSFX.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MDI_SFX_Volume", val);
                    if (CombatAudioDirector.Instance != null)
                        CombatAudioDirector.Instance.SetSFXVolume(val);
                });
            }
        }

        private void SetupPlatformQuit()
        {
            if (btnQuit != null)
            {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR
                btnQuit.gameObject.SetActive(true);
#else
                btnQuit.gameObject.SetActive(false);
#endif
            }
        }

        public void OnArcadeClicked()
        {
            LaunchServices.Session.TrySelectMode("Arcade", out _);
            PlayerPrefs.SetString("MDI_GameMode", "Arcade");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnVersusClicked()
        {
            LaunchServices.Session.TrySelectMode("LocalVersus", out _);
            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnTrainingClicked()
        {
            LaunchServices.Session.TrySelectMode("Training", out _);
            PlayerPrefs.SetString("MDI_GameMode", "Training");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnControlsClicked()
        {
            if (controlsModal != null)
            {
                controlsModal.SetActive(true);
                if (btnCloseControls != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btnCloseControls.gameObject);
            }
        }

        public void OnSettingsClicked()
        {
            if (settingsModal != null)
            {
                settingsModal.SetActive(true);
                if (btnCloseSettings != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btnCloseSettings.gameObject);
            }
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
