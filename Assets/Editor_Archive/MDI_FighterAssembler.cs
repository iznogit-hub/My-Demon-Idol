#if MDI_ARCHIVE
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Characters;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Master Fighter Assembler for Demon Idol: Iron Legacy.
    /// Assembles an optimized 2.5D mobile fighting game prefab from a FighterDefinition:
    /// - Attaches hair & silhouette accessories to Humanoid bones
    /// - Bakes all skinned & static meshes into ONE single SkinnedMeshRenderer (1 draw call)
    /// - Generates single 2048x2048 texture atlas
    /// - Applies standalone MDI_AnimeToon material with tint colors & HDR neon bloom
    /// - Generates LOD1 (50% tri decimation) with LODGroup
    /// - Saves idempotent prefab to Assets/MDI_Characters/Clans/<Clan>/<Fighter>/<Fighter>.prefab
    /// </summary>
    public static class MDI_FighterAssembler
    {
        public const string TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Assemble Selected Fighter", priority = 20)]
        public static void AssembleSelectedFighter()
        {
            FighterDefinition selected = Selection.activeObject as FighterDefinition;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("MDI Assembler", "Please select a FighterDefinition asset first.", "OK");
                return;
            }

            AssembleFighter(selected);
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Assemble All 18 Fighters", priority = 21)]
        public static void AssembleAllFighters()
        {
            Debug.Log("<color=#00e676>==================================================</color>");
            Debug.Log("<color=#00e676>  MDI: ASSEMBLING ALL 18 CLAN FIGHTER PREFABS     </color>");
            Debug.Log("<color=#00e676>==================================================</color>");

            string[] guids = AssetDatabase.FindAssets("t:FighterDefinition", new string[] { "Assets/MDI_Characters/Clans" });
            int assembledCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FighterDefinition fDef = AssetDatabase.LoadAssetAtPath<FighterDefinition>(path);
                if (fDef == null) continue;

                if (AssembleFighter(fDef))
                {
                    assembledCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[MDI Assembler] Complete:</b> {assembledCount}/18 Fighter prefabs successfully baked and assembled with single SkinnedMeshRenderer & LOD1.</color>");
        }

        public static bool AssembleFighter(FighterDefinition fDef)
        {
            if (fDef == null) return false;

            string fighterDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(fDef));
            string fighterShortName = Path.GetFileName(fighterDir); // e.g. "Arin", "Leon", "Kai"
            string targetPrefabPath = $"{fighterDir}/{fighterShortName}.prefab";

            Debug.Log($"<color=#69f0ae>[MDI Assemble]</color> Assembling <b>{fDef.displayName}</b> ({fDef.clan} Clan) -> {targetPrefabPath}");

            // 1. Verify or Fallback Base Prefab
            GameObject basePrefab = fDef.basePrefab;
            if (basePrefab == null)
            {
                Debug.LogWarning($"[MDI Assemble] Base prefab missing for {fDef.displayName}. Searching Bases folder...");
                basePrefab = FindFallbackBasePrefab(fDef);
                if (basePrefab == null)
                {
                    Debug.LogError($"[MDI Assemble] Cannot assemble {fDef.displayName}: no base model found!");
                    return false;
                }
            }

            // 2. Instantiate Base Model in staging
            GameObject instance = Object.Instantiate(basePrefab);
            instance.name = fighterShortName;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            if (fDef.animatorController != null) animator.runtimeAnimatorController = fDef.animatorController;

            // 3. Attach Hair to Head Bone
            if (fDef.hairPrefab != null)
            {
                Transform headBone = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                if (headBone == null) headBone = FindBoneRecursive(instance.transform, "head");

                if (headBone != null)
                {
                    GameObject hairObj = Object.Instantiate(fDef.hairPrefab, headBone);
                    hairObj.name = "Attached_Hair";
                    hairObj.transform.localPosition = Vector3.zero;
                    hairObj.transform.localRotation = Quaternion.identity;
                }
            }

            // 4. Attach Accessories to Target Bones
            if (fDef.accessories != null)
            {
                foreach (var acc in fDef.accessories)
                {
                    if (acc.accessoryPrefab == null) continue;

                    HumanBodyBones targetHumanBone = FighterDefinition.ToHumanBodyBones(acc.targetBone);
                    Transform bone = animator.isHuman ? animator.GetBoneTransform(targetHumanBone) : null;
                    if (bone == null) bone = FindBoneRecursive(instance.transform, targetHumanBone.ToString());
                    if (bone == null) bone = instance.transform;

                    GameObject accObj = Object.Instantiate(acc.accessoryPrefab, bone);
                    accObj.name = $"Accessory_{acc.accessoryName.Replace(' ', '_')}";
                    accObj.transform.localPosition = acc.localPosition;
                    accObj.transform.localEulerAngles = acc.localEulerAngles;
                    accObj.transform.localScale = acc.localScale;
                }
            }

            // 5. Build Material & Texture Atlas
            Material toonMat = BuildFighterMaterial(fDef, instance, fighterDir);

            // 6. Bake All Renderers into ONE SkinnedMeshRenderer (1 Draw Call Optimization)
            Mesh bakedLOD0Mesh = BakeCombinedSkinnedMesh(instance, toonMat, fighterDir, fDef.id, out SkinnedMeshRenderer mainSMR);

            // 7. Generate LOD1 Decimated Mesh (~50% tris)
            Mesh bakedLOD1Mesh = GenerateLOD1Mesh(bakedLOD0Mesh, fighterDir, fDef.id);

            // 8. Configure LODGroup Component
            ConfigureLODGroup(instance, mainSMR, bakedLOD1Mesh, toonMat);

            // 9. Save Idempotent Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
            Object.DestroyImmediate(instance);

            // 10. Update FighterDefinition asset
            fDef.assembledPrefab = savedPrefab;
            EditorUtility.SetDirty(fDef);

            Debug.Log($"<color=#00e676>[MDI Assemble Success]</color> Saved {targetPrefabPath} (LOD0: {bakedLOD0Mesh.triangles.Length / 3:N0} tris, LOD1: {bakedLOD1Mesh.triangles.Length / 3:N0} tris)");
            return true;
        }

        private static Material BuildFighterMaterial(FighterDefinition fDef, GameObject instance, string fighterDir)
        {
            Shader shader = Shader.Find(TOON_SHADER_NAME);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

            string matPath = $"{fighterDir}/Material_{fDef.id}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_SkinTint", fDef.skinTint);
            mat.SetColor("_HairTint", fDef.hairTint);
            mat.SetColor("_PrimaryTint", fDef.primaryTint);
            mat.SetColor("_SecondaryTint", fDef.secondaryTint);
            mat.SetColor("_AccentTint", fDef.accentTint);
            mat.SetColor("_NeonColor", fDef.emissiveNeonColor);
            mat.SetFloat("_NeonIntensity", fDef.emissiveIntensity);
            mat.SetFloat("_EnableOutline", 1.0f);
            mat.SetFloat("_OutlineWidth", 1.5f);

            // Find or pack textures into single 2048x2048 atlas
            Texture2D atlas = GenerateTextureAtlas(instance, fighterDir, fDef.id);
            if (atlas != null)
            {
                mat.SetTexture("_BaseMap", atlas);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D GenerateTextureAtlas(GameObject instance, string fighterDir, string fid)
        {
            string atlasPath = $"{fighterDir}/Texture_Atlas_{fid}.png";

            // Check if existing atlas is already saved
            Texture2D existingAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (existingAtlas != null) return existingAtlas;

            // Collect textures from all sub-renderers
            List<Texture2D> textures = new List<Texture2D>();
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null)
                {
                    Texture mainTex = r.sharedMaterial.mainTexture;
                    if (mainTex is Texture2D t2d && !textures.Contains(t2d))
                    {
                        textures.Add(t2d);
                    }
                }
            }

            Texture2D atlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, true);
            if (textures.Count > 0)
            {
                // Make readable copies if needed
                List<Texture2D> readableTextures = new List<Texture2D>();
                foreach (var tex in textures)
                {
                    readableTextures.Add(CreateReadableTextureCopy(tex));
                }
                atlas.PackTextures(readableTextures.ToArray(), 4, 2048, false);
            }
            else
            {
                // Fallback: procedural 256x256 neutral toon gradient texture
                Color[] pixels = new Color[256 * 256];
                for (int y = 0; y < 256; y++)
                {
                    float factor = 0.85f + (y / 256f) * 0.15f;
                    for (int x = 0; x < 256; x++)
                    {
                        pixels[y * 256 + x] = new Color(factor, factor, factor, 1f);
                    }
                }
                atlas = new Texture2D(256, 256, TextureFormat.RGBA32, true);
                atlas.SetPixels(pixels);
                atlas.Apply();
            }

            byte[] bytes = atlas.EncodeToPNG();
            File.WriteAllBytes(atlasPath, bytes);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        }

        private static Texture2D CreateReadableTextureCopy(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }

        private static Mesh BakeCombinedSkinnedMesh(GameObject instance, Material toonMat, string fighterDir, string fid, out SkinnedMeshRenderer mainSMR)
        {
            string meshPath = $"{fighterDir}/Mesh_LOD0_{fid}.asset";

            var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mfs = instance.GetComponentsInChildren<MeshFilter>(true);

            // Collect bone hierarchy from base model
            Transform rootBone = null;
            Transform[] baseBones = null;
            if (smrs.Length > 0 && smrs[0].bones != null && smrs[0].bones.Length > 0)
            {
                rootBone = smrs[0].rootBone != null ? smrs[0].rootBone : smrs[0].transform;
                baseBones = smrs[0].bones;
            }
            else
            {
                rootBone = instance.transform;
                baseBones = instance.GetComponentsInChildren<Transform>(true);
            }

            List<Transform> unifiedBones = new List<Transform>(baseBones);
            List<Matrix4x4> bindposes = new List<Matrix4x4>();
            foreach (var b in unifiedBones)
            {
                bindposes.Add(b.worldToLocalMatrix * instance.transform.localToWorldMatrix);
            }

            List<Vector3> allVertices = new List<Vector3>();
            List<Vector3> allNormals = new List<Vector3>();
            List<Vector2> allUVs = new List<Vector2>();
            List<BoneWeight> allBoneWeights = new List<BoneWeight>();
            List<int> allTriangles = new List<int>();

            // 1. Bake existing SkinnedMeshRenderers
            foreach (var smr in smrs)
            {
                if (smr.sharedMesh == null) continue;
                Mesh m = smr.sharedMesh;
                int vertOffset = allVertices.Count;

                Vector3[] verts = m.vertices;
                Vector3[] norms = m.normals;
                Vector2[] uvs = m.uv.Length == verts.Length ? m.uv : new Vector2[verts.Length];
                BoneWeight[] bws = m.boneWeights.Length == verts.Length ? m.boneWeights : new BoneWeight[verts.Length];

                allVertices.AddRange(verts);
                allNormals.AddRange(norms);
                allUVs.AddRange(uvs);

                // Remap bone weights to unified skeleton
                for (int i = 0; i < bws.Length; i++)
                {
                    BoneWeight bw = bws[i];
                    if (smr.bones != null && smr.bones.Length > 0)
                    {
                        if (bw.boneIndex0 < smr.bones.Length) bw.boneIndex0 = GetOrAddBone(unifiedBones, bindposes, instance, smr.bones[bw.boneIndex0]);
                        if (bw.boneIndex1 < smr.bones.Length) bw.boneIndex1 = GetOrAddBone(unifiedBones, bindposes, instance, smr.bones[bw.boneIndex1]);
                        if (bw.boneIndex2 < smr.bones.Length) bw.boneIndex2 = GetOrAddBone(unifiedBones, bindposes, instance, smr.bones[bw.boneIndex2]);
                        if (bw.boneIndex3 < smr.bones.Length) bw.boneIndex3 = GetOrAddBone(unifiedBones, bindposes, instance, smr.bones[bw.boneIndex3]);
                    }
                    allBoneWeights.Add(bw);
                }

                int[] tris = m.triangles;
                for (int t = 0; t < tris.Length; t++)
                {
                    allTriangles.Add(tris[t] + vertOffset);
                }

                // Delete old SMR component
                Object.DestroyImmediate(smr);
            }

            // 2. Bake Static MeshRenderers (Attached hair & accessories)
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                Mesh m = mf.sharedMesh;
                int vertOffset = allVertices.Count;
                Transform parentBone = mf.transform.parent != null ? mf.transform.parent : instance.transform;
                int boneIdx = GetOrAddBone(unifiedBones, bindposes, instance, parentBone);

                Vector3[] verts = m.vertices;
                Vector3[] norms = m.normals;
                Vector2[] uvs = m.uv.Length == verts.Length ? m.uv : new Vector2[verts.Length];

                for (int i = 0; i < verts.Length; i++)
                {
                    // Transform to root space
                    Vector3 worldPos = mf.transform.TransformPoint(verts[i]);
                    allVertices.Add(instance.transform.InverseTransformPoint(worldPos));

                    Vector3 worldNorm = mf.transform.TransformDirection(norms[i]);
                    allNormals.Add(instance.transform.InverseTransformDirection(worldNorm));

                    allUVs.Add(uvs[i]);

                    // Rigid bind to parent bone
                    BoneWeight bw = new BoneWeight { boneIndex0 = boneIdx, weight0 = 1.0f };
                    allBoneWeights.Add(bw);
                }

                int[] tris = m.triangles;
                for (int t = 0; t < tris.Length; t++)
                {
                    allTriangles.Add(tris[t] + vertOffset);
                }

                // Delete static renderer and filter
                Object.DestroyImmediate(mf.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(mf);
            }

            // 3. Assemble unified single mesh
            Mesh bakedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (bakedMesh == null)
            {
                bakedMesh = new Mesh { name = $"Mesh_LOD0_{fid}" };
                AssetDatabase.CreateAsset(bakedMesh, meshPath);
            }
            else
            {
                bakedMesh.Clear();
            }

            if (allVertices.Count > 65535) bakedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            bakedMesh.SetVertices(allVertices);
            bakedMesh.SetNormals(allNormals);
            bakedMesh.SetUVs(0, allUVs);
            bakedMesh.boneWeights = allBoneWeights.ToArray();
            bakedMesh.bindposes = bindposes.ToArray();
            bakedMesh.SetTriangles(allTriangles, 0);
            bakedMesh.RecalculateBounds();
            bakedMesh.RecalculateTangents();

            EditorUtility.SetDirty(bakedMesh);

            // 4. Create Single Authoritative SkinnedMeshRenderer on root
            mainSMR = instance.AddComponent<SkinnedMeshRenderer>();
            mainSMR.sharedMesh = bakedMesh;
            mainSMR.bones = unifiedBones.ToArray();
            mainSMR.rootBone = rootBone;
            mainSMR.sharedMaterial = toonMat;
            mainSMR.quality = SkinQuality.Bone4;

            return bakedMesh;
        }

        private static int GetOrAddBone(List<Transform> bones, List<Matrix4x4> bindposes, GameObject root, Transform bone)
        {
            int idx = bones.IndexOf(bone);
            if (idx >= 0) return idx;

            idx = bones.Count;
            bones.Add(bone);
            bindposes.Add(bone.worldToLocalMatrix * root.transform.localToWorldMatrix);
            return idx;
        }

        private static Mesh GenerateLOD1Mesh(Mesh lod0, string fighterDir, string fid)
        {
            string lod1Path = $"{fighterDir}/Mesh_LOD1_{fid}.asset";
            Mesh lod1 = AssetDatabase.LoadAssetAtPath<Mesh>(lod1Path);
            if (lod1 == null)
            {
                lod1 = new Mesh { name = $"Mesh_LOD1_{fid}" };
                AssetDatabase.CreateAsset(lod1, lod1Path);
            }
            else
            {
                lod1.Clear();
            }

            // Decimate by taking every 2nd triangle for a smooth ~50% mobile triangle count reduction
            int[] tris0 = lod0.triangles;
            List<int> tris1 = new List<int>();
            for (int i = 0; i < tris0.Length; i += 6)
            {
                tris1.Add(tris0[i]);
                tris1.Add(tris0[i + 1]);
                tris1.Add(tris0[i + 2]);
            }

            if (lod0.vertices.Length > 65535) lod1.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            lod1.vertices = lod0.vertices;
            lod1.normals = lod0.normals;
            lod1.uv = lod0.uv;
            lod1.boneWeights = lod0.boneWeights;
            lod1.bindposes = lod0.bindposes;
            lod1.SetTriangles(tris1, 0);
            lod1.RecalculateBounds();

            EditorUtility.SetDirty(lod1);
            return lod1;
        }

        private static void ConfigureLODGroup(GameObject instance, SkinnedMeshRenderer lod0SMR, Mesh lod1Mesh, Material mat)
        {
            // Create child object for LOD1 SkinnedMeshRenderer
            GameObject lod1Obj = new GameObject("LOD1_Renderer");
            lod1Obj.transform.SetParent(instance.transform, false);

            SkinnedMeshRenderer lod1SMR = lod1Obj.AddComponent<SkinnedMeshRenderer>();
            lod1SMR.sharedMesh = lod1Mesh;
            lod1SMR.bones = lod0SMR.bones;
            lod1SMR.rootBone = lod0SMR.rootBone;
            lod1SMR.sharedMaterial = mat;
            lod1SMR.quality = SkinQuality.Bone2; // 2 weights for LOD1 for mobile speed

            LODGroup group = instance.GetComponent<LODGroup>();
            if (group == null) group = instance.AddComponent<LODGroup>();

            LOD[] lods = new LOD[2];
            lods[0] = new LOD(0.40f, new Renderer[] { lod0SMR });
            lods[1] = new LOD(0.12f, new Renderer[] { lod1SMR });
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        private static GameObject FindFallbackBasePrefab(FighterDefinition fDef)
        {
            string[] searchPaths = new string[]
            {
                "Assets/MDI_Characters/Bases/Orc_Assassin/Base_Orc_Assassin.fbx",
                "Assets/MDI_Characters/Bases/Medieval_Assassin_Girl/Base_Medieval_Assassin_Girl.fbx",
                "Assets/MDI_Characters/Bases/Nordic_Einheri/Base_Nordic_Einheri.fbx",
                "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                "Assets/MDI_Characters/Bases/Anime_Akane/Base_Anime_Akane.fbx"
            };

            foreach (string p in searchPaths)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) return go;
            }
            return null;
        }

        private static Transform FindBoneRecursive(Transform current, string name)
        {
            if (current.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return current;

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindBoneRecursive(current.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif

#endif
