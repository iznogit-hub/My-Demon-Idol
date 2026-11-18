using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Characters
{
    public enum FighterGender
    {
        Male,
        Female
    }

    public enum HumanoidBoneTarget
    {
        Head,
        Neck,
        Spine,
        Chest,
        Hips,
        LeftShoulder,
        RightShoulder,
        LeftHand,
        RightHand
    }

    [Serializable]
    public class AccessoryAttachment
    {
        public string accessoryName = "Accessory";
        public GameObject accessoryPrefab;
        public HumanoidBoneTarget targetBone = HumanoidBoneTarget.Head;
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localEulerAngles = Vector3.zero;
        public Vector3 localScale = Vector3.one;
    }

    [CreateAssetMenu(fileName = "FighterDefinition", menuName = "Demon Idol/MDI/Fighter Definition")]
    public class FighterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "ORC_ARIN";
        public string displayName = "Arin the Iron Vow";
        public string clan = "Orc";
        public FighterGender gender = FighterGender.Male;

        [Header("Prefabs & Mesh Sources")]
        public GameObject basePrefab;
        public GameObject hairPrefab;
        public List<AccessoryAttachment> accessories = new List<AccessoryAttachment>();
        public GameObject assembledPrefab;

        [Header("Toon Shader Tint Colors")]
        [Tooltip("Maps to Red channel in tint mask")]
        public Color skinTint = Color.white;
        [Tooltip("Maps to Green channel in tint mask")]
        public Color hairTint = Color.white;
        [Tooltip("Maps to Blue channel in tint mask")]
        public Color primaryTint = Color.white;
        [Tooltip("Maps to Alpha channel in tint mask")]
        public Color secondaryTint = Color.white;
        [Tooltip("Accent detailing tint")]
        public Color accentTint = Color.white;

        [Header("Neon Emissive")]
        [ColorUsage(true, true)]
        public Color emissiveNeonColor = new Color(1f, 0.33f, 0f, 1f);
        [Range(0.5f, 5f)]
        public float emissiveIntensity = 2.0f;

        [Header("Animation & UI")]
        public RuntimeAnimatorController animatorController;
        public Sprite portrait;

        /// <summary>
        /// Converts HumanoidBoneTarget enum to Unity's native HumanBodyBones.
        /// </summary>
        public static HumanBodyBones ToHumanBodyBones(HumanoidBoneTarget target)
        {
            switch (target)
            {
                case HumanoidBoneTarget.Head: return HumanBodyBones.Head;
                case HumanoidBoneTarget.Neck: return HumanBodyBones.Neck;
                case HumanoidBoneTarget.Spine: return HumanBodyBones.Spine;
                case HumanoidBoneTarget.Chest: return HumanBodyBones.Chest;
                case HumanoidBoneTarget.Hips: return HumanBodyBones.Hips;
                case HumanoidBoneTarget.LeftShoulder: return HumanBodyBones.LeftShoulder;
                case HumanoidBoneTarget.RightShoulder: return HumanBodyBones.RightShoulder;
                case HumanoidBoneTarget.LeftHand: return HumanBodyBones.LeftHand;
                case HumanoidBoneTarget.RightHand: return HumanBodyBones.RightHand;
                default: return HumanBodyBones.Head;
            }
        }
    }
}
