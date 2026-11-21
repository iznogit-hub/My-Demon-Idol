#if MDI_ARCHIVE
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
        private const string C01_PREFAB_PATH = "Assets/Prefabs/Characters/C01_Akio.prefab";
        private const string C02_PREFAB_PATH = "Assets/Prefabs/Characters/C02_Aika.prefab";
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
            arenaRoot.transform.position = Vector3.zero;
            arenaRoot.transform.rotation = Quaternion.identity;

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

            // 2. Ground Combat Platform (surface precisely at Y=0.0m) - purely invisible collision box, no grey line
            GameObject ground = new GameObject("Ground_CombatPlatform");
            ground.transform.SetParent(arenaRoot.transform, false);
            ground.transform.position = new Vector3(0f, -0.25f, 0f);

            var groundCol = ground.AddComponent<BoxCollider>();
            groundCol.center = Vector3.zero;
            groundCol.size = new Vector3(32f, 0.5f, 8f);

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
            GameObject go = new GameObject("Fighter_P1_Akio");
            go.transform.position = new Vector3(-1.70f, 0f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            go.tag = "Player";

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.75f, 0);
            cc.height = 1.5f;
            cc.radius = 0.35f;

            FighterController fc = go.AddComponent<FighterController>();
            fc.fighterName = "Akio";
            fc.isPlayerControlled = true;

            // Attach New Input System-enabled Fighter Input
            go.AddComponent<MobileFighterInput>();
            go.AddComponent<FighterBlobShadow>();

            // Hurtbox
            GameObject hbGo = new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(go.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 0.75f, 0);
            BoxCollider hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.7f, 1.4f, 0.7f);
            hbCol.isTrigger = true;
            hbGo.AddComponent<Hurtbox>();

            // Hitbox
            GameObject hitGo = new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(go.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 0.9f, 0.6f);
            BoxCollider hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.0f, 1.0f, 1.2f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            // Load and apply CharacterDefinition & Visual Model C01 (Akio)
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset");
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(C01_PREFAB_PATH)
                ?? (charDef != null ? charDef.modelPrefab : null);

            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, go.transform);
                model.name = "Model_C01";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                foreach (var childFc in model.GetComponentsInChildren<FighterController>(true)) UnityEngine.Object.DestroyImmediate(childFc, true);
                foreach (var childCc in model.GetComponentsInChildren<CharacterController>(true)) UnityEngine.Object.DestroyImmediate(childCc, true);
                foreach (var childInput in model.GetComponentsInChildren<MobileFighterInput>(true)) UnityEngine.Object.DestroyImmediate(childInput, true);
                foreach (var childHb in model.GetComponentsInChildren<Hitbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var childHb in model.GetComponentsInChildren<Hurtbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var pb in model.GetComponentsInChildren<FighterPropBinder>(true)) UnityEngine.Object.DestroyImmediate(pb, true);

                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.layer = go.layer;
                    r.enabled = true;
                }

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (charDef != null && charDef.animatorController != null) anim.runtimeAnimatorController = charDef.animatorController;
                    if (charDef != null && charDef.avatar != null) anim.avatar = charDef.avatar;
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }

                if (model.GetComponent<AnimationEventReceiver>() == null)
                    model.AddComponent<AnimationEventReceiver>();
            }

            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }
            else
            {
                fc.ApplyMoveSet(MoveSet.CreateSamplePreset("C01"));
            }

            return fc;
        }

        private static FighterController SetupPlayer2CpuFighter()
        {
            GameObject go = new GameObject("Fighter_P2_Aika");
            go.transform.position = new Vector3(1.70f, 0f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.75f, 0);
            cc.height = 1.5f;
            cc.radius = 0.35f;

            FighterController fc = go.AddComponent<FighterController>();
            fc.fighterName = "Aika";
            fc.isPlayerControlled = false;

            // Attach CPU AI Controller
            var ai = go.AddComponent<CombatAIController>();
            ai.personality = CpuPersonality.Rushdown;
            ai.difficultyOverride = 1;
            go.AddComponent<FighterBlobShadow>();

            // Hurtbox
            GameObject hbGo = new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(go.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 0.75f, 0);
            BoxCollider hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.7f, 1.4f, 0.7f);
            hbCol.isTrigger = true;
            hbGo.AddComponent<Hurtbox>();

            // Hitbox
            GameObject hitGo = new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(go.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 0.9f, 0.6f);
            BoxCollider hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.0f, 1.0f, 1.2f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            // Load and apply CharacterDefinition & Visual Model C02 (Aika)
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset");
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(C02_PREFAB_PATH)
                ?? (charDef != null ? charDef.modelPrefab : null);

            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, go.transform);
                model.name = "Model_C02";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                foreach (var childFc in model.GetComponentsInChildren<FighterController>(true)) UnityEngine.Object.DestroyImmediate(childFc, true);
                foreach (var childCc in model.GetComponentsInChildren<CharacterController>(true)) UnityEngine.Object.DestroyImmediate(childCc, true);
                foreach (var childInput in model.GetComponentsInChildren<MobileFighterInput>(true)) UnityEngine.Object.DestroyImmediate(childInput, true);
                foreach (var childHb in model.GetComponentsInChildren<Hitbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var childHb in model.GetComponentsInChildren<Hurtbox>(true)) UnityEngine.Object.DestroyImmediate(childHb.gameObject, true);
                foreach (var pb in model.GetComponentsInChildren<FighterPropBinder>(true)) UnityEngine.Object.DestroyImmediate(pb, true);

                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.layer = go.layer;
                    r.enabled = true;
                }

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (charDef != null && charDef.animatorController != null) anim.runtimeAnimatorController = charDef.animatorController;
                    if (charDef != null && charDef.avatar != null) anim.avatar = charDef.avatar;
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }

                if (model.GetComponent<AnimationEventReceiver>() == null)
                    model.AddComponent<AnimationEventReceiver>();
            }

            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }
            else
            {
                fc.ApplyMoveSet(MoveSet.CreateSamplePreset("C02"));
            }

            return fc;
        }

        #endregion

        #region SF6 Camera Setup

        private static Camera SetupSF6Camera(FighterController p1, FighterController p2)
        {
            return SetupAll7StageCameras(p1, p2);
        }

        public static Camera SetupAll7StageCameras(FighterController p1 = null, FighterController p2 = null)
        {
            GameObject camerasRoot = GameObject.Find("Stage_Cameras");
            if (camerasRoot == null)
            {
                camerasRoot = new GameObject("Stage_Cameras");
            }

            var camManager = camerasRoot.GetComponent<DemonIdol.CameraControl.StageCameraManager>();
            if (camManager == null)
            {
                camManager = camerasRoot.AddComponent<DemonIdol.CameraControl.StageCameraManager>();
            }

            camManager.EnsureCameraRosterConfigured();

            GameObject mainCamGo = GameObject.Find("Main Camera");
            if (mainCamGo == null)
            {
                mainCamGo = new GameObject("Main Camera");
                mainCamGo.tag = "MainCamera";
                mainCamGo.transform.position = new Vector3(-0.05f, 1.10f, -4.15f);
                mainCamGo.transform.rotation = Quaternion.Euler(2.8f, 0f, 0f);

                Camera mCam = mainCamGo.AddComponent<Camera>();
                mCam.usePhysicalProperties = true;
                mCam.sensorSize = new Vector2(36f, 20.25f);
                mCam.focalLength = 32.0f;
                mCam.fieldOfView = 35.0f;
                mCam.nearClipPlane = 0.2f;
                mCam.farClipPlane = 120f;
                mCam.clearFlags = CameraClearFlags.SolidColor;
                mCam.backgroundColor = new Color(0.126157f, 0.203373f, 0.254717f, 0f);

                UniversalAdditionalCameraData uacd = mainCamGo.AddComponent<UniversalAdditionalCameraData>();
                uacd.renderShadows = true;

                FighterCamera2D cam2D = mainCamGo.AddComponent<FighterCamera2D>();
                if (p1 != null && p2 != null) cam2D.SetTargets(p1.transform, p2.transform);
                cam2D.focalLength = 32.0f;
                cam2D.targetCharacterScreenFraction = 0.59f;
                cam2D.offset = new Vector3(-0.05f, 1.10f, -4.15f);
                cam2D.minDistanceZ = -3.8f;
                cam2D.maxDistanceZ = -7.5f;

                var mainAl = mainCamGo.GetComponent<AudioListener>();
                if (mainAl == null) mainAl = mainCamGo.AddComponent<AudioListener>();
                mainAl.enabled = true;
            }
            mainCamGo.transform.SetParent(camerasRoot.transform, true);

            // Configure each of the 7 Stage Cameras across different areas of College Raimon
            var entries = camManager.StageCameras;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.stageId == "ST07")
                {
                    entry.camera = mainCamGo.GetComponent<Camera>();
                    entry.fighterCamera = mainCamGo.GetComponent<FighterCamera2D>();
                    continue;
                }

                string camName = $"Camera_{entry.stageId}_{entry.stageName.Split(' ')[0]}";
                Transform child = camerasRoot.transform.Find(camName);
                GameObject camObj = child != null ? child.gameObject : new GameObject(camName);
                camObj.transform.SetParent(camerasRoot.transform, false);

                camObj.transform.position = entry.basePosition;
                camObj.transform.rotation = Quaternion.Euler(entry.baseEulerAngles);

                Camera cam = camObj.GetComponent<Camera>();
                if (cam == null) cam = camObj.AddComponent<Camera>();
                cam.usePhysicalProperties = true;
                cam.sensorSize = entry.sensorSize;
                cam.focalLength = entry.focalLength;
                cam.fieldOfView = entry.fieldOfView;
                cam.lensShift = entry.lensShift;
                cam.nearClipPlane = 0.2f;
                cam.farClipPlane = 120f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.126157f, 0.203373f, 0.254717f, 0f);

                UniversalAdditionalCameraData uacd = camObj.GetComponent<UniversalAdditionalCameraData>();
                if (uacd == null) uacd = camObj.AddComponent<UniversalAdditionalCameraData>();
                uacd.renderShadows = true;

                FighterCamera2D fCam = camObj.GetComponent<FighterCamera2D>();
                if (fCam == null) fCam = camObj.AddComponent<FighterCamera2D>();
                if (p1 != null && p2 != null) fCam.SetTargets(p1.transform, p2.transform);
                fCam.focalLength = entry.focalLength;
                fCam.lensShift = entry.lensShift;
                fCam.targetCharacterScreenFraction = entry.characterScreenFraction;
                fCam.offset = entry.basePosition;
                fCam.pitchAngle = entry.baseEulerAngles.x;

                AudioListener al = camObj.GetComponent<AudioListener>();
                if (al == null) al = camObj.AddComponent<AudioListener>();
                al.enabled = false;

                entry.camera = cam;
                entry.fighterCamera = fCam;

                // Keep inactive by default until switched
                camObj.SetActive(false);
                camObj.tag = "Untagged";
            }

            EditorUtility.SetDirty(camManager);
            return mainCamGo.GetComponent<Camera>();
        }

        public static void EnsureAll7StageCamerasInScene()
        {
            SetupAll7StageCameras();
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
            director.SetMatchContext("C01", "C02", "ST07", p2IsCpu: true, isTraining: false);

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
            // P1 Portrait Box beside health bar
            GameObject p1PortraitBox = new GameObject("P1_PortraitBox");
            p1PortraitBox.transform.SetParent(hudPanel.transform, false);
            RectTransform p1PortRt = p1PortraitBox.AddComponent<RectTransform>();
            p1PortRt.anchorMin = new Vector2(0f, 1f);
            p1PortRt.anchorMax = new Vector2(0f, 1f);
            p1PortRt.anchoredPosition = new Vector2(50f, -50f);
            p1PortRt.sizeDelta = new Vector2(72f, 72f);
            Image p1PortBoxImg = p1PortraitBox.AddComponent<Image>();
            p1PortBoxImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            p1PortBoxImg.raycastTarget = false;

            GameObject p1PortInner = new GameObject("P1_Portrait");
            p1PortInner.transform.SetParent(p1PortraitBox.transform, false);
            RectTransform p1PortInnerRt = p1PortInner.AddComponent<RectTransform>();
            p1PortInnerRt.anchorMin = Vector2.zero;
            p1PortInnerRt.anchorMax = Vector2.one;
            p1PortInnerRt.sizeDelta = new Vector2(-6f, -6f);
            Image p1Portrait = p1PortInner.AddComponent<Image>();
            var akioDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset");
            if (akioDef != null && akioDef.portraitSprite != null) p1Portrait.sprite = akioDef.portraitSprite;
            p1Portrait.color = Color.white;
            p1Portrait.preserveAspect = true;
            p1Portrait.raycastTarget = false;

            // P1 Health Bar (red chip-damage lag fill)
            GameObject p1Bar = CreateBar(hudPanel, "P1_HealthBar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(430, -50), new Vector2(640, 42));
            Image p1Lag = CreateBarFill(p1Bar, "LagFill", new Color(0.92f, 0.15f, 0.15f, 0.95f), false);
            Image p1Hp = CreateBarFill(p1Bar, "HpFill", new Color(0.1f, 0.9f, 0.35f, 1.0f), false);
            Text p1Name = CreateText(p1Bar, "C01: AKIO", new Vector2(-200, 32), 22, Color.white, FontStyle.Bold);

            // P2 Portrait Box beside health bar
            GameObject p2PortraitBox = new GameObject("P2_PortraitBox");
            p2PortraitBox.transform.SetParent(hudPanel.transform, false);
            RectTransform p2PortRt = p2PortraitBox.AddComponent<RectTransform>();
            p2PortRt.anchorMin = new Vector2(1f, 1f);
            p2PortRt.anchorMax = new Vector2(1f, 1f);
            p2PortRt.anchoredPosition = new Vector2(-50f, -50f);
            p2PortRt.sizeDelta = new Vector2(72f, 72f);
            Image p2PortBoxImg = p2PortraitBox.AddComponent<Image>();
            p2PortBoxImg.color = new Color(0.24f, 0.12f, 0.16f, 0.95f);
            p2PortBoxImg.raycastTarget = false;

            GameObject p2PortInner = new GameObject("P2_Portrait");
            p2PortInner.transform.SetParent(p2PortraitBox.transform, false);
            RectTransform p2PortInnerRt = p2PortInner.AddComponent<RectTransform>();
            p2PortInnerRt.anchorMin = Vector2.zero;
            p2PortInnerRt.anchorMax = Vector2.one;
            p2PortInnerRt.sizeDelta = new Vector2(-6f, -6f);
            Image p2Portrait = p2PortInner.AddComponent<Image>();
            var aikaDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset");
            if (aikaDef != null && aikaDef.portraitSprite != null) p2Portrait.sprite = aikaDef.portraitSprite;
            p2Portrait.color = Color.white;
            p2Portrait.preserveAspect = true;
            p2Portrait.raycastTarget = false;

            // P2 Health Bar (red chip-damage lag fill)
            GameObject p2Bar = CreateBar(hudPanel, "P2_HealthBar", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-430, -50), new Vector2(640, 42));
            Image p2Lag = CreateBarFill(p2Bar, "LagFill", new Color(0.92f, 0.15f, 0.15f, 0.95f), true);
            Image p2Hp = CreateBarFill(p2Bar, "HpFill", new Color(0.1f, 0.9f, 0.35f, 1.0f), true);
            Text p2Name = CreateText(p2Bar, "C02: AIKA", new Vector2(200, 32), 22, Color.white, FontStyle.Bold);

            // Framed Timer (Center) with dark background and metallic border frame
            GameObject timerFrameGo = new GameObject("Timer_Frame");
            timerFrameGo.transform.SetParent(hudPanel.transform, false);
            RectTransform frameRt = timerFrameGo.AddComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 1f);
            frameRt.anchorMax = new Vector2(0.5f, 1f);
            frameRt.anchoredPosition = new Vector2(0, -50);
            frameRt.sizeDelta = new Vector2(116, 68);
            Image frameBorder = timerFrameGo.AddComponent<Image>();
            frameBorder.color = new Color(0.85f, 0.72f, 0.25f, 0.85f);
            frameBorder.raycastTarget = false;

            GameObject timerInnerGo = new GameObject("Timer_Background");
            timerInnerGo.transform.SetParent(timerFrameGo.transform, false);
            RectTransform innerRt = timerInnerGo.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.sizeDelta = new Vector2(-6, -6);
            Image innerBg = timerInnerGo.AddComponent<Image>();
            innerBg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);
            innerBg.raycastTarget = false;

            GameObject timerGo = new GameObject("Round_Timer");
            timerGo.transform.SetParent(timerInnerGo.transform, false);
            RectTransform timerRt = timerGo.AddComponent<RectTransform>();
            timerRt.anchorMin = Vector2.zero;
            timerRt.anchorMax = Vector2.one;
            timerRt.sizeDelta = Vector2.zero;
            Text timerText = timerGo.AddComponent<Text>();
            timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timerText.text = "99";
            timerText.fontSize = 44;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = new Color(1f, 0.90f, 0.35f, 1f);
            timerText.raycastTarget = false;

            // Round Win Markers (2 per side) beside timer
            Image[] p1WinPips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pip = new GameObject($"P1_WinPip_{i}");
                pip.transform.SetParent(hudPanel.transform, false);
                RectTransform pipRt = pip.AddComponent<RectTransform>();
                pipRt.anchorMin = new Vector2(0.5f, 1f);
                pipRt.anchorMax = new Vector2(0.5f, 1f);
                pipRt.anchoredPosition = new Vector2(-75f - (i * 24f), -50f);
                pipRt.sizeDelta = new Vector2(18f, 18f);
                Image pipImg = pip.AddComponent<Image>();
                pipImg.color = new Color(0.20f, 0.22f, 0.28f, 0.85f);
                pipImg.raycastTarget = false;
                p1WinPips[i] = pipImg;
            }

            Image[] p2WinPips = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject pip = new GameObject($"P2_WinPip_{i}");
                pip.transform.SetParent(hudPanel.transform, false);
                RectTransform pipRt = pip.AddComponent<RectTransform>();
                pipRt.anchorMin = new Vector2(0.5f, 1f);
                pipRt.anchorMax = new Vector2(0.5f, 1f);
                pipRt.anchoredPosition = new Vector2(75f + (i * 24f), -50f);
                pipRt.sizeDelta = new Vector2(18f, 18f);
                Image pipImg = pip.AddComponent<Image>();
                pipImg.color = new Color(0.20f, 0.22f, 0.28f, 0.85f);
                pipImg.raycastTarget = false;
                p2WinPips[i] = pipImg;
            }

            // Wire HUD Controller via SerializedObject
            SerializedObject soHud = new SerializedObject(hudCtrl);
            var p1NameProp = soHud.FindProperty("_p1NameText");
            var p1HpProp = soHud.FindProperty("_p1HealthFill");
            var p1LagProp = soHud.FindProperty("_p1HealthLagFill");
            var p1PortProp = soHud.FindProperty("_p1Portrait");
            var p2NameProp = soHud.FindProperty("_p2NameText");
            var p2HpProp = soHud.FindProperty("_p2HealthFill");
            var p2LagProp = soHud.FindProperty("_p2HealthLagFill");
            var p2PortProp = soHud.FindProperty("_p2Portrait");
            var timerProp = soHud.FindProperty("_timerText");
            var timerFrameProp = soHud.FindProperty("_timerFrame");

            if (p1NameProp != null) p1NameProp.objectReferenceValue = p1Name;
            if (p1HpProp != null) p1HpProp.objectReferenceValue = p1Hp;
            if (p1LagProp != null) p1LagProp.objectReferenceValue = p1Lag;
            if (p1PortProp != null) p1PortProp.objectReferenceValue = p1Portrait;
            if (p2NameProp != null) p2NameProp.objectReferenceValue = p2Name;
            if (p2HpProp != null) p2HpProp.objectReferenceValue = p2Hp;
            if (p2LagProp != null) p2LagProp.objectReferenceValue = p2Lag;
            if (p2PortProp != null) p2PortProp.objectReferenceValue = p2Portrait;
            if (timerProp != null) timerProp.objectReferenceValue = timerText;
            if (timerFrameProp != null) timerFrameProp.objectReferenceValue = frameBorder;

            var p1PipsProp = soHud.FindProperty("_p1WinPips");
            if (p1PipsProp != null)
            {
                p1PipsProp.arraySize = 2;
                p1PipsProp.GetArrayElementAtIndex(0).objectReferenceValue = p1WinPips[0];
                p1PipsProp.GetArrayElementAtIndex(1).objectReferenceValue = p1WinPips[1];
            }
            var p2PipsProp = soHud.FindProperty("_p2WinPips");
            if (p2PipsProp != null)
            {
                p2PipsProp.arraySize = 2;
                p2PipsProp.GetArrayElementAtIndex(0).objectReferenceValue = p2WinPips[0];
                p2PipsProp.GetArrayElementAtIndex(1).objectReferenceValue = p2WinPips[1];
            }

            soHud.ApplyModifiedProperties();

            hudCtrl.BindFighters(p1, p2);
            hudCtrl.SetFighterNames("AKIO", "AIKA");
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
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
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

#endif
