using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DemonIdol.Cutscenes;
using DemonIdol.Network;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.UI
{
    public enum MenuScreenState
    {
        Loading,
        SimpleAuth,
        ModeSelect,
        CharacterSelect,
        StageSelect,
        PvPLobby,
        Settings,
        InGame
    }

    /// <summary>
    /// Master UI Orchestrator combining Japanese Sumi-e brush aesthetics
    /// with SF6-grade modern tactical navigation.
    /// Handles animated transitions, Android System Back, debounced navigation,
    /// local battle pause, and story checkpoint continuation.
    /// </summary>
    public class BloodTechUIManager : MonoBehaviour
    {
        public static BloodTechUIManager Instance { get; private set; }

        public const string PLAYER_NAME_KEY = "demonidol_player_name";

        [Header("Screen Panels")]
        [SerializeField] private CanvasGroup _loadingPanel;
        [SerializeField] private CanvasGroup _authPanel;
        [SerializeField] private CanvasGroup _modeSelectPanel;
        [SerializeField] private CanvasGroup _characterSelectPanel;
        [SerializeField] private CanvasGroup _battleHUDPanel;

        [Header("Modals")]
        [SerializeField] private GameObject _pvpLobbyModal;
        [SerializeField] private GameObject _settingsModal;
        [SerializeField] private GameObject _pauseModal;

        [Header("Loading Screen Elements")]
        [SerializeField] private Slider _loadingProgressBar;
        [SerializeField] private Text _loadingPercentText;
        [SerializeField] private Button _btnStartGame;
        [SerializeField] private float _simulatedLoadDuration = 1.6f;

        [Header("Simple Auth Elements (Guest / Codename)")]
        [SerializeField] private InputField _codenameInputField;
        [SerializeField] private Text _welcomeUserText;

        [Header("Main Mode Selection Buttons")]
        [SerializeField] private Button _btnPvP;
        [SerializeField] private Button _btnArcade;
        [SerializeField] private Button _btnLocalVersus;
        [SerializeField] private Button _btnStory;
        [SerializeField] private Button _btnContinueStory;
        [SerializeField] private Text _continueStoryLabel;
        [SerializeField] private Button _btnTraining;
        [SerializeField] private Button _btnTutorialLessons;
        [SerializeField] private Button _btnSettings;
        [SerializeField] private Button _btnShowcase;

        [Header("Demon Lord Announcer")]
        [SerializeField] private DemonLordAnnouncerWidget _demonLordWidget;

        [Header("In-Game Pause Buttons")]
        [SerializeField] private Button _btnPauseResume;
        [SerializeField] private Button _btnPauseRestart;
        [SerializeField] private Button _btnPauseSettings;
        [SerializeField] private Button _btnPauseExitToMenu;

        public MenuScreenState CurrentState { get; private set; } = MenuScreenState.Loading;
        public string ActivePlayerName { get; private set; } = "KAI";
        public bool IsLocalPaused { get; private set; } = false;

        private float _lastActionTimestamp = 0f;
        private Coroutine _activeTransitionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ActivePlayerName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "KAI");
            if (_codenameInputField != null) _codenameInputField.text = ActivePlayerName;

            EnsureModeButtons();

            // Wire Mode Select Buttons with debounce (Strictly the 5 authorized buttons)
            WireButton(_btnPvP, OnClickModePvP);
            WireButton(_btnArcade, OnClickModeArcade);
            WireButton(_btnStory, OnClickModeStory);
            WireButton(_btnTraining, OnClickModeTraining);
            WireButton(_btnSettings, OnClickOpenSettings);

            // Ensure and Wire Pause Modal
            EnsurePauseModal();
            EnsurePvPLobbyModal();
        }

        private static bool _hasCompletedInitialBoot = false;

        private void Start()
        {
            EnsureComplianceAndFeedbackUI();

            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.OnMatchFinished -= HandleMatchFinished;
                MatchRoundReferee.Instance.OnMatchFinished += HandleMatchFinished;
            }

            // If returning from match to Character Select, skip boot loading sequence
            if (PlayerPrefs.GetInt("DemonIdol_ReturnToCharSelect", 0) == 1)
            {
                PlayerPrefs.SetInt("DemonIdol_ReturnToCharSelect", 0);
                PlayerPrefs.Save();
                _hasCompletedInitialBoot = true;
                TransitionToCharacterSelect(_activeSelectedMode);
                return;
            }

            // If game already completed initial boot in this session, return directly to Mode Select
            if (_hasCompletedInitialBoot)
            {
                TransitionToModeSelect();
                return;
            }

            _hasCompletedInitialBoot = true;

            // First time boot: allow GameFlowController to drive Splash & Title screen cleanly
            if (DemonIdol.Launch.DemonIdolGameFlowController.Instance != null)
            {
                ShowOnlyPanel(null);
                DemonIdol.Launch.DemonIdolGameFlowController.Instance.StartGameFlow();
            }
            else
            {
                StartCoroutine(RunLoadingSequence());
            }
        }

        private void OnDestroy()
        {
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.OnMatchFinished -= HandleMatchFinished;
            }
        }

        private string _activeSelectedMode = "Training";

        private void HandleMatchFinished(RoundWinner matchWinner)
        {
            if (_activeSelectedMode == "Story" && matchWinner == RoundWinner.Player1)
            {
                if (StoryCheckpointManager.Instance != null)
                {
                    StoryCheckpointManager.Instance.CompleteCurrentCheckpoint(() =>
                    {
                        if (DemonIdol.Launch.DemonIdolGameFlowController.Instance != null)
                        {
                            DemonIdol.Launch.DemonIdolGameFlowController.Instance.ExitModeWithEndingVideo();
                        }
                        else
                        {
                            TransitionToModeSelect();
                        }
                    });
                }
            }
        }

        public void EnsureComplianceAndFeedbackUI()
        {
            Transform uiParent = UiHierarchy.ResolveCanvasParent(transform);

            if (LoadingFeedbackUI.Instance == null)
            {
                GameObject loadObj = new GameObject("LoadingFeedbackUI", typeof(RectTransform));
                loadObj.transform.SetParent(uiParent, false);
                loadObj.AddComponent<LoadingFeedbackUI>();
            }

            if (LegalComplianceUI.Instance == null)
            {
                GameObject legalObj = new GameObject("Modal_LegalCompliance", typeof(RectTransform));
                legalObj.transform.SetParent(uiParent, false);
                legalObj.AddComponent<LegalComplianceUI>();
            }

            if (CreditsUI.Instance == null)
            {
                GameObject credObj = new GameObject("Modal_Credits", typeof(RectTransform));
                credObj.transform.SetParent(uiParent, false);
                credObj.AddComponent<CreditsUI>();
            }

            if (CutsceneManager.Instance == null)
            {
                GameObject cutsceneObj = new GameObject("CutsceneManager");
                cutsceneObj.transform.SetParent(transform, false);
                cutsceneObj.AddComponent<CutsceneManager>();
            }

            if (StoryCheckpointManager.Instance == null)
            {
                GameObject storyObj = new GameObject("StoryCheckpointManager");
                storyObj.transform.SetParent(transform, false);
                storyObj.AddComponent<StoryCheckpointManager>();
            }

            if (DemonLordNarratorManager.Instance == null)
            {
                GameObject dlObj = new GameObject("DemonLordNarratorManager");
                dlObj.transform.SetParent(transform, false);
                dlObj.AddComponent<DemonLordNarratorManager>();
            }

            if (StageSelectUI.Instance == null)
            {
                Transform existingStage = uiParent.Find("Panel_StageSelect");
                if (existingStage == null)
                {
                    GameObject stageObj = new GameObject("Panel_StageSelect", typeof(RectTransform));
                    stageObj.transform.SetParent(uiParent, false);
                    stageObj.AddComponent<StageSelectUI>();
                }
            }
        }

        private void Update()
        {
            // Android System Back / Escape Key Routing
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    HandleSystemBack();
                }
            }
            catch (System.InvalidOperationException)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    HandleSystemBack();
                }
