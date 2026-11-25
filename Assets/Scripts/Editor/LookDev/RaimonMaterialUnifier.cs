#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DemonIdol.UI;

namespace DemonIdol.Editor.LookDev
{
    /// <summary>
    /// Phase 1: Raimon College Uniform / Aesthetic Material Unifier.
    /// Unifies the 12 Launch fighters (C01, C02, C05, C06, C08, C09, C11, C12, C14, EX01, EX04, SK08)
    /// into a cohesive dark-anime Raimon College aesthetic (Obsidian Charcoal #14161E, Vermilion #C23B22, Antique Gold #D4AF37).
    /// Standardizes cel-shading shadow ramps, corridor rim-light, and cel outlines.
    /// Strictly non-destructive: all original textures and third-party packs are preserved.
    /// </summary>
    public static class RaimonMaterialUnifier
    {
        public const string UNIFIED_MAT_ROOT = "Assets/Materials/RaimonUnified";
        public const string UNIFIED_TEX_ROOT = "Assets/Textures/RaimonUnified";

        public static readonly string[] LaunchFighterIds = new string[]
        {
            "C01", "C02", "C05", "C06", "C08", "C09",
            "C11", "C12", "C14", "EX01", "EX04", "SK08"
        };

        private static readonly Dictionary<string, string> PrefabPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "C01",  "Assets/Prefabs/Characters/C01_Akio.prefab" },
            { "C02",  "Assets/Prefabs/Characters/C02_Aika.prefab" },
            { "C05",  "Assets/Prefabs/Characters/C05_Akane.prefab" },
            { "C06",  "Assets/Prefabs/Characters/C06_Cultist.prefab" },
            { "C08",  "Assets/Prefabs/Characters/C08_SanpakuChan.prefab" },
            { "C09",  "Assets/Prefabs/Characters/C09_PicoChan.prefab" },
            { "C11",  "Assets/Prefabs/Characters/C11_Casual1.prefab" },
            { "C12",  "Assets/Prefabs/Characters/C12_Fina.prefab" },
            { "C14",  "Assets/Prefabs/Characters/C14_Muryotaisu.prefab" },
            { "EX01", "Assets/Prefabs/Characters/DotaHeroes/EX01_Hugo.prefab" },
            { "EX04", "Assets/Prefabs/Characters/DotaHeroes/EX04_OfficerK.prefab" },
            { "SK08", "Assets/Prefabs/Characters/Sidekicks/SK08_Torgrim.prefab" },
        };

        // Raimon College Palette Constants
        public static readonly Color RaimonObsidian = new Color(0.078f, 0.086f, 0.118f, 1.0f); // #14161E
        public static readonly Color RaimonVermilion = new Color(0.761f, 0.231f, 0.133f, 1.0f); // #C23B22
        public static readonly Color RaimonGold = new Color(0.831f, 0.686f, 0.216f, 1.0f);     // #D4AF37
        public static readonly Color TorgrimBossObsidian = new Color(0.043f, 0.051f, 0.071f, 1.0f); // #0B0D12
        public static readonly Color TorgrimBossCrimson = new Color(0.639f, 0.157f, 0.078f, 1.0f);  // #A32814

        // Standardized Cel-Shading Parameters
        public static readonly Color CelShadowTone = new Color(0.10f, 0.11f, 0.16f, 1.0f);
        public static readonly Color CelRimCorridorTone = new Color(0.18f, 0.35f, 0.38f, 0.70f);
        public static readonly Color CelOutlineTone = new Color(0.06f, 0.07f, 0.09f, 1.0f);
        public const float CelRampStep1 = 0.42f;
        public const float CelRampStep2 = 0.78f;
        public const float CelRampSmooth = 0.008f;
        public const float CelOutlineWidth = 1.2f;

        public static void UnifyAll12LaunchFighters()
        {
            Debug.Log("<color=#00FF3B><b>[RaimonMaterialUnifier] Starting Non-Destructive 12-Fighter Raimon Material Unification Pass...</b></color>");

            EnsureDirectory(UNIFIED_MAT_ROOT);
            EnsureDirectory(UNIFIED_TEX_ROOT);

            Shader animeToonShader = Shader.Find("DemonIdol/MDI/AnimeToon");
            if (animeToonShader == null)
            {
                animeToonShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            int processedCount = 0;
            foreach (var fighterId in LaunchFighterIds)
            {
                if (!PrefabPaths.TryGetValue(fighterId, out string prefabPath) || !File.Exists(prefabPath))
                {
                    Debug.LogWarning($"[RaimonMaterialUnifier] Prefab not found for {fighterId}: {prefabPath}");
                    continue;
                }

                string matDir = $"{UNIFIED_MAT_ROOT}/{fighterId}";
                string texDir = $"{UNIFIED_TEX_ROOT}/{fighterId}";
                EnsureDirectory(matDir);
                EnsureDirectory(texDir);

                GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefab == null) continue;

                try
                {
                    Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    bool modifiedPrefab = false;

                    foreach (var rend in renderers)
                    {
                        if (rend == null) continue;
                        Material[] sharedMats = rend.sharedMaterials;
                        bool rendModified = false;

                        for (int i = 0; i < sharedMats.Length; i++)
                        {
                            Material origMat = sharedMats[i];
                            if (origMat == null) continue;

                            string matName = origMat.name.Replace("(Instance)", "").Trim();
                            string targetMatPath = $"{matDir}/{matName}_Raimon.mat";

                            Material unifiedMat = AssetDatabase.LoadAssetAtPath<Material>(targetMatPath);
                            if (unifiedMat == null)
                            {
                                unifiedMat = new Material(origMat);
                                unifiedMat.name = $"{matName}_Raimon";
                                AssetDatabase.CreateAsset(unifiedMat, targetMatPath);
                            }

                            // Classify material slot:
                            MaterialSlotCategory category = ClassifyMaterial(matName, rend.gameObject.name, fighterId);

                            ApplyRaimonAesthetics(unifiedMat, category, fighterId, texDir, animeToonShader);
                            EditorUtility.SetDirty(unifiedMat);

                            sharedMats[i] = unifiedMat;
                            rendModified = true;
                        }

                        if (rendModified)
                        {
                            rend.sharedMaterials = sharedMats;
                            modifiedPrefab = true;
                        }
                    }

                    if (modifiedPrefab)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                        processedCount++;
                        Debug.Log($"<color=#69F0AE>[RaimonMaterialUnifier] Successfully unified {fighterId} ({prefabPath})</color>");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00E676><b>[RaimonMaterialUnifier] COMPLETED: Unified {processedCount} / {LaunchFighterIds.Length} fighter prefabs into Raimon College Palette!</b></color>");
        }

        public enum MaterialSlotCategory
        {
            Skin,
            Hair,
            Eyes,
            OutfitPrimary,
            OutfitSecondaryTrim,
            OutfitMetalAccent,
            BossArmor
        }

        public static MaterialSlotCategory ClassifyMaterial(string matName, string gameObjectName, string fighterId)
        {
            string lower = (matName + " " + gameObjectName).ToLowerInvariant();

            if (fighterId == "SK08")
            {
                return MaterialSlotCategory.BossArmor;
            }

            // Eyes / Brows / Eyelashes / Eyeline
            if (lower.Contains("eye") || lower.Contains("eyebrow") || lower.Contains("eyelash") ||
                lower.Contains("eyeline") || lower.Contains("iris") || lower.Contains("highlight"))
            {
                return MaterialSlotCategory.Eyes;
            }

            // Hair
            if (lower.Contains("hair"))
            {
                return MaterialSlotCategory.Hair;
            }

            // Skin / Face / Mouth / Body (when not outfit)
            if (lower.Contains("skin") || lower.Contains("face") || lower.Contains("mouth") || lower.Contains("hada"))
            {
                return MaterialSlotCategory.Skin;
            }

            // Metals / Decos / Buttons / Rings
            if (lower.Contains("metal") || lower.Contains("deco") || lower.Contains("gold") || lower.Contains("accent") || lower.Contains("button"))
            {
                return MaterialSlotCategory.OutfitMetalAccent;
            }

            // Secondary Trim / Ties / Wraps / Belts / Shoes
            if (lower.Contains("trim") || lower.Contains("tie") || lower.Contains("shoe") || lower.Contains("belt") || lower.Contains("ribbon"))
            {
                return MaterialSlotCategory.OutfitSecondaryTrim;
            }

            // Outfit / Cloth / Uniform / Tops / Bottoms / Pants
            if (lower.Contains("cloth") || lower.Contains("top") || lower.Contains("bottom") || lower.Contains("pant") ||
                lower.Contains("dress") || lower.Contains("suit") || lower.Contains("uniform") || lower.Contains("costume") ||
                lower.Contains("fuku") || lower.Contains("armor") || lower.Contains("bag") || lower.Contains("mat"))
            {
                return MaterialSlotCategory.OutfitPrimary;
            }

            // Fighter-specific defaults for single-material models or general meshes:
            if (fighterId == "EX04" && lower.Contains("body")) return MaterialSlotCategory.OutfitPrimary; // Security uniform
            if (fighterId == "C11" && lower.Contains("body")) return MaterialSlotCategory.Skin;
            if (fighterId == "C01" || fighterId == "C02" || fighterId == "C05")
            {
                if (lower.Contains("body")) return MaterialSlotCategory.Skin;
            }

            return MaterialSlotCategory.OutfitPrimary;
        }

        private static void ApplyRaimonAesthetics(Material mat, MaterialSlotCategory category, string fighterId, string texDir, Shader animeToonShader)
        {
            // If the material has our AnimeToon shader, configure it cleanly:
            bool isAnimeToon = mat.shader != null && mat.shader.name == "DemonIdol/MDI/AnimeToon";
            if (!isAnimeToon && animeToonShader != null && mat.shader != null)
            {
                // Assign AnimeToon for standardized cel-shading if compatible
                mat.shader = animeToonShader;
                isAnimeToon = true;
            }

            // Standardize Cel-Shading properties (Shadow Ramp, Rim, Outline)
            if (mat.HasProperty("_RampThreshold1")) mat.SetFloat("_RampThreshold1", CelRampStep1);
            if (mat.HasProperty("_RampThreshold2")) mat.SetFloat("_RampThreshold2", CelRampStep2);
            if (mat.HasProperty("_RampSmoothness")) mat.SetFloat("_RampSmoothness", CelRampSmooth);
            if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", CelShadowTone);
            if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", CelRimCorridorTone);
            if (mat.HasProperty("_RimThreshold")) mat.SetFloat("_RimThreshold", 0.40f);
            if (mat.HasProperty("_EnableOutline")) mat.SetFloat("_EnableOutline", 1.0f);
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", CelOutlineWidth);
            if (mat.HasProperty("_OutlineColor")) mat.SetColor("_OutlineColor", CelOutlineTone);

            // Configure Palette per Category
            switch (category)
            {
                case MaterialSlotCategory.Skin:
                case MaterialSlotCategory.Hair:
                case MaterialSlotCategory.Eyes:
                    // Preserve natural tone, hair color, and eye textures!
                    if (mat.HasProperty("_UniformDesat")) mat.SetFloat("_UniformDesat", 0.0f);
                    if (mat.HasProperty("_UniformTint")) mat.SetColor("_UniformTint", Color.white);
                    break;

                case MaterialSlotCategory.OutfitPrimary:
                    // Primary garment: Obsidian Charcoal #14161E
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RaimonObsidian);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", RaimonObsidian);
                    if (mat.HasProperty("_UniformDesat")) mat.SetFloat("_UniformDesat", 0.85f);
                    if (mat.HasProperty("_UniformTint")) mat.SetColor("_UniformTint", RaimonObsidian);
                    RemapClothTextureIfNeeded(mat, fighterId, texDir, RaimonObsidian);
                    break;

                case MaterialSlotCategory.OutfitSecondaryTrim:
                    // Secondary trim/ties/wraps: Crimson/Vermilion #C23B22
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RaimonVermilion);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", RaimonVermilion);
                    if (mat.HasProperty("_UniformDesat")) mat.SetFloat("_UniformDesat", 0.70f);
                    if (mat.HasProperty("_UniformTint")) mat.SetColor("_UniformTint", RaimonVermilion);
                    RemapClothTextureIfNeeded(mat, fighterId, texDir, RaimonVermilion);
                    break;

                case MaterialSlotCategory.OutfitMetalAccent:
                    // Metal/buttons/accents: Antique Gold #D4AF37
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RaimonGold);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", RaimonGold);
                    if (mat.HasProperty("_UniformDesat")) mat.SetFloat("_UniformDesat", 0.50f);
                    if (mat.HasProperty("_UniformTint")) mat.SetColor("_UniformTint", RaimonGold);
                    RemapClothTextureIfNeeded(mat, fighterId, texDir, RaimonGold);
                    break;

                case MaterialSlotCategory.BossArmor:
                    // SK08 Torgrim Boss: Darker/heavier obsidian + deep crimson
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", TorgrimBossObsidian);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", TorgrimBossObsidian);
                    if (mat.HasProperty("_UniformDesat")) mat.SetFloat("_UniformDesat", 0.90f);
                    if (mat.HasProperty("_UniformTint")) mat.SetColor("_UniformTint", TorgrimBossObsidian);
                    RemapClothTextureIfNeeded(mat, fighterId, texDir, TorgrimBossObsidian);
                    break;
            }
        }

        /// <summary>
        /// Non-destructively generates a recolored/graded texture variant into Assets/Textures/RaimonUnified/<FighterID>/
        /// without touching original textures.
        /// </summary>
        private static void RemapClothTextureIfNeeded(Material mat, string fighterId, string texDir, Color targetTint)
        {
            Texture origTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            if (origTex == null && mat.HasProperty("_MainTex"))
            {
                origTex = mat.GetTexture("_MainTex");
            }
            if (origTex == null || !(origTex is Texture2D srcTex)) return;

            string srcPath = AssetDatabase.GetAssetPath(srcTex);
            if (string.IsNullOrEmpty(srcPath) || srcPath.StartsWith(UNIFIED_TEX_ROOT)) return; // Already unified

            string texName = Path.GetFileNameWithoutExtension(srcPath);
            string destPath = $"{texDir}/{texName}_Raimon.png";

            // If already created, reuse it
            Texture2D destTex = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
            if (destTex == null)
            {
                // Make source texture readable temporarily if needed
                string metaPath = srcPath + ".meta";
                bool madeReadable = EnsureTextureReadable(srcPath);

                try
                {
                    Texture2D readableSrc = srcTex;
                    // Read pixels
                    RenderTexture rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(srcTex, rt);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;

                    Texture2D temp2D = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
                    temp2D.ReadPixels(new Rect(0, 0, srcTex.width, srcTex.height), 0, 0);
                    temp2D.Apply();

                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);

                    Color[] pixels = temp2D.GetPixels();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color c = pixels[i];
                        // Grayscale luminance
                        float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                        // Tint towards target palette while preserving alpha & shading depth
                        pixels[i] = new Color(
                            Mathf.Lerp(c.r, lum * targetTint.r * 2.2f, 0.70f),
                            Mathf.Lerp(c.g, lum * targetTint.g * 2.2f, 0.70f),
                            Mathf.Lerp(c.b, lum * targetTint.b * 2.2f, 0.70f),
                            c.a
                        );
                    }
                    temp2D.SetPixels(pixels);
                    temp2D.Apply();

                    byte[] pngBytes = temp2D.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(temp2D);

                    File.WriteAllBytes(destPath, pngBytes);
                    AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);
                    destTex = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RaimonMaterialUnifier] Could not remap texture {srcPath}: {ex.Message}");
                }
            }

            if (destTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", destTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", destTex);
            }
        }

        private static bool EnsureTextureReadable(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                return true;
            }
            return false;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
