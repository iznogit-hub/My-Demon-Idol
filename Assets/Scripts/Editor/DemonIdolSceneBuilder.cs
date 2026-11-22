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
        // SCENE 1: MainMenu.unity
        // =========================================================================
        public static void BuildMainMenuScene(bool interactive = true)
        {
            if (!ConfirmAndBackup(SCENE_MAIN_MENU, "MainMenu", interactive, out _))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Audio Director (single DontDestroyOnLoad audio manager)
            GameObject audioObj = new GameObject("CombatAudio_Manager");
            audioObj.AddComponent<CombatAudioDirector>();

            // 2. Camera & Lighting
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            camObj.AddComponent<UniversalAdditionalCameraData>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0, 1.2f, -5f);

            GameObject lightObj = new GameObject("Directional_KeyLight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            // 3. Canvas & UI
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            DemonIdolMainMenuController controller = canvasObj.AddComponent<DemonIdolMainMenuController>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Title Header
            GameObject titleObj = CreateUIObject("Title_Text", canvasObj.transform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0, 0), new Vector2(800, 100));
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.text = "MY DEMON IDOL";
            titleText.fontSize = 58;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.18f, 0.35f, 1f);

            GameObject subObj = CreateUIObject("Subtitle_Text", canvasObj.transform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), new Vector2(0, 0), new Vector2(800, 40));
            Text subText = subObj.AddComponent<Text>();
            subText.font = defaultFont;
            subText.text = "NEO-SHIBUYA SOUL HARVEST // PLANAR COMBAT";
            subText.fontSize = 20;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.2f, 0.85f, 1f, 0.9f);

            // Menu Buttons Panel
            GameObject btnPanel = CreateUIObject("Menu_Panel", canvasObj.transform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0, 0), new Vector2(360, 420));

            controller.btnArcade = CreateMenuButton(btnPanel.transform, "Btn_Arcade", "ARCADE", new Vector2(0, 150), new Color(0.85f, 0.15f, 0.25f, 0.95f), defaultFont);
            controller.btnVersus = CreateMenuButton(btnPanel.transform, "Btn_Versus", "VERSUS (LOCAL 2P)", new Vector2(0, 90), new Color(0.15f, 0.45f, 0.85f, 0.95f), defaultFont);
            controller.btnTraining = CreateMenuButton(btnPanel.transform, "Btn_Training", "TRAINING DOJO", new Vector2(0, 30), new Color(0.15f, 0.65f, 0.45f, 0.95f), defaultFont);
            controller.btnControls = CreateMenuButton(btnPanel.transform, "Btn_Controls", "CONTROLS", new Vector2(0, -30), new Color(0.25f, 0.28f, 0.35f, 0.95f), defaultFont);
            controller.btnSettings = CreateMenuButton(btnPanel.transform, "Btn_Settings", "SETTINGS", new Vector2(0, -90), new Color(0.25f, 0.28f, 0.35f, 0.95f), defaultFont);
            controller.btnQuit = CreateMenuButton(btnPanel.transform, "Btn_Quit", "QUIT", new Vector2(0, -150), new Color(0.2f, 0.2f, 0.25f, 0.95f), defaultFont);

            // Controls Modal
            GameObject ctrlModal = CreateUIObject("Modal_Controls", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image ctrlModalBg = ctrlModal.AddComponent<Image>();
            ctrlModalBg.color = new Color(0f, 0f, 0f, 0.85f);

            GameObject ctrlBox = CreateUIObject("Controls_Box", ctrlModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 440));
            Image ctrlBoxBg = ctrlBox.AddComponent<Image>();
            ctrlBoxBg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

            GameObject ctrlTitle = CreateUIObject("Controls_Title", ctrlBox.transform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(500, 40));
            Text ctrlTitleTxt = ctrlTitle.AddComponent<Text>();
            ctrlTitleTxt.font = defaultFont;
            ctrlTitleTxt.text = "COMBAT CONTROLS";
            ctrlTitleTxt.fontSize = 24;
            ctrlTitleTxt.fontStyle = FontStyle.Bold;
            ctrlTitleTxt.alignment = TextAnchor.MiddleCenter;
            ctrlTitleTxt.color = new Color(1f, 0.85f, 0.3f, 1f);

            GameObject ctrlContent = CreateUIObject("Controls_Content", ctrlBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 240));
            Text ctrlContentTxt = ctrlContent.AddComponent<Text>();
            ctrlContentTxt.font = defaultFont;
            ctrlContentTxt.text = "MOVEMENT: WASD / Arrow Keys / Gamepad Left Stick\n" +
                                  "LIGHT ATTACK (A): U / Button South (A/Cross)\n" +
                                  "MEDIUM ATTACK (B): I / Button West (X/Square)\n" +
                                  "HEAVY ATTACK (C): O / Button East (B/Circle)\n" +
                                  "DRIVE (D): P / Button North (Y/Triangle)\n" +
                                  "BLOCK: Hold Backward (4) / Down-Back (1)\n" +
                                  "PAUSE: Escape / Start Button";
            ctrlContentTxt.fontSize = 17;
            ctrlContentTxt.alignment = TextAnchor.MiddleLeft;
            ctrlContentTxt.lineSpacing = 1.3f;
            ctrlContentTxt.color = Color.white;

            controller.btnCloseControls = CreateMenuButton(ctrlBox.transform, "Btn_CloseControls", "CLOSE", new Vector2(0, -170), new Color(0.85f, 0.2f, 0.3f, 1f), defaultFont);
            controller.controlsModal = ctrlModal;
            ctrlModal.SetActive(false);

            // Settings Modal
            GameObject setModal = CreateUIObject("Modal_Settings", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image setModalBg = setModal.AddComponent<Image>();
            setModalBg.color = new Color(0f, 0f, 0f, 0.85f);

            GameObject setBox = CreateUIObject("Settings_Box", setModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 360));
            Image setBoxBg = setBox.AddComponent<Image>();
            setBoxBg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

            GameObject setTitle = CreateUIObject("Settings_Title", setBox.transform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(400, 40));
            Text setTitleTxt = setTitle.AddComponent<Text>();
            setTitleTxt.font = defaultFont;
            setTitleTxt.text = "AUDIO SETTINGS";
            setTitleTxt.fontSize = 24;
            setTitleTxt.fontStyle = FontStyle.Bold;
            setTitleTxt.alignment = TextAnchor.MiddleCenter;
            setTitleTxt.color = new Color(1f, 0.85f, 0.3f, 1f);

            controller.sliderBGM = CreateSlider(setBox.transform, "Slider_BGM", "BGM Volume", new Vector2(0, 40), defaultFont);
            controller.sliderSFX = CreateSlider(setBox.transform, "Slider_SFX", "SFX Volume", new Vector2(0, -30), defaultFont);
            controller.btnCloseSettings = CreateMenuButton(setBox.transform, "Btn_CloseSettings", "SAVE & CLOSE", new Vector2(0, -120), new Color(0.2f, 0.65f, 0.45f, 1f), defaultFont);
            controller.settingsModal = setModal;
            setModal.SetActive(false);

            // 4. Single EventSystem with InputSystemUIInputModule
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, SCENE_MAIN_MENU);
            Debug.Log($"<color=#00e676>[DemonIdolSceneBuilder] Built MainMenu successfully at {SCENE_MAIN_MENU}</color>");
        }

        // =========================================================================
        // SCENE 2: CharacterSelect.unity
        // =========================================================================
        public static void BuildCharacterSelectScene(bool interactive = true)
        {
            if (!ConfirmAndBackup(SCENE_CHAR_SELECT, "CharacterSelect", interactive, out _))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & 3D Preview Pedestal
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
            camObj.AddComponent<UniversalAdditionalCameraData>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(-2.8f, 1.4f, -4.5f);
            camObj.transform.rotation = Quaternion.Euler(6f, 18f, 0f);

            GameObject lightObj = new GameObject("Directional_PreviewLight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.95f, 0.92f, 1f);
            light.intensity = 1.3f;
            lightObj.transform.rotation = Quaternion.Euler(30f, -25f, 0f);

            GameObject rimObj = new GameObject("Directional_RimLight");
            Light rimLight = rimObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.3f, 0.7f, 1f);
            rimLight.intensity = 0.8f;
            rimObj.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);

            GameObject pedestal = new GameObject("Preview_Pedestal");
            pedestal.transform.position = new Vector3(-1.8f, 0f, 0f);
            pedestal.transform.rotation = Quaternion.Euler(0f, 160f, 0f);

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Pedestal_Disc";
            disc.transform.SetParent(pedestal.transform, false);
            disc.transform.localPosition = new Vector3(0, -0.05f, 0);
            disc.transform.localScale = new Vector3(2.4f, 0.05f, 2.4f);
            var discCol = disc.GetComponent<Collider>();
            if (discCol != null) UnityEngine.Object.DestroyImmediate(discCol);

            // 2. Canvas & UI
            GameObject canvasObj = new GameObject("CharacterSelect_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            DemonIdolCharacterSelectManager mgr = canvasObj.AddComponent<DemonIdolCharacterSelectManager>();
            mgr.previewPedestal = pedestal.transform;
            mgr.previewCamera = cam;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Header Banner
            GameObject header = CreateUIObject("Header_Title", canvasObj.transform, new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(1000, 60));
            Text headerTxt = header.AddComponent<Text>();
            headerTxt.font = font;
            headerTxt.text = "SELECT YOUR FIGHTER";
            headerTxt.fontSize = 38;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(1f, 0.85f, 0.3f, 1f);

            // Dossier Card (Left side)
            GameObject dossier = CreateUIObject("Dossier_Panel", canvasObj.transform, new Vector2(0.04f, 0.5f), new Vector2(0.04f, 0.5f), new Vector2(180, 0), new Vector2(360, 520));
            Image dossierBg = dossier.AddComponent<Image>();
            dossierBg.color = new Color(0.07f, 0.09f, 0.13f, 0.95f);

            GameObject dName = CreateUIObject("Dossier_Name", dossier.transform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(320, 50));
            mgr.dossierName = dName.AddComponent<Text>();
            mgr.dossierName.font = font;
            mgr.dossierName.fontSize = 28;
            mgr.dossierName.fontStyle = FontStyle.Bold;
            mgr.dossierName.alignment = TextAnchor.MiddleCenter;
            mgr.dossierName.color = new Color(0.2f, 0.85f, 1f, 1f);

            GameObject dRole = CreateUIObject("Dossier_Role", dossier.transform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(320, 30));
            mgr.dossierArchetype = dRole.AddComponent<Text>();
            mgr.dossierArchetype.font = font;
            mgr.dossierArchetype.fontSize = 18;
            mgr.dossierArchetype.fontStyle = FontStyle.Bold;
            mgr.dossierArchetype.alignment = TextAnchor.MiddleCenter;
            mgr.dossierArchetype.color = new Color(1f, 0.85f, 0.3f, 0.9f);

            GameObject dBio = CreateUIObject("Dossier_Bio", dossier.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 160));
            mgr.dossierBio = dBio.AddComponent<Text>();
            mgr.dossierBio.font = font;
            mgr.dossierBio.fontSize = 16;
            mgr.dossierBio.alignment = TextAnchor.UpperLeft;
            mgr.dossierBio.lineSpacing = 1.25f;
            mgr.dossierBio.color = new Color(0.9f, 0.92f, 0.95f, 0.9f);

            GameObject p1Stat = CreateUIObject("P1_Status", dossier.transform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(320, 36));
            mgr.p1StatusText = p1Stat.AddComponent<Text>();
            mgr.p1StatusText.font = font;
            mgr.p1StatusText.fontSize = 18;
            mgr.p1StatusText.fontStyle = FontStyle.Bold;
            mgr.p1StatusText.alignment = TextAnchor.MiddleCenter;
            mgr.p1StatusText.color = new Color(0.2f, 0.8f, 1f, 1f);

            GameObject p2Stat = CreateUIObject("P2_Status", dossier.transform, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(320, 36));
            mgr.p2StatusText = p2Stat.AddComponent<Text>();
            mgr.p2StatusText.font = font;
            mgr.p2StatusText.fontSize = 18;
            mgr.p2StatusText.fontStyle = FontStyle.Bold;
            mgr.p2StatusText.alignment = TextAnchor.MiddleCenter;
            mgr.p2StatusText.color = new Color(1f, 0.3f, 0.3f, 1f);

            // 25 Roster Grid (Right side: 5 columns x 5 rows)
            GameObject gridPanel = CreateUIObject("Roster_Grid", canvasObj.transform, new Vector2(0.68f, 0.48f), new Vector2(0.68f, 0.48f), Vector2.zero, new Vector2(750, 680));
            mgr.gridRoot = gridPanel.GetComponent<RectTransform>();

            string[] roster = DemonIdolCharacterSelectManager.RosterIds;
            mgr.characterButtons = new Button[roster.Length];
            mgr.p1Borders = new Image[roster.Length];
            mgr.p2Borders = new Image[roster.Length];
            mgr.characterLabels = new Text[roster.Length];

            float cardW = 135f;
            float cardH = 120f;
            float spacingX = 15f;
            float spacingY = 15f;
            float startX = -((5 * cardW + 4 * spacingX) / 2f) + cardW / 2f;
            float startY = ((5 * cardH + 4 * spacingY) / 2f) - cardH / 2f;

            for (int i = 0; i < roster.Length; i++)
            {
                int row = i / 5;
                int col = i % 5;
                float x = startX + col * (cardW + spacingX);
                float y = startY - row * (cardH + spacingY);

                string cid = roster[i];
                var def = CharacterRegistry.GetCharacterById(cid);
                string cname = def != null ? def.displayName : cid;

                GameObject card = CreateUIObject($"Card_{cid}", gridPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(cardW, cardH));
                Image cardBg = card.AddComponent<Image>();
                cardBg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
                Button cardBtn = card.AddComponent<Button>();

                // Mugshot image
                GameObject mugGo = CreateUIObject("Mugshot", card.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8, -26));
                Image mugImg = mugGo.AddComponent<Image>();
                mugImg.preserveAspect = true;
                Sprite mugSprite = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{cid}_{cname}");
                if (mugSprite != null) mugImg.sprite = mugSprite;
                else mugImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);

                // Name label
                GameObject nameGo = CreateUIObject("Label", card.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 12), new Vector2(0, 24));
                Text nameTxt = nameGo.AddComponent<Text>();
                nameTxt.font = font;
                nameTxt.text = cname.ToUpper();
                nameTxt.fontSize = 13;
                nameTxt.fontStyle = FontStyle.Bold;
                nameTxt.alignment = TextAnchor.MiddleCenter;
                nameTxt.color = Color.white;

                // P1 Border
                GameObject p1Border = CreateUIObject("P1_Border", card.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(6, 6));
                Image p1BorderImg = p1Border.AddComponent<Image>();
                p1BorderImg.color = new Color(0.2f, 0.8f, 1f, 1f);
                p1Border.SetActive(false);

                // P2 Border
                GameObject p2Border = CreateUIObject("P2_Border", card.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(4, 4));
                Image p2BorderImg = p2Border.AddComponent<Image>();
                p2BorderImg.color = new Color(1f, 0.3f, 0.3f, 1f);
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
            vsBg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);

            GameObject vsBanner = CreateUIObject("VS_Banner", vsGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 100));
            Text vsTxt = vsBanner.AddComponent<Text>();
            vsTxt.font = font;
            vsTxt.text = "VS";
            vsTxt.fontSize = 72;
            vsTxt.fontStyle = FontStyle.BoldAndItalic;
            vsTxt.alignment = TextAnchor.MiddleCenter;
            vsTxt.color = new Color(1f, 0.85f, 0.2f, 1f);

            GameObject vsP1 = CreateUIObject("P1_Preview", vsGo.transform, new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f), Vector2.zero, new Vector2(300, 380));
            mgr.vsP1Portrait = vsP1.AddComponent<Image>();
            mgr.vsP1Portrait.preserveAspect = true;

            GameObject vsP1Lbl = CreateUIObject("P1_Name", vsP1.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -30), new Vector2(300, 40));
            mgr.vsP1Name = vsP1Lbl.AddComponent<Text>();
            mgr.vsP1Name.font = font;
            mgr.vsP1Name.fontSize = 28;
            mgr.vsP1Name.fontStyle = FontStyle.Bold;
            mgr.vsP1Name.alignment = TextAnchor.MiddleCenter;
            mgr.vsP1Name.color = new Color(0.2f, 0.85f, 1f, 1f);

            GameObject vsP2 = CreateUIObject("P2_Preview", vsGo.transform, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), Vector2.zero, new Vector2(300, 380));
            mgr.vsP2Portrait = vsP2.AddComponent<Image>();
            mgr.vsP2Portrait.preserveAspect = true;

            GameObject vsP2Lbl = CreateUIObject("P2_Name", vsP2.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -30), new Vector2(300, 40));
            mgr.vsP2Name = vsP2Lbl.AddComponent<Text>();
            mgr.vsP2Name.font = font;
            mgr.vsP2Name.fontSize = 28;
            mgr.vsP2Name.fontStyle = FontStyle.Bold;
            mgr.vsP2Name.alignment = TextAnchor.MiddleCenter;
            mgr.vsP2Name.color = new Color(1f, 0.35f, 0.35f, 1f);

            mgr.vsOverlay = vsGo;
            vsGo.SetActive(false);

            // 3. Single EventSystem with InputSystemUIInputModule
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, SCENE_CHAR_SELECT);
            Debug.Log($"<color=#00e676>[DemonIdolSceneBuilder] Built CharacterSelect successfully at {SCENE_CHAR_SELECT}</color>");
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
    }
}
#endif
