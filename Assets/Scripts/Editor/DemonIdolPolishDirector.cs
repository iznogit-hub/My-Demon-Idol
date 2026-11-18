#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Polish and Environment Director for Demon Idol: Iron Legacy.
    /// Polishes toon shaders, fixes untextured fighter models, sets up tournament 2.5D camera,
    /// dresses the 3D Asian stage arena from RGPoly_StylizedAsia assets, and calibrates hitboxes.
    /// </summary>
    public static class DemonIdolPolishDirector
    {
        public const string TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Polish/1. Polish All Clan Materials & Shaders", priority = 10)]
        public static void PolishAllMaterials()
        {
            Debug.Log("<color=#00e676>==================================================</color>");
            Debug.Log("<color=#00e676>  MDI: POLISHING ANIME TOON MATERIALS & TEXTURES  </color>");
            Debug.Log("<color=#00e676>==================================================</color>");

            Shader toonShader = Shader.Find(TOON_SHADER_NAME);
            if (toonShader == null)
            {
                Debug.LogError($"[PolishDirector] Could not find shader '{TOON_SHADER_NAME}'!");
                return;
            }

            // Specific character texture mappings
            var textureOverrides = new Dictionary<string, (string baseTex, string emissiveTex, Color skin, Color hair, Color prim, Color sec, Color neon, float neonInt)>
            {
                // Kai: Anime swordsman hero
                { "Material_OCC_KAI.mat", (
                    "Assets/Models/Male/PolygonalMind_100Avatars/100Avatars_058_Shiro/100Avatars_058_Shiro.png",
                    null,
                    new Color(0.96f, 0.88f, 0.82f, 1f),
                    new Color(0.95f, 0.95f, 0.98f, 1f),
                    new Color(0.35f, 0.15f, 0.50f, 1f),
                    new Color(0.15f, 0.12f, 0.20f, 1f),
                    new Color(0.62f, 0.00f, 1.00f, 1f),
                    2.4f
                )},
                // Arin: High-fidelity textured Orc Assassin
                { "Material_ORC_ARIN.mat", (
                    "Assets/MDI_Characters/Bases/Orc_Assassin/Textures/Orc_Albedo.tga",
                    "Assets/MDI_Characters/Bases/Orc_Assassin/Textures/Orc_Emission.tga",
                    new Color(0.65f, 0.72f, 0.55f, 1f),
                    new Color(0.15f, 0.15f, 0.15f, 1f),
                    new Color(0.35f, 0.35f, 0.38f, 1f),
                    new Color(0.60f, 0.30f, 0.15f, 1f),
                    new Color(1.00f, 0.33f, 0.00f, 1f),
                    2.2f
                )},
                // Reika: Mystic veilweaver
                { "Material_OCC_REIKA.mat", (
                    "Assets/Models/Female/PolygonalMind_100Avatars/100Avatars_177_MoonGirl/100Avatars_177_MoonGirl.png",
                    null,
                    new Color(0.98f, 0.90f, 0.88f, 1f),
                    new Color(0.40f, 0.20f, 0.60f, 1f),
                    new Color(0.25f, 0.18f, 0.35f, 1f),
                    new Color(0.15f, 0.45f, 0.55f, 1f),
                    new Color(0.00f, 0.85f, 1.00f, 1f),
                    2.5f
                )},
                // Zev: Stormbound / Pyresorcerer
                { "Material_NEO_ZEV.mat", (
                    "Assets/Models/Male/PolygonalMind_100Avatars/100Avatars_107_Pyresorcerer/100Avatars_107_Pyresorcerer.png",
                    null,
                    new Color(0.95f, 0.86f, 0.80f, 1f),
                    new Color(0.20f, 0.20f, 0.22f, 1f),
                    new Color(0.12f, 0.12f, 0.16f, 1f),
                    new Color(0.00f, 0.75f, 0.90f, 1f),
                    new Color(0.00f, 0.80f, 1.00f, 1f),
                    2.8f
                )},
                // Mira: Ash singer
                { "Material_ASH_MIRA.mat", (
                    "Assets/Models/Female/PolygonalMind_100Avatars/100Avatars_057_Rose/100Avatars_057_Rose.png",
                    null,
                    new Color(0.94f, 0.85f, 0.82f, 1f),
                    new Color(0.85f, 0.25f, 0.35f, 1f),
                    new Color(0.20f, 0.15f, 0.18f, 1f),
                    new Color(0.70f, 0.20f, 0.30f, 1f),
                    new Color(1.00f, 0.20f, 0.40f, 1f),
                    2.2f
                )},
                // Nox: Hollow Crown demon
                { "Material_DEM_NOX.mat", (
                    "Assets/Models/Male/PolygonalMind_100Avatars/100Avatars_031_Devil/100Avatars_031_Devil.png",
                    null,
                    new Color(0.85f, 0.30f, 0.30f, 1f),
                    new Color(0.10f, 0.10f, 0.12f, 1f),
                    new Color(0.18f, 0.12f, 0.15f, 1f),
                    new Color(0.50f, 0.10f, 0.15f, 1f),
                    new Color(1.00f, 0.15f, 0.15f, 1f),
                    2.6f
                )}
            };

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/MDI_Characters/Clans" });
            int updatedCount = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                mat.shader = toonShader;

                // Calibrate standard cel ramp thresholds (ensuring threshold 1 < threshold 2)
                mat.SetFloat("_RampThreshold1", 0.35f);
                mat.SetFloat("_RampThreshold2", 0.65f);
                mat.SetFloat("_RampSmoothness", 0.04f);

                // Calibrate shadow tone (clean stylized cool shadow)
                mat.SetColor("_ShadowColor", new Color(0.58f, 0.54f, 0.68f, 1.0f));

                // Calibrate inverted hull outline
                mat.SetFloat("_EnableOutline", 1.0f);
                mat.SetFloat("_OutlineWidth", 1.5f);
                mat.SetColor("_OutlineColor", new Color(0.10f, 0.08f, 0.12f, 1.0f));

                // Calibrate fresnel rim lighting
                mat.SetColor("_RimColor", Color.white);
                mat.SetFloat("_RimPower", 3.5f);
                mat.SetFloat("_RimThreshold", 0.35f);

                // Check for character-specific texture override
                string matFileName = Path.GetFileName(path);
                if (textureOverrides.TryGetValue(matFileName, out var ovr))
                {
                    if (!string.IsNullOrEmpty(ovr.baseTex))
                    {
                        Texture2D bTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ovr.baseTex);
                        if (bTex != null) mat.SetTexture("_BaseMap", bTex);
                    }
                    if (!string.IsNullOrEmpty(ovr.emissiveTex))
                    {
                        Texture2D eTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ovr.emissiveTex);
                        if (eTex != null) mat.SetTexture("_EmissiveMap", eTex);
                    }
                    mat.SetColor("_BaseColor", Color.white);
                    mat.SetColor("_SkinTint", ovr.skin);
                    mat.SetColor("_HairTint", ovr.hair);
                    mat.SetColor("_PrimaryTint", ovr.prim);
                    mat.SetColor("_SecondaryTint", ovr.sec);
                    mat.SetColor("_NeonColor", ovr.neon);
                    mat.SetFloat("_NeonIntensity", ovr.neonInt);
                }

                EditorUtility.SetDirty(mat);
                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00e676>[PolishDirector] Polished {updatedCount} clan materials with calibrated toon shading and textures.</color>");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Polish/2. Polish SampleScene Characters & Environment", priority = 11)]
        public static void PolishSampleScene()
        {
            PolishSceneInternal("Assets/Scenes/SampleScene.unity");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Polish/3. Polish UniversalFighter_Demo Scene", priority = 12)]
        public static void PolishUniversalFighterDemoScene()
        {
            PolishSceneInternal("Assets/Scenes/UniversalFighter_Demo.unity");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Polish/4. Run Full Batch Polish & Audit", priority = 13)]
        public static void PolishAndAuditBatch()
        {
            PolishAllMaterials();
            PolishSampleScene();
            PolishUniversalFighterDemoScene();
            SceneDiagnosticAuditor.RunAuditBatch();
        }

        private static void PolishSceneInternal(string scenePath)
        {
            Debug.Log($"<color=#69f0ae>[PolishDirector] Polishing Scene: {scenePath}</color>");

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[PolishDirector] Failed to open scene at {scenePath}");
                return;
            }

            // 1. Polish Player 1 (Kai)
            GameObject p1 = GameObject.Find("Player1_Kai");
            if (p1 != null)
            {
                SetupFighterPlayer1(p1);
            }

            // 2. Polish Player 2 (Dummy / Arin)
            GameObject p2 = GameObject.Find("Player2_Dummy");
            if (p2 != null)
            {
                SetupFighterPlayer2(p2);
            }

            // 3. Dress Asian Stage Environment
            SetupAsianStageEnvironment();

            // 4. Calibrate Main Camera & 2.5D FighterCamera2D
            SetupTournamentCamera(p1, p2);

            // 5. Calibrate Directional Light
            SetupDirectionalLight();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=#00e676>[PolishDirector] Successfully polished and saved: {scenePath}</color>");
        }

        private static void SetupFighterPlayer1(GameObject p1)
        {
            p1.transform.position = new Vector3(-2.5f, 0f, 0f);
            p1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Configure CharacterController
            var cc = p1.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 1.75f;
                cc.center = new Vector3(0f, 0.88f, 0f);
                cc.radius = 0.35f;
            }

            // Configure Model_Kai
            Transform modelKai = p1.transform.Find("Model_Kai");
            if (modelKai != null)
            {
                // Remove old untextured dummy meshes if present
                for (int i = modelKai.childCount - 1; i >= 0; i--)
                {
                    Transform child = modelKai.GetChild(i);
                    if (child.name.Contains("Dummy") || child.name.Contains("Retargeters"))
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }

                // Check if Shiro avatar is already child
                Transform shiroChild = modelKai.Find("100Avatars_058_Shiro");
                if (shiroChild == null)
                {
                    string shiroPath = "Assets/Models/Male/PolygonalMind_100Avatars/100Avatars_058_Shiro/100Avatars_058_Shiro.fbx";
                    GameObject shiroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shiroPath);
                    if (shiroPrefab != null)
                    {
                        GameObject shiroInst = (GameObject)PrefabUtility.InstantiatePrefab(shiroPrefab, modelKai);
                        shiroInst.name = "100Avatars_058_Shiro";
                        shiroInst.transform.localPosition = Vector3.zero;
                        shiroInst.transform.localRotation = Quaternion.identity;
                        shiroInst.transform.localScale = Vector3.one;
                    }
                }

                // Assign Material_OCC_KAI to all renderers under Model_Kai
                Material kaiMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MDI_Characters/Clans/Occult/Kai/Material_OCC_KAI.mat");
                if (kaiMat != null)
                {
                    foreach (var smr in modelKai.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.sharedMaterial = kaiMat;
                    }
                    foreach (var mr in modelKai.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.sharedMaterial = kaiMat;
                    }
                }

                // Wire Animator
                var anim = modelKai.GetComponent<Animator>();
                if (anim == null) anim = modelKai.gameObject.AddComponent<Animator>();

                string shiroFbx = "Assets/Models/Male/PolygonalMind_100Avatars/100Avatars_058_Shiro/100Avatars_058_Shiro.fbx";
                Avatar shiroAvatar = null;
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(shiroFbx))
                {
                    if (obj is Avatar av) { shiroAvatar = av; break; }
                }
                if (shiroAvatar != null) anim.avatar = shiroAvatar;

                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Clans/Occult/Kai/AnimatorOverride_OCC_KAI.overrideController");
                if (controller != null) anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;

                // Ensure AnimationEventReceiver exists
                if (modelKai.GetComponent<AnimationEventReceiver>() == null)
                {
                    modelKai.gameObject.AddComponent<AnimationEventReceiver>();
                }
            }

            // Calibrate 3-tier Hurtboxes on P1
            EnsureHurtbox(p1.transform, "Hurtbox_Head", BodyZone.Head, new Vector3(0f, 1.62f, 0f), 0.20f, Vector3.zero, true);
            EnsureHurtbox(p1.transform, "Hurtbox_Torso", BodyZone.Torso, new Vector3(0f, 1.05f, 0f), 0f, new Vector3(0.50f, 0.65f, 0.45f), false);
            EnsureHurtbox(p1.transform, "Hurtbox_Legs", BodyZone.Legs, new Vector3(0f, 0.42f, 0f), 0f, new Vector3(0.55f, 0.65f, 0.50f), false);

            // Calibrate Strike Hitbox
            EnsureHitbox(p1.transform, "Hitbox_Weapon", new Vector3(0f, 1.10f, 0.70f), new Vector3(0.40f, 0.50f, 0.80f));
        }

        private static void SetupFighterPlayer2(GameObject p2)
        {
            p2.transform.position = new Vector3(2.5f, 0f, 0f);
            p2.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            var cc = p2.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 2.0f;
                cc.center = new Vector3(0f, 1.0f, 0f);
                cc.radius = 0.45f;
            }

            Transform modelArin = p2.transform.Find("Model_Arin");
            if (modelArin != null)
            {
                Material orcMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MDI_Characters/Clans/Orc/Arin/Material_ORC_ARIN.mat");
                if (orcMat != null)
                {
                    foreach (var smr in modelArin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.sharedMaterial = orcMat;
                    }
                    foreach (var mr in modelArin.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.sharedMaterial = orcMat;
                    }
                }

                var anim = modelArin.GetComponent<Animator>();
                if (anim == null) anim = modelArin.gameObject.AddComponent<Animator>();

                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Clans/Orc/Arin/AnimatorOverride_ORC_ARIN.overrideController");
                if (controller != null) anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;

                if (modelArin.GetComponent<AnimationEventReceiver>() == null)
                {
                    modelArin.gameObject.AddComponent<AnimationEventReceiver>();
                }
            }

            EnsureHurtbox(p2.transform, "Hurtbox_Head", BodyZone.Head, new Vector3(0f, 1.82f, 0f), 0.24f, Vector3.zero, true);
            EnsureHurtbox(p2.transform, "Hurtbox_Torso", BodyZone.Torso, new Vector3(0f, 1.15f, 0f), 0f, new Vector3(0.70f, 0.75f, 0.55f), false);
            EnsureHurtbox(p2.transform, "Hurtbox_Legs", BodyZone.Legs, new Vector3(0f, 0.45f, 0f), 0f, new Vector3(0.75f, 0.70f, 0.60f), false);

            EnsureHitbox(p2.transform, "Hitbox_Weapon", new Vector3(0f, 1.20f, 0.80f), new Vector3(0.50f, 0.60f, 0.90f));
        }

        private static void EnsureHurtbox(Transform parent, string name, BodyZone zone, Vector3 pos, float radius, Vector3 boxSize, bool isSphere)
        {
            Transform hbTr = parent.Find(name);
            GameObject hbObj;
            if (hbTr == null)
            {
                hbObj = new GameObject(name);
                hbObj.transform.SetParent(parent, false);
            }
            else
            {
                hbObj = hbTr.gameObject;
            }

            hbObj.transform.localPosition = pos;
            hbObj.transform.localRotation = Quaternion.identity;
            hbObj.transform.localScale = Vector3.one;

            if (isSphere)
            {
                var sphere = hbObj.GetComponent<SphereCollider>();
                if (sphere == null) sphere = hbObj.AddComponent<SphereCollider>();
                sphere.radius = radius;
                sphere.isTrigger = true;
            }
            else
            {
                var box = hbObj.GetComponent<BoxCollider>();
                if (box == null) box = hbObj.AddComponent<BoxCollider>();
                box.size = boxSize;
                box.isTrigger = true;
            }

            var hurtboxComp = hbObj.GetComponent<Hurtbox>();
            if (hurtboxComp == null) hurtboxComp = hbObj.AddComponent<Hurtbox>();
            var so = new SerializedObject(hurtboxComp);
            var zoneProp = so.FindProperty("_zone");
            if (zoneProp != null) zoneProp.enumValueIndex = (int)zone;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureHitbox(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            Transform hbTr = parent.Find(name);
            GameObject hbObj;
            if (hbTr == null)
            {
                hbObj = new GameObject(name);
                hbObj.transform.SetParent(parent, false);
            }
            else
            {
                hbObj = hbTr.gameObject;
            }

            hbObj.transform.localPosition = pos;
            hbObj.transform.localRotation = Quaternion.identity;
            hbObj.transform.localScale = Vector3.one;

            var box = hbObj.GetComponent<BoxCollider>();
            if (box == null) box = hbObj.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            box.enabled = false; // Enabled only during attack active frames

            var hitboxComp = hbObj.GetComponent<Hitbox>();
            if (hitboxComp == null) hbObj.AddComponent<Hitbox>();
        }

        private static void SetupAsianStageEnvironment()
        {
            // 1. Ensure Ground Stone Bridge Arena Platform
            GameObject groundObj = GameObject.Find("Ground_StoneBridge");
            if (groundObj == null)
            {
                groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundObj.name = "Ground_StoneBridge";
            }
            groundObj.transform.position = new Vector3(0f, -0.5f, 0f);
            groundObj.transform.rotation = Quaternion.identity;
            groundObj.transform.localScale = new Vector3(30f, 1f, 6f);

            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Materials/Cocnrate 1  Mat.mat");
            if (stoneMat != null)
            {
                var mr = groundObj.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = stoneMat;
            }

            // 2. Setup FloodedGrounds_StageEnvironment
            GameObject stageEnv = GameObject.Find("FloodedGrounds_StageEnvironment");
            if (stageEnv == null)
            {
                stageEnv = new GameObject("FloodedGrounds_StageEnvironment");
            }
            stageEnv.transform.position = Vector3.zero;
            stageEnv.transform.rotation = Quaternion.identity;
            stageEnv.transform.localScale = Vector3.one;

            // Clear old children to eliminate stripped/dangling prefab references
            for (int i = stageEnv.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(stageEnv.transform.GetChild(i).gameObject);
            }

            // Populate Asian Shrine Arena Prefabs
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Shrine_1.fbx", stageEnv.transform, "Shrine_Main", new Vector3(0f, 0f, 9.5f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f));
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Pagoda.fbx", stageEnv.transform, "Pagoda_Left", new Vector3(-9.5f, 0f, 12.5f), Quaternion.Euler(0f, 15f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Dojo_1.fbx", stageEnv.transform, "Dojo_Right", new Vector3(9.5f, 0f, 12.0f), Quaternion.Euler(0f, -15f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Tora _1.fbx", stageEnv.transform, "ToriiGate_Left", new Vector3(-12.5f, 0f, 3.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Tora _1.fbx", stageEnv.transform, "ToriiGate_Right", new Vector3(12.5f, 0f, 3.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Sakura_1.fbx", stageEnv.transform, "SakuraTree_Left", new Vector3(-7.0f, 0f, 6.5f), Quaternion.Euler(0f, 35f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Sakura_2.fbx", stageEnv.transform, "SakuraTree_Right", new Vector3(7.0f, 0f, 6.5f), Quaternion.Euler(0f, -25f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
            SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/GardenWall_2.fbx", stageEnv.transform, "GardenWall_Back", new Vector3(0f, 0f, 2.8f), Quaternion.identity, new Vector3(1.6f, 1.2f, 1.0f));

            // Stone Lamps with warm amber Point Lights
            GameObject lampLeft = SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Stone Lamp_1.fbx", stageEnv.transform, "StoneLamp_Left", new Vector3(-4.5f, 0f, 2.2f), Quaternion.identity, Vector3.one);
            if (lampLeft != null) AddWarmLanternLight(lampLeft, new Vector3(0f, 1.4f, 0f));

            GameObject lampRight = SpawnStageProp("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Models/Stone Lamp_1.fbx", stageEnv.transform, "StoneLamp_Right", new Vector3(4.5f, 0f, 2.2f), Quaternion.identity, Vector3.one);
            if (lampRight != null) AddWarmLanternLight(lampRight, new Vector3(0f, 1.4f, 0f));
        }

        private static GameObject SpawnStageProp(string assetPath, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[PolishDirector] Could not load stage asset at: {assetPath}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = pos;
            instance.transform.localRotation = rot;
            instance.transform.localScale = scale;
            return instance;
        }

        private static void AddWarmLanternLight(GameObject lamp, Vector3 localOffset)
        {
            GameObject lightObj = new GameObject("Lantern_PointLight");
            lightObj.transform.SetParent(lamp.transform, false);
            lightObj.transform.localPosition = localOffset;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.78f, 0.42f, 1.0f); // Warm amber
            light.intensity = 2.2f;
            light.range = 7.0f;
            light.shadows = LightShadows.None; // Point light shadows disabled for mobile budget
        }

        private static void SetupTournamentCamera(GameObject p1, GameObject p2)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = GameObject.Find("Main Camera");
                if (camObj != null) cam = camObj.GetComponent<Camera>();
            }

            if (cam != null)
            {
                cam.fieldOfView = 42.0f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 300f;

                var fCam = cam.GetComponent<FighterCamera2D>();
                if (fCam == null) fCam = cam.gameObject.AddComponent<FighterCamera2D>();

                if (p1 != null) fCam.fighter1 = p1.transform;
                if (p2 != null) fCam.fighter2 = p2.transform;

                fCam.offset = new Vector3(0f, 1.85f, -6.8f);
                fCam.pitchAngle = 2.5f;
                fCam.minDistanceZ = -5.5f;
                fCam.maxDistanceZ = -8.5f;
                fCam.zoomFactor = 0.55f;
                fCam.smoothSpeed = 8.0f;
                fCam.smoothSpeedY = 3.5f;
                fCam.smoothSpeedZ = 5.0f;

                cam.transform.position = new Vector3(0f, 1.85f, -6.8f);
                cam.transform.rotation = Quaternion.Euler(2.5f, 0f, 0f);
            }
        }

        private static void SetupDirectionalLight()
        {
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj != null)
            {
                lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                var light = lightObj.GetComponent<Light>();
                if (light != null)
                {
                    light.color = new Color(1.0f, 0.96f, 0.90f, 1.0f);
                    light.intensity = 1.25f;
                    light.shadows = LightShadows.Soft;
                }
            }
        }
    }
}
#endif