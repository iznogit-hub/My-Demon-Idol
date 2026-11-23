using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DemonIdol.Launch;
using DemonIdol.Audio;
using DemonIdol.Combat;

namespace DemonIdol.UI
{
    public enum MainMenuPillar
    {
        Fight = 0,
        Dojo = 1,
        Roster = 2,
        Settings = 3
    }

    /// <summary>
    /// Master UI Controller for Assets/Scenes/MainMenu.unity (Modern AAA Overhaul).
    /// Features:
    /// - Live 3D Hero Stage Backdrop (idling fighter with spatial camera framing per tab).
    /// - Top Pillar Navigation Bar (FIGHT / DOJO / ROSTER / SETTINGS) with LB/RB and Q/E tab switching.
    /// - Compact Player Profile & Reward Pill reading live stats from LaunchServices.Rewards.
    /// - Asymmetric Diagonal Mode Cards with Sumi-e design tokens (CarbonInk, Vermilion, ParryGold, PaperWhite).
    /// - Quick Fight 1-press instant battle shortcut.
    /// - Slide-Over Tactical Settings Drawer (Audio, Graphics, Accessibility, Controls) with EaseOutCubic and focus memory.
    /// - Dynamic Bottom Control-Hint Footer with auto-detecting Keyboard/Gamepad glyphs.
    /// - 100% Controller + Keyboard + Mouse/Touch Parity with zero-GC Update loops.
    /// </summary>
    public class DemonIdolMainMenuController : MonoBehaviour
    {
        public static DemonIdolMainMenuController Instance { get; private set; }

        [Header("Legacy Compatible Mode Buttons")]
        public Button btnArcade;
        public Button btnVersus;
        public Button btnTraining;
        public Button btnControls;
        public Button btnSettings;
        public Button btnQuit;

        [Header("Modern Top Navigation Pillars")]
        public Button tabFight;
        public Button tabDojo;
        public Button tabRoster;
        public Button tabSettings;
        public RectTransform tabIndicator;
        public Text profileRewardText;

        [Header("Tab Panels")]
        public GameObject panelFight;
        public GameObject panelDojo;
        public GameObject panelRoster;

        [Header("Modern Mode Cards")]
        public Button btnQuickFight;
        public Button btnOnlinePvP;
        public Button btnMoveReference;
        public Button btnTutorial;
        public Button btnRosterShowcase;

        [Header("Slide-Over Settings Drawer (Phase 3)")]
        public GameObject settingsModal;
        public RectTransform settingsDrawerRoot;
        public CanvasGroup drawerCanvasGroup;
        public GameObject drawerScrim;
        public Button btnCloseSettings;
        public Button tabDrawerAudio;
        public Button tabDrawerGraphics;
        public Button tabDrawerAccessibility;
        public Button tabDrawerControls;
        public GameObject drawerPanelAudio;
        public GameObject drawerPanelGraphics;
        public GameObject drawerPanelAccessibility;
        public GameObject drawerPanelControls;

        [Header("Audio Settings")]
        public Slider sliderBGM;
        public Slider sliderSFX;
        public Slider sliderVoice;
        public Button btnTestSound;

        [Header("Graphics Settings")]
        public Button btnQualityPrev;
        public Button btnQualityNext;
        public Text txtQualityName;
        public Button btnToggleFullscreen;
        public Text txtFullscreenState;
        public Button btnToggleVSync;
        public Text txtVSyncState;
        public Text txtFpsLock;

        [Header("Accessibility Settings")]
        public Button btnToggleReducedMotion;
        public Text txtReducedMotionState;
        public Button btnToggleReducedGore;
        public Text txtReducedGoreState;

        [Header("Controls Modal / Drawer")]
        public GameObject controlsModal;
        public Button btnCloseControls;

        [Header("Footer & Dynamic Prompts")]
        public Text footerControlPromptText;

        [Header("Live 3D Hero Setup")]
        public Transform heroSpawnPoint;
        public Camera menuCamera;

