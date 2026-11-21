#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DemonIdol.UI;
using DemonIdol.Combat;
using DemonIdol.Network;
using DemonIdol.Input;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Stitches Spooky Horror blood aesthetics with Sci-Fi Tactical UI
    /// into a complete, ready-to-run Master Canvas in the active scene:
    /// Loading Screen, Mode Selection with PERSISTENT click handlers,
    /// 6-Fighter Character Selection Screen, In-Combat Battle HUD
    /// (Health bars, Lag chip drain, Super meters, 99s timer, Round banners,
    /// Combo counter), Low-latency Touch Combat Controls, PvP Lobby,
    /// and Landscape-Locked Settings.
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolUIBuilder
    {
        static DemonIdolUIBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    ForceEnsureMasterUI();
                }
            };
        }

        [InitializeOnLoadMethod]
        public static void ForceEnsureMasterUI()
        {
            if (!System.IO.File.Exists("Assets/Scenes/Home.unity")) return;
            var homeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (homeScene.path != "Assets/Scenes/Home.unity")
            {
                homeScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Home.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
            var uiMgr = Object.FindAnyObjectByType<BloodTechUIManager>(FindObjectsInactive.Include);
            if (uiMgr != null)
            {
                var modePanel = uiMgr.transform.Find("Panel_ModeSelect");
                var charPanel = uiMgr.transform.Find("Panel_CharacterSelect");
                var stagePanel = uiMgr.transform.Find("Panel_StageSelect");
                bool needsRebuild = false;
                if (modePanel != null && modePanel.Find("Card_ModeArcade") == null)
                {
                    needsRebuild = true;
                }
                if (charPanel == null || charPanel.Find("DetailBox/Pill_ActiveSelection") == null)
                {
                    needsRebuild = true;
                }
                if (stagePanel == null || stagePanel.Find("DetailBox/Pill_ActiveStatus") == null)
                {
                    needsRebuild = true;
                }
                if (!System.IO.Directory.Exists("Assets/Prefabs/Characters") || !System.IO.Directory.Exists("Assets/Prefabs/Stages") || !System.IO.File.Exists("Assets/Prefabs/Characters/C01_Akio.prefab") || !System.IO.File.Exists("Assets/Prefabs/Stages/ST01_ReactorCoreSilo.prefab"))
                {
                    needsRebuild = true;
                }
                if (needsRebuild)
                {
                    Debug.Log("<color=#00e676>[DemonIdolUIBuilder] ForceEnsureMasterUI auto-rebuilding Stage Select and Master UI...</color>");
                    BuildMasterUI();
                }
            }
        }
        private static readonly Color DarkBgColor = new Color(0.06f, 0.06f, 0.08f, 0.98f);
        private static readonly Color BloodRedColor = new Color(0.78f, 0.22f, 0.18f, 1.0f);
        private static readonly Color SciFiSlateColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
        private static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.95f, 1.0f);
        private static readonly Color GoldAccent = new Color(0.85f, 0.72f, 0.35f, 1.0f);
        private static readonly Color NeonCyan = new Color(0.2f, 0.8f, 1.0f, 1.0f);
        private static readonly Color LagYellow = new Color(0.95f, 0.85f, 0.25f, 1.0f);

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Build Complete Master UI Canvas (With Roster & Battle HUD)", priority = 1)]
        public static void BuildMasterUI()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            // Ensure valid scene is open and active
            if (System.IO.File.Exists("Assets/Scenes/Home.unity"))
            {
                var homeScene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath("Assets/Scenes/Home.unity");
                if (!homeScene.IsValid() || !homeScene.isLoaded)
                {
                    homeScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Home.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
                }
                UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(homeScene);
                activeScene = homeScene;
            }

            // 0. Enforce Landscape Screen Orientation & Lock Portrait
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // 1. Ensure EventSystem exists
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                EventSystem eventSys = es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                UiEventSystemInputBootstrap.Reconcile(eventSys);
            }

            // Purge all legacy DemonLordAnnouncerWidgets and root-level orphaned panels from scene
            foreach (var widget in Object.FindObjectsByType<DemonLordAnnouncerWidget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(widget.gameObject);
            }
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name == "Panel_CharacterSelect" || go.name == "Panel_StageSelect" || go.name == "Panel_ModeSelect" || go.name == "Panel_BattleHUD" || go.name == "DemonLordNarratorWidget")
                {
                    Object.DestroyImmediate(go);
                }
            }

            // 2. Find or Create Master Canvas
            var existingUiMgr = Object.FindAnyObjectByType<BloodTechUIManager>(FindObjectsInactive.Include);
            GameObject canvasGo = existingUiMgr != null ? existingUiMgr.gameObject : GameObject.Find("Master_BloodTech_Canvas");
            if (canvasGo != null)
            {
                // Clean up previous UI children to prevent duplicate panel stacking
                for (int i = canvasGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(canvasGo.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                canvasGo = new GameObject("Master_BloodTech_Canvas");
            }

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

            BloodTechUIManager uiManager = canvasGo.GetComponent<BloodTechUIManager>();
            if (uiManager == null) uiManager = canvasGo.AddComponent<BloodTechUIManager>();

            // 3. Screen 1: Loading Screen Panel
            Transform loadingTrans = canvasGo.transform.Find("Panel_LoadingScreen");
            GameObject loadingGo = loadingTrans != null ? loadingTrans.gameObject : CreatePanel(canvasGo, "Panel_LoadingScreen", DarkBgColor);
            CanvasGroup loadingCg = GetOrAdd<CanvasGroup>(loadingGo);

            CreateText(loadingGo, "DEMON IDOL", new Vector2(0, 120), 64, BloodRedColor, FontStyle.Bold);
            CreateText(loadingGo, "NEURAL SYNCHRONIZATION...", new Vector2(0, 40), 22, GoldAccent, FontStyle.Normal);

            GameObject barGo = new GameObject("LoadingBar");
            barGo.transform.SetParent(loadingGo.transform, false);
            RectTransform barRect = barGo.AddComponent<RectTransform>();
            barRect.anchoredPosition = new Vector2(0, -60);
            barRect.sizeDelta = new Vector2(600, 18);

            Image barBg = barGo.AddComponent<Image>();
            barBg.color = new Color(0.15f, 0.15f, 0.18f, 1.0f);

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barGo.transform, false);
            RectTransform fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = BloodRedColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            Slider slider = barGo.AddComponent<Slider>();
            slider.targetGraphic = fillImg;
            slider.fillRect = fillRect;

            Text percentText = CreateText(loadingGo, "0%", new Vector2(0, -100), 20, TextWhite, FontStyle.Bold);
            loadingGo.SetActive(false);

            // 4. Screen 2: Simple Auth Panel (Bypassed)
            Transform authTrans = canvasGo.transform.Find("Panel_SimpleAuth");
            GameObject authGo = authTrans != null ? authTrans.gameObject : CreatePanel(canvasGo, "Panel_SimpleAuth", DarkBgColor);
            CanvasGroup authCg = GetOrAdd<CanvasGroup>(authGo);
            authGo.SetActive(false);

            // 5. Screen 3: Mode Select Screen Panel
            Transform modeTrans = canvasGo.transform.Find("Panel_ModeSelect");
            GameObject modeGo;
            if (modeTrans != null)
            {
                modeGo = modeTrans.gameObject;
                for (int i = modeGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(modeGo.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                modeGo = CreatePanel(canvasGo, "Panel_ModeSelect", DarkBgColor);
            }
            CanvasGroup modeCg = GetOrAdd<CanvasGroup>(modeGo);

            // Top Header: Operative Badge (Left)
            GameObject badgeBox = CreatePanel(modeGo, "Badge_Operative", new Color(0.08f, 0.10f, 0.15f, 0.95f));
            RectTransform badgeBoxRt = badgeBox.GetComponent<RectTransform>();
            badgeBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
            badgeBoxRt.anchorMax = new Vector2(0.5f, 0.5f);
            badgeBoxRt.anchoredPosition = new Vector2(-620f, 460f);
            badgeBoxRt.sizeDelta = new Vector2(360f, 54f);
            Text welcomeText = CreateText(badgeBox, "OPERATIVE: KAI", new Vector2(-40f, 0f), 20, GoldAccent, FontStyle.Bold);
            CreateText(badgeBox, "● READY", new Vector2(120f, 0f), 12, new Color(0.0f, 0.95f, 0.60f, 1f), FontStyle.Bold);

            // Top Header: Title & Subtitle (Center)
            CreateText(modeGo, "DEMON IDOL : BATTLE OPERATIONS", new Vector2(0f, 465f), 32, BloodRedColor, FontStyle.Bold);
            CreateText(modeGo, "DISCIPLINE MATRIX // DETERMINISTIC 60HZ ENGINE", new Vector2(0f, 435f), 13, NeonCyan, FontStyle.Bold);

            // Top Header: Specs Tag (Right)
            GameObject specsBox = CreatePanel(modeGo, "Badge_Specs", new Color(0.08f, 0.10f, 0.15f, 0.95f));
            RectTransform specsBoxRt = specsBox.GetComponent<RectTransform>();
            specsBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
            specsBoxRt.anchorMax = new Vector2(0.5f, 0.5f);
            specsBoxRt.anchoredPosition = new Vector2(620f, 460f);
            specsBoxRt.sizeDelta = new Vector2(320f, 54f);
            CreateText(specsBox, "[ HIGH REFRESH // 120 FPS ]", Vector2.zero, 13, new Color(0.6f, 0.65f, 0.75f, 0.9f), FontStyle.Bold);

            // The 4 Discipline Cards (Center Grid)
            // 1. 1v1 PvP Duel
            Button pvpBtn = CreateModeCard(
                modeGo,
                "Card_ModePvP",
                "Btn_ModePvP",
                "01 // NETPLAY",
                "⚔️",
                "1v1 PVP DUEL",
                "QUANTUM ROLLBACK",
                "Real-time deterministic netplay with instant rollback.\nCompete in Ranked Duels and Custom Lobbies.",
                new Vector2(-585f, -10f),
                NeonCyan,
                BloodRedColor
            );
            ClearAndAddPersistentListener(pvpBtn.onClick, uiManager.OnClickModePvP);

            // 2. Arcade Battle
            Button arcadeBtn = CreateModeCard(
                modeGo,
                "Card_ModeArcade",
                "Btn_ModeArcade",
                "02 // TOURNAMENT",
                "🏆",
                "ARCADE BATTLE",
                "CHAMPIONSHIP LADDER",
                "Classic fighting game arcade ladder.\nDefeat 7 consecutive sorcerers to claim the Demon Throne.",
                new Vector2(-195f, -10f),
                GoldAccent,
                new Color(0.88f, 0.45f, 0.10f, 1f)
            );
            ClearAndAddPersistentListener(arcadeBtn.onClick, uiManager.OnClickModeArcade);

            // 3. Story Campaign
            Button storyBtn = CreateModeCard(
                modeGo,
                "Card_ModeStory",
                "Btn_ModeStory",
                "03 // CHRONICLES",
                "📜",
                "STORY CAMPAIGN",
                "OCCULT EPISODES",
                "Infiltrate the Tokyo dark sorcery underground.\nUncover forbidden pacts across narrative boss battles.",
                new Vector2(195f, -10f),
                new Color(0.75f, 0.40f, 0.98f, 1f),
                new Color(0.48f, 0.18f, 0.72f, 1f)
            );
            ClearAndAddPersistentListener(storyBtn.onClick, uiManager.OnClickModeStory);

            // 4. Training Dojo
            Button trainingBtn = CreateModeCard(
                modeGo,
                "Card_ModeTraining",
                "Btn_ModeTraining",
                "04 // PRACTICE",
                "🥋",
                "TRAINING DOJO",
                "COMBAT LAB & FRAMES",
                "Master gatlings, drive cancels, Domain Expansions,\nand frame-1 parry execution with live frame displays.",
                new Vector2(585f, -10f),
                new Color(0.15f, 0.95f, 0.65f, 1f),
                new Color(0.12f, 0.52f, 0.42f, 1f)
            );
            ClearAndAddPersistentListener(trainingBtn.onClick, uiManager.OnClickModeTraining);

            // Bottom Navigation Dock: Left specs text
            CreateText(modeGo, "60HZ DETERMINISTIC SIMULATION • ZERO-GC ALLOCATION • ROLLBACK QUALITY A+", new Vector2(-350f, -460f), 13, new Color(0.55f, 0.60f, 0.70f, 0.85f), FontStyle.Normal);

            // 5. System Settings Button
            GameObject settingsBtnGo = CreateButton(modeGo, "Btn_Settings", "⚙️ SYSTEM SETTINGS", new Vector2(620f, -460f), new Vector2(300f, 54f), new Color(0.14f, 0.17f, 0.24f, 1f));
            Button settingsBtn = settingsBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(settingsBtn.onClick, uiManager.OnClickOpenSettings);

            modeGo.SetActive(false);

            // 6. Screen 4: Character Selection Screen Panel (Valorant / Tekken / Street Fighter style)
            Transform charSelectTrans = canvasGo.transform.Find("Panel_CharacterSelect");
            GameObject charSelectGo = charSelectTrans != null ? charSelectTrans.gameObject : CreatePanel(canvasGo, "Panel_CharacterSelect", DarkBgColor);
            if (charSelectTrans != null)
            {
                // Clear any previous child elements to prevent duplicate stacking
                for (int i = charSelectGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(charSelectGo.transform.GetChild(i).gameObject);
                }
            }

            CanvasGroup charSelectCg = GetOrAdd<CanvasGroup>(charSelectGo);
            CharacterSelectUI charSelectUi = GetOrAdd<CharacterSelectUI>(charSelectGo);

            // 1. Top Header Bar
            CreateText(charSelectGo, "// OPERATIVE SELECTION", new Vector2(-600f, 475f), 32, TextWhite, FontStyle.Bold);
            CreateText(charSelectGo, "TACTICAL COMBAT ROSTER // 60HZ DETERMINISTIC NETCODE", new Vector2(-600f, 444f), 13, NeonCyan, FontStyle.Normal);

            // Mode Badge
            GameObject modeBadge = CreatePanel(charSelectGo, "Badge_GameMode", new Color(0.12f, 0.16f, 0.22f, 0.95f));
            RectTransform mbrt = modeBadge.GetComponent<RectTransform>();
            mbrt.sizeDelta = new Vector2(260f, 38f);
            mbrt.anchoredPosition = new Vector2(720f, 470f);
            GameObject mbBorder = CreatePanel(modeBadge, "Border", NeonCyan);
            mbBorder.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            mbBorder.GetComponent<RectTransform>().anchorMax = Vector2.one;
            mbBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 2f);
            CreateText(modeBadge, "[ COMBAT MATRIX ACTIVE ]", Vector2.zero, 13, GoldAccent, FontStyle.Bold);

            // 2. Player Target Switcher Tabs (Valorant / SF P1 & P2 toggles)
            GameObject tabP1Go = CreateButton(charSelectGo, "Btn_TabP1", "▶ [ P1: AKIO ]", new Vector2(60f, 470f), new Vector2(220f, 42f), new Color(0.08f, 0.18f, 0.28f, 0.95f));
            Button tabP1Btn = tabP1Go.GetComponent<Button>();
            ClearAndAddPersistentListener(tabP1Btn.onClick, charSelectUi.SetSelectingP1);
            GameObject tabP1BorderGo = CreatePanel(tabP1Go, "Border", NeonCyan);
            tabP1BorderGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            tabP1BorderGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            tabP1BorderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 4f);
            Text tabP1Text = tabP1Go.GetComponentInChildren<Text>();

            GameObject tabP2Go = CreateButton(charSelectGo, "Btn_TabP2", "[ P2: AIKA ]", new Vector2(300f, 470f), new Vector2(220f, 42f), new Color(0.24f, 0.10f, 0.14f, 0.95f));
            Button tabP2Btn = tabP2Go.GetComponent<Button>();
            ClearAndAddPersistentListener(tabP2Btn.onClick, charSelectUi.SetSelectingP2);
            GameObject tabP2BorderGo = CreatePanel(tabP2Go, "Border", BloodRedColor);
            tabP2BorderGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            tabP2BorderGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            tabP2BorderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 4f);
            Text tabP2Text = tabP2Go.GetComponentInChildren<Text>();

            // 3. Left Section: Operative Dossier / Spotlight Card (Valorant Agent Profile / Tekken Dossier)
            GameObject detailBox = CreatePanel(charSelectGo, "DetailBox", new Color(0.09f, 0.11f, 0.16f, 0.98f));
            RectTransform dboxRt = detailBox.GetComponent<RectTransform>();
            dboxRt.sizeDelta = new Vector2(560f, 740f);
            dboxRt.anchoredPosition = new Vector2(-520f, -10f);

            GameObject dTop = CreatePanel(detailBox, "TopAccentLine", BloodRedColor);
            RectTransform dtRt = dTop.GetComponent<RectTransform>();
            dtRt.sizeDelta = new Vector2(560f, 4f);
            dtRt.anchoredPosition = new Vector2(0f, 368f);

            CreateText(detailBox, "// INTEL DOSSIER • OPERATIVE C01", new Vector2(-120f, 335f), 11, NeonCyan, FontStyle.Bold);

            GameObject activePill = CreatePanel(detailBox, "Pill_ActiveSelection", new Color(0f, 0.90f, 1f, 0.15f));
            RectTransform aprt = activePill.GetComponent<RectTransform>();
            aprt.sizeDelta = new Vector2(170f, 26f);
            aprt.anchoredPosition = new Vector2(170f, 335f);
            Text activeBadge = CreateText(activePill, "[ P1 SELECTION ]", Vector2.zero, 12, NeonCyan, FontStyle.Bold);

            Text detailName = CreateText(detailBox, "AKIO", new Vector2(-50f, 280f), 48, TextWhite, FontStyle.Bold);
            detailName.alignment = TextAnchor.MiddleLeft;

            Text detailTitle = CreateText(detailBox, "SHADOW CHIMERA PRODIGY", new Vector2(-50f, 235f), 16, GoldAccent, FontStyle.Bold);
            detailTitle.alignment = TextAnchor.MiddleLeft;

            Text detailArch = CreateText(detailBox, "ARCHETYPE: RUSHDOWN / MIXUP", new Vector2(-50f, 202f), 14, NeonCyan, FontStyle.Normal);
            detailArch.alignment = TextAnchor.MiddleLeft;

            Text detailDesc = CreateTextWrapped(detailBox, "Highschool prodigy summoning shikigami shadows, setting up unseeable pincer strikes and shadow puddle mixups.", new Vector2(0f, 130f), new Vector2(500f, 65f), 13, new Color(0.72f, 0.78f, 0.88f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject div1 = CreatePanel(detailBox, "Div1", new Color(1f, 1f, 1f, 0.1f));
            div1.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 2f);
            div1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);

            CreateText(detailBox, "// COMBAT PARAMETERS", new Vector2(-160f, 58f), 11, new Color(0.5f, 0.58f, 0.7f, 1f), FontStyle.Bold);

            var (speedBar, speedVal) = CreateDetailedStatRow(detailBox, "SPEED", new Vector2(0f, 25f), NeonCyan);
            var (powerBar, powerVal) = CreateDetailedStatRow(detailBox, "POWER", new Vector2(0f, -10f), BloodRedColor);
            var (reachBar, reachVal) = CreateDetailedStatRow(detailBox, "REACH", new Vector2(0f, -45f), GoldAccent);
            var (defBar, defVal) = CreateDetailedStatRow(detailBox, "DEFENSE", new Vector2(0f, -80f), new Color(0.0f, 0.9f, 0.46f, 1f));

            GameObject div2 = CreatePanel(detailBox, "Div2", new Color(1f, 1f, 1f, 0.1f));
            div2.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 2f);
            div2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);

            GameObject domainBox = CreatePanel(detailBox, "DomainBox", new Color(0.06f, 0.08f, 0.12f, 0.95f));
            RectTransform dbox = domainBox.GetComponent<RectTransform>();
            dbox.sizeDelta = new Vector2(500f, 140f);
            dbox.anchoredPosition = new Vector2(0f, -220f);

            GameObject dBorder = CreatePanel(domainBox, "Border", new Color(0.92f, 0.22f, 0.20f, 0.35f));
            dBorder.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            dBorder.GetComponent<RectTransform>().anchorMax = Vector2.one;
            dBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 2f);

            GameObject dAccent = CreatePanel(domainBox, "Accent", BloodRedColor);
            dAccent.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 2f);
            dAccent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 69f);

            Text domainName = CreateText(domainBox, "⚡ DOMAIN: CHIMERA SHADOW GARDEN", new Vector2(0f, 38f), 16, BloodRedColor, FontStyle.Bold);
            Text domainDesc = CreateTextWrapped(domainBox, "Floods the stage with liquid darkness, spawning twin shikigami clones delivering inescapable high-low crossups.", new Vector2(0f, -15f), new Vector2(460f, 60f), 12, TextWhite, FontStyle.Normal, TextAnchor.MiddleCenter);

            // 4. Right Section: 14 Roster Cards (2 rows of 7, Valorant / SF style)
            int rosterCount = FighterRosterCatalog.Roster.Length;
            Button[] rosterBtns = new Button[rosterCount];
            Image[] rosterHighlights = new Image[rosterCount];
            Text[] rosterNames = new Text[rosterCount];
            Image[] rosterP1Badges = new Image[rosterCount];
            Image[] rosterP2Badges = new Image[rosterCount];

            int cols = 7;
            float cardW = 122f;
            float cardH = 220f;
            float spacingX = 14f;
            float totalW = (cols * cardW) + ((cols - 1) * spacingX);
            float startX = 360f - (totalW * 0.5f) + (cardW * 0.5f);

            for (int i = 0; i < rosterCount; i++)
            {
                var profile = FighterRosterCatalog.Roster[i];
                int col = i % cols;
                int row = i / cols;
                float x = startX + (col * (cardW + spacingX));
                float y = (row == 0) ? 140f : -100f;

                GameObject card = CreatePanel(charSelectGo, $"Card_{profile.displayName}", new Color(0.10f, 0.13f, 0.19f, 0.95f));
                RectTransform crt = card.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(cardW, cardH);
                crt.anchoredPosition = new Vector2(x, y);

                GameObject topAccent = CreatePanel(card, "TopAccent", profile.themeColor);
                RectTransform taRt = topAccent.GetComponent<RectTransform>();
                taRt.sizeDelta = new Vector2(cardW, 4f);
                taRt.anchoredPosition = new Vector2(0f, 108f);

                CreateText(card, profile.id, new Vector2(-36f, 92f), 10, NeonCyan, FontStyle.Bold);

                GameObject portraitBox = CreatePanel(card, "PortraitBox", new Color(0.06f, 0.08f, 0.12f, 0.9f));
                RectTransform pbrt = portraitBox.GetComponent<RectTransform>();
                pbrt.sizeDelta = new Vector2(106f, 95f);
                pbrt.anchoredPosition = new Vector2(0f, 26f);

                string initials = profile.displayName.Length >= 2 ? profile.displayName.Substring(0, 2) : profile.displayName;
                CreateText(portraitBox, initials, Vector2.zero, 32, new Color(0.45f, 0.55f, 0.70f, 0.35f), FontStyle.Bold);

                Text cardTitle = CreateText(card, profile.displayName, new Vector2(0f, -44f), 15, TextWhite, FontStyle.Bold);
                rosterNames[i] = cardTitle;

                string shortArch = profile.archetype.Split('/')[0].Trim();
                CreateText(card, shortArch, new Vector2(0f, -68f), 9, GoldAccent, FontStyle.Normal);

                CreateText(card, "[ READY ]", new Vector2(0f, -88f), 10, new Color(0.35f, 0.45f, 0.58f, 1f), FontStyle.Bold);

                GameObject border = CreatePanel(card, "HighlightBorder", NeonCyan);
                RectTransform brt = border.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.sizeDelta = new Vector2(6f, 6f);
                Image borderImg = border.GetComponent<Image>();
                rosterHighlights[i] = borderImg;
                borderImg.color = (i == 0) ? NeonCyan : (i == 1 ? BloodRedColor : new Color(1f, 1f, 1f, 0.08f));

                GameObject p1bGo = CreatePanel(card, "Badge_P1", NeonCyan);
                p1bGo.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 18f);
                p1bGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-40f, 92f);
                CreateText(p1bGo, "P1", Vector2.zero, 11, new Color(0.05f, 0.05f, 0.08f, 1f), FontStyle.Bold);
                rosterP1Badges[i] = p1bGo.GetComponent<Image>();
                p1bGo.SetActive(i == 0);

                GameObject p2bGo = CreatePanel(card, "Badge_P2", BloodRedColor);
                p2bGo.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 18f);
                p2bGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(40f, 92f);
                CreateText(p2bGo, "P2", Vector2.zero, 11, TextWhite, FontStyle.Bold);
                rosterP2Badges[i] = p2bGo.GetComponent<Image>();
                p2bGo.SetActive(i == 1);

                Button cardBtn = card.AddComponent<Button>();
                cardBtn.targetGraphic = card.GetComponent<Image>();
                ClearAndAddPersistentListenerInt(cardBtn.onClick, charSelectUi.SelectFighter, i);
                rosterBtns[i] = cardBtn;
            }

            // 5. Bottom Navigation Dock
            GameObject backModesBtnGo = CreateButton(charSelectGo, "Btn_BackToModes", "◀ BACK TO HUB", new Vector2(-520f, -450f), new Vector2(240f, 56f), SciFiSlateColor);
            Button backModesBtn = backModesBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(backModesBtn.onClick, charSelectUi.OnClickBack);

            GameObject deployBtnGo = CreateButton(charSelectGo, "Btn_DeployToBattle", "LOCK IN OPERATIVE ▶", new Vector2(360f, -450f), new Vector2(560f, 64f), BloodRedColor);
            Button deployBtn = deployBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(deployBtn.onClick, charSelectUi.OnClickDeploy);
            Text deployBtnText = deployBtnGo.GetComponentInChildren<Text>();
            deployBtnText.fontSize = 22;

            // 6. SerializedObject wiring for CharacterSelectUI
            SerializedObject soChar = new SerializedObject(charSelectUi);
            soChar.FindProperty("_detailNameText").objectReferenceValue = detailName;
            soChar.FindProperty("_detailTitleText").objectReferenceValue = detailTitle;
            soChar.FindProperty("_detailArchetypeText").objectReferenceValue = detailArch;
            soChar.FindProperty("_detailDescriptionText").objectReferenceValue = detailDesc;
            soChar.FindProperty("_activeSelectionBadgeText").objectReferenceValue = activeBadge;
            soChar.FindProperty("_speedBarFill").objectReferenceValue = speedBar;
            soChar.FindProperty("_powerBarFill").objectReferenceValue = powerBar;
            soChar.FindProperty("_reachBarFill").objectReferenceValue = reachBar;
            soChar.FindProperty("_defenseBarFill").objectReferenceValue = defBar;
            soChar.FindProperty("_speedValueText").objectReferenceValue = speedVal;
            soChar.FindProperty("_powerValueText").objectReferenceValue = powerVal;
            soChar.FindProperty("_reachValueText").objectReferenceValue = reachVal;
            soChar.FindProperty("_defenseValueText").objectReferenceValue = defVal;
            soChar.FindProperty("_domainNameText").objectReferenceValue = domainName;
            soChar.FindProperty("_domainDescText").objectReferenceValue = domainDesc;
            soChar.FindProperty("_btnDeploy").objectReferenceValue = deployBtn;
            soChar.FindProperty("_btnBackToModes").objectReferenceValue = backModesBtn;
            soChar.FindProperty("_deployButtonText").objectReferenceValue = deployBtnText;
            soChar.FindProperty("_btnTabP1").objectReferenceValue = tabP1Btn;
            soChar.FindProperty("_btnTabP2").objectReferenceValue = tabP2Btn;
            soChar.FindProperty("_tabP1Text").objectReferenceValue = tabP1Text;
            soChar.FindProperty("_tabP2Text").objectReferenceValue = tabP2Text;
            soChar.FindProperty("_tabP1Border").objectReferenceValue = tabP1BorderGo.GetComponent<Image>();
            soChar.FindProperty("_tabP2Border").objectReferenceValue = tabP2BorderGo.GetComponent<Image>();

            var rBtnsProp = soChar.FindProperty("_rosterButtons");
            rBtnsProp.arraySize = rosterCount;
            for (int i = 0; i < rosterCount; i++) rBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = rosterBtns[i];

            var rHighProp = soChar.FindProperty("_rosterHighlights");
            rHighProp.arraySize = rosterCount;
            for (int i = 0; i < rosterCount; i++) rHighProp.GetArrayElementAtIndex(i).objectReferenceValue = rosterHighlights[i];

            var rNamesProp = soChar.FindProperty("_rosterNames");
            rNamesProp.arraySize = rosterCount;
            for (int i = 0; i < rosterCount; i++) rNamesProp.GetArrayElementAtIndex(i).objectReferenceValue = rosterNames[i];

            var rP1BadgesProp = soChar.FindProperty("_rosterP1Badges");
            rP1BadgesProp.arraySize = rosterCount;
            for (int i = 0; i < rosterCount; i++) rP1BadgesProp.GetArrayElementAtIndex(i).objectReferenceValue = rosterP1Badges[i];

            var rP2BadgesProp = soChar.FindProperty("_rosterP2Badges");
            rP2BadgesProp.arraySize = rosterCount;
            for (int i = 0; i < rosterCount; i++) rP2BadgesProp.GetArrayElementAtIndex(i).objectReferenceValue = rosterP2Badges[i];

            var prefabsProp = soChar.FindProperty("_characterPrefabs");
            if (prefabsProp != null)
            {
                prefabsProp.arraySize = FighterRosterCatalog.Roster.Length;
                for (int i = 0; i < FighterRosterCatalog.Roster.Length; i++)
                {
                    GameObject pAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FighterRosterCatalog.Roster[i].prefabPath);
                    prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = pAsset;
                }
            }

            soChar.ApplyModifiedProperties();

            charSelectGo.SetActive(false);

            // 6B. Screen 5: Arena Stage Selection Screen Panel (Valorant / Tekken / Street Fighter Style)
            Transform stageSelectTrans = canvasGo.transform.Find("Panel_StageSelect");
            GameObject stageSelectGo = stageSelectTrans != null ? stageSelectTrans.gameObject : CreatePanel(canvasGo, "Panel_StageSelect", DarkBgColor);
            if (stageSelectTrans != null)
            {
                // Clear any previous child elements to prevent duplicate stacking
                for (int i = stageSelectGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(stageSelectGo.transform.GetChild(i).gameObject);
                }
            }

            CanvasGroup stageSelectCg = GetOrAdd<CanvasGroup>(stageSelectGo);
            StageSelectUI stageSelectUi = GetOrAdd<StageSelectUI>(stageSelectGo);

            // 1. Top Header Bar
            Text stScreenTitle = CreateText(stageSelectGo, "// ARENA SELECTION", new Vector2(-600f, 475f), 32, TextWhite, FontStyle.Bold);
            Text stScreenSub = CreateText(stageSelectGo, "TACTICAL COMBAT THEATRES // 60HZ DETERMINISTIC ROLLBACK", new Vector2(-600f, 444f), 13, NeonCyan, FontStyle.Normal);

            // Mode Badge
            GameObject stModeBadge = CreatePanel(stageSelectGo, "Badge_GameMode", new Color(0.12f, 0.16f, 0.22f, 0.95f));
            RectTransform smbrt = stModeBadge.GetComponent<RectTransform>();
            smbrt.sizeDelta = new Vector2(260f, 38f);
            smbrt.anchoredPosition = new Vector2(720f, 470f);
            GameObject smbBorder = CreatePanel(stModeBadge, "Border", NeonCyan);
            smbBorder.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            smbBorder.GetComponent<RectTransform>().anchorMax = Vector2.one;
            smbBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 2f);
            Text stModeBadgeTxt = CreateText(stModeBadge, "[ ARENA MATRIX ONLINE ]", Vector2.zero, 13, GoldAccent, FontStyle.Bold);

            // 2. Left Section: Arena Intel Dossier (Valorant / Tekken style)
            GameObject stDetailBox = CreatePanel(stageSelectGo, "DetailBox", new Color(0.09f, 0.11f, 0.16f, 0.98f));
            RectTransform sdtRt = stDetailBox.GetComponent<RectTransform>();
            sdtRt.sizeDelta = new Vector2(560f, 740f);
            sdtRt.anchoredPosition = new Vector2(-520f, -10f);

            GameObject sdtTop = CreatePanel(stDetailBox, "TopAccentLine", BloodRedColor);
            RectTransform stTopRt = sdtTop.GetComponent<RectTransform>();
            stTopRt.sizeDelta = new Vector2(560f, 4f);
            stTopRt.anchoredPosition = new Vector2(0f, 368f);

            Text stSerialTxt = CreateText(stDetailBox, "// SECTOR DOSSIER • ARENA ST01", new Vector2(-120f, 335f), 11, NeonCyan, FontStyle.Bold);

            GameObject stActivePill = CreatePanel(stDetailBox, "Pill_ActiveStatus", new Color(0f, 0.90f, 1f, 0.15f));
            RectTransform stAprt = stActivePill.GetComponent<RectTransform>();
            stAprt.sizeDelta = new Vector2(200f, 26f);
            stAprt.anchoredPosition = new Vector2(150f, 335f);
            Text stStatusTxt = CreateText(stActivePill, "[ COMBAT ZONE AUTHORIZED ]", Vector2.zero, 12, NeonCyan, FontStyle.Bold);

            Text stNameTxt = CreateText(stDetailBox, "REACTOR CORE SILO", new Vector2(-50f, 280f), 40, TextWhite, FontStyle.Bold);
            stNameTxt.alignment = TextAnchor.MiddleLeft;

            Text stSubTxt = CreateText(stDetailBox, "SECTOR 7 // INDUSTRIAL CONDUIT", new Vector2(-50f, 235f), 16, GoldAccent, FontStyle.Bold);
            stSubTxt.alignment = TextAnchor.MiddleLeft;

            Text stHazardTxt = CreateText(stDetailBox, "[ HIGH-VOLTAGE CONDUIT • WALL-SPLAT ACTIVE ]", new Vector2(-50f, 202f), 13, NeonCyan, FontStyle.Normal);
            stHazardTxt.alignment = TextAnchor.MiddleLeft;

            Text stDescTxt = CreateTextWrapped(stDetailBox, "Deep subterranean reactor shaft with high-voltage vertical containment lines, conductive blast floors, and heavy acoustic reverberation.", new Vector2(0f, 130f), new Vector2(500f, 65f), 13, new Color(0.72f, 0.78f, 0.88f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject stDiv1 = CreatePanel(stDetailBox, "Div1", new Color(1f, 1f, 1f, 0.1f));
            stDiv1.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 2f);
            stDiv1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);

            CreateText(stDetailBox, "// ARENA COMBAT PARAMETERS", new Vector2(-150f, 58f), 11, new Color(0.5f, 0.58f, 0.7f, 1f), FontStyle.Bold);

            var (wBar, wVal) = CreateDetailedStatRow(stDetailBox, "WIDTH", new Vector2(0f, 25f), NeonCyan);
            var (wlBar, wlVal) = CreateDetailedStatRow(stDetailBox, "WALL SPLAT", new Vector2(0f, -10f), BloodRedColor);
            var (cBar, cVal) = CreateDetailedStatRow(stDetailBox, "CAMERA", new Vector2(0f, -45f), GoldAccent);
            var (sfBar, sfVal) = CreateDetailedStatRow(stDetailBox, "SURFACE", new Vector2(0f, -80f), new Color(0.0f, 0.9f, 0.46f, 1f));

            GameObject stDiv2 = CreatePanel(stDetailBox, "Div2", new Color(1f, 1f, 1f, 0.1f));
            stDiv2.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 2f);
            stDiv2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);

            GameObject bgmBox = CreatePanel(stDetailBox, "BGMBox", new Color(0.06f, 0.08f, 0.12f, 0.95f));
            RectTransform bgmRt = bgmBox.GetComponent<RectTransform>();
            bgmRt.sizeDelta = new Vector2(500f, 140f);
            bgmRt.anchoredPosition = new Vector2(0f, -220f);

            GameObject bgmBorder = CreatePanel(bgmBox, "Border", new Color(0.85f, 0.72f, 0.35f, 0.35f));
            bgmBorder.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgmBorder.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bgmBorder.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 2f);

            GameObject bgmTopAccent = CreatePanel(bgmBox, "Accent", GoldAccent);
            bgmTopAccent.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 2f);
            bgmTopAccent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 69f);

            Text bgmTitle = CreateText(bgmBox, "🎵 BGM: REACTOR CORE SILO", new Vector2(0f, 38f), 16, GoldAccent, FontStyle.Bold);
            Text bgmSub = CreateTextWrapped(bgmBox, "60HZ DYNAMIC STEREO // 128 BPM HIGH-OCTANE OVERDRIVE", new Vector2(0f, -15f), new Vector2(460f, 60f), 12, TextWhite, FontStyle.Normal, TextAnchor.MiddleCenter);

            // 3. Right Section: 6 Active Stage Cards (3x2 Grid)
            string[] stageIds = new[] { "ST01", "ST02", "ST03", "ST04", "ST05", "ST06" };
            string[] stageNames = new[] { 
                "LOW POLY DESERT", 
                "RTS SCI-FI BASE", 
                "SPACE ALIEN WORLDS", 
                "CARTOON CITY", 
                "PIXELDRIFT SUBWAY", 
                "MODERN OFFICE TOWER" 
            };
            string[] stageArchetypes = new[] { 
                "CANYON DUNES", 
                "MECH GANTRY", 
                "CRYSTAL PLATEAU", 
                "DOWNTOWN CORNER", 
                "TRANSIT CONCOURSE", 
                "SKYLINE HELIPAD" 
            };
            string[] stageGlyphs = new[] { 
                "🏜 DESERT CANYON", 
                "🛰 SCI-FI GANTRY", 
                "🪐 ALIEN BIOSPHERE", 
                "🏙 URBAN STREET", 
                "🚇 METRO STATION", 
                "🏢 PENTHOUSE TOWER" 
            };
            Color[] stageColors = new[] { 
                new Color(0.95f, 0.72f, 0.35f, 1f), 
                NeonCyan, 
                new Color(0.75f, 0.35f, 0.95f, 1f), 
                new Color(0.35f, 0.85f, 0.45f, 1f), 
                new Color(0.95f, 0.45f, 0.25f, 1f), 
                new Color(0.40f, 0.65f, 0.95f, 1f) 
            };

            int activeCount = 6;
            Button[] stageBtns = new Button[activeCount];
            Image[] stageHighlights = new Image[activeCount];
            Text[] stageCardNames = new Text[activeCount];
            Text[] stageCardIds = new Text[activeCount];
            Text[] stageCardArchetypes = new Text[activeCount];

            float stCardW = 290f;
            float stCardH = 200f;
            float stSpacingX = 20f;
            float stSpacingY = 20f;
            float stTotalW = (3 * stCardW) + (2 * stSpacingX); // 910f
            float stStartX = 360f - (stTotalW * 0.5f) + (stCardW * 0.5f);

            for (int i = 0; i < activeCount; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x = stStartX + (col * (stCardW + stSpacingX));
                float y = (row == 0) ? 135f : -85f;

                GameObject card = CreatePanel(stageSelectGo, $"Card_Stage_{stageIds[i]}", new Color(0.10f, 0.13f, 0.19f, 0.95f));
                RectTransform crt = card.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(stCardW, stCardH);
                crt.anchoredPosition = new Vector2(x, y);

                GameObject topAccent = CreatePanel(card, "TopAccent", stageColors[i]);
                RectTransform taRt = topAccent.GetComponent<RectTransform>();
                taRt.sizeDelta = new Vector2(stCardW, 4f);
                taRt.anchoredPosition = new Vector2(0f, 98f);

                Text idTxt = CreateText(card, stageIds[i], new Vector2(-105f, 75f), 12, NeonCyan, FontStyle.Bold);
                stageCardIds[i] = idTxt;

                GameObject badgeAuth = CreatePanel(card, "Badge_Auth", new Color(0f, 0.9f, 0.46f, 0.15f));
                badgeAuth.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 22f);
                badgeAuth.GetComponent<RectTransform>().anchoredPosition = new Vector2(80f, 75f);
                CreateText(badgeAuth, "[ AUTHORIZED ]", Vector2.zero, 10, new Color(0.0f, 0.9f, 0.46f, 1f), FontStyle.Bold);

                GameObject glyphBox = CreatePanel(card, "GlyphBox", new Color(0.06f, 0.08f, 0.12f, 0.9f));
                RectTransform gbrt = glyphBox.GetComponent<RectTransform>();
                gbrt.sizeDelta = new Vector2(266f, 68f);
                gbrt.anchoredPosition = new Vector2(0f, 20f);
                CreateText(glyphBox, stageGlyphs[i], Vector2.zero, 18, new Color(0.55f, 0.65f, 0.80f, 0.4f), FontStyle.Bold);

                Text nameTxt = CreateText(card, stageNames[i], new Vector2(0f, -32f), 15, TextWhite, FontStyle.Bold);
                stageCardNames[i] = nameTxt;

                Text archTxt = CreateText(card, stageArchetypes[i], new Vector2(0f, -56f), 11, GoldAccent, FontStyle.Normal);
                stageCardArchetypes[i] = archTxt;

                GameObject border = CreatePanel(card, "HighlightBorder", NeonCyan);
                RectTransform brt = border.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.sizeDelta = new Vector2(6f, 6f);
                Image borderImg = border.GetComponent<Image>();
                stageHighlights[i] = borderImg;
                borderImg.color = (i == 0) ? NeonCyan : new Color(1f, 1f, 1f, 0.08f);

                Button cardBtn = card.AddComponent<Button>();
                cardBtn.targetGraphic = card.GetComponent<Image>();
                int capturedIndex = i;
                cardBtn.onClick.AddListener(() => stageSelectUi.SelectStage(capturedIndex));
                stageBtns[i] = cardBtn;
            }

            // 4. Bottom Navigation Dock
            GameObject backStageBtnGo = CreateButton(stageSelectGo, "Btn_BackToChars", "◀ BACK TO OPERATIVE SELECT", new Vector2(-520f, -450f), new Vector2(260f, 56f), SciFiSlateColor);
            Button backStageBtn = backStageBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(backStageBtn.onClick, stageSelectUi.OnClickBack);

            GameObject deployStageBtnGo = CreateButton(stageSelectGo, "Btn_DeployStage", "DEPLOY TO BATTLE ARENA ▶", new Vector2(360f, -450f), new Vector2(560f, 64f), BloodRedColor);
            Button deployStageBtn = deployStageBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(deployStageBtn.onClick, stageSelectUi.OnClickDeploy);
            Text deployStageBtnText = deployStageBtnGo.GetComponentInChildren<Text>();
            deployStageBtnText.fontSize = 22;

            // 5. SerializedObject wiring for StageSelectUI
            SerializedObject soStage = new SerializedObject(stageSelectUi);
            soStage.FindProperty("_canvasGroup").objectReferenceValue = stageSelectCg;
            soStage.FindProperty("_screenTitleText").objectReferenceValue = stScreenTitle;
            soStage.FindProperty("_screenSubtitleText").objectReferenceValue = stScreenSub;
            soStage.FindProperty("_modeBadgeText").objectReferenceValue = stModeBadgeTxt;
            soStage.FindProperty("_dossierSerialText").objectReferenceValue = stSerialTxt;
            soStage.FindProperty("_dossierStatusText").objectReferenceValue = stStatusTxt;
            soStage.FindProperty("_stageNameText").objectReferenceValue = stNameTxt;
            soStage.FindProperty("_stageSubtitleText").objectReferenceValue = stSubTxt;
            soStage.FindProperty("_stageHazardText").objectReferenceValue = stHazardTxt;
            soStage.FindProperty("_stageDescText").objectReferenceValue = stDescTxt;
            soStage.FindProperty("_widthBarFill").objectReferenceValue = wBar;
            soStage.FindProperty("_wallBarFill").objectReferenceValue = wlBar;
            soStage.FindProperty("_camBarFill").objectReferenceValue = cBar;
            soStage.FindProperty("_surfaceBarFill").objectReferenceValue = sfBar;
            soStage.FindProperty("_widthValueText").objectReferenceValue = wVal;
            soStage.FindProperty("_wallValueText").objectReferenceValue = wlVal;
            soStage.FindProperty("_camValueText").objectReferenceValue = cVal;
            soStage.FindProperty("_surfaceValueText").objectReferenceValue = sfVal;
            soStage.FindProperty("_stageBgmText").objectReferenceValue = bgmTitle;
            soStage.FindProperty("_stageBgmSubtext").objectReferenceValue = bgmSub;
            soStage.FindProperty("_btnDeploy").objectReferenceValue = deployStageBtn;
            soStage.FindProperty("_btnBack").objectReferenceValue = backStageBtn;
            soStage.FindProperty("_deployButtonText").objectReferenceValue = deployStageBtnText;

            var sBtnsProp = soStage.FindProperty("_stageButtons");
            sBtnsProp.arraySize = activeCount;
            for (int i = 0; i < activeCount; i++) sBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = stageBtns[i];

            var sHighProp = soStage.FindProperty("_stageHighlights");
            sHighProp.arraySize = activeCount;
            for (int i = 0; i < activeCount; i++) sHighProp.GetArrayElementAtIndex(i).objectReferenceValue = stageHighlights[i];

            var sNamesProp = soStage.FindProperty("_stageCardNames");
            sNamesProp.arraySize = activeCount;
            for (int i = 0; i < activeCount; i++) sNamesProp.GetArrayElementAtIndex(i).objectReferenceValue = stageCardNames[i];

            var sIdsProp = soStage.FindProperty("_stageCardIds");
            sIdsProp.arraySize = activeCount;
            for (int i = 0; i < activeCount; i++) sIdsProp.GetArrayElementAtIndex(i).objectReferenceValue = stageCardIds[i];

            var sArchsProp = soStage.FindProperty("_stageCardArchetypes");
            sArchsProp.arraySize = activeCount;
            for (int i = 0; i < activeCount; i++) sArchsProp.GetArrayElementAtIndex(i).objectReferenceValue = stageCardArchetypes[i];

            soStage.ApplyModifiedProperties();

            stageSelectGo.SetActive(false);

            // 7. Screen 5: In-Combat Battle HUD
            Transform hudTrans = canvasGo.transform.Find("Panel_BattleHUD");
            GameObject hudGo = hudTrans != null ? hudTrans.gameObject : CreatePanel(canvasGo, "Panel_BattleHUD", Color.clear);
            if (hudTrans != null)
            {
                // Clear any previous child overlays to prevent duplicate TouchControlsOverlay / HUD stacking
                for (int i = hudGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(hudGo.transform.GetChild(i).gameObject);
                }
            }
            CanvasGroup hudCg = GetOrAdd<CanvasGroup>(hudGo);
            BattleHUDController hudCtrl = GetOrAdd<BattleHUDController>(hudGo);

            // Top Bar: P1 Health, Timer, P2 Health
            GameObject p1BarGo = CreatePanel(hudGo, "P1_HealthBar", new Color(0.12f, 0.12f, 0.16f, 0.95f));
            RectTransform p1BarRt = p1BarGo.GetComponent<RectTransform>();
            p1BarRt.anchorMin = new Vector2(0, 1);
            p1BarRt.anchorMax = new Vector2(0, 1);
            p1BarRt.sizeDelta = new Vector2(650, 42);
            p1BarRt.anchoredPosition = new Vector2(380, -50);

            Image p1LagImg = CreateBarFill(p1BarGo, "LagFill", LagYellow, Image.FillMethod.Horizontal, false);
            Image p1HpImg = CreateBarFill(p1BarGo, "HpFill", BloodRedColor, Image.FillMethod.Horizontal, false);
            Text p1Name = CreateText(p1BarGo, "AKIO", new Vector2(-220, 32), 22, TextWhite, FontStyle.Bold);

            Image[] p1Pips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pip = CreatePanel(p1BarGo, $"Pip_{i}", new Color(0.2f, 0.2f, 0.2f, 0.8f));
                RectTransform prt = pip.GetComponent<RectTransform>();
                prt.sizeDelta = new Vector2(16, 16);
                prt.anchoredPosition = new Vector2(-100 + (i * 24), 32);
                p1Pips[i] = pip.GetComponent<Image>();
            }

            GameObject p2BarGo = CreatePanel(hudGo, "P2_HealthBar", new Color(0.12f, 0.12f, 0.16f, 0.95f));
            RectTransform p2BarRt = p2BarGo.GetComponent<RectTransform>();
            p2BarRt.anchorMin = new Vector2(1, 1);
            p2BarRt.anchorMax = new Vector2(1, 1);
            p2BarRt.sizeDelta = new Vector2(650, 42);
            p2BarRt.anchoredPosition = new Vector2(-380, -50);

            Image p2LagImg = CreateBarFill(p2BarGo, "LagFill", LagYellow, Image.FillMethod.Horizontal, true);
            Image p2HpImg = CreateBarFill(p2BarGo, "HpFill", BloodRedColor, Image.FillMethod.Horizontal, true);
            Text p2Name = CreateText(p2BarGo, "AIKA", new Vector2(220, 32), 22, TextWhite, FontStyle.Bold);

            Image[] p2Pips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pip = CreatePanel(p2BarGo, $"Pip_{i}", new Color(0.2f, 0.2f, 0.2f, 0.8f));
                RectTransform prt = pip.GetComponent<RectTransform>();
                prt.sizeDelta = new Vector2(16, 16);
                prt.anchoredPosition = new Vector2(100 - (i * 24), 32);
                p2Pips[i] = pip.GetComponent<Image>();
            }

            GameObject timerBox = CreatePanel(hudGo, "TimerBox", new Color(0.08f, 0.08f, 0.12f, 0.95f));
            RectTransform tbRt = timerBox.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 1);
            tbRt.anchorMax = new Vector2(0.5f, 1);
            tbRt.sizeDelta = new Vector2(120, 80);
            tbRt.anchoredPosition = new Vector2(0, -60);
            Text timerText = CreateText(timerBox, "99", Vector2.zero, 52, GoldAccent, FontStyle.Bold);

            GameObject p1SuperGo = CreatePanel(hudGo, "P1_SuperMeter", new Color(0.1f, 0.1f, 0.14f, 0.95f));
            RectTransform p1sRt = p1SuperGo.GetComponent<RectTransform>();
            p1sRt.anchorMin = new Vector2(0, 0);
            p1sRt.anchorMax = new Vector2(0, 0);
            p1sRt.sizeDelta = new Vector2(480, 24);
            p1sRt.anchoredPosition = new Vector2(280, 45);
            Image p1SuperImg = CreateBarFill(p1SuperGo, "Fill", NeonCyan, Image.FillMethod.Horizontal, false);
            Text p1DomainReady = CreateText(p1SuperGo, "★ DOMAIN EXPANSION READY ★", new Vector2(0, 25), 14, NeonCyan, FontStyle.Bold);
            p1DomainReady.gameObject.SetActive(false);

            GameObject p2SuperGo = CreatePanel(hudGo, "P2_SuperMeter", new Color(0.1f, 0.1f, 0.14f, 0.95f));
            RectTransform p2sRt = p2SuperGo.GetComponent<RectTransform>();
            p2sRt.anchorMin = new Vector2(1, 0);
            p2sRt.anchorMax = new Vector2(1, 0);
            p2sRt.sizeDelta = new Vector2(480, 24);
            p2sRt.anchoredPosition = new Vector2(-280, 45);
            Image p2SuperImg = CreateBarFill(p2SuperGo, "Fill", NeonCyan, Image.FillMethod.Horizontal, true);
            Text p2DomainReady = CreateText(p2SuperGo, "★ DOMAIN EXPANSION READY ★", new Vector2(0, 25), 14, NeonCyan, FontStyle.Bold);
            p2DomainReady.gameObject.SetActive(false);

            // P1 & P2 Burst Gauge Indicators
            GameObject p1BurstGo = CreatePanel(p1BarGo, "P1_BurstBar", new Color(0.1f, 0.12f, 0.16f, 0.9f));
            RectTransform p1bRt = p1BurstGo.GetComponent<RectTransform>();
            p1bRt.anchorMin = new Vector2(0, 0);
            p1bRt.anchorMax = new Vector2(0, 0);
            p1bRt.sizeDelta = new Vector2(240, 10);
            p1bRt.anchoredPosition = new Vector2(160, 20);
            Image p1BurstImg = CreateBarFill(p1BurstGo, "Fill", GoldAccent, Image.FillMethod.Horizontal, false);

            GameObject p2BurstGo = CreatePanel(p2BarGo, "P2_BurstBar", new Color(0.1f, 0.12f, 0.16f, 0.9f));
            RectTransform p2bRt = p2BurstGo.GetComponent<RectTransform>();
            p2bRt.anchorMin = new Vector2(1, 0);
            p2bRt.anchorMax = new Vector2(1, 0);
            p2bRt.sizeDelta = new Vector2(240, 10);
            p2bRt.anchoredPosition = new Vector2(-160, 20);
            Image p2BurstImg = CreateBarFill(p2BurstGo, "Fill", GoldAccent, Image.FillMethod.Horizontal, true);

            // Center Announcements
            GameObject bannerGo = new GameObject("AnnouncementBanner");
            bannerGo.transform.SetParent(hudGo.transform, false);
            RectTransform banRt = bannerGo.AddComponent<RectTransform>();
            banRt.sizeDelta = new Vector2(900, 200);
            banRt.anchoredPosition = new Vector2(0, 80);
            CanvasGroup banCg = bannerGo.AddComponent<CanvasGroup>();
            Text banText = CreateText(bannerGo, "ROUND 1", Vector2.zero, 72, GoldAccent, FontStyle.Bold);

            // Callout Banner (Counter Hit, Rapid Cancel, Psychic Burst)
            GameObject calloutGo = new GameObject("CalloutBanner");
            calloutGo.transform.SetParent(hudGo.transform, false);
            RectTransform calloutRt = calloutGo.AddComponent<RectTransform>();
            calloutRt.sizeDelta = new Vector2(600, 80);
            calloutRt.anchoredPosition = new Vector2(0, 160);
            CanvasGroup calloutCg = calloutGo.AddComponent<CanvasGroup>();
            calloutCg.alpha = 0f;
            Text calloutText = CreateText(calloutGo, "COUNTER HIT!", Vector2.zero, 34, BloodRedColor, FontStyle.Bold);

            GameObject comboGo = new GameObject("ComboFloater");
            comboGo.transform.SetParent(hudGo.transform, false);
            RectTransform comRt = comboGo.AddComponent<RectTransform>();
            comRt.sizeDelta = new Vector2(400, 120);
            comRt.anchoredPosition = new Vector2(-420, 160);
            CanvasGroup comCg = comboGo.AddComponent<CanvasGroup>();
            Text comboText = CreateText(comboGo, "3 HITS!\n<size=18>450 DMG</size>", Vector2.zero, 36, BloodRedColor, FontStyle.Bold);

            // Victory Modal
            GameObject vicModal = CreatePanel(hudGo, "Modal_Victory", new Color(0, 0, 0, 0.88f));
            vicModal.SetActive(false);
            GameObject vicBox = CreatePanel(vicModal, "VictoryBox", SciFiSlateColor);
            RectTransform vicRt = vicBox.GetComponent<RectTransform>();
            vicRt.sizeDelta = new Vector2(700, 400);

            Text vicText = CreateText(vicBox, "VICTORY: MIRA", new Vector2(0, 90), 40, BloodRedColor, FontStyle.Bold);
            GameObject rematchBtnGo = CreateButton(vicBox, "Btn_Rematch", "REMATCH", new Vector2(0, 0), new Vector2(300, 55), BloodRedColor);
            Button rematchBtn = rematchBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(rematchBtn.onClick, hudCtrl.OnClickRematch);

            GameObject returnMenuBtnGo = CreateButton(vicBox, "Btn_ReturnMenu", "RETURN TO MENU", new Vector2(0, -75), new Vector2(300, 55), new Color(0.25f, 0.25f, 0.3f, 1f));
            Button returnMenuBtn = returnMenuBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(returnMenuBtn.onClick, hudCtrl.OnClickReturnToMenu);

            // 7.1 Touch Combat Controls Overlay
            GameObject touchOverlay = CreatePanel(hudGo, "TouchControlsOverlay", Color.clear);
            TouchVirtualControls touchCtrl = GetOrAdd<TouchVirtualControls>(touchOverlay);

            // Virtual Joystick (Left)
            GameObject joyBg = CreatePanel(touchOverlay, "Joystick_Background", new Color(0.1f, 0.12f, 0.18f, 0.65f));
            RectTransform jbgRt = joyBg.GetComponent<RectTransform>();
            jbgRt.anchorMin = Vector2.zero;
            jbgRt.anchorMax = Vector2.zero;
            jbgRt.sizeDelta = new Vector2(180, 180);
            jbgRt.anchoredPosition = new Vector2(150, 150);

            GameObject joyHandle = CreatePanel(joyBg, "Joystick_Handle", new Color(0.85f, 0.2f, 0.2f, 0.9f));
            RectTransform jHandleRt = joyHandle.GetComponent<RectTransform>();
            jHandleRt.anchorMin = new Vector2(0.5f, 0.5f);
            jHandleRt.anchorMax = new Vector2(0.5f, 0.5f);
            jHandleRt.sizeDelta = new Vector2(70, 70);
            jHandleRt.anchoredPosition = Vector2.zero;

            // Attach VirtualJoystickZone component
            VirtualJoystickZone joyZone = GetOrAdd<VirtualJoystickZone>(joyBg);
            joyZone.BindController(touchCtrl);

            // Right Action Buttons: 3-Button Master Layout with 50 Free Stylized Icons [P (Proj), M (Melee 3-Hit), S (Special), ULTRA (P+M+S), GRAB [66], BREAK [P+M]]
            GameObject btnL = CreateTouchActionButton(touchOverlay, "Btn_Touch_Light", "P", new Vector2(-235, 95), new Vector2(85, 85), NeonCyan, "Icon_Projectile_512");
            GameObject btnM = CreateTouchActionButton(touchOverlay, "Btn_Touch_Medium", "M", new Vector2(-125, 185), new Vector2(95, 95), BloodRedColor, "Icon_MeleeCombo_512");
            GameObject btnH = CreateTouchActionButton(touchOverlay, "Btn_Touch_Heavy", "M", new Vector2(-125, 185), new Vector2(95, 95), BloodRedColor, "Icon_MeleeCombo_512");
            GameObject btnD = CreateTouchActionButton(touchOverlay, "Btn_Touch_Drive", "S", new Vector2(-215, 215), new Vector2(85, 85), GoldAccent, "Icon_Special_512");
            GameObject btnS = CreateTouchActionButton(touchOverlay, "Btn_Touch_Super", "★ ULTRA ★", new Vector2(-125, 310), new Vector2(115, 70), BloodRedColor, "Icon_UltraSuper_512");
            GameObject btnP = CreateTouchActionButton(touchOverlay, "Btn_Touch_Parry", "PARRY", new Vector2(-330, 260), new Vector2(75, 50), SciFiSlateColor);
            GameObject btnB = CreateTouchActionButton(touchOverlay, "Btn_Touch_Backdash", "DASH", new Vector2(-415, 260), new Vector2(75, 50), SciFiSlateColor);
            GameObject btnBurst = CreateTouchActionButton(touchOverlay, "Btn_Touch_Burst", "BREAK", new Vector2(-325, 180), new Vector2(80, 65), GoldAccent, "Icon_GrabBreak_512");
            GameObject btnBarrier = CreateTouchActionButton(touchOverlay, "Btn_Touch_Throw", "GRAB", new Vector2(-325, 95), new Vector2(80, 65), SciFiSlateColor, "Icon_Grab_512");
            GameObject btnStepUp = CreateTouchActionButton(touchOverlay, "Btn_Touch_StepUp", "▲ Z", new Vector2(-230, 310), new Vector2(70, 50), SciFiSlateColor);
            GameObject btnStepDown = CreateTouchActionButton(touchOverlay, "Btn_Touch_StepDown", "▼ Z", new Vector2(-310, 310), new Vector2(70, 50), SciFiSlateColor);

            // Optional Super Virtual Joysticks (Dual On-Screen Joystick System)
            SuperVirtualJoysticksController superJoy = GetOrAdd<SuperVirtualJoysticksController>(touchOverlay);
            GameObject rightJoyBg = CreatePanel(touchOverlay, "Joystick_Right_Background", new Color(0.1f, 0.12f, 0.18f, 0.65f));
            RectTransform rjbgRt = rightJoyBg.GetComponent<RectTransform>();
            rjbgRt.anchorMin = new Vector2(1, 0);
            rjbgRt.anchorMax = new Vector2(1, 0);
            rjbgRt.sizeDelta = new Vector2(180, 180);
            rjbgRt.anchoredPosition = new Vector2(-180, 150);

            GameObject rightJoyHandle = CreatePanel(rightJoyBg, "Joystick_Right_Handle", new Color(0.2f, 0.65f, 1.0f, 0.9f));
            RectTransform rHandleRt = rightJoyHandle.GetComponent<RectTransform>();
            rHandleRt.anchorMin = new Vector2(0.5f, 0.5f);
            rHandleRt.anchorMax = new Vector2(0.5f, 0.5f);
            rHandleRt.sizeDelta = new Vector2(70, 70);
            rHandleRt.anchoredPosition = Vector2.zero;

            rightJoyBg.SetActive(false); // Enabled when user selects Dual Joystick mode

            SerializedObject soSuperJoy = new SerializedObject(superJoy);
            soSuperJoy.FindProperty("_leftStickBase").objectReferenceValue = jbgRt;
            soSuperJoy.FindProperty("_leftStickHandle").objectReferenceValue = jHandleRt;
            soSuperJoy.FindProperty("_rightStickBase").objectReferenceValue = rjbgRt;
            soSuperJoy.FindProperty("_rightStickHandle").objectReferenceValue = rHandleRt;
            soSuperJoy.FindProperty("_standardButtonsContainer").objectReferenceValue = btnL.transform.parent.gameObject;
            soSuperJoy.ApplyModifiedProperties();

            // Serialized wiring for TouchVirtualControls
            SerializedObject soTouch = new SerializedObject(touchCtrl);
            soTouch.FindProperty("_joystickBackground").objectReferenceValue = jbgRt;
            soTouch.FindProperty("_joystickHandle").objectReferenceValue = jHandleRt;
            soTouch.FindProperty("_joystickRadius").floatValue = 80f;
            soTouch.FindProperty("_btnLight").objectReferenceValue = btnL.GetComponent<Button>();
            soTouch.FindProperty("_btnMedium").objectReferenceValue = btnM.GetComponent<Button>();
            soTouch.FindProperty("_btnHeavy").objectReferenceValue = btnH.GetComponent<Button>();
            soTouch.FindProperty("_btnDrive").objectReferenceValue = btnD.GetComponent<Button>();
            soTouch.FindProperty("_btnSuper").objectReferenceValue = btnS.GetComponent<Button>();
            soTouch.FindProperty("_btnParry").objectReferenceValue = btnP.GetComponent<Button>();
            soTouch.FindProperty("_btnBackdash").objectReferenceValue = btnB.GetComponent<Button>();
            soTouch.FindProperty("_btnBurst").objectReferenceValue = btnBurst.GetComponent<Button>();
            soTouch.FindProperty("_btnBarrier").objectReferenceValue = btnBarrier.GetComponent<Button>();
            soTouch.FindProperty("_btnSidestepUp").objectReferenceValue = btnStepUp.GetComponent<Button>();
            soTouch.FindProperty("_btnSidestepDown").objectReferenceValue = btnStepDown.GetComponent<Button>();
            soTouch.ApplyModifiedProperties();

            // SerializedObject wiring for BattleHUDController
            SerializedObject soHud = new SerializedObject(hudCtrl);
            soHud.FindProperty("_p1NameText").objectReferenceValue = p1Name;
            soHud.FindProperty("_p1HealthFill").objectReferenceValue = p1HpImg;
            soHud.FindProperty("_p1HealthLagFill").objectReferenceValue = p1LagImg;
            soHud.FindProperty("_p1SuperFill").objectReferenceValue = p1SuperImg;
            soHud.FindProperty("_p1DomainReadyText").objectReferenceValue = p1DomainReady;
            soHud.FindProperty("_p1BurstFill").objectReferenceValue = p1BurstImg;

            var p1PipsProp = soHud.FindProperty("_p1WinPips");
            p1PipsProp.arraySize = 2;
            p1PipsProp.GetArrayElementAtIndex(0).objectReferenceValue = p1Pips[0];
            p1PipsProp.GetArrayElementAtIndex(1).objectReferenceValue = p1Pips[1];

            soHud.FindProperty("_p2NameText").objectReferenceValue = p2Name;
            soHud.FindProperty("_p2HealthFill").objectReferenceValue = p2HpImg;
            soHud.FindProperty("_p2HealthLagFill").objectReferenceValue = p2LagImg;
            soHud.FindProperty("_p2SuperFill").objectReferenceValue = p2SuperImg;
            soHud.FindProperty("_p2DomainReadyText").objectReferenceValue = p2DomainReady;
            soHud.FindProperty("_p2BurstFill").objectReferenceValue = p2BurstImg;

            var p2PipsProp = soHud.FindProperty("_p2WinPips");
            p2PipsProp.arraySize = 2;
            p2PipsProp.GetArrayElementAtIndex(0).objectReferenceValue = p2Pips[0];
            p2PipsProp.GetArrayElementAtIndex(1).objectReferenceValue = p2Pips[1];

            soHud.FindProperty("_timerText").objectReferenceValue = timerText;
            soHud.FindProperty("_roundBannerText").objectReferenceValue = banText;
            soHud.FindProperty("_roundBannerGroup").objectReferenceValue = banCg;
            soHud.FindProperty("_calloutBannerText").objectReferenceValue = calloutText;
            soHud.FindProperty("_calloutBannerGroup").objectReferenceValue = calloutCg;
            soHud.FindProperty("_comboCounterText").objectReferenceValue = comboText;
            soHud.FindProperty("_comboGroup").objectReferenceValue = comCg;
            soHud.FindProperty("_victoryModal").objectReferenceValue = vicModal;
            soHud.FindProperty("_victoryWinnerText").objectReferenceValue = vicText;
            soHud.FindProperty("_btnRematch").objectReferenceValue = rematchBtn;
            soHud.FindProperty("_btnReturnToMenu").objectReferenceValue = returnMenuBtn;
            soHud.ApplyModifiedProperties();

            hudGo.SetActive(false);

            // 8. Screen 6: PvP Lobby Modal
            Transform pvpTrans = canvasGo.transform.Find("Modal_PvPLobby");
            GameObject pvpModalGo = pvpTrans != null ? pvpTrans.gameObject : CreatePanel(canvasGo, "Modal_PvPLobby", new Color(0, 0, 0, 0.85f));
            PvPLobbyUI pvpUi = GetOrAdd<PvPLobbyUI>(pvpModalGo);

            GameObject pvpBox = CreatePanel(pvpModalGo, "PvPBox", SciFiSlateColor);
            RectTransform pvpBoxRect = pvpBox.GetComponent<RectTransform>();
            pvpBoxRect.sizeDelta = new Vector2(750, 500);

            CreateText(pvpBox, "QUANTUM 1v1 ROLLBACK DUEL", new Vector2(0, 180), 32, BloodRedColor, FontStyle.Bold);

            GameObject quickMatchBtnGo = CreateButton(pvpBox, "Btn_QuickMatch", "FIND RANKED MATCH", new Vector2(0, 70), new Vector2(400, 60), BloodRedColor);
            Button qmBtn = quickMatchBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(qmBtn.onClick, pvpUi.OnClickQuickMatch);

            GameObject privateMatchBtnGo = CreateButton(pvpBox, "Btn_CreatePrivate", "CREATE PRIVATE ROOM (CODE)", new Vector2(0, -20), new Vector2(400, 60), SciFiSlateColor);
            Button pmBtn = privateMatchBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(pmBtn.onClick, pvpUi.OnClickCreatePrivateRoom);

            GameObject closePvPBtnGo = CreateButton(pvpBox, "Btn_ClosePvP", "BACK TO HUB", new Vector2(0, -180), new Vector2(250, 50), new Color(0.3f, 0.3f, 0.35f, 1f));
            Button closePvPBtn = closePvPBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(closePvPBtn.onClick, uiManager.OnClickClosePvPModal);

            pvpModalGo.SetActive(false);

            // 9. Screen 7: Settings Modal
            Transform setTrans = canvasGo.transform.Find("Modal_Settings");
            GameObject settingsModalGo = setTrans != null ? setTrans.gameObject : CreatePanel(canvasGo, "Modal_Settings", new Color(0, 0, 0, 0.85f));
            SettingsPanel setUi = GetOrAdd<SettingsPanel>(settingsModalGo);

            GameObject setBox = CreatePanel(settingsModalGo, "SettingsBox", SciFiSlateColor);
            RectTransform setBoxRect = setBox.GetComponent<RectTransform>();
            setBoxRect.sizeDelta = new Vector2(700, 500);

            CreateText(setBox, "SYSTEM CONFIGURATION", new Vector2(0, 180), 32, GoldAccent, FontStyle.Bold);
            CreateText(setBox, "• LANDSCAPE-LOCKED DISPLAY (PORTRAIT DISABLED)\n• 120 FPS PROMOTION / HIGH REFRESH RATE ACTIVE\n• TAPTIC CORE HAPTICS ENABLED\n• 60HZ DETERMINISTIC TICK LOCKED\n• ZERO-GC ALLOCATION PIPELINE", new Vector2(0, 30), 18, TextWhite, FontStyle.Normal);

            GameObject closeSetBtnGo = CreateButton(setBox, "Btn_CloseSettings", "SAVE & CLOSE", new Vector2(0, -170), new Vector2(250, 50), BloodRedColor);
            Button closeSetBtn = closeSetBtnGo.GetComponent<Button>();
            ClearAndAddPersistentListener(closeSetBtn.onClick, uiManager.OnClickCloseSettingsModal);

            settingsModalGo.SetActive(false);

            // 10. Bind Master References into BloodTechUIManager
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("_loadingPanel").objectReferenceValue = loadingCg;
            so.FindProperty("_authPanel").objectReferenceValue = authCg;
            so.FindProperty("_modeSelectPanel").objectReferenceValue = modeCg;
            so.FindProperty("_characterSelectPanel").objectReferenceValue = charSelectCg;
            so.FindProperty("_battleHUDPanel").objectReferenceValue = hudCg;
            so.FindProperty("_pvpLobbyModal").objectReferenceValue = pvpModalGo;
            so.FindProperty("_settingsModal").objectReferenceValue = settingsModalGo;
            so.FindProperty("_loadingProgressBar").objectReferenceValue = slider;
            so.FindProperty("_loadingPercentText").objectReferenceValue = percentText;
            so.FindProperty("_codenameInputField").objectReferenceValue = null;
            so.FindProperty("_welcomeUserText").objectReferenceValue = welcomeText;
            so.FindProperty("_btnPvP").objectReferenceValue = pvpBtn;
            so.FindProperty("_btnArcade").objectReferenceValue = arcadeBtn;
            so.FindProperty("_btnStory").objectReferenceValue = storyBtn;
            so.FindProperty("_btnTraining").objectReferenceValue = trainingBtn;
            so.FindProperty("_btnSettings").objectReferenceValue = settingsBtn;
            so.FindProperty("_btnLocalVersus").objectReferenceValue = null;
            so.FindProperty("_btnShowcase").objectReferenceValue = null;
            so.FindProperty("_btnTutorialLessons").objectReferenceValue = null;
            so.FindProperty("_btnContinueStory").objectReferenceValue = null;
            so.ApplyModifiedProperties();

            // 11. Ensure MatchRoundReferee and ClutchSlowMotionManager are attached to CombatManager in scene
            GameObject cmGo = GameObject.Find("CombatManager");
            if (cmGo != null)
            {
                MatchRoundReferee referee = cmGo.GetComponent<MatchRoundReferee>();
                if (referee == null) referee = cmGo.AddComponent<MatchRoundReferee>();

                ClutchSlowMotionManager slowMo = cmGo.GetComponent<ClutchSlowMotionManager>();
                if (slowMo == null) slowMo = cmGo.AddComponent<ClutchSlowMotionManager>();

                OnboardingDirector oDirector = cmGo.GetComponent<OnboardingDirector>();
                if (oDirector == null) oDirector = cmGo.AddComponent<OnboardingDirector>();
            }

            // 12. Ensure Throw and Low Attack assets are assigned to scene fighters
            AttackData throwAsset = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_Throw.asset");
            AttackData lowAsset = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");
            foreach (var fc in Object.FindObjectsByType<FighterController>())
            {
                SerializedObject soFc = new SerializedObject(fc);
                if (fc.throwAttackData == null && throwAsset != null)
                {
                    var p = soFc.FindProperty("throwAttackData");
                    if (p != null) p.objectReferenceValue = throwAsset;
                }
                if (fc.lowAttackData == null && lowAsset != null)
                {
                    var p = soFc.FindProperty("lowAttackData");
                    if (p != null) p.objectReferenceValue = lowAsset;
                }
                soFc.ApplyModifiedProperties();
            }

            // 13. Ensure Authoritative Character & Stage Prefabs in Assets/Prefabs
            BuildAllCharacterAndStagePrefabs();

            // 14. Deploy Master Armory, Icons, Frames, Mugshots, URP Indoor & Replays
            DemonIdolArmoryBuilder.DeployAll();

            // Mark and save scene
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#C73E2E><b>[Demon Idol]</b></color> Master Blood-Tech UI Canvas + Roster Grid + Battle HUD + Touch Controls successfully built and persistent!");
        }

        #region Helpers

        private static Button CreateModeCard(GameObject parent, string cardName, string btnName, string tag, string glyph, string title, string sub, string desc, Vector2 pos, Color accentColor, Color btnColor)
        {
            GameObject card = new GameObject(cardName);
            card.transform.SetParent(parent.transform, false);

            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchoredPosition = pos;
            cardRt.sizeDelta = new Vector2(370f, 560f);

            // Dark Glass Slate Card Body
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

            // Glowing Border Frame
            GameObject border = new GameObject("Border");
            border.transform.SetParent(card.transform, false);
            RectTransform borderRt = border.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(4f, 4f);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f);

            // Top Accent Bar (6px)
            GameObject topBar = new GameObject("TopAccentBar");
            topBar.transform.SetParent(card.transform, false);
            RectTransform topBarRt = topBar.AddComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 1f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = Vector2.zero;
            topBarRt.sizeDelta = new Vector2(0f, 6f);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = accentColor;

            // Mode Tag
            CreateText(card, tag, new Vector2(-95f, 240f), 12, accentColor, FontStyle.Bold);

            // Icon Glyph
            CreateText(card, glyph, new Vector2(0f, 155f), 52, Color.white, FontStyle.Normal);

            // Title
            CreateText(card, title, new Vector2(0f, 75f), 24, TextWhite, FontStyle.Bold);

            // Subtitle
            CreateText(card, sub, new Vector2(0f, 40f), 12, accentColor, FontStyle.Bold);

            // Divider Line
            GameObject div = new GameObject("Divider");
            div.transform.SetParent(card.transform, false);
            RectTransform divRt = div.AddComponent<RectTransform>();
            divRt.anchoredPosition = new Vector2(0f, 15f);
            divRt.sizeDelta = new Vector2(280f, 1.5f);
            Image divImg = div.AddComponent<Image>();
            divImg.color = new Color(0.25f, 0.30f, 0.40f, 0.5f);

            // Description Box
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(card.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(0f, -60f);
            descRt.sizeDelta = new Vector2(310f, 95f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descTxt.text = desc;
            descTxt.fontSize = 13;
            descTxt.color = new Color(0.80f, 0.85f, 0.92f, 0.9f);
            descTxt.alignment = TextAnchor.MiddleCenter;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;

            // Action Button at Bottom
            GameObject btnGo = CreateButton(card, btnName, "SELECT DISCIPLINE ▶", new Vector2(0f, -220f), new Vector2(300f, 54f), btnColor);
            Button btn = btnGo.GetComponent<Button>();

            // Full card click also triggers button
            Button cardBtn = card.AddComponent<Button>();
            cardBtn.targetGraphic = cardBg;
            ColorBlock cb = cardBtn.colors;
            cb.highlightedColor = new Color(0.12f, 0.15f, 0.22f, 1f);
            cb.pressedColor = new Color(0.06f, 0.08f, 0.11f, 1f);
            cardBtn.colors = cb;
            cardBtn.onClick.AddListener(() => btn.onClick.Invoke());

            return btn;
        }

        private static GameObject CreatePanel(GameObject parent, string name, Color color)
        {
            Transform existing = parent.transform.Find(name);
            if (existing != null)
            {
                Image existingImg = existing.GetComponent<Image>();
                if (existingImg != null) existingImg.color = color;
                return existing.gameObject;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private static Text CreateText(GameObject parent, string content, Vector2 pos, int fontSize, Color color, FontStyle style)
        {
            GameObject go = new GameObject("Label_" + content.Replace(" ", "_").Replace("\n", "_"));
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;

            Text txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return txt;
        }

        private static GameObject CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = color;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.highlightedColor = color * 1.25f;
            cb.pressedColor = color * 0.75f;
            btn.colors = cb;

            CreateText(go, label, Vector2.zero, 18, TextWhite, FontStyle.Bold);

            return go;
        }

        private static GameObject CreateTouchActionButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color color, string iconName = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = go.AddComponent<Image>();
            img.color = color;

            if (!string.IsNullOrEmpty(iconName))
            {
                string iconPath = $"Assets/CombatData/Icons/{iconName}.png";
                Sprite iconSpr = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (iconSpr != null)
                {
                    img.sprite = iconSpr;
                    img.color = Color.white;
                }
            }

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            CreateText(go, label, Vector2.zero, 24, TextWhite, FontStyle.Bold);

            return go;
        }

        private static Image CreateBarFill(GameObject parent, string name, Color color, Image.FillMethod fillMethod, bool fillOriginRight)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = fillMethod;
            img.fillOrigin = fillOriginRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;

            return img;
        }

        private static Image CreateStatRow(GameObject parent, string statName, Vector2 pos)
        {
            CreateText(parent, statName, pos + new Vector2(-60, 0), 14, GoldAccent, FontStyle.Bold);

            GameObject barBg = CreatePanel(parent, $"Stat_{statName}", new Color(0.18f, 0.18f, 0.22f, 1f));
            RectTransform brt = barBg.GetComponent<RectTransform>();
            brt.anchoredPosition = pos + new Vector2(50, 0);
            brt.sizeDelta = new Vector2(140, 12);

            return CreateBarFill(barBg, "Fill", BloodRedColor, Image.FillMethod.Horizontal, false);
        }

        private static Text CreateTextWrapped(GameObject parent, string content, Vector2 pos, Vector2 size, int fontSize, Color color, FontStyle style, TextAnchor alignment)
        {
            GameObject go = new GameObject("Label_" + content.Substring(0, Mathf.Min(content.Length, 12)).Replace(" ", "_"));
            go.transform.SetParent(parent.transform, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Text txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.lineSpacing = 1.15f;

            return txt;
        }

        private static (Image fill, Text valText) CreateDetailedStatRow(GameObject parent, string statName, Vector2 pos, Color fillColor)
        {
            Text label = CreateText(parent, statName, pos + new Vector2(-170f, 0f), 13, GoldAccent, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleRight;

            GameObject barBg = CreatePanel(parent, $"StatTrack_{statName}", new Color(0.12f, 0.15f, 0.22f, 0.9f));
            RectTransform brt = barBg.GetComponent<RectTransform>();
            brt.anchoredPosition = pos + new Vector2(30f, 0f);
            brt.sizeDelta = new Vector2(240f, 14f);

            GameObject trackBorder = CreatePanel(barBg, "Border", new Color(1f, 1f, 1f, 0.08f));
            RectTransform tbrt = trackBorder.GetComponent<RectTransform>();
            tbrt.anchorMin = Vector2.zero;
            tbrt.anchorMax = Vector2.one;
            tbrt.sizeDelta = new Vector2(2f, 2f);

            Image fill = CreateBarFill(barBg, "Fill", fillColor, Image.FillMethod.Horizontal, false);

            Text valText = CreateText(parent, "80%", pos + new Vector2(200f, 0f), 13, TextWhite, FontStyle.Bold);
            valText.alignment = TextAnchor.MiddleLeft;

            return (fill, valText);
        }

        private static void ClearAndAddPersistentListener(Button.ButtonClickedEvent unityEvent, UnityEngine.Events.UnityAction action)
        {
            while (unityEvent.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, 0);
            }
            UnityEventTools.AddPersistentListener(unityEvent, action);
        }

        private static void ClearAndAddPersistentListenerInt(Button.ButtonClickedEvent unityEvent, UnityEngine.Events.UnityAction<int> action, int argument)
        {
            while (unityEvent.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, 0);
            }
            UnityEventTools.AddIntPersistentListener(unityEvent, action, argument);
        }

        private static void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        #endregion

        #region Character & Stage Prefab Organizer

        public const string PREFABS_ROOT = "Assets/Prefabs";
        public const string CHARACTERS_DIR = "Assets/Prefabs/Characters";
        public const string STAGES_DIR = "Assets/Prefabs/Stages";
        private const string MDI_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Prefabs/Build All Character and Stage Prefabs", priority = 2)]
        public static void MenuBuildPrefabs()
        {
            BuildAllCharacterAndStagePrefabs();
        }

        public static void BuildAllCharacterAndStagePrefabs()
        {
            EnsurePrefabDirectories();

            // 1. Build 14 Character Roster / Model Prefabs
            Build14CharacterRosterPrefabs();

            // 2. Build Combat-Ready Fighter Prefabs
            BuildCombatReadyFighterPrefabs();

            // 3. Build 4 Arena Stage Prefabs + Universal Base Stage
            BuildAllStagePrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676><b>[Demon Idol Prefabs]</b> Successfully generated authoritative Character and Stage prefabs in Assets/Prefabs!</color>");
        }

        private static void EnsurePrefabDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(CHARACTERS_DIR))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");
            }
            if (!AssetDatabase.IsValidFolder(STAGES_DIR))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Stages");
            }
        }

        private static void Build14CharacterRosterPrefabs()
        {
            RuntimeAnimatorController templateAnim = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);

            for (int i = 0; i < FighterRosterCatalog.Roster.Length; i++)
            {
                var profile = FighterRosterCatalog.Roster[i];
                string canonicalId = profile.id;
                string cleanName = profile.displayName.Trim();
                string targetPrefabPath = profile.prefabPath;

                GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(profile.prefabPath);
                if (sourceAsset == null)
                {
                    Debug.LogWarning($"[DemonIdolUIBuilder] Source model not found for {profile.displayName} at {profile.prefabPath}");
                    continue;
                }

                GameObject instance = Object.Instantiate(sourceAsset);
                instance.name = $"{canonicalId}_{cleanName}";
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                CharacterController cc = instance.GetComponent<CharacterController>();
                if (cc == null) cc = instance.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 1.0f, 0);
                cc.height = 2.0f;
                cc.radius = 0.35f;

                Animator anim = instance.GetComponentInChildren<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                if (anim.runtimeAnimatorController == null && templateAnim != null)
                {
                    anim.runtimeAnimatorController = templateAnim;
                }
                anim.applyRootMotion = false;

                if (instance.GetComponent<AnimationEventReceiver>() == null)
                {
                    instance.AddComponent<AnimationEventReceiver>();
                }

                PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
                Object.DestroyImmediate(instance);
            }
        }

        private static void BuildCombatReadyFighterPrefabs()
        {
            var lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            var medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            var heavyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            var driveAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DriveSkill.asset");
            var jumpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");
            var superAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DomainSuper.asset");
            var throwAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_Throw.asset");
            var lowAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");

            RuntimeAnimatorController templateAnim = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);

            BuildFighterCombatPrefab(
                "Fighter_Player1_Akio",
                "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/Prefab/Akio Highschool Uniform.prefab",
                "Akio",
                true,
                lightAtk, medAtk, heavyAtk, driveAtk, jumpAtk, superAtk, throwAtk, lowAtk, templateAnim);

            BuildFighterCombatPrefab(
                "Fighter_Player2_Aika",
                "Assets/Novacorp Studios/Characters/Hikari Series/Aika/Highschool Uniform/Prefab/Aika Highschool Uniform.prefab",
                "Aika",
                false,
                lightAtk, medAtk, heavyAtk, driveAtk, jumpAtk, superAtk, throwAtk, lowAtk, templateAnim);

        }

        private static void BuildFighterCombatPrefab(
            string prefabName,
            string visualSourcePath,
            string fighterDisplayName,
            bool isPlayerControlled,
            AttackData lightAtk, AttackData medAtk, AttackData heavyAtk, AttackData driveAtk,
            AttackData jumpAtk, AttackData superAtk, AttackData throwAtk, AttackData lowAtk,
            RuntimeAnimatorController animController)
        {
            string targetPath = $"{CHARACTERS_DIR}/{prefabName}.prefab";

            GameObject root = new GameObject(prefabName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1.0f, 0);
            cc.height = 2.0f;
            cc.radius = 0.35f;

            FighterController fc = root.AddComponent<FighterController>();
            fc.fighterName = fighterDisplayName;
            fc.isPlayerControlled = isPlayerControlled;
            fc.maxHealth = 1000;
            fc.maxSuperMeter = 100;
            fc.lightAttackData = lightAtk;
            fc.mediumAttackData = medAtk;
            fc.heavyAttackData = heavyAtk;
            fc.driveSkillData = driveAtk;
            fc.jumpAttackData = jumpAtk;
            fc.domainSuperData = superAtk;
            fc.throwAttackData = throwAtk;
            fc.lowAttackData = lowAtk;

            if (isPlayerControlled)
            {
                root.AddComponent<MobileFighterInput>();
            }
            else
            {
                var dummyAI = root.AddComponent<TrainingDummyAI>();
                dummyAI.mode = DummyMode.CounterHitPractice;
            }

            GameObject hbGo = new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(root.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
            BoxCollider hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.8f, 1.8f, 0.8f);
            hbCol.isTrigger = true;
            hbGo.AddComponent<Hurtbox>();

            GameObject hitGo = new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(root.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f);
            BoxCollider hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            GameObject visualAsset = AssetDatabase.LoadAssetAtPath<GameObject>(visualSourcePath);
            if (visualAsset != null)
            {
                GameObject visualInst = Object.Instantiate(visualAsset, root.transform);
                visualInst.name = $"Model_{fighterDisplayName}";
                visualInst.transform.localPosition = Vector3.zero;
                visualInst.transform.localRotation = Quaternion.identity;
                visualInst.transform.localScale = Vector3.one;

                Animator anim = visualInst.GetComponentInChildren<Animator>();
                if (anim != null && anim.runtimeAnimatorController == null && animController != null)
                {
                    anim.runtimeAnimatorController = animController;
                }
            }

            if (root.GetComponent<AnimationEventReceiver>() == null)
            {
                root.AddComponent<AnimationEventReceiver>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildAllStagePrefabs()
        {
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Ground.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Metal_Black.mat");
            Material litMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Lit.mat");
            Material dungeonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/LowPolyDungeonsLite/Materials/LowPolyDungeonsLite_MAIN.mat");

            BuildStagePrefab("ST01_LowPolyDesert", dungeonMat != null ? dungeonMat : concreteMat, new Color(1.0f, 0.72f, 0.35f, 1f), StageThemeType.LowPolyDesert);
            BuildStagePrefab("ST02_RTSSciFiHangar", metalMat != null ? metalMat : concreteMat, new Color(0.2f, 0.85f, 1.0f, 1.0f), StageThemeType.RTSSciFi);
            BuildStagePrefab("ST03_SpaceAlienWorlds", litMat != null ? litMat : concreteMat, new Color(0.78f, 0.32f, 0.98f, 1.0f), StageThemeType.SpaceAlienWorlds);
            BuildStagePrefab("ST04_CartoonCity", concreteMat, new Color(1.0f, 0.95f, 0.75f, 1.0f), StageThemeType.CartoonCity);
            BuildStagePrefab("ST05_MetroSubway", metalMat != null ? metalMat : concreteMat, new Color(0.95f, 0.55f, 0.25f, 1.0f), StageThemeType.MetroSubway);
            BuildStagePrefab("ST06_ModernOfficeTower", litMat != null ? litMat : metalMat, new Color(0.35f, 0.65f, 1.0f, 1.0f), StageThemeType.ModernOfficeTower);
            BuildStagePrefab("ST07_CollegeRaimon", concreteMat, new Color(1.0f, 0.85f, 0.9f, 1.0f), StageThemeType.CollegeRaimon);
            BuildStagePrefab("Stage_CombatArena_Base", concreteMat, new Color(0.9f, 0.9f, 0.9f, 1.0f), StageThemeType.Base);
        }

        private enum StageThemeType
        {
            Base,
            LowPolyDesert,
            RTSSciFi,
            SpaceAlienWorlds,
            CartoonCity,
            MetroSubway,
            ModernOfficeTower,
            CollegeRaimon
        }

        private static void BuildStagePrefab(string stageName, Material surfaceMat, Color accentColor, StageThemeType theme)
        {
            string targetPath = $"{STAGES_DIR}/{stageName}.prefab";

            GameObject root = new GameObject(stageName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Ground_CombatPlatform";
            platform.transform.SetParent(root.transform, false);
            platform.transform.position = new Vector3(0f, -0.25f, 0f);
            platform.transform.localScale = new Vector3(32f, 0.5f, 8f);

            var groundCol = platform.GetComponent<BoxCollider>();
            if (groundCol == null) groundCol = platform.AddComponent<BoxCollider>();
            groundCol.center = Vector3.zero;
            groundCol.size = Vector3.one;

            if (surfaceMat != null)
            {
                var mr = platform.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = surfaceMat;
            }

            GameObject leftWall = new GameObject("Boundary_LeftWall");
            leftWall.transform.SetParent(root.transform, false);
            leftWall.transform.position = new Vector3(-14.0f, 2.5f, 0f);
            BoxCollider lCol = leftWall.AddComponent<BoxCollider>();
            lCol.size = new Vector3(1.0f, 6.0f, 8.0f);

            GameObject rightWall = new GameObject("Boundary_RightWall");
            rightWall.transform.SetParent(root.transform, false);
            rightWall.transform.position = new Vector3(14.0f, 2.5f, 0f);
            BoxCollider rCol = rightWall.AddComponent<BoxCollider>();
            rCol.size = new Vector3(1.0f, 6.0f, 8.0f);

            GameObject p1Spawn = new GameObject("SpawnPoint_P1");
            p1Spawn.transform.SetParent(root.transform, false);
            p1Spawn.transform.position = new Vector3(-2.5f, 0f, 0f);
            p1Spawn.transform.rotation = Quaternion.Euler(0, 90f, 0);

            GameObject p2Spawn = new GameObject("SpawnPoint_P2");
            p2Spawn.transform.SetParent(root.transform, false);
            p2Spawn.transform.position = new Vector3(2.5f, 0f, 0f);
            p2Spawn.transform.rotation = Quaternion.Euler(0, -90f, 0);

            GameObject decorRoot = new GameObject("Environment_BackdropDiorama");
            decorRoot.transform.SetParent(root.transform, false);

            if (theme == StageThemeType.LowPolyDesert)
            {
                GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Rock_01.prefab");
                if (rockPrefab != null)
                {
                    var r1 = Object.Instantiate(rockPrefab, decorRoot.transform);
                    r1.transform.localPosition = new Vector3(-11.5f, -0.2f, 4.0f);
                    r1.transform.localScale = new Vector3(2.5f, 2.8f, 2.5f);
                    r1.transform.localEulerAngles = new Vector3(0f, 45f, 0f);

                    var r2 = Object.Instantiate(rockPrefab, decorRoot.transform);
                    r2.transform.localPosition = new Vector3(11.5f, -0.2f, 4.0f);
                    r2.transform.localScale = new Vector3(2.5f, 3.2f, 2.5f);
                    r2.transform.localEulerAngles = new Vector3(0f, -30f, 0f);

                    var r3 = Object.Instantiate(rockPrefab, decorRoot.transform);
                    r3.transform.localPosition = new Vector3(0f, -0.2f, 6.0f);
                    r3.transform.localScale = new Vector3(4.0f, 2.0f, 3.0f);
                }
                GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Column_01.prefab");
                if (colPrefab != null)
                {
                    var c1 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c1.transform.localPosition = new Vector3(-7.0f, 0f, 3.8f);
                    c1.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
                    var c2 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c2.transform.localPosition = new Vector3(7.0f, 0f, 3.8f);
                    c2.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
                }
            }
            else if (theme == StageThemeType.RTSSciFi)
            {
                GameObject monPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelMonitor.prefab");
                if (monPrefab != null)
                {
                    var monL = Object.Instantiate(monPrefab, decorRoot.transform);
                    monL.transform.localPosition = new Vector3(-6.5f, 1.5f, 3.8f);
                    var monR = Object.Instantiate(monPrefab, decorRoot.transform);
                    monR.transform.localPosition = new Vector3(6.5f, 1.5f, 3.8f);
                }
                GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWall.prefab");
                if (wallPrefab != null)
                {
                    var wall = Object.Instantiate(wallPrefab, decorRoot.transform);
                    wall.transform.localPosition = new Vector3(0f, 0f, 4.2f);
                    wall.transform.localScale = new Vector3(1.5f, 1.2f, 1.0f);
                }
                GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelColumn.prefab");
                if (colPrefab != null)
                {
                    var colL = Object.Instantiate(colPrefab, decorRoot.transform);
                    colL.transform.localPosition = new Vector3(-10.5f, 0f, 3.5f);
                    var colR = Object.Instantiate(colPrefab, decorRoot.transform);
                    colR.transform.localPosition = new Vector3(10.5f, 0f, 3.5f);
                }
            }
            else if (theme == StageThemeType.SpaceAlienWorlds)
            {
                GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Column_01.prefab");
                if (colPrefab != null)
                {
                    var c1 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c1.transform.localPosition = new Vector3(-8.5f, 0f, 3.6f);
                    c1.transform.localScale = new Vector3(1.3f, 2.0f, 1.3f);
                    var c2 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c2.transform.localPosition = new Vector3(8.5f, 0f, 3.6f);
                    c2.transform.localScale = new Vector3(1.3f, 2.0f, 1.3f);
                }
                GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Rock_01.prefab");
                if (rockPrefab != null)
                {
                    var r1 = Object.Instantiate(rockPrefab, decorRoot.transform);
                    r1.transform.localPosition = new Vector3(-4.0f, 0f, 4.5f);
                    r1.transform.localScale = new Vector3(2.0f, 3.5f, 2.0f);
                    var r2 = Object.Instantiate(rockPrefab, decorRoot.transform);
                    r2.transform.localPosition = new Vector3(4.0f, 0f, 4.5f);
                    r2.transform.localScale = new Vector3(2.0f, 3.5f, 2.0f);
                }
            }
            else if (theme == StageThemeType.CartoonCity)
            {
                GameObject lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HairPack/Art/SpotLight/Prefabs/SpotLight_01.prefab");
                if (lightPrefab != null)
                {
                    var s1 = Object.Instantiate(lightPrefab, decorRoot.transform);
                    s1.transform.localPosition = new Vector3(-10.0f, 0f, 2.8f);
                    s1.transform.localEulerAngles = new Vector3(0, 25f, 0);

                    var s2 = Object.Instantiate(lightPrefab, decorRoot.transform);
                    s2.transform.localPosition = new Vector3(10.0f, 0f, 2.8f);
                    s2.transform.localEulerAngles = new Vector3(0, -25f, 0);
                }
                GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWallDoor.prefab");
                if (wallPrefab != null)
                {
                    var wall = Object.Instantiate(wallPrefab, decorRoot.transform);
                    wall.transform.localPosition = new Vector3(0f, 0f, 4.2f);
                }
            }
            else if (theme == StageThemeType.MetroSubway)
            {
                GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelColumn.prefab");
                if (colPrefab != null)
                {
                    var c1 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c1.transform.localPosition = new Vector3(-7.5f, 0f, 3.5f);
                    var c2 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c2.transform.localPosition = new Vector3(7.5f, 0f, 3.5f);
                }
                GameObject ductPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelDuct.prefab");
                if (ductPrefab != null)
                {
                    var duct = Object.Instantiate(ductPrefab, decorRoot.transform);
                    duct.transform.localPosition = new Vector3(0f, 3.6f, 3.8f);
                }
                GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWall.prefab");
                if (wallPrefab != null)
                {
                    var wall = Object.Instantiate(wallPrefab, decorRoot.transform);
                    wall.transform.localPosition = new Vector3(0f, 0f, 4.5f);
                }
            }
            else if (theme == StageThemeType.ModernOfficeTower)
            {
                GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWallDoor.prefab");
                if (wallPrefab != null)
                {
                    var w1 = Object.Instantiate(wallPrefab, decorRoot.transform);
                    w1.transform.localPosition = new Vector3(-6.0f, 0f, 4.0f);
                    var w2 = Object.Instantiate(wallPrefab, decorRoot.transform);
                    w2.transform.localPosition = new Vector3(6.0f, 0f, 4.0f);
                }
                GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelColumn.prefab");
                if (colPrefab != null)
                {
                    var c1 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c1.transform.localPosition = new Vector3(-11.0f, 0f, 3.5f);
                    var c2 = Object.Instantiate(colPrefab, decorRoot.transform);
                    c2.transform.localPosition = new Vector3(11.0f, 0f, 3.5f);
                }
            }
            else if (theme == StageThemeType.CollegeRaimon)
            {
                GameObject raimonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CollegeRaimon/Prefabs/CollegeRaimon.prefab");
                if (raimonPrefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(raimonPrefab, decorRoot.transform);
                    inst.name = "CollegeRaimon_Courtyard";
                    inst.transform.localPosition = new Vector3(-8.0f, 0.0f, 12.0f);
                    inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    inst.transform.localScale = Vector3.one;
                }
            }

            GameObject lightGo = new GameObject("Stage_AccentLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 4.5f, -2.0f);
            Light lComp = lightGo.AddComponent<Light>();
            lComp.type = LightType.Point;
            lComp.color = accentColor;
            lComp.range = 30f;
            lComp.intensity = 1.35f;

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            Object.DestroyImmediate(root);
        }

        #endregion

        #region 90s Cel Anime Aesthetics

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Shaders/Apply 90s Anime Aesthetics to Character Materials", priority = 10)]
        public static void Apply90sAnimeAestheticsToAllMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int modifiedCount = 0;

            Shader animeShader = Shader.Find("DemonIdol/MDI/AnimeToon");
            if (animeShader == null)
            {
                Debug.LogError("[90s Anime] Could not find shader 'DemonIdol/MDI/AnimeToon'");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader != animeShader) continue;

                Undo.RecordObject(mat, "Apply 90s Anime Aesthetics");

                string lowerName = mat.name.ToLower();
                bool isEyeDetail = lowerName.Contains("eyeiris") || lowerName.Contains("eyehighlight") || lowerName.Contains("facebrow") || lowerName.Contains("eye_reflection");

                // 1. Inked Cel Lines
                if (isEyeDetail)
                {
                    mat.SetFloat("_EnableOutline", 0.0f);
                }
                else
                {
                    mat.SetFloat("_EnableOutline", 1.0f);
                    mat.SetFloat("_OutlineWidth", 1.2f);
                    mat.SetColor("_OutlineColor", new Color(0.09f, 0.07f, 0.12f, 1.0f)); // Classic dark cel ink
                }

                // 2. 90s Crisp 2-Tone Painted Cel Shading
                mat.SetFloat("_RampSmoothness", 0.008f); // crisp hand-painted cel boundary
                mat.SetFloat("_RampThreshold1", 0.42f);  // classic 2-tone shadow boundary
                mat.SetFloat("_RampThreshold2", 0.78f);  // graphic highlight boundary

                // 3. 90s Stylized Specular Glint (Hair Sheen / Angel Ring)
                mat.SetFloat("_SpecIntensity", 0.8f);
                mat.SetFloat("_SpecPower", 48.0f);
                mat.SetFloat("_SpecThreshold", 0.55f);
                mat.SetColor("_SpecColor", new Color(1.0f, 0.98f, 0.92f, 1.0f));

                // 4. 90s Celluloid Backlight Rim
                mat.SetColor("_RimColor", new Color(0.95f, 0.92f, 0.86f, 0.55f));
                mat.SetFloat("_RimPower", 3.5f);
                mat.SetFloat("_RimThreshold", 0.40f);

                // 5. 35mm Celluloid Film Warmth, Emulsion Halation & Grain
                mat.SetFloat("_VintageWarmth", 0.40f);
                mat.SetFloat("_FilmGrain", 0.08f);
                mat.SetFloat("_HalationIntensity", 0.25f);

                // 6. Zero Modern Sci-Fi Neon / Chromatic Contour
                mat.SetFloat("_NeonIntensity", 0.0f);
                mat.SetFloat("_ChromaticEdgeIntensity", 0.0f);
                mat.SetColor("_NeonColor", new Color(0, 0, 0, 0));

                EditorUtility.SetDirty(mat);
                modifiedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[90s Anime] Successfully configured {modifiedCount} materials with 90s Cel Anime Aesthetics!");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Shaders/Capture Showcase Character Image", priority = 11)]
        public static void CaptureShowcaseCharacterImage()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam == null)
            {
                Debug.LogError("[90s Anime] No camera found in scene to capture!");
                return;
            }

            int width = 1080;
            int height = 1920;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = RenderTexture.active;
            RenderTexture prevCamRt = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevCamRt;
            RenderTexture.active = prevRt;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string outPath = System.IO.Path.Combine(Application.dataPath, "../Screenshots/ninety_anime_render.png");
            System.IO.File.WriteAllBytes(outPath, bytes);
            Debug.Log($"[90s Anime] Saved 90s character render to {outPath}");
        }

        #endregion
    }
}
#endif