#endif
            }
        }

        private bool CheckDebounce()
        {
            if (Time.unscaledTime - _lastActionTimestamp < SumiStyleTokens.MenuDebounceSeconds)
            {
                return false;
            }
            _lastActionTimestamp = Time.unscaledTime;
            return true;
        }

        #region Screen Flow & Transitions

        private IEnumerator RunLoadingSequence()
        {
            ShowOnlyPanel(_loadingPanel);
            CurrentState = MenuScreenState.Loading;

            if (_btnStartGame != null) _btnStartGame.gameObject.SetActive(false);

            float elapsed = 0f;
            while (elapsed < _simulatedLoadDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _simulatedLoadDuration);

                if (_loadingProgressBar != null) _loadingProgressBar.value = progress;
                if (_loadingPercentText != null) _loadingPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
                yield return null;
            }

            if (_loadingProgressBar != null) _loadingProgressBar.value = 1f;
            if (_loadingPercentText != null) _loadingPercentText.text = "CALIBRATION COMPLETE // SYSTEM READY";

            EnsureStartGameButton();

            if (_btnStartGame != null)
            {
                _btnStartGame.gameObject.SetActive(true);
                bool started = false;
                _btnStartGame.onClick.RemoveAllListeners();
                _btnStartGame.onClick.AddListener(() => started = true);

                float pulseTimer = 0f;
                CanvasGroup btnCg = _btnStartGame.GetComponent<CanvasGroup>();
                if (btnCg == null) btnCg = _btnStartGame.gameObject.AddComponent<CanvasGroup>();

                while (!started)
                {
                    pulseTimer += Time.unscaledDeltaTime * 4f;
                    btnCg.alpha = 0.7f + 0.3f * Mathf.Sin(pulseTimer);

                    try
                    {
                        if (UnityEngine.Input.anyKeyDown && !UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                        {
                            started = true;
                        }
                    }
                    catch (System.InvalidOperationException)
                    {
#if ENABLE_INPUT_SYSTEM
                        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame)
                        {
                            started = true;
                        }
                        else if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                        {
                            started = true;
                        }
#endif
                    }
                    yield return null;
                }
                btnCg.alpha = 1f;
                DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            }

            if (DemonIdol.Launch.DemonIdolGameFlowController.Instance != null)
            {
                DemonIdol.Launch.DemonIdolGameFlowController.Instance.ShowModeSelectScreen();
            }
            else
            {
                TransitionToModeSelect();
            }
        }

        public void EnsureStartGameButton()
        {
            if (_btnStartGame != null || _loadingPanel == null) return;

            Transform existing = _loadingPanel.transform.Find("Btn_StartGame") ?? _loadingPanel.transform.Find("Btn_PressToStart");
            if (existing != null)
            {
                _btnStartGame = existing.GetComponent<Button>();
                return;
            }

            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject btnObj = new GameObject("Btn_StartGame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
            btnObj.transform.SetParent(_loadingPanel.transform, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, -160f);
            rt.sizeDelta = new Vector2(500f, 65f);

            Image img = btnObj.GetComponent<Image>();
            _btnStartGame = btnObj.GetComponent<Button>();
            _btnStartGame.targetGraphic = img;
            if (atlas != null) atlas.StyleButton(_btnStartGame, 2, true);
            else img.color = SumiStyleTokens.ToriiRed;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            Text txt = txtObj.GetComponent<Text>();
            txt.font = font;
            txt.text = "▶  PRESS TO START  ◀";
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = SumiStyleTokens.PaperWhite;

            Transform existingPrologue = _loadingPanel.transform.Find("Btn_WatchPrologue");
            if (existingPrologue == null)
            {
                GameObject proObj = new GameObject("Btn_WatchPrologue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                proObj.transform.SetParent(_loadingPanel.transform, false);
                RectTransform prt = proObj.GetComponent<RectTransform>();
                prt.anchoredPosition = new Vector2(0f, -232f);
                prt.sizeDelta = new Vector2(360f, 40f);
                Image pImg = proObj.GetComponent<Image>();
                Button pBtn = proObj.GetComponent<Button>();
                pBtn.targetGraphic = pImg;
                if (atlas != null) atlas.StyleButton(pBtn, 3, false);
                else pImg.color = new Color(0.18f, 0.12f, 0.28f, 0.95f);
                pBtn.onClick.AddListener(() =>
                {
                    DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
                    DemonLordNarratorManager.Instance?.PlayGameIntroduction();
                });

                GameObject pTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                pTxtGo.transform.SetParent(proObj.transform, false);
                RectTransform ptrt = pTxtGo.GetComponent<RectTransform>();
                ptrt.anchorMin = Vector2.zero;
                ptrt.anchorMax = Vector2.one;
                ptrt.sizeDelta = Vector2.zero;
                Text pTxt = pTxtGo.GetComponent<Text>();
                pTxt.font = font;
                pTxt.text = "★ WATCH PROLOGUE (DEMON LORD)";
                pTxt.fontSize = 13;
                pTxt.fontStyle = FontStyle.Bold;
                pTxt.alignment = TextAnchor.MiddleCenter;
                pTxt.color = new Color(1f, 0.85f, 0.4f, 1f);
            }
        }

        public void TransitionToModeSelect()
        {
            UnpauseIfPaused();

            if (PlayerPrefs.GetInt("DemonIdol_ReturnToCharSelect", 0) == 1)
            {
                PlayerPrefs.SetInt("DemonIdol_ReturnToCharSelect", 0);
                PlayerPrefs.Save();
                TransitionToCharacterSelect(_activeSelectedMode);
                return;
            }

            if (_welcomeUserText != null)
            {
                _welcomeUserText.text = $"OPERATIVE: {ActivePlayerName.ToUpper()}";
            }

            UpdateContinueStoryButton();

            EnsureDemonLordWidget();
            if (_demonLordWidget != null)
            {
                _demonLordWidget.UpdateForMode(_activeSelectedMode);
            }

            CrossFadeTo(_modeSelectPanel);
            CurrentState = MenuScreenState.ModeSelect;
        }

        public void EnsureDemonLordWidget()
        {
            if (_modeSelectPanel == null) return;
            if (_demonLordWidget != null) return;

            Transform existing = _modeSelectPanel.transform.Find("DemonLordAnnouncerWidget");
            if (existing != null)
            {
                _demonLordWidget = existing.GetComponent<DemonLordAnnouncerWidget>();
                if (_demonLordWidget != null) return;
            }

            GameObject widgetGo = new GameObject("DemonLordAnnouncerWidget", typeof(RectTransform));
            widgetGo.transform.SetParent(_modeSelectPanel.transform, false);
            RectTransform rt = widgetGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -28f);
            rt.sizeDelta = new Vector2(850f, 96f);

            _demonLordWidget = widgetGo.AddComponent<DemonLordAnnouncerWidget>();
            _demonLordWidget.EnsureWidgetHierarchy();
        }

        public void TransitionToCharacterSelect(string mode = "Training")
        {
            if (!CheckDebounce()) return;

            if (!LaunchServices.Session.TrySelectMode(mode, out string rejection))
            {
                ShowModeNotice(rejection);
                return;
            }

            _activeSelectedMode = mode;
            UnpauseIfPaused();

            if (CharacterSelectUI.Instance != null)
            {
                CharacterSelectUI.Instance.OpenForMode(mode);
            }
            CrossFadeTo(_characterSelectPanel);
            CurrentState = MenuScreenState.CharacterSelect;
        }

        public void TransitionToInGame()
        {
            CrossFadeTo(_battleHUDPanel);
            CurrentState = MenuScreenState.InGame;
        }

        public void TransitionToStageSelect()
        {
            if (!CheckDebounce()) return;
            UnpauseIfPaused();

            if (CharacterSelectUI.Instance != null)
            {
                CharacterSelectUI.Instance.gameObject.SetActive(false);
            }

            if (StageSelectUI.Instance != null)
            {
                StageSelectUI.Instance.OpenForMode(_activeSelectedMode);
            }

            CanvasGroup stageGroup = StageSelectUI.Instance != null ? StageSelectUI.Instance.GetComponent<CanvasGroup>() : null;
            if (stageGroup != null)
            {
                CrossFadeTo(stageGroup);
            }
            CurrentState = MenuScreenState.StageSelect;
        }

        public void TransitionToFight(StageDefinition stage = null)
        {
            if (!CheckDebounce()) return;
            UnpauseIfPaused();

            StartCoroutine(FightTransitionSequence(stage));
        }

        private GameObject _activeStagePrefabInstance;

        public void ApplyStageArena(StageDefinition stage)
        {
            if (stage == null) return;

            // 1. Move fighters to stage spawn coordinates
            GameObject p1Obj = GameObject.Find("Player1_Akio") ?? GameObject.Find("Player1_Kai") ?? GameObject.Find("Player1_Fighter");
            GameObject p2Obj = GameObject.Find("Player2_Aika") ?? GameObject.Find("Player2_Dummy") ?? GameObject.Find("Player2_Fighter");

            if (p1Obj != null)
            {
                p1Obj.transform.position = stage.p1SpawnPosition;
            }
            if (p2Obj != null)
            {
                p2Obj.transform.position = stage.p2SpawnPosition;
            }

            // 2. Configure PushbackSolver arena limits & wall splat thresholds
            PushbackSolver.SetArenaBounds(stage.ArenaLimitX, stage.wallThreshold);

            // 3. Configure Camera framing limits
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.ApplyStageCameraLimits(stage);
            }

            // 4. Play stage BGM audio
            if (stage.bgmAudioClip != null)
            {
                AudioSource audio = Camera.main != null ? Camera.main.GetComponent<AudioSource>() : null;
                if (audio != null)
                {
                    audio.clip = stage.bgmAudioClip;
                    audio.loop = true;
                    audio.Play();
                }
            }

            // 5. Instantiate or swap custom stage prefab if authored
            if (stage.stagePrefab != null)
            {
                if (_activeStagePrefabInstance != null)
                {
                    Destroy(_activeStagePrefabInstance);
                }
                _activeStagePrefabInstance = Instantiate(stage.stagePrefab, Vector3.zero, Quaternion.identity);
            }
        }

        private IEnumerator FightTransitionSequence(StageDefinition stage)
        {
            if (StageSelectUI.Instance != null)
            {
                StageSelectUI.Instance.Close();
            }

            if (stage != null)
            {
                ApplyStageArena(stage);
            }

            if (LoadingFeedbackUI.Instance != null)
            {
                string title = stage != null ? $"DEPLOYING TO {stage.displayName.ToUpper()}..." : "CALIBRATING 60HZ COMBAT ENGINE...";
                bool loadingComplete = false;
                LoadingFeedbackUI.Instance.ShowLoading(0.85f, title, () => loadingComplete = true);
                while (!loadingComplete)
                {
                    yield return null;
                }
            }

            TransitionToInGame();

            // Initialize Homegrown Combat Director with selected fighters and stage
            var p1Def = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.SelectedP1Definition : null;
            var p2Def = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.SelectedP2Definition : null;
            string p1Id = p1Def != null ? p1Def.id : LaunchServices.Session.Player1Id;
            string p2Id = p2Def != null ? p2Def.id : LaunchServices.Session.Player2Id;
            string stageId = stage != null ? stage.id : LaunchServices.Session.StageId;

            if (DemonIdol.Combat.HomegrownCombatDirector.Instance != null)
            {
                DemonIdol.Combat.HomegrownCombatDirector.Instance.SetMatchContext(p1Id, p2Id, stageId, LaunchServices.Session.P2IsCpu, _activeSelectedMode == "Training");
                DemonIdol.Combat.HomegrownCombatDirector.Instance.InitializeHomegrownMatch();
            }

            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.isTrainingMode = (_activeSelectedMode == "Training");
                MatchRoundReferee.Instance.StartMatch();
            }

            if ((_activeSelectedMode == "Tutorial" || _activeSelectedMode == "Curriculum") && OnboardingDirector.Instance != null)
            {
                OnboardingDirector.Instance.StartCurriculum();
            }
            else if (OnboardingDirector.Instance != null)
            {
                OnboardingDirector.Instance.StopCurriculum();
            }
        }

        private void CrossFadeTo(CanvasGroup targetGroup)
        {
            CanvasGroup fromGroup = GetCurrentCanvasGroup();
            if (_activeTransitionRoutine != null) StopCoroutine(_activeTransitionRoutine);
            _activeTransitionRoutine = StartCoroutine(CrossFadeRoutine(fromGroup, targetGroup));
        }

        private IEnumerator CrossFadeRoutine(CanvasGroup fromGroup, CanvasGroup targetGroup)
        {
            bool reducedMotion = SettingsPanel.ReducedMotion;
            float duration = reducedMotion ? SumiStyleTokens.ReducedMotionMaxDuration : SumiStyleTokens.PanelInDurationNormal;

            CanvasGroup[] allPanels = new CanvasGroup[] { _loadingPanel, _authPanel, _modeSelectPanel, _characterSelectPanel, _battleHUDPanel };

            // Instantly ensure anything that is not the target cannot intercept clicks
            foreach (var panel in allPanels)
            {
                if (panel != null && panel != targetGroup)
                {
                    panel.blocksRaycasts = false;
                }
            }

            if (fromGroup != null && fromGroup != targetGroup)
            {
                fromGroup.blocksRaycasts = false;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    float alpha = reducedMotion ? 0f : (1f - SumiStyleTokens.EaseInCubic(t / duration));
                    fromGroup.alpha = alpha;
                    yield return null;
                }
                fromGroup.alpha = 0f;
            }

            // Ensure all non-target panels are completely silent and inactive to raycasts
            foreach (var panel in allPanels)
            {
                if (panel != null && panel != targetGroup)
                {
                    panel.alpha = 0f;
                    panel.blocksRaycasts = false;
                }
            }

            if (targetGroup != null)
            {
                targetGroup.gameObject.SetActive(true);
                targetGroup.blocksRaycasts = true;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    float alpha = reducedMotion ? 1f : SumiStyleTokens.EaseOutCubic(t / duration);
                    targetGroup.alpha = alpha;
                    yield return null;
                }
                targetGroup.alpha = 1f;
            }
        }

        private void ShowOnlyPanel(CanvasGroup activeGroup)
        {
            SetGroupActive(_loadingPanel, _loadingPanel == activeGroup);
            SetGroupActive(_authPanel, _authPanel == activeGroup);
            SetGroupActive(_modeSelectPanel, _modeSelectPanel == activeGroup);
            SetGroupActive(_characterSelectPanel, _characterSelectPanel == activeGroup);
            SetGroupActive(_battleHUDPanel, _battleHUDPanel == activeGroup);
            if (_pvpLobbyModal != null) _pvpLobbyModal.SetActive(false);
            if (_settingsModal != null) _settingsModal.SetActive(false);
            if (_pauseModal != null) _pauseModal.SetActive(false);
        }

        private void SetGroupActive(CanvasGroup group, bool active)
        {
            if (group == null) return;
            group.alpha = active ? 1f : 0f;
            group.blocksRaycasts = active;
            group.gameObject.SetActive(true);
        }

        private CanvasGroup GetCurrentCanvasGroup()
        {
            switch (CurrentState)
            {
                case MenuScreenState.Loading: return _loadingPanel;
                case MenuScreenState.SimpleAuth: return _authPanel;
                case MenuScreenState.ModeSelect: return _modeSelectPanel;
                case MenuScreenState.CharacterSelect: return _characterSelectPanel;
                case MenuScreenState.StageSelect: return StageSelectUI.Instance != null ? StageSelectUI.Instance.GetComponent<CanvasGroup>() : null;
                case MenuScreenState.InGame: return _battleHUDPanel;
                default: return null;
            }
        }

        #endregion

        #region Local Pause & Android Back Handling

        public void HandleSystemBack()
        {
            // 0. If Legal Compliance Modal is open -> close it
            if (LegalComplianceUI.Instance != null && LegalComplianceUI.Instance.gameObject.activeSelf)
            {
                LegalComplianceUI.Instance.Close();
                return;
            }

            // 0.1 If Credits Modal is open -> close it
            if (CreditsUI.Instance != null && CreditsUI.Instance.gameObject.activeSelf)
            {
                CreditsUI.Instance.Close();
                return;
            }

            // 0.2 If Stage Select is open -> return to Character Select
            if (CurrentState == MenuScreenState.StageSelect)
            {
                if (StageSelectUI.Instance != null && StageSelectUI.Instance.gameObject.activeSelf)
                {
                    StageSelectUI.Instance.OnClickBack();
                    return;
                }
            }

            // 1. If Settings Modal is open -> close it
            if (_settingsModal != null && _settingsModal.activeSelf)
            {
                OnClickCloseSettingsModal();
                return;
            }

            // 2. If PvP Modal is open -> close it
            if (_pvpLobbyModal != null && _pvpLobbyModal.activeSelf)
            {
                OnClickClosePvPModal();
                return;
            }

            // 3. If In-Game Local Battle -> Toggle Local Pause
            if (CurrentState == MenuScreenState.InGame)
            {
                if (IsLocalPaused)
                {
                    OnClickResumeBattle();
                }
                else
                {
                    ToggleLocalPause(true);
                }
                return;
            }

            // 4. If in Character Select -> Return to Mode Select
            if (CurrentState == MenuScreenState.CharacterSelect)
            {
                if (DemonIdol.Launch.DemonIdolGameFlowController.Instance != null)
                {
                    DemonIdol.Launch.DemonIdolGameFlowController.Instance.ShowModeSelectScreen();
                }
                else
                {
                    TransitionToModeSelect();
                }
                return;
            }
        }

        private void WireButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }

        private void EnsureModeButtons()
        {
            if (_modeSelectPanel == null) return;
            Transform modeRoot = _modeSelectPanel.transform;

            if (_btnPvP == null) _btnPvP = FindButtonRecursive(modeRoot.gameObject, "Btn_ModePvP", "Btn_PvP");
            if (_btnArcade == null) _btnArcade = FindButtonRecursive(modeRoot.gameObject, "Btn_ModeArcade", "Btn_Arcade");
            if (_btnStory == null) _btnStory = FindButtonRecursive(modeRoot.gameObject, "Btn_ModeStory", "Btn_Story");
            if (_btnTraining == null) _btnTraining = FindButtonRecursive(modeRoot.gameObject, "Btn_ModeTraining", "Btn_Training");
            if (_btnSettings == null) _btnSettings = FindButtonRecursive(modeRoot.gameObject, "Btn_Settings", "Btn_ModeSettings");

            // Strip out any unauthorized or legacy buttons from earlier prototypes
            string[] legacyButtons = { "Btn_ModeLocalVersus", "Btn_ModeShowcase", "Btn_TutorialLessons", "Btn_ContinueStory", "DemonLordAnnouncerWidget" };
            foreach (var name in legacyButtons)
            {
                Transform t = modeRoot.Find(name);
                if (t != null)
                {
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }
        }

        public void EnsurePauseModal()
        {
            if (_pauseModal == null)
            {
                Transform existing = transform.Find("Modal_Pause");
                if (existing != null)
                {
                    _pauseModal = existing.gameObject;
                }
                else
                {
                    _pauseModal = BuildPauseModal();
                }
            }

            if (_pauseModal != null)
            {
                if (_btnPauseResume == null) _btnPauseResume = FindButtonRecursive(_pauseModal, "Btn_Resume", "Btn_PauseResume", "Button_Resume");
                if (_btnPauseRestart == null) _btnPauseRestart = FindButtonRecursive(_pauseModal, "Btn_Restart", "Btn_RestartMatch", "Button_Restart");
                if (_btnPauseSettings == null) _btnPauseSettings = FindButtonRecursive(_pauseModal, "Btn_Settings", "Btn_PauseSettings", "Button_Settings");
                if (_btnPauseExitToMenu == null) _btnPauseExitToMenu = FindButtonRecursive(_pauseModal, "Btn_ExitToMenu", "Btn_PauseExit", "Button_Exit");

                if (_btnPauseResume != null)
                {
                    _btnPauseResume.onClick.RemoveListener(OnClickResumeBattle);
                    _btnPauseResume.onClick.AddListener(OnClickResumeBattle);
                }
                if (_btnPauseRestart != null)
                {
                    _btnPauseRestart.onClick.RemoveListener(OnClickRestartMatch);
                    _btnPauseRestart.onClick.AddListener(OnClickRestartMatch);
                }
                if (_btnPauseSettings != null)
                {
                    _btnPauseSettings.onClick.RemoveListener(OnClickOpenSettings);
                    _btnPauseSettings.onClick.AddListener(OnClickOpenSettings);
                }
                if (_btnPauseExitToMenu != null)
                {
                    _btnPauseExitToMenu.onClick.RemoveListener(OnClickExitBattleToMenu);
                    _btnPauseExitToMenu.onClick.AddListener(OnClickExitBattleToMenu);
                }

                _pauseModal.SetActive(false);
            }
        }

        private GameObject BuildPauseModal()
        {
            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            GameObject modal = new GameObject("Modal_Pause", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modal.transform.SetParent(transform, false);
            RectTransform rt = modal.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = modal.GetComponent<Image>();
            if (atlas != null) atlas.StylePanel(img, false);
            else img.color = new Color(0.05f, 0.05f, 0.08f, 0.94f);

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Title
            GameObject titleObj = new GameObject("Text_PauseTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(modal.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 140f);
            trt.sizeDelta = new Vector2(500f, 60f);
            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.text = "COMBAT SUSPENDED";
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = SumiStyleTokens.PaperWhite;

            // Subtitle
            GameObject subObj = new GameObject("Text_PauseSub", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subObj.transform.SetParent(modal.transform, false);
            RectTransform srt = subObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, 90f);
            srt.sizeDelta = new Vector2(500f, 30f);
            Text subTxt = subObj.GetComponent<Text>();
            subTxt.font = font;
            subTxt.text = "SYSTEM FROZEN // SELECT ACTION";
            subTxt.fontSize = 15;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = SumiStyleTokens.ParryGold;

            // Buttons — Bloodlines styled
            _btnPauseResume = CreateModalButton(modal.transform, "Btn_Resume", "RESUME MATCH", new Vector2(0f, 25f), new Vector2(280f, 48f), SumiStyleTokens.ToriiRed, OnClickResumeBattle, font);
            if (atlas != null) atlas.StyleButton(_btnPauseResume, 1, true);

            _btnPauseRestart = CreateModalButton(modal.transform, "Btn_Restart", "RESTART MATCH", new Vector2(0f, -35f), new Vector2(280f, 48f), SumiStyleTokens.CarbonInkWash, OnClickRestartMatch, font);
            if (atlas != null) atlas.StyleButton(_btnPauseRestart, 2, false);

            _btnPauseSettings = CreateModalButton(modal.transform, "Btn_Settings", "SYSTEM SETTINGS", new Vector2(0f, -95f), new Vector2(280f, 48f), SumiStyleTokens.CarbonInkWash, OnClickOpenSettings, font);
            if (atlas != null) atlas.StyleButton(_btnPauseSettings, 2, false);

            _btnPauseExitToMenu = CreateModalButton(modal.transform, "Btn_ExitToMenu", "EXIT TO MENU", new Vector2(0f, -155f), new Vector2(280f, 48f), SumiStyleTokens.CarbonInkWash, OnClickExitBattleToMenu, font);
            if (atlas != null) atlas.StyleButton(_btnPauseExitToMenu, 3, false);

            return modal;
        }

        private Button CreateModalButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = color;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = color * 1.25f;
            cb.pressedColor = color * 0.75f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            Text txt = txtObj.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private Button FindButtonRecursive(GameObject root, params string[] candidateNames)
        {
            if (root == null) return null;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                foreach (var name in candidateNames)
                {
                    if (string.Equals(b.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return b;
                    }
                }
            }
            return null;
        }

        public void EnsurePvPLobbyModal()
        {
            if (_pvpLobbyModal == null)
            {
                Transform existing = transform.Find("Modal_PvPLobby");
                if (existing != null)
                {
                    _pvpLobbyModal = existing.gameObject;
                }
                else
                {
                    var found = FindAnyObjectByType<PvPLobbyUI>(FindObjectsInactive.Include);
                    if (found != null)
                    {
                        _pvpLobbyModal = found.gameObject;
                    }
                    else
                    {
                        _pvpLobbyModal = BuildPvPLobbyModal();
                    }
                }
            }

            if (_pvpLobbyModal != null)
            {
                var pvpUi = _pvpLobbyModal.GetComponent<PvPLobbyUI>();
                if (pvpUi == null)
                {
                    pvpUi = _pvpLobbyModal.AddComponent<PvPLobbyUI>();
                }
                pvpUi.EnsureLobbyUI();
            }
        }

        private GameObject BuildPvPLobbyModal()
        {
            GameObject modal = new GameObject("Modal_PvPLobby", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PvPLobbyUI));
            modal.transform.SetParent(transform, false);
            RectTransform rt = modal.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = modal.GetComponent<Image>();
            img.color = new Color(0.04f, 0.05f, 0.07f, 0.95f);

            var pvpUi = modal.GetComponent<PvPLobbyUI>();
            if (pvpUi != null) pvpUi.EnsureLobbyUI();

            modal.SetActive(false);
            return modal;
        }

        public void ToggleLocalPause(bool pause)
        {
            IsLocalPaused = pause;
            if (_pauseModal == null)
            {
                EnsurePauseModal();
            }

            if (_pauseModal != null)
            {
                _pauseModal.SetActive(pause);
            }

            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.PauseSimulation(pause);
                Time.timeScale = pause ? 0f : 1f;
            }
            else
            {
                Time.timeScale = pause ? 0f : 1f;
            }
        }

        public void OnClickRestartMatch()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            ToggleLocalPause(false);
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StartMatch();
            }
        }

        public void OnClickResumeBattle()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            ToggleLocalPause(false);
        }

        public void OnClickExitBattleToMenu()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            ToggleLocalPause(false);
            if (DemonIdol.Launch.DemonIdolGameFlowController.Instance != null)
            {
                DemonIdol.Launch.DemonIdolGameFlowController.Instance.ExitModeWithEndingVideo();
            }
            else
            {
                TransitionToModeSelect();
            }
        }

        private void UnpauseIfPaused()
        {
            if (IsLocalPaused)
            {
                ToggleLocalPause(false);
            }
        }

        #endregion

        #region User Actions & Mode Callbacks

        public void OnClickModePvP()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            EnsurePvPLobbyModal();
            if (_pvpLobbyModal != null)
            {
                _pvpLobbyModal.SetActive(true);
                CurrentState = MenuScreenState.PvPLobby;
                var pvpUi = _pvpLobbyModal.GetComponent<PvPLobbyUI>();
                if (pvpUi != null)
                {
                    pvpUi.EnsureLobbyUI();
                    pvpUi.ShowPanel(pvpUi.MainLobbyPanel);
                }
            }
        }

        public void OnClickModeArcade()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            TransitionToCharacterSelect("Arcade");
        }

        public void OnClickModeLocalVersus()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            TransitionToCharacterSelect("LocalVersus");
        }

        void ShowModeNotice(string message)
        {
            if (_modeSelectPanel == null)
            {
                Debug.Log(message);
                return;
            }
            Transform existing = _modeSelectPanel.transform.Find("Text_ModeNotice");
            Text label;
            if (existing == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                var go = new GameObject("Text_ModeNotice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(_modeSelectPanel.transform, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, -180f);
                rect.sizeDelta = new Vector2(760f, 70f);
                label = go.GetComponent<Text>();
                label.font = font;
                label.fontSize = 18;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(1f, 0.85f, 0.45f, 1f);
            }
            else
            {
                label = existing.GetComponent<Text>();
            }
            if (label != null)
                label.text = message;
        }

        Button CreateModeButton(Transform parent, string name, string caption, Vector2 position)
        {
            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(650f, 56f);
            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            bool isRed = name.Contains("Arcade") || name.Contains("PvP") || name.Contains("Story") || name.Contains("LocalVersus");
            if (atlas != null) atlas.StyleButton(button, 1, isRed);
            else image.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(go.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = caption;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return button;
        }

        public void OnClickModeStory()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (StoryCheckpointManager.Instance != null)
            {
                StoryCheckpointManager.Instance.StartCurrentCheckpoint(() =>
                {
                    TransitionToCharacterSelect("Story");
                });
            }
            else
            {
                TransitionToCharacterSelect("Story");
            }
        }

        public void OnClickContinueStory()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (StoryCheckpointManager.Instance != null)
            {
                StoryCheckpointManager.Instance.StartCurrentCheckpoint(() =>
                {
                    TransitionToCharacterSelect("Story");
                });
            }
            else
            {
                TransitionToCharacterSelect("Story");
            }
        }

        public void OnClickModeTraining()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            LaunchDirectTraining1vCPU();
        }

        public void LaunchDirectTraining1vCPU()
        {
            var p1 = CharacterRegistry.GetCharacterById("C01");
            var p2 = CharacterRegistry.GetCharacterById("C02");
            var stage = StageRegistry.GetStageById("ST01");

            _activeSelectedMode = "Training";
            TransitionToFight(stage);
        }

        public void OnClickModeLessons()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            // Structured Onboarding Curriculum
            TransitionToCharacterSelect("Tutorial");
        }

        public void OnClickOpenSettings()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_settingsModal != null)
            {
                _settingsModal.SetActive(true);
                SettingsPanel.Instance?.SyncUIFromData();
            }
        }

        public void OnClickModeShowcase()
        {
            if (!CheckDebounce()) return;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            SceneManager.LoadScene("RosterShowcase");
        }

        public void OnClickCloseSettingsModal()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_settingsModal != null) _settingsModal.SetActive(false);
        }

        public void OnClickClosePvPModal()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_pvpLobbyModal != null) _pvpLobbyModal.SetActive(false);
            if (CurrentState == MenuScreenState.PvPLobby)
            {
                CurrentState = MenuScreenState.ModeSelect;
            }
        }

        private void UpdateContinueStoryButton()
        {
            if (_btnContinueStory == null) return;

            int checkpoint = StoryCheckpointManager.Instance != null ? StoryCheckpointManager.Instance.CurrentCheckpointIndex : 0;
            bool hasProgress = checkpoint > 0;

            _btnContinueStory.interactable = hasProgress;
            if (_continueStoryLabel != null)
            {
                _continueStoryLabel.text = hasProgress ? $"CONTINUE (ACT {checkpoint + 1})" : "CONTINUE (NO SAVE)";
                _continueStoryLabel.color = hasProgress ? SumiStyleTokens.ParryGold : SumiStyleTokens.DisabledInk;
            }
        }

        #endregion
    }
}
