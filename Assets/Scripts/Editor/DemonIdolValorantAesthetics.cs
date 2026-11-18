#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DemonIdol.EditorTools
{
    /// <summary>
    /// Transforms the visuals of Demon Idol into the competitive, high-clarity anime aesthetic of Valorant.
    /// Configures:
    /// 1. Valorant Post-Processing Profile (Tonemapping Neutral, Selective Emissive Bloom, Punchy Contrast/Saturation, Zero Film Grain, Subtle Optical Edge Dispersion).
    /// 2. Directional Sun & Ambient Sky Lighting calibration for clean, un-washed character silhouettes.
    /// 3. Fine-tuning of all 14 characters with crisp two-tone cel shading, agent contour silhouette highlights (fresnel outline pop), and stylized hair/metal specular sheen.
    /// </summary>
    public static class DemonIdolValorantAesthetics
    {
        private const string MDI_TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";
        private const string MATERIAL_ROOT_DIR = "Assets/MDI_Characters/Materials";
        private const string VALORANT_PROFILE_PATH = "Assets/Settings/ValorantVolumeProfile.asset";
        private const string SUSPENSE_PROFILE_PATH = "Assets/Settings/SuspenseVolumeProfile.asset";

        public struct AgentStyleDef
        {
            public string folder;
            public string codename;
            public Color neonAura;
            public Color hairSheen;
            public Color clothSheen;
            public Color clothTint;
            public float neonIntensity;
        }

        private static readonly Dictionary<string, AgentStyleDef> _agentStyles = new Dictionary<string, AgentStyleDef>(StringComparer.OrdinalIgnoreCase)
        {
            ["Akio"] = new AgentStyleDef
            {
                folder = "Akio",
                codename = "C01 Akio (Void Prodigy)",
                neonAura = new Color(0.55f, 0.20f, 0.95f, 1f),
                hairSheen = new Color(0.60f, 0.35f, 0.95f, 1f),
                clothSheen = new Color(0.35f, 0.25f, 0.60f, 1f),
                clothTint = new Color(0.08f, 0.09f, 0.14f, 1f),
                neonIntensity = 1.6f
            },
            ["Aika"] = new AgentStyleDef
            {
                folder = "Aika",
                codename = "C02 Aika (Cursed Resonance)",
                neonAura = new Color(0.95f, 0.15f, 0.65f, 1f),
                hairSheen = new Color(0.95f, 0.65f, 0.45f, 1f),
                clothSheen = new Color(0.70f, 0.20f, 0.40f, 1f),
                clothTint = new Color(0.12f, 0.11f, 0.13f, 1f),
                neonIntensity = 1.7f
            },
            ["AnimeGirl"] = new AgentStyleDef
            {
                folder = "AnimeGirl",
                codename = "C03 AnimeGirl (Cobalt Brawler)",
                neonAura = new Color(0.15f, 0.55f, 1.00f, 1f),
                hairSheen = new Color(1.00f, 0.75f, 0.85f, 1f),
                clothSheen = new Color(0.30f, 0.50f, 0.85f, 1f),
                clothTint = new Color(0.10f, 0.12f, 0.20f, 1f),
                neonIntensity = 1.8f
            },
            ["UnityChan"] = new AgentStyleDef
            {
                folder = "UnityChan",
                codename = "C04 UnityChan (Limitless Radiance)",
                neonAura = new Color(0.00f, 0.95f, 1.00f, 1f),
                hairSheen = new Color(0.70f, 0.95f, 1.00f, 1f),
                clothSheen = new Color(0.20f, 0.80f, 0.95f, 1f),
                clothTint = new Color(0.07f, 0.07f, 0.10f, 1f),
                neonIntensity = 2.0f
            },
            ["Akane"] = new AgentStyleDef
            {
                folder = "Akane",
                codename = "C05 Akane (Gilded Resonance)",
                neonAura = new Color(1.00f, 0.82f, 0.25f, 1f),
                hairSheen = new Color(1.00f, 0.90f, 0.60f, 1f),
                clothSheen = new Color(0.85f, 0.70f, 0.30f, 1f),
                clothTint = new Color(0.11f, 0.11f, 0.13f, 1f),
                neonIntensity = 1.7f
            },
            ["Cultist"] = new AgentStyleDef
            {
                folder = "Cultist",
                codename = "C06 Cultist (Toxic Controller)",
                neonAura = new Color(0.10f, 0.95f, 0.40f, 1f),
                hairSheen = new Color(0.20f, 0.85f, 0.50f, 1f),
                clothSheen = new Color(0.15f, 0.60f, 0.35f, 1f),
                clothTint = new Color(0.09f, 0.09f, 0.11f, 1f),
                neonIntensity = 1.7f
            },
            ["StellarWitch"] = new AgentStyleDef
            {
                folder = "StellarWitch",
                codename = "C07 StellarWitch (Radiant Sentinel)",
                neonAura = new Color(0.20f, 1.00f, 0.75f, 1f),
                hairSheen = new Color(0.75f, 0.55f, 0.35f, 1f),
                clothSheen = new Color(0.40f, 0.90f, 0.70f, 1f),
                clothTint = new Color(0.88f, 0.90f, 0.92f, 1f),
                neonIntensity = 1.6f
            },
            ["SanpakuChan"] = new AgentStyleDef
            {
                folder = "SanpakuChan",
                codename = "C08 SanpakuChan (Nightmare Initiator)",
                neonAura = new Color(0.70f, 0.35f, 1.00f, 1f),
                hairSheen = new Color(0.90f, 0.88f, 0.98f, 1f),
                clothSheen = new Color(0.60f, 0.40f, 0.80f, 1f),
                clothTint = new Color(0.10f, 0.09f, 0.12f, 1f),
                neonIntensity = 1.6f
            },
            ["PicoChan"] = new AgentStyleDef
            {
                folder = "PicoChan",
                codename = "C09 PicoChan (Tech Radianite)",
                neonAura = new Color(1.00f, 0.65f, 0.05f, 1f),
                hairSheen = new Color(1.00f, 0.85f, 0.40f, 1f),
                clothSheen = new Color(0.90f, 0.70f, 0.20f, 1f),
                clothTint = new Color(0.14f, 0.14f, 0.16f, 1f),
                neonIntensity = 1.9f
            },
            ["Kiki"] = new AgentStyleDef
            {
                folder = "Kiki",
                codename = "C10 Kiki (Arctic Shinigami)",
                neonAura = new Color(0.55f, 0.65f, 1.00f, 1f),
                hairSheen = new Color(0.60f, 0.75f, 1.00f, 1f),
                clothSheen = new Color(0.40f, 0.55f, 0.85f, 1f),
                clothTint = new Color(0.08f, 0.08f, 0.10f, 1f),
                neonIntensity = 1.7f
            },
            ["Casual1"] = new AgentStyleDef
            {
                folder = "Casual1",
                codename = "C11 Casual1 (Jackpot Neon)",
                neonAura = new Color(0.05f, 0.95f, 0.85f, 1f),
                hairSheen = new Color(0.95f, 0.95f, 0.75f, 1f),
                clothSheen = new Color(0.20f, 0.80f, 0.75f, 1f),
                clothTint = new Color(0.12f, 0.12f, 0.15f, 1f),
                neonIntensity = 1.8f
            },
            ["Underwear1"] = new AgentStyleDef
            {
                folder = "Underwear1",
                codename = "C12 Underwear1 (Steel Hunter)",
                neonAura = new Color(0.15f, 0.85f, 0.55f, 1f),
                hairSheen = new Color(0.35f, 0.80f, 0.50f, 1f),
                clothSheen = new Color(0.25f, 0.70f, 0.50f, 1f),
                clothTint = new Color(0.11f, 0.11f, 0.13f, 1f),
                neonIntensity = 1.6f
            },
            ["SapphiArtchan"] = new AgentStyleDef
            {
                folder = "SapphiArtchan",
                codename = "C13 SapphiArtchan (Sakura Energizer)",
                neonAura = new Color(1.00f, 0.35f, 0.65f, 1f),
                hairSheen = new Color(1.00f, 0.85f, 0.92f, 1f),
                clothSheen = new Color(0.85f, 0.40f, 0.60f, 1f),
                clothTint = new Color(0.14f, 0.12f, 0.15f, 1f),
                neonIntensity = 1.8f
            },
            ["Muryotaisu"] = new AgentStyleDef
            {
                folder = "Muryotaisu",
                codename = "C14 Muryotaisu (Malevolent King)",
                neonAura = new Color(1.00f, 0.08f, 0.15f, 1f),
                hairSheen = new Color(0.95f, 0.25f, 0.30f, 1f),
                clothSheen = new Color(0.75f, 0.15f, 0.20f, 1f),
                clothTint = new Color(0.09f, 0.08f, 0.09f, 1f),
                neonIntensity = 2.0f
            }
        };

        [InitializeOnLoadMethod]
        private static void AutoApplyOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool("DemonIdol_ValorantApplied_v1", false)) return;
                SessionState.SetBool("DemonIdol_ValorantApplied_v1", true);
                ApplyValorantAesthetics();
            };
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Aesthetics/Apply Valorant Post-Processing & Fine-Tune Characters", false, 1)]
        public static void ApplyValorantAesthetics()
        {
            Debug.Log("==================================================================");
            Debug.Log("<color=#00e676>[DemonIdolValorant] Applying Valorant Visual Transformation...</color>");

            // 1. Configure the Valorant Post-Processing Profiles
            VolumeProfile valorantProfile = ConfigureValorantProfile(VALORANT_PROFILE_PATH);
            ConfigureValorantProfile(SUSPENSE_PROFILE_PATH); // Keep suspense profile aligned

            // 2. Open Home Scene
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.path.Contains("Home.unity"))
            {
                string homePath = "Assets/Scenes/Home.unity";
                if (File.Exists(homePath))
                {
                    activeScene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);
                }
            }

            // 3. Bind Profile to Global Volume in Scene
            BindSceneGlobalVolume(activeScene, valorantProfile);

            // 4. Calibrate Scene Lighting for Valorant Competitive Clarity
            CalibrateSceneLighting(activeScene);

            // 5. Fine-Tune All 14 Character Materials
            FineTuneAllCharacterMaterials();

            // 6. Save and Refresh Assets
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CaptureGameViewWithPostProcessing();

            Debug.Log("<color=#00e676>[DemonIdolValorant] Valorant Post-Processing & Character Fine-Tuning Complete!</color>");
            Debug.Log("==================================================================");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Aesthetics/Capture Game View With Post-Processing", false, 3)]
        public static void CaptureGameViewWithPostProcessing()
        {
            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshot_valorant_postprocessed.png");
            ScreenCapture.CaptureScreenshot(outPath);
            Debug.Log($"<color=#00e676>[DemonIdolValorant] ScreenCapture requested to {outPath}</color>");
        }

        public static void ApplyValorantAestheticsBatch()
        {
            try
            {
                ApplyValorantAesthetics();
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DemonIdolValorant] Batch execution failed: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static VolumeProfile ConfigureValorantProfile(string profilePath)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = Path.GetFileNameWithoutExtension(profilePath);
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            // 1. Tonemapping: Neutral (Crucial for Valorant vibrant dynamic range without blowing out)
            Tonemapping tm;
            if (!profile.TryGet<Tonemapping>(out tm))
            {
                tm = profile.Add<Tonemapping>(true);
                tm.name = "Tonemapping";
                AssetDatabase.AddObjectToAsset(tm, profile);
            }
            tm.active = true;
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.Neutral;

            // 2. Color Adjustments: Punchy contrast, saturated vibrant hues, crisp exposure
            ColorAdjustments cadj;
            if (!profile.TryGet<ColorAdjustments>(out cadj))
            {
                cadj = profile.Add<ColorAdjustments>(true);
                cadj.name = "ColorAdjustments";
                AssetDatabase.AddObjectToAsset(cadj, profile);
            }
            cadj.active = true;
            cadj.postExposure.overrideState = true;
            cadj.postExposure.value = 0.18f;
            cadj.contrast.overrideState = true;
            cadj.contrast.value = 24.0f;
            cadj.saturation.overrideState = true;
            cadj.saturation.value = 18.0f; // Valorant signature vibrant saturated colors!
            cadj.colorFilter.overrideState = true;
            cadj.colorFilter.value = Color.white;

            // 3. Bloom: Strictly selective! High threshold so only emissive neon/abilities bloom cleanly
            Bloom bl;
            if (!profile.TryGet<Bloom>(out bl))
            {
                bl = profile.Add<Bloom>(true);
                bl.name = "Bloom";
                AssetDatabase.AddObjectToAsset(bl, profile);
            }
            bl.active = true;
            bl.threshold.overrideState = true;
            bl.threshold.value = 1.10f; // Threshold > 1.0 guarantees standard lit textures NEVER fog or blur!
            bl.intensity.overrideState = true;
            bl.intensity.value = 0.95f;
            bl.scatter.overrideState = true;
            bl.scatter.value = 0.68f;
            bl.highQualityFiltering.overrideState = true;
            bl.highQualityFiltering.value = true;

            // 4. Chromatic Aberration: Clean subtle optical edge refraction (not extreme distortion)
            ChromaticAberration ca;
            if (!profile.TryGet<ChromaticAberration>(out ca))
            {
                ca = profile.Add<ChromaticAberration>(true);
                ca.name = "ChromaticAberration";
                AssetDatabase.AddObjectToAsset(ca, profile);
            }
            ca.active = true;
            ca.intensity.overrideState = true;
            ca.intensity.value = 0.18f;

            // 5. Vignette: Gentle framing that maintains 100% peripheral combat readability
            Vignette vig;
            if (!profile.TryGet<Vignette>(out vig))
            {
                vig = profile.Add<Vignette>(true);
                vig.name = "Vignette";
                AssetDatabase.AddObjectToAsset(vig, profile);
            }
            vig.active = true;
            vig.color.overrideState = true;
            vig.color.value = Color.black;
            vig.intensity.overrideState = true;
            vig.intensity.value = 0.20f;
            vig.smoothness.overrideState = true;
            vig.smoothness.value = 0.40f;

            // 6. Film Grain: Zero! Valorant has zero grain noise for competitive clarity
            FilmGrain gr;
            if (profile.TryGet<FilmGrain>(out gr))
            {
                gr.active = false;
                gr.intensity.overrideState = true;
                gr.intensity.value = 0.0f;
            }

            // 7. Motion Blur: Disabled for 60 FPS combat readability
            MotionBlur mb;
            if (profile.TryGet<MotionBlur>(out mb))
            {
                mb.active = false;
                mb.intensity.overrideState = true;
                mb.intensity.value = 0.0f;
            }

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void BindSceneGlobalVolume(Scene scene, VolumeProfile profile)
        {
            Volume globalVol = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Volume v = root.GetComponentInChildren<Volume>(true);
                if (v != null && v.isGlobal)
                {
                    globalVol = v;
                    break;
                }
            }

            if (globalVol == null)
            {
                GameObject vGo = new GameObject("Global Volume");
                globalVol = vGo.AddComponent<Volume>();
                globalVol.isGlobal = true;
            }

            globalVol.sharedProfile = profile;
            globalVol.weight = 1.0f;
            EditorUtility.SetDirty(globalVol);
            Debug.Log($"[DemonIdolValorant] Bound '{profile.name}' to Global Volume '{globalVol.gameObject.name}'.");
        }

        private static void CalibrateSceneLighting(Scene scene)
        {
            // Calibrate Main Directional Light
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        l.intensity = 1.55f; // Balanced sunlight intensity (replaces overexposed 7.61)
                        l.color = new Color(1.0f, 0.96f, 0.90f, 1.0f); // Warm sun key light
                        l.shadows = LightShadows.Soft;
                        l.shadowStrength = 0.75f;
                        l.shadowNormalBias = 0.25f;
                        l.shadowBias = 0.03f;
                        EditorUtility.SetDirty(l);
                        Debug.Log($"[DemonIdolValorant] Calibrated Directional Light '{l.gameObject.name}' to 1.55 intensity, soft shadows.");
                    }
                }
            }

            // Calibrate Ambient Sky Bounce Lighting (Trilight / Gradient)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.72f, 0.85f, 1.0f);     // Soft cool sky fill
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.48f, 0.55f, 1.0f); // Neutral horizon fill
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.28f, 0.32f, 1.0f);  // Crisp dark ground contact
            RenderSettings.ambientIntensity = 1.15f;
        }

        private static void FineTuneAllCharacterMaterials()
        {
            int totalTuned = 0;

            foreach (var kvp in _agentStyles)
            {
                string folder = kvp.Key;
                AgentStyleDef style = kvp.Value;
                string dirPath = $"{MATERIAL_ROOT_DIR}/{folder}";
                if (!Directory.Exists(dirPath)) continue;

                string[] matFiles = Directory.GetFiles(dirPath, "*.mat", SearchOption.AllDirectories);
                foreach (string f in matFiles)
                {
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(f);
                    if (mat == null || mat.shader == null || mat.shader.name != MDI_TOON_SHADER_NAME) continue;

                    string lowerName = mat.name.ToLowerInvariant();

                    // 1. Crisp Valorant Two-Tone Cel Shading
                    mat.SetFloat("_RampThreshold1", 0.46f);
                    mat.SetFloat("_RampThreshold2", 0.76f);
                    mat.SetFloat("_RampSmoothness", 0.018f); // Sharp stepped cel threshold
                    mat.SetColor("_ShadowColor", new Color(0.58f, 0.55f, 0.68f, 1.0f)); // Clean cool slate shadow

                    // 2. Valorant Agent Contour & Silhouette Pop (colored enemy/ally glancing fresnel highlight)
                    mat.SetFloat("_ChromaticEdgePower", 5.2f); // Sharp exponent hugs outermost perimeter
                    mat.SetFloat("_ChromaticEdgeThreshold", 0.22f);
                    mat.SetFloat("_ChromaticEdgeIntensity", 1.6f);
                    mat.SetColor("_NeonColor", style.neonAura);
                    mat.SetFloat("_NeonIntensity", style.neonIntensity);

                    // Strictly disable white outline and white rim
                    mat.SetFloat("_EnableOutline", 0f);
                    mat.SetFloat("_OutlineWidth", 0f);
                    mat.SetColor("_OutlineColor", Color.clear);
                    mat.SetColor("_RimColor", Color.clear);

                    // 3. Valorant Stylized Specular Sheen (Hair, Metals, Accessories vs Matte Skin/Cloth)
                    if (lowerName.Contains("hair") || lowerName.Contains("hairback"))
                    {
                        // Saturated anime hair sheen
                        mat.SetColor("_SpecColor", style.hairSheen);
                        mat.SetFloat("_SpecIntensity", 2.2f);
                        mat.SetFloat("_SpecPower", 36.0f);
                        mat.SetFloat("_SpecThreshold", 0.45f);
                    }
                    else if (lowerName.Contains("metal") || lowerName.Contains("weapon") || lowerName.Contains("gear") || lowerName.Contains("shoe") || lowerName.Contains("pico_body"))
                    {
                        // Crisp metallic specular highlight
                        mat.SetColor("_SpecColor", Color.white);
                        mat.SetFloat("_SpecIntensity", 1.8f);
                        mat.SetFloat("_SpecPower", 48.0f);
                        mat.SetFloat("_SpecThreshold", 0.50f);
                    }
                    else if (lowerName.Contains("eye") || lowerName.Contains("iris") || lowerName.Contains("pupil"))
                    {
                        // Eye shine reflection
                        mat.SetColor("_SpecColor", Color.white);
                        mat.SetFloat("_SpecIntensity", 2.5f);
                        mat.SetFloat("_SpecPower", 64.0f);
                        mat.SetFloat("_SpecThreshold", 0.50f);
                    }
                    else if (lowerName.Contains("skin") || lowerName.Contains("face") || lowerName.Contains("body") || lowerName.Contains("mouth") || lowerName.Contains("brow") || lowerName.Contains("eyeline") || lowerName.Contains("cheek"))
                    {
                        // Soft, silky anime skin (zero plastic specular sheen)
                        mat.SetFloat("_SpecIntensity", 0.0f);
                        mat.SetColor("_SkinTint", Color.white);
                    }
                    else
                    {
                        // Stylized techwear cloth (subtle structured fabric sheen)
                        mat.SetColor("_SpecColor", style.clothSheen);
                        mat.SetFloat("_SpecIntensity", 0.35f);
                        mat.SetFloat("_SpecPower", 20.0f);
                        mat.SetFloat("_SpecThreshold", 0.55f);
                    }

                    EditorUtility.SetDirty(mat);
                    totalTuned++;
                }
            }

            Debug.Log($"[DemonIdolValorant] Successfully fine-tuned {totalTuned} materials across all 14 characters for Valorant aesthetics.");
        }
    }
}
#endif
