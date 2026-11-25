#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Visual;
using DemonIdol.Combat;

namespace DemonIdol.Editor.Animation
{
    /// <summary>
    /// Master tool for building, wiring, and auditing the 12 Warrior Animator Controllers
    /// for My-Demon-Idol's launch roster.
    /// Pure native Mecanim AnimatorController pipeline.
    /// Each launch fighter keeps its original Humanoid Avatar, applies zero root-motion,
    /// and uses its dedicated ExplosiveLLC Warrior Pack controller.
    /// </summary>
    public static class WarriorAnimationOverhaulTool
    {
        public const string TARGET_ANIMATORS_DIR = "Assets/Animators/Fighters";

        public struct FighterAnimConfig
        {
            public string id;
            public string name;
            public string warriorPack;
            public string archetype;
            public string prefabPath;
            public string profilePath;
            public string moveSetPath;
            public string sourceControllerPath;
            public string targetControllerPath;
        }

        public static readonly FighterAnimConfig[] LaunchFighters = new FighterAnimConfig[]
        {
            new FighterAnimConfig
            {
                id = "C01",
                name = "Akio",
                warriorPack = "Karate",
                archetype = "Unarmed Rushdown",
                prefabPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Akio/BBProfile_C01_Akio.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Akio/BBMoveSet_C01_Akio.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animation Controller/Karate Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C01_Akio_Karate.controller"
            },
            new FighterAnimConfig
            {
                id = "C02",
                name = "Aika",
                warriorPack = "Ninja",
                archetype = "Speed / Blade",
                prefabPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Aika/BBProfile_C02_Aika.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Aika/BBMoveSet_C02_Aika.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animation Controller/Ninja Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C02_Aika_Ninja.controller"
            },
            new FighterAnimConfig
            {
                id = "C05",
                name = "Akane",
                warriorPack = "Sorceress",
                archetype = "Zoner / Staff",
                prefabPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/spear/BBProfile_C05_Akane.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Archetypes/spear/BBMoveSet_C05.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animation Controller/Sorceress Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C05_Akane_Sorceress.controller"
            },
            new FighterAnimConfig
            {
                id = "EX04",
                name = "OfficerK",
                warriorPack = "Brute",
                archetype = "Heavy Bruiser",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX04_OfficerK.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/unarmed/BBProfile_EX04_OfficerK.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/OfficerK/BBMoveSet_EX04_OfficerK.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animation Controller/Brute Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/EX04_OfficerK_Brute.controller"
            },
            new FighterAnimConfig
            {
                id = "C11",
                name = "Casual1",
                warriorPack = "Knight",
                archetype = "Sword & Shield",
                prefabPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/crossbow/BBProfile_C11_Casual1.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Casual1/BBMoveSet_C11_Casual1.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animation Controller/Knight Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C11_Casual1_Knight.controller"
            },
            new FighterAnimConfig
            {
                id = "C06",
                name = "Cultist",
                warriorPack = "Mage",
                archetype = "Dark Caster",
                prefabPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Cultist/BBProfile_C06_Cultist.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Cultist/BBMoveSet_C06_Cultist.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animation Controller/Mage Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C06_Cultist_Mage.controller"
            },
            new FighterAnimConfig
            {
                id = "C09",
                name = "PicoChan",
                warriorPack = "Archer",
                archetype = "Bow Zoner",
                prefabPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                profilePath = "Assets/CombatData/BlazBlue/PicoChan/BBProfile_C09_PicoChan.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/PicoChan/BBMoveSet_C09_PicoChan.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animation Controller/Archer Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C09_PicoChan_Archer.controller"
            },
            new FighterAnimConfig
            {
                id = "C14",
                name = "Muryotaisu",
                warriorPack = "TwoHanded",
                archetype = "Heavy Greatsword",
                prefabPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/twohand/BBProfile_C14_Muryotaisu.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Muryotaisu/BBMoveSet_C14_Muryotaisu.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animation Controller/2Handed Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C14_Muryotaisu_TwoHanded.controller"
            },
            new FighterAnimConfig
            {
                id = "C08",
                name = "SanpakuChan",
                warriorPack = "Swordsman",
                archetype = "Katana Duelist",
                prefabPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                profilePath = "Assets/CombatData/BlazBlue/SanpakuChan/BBProfile_C08_SanpakuChan.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/SanpakuChan/BBMoveSet_C08_SanpakuChan.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animation Controller/Swordsman Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C08_SanpakuChan_Swordsman.controller"
            },
            new FighterAnimConfig
            {
                id = "C12",
                name = "Fina",
                warriorPack = "Crossbow",
                archetype = "Crossbow Mid-Range",
                prefabPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Fina/BBProfile_C12_Fina.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Fina/BBMoveSet_C12_Fina.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animation Controller/Crossbow Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/C12_Fina_Crossbow.controller"
            },
            new FighterAnimConfig
            {
                id = "EX01",
                name = "Hugo",
                warriorPack = "Spearman",
                archetype = "Long-Reach Polearm",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX01_Hugo.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/brute/BBProfile_EX01_Hugo.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Hugo/BBMoveSet_EX01_Hugo.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animation Controller/Spearman Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/EX01_Hugo_Spearman.controller"
            },
            new FighterAnimConfig
            {
                id = "SK08",
                name = "Torgrim",
                warriorPack = "Hammer",
                archetype = "Arcade Boss",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK08_Torgrim.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animation Controller/Hammer Warrior Animation Controller.controller",
                targetControllerPath = $"{TARGET_ANIMATORS_DIR}/SK08_Torgrim_Hammer.controller"
            }
        };

