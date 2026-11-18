using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonIdol.EditorTools
{
    [InitializeOnLoad]
    public static class DemonIdolToonUnifier
    {
        private const string MDI_TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";
        private const string PREF_KEY = "DemonIdol_ToonUnified_RunOnce_v1";

        static DemonIdolToonUnifier()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!EditorPrefs.GetBool(PREF_KEY, false))
            {
                EditorPrefs.SetBool(PREF_KEY, true);
                Debug.Log("[DemonIdolToonUnifier] Auto-running character toon unification pass...");
                ExecuteToonify();
            }
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Characters/Inspect Home Scene Hierarchy", false, 11)]
        public static void InspectSceneHierarchy()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Debug.Log($"[DemonIdolInspect] Dumping characters in active scene: {activeScene.name}");
            GameObject[] roots = activeScene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0) continue;

                Animator anim = root.GetComponentInChildren<Animator>(true);
                Transform head = null;
                if (anim != null && anim.isHuman)
                {
                    try { head = anim.GetBoneTransform(HumanBodyBones.Head); } catch { }
                }
                if (head == null)
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    {
                        string tname = t.name.ToLowerInvariant();
                        if ((tname.Contains("head") || tname.Contains("neck")) && !tname.Contains("end") && !tname.Contains("ribbon") && !tname.Contains("hair") && !tname.Contains("locator"))
                        {
                            head = t;
                            break;
                        }
                    }
                }

                Debug.Log($"[CharInspect] '{root.name}' | HasAnim: {(anim != null)} | Head: {(head != null ? head.name : "NULL")} | Renderers: {rends.Length}");
                foreach (Renderer r in rends)
                {
                    string matNames = "";
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null) matNames += m.name + ", ";
                    }
                    Debug.Log($"   -> Rend: '{r.gameObject.name}' (Type: {r.GetType().Name}) | Mats: [{matNames}]");
                }
            }
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Characters/Toonify & Fix All 14 Characters in Home Scene", false, 10)]
        public static void ExecuteToonify()
        {
            Debug.Log("==================================================================");
            Debug.Log("[DemonIdolToonUnifier] Starting unified Toon Shading pass for all 14 characters...");

            Shader mdiToonShader = Shader.Find(MDI_TOON_SHADER_NAME);
            if (mdiToonShader == null)
            {
                Debug.LogError($"[DemonIdolToonUnifier] Could not find shader '{MDI_TOON_SHADER_NAME}'. Aborting.");
                return;
            }

            // 1. Refresh AssetDatabase to pick up on-disk material updates
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 2. Scan and audit materials
            string[] characterFolders = new string[]
            {
                "Assets/Anime Girl",
                "Assets/unity-chan!",
                "Assets/3d-character_animeGirlAkane",
                "Assets/AnimeGirls",
                "Assets/SanpakuChan",
                "Assets/Novacorp Studios",
                "Assets/SapphiArt",
                "Assets/StellarWitch3D",
                "Assets/Picola",
                "Assets/Kiki",
                "Assets/CeltriusStudios",
                "Assets/Mryotaisu"
            };

            int materialCount = 0;
            int fixedTexturesCount = 0;

            string[] matGuids = AssetDatabase.FindAssets("t:Material", characterFolders);
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Ensure shader is MDI AnimeToon
                if (mat.shader != mdiToonShader)
                {
                    mat.shader = mdiToonShader;
                    EditorUtility.SetDirty(mat);
                }

                // Verify texture binding
                Texture baseMap = mat.GetTexture("_BaseMap");
                Texture mainTex = mat.GetTexture("_MainTex");

                if (baseMap == null && mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                    EditorUtility.SetDirty(mat);
                    fixedTexturesCount++;
                }
                else if (baseMap != null && mainTex == null)
                {
                    mat.SetTexture("_MainTex", baseMap);
                    EditorUtility.SetDirty(mat);
                    fixedTexturesCount++;
                }

                // Verify rendering features based on material name
                ApplyStylingRules(mat);

                materialCount++;
            }

            // 3. Inspect active Home scene and verify all 14 characters
            Scene activeScene = SceneManager.GetActiveScene();
            bool isHomeScene = activeScene.path.Contains("Home.unity");

            if (!isHomeScene)
            {
                string homeScenePath = "Assets/Scenes/Home.unity";
                if (File.Exists(homeScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(homeScenePath, OpenSceneMode.Single);
                    isHomeScene = true;
                }
            }

            int verifiedRenderers = 0;
            if (isHomeScene)
            {
                GameObject[] roots = activeScene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer r in renderers)
                    {
                        Material[] sharedMats = r.sharedMaterials;
                        bool changed = false;
                        for (int i = 0; i < sharedMats.Length; i++)
                        {
                            if (sharedMats[i] != null && sharedMats[i].shader != mdiToonShader)
                            {
                                sharedMats[i].shader = mdiToonShader;
                                EditorUtility.SetDirty(sharedMats[i]);
                                changed = true;
                            }
                        }
                        if (changed)
                        {
                            r.sharedMaterials = sharedMats;
                            EditorUtility.SetDirty(r);
                        }
                        verifiedRenderers++;
                    }
                }

                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[DemonIdolToonUnifier] Home scene saved with {verifiedRenderers} verified character renderers.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DemonIdolToonUnifier] Complete! Processed {materialCount} materials, fixed {fixedTexturesCount} texture references, unified on '{MDI_TOON_SHADER_NAME}'.");
            Debug.Log("==================================================================");
        }

        private static void ApplyStylingRules(Material mat)
        {
            string name = mat.name.ToLowerInvariant();

            // Eyelashes / Eyebrows / Eyeline (Cutout)
            if (name.Contains("eyebrow") || name.Contains("eyelash") || name.Contains("eyeline") || name.Contains("brow") || name.Contains("lash"))
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.SetFloat("_Cull", 0f); // Double-sided
                mat.SetFloat("_EnableOutline", 0f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2450;
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                EditorUtility.SetDirty(mat);
            }
            // Cheek / Blush (Transparent)
            else if (name.Contains("cheek") || name.Contains("blush"))
            {
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_Cull", 0f);
                mat.SetFloat("_EnableOutline", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
                EditorUtility.SetDirty(mat);
            }
            // Eyes / Iris / Pupils
            else if (name.Contains("eyeiris") || name.Contains("eyehighlight") || name.Contains("eyewhite") || name.Contains("eye_") || name.Contains("pupil") || name == "eye" || name.Contains("eyel1") || name.Contains("eyer1"))
            {
                mat.SetFloat("_EnableOutline", 0f);
                mat.SetFloat("_Cull", 2f);
                mat.SetFloat("_RampThreshold1", 0.15f);
                mat.SetFloat("_RampThreshold2", 0.35f);
                mat.SetColor("_ShadowColor", new Color(0.82f, 0.8f, 0.86f, 1f));
                EditorUtility.SetDirty(mat);
            }
            // Hair
            else if (name.Contains("hair") || name.Contains("bangs"))
            {
                mat.SetFloat("_Cull", 0f); // Double-sided hair
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.0f);
                mat.SetColor("_ShadowColor", new Color(0.65f, 0.6f, 0.72f, 1f));
                EditorUtility.SetDirty(mat);
            }
            // Skin / Face / Body
            else if (name.Contains("skin") || name.Contains("face") || name.Contains("body"))
            {
                mat.SetFloat("_Cull", 2f);
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.2f);
                mat.SetColor("_ShadowColor", new Color(0.85f, 0.65f, 0.68f, 1f)); // Warm anime skin shadow
                mat.SetFloat("_RampThreshold1", 0.4f);
                mat.SetFloat("_RampThreshold2", 0.7f);
                EditorUtility.SetDirty(mat);
            }
            // Fabrics / Skirts / Capes
            else if (name.Contains("skirt") || name.Contains("dress") || name.Contains("ribbon") || name.Contains("cape"))
            {
                mat.SetFloat("_Cull", 0f); // Double-sided
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.3f);
                mat.SetColor("_ShadowColor", new Color(0.6f, 0.58f, 0.7f, 1f));
                EditorUtility.SetDirty(mat);
            }
        }
    }
}
