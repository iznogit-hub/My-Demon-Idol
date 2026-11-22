#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    public static class Batch5CharacterPolishTool
    {
        public struct MoveSpec
        {
            public string moveId;
            public string attackName;
            public BBButton button;
            public BBMoveKind kind;
            public BBMotion motion;
            public bool airOk;
            public bool groundOk;
            public int startup;
            public int active;
            public int recovery;
            public float damage;
            public bool causesKnockdown;
            public string fbxPath;
            public string subclipName;
            public Vector2 hitBoxOffset;
            public Vector2 hitBoxHalfSize;
            public BBAttribute attribute;

            public MoveSpec(string moveId, string name, BBButton button, BBMoveKind kind, BBMotion motion,
                            bool airOk, bool groundOk, int startup, int active, int recovery, float damage,
                            bool causesKnockdown, string fbxPath, string subclipName,
                            Vector2 hitBoxOffset, Vector2 hitBoxHalfSize, BBAttribute attribute = BBAttribute.Body)
            {
                this.moveId = moveId;
                this.attackName = name;
                this.button = button;
                this.kind = kind;
                this.motion = motion;
                this.airOk = airOk;
                this.groundOk = groundOk;
                this.startup = startup;
                this.active = active;
                this.recovery = recovery;
                this.damage = damage;
                this.causesKnockdown = causesKnockdown;
                this.fbxPath = fbxPath;
                this.subclipName = subclipName;
                this.hitBoxOffset = hitBoxOffset;
                this.hitBoxHalfSize = hitBoxHalfSize;
                this.attribute = attribute;
            }
        }

        public struct LocomotionSpec
        {
            public string idleFbx;
            public string idleSubclip;
            public string walkFwdFbx;
            public string walkFwdSubclip;
            public string walkBackFbx;
            public string walkBackSubclip;
            public string crouchFbx;
            public string crouchSubclip;
            public string jumpFbx;
            public string jumpSubclip;
            public string landFbx;
            public string landSubclip;
            public string dashFbx;
            public string dashSubclip;
            public string backdashFbx;
            public string backdashSubclip;
            public string blockFbx;
            public string blockSubclip;
            public string hitFbx;
            public string hitSubclip;
            public string knockdownFbx;
            public string knockdownSubclip;
            public string wakeupFbx;
            public string wakeupSubclip;
            public string winFbx;
            public string winSubclip;
        }

        public static void ApplyBatch5Polish()
        {
            Debug.Log("<color=#00e676>[Batch5Polish] Starting character polish for EX03, EX04, EX05, EX06, EX07...</color>");

            PolishEX03_Viper();
            PolishEX04_OfficerK();
            PolishEX05_Kira();
            PolishEX06_Sentry();
            PolishEX07_Lyra();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676>[Batch5Polish] Successfully polished all 5 fighters in Batch 5!</color>");
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName = null)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[Batch5Polish] No clips found in {fbxPath}");
                return null;
            }

            if (!string.IsNullOrEmpty(clipName))
            {
                var match = clips.FirstOrDefault(c => string.Equals(c.name, clipName, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            return clips[0];
        }

        private static void ApplyLocomotion(AnimatorOverrideController overrideCtrl, LocomotionSpec loco)
        {
            if (overrideCtrl == null) return;

            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideCtrl.GetOverrides(list);

            var clipMap = new Dictionary<string, AnimationClip>();
            foreach (var pair in list)
            {
                if (pair.Key != null) clipMap[pair.Key.name] = pair.Value;
            }

            void SetOverride(string originalClipName, string fbxPath, string subclip = null)
            {
                var clip = LoadClip(fbxPath, subclip);
                if (clip != null)
                {
                    clipMap[originalClipName] = clip;
                }
            }

            SetOverride("HumanF@CombatIdle01", loco.idleFbx, loco.idleSubclip);
            SetOverride("HumanF@Idle01", loco.crouchFbx, loco.crouchSubclip);
            SetOverride("HumanF@Run01_Forward", loco.walkFwdFbx, loco.walkFwdSubclip);
            SetOverride("HumanF@Run01_Backward", loco.walkBackFbx, loco.walkBackSubclip);
            SetOverride("HumanF@StrafeRun01_Right", loco.dashFbx, loco.dashSubclip);
            SetOverride("HumanF@CastingEnter01", loco.jumpFbx, loco.jumpSubclip);
            SetOverride("HumanF@CombatIdle1H01", loco.blockFbx, loco.blockSubclip);
            SetOverride("HumanF@CombatDamage01", loco.hitFbx, loco.hitSubclip);
            SetOverride("HumanF@CastingDamage01", loco.hitFbx, loco.hitSubclip);
            SetOverride("HumanF@Death01", loco.knockdownFbx, loco.knockdownSubclip);
            SetOverride("HumanF@CastingExit01", loco.wakeupFbx, loco.wakeupSubclip);
            SetOverride("HumanF@Idle03", loco.winFbx, loco.winSubclip);

            var updated = list.Select(pair =>
            {
                if (pair.Key != null && clipMap.TryGetValue(pair.Key.name, out var newClip) && newClip != null)
                {
                    return new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, newClip);
                }
                return pair;
            }).ToList();

            overrideCtrl.ApplyOverrides(updated);
            EditorUtility.SetDirty(overrideCtrl);
        }

        private static void ConfigureMoveset(string profilePath, string movesetPath, string movesDir,
                                             List<MoveSpec> specs, LocomotionSpec loco, string overridePath)
        {
            if (!Directory.Exists(movesDir))
            {
                Directory.CreateDirectory(movesDir);
            }

            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(movesetPath);
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);

            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.name = Path.GetFileNameWithoutExtension(movesetPath);
                AssetDatabase.CreateAsset(moveSet, movesetPath);
            }

            if (profile == null)
            {
                Debug.LogError($"[Batch5Polish] Missing profile ({profilePath})");
                return;
            }

            profile.moveSet = moveSet;
            if (overrideCtrl != null) profile.blazBlueOverrideController = overrideCtrl;

            var moveList = new List<BBAttackData>(moveSet.moves ?? new BBAttackData[0]);

            foreach (var spec in specs)
            {
                var existing = moveList.FirstOrDefault(m => m != null && m.moveId == spec.moveId);
                string assetPath = existing != null ? AssetDatabase.GetAssetPath(existing)
                                                    : Path.Combine(movesDir, $"BB_{profile.characterName}_{spec.moveId}.asset");

                BBAttackData atk = existing;
                if (atk == null)
                {
                    atk = AssetDatabase.LoadAssetAtPath<BBAttackData>(assetPath);
                }
                if (atk == null)
                {
                    atk = ScriptableObject.CreateInstance<BBAttackData>();
                    atk.name = $"BB_{profile.characterName}_{spec.moveId}";
                    AssetDatabase.CreateAsset(atk, assetPath);
                }

                atk.moveId = spec.moveId;
                atk.attackName = spec.attackName;
                atk.button = spec.button;
                atk.kind = spec.kind;
                atk.motion = spec.motion;
                atk.airOk = spec.airOk;
                atk.groundOk = spec.groundOk;
                atk.startupFrames = spec.startup;
                atk.activeFrames = spec.active;
                atk.recoveryFrames = spec.recovery;
                atk.damage = spec.damage;
                atk.causesKnockdown = spec.causesKnockdown;
                atk.hardKnockdown = spec.causesKnockdown;
                atk.hitstunTicks = spec.startup + spec.active + 4;
                atk.blockstunTicks = spec.startup + spec.active + 1;
                atk.hitstopAttacker = spec.kind == BBMoveKind.DistortionDrive ? 12 : (spec.button == BBButton.C ? 7 : 5);
                atk.hitstopDefender = atk.hitstopAttacker;
                atk.pushbackOnHit = spec.button == BBButton.C ? 1.5f : 1.0f;
                atk.pushbackOnBlock = 0.8f;

                if (spec.kind == BBMoveKind.DistortionDrive)
                {
                    atk.heatCost = 50;
                    atk.rapidCancelable = false;
                }
                else
                {
                    atk.heatCost = 0;
                    atk.rapidCancelable = true;
                }

                AnimationClip clip = LoadClip(spec.fbxPath, spec.subclipName);
                if (clip != null)
                {
                    atk.clip = clip;
                    atk.clipName = clip.name;
                    atk.clipFirstFrame = 0;
                    atk.clipLastFrame = Mathf.RoundToInt(clip.length * 24f);
                }

                BakeMoveBoxTrack(atk, spec);
                EditorUtility.SetDirty(atk);

                if (!moveList.Contains(atk))
                {
                    moveList.Add(atk);
                }
            }

            moveSet.moves = moveList.Where(m => m != null).ToArray();
            EditorUtility.SetDirty(moveSet);
            EditorUtility.SetDirty(profile);

            ApplyLocomotion(overrideCtrl, loco);
        }

        private static void BakeMoveBoxTrack(BBAttackData atk, MoveSpec spec)
        {
            int totalTicks = atk.TotalTicks;
            if (totalTicks <= 0) totalTicks = 1;

            if (atk.boxes == null)
            {
                atk.boxes = ScriptableObject.CreateInstance<BBBoxTrack>();
                atk.boxes.name = $"{atk.name}_Boxes";
                AssetDatabase.AddObjectToAsset(atk.boxes, atk);
            }

            var track = atk.boxes;
            track.tickCount = totalTicks;
            track.frameStart = new int[totalTicks + 1];

            var boxList = new List<BBBox>();
            int currentOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentOffset;

                float hurtY = spec.airOk ? 0.70f : (spec.moveId.StartsWith("2") ? 0.55f : 0.85f);
                float hurtH = spec.airOk ? 0.65f : (spec.moveId.StartsWith("2") ? 0.50f : 0.80f);
                boxList.Add(new BBBox(0f, hurtY, 0.35f, hurtH, BBBoxKind.Hurt, 0, BBAttribute.Body));
                currentOffset++;

                if (t >= spec.startup && t < spec.startup + spec.active)
                {
                    boxList.Add(new BBBox(
                        spec.hitBoxOffset.x,
                        spec.hitBoxOffset.y,
                        spec.hitBoxHalfSize.x,
                        spec.hitBoxHalfSize.y,
                        spec.moveId == "BC" ? BBBoxKind.Throw : BBBoxKind.Hit,
                        0,
                        spec.attribute
                    ));
                    currentOffset++;
                }
            }
            track.frameStart[totalTicks] = currentOffset;
            track.boxes = boxList.ToArray();
            EditorUtility.SetDirty(track);
        }

        // =========================================================================
        // 1. EX03 VIPER (Sword / Predatory Mercenary)
        // =========================================================================
        private static void PolishEX03_Viper()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Fang Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1",
                    new Vector2(0.85f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Venom Slice", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1",
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Viper Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 24, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Low Fang", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 11, 250, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2",
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Ankle Pierce", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 17, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Shadow Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    12, 5, 25, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Fang", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Coil", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 5, 16, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1",
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Toxic Bite", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    11, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Mercenary Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Venom Lunge", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    11, 6, 20, 800, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Serpent Feint", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Viper Cataclysm", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    9, 12, 36, 2600, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Idle.FBX",
                crouchFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Jump.FBX",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Fall.FBX",
                blockFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Idle.FBX",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Fall.FBX",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/sword/BBProfile_EX03_Viper.asset",
                             "Assets/CombatData/BlazBlue/Viper/BBMoveSet_EX03_Viper.asset",
                             "Assets/CombatData/BlazBlue/Viper",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX03_Viper_Override.overrideController");
        }

        // =========================================================================
        // 2. EX04 OFFICER K (Striker / Riot Police Officer)
        // =========================================================================
        private static void PolishEX04_OfficerK()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Riot Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab",
                    new Vector2(0.85f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Enforcer Palm", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm",
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Suppression Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 24, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Crouch Check", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 11, 250, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Shin Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 17, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null,
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Takedown Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    12, 5, 25, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Riot Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab",
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Palm Strike", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 5, 16, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm",
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Riot Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Restraint Impact", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    11, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Police Takedown", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Baton Charge", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    11, 6, 20, 800, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Crowd Dispeller", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Maximum Suppression", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    9, 12, 36, 2600, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX",
                crouchFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/unarmed/BBProfile_EX04_OfficerK.asset",
                             "Assets/CombatData/BlazBlue/OfficerK/BBMoveSet_EX04_OfficerK.asset",
                             "Assets/CombatData/BlazBlue/OfficerK",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX04_OfficerK_Override.overrideController");
        }

        // =========================================================================
        // 3. EX05 KIRA (Sword / Iaido Swordsman)
        // =========================================================================
        private static void PolishEX05_Kira()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Iaido Draw", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash",
                    new Vector2(0.85f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Flash Point", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Moon Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 24, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Low Draw", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 11, 250, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1",
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Severing Step", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 17, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Ground Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    12, 5, 25, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Draw", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash",
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Needle", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 5, 16, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Crescent", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Blade Transcendence", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    11, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Hilt Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Lightning Step", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    11, 6, 20, 800, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Severing Shadow", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Judgement Cut", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    9, 12, 36, 2600, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Idle.FBX",
                crouchFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Jump.FBX",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Fall.FBX",
                blockFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Idle.FBX",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Fall.FBX",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/archer/BBProfile_EX05_Kira.asset",
                             "Assets/CombatData/BlazBlue/Kira/BBMoveSet_EX05_Kira.asset",
                             "Assets/CombatData/BlazBlue/Kira",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX05_Kira_Override.overrideController");
        }

        // =========================================================================
        // 4. EX06 SENTRY (Polearm / Mechanical Automaton)
        // =========================================================================
        private static void PolishEX06_Sentry()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Piston Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1",
                    new Vector2(0.85f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Clockwork Pierce", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 18, 600, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx", null,
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Overclock Lance", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 24, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Low Calibration", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 11, 250, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1",
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Perimeter Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 17, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Trip Protocol", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    12, 5, 25, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Piston", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1",
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 5, 16, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx", null,
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Heavy Lance", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Sentinel Protocol", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    11, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Shield Eject", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Piston Charge", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    11, 6, 20, 800, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx", null,
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Defense Barrier", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Annihilation Drill", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    9, 12, 36, 2600, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                crouchFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Jump.FBX",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                blockFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/spear/BBProfile_EX06_Sentry.asset",
                             "Assets/CombatData/BlazBlue/Sentry/BBMoveSet_EX06_Sentry.asset",
                             "Assets/CombatData/BlazBlue/Sentry",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX06_Sentry_Override.overrideController");
        }

        // =========================================================================
        // 5. EX07 LYRA (Caster / Sorceress of Dawn)
        // =========================================================================
        private static void PolishEX07_Lyra()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Dawn Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 12, 300, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null,
                    new Vector2(0.90f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Projectile),
                // 5B
                new MoveSpec("5B", "Radiant Burst", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst",
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Projectile),
                // 5C
                new MoveSpec("5C", "Solar Flare", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 5, 24, 900, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Projectile),
                // 2A
                new MoveSpec("2A", "Prism Ray", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 10, 250, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast.fbx", null,
                    new Vector2(0.85f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Projectile),
                // 2B
                new MoveSpec("2B", "Gleam Wave", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 16, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Cast.fbx", null,
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Projectile),
                // 2C
                new MoveSpec("2C", "Aurora Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 26, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Projectile),
                // j.A
                new MoveSpec("j.A", "Air Dawn Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Load.fbx", null,
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Projectile),
                // j.B
                new MoveSpec("j.B", "Air Solar Orb", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    9, 5, 16, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst",
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Projectile),
                // j.C
                new MoveSpec("j.C", "Air Supernova", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Projectile),
                // 5D
                new MoveSpec("5D", "Sunburst Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    12, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst",
                    new Vector2(1.05f, 1.00f), new Vector2(0.65f, 0.45f), BBAttribute.Projectile),
                // BC
                new MoveSpec("BC", "Celestial Tether", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Dawn Beam", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    12, 6, 22, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave",
                    new Vector2(1.00f, 0.85f), new Vector2(0.60f, 0.45f), BBAttribute.Projectile),
                // 214B
                new MoveSpec("214B", "Solar Shield", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Projectile),
                // 236236C
                new MoveSpec("236236C", "Dawn of Creation", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    10, 12, 36, 2600, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Projectile)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Idle.FBX",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Jump.FBX",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Fall.FBX",
                blockFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@CombatIdle1H01.fbx",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Fall.FBX",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/sorceress/BBProfile_EX07_Lyra.asset",
                             "Assets/CombatData/BlazBlue/Lyra/BBMoveSet_EX07_Lyra.asset",
                             "Assets/CombatData/BlazBlue/Lyra",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX07_Lyra_Override.overrideController");
        }
    }
}
#endif
