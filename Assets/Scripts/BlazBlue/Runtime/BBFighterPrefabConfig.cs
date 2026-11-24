using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Visual;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Lightweight prefab configuration component for fighters.
    /// Provides zero runtime overhead while acting as the single authoritative authoring anchor
    /// in Prefab Mode for animations, 7-move kit, locomotion, weapon rig, and fighter identity.
    /// </summary>
    [DisallowMultipleComponent]
    public class BBFighterPrefabConfig : MonoBehaviour
    {
        [Header("Fighter Identity")]
        public string characterId = "";
        public string characterDisplayName = "";

        [Header("Linked Core Assets")]
        public BBCharacterProfile profile;
        public CharacterDefinition characterDefinition;
        public AnimatorOverrideController overrideController;

        [Header("Rig & Subcomponents")]
        public Animator animator;
        public BBWeaponRig weaponRig;

#if UNITY_EDITOR
        private void Reset()
        {
            ResolveLinkedAssets();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(characterId) || profile == null || characterDefinition == null || animator == null)
            {
                ResolveLinkedAssets();
            }
        }

        /// <summary>
        /// Automatically resolves linked ScriptableObjects and components based on GameObject/prefab name.
        /// </summary>
        public void ResolveLinkedAssets()
        {
            // 1. Resolve Character ID and Display Name
            if (string.IsNullOrEmpty(characterId))
            {
                string goName = gameObject.name.Replace("(Clone)", "").Trim();
                if (goName.StartsWith("C") || goName.StartsWith("EX") || goName.StartsWith("SK"))
                {
                    int underscore = goName.IndexOf('_');
                    if (underscore > 0)
                    {
                        characterId = goName.Substring(0, underscore);
                        characterDisplayName = goName.Substring(underscore + 1);
                    }
                    else
                    {
                        characterId = goName;
                        characterDisplayName = goName;
                    }
                }
                else
                {
                    characterId = goName;
                    characterDisplayName = goName;
                }
            }

            // 2. Resolve Animator
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            // 3. Resolve BBWeaponRig
            if (weaponRig == null)
            {
                weaponRig = GetComponentInChildren<BBWeaponRig>(true);
            }

            // 4. Resolve CharacterDefinition
            if (characterDefinition == null && !string.IsNullOrEmpty(characterId))
            {
                string[] defGuids = AssetDatabase.FindAssets($"CharacterDefinition_{characterId} t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
                if (defGuids.Length == 0)
                {
                    defGuids = AssetDatabase.FindAssets($"{characterId} t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
                }
                if (defGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(defGuids[0]);
                    characterDefinition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                }
            }

            // 5. Resolve BBProfile
            if (profile == null)
            {
                if (characterDefinition != null && characterDefinition.blazBlueProfile is BBCharacterProfile cp)
                {
                    profile = cp;
                }
                else if (!string.IsNullOrEmpty(characterId))
                {
                    string[] profGuids = AssetDatabase.FindAssets($"BBProfile_{characterId} t:BBCharacterProfile", new[] { "Assets/CombatData/BlazBlue" });
                    if (profGuids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(profGuids[0]);
                        profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(path);
                    }
                }
            }

            // 6. Resolve AnimatorOverrideController
            if (overrideController == null)
            {
                if (animator != null && animator.runtimeAnimatorController is AnimatorOverrideController aoc)
                {
                    overrideController = aoc;
                }
                else if (profile != null && profile.blazBlueOverrideController != null)
                {
                    overrideController = profile.blazBlueOverrideController;
                }
                else if (characterDefinition != null && characterDefinition.animatorController is AnimatorOverrideController defAoc)
                {
                    overrideController = defAoc;
                }
                else if (!string.IsNullOrEmpty(characterDisplayName))
                {
                    string[] aocGuids = AssetDatabase.FindAssets($"{characterDisplayName}_BB_Override t:AnimatorOverrideController", new[] { "Assets/MDI_Characters/Runtime/BlazBlue" });
                    if (aocGuids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(aocGuids[0]);
                        overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
                    }
                }
            }
        }
#endif
    }
}
