#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Tests;
using DemonIdol.CameraControl;
using DemonIdol.Combat;
using DemonIdol.Cinematics;
using DemonIdol.UI;
using DemonIdol.Editor.Combat;
using DemonIdol.Editor.Animation;
using DemonIdol.Editor.LookDev;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Non-interactive headless sync utility for My-Demon-Idol.
    /// Safely synchronizes LookDev materials, 12 Warrior Controllers, baked hitboxes,
    /// and the 3 live Unity scenes (MainMenu, CharacterSelect, BattleArena) without modal dialogs.
    /// Can be executed via CLI batchmode or the Unity Editor menu.
    /// </summary>
    public static class DemonIdolLaunchSync
    {
        public const string SCENE_MAIN_MENU = "Assets/Scenes/MainMenu.unity";
        public const string SCENE_CHAR_SELECT = "Assets/Scenes/CharacterSelect.unity";
        public const string SCENE_BATTLE_ARENA = "Assets/Scenes/BattleArena.unity";

        [MenuItem("Demon Idol/Launch/Sync All Systems to Real Scenes (Non-Interactive)", priority = 1)]
        public static void RunHeadlessLaunchSync()
        {
            Debug.Log("<color=#00e676><b>[DemonIdolLaunchSync] ========================================</b></color>");
            Debug.Log("<color=#00e676><b>[DemonIdolLaunchSync] STARTING HEADLESS LAUNCH SYNC PASS...</b></color>");
            Debug.Log("<color=#00e676><b>[DemonIdolLaunchSync] ========================================</b></color>");

            try
            {
                // 1. LookDev (conditional lilToon migration + Raimon aesthetic unification)
                SyncLookDev();

                // 2. 12-Fighter Warrior Controllers & Hitboxes
                SyncWarriorControllersAndHitboxes();

                // 3. Sync the 3 Real Scenes (in-place first, fallback to headless rebuild if structurally broken)
                SyncScenes();

                // 4. Save and refresh assets
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Clean legacy backups if generated
                CleanLegacyBackups();

                Debug.Log("<color=#00e676><b>[DemonIdolLaunchSync] ========================================</b></color>");
                Debug.Log("<color=#00e676><b>[DemonIdolLaunchSync] LAUNCH SYNC COMPLETED SUCCESSFULLY!</b></color>");
                Debug.Log("<color=#00e676><b>[DemonIdolLaunchSync] ========================================</b></color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#ff1744><b>[DemonIdolLaunchSync] ERROR DURING SYNC: {ex}</b></color>");
                throw;
            }
        }

        // =========================================================================
        // 1. LOOKDEV
        // =========================================================================
        private static void SyncLookDev()
        {
            Debug.Log("<color=#81c784>[DemonIdolLaunchSync] [Step 1/3] Checking LookDev & Materials...</color>");
            bool hasLilToon = false;
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null && mat.shader.name.IndexOf("lilToon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasLilToon = true;
                    Debug.Log($"[DemonIdolLaunchSync] Found lilToon material: {path}");
                    break;
                }
            }

            if (hasLilToon)
            {
                Debug.Log("[DemonIdolLaunchSync] lilToon materials found. Running URPAutodeskInteractiveMigrator.MigrateAll()...");
                URPAutodeskInteractiveMigrator.MigrateAll();
            }
            else
            {
                Debug.Log("[DemonIdolLaunchSync] 0 lilToon materials found. Skipping lilToon migration.");
            }

            Debug.Log("[DemonIdolLaunchSync] Running RaimonMaterialUnifier.UnifyAll12LaunchFighters()...");
            RaimonMaterialUnifier.UnifyAll12LaunchFighters();
        }

        // =========================================================================
        // 2. 12-FIGHTER WARRIOR CONTROLLERS & HITBOXES
        // =========================================================================
        private static void SyncWarriorControllersAndHitboxes()
        {
            Debug.Log("<color=#81c784>[DemonIdolLaunchSync] [Step 2/3] Building Warrior Controllers & Baking Hitboxes...</color>");
            Debug.Log("[DemonIdolLaunchSync] Building 12 Warrior Controllers...");
            WarriorAnimationOverhaulTool.Build12WarriorControllers();

            Debug.Log("[DemonIdolLaunchSync] Baking 12 Warrior Hitboxes...");
            WarriorHitboxBaker.BakeAll12WarriorHitboxes();

            Debug.Log("[DemonIdolLaunchSync] Auditing Hitbox Reach vs Avatar Reach...");
            WarriorAnimationOverhaulTool.AuditHitboxVsAvatarReach();
        }

        // =========================================================================
        // 3. SYNC REAL SCENES
        // =========================================================================
        private static void SyncScenes()
        {
            Debug.Log("<color=#81c784>[DemonIdolLaunchSync] [Step 3/3] Synchronizing Real Scenes...</color>");

            SyncMainMenu();
            SyncCharacterSelect();
            SyncBattleArena();

            // Enforce build settings order: MainMenu (0), CharacterSelect (1), BattleArena (2)
            DemonIdolSceneBuilder.SetBuildOrder();
        }

        private static void SyncMainMenu()
        {
            Debug.Log("[DemonIdolLaunchSync] Syncing MainMenu.unity...");
            Scene scene = EditorSceneManager.OpenScene(SCENE_MAIN_MENU, OpenSceneMode.Single);
            var ctrl = UnityEngine.Object.FindAnyObjectByType<DemonIdolMainMenuController>();

            if (ctrl == null || CountMissingScriptsInScene(scene) > 0)
            {
                Debug.LogWarning("[DemonIdolLaunchSync] MainMenu missing controller or has missing scripts. Rebuilding headless...");
                DemonIdolSceneBuilder.BuildMainMenuScene(false);
                scene = EditorSceneManager.OpenScene(SCENE_MAIN_MENU, OpenSceneMode.Single);
                ctrl = UnityEngine.Object.FindAnyObjectByType<DemonIdolMainMenuController>();
            }

            if (ctrl == null)
            {
                throw new InvalidOperationException("[DemonIdolLaunchSync] DemonIdolMainMenuController not found after scene sync!");
            }

            // Ensure Hero_SpawnPoint is a real, serialized GameObject in the hierarchy
            ctrl.ContextEnsureHeroSpawnPoint();

            // Verify ctrl.menuCamera points to MainCamera
            Camera cam = ctrl.menuCamera;
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    var camGo = GameObject.Find("Main Camera");
                    if (camGo != null) cam = camGo.GetComponent<Camera>();
                }
                if (cam == null)
                {
                    cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                }
                ctrl.menuCamera = cam;
            }

            // Verify MenuFighterCameraRig is attached to menuCamera
            if (ctrl.menuCamera != null)
            {
                var rig = ctrl.menuCamera.GetComponent<MenuFighterCameraRig>();
                if (rig == null)
                {
                    rig = ctrl.menuCamera.gameObject.AddComponent<MenuFighterCameraRig>();
                }
                EditorUtility.SetDirty(rig);
            }

            EditorUtility.SetDirty(ctrl);
            if (ctrl.menuCamera != null) EditorUtility.SetDirty(ctrl.menuCamera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DemonIdolLaunchSync] MainMenu.unity synchronized and saved.");
        }

        private static void SyncCharacterSelect()
        {
            Debug.Log("[DemonIdolLaunchSync] Syncing CharacterSelect.unity...");
            Scene scene = EditorSceneManager.OpenScene(SCENE_CHAR_SELECT, OpenSceneMode.Single);

            if (scene.GetRootGameObjects().Length == 0 || CountMissingScriptsInScene(scene) > 0)
            {
                Debug.LogWarning("[DemonIdolLaunchSync] CharacterSelect broken or has missing scripts. Rebuilding headless...");
                DemonIdolSceneBuilder.BuildCharacterSelectScene(false);
                scene = EditorSceneManager.OpenScene(SCENE_CHAR_SELECT, OpenSceneMode.Single);
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = GameObject.Find("Main Camera");
                if (camGo != null) cam = camGo.GetComponent<Camera>();
            }
            if (cam == null)
            {
                cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }

            bool dirtied = false;
            if (cam != null)
            {
                var rig = cam.GetComponent<MenuFighterCameraRig>();
                if (rig == null)
                {
                    cam.gameObject.AddComponent<MenuFighterCameraRig>();
                    dirtied = true;
                }
            }

            if (dirtied)
            {
                if (cam != null) EditorUtility.SetDirty(cam);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[DemonIdolLaunchSync] CharacterSelect.unity synchronized.");
        }

        private static void SyncBattleArena()
        {
            Debug.Log("[DemonIdolLaunchSync] Syncing BattleArena.unity...");
            Scene scene = EditorSceneManager.OpenScene(SCENE_BATTLE_ARENA, OpenSceneMode.Single);

            int missing = CountMissingScriptsInScene(scene);
            var stageCam = UnityEngine.Object.FindAnyObjectByType<StageCameraManager>();
            var matchBootstrap = UnityEngine.Object.FindAnyObjectByType<BBMatchBootstrap>();

            if (missing > 0 || stageCam == null || matchBootstrap == null)
            {
                Debug.LogWarning($"[DemonIdolLaunchSync] BattleArena missing core components (StageCamera={stageCam!=null}, MatchBootstrap={matchBootstrap!=null}, missingScripts={missing}). Rebuilding headless...");
                DemonIdolSceneBuilder.BuildBattleArenaScene(false);
                scene = EditorSceneManager.OpenScene(SCENE_BATTLE_ARENA, OpenSceneMode.Single);
                stageCam = UnityEngine.Object.FindAnyObjectByType<StageCameraManager>();
            }

            if (stageCam != null)
            {
                stageCam.EnsureCameraRosterConfigured();
                EditorUtility.SetDirty(stageCam);
            }

            // Verify active combat camera has FighterCamera2D and configure physical camera
            var fighterCams = UnityEngine.Object.FindObjectsByType<FighterCamera2D>(FindObjectsSortMode.None);
            foreach (var fc in fighterCams)
            {
                fc.ConfigurePhysicalCamera();
                EditorUtility.SetDirty(fc);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DemonIdolLaunchSync] BattleArena.unity synchronized and saved.");
        }

        private static int CountMissingScriptsInScene(Scene scene)
        {
            int count = 0;
            var rootGos = scene.GetRootGameObjects();
            foreach (var go in rootGos)
            {
                count += CountMissingScriptsInGameObject(go);
            }
            return count;
        }

        private static int CountMissingScriptsInGameObject(GameObject go)
        {
            int missing = 0;
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missing++;
                }
            }
            for (int i = 0; i < go.transform.childCount; i++)
            {
                missing += CountMissingScriptsInGameObject(go.transform.GetChild(i).gameObject);
            }
            return missing;
        }

        private static void CleanLegacyBackups()
        {
            string legacyPath = "Assets/Scenes/_Legacy";
            if (Directory.Exists(legacyPath))
            {
                Debug.Log($"[DemonIdolLaunchSync] Removing legacy backup clutter at {legacyPath}");
                Directory.Delete(legacyPath, true);
                string meta = legacyPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }
        }
    }
}
#endif
