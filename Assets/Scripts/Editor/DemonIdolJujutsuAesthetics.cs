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
    public static class DemonIdolJujutsuAesthetics
    {
        private const string MDI_TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";
        private const string MATERIAL_ROOT_DIR = "Assets/MDI_Characters/Materials";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Characters/Apply Suspense Chromatic Aberration & Remove Outlines", false, 11)]
        public static void ApplySuspenseChromaticAberration()
        {
            Debug.Log("==================================================================");
            Debug.Log("[DemonIdolSuspense] Converting characters from white outlines to suspense chromatic aberration...");

            // 1. Update all character materials to remove white outlines and enable chromatic edge dispersion
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/MDI_Characters", "Assets/HairPack", "Assets/Kiki" });
            int matsUpdated = 0;
            foreach (string guid in matGuids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || m.shader == null || m.shader.name != MDI_TOON_SHADER_NAME) continue;

                // Completely disable white/bright outlines
                m.SetFloat("_EnableOutline", 0f);
                m.SetFloat("_OutlineWidth", 0f);
                m.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.08f, 0f));
                m.SetColor("_RimColor", Color.clear); // Removes white rim outlines!

                // Enable suspenseful chromatic aberration edge dispersion
                m.SetFloat("_ChromaticEdgeIntensity", 2.2f);
                m.SetFloat("_ChromaticEdgePower", 3.2f);
                m.SetFloat("_ChromaticEdgeThreshold", 0.28f);

                EditorUtility.SetDirty(m);
                matsUpdated++;
            }
            Debug.Log($"[DemonIdolSuspense] Updated {matsUpdated} materials (disabled white outlines, enabled chromatic edge dispersion).");

            // 2. Configure Home scene Volume and Camera Post-Processing
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.path.Contains("Home.unity"))
            {
                string homePath = "Assets/Scenes/Home.unity";
                if (File.Exists(homePath))
                {
                    activeScene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);
                }
            }

            // Create or load SuspenseVolumeProfile
            string profilePath = "Assets/Settings/SuspenseVolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "SuspenseVolumeProfile";

                // Configure Chromatic Aberration (suspense lens dispersion)
                var chromaticAberration = profile.Add<ChromaticAberration>(true);
                chromaticAberration.active = true;
                chromaticAberration.intensity.overrideState = true;
                chromaticAberration.intensity.value = 0.70f;

                // Configure Vignette (dark suspense framing)
                var vignette = profile.Add<Vignette>(true);
                vignette.active = true;
                vignette.color.overrideState = true;
                vignette.color.value = Color.black;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.42f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.35f;

                // Configure Color Adjustments (high-contrast suspenseful dark mood)
                var colorAdj = profile.Add<ColorAdjustments>(true);
                colorAdj.active = true;
                colorAdj.contrast.overrideState = true;
                colorAdj.contrast.value = 22f;
                colorAdj.saturation.overrideState = true;
                colorAdj.saturation.value = -6f;
                colorAdj.postExposure.overrideState = true;
                colorAdj.postExposure.value = 0.15f;

                // Configure Bloom (gives cursed energy neon auras an eerie atmospheric bleed)
                var bloom = profile.Add<Bloom>(true);
                bloom.active = true;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.85f;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 1.25f;

                // Configure Film Grain (cinematic anime suspense texture)
                var grain = profile.Add<FilmGrain>(true);
                grain.active = true;
                grain.type.overrideState = true;
                grain.type.value = FilmGrainLookup.Thin1;
                grain.intensity.overrideState = true;
                grain.intensity.value = 0.25f;
                grain.response.overrideState = true;
                grain.response.value = 0.8f;

                AssetDatabase.CreateAsset(profile, profilePath);
                foreach (var item in profile.components)
                {
                    if (item != null)
                    {
                        item.name = item.GetType().Name;
                        AssetDatabase.AddObjectToAsset(item, profile);
                    }
                }
            }
            else
            {
                // Update existing profile components
                ChromaticAberration ca;
                if (!profile.TryGet<ChromaticAberration>(out ca))
                {
                    ca = profile.Add<ChromaticAberration>(true);
                    ca.name = "ChromaticAberration";
                    AssetDatabase.AddObjectToAsset(ca, profile);
                }
                ca.active = true;
                ca.intensity.overrideState = true;
                ca.intensity.value = 0.70f;

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
                vig.intensity.value = 0.42f;
                vig.smoothness.overrideState = true;
                vig.smoothness.value = 0.35f;

                ColorAdjustments cadj;
                if (!profile.TryGet<ColorAdjustments>(out cadj))
                {
                    cadj = profile.Add<ColorAdjustments>(true);
                    cadj.name = "ColorAdjustments";
                    AssetDatabase.AddObjectToAsset(cadj, profile);
                }
                cadj.active = true;
                cadj.contrast.overrideState = true;
                cadj.contrast.value = 22f;
                cadj.saturation.overrideState = true;
                cadj.saturation.value = -6f;
                cadj.postExposure.overrideState = true;
                cadj.postExposure.value = 0.15f;

                Bloom bl;
                if (!profile.TryGet<Bloom>(out bl))
                {
                    bl = profile.Add<Bloom>(true);
                    bl.name = "Bloom";
                    AssetDatabase.AddObjectToAsset(bl, profile);
                }
                bl.active = true;
                bl.threshold.overrideState = true;
                bl.threshold.value = 0.85f;
                bl.intensity.overrideState = true;
                bl.intensity.value = 1.25f;

                FilmGrain gr;
                if (!profile.TryGet<FilmGrain>(out gr))
                {
                    gr = profile.Add<FilmGrain>(true);
                    gr.name = "FilmGrain";
                    AssetDatabase.AddObjectToAsset(gr, profile);
                }
                gr.active = true;
                gr.type.overrideState = true;
                gr.type.value = FilmGrainLookup.Thin1;
                gr.intensity.overrideState = true;
                gr.intensity.value = 0.25f;
                gr.response.overrideState = true;
                gr.response.value = 0.8f;
            }

            EditorUtility.SetDirty(profile);

            // 3. Assign to Global Volume in Home.unity
            GameObject[] roots = activeScene.GetRootGameObjects();
            Volume targetVolume = null;
            foreach (GameObject r in roots)
            {
                Volume v = r.GetComponentInChildren<Volume>(true);
                if (v != null)
                {
                    targetVolume = v;
                    break;
                }
            }
            if (targetVolume == null)
            {
                GameObject vGo = new GameObject("Global Volume");
                targetVolume = vGo.AddComponent<Volume>();
            }
            targetVolume.isGlobal = true;
            targetVolume.weight = 1.0f;
            targetVolume.sharedProfile = profile;
            EditorUtility.SetDirty(targetVolume);

            // 4. Enable Post-Processing on Main Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                foreach (GameObject r in roots)
                {
                    Camera c = r.GetComponentInChildren<Camera>(true);
                    if (c != null) { cam = c; break; }
                }
            }
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.043f, 0.051f, 0.078f, 1f); // Deep suspense dark void #0B0D14

                UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
                EditorUtility.SetDirty(camData);
                EditorUtility.SetDirty(cam);
                Debug.Log($"[DemonIdolSuspense] Configured dark suspense backdrop & Post-Processing (SMAA High) on camera '{cam.name}'.");
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[DemonIdolSuspense] Exiting Play Mode to persist scene changes.");
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            AssetDatabase.SaveAssets();

            Debug.Log("[DemonIdolSuspense] Complete! White outlines eliminated, Suspense Chromatic Aberration & occult post-processing active.");
            Debug.Log("==================================================================");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Characters/Verify Jujutsu Aesthetics In Scene", false, 13)]
        public static void VerifyAesthetics()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Debug.Log($"==================================================================");
            Debug.Log($"[DemonIdolVerification] Verifying all 14 characters in '{activeScene.name}'...");

            string[] expectedChars = new string[]
            {
                "Akio", "Aika", "Anime_Girl", "unitychan", "angGirl",
                "Cultist", "StellarWitch", "SanpakuChan", "PicoChan", "Kiki",
                "Casual1", "Underwear1", "SapphiArtchan", "Muryotaisu"
            };

            GameObject[] roots = activeScene.GetRootGameObjects();
            int totalFound = 0;
            int totalMatsValid = 0;
            int totalBroken = 0;

            foreach (string charName in expectedChars)
            {
                GameObject root = FindRoot(roots, charName);
                if (root == null)
                {
                    Debug.LogError($"[Verification] Character '{charName}' NOT found in scene!");
                    continue;
                }

                totalFound++;
                Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
                List<string> attachedHair = new List<string>();
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("MDI_Hair_"))
                        attachedHair.Add(t.name);
                }

                int charMats = 0;
                string uniformSummary = "";

                foreach (Renderer r in rends)
                {
                    if (!r.enabled) continue;
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null)
                        {
                            totalBroken++;
                            continue;
                        }
                        charMats++;
                        if (m.shader != null && m.shader.name == MDI_TOON_SHADER_NAME)
                        {
                            totalMatsValid++;
                            if (m.HasProperty("_UniformDesat") && m.GetFloat("_UniformDesat") > 0.5f)
                            {
                                Color tint = m.GetColor("_UniformTint");
                                Color neon = m.GetColor("_NeonColor");
                                float nint = m.GetFloat("_NeonIntensity");
                                uniformSummary = $"Desat: {m.GetFloat("_UniformDesat"):F2}, Tint: #{ColorUtility.ToHtmlStringRGB(tint)}, Neon: #{ColorUtility.ToHtmlStringRGB(neon)} (x{nint:F1})";
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[Verification] Character '{charName}' has non-MDI material '{m.name}' using shader '{m.shader?.name}'");
                            totalBroken++;
                        }
                    }
                }

                string hairStr = attachedHair.Count > 0 ? string.Join(", ", attachedHair) : "Integrated / Restyled";
                Debug.Log($"[Verification OK] '{root.name}' | Active Mats: {charMats} | Hair: [{hairStr}] | Uniform: {uniformSummary}");
            }

            Debug.Log($"[DemonIdolVerification] Completed: Found {totalFound}/14 characters, {totalMatsValid} verified MDI materials, {totalBroken} broken.");
            Debug.Log("==================================================================");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Characters/Apply Jujutsu & Bleach Aesthetics to All 14 Characters", false, 12)]
        public static void ApplyAesthetics()
        {
            Debug.Log("==================================================================");
            Debug.Log("[DemonIdolJujutsuAesthetics] Applying Jujutsu Kaisen & Bleach aesthetic transformation...");

            Shader mdiShader = Shader.Find(MDI_TOON_SHADER_NAME);
            if (mdiShader == null)
            {
                Debug.LogError($"[DemonIdolJujutsuAesthetics] Shader '{MDI_TOON_SHADER_NAME}' not found! Aborting.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(MATERIAL_ROOT_DIR))
            {
                if (!AssetDatabase.IsValidFolder("Assets/MDI_Characters"))
                    AssetDatabase.CreateFolder("Assets", "MDI_Characters");
                AssetDatabase.CreateFolder("Assets/MDI_Characters", "Materials");
            }

            // Ensure Home scene is open
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.path.Contains("Home.unity"))
            {
                string homePath = "Assets/Scenes/Home.unity";
                if (File.Exists(homePath))
                {
                    activeScene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);
                }
            }

            GameObject[] roots = activeScene.GetRootGameObjects();

            // Transform each of the 14 characters
            TransformAkio(FindRoot(roots, "Akio"), mdiShader);
            TransformAika(FindRoot(roots, "Aika"), mdiShader);
            TransformAnimeGirl(FindRoot(roots, "Anime_Girl"), mdiShader);
            TransformUnityChan(FindRoot(roots, "unitychan"), mdiShader);
            TransformAkane(FindRoot(roots, "angGirl"), mdiShader);
            TransformCultist(FindRoot(roots, "Cultist"), mdiShader);
            TransformStellarWitch(FindRoot(roots, "StellarWitch"), mdiShader);
            TransformSanpakuChan(FindRoot(roots, "SanpakuChan"), mdiShader);
            TransformPicoChan(FindRoot(roots, "PicoChan"), mdiShader);
            TransformKiki(FindRoot(roots, "Kiki"), mdiShader);
            TransformCasual1(FindRoot(roots, "Casual1"), mdiShader);
            TransformUnderwear1(FindRoot(roots, "Underwear1"), mdiShader);
            TransformSapphiArtchan(FindRoot(roots, "SapphiArtchan"), mdiShader);
            TransformMuryotaisu(FindRoot(roots, "Muryotaisu"), mdiShader);

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[DemonIdolSuspense] Exiting Play Mode to persist scene changes.");
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DemonIdolJujutsuAesthetics] All 14 characters transformed with Jujutsu & Bleach aesthetics and unique hairstyles!");
            Debug.Log("==================================================================");
        }

        private static GameObject FindRoot(GameObject[] roots, string partialName)
        {
            string lower = partialName.ToLowerInvariant();
            foreach (GameObject r in roots)
            {
                if (r.name.ToLowerInvariant().Contains(lower))
                    return r;
            }
            return null;
        }

        private static Transform FindHead(GameObject character)
        {
            if (character == null) return null;
            Animator anim = character.GetComponentInChildren<Animator>(true);
            if (anim != null && anim.isHuman)
            {
                try
                {
                    Transform h = anim.GetBoneTransform(HumanBodyBones.Head);
                    if (h != null) return h;
                }
                catch { }
            }

            foreach (Transform t in character.GetComponentsInChildren<Transform>(true))
            {
                string tn = t.name.ToLowerInvariant();
                if ((tn.Contains("head") || tn == "neck" || tn == "character1_head") && !tn.Contains("end") && !tn.Contains("ribbon") && !tn.Contains("hair") && !tn.Contains("locator"))
                    return t;
            }
            return null;
        }

        private static Material GetOrCreateDedicatedMaterial(string charFolder, Material sourceMat, string newMatName, Shader shader)
        {
            string dir = $"{MATERIAL_ROOT_DIR}/{charFolder}";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder(MATERIAL_ROOT_DIR, charFolder);
            }

            string path = $"{dir}/{newMatName}.mat";
            Material targetMat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (targetMat == null)
            {
                if (sourceMat != null)
                {
                    targetMat = new Material(sourceMat);
                }
                else
                {
                    targetMat = new Material(shader);
                }
                targetMat.name = newMatName;
                targetMat.shader = shader;
                AssetDatabase.CreateAsset(targetMat, path);
            }
            else
            {
                targetMat.shader = shader;
            }

            // Sync base map textures if present
            if (sourceMat != null)
            {
                Texture mainTex = sourceMat.GetTexture("_MainTex");
                Texture baseMap = sourceMat.GetTexture("_BaseMap");
                Texture chosenTex = baseMap != null ? baseMap : mainTex;
                if (chosenTex != null)
                {
                    targetMat.SetTexture("_BaseMap", chosenTex);
                    targetMat.SetTexture("_MainTex", chosenTex);
                }
            }

            return targetMat;
        }

        private static GameObject AttachHairPrefab(Transform head, string prefabPath, Material hairMat, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            if (head == null) return null;

            // Remove previous custom hair attachments if any
            for (int i = head.childCount - 1; i >= 0; i--)
            {
                Transform child = head.GetChild(i);
                if (child.name.StartsWith("MDI_Hair_"))
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[DemonIdolJujutsuAesthetics] Hair prefab at '{prefabPath}' not found.");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, head);
            instance.name = "MDI_Hair_" + prefab.name;
            instance.transform.localPosition = localPos;
            instance.transform.localEulerAngles = localRot;
            instance.transform.localScale = localScale;

            if (hairMat != null)
            {
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++) mats[m] = hairMat;
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }

            EditorUtility.SetDirty(instance);
            return instance;
        }

        // =========================================================================================
        // 1. AKIO: Megumi Fushiguro (Shadow Sorcerer)
        // Uniform: Midnight Navy (#151824) | Aura: Shadow Violet (#4D44B5) | Hair: Jet Black Spikes
        // =========================================================================================
        private static void TransformAkio(GameObject go, Shader shader)
        {
            if (go == null) return;
            SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) return;

            Material[] mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string mname = mats[i].name.ToLowerInvariant();
                Material dedicated = GetOrCreateDedicatedMaterial("Akio", mats[i], "Akio_" + mats[i].name, shader);

                if (mname.Contains("cloth") || mname.Contains("bottom"))
                {
                    // Midnight Navy Jujutsu High Uniform
                    dedicated.SetFloat("_UniformDesat", 0.85f);
                    dedicated.SetColor("_UniformTint", new Color(0.082f, 0.094f, 0.141f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.30f, 0.27f, 0.71f, 1f)); // Shadow Violet #4D44B5
                    dedicated.SetFloat("_NeonIntensity", 1.6f);
                }
                else if (mname.Contains("hair"))
                {
                    // Megumi Jet Obsidian Hair
                    dedicated.SetColor("_BaseColor", new Color(0.07f, 0.07f, 0.09f, 1f));
                    dedicated.SetFloat("_UniformDesat", 0.9f);
                    dedicated.SetColor("_UniformTint", new Color(0.07f, 0.07f, 0.09f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.30f, 0.27f, 0.71f, 1f));
                    dedicated.SetFloat("_NeonIntensity", 1.2f);
                }
                else if (mname.Contains("skin") || mname.Contains("face"))
                {
                    dedicated.SetFloat("_UniformDesat", 0f);
                    dedicated.SetColor("_BaseColor", Color.white);
                }

                EditorUtility.SetDirty(dedicated);
                mats[i] = dedicated;
            }
            smr.sharedMaterials = mats;
            EditorUtility.SetDirty(smr);

            // Attach Spiky Hair
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/HairPack/Prefabs/Hairs/Hair_5.prefab", mats[0], new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f));
        }

        // =========================================================================================
        // 2. AIKA: Nobara Kugisaki (Straw Doll Resonance)
        // Uniform: Tokyo Navy Blazer (#181A28) | Aura: Cursed Rose (#D9435F) | Hair: Auburn Bob (#944528)
        // =========================================================================================
        private static void TransformAika(GameObject go, Shader shader)
        {
            if (go == null) return;
            SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) return;

            Material[] mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string mname = mats[i].name.ToLowerInvariant();
                Material dedicated = GetOrCreateDedicatedMaterial("Aika", mats[i], "Aika_" + mats[i].name, shader);

                if (mname.Contains("cloth") || mname.Contains("top"))
                {
                    // Tokyo High Tailored Navy Blazer
                    dedicated.SetFloat("_UniformDesat", 0.85f);
                    dedicated.SetColor("_UniformTint", new Color(0.094f, 0.102f, 0.157f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.85f, 0.26f, 0.37f, 1f)); // Cursed Rose #D9435F
                    dedicated.SetFloat("_NeonIntensity", 1.8f);
                }
                else if (mname.Contains("hair"))
                {
                    // Nobara Ginger Auburn Bob
                    dedicated.SetColor("_BaseColor", new Color(0.58f, 0.27f, 0.16f, 1f));
                    dedicated.SetFloat("_UniformDesat", 0.85f);
                    dedicated.SetColor("_UniformTint", new Color(0.58f, 0.27f, 0.16f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.85f, 0.26f, 0.37f, 1f));
                    dedicated.SetFloat("_NeonIntensity", 1.2f);
                }
                else if (mname.Contains("skin") || mname.Contains("face"))
                {
                    dedicated.SetFloat("_UniformDesat", 0f);
                    dedicated.SetColor("_BaseColor", Color.white);
                }

                EditorUtility.SetDirty(dedicated);
                mats[i] = dedicated;
            }
            smr.sharedMaterials = mats;
            EditorUtility.SetDirty(smr);

            // Attach Sleek Bob
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/HairPack/Prefabs/Hairs/Hair 2.prefab", mats[0], new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(1.02f, 1.02f, 1.02f));
        }

        // =========================================================================================
        // 3. ANIME_GIRL: Yuji Itadori (Cursed Brawler)
        // Uniform: Charcoal Navy (#181822) | Aura: Divergent Blue (#00E5FF) | Hair: Cursed Rose-Pink (#D9688A)
        // =========================================================================================
        private static void TransformAnimeGirl(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            Material hairMat = null;

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string prefix = rname.Contains("hair") ? "Hair" : (rname.Contains("t-shirt") || rname.Contains("brasalet") ? "Cloth" : "Body");
                    Material dedicated = GetOrCreateDedicatedMaterial("AnimeGirl", shared[i], "AnimeGirl_" + prefix + "_" + shared[i].name, shader);

                    if (rname.Contains("t-shirt") || rname.Contains("brasalet"))
                    {
                        // Charcoal Navy Sorcerer Tunic
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.094f, 0.094f, 0.133f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.00f, 0.90f, 1.00f, 1f)); // Divergent Blue #00E5FF
                        dedicated.SetFloat("_NeonIntensity", 2.0f);
                    }
                    else if (rname.Contains("hair"))
                    {
                        // Yuji Rose-Pink Spikes
                        dedicated.SetColor("_BaseColor", new Color(0.85f, 0.41f, 0.54f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.85f, 0.41f, 0.54f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.00f, 0.90f, 1.00f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.4f);
                        hairMat = dedicated;
                        r.enabled = false; // Hide base hair to swap with new spiky style
                    }
                    else
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }

            // Attach Spiky Hair
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/HairPack/Prefabs/Hairs/Hair_5.prefab", hairMat, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f));
        }

        // =========================================================================================
        // 4. UNITY-CHAN: Satoru Gojo (Limitless Special Grade)
        // Uniform: Midnight Onyx Coat (#12131C) | Aura: Limitless Infinity Cyan (#00F0FF) | Hair: Pure Snow White (#F5F7FC)
        // =========================================================================================
        private static void TransformUnityChan(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();

                // Disable twintails and ribbons for sleek Gojo silhouette
                if (rname.Contains("tail") || rname.Contains("hairband") || rname.Contains("hair_accce"))
                {
                    r.enabled = false;
                    continue;
                }

                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("UnityChan", shared[i], "UnityChan_" + shared[i].name, shader);

                    if (mname.Contains("body") || rname.Contains("uwagi") || rname.Contains("shirt") || rname.Contains("leg"))
                    {
                        // Midnight Onyx Coat
                        dedicated.SetFloat("_UniformDesat", 0.90f);
                        dedicated.SetColor("_UniformTint", new Color(0.071f, 0.075f, 0.110f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.00f, 0.94f, 1.00f, 1f)); // Infinity Cyan #00F0FF
                        dedicated.SetFloat("_NeonIntensity", 2.5f);
                    }
                    else if (mname.Contains("hair"))
                    {
                        // Gojo Pure Snow White
                        dedicated.SetColor("_BaseColor", new Color(0.96f, 0.97f, 0.99f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.95f);
                        dedicated.SetColor("_UniformTint", new Color(0.96f, 0.97f, 0.99f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.00f, 0.94f, 1.00f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.8f);
                    }
                    else if (mname.Contains("eye"))
                    {
                        // Six Eyes Luminous Cyan
                        dedicated.SetColor("_BaseColor", new Color(0.00f, 0.94f, 1.00f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.00f, 0.94f, 1.00f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 2.0f);
                    }
                    else if (mname.Contains("skin") || mname.Contains("face"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }
        }

        // =========================================================================================
        // 5. AKANE (angGirl): Toge Inumaki (Cursed Speech Infiltrator)
        // Uniform: High-Collar Navy Coat (#171926) | Aura: Mystic Violet (#8B5CF6) | Hair: Platinum Beige (#DED8C8)
        // =========================================================================================
        private static void TransformAkane(GameObject go, Shader shader)
        {
            if (go == null) return;
            SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) return;

            Material[] mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string mname = mats[i].name.ToLowerInvariant();
                Material dedicated = GetOrCreateDedicatedMaterial("Akane", mats[i], "Akane_" + mats[i].name.Replace(" (Instance)", ""), shader);

                if (mname.Contains("cloth") || mname.Contains("tops") || mname.Contains("shoes"))
                {
                    // High-Collar Sorcerer Coat
                    dedicated.SetFloat("_UniformDesat", 0.85f);
                    dedicated.SetColor("_UniformTint", new Color(0.090f, 0.098f, 0.149f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.55f, 0.36f, 0.96f, 1f)); // Mystic Violet #8B5CF6
                    dedicated.SetFloat("_NeonIntensity", 1.8f);
                }
                else if (mname.Contains("hair"))
                {
                    // Inumaki Platinum Beige
                    dedicated.SetColor("_BaseColor", new Color(0.87f, 0.85f, 0.78f, 1f));
                    dedicated.SetFloat("_UniformDesat", 0.85f);
                    dedicated.SetColor("_UniformTint", new Color(0.87f, 0.85f, 0.78f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.55f, 0.36f, 0.96f, 1f));
                    dedicated.SetFloat("_NeonIntensity", 1.2f);
                }
                else if (mname.Contains("skin") || mname.Contains("face"))
                {
                    dedicated.SetFloat("_UniformDesat", 0f);
                    dedicated.SetColor("_BaseColor", Color.white);
                }

                EditorUtility.SetDirty(dedicated);
                mats[i] = dedicated;
            }
            smr.sharedMaterials = mats;
            EditorUtility.SetDirty(smr);
        }

        // =========================================================================================
        // 6. CULTIST: Suguru Geto (Curse Spirit Manipulator)
        // Uniform: Void Buddhist Robes (#111218) | Aura: Dark Curse Miasma Purple (#581C87) | Hair: Void Obsidian
        // =========================================================================================
        private static void TransformCultist(GameObject go, Shader shader)
        {
            if (go == null) return;
            SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) return;

            Material[] mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string mname = mats[i].name.ToLowerInvariant();
                Material dedicated = GetOrCreateDedicatedMaterial("Cultist", mats[i], "Cultist_" + mats[i].name, shader);

                if (mname.Contains("cloth") || mname.Contains("tops") || mname.Contains("bottoms") || mname.Contains("shoes"))
                {
                    // Void Buddhist Robes
                    dedicated.SetFloat("_UniformDesat", 0.88f);
                    dedicated.SetColor("_UniformTint", new Color(0.067f, 0.071f, 0.094f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.35f, 0.11f, 0.53f, 1f)); // Curse Miasma #581C87
                    dedicated.SetFloat("_NeonIntensity", 1.8f);
                }
                else if (mname.Contains("hair"))
                {
                    // Geto Obsidian Hair
                    dedicated.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.08f, 1f));
                    dedicated.SetFloat("_UniformDesat", 0.95f);
                    dedicated.SetColor("_UniformTint", new Color(0.06f, 0.06f, 0.08f, 1f));
                    dedicated.SetColor("_NeonColor", new Color(0.35f, 0.11f, 0.53f, 1f));
                    dedicated.SetFloat("_NeonIntensity", 1.3f);
                }
                else if (mname.Contains("skin") || mname.Contains("face"))
                {
                    dedicated.SetFloat("_UniformDesat", 0f);
                    dedicated.SetColor("_BaseColor", Color.white);
                }

                EditorUtility.SetDirty(dedicated);
                mats[i] = dedicated;
            }
            smr.sharedMaterials = mats;
            EditorUtility.SetDirty(smr);
        }

        // =========================================================================================
        // 7. STELLAR WITCH: Shoko Ieiri / Bleach Captain (Reverse Cursed Technique Doctor)
        // Uniform: Stark White High-Grade Overcoat (#EAEFF5) | Aura: Healing Jade (#10B981) | Hair: Espresso Brunette
        // =========================================================================================
        private static void TransformStellarWitch(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();

                // Remove witch hat for modern medical sorcerer aesthetic
                if (rname.Contains("hat"))
                {
                    r.enabled = false;
                    continue;
                }

                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("StellarWitch", shared[i], "StellarWitch_" + shared[i].name, shader);

                    if (mname.Contains("cloth") || mname.Contains("blouth") || rname.Contains("robe") || rname.Contains("blouse") || rname.Contains("scart"))
                    {
                        // Stark White High-Grade Doctor Overcoat
                        dedicated.SetFloat("_UniformDesat", 0.90f);
                        dedicated.SetColor("_UniformTint", new Color(0.918f, 0.937f, 0.961f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.06f, 0.73f, 0.51f, 1f)); // Healing Jade #10B981
                        dedicated.SetFloat("_NeonIntensity", 1.8f);
                    }
                    else if (mname.Contains("hair"))
                    {
                        // Shoko Espresso Brunette
                        dedicated.SetColor("_BaseColor", new Color(0.21f, 0.13f, 0.11f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.21f, 0.13f, 0.11f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.06f, 0.73f, 0.51f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.2f);
                    }
                    else if (mname.Contains("skin") || mname.Contains("face"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }
        }

        // =========================================================================================
        // 8. SANPAKU-CHAN: Mei Mei (Raven Crow Striker)
        // Uniform: Slate-Navy Vest & Slacks (#1A1D26) | Aura: Amethyst (#7C3AED) | Hair: Silver-Lilac (#C4B5FD)
        // =========================================================================================
        private static void TransformSanpakuChan(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("SanpakuChan", shared[i], "SanpakuChan_" + shared[i].name, shader);

                    if (mname.Contains("body") || rname.Contains("body"))
                    {
                        // Slate-Navy Vest & Slacks
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.102f, 0.114f, 0.149f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.49f, 0.23f, 0.93f, 1f)); // Raven Amethyst #7C3AED
                        dedicated.SetFloat("_NeonIntensity", 1.8f);
                    }
                    else if (mname.Contains("hair") || rname.Contains("hair"))
                    {
                        // Mei Mei Silver-Lilac Hair
                        dedicated.SetColor("_BaseColor", new Color(0.77f, 0.71f, 0.99f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.77f, 0.71f, 0.99f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.49f, 0.23f, 0.93f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.5f);
                    }
                    else if (mname.Contains("face") || rname.Contains("face"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }
        }

        // =========================================================================================
        // 9. PICO-CHAN: Kokichi Muta / Mechamaru (Puppet Artificer)
        // Uniform: Gunmetal Techwear (#1C1F2A) | Aura: Electric Turquoise (#0EA5E9) | Hair: Steel Cyan (#1E405A)
        // =========================================================================================
        private static void TransformPicoChan(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();

                // Disable the 4 long twintails for sharp techwear haircut
                if (rname.Contains("hair_tail"))
                {
                    r.enabled = false;
                    continue;
                }

                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("PicoChan", shared[i], "PicoChan_" + shared[i].name, shader);

                    if (mname.Contains("clothes") || mname.Contains("jacket") || mname.Contains("bag") || mname.Contains("metal"))
                    {
                        // Gunmetal Techwear Sorcerer Suit
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.110f, 0.122f, 0.165f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.05f, 0.65f, 0.91f, 1f)); // Electric Turquoise #0EA5E9
                        dedicated.SetFloat("_NeonIntensity", 2.0f);
                    }
                    else if (mname.Contains("hair"))
                    {
                        // Mechamaru Steel Cyan Hair
                        dedicated.SetColor("_BaseColor", new Color(0.12f, 0.25f, 0.35f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.12f, 0.25f, 0.35f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.05f, 0.65f, 0.91f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.5f);
                    }
                    else if (mname.Contains("face") || mname.Contains("eye"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }
        }

        // =========================================================================================
        // 10. KIKI-V2: Rukia Kuchiki / Maki Zen'in (Cursed Blade Shinigami)
        // Uniform: Bleach Shihakusho Obsidian (#13141B) | Aura: Frost Silver (#E2E8F0) | Hair: Jet Bob
        // =========================================================================================
        private static void TransformKiki(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            Material hairMat = null;

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();

                // Disable Santa hat and base hair to swap with sleek Short_Hair
                if (rname.Contains("hat") || rname.Contains("hair"))
                {
                    r.enabled = false;
                }

                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("Kiki", shared[i], "Kiki_" + shared[i].name, shader);

                    if (mname.Contains("dress") || rname.Contains("dress"))
                    {
                        // Bleach Shihakusho Obsidian Robes
                        dedicated.SetFloat("_UniformDesat", 0.90f);
                        dedicated.SetColor("_UniformTint", new Color(0.075f, 0.078f, 0.106f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.89f, 0.91f, 0.94f, 1f)); // Frost Silver #E2E8F0
                        dedicated.SetFloat("_NeonIntensity", 1.8f);
                    }
                    else if (mname.Contains("hair"))
                    {
                        // Rukia Jet Black Hair
                        dedicated.SetColor("_BaseColor", new Color(0.07f, 0.07f, 0.09f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.90f);
                        dedicated.SetColor("_UniformTint", new Color(0.07f, 0.07f, 0.09f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.89f, 0.91f, 0.94f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.3f);
                        hairMat = dedicated;
                    }
                    else if (mname.Contains("skin") || mname.Contains("face"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }

            // Attach Short Hair Bob
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/Kiki/Xmas Girl/Prefabs/Short_Hair.prefab", hairMat, new Vector3(0f, 0.06f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f));
        }

        // =========================================================================================
        // 11. CASUAL1: Kinji Hakari (Tokyo Sorcerer Streetwear)
        // Uniform: Tokyo Streetwear Bomber (#181A24) | Aura: Jackpot Gold (#EAB308) | Hair: Ash Brown Spikes
        // =========================================================================================
        private static void TransformCasual1(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            Material hairMat = null;

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("Casual1", shared[i], "Casual1_" + shared[i].name, shader);

                    if (mname == "mat" || rname == "casual1")
                    {
                        // Tokyo Streetwear Bomber & Slacks
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.094f, 0.102f, 0.141f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.92f, 0.70f, 0.03f, 1f)); // Jackpot Gold #EAB308
                        dedicated.SetFloat("_NeonIntensity", 2.2f);
                    }
                    else if (mname.Contains("hair") || rname.Contains("hair"))
                    {
                        // Hakari Ash Brown
                        dedicated.SetColor("_BaseColor", new Color(0.16f, 0.14f, 0.17f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.16f, 0.14f, 0.17f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.92f, 0.70f, 0.03f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.5f);
                        hairMat = dedicated;
                        r.enabled = false; // Hide base hair to swap with new style
                    }
                    else if (mname.Contains("skin") || mname.Contains("face") || mname.Contains("body"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }

            // Attach Hair 4 (disheveled spiky cut)
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/HairPack/Prefabs/Hairs/Hair 4.prefab", hairMat, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f));
        }

        // =========================================================================================
        // 12. UNDERWEAR1: Maki Zen'in (Combat Dojo Trainee)
        // Uniform: Dark Dojo Tactical Garb (#191B24) | Aura: Cursed Pine (#14B8A6) | Hair: Dark Forest Teal Ponytail
        // =========================================================================================
        private static void TransformUnderwear1(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            Material hairMat = null;

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();
                if (rname.Contains("hair"))
                {
                    r.enabled = false; // Hide old hair to attach tactical ponytail
                }

                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("Underwear1", shared[i], "Underwear1_" + shared[i].name, shader);

                    if (mname.Contains("underwear") || rname.Contains("underwear") || rname.Contains("bikini"))
                    {
                        // Tactical Dark Dojo Compression Outfit
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.098f, 0.106f, 0.141f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.08f, 0.72f, 0.65f, 1f)); // Cursed Pine #14B8A6
                        dedicated.SetFloat("_NeonIntensity", 1.6f);
                    }
                    else if (mname.Contains("hair"))
                    {
                        // Maki Dark Forest Teal
                        dedicated.SetColor("_BaseColor", new Color(0.11f, 0.22f, 0.22f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.11f, 0.22f, 0.22f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.08f, 0.72f, 0.65f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.4f);
                        hairMat = dedicated;
                    }
                    else if (mname.Contains("skin") || mname.Contains("face") || mname.Contains("body"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }

            // Attach Tactical High Ponytail
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/HairPack/Prefabs/Hairs/Hair 4.prefab", hairMat, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f));
        }

        // =========================================================================================
        // 13. SAPPHIARTCHAN: Takada-chan (Cursed Pop Idol / Todo's Muse)
        // Uniform: Gothic Midnight Idol Dress (#161722) | Aura: Pastel Violet (#C084FC) | Hair: Pastel Lavender
        // =========================================================================================
        private static void TransformSapphiArtchan(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in rends)
            {
                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("SapphiArtchan", shared[i], "SapphiArtchan_" + shared[i].name, shader);

                    if (mname.Contains("clothes"))
                    {
                        // Gothic Midnight Idol Dress
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.086f, 0.090f, 0.133f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.75f, 0.52f, 0.99f, 1f)); // Pastel Violet #C084FC
                        dedicated.SetFloat("_NeonIntensity", 1.8f);
                    }
                    else if (mname.Contains("chequer"))
                    {
                        // Dark Violet Tartan Trim
                        dedicated.SetFloat("_UniformDesat", 0.80f);
                        dedicated.SetColor("_UniformTint", new Color(0.18f, 0.06f, 0.40f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.75f, 0.52f, 0.99f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.6f);
                    }
                    else if (mname.Contains("hair"))
                    {
                        // Takada-chan Pastel Lavender
                        dedicated.SetColor("_BaseColor", new Color(0.85f, 0.71f, 1.00f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.85f, 0.71f, 1.00f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.75f, 0.52f, 0.99f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.5f);
                    }
                    else if (mname.Contains("skin") || mname.Contains("face") || mname.Contains("eye"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }
        }

        // =========================================================================================
        // 14. MURYOTAISU: Ryomen Sukuna (Ancient Calamity / King of Curses)
        // Uniform: Malevolent Crimson Robes (#221218) | Aura: Blood Crimson (#EF4444) | Hair: Charcoal & Blood Rose
        // =========================================================================================
        private static void TransformMuryotaisu(GameObject go, Shader shader)
        {
            if (go == null) return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            Material hairMat = null;

            foreach (Renderer r in rends)
            {
                string rname = r.gameObject.name.ToLowerInvariant();

                // Disable AngelRing for pure malevolent demon aesthetic
                if (rname.Contains("angelring"))
                {
                    r.enabled = false;
                    continue;
                }

                if (rname.Contains("hair"))
                {
                    r.enabled = false; // Hide old hair to swap with Sukuna spiky hair
                }

                Material[] shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string mname = shared[i].name.ToLowerInvariant();
                    Material dedicated = GetOrCreateDedicatedMaterial("Muryotaisu", shared[i], "Muryotaisu_" + shared[i].name, shader);

                    if (mname.Contains("body") || rname.Contains("coat") || rname.Contains("body"))
                    {
                        // Malevolent Shrine Crimson & Obsidian Robes
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.133f, 0.071f, 0.094f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.94f, 0.27f, 0.27f, 1f)); // Blood Red #EF4444
                        dedicated.SetFloat("_NeonIntensity", 2.6f);
                    }
                    else if (mname.Contains("hair") || rname.Contains("hair"))
                    {
                        // Sukuna Charcoal & Blood Rose Spikes
                        dedicated.SetColor("_BaseColor", new Color(0.21f, 0.07f, 0.09f, 1f));
                        dedicated.SetFloat("_UniformDesat", 0.85f);
                        dedicated.SetColor("_UniformTint", new Color(0.21f, 0.07f, 0.09f, 1f));
                        dedicated.SetColor("_NeonColor", new Color(0.94f, 0.27f, 0.27f, 1f));
                        dedicated.SetFloat("_NeonIntensity", 1.8f);
                        hairMat = dedicated;
                    }
                    else if (mname.Contains("face"))
                    {
                        dedicated.SetFloat("_UniformDesat", 0f);
                        dedicated.SetColor("_BaseColor", Color.white);
                    }

                    EditorUtility.SetDirty(dedicated);
                    shared[i] = dedicated;
                }
                r.sharedMaterials = shared;
                EditorUtility.SetDirty(r);
            }

            // Attach Spiky Hair
            Transform head = FindHead(go);
            AttachHairPrefab(head, "Assets/HairPack/Prefabs/Hairs/Hair_5.prefab", hairMat, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(1f, 1f, 1f));
        }
    }
}
