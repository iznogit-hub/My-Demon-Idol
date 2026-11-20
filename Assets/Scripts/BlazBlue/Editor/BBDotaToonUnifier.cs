#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Phase 4 Shader Unifier: Unifies all 16 Dota lineup heroes (8 Synty Sidekicks + 8 External Dota Heroes)
    /// and their 11 warrior weapon props onto the master "DemonIdol/MDI/AnimeToon" cel shader,
    /// matching C01_Akio's 90s anime hand-painted look, cel shadow ramps, and ink outlines.
    /// </summary>
    [InitializeOnLoad]
    public static class BBDotaToonUnifier
    {
        public const string MDI_TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";
        public const string TRIGGER_FILE = "toonify_dota_heroes.trigger";

        [InitializeOnLoadMethod]
        public static void InitializeTriggerListener()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                ExecuteDotaToonify();
            }
        }

        [MenuItem("Demon Idol/🎨 Shaders/Unify 16 Dota Heroes & Weapons To Anime Toon", priority = 13)]
        public static void ExecuteDotaToonify()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: UNIFYING 16 DOTA HEROES & WEAPONS TO ANIME TOON </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            Shader mdiToonShader = Shader.Find(MDI_TOON_SHADER_NAME);
            if (mdiToonShader == null)
            {
                Debug.LogError($"[BBDotaToonUnifier] Could not find shader '{MDI_TOON_SHADER_NAME}'!");
                return;
            }

            // 1. Folders containing character & weapon materials
            string[] searchFolders = new string[]
            {
                "Assets/Synty/SidekickCharacters",
                "Assets/Prefabs/Characters/Sidekicks/Materials",
                "Assets/GAMWILL Character Pack Monster  Bionic Cartoon Zombie Gorilla",
                "Assets/Santuary/Characters",
                "Assets/Shady_3d",
                "Assets/JBlue/Security Officer",
                "Assets/RetroStyleGames/StylizedCharacters",
                "Assets/SimpleAssets/Humans/Guard",
                "Assets/Shokubutsu Studio/Low Poly Female Free",
                "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE",
                "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE",
                "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE",
                "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE"
            };

            HashSet<Material> targetMaterials = new HashSet<Material>();

            // Find all materials in the specific folders
            string[] matGuids = AssetDatabase.FindAssets("t:Material", searchFolders);
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    targetMaterials.Add(mat);
                }
            }
            Debug.Log($"[BBDotaToonUnifier] Found {targetMaterials.Count} materials in target hero packages.");

            // 2. Prefabs to audit: 8 Sidekicks + 8 External Dota Heroes + 11 Warrior Weapons
            List<string> prefabFolders = new List<string>
            {
                "Assets/Prefabs/Characters/Sidekicks",
                "Assets/Prefabs/Characters/DotaHeroes",
                "Assets/Prefabs/Weapons/Warrior"
            };

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", prefabFolders.ToArray());
            Material graniteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Santuary/Characters/Materials/SenctuaryGuardian_D.mat");

            int prefabsUpdated = 0;

            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                bool prefabDirty = false;
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer rend in renderers)
                {
                    Material[] sharedMats = rend.sharedMaterials;
                    bool matsModified = false;

                    for (int i = 0; i < sharedMats.Length; i++)
                    {
                        Material m = sharedMats[i];

                        // Granite repair: check if renderer is missing valid material or pointing to subasset
                        if ((m == null || m.name.Contains("Senctuary") || prefabPath.Contains("EX02_Granite")) && graniteMat != null && prefabPath.Contains("EX02_Granite"))
                        {
                            sharedMats[i] = graniteMat;
                            m = graniteMat;
                            matsModified = true;
                        }

                        if (m != null)
                        {
                            targetMaterials.Add(m);
                        }
                    }

                    if (matsModified)
                    {
                        rend.sharedMaterials = sharedMats;
                        EditorUtility.SetDirty(rend);
                        prefabDirty = true;
                    }
                }

                if (prefabDirty)
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                    prefabsUpdated++;
                    Debug.Log($"[BBDotaToonUnifier] Repaired & saved prefab: {prefab.name}");
                }
            }

            // 3. Process every material onto DemonIdol/MDI/AnimeToon with Akio styling parameters
            int matsUpdated = 0;
            int texSynced = 0;

            foreach (Material mat in targetMaterials)
            {
                if (mat == null) continue;

                // Set shader
                if (mat.shader != mdiToonShader)
                {
                    mat.shader = mdiToonShader;
                    matsUpdated++;
                }

                // Synchronize BaseMap and MainTex from any legacy/custom albedo property
                Texture baseMap = mat.GetTexture("_BaseMap");
                if (baseMap == null)
                {
                    // Inspect the material's saved texture guids from disk
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    if (!string.IsNullOrEmpty(matPath) && File.Exists(matPath))
                    {
                        string content = File.ReadAllText(matPath);
                        string[] targetProps = new string[] { "_ColorMap", "_Base_Color_Map", "_MainTex", "_Albedo", "_Diffuse" };
                        foreach (string prop in targetProps)
                        {
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                                content,
                                $@"- {prop}:\s*\r?\n\s*m_Texture:\s*\{{fileID:\s*2800000,\s*guid:\s*([a-f0-9]{{32}})"
                            );
                            if (match.Success)
                            {
                                string texGuid = match.Groups[1].Value;
                                string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                                Texture foundTex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                                if (foundTex != null)
                                {
                                    baseMap = foundTex;
                                    mat.SetTexture("_BaseMap", baseMap);
                                    mat.SetTexture("_MainTex", baseMap);
                                    texSynced++;
                                    Debug.Log($"[BBDotaToonUnifier] Synced {mat.name} BaseMap <- {prop} ({foundTex.name})");
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (mat.GetTexture("_MainTex") == null)
                    {
                        mat.SetTexture("_MainTex", baseMap);
                        texSynced++;
                    }
                }

                // Apply Akio's cel parameters
                ApplyCelAndStylingParameters(mat);

                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log($"<color=#00e676>  BBDotaToonUnifier COMPLETE:                                </color>");
            Debug.Log($"<color=#00e676>  - Total Materials Processed: {targetMaterials.Count}       </color>");
            Debug.Log($"<color=#00e676>  - Shader Swapped to AnimeToon: {matsUpdated}               </color>");
            Debug.Log($"<color=#00e676>  - Textures Synchronized: {texSynced}                       </color>");
            Debug.Log($"<color=#00e676>  - Prefabs Repaired & Saved: {prefabsUpdated}               </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");
        }

        private static void ApplyCelAndStylingParameters(Material mat)
        {
            string name = mat.name.ToLowerInvariant();

            // Core 90s Cel Shading defaults matching C01_Akio
            mat.SetFloat("_RampThreshold1", 0.42f);
            mat.SetFloat("_RampThreshold2", 0.78f);
            mat.SetFloat("_RampSmoothness", 0.008f);

            // Celluloid film & vintage parameters
            mat.SetFloat("_VintageWarmth", 0.40f);
            mat.SetFloat("_FilmGrain", 0.08f);
            mat.SetFloat("_HalationIntensity", 0.25f);
            mat.SetColor("_RimColor", new Color(0.95f, 0.92f, 0.86f, 0.55f));
            mat.SetFloat("_RimPower", 3.5f);
            mat.SetFloat("_RimThreshold", 0.40f);

            // Inked cel outline default
            mat.SetFloat("_EnableOutline", 1.0f);
            mat.SetFloat("_OutlineWidth", 1.2f);
            mat.SetColor("_OutlineColor", new Color(0.09f, 0.07f, 0.12f, 1.0f));

            // Default Shadow Color (vintage hand-painted celluloid shadow tone)
            mat.SetColor("_ShadowColor", new Color(0.68f, 0.58f, 0.72f, 1.0f));

            // Base Tint
            if (!mat.HasProperty("_BaseColor") || mat.GetColor("_BaseColor") == Color.black)
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            // Category-specific overrides
            if (name.Contains("eyebrow") || name.Contains("eyelash") || name.Contains("eyeline") || name.Contains("brow") || name.Contains("lash"))
            {
                // Cutout eyebrows/lashes
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.SetFloat("_Cull", 0f); // Double-sided
                mat.SetFloat("_EnableOutline", 0f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2450;
                mat.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else if (name.Contains("cheek") || name.Contains("blush"))
            {
                // Transparent blush
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_Cull", 0f);
                mat.SetFloat("_EnableOutline", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
            }
            else if (name.Contains("eye") || name.Contains("iris") || name.Contains("pupil"))
            {
                // Eyes / Iris
                mat.SetFloat("_EnableOutline", 0f);
                mat.SetFloat("_Cull", 2f);
                mat.SetFloat("_RampThreshold1", 0.15f);
                mat.SetFloat("_RampThreshold2", 0.35f);
                mat.SetColor("_ShadowColor", new Color(0.82f, 0.80f, 0.86f, 1f));

                if (name.Contains("transparent"))
                {
                    mat.SetFloat("_AlphaClip", 1f);
                    mat.SetFloat("_Cutoff", 0.5f);
                    mat.renderQueue = 2450;
                    mat.EnableKeyword("_ALPHATEST_ON");
                }
            }
            else if (name.Contains("hair") || name.Contains("bangs"))
            {
                // Hair: double-sided, subtle outline, cool lilac cel shadow
                mat.SetFloat("_Cull", 0f);
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.0f);
                mat.SetColor("_ShadowColor", new Color(0.65f, 0.60f, 0.72f, 1f));
            }
            else if (name.Contains("skin") || name.Contains("face") || name.Contains("body") || name.Contains("flesh") || name.Contains("gorilla") || name.Contains("hugo"))
            {
                // Skin/Flesh: warm anime skin shadow tone
                mat.SetFloat("_Cull", 2f);
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.2f);
                mat.SetColor("_ShadowColor", new Color(0.85f, 0.65f, 0.68f, 1f));
                mat.SetFloat("_RampThreshold1", 0.40f);
                mat.SetFloat("_RampThreshold2", 0.70f);
            }
            else if (name.Contains("weapon") || name.Contains("sword") || name.Contains("shield") || name.Contains("bow") || name.Contains("arrow") || name.Contains("crossbow") || name.Contains("hammer") || name.Contains("spear") || name.Contains("staff") || name.Contains("greatsword"))
            {
                // Weapons & Props: double-sided blades, bold comic outline, crisp metallic specular glint
                mat.SetFloat("_Cull", 0f);
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.3f);
                mat.SetColor("_ShadowColor", new Color(0.60f, 0.58f, 0.70f, 1f));
                mat.SetFloat("_SpecIntensity", 0.8f);
                mat.SetFloat("_SpecPower", 48f);
                mat.SetFloat("_SpecThreshold", 0.55f);
            }
            else
            {
                // General clothing, armor, accessories, robot parts, rock golem plates
                mat.SetFloat("_Cull", 0f); // Double-sided for thin cloth/armor meshes
                mat.SetFloat("_EnableOutline", 1f);
                mat.SetFloat("_OutlineWidth", 1.25f);
                mat.SetColor("_ShadowColor", new Color(0.68f, 0.58f, 0.72f, 1f));
            }
        }
    }
}
#endif
