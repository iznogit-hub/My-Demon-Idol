using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DemonIdol.Cutscenes;
using DemonIdol.UI;
using DemonIdol.Combat;
using DemonIdol.Network;

namespace DemonIdol.Launch
{
    public enum GameFlowState
    {
        StudioSplash,
        GameIntroVideo,
        StartTitleScreen,
        TutorialAutoplay,
        ModeSelect,
        ModeInterstitialLoading,
        InActiveMode,
        ModeExitEndingVideo
    }

    /// <summary>
    /// Master Game Flow Controller for Demon Idol:
    /// 1. Studio Splash ("Famous Web Interactive Studio")
    /// 2. Demon Idol Intro MP4 Movie
    /// 3. Title Screen with pulsing Start Button
    /// 4. 10-Second High-Octane Autoplay Tutorial (How to Hit, How to Block, How to Hit Super)
    /// 5. Mode Select (Online Mode, Story Mode, Training Dojo, Options)
    /// 6. Mode Loading Interstitial Video transitions
    /// 7. Mode Exit Ending Video -> takes user back to Start Button screen.
    /// </summary>
    public class DemonIdolGameFlowController : MonoBehaviour
    {
        private static DemonIdolGameFlowController _instance;
        public static DemonIdolGameFlowController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DemonIdolGameFlowController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DemonIdolGameFlowController");
                        _instance = go.AddComponent<DemonIdolGameFlowController>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        public GameFlowState CurrentState { get; private set; } = GameFlowState.StudioSplash;

        private Canvas _flowCanvas;
        private CanvasGroup _studioSplashGroup;
        private CanvasGroup _titleScreenGroup;
        private CanvasGroup _tutorialGroup;
        private CanvasGroup _modeSelectGroup;
        private CanvasGroup _interstitialGroup;

        // Tutorial UI elements
        private Slider _tutorialProgressBar;
        private Text _tutorialPhaseTitle;
        private Text _tutorialPhasePrompt;
        private Text _tutorialPhaseDetails;
        private Image _tutorialVisualIcon;
        private Button _btnSkipTutorial;

        // Mode Select elements
        private Button _btnOnlineMode;
        private Button _btnStoryMode;
        private Button _btnTrainingMode;
        private Button _btnOptions;
        private Button _btnCredits;
        private Button _btnBackToTitle;

        // Interstitial elements
        private Text _interstitialTipText;
        private Slider _interstitialProgressBar;
        private Button _btnSkipInterstitial;

        private Coroutine _activeFlowRoutine;
        private string _pendingTargetMode = "Training";