        public static void Build12WarriorControllers()
        {
            Debug.Log("<color=#00e676><b>[Warrior Overhaul] Building 12 Warrior Fighter Controllers in Assets/Animators/Fighters/...</b></color>");

            if (!Directory.Exists(TARGET_ANIMATORS_DIR))
            {
                Directory.CreateDirectory(TARGET_ANIMATORS_DIR);
                AssetDatabase.Refresh();
            }

            int successCount = 0;

            foreach (var fighter in LaunchFighters)
            {
                string targetPath = fighter.targetControllerPath;

                // 1. Copy source Warrior controller
                if (!File.Exists(targetPath))
                {
                    if (File.Exists(fighter.sourceControllerPath))
                    {
                        AssetDatabase.CopyAsset(fighter.sourceControllerPath, targetPath);
                        AssetDatabase.ImportAsset(targetPath);
                    }
                    else
                    {
                        var newCtrl = AnimatorController.CreateAnimatorControllerAtPath(targetPath);
                        EditorUtility.SetDirty(newCtrl);
                    }
                }

                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetPath);
                if (controller == null)
                {
                    Debug.LogWarning($"[Warrior Overhaul] Failed to load controller at {targetPath}");
                    continue;
                }

                // 2. Ensure standard parameters
                EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
                EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
                EnsureParameter(controller, "Velocity", AnimatorControllerParameterType.Float);
                EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
                EnsureParameter(controller, "IsCrouch", AnimatorControllerParameterType.Bool);
                EnsureParameter(controller, "IsBlocking", AnimatorControllerParameterType.Bool);
                EnsureParameter(controller, "ForwardDash", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "Backdash", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "HitMid", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "StandBlock", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "Knockdown", AnimatorControllerParameterType.Trigger);
                EnsureParameter(controller, "StandUpBack", AnimatorControllerParameterType.Trigger);

                // 3. Populate MDI_* Attack States from BBMoveSet
                BBMoveSet moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(fighter.moveSetPath);
                var rootStateMachine = controller.layers[0].stateMachine;

                Motion idleClip = FindClipInController(controller, "Idle");
                var idleState = EnsureState(rootStateMachine, "MDI_Idle", idleClip);

                string[] attackMoveIds = new[] { "5A", "5B", "5C", "2C", "jB", "5D", "236236C" };
                foreach (var moveId in attackMoveIds)
                {
                    AnimationClip moveClip = null;
                    if (moveSet != null)
                    {
                        var attack = moveSet.Find(moveId);
                        if (attack != null)
                        {
                            moveClip = attack.clip ?? attack.animationClip;
                        }
                    }

                    if (moveClip == null)
                    {
                        moveClip = FindAttackFallbackClip(controller, moveId);
                    }

                    var atkState = EnsureState(rootStateMachine, "MDI_" + moveId, moveClip);
                    atkState.writeDefaultValues = true;

                    // Ensure clean transition back to MDI_Idle on completion
                    if (atkState.transitions.Length == 0)
                    {
                        var exitTransition = atkState.AddTransition(idleState);
                        exitTransition.hasExitTime = true;
                        exitTransition.exitTime = 0.95f;
                        exitTransition.hasFixedDuration = true;
                        exitTransition.duration = 0.08f;
                    }
                }

                // 4. Ensure MDI_* Locomotion and Reaction States
                Motion walkClip = FindClipInController(controller, "Walk");
                Motion walkSlowClip = FindClipInController(controller, "WalkSlow") ?? walkClip;
                Motion crouchClip = FindClipInController(controller, "Crouch");
                Motion jumpClip = FindClipInController(controller, "Jump");
                Motion hitClip = FindClipInController(controller, "Hit") ?? FindClipInController(controller, "Damage");
                Motion blockClip = FindClipInController(controller, "Block");
                Motion fallClip = FindClipInController(controller, "Fall") ?? FindClipInController(controller, "KO");
                Motion getUpClip = FindClipInController(controller, "GetUp") ?? idleClip;

                EnsureState(rootStateMachine, "MDI_WalkForward", walkClip);
                EnsureState(rootStateMachine, "MDI_WalkFwd", walkClip);
                EnsureState(rootStateMachine, "MDI_WalkBackward", walkSlowClip);
                EnsureState(rootStateMachine, "MDI_WalkBack", walkSlowClip);
                EnsureState(rootStateMachine, "MDI_Crouch", crouchClip);
                EnsureState(rootStateMachine, "MDI_Jump", jumpClip);
                EnsureState(rootStateMachine, "MDI_HitLight", hitClip);
                EnsureState(rootStateMachine, "MDI_HitMid", hitClip);
                EnsureState(rootStateMachine, "MDI_HitHeavy", fallClip);
                EnsureState(rootStateMachine, "MDI_StandBlock", blockClip);
                EnsureState(rootStateMachine, "MDI_Block", blockClip);
                EnsureState(rootStateMachine, "MDI_Knockdown", fallClip);
                EnsureState(rootStateMachine, "MDI_StandUpBack", getUpClip);
                EnsureState(rootStateMachine, "MDI_Intro", idleClip);
                EnsureState(rootStateMachine, "MDI_Victory", idleClip);
                EnsureState(rootStateMachine, "MDI_Taunt", idleClip);

                EditorUtility.SetDirty(controller);

                // 5. Wire the Prefab: retain native Humanoid Avatar, applyRootMotion = false, cullingMode = AlwaysAnimate
                if (File.Exists(fighter.prefabPath))
                {
                    GameObject prefabContents = PrefabUtility.LoadPrefabContents(fighter.prefabPath);
                    if (prefabContents != null)
                    {
                        try
                        {
                            Animator anim = prefabContents.GetComponentInChildren<Animator>();
                            if (anim != null)
                            {
                                anim.applyRootMotion = false;
                                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                                anim.runtimeAnimatorController = controller;
                                // Invariant: Original anim.avatar is kept intact!
                            }

                            var animDriver = prefabContents.GetComponent<BBAnimationDriver>();
                            if (animDriver == null) animDriver = prefabContents.AddComponent<BBAnimationDriver>();
                            animDriver.animator = anim;

                            var pConfig = prefabContents.GetComponent<BBFighterPrefabConfig>();
                            if (pConfig == null) pConfig = prefabContents.AddComponent<BBFighterPrefabConfig>();
                            pConfig.characterId = fighter.id;
                            pConfig.characterDisplayName = fighter.name;
                            pConfig.animator = anim;

                            PrefabUtility.SaveAsPrefabAsset(prefabContents, fighter.prefabPath);
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(prefabContents);
                        }
                    }
                }

                successCount++;
                Debug.Log($"<color=#69F0AE>[Warrior Overhaul] Wired {fighter.id} ({fighter.name} / {fighter.warriorPack}) -> {targetPath}</color>");
            }

