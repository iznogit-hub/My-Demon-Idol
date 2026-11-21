#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.Audio;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Director for the 6 Authoritative Demon Idol Battle Stages & SF6 Cinematic Physical Camera.
    /// Stage 1: Low Poly Desert (Sun-baked canyon floor, sandstone mesa spires, dunes, warm amber dust)
    /// Stage 2: RTS Sci-Fi Game Assets v1 (Industrial gantry base, cyber conduits, cargo units, cyan neon status lines)
    /// Stage 3: Space Alien Worlds (Extraterrestrial crystal spires, bioluminescent flora, obsidian floor, purple nebulae)
    /// Stage 4: Cartoon City (Stylized downtown street corner, storefront facades, asphalt platform, warm daylight)
    /// Stage 5: PixelDrift: Metro Subway (Gritty retro underground transit platform, subway tracks, rusted metal pillars)
    /// Stage 6: Modern Office Tower (High-rise executive penthouse / rooftop helipad overlooking dusk skyline)
    /// 
    /// Calibrates character framing to exactly 40% of screen height with Street Fighter 6 dynamic handling:
    /// - Physical Camera: Full Frame Super 35 (36mm x 20.25mm), 50mm portrait prime, heroic lens shift (0, 0.035)
    /// - Grounded eye-level (1.25m), low heroic pitch (2.8 deg), decoupled normal jumps
    /// - Asymmetric aggression lead tracking the advancing/attacking fighter
    /// - Dynamic zoom curve: 45% at close/throw range, 40% neutral, 35% wide corner spacing
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolStageDirector
    {
        private const string STAGES_PREFAB_DIR = "Assets/Prefabs/Stages";
        private const string STAGES_DATA_DIR = "Assets/CombatData/Stages";
        private const string SCENE_PATH = "Assets/Scenes/Home.unity";

        private static bool s_pendingRun = false;

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                string triggerFile = "Temp/run_stage_director.txt";
                if (File.Exists(triggerFile) || EditorPrefs.GetBool("MDI_PendingStageDirectorRun", false))
                {
                    if (File.Exists(triggerFile))
                    {
                        try { File.Delete(triggerFile); } catch { }
                    }
                    EditorPrefs.SetBool("MDI_PendingStageDirectorRun", false);
                    ExecuteFullStageAndCameraSetup();
                }
            };
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Stages/Build All 6 Stages and SF6 Camera", priority = 1)]
        public static void MenuBuildAll()
        {
            ExecuteFullStageAndCameraSetup();
        }

        public static void TriggerOnNextCompile()
        {
            EditorPrefs.SetBool("MDI_PendingStageDirectorRun", true);
        }

        public static void ExecuteFullStageAndCameraSetup()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: 6 BATTLE STAGES & SF6 PHYSICAL CAMERA DIRECTOR  </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            EnsureDirectories();

            // 0. Sanitize missing scripts on all Character prefabs
            SanitizeAllCharacterPrefabs();

            // 1. Build and Author all 6 Stage Prefabs
            BuildAll6StagePrefabs();

            // 2. Generate and Author all 6 StageDefinition ScriptableObjects
            GenerateAll6StageDefinitions();

            // 3. Rebuild Master UI Canvas (6-Card Stage Select Grid, 3-Button Battle HUD)
            DemonIdolUIBuilder.BuildMasterUI();

            // 4. Configure Home Scene with SF6 Physical Camera and Ground Combat Arena
            ConfigureHomeScene();

            // 5. Deploy Master Armory, Icons, Frames, Mugshots, URP Indoor & Replays
            DemonIdolArmoryBuilder.DeployAll();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676><b>[DemonIdol StageDirector] SUCCESS:</b> All 6 Stages & SF6 40% Physical Camera Deployed!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(STAGES_PREFAB_DIR)) AssetDatabase.CreateFolder("Assets/Prefabs", "Stages");
            if (!AssetDatabase.IsValidFolder("Assets/CombatData")) AssetDatabase.CreateFolder("Assets", "CombatData");
            if (!AssetDatabase.IsValidFolder(STAGES_DATA_DIR)) AssetDatabase.CreateFolder("Assets/CombatData", "Stages");
        }

        private static void SanitizeAllCharacterPrefabs()
        {
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" });
            foreach (string guid in prefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root != null)
                {
                    Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                    int removed = 0;
                    foreach (var t in allTransforms)
                    {
                        removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    }
                    if (removed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        Debug.Log($"<color=#81c784>[StageDirector] Cleaned {removed} missing scripts from {path}</color>");
                    }
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        #region Stage Prefab Construction

        private static void BuildAll6StagePrefabs()
        {
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Ground.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Metal_Black.mat");
            Material litMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Lit.mat");
            Material dungeonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/LowPolyDungeonsLite/Materials/LowPolyDungeonsLite_MAIN.mat");

            // Stage 1: Low Poly Desert
            BuildSingleStagePrefab(
                "ST01_LowPolyDesert",
                dungeonMat != null ? dungeonMat : concreteMat,
                new Color(1.0f, 0.72f, 0.35f, 1f),
                root => BuildDesertDiorama(root)
            );

            // Stage 2: RTS Sci-Fi Game Assets v1
            BuildSingleStagePrefab(
                "ST02_RTSSciFiHangar",
                metalMat != null ? metalMat : concreteMat,
                new Color(0.2f, 0.85f, 1.0f, 1.0f),
                root => BuildSciFiDiorama(root)
            );

            // Stage 3: Free Demo Of Low Poly Space Alien Worlds 3D Asset Pack
            BuildSingleStagePrefab(
                "ST03_SpaceAlienWorlds",
                litMat != null ? litMat : concreteMat,
                new Color(0.78f, 0.32f, 0.98f, 1.0f),
                root => BuildAlienWorldsDiorama(root)
            );

            // Stage 4: Cartoon City FREE - Low Poly City 3D Models Pack
            BuildSingleStagePrefab(
                "ST04_CartoonCity",
                concreteMat,
                new Color(1.0f, 0.95f, 0.75f, 1.0f),
                root => BuildCartoonCityDiorama(root)
            );

            // Stage 5: PixelDrift: Metro Subway for Boomer FPS VR
            BuildSingleStagePrefab(
                "ST05_MetroSubway",
                metalMat != null ? metalMat : concreteMat,
                new Color(0.95f, 0.55f, 0.25f, 1.0f),
                root => BuildMetroSubwayDiorama(root)
            );

            // Stage 6: Modern Office Tower
            BuildSingleStagePrefab(
                "ST06_ModernOfficeTower",
                litMat != null ? litMat : metalMat,
                new Color(0.35f, 0.65f, 1.0f, 1.0f),
                root => BuildOfficeTowerDiorama(root)
            );

            // Stage 7: College Raimon (Japanese School & Academy Courtyard)
            BuildSingleStagePrefab(
                "ST07_CollegeRaimon",
                concreteMat,
                new Color(1.0f, 0.85f, 0.9f, 1.0f),
                root => BuildCollegeRaimonDiorama(root)
            );
        }

        private static void BuildSingleStagePrefab(string stageName, Material groundMat, Color lightColor, Action<GameObject> populateDiorama)
        {
            string targetPath = $"{STAGES_PREFAB_DIR}/{stageName}.prefab";

            GameObject root = new GameObject(stageName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            // Combat Floor Platform (32m width, 8m depth, height -0.25m -> surface at Y=0.0m)
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Ground_CombatPlatform";
            platform.transform.SetParent(root.transform, false);
            platform.transform.position = new Vector3(0f, -0.25f, 0f);
            platform.transform.localScale = new Vector3(32f, 0.5f, 8f);

            var groundCol = platform.GetComponent<BoxCollider>();
            if (groundCol == null) groundCol = platform.AddComponent<BoxCollider>();
            groundCol.center = Vector3.zero;
            groundCol.size = Vector3.one;

            if (groundMat != null)
            {
                var mr = platform.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = groundMat;
            }

            // Left & Right Arena Wall Boundaries (14m half-width)
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

            // Fighter Spawn Points
            GameObject p1Spawn = new GameObject("SpawnPoint_P1");
            p1Spawn.transform.SetParent(root.transform, false);
            p1Spawn.transform.position = new Vector3(-2.5f, 0f, 0f);
            p1Spawn.transform.rotation = Quaternion.Euler(0, 90f, 0);

            GameObject p2Spawn = new GameObject("SpawnPoint_P2");
            p2Spawn.transform.SetParent(root.transform, false);
            p2Spawn.transform.position = new Vector3(2.5f, 0f, 0f);
            p2Spawn.transform.rotation = Quaternion.Euler(0, -90f, 0);

            // 3D Background Diorama
            GameObject decorRoot = new GameObject("Environment_BackdropDiorama");
            decorRoot.transform.SetParent(root.transform, false);
            populateDiorama?.Invoke(decorRoot);

            // Stage Thematic Point Light
            GameObject lightGo = new GameObject("Stage_AccentLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 4.5f, -2.0f);
            Light lComp = lightGo.AddComponent<Light>();
            lComp.type = LightType.Point;
            lComp.color = lightColor;
            lComp.range = 32f;
            lComp.intensity = 1.35f;

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"<color=#81c784>[StageDirector] Saved Prefab: {targetPath}</color>");
        }

        private static void BuildDesertDiorama(GameObject decorRoot)
        {
            GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Rock_01.prefab");
            if (rockPrefab != null)
            {
                var r1 = UnityEngine.Object.Instantiate(rockPrefab, decorRoot.transform);
                r1.transform.localPosition = new Vector3(-11.5f, -0.2f, 4.0f);
                r1.transform.localScale = new Vector3(2.8f, 3.2f, 2.8f);
                r1.transform.localEulerAngles = new Vector3(0f, 45f, 0f);

                var r2 = UnityEngine.Object.Instantiate(rockPrefab, decorRoot.transform);
                r2.transform.localPosition = new Vector3(11.5f, -0.2f, 4.0f);
                r2.transform.localScale = new Vector3(2.8f, 3.5f, 2.8f);
                r2.transform.localEulerAngles = new Vector3(0f, -30f, 0f);

                var r3 = UnityEngine.Object.Instantiate(rockPrefab, decorRoot.transform);
                r3.transform.localPosition = new Vector3(0f, -0.2f, 6.2f);
                r3.transform.localScale = new Vector3(4.5f, 2.2f, 3.5f);
            }
            GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Column_01.prefab");
            if (colPrefab != null)
            {
                var c1 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c1.transform.localPosition = new Vector3(-7.0f, 0f, 3.8f);
                c1.transform.localScale = new Vector3(1.3f, 1.6f, 1.3f);
                var c2 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c2.transform.localPosition = new Vector3(7.0f, 0f, 3.8f);
                c2.transform.localScale = new Vector3(1.3f, 1.6f, 1.3f);
            }
        }

        private static void BuildSciFiDiorama(GameObject decorRoot)
        {
            GameObject monPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelMonitor.prefab");
            if (monPrefab != null)
            {
                var monL = UnityEngine.Object.Instantiate(monPrefab, decorRoot.transform);
                monL.transform.localPosition = new Vector3(-6.5f, 1.5f, 3.8f);
                var monR = UnityEngine.Object.Instantiate(monPrefab, decorRoot.transform);
                monR.transform.localPosition = new Vector3(6.5f, 1.5f, 3.8f);
            }
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWall.prefab");
            if (wallPrefab != null)
            {
                var wall = UnityEngine.Object.Instantiate(wallPrefab, decorRoot.transform);
                wall.transform.localPosition = new Vector3(0f, 0f, 4.2f);
                wall.transform.localScale = new Vector3(1.5f, 1.2f, 1.0f);
            }
            GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelColumn.prefab");
            if (colPrefab != null)
            {
                var colL = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                colL.transform.localPosition = new Vector3(-10.5f, 0f, 3.5f);
                var colR = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                colR.transform.localPosition = new Vector3(10.5f, 0f, 3.5f);
            }
        }

        private static void BuildAlienWorldsDiorama(GameObject decorRoot)
        {
            GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Column_01.prefab");
            if (colPrefab != null)
            {
                var c1 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c1.transform.localPosition = new Vector3(-8.5f, 0f, 3.6f);
                c1.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
                var c2 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c2.transform.localPosition = new Vector3(8.5f, 0f, 3.6f);
                c2.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
            }
            GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyDungeonsLite/Prefabs/Rock_01.prefab");
            if (rockPrefab != null)
            {
                var r1 = UnityEngine.Object.Instantiate(rockPrefab, decorRoot.transform);
                r1.transform.localPosition = new Vector3(-4.0f, 0f, 4.5f);
                r1.transform.localScale = new Vector3(2.2f, 3.8f, 2.2f);
                var r2 = UnityEngine.Object.Instantiate(rockPrefab, decorRoot.transform);
                r2.transform.localPosition = new Vector3(4.0f, 0f, 4.5f);
                r2.transform.localScale = new Vector3(2.2f, 3.8f, 2.2f);
            }
        }

        private static void BuildCartoonCityDiorama(GameObject decorRoot)
        {
            GameObject lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HairPack/Art/SpotLight/Prefabs/SpotLight_01.prefab");
            if (lightPrefab != null)
            {
                var s1 = UnityEngine.Object.Instantiate(lightPrefab, decorRoot.transform);
                s1.transform.localPosition = new Vector3(-10.0f, 0f, 2.8f);
                s1.transform.localEulerAngles = new Vector3(0, 25f, 0);

                var s2 = UnityEngine.Object.Instantiate(lightPrefab, decorRoot.transform);
                s2.transform.localPosition = new Vector3(10.0f, 0f, 2.8f);
                s2.transform.localEulerAngles = new Vector3(0, -25f, 0);
            }
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWallDoor.prefab");
            if (wallPrefab != null)
            {
                var wall = UnityEngine.Object.Instantiate(wallPrefab, decorRoot.transform);
                wall.transform.localPosition = new Vector3(0f, 0f, 4.2f);
            }
        }

        private static void BuildMetroSubwayDiorama(GameObject decorRoot)
        {
            GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelColumn.prefab");
            if (colPrefab != null)
            {
                var c1 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c1.transform.localPosition = new Vector3(-7.5f, 0f, 3.5f);
                var c2 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c2.transform.localPosition = new Vector3(7.5f, 0f, 3.5f);
            }
            GameObject ductPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelDuct.prefab");
            if (ductPrefab != null)
            {
                var duct = UnityEngine.Object.Instantiate(ductPrefab, decorRoot.transform);
                duct.transform.localPosition = new Vector3(0f, 3.6f, 3.8f);
            }
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWall.prefab");
            if (wallPrefab != null)
            {
                var wall = UnityEngine.Object.Instantiate(wallPrefab, decorRoot.transform);
                wall.transform.localPosition = new Vector3(0f, 0f, 4.5f);
            }
        }

        private static void BuildOfficeTowerDiorama(GameObject decorRoot)
        {
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelWallDoor.prefab");
            if (wallPrefab != null)
            {
                var w1 = UnityEngine.Object.Instantiate(wallPrefab, decorRoot.transform);
                w1.transform.localPosition = new Vector3(-6.0f, 0f, 4.0f);
                var w2 = UnityEngine.Object.Instantiate(wallPrefab, decorRoot.transform);
                w2.transform.localPosition = new Vector3(6.0f, 0f, 4.0f);
            }
            GameObject colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mryotaisu/Prefabs/LevelColumn.prefab");
            if (colPrefab != null)
            {
                var c1 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c1.transform.localPosition = new Vector3(-11.0f, 0f, 3.5f);
                var c2 = UnityEngine.Object.Instantiate(colPrefab, decorRoot.transform);
                c2.transform.localPosition = new Vector3(11.0f, 0f, 3.5f);
            }
        }

        private static void BuildCollegeRaimonDiorama(GameObject decorRoot)
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

        #endregion

        #region StageDefinition ScriptableObjects

        private static void GenerateAll6StageDefinitions()
        {
            DemonIdolCollegeRaimonSetup.CreateAllStageDefinitions();
        }

        private static void CreateOrUpdateStageDef(string id, string displayName, string desc, string bgm, string prefabName)
        {
            string assetPath = $"{STAGES_DATA_DIR}/{id}_{prefabName}.asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(assetPath);
            if (stage == null)
            {
                stage = ScriptableObject.CreateInstance<StageDefinition>();
                AssetDatabase.CreateAsset(stage, assetPath);
            }

            stage.id = id;
            stage.displayName = displayName;
            stage.description = desc;
            stage.bgmTrackName = bgm;
            stage.sceneName = "Home";
            stage.playableWidth = 23.0f;
            stage.wallThreshold = 10.2f;
            stage.groundSurfaceY = 0.0f;
            stage.cameraMinZ = -6.5f;
            stage.cameraMaxZ = -13.5f;
            stage.cameraMinY = 1.0f;
            stage.cameraMaxY = 6.5f;
            stage.signatureArenaAuthored = true;
            stage.backgroundResourcePath = $"Prefabs/Stages/{prefabName}";
            stage.p1SpawnPosition = new Vector3(-2.5f, 0f, 0f);
            stage.p2SpawnPosition = new Vector3(2.5f, 0f, 0f);

            GameObject pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{STAGES_PREFAB_DIR}/{prefabName}.prefab");
            if (pPrefab != null)
            {
                stage.stagePrefab = pPrefab;
            }

            EditorUtility.SetDirty(stage);
        }

        #endregion

        #region Home Scene Configuration

        private static void ConfigureHomeScene()
        {
            if (!File.Exists(SCENE_PATH)) return;
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            // 1. Configure Main Camera with SF6 Cinematic Physical Camera & 40% Framing
            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.043f, 0.051f, 0.078f, 1f);

                UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;

                FighterCamera2D cam2d = cam.GetComponent<FighterCamera2D>();
                if (cam2d == null) cam2d = cam.gameObject.AddComponent<FighterCamera2D>();

                // Authentic SF6 Cinematic Physical Camera Calibration:
                cam2d.usePhysicalCamera = true;
                cam2d.sensorSize = new Vector2(36.0f, 20.25f); // Super 35 Full Frame 16:9
                cam2d.focalLength = 50.0f; // 50mm portrait prime
                cam2d.lensShift = new Vector2(0f, 0.035f); // Heroic grounded rise

                // Exact 40% Character Screen Height Framing
                cam2d.standardFighterHeight = 1.75f;
                cam2d.targetCharacterScreenFraction = 0.40f;
                cam2d.closeCombatScreenFraction = 0.45f;
                cam2d.wideSpacingScreenFraction = 0.35f;

                // SF6 Dynamic Handling
                cam2d.offset = new Vector3(0f, 1.25f, -10.8f);
                cam2d.pitchAngle = 2.8f;
                cam2d.aggressionLeadAmount = 0.35f;
                cam2d.verticalLaunchThresholdY = 2.5f;
                cam2d.minDistanceZ = -6.5f;
                cam2d.maxDistanceZ = -13.5f;

                cam2d.ConfigurePhysicalCamera();

                GameObject p1Go = GameObject.Find("Player1_Akio");
                GameObject p2Go = GameObject.Find("Player2_Aika");
                if (p1Go != null && p2Go != null)
                {
                    cam2d.SetTargets(p1Go.transform, p2Go.transform);
                }

                EditorUtility.SetDirty(cam2d);
                EditorUtility.SetDirty(camData);
                EditorUtility.SetDirty(cam);
            }

            // 2. Ensure Arena Platform
            GameObject ground = GameObject.Find("Ground_CombatPlatform");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground_CombatPlatform";
            }
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(32f, 0.5f, 8f);
            BoxCollider gCol = ground.GetComponent<BoxCollider>();
            if (gCol == null) gCol = ground.AddComponent<BoxCollider>();
            gCol.center = Vector3.zero;
            gCol.size = Vector3.one;

            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Ground.mat");
            if (groundMat != null)
            {
                var mr = ground.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = groundMat;
            }

            // 3. Ensure Global Volume
            Volume vol = UnityEngine.Object.FindAnyObjectByType<Volume>();
            if (vol == null)
            {
                GameObject vGo = new GameObject("Global Volume");
                vol = vGo.AddComponent<Volume>();
            }
            vol.isGlobal = true;
            vol.weight = 1.0f;
            VolumeProfile prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/URP_IndoorTemplateVolumeProfile.asset")
                ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/ValorantVolumeProfile.asset");
            if (prof != null) vol.sharedProfile = prof;
            EditorUtility.SetDirty(vol);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        #endregion
    }
}
#endif

#endif
