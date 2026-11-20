#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.Audio;
using DemonIdol.VFX;
using DemonIdol.Input;
using DemonIdol.Launch;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Builder for the clean, isolated, production-grade Arcade Arena scene.
    /// Completely bypasses legacy scene clutter and builds a fresh, fully functional 1v1 battle arena:
    /// - P1: C12 Fina (fully controllable via WASD/Arrows + J/K/L/Space/U + Touch)
    /// - P2: C07 StellarWitch (CPU Sparring / AI controlled)
    /// - 3D Stage Backdrop Diorama (Ink Fortress Cathedral / Reactor Core) with strict 2.5D plane (Z=0)
    /// - Ground Combat Platform: surface at Y=0.0m, bounds ±14.0m, wall collision
    /// - SF6 40% height dynamic physical camera tracking both fighters
    /// - Real-time Battle HUD (HP bars, lag fills, timer, super meter gauges, non-blocking touch controls)
    /// - CombatTickEngine (60Hz), HomegrownCombatDirector, and MatchRoundReferee
    /// </summary>
    public static class DemonIdolArcadeArenaBuilder
    {
        public const string ARENA_SCENE_PATH = "Assets/Scenes/ArcadeArena.unity";
        public static string ActiveStagePrefabPath = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab";
        public static Color ActiveKeyLightColor = new Color(1.0f, 0.96f, 0.90f, 1f);
        public static float ActiveLightIntensity = 1.25f;

        private const string MDI_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";
        private const string C12_PREFAB_PATH = "Assets/Prefabs/Characters/C12_Fina.prefab";
        private const string C07_PREFAB_PATH = "Assets/Prefabs/Characters/C07_StellarWitch.prefab";
        private const string STAGE_PREFAB_PATH = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab";
        private const string URP_VOLUME_PATH = "Assets/Settings/URP_IndoorTemplateVolumeProfile.asset";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Arena/Build Clean Arcade Arena (C12 Underwear vs C07 StellarWitch)", priority = 0)]
        public static void BuildArcadeArenaMenu()
        {
            BuildArcadeArenaScene();
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildIfSceneMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ARENA_SCENE_PATH) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Debug.Log("<color=#00e676>[ArcadeArenaBuilder] ArcadeArena.unity is missing. Auto-generating clean arena scene now...</color>");
                    BuildArcadeArenaScene();
                }
            };
        }

        public static void BuildArcadeArenaScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ArcadeArenaBuilder] Please exit Play Mode before building the arena scene.");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += BuildArcadeArenaScene;
                return;
            }

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: BUILDING CLEAN ARCADE ARENA (C12 VS C07)       </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Create a brand-new pristine scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Stage Backdrop & Combat Platform
            GameObject stageRoot = SetupStageEnvironment();

            // 3. Directional Key Light, Rim Light, URP Volume & Reflection Probe
            SetupLightingAndPostProcessing();

            // 4. Spawn Player 1: C12 Fina (Player Controlled)
            FighterController p1 = SetupPlayer1Fighter();

            // 5. Spawn Player 2: C07 StellarWitch (CPU Controlled)
            FighterController p2 = SetupPlayer2CpuFighter();

            // 6. Cross-bind opponents
            p1.opponent = p2;
            p2.opponent = p1;

            // 7. SF6 Dynamic 2D Camera
            Camera mainCam = SetupSF6Camera(p1, p2);

            // 8. EventSystem with InputSystemUIInputModule
            SetupEventSystem();

            // 9. Match Managers & Directors
            SetupMatchDirectors(p1, p2);

            // 10. Sleek Battle HUD (Non-blocking raycasts)
            SetupBattleHUD(p1, p2);

            // 11. Save Scene
            string dir = Path.GetDirectoryName(ARENA_SCENE_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            bool saved = EditorSceneManager.SaveScene(scene, ARENA_SCENE_PATH);
            RegisterSceneInBuildSettings(ARENA_SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[ArcadeArenaBuilder] SUCCESS!</b> Clean Arcade Arena built at '{ARENA_SCENE_PATH}'!</color>");
            Debug.Log("<color=#00e676>P1: C12 Fina (Active Player) | P2: C07 StellarWitch (CPU AI)</color>");
            Debug.Log("<color=#00e676>Controls: WASD/Arrows to move, J=Light, K=Heavy, L=Special, Space=Throw, U=Super.</color>");
            Debug.Log("<color=#00e676>=============================================================</color>");
        }

        #region Environment & Stage Setup

        private static GameObject SetupStageEnvironment()
        {
            GameObject arenaRoot = new GameObject("Arcade_Stage_Root");
            arenaRoot.transform.position = new Vector3(-8.85f, -109.2f, 0f);

            // 1. Instantiate Backdrop Diorama
            string prefabPath = !string.IsNullOrEmpty(ActiveStagePrefabPath) && File.Exists(ActiveStagePrefabPath)
                ? ActiveStagePrefabPath
                : STAGE_PREFAB_PATH;
            GameObject stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (stagePrefab == null)
            {
                stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stages/ST01_LowPolyDesert.prefab")
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stages/ST01_ReactorCoreSilo.prefab");
            }

            if (stagePrefab != null)
            {
                GameObject stageInstance = (GameObject)PrefabUtility.InstantiatePrefab(stagePrefab, arenaRoot.transform);
                stageInstance.name = "Backdrop_Diorama";
                stageInstance.transform.localPosition = Vector3.zero;
                stageInstance.transform.localRotation = Quaternion.identity;
            }

            // 2. Ground Combat Platform (surface precisely at Y=0.0m)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_CombatPlatform";
            ground.transform.SetParent(arenaRoot.transform, false);
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(32f, 0.5f, 8f);

            var groundCol = ground.GetComponent<BoxCollider>();
            if (groundCol == null) groundCol = ground.AddComponent<BoxCollider>();
            groundCol.center = Vector3.zero;
            groundCol.size = Vector3.one;

            var groundMr = ground.GetComponent<MeshRenderer>();
            if (groundMr != null)
            {
                Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Ground.mat")
                    ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Metal_Black.mat")
                    ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Floor.mat");
                if (floorMat != null) groundMr.sharedMaterial = floorMat;
            }

            // 3. Left & Right Arena Wall Boundaries (±14.0m)
            GameObject leftWall = new GameObject("Boundary_LeftWall");
            leftWall.transform.SetParent(arenaRoot.transform, false);
            leftWall.transform.position = new Vector3(-14.0f, 2.5f, 0f);
            BoxCollider lCol = leftWall.AddComponent<BoxCollider>();
            lCol.size = new Vector3(1.0f, 8.0f, 8.0f);

            GameObject rightWall = new GameObject("Boundary_RightWall");
            rightWall.transform.SetParent(arenaRoot.transform, false);
            rightWall.transform.position = new Vector3(14.0f, 2.5f, 0f);
            BoxCollider rCol = rightWall.AddComponent<BoxCollider>();
            rCol.size = new Vector3(1.0f, 8.0f, 8.0f);

            PushbackSolver.SetArenaBounds(14.0f, 13.2f);
            return arenaRoot;
        }

        #endregion

        #region Lighting & Post-Processing

        private static void SetupLightingAndPostProcessing()
        {
            GameObject lightingRoot = new GameObject("Lighting_And_Atmosphere");

            // Key Directional Sun Light (Warm anime tone)
            GameObject keyLightGo = new GameObject("Directional_KeyLight");
            keyLightGo.transform.SetParent(lightingRoot.transform, false);
            keyLightGo.transform.rotation = Quaternion.Euler(32f, -32f, 0f);
            Light keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = ActiveKeyLightColor;
            keyLight.intensity = ActiveLightIntensity;
            keyLight.shadows = LightShadows.Soft;

            // Soft Rim / Back Light
            GameObject rimLightGo = new GameObject("Directional_RimLight");
            rimLightGo.transform.SetParent(lightingRoot.transform, false);
            rimLightGo.transform.rotation = Quaternion.Euler(-18f, 150f, 0f);
            Light rimLight = rimLightGo.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.75f, 0.88f, 1.0f, 1f);
            rimLight.intensity = 0.45f;
            rimLight.shadows = LightShadows.None;

            // Global URP Volume Profile
            GameObject volGo = new GameObject("Global_URP_Volume");
            volGo.transform.SetParent(lightingRoot.transform, false);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(URP_VOLUME_PATH)
                ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/ValorantVolumeProfile.asset");
            if (profile != null) vol.profile = profile;

            // Realtime Reflection Probe
            GameObject probeGo = new GameObject("Arena_ReflectionProbe");
            probeGo.transform.SetParent(lightingRoot.transform, false);
            probeGo.transform.position = new Vector3(0f, 2.5f, 0f);
            ReflectionProbe probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.size = new Vector3(36f, 12f, 16f);
        }

        #endregion

        #region Fighters Setup (C12 P1 & C07 P2)

        private static FighterController SetupPlayer1Fighter()
        {
            GameObject go = new GameObject("Fighter_P1_Fina");
            go.transform.position = new Vector3(-2.5f, 0f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            go.tag = "Player";

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.height = 1.9f;
            cc.radius = 0.35f;

            FighterController fc = go.AddComponent<FighterController>();
            fc.fighterName = "01 Fina";
            fc.isPlayerControlled = true;

            // Attach New Input System-enabled Fighter Input
            go.AddComponent<MobileFighterInput>();

            // Hurtbox
            GameObject hbGo = new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(go.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
            BoxCollider hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.8f, 1.8f, 0.8f);
            hbCol.isTrigger = true;
            hbGo.AddComponent<Hurtbox>();

            // Hitbox
            GameObject hitGo = new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(go.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f);
            BoxCollider hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            // Instantiate Visual Model C12 (Fina)
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(C12_PREFAB_PATH)
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/C12_Fina.prefab");

            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, go.transform);
                model.name = "Model_C12";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                // Completely unpack prefab instance so we can cleanly sanitize components
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // Strip duplicate logic/physics components from child model to prevent collision clash
                // Destroy FighterController FIRST so RequireComponent constraint on CharacterController is released
                foreach (var childFc in model.GetComponentsInChildren<FighterController>(true)) UnityEngine.Object.DestroyImmediate(childFc, true);
                foreach (var childCc in model.GetComponentsInChildren<CharacterController>(true)) UnityEngine.Object.DestroyImmediate(childCc, true);
                foreach (var childInput in model.GetComponentsInChildren<MobileFighterInput>(true)) UnityEngine.Object.DestroyImmediate(childInput, true);
                foreach (var childHb in model.GetComponentsInChildren<Hitbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var childHb in model.GetComponentsInChildren<Hurtbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var pb in model.GetComponentsInChildren<FighterPropBinder>(true)) UnityEngine.Object.DestroyImmediate(pb, true);

                // Strip any attached weapon props, wands, staves
                var allT = model.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t != null && t != model.transform && (
                        t.name.StartsWith("Prop_") || 
                        t.name.Contains("Staff") || 
                        t.name.Contains("Blaster") || 
                        t.name.Contains("Katana") || 
                        t.name.Contains("Sword") ||
                        t.name.Contains("MagicWand") ||
                        t.name.Contains("Wand") ||
                        t.name.Contains("Orb") ||
                        t.name.Contains("Gun")))
                    {
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                    }
                }

                // Ensure all renderers are enabled and on the fighter layer
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.layer = go.layer;
                    r.enabled = true;
                }

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/finna.controller")
                        ?? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/AnimeGirls/BaseCharacter/Demo files/AC.controller")
                        ?? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/smoky_fox/FinaAnimeGirl/Model/Fina.fbx");
                    foreach (var a in allAssets)
                    {
                        if (a is Avatar av) { anim.avatar = av; break; }
                    }
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }

                if (model.GetComponent<AnimationEventReceiver>() == null)
                    model.AddComponent<AnimationEventReceiver>();
            }

            // Load and apply CharacterDefinition & MoveSet
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C12_Fina.asset");
            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }
            else
            {
                fc.ApplyMoveSet(MoveSet.CreateSamplePreset("C12"));
            }

            return fc;
        }

        private static FighterController SetupPlayer2CpuFighter()
        {
            GameObject go = new GameObject("Fighter_P2_StellarWitch");
            go.transform.position = new Vector3(2.5f, 0f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.height = 1.9f;
            cc.radius = 0.35f;

            FighterController fc = go.AddComponent<FighterController>();
            fc.fighterName = "02 StellarWitch";
            fc.isPlayerControlled = false;

            // Attach CPU AI Controller
            var ai = go.AddComponent<CombatAIController>();
            ai.personality = CpuPersonality.Zoner;
            ai.difficultyOverride = 1;

            // Hurtbox
            GameObject hbGo = new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(go.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
            BoxCollider hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.8f, 1.8f, 0.8f);
            hbCol.isTrigger = true;
            hbGo.AddComponent<Hurtbox>();

            // Hitbox
            GameObject hitGo = new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(go.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f);
            BoxCollider hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            // Instantiate Visual Model C07 (StellarWitch)
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(C07_PREFAB_PATH);
            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, go.transform);
                model.name = "Model_C07";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                // Completely unpack prefab instance so we can cleanly sanitize components
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // Strip duplicate logic/physics components from child model to prevent collision clash
                // Destroy FighterController FIRST so RequireComponent constraint on CharacterController is released
                foreach (var childFc in model.GetComponentsInChildren<FighterController>(true)) UnityEngine.Object.DestroyImmediate(childFc, true);
                foreach (var childCc in model.GetComponentsInChildren<CharacterController>(true)) UnityEngine.Object.DestroyImmediate(childCc, true);
                foreach (var childInput in model.GetComponentsInChildren<MobileFighterInput>(true)) UnityEngine.Object.DestroyImmediate(childInput, true);
                foreach (var childHb in model.GetComponentsInChildren<Hitbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var childHb in model.GetComponentsInChildren<Hurtbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var pb in model.GetComponentsInChildren<FighterPropBinder>(true)) UnityEngine.Object.DestroyImmediate(pb, true);

                // Strip any attached weapon props, wands, staves
                var allT = model.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t != null && t != model.transform && (
                        t.name.StartsWith("Prop_") || 
                        t.name.Contains("Staff") || 
                        t.name.Contains("Blaster") || 
                        t.name.Contains("Katana") || 
                        t.name.Contains("Sword") ||
                        t.name.Contains("MagicWand") ||
                        t.name.Contains("Wand") ||
                        t.name.Contains("Orb") ||
                        t.name.Contains("Gun")))
                    {
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                    }
                }

                // Ensure all renderers are enabled and on the fighter layer
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.layer = go.layer;
                    r.enabled = true;
                }

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/StellarWitch3D/Meshes/StellarWitch.fbx");
                    foreach (var a in allAssets)
                    {
                        if (a is Avatar av) { anim.avatar = av; break; }
                    }
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }

                if (model.GetComponent<AnimationEventReceiver>() == null)
                    model.AddComponent<AnimationEventReceiver>();
            }

            // Load and apply CharacterDefinition & MoveSet
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C07_StellarWitch.asset");
            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }
            else
            {
                fc.ApplyMoveSet(MoveSet.CreateSamplePreset("C07"));
            }

            return fc;
        }

        #endregion

        #region SF6 Camera Setup

        private static Camera SetupSF6Camera(FighterController p1, FighterController p2)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 1.4f, -4.8f);
            camGo.transform.rotation = Quaternion.Euler(2.8f, 0f, 0f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 120f;

            FighterCamera2D cam2D = camGo.AddComponent<FighterCamera2D>();
            cam2D.SetTargets(p1.transform, p2.transform);

            // Audio Listener
            camGo.AddComponent<AudioListener>();

            return cam;
        }

        #endregion

        #region EventSystem Setup

        private static void SetupEventSystem()
        {
            GameObject esGo = new GameObject("EventSystem");
            EventSystem es = esGo.AddComponent<EventSystem>();
            UiEventSystemInputBootstrap.Reconcile(es);
        }

        #endregion

        #region Match Directors

        private static void SetupMatchDirectors(FighterController p1, FighterController p2)
        {
            GameObject mgrGo = new GameObject("Combat_Managers");

            // 1. Fixed 60Hz Tick Engine
            mgrGo.AddComponent<CombatTickEngine>();

            // 2. Homegrown Director
            HomegrownCombatDirector director = mgrGo.AddComponent<HomegrownCombatDirector>();
            SerializedObject soDir = new SerializedObject(director);
            var p1Prop = soDir.FindProperty("_fighter1");
            var p2Prop = soDir.FindProperty("_fighter2");
            if (p1Prop != null) p1Prop.objectReferenceValue = p1;
            if (p2Prop != null) p2Prop.objectReferenceValue = p2;
            soDir.ApplyModifiedProperties();
            director.SetMatchContext("C12", "C07", "ST04", p2IsCpu: true, isTraining: false);

            // 3. Match Round Referee (auto begins round 1)
            MatchRoundReferee referee = mgrGo.AddComponent<MatchRoundReferee>();
            SerializedObject soRef = new SerializedObject(referee);
            var rf1Prop = soRef.FindProperty("_fighter1");
            var rf2Prop = soRef.FindProperty("_fighter2");
            if (rf1Prop != null) rf1Prop.objectReferenceValue = p1;
            if (rf2Prop != null) rf2Prop.objectReferenceValue = p2;
            soRef.ApplyModifiedProperties();
            referee.BindFighters(p1, p2);

            // 4. Combat Audio Director
            mgrGo.AddComponent<CombatAudioDirector>();
        }

        #endregion

        #region Battle HUD Setup

        private static void SetupBattleHUD(FighterController p1, FighterController p2)
        {
            GameObject canvasGo = new GameObject("Battle_HUD_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Main Battle HUD Panel (RaycastTarget = false to never block game inputs)
            GameObject hudPanel = new GameObject("Panel_BattleHUD");
            hudPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRt = hudPanel.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.sizeDelta = Vector2.zero;
            hudPanel.AddComponent<CanvasGroup>();
            BattleHUDController hudCtrl = hudPanel.AddComponent<BattleHUDController>();

            // --- Top Bar: P1 Health, Timer, P2 Health ---
            // P1 Bar
            GameObject p1Bar = CreateBar(hudPanel, "P1_HealthBar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(380, -50), new Vector2(650, 42));
            Image p1Lag = CreateBarFill(p1Bar, "LagFill", new Color(1f, 0.85f, 0.2f, 0.8f), false);
            Image p1Hp = CreateBarFill(p1Bar, "HpFill", new Color(0.1f, 0.9f, 0.35f, 1.0f), false);
            Text p1Name = CreateText(p1Bar, "C12: FINA", new Vector2(-220, 32), 22, Color.white, FontStyle.Bold);

            // P2 Bar
            GameObject p2Bar = CreateBar(hudPanel, "P2_HealthBar", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-380, -50), new Vector2(650, 42));
            Image p2Lag = CreateBarFill(p2Bar, "LagFill", new Color(1f, 0.85f, 0.2f, 0.8f), true);
            Image p2Hp = CreateBarFill(p2Bar, "HpFill", new Color(0.1f, 0.9f, 0.35f, 1.0f), true);
            Text p2Name = CreateText(p2Bar, "C07: STELLAR WITCH", new Vector2(220, 32), 22, Color.white, FontStyle.Bold);

            // Timer (Center)
            GameObject timerGo = new GameObject("Round_Timer");
            timerGo.transform.SetParent(hudPanel.transform, false);
            RectTransform timerRt = timerGo.AddComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.5f, 1f);
            timerRt.anchorMax = new Vector2(0.5f, 1f);
            timerRt.anchoredPosition = new Vector2(0, -50);
            timerRt.sizeDelta = new Vector2(120, 60);
            Text timerText = timerGo.AddComponent<Text>();
            timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timerText.text = "99";
            timerText.fontSize = 44;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = new Color(1f, 0.88f, 0.2f, 1f);
            timerText.raycastTarget = false;

            // Wire HUD Controller via SerializedObject
            SerializedObject soHud = new SerializedObject(hudCtrl);
            var p1NameProp = soHud.FindProperty("_p1NameText");
            var p1HpProp = soHud.FindProperty("_p1HealthFill");
            var p1LagProp = soHud.FindProperty("_p1HealthLagFill");
            var p2NameProp = soHud.FindProperty("_p2NameText");
            var p2HpProp = soHud.FindProperty("_p2HealthFill");
            var p2LagProp = soHud.FindProperty("_p2HealthLagFill");
            var timerProp = soHud.FindProperty("_timerText");

            if (p1NameProp != null) p1NameProp.objectReferenceValue = p1Name;
            if (p1HpProp != null) p1HpProp.objectReferenceValue = p1Hp;
            if (p1LagProp != null) p1LagProp.objectReferenceValue = p1Lag;
            if (p2NameProp != null) p2NameProp.objectReferenceValue = p2Name;
            if (p2HpProp != null) p2HpProp.objectReferenceValue = p2Hp;
            if (p2LagProp != null) p2LagProp.objectReferenceValue = p2Lag;
            if (timerProp != null) timerProp.objectReferenceValue = timerText;
            soHud.ApplyModifiedProperties();

            hudCtrl.BindFighters(p1, p2);
            hudCtrl.SetFighterNames("UNDERWEAR1", "STELLAR WITCH");
        }

        private static GameObject CreateBar(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent.transform, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image bg = bar.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);
            bg.raycastTarget = false; // Never block clicks
            return bar;
        }

        private static Image CreateBarFill(GameObject parent, string name, Color color, bool rightToLeft)
        {
            GameObject fill = new GameObject(name);
            fill.transform.SetParent(parent.transform, false);
            RectTransform rt = fill.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Image img = fill.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = rightToLeft ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1.0f;
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateText(GameObject parent, string text, Vector2 pos, int size, Color color, FontStyle style)
        {
            GameObject textGo = new GameObject("Label_" + text);
            textGo.transform.SetParent(parent.transform, false);
            RectTransform rt = textGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 36);
            Text t = textGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;
            return t;
        }

        #endregion

        #region Build Settings Registration

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            }

            // Remove deleted SampleScene if present
            scenes.RemoveAll(s => s.path == "Assets/Scenes/SampleScene.unity");

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion
    }
}
#endif
