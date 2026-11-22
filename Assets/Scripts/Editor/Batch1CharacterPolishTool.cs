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
    public static class Batch1CharacterPolishTool
    {
        private const string BASE_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";

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
            public string subclipName; // null or empty to pick main take
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

        // Authoritative MenuItem is in DemonIdolMenu.cs

        public static void ApplyBatch1Polish()
        {
            Debug.Log("<color=#00e676>[Batch1Polish] Starting character polish for C01, C02, C04, C05, C06...</color>");

            PolishC01_Akio();
            PolishC02_Aika();
            PolishC04_UnityChan();
            PolishC05_Akane();
            PolishC06_Cultist();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676>[Batch1Polish] Successfully polished all 5 fighters in Batch 1!</color>");
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName = null)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[Batch1Polish] No clips found in {fbxPath}");
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

            // Map based on original clips from MDI_KI_Melee_Fighter
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
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(movesetPath);
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);

            if (moveSet == null || profile == null)
            {
                Debug.LogError($"[Batch1Polish] Missing profile ({profilePath}) or moveset ({movesetPath})");
                return;
            }

            if (!Directory.Exists(movesDir))
            {
                Directory.CreateDirectory(movesDir);
            }

            var moveList = new List<BBAttackData>(moveSet.moves ?? new BBAttackData[0]);

            foreach (var spec in specs)
            {
                // Find or create BBAttackData
                var existing = moveList.FirstOrDefault(m => m != null && m.moveId == spec.moveId);
                string assetPath = existing != null ? AssetDatabase.GetAssetPath(existing)
                                                    : Path.Combine(movesDir, $"BB_{profile.characterId}_{spec.moveId}.asset");

                BBAttackData atk = existing;
                if (atk == null)
                {
                    atk = AssetDatabase.LoadAssetAtPath<BBAttackData>(assetPath);
                }
                if (atk == null)
                {
                    atk = ScriptableObject.CreateInstance<BBAttackData>();
                    atk.name = $"BB_{profile.characterId}_{spec.moveId}";
                    AssetDatabase.CreateAsset(atk, assetPath);
                }

                // Apply parameters
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

                // Clip
                AnimationClip clip = LoadClip(spec.fbxPath, spec.subclipName);
                if (clip != null)
                {
                    atk.clip = clip;
                    atk.clipName = clip.name;
                    atk.clipFirstFrame = 0;
                    atk.clipLastFrame = Mathf.RoundToInt(clip.length * 24f);
                }

                // Box Track
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

            // Locomotion
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

                // Body Hurtbox
                float hurtY = spec.airOk ? 0.70f : (spec.moveId.StartsWith("2") ? 0.55f : 0.85f);
                float hurtH = spec.airOk ? 0.65f : (spec.moveId.StartsWith("2") ? 0.50f : 0.80f);
                boxList.Add(new BBBox(0f, hurtY, 0.35f, hurtH, BBBoxKind.Hurt, 0, BBAttribute.Body));
                currentOffset++;

                // Hitbox during active phase
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
        // 1. C01 AKIO
        // =========================================================================
        private static void PolishC01_Akio()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Standing Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 14, 300, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.65f, 1.25f), new Vector2(0.35f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Standing Straight", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 20, 600, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null,
                    new Vector2(0.85f, 1.20f), new Vector2(0.45f, 0.25f)),
                // 5C
                new MoveSpec("5C", "Standing Hook", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 26, 900, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", null,
                    new Vector2(1.05f, 1.15f), new Vector2(0.55f, 0.30f)),
                // 2A
                new MoveSpec("2A", "Crouching Tap", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null,
                    new Vector2(0.60f, 0.45f), new Vector2(0.35f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Crouching Low Strike", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", null,
                    new Vector2(0.80f, 0.40f), new Vector2(0.45f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Crouching Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.95f, 0.25f), new Vector2(0.60f, 0.25f), BBAttribute.Foot),
                // j.A (UNIQUE: Male@Punch from Fighter Pack)
                new MoveSpec("j.A", "Jumping Light Tap", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null,
                    new Vector2(0.60f, 0.20f), new Vector2(0.35f, 0.30f)),
                // j.B (UNIQUE: Karate_Jab from Warrior Bundle 1 Karate)
                new MoveSpec("j.B", "Jumping Straight Punch", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab",
                    new Vector2(0.80f, 0.10f), new Vector2(0.45f, 0.30f)),
                // j.C (UNIQUE: Karate_Palm from Warrior Bundle 1 Karate)
                new MoveSpec("j.C", "Jumping Palm Slam", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm",
                    new Vector2(0.90f, -0.10f), new Vector2(0.55f, 0.35f)),
                // 5D
                new MoveSpec("5D", "Iron Rush Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null,
                    new Vector2(1.10f, 1.10f), new Vector2(0.55f, 0.35f)),
                // BC
                new MoveSpec("BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null,
                    new Vector2(0.60f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A
                new MoveSpec("236A", "Crimson Straight", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    8, 3, 16, 950, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null,
                    new Vector2(1.15f, 1.05f), new Vector2(0.60f, 0.35f)),
                // 214B
                new MoveSpec("214B", "Shadow Counter Kick", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1100, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(1.20f, 1.15f), new Vector2(0.65f, 0.40f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Void Rush Cataclysm", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Attack1",
                    new Vector2(1.40f, 1.10f), new Vector2(0.90f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Martial Art Animations Sample/Animations/Fight_Idle.fbx",
                walkFwdFbx = "Assets/Martial Art Animations Sample/Animations/Walk_F.fbx",
                walkBackFbx = "Assets/Martial Art Animations Sample/Animations/Walk_B.fbx",
                crouchFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                backdashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_B.fbx",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                hitFbx = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                knockdownFbx = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                wakeupFbx = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                winFbx = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/Akio/BBProfile_C01_Akio.asset",
                "Assets/CombatData/BlazBlue/Akio/BBMoveSet_C01_Akio.asset",
                "Assets/CombatData/BlazBlue/Akio",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/Akio_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 2. C02 AIKA
        // =========================================================================
        private static void PolishC02_Aika()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Snap Kick", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 14, 300, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null,
                    new Vector2(0.65f, 1.15f), new Vector2(0.35f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Thrust Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 20, 600, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", null,
                    new Vector2(0.85f, 1.10f), new Vector2(0.45f, 0.25f)),
                // 5C
                new MoveSpec("5C", "Axe Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 26, 900, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null,
                    new Vector2(1.05f, 1.25f), new Vector2(0.55f, 0.35f)),
                // 2A
                new MoveSpec("2A", "Crouching Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.60f, 0.45f), new Vector2(0.35f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Crouching Mid", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null,
                    new Vector2(0.80f, 0.40f), new Vector2(0.45f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Sweep Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.95f, 0.25f), new Vector2(0.60f, 0.25f), BBAttribute.Foot),
                // j.A (UNIQUE: Karate_Roundhouse from Warrior Bundle 1 Karate)
                new MoveSpec("j.A", "Air Snap Kick", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(0.60f, 0.20f), new Vector2(0.35f, 0.30f)),
                // j.B (UNIQUE: Dodge_F flying kick from Martial Art Animations)
                new MoveSpec("j.B", "Air Flying Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Martial Art Animations Sample/Animations/Dodge_F.fbx", null,
                    new Vector2(0.85f, 0.10f), new Vector2(0.50f, 0.30f)),
                // j.C (UNIQUE: Dodge_B somersault heel drop)
                new MoveSpec("j.C", "Air Somersault Heel", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/Martial Art Animations Sample/Animations/Dodge_B.fbx", null,
                    new Vector2(0.90f, -0.10f), new Vector2(0.55f, 0.35f)),
                // 5D
                new MoveSpec("5D", "Acrobatic Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", null,
                    new Vector2(1.10f, 1.10f), new Vector2(0.55f, 0.35f)),
                // BC
                new MoveSpec("BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null,
                    new Vector2(0.60f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A
                new MoveSpec("236A", "Lunging Crescent Kick", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    8, 3, 16, 950, false,
                    "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx", null,
                    new Vector2(1.15f, 1.05f), new Vector2(0.60f, 0.35f)),
                // 214B
                new MoveSpec("214B", "Spinning Roundhouse", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1100, false,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_B.fbx", null,
                    new Vector2(1.20f, 1.15f), new Vector2(0.65f, 0.40f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Spiral Tempest Hurricane", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Attack1",
                    new Vector2(1.40f, 1.10f), new Vector2(0.90f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Martial Art Animations Sample/Animations/Fight_Idle.fbx",
                walkFwdFbx = "Assets/Martial Art Animations Sample/Animations/Walk_F.fbx",
                walkBackFbx = "Assets/Martial Art Animations Sample/Animations/Walk_B.fbx",
                crouchFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                backdashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_B.fbx",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                hitFbx = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                knockdownFbx = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                wakeupFbx = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                winFbx = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/Aika/BBProfile_C02_Aika.asset",
                "Assets/CombatData/BlazBlue/Aika/BBMoveSet_C02_Aika.asset",
                "Assets/CombatData/BlazBlue/Aika",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/Aika_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 3. C04 UNITYCHAN
        // =========================================================================
        private static void PolishC04_UnityChan()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Fast Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 300, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null,
                    new Vector2(0.65f, 1.25f), new Vector2(0.35f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Double Palm Strike", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 600, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null,
                    new Vector2(0.85f, 1.20f), new Vector2(0.45f, 0.25f)),
                // 5C
                new MoveSpec("5C", "Heavy Uppercut", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 25, 900, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", null,
                    new Vector2(1.05f, 1.15f), new Vector2(0.55f, 0.30f)),
                // 2A
                new MoveSpec("2A", "Crouching Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.60f, 0.45f), new Vector2(0.35f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Crouching Low Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null,
                    new Vector2(0.80f, 0.40f), new Vector2(0.45f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Crouching Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", null,
                    new Vector2(0.95f, 0.25f), new Vector2(0.60f, 0.25f), BBAttribute.Foot),
                // j.A (UNIQUE: Male@Punch from Fighter Pack)
                new MoveSpec("j.A", "Air Light Tap", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null,
                    new Vector2(0.60f, 0.20f), new Vector2(0.35f, 0.30f)),
                // j.B (UNIQUE: Karate_Palm from Warrior Bundle 1 Karate)
                new MoveSpec("j.B", "Air Palm Strike", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm",
                    new Vector2(0.80f, 0.10f), new Vector2(0.45f, 0.30f)),
                // j.C (UNIQUE: Karate_Roundhouse from Warrior Bundle 1 Karate)
                new MoveSpec("j.C", "Air Dive Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse",
                    new Vector2(0.90f, -0.10f), new Vector2(0.55f, 0.35f)),
                // 5D
                new MoveSpec("5D", "Cheer Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null,
                    new Vector2(1.10f, 1.10f), new Vector2(0.55f, 0.35f)),
                // BC
                new MoveSpec("BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null,
                    new Vector2(0.60f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A
                new MoveSpec("236A", "Heavy Charging Fist", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    10, 3, 20, 950, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null,
                    new Vector2(1.15f, 1.05f), new Vector2(0.60f, 0.35f)),
                // 214B
                new MoveSpec("214B", "Berserker Uppercut", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1100, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null,
                    new Vector2(1.20f, 1.15f), new Vector2(0.65f, 0.40f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Ultra Burst Rush", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Attack1",
                    new Vector2(1.40f, 1.10f), new Vector2(0.90f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkForward.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkBackward.FBX",
                crouchFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                backdashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_B.fbx",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                hitFbx = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                knockdownFbx = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                wakeupFbx = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                winFbx = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/UnityChan/BBProfile_C04_UnityChan.asset",
                "Assets/CombatData/BlazBlue/UnityChan/BBMoveSet_C04_UnityChan.asset",
                "Assets/CombatData/BlazBlue/UnityChan",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/UnityChan_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 4. C05 AKANE
        // =========================================================================
        private static void PolishC05_Akane()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Spear Thrust", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 16, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1",
                    new Vector2(1.20f, 1.25f), new Vector2(0.60f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Spear Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 22, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep",
                    new Vector2(1.40f, 1.20f), new Vector2(0.70f, 0.25f)),
                // 5C
                new MoveSpec("5C", "Spear Lance", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    15, 3, 28, 900, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance",
                    new Vector2(1.60f, 1.15f), new Vector2(0.80f, 0.30f)),
                // 2A
                new MoveSpec("2A", "Low Spear Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 16, 280, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", null,
                    new Vector2(0.90f, 0.45f), new Vector2(0.45f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Low Spear Strike", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 22, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx", null,
                    new Vector2(1.10f, 0.40f), new Vector2(0.55f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Low Ground Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 28, 750, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx", null,
                    new Vector2(1.30f, 0.25f), new Vector2(0.65f, 0.25f), BBAttribute.Foot),
                // j.A (UNIQUE: HumanM@Attack1H01_R)
                new MoveSpec("j.A", "Air Spear Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx", null,
                    new Vector2(0.80f, 0.20f), new Vector2(0.45f, 0.30f)),
                // j.B (UNIQUE: HumanM@Attack1H01_L)
                new MoveSpec("j.B", "Air Horizontal Slash", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx", null,
                    new Vector2(1.00f, 0.10f), new Vector2(0.55f, 0.30f)),
                // j.C (UNIQUE: HumanM@Attack2H01)
                new MoveSpec("j.C", "Air Downward Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx", null,
                    new Vector2(1.10f, -0.10f), new Vector2(0.60f, 0.35f)),
                // 5D
                new MoveSpec("5D", "Polearm Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@AttackPolearm01.fbx", null,
                    new Vector2(1.40f, 1.10f), new Vector2(0.70f, 0.35f)),
                // BC
                new MoveSpec("BC", "Shaft Knock Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx", null,
                    new Vector2(0.70f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A
                new MoveSpec("236A", "Lunging Lancer", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    9, 3, 18, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Attack1",
                    new Vector2(1.40f, 1.05f), new Vector2(0.75f, 0.35f)),
                // 214A (UNIQUE: TwoHanded_Sweep)
                new MoveSpec("214A", "Spinning Spear Whirl", BBButton.A, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1050, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep",
                    new Vector2(1.35f, 1.15f), new Vector2(0.70f, 0.40f)),
                // 236236C (Super: TwoHanded_Slam)
                new MoveSpec("236236C", "Celestial Dragon Piercer", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam",
                    new Vector2(1.60f, 1.10f), new Vector2(0.95f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@WalkSlow.FBX",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@CombatIdlePolearm01.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Run.FBX",
                backdashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@WalkSlow.FBX",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                hitFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                wakeupFbx = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/Archetypes/spear/BBProfile_C05_Akane.asset",
                "Assets/CombatData/BlazBlue/Archetypes/spear/BBMoveSet_C05.asset",
                "Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/Akane_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 5. C06 CULTIST
        // =========================================================================
        private static void PolishC06_Cultist()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Right Hand Void Dart", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 16, 300, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null,
                    new Vector2(0.85f, 1.25f), new Vector2(0.45f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Left Hand Hex", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 22, 600, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null,
                    new Vector2(1.05f, 1.20f), new Vector2(0.55f, 0.25f)),
                // 5C
                new MoveSpec("5C", "Void Eruption", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    15, 3, 28, 900, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null,
                    new Vector2(1.25f, 1.15f), new Vector2(0.65f, 0.30f)),
                // 2A
                new MoveSpec("2A", "Low Shadow Spike", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 16, 280, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Cast.fbx", null,
                    new Vector2(0.70f, 0.45f), new Vector2(0.40f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Low Hex Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 22, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Cast.fbx", null,
                    new Vector2(0.90f, 0.40f), new Vector2(0.50f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Ground Glyph Eruption", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 28, 750, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Cast.fbx", null,
                    new Vector2(1.10f, 0.25f), new Vector2(0.65f, 0.25f), BBAttribute.Foot),
                // j.A (UNIQUE: HumanM@MagicAttackDirect1H01_R - Load)
                new MoveSpec("j.A", "Air Dark Needle", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Load.fbx", null,
                    new Vector2(0.70f, 0.20f), new Vector2(0.40f, 0.30f)),
                // j.B (UNIQUE: HumanM@MagicAttackDirect1H01_L - Load)
                new MoveSpec("j.B", "Air Spectral Arc", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Load.fbx", null,
                    new Vector2(0.90f, 0.10f), new Vector2(0.50f, 0.30f)),
                // j.C (UNIQUE: HumanM@MagicAttackDirect2H01 - Load)
                new MoveSpec("j.C", "Air Void Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Load.fbx", null,
                    new Vector2(1.00f, -0.10f), new Vector2(0.55f, 0.35f)),
                // 5D
                new MoveSpec("5D", "Hex Field Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null,
                    new Vector2(1.20f, 1.10f), new Vector2(0.65f, 0.35f)),
                // BC
                new MoveSpec("BC", "Soul Drain Grasp", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L.fbx", null,
                    new Vector2(0.65f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A (UNIQUE: Sorceress_Burst from Warrior Bundle 1 Sorceress)
                new MoveSpec("236A", "Dark Energy Burst", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    9, 3, 18, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst",
                    new Vector2(1.25f, 1.05f), new Vector2(0.65f, 0.35f)),
                // 214A (UNIQUE: Sorceress_Wave from Warrior Bundle 1 Sorceress)
                new MoveSpec("214A", "Abyssal Shockwave", BBButton.A, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1050, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave",
                    new Vector2(1.30f, 1.15f), new Vector2(0.70f, 0.40f)),
                // 236236C (Super: Sorceress_Nova)
                new MoveSpec("236236C", "Cataclysmic Void Nova", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova",
                    new Vector2(1.50f, 1.10f), new Vector2(0.95f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingIdle01.fbx",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@WalkSlow.FBX",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingEnter01.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Run.FBX",
                backdashFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx",
                blockFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingEnter01.fbx",
                hitFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingDamage01.fbx",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Fall.FBX",
                wakeupFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingExit01.fbx",
                winFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingIdle01.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/Cultist/BBProfile_C06_Cultist.asset",
                "Assets/CombatData/BlazBlue/Cultist/BBMoveSet_C06_Cultist.asset",
                "Assets/CombatData/BlazBlue/Cultist",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/Cultist_BB_Override.overrideController"
            );
        }
    }
}
#endif
