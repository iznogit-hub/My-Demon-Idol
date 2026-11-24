#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DemonIdol.Combat;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Automated 25-Fighter Portrait & Super Cut-In Generator.
    /// Renders high-res 2D bust portraits directly from 3D prefabs with Sumi-e ink framing,
    /// configures Sprite asset import settings, and assigns CharacterDefinition.portraitSprite
    /// across all 25 roster fighters.
    /// </summary>
    public static class DemonIdolPortraitGenerator
    {
        private const int PORTRAIT_SIZE = 512;
        private const string PORTRAITS_DIR = "Assets/Resources/Portraits";
        private const string MUGSHOTS_DIR = "Assets/Resources/UI/Portraits/Mugshots";

        public static readonly string[] RosterIds = new string[]
        {
            "C01", "C02", "C04", "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12", "C13", "C14",
            "SK02", "SK03", "SK05", "SK06", "SK08",
            "EX01", "EX02", "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        public static void GenerateAllPortraits()
        {
            if (!Directory.Exists(PORTRAITS_DIR)) Directory.CreateDirectory(PORTRAITS_DIR);
            if (!Directory.Exists(MUGSHOTS_DIR)) Directory.CreateDirectory(MUGSHOTS_DIR);

            string[] rosterIds = RosterIds;
            int generatedCount = 0;

            try
            {
                for (int i = 0; i < rosterIds.Length; i++)
                {
                    string charId = rosterIds[i];
                    EditorUtility.DisplayProgressBar("Generating 25-Fighter Portraits", $"Rendering {charId} ({i + 1}/{rosterIds.Length})...", (float)i / rosterIds.Length);

                    CharacterDefinition def = FindCharacterDefinition(charId);
                    if (def == null)
                    {
                        Debug.LogWarning($"[PortraitGenerator] No CharacterDefinition found for {charId}. Skipping.");
                        continue;
                    }

                    if (GenerateAndAssignPortraitForFighter(def) != null)
                    {
                        generatedCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#00e676>[PortraitGenerator] Successfully generated and configured {generatedCount}/25 fighter portraits in {PORTRAITS_DIR} and {MUGSHOTS_DIR}!</color>");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Renders and configures a bust portrait and mugshot for a single character definition,
        /// optionally using an active model instance/prefab.
        /// </summary>
        public static Sprite GenerateAndAssignPortraitForFighter(CharacterDefinition def, GameObject customModelPrefab = null)
        {
            if (def == null) return null;
            if (!Directory.Exists(PORTRAITS_DIR)) Directory.CreateDirectory(PORTRAITS_DIR);
            if (!Directory.Exists(MUGSHOTS_DIR)) Directory.CreateDirectory(MUGSHOTS_DIR);

            GameObject modelToRender = customModelPrefab != null ? customModelPrefab : def.modelPrefab;
            Texture2D portraitTex = null;

            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && modelToRender != null)
            {
                try
                {
                    portraitTex = Render3DModelBust(modelToRender);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PortraitGenerator] 3D render failed for {def.id} ({ex.Message}), generating procedural Sumi-e portrait fallback.");
                }
            }

            if (portraitTex == null)
            {
                portraitTex = GenerateSumiEProceduralPortrait(def);
            }

            if (portraitTex == null) return null;

            byte[] pngBytes = portraitTex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(portraitTex);

            string portraitPath = $"{PORTRAITS_DIR}/Portrait_{def.id}_{def.displayName}.png";
            string mugshotPath = $"{MUGSHOTS_DIR}/Mugshot_{def.id}_{def.displayName}.png";

            File.WriteAllBytes(portraitPath, pngBytes);
            File.WriteAllBytes(mugshotPath, pngBytes);

            ConfigureSpriteImporter(portraitPath);
            ConfigureSpriteImporter(mugshotPath);

            AssetDatabase.ImportAsset(portraitPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(mugshotPath, ImportAssetOptions.ForceUpdate);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            if (sprite != null)
            {
                Undo.RecordObject(def, "Update Fighter Portrait");
                def.portraitSprite = sprite;
                EditorUtility.SetDirty(def);
            }

            return sprite;
        }

        private static CharacterDefinition FindCharacterDefinition(string charId)
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (def != null && string.Equals(def.id, charId, StringComparison.OrdinalIgnoreCase))
                {
                    return def;
                }
            }
            return null;
        }

        private static Texture2D RenderFighterPortrait(CharacterDefinition def)
        {
            // If graphics hardware is available and modelPrefab exists, render 3D bust directly
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && def.modelPrefab != null)
            {
                try
                {
                    return Render3DModelBust(def.modelPrefab);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PortraitGenerator] 3D render failed for {def.id} ({ex.Message}), generating procedural Sumi-e portrait fallback.");
                }
            }

            // High-resolution procedural Sumi-e calligraphic portrait
            return GenerateSumiEProceduralPortrait(def);
        }

        private static Texture2D Render3DModelBust(GameObject modelPrefab)
        {
            Vector3 stagePos = new Vector3(0, 1000, 0);
            GameObject root = new GameObject("Temp_PortraitStage");
            root.transform.position = stagePos;

            GameObject model = null;
            Camera cam = null;
            Light keyLight = null;
            Light rimLight = null;
            RenderTexture rt = null;

            try
            {
                model = UnityEngine.Object.Instantiate(modelPrefab, stagePos, Quaternion.Euler(0, 168, 0), root.transform);

                // Initialize weapon rig if present
                var rig = model.GetComponent<BBWeaponRig>();
                if (rig != null) rig.Initialize();

                // Locate head / bust height
                Vector3 headPos = stagePos + new Vector3(0, 1.55f, 0);
                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null && anim.isHuman)
                {
                    Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
                    if (head != null) headPos = head.position;
                }
                else
                {
                    var renderers = model.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds b = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                        headPos = new Vector3(b.center.x, b.min.y + b.size.y * 0.86f, b.center.z);
                    }
                }

                // Setup Camera
                GameObject camGo = new GameObject("Temp_PortraitCam");
                camGo.transform.SetParent(root.transform);
                cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.clear;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 15f;
                cam.fieldOfView = 22f;

                camGo.transform.position = headPos + new Vector3(0.06f, 0.02f, 0.78f);
                camGo.transform.LookAt(headPos - new Vector3(0, 0.04f, 0));

                // Setup Key & Rim Lights
                GameObject keyGo = new GameObject("Temp_KeyLight");
                keyGo.transform.SetParent(root.transform);
                keyLight = keyGo.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(1.0f, 0.96f, 0.90f);
                keyLight.intensity = 1.35f;
                keyGo.transform.rotation = Quaternion.Euler(30, 210, 0);

                GameObject rimGo = new GameObject("Temp_RimLight");
                rimGo.transform.SetParent(root.transform);
                rimLight = rimGo.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = new Color(0.80f, 0.90f, 1.0f);
                rimLight.intensity = 1.60f;
                rimGo.transform.rotation = Quaternion.Euler(15, 30, 0);

                // Render into RenderTexture
                rt = new RenderTexture(PORTRAIT_SIZE, PORTRAIT_SIZE, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(PORTRAIT_SIZE, PORTRAIT_SIZE, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, PORTRAIT_SIZE, PORTRAIT_SIZE), 0, 0);

                // Apply Sumi-e soft circular vignette mask
                ApplySumiEVignette(tex);
                tex.Apply();
                return tex;
            }
            finally
            {
                RenderTexture.active = null;
                if (rt != null) rt.Release();
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ApplySumiEVignette(Texture2D tex)
        {
            float half = PORTRAIT_SIZE * 0.5f;
            Color[] pixels = tex.GetPixels();

            for (int y = 0; y < PORTRAIT_SIZE; y++)
            {
                for (int x = 0; x < PORTRAIT_SIZE; x++)
                {
                    int idx = y * PORTRAIT_SIZE + x;
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Fade alpha to 0 beyond 0.85 radius
                    if (dist > 0.82f)
                    {
                        float alphaMult = Mathf.SmoothStep(1.0f, 0.0f, (dist - 0.82f) / 0.16f);
                        pixels[idx].a *= alphaMult;
                    }
                }
            }
            tex.SetPixels(pixels);
        }

        private static Texture2D GenerateSumiEProceduralPortrait(CharacterDefinition def)
        {
            Texture2D tex = new Texture2D(PORTRAIT_SIZE, PORTRAIT_SIZE, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[PORTRAIT_SIZE * PORTRAIT_SIZE];

            float half = PORTRAIT_SIZE * 0.5f;
            Color carbonInk = new Color(0.10f, 0.10f, 0.12f, 0.95f);
            Color vermilion = new Color(0.76f, 0.23f, 0.13f, 1.0f);
            Color parryGold = new Color(0.83f, 0.69f, 0.22f, 1.0f);

            // Determine accent color from archetype
            var arch = BBRosterArchetype.Get(def.id);
            Color accentColor = vermilion;
            if (arch.archetypeClass == BBArchetypeClass.Zoner) accentColor = new Color(0.20f, 0.60f, 0.85f, 1.0f);
            else if (arch.archetypeClass == BBArchetypeClass.Grappler) accentColor = new Color(0.85f, 0.45f, 0.10f, 1.0f);
            else if (arch.archetypeClass == BBArchetypeClass.Swordsman) accentColor = parryGold;

            for (int y = 0; y < PORTRAIT_SIZE; y++)
            {
                for (int x = 0; x < PORTRAIT_SIZE; x++)
                {
                    int idx = y * PORTRAIT_SIZE + x;
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 0.96f)
                    {
                        pixels[idx] = Color.clear;
                    }
                    else if (dist >= 0.91f && dist <= 0.96f)
                    {
                        // Outer gold/accent rim ring
                        float edgeAlpha = Mathf.SmoothStep(0f, 1f, (0.96f - dist) / 0.05f);
                        pixels[idx] = new Color(accentColor.r, accentColor.g, accentColor.b, edgeAlpha);
                    }
                    else if (dist >= 0.86f && dist < 0.91f)
                    {
                        // Inner dark divider
                        pixels[idx] = new Color(0.05f, 0.05f, 0.07f, 0.95f);
                    }
                    else
                    {
                        // Inner Sumi-e gradient wash
                        float verticalGradient = (float)y / PORTRAIT_SIZE;
                        Color wash = Color.Lerp(carbonInk, new Color(carbonInk.r + accentColor.r * 0.15f, carbonInk.g + accentColor.g * 0.15f, carbonInk.b + accentColor.b * 0.15f), verticalGradient);
                        pixels[idx] = wash;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = PORTRAIT_SIZE;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
        }
    }
}
#endif
