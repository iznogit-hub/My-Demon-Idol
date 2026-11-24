#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DemonIdol.BlazBlue;

namespace DemonIdol.Editor
{
    public static class DemonIdolStageTemplateCreator
    {
        public const string STAGES_FOLDER = "Assets/Prefabs/Stages";

        public static void CreateStageTemplate()
        {
            if (!Directory.Exists(STAGES_FOLDER))
            {
                Directory.CreateDirectory(STAGES_FOLDER);
                AssetDatabase.Refresh();
            }

            string baseName = "ST_Template_NewStage";
            string assetPath = $"{STAGES_FOLDER}/{baseName}.prefab";
            int counter = 1;
            while (File.Exists(assetPath))
            {
                assetPath = $"{STAGES_FOLDER}/{baseName}_{counter:D2}.prefab";
                counter++;
            }

            // Create temporary hierarchy
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(assetPath));

            // 1. Add and configure BBStagePrefabConfig
            var config = root.AddComponent<BBStagePrefabConfig>();
            config.stageId = "ST_NEW";
            config.stageDisplayName = "New Stage Template";
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
            config.ambientColor = new Color(0.2f, 0.22f, 0.28f, 1f);
            config.directionalLightColor = Color.white;
            config.directionalLightIntensity = 1.0f;
            config.fogEnabled = true;
            config.fogColor = new Color(0.08f, 0.1f, 0.15f, 1f);
            config.fogDensity = 0.025f;

            // Resolve default volume profile and audio assets if available
            config.postProcessProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/CollegeRaimon/RenderSettings/URP/DefaultVolumeProfile.asset");
            config.stageBGM = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_fight.wav");
            config.ambientLoopSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_rain_loop.wav");

            // 2. Child: Ground_CombatPlatform
            GameObject ground = new GameObject("Ground_CombatPlatform");
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var groundCol = ground.AddComponent<BoxCollider>();
            groundCol.size = new Vector3(24f, 0.5f, 6f);

            // 3. Child: Boundary_LeftWall
            GameObject leftWall = new GameObject("Boundary_LeftWall");
            leftWall.transform.SetParent(root.transform, false);
            leftWall.transform.localPosition = new Vector3(-10.2f, 3.5f, 0f);
            var leftCol = leftWall.AddComponent<BoxCollider>();
            leftCol.size = new Vector3(0.2f, 7.0f, 4.0f);

            // 4. Child: Boundary_RightWall
            GameObject rightWall = new GameObject("Boundary_RightWall");
            rightWall.transform.SetParent(root.transform, false);
            rightWall.transform.localPosition = new Vector3(10.2f, 3.5f, 0f);
            var rightCol = rightWall.AddComponent<BoxCollider>();
            rightCol.size = new Vector3(0.2f, 7.0f, 4.0f);

            // 5. Child: SpawnPoint_P1
            GameObject p1Spawn = new GameObject("SpawnPoint_P1");
            p1Spawn.transform.SetParent(root.transform, false);
            p1Spawn.transform.localPosition = new Vector3(-2.5f, 0f, 0f);
            p1Spawn.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // 6. Child: SpawnPoint_P2
            GameObject p2Spawn = new GameObject("SpawnPoint_P2");
            p2Spawn.transform.SetParent(root.transform, false);
            p2Spawn.transform.localPosition = new Vector3(2.5f, 0f, 0f);
            p2Spawn.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            // 7. Child: Lighting_Rig
            GameObject lightingRig = new GameObject("Lighting_Rig");
            lightingRig.transform.SetParent(root.transform, false);
            GameObject dirLightGo = new GameObject("Directional Light");
            dirLightGo.transform.SetParent(lightingRig.transform, false);
            dirLightGo.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            var dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = Color.white;
            dirLight.intensity = 1.0f;

            // 8. Child: StageProps_Root
            GameObject stageProps = new GameObject("StageProps_Root");
            stageProps.transform.SetParent(root.transform, false);
            config.stagePropsRoot = stageProps;

            // Save as Prefab
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);

            Debug.Log($"<color=#00e676>[DemonIdolStageTemplateCreator] Successfully created Stage Prefab Template at: {assetPath}</color>");
        }
    }
}
#endif
