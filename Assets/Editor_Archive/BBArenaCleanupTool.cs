#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    [InitializeOnLoad]
    public static class BBArenaCleanupTool
    {
        public const string TRIGGER_FILE = "clean_arena.trigger";
        private const string SCENE_PATH = "Assets/Scenes/ArcadeArena.unity";
        private const string ST07_PREFAB_PATH = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab";
        private const string RAIN_MAT_PATH = "Assets/Materials/VFX_Rain_Streak_URP.mat";
        private const string RAIN_TEX_PATH = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr stretch rain blur.png";

        static BBArenaCleanupTool()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                ExecuteFullCleanup();
            }
        }

        [MenuItem("MDI/Execute Arena Cleanup and Convert Rain Shaders")]
        public static void ExecuteFullCleanup()
        {
            Debug.Log("<color=#00e676>[BBArenaCleanupTool] Starting Arena Cleanup and Rain Shader Conversion...</color>");

            List<string> convertedMaterials = new List<string>();

            // 1. Convert any non-URP shaders on CFXR materials
            ConvertCFXRMaterialsToURP(convertedMaterials);

            // 2. Setup URP Rain Material & configure VFX_RainDownpour on ST07_CollegeRaimon.prefab
            ConfigureRainPrefab(convertedMaterials);

            // 3. Clean ArcadeArena scene (remove stale projectiles, ensure URP materials)
            CleanArcadeArenaScene(convertedMaterials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBArenaCleanupTool] COMPLETED! Total materials converted: {convertedMaterials.Count}</color>");
            foreach (var matName in convertedMaterials)
            {
                Debug.Log($"<color=#81c784> - Converted Material: {matName}</color>");
            }
        }

        private static void ConvertCFXRMaterialsToURP(List<string> convertedMaterials)
        {
            Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticlesUnlit == null)
            {
                Debug.LogWarning("[BBArenaCleanupTool] Could not find Universal Render Pipeline/Particles/Unlit shader.");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics" });
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                if (mat.shader != null && (mat.shader.name.Contains("Cartoon FX") || mat.shader.name.Contains("Legacy") || mat.shader.name.StartsWith("Particles/")))
                {
                    Texture mainTex = mat.mainTexture;
                    Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    float srcBlend = mat.HasProperty("_SrcBlend") ? mat.GetFloat("_SrcBlend") : (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    float dstBlend = mat.HasProperty("_DstBlend") ? mat.GetFloat("_DstBlend") : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

                    mat.shader = urpParticlesUnlit;
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                    }
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", col);
                    }
                    mat.SetFloat("_Surface", 1f); // Transparent
                    mat.SetFloat("_Blend", (dstBlend == (float)UnityEngine.Rendering.BlendMode.One) ? 2f : 0f); // Additive or Alpha
                    mat.SetFloat("_SrcBlend", srcBlend);
                    mat.SetFloat("_DstBlend", dstBlend);
                    mat.SetFloat("_ZWrite", 0f);

                    EditorUtility.SetDirty(mat);
                    convertedMaterials.Add($"{mat.name} ({path}) -> URP/Particles/Unlit");
                }
            }
        }

        private static void ConfigureRainPrefab(List<string> convertedMaterials)
        {
            Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticlesUnlit == null) return;

            // Ensure Materials directory
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // Create or load VFX_Rain_Streak_URP.mat
            Material rainMat = AssetDatabase.LoadAssetAtPath<Material>(RAIN_MAT_PATH);
            if (rainMat == null)
            {
                rainMat = new Material(urpParticlesUnlit);
                AssetDatabase.CreateAsset(rainMat, RAIN_MAT_PATH);
                convertedMaterials.Add($"Created {RAIN_MAT_PATH} -> URP/Particles/Unlit");
            }
            else
            {
                rainMat.shader = urpParticlesUnlit;
            }

            Texture2D rainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(RAIN_TEX_PATH);
            if (rainTex != null)
            {
                rainMat.SetTexture("_BaseMap", rainTex);
            }

            // Soft light translucent blue-white streak
            Color rainColor = new Color(0.85f, 0.92f, 1.0f, 0.32f);
            rainMat.SetColor("_BaseColor", rainColor);
            rainMat.SetFloat("_Surface", 1f); // Transparent
            rainMat.SetFloat("_Blend", 0f);   // Alpha blend
            rainMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rainMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            rainMat.SetFloat("_ZWrite", 0f);
            rainMat.enableInstancing = true;
            EditorUtility.SetDirty(rainMat);

            // Configure ST07_CollegeRaimon.prefab
            if (File.Exists(ST07_PREFAB_PATH))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(ST07_PREFAB_PATH);
                try
                {
                    Transform rainTrans = root.transform.Find("VFX_RainDownpour");
                    if (rainTrans != null)
                    {
                        // Position mostly BEHIND fighters (fighters at Z=0, rain centered at Z=2.5m)
                        rainTrans.localPosition = new Vector3(0f, 14f, 2.5f);

                        var ps = rainTrans.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            var main = ps.main;
                            main.loop = true;
                            main.playOnAwake = true;
                            main.startLifetime = 0.75f;
                            main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 28f);
                            // Thin soft light streaks
                            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
                            main.startColor = new Color(0.88f, 0.94f, 1.0f, 0.35f);
                            main.maxParticles = 2200;
                            main.simulationSpace = ParticleSystemSimulationSpace.World;

                            var emission = ps.emission;
                            emission.rateOverTime = 1000f;

                            var shape = ps.shape;
                            shape.shapeType = ParticleSystemShapeType.Box;
                            // Box depth 8m centered at Z=2.5m extends from -1.5m to +6.5m (mostly behind fighters)
                            shape.scale = new Vector3(34f, 1f, 8f);

                            var vel = ps.velocityOverLifetime;
                            vel.enabled = true;
                            vel.space = ParticleSystemSimulationSpace.World;
                            vel.x = new ParticleSystem.MinMaxCurve(-3.0f);
                            vel.y = new ParticleSystem.MinMaxCurve(-24f);
                            vel.z = new ParticleSystem.MinMaxCurve(-1.2f);
                        }

                        var psr = rainTrans.GetComponent<ParticleSystemRenderer>();
                        if (psr != null)
                        {
                            psr.renderMode = ParticleSystemRenderMode.Stretch;
                            psr.cameraVelocityScale = 0f;
                            psr.velocityScale = 0.035f;
                            psr.lengthScale = 2.2f;
                            psr.sharedMaterial = rainMat;
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, ST07_PREFAB_PATH);
                    Debug.Log("<color=#81c784>[BBArenaCleanupTool] Saved VFX_RainDownpour on ST07_CollegeRaimon.prefab with URP rain material and Z=2.5m backdrop positioning.</color>");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void CleanArcadeArenaScene(List<string> convertedMaterials)
        {
            if (!File.Exists(SCENE_PATH)) return;

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // 1. Destroy any stale projectile objects in the scene
            var allRoots = scene.GetRootGameObjects();
            int destroyedCount = 0;
            var rootTargets = new List<GameObject>();

            foreach (var rootGo in allRoots)
            {
                if (rootGo == null) continue;
                string n = rootGo.name;
                if (n.Contains("Projectile") || n.Contains("CFXR Hit D"))
                {
                    rootTargets.Add(rootGo);
                }
            }

            foreach (var go in rootTargets)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    destroyedCount++;
                }
            }

            // Also check for any remaining MDIProjectile anywhere in remaining hierarchies
            var remainingRoots = scene.GetRootGameObjects();
            foreach (var rootGo in remainingRoots)
            {
                if (rootGo == null) continue;
                var projs = rootGo.GetComponentsInChildren<MDIProjectile>(true);
                foreach (var p in projs)
                {
                    if (p != null && p.gameObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(p.gameObject);
                        destroyedCount++;
                    }
                }
            }
            if (destroyedCount > 0)
            {
                Debug.Log($"<color=#81c784>[BBArenaCleanupTool] Removed {destroyedCount} stale projectile/spark objects from ArcadeArena scene.</color>");
            }

            // 2. Scan all renderers in the scene for non-URP shaders
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null || mat.shader == null) continue;

                    string sName = mat.shader.name;
                    if (sName.StartsWith("Standard") || sName.StartsWith("Legacy Shaders/") || sName.StartsWith("Particles/"))
                    {
                        Shader targetShader = (r is ParticleSystemRenderer) ? urpParticlesUnlit : urpLit;
                        if (targetShader != null)
                        {
                            Texture mainTex = mat.mainTexture;
                            Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                            mat.shader = targetShader;
                            if (mainTex != null)
                            {
                                mat.SetTexture("_BaseMap", mainTex);
                            }
                            if (mat.HasProperty("_BaseColor"))
                            {
                                mat.SetColor("_BaseColor", col);
                            }
                            EditorUtility.SetDirty(mat);
                            convertedMaterials.Add($"{mat.name} on {r.gameObject.name} -> {targetShader.name}");
                        }
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#81c784>[BBArenaCleanupTool] Cleaned and saved ArcadeArena.unity successfully.</color>");
        }
    }
}
#endif

#endif