        // Runtime State
        public MainMenuPillar CurrentPillar { get; private set; } = MainMenuPillar.Fight;
        public bool IsDrawerOpen { get; private set; } = false;

        private GameObject _activeHeroModel;
        private string _activeHeroId = "";
        private GameObject _lastSelectedBeforeDrawer;
        private Coroutine _drawerRoutine;
        private bool _isUsingGamepad = false;

        // Camera Framing per Tab
        private Vector3 _targetCamPos = new Vector3(0.0f, 1.25f, -4.5f);
        private Quaternion _targetCamRot = Quaternion.Euler(4f, -4f, 0f);
        private static readonly Vector3[] CamPositions = new Vector3[]
        {
            new Vector3(0.0f, 1.25f, -4.5f),   // FIGHT
            new Vector3(0.35f, 1.30f, -4.2f),  // DOJO
            new Vector3(-0.40f, 1.20f, -4.8f), // ROSTER
            new Vector3(0.60f, 1.25f, -5.0f)   // SETTINGS
        };
        private static readonly Quaternion[] CamRotations = new Quaternion[]
        {
            Quaternion.Euler(4f, -4f, 0f),     // FIGHT
            Quaternion.Euler(6f, -10f, 0f),    // DOJO
            Quaternion.Euler(3f, 6f, 0f),      // ROSTER
            Quaternion.Euler(2f, -14f, 0f)     // SETTINGS
        };

        private void Awake()
        {
            Instance = this;
            EnsureAudioDirector();
            EnsureModernHierarchy();
        }

        private void Start()
        {
            SetupButtonListeners();
            SetupPlatformQuit();
            SetupSettingsSliders();
            SetupGraphicsAndAccessibilityUI();
            RefreshRewardPill();
            SpawnHeroFighter();
            SelectPillar(MainMenuPillar.Fight, immediate: true);
            UpdateControlPromptFooter(force: true);
        }

        private void Update()
        {
            // 1. Camera Spatial Damped Lerp
            if (menuCamera != null)
            {
                float dt = Time.unscaledDeltaTime;
                menuCamera.transform.position = Vector3.Lerp(menuCamera.transform.position, _targetCamPos, dt * 6.5f);
                menuCamera.transform.rotation = Quaternion.Slerp(menuCamera.transform.rotation, _targetCamRot, dt * 6.5f);
            }

            // 2. Input Device Detection (Zero GC)
            DetectInputDevice();

            // 3. Tab Switching Shortcuts (LB/RB or Q/E) when drawer is closed
            if (!IsDrawerOpen)
            {
                bool nextTab = false;
                bool prevTab = false;

#if ENABLE_INPUT_SYSTEM
                var pad = Gamepad.current;
                if (pad != null)
                {
                    if (pad.rightShoulder.wasPressedThisFrame) nextTab = true;
                    if (pad.leftShoulder.wasPressedThisFrame) prevTab = true;
                }

                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.eKey.wasPressedThisFrame || kb.pageDownKey.wasPressedThisFrame) nextTab = true;
                    if (kb.qKey.wasPressedThisFrame || kb.pageUpKey.wasPressedThisFrame) prevTab = true;
                    if (kb.tKey.wasPressedThisFrame) OnQuickFightClicked();
                }
#endif
                if (UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.PageDown)) nextTab = true;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.PageUp)) prevTab = true;

                if (nextTab)
                {
                    int next = ((int)CurrentPillar + 1) % 4;
                    SelectPillar((MainMenuPillar)next);
                }
                else if (prevTab)
                {
                    int prev = ((int)CurrentPillar + 3) % 4;
                    SelectPillar((MainMenuPillar)prev);
                }

                // Global Settings Drawer Shortcut (Esc / Menu)
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)
#if ENABLE_INPUT_SYSTEM
                    || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
