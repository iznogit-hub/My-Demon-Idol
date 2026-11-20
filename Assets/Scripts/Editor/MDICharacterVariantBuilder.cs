using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.Characters;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Rebuilds the character variant prefabs from variant_recipe.json.
    /// The YAML prefabs already in the Variants folder are the primary assets.
    /// Run this inside Unity if those overrides need to be regenerated, or to nest
    /// accessory models as real prefab instances instead of the play-mode attach.
    /// </summary>
    public static class MDICharacterVariantBuilder
    {
        const string RecipePath = "Assets/Prefabs/Characters/Variants/variant_recipe.json";

        [MenuItem("Demon Idol/Characters/Rebuild Variant Prefabs From Recipe")]
        public static void RebuildFromRecipe()
        {
            Debug.Log("[MDI Variants] Variant prefabs were retired. This menu no longer writes Assets/Prefabs/Characters/Variants.");
        }

        static void BuildOne(VariantRecipe variant)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variant.basePrefab);
            if (basePrefab == null)
                throw new InvalidOperationException("Missing base prefab " + variant.basePrefab);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = variant.name;
            var root = instance.transform.localScale;
            float scale = variant.rootScale <= 0f ? 1f : variant.rootScale;
            instance.transform.localScale = new Vector3(root.x * scale, root.y * scale, root.z * scale);

            if (variant.disable != null)
            {
                foreach (var part in variant.disable)
                {
                    foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == part)
                            t.gameObject.SetActive(false);
                    }
                }
            }

            if (variant.scales != null)
            {
                foreach (var rule in variant.scales)
                {
                    if (rule == null || string.IsNullOrEmpty(rule.contains) || Mathf.Approximately(rule.factor, 1f))
                        continue;
                    foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                    {
                        if (t == instance.transform || t.GetComponent<Renderer>() != null)
                            continue;
                        if (!MDIVariantLook.IsSafeScaleBone(t.name))
                            continue;
                        if (t.name.IndexOf(rule.contains, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        t.localScale *= rule.factor;
                    }
                }
            }

            if (variant.materials != null)
            {
                foreach (var assign in variant.materials)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(assign.material);
                    if (mat == null)
                    {
                        Debug.LogWarning(variant.name + " missing material " + assign.material);
                        continue;
                    }
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer.gameObject.name != assign.renderer)
                            continue;
                        var mats = renderer.sharedMaterials;
                        if (assign.slot < 0 || assign.slot >= mats.Length)
                            continue;
                        mats[assign.slot] = mat;
                        renderer.sharedMaterials = mats;
                    }
                }
            }

            if (variant.meshSwaps != null)
            {
                foreach (var swap in variant.meshSwaps)
                {
                    Mesh mesh = null;
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(swap.fbx))
                    {
                        if (asset is Mesh candidate && candidate.name == swap.meshName)
                        {
                            mesh = candidate;
                            break;
                        }
                    }
                    if (mesh == null)
                    {
                        Debug.LogWarning(variant.name + " missing mesh " + swap.meshName + " in " + swap.fbx);
                        continue;
                    }
                    foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (smr.gameObject.name != swap.renderer)
                            continue;
                        smr.sharedMesh = mesh;
                    }
                }
            }

            var look = instance.GetComponent<MDIVariantLook>();
            if (look == null)
                look = instance.AddComponent<MDIVariantLook>();
            look.variantId = variant.name;
            look.description = variant.description;
            look.expressions = new List<MDIVariantLook.BlendPreset>();
            if (variant.expressions != null)
            {
                foreach (var exp in variant.expressions)
                {
                    look.expressions.Add(new MDIVariantLook.BlendPreset
                    {
                        shapeName = exp.shapeName,
                        weight = exp.weight
                    });
                }
            }
            look.accessories = new List<MDIVariantLook.AccessoryAttach>();
            if (variant.accessories != null)
            {
                foreach (var acc in variant.accessories)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(acc.prefab);
                    var attach = new MDIVariantLook.AccessoryAttach
                    {
                        prefab = prefab,
                        boneSlot = acc.boneSlot,
                        localPosition = new Vector3(acc.px, acc.py, acc.pz),
                        localEulerAngles = new Vector3(acc.ex, acc.ey, acc.ez),
                        localScale = new Vector3(acc.sx, acc.sy, acc.sz)
                    };
                    look.accessories.Add(attach);
                    if (prefab == null)
                        continue;
                    var bone = MDIVariantLook.FindSlot(instance.transform, acc.boneSlot);
                    if (bone == null)
                    {
                        Debug.LogWarning(variant.name + " has no bone for slot " + acc.boneSlot);
                        continue;
                    }
                    var spawned = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bone);
                    spawned.name = "MDIAcc_" + prefab.name + "_" + acc.boneSlot;
                    spawned.transform.localPosition = attach.localPosition;
                    spawned.transform.localEulerAngles = attach.localEulerAngles;
                    spawned.transform.localScale = attach.localScale;
                }
            }

            string outPath = "Assets/Prefabs/Characters/Variants/Prefabs/" + variant.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, outPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Serializable] public class RecipeFile { public VariantRecipe[] variants; }
    [Serializable] public class VariantRecipe
    {
        public string name;
        public string basePrefab;
        public string description;
        public float rootScale;
        public string[] disable;
        public ScaleRule[] scales;
        public MatAssign[] materials;
        public MeshSwap[] meshSwaps;
        public ExprAssign[] expressions;
        public AccAssign[] accessories;
    }
    [Serializable] public class ScaleRule { public string contains; public float factor; }
    [Serializable] public class MatAssign { public string renderer; public int slot; public string material; }
    [Serializable] public class MeshSwap { public string renderer; public string fbx; public string meshName; }
    [Serializable] public class ExprAssign { public string shapeName; public float weight; }
    [Serializable] public class AccAssign
    {
        public string prefab;
        public string boneSlot;
        public float px, py, pz, ex, ey, ez, sx, sy, sz;
    }
}
