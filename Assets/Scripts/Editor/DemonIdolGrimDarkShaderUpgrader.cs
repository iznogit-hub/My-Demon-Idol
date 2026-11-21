#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.VFX;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Automated material upgrader for College Raimon assets:
    /// Replaces standard/flat shadergraphs with high-fidelity URP Grim Dark shaders:
    /// - PBR_OPAQUE_Double -> CollegeRaimon/GrimDark_Opaque
    /// - PBR_MASK_Double   -> CollegeRaimon/GrimDark_Mask
    /// - PBR_BLEND_Front  -> CollegeRaimon/GrimDark_Blend
    /// Configures eerie horror glow for light/lantern/window materials,
    /// and configures GrimDarkStormDirector on ST07_CollegeRaimon.prefab.
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolGrimDarkShaderUpgrader
    {
        private const string MATERIALS_DIR = "Assets/CollegeRaimon/Materials";
        private const string ST07_PREFAB_PATH = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab";
        private const string TRIGGER_FILE = "Temp/run_grimdark_upgrade.txt";

        static DemonIdolGrimDarkShaderUpgrader()
        {
            EditorApplication.update += CheckPendingTrigger;
        }

        private static void CheckPendingTrigger()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                UpgradeAllCollegeRaimonMaterials();
            }
        }

        public static void TriggerUpgrade()
        {
            File.WriteAllText(TRIGGER_FILE, "run");
            AssetDatabase.Refresh();
        }

        [MenuItem("Demon Idol/Stages/Upgrade College Raimon to Grim Dark Shaders")]
        public static void UpgradeAllCollegeRaimonMaterials()
        {
            Shader opaqueShader = Shader.Find("CollegeRaimon/GrimDark_Opaque");
            Shader maskShader = Shader.Find("CollegeRaimon/GrimDark_Mask");
            Shader blendShader = Shader.Find("CollegeRaimon/GrimDark_Blend");

            if (opaqueShader == null || maskShader == null || blendShader == null)
            {
                Debug.LogError($"[GrimDark Upgrader] Could not find Grim Dark shaders! Opaque={opaqueShader!=null}, Mask={maskShader!=null}, Blend={blendShader!=null}");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_DIR });
            int upgradedCount = 0;
            int lightMaterialCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in matGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string curShaderName = mat.shader != null ? mat.shader.name : "";
                    bool isOpaque = curShaderName.Contains("OPAQUE") || curShaderName.Contains("GrimDark_Opaque") || curShaderName.Contains("Lit");
                    bool isMask = curShaderName.Contains("MASK") || curShaderName.Contains("GrimDark_Mask") || curShaderName.Contains("Cutout");
                    bool isBlend = curShaderName.Contains("BLEND") || curShaderName.Contains("GrimDark_Blend") || curShaderName.Contains("Transparent");

                    // Select matching GrimDark shader
                    Shader targetShader = opaqueShader;
                    if (isMask) targetShader = maskShader;
                    else if (isBlend) targetShader = blendShader;

                    mat.shader = targetShader;

                    // Set default grim dark atmosphere parameters
                    if (mat.HasProperty("_GrimDarkDesat")) mat.SetFloat("_GrimDarkDesat", 0.28f);
                    if (mat.HasProperty("_NightAmbientTint")) mat.SetColor("_NightAmbientTint", new Color(0.58f, 0.65f, 0.82f, 1f));
                    if (mat.HasProperty("_ShadowDarkness")) mat.SetFloat("_ShadowDarkness", 0.75f);
                    if (mat.HasProperty("_Wetness")) mat.SetFloat("_Wetness", 0.85f);
                    if (mat.HasProperty("_PuddleGloss")) mat.SetFloat("_PuddleGloss", 0.92f);
                    if (mat.HasProperty("_PuddleSmoothness")) mat.SetFloat("_PuddleSmoothness", 0.35f);
                    if (mat.HasProperty("_RainRippleStrength")) mat.SetFloat("_RainRippleStrength", 0.35f);
                    if (mat.HasProperty("_RainRippleScale")) mat.SetFloat("_RainRippleScale", 4.5f);
                    if (mat.HasProperty("_RainRippleSpeed")) mat.SetFloat("_RainRippleSpeed", 2.8f);

                    // Eerie window & lamp horror glow
                    string lowerName = mat.name.ToLowerInvariant();
                    if (lowerName.Contains("window"))
                    {
                        // Abandoned midnight academy windows: dark reflective rain-streaked glass
                        if (mat.HasProperty("_HorrorGlowIntensity")) mat.SetFloat("_HorrorGlowIntensity", 0.0f);
                        if (mat.HasProperty("_GrimDarkDesat")) mat.SetFloat("_GrimDarkDesat", 0.92f);
                        if (mat.HasProperty("_NightAmbientTint")) mat.SetColor("_NightAmbientTint", new Color(0.12f, 0.16f, 0.28f, 1f));
                        if (mat.HasProperty("_PuddleGloss")) mat.SetFloat("_PuddleGloss", 0.96f);
                        if (mat.HasProperty("_PuddleSmoothness")) mat.SetFloat("_PuddleSmoothness", 0.85f);
                        if (mat.HasProperty("_BV_BaseColor")) mat.SetColor("_BV_BaseColor", new Color(0.14f, 0.18f, 0.32f, 1f));
                    }
                    else if (lowerName.Contains("streetlight") || lowerName.Contains("polelight") || lowerName.Contains("lighton") || lowerName.Contains("lantern"))
                    {
                        if (mat.HasProperty("_HorrorGlowIntensity")) mat.SetFloat("_HorrorGlowIntensity", 1.8f);
                        if (mat.HasProperty("_HorrorGlowColor")) mat.SetColor("_HorrorGlowColor", new Color(1.0f, 0.78f, 0.35f, 1.0f));
                        if (mat.HasProperty("_FlickerSpeed")) mat.SetFloat("_FlickerSpeed", 3.8f);
                        lightMaterialCount++;
                    }
                    else
                    {
                        if (mat.HasProperty("_HorrorGlowIntensity")) mat.SetFloat("_HorrorGlowIntensity", 0.0f);
                    }

                    EditorUtility.SetDirty(mat);
                    upgradedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Also attach GrimDarkStormDirector to ST07_CollegeRaimon.prefab
            ConfigureStagePrefab();

            Debug.Log($"<color=#00e676><b>[Grim Dark Shaders] SUCCESS!</b> Upgraded {upgradedCount} materials ({lightMaterialCount} horror lights configured) in {MATERIALS_DIR}.</color>");
        }

        [MenuItem("Demon Idol/Stages/Revert College Raimon Shaders to Original")]
        public static void RevertAllCollegeRaimonMaterials()
        {
            Shader opaqueSG = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CollegeRaimon/Shaders/URP/PBR_OPAQUE_Double.shadergraph");
            Shader maskSG = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CollegeRaimon/Shaders/URP/PBR_MASK_Double.shadergraph");
            Shader blendSG = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CollegeRaimon/Shaders/URP/PBR_BLEND_Front.shadergraph");

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_DIR });
            int revertedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in matGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string curShaderName = mat.shader != null ? mat.shader.name : "";
                    if (curShaderName.Contains("Mask"))
                    {
                        if (maskSG != null) mat.shader = maskSG;
                    }
                    else if (curShaderName.Contains("Blend"))
                    {
                        if (blendSG != null) mat.shader = blendSG;
                    }
                    else
                    {
                        if (opaqueSG != null) mat.shader = opaqueSG;
                    }

                    EditorUtility.SetDirty(mat);
                    revertedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Grim Dark Shaders] Reverted {revertedCount} materials to original shadergraphs.");
        }

        public static void ConfigureStagePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ST07_PREFAB_PATH);
            if (prefab == null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(ST07_PREFAB_PATH);
            try
            {
                var stormDir = root.GetComponent<GrimDarkStormDirector>();
                if (stormDir == null)
                {
                    stormDir = root.AddComponent<GrimDarkStormDirector>();
                }

                SerializedObject so = new SerializedObject(stormDir);

                // Wire Audio Clips
                var rainClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_rain_loop.wav");
                var thunderClose = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_thunder_close.wav");
                var thunderDist = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_thunder_distant.wav");

                var pRainClip = so.FindProperty("_rainLoopClip");
                if (pRainClip != null) pRainClip.objectReferenceValue = rainClip;

                var pThunderClose = so.FindProperty("_thunderCloseClip");
                if (pThunderClose != null) pThunderClose.objectReferenceValue = thunderClose;

                var pThunderDist = so.FindProperty("_thunderDistantClip");
                if (pThunderDist != null) pThunderDist.objectReferenceValue = thunderDist;

                // Wire Lights
                var accentTrans = root.transform.Find("Stage_AccentLight");
                if (accentTrans != null)
                {
                    var pAccent = so.FindProperty("_accentLight");
                    if (pAccent != null) pAccent.objectReferenceValue = accentTrans.GetComponent<Light>();
                }

                // AudioSources
                Transform rainAudioTrans = root.transform.Find("Audio_RainLoop");
                if (rainAudioTrans == null)
                {
                    GameObject rainAudioGo = new GameObject("Audio_RainLoop");
                    rainAudioGo.transform.SetParent(root.transform, false);
                    var src = rainAudioGo.AddComponent<AudioSource>();
                    src.loop = true;
                    src.playOnAwake = false;
                    src.clip = rainClip;
                    src.volume = 0.55f;
                    rainAudioTrans = rainAudioGo.transform;
                }
                var pRainSrc = so.FindProperty("_rainAudioSource");
                if (pRainSrc != null) pRainSrc.objectReferenceValue = rainAudioTrans.GetComponent<AudioSource>();

                Transform thunderAudioTrans = root.transform.Find("Audio_Thunder");
                if (thunderAudioTrans == null)
                {
                    GameObject thunderAudioGo = new GameObject("Audio_Thunder");
                    thunderAudioGo.transform.SetParent(root.transform, false);
                    var src = thunderAudioGo.AddComponent<AudioSource>();
                    src.loop = false;
                    src.playOnAwake = false;
                    src.volume = 0.85f;
                    thunderAudioTrans = thunderAudioGo.transform;
                }
                var pThunderSrc = so.FindProperty("_thunderAudioSource");
                if (pThunderSrc != null) pThunderSrc.objectReferenceValue = thunderAudioTrans.GetComponent<AudioSource>();

                // Rain Particle System
                Transform rainVfxTrans = root.transform.Find("VFX_RainDownpour");
                if (rainVfxTrans == null)
                {
                    GameObject rainVfxGo = new GameObject("VFX_RainDownpour");
                    rainVfxGo.transform.SetParent(root.transform, false);
                    rainVfxGo.transform.localPosition = new Vector3(0f, 14f, 0f);

                    var ps = rainVfxGo.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.loop = true;
                    main.playOnAwake = true;
                    main.startLifetime = 0.75f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 28f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
                    main.startColor = new Color(0.85f, 0.92f, 1.0f, 0.45f);
                    main.maxParticles = 2500;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;

                    var emission = ps.emission;
                    emission.rateOverTime = 1200f;

                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(36f, 1f, 16f);

                    var vel = ps.velocityOverLifetime;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(-3.5f);
                    vel.y = new ParticleSystem.MinMaxCurve(-24f);
                    vel.z = new ParticleSystem.MinMaxCurve(-1.5f);

                    var psr = rainVfxGo.GetComponent<ParticleSystemRenderer>();
                    psr.renderMode = ParticleSystemRenderMode.Stretch;
                    psr.velocityScale = 0.04f;
                    psr.lengthScale = 2.5f;

                    rainVfxTrans = rainVfxGo.transform;
                }
                var pRainPs = so.FindProperty("_rainParticleSystem");
                if (pRainPs != null) pRainPs.objectReferenceValue = rainVfxTrans.GetComponent<ParticleSystem>();

                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(root, ST07_PREFAB_PATH);
                Debug.Log($"<color=#81c784>[Grim Dark Shaders] Fully configured GrimDarkStormDirector on {ST07_PREFAB_PATH}</color>");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string GetArtifactDir()
        {
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), 
                ".gemini", "antigravity", "brain", string.Concat("133976a6", "-c7af-", "46db-", "b333-", "cdce0672aa09"));
        }

        [MenuItem("Demon Idol/Stages/Capture Grim Dark Storm Screenshots")]
        public static void CaptureGrimDarkStormScreens()
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != "Assets/Scenes/ArcadeArena.unity")
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ArcadeArena.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            }

            var session = DemonIdol.Launch.LaunchServices.Session;
            session.TrySelectMode("Versus", out _);
            session.BeginMatch("C01", "C02", "ST07");

            DemonIdol.BlazBlue.BBMatchBootstrap.EnsureMatch(false);

            // Apply ST07 zone
            var anchor = UnityEngine.Object.FindAnyObjectByType<DemonIdol.Combat.CampusZoneAnchor>();
            if (anchor != null) anchor.ApplyZone("ST07");

            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            string artifactDir = GetArtifactDir();
            Directory.CreateDirectory("Screenshots");
            Directory.CreateDirectory(artifactDir);

            // Clean up any stray clones
            var cloneList = new System.Collections.Generic.List<GameObject>();
            foreach (var go in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name.Contains("(Clone)") && go.scene.isLoaded) cloneList.Add(go);
            }
            foreach (var go in cloneList) UnityEngine.Object.DestroyImmediate(go);

            // 1. Capture Calm Grim Dark Midnight
            Shader.SetGlobalFloat("_GlobalWetness", 0.88f);
            Shader.SetGlobalFloat("_GlobalLightningIntensity", 0.0f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.035f, 0.050f, 0.095f, 1f);

            var dirLight = GameObject.Find("Directional Light");
            Light keyLight = dirLight != null ? dirLight.GetComponent<Light>() : null;
            if (keyLight != null)
            {
                keyLight.color = new Color(0.38f, 0.45f, 0.70f, 1f);
                keyLight.intensity = 0.95f;
            }

            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var texMidnight = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            texMidnight.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            texMidnight.Apply();

            byte[] bytesMidnight = texMidnight.EncodeToPNG();
            File.WriteAllBytes("Screenshots/college_raimon_grimdark_midnight.png", bytesMidnight);
            File.WriteAllBytes(Path.Combine(artifactDir, "college_raimon_grimdark_midnight.png"), bytesMidnight);

            // 2. Capture Lightning Strike Flash
            Shader.SetGlobalFloat("_GlobalLightningIntensity", 3.8f);
            Shader.SetGlobalColor("_GlobalLightningColor", new Color(0.85f, 0.92f, 1.35f, 1f));
            Shader.SetGlobalVector("_GlobalLightningDirection", new Vector3(0.28f, 0.94f, 0.18f).normalized);

            if (keyLight != null)
            {
                keyLight.intensity = 4.2f;
                keyLight.color = new Color(0.85f, 0.92f, 1.35f, 1f);
            }

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var texLightning = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            texLightning.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            texLightning.Apply();

            byte[] bytesLightning = texLightning.EncodeToPNG();
            File.WriteAllBytes("Screenshots/college_raimon_grimdark_lightning.png", bytesLightning);
            File.WriteAllBytes(Path.Combine(artifactDir, "college_raimon_grimdark_lightning.png"), bytesLightning);

            // Clean up
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(texMidnight);
            UnityEngine.Object.DestroyImmediate(texLightning);

            // Reset lighting
            Shader.SetGlobalFloat("_GlobalLightningIntensity", 0.0f);
            if (keyLight != null)
            {
                keyLight.color = new Color(0.38f, 0.45f, 0.70f, 1f);
                keyLight.intensity = 0.95f;
            }

            Debug.Log("<color=#81c784>[Grim Dark Screenshots] Successfully captured college_raimon_grimdark_midnight.png and college_raimon_grimdark_lightning.png!</color>");
        }
    }
}
#endif
