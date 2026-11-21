#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;
using DemonIdol.Characters;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Automated setup director for College Raimon environment across all 3 game scenes:
    /// - Scene 1: Assets/Scenes/MainMenu.unity (Japanese campus backdrop + ST07 Stage Select)
    /// - Scene 2: Assets/Scenes/ArcadeArena.unity (2.5D combat arena stage prefab ST07_CollegeRaimon)
    /// - Scene 3: Assets/Scenes/RosterShowcase.unity (3D diorama showcase environment)
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolCollegeRaimonSetup
    {
        private const string STAGES_PREFAB_DIR = "Assets/Prefabs/Stages";
        private const string STAGES_DATA_DIR = "Assets/CombatData/Stages";
        private const string RAIMON_PREFAB_PATH = "Assets/CollegeRaimon/Prefabs/CollegeRaimon.prefab";
        private const string ST07_PREFAB_PATH = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab";

        private const string SCENE_MAIN_MENU = "Assets/Scenes/MainMenu.unity";
        private const string SCENE_ARCADE_ARENA = "Assets/Scenes/ArcadeArena.unity";
        private const string SCENE_ROSTER_SHOWCASE = "Assets/Scenes/RosterShowcase.unity";

        private const string TRIGGER_FILE = "Temp/run_college_raimon_setup.txt";

        static DemonIdolCollegeRaimonSetup()
        {
            EditorApplication.update += CheckPendingTrigger;
        }

        private static void CheckPendingTrigger()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                ExecuteFullSetup();
            }
        }

        public static void TriggerSetup()
        {
            File.WriteAllText(TRIGGER_FILE, "run");
            AssetDatabase.Refresh();
        }

        public static void ExecuteFullSetup()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: COLLEGE RAIMON 3-SCENE INTEGRATION PIPELINE    </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Build ST07_CollegeRaimon.prefab for 2.5D Combat Arena
            BuildCollegeRaimonStagePrefab();

            // 2. Create ST07_ST07_CollegeRaimon.asset ScriptableObject definition
            CreateStageDefinition();

            // 3. Setup Scene 1: MainMenu.unity
            SetupMainMenuScene();

            // 4. Setup Scene 2: ArcadeArena.unity
            SetupArcadeArenaScene();

            // 5. Setup Scene 3: RosterShowcase.unity
            SetupRosterShowcaseScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676><b>[College Raimon Setup] SUCCESS!</b> Fully integrated across all 3 scenes & ST07 registered.</color>");
        }

        public static void BuildCollegeRaimonStagePrefab()
        {
            if (!AssetDatabase.IsValidFolder(STAGES_PREFAB_DIR))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "Stages");
            }

            GameObject root = new GameObject("ST07_CollegeRaimon");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            // Combat Floor Platform (32m width, 8m depth, surface at Y=0.0m) - Pure invisible collision platform, no grey line
            GameObject platform = new GameObject("Ground_CombatPlatform");
            platform.transform.SetParent(root.transform, false);
            platform.transform.position = new Vector3(0f, -0.25f, 0f);

            var groundCol = platform.AddComponent<BoxCollider>();
            groundCol.center = Vector3.zero;
            groundCol.size = new Vector3(32f, 0.5f, 8f);

            // Left & Right Arena Wall Boundaries (±14.0m)
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

            // 3D Background Diorama: Nested College Raimon
            GameObject decorRoot = new GameObject("Environment_BackdropDiorama");
            decorRoot.transform.SetParent(root.transform, false);

            GameObject raimonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RAIMON_PREFAB_PATH);
            GameObject inst = null;
            if (raimonPrefab != null)
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(raimonPrefab, decorRoot.transform);
                inst.name = "CollegeRaimon_Courtyard";
                float s = 0.65f;
                inst.transform.localScale = new Vector3(s, s, s);
                inst.transform.localPosition = new Vector3(-10.2f * s, -100.2f * s, 2.2f);
                inst.transform.localRotation = Quaternion.Euler(0.1f, 0f, 0f);
            }

            // Stage Thematic Point Light (Cherry Blossom Sakura Glow)
            GameObject lightGo = new GameObject("Stage_AccentLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 4.5f, -2.0f);
            Light lComp = lightGo.AddComponent<Light>();
            lComp.type = LightType.Point;
            lComp.color = new Color(1.0f, 0.85f, 0.90f, 1.0f);
            lComp.range = 32f;
            lComp.intensity = 1.35f;

            // Master Campus Zone Anchor for all 7 derived stages
            CampusZoneAnchor anchor = root.AddComponent<CampusZoneAnchor>();
            SerializedObject soAnchor = new SerializedObject(anchor);
            var campusProp = soAnchor.FindProperty("_campusTransform");
            var accentLightProp = soAnchor.FindProperty("_accentLight");
            if (campusProp != null && inst != null) campusProp.objectReferenceValue = inst.transform;
            if (accentLightProp != null) accentLightProp.objectReferenceValue = lComp;
            soAnchor.ApplyModifiedProperties();

            // Attach Grim Dark Atmospheric Storm Director
            root.AddComponent<DemonIdol.VFX.GrimDarkStormDirector>();

            PrefabUtility.SaveAsPrefabAsset(root, ST07_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"<color=#81c784>[College Raimon] Saved Stage Prefab: {ST07_PREFAB_PATH}</color>");
        }

        public static void CreateStageDefinition()
        {
            CreateAllStageDefinitions();
        }

        public static void CreateAllStageDefinitions()
        {
            if (!AssetDatabase.IsValidFolder(STAGES_DATA_DIR))
            {
                if (!AssetDatabase.IsValidFolder("Assets/CombatData")) AssetDatabase.CreateFolder("Assets", "CombatData");
                AssetDatabase.CreateFolder("Assets/CombatData", "Stages");
            }

            var definitions = new (string id, string displayName, string desc, string bgm, string filename)[]
            {
                ("ST01", "Central Courtyard", "Iconic central courtyard flanked by the clocktower facade and main academic wing.", "Courtyard High Noon BGM", "ST01_ST01_LowPolyDesert.asset"),
                ("ST02", "Sakura Main Gate", "Traditional school entrance pillars and wrought iron gates framed by blooming sakura cherry blossoms.", "Sakura Dawn BGM", "ST02_ST02_RTSSciFiHangar.asset"),
                ("ST03", "Rooftop Observatory", "High rooftop deck enclosed by chain-link fences, overlooking the sprawling urban metropolis at twilight.", "Skyline Sunset BGM", "ST03_ST03_SpaceAlienWorlds.asset"),
                ("ST04", "Martial Arts Dojo", "Traditional Japanese wooden dojo pavilion with lattice screens, polished decking, and hanging lanterns.", "Dojo Discipline BGM", "ST04_ST04_CartoonCity.asset"),
                ("ST05", "Athletic Stadium", "Campus athletic field bordered by running tracks, bleacher stands, and high-intensity floodlight towers.", "Stadium Overcast BGM", "ST05_ST05_MetroSubway.asset"),
                ("ST06", "Transit Promenade", "Modern transit promenade connecting campus walkways, bicycle shelters, and vending kiosks.", "Promenade Dusk BGM", "ST06_ST06_ModernOfficeTower.asset"),
                ("ST07", "College Raimon", "Sprawling Japanese high school & college campus courtyard under a midnight thunderstorm with sakura blossoms.", "Academy Courtyard BGM", "ST07_ST07_CollegeRaimon.asset")
            };

            GameObject pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ST07_PREFAB_PATH);

            foreach (var def in definitions)
            {
                string assetPath = $"{STAGES_DATA_DIR}/{def.filename}";
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(assetPath);
                if (stage == null)
                {
                    stage = ScriptableObject.CreateInstance<StageDefinition>();
                    AssetDatabase.CreateAsset(stage, assetPath);
                }

                stage.id = def.id;
                stage.displayName = def.displayName;
                stage.description = def.desc;
                stage.bgmTrackName = def.bgm;
                stage.sceneName = "ArcadeArena";
                stage.playableWidth = 23.0f;
                stage.wallThreshold = 10.2f;
                stage.groundSurfaceY = 0.0f;
                stage.cameraMinZ = -6.5f;
                stage.cameraMaxZ = -13.5f;
                stage.cameraMinY = 1.0f;
                stage.cameraMaxY = 6.5f;
                stage.signatureArenaAuthored = true;
                stage.backgroundResourcePath = "Prefabs/Stages/ST07_CollegeRaimon";
                stage.p1SpawnPosition = new Vector3(-2.5f, 0f, 0f);
                stage.p2SpawnPosition = new Vector3(2.5f, 0f, 0f);

                if (pPrefab != null)
                {
                    stage.stagePrefab = pPrefab;
                }

                EditorUtility.SetDirty(stage);
            }

            Debug.Log($"<color=#81c784>[College Raimon] Saved All 7 Unified Stage Definitions derived from College Raimon campus.</color>");
        }

        public static void SetupMainMenuScene()
        {
            // Build the clean Main Menu scene with ST07 card and campus backdrop
            DemonIdolMainMenuBuilder.BuildMainMenuScene();
            Debug.Log("<color=#81c784>[College Raimon] MainMenu.unity built with campus backdrop & ST07 button.</color>");
        }

        public static void SetupArcadeArenaScene()
        {
            // Set ST07 as active stage prefab and build ArcadeArena.unity
            DemonIdolArcadeArenaBuilder.ActiveStagePrefabPath = ST07_PREFAB_PATH;
            DemonIdolArcadeArenaBuilder.BuildArcadeArenaScene();
            Debug.Log("<color=#81c784>[College Raimon] ArcadeArena.unity built with ST07_CollegeRaimon arena.</color>");
        }

        public static void SetupRosterShowcaseScene()
        {
            if (!File.Exists(SCENE_ROSTER_SHOWCASE)) return;

            Scene scene = EditorSceneManager.OpenScene(SCENE_ROSTER_SHOWCASE, OpenSceneMode.Single);

            // 1. Find or create Pedestal_Environment
            GameObject pedestalEnv = GameObject.Find("Pedestal_Environment");
            if (pedestalEnv == null)
            {
                pedestalEnv = new GameObject("Pedestal_Environment");
            }

            // Ensure Platform_Base exists
            Transform platformBase = pedestalEnv.transform.Find("Platform_Base");
            if (platformBase == null)
            {
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name = "Platform_Base";
                platform.transform.SetParent(pedestalEnv.transform, false);
                platform.transform.position = new Vector3(0.8f, -0.6f, 0f);
                platform.transform.localScale = new Vector3(3.2f, 0.3f, 3.2f);
            }

            // Add or replace CollegeRaimon_Backdrop
            Transform existingBackdrop = pedestalEnv.transform.Find("CollegeRaimon_Backdrop");
            if (existingBackdrop != null)
            {
                UnityEngine.Object.DestroyImmediate(existingBackdrop.gameObject);
            }

            GameObject raimonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RAIMON_PREFAB_PATH);
            if (raimonPrefab != null)
            {
                GameObject campusBackdrop = (GameObject)PrefabUtility.InstantiatePrefab(raimonPrefab, pedestalEnv.transform);
                campusBackdrop.name = "CollegeRaimon_Backdrop";
                campusBackdrop.transform.localPosition = new Vector3(-6.0f, -0.6f, 16.0f);
                campusBackdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                campusBackdrop.transform.localScale = Vector3.one;
            }

            // 2. Ensure MDI_RosterShowcaseController exists on a root controller
            GameObject showcaseCtrlGo = GameObject.Find("Showcase_Director");
            if (showcaseCtrlGo == null)
            {
                showcaseCtrlGo = new GameObject("Showcase_Director");
            }

            var showcaseCtrl = showcaseCtrlGo.GetComponent<MDI_RosterShowcaseController>();
            if (showcaseCtrl == null)
            {
                showcaseCtrl = showcaseCtrlGo.AddComponent<MDI_RosterShowcaseController>();
            }

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camGo = GameObject.Find("Main Camera");
                if (camGo != null) mainCam = camGo.GetComponent<Camera>();
            }
            showcaseCtrl.showcaseCamera = mainCam;

            // Populate fighter definitions from project assets
            showcaseCtrl.fighterDefinitions.Clear();
            string[] fighterGuids = AssetDatabase.FindAssets("t:FighterDefinition");
            foreach (string guid in fighterGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var fDef = AssetDatabase.LoadAssetAtPath<DemonIdol.Characters.FighterDefinition>(assetPath);
                if (fDef != null && !showcaseCtrl.fighterDefinitions.Contains(fDef))
                {
                    showcaseCtrl.fighterDefinitions.Add(fDef);
                }
            }

            EditorSceneManager.SaveScene(scene, SCENE_ROSTER_SHOWCASE);
            Debug.Log("<color=#81c784>[College Raimon] RosterShowcase.unity configured with campus diorama & MDI_RosterShowcaseController.</color>");
        }
    }
}
#endif
