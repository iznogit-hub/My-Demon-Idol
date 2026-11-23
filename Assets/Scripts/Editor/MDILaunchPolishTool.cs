#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DemonIdol.Combat;
using DemonIdol.Data;

namespace DemonIdol.Editor
{
    public static class MDILaunchPolishTool
    {
        private static readonly Dictionary<string, float> AuthoredHeights = new Dictionary<string, float>
        {
            { "C01", 1.75f },
            { "C02", 1.65f },
            { "C04", 1.63f },
            { "C05", 1.68f },
            { "C06", 1.76f },
            { "C07", 1.62f },
            { "C08", 1.66f },
            { "C09", 1.60f },
            { "C10", 1.72f },
            { "C11", 1.70f },
            { "C12", 1.68f },
            { "C13", 1.62f },
            { "C14", 1.80f },
            { "SK02", 1.82f },
            { "SK03", 1.74f },
            { "SK05", 1.78f },
            { "SK06", 1.82f },
            { "SK08", 1.64f },
            { "EX01", 1.85f },
            { "EX02", 1.84f },
            { "EX03", 1.75f },
            { "EX04", 1.78f },
            { "EX05", 1.70f },
            { "EX06", 1.83f },
            { "EX07", 1.67f }
        };

        [MenuItem("Tools/MDI/Debug EX06")]
        public static void DebugEX06()
        {
            FixEX06AvatarMapping();
        }

