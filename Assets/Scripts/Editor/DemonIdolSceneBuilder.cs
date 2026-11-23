#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;
using DemonIdol.Launch;
using DemonIdol.UI;
using DemonIdol.Audio;
using DemonIdol.BlazBlue;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master idempotent scene builder for Demon Idol:
    /// 1. Assets/Scenes/MainMenu.unity
    /// 2. Assets/Scenes/CharacterSelect.unity
    /// 3. Assets/Scenes/BattleArena.unity
    /// Enforces:
    /// - Confirmation dialogs for interactive runs
    /// - Automatic legacy backup to Assets/Scenes/_Legacy/<name>_<timestamp>.unity
    /// - Single EventSystem with InputSystemUIInputModule in each scene
    /// - URP lighting and materials
    /// - Clean Match_Host in BattleArena without baked fighters or audio director
    /// </summary>
    public static class DemonIdolSceneBuilder
    {
        public const string SCENE_MAIN_MENU = "Assets/Scenes/MainMenu.unity";
        public const string SCENE_CHAR_SELECT = "Assets/Scenes/CharacterSelect.unity";
        public const string SCENE_BATTLE_ARENA = "Assets/Scenes/BattleArena.unity";
        public const string SCENE_LEGACY_ARCADE = "Assets/Scenes/ArcadeArena.unity";
        public const string LEGACY_DIR = "Assets/Scenes/_Legacy";

        public static bool ConfirmAndBackup(string scenePath, string sceneName, bool interactive, out string backupPath)
        {
            backupPath = null;
            if (interactive)
            {
                if (!EditorUtility.DisplayDialog($"Build {sceneName}",
                    $"Rebuild {sceneName} from scratch?\nExisting scene will be backed up to {LEGACY_DIR}/.",
                    "Build Scene", "Cancel"))
                {
                    return false;
                }
            }

            if (!Directory.Exists(LEGACY_DIR))
            {
                Directory.CreateDirectory(LEGACY_DIR);
            }

            if (File.Exists(scenePath))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backupPath = $"{LEGACY_DIR}/{sceneName}_{timestamp}.unity";
                File.Copy(scenePath, backupPath, true);
                string metaSrc = scenePath + ".meta";
                if (File.Exists(metaSrc))
                {
                    File.Copy(metaSrc, backupPath + ".meta", true);
                }
                AssetDatabase.ImportAsset(backupPath);
                Debug.Log($"<color=#81c784>[DemonIdolSceneBuilder] Backed up {sceneName} to {backupPath}</color>");
            }
            return true;
        }

        public static void BuildAllThreeScenes(bool interactive = true)
        {
            if (interactive)
            {
                if (!EditorUtility.DisplayDialog("Build All Three Scenes",
                    "Rebuild MainMenu, CharacterSelect, and BattleArena from scratch and set build order?\nExisting scenes will be backed up to Assets/Scenes/_Legacy/.",
                    "Build All Three", "Cancel"))
                {
                    return;
                }
            }

            BuildMainMenuScene(false);
            BuildCharacterSelectScene(false);
            BuildBattleArenaScene(false);
            SetBuildOrder();

            Debug.Log("<color=#00e676><b>[DemonIdolSceneBuilder] SUCCESS: All 3 scenes built and Build Settings configured!</b></color>");
        }

        public static void BuildModernMenuScenes(bool interactive = true)
        {
            if (interactive)
            {
                if (!EditorUtility.DisplayDialog("Rebuild Modern Menu Scenes",
                    "Rebuild modern MainMenu and CharacterSelect scenes with AAA Sumi-e UI?\nExisting scenes will be backed up to Assets/Scenes/_Legacy/.",
                    "Rebuild Scenes", "Cancel"))
                {
                    return;
                }
            }

            BuildMainMenuScene(false);
            BuildCharacterSelectScene(false);
            SetBuildOrder();
            Debug.Log("<color=#00e676><b>[DemonIdolSceneBuilder] SUCCESS: Modern MainMenu and CharacterSelect scenes rebuilt!</b></color>");
        }

        public static void BuildModernMenuScenesHeadless()
        {
            BuildMainMenuScene(false);
            BuildCharacterSelectScene(false);
            SetBuildOrder();
            Debug.Log("[DemonIdolSceneBuilder] SUCCESS: Headless modern menu build complete.");
            EditorApplication.Exit(0);
        }

        public static void SetBuildOrder()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(SCENE_MAIN_MENU, true),
                new EditorBuildSettingsScene(SCENE_CHAR_SELECT, true),
                new EditorBuildSettingsScene(SCENE_BATTLE_ARENA, true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00e676>[DemonIdolSceneBuilder] EditorBuildSettings updated: MainMenu (0), CharacterSelect (1), BattleArena (2).</color>");
        }

        // =========================================================================
        // SCENE 1: MainMenu.unity (Modern AAA Hub with Live 3D Hero & Slide Drawer)
        // =========================================================================
        public static void BuildMainMenuScene(bool interactive = true)
        {
            if (!ConfirmAndBackup(SCENE_MAIN_MENU, "MainMenu", interactive, out _))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Audio Director (single DontDestroyOnLoad audio manager)
            GameObject audioObj = new GameObject("CombatAudio_Manager");
            audioObj.AddComponent<CombatAudioDirector>();

            // 2. Camera & Spatial Lighting
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            camObj.AddComponent<UniversalAdditionalCameraData>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0f, 1.25f, -4.5f);
            camObj.transform.rotation = Quaternion.Euler(4f, -4f, 0f);

            GameObject keyLight = new GameObject("Directional_KeyLight");
            Light kl = keyLight.AddComponent<Light>();
            kl.type = LightType.Directional;
            kl.color = new Color(1f, 0.95f, 0.88f);
            kl.intensity = 1.3f;
            keyLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            GameObject rimLight = new GameObject("Directional_RimLight");
            Light rl = rimLight.AddComponent<Light>();
            rl.type = LightType.Directional;
            rl.color = new Color(0.4f, 0.7f, 1f);
            rl.intensity = 0.85f;
            rimLight.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);

            // 3. Live 3D Hero Pedestal (Right 55% Viewport)
            GameObject heroPedestal = new GameObject("Hero_SpawnPoint");
            heroPedestal.transform.position = new Vector3(1.75f, 0f, 0f);
            heroPedestal.transform.rotation = Quaternion.Euler(0f, -145f, 0f);

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Pedestal_Disc";
            disc.transform.SetParent(heroPedestal.transform, false);
            disc.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            disc.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);
            var discCol = disc.GetComponent<Collider>();
            if (discCol != null) UnityEngine.Object.DestroyImmediate(discCol);

            // 4. Canvas & Modern UI
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<SafeAreaFitter>();

            DemonIdolMainMenuController ctrl = canvasObj.AddComponent<DemonIdolMainMenuController>();
            ctrl.menuCamera = cam;
            ctrl.heroSpawnPoint = heroPedestal.transform;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // =========================================================================
            // TOP PILLAR NAVIGATION BAR
            // =========================================================================
            GameObject headerBar = CreateUIObject("Top_Pillar_Nav", canvasObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(0f, 80f));
            Image headerBg = headerBar.AddComponent<Image>();
            headerBg.color = SumiStyleTokens.CarbonInkWash;

            // Title & Subtitle Left
            GameObject titleObj = CreateUIObject("Title_Text", headerBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, 14f), new Vector2(300f, 36f));
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = defaultFont;
            titleTxt.text = "MY DEMON IDOL";
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = SumiStyleTokens.Vermilion;

            GameObject subObj = CreateUIObject("Subtitle_Text", headerBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, -14f), new Vector2(300f, 20f));
            Text subTxt = subObj.AddComponent<Text>();
            subTxt.font = defaultFont;
            subTxt.text = "NEO-SHIBUYA SOUL HARVEST";
            subTxt.fontSize = 13;
            subTxt.fontStyle = FontStyle.Bold;
            subTxt.alignment = TextAnchor.MiddleLeft;
            subTxt.color = SumiStyleTokens.ParryGold;

            // 4 Top Pillars Center
            float tabW = 140f;
            ctrl.tabFight = CreateTabButton(headerBar.transform, "Tab_Fight", "FIGHT", new Vector2(-210f, 0f), new Vector2(tabW, 46f), defaultFont);
            ctrl.tabDojo = CreateTabButton(headerBar.transform, "Tab_Dojo", "DOJO", new Vector2(-70f, 0f), new Vector2(tabW, 46f), defaultFont);
            ctrl.tabRoster = CreateTabButton(headerBar.transform, "Tab_Roster", "ROSTER", new Vector2(70f, 0f), new Vector2(tabW, 46f), defaultFont);
            ctrl.tabSettings = CreateTabButton(headerBar.transform, "Tab_Settings", "SETTINGS", new Vector2(210f, 0f), new Vector2(tabW, 46f), defaultFont);

            GameObject indObj = CreateUIObject("Tab_Indicator", headerBar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 4f), new Vector2(tabW, 4f));
            Image indImg = indObj.AddComponent<Image>();
            indImg.color = SumiStyleTokens.Vermilion;
            ctrl.tabIndicator = indObj.GetComponent<RectTransform>();

            // Profile Reward Pill Right
            GameObject pillObj = CreateUIObject("Profile_Reward_Pill", headerBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-160f, 0f), new Vector2(260f, 38f));
            Image pillBg = pillObj.AddComponent<Image>();
            pillBg.color = SumiStyleTokens.CarbonInk;
            ctrl.profileRewardText = CreateUIObject("Reward_Text", pillObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).AddComponent<Text>();
            ctrl.profileRewardText.font = defaultFont;
            ctrl.profileRewardText.text = "LVL 1 // 0 XP // 0 BATTLES";
            ctrl.profileRewardText.fontSize = 13;
            ctrl.profileRewardText.fontStyle = FontStyle.Bold;
            ctrl.profileRewardText.alignment = TextAnchor.MiddleCenter;
            ctrl.profileRewardText.color = SumiStyleTokens.PaperWhite;

            // =========================================================================
            // ASYMMETRIC MODE CARDS (LEFT 45% FOCUS RAIL)
            // =========================================================================
            Vector2 cardSize = new Vector2(460f, 85f);
            Color cardBg = SumiStyleTokens.CarbonInkWash;

            // Panel: FIGHT
            GameObject pFight = CreateUIObject("Panel_FIGHT", canvasObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, -20f), new Vector2(500f, 540f));
            ctrl.panelFight = pFight;

            ctrl.btnArcade = CreateCardButton(pFight.transform, "Card_Arcade", "ARCADE LADDER", "Battle through the 25-fighter planar gauntlet against AI.", new Vector2(240f, 150f), cardSize, cardBg, SumiStyleTokens.Vermilion, defaultFont);
            ctrl.btnVersus = CreateCardButton(pFight.transform, "Card_Versus", "LOCAL VERSUS (1v1)", "Authentic 4-button tournament combat for 2 local players.", new Vector2(240f, 50f), cardSize, cardBg, new Color(0.2f, 0.6f, 1f), defaultFont);
            ctrl.btnOnlinePvP = CreateCardButton(pFight.transform, "Card_OnlinePvP", "ONLINE QUANTUM PVP", "Photon Quantum 3.0 deterministic rollback matchmaking.", new Vector2(240f, -50f), cardSize, cardBg, SumiStyleTokens.ParryGold, defaultFont);
            ctrl.btnQuickFight = CreateCardButton(pFight.transform, "Card_QuickFight", "QUICK MATCH [1-TAP]", "Instant battle with last fighter & stage.", new Vector2(240f, -150f), cardSize, cardBg, SumiStyleTokens.ToriiRed, defaultFont);

            // Panel: DOJO
            GameObject pDojo = CreateUIObject("Panel_DOJO", canvasObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, -20f), new Vector2(500f, 540f));
            ctrl.panelDojo = pDojo;

            ctrl.btnTraining = CreateCardButton(pDojo.transform, "Card_Training", "PRACTICE DOJO", "Master Revolver Gatlings & frame meters with infinite meter.", new Vector2(240f, 100f), cardSize, cardBg, SumiStyleTokens.ParryGold, defaultFont);
            ctrl.btnMoveReference = CreateCardButton(pDojo.transform, "Card_MoveReference", "TACTICAL MOVELIST", "Frame data, specials, and BlazBlue system mechanics.", new Vector2(240f, 0f), cardSize, cardBg, SumiStyleTokens.PaperWhite, defaultFont);
            ctrl.btnTutorial = CreateCardButton(pDojo.transform, "Card_Tutorial", "SOUL HARVEST TUTORIAL", "Interactive fighting game fundamentals onboarding.", new Vector2(240f, -100f), cardSize, cardBg, SumiStyleTokens.Vermilion, defaultFont);
            pDojo.SetActive(false);

            // Panel: ROSTER
            GameObject pRoster = CreateUIObject("Panel_ROSTER", canvasObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, -20f), new Vector2(500f, 540f));
            ctrl.panelRoster = pRoster;

            ctrl.btnRosterShowcase = CreateCardButton(pRoster.transform, "Card_RosterShowcase", "ROSTER SHOWCASE", "Inspect all 25 fighters, archetypes, and weapon masteries.", new Vector2(240f, 40f), cardSize, cardBg, SumiStyleTokens.ParryGold, defaultFont);
            pRoster.SetActive(false);

            // =========================================================================
            // SLIDE-OVER TACTICAL SETTINGS DRAWER (PHASE 3)
            // =========================================================================
            GameObject scrim = CreateUIObject("Drawer_Scrim", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image scrimImg = scrim.AddComponent<Image>();
            scrimImg.color = SumiStyleTokens.CarbonInkWash;
            ctrl.drawerScrim = scrim;
            scrim.SetActive(false);

            GameObject drawerRoot = CreateUIObject("Settings_Drawer_Root", canvasObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(560f, 0f), new Vector2(540f, 0f));
            Image drawerBg = drawerRoot.AddComponent<Image>();
            drawerBg.color = SumiStyleTokens.CarbonInk;
            ctrl.settingsDrawerRoot = drawerRoot.GetComponent<RectTransform>();
            ctrl.drawerCanvasGroup = drawerRoot.AddComponent<CanvasGroup>();
            ctrl.drawerCanvasGroup.alpha = 0f;
            ctrl.settingsModal = drawerRoot;

            // Drawer Header
            GameObject dHdr = CreateUIObject("Drawer_Header", drawerRoot.transform, new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(500f, 50f));
            Text dHdrTxt = dHdr.AddComponent<Text>();
            dHdrTxt.font = defaultFont;
            dHdrTxt.text = "TACTICAL SETTINGS // CONFIG";
            dHdrTxt.fontSize = 22;
            dHdrTxt.fontStyle = FontStyle.Bold;
            dHdrTxt.alignment = TextAnchor.MiddleCenter;
            dHdrTxt.color = SumiStyleTokens.ParryGold;

            // Drawer Sub-Tabs
            float dTabW = 110f;
            ctrl.tabDrawerAudio = CreateTabButton(drawerRoot.transform, "Tab_Audio", "AUDIO", new Vector2(-165f, 400f), new Vector2(dTabW, 36f), defaultFont);
            ctrl.tabDrawerGraphics = CreateTabButton(drawerRoot.transform, "Tab_Graphics", "GRAPHICS", new Vector2(-55f, 400f), new Vector2(dTabW, 36f), defaultFont);
            ctrl.tabDrawerAccessibility = CreateTabButton(drawerRoot.transform, "Tab_Access", "ACCESS", new Vector2(55f, 400f), new Vector2(dTabW, 36f), defaultFont);
            ctrl.tabDrawerControls = CreateTabButton(drawerRoot.transform, "Tab_Controls", "CONTROLS", new Vector2(165f, 400f), new Vector2(dTabW, 36f), defaultFont);

            // Audio Drawer Panel
            GameObject dAudio = CreateUIObject("Drawer_Audio", drawerRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(480f, 320f));
            ctrl.drawerPanelAudio = dAudio;
            ctrl.sliderBGM = CreateSlider(dAudio.transform, "Slider_BGM", "BGM Volume", new Vector2(0f, 80f), defaultFont);
            ctrl.sliderSFX = CreateSlider(dAudio.transform, "Slider_SFX", "SFX Volume", new Vector2(0f, 10f), defaultFont);
            ctrl.sliderVoice = CreateSlider(dAudio.transform, "Slider_Voice", "Announcer Volume", new Vector2(0f, -60f), defaultFont);
            ctrl.btnTestSound = CreateMenuButton(dAudio.transform, "Btn_TestSFX", "TEST AUDIO CHIME", new Vector2(0f, -125f), new Color(0.2f, 0.35f, 0.5f, 1f), defaultFont);

            // Graphics Drawer Panel
            GameObject dGraphics = CreateUIObject("Drawer_Graphics", drawerRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(480f, 320f));
            ctrl.drawerPanelGraphics = dGraphics;

            ctrl.btnQualityPrev = CreateTabButton(dGraphics.transform, "Btn_QualityPrev", "<", new Vector2(-140f, 80f), new Vector2(50f, 40f), defaultFont);
            ctrl.btnQualityNext = CreateTabButton(dGraphics.transform, "Btn_QualityNext", ">", new Vector2(140f, 80f), new Vector2(50f, 40f), defaultFont);
            ctrl.txtQualityName = CreateUIObject("Txt_Quality", dGraphics.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(200f, 40f)).AddComponent<Text>();
            ctrl.txtQualityName.font = defaultFont;
            ctrl.txtQualityName.fontSize = 18;
            ctrl.txtQualityName.fontStyle = FontStyle.Bold;
            ctrl.txtQualityName.alignment = TextAnchor.MiddleCenter;
            ctrl.txtQualityName.color = SumiStyleTokens.PaperWhite;

            ctrl.btnToggleFullscreen = CreateMenuButton(dGraphics.transform, "Btn_Fullscreen", "FULLSCREEN: ON", new Vector2(0f, 10f), new Color(0.2f, 0.25f, 0.35f, 1f), defaultFont);
            ctrl.txtFullscreenState = ctrl.btnToggleFullscreen.GetComponentInChildren<Text>();

            ctrl.btnToggleVSync = CreateMenuButton(dGraphics.transform, "Btn_VSync", "VSYNC: ENABLED", new Vector2(0f, -60f), new Color(0.2f, 0.25f, 0.35f, 1f), defaultFont);
            ctrl.txtVSyncState = ctrl.btnToggleVSync.GetComponentInChildren<Text>();

            ctrl.txtFpsLock = CreateUIObject("Txt_FpsLock", dGraphics.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -125f), new Vector2(400f, 30f)).AddComponent<Text>();
            ctrl.txtFpsLock.font = defaultFont;
            ctrl.txtFpsLock.fontSize = 14;
            ctrl.txtFpsLock.alignment = TextAnchor.MiddleCenter;
            ctrl.txtFpsLock.color = SumiStyleTokens.ParryGold;
            dGraphics.SetActive(false);

            // Accessibility Drawer Panel
            GameObject dAccess = CreateUIObject("Drawer_Access", drawerRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(480f, 320f));
            ctrl.drawerPanelAccessibility = dAccess;

            ctrl.btnToggleReducedMotion = CreateMenuButton(dAccess.transform, "Btn_ReducedMotion", "REDUCED MOTION: OFF", new Vector2(0f, 40f), new Color(0.2f, 0.25f, 0.35f, 1f), defaultFont);
            ctrl.txtReducedMotionState = ctrl.btnToggleReducedMotion.GetComponentInChildren<Text>();

            ctrl.btnToggleReducedGore = CreateMenuButton(dAccess.transform, "Btn_ReducedGore", "REDUCED GORE: OFF", new Vector2(0f, -40f), new Color(0.2f, 0.25f, 0.35f, 1f), defaultFont);
            ctrl.txtReducedGoreState = ctrl.btnToggleReducedGore.GetComponentInChildren<Text>();
            dAccess.SetActive(false);

            // Controls Drawer Panel
            GameObject dControls = CreateUIObject("Drawer_Controls", drawerRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(480f, 320f));
            ctrl.drawerPanelControls = dControls;
            Text ctrlTxt = CreateUIObject("Controls_Help", dControls.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).AddComponent<Text>();
            ctrlTxt.font = defaultFont;
            ctrlTxt.text = "LIGHT ATTACK (A): J / Button South\n" +
                           "MEDIUM ATTACK (B): K / Button West\n" +
                           "HEAVY ATTACK (C): L / Button East\n" +
                           "DRIVE (D): Space / Button North\n" +
                           "CRUSH TRIGGER: U (A+B) [25% Heat]\n" +
                           "BARRIER BLOCK: 4+A+B [Barrier Drain]\n" +
                           "THROW: O (B+C)\n" +
                           "RAPID CANCEL: A+B+C [50% Heat]\n" +
                           "BURST / OVERDRIVE: P (A+B+C+D)";
            ctrlTxt.fontSize = 15;
            ctrlTxt.alignment = TextAnchor.MiddleLeft;
            ctrlTxt.lineSpacing = 1.35f;
            ctrlTxt.color = SumiStyleTokens.PaperWhite;
            dControls.SetActive(false);

            // Close Drawer Button
            ctrl.btnCloseSettings = CreateMenuButton(drawerRoot.transform, "Btn_CloseDrawer", "CLOSE DRAWER ▶", new Vector2(0f, -450f), SumiStyleTokens.Vermilion, defaultFont);

            // =========================================================================
            // FOOTER CONTROL PROMPTS & QUIT
            // =========================================================================
            GameObject footerBar = CreateUIObject("Bottom_Footer_Bar", canvasObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 25f), new Vector2(0f, 50f));
            Image footerBg = footerBar.AddComponent<Image>();
            footerBg.color = SumiStyleTokens.CarbonInkWash;

            ctrl.footerControlPromptText = CreateUIObject("Control_Prompts", footerBar.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).AddComponent<Text>();
            ctrl.footerControlPromptText.font = defaultFont;
            ctrl.footerControlPromptText.fontSize = 14;
            ctrl.footerControlPromptText.alignment = TextAnchor.MiddleCenter;
            ctrl.footerControlPromptText.color = SumiStyleTokens.PaperWhite;

            ctrl.btnQuit = CreateMenuButton(canvasObj.transform, "Btn_Quit", "QUIT", new Vector2(80f, 60f), new Color(0.18f, 0.2f, 0.25f, 0.95f), defaultFont);
            ctrl.btnQuit.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 40f);
            ctrl.btnQuit.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            ctrl.btnQuit.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0f);

            // 5. Single EventSystem with InputSystemUIInputModule
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, SCENE_MAIN_MENU);
            Debug.Log($"<color=#00e676>[DemonIdolSceneBuilder] Built modern MainMenu successfully at {SCENE_MAIN_MENU}</color>");
        }

        // =========================================================================
        // SCENE 2: CharacterSelect.unity (Dual 3D Pedestals, Info Wings & Inline Options)
        // =========================================================================
        public static void BuildCharacterSelectScene(bool interactive = true)
        {
            if (!ConfirmAndBackup(SCENE_CHAR_SELECT, "CharacterSelect", interactive, out _))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Dual 3D Preview Pedestals (Upper 62% Viewport)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            camObj.AddComponent<UniversalAdditionalCameraData>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0f, 1.4f, -4.5f);
            camObj.transform.rotation = Quaternion.Euler(5f, 0f, 0f);

            GameObject keyLight = new GameObject("Directional_KeyLight");
            Light kl = keyLight.AddComponent<Light>();
            kl.type = LightType.Directional;
            kl.color = new Color(1f, 0.96f, 0.92f);
            kl.intensity = 1.35f;
            keyLight.transform.rotation = Quaternion.Euler(30f, -20f, 0f);

            GameObject p1Light = new GameObject("P1_RimLight");
            Light p1l = p1Light.AddComponent<Light>();
            p1l.type = LightType.Directional;
            p1l.color = SumiStyleTokens.Vermilion;
            p1l.intensity = 0.75f;
            p1Light.transform.rotation = Quaternion.Euler(-15f, 130f, 0f);

            GameObject p2Light = new GameObject("P2_RimLight");
            Light p2l = p2Light.AddComponent<Light>();
            p2l.type = LightType.Directional;
            p2l.color = SumiStyleTokens.ParryGold;
            p2l.intensity = 0.75f;
            p2Light.transform.rotation = Quaternion.Euler(-15f, -130f, 0f);

            // P1 Pedestal (Left)
            GameObject p1Ped = new GameObject("Preview_Pedestal_P1");
            p1Ped.transform.position = new Vector3(-2.2f, 0f, 0f);
            p1Ped.transform.rotation = Quaternion.Euler(0f, 115f, 0f);
            GameObject disc1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc1.name = "P1_Disc";
            disc1.transform.SetParent(p1Ped.transform, false);
            disc1.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            disc1.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);
            var c1 = disc1.GetComponent<Collider>();
            if (c1 != null) UnityEngine.Object.DestroyImmediate(c1);

            // P2 Pedestal (Right)
            GameObject p2Ped = new GameObject("Preview_Pedestal_P2");
            p2Ped.transform.position = new Vector3(2.2f, 0f, 0f);
            p2Ped.transform.rotation = Quaternion.Euler(0f, -115f, 0f);
            GameObject disc2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc2.name = "P2_Disc";
            disc2.transform.SetParent(p2Ped.transform, false);
            disc2.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            disc2.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);
            var c2 = disc2.GetComponent<Collider>();
            if (c2 != null) UnityEngine.Object.DestroyImmediate(c2);

            // 2. Canvas & Modern UI
            GameObject canvasObj = new GameObject("CharacterSelect_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<SafeAreaFitter>();

            DemonIdolCharacterSelectManager mgr = canvasObj.AddComponent<DemonIdolCharacterSelectManager>();
            mgr.p1Pedestal = p1Ped.transform;
            mgr.p2Pedestal = p2Ped.transform;
            mgr.previewPedestal = p1Ped.transform;
            mgr.previewCamera = cam;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Header Banner
            GameObject header = CreateUIObject("Header_Title", canvasObj.transform, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), Vector2.zero, new Vector2(1000f, 50f));
            Text headerTxt = header.AddComponent<Text>();
            headerTxt.font = font;
            headerTxt.text = "SELECT YOUR FIGHTER";
            headerTxt.fontSize = 32;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.color = SumiStyleTokens.ParryGold;

            // =========================================================================
            // TACTICAL FIGHTER INFO WINGS (UPPER 62%)
            // =========================================================================
            // P1 Wing (Top Left)
            GameObject p1WingObj = CreateUIObject("P1_Info_Wing", canvasObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -220f), new Vector2(340f, 360f));
            Image p1WingBg = p1WingObj.AddComponent<Image>();
            p1WingBg.color = SumiStyleTokens.CarbonInkWash;

            GameObject p1Accent = CreateUIObject("AccentBar", p1WingObj.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(6f, 0f));
            p1Accent.AddComponent<Image>().color = SumiStyleTokens.Vermilion;

            mgr.p1NameWing = CreateUIObject("P1_Name", p1WingObj.transform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(300f, 36f)).AddComponent<Text>();
            mgr.p1NameWing.font = font;
            mgr.p1NameWing.fontSize = 26;
            mgr.p1NameWing.fontStyle = FontStyle.Bold;
            mgr.p1NameWing.alignment = TextAnchor.MiddleCenter;
            mgr.p1NameWing.color = SumiStyleTokens.PaperWhite;

            mgr.p1TitleWing = CreateUIObject("P1_Title", p1WingObj.transform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(300f, 24f)).AddComponent<Text>();
            mgr.p1TitleWing.font = font;
            mgr.p1TitleWing.fontSize = 14;
            mgr.p1TitleWing.alignment = TextAnchor.MiddleCenter;
            mgr.p1TitleWing.color = SumiStyleTokens.PaperWash;

            mgr.p1ArchetypeBadge = CreateUIObject("P1_Archetype", p1WingObj.transform, new Vector2(0.5f, 0.67f), new Vector2(0.5f, 0.67f), Vector2.zero, new Vector2(300f, 26f)).AddComponent<Text>();
            mgr.p1ArchetypeBadge.font = font;
            mgr.p1ArchetypeBadge.fontSize = 16;
            mgr.p1ArchetypeBadge.fontStyle = FontStyle.Bold;
            mgr.p1ArchetypeBadge.alignment = TextAnchor.MiddleCenter;
            mgr.p1ArchetypeBadge.color = SumiStyleTokens.Vermilion;

            mgr.p1DriveWing = CreateUIObject("P1_Drive", p1WingObj.transform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(300f, 24f)).AddComponent<Text>();
            mgr.p1DriveWing.font = font;
            mgr.p1DriveWing.fontSize = 15;
            mgr.p1DriveWing.fontStyle = FontStyle.Bold;
            mgr.p1DriveWing.alignment = TextAnchor.MiddleCenter;
            mgr.p1DriveWing.color = SumiStyleTokens.ParryGold;

            mgr.p1ReachBar = CreateStatBar(p1WingObj.transform, "P1_Reach", "REACH", new Vector2(0f, -20f), SumiStyleTokens.Vermilion, font);
            mgr.p1SpeedBar = CreateStatBar(p1WingObj.transform, "P1_Speed", "SPEED", new Vector2(0f, -50f), new Color(0.2f, 0.8f, 1f), font);
            mgr.p1PowerBar = CreateStatBar(p1WingObj.transform, "P1_Power", "POWER", new Vector2(0f, -80f), SumiStyleTokens.ToriiRed, font);

            mgr.p1StatusText = CreateUIObject("P1_Status", p1WingObj.transform, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(300f, 30f)).AddComponent<Text>();
            mgr.p1StatusText.font = font;
            mgr.p1StatusText.fontSize = 17;
            mgr.p1StatusText.fontStyle = FontStyle.Bold;
            mgr.p1StatusText.alignment = TextAnchor.MiddleCenter;
            mgr.p1StatusText.color = SumiStyleTokens.Vermilion;

            // P2 Wing (Top Right)
            GameObject p2WingObj = CreateUIObject("P2_Info_Wing", canvasObj.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -220f), new Vector2(340f, 360f));
            Image p2WingBg = p2WingObj.AddComponent<Image>();
            p2WingBg.color = SumiStyleTokens.CarbonInkWash;

            GameObject p2Accent = CreateUIObject("AccentBar", p2WingObj.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(6f, 0f));
            p2Accent.AddComponent<Image>().color = SumiStyleTokens.ParryGold;

            mgr.p2NameWing = CreateUIObject("P2_Name", p2WingObj.transform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(300f, 36f)).AddComponent<Text>();
            mgr.p2NameWing.font = font;
            mgr.p2NameWing.fontSize = 26;
            mgr.p2NameWing.fontStyle = FontStyle.Bold;
            mgr.p2NameWing.alignment = TextAnchor.MiddleCenter;
            mgr.p2NameWing.color = SumiStyleTokens.PaperWhite;

            mgr.p2TitleWing = CreateUIObject("P2_Title", p2WingObj.transform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(300f, 24f)).AddComponent<Text>();
            mgr.p2TitleWing.font = font;
            mgr.p2TitleWing.fontSize = 14;
            mgr.p2TitleWing.alignment = TextAnchor.MiddleCenter;
            mgr.p2TitleWing.color = SumiStyleTokens.PaperWash;

            mgr.p2ArchetypeBadge = CreateUIObject("P2_Archetype", p2WingObj.transform, new Vector2(0.5f, 0.67f), new Vector2(0.5f, 0.67f), Vector2.zero, new Vector2(300f, 26f)).AddComponent<Text>();
            mgr.p2ArchetypeBadge.font = font;
            mgr.p2ArchetypeBadge.fontSize = 16;
            mgr.p2ArchetypeBadge.fontStyle = FontStyle.Bold;
            mgr.p2ArchetypeBadge.alignment = TextAnchor.MiddleCenter;
            mgr.p2ArchetypeBadge.color = SumiStyleTokens.ParryGold;

            mgr.p2DriveWing = CreateUIObject("P2_Drive", p2WingObj.transform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(300f, 24f)).AddComponent<Text>();
            mgr.p2DriveWing.font = font;
            mgr.p2DriveWing.fontSize = 15;
            mgr.p2DriveWing.fontStyle = FontStyle.Bold;
            mgr.p2DriveWing.alignment = TextAnchor.MiddleCenter;
            mgr.p2DriveWing.color = SumiStyleTokens.ParryGold;

            mgr.p2ReachBar = CreateStatBar(p2WingObj.transform, "P2_Reach", "REACH", new Vector2(0f, -20f), SumiStyleTokens.ParryGold, font);
            mgr.p2SpeedBar = CreateStatBar(p2WingObj.transform, "P2_Speed", "SPEED", new Vector2(0f, -50f), new Color(0.2f, 0.8f, 1f), font);
            mgr.p2PowerBar = CreateStatBar(p2WingObj.transform, "P2_Power", "POWER", new Vector2(0f, -80f), SumiStyleTokens.ToriiRed, font);

            mgr.p2StatusText = CreateUIObject("P2_Status", p2WingObj.transform, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(300f, 30f)).AddComponent<Text>();
            mgr.p2StatusText.font = font;
            mgr.p2StatusText.fontSize = 17;
            mgr.p2StatusText.fontStyle = FontStyle.Bold;
            mgr.p2StatusText.alignment = TextAnchor.MiddleCenter;
            mgr.p2StatusText.color = SumiStyleTokens.ParryGold;

            // =========================================================================
            // INLINE MATCH OPTIONS STRIP
            // =========================================================================
            GameObject optStrip = CreateUIObject("Match_Options_Strip", canvasObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 360f), new Vector2(980f, 48f));
            Image optBg = optStrip.AddComponent<Image>();
            optBg.color = SumiStyleTokens.CarbonInkWash;
            mgr.matchOptionsStrip = optStrip;

            // Stage Prev / Next
            mgr.btnStagePrev = CreateTabButton(optStrip.transform, "Btn_StagePrev", "<", new Vector2(-420f, 0f), new Vector2(36f, 36f), font);
            mgr.txtStageSelected = CreateUIObject("Txt_Stage", optStrip.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, 0f), new Vector2(240f, 36f)).AddComponent<Text>();
            mgr.txtStageSelected.font = font;
            mgr.txtStageSelected.fontSize = 13;
            mgr.txtStageSelected.fontStyle = FontStyle.Bold;
            mgr.txtStageSelected.alignment = TextAnchor.MiddleCenter;
            mgr.txtStageSelected.color = SumiStyleTokens.PaperWhite;
            mgr.btnStageNext = CreateTabButton(optStrip.transform, "Btn_StageNext", ">", new Vector2(-140f, 0f), new Vector2(36f, 36f), font);

            // Rounds Prev / Next
            mgr.btnRoundsPrev = CreateTabButton(optStrip.transform, "Btn_RoundsPrev", "<", new Vector2(-70f, 0f), new Vector2(36f, 36f), font);
            mgr.txtRoundsSelected = CreateUIObject("Txt_Rounds", optStrip.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(180f, 36f)).AddComponent<Text>();
            mgr.txtRoundsSelected.font = font;
            mgr.txtRoundsSelected.fontSize = 13;
            mgr.txtRoundsSelected.fontStyle = FontStyle.Bold;
            mgr.txtRoundsSelected.alignment = TextAnchor.MiddleCenter;
            mgr.txtRoundsSelected.color = SumiStyleTokens.ParryGold;
            mgr.btnRoundsNext = CreateTabButton(optStrip.transform, "Btn_RoundsNext", ">", new Vector2(150f, 0f), new Vector2(36f, 36f), font);

            // Start Fight Button Right
            mgr.btnStartFight = CreateMenuButton(optStrip.transform, "Btn_StartFight", "START FIGHT ▶", new Vector2(360f, 0f), SumiStyleTokens.Vermilion, font);
            mgr.btnStartFight.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);

            // =========================================================================
            // CATEGORIZED 25-FIGHTER BOTTOM DOCK (LOWER 32%)
            // =========================================================================
            // Filter Pills
            GameObject filterRow = CreateUIObject("Filter_Tabs_Row", canvasObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 310f), new Vector2(980f, 36f));
            mgr.tabCatAll = CreateTabButton(filterRow.transform, "Tab_All", "ALL (25)", new Vector2(-360f, 0f), new Vector2(150f, 32f), font);
            mgr.tabCatMain = CreateTabButton(filterRow.transform, "Tab_Main", "MAIN CAST (13)", new Vector2(-180f, 0f), new Vector2(170f, 32f), font);
            mgr.tabCatKnights = CreateTabButton(filterRow.transform, "Tab_Knights", "SHADOW KNIGHTS (5)", new Vector2(20f, 0f), new Vector2(190f, 32f), font);
            mgr.tabCatExpansion = CreateTabButton(filterRow.transform, "Tab_Exp", "EXPANSION (7)", new Vector2(220f, 0f), new Vector2(170f, 32f), font);
            mgr.btnRandomPick = CreateTabButton(filterRow.transform, "Tab_Rand", "RANDOM (?)", new Vector2(410f, 0f), new Vector2(150f, 32f), font);

            // Roster Grid
            GameObject gridPanel = CreateUIObject("Roster_Grid", canvasObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(1000f, 250f));
            mgr.gridRoot = gridPanel.GetComponent<RectTransform>();

            string[] roster = DemonIdolCharacterSelectManager.RosterIds;
            mgr.characterButtons = new Button[roster.Length];
            mgr.p1Borders = new Image[roster.Length];
            mgr.p2Borders = new Image[roster.Length];
            mgr.characterLabels = new Text[roster.Length];

            float cardW = 90f;
            float cardH = 80f;
            float spacingX = 8f;
            float spacingY = 8f;
            float startX = -((13 * cardW + 12 * spacingX) / 2f) + cardW / 2f;

            for (int i = 0; i < roster.Length; i++)
            {
                int row = i >= 13 ? 1 : 0;
                int col = i >= 13 ? (i - 13) : i;

                float x = startX + col * (cardW + spacingX);
                if (row == 1) x += cardW * 0.5f; // Slight stagger
                float y = (row == 0) ? 46f : -46f;

                string cid = roster[i];
                var def = CharacterRegistry.GetCharacterById(cid);
                string cname = def != null ? def.displayName : cid;

                GameObject card = CreateUIObject($"Card_{cid}", gridPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(cardW, cardH));
                Image cardBg = card.AddComponent<Image>();
                cardBg.color = SumiStyleTokens.CarbonInk;
                Button cardBtn = card.AddComponent<Button>();

                // Mugshot
                GameObject mugGo = CreateUIObject("Mugshot", card.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -20f));
                Image mugImg = mugGo.AddComponent<Image>();
                mugImg.preserveAspect = true;
                Sprite mugSprite = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{cid}_{cname}");
                if (mugSprite != null) mugImg.sprite = mugSprite;
                else mugImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);

                // Label
                GameObject nameGo = CreateUIObject("Label", card.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 20f));
                Text nameTxt = nameGo.AddComponent<Text>();
                nameTxt.font = font;
                nameTxt.text = cname.ToUpper();
                nameTxt.fontSize = 11;
                nameTxt.fontStyle = FontStyle.Bold;
                nameTxt.alignment = TextAnchor.MiddleCenter;
                nameTxt.color = SumiStyleTokens.PaperWhite;

                // P1 Border (Outer)
                GameObject p1Border = CreateUIObject("P1_Border", card.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(8f, 8f));
                Image p1BorderImg = p1Border.AddComponent<Image>();
                p1BorderImg.color = SumiStyleTokens.Vermilion;
                p1Border.SetActive(false);

                // P2 Border (Inner)
                GameObject p2Border = CreateUIObject("P2_Border", card.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(4f, 4f));
                Image p2BorderImg = p2Border.AddComponent<Image>();
                p2BorderImg.color = SumiStyleTokens.ParryGold;
                p2Border.SetActive(false);

                int slotIdx = i;
                cardBtn.onClick.AddListener(() => mgr.SelectSlot(slotIdx, true));

                mgr.characterButtons[i] = cardBtn;
                mgr.p1Borders[i] = p1BorderImg;
                mgr.p2Borders[i] = p2BorderImg;
                mgr.characterLabels[i] = nameTxt;
            }

            // VS Screen Overlay
            GameObject vsGo = CreateUIObject("VS_Overlay", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image vsBg = vsGo.AddComponent<Image>();
            vsBg.color = SumiStyleTokens.CarbonInkWash;

            GameObject vsBanner = CreateUIObject("VS_Banner", vsGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 120f));
            Text vsTxt = vsBanner.AddComponent<Text>();
            vsTxt.font = font;
            vsTxt.text = "VS";
            vsTxt.fontSize = 84;
            vsTxt.fontStyle = FontStyle.BoldAndItalic;
            vsTxt.alignment = TextAnchor.MiddleCenter;
            vsTxt.color = SumiStyleTokens.ParryGold;

            GameObject vsP1 = CreateUIObject("P1_Preview", vsGo.transform, new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f), Vector2.zero, new Vector2(320f, 420f));
            mgr.vsP1Portrait = vsP1.AddComponent<Image>();
            mgr.vsP1Portrait.preserveAspect = true;
            mgr.vsP1Name = CreateUIObject("P1_Name", vsP1.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(300f, 40f)).AddComponent<Text>();
            mgr.vsP1Name.font = font;
            mgr.vsP1Name.fontSize = 28;
            mgr.vsP1Name.fontStyle = FontStyle.Bold;
            mgr.vsP1Name.alignment = TextAnchor.MiddleCenter;
            mgr.vsP1Name.color = SumiStyleTokens.Vermilion;

            GameObject vsP2 = CreateUIObject("P2_Preview", vsGo.transform, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), Vector2.zero, new Vector2(320f, 420f));
            mgr.vsP2Portrait = vsP2.AddComponent<Image>();
            mgr.vsP2Portrait.preserveAspect = true;
            mgr.vsP2Name = CreateUIObject("P2_Name", vsP2.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(300f, 40f)).AddComponent<Text>();
            mgr.vsP2Name.font = font;
            mgr.vsP2Name.fontSize = 28;
            mgr.vsP2Name.fontStyle = FontStyle.Bold;
            mgr.vsP2Name.alignment = TextAnchor.MiddleCenter;
            mgr.vsP2Name.color = SumiStyleTokens.ParryGold;

            mgr.vsStageName = CreateUIObject("VS_Stage", vsGo.transform, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), Vector2.zero, new Vector2(600f, 40f)).AddComponent<Text>();
            mgr.vsStageName.font = font;
            mgr.vsStageName.fontSize = 20;
            mgr.vsStageName.fontStyle = FontStyle.Bold;
            mgr.vsStageName.alignment = TextAnchor.MiddleCenter;
            mgr.vsStageName.color = SumiStyleTokens.PaperWhite;

            mgr.vsOverlay = vsGo;
            vsGo.SetActive(false);

            // Footer Control Prompts
            GameObject footerBar = CreateUIObject("Bottom_Footer_Bar", canvasObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 40f));
            mgr.footerPromptsText = CreateUIObject("Footer_Prompts", footerBar.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).AddComponent<Text>();
            mgr.footerPromptsText.font = font;
            mgr.footerPromptsText.fontSize = 14;
            mgr.footerPromptsText.alignment = TextAnchor.MiddleCenter;
            mgr.footerPromptsText.color = SumiStyleTokens.PaperWhite;

            // 3. Single EventSystem with InputSystemUIInputModule
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, SCENE_CHAR_SELECT);
            Debug.Log($"<color=#00e676>[DemonIdolSceneBuilder] Built modern CharacterSelect successfully at {SCENE_CHAR_SELECT}</color>");
        }

        // =========================================================================
        // SCENE 3: BattleArena.unity
        // =========================================================================
        public static void BuildBattleArenaScene(bool interactive = true)
        {
            if (!ConfirmAndBackup(SCENE_BATTLE_ARENA, "BattleArena", interactive, out _))
                return;

            // Also backup legacy ArcadeArena if present
            if (File.Exists(SCENE_LEGACY_ARCADE))
            {
                string legacyDst = $"{LEGACY_DIR}/ArcadeArena_Legacy.unity";
                if (!File.Exists(legacyDst))
                {
                    File.Copy(SCENE_LEGACY_ARCADE, legacyDst, true);
                    AssetDatabase.ImportAsset(legacyDst);
                }
            }

            // Copy ArcadeArena as starting baseline if BattleArena does not exist yet
            if (!File.Exists(SCENE_BATTLE_ARENA) && File.Exists(SCENE_LEGACY_ARCADE))
            {
                File.Copy(SCENE_LEGACY_ARCADE, SCENE_BATTLE_ARENA, true);
                AssetDatabase.ImportAsset(SCENE_BATTLE_ARENA);
            }

            Scene scene = EditorSceneManager.OpenScene(SCENE_BATTLE_ARENA, OpenSceneMode.Single);

            // 1. Setup Match_Host with all 4 required components
            GameObject matchHost = GameObject.Find("Match_Host");
            if (matchHost == null)
            {
                GameObject combatManagers = GameObject.Find("Combat_Managers");
                if (combatManagers != null)
                {
                    combatManagers.name = "Match_Host";
                    matchHost = combatManagers;
                }
                else
                {
                    matchHost = new GameObject("Match_Host");
                }
            }

            // Remove CombatAudioDirector from Match_Host and whole scene
            var audioDirs = UnityEngine.Object.FindObjectsByType<CombatAudioDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ad in audioDirs)
            {
                UnityEngine.Object.DestroyImmediate(ad);
            }

            var director = matchHost.GetComponent<HomegrownCombatDirector>();
            if (director == null) director = matchHost.AddComponent<HomegrownCombatDirector>();
            director.enabled = true;
            director.RosterDatabase = AssetDatabase.LoadAssetAtPath<DemonIdol.Data.MDIRosterDatabase>("Assets/Resources/MDI_RosterDatabase.asset");

            var tickEngine = matchHost.GetComponent<CombatTickEngine>();
            if (tickEngine == null) tickEngine = matchHost.AddComponent<CombatTickEngine>();
            tickEngine.enabled = true;

            var referee = matchHost.GetComponent<MatchRoundReferee>();
            if (referee == null) referee = matchHost.AddComponent<MatchRoundReferee>();
            referee.enabled = true;

            var bootstrap = matchHost.GetComponent<BBMatchBootstrap>();
            if (bootstrap == null) bootstrap = matchHost.AddComponent<BBMatchBootstrap>();
            bootstrap.enabled = true;

            // 2. Remove Baked Fighters (Director spawns dynamic picks)
            string[] bakedNames = new string[] { "Fighter_P1_Akio", "Fighter_P2_Aika", "Player1", "Player2" };
            foreach (var bName in bakedNames)
            {
                GameObject baked = GameObject.Find(bName);
                if (baked != null)
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            // Remove any loose child models
            var allGo = scene.GetRootGameObjects();
            foreach (var rGo in allGo)
            {
                if (rGo.name.StartsWith("Model_C") || rGo.name.StartsWith("Model_SK") || rGo.name.StartsWith("Model_EX"))
                {
                    UnityEngine.Object.DestroyImmediate(rGo);
                }
            }

            // Ensure Spawn Points exist
            GameObject spawn1 = GameObject.Find("SpawnPoint_P1");
            if (spawn1 == null)
            {
                spawn1 = new GameObject("SpawnPoint_P1");
                spawn1.transform.position = new Vector3(-2.5f, 0, 0);
                spawn1.transform.rotation = Quaternion.Euler(0, 90f, 0);
            }

            GameObject spawn2 = GameObject.Find("SpawnPoint_P2");
            if (spawn2 == null)
            {
                spawn2 = new GameObject("SpawnPoint_P2");
                spawn2.transform.position = new Vector3(2.5f, 0, 0);
                spawn2.transform.rotation = Quaternion.Euler(0, -90f, 0);
            }

            // 3. Ensure ST07_CollegeRaimon prefab at exact transform
            GameObject stageRoot = GameObject.Find("Arcade_Stage_Root");
            if (stageRoot == null) stageRoot = new GameObject("Arcade_Stage_Root");

            Transform st07 = stageRoot.transform.Find("ST07_CollegeRaimon");
            if (st07 == null)
            {
                // Instantiate ST07 prefab
                GameObject st07Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab");
                if (st07Prefab != null)
                {
                    GameObject instantiated = (GameObject)PrefabUtility.InstantiatePrefab(st07Prefab, stageRoot.transform);
                    instantiated.name = "ST07_CollegeRaimon";
                    instantiated.transform.localPosition = new Vector3(-1.71f, 90.5f, 6.46f);
                    instantiated.transform.localRotation = Quaternion.identity;
                    instantiated.transform.localScale = new Vector3(0.89f, 0.89f, 0.89f);
                }
            }
            else
            {
                st07.localPosition = new Vector3(-1.71f, 90.5f, 6.46f);
                st07.localRotation = Quaternion.identity;
                st07.localScale = new Vector3(0.89f, 0.89f, 0.89f);
            }

            // 4. Ensure Camera Rig matches exact settings
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                camObj.transform.localPosition = new Vector3(-0.05f, 1.05f, -3.55f);
                camObj.transform.localRotation = new Quaternion(0.024432179f, 0f, 0f, 0.9997015f);

                Camera cam = camObj.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.usePhysicalProperties = true;
                    cam.sensorSize = new Vector2(36f, 20.25f);
                    cam.focalLength = 32f;
                    cam.lensShift = new Vector2(0f, 0.035f);
                }

                FighterCamera2D fc = camObj.GetComponent<FighterCamera2D>();
                if (fc != null)
                {
                    fc.usePhysicalCamera = true;
                    fc.sensorSize = new Vector2(36f, 20.25f);
                    fc.focalLength = 32f;
                    fc.lensShift = new Vector2(0f, 0.035f);
                    fc.standardFighterHeight = 1.35f;
                    fc.targetCharacterScreenFraction = 0.59f;
                    fc.offset = new Vector3(-0.05f, 1.05f, -3.55f);
                    fc.pitchAngle = 2.8f;
                }
            }

            // 5. Ensure Battle_HUD_Canvas is active with HUD controller
            GameObject hudCanvas = GameObject.Find("Battle_HUD_Canvas");
            if (hudCanvas != null)
            {
                hudCanvas.SetActive(true);
                Canvas cv = hudCanvas.GetComponent<Canvas>();
                if (cv != null) cv.enabled = true;

                BattleHUDController hudCtrl = hudCanvas.GetComponent<BattleHUDController>();
                if (hudCtrl != null)
                {
                    hudCtrl.enabled = true;
                    hudCtrl.EnsurePauseModal();
                    hudCtrl.EnsureVictoryModal();
                }
            }

            // 6. Ensure exactly one EventSystem with InputSystemUIInputModule
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, SCENE_BATTLE_ARENA);
            Debug.Log($"<color=#00e676>[DemonIdolSceneBuilder] Built BattleArena successfully at {SCENE_BATTLE_ARENA}</color>");
        }

        // =========================================================================
        // Helpers
        // =========================================================================
        private static void CreateEventSystem()
        {
            var existing = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject esGo;
            if (existing.Length > 0)
            {
                esGo = existing[0].gameObject;
                for (int i = 1; i < existing.Length; i++)
                {
                    UnityEngine.Object.DestroyImmediate(existing[i].gameObject);
                }
            }
            else
            {
                esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
            }

            esGo.name = "EventSystem";
            esGo.SetActive(true);

            var oldModule = esGo.GetComponent<StandaloneInputModule>();
            if (oldModule != null) UnityEngine.Object.DestroyImmediate(oldModule);

            var inputModule = esGo.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = esGo.AddComponent<InputSystemUIInputModule>();
            inputModule.enabled = true;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos, Color normalColor, Font font)
        {
            GameObject btnGo = CreateUIObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(320, 50));
            Image img = btnGo.AddComponent<Image>();
            img.color = normalColor;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = normalColor * 1.25f;
            cb.pressedColor = normalColor * 0.8f;
            cb.selectedColor = normalColor * 1.25f;
            btn.colors = cb;

            GameObject lblGo = CreateUIObject("Text", btnGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text txt = lblGo.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private static Slider CreateSlider(Transform parent, string name, string label, Vector2 pos, Font font)
        {
            GameObject root = CreateUIObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(400, 40));

            GameObject lblGo = CreateUIObject("Label", root.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(140, 30));
            Text lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.text = label;
            lbl.fontSize = 16;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = Color.white;

            GameObject sliderGo = CreateUIObject("SliderObj", root.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-110, 0), new Vector2(200, 20));
            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject bg = CreateUIObject("Background", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 1f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            return slider;
        }

        private static Button CreateCardButton(Transform parent, string name, string title, string subtitle, Vector2 pos, Vector2 size, Color bgColor, Color accentColor, Font font)
        {
            GameObject cardGo = CreateUIObject(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pos, size);
            Image bg = cardGo.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = cardGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.35f;
            cb.pressedColor = bgColor * 0.8f;
            cb.selectedColor = bgColor * 1.35f;
            btn.colors = cb;

            // Accent bar on left
            GameObject barGo = CreateUIObject("AccentBar", cardGo.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(6f, 0f));
            Image barImg = barGo.AddComponent<Image>();
            barImg.color = accentColor;

            // Title
            GameObject titleGo = CreateUIObject("Title", cardGo.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(24f, 14f), new Vector2(-36f, 28f));
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = font;
            titleTxt.text = title;
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = SumiStyleTokens.PaperWhite;

            // Subtitle
            GameObject subGo = CreateUIObject("Subtitle", cardGo.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(24f, -14f), new Vector2(-36f, 24f));
            Text subTxt = subGo.AddComponent<Text>();
            subTxt.font = font;
            subTxt.text = subtitle;
            subTxt.fontSize = 13;
            subTxt.alignment = TextAnchor.MiddleLeft;
            subTxt.color = SumiStyleTokens.PaperWash;

            return btn;
        }

        private static Slider CreateStatBar(Transform parent, string name, string label, Vector2 pos, Color fillColor, Font font)
        {
            GameObject root = CreateUIObject(name, parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), pos, new Vector2(-40f, 24f));

            GameObject lblGo = CreateUIObject("Label", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(80f, 20f));
            Text lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.text = label;
            lbl.fontSize = 12;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = SumiStyleTokens.PaperWhite;

            GameObject sliderGo = CreateUIObject("BarObj", root.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(160f, 12f));
            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;

            GameObject bg = CreateUIObject("Background", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.16f, 0.22f, 1f);

            GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            slider.fillRect = fill.GetComponent<RectTransform>();

            return slider;
        }

        private static Button CreateTabButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Font font)
        {
            GameObject btnGo = CreateUIObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            Image img = btnGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.12f, 0.13f, 0.18f, 0.95f);
            cb.highlightedColor = SumiStyleTokens.CarbonInk;
            cb.pressedColor = SumiStyleTokens.CarbonInk * 0.8f;
            cb.selectedColor = SumiStyleTokens.CarbonInk;
            btn.colors = cb;

            GameObject lblGo = CreateUIObject("Text", btnGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text txt = lblGo.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = SumiStyleTokens.PaperWhite;

            return btn;
        }
    }
}
#endif