#endif
                )
                {
                    OnSettingsClicked();
                }
            }
            else
            {
                // Drawer Close Shortcuts (Esc / East / K)
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.K)
#if ENABLE_INPUT_SYSTEM
                    || (Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame)
#endif
                )
                {
                    CloseSettingsDrawer();
                }
            }
        }

        private void DetectInputDevice()
        {
            bool wasGamepad = _isUsingGamepad;
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
            {
                if (Gamepad.current.wasUpdatedThisFrame)
                {
                    _isUsingGamepad = true;
                }
            }
            if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            {
                _isUsingGamepad = false;
            }
            if (Mouse.current != null && Mouse.current.wasUpdatedThisFrame)
            {
                _isUsingGamepad = false;
            }
#endif
            if (_isUsingGamepad != wasGamepad)
            {
                UpdateControlPromptFooter(force: true);
            }
        }

        private void UpdateControlPromptFooter(bool force = false)
        {
            if (footerControlPromptText == null) return;

            if (_isUsingGamepad)
            {
                footerControlPromptText.text = "[A / South] SELECT    [B / East] BACK    [LB / RB] SWITCH TAB    [Y] QUICK FIGHT    [Start] SETTINGS";
            }
            else
            {
                footerControlPromptText.text = "[J / Enter] SELECT    [K / Esc] BACK    [Q / E] SWITCH TAB    [T] QUICK FIGHT    [Esc] SETTINGS";
            }
        }

        public void SelectPillar(MainMenuPillar pillar, bool immediate = false)
        {
            CurrentPillar = pillar;
            int idx = (int)pillar;

            _targetCamPos = CamPositions[idx];
            _targetCamRot = CamRotations[idx];
            if (immediate && menuCamera != null)
            {
                menuCamera.transform.position = _targetCamPos;
                menuCamera.transform.rotation = _targetCamRot;
            }

            // Update tab button visuals
            UpdateTabButtonVisual(tabFight, pillar == MainMenuPillar.Fight);
            UpdateTabButtonVisual(tabDojo, pillar == MainMenuPillar.Dojo);
            UpdateTabButtonVisual(tabRoster, pillar == MainMenuPillar.Roster);
            UpdateTabButtonVisual(tabSettings, pillar == MainMenuPillar.Settings);

            // Move indicator bar
            if (tabIndicator != null)
            {
                Button activeBtn = pillar == MainMenuPillar.Fight ? tabFight :
                                   pillar == MainMenuPillar.Dojo ? tabDojo :
                                   pillar == MainMenuPillar.Roster ? tabRoster : tabSettings;
                if (activeBtn != null)
                {
                    tabIndicator.transform.position = new Vector3(activeBtn.transform.position.x, tabIndicator.transform.position.y, tabIndicator.transform.position.z);
                }
            }

            // Activate sub-panels
            if (panelFight != null) panelFight.SetActive(pillar == MainMenuPillar.Fight);
            if (panelDojo != null) panelDojo.SetActive(pillar == MainMenuPillar.Dojo);
            if (panelRoster != null) panelRoster.SetActive(pillar == MainMenuPillar.Roster);

            // Settings opens drawer directly
            if (pillar == MainMenuPillar.Settings)
            {
                OpenSettingsDrawer();
                return;
            }
            else if (IsDrawerOpen)
            {
                CloseSettingsDrawer();
            }

            // Set default focus to first card in active panel
            Button firstBtn = null;
            if (pillar == MainMenuPillar.Fight) firstBtn = btnArcade != null ? btnArcade : btnVersus;
            else if (pillar == MainMenuPillar.Dojo) firstBtn = btnTraining != null ? btnTraining : btnMoveReference;
            else if (pillar == MainMenuPillar.Roster) firstBtn = btnRosterShowcase;

            if (firstBtn != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstBtn.gameObject);
            }

            if (!immediate && CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayButtonHover();
            }
        }

        private void UpdateTabButtonVisual(Button btn, bool isActive)
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.color = isActive ? SumiStyleTokens.ParryGold : SumiStyleTokens.PaperWhite;
                txt.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        // =========================================================================
        // LIVE 3D HERO FIGHTER
        // =========================================================================
        public void SpawnHeroFighter()
        {
            string heroId = PlayerPrefs.GetString("MDI_P1_CharacterId", "C01");
            if (string.IsNullOrEmpty(heroId)) heroId = "C01";
            if (_activeHeroId == heroId && _activeHeroModel != null) return;

            _activeHeroId = heroId;
            if (_activeHeroModel != null)
            {
                Destroy(_activeHeroModel);
                _activeHeroModel = null;
            }

            var def = CharacterRegistry.GetCharacterById(heroId);
            if (def == null || def.modelPrefab == null) return;

            Transform spawn = heroSpawnPoint != null ? heroSpawnPoint : transform;
            _activeHeroModel = Instantiate(def.modelPrefab, spawn.position, spawn.rotation, spawn);
            _activeHeroModel.name = $"Hero_{heroId}";

            Animator anim = _activeHeroModel.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                if (def.animatorController != null)
                {
                    anim.runtimeAnimatorController = def.animatorController;
                }
            }

            // Bind MenuFighterCameraRig for dynamic eye-level telephoto framing and Bokeh DOF focus
            if (menuCamera != null)
            {
                var rig = menuCamera.GetComponent<DemonIdol.Cinematics.MenuFighterCameraRig>();
                if (rig != null)
                {
                    rig.SetTargetFighter(_activeHeroModel, _activeHeroId);
                }
            }
        }

        // =========================================================================
        // REWARDS PILL
        // =========================================================================
        public void RefreshRewardPill()
        {
            if (profileRewardText == null) return;

            int xp = LaunchServices.Rewards != null ? LaunchServices.Rewards.CosmeticXp : 0;
            int matches = LaunchServices.Rewards != null ? LaunchServices.Rewards.GrantCount : 0;
            int level = 1 + (xp / 100);

            profileRewardText.text = $"LVL {level} // {xp} XP // {matches} BATTLES";
        }

        // =========================================================================
        // SLIDE-OVER SETTINGS DRAWER (PHASE 3)
        // =========================================================================
        public void OpenSettingsDrawer()
        {
            if (IsDrawerOpen) return;
            IsDrawerOpen = true;

            _lastSelectedBeforeDrawer = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            if (settingsModal != null) settingsModal.SetActive(true);
            if (drawerScrim != null) drawerScrim.SetActive(true);

            if (_drawerRoutine != null) StopCoroutine(_drawerRoutine);
            _drawerRoutine = StartCoroutine(AnimateDrawer(open: true));

            // Select Audio tab by default inside drawer
            SelectDrawerTab(0);

            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayButtonClick();
            }
        }

        public void CloseSettingsDrawer()
        {
            if (!IsDrawerOpen) return;
            IsDrawerOpen = false;

            if (_drawerRoutine != null) StopCoroutine(_drawerRoutine);
            _drawerRoutine = StartCoroutine(AnimateDrawer(open: false));

            // Restore focus
            if (_lastSelectedBeforeDrawer != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedBeforeDrawer);
            }
            else if (tabFight != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(tabFight.gameObject);
            }

            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayButtonHover();
            }
        }

        private IEnumerator AnimateDrawer(bool open)
        {
            if (settingsDrawerRoot == null) yield break;

            float duration = open ? SumiStyleTokens.PanelInDuration : SumiStyleTokens.PanelOutDuration;
            float startX = settingsDrawerRoot.anchoredPosition.x;
            float targetX = open ? 0f : 560f;

            float startAlpha = drawerCanvasGroup != null ? drawerCanvasGroup.alpha : (open ? 0f : 1f);
            float targetAlpha = open ? 1f : 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = open ? SumiStyleTokens.EaseOutCubic(t) : SumiStyleTokens.EaseInCubic(t);

                settingsDrawerRoot.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, ease), settingsDrawerRoot.anchoredPosition.y);
                if (drawerCanvasGroup != null)
                {
                    drawerCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);
                }
                yield return null;
            }

            settingsDrawerRoot.anchoredPosition = new Vector2(targetX, settingsDrawerRoot.anchoredPosition.y);
            if (drawerCanvasGroup != null) drawerCanvasGroup.alpha = targetAlpha;

            if (!open)
            {
                if (drawerScrim != null) drawerScrim.SetActive(false);
                if (settingsModal != null) settingsModal.SetActive(false);
            }
            else
            {
                if (sliderBGM != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(sliderBGM.gameObject);
                }
            }
        }

        public void SelectDrawerTab(int tabIndex)
        {
            if (drawerPanelAudio != null) drawerPanelAudio.SetActive(tabIndex == 0);
            if (drawerPanelGraphics != null) drawerPanelGraphics.SetActive(tabIndex == 1);
            if (drawerPanelAccessibility != null) drawerPanelAccessibility.SetActive(tabIndex == 2);
            if (drawerPanelControls != null) drawerPanelControls.SetActive(tabIndex == 3);

            UpdateTabButtonVisual(tabDrawerAudio, tabIndex == 0);
            UpdateTabButtonVisual(tabDrawerGraphics, tabIndex == 1);
            UpdateTabButtonVisual(tabDrawerAccessibility, tabIndex == 2);
            UpdateTabButtonVisual(tabDrawerControls, tabIndex == 3);
        }

        private void SetupSettingsSliders()
        {
            if (sliderBGM != null)
            {
                sliderBGM.value = PlayerPrefs.GetFloat("MDI_BGM_Volume", 0.8f);
                sliderBGM.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MDI_BGM_Volume", val);
                    CombatAudioDirector.Instance?.SetMusicVolume(val);
                });
            }

            if (sliderSFX != null)
            {
                sliderSFX.value = PlayerPrefs.GetFloat("MDI_SFX_Volume", 1.0f);
                sliderSFX.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MDI_SFX_Volume", val);
                    CombatAudioDirector.Instance?.SetSFXVolume(val);
                });
            }

            if (sliderVoice != null)
            {
                sliderVoice.value = PlayerPrefs.GetFloat("MDI_Voice_Volume", 0.9f);
                sliderVoice.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MDI_Voice_Volume", val);
                });
            }

            if (btnTestSound != null)
            {
                btnTestSound.onClick.AddListener(() =>
                {
                    CombatAudioDirector.Instance?.PlayHitLight();
                });
            }
        }

        private void SetupGraphicsAndAccessibilityUI()
        {
            // Quality
            int currentQuality = QualitySettings.GetQualityLevel();
            UpdateQualityLabel(currentQuality);
            if (btnQualityNext != null) btnQualityNext.onClick.AddListener(() => CycleQuality(1));
            if (btnQualityPrev != null) btnQualityPrev.onClick.AddListener(() => CycleQuality(-1));

            // Fullscreen
            UpdateFullscreenLabel();
            if (btnToggleFullscreen != null) btnToggleFullscreen.onClick.AddListener(() =>
            {
                Screen.fullScreen = !Screen.fullScreen;
                UpdateFullscreenLabel();
            });

            // VSync
            UpdateVSyncLabel();
            if (btnToggleVSync != null) btnToggleVSync.onClick.AddListener(() =>
            {
                QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
                UpdateVSyncLabel();
            });

            if (txtFpsLock != null)
            {
                txtFpsLock.text = $"TARGET: {Application.targetFrameRate} FPS";
            }

            // Reduced Motion
            UpdateReducedMotionLabel();
            if (btnToggleReducedMotion != null) btnToggleReducedMotion.onClick.AddListener(() =>
            {
                bool next = !SettingsPanel.ReducedMotion;
                PlayerPrefs.SetInt("mdi_reduced_motion", next ? 1 : 0);
                PlayerPrefs.Save();
                UpdateReducedMotionLabel();
            });

            // Reduced Gore
            UpdateReducedGoreLabel();
            if (btnToggleReducedGore != null) btnToggleReducedGore.onClick.AddListener(() =>
            {
                bool next = !SettingsPanel.ReducedGore;
                PlayerPrefs.SetInt("mdi_reduced_gore", next ? 1 : 0);
                PlayerPrefs.Save();
                UpdateReducedGoreLabel();
            });
        }

        private void CycleQuality(int dir)
        {
            string[] names = QualitySettings.names;
            int q = QualitySettings.GetQualityLevel();
            q = (q + dir + names.Length) % names.Length;
            QualitySettings.SetQualityLevel(q, true);
            UpdateQualityLabel(q);
        }

        private void UpdateQualityLabel(int q)
        {
            if (txtQualityName != null)
            {
                string[] names = QualitySettings.names;
                txtQualityName.text = (names != null && q < names.Length) ? names[q].ToUpper() : $"TIER {q}";
            }
        }

        private void UpdateFullscreenLabel()
        {
            if (txtFullscreenState != null)
            {
                txtFullscreenState.text = Screen.fullScreen ? "FULLSCREEN: ON" : "FULLSCREEN: OFF";
            }
        }

        private void UpdateVSyncLabel()
        {
            if (txtVSyncState != null)
            {
                txtVSyncState.text = QualitySettings.vSyncCount > 0 ? "VSYNC: ENABLED" : "VSYNC: DISABLED";
            }
        }

        private void UpdateReducedMotionLabel()
        {
            if (txtReducedMotionState != null)
            {
                txtReducedMotionState.text = SettingsPanel.ReducedMotion ? "REDUCED MOTION: ON" : "REDUCED MOTION: OFF";
            }
        }

        private void UpdateReducedGoreLabel()
        {
            if (txtReducedGoreState != null)
            {
                txtReducedGoreState.text = SettingsPanel.ReducedGore ? "REDUCED GORE: ON" : "REDUCED GORE: OFF";
            }
        }

        // =========================================================================
        // BUTTON LISTENERS & MODE NAVIGATION
        // =========================================================================
        private void SetupButtonListeners()
        {
            // Top Tabs
            if (tabFight != null) tabFight.onClick.AddListener(() => SelectPillar(MainMenuPillar.Fight));
            if (tabDojo != null) tabDojo.onClick.AddListener(() => SelectPillar(MainMenuPillar.Dojo));
            if (tabRoster != null) tabRoster.onClick.AddListener(() => SelectPillar(MainMenuPillar.Roster));
            if (tabSettings != null) tabSettings.onClick.AddListener(() => SelectPillar(MainMenuPillar.Settings));

            // Drawer Tabs
            if (tabDrawerAudio != null) tabDrawerAudio.onClick.AddListener(() => SelectDrawerTab(0));
            if (tabDrawerGraphics != null) tabDrawerGraphics.onClick.AddListener(() => SelectDrawerTab(1));
            if (tabDrawerAccessibility != null) tabDrawerAccessibility.onClick.AddListener(() => SelectDrawerTab(2));
            if (tabDrawerControls != null) tabDrawerControls.onClick.AddListener(() => SelectDrawerTab(3));

            // Mode Buttons
            if (btnArcade != null) btnArcade.onClick.AddListener(OnArcadeClicked);
            if (btnVersus != null) btnVersus.onClick.AddListener(OnVersusClicked);
            if (btnTraining != null) btnTraining.onClick.AddListener(OnTrainingClicked);
            if (btnQuickFight != null) btnQuickFight.onClick.AddListener(OnQuickFightClicked);
            if (btnOnlinePvP != null) btnOnlinePvP.onClick.AddListener(OnOnlinePvPClicked);
            if (btnMoveReference != null) btnMoveReference.onClick.AddListener(OnControlsClicked);
            if (btnTutorial != null) btnTutorial.onClick.AddListener(OnTutorialClicked);
            if (btnRosterShowcase != null) btnRosterShowcase.onClick.AddListener(OnRosterShowcaseClicked);

            if (btnControls != null) btnControls.onClick.AddListener(OnControlsClicked);
            if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);

            if (btnCloseControls != null) btnCloseControls.onClick.AddListener(() =>
            {
                if (controlsModal != null) controlsModal.SetActive(false);
                if (btnControls != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btnControls.gameObject);
            });

            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(CloseSettingsDrawer);
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
            CombatAudioDirector.Instance?.PlayButtonClick();
            LaunchServices.Session.TrySelectMode("Arcade", out _);
            PlayerPrefs.SetString("MDI_GameMode", "Arcade");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnVersusClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            LaunchServices.Session.TrySelectMode("LocalVersus", out _);
            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnTrainingClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            LaunchServices.Session.TrySelectMode("Training", out _);
            PlayerPrefs.SetString("MDI_GameMode", "Training");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnQuickFightClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            string p1 = PlayerPrefs.GetString("MDI_P1_CharacterId", "C01");
            string p2 = PlayerPrefs.GetString("MDI_P2_CharacterId", "C02");
            string stage = PlayerPrefs.GetString("MDI_StageId", "ST07");
            string mode = PlayerPrefs.GetString("MDI_GameMode", "Arcade");

            LaunchServices.Session.TrySelectMode(mode, out _);
            LaunchServices.Session.BeginMatch(p1, p2, stage);
            SceneManager.LoadScene("BattleArena");
        }

        public void OnOnlinePvPClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            var pvp = FindAnyObjectByType<PvPLobbyUI>(FindObjectsInactive.Include);
            if (pvp != null)
            {
                pvp.gameObject.SetActive(true);
            }
            else
            {
                // Fallback to Versus
                OnVersusClicked();
            }
        }

        public void OnTutorialClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            LaunchServices.Session.TrySelectMode("Training", out _);
            PlayerPrefs.SetString("MDI_GameMode", "Training");
            PlayerPrefs.Save();
            SceneManager.LoadScene("CharacterSelect");
        }

        public void OnRosterShowcaseClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            if (Application.CanStreamedLevelBeLoaded("RosterShowcase"))
            {
                SceneManager.LoadScene("RosterShowcase");
            }
            else
            {
                OnVersusClicked();
            }
        }

        public void OnControlsClicked()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            if (controlsModal != null)
            {
                controlsModal.SetActive(true);
                if (btnCloseControls != null && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btnCloseControls.gameObject);
            }
            else
            {
                // Open Drawer Controls tab
                OpenSettingsDrawer();
                SelectDrawerTab(3);
            }
        }

        public void OnSettingsClicked()
        {
            OpenSettingsDrawer();
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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

        // =========================================================================
        // DYNAMIC RUNTIME MODERN HIERARCHY ENSURER (SCENE INDEPENDENT)
        // =========================================================================
        public void EnsureModernHierarchy()
        {
            if (menuCamera == null)
            {
                var cam = Camera.main;
                if (cam != null) menuCamera = cam;
            }

            if (heroSpawnPoint == null)
            {
                var hp = GameObject.Find("Hero_SpawnPoint");
                if (hp == null)
                {
                    hp = new GameObject("Hero_SpawnPoint");
                    hp.transform.position = new Vector3(1.75f, 0f, 0f);
                    hp.transform.rotation = Quaternion.Euler(0f, -145f, 0f);
                }
                heroSpawnPoint = hp.transform;
            }

            // Ensure SafeAreaFitter is present
            if (GetComponent<SafeAreaFitter>() == null && GetComponent<RectTransform>() != null)
            {
                gameObject.AddComponent<SafeAreaFitter>();
            }
        }
    }
}
