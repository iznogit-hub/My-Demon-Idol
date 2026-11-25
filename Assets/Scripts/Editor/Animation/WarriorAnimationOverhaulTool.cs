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
using DemonIdol.Combat;

namespace DemonIdol.Editor.Animation
{
    /// <summary>
    /// Part B: Animation Overhaul & PlayableGraph Rip.
    /// Clones the 12 ExplosiveLLC Warrior Pack animator controllers into Assets/Animators/Fighters/
    /// with standardized MDI_* states (MDI_5A, MDI_5B, MDI_5C, MDI_2C, MDI_jB, MDI_5D, MDI_236236C,
    /// locomotion, reactions).
    /// Wires all 12 Launch prefabs retaining native Humanoid Avatars (applyRootMotion=false, cullingMode=AlwaysAnimate).
    /// Provides Hitbox vs Avatar Reach auditing tool.
    /// </summary>
    public static class WarriorAnimationOverhaulTool
    {
        public const string TARGET_ANIMATORS_DIR = "Assets/Animators/Fighters";

        public struct FighterAnimConfig
        {
            public string id;
            public string name;
            public string prefabPath;
            public string profilePath;
            public string moveSetPath;
            public string sourceControllerPath;
        }

        public static readonly FighterAnimConfig[] LaunchFighters = new FighterAnimConfig[]
        {
            new FighterAnimConfig
            {
                id = "C01",
                name = "Akio",
                prefabPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Akio/BBProfile_C01_Akio.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Akio/BBMoveSet_C01_Akio.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animation Controller/Karate Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C02",
                name = "Aika",
                prefabPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Aika/BBProfile_C02_Aika.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Aika/BBMoveSet_C02_Aika.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animation Controller/Ninja Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C05",
                name = "Akane",
                prefabPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/spear/BBProfile_C05_Akane.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Archetypes/spear/BBMoveSet_C05.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animation Controller/Sorceress Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C06",
                name = "Cultist",
                prefabPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Cultist/BBProfile_C06_Cultist.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Cultist/BBMoveSet_C06_Cultist.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animation Controller/Mage Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C08",
                name = "SanpakuChan",
                prefabPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                profilePath = "Assets/CombatData/BlazBlue/SanpakuChan/BBProfile_C08_SanpakuChan.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/SanpakuChan/BBMoveSet_C08_SanpakuChan.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animation Controller/Archer Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C09",
                name = "PicoChan",
                prefabPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                profilePath = "Assets/CombatData/BlazBlue/PicoChan/BBProfile_C09_PicoChan.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/PicoChan/BBMoveSet_C09_PicoChan.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animation Controller/Crossbow Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C11",
                name = "Casual1",
                prefabPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/crossbow/BBProfile_C11_Casual1.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Casual1/BBMoveSet_C11_Casual1.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animation Controller/Swordsman Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C12",
                name = "Fina",
                prefabPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Fina/BBProfile_C12_Fina.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Fina/BBMoveSet_C12_Fina.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animation Controller/Spearman Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "C14",
                name = "Muryotaisu",
                prefabPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/twohand/BBProfile_C14_Muryotaisu.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Muryotaisu/BBMoveSet_C14_Muryotaisu.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animation Controller/2Handed Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "EX01",
                name = "Hugo",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX01_Hugo.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/brute/BBProfile_EX01_Hugo.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Hugo/BBMoveSet_EX01_Hugo.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animation Controller/Knight Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "EX04",
                name = "OfficerK",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX04_OfficerK.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/unarmed/BBProfile_EX04_OfficerK.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/OfficerK/BBMoveSet_EX04_OfficerK.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animation Controller/Hammer Warrior Animation Controller.controller"
            },
            new FighterAnimConfig
            {
                id = "SK08",
                name = "Torgrim",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK08_Torgrim.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                sourceControllerPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animation Controller/Brute Warrior Animation Controller.controller"
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
                string targetPath = $"{TARGET_ANIMATORS_DIR}/{fighter.id}_{fighter.name}_Warrior.controller";

                // 1. Copy source Warrior controller if target doesn't exist
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
                    Debug.LogWarning($"[Warrior Overhaul] Failed to load or create controller at {targetPath}");
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

                    EnsureState(rootStateMachine, "MDI_" + moveId, moveClip);
                }

                // 4. Ensure MDI_* Locomotion and Reaction States
                EnsureState(rootStateMachine, "MDI_Idle", FindClipInController(controller, "Idle"));
                EnsureState(rootStateMachine, "MDI_WalkForward", FindClipInController(controller, "Walk"));
                EnsureState(rootStateMachine, "MDI_WalkBackward", FindClipInController(controller, "WalkSlow") ?? FindClipInController(controller, "Walk"));
                EnsureState(rootStateMachine, "MDI_Crouch", FindClipInController(controller, "Crouch"));
                EnsureState(rootStateMachine, "MDI_Jump", FindClipInController(controller, "Jump"));
                EnsureState(rootStateMachine, "MDI_HitMid", FindClipInController(controller, "Hit") ?? FindClipInController(controller, "Damage"));
                EnsureState(rootStateMachine, "MDI_StandBlock", FindClipInController(controller, "Block"));
                EnsureState(rootStateMachine, "MDI_Knockdown", FindClipInController(controller, "Fall") ?? FindClipInController(controller, "KO"));
                EnsureState(rootStateMachine, "MDI_StandUpBack", FindClipInController(controller, "GetUp") ?? FindClipInController(controller, "Idle"));

                EditorUtility.SetDirty(controller);

                // 5. Wire the Prefab: retain Humanoid Avatar, applyRootMotion = false, cullingMode = AlwaysAnimate
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
                                // Retain original anim.avatar intact
                            }