        public static void FixEX06AvatarMapping()
        {
            string prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX06_Sentry.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            GameObject go = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Transform[] allT = go.GetComponentsInChildren<Transform>(true);
                Transform rootX = allT.FirstOrDefault(t => t.name == "root.x");
                Transform spine01 = allT.FirstOrDefault(t => t.name == "spine_01.x");
                Transform spine02 = allT.FirstOrDefault(t => t.name == "spine_02.x");
                Transform spine03 = allT.FirstOrDefault(t => t.name == "spine_03.x");
                Transform neckX = allT.FirstOrDefault(t => t.name == "neck.x");
                Transform headX = allT.FirstOrDefault(t => t.name == "head.x");

                Transform shoulderL = allT.FirstOrDefault(t => t.name == "shoulder.l");
                Transform armTwistL = allT.FirstOrDefault(t => t.name == "arm_twist.l");
                Transform forearmL = allT.FirstOrDefault(t => t.name == "forearm.l");
                Transform handL = allT.FirstOrDefault(t => t.name == "hand.l");

                Transform shoulderR = allT.FirstOrDefault(t => t.name == "shoulder.r");
                Transform armTwistR = allT.FirstOrDefault(t => t.name == "arm_twist.r");
                Transform forearmR = allT.FirstOrDefault(t => t.name == "forearm.r");
                Transform handR = allT.FirstOrDefault(t => t.name == "hand.r");

                Transform thighTwistL = allT.FirstOrDefault(t => t.name == "thigh_twist.l");
                Transform legFkL = allT.FirstOrDefault(t => t.name == "leg_fk.l");
                Transform footL = allT.FirstOrDefault(t => t.name == "foot.l");
                Transform toesL = allT.FirstOrDefault(t => t.name == "toes_01.l");

                Transform thighTwistR = allT.FirstOrDefault(t => t.name == "thigh_twist.r");
                Transform legFkR = allT.FirstOrDefault(t => t.name == "leg_fk.r");
                Transform footR = allT.FirstOrDefault(t => t.name == "foot.r");
                Transform toesR = allT.FirstOrDefault(t => t.name == "toes_01.r");

                // Establish clean humanoid hierarchy
                if (rootX != null)
                {
                    if (spine01 != null) spine01.SetParent(rootX, true);
                    if (thighTwistL != null) thighTwistL.SetParent(rootX, true);
                    if (thighTwistR != null) thighTwistR.SetParent(rootX, true);
                }

                if (spine01 != null && spine02 != null) spine02.SetParent(spine01, true);
                if (spine02 != null && spine03 != null) spine03.SetParent(spine02, true);

                if (spine03 != null)
                {
                    if (neckX != null) neckX.SetParent(spine03, true);
                    if (shoulderL != null) shoulderL.SetParent(spine03, true);
                    if (shoulderR != null) shoulderR.SetParent(spine03, true);
                }

                if (neckX != null && headX != null) headX.SetParent(neckX, true);

                if (shoulderL != null && armTwistL != null) armTwistL.SetParent(shoulderL, true);
                if (armTwistL != null && forearmL != null) forearmL.SetParent(armTwistL, true);
                if (forearmL != null && handL != null) handL.SetParent(forearmL, true);

                if (shoulderR != null && armTwistR != null) armTwistR.SetParent(shoulderR, true);
                if (armTwistR != null && forearmR != null) forearmR.SetParent(armTwistR, true);
                if (forearmR != null && handR != null) handR.SetParent(forearmR, true);

                if (thighTwistL != null && legFkL != null) legFkL.SetParent(thighTwistL, true);
                if (legFkL != null && footL != null) footL.SetParent(legFkL, true);
                if (footL != null && toesL != null) toesL.SetParent(footL, true);

                if (thighTwistR != null && legFkR != null) legFkR.SetParent(thighTwistR, true);
                if (legFkR != null && footR != null) footR.SetParent(legFkR, true);
                if (footR != null && toesR != null) toesR.SetParent(footR, true);

                var humanBones = new List<HumanBone>
                {
                    new HumanBone { humanName = "Hips", boneName = "root.x", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Spine", boneName = "spine_01.x", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Chest", boneName = "spine_02.x", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "UpperChest", boneName = "spine_03.x", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Neck", boneName = "neck.x", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Head", boneName = "head.x", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "LeftUpperLeg", boneName = "thigh_twist.l", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftLowerLeg", boneName = "leg_fk.l", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftFoot", boneName = "foot.l", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftToes", boneName = "toes_01.l", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "RightUpperLeg", boneName = "thigh_twist.r", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightLowerLeg", boneName = "leg_fk.r", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightFoot", boneName = "foot.r", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightToes", boneName = "toes_01.r", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "LeftShoulder", boneName = "shoulder.l", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftUpperArm", boneName = "arm_twist.l", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftLowerArm", boneName = "forearm.l", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftHand", boneName = "hand.l", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "RightShoulder", boneName = "shoulder.r", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightUpperArm", boneName = "arm_twist.r", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightLowerArm", boneName = "forearm.r", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightHand", boneName = "hand.r", limit = new HumanLimit { useDefaultValues = true } },
                };

                var skeletonBones = new List<SkeletonBone>();
                AddSkeletonBonesRecursive(go.transform, skeletonBones);

                var hd = new HumanDescription
                {
                    human = humanBones.ToArray(),
                    skeleton = skeletonBones.ToArray(),
                    armStretch = 0.05f,
                    legStretch = 0.05f,
                    feetSpacing = 0f,
                    hasTranslationDoF = false
                };

                Avatar avatar = AvatarBuilder.BuildHumanAvatar(go, hd);
                if (avatar != null && avatar.isValid && avatar.isHuman)
                {
                    avatar.name = "EX06_Sentry_Avatar";
                    string avatarPath = "Assets/CombatData/Characters/EX06_Sentry_Avatar.asset";
                    AssetDatabase.CreateAsset(avatar, avatarPath);
                    Debug.Log($"<color=#00e676>[MDILaunchPolishTool] Created Humanoid Avatar for EX06 at {avatarPath} (isValid={avatar.isValid}, isHuman={avatar.isHuman})</color>");

                    var anim = go.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.avatar = avatar;
                    }

                    PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

                    var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_EX06_Sentry.asset");
                    if (charDef != null)
                    {
                        charDef.avatar = avatar;
                        EditorUtility.SetDirty(charDef);
                    }
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.LogError($"[MDILaunchPolishTool] EX06 BuildHumanAvatar failed! valid={avatar?.isValid} human={avatar?.isHuman}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [MenuItem("Tools/MDI/Apply Launch Polish (Visuals, Roster, Prefabs)")]
        public static void ApplyLaunchPolish()
        {
            Debug.Log("<color=#00e676>[MDILaunchPolishTool] Starting Launch Polish pass...</color>");

            // 1. Configure EX07 Avatar Mapping on Female 1.fbx
            FixEX07AvatarMapping();

            // 1b. Configure EX02 Avatar Mapping
            FixEX02AvatarMapping();

            // 1c. Configure EX06 Avatar Mapping
            FixEX06AvatarMapping();

            // 2. Fix Character Prefabs (strip nested CharacterController, FighterController, MobileFighterInput)
            FixCharacterPrefabs();

            // 3. Fix C07 Definition Text
            FixC07Definition();

            // 4. Set authored heightMeters across all 25 CharacterDefinition assets
            ApplyCharacterHeights();

            // 5. Re-save Roster Database
            if (MDIRosterDatabase.Instance != null)
            {
                EditorUtility.SetDirty(MDIRosterDatabase.Instance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676>[MDILaunchPolishTool] Launch Polish pass completed successfully!</color>");
        }

        public static void FixEX07AvatarMapping()
        {
            string fbxPath = "Assets/Shokubutsu Studio/Low Poly Female Free/Meshes/Smooth Shaded/Female 1.fbx";
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[MDILaunchPolishTool] ModelImporter not found at {fbxPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;

            var humanBones = new List<HumanBone>
            {
                new HumanBone { humanName = "Hips", boneName = "spine" },
                new HumanBone { humanName = "Spine", boneName = "spine.001" },
                new HumanBone { humanName = "Chest", boneName = "spine.002" },
                new HumanBone { humanName = "UpperChest", boneName = "spine.003" },
                new HumanBone { humanName = "Neck", boneName = "spine.004" },
                new HumanBone { humanName = "Head", boneName = "spine.005" },

                new HumanBone { humanName = "LeftUpperLeg", boneName = "thigh.L" },
                new HumanBone { humanName = "LeftLowerLeg", boneName = "shin.L" },
                new HumanBone { humanName = "LeftFoot", boneName = "foot.L" },
                new HumanBone { humanName = "LeftToes", boneName = "toe.L" },

                new HumanBone { humanName = "RightUpperLeg", boneName = "thigh.R" },
                new HumanBone { humanName = "RightLowerLeg", boneName = "shin.R" },
                new HumanBone { humanName = "RightFoot", boneName = "foot.R" },
                new HumanBone { humanName = "RightToes", boneName = "toe.R" },

                new HumanBone { humanName = "LeftShoulder", boneName = "shoulder.L" },
                new HumanBone { humanName = "LeftUpperArm", boneName = "upper_arm.L" },
                new HumanBone { humanName = "LeftLowerArm", boneName = "forearm.L" },
                new HumanBone { humanName = "LeftHand", boneName = "hand.L" },

                new HumanBone { humanName = "RightShoulder", boneName = "shoulder.R" },
                new HumanBone { humanName = "RightUpperArm", boneName = "upper_arm.R" },
                new HumanBone { humanName = "RightLowerArm", boneName = "forearm.R" },
                new HumanBone { humanName = "RightHand", boneName = "hand.R" },

                new HumanBone { humanName = "LeftThumbProximal", boneName = "thumb.02.L" },
                new HumanBone { humanName = "LeftThumbIntermediate", boneName = "thumb.03.L" },
                new HumanBone { humanName = "RightThumbProximal", boneName = "thumb.02.R" },
                new HumanBone { humanName = "RightThumbIntermediate", boneName = "thumb.03.R" },

                new HumanBone { humanName = "LeftIndexProximal", boneName = "f_index.01.L" },
                new HumanBone { humanName = "LeftIndexIntermediate", boneName = "f_index.02.L" },
                new HumanBone { humanName = "LeftIndexDistal", boneName = "f_index.03.L" },

                new HumanBone { humanName = "RightIndexProximal", boneName = "f_index.01.R" },
                new HumanBone { humanName = "RightIndexIntermediate", boneName = "f_index.02.R" },
                new HumanBone { humanName = "RightIndexDistal", boneName = "f_index.03.R" },

                new HumanBone { humanName = "LeftMiddleProximal", boneName = "f_middle.01.L" },
                new HumanBone { humanName = "LeftMiddleIntermediate", boneName = "f_middle.02.L" },
                new HumanBone { humanName = "LeftMiddleDistal", boneName = "f_middle.03.L" },

                new HumanBone { humanName = "RightMiddleProximal", boneName = "f_middle.01.R" },
                new HumanBone { humanName = "RightMiddleIntermediate", boneName = "f_middle.02.R" },
                new HumanBone { humanName = "RightMiddleDistal", boneName = "f_middle.03.R" },
            };

            HumanDescription desc = importer.humanDescription;
            desc.human = humanBones.ToArray();
            importer.humanDescription = desc;
            importer.SaveAndReimport();

            Debug.Log($"<color=#00e676>[MDILaunchPolishTool] Reimported {fbxPath} with mapped Humanoid avatar ({humanBones.Count} bones).</color>");
        }

        public static void FixEX02AvatarMapping()
        {
            string prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX02_Granite.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MDILaunchPolishTool] EX02 prefab not found at {prefabPath}");
                return;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var humanBones = new List<HumanBone>
                {
                    new HumanBone { humanName = "Hips", boneName = "Bip001 Pelvis", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Spine", boneName = "Bip001 Spine", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Chest", boneName = "Bip001 Spine1", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "UpperChest", boneName = "Bip001 Spine2", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Neck", boneName = "Bip001 Neck", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "Head", boneName = "Bip001 Head", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "LeftUpperLeg", boneName = "Bip001 L Thigh", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftLowerLeg", boneName = "Bip001 L Calf", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftFoot", boneName = "Bip001 L Foot", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftToes", boneName = "Bip001 L Toe0", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "RightUpperLeg", boneName = "Bip001 R Thigh", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightLowerLeg", boneName = "Bip001 R Calf", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightFoot", boneName = "Bip001 R Foot", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightToes", boneName = "Bip001 R Toe0", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "LeftShoulder", boneName = "Bip001 L Clavicle", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftUpperArm", boneName = "Bip001 L UpperArm", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftLowerArm", boneName = "Bip001 L Forearm", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftHand", boneName = "Bip001 L Hand", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "RightShoulder", boneName = "Bip001 R Clavicle", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightUpperArm", boneName = "Bip001 R UpperArm", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightLowerArm", boneName = "Bip001 R Forearm", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightHand", boneName = "Bip001 R Hand", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "LeftThumbProximal", boneName = "Bip001 L Finger0", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftThumbIntermediate", boneName = "Bip001 L Finger01", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftIndexProximal", boneName = "Bip001 L Finger1", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftIndexIntermediate", boneName = "Bip001 L Finger11", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftMiddleProximal", boneName = "Bip001 L Finger2", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftMiddleIntermediate", boneName = "Bip001 L Finger21", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftRingProximal", boneName = "Bip001 L Finger3", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftRingIntermediate", boneName = "Bip001 L Finger31", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftLittleProximal", boneName = "Bip001 L Finger4", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "LeftLittleIntermediate", boneName = "Bip001 L Finger41", limit = new HumanLimit { useDefaultValues = true } },

                    new HumanBone { humanName = "RightThumbProximal", boneName = "Bip001 R Finger0", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightThumbIntermediate", boneName = "Bip001 R Finger01", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightIndexProximal", boneName = "Bip001 R Finger1", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightIndexIntermediate", boneName = "Bip001 R Finger11", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightMiddleProximal", boneName = "Bip001 R Finger2", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightMiddleIntermediate", boneName = "Bip001 R Finger21", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightRingProximal", boneName = "Bip001 R Finger3", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightRingIntermediate", boneName = "Bip001 R Finger31", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightLittleProximal", boneName = "Bip001 R Finger4", limit = new HumanLimit { useDefaultValues = true } },
                    new HumanBone { humanName = "RightLittleIntermediate", boneName = "Bip001 R Finger41", limit = new HumanLimit { useDefaultValues = true } },
                };

                var skeletonBones = new List<SkeletonBone>();
                AddSkeletonBonesRecursive(go.transform, skeletonBones);

                var hd = new HumanDescription
                {
                    human = humanBones.ToArray(),
                    skeleton = skeletonBones.ToArray(),
                    armStretch = 0.05f,
                    legStretch = 0.05f,
                    feetSpacing = 0f,
                    hasTranslationDoF = false
                };

                Avatar avatar = AvatarBuilder.BuildHumanAvatar(go, hd);
                if (avatar != null && avatar.isValid)
                {
                    avatar.name = "EX02_Granite_Avatar";
                    string avatarPath = "Assets/CombatData/Characters/EX02_Granite_Avatar.asset";
                    AssetDatabase.CreateAsset(avatar, avatarPath);
                    Debug.Log($"<color=#00e676>[MDILaunchPolishTool] Created Humanoid Avatar at {avatarPath} (isValid={avatar.isValid}, isHuman={avatar.isHuman})</color>");

                    var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_EX02_Granite.asset");
                    if (charDef != null)
                    {
                        charDef.avatar = avatar;
                        EditorUtility.SetDirty(charDef);
                    }

                    var anim = prefab.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.avatar = avatar;
                        EditorUtility.SetDirty(prefab);
                    }
                }
                else
                {
                    Debug.LogError($"[MDILaunchPolishTool] BuildHumanAvatar failed! valid={avatar?.isValid} human={avatar?.isHuman}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void AddSkeletonBonesRecursive(Transform t, List<SkeletonBone> list)
        {
            list.Add(new SkeletonBone
            {
                name = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale
            });
            for (int i = 0; i < t.childCount; i++)
            {
                AddSkeletonBonesRecursive(t.GetChild(i), list);
            }
        }

        public static void FixCharacterPrefabs()
        {
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" });
            foreach (var guid in prefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Fighter_Player")) continue; // Keep player wrappers intact if any
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                bool modified = false;
                foreach (var fc in prefab.GetComponentsInChildren<FighterController>(true))
                {
                    UnityEngine.Object.DestroyImmediate(fc, true);
                    modified = true;
                }
                foreach (var cc in prefab.GetComponentsInChildren<CharacterController>(true))
                {
                    UnityEngine.Object.DestroyImmediate(cc, true);
                    modified = true;
                }
                foreach (var mi in prefab.GetComponentsInChildren<DemonIdol.Combat.MobileFighterInput>(true))
                {
                    UnityEngine.Object.DestroyImmediate(mi, true);
                    modified = true;
                }

                if (modified)
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                    Debug.Log($"<color=#00e676>[MDILaunchPolishTool] Cleaned nested logic/physics components from {path}.</color>");
                }
            }
        }

        public static void FixC07Definition()
        {
            string defPath = "Assets/CombatData/Characters/CharacterDefinition_C07_StellarWitch.asset";
            var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(defPath);
            if (def != null)
            {
                def.title = "The Star-Caster";
                def.ufeCharacterName = "StellarWitch";
                def.domainName = "Astral Renewal Matrix";
                def.description = "Graceful astral spellcaster mastering celestial summons, omnidirectional starbursts, and cosmic nova domain expansions.";
                def.heightMeters = 1.62f;
                EditorUtility.SetDirty(def);
                Debug.Log($"<color=#00e676>[MDILaunchPolishTool] Updated C07 CharacterDefinition text and domain.</color>");
            }
        }

        public static void ApplyCharacterHeights()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (def != null && AuthoredHeights.TryGetValue(def.id, out float h))
                {
                    def.heightMeters = h;
                    EditorUtility.SetDirty(def);
                }
            }
            Debug.Log($"<color=#00e676>[MDILaunchPolishTool] Applied calibrated heightMeters to all 25 CharacterDefinition assets.</color>");
        }

        public static void DiagnoseC01Bounds()
        {
            var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset");
            if (def == null) { Debug.LogError("C01 def is null"); return; }
            var prefab = def.modelPrefab;
            if (prefab == null) { Debug.LogError("C01 prefab is null"); return; }

            var go = new GameObject("TestFighter");
            var fc = go.AddComponent<FighterController>();
            var model = UnityEngine.Object.Instantiate(prefab, go.transform);
            model.name = "Model_C01";

            Debug.Log($"[DiagnoseC01Bounds] Model instantiated: {model.name}. Renderers count: {model.GetComponentsInChildren<Renderer>(true).Length}");
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"[DiagnoseC01Bounds] Renderer: {r.GetType().Name} on '{r.gameObject.name}', enabled={r.enabled}, bounds={r.bounds.size}, min={r.bounds.min}, max={r.bounds.max}");
                if (r is SkinnedMeshRenderer smr)
                {
                    Debug.Log($"[DiagnoseC01Bounds] SMR sharedMesh={smr.sharedMesh?.name}, meshBounds={smr.sharedMesh?.bounds.size}, rootBone={smr.rootBone?.name}");
                }
            }

            HomegrownCombatDirector.CalibrateModelScalingAndGrounding(model, fc, def);
            Debug.Log($"[DiagnoseC01Bounds] After CalibrateModelScalingAndGrounding: localScale={model.transform.localScale}, localPos={model.transform.localPosition}");
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"[DiagnoseC01Bounds] After Calibrate Renderer: {r.GetType().Name} on '{r.gameObject.name}', bounds={r.bounds.size}, min={r.bounds.min}, max={r.bounds.max}");
            }

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
#endif
