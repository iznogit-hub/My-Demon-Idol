#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace DemonIdol.Editor.LookDev
{
    /// <summary>
    /// LookDev tool: Migrates materials from lilToon / legacy shaders to native URP PBR
    /// (Universal Render Pipeline/Autodesk Interactive with Transparent/Cutout variants and Universal Render Pipeline/Lit fallback).
    /// Strictly preserves all original textures, colors, and normal/emission maps while applying realistic PBR tuning.
    /// Also ensures valid reflection probes and balanced directional lighting across all three active scenes.
    /// </summary>
    public static class URPAutodeskInteractiveMigrator
    {
        public enum PBRCategory
        {
            Skin,
            HairEyelash,
            EyesTranslucent,
            ClothUniform,
            MetalArmorWeapon,
            DefaultOpaque
        }

        public static void MigrateAll()
        {
            Debug.Log("<color=#00e676><b>[URP LookDev] Starting URP Autodesk Interactive / PBR Migration...</b></color>");

            Shader autodeskOpaque = Shader.Find("Universal Render Pipeline/Autodesk Interactive/Autodesk Interactive") 
                                  ?? Shader.Find("Shader Graphs/AutodeskInteractive") 
                                  ?? Shader.Find("Autodesk Interactive");

            Shader autodeskMasked = Shader.Find("Universal Render Pipeline/Autodesk Interactive/Autodesk Interactive Masked")
                                  ?? Shader.Find("Shader Graphs/AutodeskInteractiveMasked");

            Shader autodeskTrans = Shader.Find("Universal Render Pipeline/Autodesk Interactive/Autodesk Interactive Transparent")
                                 ?? Shader.Find("Shader Graphs/AutodeskInteractiveTransparent");

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

            Debug.Log($"[URP LookDev] Resolved Shaders: AutodeskOpaque='{autodeskOpaque?.name}', URP_Lit='{urpLit?.name}'");

            // 1. Scan and migrate materials across Assets/
            int migratedCount = 0;
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.StartsWith("Assets/lilToon", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                bool isLilToon = mat.shader == null || 
                                 string.IsNullOrEmpty(mat.shader.name) || 
                                 mat.shader.name.IndexOf("lilToon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 mat.shader.name.IndexOf("liltoon", StringComparison.OrdinalIgnoreCase) >= 0;

                // Also check if this material is in RaimonUnified or Characters and needs PBR tuning
                bool isCharacterMat = path.IndexOf("RaimonUnified", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      path.IndexOf("MDI_Characters", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isLilToon && !isCharacterMat)
                {
                    continue;
                }

                if (isLilToon)
                {
                    MigrateMaterialToURP(mat, autodeskOpaque, autodeskMasked, autodeskTrans, urpLit);
                    EditorUtility.SetDirty(mat);
                    migratedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[URP LookDev] Migrated {migratedCount} materials to native URP PBR.</b></color>");

            // 2. Setup Lighting & Reflection Probes in the 3 main scenes
            EnsureSceneLightingAndReflectionProbes();
        }

        public static void MigrateMaterialToURP(Material mat, Shader autodeskOpaque, Shader autodeskMasked, Shader autodeskTrans, Shader urpLit)
        {
            if (mat == null) return;

            // 1. Read & Preserve Original Textures & Properties BEFORE switching shader
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            Texture albedoTex = mainTex != null ? mainTex : baseMap;

            Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            if (bumpMap == null && mat.HasProperty("_NormalMap")) bumpMap = mat.GetTexture("_NormalMap");

            Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            if (emissionMap == null && mat.HasProperty("_EmissiveMap")) emissionMap = mat.GetTexture("_EmissiveMap");

            Color baseColor = Color.white;
            if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");

            Color emissionColor = Color.black;
            if (mat.HasProperty("_EmissionColor")) emissionColor = mat.GetColor("_EmissionColor");

            float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.45f;
            float cull = mat.HasProperty("_Cull") ? mat.GetFloat("_Cull") : 2.0f;
            int renderQueue = mat.renderQueue;

            // Classify material
            PBRCategory cat = ClassifyMaterial(mat.name);

            // Select Target Shader & Surface Mode
            Shader targetShader = null;
            bool isTransparent = false;
            bool isCutout = false;

            switch (cat)
            {
                case PBRCategory.HairEyelash:
                    targetShader = autodeskMasked ?? urpLit;
                    isCutout = true;
                    cull = 0.0f; // Double-sided
                    cutoff = 0.45f;
                    break;

                case PBRCategory.EyesTranslucent:
                    targetShader = autodeskTrans ?? urpLit;
                    isTransparent = true;
                    break;

                default:
                    targetShader = autodeskOpaque ?? urpLit;
                    break;
            }

            mat.shader = targetShader;

            // Re-apply preserved textures and colors
            if (albedoTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedoTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedoTex);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            if (bumpMap != null)
            {
                if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", bumpMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.DisableKeyword("_NORMALMAP");
            }

            if (emissionMap != null || (emissionColor.maxColorComponent > 0.01f))
            {
                if (emissionMap != null && mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emissionMap);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            // Apply category-specific PBR settings
            ApplyCategoryPBR(mat, cat, isCutout, isTransparent, cutoff, cull);
        }

        public static PBRCategory ClassifyMaterial(string name)
        {
            string lower = name.ToLowerInvariant();

            // Skin
            if (lower.Contains("skin") || lower.Contains("face") || lower.Contains("body") || lower.Contains("hada") || lower.Contains("head"))
            {
                return PBRCategory.Skin;
            }

            // Hair / Eyelash / Eyebrow
            if (lower.Contains("hair") || lower.Contains("lash") || lower.Contains("brow") || lower.Contains("bang") || lower.Contains("strand"))
            {
                return PBRCategory.HairEyelash;
            }

            // Eyes / Translucent
            if (lower.Contains("eye") || lower.Contains("cornea") || lower.Contains("glass") || lower.Contains("halo") || 
                lower.Contains("overlay") || lower.Contains("trans") || lower.Contains("iris") || lower.Contains("highlight"))
            {
                return PBRCategory.EyesTranslucent;
            }

            // Metal / Armor / Weapon
            if (lower.Contains("metal") || lower.Contains("armor") || lower.Contains("sword") || lower.Contains("blade") || 
                lower.Contains("hammer") || lower.Contains("shield") || lower.Contains("button") || lower.Contains("gold") ||
                lower.Contains("iron") || lower.Contains("steel"))
            {
                return PBRCategory.MetalArmorWeapon;
            }

            // Cloth / Uniform / Leather
            if (lower.Contains("cloth") || lower.Contains("blazer") || lower.Contains("skirt") || lower.Contains("gakuran") || 
                lower.Contains("hoodie") || lower.Contains("coat") || lower.Contains("boot") || lower.Contains("shoe") ||
                lower.Contains("pant") || lower.Contains("dress") || lower.Contains("suit") || lower.Contains("uniform") ||
                lower.Contains("top") || lower.Contains("bottom"))
            {
                return PBRCategory.ClothUniform;
            }

            return PBRCategory.DefaultOpaque;
        }

        private static void ApplyCategoryPBR(Material mat, PBRCategory cat, bool isCutout, bool isTransparent, float cutoff, float cull)
        {
            float metallic = 0.0f;
            float smoothness = 0.5f;
            float roughness = 0.5f;

            switch (cat)
            {
                case PBRCategory.Skin:
                    // Natural matte skin, never plastic/oily
                    metallic = 0.0f;
                    roughness = 0.62f;
                    smoothness = 0.38f;
                    break;

                case PBRCategory.HairEyelash:
                    metallic = 0.0f;
                    roughness = 0.48f;
                    smoothness = 0.52f;
                    break;

                case PBRCategory.EyesTranslucent:
                    // High smoothness / catchlights
                    metallic = 0.0f;
                    roughness = 0.15f;
                    smoothness = 0.85f;
                    break;

                case PBRCategory.ClothUniform:
                    // Rich realistic fabric
                    metallic = 0.0f;
                    roughness = 0.78f;
                    smoothness = 0.22f;
                    break;

                case PBRCategory.MetalArmorWeapon:
                    // Realistic gleaming metal
                    metallic = 0.85f;
                    roughness = 0.28f;
                    smoothness = 0.72f;
                    break;

                default:
                    metallic = 0.0f;
                    roughness = 0.50f;
                    smoothness = 0.50f;
                    break;
            }

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Roughness")) mat.SetFloat("_Roughness", roughness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            // Surface mode
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", isTransparent ? 1.0f : 0.0f);
            }

            if (mat.HasProperty("_AlphaClip"))
            {
                mat.SetFloat("_AlphaClip", isCutout ? 1.0f : 0.0f);
                if (isCutout)
                {
                    mat.EnableKeyword("_ALPHATEST_ON");
                    if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
                }
                else
                {
                    mat.DisableKeyword("_ALPHATEST_ON");
                }
            }

            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", cull);

            if (isTransparent)
            {
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0.0f);
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else if (isCutout)
            {
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1.0f);
            }
            else
            {
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1.0f);
            }
        }

        public static void EnsureSceneLightingAndReflectionProbes()
        {
            string[] scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/CharacterSelect.unity",
                "Assets/Scenes/BattleArena.unity"
            };

            foreach (var scenePath in scenes)
            {
                if (!File.Exists(scenePath)) continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool modified = false;

                // 1. Reflection Probe
                ReflectionProbe probe = UnityEngine.Object.FindFirstObjectByType<ReflectionProbe>();
                if (probe == null)
                {
                    var probeGo = new GameObject("Environment_ReflectionProbe");
                    probe = probeGo.AddComponent<ReflectionProbe>();
                    probe.transform.position = new Vector3(0, 3f, 0);
                    modified = true;
                    Debug.Log($"[URP LookDev] Added Reflection Probe to {scenePath}");
                }

                if (probe != null)
                {
                    probe.mode = ReflectionProbeMode.Realtime;
                    probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                    probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                    probe.boxProjection = true;
                    probe.size = new Vector3(50f, 25f, 50f);
                    probe.center = Vector3.zero;
                    probe.intensity = 1.0f;
                    probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                    modified = true;
                }

                // 2. Directional Light
                Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        // Ensure Key light intensity ~1.2 - 1.5, neutral/warm tint
                        if (light.name.Contains("Key") || light.name.Contains("Directional") || light.name.Contains("Sun"))
                        {
                            light.intensity = Mathf.Clamp(light.intensity, 1.2f, 1.5f);
                            light.color = new Color(1.0f, 0.98f, 0.95f);
                            modified = true;
                        }
                        else if (light.name.Contains("Fill"))
                        {
                            light.intensity = Mathf.Clamp(light.intensity, 0.4f, 0.6f);
                            light.color = new Color(0.85f, 0.90f, 1.0f);
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"<color=#69F0AE>[URP LookDev] Scene lighting and reflection probes updated: {scenePath}</color>");
                }
            }
        }
    }
}
#endif