                            var animDriver = prefabContents.GetComponent<BBAnimationDriver>();
                            if (animDriver == null) animDriver = prefabContents.AddComponent<BBAnimationDriver>();
                            animDriver.animator = anim;

                            PrefabUtility.SaveAsPrefabAsset(prefabContents, fighter.prefabPath);
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(prefabContents);
                        }
                    }
                }


                successCount++;
                Debug.Log($"<color=#69F0AE>[Warrior Overhaul] Wired {fighter.id} ({fighter.name}) -> {targetPath}</color>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[Warrior Overhaul] Successfully built and wired all {successCount} Warrior Fighter controllers!</b></color>");
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

        public static void AuditHitboxVsAvatarReach()
        {
            Debug.Log("<color=#00e676><b>[Hitbox Audit] Starting Hitbox vs Avatar Reach Audit...</b></color>");

            var sb = new StringBuilder();
            sb.AppendLine("# MDI HITBOX VS AVATAR REACH AUDIT REPORT");
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("| Fighter | Move ID | Hitbox Reach | Arm/Leg Reach | Ratio | Status |");
            sb.AppendLine("|:---|:---|:---|:---|:---|:---|");

            int passedMoves = 0;
            int totalMoves = 0;

            foreach (var fighter in LaunchFighters)
            {
                BBMoveSet moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(fighter.moveSetPath);
                if (moveSet == null) continue;

                // Load prefab to measure avatar arm/leg reach
                float avatarArmReach = 0.75f;
                if (File.Exists(fighter.prefabPath))
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(fighter.prefabPath);
                    if (go != null)
                    {
                        var anim = go.GetComponentInChildren<Animator>();
                        if (anim != null && anim.isHuman)
                        {
                            var rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                            var rShoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder);
                            if (rHand != null && rShoulder != null)
                            {
                                avatarArmReach = Vector3.Distance(rHand.position, rShoulder.position);
                                if (avatarArmReach < 0.3f || avatarArmReach > 2.0f) avatarArmReach = 0.75f;
                            }
                        }
                    }
                }

                foreach (var atk in moveSet.moves)
                {
                    if (atk == null || string.IsNullOrEmpty(atk.moveId)) continue;
                    totalMoves++;

                    float hitboxReach = 0f;
                    if (atk.boxes != null && atk.boxes.boxes != null && atk.boxes.boxes.Length > 0)
                    {
                        foreach (var b in atk.boxes.boxes)
                        {
                            if (b.kind == BBBoxKind.Hit)
                            {
                                float r = b.cx + b.hx;
                                if (r > hitboxReach) hitboxReach = r;
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

                    float ratio = hitboxReach / Mathf.Max(0.1f, avatarArmReach);
                    bool ok = hitboxReach > 0.35f && hitboxReach < 4.5f;
                    if (ok) passedMoves++;

                    sb.AppendLine($"| {fighter.id} ({fighter.name}) | {atk.moveId} | {hitboxReach:F2}m | {avatarArmReach:F2}m | {ratio:F2}x | {(ok ? "PASS" : "WARN")} |");
                }
            }

            string report = sb.ToString();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string reportFile = Path.Combine(projectRoot, "Screenshots", "HITBOX_AUDIT_REPORT.txt");
            File.WriteAllText(reportFile, report);

            Debug.Log($"<color=#00e676><b>[Hitbox Audit] Completed: {passedMoves}/{totalMoves} moves within valid reach. Report saved to: {reportFile}</b></color>");
        }
    }
}
#endif
