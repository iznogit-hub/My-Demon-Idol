using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Phase 2: Locomotion & Weapon Stance Override Updates.
    /// Wires all 12 character archetypes to their authentic warrior animation pack clips:
    /// - Idle (Weapon combat stance)
    /// - Walk (Forward combat movement)
    /// - WalkSlow (Backward tactical retreat)
    /// - Run (Forward sprint / evasive sidestep)
    /// - Jump (Airborne leap)
    /// - Fall (Airborne recovery / descent)
    /// - Attack1 (Signature weapon strike)
    /// </summary>
    public static class BBWarriorStanceOverrideBuilder
    {
        public class WarriorConfig
        {
            public string CharacterId;
            public string CharacterName;
            public string WarriorName;
            public string AnimationsFolder;
            public string OverrideControllerPath;
            public string ProfileAssetPath;
        }

        public static readonly WarriorConfig[] CONFIGS = new[]
        {
            new WarriorConfig
            {
                CharacterId = "C01",
                CharacterName = "Akio",
                WarriorName = "Swordsman",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Akio_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/Akio/BBProfile_C01_Akio.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C02",
                CharacterName = "Aika",
                WarriorName = "Karate",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Aika_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/Aika/BBProfile_C02_Aika.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C04",
                CharacterName = "UnityChan",
                WarriorName = "Archer",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/UnityChan_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/UnityChan/BBProfile_C04_UnityChan.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C05",
                CharacterName = "Akane",
                WarriorName = "Spearman",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Akane_BB_Override.overrideController",
                ProfileAssetPath = null
            },
            new WarriorConfig
            {
                CharacterId = "C06",
                CharacterName = "Cultist",
                WarriorName = "Mage",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Cultist_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/Cultist/BBProfile_C06_Cultist.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C07",
                CharacterName = "StellarWitch",
                WarriorName = "Sorceress",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/StellarWitch_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/StellarWitch/BBProfile_C07_StellarWitch.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C08",
                CharacterName = "SanpakuChan",
                WarriorName = "Ninja",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/SanpakuChan_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/SanpakuChan/BBProfile_C08_SanpakuChan.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C09",
                CharacterName = "PicoChan",
                WarriorName = "Hammer",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/PicoChan_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/PicoChan/BBProfile_C09_PicoChan.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C10",
                CharacterName = "Reika",
                WarriorName = "Knight",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Reika_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/Reika/BBProfile_C10_Reika.asset"
            },
            new WarriorConfig
            {
                CharacterId = "C11",
                CharacterName = "Casual1",
                WarriorName = "Crossbow",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Casual1_BB_Override.overrideController",
                ProfileAssetPath = null
            },
            new WarriorConfig
            {
                CharacterId = "C12",
                CharacterName = "Fina",
                WarriorName = "2Handed",
                AnimationsFolder = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations",
                OverrideControllerPath = "Assets/MDI_Characters/Runtime/BlazBlue/Fina_BB_Override.overrideController",
                ProfileAssetPath = "Assets/CombatData/BlazBlue/Fina/BBProfile_C12_Fina.asset"
            }
        };

        private const string BASE_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";

        [MenuItem("Demon Idol/BlazBlue/Execute Phase 2 (Locomotion and Stance Overrides)")]
        public static void ExecutePhase2()
        {
            Debug.Log("================================================================");
            Debug.Log("[BBWarriorStanceOverrideBuilder] Starting Phase 2 Execution...");
            Debug.Log("================================================================");

            var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BASE_CONTROLLER_PATH);
            if (baseController == null)
            {
                Debug.LogError($"[BBWarriorStanceOverrideBuilder] FAILED: Base controller not found at {BASE_CONTROLLER_PATH}");
                return;
            }

            int successCount = 0;
            int warningCount = 0;

            foreach (var cfg in CONFIGS)
            {
                try
                {
                    bool ok = ProcessCharacter(cfg, baseController);
                    if (ok) successCount++;
                    else warningCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBWarriorStanceOverrideBuilder] Exception processing {cfg.CharacterId} ({cfg.CharacterName}): {ex.Message}\n{ex.StackTrace}");
                    warningCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("================================================================");
            Debug.Log($"[BBWarriorStanceOverrideBuilder] Phase 2 Finished. Success: {successCount}/{CONFIGS.Length}, Warnings: {warningCount}");
            Debug.Log("================================================================");
        }

        public static bool ApplyWarriorStances(AnimatorOverrideController overrideCtrl, string characterKey)
        {
            var cfg = CONFIGS.FirstOrDefault(c =>
                c.CharacterId.Equals(characterKey, StringComparison.OrdinalIgnoreCase) ||
                c.CharacterName.Equals(characterKey, StringComparison.OrdinalIgnoreCase));

            if (cfg == null)
            {
                Debug.LogWarning($"[BBWarriorStanceOverrideBuilder] No configuration found for character key '{characterKey}'");
                return false;
            }

            if (overrideCtrl == null) return false;

            var idleClip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "Idle");
            var walkClip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "Walk");
            var walkSlowClip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "WalkSlow");
            var runClip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "Run");
            var jumpClip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "Jump");
            var fallClip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "Fall");
            var attack1Clip = FindWarriorClip(cfg.AnimationsFolder, cfg.WarriorName, "Attack1");

            var overridesList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideCtrl.GetOverrides(overridesList);

            int modifiedCount = 0;
            for (int i = 0; i < overridesList.Count; i++)
            {
                var original = overridesList[i].Key;
                if (original == null) continue;
                string oname = original.name;
                var currentVal = overridesList[i].Value;

                AnimationClip replacement = null;

                // Locomotion & Weapon Stances
                if (oname == "HumanF@Idle01" && idleClip != null)
                {
                    replacement = idleClip;
                }
                else if (oname == "HumanF@Run01_Forward" && walkClip != null)
                {
                    replacement = walkClip;
                }
                else if (oname == "HumanF@Run01_Backward" && walkSlowClip != null)
                {
                    replacement = walkSlowClip;
                }
                else if (oname == "HumanF@StrafeRun01_Right" && runClip != null)
                {
                    replacement = runClip;
                }
                else if (oname == "HumanF@CastingEnter01" && jumpClip != null)
                {
                    replacement = jumpClip;
                }
                else if (oname == "HumanF@Idle03" && idleClip != null)
                {
                    replacement = idleClip;
                }
                // Base attacks (fallback layer 0)
                else if (oname == "HumanF@Attack1H01_R" && attack1Clip != null)
                {
                    replacement = attack1Clip;
                }
                else if (oname == "HumanF@Attack1H01_L" && attack1Clip != null)
                {
                    replacement = attack1Clip;
                }
                else if (oname == "HumanF@Attack2H01" && attack1Clip != null)
                {
                    replacement = attack1Clip;
                }
                else if (oname == "HumanF@AttackPolearm01" && attack1Clip != null)
                {
                    replacement = attack1Clip;
                }

                if (replacement != null && replacement != currentVal)
                {
                    overridesList[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
                    modifiedCount++;
                }
            }

            overrideCtrl.ApplyOverrides(overridesList);
            EditorUtility.SetDirty(overrideCtrl);
            return true;
        }

        private static bool ProcessCharacter(WarriorConfig cfg, RuntimeAnimatorController baseController)
        {
            Debug.Log($"\n>>> Processing {cfg.CharacterId} ({cfg.CharacterName}) -> Warrior Archetype: {cfg.WarriorName}");

            // 1. Ensure override controller exists
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(cfg.OverrideControllerPath);
            if (overrideCtrl == null)
            {
                string dir = Path.GetDirectoryName(cfg.OverrideControllerPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                overrideCtrl = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(overrideCtrl, cfg.OverrideControllerPath);
                Debug.Log($"[BBWarriorStanceOverrideBuilder] Created new AnimatorOverrideController at {cfg.OverrideControllerPath}");
            }
            else if (overrideCtrl.runtimeAnimatorController != baseController)
            {
                overrideCtrl.runtimeAnimatorController = baseController;
            }

            // 2. Apply warrior stances
            bool ok = ApplyWarriorStances(overrideCtrl, cfg.CharacterId);

            // 3. Link with BBCharacterProfile if path provided and asset exists
            if (!string.IsNullOrEmpty(cfg.ProfileAssetPath))
            {
                var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(cfg.ProfileAssetPath);
                if (profile != null)
                {
                    profile.blazBlueOverrideController = overrideCtrl;
                    EditorUtility.SetDirty(profile);
                    Debug.Log($"  Linked override controller to BBCharacterProfile: {Path.GetFileName(cfg.ProfileAssetPath)}");
                }
            }

            return ok;
        }

        private static AnimationClip FindWarriorClip(string folder, string warriorName, string clipType)
        {
            string[] candidateFilenames;
            if (clipType == "Idle")
            {
                candidateFilenames = new[] { $"{warriorName}@Idle.FBX", $"{warriorName}@Idle1.FBX" };
            }
            else if (clipType == "WalkSlow")
            {
                candidateFilenames = new[] { $"{warriorName}@WalkSlow.FBX", $"{warriorName}@Walk.FBX" };
            }
            else
            {
                candidateFilenames = new[] { $"{warriorName}@{clipType}.FBX" };
            }

            foreach (var filename in candidateFilenames)
            {
                string fullPath = $"{folder}/{filename}";
                var assets = AssetDatabase.LoadAllAssetsAtPath(fullPath);
                if (assets != null && assets.Length > 0)
                {
                    foreach (var asset in assets)
                    {
                        if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        {
                            return clip;
                        }
                    }
                }
            }

            Debug.LogWarning($"[BBWarriorStanceOverrideBuilder] Could not find clip of type '{clipType}' in {folder}");
            return null;
        }
    }
}