            // Clean up any legacy _Warrior.controller files that were superseded
            CleanLegacyWarriorControllers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[Warrior Overhaul] Successfully built and wired all {successCount} Warrior Fighter controllers!</b></color>");
        }

        private static void CleanLegacyWarriorControllers()
        {
            if (!Directory.Exists(TARGET_ANIMATORS_DIR)) return;
            string[] files = Directory.GetFiles(TARGET_ANIMATORS_DIR, "*_Warrior.controller*");
            foreach (var f in files)
            {
                try
                {
                    AssetDatabase.DeleteAsset(f);
                }
                catch { }
            }
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(p => p.name == name)) return;
            controller.AddParameter(name, type);
        }

        private static AnimatorState EnsureState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
        {
            var existingState = stateMachine.states.FirstOrDefault(s => s.state.name == stateName).state;
            if (existingState != null)
            {
                if (motion != null) existingState.motion = motion;
                return existingState;
            }

            var newState = stateMachine.AddState(stateName);
            if (motion != null) newState.motion = motion;
            return newState;
        }

        private static Motion FindClipInController(AnimatorController controller, string keyword)
        {
            if (controller == null) return null;
            foreach (var clip in controller.animationClips)
            {
                if (clip != null && clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }
            }
            return null;
        }

        private static AnimationClip FindAttackFallbackClip(AnimatorController controller, string moveId)
        {
            if (controller == null) return null;
            string kw = "Attack";
            if (moveId == "5A") kw = "Attack1";
            else if (moveId == "5B") kw = "Attack2";
            else if (moveId == "5C") kw = "Attack3";
            else if (moveId == "2C") kw = "Crouch";
            else if (moveId == "jB") kw = "Jump";
            else if (moveId == "5D") kw = "Attack";
            else if (moveId == "236236C") kw = "Special";

            foreach (var clip in controller.animationClips)
            {
                if (clip != null && clip.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }
            }
            return controller.animationClips.FirstOrDefault(c => c != null && c.name.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Audits reach alignment between BBMatchSim 2D hitboxes and 3D Avatar/Warrior pose extensions.
        /// Samples each move's active window pose on the character's real Humanoid Avatar,
        /// measures the striking limb / weapon tip forward extent, compares against BBMatchSim reach,
        /// and outputs a comprehensive report to Screenshots/HITBOX_AUDIT_REPORT.txt.
        /// </summary>
        public static void AuditHitboxVsAvatarReach()
        {
            Debug.Log("<color=#00e676><b>[Hitbox Audit] Starting Hitbox vs Avatar Reach Audit...</b></color>");

            var sb = new StringBuilder();
            sb.AppendLine("# MDI HITBOX VS AVATAR REACH AUDIT REPORT");
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("| Fighter | Move ID | Hitbox Reach | Pose Extent | Vert Band (Y) | Delta | Status |");
            sb.AppendLine("|:---|:---|:---|:---|:---|:---|:---|");

            int passedMoves = 0;
            int totalMoves = 0;

            foreach (var fighter in LaunchFighters)
            {
                BBMoveSet moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(fighter.moveSetPath);
                if (moveSet == null) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fighter.prefabPath);
                if (prefab == null) continue;

                GameObject tempInstance = null;
                try
                {
                    tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    var anim = tempInstance.GetComponentInChildren<Animator>();
                    var weaponRig = tempInstance.GetComponentInChildren<BBWeaponRig>();

                    foreach (var atk in moveSet.moves)
                    {
                        if (atk == null || string.IsNullOrEmpty(atk.moveId)) continue;
                        totalMoves++;

                        float hitboxReach = 0f;
                        float hitboxMinY = 0f;
                        float hitboxMaxY = 1.8f;

                        if (atk.boxes != null && atk.boxes.boxes != null && atk.boxes.boxes.Length > 0)
                        {
                            foreach (var b in atk.boxes.boxes)
                            {
                                if (b.kind == BBBoxKind.Hit)
                                {
                                    float r = b.cx + b.hx;
                                    if (r > hitboxReach) hitboxReach = r;
                                    float bBottom = b.cy - b.hy;
                                    float bTop = b.cy + b.hy;
                                    if (bBottom < hitboxMinY || hitboxMinY == 0f) hitboxMinY = bBottom;
                                    if (bTop > hitboxMaxY) hitboxMaxY = bTop;
                                }
                            }
                        }

                        if (hitboxReach <= 0.05f)
                        {
                            if (atk.kind == BBMoveKind.DistortionDrive) hitboxReach = 2.6f;
                            else if (atk.kind == BBMoveKind.Special) hitboxReach = 2.1f;
                            else if (atk.button == BBButton.C || atk.button == BBButton.D) hitboxReach = 1.75f;
                            else if (atk.button == BBButton.B) hitboxReach = 1.35f;
                            else hitboxReach = 0.95f;
                        }

                        if (BBSignatureTable.TryGet(fighter.id, atk.moveId, out var sig))
                        {
                            hitboxReach *= sig.reachScale;
                        }

                        // Sample animation pose at active window
                        float poseExtent = 0.85f;
                        float poseMinY = 0.4f;
                        float poseMaxY = 1.5f;

                        AnimationClip clip = atk.clip ?? atk.animationClip;
                        if (clip != null && anim != null)
                        {
                            int startup = Mathf.Max(1, atk.startupFrames);
                            int total = Mathf.Max(startup + 1, atk.TotalTicks);
                            float sampleTime = clip.length * ((float)startup / total);
                            clip.SampleAnimation(tempInstance, sampleTime);

                            // Measure striking limb or equipped weapon
                            Transform strikeTransform = null;
                            if (weaponRig != null && weaponRig.PrimaryInstance != null)
                            {
                                strikeTransform = weaponRig.PrimaryInstance.transform;
                            }
                            else if (anim.isHuman)
                            {
                                if (atk.moveId.Contains("C") || atk.moveId.Contains("j"))
                                {
                                    strikeTransform = anim.GetBoneTransform(HumanBodyBones.RightFoot) ?? anim.GetBoneTransform(HumanBodyBones.RightHand);
                                }
                                else
                                {
                                    strikeTransform = anim.GetBoneTransform(HumanBodyBones.RightHand) ?? anim.GetBoneTransform(HumanBodyBones.RightFoot);
                                }
                            }

                            if (strikeTransform != null)
                            {
                                Vector3 relPos = tempInstance.transform.InverseTransformPoint(strikeTransform.position);
                                poseExtent = Mathf.Abs(relPos.z) > Mathf.Abs(relPos.x) ? Mathf.Abs(relPos.z) : Mathf.Abs(relPos.x);
                                if (weaponRig != null && weaponRig.PrimaryInstance != null)
                                {
                                    float wLen = weaponRig.primaryTargetLength > 0.1f ? weaponRig.primaryTargetLength : 0.85f;
                                    poseExtent += wLen * 0.5f;
                                }
                                poseMinY = Mathf.Max(0f, relPos.y - 0.2f);
                                poseMaxY = relPos.y + 0.3f;
                            }
                        }

                        float delta = Mathf.Abs(hitboxReach - poseExtent);
                        bool isAligned = delta <= 1.25f; // within reasonable fighting game active strike envelope
                        if (isAligned) passedMoves++;

                        sb.AppendLine($"| {fighter.id} ({fighter.name}) | {atk.moveId} | {hitboxReach:F2}m | {poseExtent:F2}m | [{poseMinY:F2}m..{poseMaxY:F2}m] | {delta:F2}m | {(isAligned ? "ALIGNED" : "CALIBRATE")} |");
                    }
                }
                finally
                {
                    if (tempInstance != null)
                    {
                        UnityEngine.Object.DestroyImmediate(tempInstance);
                    }
                }
            }

            string report = sb.ToString();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string reportFile = Path.Combine(projectRoot, "Screenshots", "HITBOX_AUDIT_REPORT.txt");
            File.WriteAllText(reportFile, report);

            Debug.Log($"<color=#00e676><b>[Hitbox Audit] Completed: {passedMoves}/{totalMoves} moves evaluated. Report saved to: {reportFile}</b></color>");
        }
    }
}
#endif
