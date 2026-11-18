using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Characters
{
    /// <summary>
    /// Visual preset for an additive character variant.
    /// Applies blend-shape expressions and parents accessory models to hand, head, or spine bones.
    /// Combat code does not reference this component. Existing fighter prefabs do not use it.
    /// </summary>
    [DisallowMultipleComponent]
    public class MDIVariantLook : MonoBehaviour
    {
        [TextArea(2, 6)] public string description;
        public string variantId;

        public List<BlendPreset> expressions = new List<BlendPreset>();
        public List<AccessoryAttach> accessories = new List<AccessoryAttach>();

        [Serializable]
        public class BlendPreset
        {
            public string shapeName;
            public float weight = 100f;
        }

        [Serializable]
        public class AccessoryAttach
        {
            public GameObject prefab;
            public string boneSlot;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
        }

        static readonly Dictionary<string, string[]> Slots = new Dictionary<string, string[]>
        {
            { "rightHand", new[] { "Character1_RightHand", "RightHand", "HandR", "Hand_R", "Hand.R", "hand.R", "hand_r", "Right wrist", "RightWrist" } },
            { "leftHand", new[] { "Character1_LeftHand", "LeftHand", "HandL", "Hand_L", "Hand.L", "hand.L", "hand_l", "Left wrist", "LeftWrist" } },
            { "head", new[] { "Character1_Head", "Head", "head" } },
            { "spine", new[] { "Character1_Spine2", "Spine2", "spine_02", "Character1_Spine", "Spine", "Chest", "Upper Chest" } },
        };

        void Awake()
        {
            if (Application.isPlaying)
                ApplyRuntime();
        }

        public void ApplyRuntime()
        {
            ApplyExpressions();
            AttachAccessories();
        }

        public void ApplyExpressions()
        {
            if (expressions == null)
                return;
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var exp in expressions)
            {
                if (exp == null || string.IsNullOrEmpty(exp.shapeName))
                    continue;
                foreach (var smr in renderers)
                {
                    if (smr.sharedMesh == null)
                        continue;
                    int index = smr.sharedMesh.GetBlendShapeIndex(exp.shapeName);
                    if (index >= 0)
                        smr.SetBlendShapeWeight(index, exp.weight);
                }
            }
        }

        public void AttachAccessories()
        {
            if (accessories == null)
                return;
            foreach (var acc in accessories)
            {
                if (acc == null || acc.prefab == null || string.IsNullOrEmpty(acc.boneSlot))
                    continue;
                var bone = FindSlot(transform, acc.boneSlot);
                if (bone == null)
                    continue;
                string marker = "MDIAcc_" + acc.prefab.name + "_" + acc.boneSlot;
                if (bone.Find(marker) != null)
                    continue;
                var inst = Instantiate(acc.prefab, bone);
                inst.name = marker;
                inst.transform.localPosition = acc.localPosition;
                inst.transform.localEulerAngles = acc.localEulerAngles;
                inst.transform.localScale = acc.localScale == Vector3.zero ? Vector3.one : acc.localScale;
            }
        }

        public static Transform FindSlot(Transform root, string slot)
        {
            if (root == null || string.IsNullOrEmpty(slot) || !Slots.TryGetValue(slot, out var names))
                return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var candidate in names)
            {
                foreach (var t in all)
                {
                    if (t.name != candidate)
                        continue;
                    if (t.GetComponent<Renderer>() != null)
                        continue;
                    return t;
                }
            }
            foreach (var candidate in names)
            {
                foreach (var t in all)
                {
                    if (t.name == candidate)
                        return t;
                }
            }
            return null;
        }

        public static bool IsSafeScaleBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return false;
            string n = boneName.ToLowerInvariant();
            if (n == "head" || n == "character1_head" || n == "armature" || n == "root")
                return false;
            string[] bad =
            {
                "finger", "wrist", "hand", "arm", "leg", "spine", "chest", "neck", "hips",
                "thigh", "foot", "toe", "shoulder", "pelvis", "forearm", "shin", "hips"
            };
            foreach (var token in bad)
            {
                if (n.Contains(token))
                    return false;
            }
            string[] good = { "hair", "ribbon", "hood", "goggle", "picolabo", "bag", "angel", "hat", "bang", "braid", "ponytail", "earling", "skirt", "tail" };
            foreach (var token in good)
            {
                if (n.Contains(token))
                    return true;
            }
            return false;
        }
    }
}
