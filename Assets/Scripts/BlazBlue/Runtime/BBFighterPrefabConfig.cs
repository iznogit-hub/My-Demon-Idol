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
    /// Editable per-move 2D AABB hitbox configuration baked from real animation poses & weapon geometry.
    /// Allows fighting game designers to inspect and tweak frame data and hitboxes directly in the Prefab Inspector.
    /// </summary>
    [Serializable]
    public struct MoveHitboxConfig
    {
        [Tooltip("Standard move identifier (5A, 5B, 5C, 2C, j.B, 5D, 236236C)")]
        public string moveId;

        [Header("Frame Timings (60 FPS Ticks)")]
        public int startupTicks;
        public int activeTicks;
        public int recoveryTicks;

        [Header("2D AABB Hitbox (Meters Relative to Fighter Root)")]
        [Tooltip("Center offset (cx, cy) relative to root facing +X")]
        public Vector2 hitboxOffset;
        [Tooltip("Full width (X) and height (Y) of the hitbox")]
        public Vector2 hitboxSize;

        [Header("Projectile Properties")]
        [Tooltip("Spawn offset (X, Y) relative to root for projectile moves")]
        public Vector2 projectileSpawnOffset;
        [Tooltip("Collision radius for projectile")]
        public float projectileRadius;

        [Header("Flags")]
        [Tooltip("Whether this attack box is disjoint (immune to counter-strikes on attacking limb)")]
        public bool isDisjoint;
        [Tooltip("If true, the automated Hitbox Baker will NOT overwrite developer modifications")]
        public bool useCustomOverride;

        public float Reach => hitboxOffset.x + (hitboxSize.x * 0.5f);
    }

    /// <summary>
    /// Lightweight prefab configuration component for fighters.
    /// Provides zero runtime overhead while acting as the single authoritative authoring anchor
    /// in Prefab Mode for animations, 7-move kit, locomotion, weapon rig, and fighter identity.
    /// Supports per-fighter AnimatorOverrideController mapping to hot-swap custom AnimationClips
    /// on top of the character's dedicated ExplosiveLLC Warrior Pack AnimatorController.
    /// Stores baked 2D AABB hitboxes and calibrated body hurtboxes for zero-allocation BBMatchSim queries.
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

        [Header("Baked 2D AABB Hitboxes & Frame Timings")]
        [Tooltip("Baked 7-move hitboxes and timings extracted from Warrior pack geometry & events")]
        public List<MoveHitboxConfig> moveHitboxes = new List<MoveHitboxConfig>();

        [Header("Baked Body Hurtboxes & Pushbox (Center X, Y, Width, Height)")]
        [Tooltip("Standing hurtbox bounds (x=centerX, y=centerY, width=fullWidth, height=fullHeight)")]
        public Rect standingHurtbox;
        [Tooltip("Crouching hurtbox bounds (x=centerX, y=centerY, width=fullWidth, height=fullHeight)")]
        public Rect crouchingHurtbox;
        [Tooltip("Pushbox bounds preventing fighter clipping")]
        public Rect pushbox;

        [Header("Hitbox Visual Debugger")]
        [Tooltip("Show live 2D fighting-plane hitboxes, hurtboxes, pushboxes, and projectile spawn points")]
        public bool showHitboxes = false;
        public static bool GlobalShowHitboxes = false;

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

        /// <summary>
        /// Applies baked hurtboxes, pushbox, and attack hitboxes to the fighter's runtime BBFighterBrain.
        /// Operates deterministically with zero runtime GC allocation.
        /// </summary>
        public void ApplyBakedHitboxesToBrain(BBFighterBrain targetBrain)
        {
            if (targetBrain == null) return;

            // 1. Calibrate body boxes from baked hurtbox / pushbox
            if (standingHurtbox.width > 0.05f)
            {
                if (targetBrain.bodyBoxes == null) targetBrain.bodyBoxes = new BBBodyBoxes();
                float hx = standingHurtbox.width * 0.5f;
                float hy = standingHurtbox.height * 0.5f;

                targetBrain.bodyBoxes.standHurtBoxes = new BBBox[]
                {
                    new BBBox(standingHurtbox.x, standingHurtbox.y + hy * 0.55f, hx * 0.65f, hy * 0.35f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                    new BBBox(standingHurtbox.x, standingHurtbox.y, hx * 0.85f, hy * 0.45f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                    new BBBox(standingHurtbox.x, standingHurtbox.y - hy * 0.55f, hx * 0.90f, hy * 0.40f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
                };
                targetBrain.bodyBoxes.standPushBox = new BBBox(pushbox.x, pushbox.y, pushbox.width * 0.5f, pushbox.height * 0.5f, BBBoxKind.Push);
                targetBrain.bodyBoxes.standThrowHurtBox = new BBBox(standingHurtbox.x, standingHurtbox.y, hx * 0.82f, hy * 0.7f, BBBoxKind.ThrowHurt);

                if (crouchingHurtbox.width > 0.05f)
                {
                    float chx = crouchingHurtbox.width * 0.5f;
                    float chy = crouchingHurtbox.height * 0.5f;
                    targetBrain.bodyBoxes.crouchHurtBoxes = new BBBox[]
                    {
                        new BBBox(crouchingHurtbox.x, crouchingHurtbox.y + chy * 0.45f, chx * 0.60f, chy * 0.35f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                        new BBBox(crouchingHurtbox.x, crouchingHurtbox.y - chy * 0.15f, chx * 0.85f, chy * 0.40f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                        new BBBox(crouchingHurtbox.x, crouchingHurtbox.y - chy * 0.65f, chx * 0.95f, chy * 0.30f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
                    };
                    targetBrain.bodyBoxes.crouchPushBox = new BBBox(crouchingHurtbox.x, crouchingHurtbox.y, chx, chy, BBBoxKind.Push);
                    targetBrain.bodyBoxes.crouchThrowHurtBox = new BBBox(crouchingHurtbox.x, crouchingHurtbox.y, chx * 0.85f, chy * 0.6f, BBBoxKind.ThrowHurt);
                }
            }

            // 2. Calibrate attack boxes for active moves
            if (moveHitboxes != null && moveHitboxes.Count > 0)
            {
                foreach (var m in moveHitboxes)
                {
                    var atk = targetBrain.FindAttackByMoveId(m.moveId);
                    if (atk != null && atk.boxes != null && m.hitboxSize.x > 0.05f)
                    {
                        float hx = m.hitboxSize.x * 0.5f;
                        float hy = m.hitboxSize.y * 0.5f;
                        float cx = m.hitboxOffset.x;
                        float cy = m.hitboxOffset.y;

                        for (int b = 0; b < atk.boxes.boxes.Length; b++)
                        {
                            if (atk.boxes.boxes[b].kind == BBBoxKind.Hit)
                            {
                                atk.boxes.boxes[b].cx = cx;
                                atk.boxes.boxes[b].cy = cy;
                                atk.boxes.boxes[b].hx = hx;
                                atk.boxes.boxes[b].hy = hy;
                                if (m.isDisjoint) atk.boxes.boxes[b].flags |= BBBox.FLAG_DISJOINT;
                            }
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnDrawGizmos()
        {
            if (!showHitboxes && !GlobalShowHitboxes) return;
            DrawHitboxGizmos();
        }

        private void DrawHitboxGizmos()
        {
            Vector3 pos = transform.position;
            int facing = 1;
            var brain = GetComponent<BBFighterBrain>();
            if (brain != null && brain.fighter != null) facing = brain.fighter.FacingDirection;

            // 1. Draw Pushbox (Yellow wireframe)
            if (pushbox.width > 0.05f)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.9f);
                Vector3 pushCenter = pos + new Vector3(pushbox.x * facing, pushbox.y, 0f);
                Gizmos.DrawWireCube(pushCenter, new Vector3(pushbox.width, pushbox.height, 0.1f));
            }

            // 2. Draw Hurtbox (Green semi-transparent + wireframe)
            Rect currentHurt = (brain != null && brain.bbState == BBState.Crouching && crouchingHurtbox.width > 0.05f)
                ? crouchingHurtbox : standingHurtbox;

            if (currentHurt.width > 0.05f)
            {
                Vector3 hurtCenter = pos + new Vector3(currentHurt.x * facing, currentHurt.y, 0f);
                Gizmos.color = new Color(0.1f, 0.95f, 0.2f, 0.25f);
                Gizmos.DrawCube(hurtCenter, new Vector3(currentHurt.width, currentHurt.height, 0.08f));
                Gizmos.color = new Color(0.1f, 0.95f, 0.2f, 0.85f);
                Gizmos.DrawWireCube(hurtCenter, new Vector3(currentHurt.width, currentHurt.height, 0.08f));
            }

            // 3. Draw Attack Hitbox (Red semi-transparent + wireframe)
            if (moveHitboxes != null && moveHitboxes.Count > 0)
            {
                MoveHitboxConfig activeConfig = default;
                bool foundActive = false;

                if (brain != null && brain.currentAttack != null)
                {
                    foreach (var m in moveHitboxes)
                    {
                        if (m.moveId == brain.currentAttack.moveId)
                        {
                            activeConfig = m;
                            foundActive = true;
                            break;
                        }
                    }
                }
                else
                {
                    activeConfig = moveHitboxes[0]; // Preview 5A in Editor
                    foundActive = true;
                }

                if (foundActive && activeConfig.hitboxSize.x > 0.05f)
                {
                    Vector3 hitCenter = pos + new Vector3(activeConfig.hitboxOffset.x * facing, activeConfig.hitboxOffset.y, 0f);
                    Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.35f);
                    Gizmos.DrawCube(hitCenter, new Vector3(activeConfig.hitboxSize.x, activeConfig.hitboxSize.y, 0.12f));
                    Gizmos.color = new Color(1f, 0.05f, 0.05f, 0.95f);
                    Gizmos.DrawWireCube(hitCenter, new Vector3(activeConfig.hitboxSize.x, activeConfig.hitboxSize.y, 0.12f));

                    // 4. Draw Projectile Spawn Point (Cyan crosshair)
                    if (activeConfig.projectileSpawnOffset != Vector2.zero)
                    {
                        Vector3 spawnPos = pos + new Vector3(activeConfig.projectileSpawnOffset.x * facing, activeConfig.projectileSpawnOffset.y, 0f);
                        Gizmos.color = Color.cyan;
                        float r = activeConfig.projectileRadius > 0.05f ? activeConfig.projectileRadius : 0.2f;
                        Gizmos.DrawWireSphere(spawnPos, r);
                        Gizmos.DrawLine(spawnPos - Vector3.right * 0.15f, spawnPos + Vector3.right * 0.15f);
                        Gizmos.DrawLine(spawnPos - Vector3.up * 0.15f, spawnPos + Vector3.up * 0.15f);
                    }
                }
            }
        }
#endif

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
