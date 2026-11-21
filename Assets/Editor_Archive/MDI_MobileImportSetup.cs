#if MDI_ARCHIVE
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Editor automation tool for iPhone App Store mobile-light compliance.
    /// Configures Humanoid rigs, 4 bone weights, mesh compression, ASTC 6x6 texture formats,
    /// and enforces the <25k tri and <75 bone budget.
    /// </summary>
    public static class MDI_MobileImportSetup
    {
        public const string BASES_DIR = "Assets/MDI_Characters/Bases";
        public const int MAX_MOBILE_TRIS = 25000;
        public const int MAX_MOBILE_BONES = 75;

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Mobile Import Setup", priority = 10)]
        public static void RunMobileImportSetup()
        {
            if (!Directory.Exists(BASES_DIR))
            {
                Directory.CreateDirectory(BASES_DIR);
            }

            Debug.Log("<color=#00e676>==================================================</color>");
            Debug.Log("<color=#00e676>  MDI: EXECUTING MOBILE IMPORT SETUP (iPhone URP) </color>");
            Debug.Log("<color=#00e676>==================================================</color>");

            int modelsConfigured = 0;
            int texturesConfigured = 0;

            // 1. Process all 3D Models in Bases
            string[] modelFiles = Directory.GetFiles(BASES_DIR, "*.*", SearchOption.AllDirectories);
            foreach (string filePath in modelFiles)
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".fbx" && ext != ".obj" && ext != ".dae") continue;

                string relativePath = filePath.Replace('\\', '/');
                if (ProcessModelFile(relativePath))
                {
                    modelsConfigured++;
                }
            }

            // 2. Process all Textures in Bases & Shared
            string[] searchDirs = new string[] { BASES_DIR, "Assets/MDI_Characters/Shared" };
            foreach (string dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;
                string[] textureFiles = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (string filePath in textureFiles)
                {
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext != ".png" && ext != ".tga" && ext != ".jpg" && ext != ".jpeg" && ext != ".psd") continue;

                    string relativePath = filePath.Replace('\\', '/');
                    if (ProcessTextureFile(relativePath))
                    {
                        texturesConfigured++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[MDI Import Setup] Complete:</b> {modelsConfigured} models and {texturesConfigured} textures configured for iPhone App Store specifications.</color>");
        }

        private static bool ProcessModelFile(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return false;

            bool modified = false;

            // 1. Rig setup: Humanoid (ModelImporterAnimationType.Human)
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modified = true;
            }

            // 2. Mesh optimization: 4 bone weights, medium compression, read/write off
            if (importer.maxBonesPerVertex != 4)
            {
                importer.maxBonesPerVertex = 4;
                modified = true;
            }

            if (importer.meshCompression != ModelImporterMeshCompression.Medium)
            {
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                modified = true;
            }

            if (importer.isReadable)
            {
                importer.isReadable = false;
                modified = true;
            }

            // 3. Optimize mesh data
            if (!importer.optimizeMeshPolygons)
            {
                importer.optimizeMeshPolygons = true;
                importer.optimizeMeshVertices = true;
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            // 4. Audit Triangle & Bone counts
            AuditModelBudget(assetPath);

            return modified;
        }

        public static void AuditModelBudget(string assetPath)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelPrefab == null) return;

            int totalTris = 0;
            var meshes = new HashSet<Mesh>();
            var renderers = modelPrefab.GetComponentsInChildren<Renderer>(true);

            foreach (var r in renderers)
            {
                if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                {
                    if (meshes.Add(smr.sharedMesh))
                    {
                        totalTris += smr.sharedMesh.triangles.Length / 3;
                    }
                }
                else if (r is MeshRenderer mr)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null && meshes.Add(mf.sharedMesh))
                    {
                        totalTris += mf.sharedMesh.triangles.Length / 3;
                    }
                }
            }

            // Count bones
            int boneCount = 0;
            var smrs = modelPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                if (smr.bones != null && smr.bones.Length > boneCount)
                {
                    boneCount = smr.bones.Length;
                }
            }

            if (boneCount == 0)
            {
                // Fallback: count humanoid transform hierarchy
                boneCount = modelPrefab.GetComponentsInChildren<Transform>(true).Length;
            }

            string modelName = Path.GetFileNameWithoutExtension(assetPath);
            string statusColor = (totalTris <= MAX_MOBILE_TRIS && boneCount <= MAX_MOBILE_BONES) ? "#69f0ae" : "#ffab00";
            Debug.Log($"<color={statusColor}>[MDI Model Audit]</color> <b>{modelName}</b>: {totalTris:N0} tris, {boneCount} bones");

            if (totalTris > MAX_MOBILE_TRIS)
            {
                Debug.LogWarning($"<color=#ff5252>[MDI BUDGET WARNING]</color> {modelName} exceeds triangle budget ({totalTris:N0} > {MAX_MOBILE_TRIS:N0} tris)!");
            }

            if (boneCount > MAX_MOBILE_BONES)
            {
                Debug.LogWarning($"<color=#ff5252>[MDI BUDGET WARNING]</color> {modelName} exceeds bone budget ({boneCount} > {MAX_MOBILE_BONES} bones)!");
            }
        }

        private static bool ProcessTextureFile(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            bool modified = false;

            // Generate mipmaps
            if (!importer.mipmapEnabled)
            {
                importer.mipmapEnabled = true;
                modified = true;
            }

            // Determine max size: 2048 for atlases, 1024 for base maps
            bool isAtlas = assetPath.IndexOf("atlas", System.StringComparison.OrdinalIgnoreCase) >= 0;
            int targetMaxSize = isAtlas ? 2048 : 1024;

            // Configure default size
            if (importer.maxTextureSize != targetMaxSize)
            {
                importer.maxTextureSize = targetMaxSize;
                modified = true;
            }

            // Configure iPhone Platform Settings: ASTC 6x6
            TextureImporterPlatformSettings iosSettings = importer.GetPlatformTextureSettings("iPhone");
            if (!iosSettings.overridden ||
                iosSettings.format != TextureImporterFormat.ASTC_6x6 ||
                iosSettings.maxTextureSize != targetMaxSize)
            {
                iosSettings.overridden = true;
                iosSettings.name = "iPhone";
                iosSettings.maxTextureSize = targetMaxSize;
                iosSettings.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(iosSettings);
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return modified;
        }
    }
}
#endif

#endif
