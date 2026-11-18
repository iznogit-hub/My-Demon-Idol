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

            // Right Roster Grid Container (2 rows of 7 = 14 fighters)
            GameObject gridRoot = new GameObject("Roster_Grid_Root", typeof(RectTransform));
            gridRoot.transform.SetParent(charPanel.transform, false);
            RectTransform gridRt = gridRoot.GetComponent<RectTransform>();
            SetRect(gridRt, new Vector2(0.42f, 0.22f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            controller.RosterGridRoot = gridRoot.transform;

            string[] names = new[]
            {
                "C01: AKIO", "C02: AIKA", "C03: REN", "C04: UNITYCHAN",
                "C05: AKANE", "C06: CULTIST", "C07: STELLARWITCH", "C08: SANPAKU",
                "C09: PICOCHAN", "C10: REIKA", "C11: CASUAL1", "C12: FINA",
                "C13: SAPPHI", "C14: MURYOTAISU"
            };

            controller.CharacterButtons = new Button[14];
            controller.CharacterHighlights = new Image[14];
            controller.CharacterNameLabels = new Text[14];

            for (int i = 0; i < 14; i++)
            {
                int row = i / 7;
                int col = i % 7;

                float xMin = col * (1f / 7f) + 0.005f;
                float xMax = (col + 1) * (1f / 7f) - 0.005f;
                float yMin = (1 - row) * 0.48f + 0.02f;
                float yMax = yMin + 0.46f;

                GameObject cardGo = new GameObject($"Card_Fighter_{i:D2}", typeof(RectTransform));
                cardGo.transform.SetParent(gridRoot.transform, false);
                RectTransform cardRt = cardGo.GetComponent<RectTransform>();
                SetRect(cardRt, new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);

                Image cardBg = cardGo.AddComponent<Image>();
                cardBg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

                Button cardBtn = cardGo.AddComponent<Button>();
                controller.CharacterButtons[i] = cardBtn;

                // Outline highlight
                GameObject hlGo = new GameObject("Highlight_Border", typeof(RectTransform));
                hlGo.transform.SetParent(cardGo.transform, false);
                RectTransform hlRt = hlGo.GetComponent<RectTransform>();
                SetRect(hlRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image hlImg = hlGo.AddComponent<Image>();
                hlImg.color = (i == 11) ? new Color(0f, 0.9f, 1f, 1f) : new Color(1f, 1f, 1f, 0.1f);
                controller.CharacterHighlights[i] = hlImg;

                Text nameLabel = CreateText(cardGo.transform, "Label_Name", names[i], animeFont, 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                SetRect(nameLabel.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.35f), Vector2.zero, Vector2.zero);
                controller.CharacterNameLabels[i] = nameLabel;
            }

            // Bottom Navigation for Character Select
            Button btnCharNext = CreateButton(charPanel.transform, "Btn_CharNext", "PROCEED TO ARENA SELECTION ▶", animeFont, 24, new Color(0.9f, 0.15f, 0.35f, 0.95f), Color.white);
            SetRect(btnCharNext.GetComponent<RectTransform>(), new Vector2(0.72f, 0.08f), new Vector2(0.72f, 0.08f), new Vector2(460, 60), Vector2.zero);
            controller.BtnCharSelectNext = btnCharNext;

            Button btnCharBack = CreateButton(charPanel.transform, "Btn_CharBack", "◀ BACK TO MODES", animeFont, 20, new Color(0.2f, 0.2f, 0.25f, 0.8f), Color.white);
            SetRect(btnCharBack.GetComponent<RectTransform>(), new Vector2(0.48f, 0.08f), new Vector2(0.48f, 0.08f), new Vector2(220, 60), Vector2.zero);
            controller.BtnCharSelectBack = btnCharBack;

            // ==========================================
            // 4. Stage Select Panel
            // ==========================================
            GameObject stagePanel = CreateUIPanel(canvasGo.transform, "Panel_StageSelect", new Color(0.02f, 0.02f, 0.06f, 0.88f));
            controller.StageSelectPanel = stagePanel;

            Text stageTitle = CreateText(stagePanel.transform, "Stage_Title", "SELECT COMBAT ARENA THEATRE", animeFont, 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRect(stageTitle.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(800, 60), Vector2.zero);

            // Stage cards (3 x 2 grid)
            controller.StageButtons = new Button[6];
            controller.StageHighlights = new Image[6];

            string[] stageLabels = new[]
            {
                "ST01: REACTOR CORE",
                "ST02: CYBER HANGAR",
                "ST03: NEO SHIBUYA",
                "ST04: INK FORTRESS",
                "ST05: METRO SUBWAY",
                "ST06: OFFICE TOWER"
            };

            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;

                float xMin = 0.15f + col * 0.25f;
                float xMax = xMin + 0.20f;
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

                Text lbl = CreateText(stCard.transform, "Label", stageLabels[i], animeFont, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
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
