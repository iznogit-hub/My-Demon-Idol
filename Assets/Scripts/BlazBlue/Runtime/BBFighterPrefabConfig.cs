using System;
using System.Collections.Generic;
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
    /// Supports per-fighter AnimatorOverrideController mapping to hot-swap custom AnimationClips
    /// on top of the character's dedicated ExplosiveLLC Warrior Pack AnimatorController.
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

        [Header("Character Original / Custom Clip Overrides - Locomotion & Personality")]
        [Tooltip("Optional custom idle animation clip for this character")]
        public AnimationClip customIdle;
        [Tooltip("Optional custom walk forward animation clip")]
        public AnimationClip customWalkForward;
        [Tooltip("Optional custom walk backward animation clip")]
        public AnimationClip customWalkBack;
        [Tooltip("Optional custom jump animation clip")]
        public AnimationClip customJump;
        [Tooltip("Optional custom block animation clip")]
        public AnimationClip customBlock;
        [Tooltip("Optional custom match intro animation clip")]
        public AnimationClip customIntro;
        [Tooltip("Optional custom victory animation clip")]
        public AnimationClip customVictory;
        [Tooltip("Optional custom taunt animation clip")]
        public AnimationClip customTaunt;

        [Header("Character Original / Custom Clip Overrides - 7-Move Combat Kit")]
        [Tooltip("Optional custom 5A (Light Normal) animation clip")]
        public AnimationClip custom5A;
        [Tooltip("Optional custom 5B (Medium Normal) animation clip")]
        public AnimationClip custom5B;
        [Tooltip("Optional custom 5C (Heavy Normal) animation clip")]
        public AnimationClip custom5C;
        [Tooltip("Optional custom 2C (Crouch Heavy) animation clip")]
        public AnimationClip custom2C;
        [Tooltip("Optional custom j.B (Jump Normal) animation clip")]
        public AnimationClip customJB;
        [Tooltip("Optional custom 5D (Drive Attack) animation clip")]
        public AnimationClip custom5D;
        [Tooltip("Optional custom 236236C (Super Attack) animation clip")]
        public AnimationClip customSuper236236C;

        /// <summary>
        /// Returns true if at least one custom AnimationClip override has been assigned.
        /// </summary>
        public bool HasCustomOverrides()
        {
            return customIdle != null || customWalkForward != null || customWalkBack != null
                || customJump != null || customBlock != null || customIntro != null || customVictory != null || customTaunt != null
                || custom5A != null || custom5B != null || custom5C != null || custom2C != null || customJB != null || custom5D != null || customSuper236236C != null;
        }

        /// <summary>
        /// Builds an AnimatorOverrideController binding all assigned custom AnimationClips
        /// onto the base Warrior controller.
        /// </summary>
        public AnimatorOverrideController CreateRuntimeOverrideController()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return null;

            RuntimeAnimatorController baseCtrl = animator.runtimeAnimatorController;
            if (baseCtrl is AnimatorOverrideController existingAoc)
            {
                baseCtrl = existingAoc.runtimeAnimatorController;
            }
            if (baseCtrl == null) return null;

            var aoc = new AnimatorOverrideController(baseCtrl);
            var clipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(clipOverrides);

            void ApplyMatch(string token, AnimationClip customClip)
            {
                if (customClip == null) return;
                for (int i = 0; i < clipOverrides.Count; i++)
                {
                    var origClip = clipOverrides[i].Key;
                    if (origClip == null) continue;
                    if (origClip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        string.Equals(origClip.name, token, StringComparison.OrdinalIgnoreCase))
                    {
                        clipOverrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(origClip, customClip);
                    }
                }
            }

            ApplyMatch("5A", custom5A);
            ApplyMatch("5B", custom5B);
            ApplyMatch("5C", custom5C);
            ApplyMatch("2C", custom2C);
            ApplyMatch("jB", customJB);
            ApplyMatch("j.B", customJB);
            ApplyMatch("5D", custom5D);
            ApplyMatch("236236C", customSuper236236C);
            ApplyMatch("Super", customSuper236236C);

            ApplyMatch("Idle", customIdle);
            ApplyMatch("WalkForward", customWalkForward);
            ApplyMatch("WalkFwd", customWalkForward);
            ApplyMatch("WalkBackward", customWalkBack);
            ApplyMatch("WalkBack", customWalkBack);
            ApplyMatch("Jump", customJump);
            ApplyMatch("Block", customBlock);
            ApplyMatch("StandBlock", customBlock);
            ApplyMatch("Intro", customIntro);
            ApplyMatch("Victory", customVictory);
            ApplyMatch("Taunt", customTaunt);

            aoc.ApplyOverrides(clipOverrides);
            return aoc;
        }

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

        /// <summary>
        /// Editor utility to immediately generate and apply an AnimatorOverrideController
        /// from the assigned custom animation clips.
        /// </summary>
        [ContextMenu("Demon Idol/Apply Custom Clip Overrides")]
        public void ApplyOverridesInEditor()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[BBFighterPrefabConfig] No Animator found on {gameObject.name}");
                return;
            }

            if (!HasCustomOverrides())
            {
                Debug.Log($"[BBFighterPrefabConfig] {gameObject.name}: No custom clips assigned. Keeping base controller.");
                return;
            }

            var aoc = CreateRuntimeOverrideController();
            if (aoc != null)
            {
                animator.runtimeAnimatorController = aoc;
                overrideController = aoc;
                EditorUtility.SetDirty(gameObject);
                Debug.Log($"<color=#00e676>[BBFighterPrefabConfig] Applied custom clip overrides to {gameObject.name}!</color>");
            }
        }

        /// <summary>
        /// Scans project folders for humanoid animation clips matching this character's name/ID
        /// and reports them in the console.
        /// </summary>
        [ContextMenu("Demon Idol/Auto-Detect Character Original Clips")]
        public void AutoDetectOriginalClips()
        {
            Debug.Log($"<color=#00e676>[BBFighterPrefabConfig] Auto-detecting original clips for {characterId} ({characterDisplayName})...</color>");
            string[] searchFolders = new string[]
            {
                "Assets/MDI_Characters",
                "Assets/Prefabs/Characters",
                "Assets/CombatData"
            };

            string[] guids = AssetDatabase.FindAssets($"{characterDisplayName} t:AnimationClip", searchFolders);
            if (guids.Length == 0 && !string.IsNullOrEmpty(characterId))
            {
                guids = AssetDatabase.FindAssets($"{characterId} t:AnimationClip", searchFolders);
            }

            int foundCount = 0;
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (clip != null && clip.isHumanMotion)
                {
                    foundCount++;
                    Debug.Log($"  [Detected Clip] {clip.name} (length: {clip.length:F2}s, loop: {clip.isLooping}) at: {p}");
                }
            }

            Debug.Log($"<color=#00e676>[BBFighterPrefabConfig] Detection complete: Found {foundCount} candidate humanoid clips for {characterId}.</color>");
        }
#endif
    }
}
