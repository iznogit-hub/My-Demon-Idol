#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master builder to set up the 3D Japanese Shrine diorama stage and
    /// reassemble Kai and Arin with native rigged humanoid models, toon cel shading,
    /// authentic clan textures, and glowing HDR neon outlines.
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolStageAndFighterBuilder
    {
        private const string PREF_KEY = "DemonIdol_StageAndFighterSetup_v2";

        [InitializeOnLoadMethod]
        private static void AutoRunOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PREF_KEY, false))
                {
                    Debug.Log("<color=#00e676>[DemonIdol Builder] Auto-executing Stage and Fighter Setup...</color>");
                    BuildAll();
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
            };
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Build Stage and Fighters (Kai, Arin & Whispering Grove)", priority = 0)]
        public static void ForceBuildAll()
        {
            BuildAll();
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Capture In-Game Screenshot", priority = 1)]
        public static void CaptureGameView()
        {
            ScreenCapture.CaptureScreenshot("screenshot_playmode_verified.png");
            Debug.Log("<color=#00e676>[DemonIdol Builder] Captured in-game screenshot to screenshot_playmode_verified.png</color>");
        }

        public static void BuildAll()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("<color=#ffb74d>[DemonIdol Builder] Exiting Play Mode before setting up scene...</color>");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () =>
                {
                    BuildAll();
                };
                return;
            }

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: ASSEMBLING STAGE DIORAMA & COMBAT CHARACTERS  </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Build Kai Prefab
            BuildKaiPrefab();

            // 2. Build Arin Prefab
            BuildArinPrefab();

            // 3. Setup HDRI Skybox & Stage Platform in SampleScene
            SetupStageAndFightersInScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676><b>[DemonIdol Builder] COMPLETE:</b> Whispering Grove Stage and Fighters Launch-Ready!</color>");
        }

        public static void BuildKaiPrefab()
        {
            string baseFbxPath = "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx";
            string targetPrefabPath = "Assets/MDI_Characters/Clans/Occult/Kai/Kai.prefab";
            string matPath = "Assets/MDI_Characters/Clans/Occult/Kai/Material_OCC_KAI.mat";
            string controllerPath = "Assets/MDI_Characters/Clans/Occult/Kai/AnimatorOverride_OCC_KAI.overrideController";
            string daggerPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Dagger.fbx";

            GameObject baseFbx = AssetDatabase.LoadAssetAtPath<GameObject>(baseFbxPath);
            if (baseFbx == null)
            {
                Debug.LogError($"[BuildKai] Base FBX not found at {baseFbxPath}");
                return;
            }

            GameObject instance = Object.Instantiate(baseFbx);
            instance.name = "Kai";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Animator setup
            Animator anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (rac != null) anim.runtimeAnimatorController = rac;

            // Material assignment to DummyMesh
            Material kaiMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (kaiMat != null)
            {
                var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    smr.sharedMaterial = kaiMat;
                }
            }

            // Attach Dagger to right hand bone
            Transform rightHand = FindBoneRecursive(instance.transform, "B-hand_R");
            if (rightHand == null) rightHand = FindBoneRecursive(instance.transform, "hand_R");
            if (rightHand != null)
            {
                GameObject daggerFbx = AssetDatabase.LoadAssetAtPath<GameObject>(daggerPath);
                if (daggerFbx != null)
                {
                    GameObject daggerObj = Object.Instantiate(daggerFbx, rightHand);
                    daggerObj.name = "Accessory_Dagger";
                    daggerObj.transform.localPosition = new Vector3(0.04f, 0.08f, -0.02f);
                    daggerObj.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
                    daggerObj.transform.localScale = Vector3.one * 0.85f;
                    if (kaiMat != null)
                    {
                        foreach (var r in daggerObj.GetComponentsInChildren<Renderer>(true))
                        {
                            r.sharedMaterial = kaiMat;
                        }
                    }
                }
            }

            // AnimationEventReceiver
            if (instance.GetComponent<AnimationEventReceiver>() == null)
            {
                instance.AddComponent<AnimationEventReceiver>();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"<color=#00e676>[BuildKai] Successfully assembled native {targetPrefabPath}</color>");
        }

        public static void BuildArinPrefab()
        {
            string baseFbxPath = "Assets/MDI_Characters/Bases/Orc_Assassin/Base_Orc_Assassin.fbx";
            string targetPrefabPath = "Assets/MDI_Characters/Clans/Orc/Arin/Arin.prefab";
            string matPath = "Assets/MDI_Characters/Clans/Orc/Arin/Material_ORC_ARIN.mat";
            string controllerPath = "Assets/MDI_Characters/Clans/Orc/Arin/AnimatorOverride_ORC_ARIN.overrideController";
            string warhammerPath = "Assets/Models/Accessories/Weapons/CC0_Melee/MedievalWarhammer/MedievalWarhammer.fbx";

            GameObject baseFbx = AssetDatabase.LoadAssetAtPath<GameObject>(baseFbxPath);
            if (baseFbx == null)
            {
                Debug.LogError($"[BuildArin] Base FBX not found at {baseFbxPath}");
                return;
            }

            GameObject instance = Object.Instantiate(baseFbx);
            instance.name = "Arin";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Animator setup
            Animator anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (rac != null) anim.runtimeAnimatorController = rac;

            // Material assignment to all Orc body and armor renderers
            Material orcMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (orcMat != null)
            {
                var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    smr.sharedMaterial = orcMat;
                }
                var mrs = instance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in mrs)
                {
                    mr.sharedMaterial = orcMat;
                }
            }

            // Attach Warhammer to right hand
            Transform rightHand = FindBoneRecursive(instance.transform, "ORK_RightHand");
            if (rightHand == null) rightHand = FindBoneRecursive(instance.transform, "RightHand");
            if (rightHand != null)
            {
                GameObject hammerFbx = AssetDatabase.LoadAssetAtPath<GameObject>(warhammerPath);
                if (hammerFbx != null)
                {
                    GameObject hammerObj = Object.Instantiate(hammerFbx, rightHand);
                    hammerObj.name = "Accessory_Warhammer";
                    hammerObj.transform.localPosition = new Vector3(0f, 0.12f, 0.05f);
                    hammerObj.transform.localEulerAngles = new Vector3(180f, 0f, 90f);
                    hammerObj.transform.localScale = Vector3.one * 1.1f;
                    if (orcMat != null)
                    {
                        foreach (var r in hammerObj.GetComponentsInChildren<Renderer>(true))
                        {
                            r.sharedMaterial = orcMat;
                        }
                    }
                }
            }

            // AnimationEventReceiver
            if (instance.GetComponent<AnimationEventReceiver>() == null)
            {
                instance.AddComponent<AnimationEventReceiver>();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"<color=#00e676>[BuildArin] Successfully assembled native {targetPrefabPath}</color>");
        }

        public static void SetupStageAndFightersInScene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.GetActiveScene();
            bool needToOpen = scene.path != scenePath;
            if (needToOpen)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Create Panoramic Ninomaru Teien Skybox
            Material skyMat = EnsureSkyboxMaterial();
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1.1f;
            }

            // 2. Calibrate Directional Light for golden sunset ambience
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.transform.rotation = Quaternion.Euler(25f, -35f, 0f);
                    l.color = new Color(1.0f, 0.965f, 0.898f); // Warm golden sunlight (#FFF6E5)
                    l.intensity = 1.25f;
                    l.shadows = LightShadows.Soft;
                    break;
                }
            }

            // 3. Texture Ground_StoneBridge
            GameObject ground = GameObject.Find("Ground_StoneBridge");
            if (ground != null)
            {
                ground.transform.position = new Vector3(0, -0.5f, 0);
                ground.transform.localScale = new Vector3(32f, 1f, 6f);
                MeshRenderer mr = ground.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Materials/Cocnrate 1  Mat.mat");
                    if (concreteMat != null) mr.sharedMaterial = concreteMat;
                }
            }

            // 4. Construct Japanese Shrine Stage Diorama under FloodedGrounds_StageEnvironment
            GameObject stageRoot = GameObject.Find("FloodedGrounds_StageEnvironment");
            if (stageRoot == null)
            {
                stageRoot = new GameObject("FloodedGrounds_StageEnvironment");
                stageRoot.transform.position = Vector3.zero;
            }

            // Clear old children
            for (int i = stageRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(stageRoot.transform.GetChild(i).gameObject);
            }

            string prefabsDir = "Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Prefabs/";

            // Centerpiece: Grand Shrine (Shrine_1 Variant)
            InstantiatePrefabUnder(prefabsDir + "Shrine_1 Variant.prefab", stageRoot.transform, new Vector3(0f, 0f, 8.5f), Quaternion.identity, Vector3.one * 1.4f);

            // Sacred Torii Gate (Tora _1 Variant)
            InstantiatePrefabUnder(prefabsDir + "Tora _1 Variant.prefab", stageRoot.transform, new Vector3(0f, 0f, 4.8f), Quaternion.identity, Vector3.one * 1.2f);

            // Left Multi-tier Pagoda
            InstantiatePrefabUnder(prefabsDir + "Pagoda Variant.prefab", stageRoot.transform, new Vector3(-8.0f, 0f, 12.0f), Quaternion.identity, Vector3.one * 1.3f);

            // Right Martial Dojo
            InstantiatePrefabUnder(prefabsDir + "Dojo_1 Variant.prefab", stageRoot.transform, new Vector3(8.0f, 0f, 11.0f), Quaternion.identity, Vector3.one * 1.2f);

            // Garden Walls & Gates flanking the arena
            InstantiatePrefabUnder(prefabsDir + "GardenWall_Gate Variant.prefab", stageRoot.transform, new Vector3(-10.0f, 0f, 4.2f), Quaternion.Euler(0, 15f, 0), Vector3.one);
            InstantiatePrefabUnder(prefabsDir + "GardenWall_Gate_2 Variant.prefab", stageRoot.transform, new Vector3(10.0f, 0f, 4.2f), Quaternion.Euler(0, -15f, 0), Vector3.one);

            // Blooming Sakura Trees
            InstantiatePrefabUnder(prefabsDir + "Sakura_1 Variant.prefab", stageRoot.transform, new Vector3(-12.5f, 0f, 4.5f), Quaternion.Euler(0, 30f, 0), Vector3.one * 1.15f);
            InstantiatePrefabUnder(prefabsDir + "Sakura_2 Variant.prefab", stageRoot.transform, new Vector3(12.5f, 0f, 4.5f), Quaternion.Euler(0, -45f, 0), Vector3.one * 1.15f);

            // Kasuga Stone Lanterns along the edge
            InstantiatePrefabUnder(prefabsDir + "Stone Lamp_1 Variant.prefab", stageRoot.transform, new Vector3(-5.5f, 0f, 2.5f), Quaternion.identity, Vector3.one);
            InstantiatePrefabUnder(prefabsDir + "Stone Lamp_1 Variant.prefab", stageRoot.transform, new Vector3(5.5f, 0f, 2.5f), Quaternion.identity, Vector3.one);
            InstantiatePrefabUnder(prefabsDir + "Stone Lamp_1 Variant.prefab", stageRoot.transform, new Vector3(-11.5f, 0f, 2.5f), Quaternion.identity, Vector3.one);
            InstantiatePrefabUnder(prefabsDir + "Stone Lamp_1 Variant.prefab", stageRoot.transform, new Vector3(11.5f, 0f, 2.5f), Quaternion.identity, Vector3.one);

            // Hanging Paper Lanterns on Torii Gate
            InstantiatePrefabUnder(prefabsDir + "Lampion_1 Variant.prefab", stageRoot.transform, new Vector3(-2.1f, 2.8f, 4.8f), Quaternion.identity, Vector3.one);
            InstantiatePrefabUnder(prefabsDir + "Lampion_1 Variant.prefab", stageRoot.transform, new Vector3(2.1f, 2.8f, 4.8f), Quaternion.identity, Vector3.one);

            // Falling Cherry Leaves particle system
            string leavesPrefabPath = "Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Materials/Falling Leaves.prefab";
            InstantiatePrefabUnder(leavesPrefabPath, stageRoot.transform, new Vector3(0f, 3.5f, 0.5f), Quaternion.identity, Vector3.one);

            // 5. Pre-wire Kai and Arin models inside the scene
            SetupFighterGameObject("Player1_Kai", "Assets/MDI_Characters/Clans/Occult/Kai/Kai.prefab", "Model_Kai", new Vector3(-2.5f, 0f, 0f), 90f, true);
            SetupFighterGameObject("Player2_Dummy", "Assets/MDI_Characters/Clans/Orc/Arin/Arin.prefab", "Model_Arin", new Vector3(2.5f, 0f, 0f), -90f, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=#00e676>[SetupStageAndFighters] Saved {scenePath} with 3D Whispering Grove stage & wired fighters!</color>");
        }

        private static void SetupFighterGameObject(string rootName, string prefabPath, string modelName, Vector3 pos, float eulerY, bool isPlayer)
        {
            GameObject rootGo = GameObject.Find(rootName);
            if (rootGo == null)
            {
                rootGo = new GameObject(rootName);
                rootGo.transform.position = pos;
                rootGo.transform.rotation = Quaternion.Euler(0, eulerY, 0);
            }

            rootGo.transform.localScale = Vector3.one;

            CharacterController cc = rootGo.GetComponent<CharacterController>();
            if (cc == null) cc = rootGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.height = 1.9f;
            cc.radius = 0.35f;

            FighterController fc = rootGo.GetComponent<FighterController>();
            if (fc == null) fc = rootGo.AddComponent<FighterController>();
            fc.isPlayerControlled = isPlayer;

            // Remove any legacy models (keep Hitbox and Hurtbox)
            for (int i = rootGo.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = rootGo.transform.GetChild(i);
                if (child.name.StartsWith("Model_") || child.name == "Kai" || child.name == "Arin")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Instantiate visual prefab
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (modelPrefab != null)
            {
                GameObject modelInstance = Object.Instantiate(modelPrefab, rootGo.transform);
                modelInstance.name = modelName;
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                Animator anim = modelInstance.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    fc.animator = anim;
                }
            }
        }

        private static GameObject InstantiatePrefabUnder(string prefabPath, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[InstantiatePrefabUnder] Prefab not found: {prefabPath}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance != null)
            {
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                instance.transform.localScale = localScale;
            }
            return instance;
        }

        private static Material EnsureSkyboxMaterial()
        {
            string skyMatPath = "Assets/Materials/Skybox_NinomaruTeien.mat";
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
            Shader panShader = Shader.Find("Skybox/Panoramic");
            if (panShader == null) panShader = Shader.Find("Universal Render Pipeline/Lit");

            if (skyMat == null)
            {
                if (!Directory.Exists("Assets/Materials"))
                {
                    Directory.CreateDirectory("Assets/Materials");
                }
                skyMat = new Material(panShader);
                AssetDatabase.CreateAsset(skyMat, skyMatPath);
            }
            else
            {
                skyMat.shader = panShader;
            }

            Texture2D hdri = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Environments/HDRI/PolyHaven/ninomaru_teien_2k.hdr");
            if (hdri != null)
            {
                skyMat.SetTexture("_MainTex", hdri);
            }

            skyMat.SetFloat("_Exposure", 1.15f);
            skyMat.SetFloat("_Rotation", 180f);
            EditorUtility.SetDirty(skyMat);
            return skyMat;
        }

        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase)) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
