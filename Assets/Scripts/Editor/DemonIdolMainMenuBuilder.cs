#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DemonIdol.UI;
using DemonIdol.Launch;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master automated scene generator for Assets/Scenes/MainMenu.unity.
    /// Builds a launch-ready Main Menu scene with:
    /// 1. Cinematic 3-point Studio Lighting & URP Post-Processing
    /// 2. 3D Character Preview Pedestal
    /// 3. Title Screen with anime styling & Demon Lord branding
    /// 4. Game Mode Selection (Arcade, Local Versus, Training Dojo)
    /// 5. 14-Character Roster Select Grid with live stats and domain info
    /// 6. 6-Stage Selection Carousel
    /// 7. Direct scene handoff into ArcadeArena.unity
    /// </summary>
    public static class DemonIdolMainMenuBuilder
    {
        public const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";

        [System.Serializable]
        public class MenuOrchestrationConfig
        {
            public string titleLogo = "MY DEMON IDOL";
            public string subtitle = "NEO-SHIBUYA SOUL HARVEST // 2.5D PLANAR COMBAT";
            public string quote = "\"I am the Demon Lord. Step into the arena and offer your rhythm.\"";
            public string buttonStartText = "ENTER THE ARENA ▶";

            public string defaultMode = "Arcade";
            public string defaultP1 = "C12";
            public string defaultP2 = "C07";
            public string defaultStage = "ST01";

            public Color titleColor = new Color(1f, 0.15f, 0.35f, 1f);
            public Color subtitleColor = new Color(0.2f, 0.85f, 1f, 0.9f);
            public Color quoteColor = new Color(1f, 0.85f, 0.3f, 0.8f);
            public Color startButtonColor = new Color(0.9f, 0.1f, 0.3f, 0.9f);
        }

        public static MenuOrchestrationConfig ActiveConfig = new MenuOrchestrationConfig();

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Play Match in Editor", priority = -50)]
        public static void PlayMatchInEditor()
        {
            EditorApplication.isPlaying = true;
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Scenes/Open Main Menu Scene", priority = 1)]
        public static void OpenMainMenuScene()
        {
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Scenes/Open Arcade Arena Scene", priority = 2)]
        public static void OpenArcadeArenaScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ArcadeArena.unity", OpenSceneMode.Single);
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Scenes/Build Launch Main Menu Scene", priority = 0)]
        public static void BuildMainMenuScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[MainMenuBuilder] Please exit Play Mode before building MainMenu scene.");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += BuildMainMenuScene;
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning("[MainMenuBuilder] Editor is currently compiling scripts, delaying build until compilation completes...");
                EditorApplication.delayCall += BuildMainMenuScene;
                return;
            }

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: BUILDING LAUNCH MAIN MENU SCENE                </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Create a brand-new pristine scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Lighting & URP Volume
            SetupStudioLighting();

            // 3. Setup Pedestal Platform
            Transform pedestal = SetupPedestalPlatform();

            // 4. Setup Camera
            SetupMainMenuCamera();

            // 5. Setup EventSystem
            SetupEventSystem();

            // 6. Setup Canvas & UI Hierarchy
            SetupMainMenuCanvas(pedestal);

            // 7. Save Scene
            string dir = Path.GetDirectoryName(MAIN_MENU_SCENE_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            bool saved = EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);
            RegisterSceneInBuildSettings(MAIN_MENU_SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[MainMenuBuilder] SUCCESS:</b> Launch Main Menu built at '{MAIN_MENU_SCENE_PATH}'!</color>");
            Debug.Log("<color=#00e676>=============================================================</color>");
        }

        private static void SetupStudioLighting()
        {
            GameObject lightingRoot = new GameObject("Lighting_And_Atmosphere");

            // Key Light (Warm Amber)
            GameObject keyLight = new GameObject("Key_Light_Amber");
            keyLight.transform.SetParent(lightingRoot.transform);
            Light kl = keyLight.AddComponent<Light>();
            kl.type = LightType.Directional;
            kl.color = new Color(1.0f, 0.92f, 0.82f);
            kl.intensity = 1.6f;
            keyLight.transform.rotation = Quaternion.Euler(35, -30, 0);

            // Rim Light (Cyan Neon)
            GameObject rimLight = new GameObject("Rim_Light_Cyan");
            rimLight.transform.SetParent(lightingRoot.transform);
            Light rl = rimLight.AddComponent<Light>();
            rl.type = LightType.Directional;
            rl.color = new Color(0.2f, 0.85f, 1.0f);
            rl.intensity = 1.8f;
            rimLight.transform.rotation = Quaternion.Euler(20, 150, 0);

            // Back Fill Light (Magenta Accent)
            GameObject fillLight = new GameObject("Fill_Light_Magenta");
            fillLight.transform.SetParent(lightingRoot.transform);
            Light fl = fillLight.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.9f, 0.2f, 0.6f);
            fl.intensity = 0.8f;
            fillLight.transform.rotation = Quaternion.Euler(-15, 60, 0);

            // Global Post-Processing Volume
            GameObject volGo = new GameObject("Global_URP_Volume");
            volGo.transform.SetParent(lightingRoot.transform);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;

            VolumeProfile prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/Valorant_PostProcessing_Profile.asset")
                ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/Suspense_PostProcessing_Profile.asset");

            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = prof.Add<Bloom>(true);
                bloom.intensity.value = 1.2f;
                bloom.threshold.value = 0.9f;

                var tonemapping = prof.Add<Tonemapping>(true);
                tonemapping.mode.value = TonemappingMode.ACES;

                var ca = prof.Add<ChromaticAberration>(true);
                ca.intensity.value = 0.15f;

                AssetDatabase.CreateAsset(prof, "Assets/Settings/MainMenu_VolumeProfile.asset");
            }
            vol.profile = prof;
        }

        private static Transform SetupPedestalPlatform()
        {
            GameObject pedestalRoot = new GameObject("Pedestal_Environment");

            // Base platform cylinder
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Platform_Base";
            platform.transform.SetParent(pedestalRoot.transform);
            platform.transform.position = new Vector3(0.8f, -0.6f, 0f);
            platform.transform.localScale = new Vector3(3.2f, 0.3f, 3.2f);

            var mr = platform.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MDI_Characters/Materials/AnimeGirl/AnimeGirl_Body_Material.mat");
                if (mat != null) mr.sharedMaterial = mat;
            }

            // Pedestal character spawn transform
            GameObject spawnPoint = new GameObject("Character_Spawn_Point");
            spawnPoint.transform.SetParent(pedestalRoot.transform);
            spawnPoint.transform.position = new Vector3(0.8f, -0.3f, 0f);
            spawnPoint.transform.rotation = Quaternion.Euler(0, 180, 0);

            // Default Fina preview character
            GameObject c12Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/C12_Fina.prefab");
            if (c12Prefab != null)
            {
                GameObject previewChar = (GameObject)PrefabUtility.InstantiatePrefab(c12Prefab, spawnPoint.transform);
                previewChar.name = "Model_Preview_C12";
                previewChar.transform.localPosition = Vector3.zero;
                previewChar.transform.localRotation = Quaternion.identity;

                Animator anim = previewChar.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/AnimeGirls/BaseCharacter/Demo files/AC.controller")
                        ?? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");
                }
            }

            // College Raimon Japanese campus backdrop
            GameObject raimonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CollegeRaimon/Prefabs/CollegeRaimon.prefab");
            if (raimonPrefab != null)
            {
                GameObject campusBackdrop = (GameObject)PrefabUtility.InstantiatePrefab(raimonPrefab, pedestalRoot.transform);
                campusBackdrop.name = "CollegeRaimon_Backdrop";
                campusBackdrop.transform.localPosition = new Vector3(-6.0f, -0.6f, 16.0f);
                campusBackdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                campusBackdrop.transform.localScale = Vector3.one;
            }

            return spawnPoint.transform;
        }

        private static void SetupMainMenuCamera()
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1.0f);
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            camGo.transform.position = new Vector3(0.8f, 0.8f, -3.8f);
            camGo.transform.rotation = Quaternion.Euler(4f, 0f, 0f);

            camGo.AddComponent<AudioListener>();

            var urpData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = true;
        }

        private static void SetupEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static void SetupMainMenuCanvas(Transform pedestalSpawnPoint)
        {
            GameObject canvasGo = new GameObject("Master_MainMenu_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            MainMenuController controller = canvasGo.AddComponent<MainMenuController>();
            controller.PedestalSpawnPoint = pedestalSpawnPoint;
            controller.SelectedP1Id = ActiveConfig.defaultP1;
            controller.SelectedP2Id = ActiveConfig.defaultP2;
            controller.SelectedStageId = ActiveConfig.defaultStage;
            controller.ActiveMode = ActiveConfig.defaultMode;

            Font animeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ==========================================
            // 1. Title Screen Panel
            // ==========================================
            GameObject titlePanel = CreateUIPanel(canvasGo.transform, "Panel_TitleScreen", new Color(0, 0, 0, 0.4f));
            controller.TitlePanel = titlePanel;

            // Title Header
            Text titleText = CreateText(titlePanel.transform, "Title_Logo", ActiveConfig.titleLogo, animeFont, 80, FontStyle.Bold, TextAnchor.MiddleCenter, ActiveConfig.titleColor);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(1000, 120), Vector2.zero);

            Text subtitleText = CreateText(titlePanel.transform, "Title_Subtitle", ActiveConfig.subtitle, animeFont, 24, FontStyle.Normal, TextAnchor.MiddleCenter, ActiveConfig.subtitleColor);
            SetRect(subtitleText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(800, 40), Vector2.zero);

            Text quoteText = CreateText(titlePanel.transform, "Title_Quote", ActiveConfig.quote, animeFont, 20, FontStyle.Italic, TextAnchor.MiddleCenter, ActiveConfig.quoteColor);
            SetRect(quoteText.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(800, 40), Vector2.zero);
            controller.TitleQuoteText = quoteText;

            Button btnStart = CreateButton(titlePanel.transform, "Btn_StartGame", ActiveConfig.buttonStartText, animeFont, 32, ActiveConfig.startButtonColor, Color.white);
            SetRect(btnStart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(400, 75), Vector2.zero);
            controller.BtnTitleStart = btnStart;

            // ==========================================
            // 2. Mode Select Panel
            // ==========================================
            GameObject modePanel = CreateUIPanel(canvasGo.transform, "Panel_ModeSelect", new Color(0.02f, 0.02f, 0.05f, 0.85f));
            controller.ModeSelectPanel = modePanel;

            Text modeTitle = CreateText(modePanel.transform, "Mode_Title", "SELECT COMBAT PROTOCOL", animeFont, 48, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRect(modeTitle.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(800, 60), Vector2.zero);

            Button btnArcade = CreateButton(modePanel.transform, "Card_ModeArcade", "ARCADE LADDER\n<size=18><color=#88FFFFFF>Challenge sequential sorcerers to conquer the abyss</color></size>", animeFont, 28, new Color(0.12f, 0.15f, 0.22f, 0.95f), new Color(0.2f, 0.85f, 1f, 1f));
            SetRect(btnArcade.GetComponent<RectTransform>(), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(650, 90), Vector2.zero);
            controller.BtnModeArcade = btnArcade;

            Button btnVersus = CreateButton(modePanel.transform, "Card_ModeVersus", "LOCAL VERSUS\n<size=18><color=#88FFFFFF>Two-player couch battle on the planar combat stage</color></size>", animeFont, 28, new Color(0.12f, 0.15f, 0.22f, 0.95f), new Color(1f, 0.25f, 0.4f, 1f));
            SetRect(btnVersus.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(650, 90), Vector2.zero);
            controller.BtnModeVersus = btnVersus;

            Button btnTraining = CreateButton(modePanel.transform, "Card_ModeTraining", "TRAINING DOJO\n<size=18><color=#88FFFFFF>Master framedata, drive skills, and domain combos</color></size>", animeFont, 28, new Color(0.12f, 0.15f, 0.22f, 0.95f), new Color(0.3f, 1f, 0.5f, 1f));
            SetRect(btnTraining.GetComponent<RectTransform>(), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(650, 90), Vector2.zero);
            controller.BtnModeTraining = btnTraining;

            Button btnModeBack = CreateButton(modePanel.transform, "Btn_ModeBack", "◀ BACK TO TITLE", animeFont, 20, new Color(0.2f, 0.2f, 0.25f, 0.8f), Color.white);
            SetRect(btnModeBack.GetComponent<RectTransform>(), new Vector2(0.15f, 0.1f), new Vector2(0.15f, 0.1f), new Vector2(220, 50), Vector2.zero);
            controller.BtnModeBack = btnModeBack;

            // ==========================================
            // 3. Character Select Panel
            // ==========================================
            GameObject charPanel = CreateUIPanel(canvasGo.transform, "Panel_CharacterSelect", new Color(0.02f, 0.02f, 0.06f, 0.88f));
            controller.CharacterSelectPanel = charPanel;

            // Left Dossier Container
            GameObject dossierRoot = new GameObject("Dossier_Container", typeof(RectTransform));
            dossierRoot.transform.SetParent(charPanel.transform, false);
            RectTransform dosRt = dossierRoot.GetComponent<RectTransform>();
            SetRect(dosRt, new Vector2(0.03f, 0.1f), new Vector2(0.38f, 0.92f), Vector2.zero, Vector2.zero);

            Image dosBg = dossierRoot.AddComponent<Image>();
            dosBg.color = new Color(0.06f, 0.07f, 0.11f, 0.95f);

            Text badgeText = CreateText(dossierRoot.transform, "Badge_Selecting", "[ SELECTING PLAYER 1 ]", animeFont, 18, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0f, 0.9f, 1f, 1f));
            SetRect(badgeText.rectTransform, new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            controller.SelectionBadgeText = badgeText;

            Text dosName = CreateText(dossierRoot.transform, "Dossier_Name", "UNDERWEAR1", animeFont, 36, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            SetRect(dosName.rectTransform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);
            controller.DossierNameText = dosName;

            Text dosTitle = CreateText(dossierRoot.transform, "Dossier_Title", "HEAVENLY RESTRICTION BRAWLER", animeFont, 18, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.3f, 1f));
            SetRect(dosTitle.rectTransform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            controller.DossierTitleText = dosTitle;

            Text dosArch = CreateText(dossierRoot.transform, "Dossier_Archetype", "ARCHETYPE: TACTICAL RUSHDOWN", animeFont, 16, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.3f, 0.9f, 0.5f, 1f));
            SetRect(dosArch.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.79f), Vector2.zero, Vector2.zero);
            controller.DossierArchetypeText = dosArch;

            Text dosDomain = CreateText(dossierRoot.transform, "Dossier_Domain", "⚡ DOMAIN: REVERSE HEAVEN BURST", animeFont, 17, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.3f, 0.6f, 1f));
            SetRect(dosDomain.rectTransform, new Vector2(0.05f, 0.67f), new Vector2(0.95f, 0.73f), Vector2.zero, Vector2.zero);
            controller.DossierDomainText = dosDomain;

            Text dosDesc = CreateText(dossierRoot.transform, "Dossier_Desc", "Tactical martial artist utilizing supreme speed, relentless low mixups, and devastating domain expansion burst cancels.", animeFont, 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.8f, 0.85f, 0.9f, 0.85f));
            SetRect(dosDesc.rectTransform, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.66f), Vector2.zero, Vector2.zero);
            controller.DossierDescText = dosDesc;

            // Stat Bars Root
            GameObject statsRoot = new GameObject("Stats_Root", typeof(RectTransform));
            statsRoot.transform.SetParent(dossierRoot.transform, false);
            SetRect(statsRoot.GetComponent<RectTransform>(), new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.46f), Vector2.zero, Vector2.zero);

            controller.StatSpeedFill = CreateStatBar(statsRoot.transform, "Speed", "SPEED", 0.75f, new Color(0.2f, 0.8f, 1f, 1f), out controller.StatSpeedValue, animeFont);
            controller.StatPowerFill = CreateStatBar(statsRoot.transform, "Power", "POWER", 0.55f, new Color(1f, 0.3f, 0.4f, 1f), out controller.StatPowerValue, animeFont);
            controller.StatReachFill = CreateStatBar(statsRoot.transform, "Reach", "REACH", 0.35f, new Color(1f, 0.8f, 0.2f, 1f), out controller.StatReachValue, animeFont);
            controller.StatDefenseFill = CreateStatBar(statsRoot.transform, "Defense", "DEFENSE", 0.15f, new Color(0.3f, 0.9f, 0.5f, 1f), out controller.StatDefenseValue, animeFont);

            // Tab P1 / P2 Switcher
            Button tabP1 = CreateButton(dossierRoot.transform, "Btn_TabP1", "[ P1: CHALLENGER ]", animeFont, 16, new Color(0f, 0.4f, 0.5f, 0.9f), Color.white);
            SetRect(tabP1.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.13f), Vector2.zero, Vector2.zero);
            controller.BtnTabP1 = tabP1;

            Button tabP2 = CreateButton(dossierRoot.transform, "Btn_TabP2", "[ P2: OPPONENT ]", animeFont, 16, new Color(0.5f, 0.15f, 0.25f, 0.9f), Color.white);
            SetRect(tabP2.GetComponent<RectTransform>(), new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.13f), Vector2.zero, Vector2.zero);
            controller.BtnTabP2 = tabP2;

            // -------------------------------------------------------------
            // Faction Filter Tabs (ALL / SORCERERS / SIDEKICKS / DOTA HEROES)
            // -------------------------------------------------------------
            GameObject tabsRoot = new GameObject("Faction_Tabs_Root", typeof(RectTransform));
            tabsRoot.transform.SetParent(charPanel.transform, false);
            RectTransform tabsRt = tabsRoot.GetComponent<RectTransform>();
            SetRect(tabsRt, new Vector2(0.40f, 0.88f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);

            string[] tabLabels = new[] { "ALL (25)", "SORCERERS (13)", "SIDEKICKS (5)", "DOTA HEROES (7)" };
            Button[] tabButtons = new Button[4];
            Image[] tabHighlights = new Image[4];

            for (int t = 0; t < 4; t++)
            {
                float tMin = t * 0.25f + 0.005f;
                float tMax = (t + 1) * 0.25f - 0.005f;

                GameObject tabGo = new GameObject($"Tab_Faction_{t}", typeof(RectTransform));
                tabGo.transform.SetParent(tabsRoot.transform, false);
                RectTransform tabRt = tabGo.GetComponent<RectTransform>();
                SetRect(tabRt, new Vector2(tMin, 0f), new Vector2(tMax, 1f), Vector2.zero, Vector2.zero);

                Image tabBg = tabGo.AddComponent<Image>();
                tabBg.color = (t == 0) ? new Color(0.9f, 0.15f, 0.35f, 1f) : new Color(0.15f, 0.18f, 0.25f, 0.8f);
                tabHighlights[t] = tabBg;

                Button tabBtn = tabGo.AddComponent<Button>();
                tabButtons[t] = tabBtn;

                Text tabTxt = CreateText(tabGo.transform, "Label", tabLabels[t], animeFont, 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                SetRect(tabTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            controller.BtnFactionAll = tabButtons[0];
            controller.BtnFactionSorcerers = tabButtons[1];
            controller.BtnFactionSidekicks = tabButtons[2];
            controller.BtnFactionDota = tabButtons[3];

            controller.FactionTabHighlightAll = tabHighlights[0];
            controller.FactionTabHighlightSorcerers = tabHighlights[1];
            controller.FactionTabHighlightSidekicks = tabHighlights[2];
            controller.FactionTabHighlightDota = tabHighlights[3];

            // -------------------------------------------------------------
            // Right Roster Grid Container (6 columns x 5 rows = 30 fighters)
            // -------------------------------------------------------------
            GameObject gridRoot = new GameObject("Roster_Grid_Root", typeof(RectTransform));
            gridRoot.transform.SetParent(charPanel.transform, false);
            RectTransform gridRt = gridRoot.GetComponent<RectTransform>();
            SetRect(gridRt, new Vector2(0.40f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero);
            controller.RosterGridRoot = gridRoot.transform;

            int totalRoster = MainMenuController.CharacterIds.Length;
            controller.CharacterButtons = new Button[totalRoster];
            controller.CharacterHighlights = new Image[totalRoster];
            controller.CharacterNameLabels = new Text[totalRoster];

            for (int i = 0; i < totalRoster; i++)
            {
                int row = i / 6;
                int col = i % 6;

                float xMin = col * (1f / 6f) + 0.003f;
                float xMax = (col + 1) * (1f / 6f) - 0.003f;
                float yMin = (4 - row) * 0.20f + 0.004f;
                float yMax = (5 - row) * 0.20f - 0.004f;

                string charId = MainMenuController.CharacterIds[i];
                string displayName = CharacterRegistry.ResolveDefaultDisplayName(charId).ToUpper();

                GameObject cardGo = new GameObject($"Card_Fighter_{charId}", typeof(RectTransform));
                cardGo.transform.SetParent(gridRoot.transform, false);
                RectTransform cardRt = cardGo.GetComponent<RectTransform>();
                SetRect(cardRt, new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);

                Image cardBg = cardGo.AddComponent<Image>();
                cardBg.color = new Color(0.08f, 0.10f, 0.16f, 0.94f);

                Button cardBtn = cardGo.AddComponent<Button>();
                controller.CharacterButtons[i] = cardBtn;

                // Outline highlight
                GameObject hlGo = new GameObject("Highlight_Border", typeof(RectTransform));
                hlGo.transform.SetParent(cardGo.transform, false);
                RectTransform hlRt = hlGo.GetComponent<RectTransform>();
                SetRect(hlRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image hlImg = hlGo.AddComponent<Image>();
                hlImg.color = (charId == "C12") ? new Color(0f, 0.9f, 1f, 1f) : new Color(1f, 1f, 1f, 0.12f);
                controller.CharacterHighlights[i] = hlImg;

                // ID badge top-left
                Text idLabel = CreateText(cardGo.transform, "Label_Id", charId, animeFont, 11, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.3f, 0.85f));
                SetRect(idLabel.rectTransform, new Vector2(0.06f, 0.65f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);

                // Name label bottom
                Text nameLabel = CreateText(cardGo.transform, "Label_Name", displayName, animeFont, 12, FontStyle.Bold, TextAnchor.LowerCenter, Color.white);
                SetRect(nameLabel.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.60f), Vector2.zero, Vector2.zero);
                controller.CharacterNameLabels[i] = nameLabel;
            }

            // Bottom Navigation for Character Select
            Button btnCharNext = CreateButton(charPanel.transform, "Btn_CharNext", "PROCEED TO ARENA SELECTION ▶", animeFont, 22, new Color(0.9f, 0.15f, 0.35f, 0.95f), Color.white);
            SetRect(btnCharNext.GetComponent<RectTransform>(), new Vector2(0.60f, 0.05f), new Vector2(0.98f, 0.12f), Vector2.zero, Vector2.zero);
            controller.BtnCharSelectNext = btnCharNext;

            Button btnCharBack = CreateButton(charPanel.transform, "Btn_CharBack", "◀ BACK TO MODES", animeFont, 18, new Color(0.2f, 0.2f, 0.25f, 0.8f), Color.white);
            SetRect(btnCharBack.GetComponent<RectTransform>(), new Vector2(0.40f, 0.05f), new Vector2(0.58f, 0.12f), Vector2.zero, Vector2.zero);
            controller.BtnCharSelectBack = btnCharBack;

            // ==========================================
            // 4. Stage Select Panel
            // ==========================================
            GameObject stagePanel = CreateUIPanel(canvasGo.transform, "Panel_StageSelect", new Color(0.02f, 0.02f, 0.06f, 0.88f));
            controller.StageSelectPanel = stagePanel;

            Text stageTitle = CreateText(stagePanel.transform, "Stage_Title", "SELECT COMBAT ARENA THEATRE", animeFont, 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRect(stageTitle.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(800, 60), Vector2.zero);

            // Stage cards (4-column grid for ST01 - ST07)
            string[] stageLabels = new[]
            {
                "ST01: REACTOR CORE",
                "ST02: CYBER HANGAR",
                "ST03: NEO SHIBUYA",
                "ST04: INK FORTRESS",
                "ST05: METRO SUBWAY",
                "ST06: OFFICE TOWER",
                "ST07: COLLEGE RAIMON"
            };

            controller.StageButtons = new Button[stageLabels.Length];
            controller.StageHighlights = new Image[stageLabels.Length];

            for (int i = 0; i < stageLabels.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;

                float xMin = 0.08f + col * 0.22f;
                float xMax = xMin + 0.18f;
                float yMin = 0.58f - row * 0.18f;
                float yMax = yMin + 0.14f;

                GameObject stCard = new GameObject($"Card_Stage_{i:D2}", typeof(RectTransform));
                stCard.transform.SetParent(stagePanel.transform, false);
                SetRect(stCard.GetComponent<RectTransform>(), new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);

                Image bg = stCard.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

                Button btn = stCard.AddComponent<Button>();
                controller.StageButtons[i] = btn;

                GameObject hlGo = new GameObject("Border_Highlight", typeof(RectTransform));
                hlGo.transform.SetParent(stCard.transform, false);
                SetRect(hlGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image hlImg = hlGo.AddComponent<Image>();
                hlImg.color = (i == 0) ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.15f);
                controller.StageHighlights[i] = hlImg;

                Text lbl = CreateText(stCard.transform, "Label", stageLabels[i], animeFont, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                SetRect(lbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            // Stage Dossier Info Box
            GameObject stDossier = new GameObject("Stage_Dossier_Box", typeof(RectTransform));
            stDossier.transform.SetParent(stagePanel.transform, false);
            SetRect(stDossier.GetComponent<RectTransform>(), new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.36f), Vector2.zero, Vector2.zero);
            Image dosBoxBg = stDossier.AddComponent<Image>();
            dosBoxBg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

            Text stNameText = CreateText(stDossier.transform, "StageName", "REACTOR CORE SILO // TACTICAL EXPANSION", animeFont, 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f, 1f));
            SetRect(stNameText.rectTransform, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            controller.StageNameText = stNameText;

            Text stHazText = CreateText(stDossier.transform, "StageHazards", "[ HIGH VOLTAGE REACTOR CORE • INTENSE WALL SPLAT ]", animeFont, 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.3f, 0.9f, 0.5f, 1f));
            SetRect(stHazText.rectTransform, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.65f), Vector2.zero, Vector2.zero);
            controller.StageHazardText = stHazText;

            Text stSurfText = CreateText(stDossier.transform, "StageSurfaces", "SURFACE: PLATFORM // BOUNDS: ±14.0 METERS", animeFont, 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.8f, 0.9f, 0.8f));
            SetRect(stSurfText.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.35f), Vector2.zero, Vector2.zero);
            controller.StageSurfaceText = stSurfText;

            // Launch Battle Button
            Button btnFight = CreateButton(stagePanel.transform, "Btn_Fight", "BATTLE START! ▶", animeFont, 30, new Color(1f, 0.1f, 0.3f, 0.95f), Color.white);
            SetRect(btnFight.GetComponent<RectTransform>(), new Vector2(0.65f, 0.10f), new Vector2(0.65f, 0.10f), new Vector2(400, 70), Vector2.zero);
            controller.BtnFight = btnFight;

            Button btnStageBack = CreateButton(stagePanel.transform, "Btn_StageBack", "◀ BACK TO CHARACTERS", animeFont, 20, new Color(0.2f, 0.2f, 0.25f, 0.8f), Color.white);
            SetRect(btnStageBack.GetComponent<RectTransform>(), new Vector2(0.35f, 0.10f), new Vector2(0.35f, 0.10f), new Vector2(260, 70), Vector2.zero);
            controller.BtnStageBack = btnStageBack;

            // ==========================================
            // 5. Pre-Match VS Splash Overlay
            // ==========================================
            SetupPreMatchVSOverlay(canvasGo.transform, animeFont);
        }

        private static void SetupPreMatchVSOverlay(Transform canvasTransform, Font animeFont)
        {
            GameObject overlayGo = new GameObject("Overlay_PreMatchVS", typeof(RectTransform));
            overlayGo.transform.SetParent(canvasTransform, false);
            RectTransform ovRt = overlayGo.GetComponent<RectTransform>();
            SetRect(ovRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image ovBg = overlayGo.AddComponent<Image>();
            ovBg.color = new Color(0.02f, 0.02f, 0.05f, 0.96f);

            CanvasGroup cg = overlayGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            PreMatchVSOverlay vsOverlay = overlayGo.AddComponent<PreMatchVSOverlay>();
            vsOverlay.OverlayRoot = overlayGo;
            vsOverlay.CanvasGroup = cg;

            // -------------------------------------------------------------
            // P1 Challenger Panel (Cyan Accent)
            // -------------------------------------------------------------
            GameObject p1Go = new GameObject("Panel_P1_Challenger", typeof(RectTransform));
            p1Go.transform.SetParent(overlayGo.transform, false);
            RectTransform p1Rt = p1Go.GetComponent<RectTransform>();
            SetRect(p1Rt, new Vector2(0f, 0f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);
            vsOverlay.P1Panel = p1Rt;

            // P1 portrait box
            GameObject p1PortGo = new GameObject("P1_Portrait", typeof(RectTransform));
            p1PortGo.transform.SetParent(p1Rt, false);
            RectTransform p1PortRt = p1PortGo.GetComponent<RectTransform>();
            SetRect(p1PortRt, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            Image p1PortImg = p1PortGo.AddComponent<Image>();
            p1PortImg.color = new Color(0.1f, 0.2f, 0.3f, 0.3f);
            vsOverlay.P1PortraitImage = p1PortImg;

            // P1 texts
            Text p1Badge = CreateText(p1Rt, "P1_Badge", "[ CHALLENGER // PLAYER 1 ]", animeFont, 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0f, 0.9f, 1f, 1f));
            SetRect(p1Badge.rectTransform, new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.26f), Vector2.zero, Vector2.zero);

            Text p1Name = CreateText(p1Rt, "P1_Name", "FINA", animeFont, 44, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetRect(p1Name.rectTransform, new Vector2(0.12f, 0.15f), new Vector2(0.88f, 0.22f), Vector2.zero, Vector2.zero);
            vsOverlay.P1NameText = p1Name;

            Text p1Title = CreateText(p1Rt, "P1_Title", "HEAVENLY RESTRICTION BRAWLER", animeFont, 20, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f, 1f));
            SetRect(p1Title.rectTransform, new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.15f), Vector2.zero, Vector2.zero);
            vsOverlay.P1TitleText = p1Title;

            Text p1Arch = CreateText(p1Rt, "P1_Archetype", "ARCHETYPE: TACTICAL RUSHDOWN", animeFont, 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.3f, 0.9f, 0.5f, 1f));
            SetRect(p1Arch.rectTransform, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.10f), Vector2.zero, Vector2.zero);
            vsOverlay.P1ArchetypeText = p1Arch;

            Text p1Domain = CreateText(p1Rt, "P1_Domain", "⚡ DOMAIN: REVERSE HEAVEN BURST", animeFont, 17, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.3f, 0.6f, 1f));
            SetRect(p1Domain.rectTransform, new Vector2(0.12f, 0.02f), new Vector2(0.88f, 0.06f), Vector2.zero, Vector2.zero);
            vsOverlay.P1DomainText = p1Domain;

            // -------------------------------------------------------------
            // P2 Opponent Panel (Crimson Accent)
            // -------------------------------------------------------------
            GameObject p2Go = new GameObject("Panel_P2_Opponent", typeof(RectTransform));
            p2Go.transform.SetParent(overlayGo.transform, false);
            RectTransform p2Rt = p2Go.GetComponent<RectTransform>();
            SetRect(p2Rt, new Vector2(0.52f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            vsOverlay.P2Panel = p2Rt;

            // P2 portrait box
            GameObject p2PortGo = new GameObject("P2_Portrait", typeof(RectTransform));
            p2PortGo.transform.SetParent(p2Rt, false);
            RectTransform p2PortRt = p2PortGo.GetComponent<RectTransform>();
            SetRect(p2PortRt, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            Image p2PortImg = p2PortGo.AddComponent<Image>();
            p2PortImg.color = new Color(0.3f, 0.1f, 0.15f, 0.3f);
            vsOverlay.P2PortraitImage = p2PortImg;

            // P2 texts
            Text p2Badge = CreateText(p2Rt, "P2_Badge", "[ OPPONENT // PLAYER 2 ]", animeFont, 18, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.25f, 0.35f, 1f));
            SetRect(p2Badge.rectTransform, new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.26f), Vector2.zero, Vector2.zero);

            Text p2Name = CreateText(p2Rt, "P2_Name", "STELLARWITCH", animeFont, 44, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
            SetRect(p2Name.rectTransform, new Vector2(0.12f, 0.15f), new Vector2(0.88f, 0.22f), Vector2.zero, Vector2.zero);
            vsOverlay.P2NameText = p2Name;

            Text p2Title = CreateText(p2Rt, "P2_Title", "STELLAR VOID SORCERESS", animeFont, 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.3f, 1f));
            SetRect(p2Title.rectTransform, new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.15f), Vector2.zero, Vector2.zero);
            vsOverlay.P2TitleText = p2Title;

            Text p2Arch = CreateText(p2Rt, "P2_Archetype", "ARCHETYPE: ZONER / TRAP", animeFont, 16, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.3f, 0.9f, 0.5f, 1f));
            SetRect(p2Arch.rectTransform, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.10f), Vector2.zero, Vector2.zero);
            vsOverlay.P2ArchetypeText = p2Arch;

            Text p2Domain = CreateText(p2Rt, "P2_Domain", "⚡ DOMAIN: NEBULA GRAVITY WELL", animeFont, 17, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.3f, 0.6f, 1f));
            SetRect(p2Domain.rectTransform, new Vector2(0.12f, 0.02f), new Vector2(0.88f, 0.06f), Vector2.zero, Vector2.zero);
            vsOverlay.P2DomainText = p2Domain;

            // -------------------------------------------------------------
            // Center Clash Emblem
            // -------------------------------------------------------------
            GameObject clashGo = new GameObject("Root_CenterClash", typeof(RectTransform));
            clashGo.transform.SetParent(overlayGo.transform, false);
            RectTransform clashRt = clashGo.GetComponent<RectTransform>();
            SetRect(clashRt, new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.80f), Vector2.zero, Vector2.zero);
            vsOverlay.CenterClashRoot = clashRt;

            Text kanjiText = CreateText(clashGo.transform, "Text_KanjiClash", "決戦", animeFont, 50, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.2f, 0.3f, 1f));
            SetRect(kanjiText.rectTransform, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.90f), Vector2.zero, Vector2.zero);
            vsOverlay.KanjiClashText = kanjiText;

            Text vsText = CreateText(clashGo.transform, "Text_VS", "VS", animeFont, 84, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f, 1f));
            SetRect(vsText.rectTransform, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.65f), Vector2.zero, Vector2.zero);
            vsOverlay.VsEmblemText = vsText;

            Text modeBadge = CreateText(clashGo.transform, "Badge_Mode", "[ ARCADE BATTLE ]", animeFont, 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.2f, 0.85f, 1f, 1f));
            SetRect(modeBadge.rectTransform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.32f), Vector2.zero, Vector2.zero);
            vsOverlay.ModeBadgeText = modeBadge;

            Text stageBadge = CreateText(clashGo.transform, "Badge_Stage", "ARENA: REACTOR CORE // TACTICAL EXPANSION", animeFont, 16, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            SetRect(stageBadge.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.22f), Vector2.zero, Vector2.zero);
            vsOverlay.StageBadgeText = stageBadge;

            Text skipPrompt = CreateText(clashGo.transform, "Text_SkipPrompt", "PRESS ANY KEY OR TAP SCREEN TO SKIP ▶", animeFont, 13, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f, 0.8f));
            SetRect(skipPrompt.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.10f), Vector2.zero, Vector2.zero);

            overlayGo.SetActive(false);
        }

        private static Image CreateStatBar(Transform parent, string id, string label, float yNorm, Color barColor, out Text valueText, Font font)
        {
            GameObject row = new GameObject($"StatRow_{id}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0f, yNorm), new Vector2(1f, yNorm + 0.18f), Vector2.zero, Vector2.zero);

            Text lbl = CreateText(row.transform, "Label", label, font, 14, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetRect(lbl.rectTransform, new Vector2(0f, 0f), new Vector2(0.3f, 1f), Vector2.zero, Vector2.zero);

            // Bar background
            GameObject barBg = new GameObject("BarBg", typeof(RectTransform));
            barBg.transform.SetParent(row.transform, false);
            SetRect(barBg.GetComponent<RectTransform>(), new Vector2(0.32f, 0.2f), new Vector2(0.82f, 0.8f), Vector2.zero, Vector2.zero);
            Image bgImg = barBg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            // Bar fill
            GameObject barFill = new GameObject("BarFill", typeof(RectTransform));
            barFill.transform.SetParent(barBg.transform, false);
            SetRect(barFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.85f;
            fillImg.color = barColor;

            valueText = CreateText(row.transform, "Value", "85%", font, 13, FontStyle.Normal, TextAnchor.MiddleRight, barColor);
            SetRect(valueText.rectTransform, new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            return fillImg;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            SetRect(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string text, Font font, int size, FontStyle style, TextAnchor align, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.supportRichText = true;
            return t;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, int size, Color bgColor, Color textColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.color = bgColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.8f;
            btn.colors = cb;

            Text t = CreateText(go.transform, "Label", label, font, size, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
            SetRect(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return btn;
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            if (min == max)
            {
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
            }
            else
            {
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i + 1] = scenes[i];
            }
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
#endif
