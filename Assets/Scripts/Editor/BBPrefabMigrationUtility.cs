#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DemonIdol.BlazBlue;

namespace DemonIdol.Editor
{
    public static class BBPrefabMigrationUtility
    {
        public static void ConfigureST07StagePrefab()
        {
            string prefabPath = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab";
            if (!File.Exists(prefabPath))
            {
                Debug.LogError($"[BBPrefabMigrationUtility] {prefabPath} not found!");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var config = root.GetComponent<BBStagePrefabConfig>();
                if (config == null) config = root.AddComponent<BBStagePrefabConfig>();

                config.stageId = "ST07";
                config.stageDisplayName = "College Raimon — Rainy Courtyard";
                config.p1SpawnX = -2.5f;
                config.p2SpawnX = 2.5f;
                config.floorY = 0f;
                config.leftWallX = -10.2f;
                config.rightWallX = 10.2f;
                config.cameraMinX = -8.0f;
                config.cameraMaxX = 8.0f;
                config.cameraMinY = 1.0f;
                config.cameraMaxY = 6.5f;
                config.cameraDefaultZ = -4.8f;
                config.fogEnabled = true;
                config.fogColor = new Color(0.08f, 0.1f, 0.15f, 1f);
                config.fogDensity = 0.025f;
                config.ambientColor = new Color(0.2f, 0.22f, 0.28f, 1f);
                config.directionalLightColor = new Color(0.7f, 0.75f, 0.9f, 1f);
                config.directionalLightIntensity = 0.85f;

                config.postProcessProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/CollegeRaimon/RenderSettings/URP/DefaultVolumeProfile.asset");
                config.stageBGM = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_fight.wav");
                config.ambientLoopSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_rain_loop.wav");

                Transform diorama = root.transform.Find("Environment_BackdropDiorama");
                if (diorama != null) config.stagePropsRoot = diorama.gameObject;

                // Corner walls: align to ±10.2m
                Transform leftWall = root.transform.Find("Boundary_LeftWall");
                if (leftWall != null)
                {
                    leftWall.localPosition = new Vector3(-10.2f, 3.5f, 0f);
                    var col = leftWall.GetComponent<BoxCollider>();
                    if (col != null) col.size = new Vector3(0.2f, 7.0f, 4.0f);
                }
                Transform rightWall = root.transform.Find("Boundary_RightWall");
                if (rightWall != null)
                {
                    rightWall.localPosition = new Vector3(10.2f, 3.5f, 0f);
                    var col = rightWall.GetComponent<BoxCollider>();
                    if (col != null) col.size = new Vector3(0.2f, 7.0f, 4.0f);
                }

                // Spawn points
                Transform p1Spawn = root.transform.Find("SpawnPoint_P1");
                if (p1Spawn != null) p1Spawn.localPosition = new Vector3(-2.5f, 0f, 0f);
                Transform p2Spawn = root.transform.Find("SpawnPoint_P2");
                if (p2Spawn != null) p2Spawn.localPosition = new Vector3(2.5f, 0f, 0f);

                // Ground platform
                Transform ground = root.transform.Find("Ground_CombatPlatform");
                if (ground != null)
                {
                    ground.localPosition = new Vector3(0f, -0.25f, 0f);
                    var col = ground.GetComponent<BoxCollider>();
                    if (col != null) col.size = new Vector3(24f, 0.5f, 6f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("<color=#00e676>[BBPrefabMigrationUtility] ST07_CollegeRaimon.prefab successfully upgraded with BBStagePrefabConfig!</color>");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void ConfigureAllFighterPrefabs()
        {
            string[] fighterGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" });
            int count = 0;
            foreach (string guid in fighterGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith("Fighter_Player")) continue; // skip runtime test wrapper prefabs

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var config = root.GetComponent<BBFighterPrefabConfig>();
                    if (config == null) config = root.AddComponent<BBFighterPrefabConfig>();

                    config.ResolveLinkedAssets();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                    Debug.Log($"<color=#00e676>[BBPrefabMigrationUtility] Configured BBFighterPrefabConfig on {fileName}</color>");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676>[BBPrefabMigrationUtility] Finished configuring BBFighterPrefabConfig on {count} fighter prefabs!</color>");
        }

        public static void RunAll()
        {
            ConfigureST07StagePrefab();
            ConfigureAllFighterPrefabs();
        }
    }
}
#endif