        private static readonly string[] CombatTips = new[]
        {
            "TIP: Hold Back [←] to Guard against standing high and mid attacks!",
            "TIP: Hold Down-Back [↙] to Crouch Guard against low sweep kicks!",
            "TIP: Normal attacks can be canceled into Special Moves on hit contact!",
            "TIP: Super Moves (Domain Expansion) possess full invincible frames on startup!",
            "TIP: Press Light + Heavy simultaneously to break out of enemy command grabs!",
            "TIP: Pushback distance is calibrated to 3rd Strike standards for precise footsies!"
        };

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureFlowCanvas();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(RunMasterGameFlowSequence());
            }
        }

        #region Master Flow Orchestration

        public void StartGameFlow()
        {
            if (_activeFlowRoutine != null) return; // Prevent duplicate concurrent flow runs
            _activeFlowRoutine = StartCoroutine(RunMasterGameFlowSequence());
        }

        private IEnumerator RunMasterGameFlowSequence()
        {
            EnsureFlowCanvas();
            if (_flowCanvas != null) _flowCanvas.gameObject.SetActive(true);

            // Directly boot to Title / Start Button Screen - zero video delays, zero MP4s
            yield return StartCoroutine(ShowTitleStartScreen());
        }

        /// <summary>
        /// Phase 1: Displays "Famous Web Interactive Studio" splash screen with ultra-fast 0.8s minimalist fade.
        /// </summary>
        private IEnumerator PlayStudioSplashScreen()
        {
            CurrentState = GameFlowState.StudioSplash;
            HideAllPanels();

            if (_studioSplashGroup != null)
            {
                _studioSplashGroup.gameObject.SetActive(true);
                _studioSplashGroup.alpha = 0f;
            }

            bool skipRequested = false;

            // Swift Fade in (0.25s)
            float t = 0f;
            while (t < 0.25f && !skipRequested)
            {
                t += Time.unscaledDeltaTime;
                if (_studioSplashGroup != null) _studioSplashGroup.alpha = Mathf.Clamp01(t / 0.25f);
                if (CheckAnyInput()) skipRequested = true;
                yield return null;
            }
            if (_studioSplashGroup != null) _studioSplashGroup.alpha = 1f;

            // Quick Hold (0.35s)
            float holdTime = 0f;
            while (holdTime < 0.35f && !skipRequested)
            {
                holdTime += Time.unscaledDeltaTime;
                if (CheckAnyInput()) skipRequested = true;
                yield return null;
            }

            // Swift Fade out (0.20s)
            t = 0f;
            while (t < 0.20f && !skipRequested)
            {
                t += Time.unscaledDeltaTime;
                if (_studioSplashGroup != null) _studioSplashGroup.alpha = 1f - Mathf.Clamp01(t / 0.20f);
                yield return null;
            }

            if (_studioSplashGroup != null)
            {
                _studioSplashGroup.alpha = 0f;
                _studioSplashGroup.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Phase 2: Plays Demon Idol Intro MP4 Video.
        /// </summary>
        private IEnumerator PlayGameIntroVideo()
        {
            CurrentState = GameFlowState.GameIntroVideo;
            HideAllPanels();

            bool introFinished = false;
            string videoUrl = ResolveVideoUrl(DemonLordAssetCatalog.INTRO_HAILUO);
            if (string.IsNullOrEmpty(videoUrl) || !VideoExists(videoUrl))
            {
                videoUrl = ResolveVideoUrl(DemonLordAssetCatalog.INTRO_KLING_A);
            }

            if (!string.IsNullOrEmpty(videoUrl) && VideoExists(videoUrl) && CutsceneManager.Instance != null)
            {
                CutsceneManager.Instance.PlayCutsceneFromUrl(
                    videoUrl,
                    onComplete: () => introFinished = true,
                    onFailed: () => introFinished = true,
                    caption: "MY DEMON IDOL: AWAKENING OF THE UNDERWORLD"
                );

                while (!introFinished)
                {
                    yield return null;
                }
            }
            else
            {
                // Instant fallback if video asset cannot be resolved
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        /// <summary>
        /// Phase 3: Shows Title Screen with "MY DEMON IDOL" logo and pulsing "PRESS START TO PLAY" button.
        /// </summary>
        public IEnumerator ShowTitleStartScreen()
        {
            CurrentState = GameFlowState.StartTitleScreen;
            if (_flowCanvas != null) _flowCanvas.gameObject.SetActive(true);
            HideAllPanels();

            if (_titleScreenGroup != null)
            {
                _titleScreenGroup.gameObject.SetActive(true);
                _titleScreenGroup.alpha = 1f;
            }

            bool startPressed = false;
            Button btnStart = _titleScreenGroup != null ? _titleScreenGroup.GetComponentInChildren<Button>(true) : null;
            if (btnStart != null)
            {
                btnStart.onClick.RemoveAllListeners();
                btnStart.onClick.AddListener(() => startPressed = true);
            }

            CanvasGroup pulseGroup = btnStart != null ? btnStart.GetComponent<CanvasGroup>() : null;
            float pulseTimer = 0f;

            while (!startPressed)
            {
                pulseTimer += Time.unscaledDeltaTime * 4.5f;
                if (pulseGroup != null)
                {
                    pulseGroup.alpha = 0.65f + 0.35f * Mathf.Sin(pulseTimer);
                }

                if (CheckAnyInput())
                {
                    startPressed = true;
                }
                yield return null;
            }

            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            // Advance directly to Mode Select (Home / Menu Screen) - zero MP4 delay
            ShowModeSelectScreen();
        }

        /// <summary>
        /// Phase 4: 10-Second High-Octane Autoplay Tutorial:
        /// 1. How to Hit (0s - 3.5s)
        /// 2. How to Block (3.5s - 7.0s)
        /// 3. How to Hit Super (7.0s - 10.5s)
        /// </summary>
        private IEnumerator PlayTutorialAutoplaySequence()
        {
            CurrentState = GameFlowState.TutorialAutoplay;
            HideAllPanels();

            // Play background tutorial movie
            string tutVideoUrl = ResolveVideoUrl(DemonLordAssetCatalog.TUTORIAL_HOW_TO_PLAY);
            if (!string.IsNullOrEmpty(tutVideoUrl) && VideoExists(tutVideoUrl) && CutsceneManager.Instance != null)
            {
                CutsceneManager.Instance.PlayCutsceneFromUrl(tutVideoUrl, null, null, "HOW TO PLAY: 3RD STRIKE COMBAT LESSONS");
            }

            if (_tutorialGroup != null)
            {
                _tutorialGroup.gameObject.SetActive(true);
                _tutorialGroup.alpha = 1f;
            }

            bool skipped = false;
            if (_btnSkipTutorial != null)
            {
                _btnSkipTutorial.onClick.RemoveAllListeners();
                _btnSkipTutorial.onClick.AddListener(() => skipped = true);
            }

            float duration = 10.5f;
            float elapsed = 0f;

            while (elapsed < duration && !skipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                if (_tutorialProgressBar != null) _tutorialProgressBar.value = progress;

                // Segment 1: How to Hit (0s - 3.5s)
                if (elapsed < 3.5f)
                {
                    if (_tutorialPhaseTitle != null) _tutorialPhaseTitle.text = "COMBAT LESSON 01: HOW TO HIT";
                    if (_tutorialPhasePrompt != null) _tutorialPhasePrompt.text = "LIGHT [ L ]   •   MEDIUM [ M ]   •   HEAVY [ H ]";
                    if (_tutorialPhaseDetails != null) _tutorialPhaseDetails.text = "Chain attacks in order (Light -> Medium -> Heavy) to stagger your opponent into hitstun combos!";
                    if (_tutorialVisualIcon != null) _tutorialVisualIcon.color = new Color(0.95f, 0.25f, 0.20f, 1f); // Torii Red
                }
                // Segment 2: How to Block (3.5s - 7.0s)
                else if (elapsed < 7.0f)
                {
                    if (_tutorialPhaseTitle != null) _tutorialPhaseTitle.text = "COMBAT LESSON 02: HOW TO BLOCK";
                    if (_tutorialPhasePrompt != null) _tutorialPhasePrompt.text = "HOLD BACK [ ← ] TO GUARD ATTACKS";
                    if (_tutorialPhaseDetails != null) _tutorialPhaseDetails.text = "Neutral block guards standing mid/high strikes. Crouch guard [ ↙ ] defends against low sweeps!";
                    if (_tutorialVisualIcon != null) _tutorialVisualIcon.color = new Color(0.20f, 0.75f, 1.0f, 1f); // Barrier Cyan
                }
                // Segment 3: How to Hit Super (7.0s - 10.5s)
                else
                {
                    if (_tutorialPhaseTitle != null) _tutorialPhaseTitle.text = "COMBAT LESSON 03: HOW TO HIT SUPER";
                    if (_tutorialPhasePrompt != null) _tutorialPhasePrompt.text = "FULL GAUGE [ ⚡⚡⚡ ] -> PRESS SUPER / DOMAIN";
                    if (_tutorialPhaseDetails != null) _tutorialPhaseDetails.text = "When gauge reaches 100%, trigger your character's ultimate cinematic Domain Expansion Astral Finish!";
                    if (_tutorialVisualIcon != null) _tutorialVisualIcon.color = new Color(1.0f, 0.85f, 0.20f, 1f); // Parry Gold
                }

                yield return null;
            }

            if (_tutorialGroup != null)
            {
                _tutorialGroup.alpha = 0f;
                _tutorialGroup.gameObject.SetActive(false);
            }

            // Advance to Mode Select
            ShowModeSelectScreen();
        }

        /// <summary>
        /// Phase 5: Displays Mode Select Screen with Online Mode, Story Mode, Training Dojo, and Options.
        /// </summary>
        public void ShowModeSelectScreen()
        {
            CurrentState = GameFlowState.ModeSelect;
            HideAllPanels();

            // Delegate 100% to Master UI Canvas Mode Select if present
            if (BloodTechUIManager.Instance != null)
            {
                if (_flowCanvas != null) _flowCanvas.gameObject.SetActive(false);
                BloodTechUIManager.Instance.TransitionToModeSelect();
                return;
            }

            if (_modeSelectGroup != null)
            {
                _modeSelectGroup.gameObject.SetActive(true);
                _modeSelectGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Phase 6: Mode Transition (zero MP4 delay).
        /// </summary>
        public void LaunchModeWithInterstitial(string mode)
        {
            _pendingTargetMode = mode;
            if (_activeFlowRoutine != null) StopCoroutine(_activeFlowRoutine);
            HideAllPanels();
            CurrentState = GameFlowState.InActiveMode;
            ExecuteModeTransition(mode);
        }

        private void ExecuteModeTransition(string mode)
        {
            if (string.Equals(mode, "Training", StringComparison.OrdinalIgnoreCase))
            {
                if (BloodTechUIManager.Instance != null)
                {
                    BloodTechUIManager.Instance.OnClickModeTraining();
                }
                else
                {
                    if (DemonIdol.Combat.HomegrownCombatDirector.Instance != null)
                    {
                        DemonIdol.Combat.HomegrownCombatDirector.Instance.SetMatchContext("C01", "C02", "ST01", true, true);
                        DemonIdol.Combat.HomegrownCombatDirector.Instance.InitializeHomegrownMatch();
                    }
                    if (DemonIdol.Combat.MatchRoundReferee.Instance != null)
                    {
                        DemonIdol.Combat.MatchRoundReferee.Instance.isTrainingMode = true;
                        DemonIdol.Combat.MatchRoundReferee.Instance.StartMatch();
                    }
                }
            }
            else if (string.Equals(mode, "Online", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "PvP", StringComparison.OrdinalIgnoreCase))
            {
                if (BloodTechUIManager.Instance != null)
                {
                    BloodTechUIManager.Instance.OnClickModePvP();
                }
            }
            else if (string.Equals(mode, "Story", StringComparison.OrdinalIgnoreCase))
            {
                if (BloodTechUIManager.Instance != null)
                {
                    BloodTechUIManager.Instance.OnClickModeStory();
                }
            }
        }

        /// <summary>
        /// Phase 7: Mode Exit Flow (zero MP4 delay).
        /// Navigates user straight to Title / Start Button screen cleanly.
        /// </summary>
        public void ExitModeWithEndingVideo()
        {
            if (_activeFlowRoutine != null) StopCoroutine(_activeFlowRoutine);
            // End any running match cleanly
            if (DemonIdol.Combat.MatchRoundReferee.Instance != null)
            {
                DemonIdol.Combat.MatchRoundReferee.Instance.StopMatch();
            }
            StartCoroutine(ShowTitleStartScreen());
        }

        #endregion

        #region Dynamic UI Generation & Hierarchy

        private void EnsureFlowCanvas()
        {
            if (_flowCanvas != null) return;

            GameObject canvasGo = GameObject.Find("DemonIdol_GameFlow_Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("DemonIdol_GameFlow_Canvas");
                if (Application.isPlaying) DontDestroyOnLoad(canvasGo);
            }

            _flowCanvas = canvasGo.GetComponent<Canvas>();
            if (_flowCanvas == null) _flowCanvas = canvasGo.AddComponent<Canvas>();
            _flowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _flowCanvas.sortingOrder = 200; // Above master canvas

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Studio Splash Panel
            _studioSplashGroup = BuildStudioSplashPanel(_flowCanvas.transform, font);

            // 2. Title Screen Panel
            _titleScreenGroup = BuildTitleScreenPanel(_flowCanvas.transform, font);

            // 3. Tutorial Autoplay Panel
            _tutorialGroup = BuildTutorialPanel(_flowCanvas.transform, font);

            // 4. Mode Select Panel
            _modeSelectGroup = BuildModeSelectPanel(_flowCanvas.transform, font);

            // 5. Interstitial Loading Panel
            _interstitialGroup = BuildInterstitialPanel(_flowCanvas.transform, font);

            HideAllPanels();
        }

        private CanvasGroup BuildStudioSplashPanel(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_StudioSplash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 1f);

            // Studio Title Text
            GameObject titleObj = new GameObject("Text_StudioName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 25f);
            trt.sizeDelta = new Vector2(1200f, 80f);
            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.text = "FAMOUS WEB INTERACTIVE STUDIO";
            titleTxt.fontSize = 44;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.95f, 0.82f, 0.35f, 1f); // Gold

            // Accent Divider Line
            GameObject dividerObj = new GameObject("Divider_Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dividerObj.transform.SetParent(panel.transform, false);
            RectTransform drt = dividerObj.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, -12f);
            drt.sizeDelta = new Vector2(360f, 2f);
            dividerObj.GetComponent<Image>().color = new Color(0.95f, 0.82f, 0.35f, 0.65f); // Subtle gold accent

            // Subtitle
            GameObject subObj = new GameObject("Text_StudioSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subObj.transform.SetParent(panel.transform, false);
            RectTransform srt = subObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, -40f);
            srt.sizeDelta = new Vector2(800f, 40f);
            Text subTxt = subObj.GetComponent<Text>();
            subTxt.font = font;
            subTxt.text = "— ENTERTAINMENT REDEFINED —";
            subTxt.fontSize = 20;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.8f, 0.85f, 0.95f, 0.75f);

            return panel.GetComponent<CanvasGroup>();
        }

        private CanvasGroup BuildTitleScreenPanel(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_TitleScreen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.98f);

            // Title Accent Line (Top)
            GameObject topRule = new GameObject("Title_TopRule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            topRule.transform.SetParent(panel.transform, false);
            RectTransform trtRule = topRule.GetComponent<RectTransform>();
            trtRule.anchoredPosition = new Vector2(0f, 255f);
            trtRule.sizeDelta = new Vector2(400f, 3f);
            topRule.GetComponent<Image>().color = new Color(0.92f, 0.22f, 0.20f, 0.85f); // Crimson Accent

            // Title
            GameObject titleObj = new GameObject("Text_GameTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 180f);
            trt.sizeDelta = new Vector2(1100f, 120f);
            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.text = "MY DEMON IDOL";
            titleTxt.fontSize = 78;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.92f, 0.22f, 0.20f, 1f); // Torii Crimson Red

            // Subtitle
            GameObject subObj = new GameObject("Text_Subtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subObj.transform.SetParent(panel.transform, false);
            RectTransform srt = subObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, 105f);
            srt.sizeDelta = new Vector2(900f, 40f);
            Text subTxt = subObj.GetComponent<Text>();
            subTxt.font = font;
            subTxt.text = "TACTICAL ANIME FIGHTER // DETERMINISTIC COMBAT MATRIX";
            subTxt.fontSize = 20;
            subTxt.fontStyle = FontStyle.Bold;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.95f, 0.85f, 0.35f, 0.95f); // Gold

            // Start Button Box
            GameObject btnObj = new GameObject("Btn_StartGame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
            btnObj.transform.SetParent(panel.transform, false);
            RectTransform brt = btnObj.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(0f, -80f);
            brt.sizeDelta = new Vector2(500f, 80f);
            btnObj.GetComponent<Image>().color = new Color(0.85f, 0.18f, 0.18f, 0.95f);

            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            StretchFull(btnTxtObj.GetComponent<RectTransform>());
            Text btnTxt = btnTxtObj.GetComponent<Text>();
            btnTxt.font = font;
            btnTxt.text = "▶ PRESS START TO PLAY";
            btnTxt.fontSize = 28;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;

            // Prompt
            GameObject promptObj = new GameObject("Text_Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            promptObj.transform.SetParent(panel.transform, false);
            RectTransform prt = promptObj.GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -150f);
            prt.sizeDelta = new Vector2(800f, 35f);
            Text pTxt = promptObj.GetComponent<Text>();
            pTxt.font = font;
            pTxt.text = "[ TAP SCREEN • CLICK • PRESS ANY KEY ]";
            pTxt.fontSize = 16;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = new Color(0.25f, 0.85f, 0.95f, 0.85f); // Radianite Cyan

            // Footer
            GameObject footerObj = new GameObject("Text_Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            footerObj.transform.SetParent(panel.transform, false);
            RectTransform frt = footerObj.GetComponent<RectTransform>();
            frt.anchoredPosition = new Vector2(0f, -440f);
            frt.sizeDelta = new Vector2(1000f, 30f);
            Text fTxt = footerObj.GetComponent<Text>();
            fTxt.font = font;
            fTxt.text = "VER 2.7.0 // 120 FPS PROMOTION // 60HZ DETERMINISTIC ROLLBACK ENGINE";
            fTxt.fontSize = 13;
            fTxt.alignment = TextAnchor.MiddleCenter;
            fTxt.color = new Color(0.55f, 0.60f, 0.70f, 0.75f);

            return panel.GetComponent<CanvasGroup>();
        }

        private CanvasGroup BuildTutorialPanel(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_TutorialAutoplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.98f);

            // Top Header
            GameObject headerObj = new GameObject("Text_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            headerObj.transform.SetParent(panel.transform, false);
            RectTransform hrt = headerObj.GetComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(0f, 380f);
            hrt.sizeDelta = new Vector2(1000f, 60f);
            Text hTxt = headerObj.GetComponent<Text>();
            hTxt.font = font;
            hTxt.text = "COMBAT ACADEMY // 10-SECOND QUICK START";
            hTxt.fontSize = 28;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = new Color(0.95f, 0.85f, 0.35f, 1f);

            // Phase Title
            GameObject titleObj = new GameObject("Text_PhaseTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 220f);
            trt.sizeDelta = new Vector2(1200f, 70f);
            _tutorialPhaseTitle = titleObj.GetComponent<Text>();
            _tutorialPhaseTitle.font = font;
            _tutorialPhaseTitle.fontSize = 42;
            _tutorialPhaseTitle.fontStyle = FontStyle.Bold;
            _tutorialPhaseTitle.alignment = TextAnchor.MiddleCenter;
            _tutorialPhaseTitle.color = Color.white;

            // Visual Center Icon / Emblem
            GameObject iconObj = new GameObject("Image_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(panel.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(0f, 70f);
            irt.sizeDelta = new Vector2(160f, 160f);
            _tutorialVisualIcon = iconObj.GetComponent<Image>();
            _tutorialVisualIcon.color = new Color(0.92f, 0.22f, 0.20f, 1f);

            // Phase Prompt
            GameObject promptObj = new GameObject("Text_PhasePrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            promptObj.transform.SetParent(panel.transform, false);
            RectTransform prt = promptObj.GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -60f);
            prt.sizeDelta = new Vector2(1200f, 60f);
            _tutorialPhasePrompt = promptObj.GetComponent<Text>();
            _tutorialPhasePrompt.font = font;
            _tutorialPhasePrompt.fontSize = 32;
            _tutorialPhasePrompt.fontStyle = FontStyle.Bold;
            _tutorialPhasePrompt.alignment = TextAnchor.MiddleCenter;
            _tutorialPhasePrompt.color = new Color(0.2f, 0.85f, 1.0f, 1f);

            // Phase Details
            GameObject detObj = new GameObject("Text_PhaseDetails", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            detObj.transform.SetParent(panel.transform, false);
            RectTransform drt = detObj.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, -140f);
            drt.sizeDelta = new Vector2(1000f, 80f);
            _tutorialPhaseDetails = detObj.GetComponent<Text>();
            _tutorialPhaseDetails.font = font;
            _tutorialPhaseDetails.fontSize = 22;
            _tutorialPhaseDetails.alignment = TextAnchor.MiddleCenter;
            _tutorialPhaseDetails.color = new Color(0.9f, 0.9f, 0.95f, 0.85f);

            // Progress Bar
            GameObject barObj = new GameObject("ProgressBar_Tutorial", typeof(RectTransform), typeof(Slider));
            barObj.transform.SetParent(panel.transform, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0f, -280f);
            barRt.sizeDelta = new Vector2(800f, 18f);
            _tutorialProgressBar = barObj.GetComponent<Slider>();
            _tutorialProgressBar.minValue = 0f;
            _tutorialProgressBar.maxValue = 1f;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barObj.transform, false);
            StretchFull(fillArea.GetComponent<RectTransform>());

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            StretchFull(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.95f, 0.85f, 0.35f, 1f);
            _tutorialProgressBar.fillRect = fill.GetComponent<RectTransform>();

            // Skip Tutorial Button
            GameObject skipObj = new GameObject("Btn_SkipTutorial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skipObj.transform.SetParent(panel.transform, false);
            RectTransform srt = skipObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(740f, 440f);
            srt.sizeDelta = new Vector2(240f, 50f);
            skipObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.85f);
            _btnSkipTutorial = skipObj.GetComponent<Button>();

            GameObject sTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            sTxtObj.transform.SetParent(skipObj.transform, false);
            StretchFull(sTxtObj.GetComponent<RectTransform>());
            Text sTxt = sTxtObj.GetComponent<Text>();
            sTxt.font = font;
            sTxt.text = "SKIP TUTORIAL [>>]";
            sTxt.fontSize = 18;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = Color.white;

            return panel.GetComponent<CanvasGroup>();
        }

        private CanvasGroup BuildModeSelectPanel(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_FlowModeSelect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.98f);

            // Header
            GameObject headerObj = new GameObject("Text_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            headerObj.transform.SetParent(panel.transform, false);
            RectTransform hrt = headerObj.GetComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(0f, 380f);
            hrt.sizeDelta = new Vector2(1000f, 60f);
            Text hTxt = headerObj.GetComponent<Text>();
            hTxt.font = font;
            hTxt.text = "SELECT BATTLE DISCIPLINE";
            hTxt.fontSize = 36;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = new Color(0.95f, 0.85f, 0.35f, 1f);

            // 3 Mode Cards: Online, Story, Training
            _btnOnlineMode = CreateModeCard(panel.transform, "Card_Online", "ONLINE MODE", "Ranked Matches • Casual Lobbies • Global Netplay", new Vector2(-480f, 40f), new Color(0.18f, 0.45f, 0.75f, 0.95f), () => LaunchModeWithInterstitial("Online"), font);
            _btnStoryMode = CreateModeCard(panel.transform, "Card_Story", "STORY MODE", "Demon Lord Campaign • Underworld Infiltration", new Vector2(0f, 40f), new Color(0.72f, 0.20f, 0.22f, 0.95f), () => LaunchModeWithInterstitial("Story"), font);
            _btnTrainingMode = CreateModeCard(panel.transform, "Card_Training", "TRAINING DOJO", "Practice Stances • Frame Data • Move Lists", new Vector2(480f, 40f), new Color(0.20f, 0.65f, 0.40f, 0.95f), () => LaunchModeWithInterstitial("Training"), font);

            // Bottom Buttons
            _btnOptions = CreateNavButton(panel.transform, "Btn_Options", "SETTINGS & OPTIONS", new Vector2(-220f, -340f), () => SettingsPanel.Instance?.Open(), font);
            _btnCredits = CreateNavButton(panel.transform, "Btn_Credits", "CREDITS", new Vector2(220f, -340f), () => CreditsUI.Instance?.Open(), font);
            _btnBackToTitle = CreateNavButton(panel.transform, "Btn_BackToTitle", "RETURN TO START", new Vector2(0f, -420f), () => StartCoroutine(ShowTitleStartScreen()), font);

            return panel.GetComponent<CanvasGroup>();
        }

        private Button CreateModeCard(Transform parent, string name, string title, string desc, Vector2 pos, Color accentColor, Action onClick, Font font)
        {
            GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(parent, false);
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(420f, 440f);
            cardObj.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.95f);
            Button btn = cardObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Accent Top Banner
            GameObject bannerObj = new GameObject("AccentBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bannerObj.transform.SetParent(cardObj.transform, false);
            RectTransform brt = bannerObj.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(0f, 195f);
            brt.sizeDelta = new Vector2(420f, 50f);
            bannerObj.GetComponent<Image>().color = accentColor;

            // Title
            GameObject titleObj = new GameObject("Text_Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 90f);
            trt.sizeDelta = new Vector2(380f, 60f);
            Text tTxt = titleObj.GetComponent<Text>();
            tTxt.font = font;
            tTxt.text = title;
            tTxt.fontSize = 30;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = Color.white;

            // Description
            GameObject descObj = new GameObject("Text_Desc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform drt = descObj.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, -40f);
            drt.sizeDelta = new Vector2(360f, 120f);
            Text dTxt = descObj.GetComponent<Text>();
            dTxt.font = font;
            dTxt.text = desc;
            dTxt.fontSize = 20;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = new Color(0.85f, 0.90f, 0.95f, 0.8f);

            // Enter Button Pill
            GameObject pillObj = new GameObject("Pill_Enter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pillObj.transform.SetParent(cardObj.transform, false);
            RectTransform prt = pillObj.GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -160f);
            prt.sizeDelta = new Vector2(260f, 50f);
            pillObj.GetComponent<Image>().color = accentColor;

            GameObject pTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            pTxtObj.transform.SetParent(pillObj.transform, false);
            StretchFull(pTxtObj.GetComponent<RectTransform>());
            Text pTxt = pTxtObj.GetComponent<Text>();
            pTxt.font = font;
            pTxt.text = "ENTER MODE >>";
            pTxt.fontSize = 18;
            pTxt.fontStyle = FontStyle.Bold;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = Color.white;

            return btn;
        }

        private Button CreateNavButton(Transform parent, string name, string label, Vector2 pos, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 55f);
            btnObj.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            StretchFull(txtObj.GetComponent<RectTransform>());
            Text txt = txtObj.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private CanvasGroup BuildInterstitialPanel(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_ModeInterstitial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.98f);

            // Tip Text
            GameObject tipObj = new GameObject("Text_Tip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            tipObj.transform.SetParent(panel.transform, false);
            RectTransform trt = tipObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, -220f);
            trt.sizeDelta = new Vector2(1200f, 60f);
            _interstitialTipText = tipObj.GetComponent<Text>();
            _interstitialTipText.font = font;
            _interstitialTipText.text = "TIP: Hold Back [←] to Guard against standing high and mid attacks!";
            _interstitialTipText.fontSize = 24;
            _interstitialTipText.alignment = TextAnchor.MiddleCenter;
            _interstitialTipText.color = new Color(0.95f, 0.85f, 0.35f, 1f);

            // Progress Bar
            GameObject barObj = new GameObject("ProgressBar_Interstitial", typeof(RectTransform), typeof(Slider));
            barObj.transform.SetParent(panel.transform, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0f, -300f);
            barRt.sizeDelta = new Vector2(800f, 18f);
            _interstitialProgressBar = barObj.GetComponent<Slider>();
            _interstitialProgressBar.minValue = 0f;
            _interstitialProgressBar.maxValue = 1f;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barObj.transform, false);
            StretchFull(fillArea.GetComponent<RectTransform>());

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            StretchFull(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.92f, 0.22f, 0.20f, 1f);
            _interstitialProgressBar.fillRect = fill.GetComponent<RectTransform>();

            // Skip Button
            GameObject skipObj = new GameObject("Btn_SkipInterstitial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skipObj.transform.SetParent(panel.transform, false);
            RectTransform srt = skipObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(740f, 440f);
            srt.sizeDelta = new Vector2(240f, 50f);
            skipObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.85f);
            _btnSkipInterstitial = skipObj.GetComponent<Button>();

            GameObject sTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            sTxtObj.transform.SetParent(skipObj.transform, false);
            StretchFull(sTxtObj.GetComponent<RectTransform>());
            Text sTxt = sTxtObj.GetComponent<Text>();
            sTxt.font = font;
            sTxt.text = "CONTINUE >>";
            sTxt.fontSize = 18;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = Color.white;

            return panel.GetComponent<CanvasGroup>();
        }

        private void HideAllPanels()
        {
            if (_studioSplashGroup != null) _studioSplashGroup.gameObject.SetActive(false);
            if (_titleScreenGroup != null) _titleScreenGroup.gameObject.SetActive(false);
            if (_tutorialGroup != null) _tutorialGroup.gameObject.SetActive(false);
            if (_modeSelectGroup != null) _modeSelectGroup.gameObject.SetActive(false);
            if (_interstitialGroup != null) _interstitialGroup.gameObject.SetActive(false);
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private bool CheckAnyInput()
        {
            try
            {
                if (UnityEngine.Input.anyKeyDown && !UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    return true;
                }
            }
            catch (System.InvalidOperationException)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    return true;
                }
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    return true;
                }
#endif
            }
            return false;
        }

        private string ResolveVideoUrl(string relativeAssetPath)
        {
            if (string.IsNullOrEmpty(relativeAssetPath)) return null;

#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, relativeAssetPath);
#else
            string streamingPath = Path.Combine(Application.streamingAssetsPath, relativeAssetPath);
            if (File.Exists(streamingPath)) return "file://" + Path.GetFullPath(streamingPath);

            string fullPath = Path.Combine(Application.dataPath, relativeAssetPath);
            if (File.Exists(fullPath)) return "file://" + Path.GetFullPath(fullPath);

            string projectRel = Path.GetFullPath(Path.Combine("Assets", relativeAssetPath));
            if (File.Exists(projectRel)) return "file://" + projectRel;

            return "file://" + fullPath;
#endif
        }

        private static bool VideoExists(string urlOrPath)
        {
            if (string.IsNullOrEmpty(urlOrPath)) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, jar:file StreamingAssets are packed inside APK and cannot be checked via File.Exists
            if (urlOrPath.StartsWith("jar:", StringComparison.OrdinalIgnoreCase)) return true;
#endif
            string path = urlOrPath;
            if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(7);
            }
            return File.Exists(path);
        }

        #endregion
    }
}
