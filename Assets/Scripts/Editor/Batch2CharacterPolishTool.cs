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
    public static class Batch2CharacterPolishTool
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

        public static void ApplyBatch2Polish()
        {
            Debug.Log("<color=#00e676>[Batch2Polish] Starting character polish for C07, C08, C09, C10, C11...</color>");

            PolishC07_StellarWitch();
            PolishC08_SanpakuChan();
            PolishC09_PicoChan();
            PolishC10_Reika();
            PolishC11_Casual1();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676>[Batch2Polish] Successfully polished all 5 fighters in Batch 2!</color>");
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName = null)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[Batch2Polish] No clips found in {fbxPath}");
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
                Debug.LogError($"[Batch2Polish] Missing profile ({profilePath})");
                return;
            }

            profile.moveSet = moveSet;
            if (overrideCtrl != null) profile.blazBlueOverrideController = overrideCtrl;

            if (!Directory.Exists(movesDir))
            {
                Directory.CreateDirectory(movesDir);
            }

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
        // 1. C07 STELLARWITCH (Caster)
        // =========================================================================
        private static void PolishC07_StellarWitch()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Arcane Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 16, 300, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null,
                    new Vector2(0.85f, 1.25f), new Vector2(0.45f, 0.30f), BBAttribute.Projectile),
                // 5B
                new MoveSpec("5B", "Astral Pulse", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 22, 600, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null,
                    new Vector2(1.05f, 1.20f), new Vector2(0.55f, 0.35f), BBAttribute.Projectile),
                // 5C
                new MoveSpec("5C", "Stellar Beam", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    15, 3, 28, 900, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null,
                    new Vector2(1.30f, 1.15f), new Vector2(0.70f, 0.40f), BBAttribute.Projectile),
                // 2A
                new MoveSpec("2A", "Low Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 16, 280, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast.fbx", null,
                    new Vector2(0.80f, 0.50f), new Vector2(0.40f, 0.30f), BBAttribute.Projectile),
                // 2B
                new MoveSpec("2B", "Low Pulse", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 22, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Cast.fbx", null,
                    new Vector2(1.00f, 0.45f), new Vector2(0.50f, 0.30f), BBAttribute.Projectile),
                // 2C
                new MoveSpec("2C", "Eldritch Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 28, 750, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Cast.fbx", null,
                    new Vector2(1.20f, 0.30f), new Vector2(0.65f, 0.30f), BBAttribute.Projectile),
                // j.A
                new MoveSpec("j.A", "Air Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Load.fbx", null,
                    new Vector2(0.80f, 0.20f), new Vector2(0.45f, 0.35f), BBAttribute.Projectile),
                // j.B
                new MoveSpec("j.B", "Air Burst", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Load.fbx", null,
                    new Vector2(1.00f, 0.10f), new Vector2(0.55f, 0.35f), BBAttribute.Projectile),
                // j.C
                new MoveSpec("j.C", "Stellar Descent", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Load.fbx", null,
                    new Vector2(1.15f, -0.10f), new Vector2(0.65f, 0.40f), BBAttribute.Projectile),
                // 5D (Drive)
                new MoveSpec("5D", "Star Field", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", null,
                    new Vector2(1.10f, 1.10f), new Vector2(0.60f, 0.50f), BBAttribute.Projectile),
                // BC (Throw)
                new MoveSpec("BC", "Gravity Collapse", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", null,
                    new Vector2(0.65f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A (Special 1)
                new MoveSpec("236A", "Comet Fire", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    9, 3, 18, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst",
                    new Vector2(1.20f, 1.10f), new Vector2(0.65f, 0.35f), BBAttribute.Projectile),
                // 214B (Special 2)
                new MoveSpec("214B", "Astral Rift", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1050, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave",
                    new Vector2(1.30f, 0.90f), new Vector2(0.70f, 0.40f), BBAttribute.Projectile),
                // 236236C (Super)
                new MoveSpec("236236C", "Cosmic Cataclysm", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova",
                    new Vector2(1.50f, 1.10f), new Vector2(0.95f, 0.60f), BBAttribute.Projectile)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx",
                walkFwdFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx",
                walkBackFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx",
                jumpFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                landFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                dashFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Strafe/StrafeRun/HumanF@StrafeRun01_Right.fbx",
                backdashFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                blockFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@CombatIdle1H01.fbx",
                hitFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingDamage01.fbx",
                knockdownFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx",
                wakeupFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                winFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle03.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/StellarWitch/BBProfile_C07_StellarWitch.asset",
                "Assets/CombatData/BlazBlue/StellarWitch/BBMoveSet_C07_StellarWitch.asset",
                "Assets/CombatData/BlazBlue/StellarWitch",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/StellarWitch_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 2. C08 SANPAKUCHAN (Ninja)
        // =========================================================================
        private static void PolishC08_SanpakuChan()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Shadow Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1",
                    new Vector2(0.70f, 1.20f), new Vector2(0.40f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Cross Cut", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2",
                    new Vector2(0.90f, 1.15f), new Vector2(0.50f, 0.30f)),
                // 5C
                new MoveSpec("5C", "Double Rend", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 25, 900, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross",
                    new Vector2(1.10f, 1.10f), new Vector2(0.60f, 0.35f)),
                // 2A
                new MoveSpec("2A", "Low Dagger Tap", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.65f, 0.45f), new Vector2(0.35f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Ankle Cutter", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null,
                    new Vector2(0.80f, 0.40f), new Vector2(0.45f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Shadow Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.95f, 0.25f), new Vector2(0.60f, 0.25f), BBAttribute.Foot),
                // j.A
                new MoveSpec("j.A", "Air Slice", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1",
                    new Vector2(0.65f, 0.20f), new Vector2(0.40f, 0.30f)),
                // j.B
                new MoveSpec("j.B", "Dive Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Martial Art Animations Sample/Animations/Dodge_F.fbx", null,
                    new Vector2(0.85f, 0.05f), new Vector2(0.50f, 0.30f)),
                // j.C
                new MoveSpec("j.C", "Air Cross", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross",
                    new Vector2(1.00f, -0.10f), new Vector2(0.60f, 0.35f)),
                // 5D (Drive)
                new MoveSpec("5D", "Sanpaku Cross", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null,
                    new Vector2(1.10f, 1.10f), new Vector2(0.55f, 0.35f)),
                // BC (Throw)
                new MoveSpec("BC", "Neck Twist Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null,
                    new Vector2(0.60f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A (Special 1)
                new MoveSpec("236A", "Shadow Step", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    8, 3, 16, 950, false,
                    "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx", null,
                    new Vector2(1.15f, 1.05f), new Vector2(0.60f, 0.35f)),
                // 214B (Special 2)
                new MoveSpec("214B", "Rising Shuriken", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1100, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Attack1",
                    new Vector2(1.20f, 1.15f), new Vector2(0.65f, 0.40f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Thousand Moon Execution", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2",
                    new Vector2(1.40f, 1.10f), new Vector2(0.90f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@WalkSlow.FBX",
                crouchFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Run.FBX",
                backdashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_B.fbx",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Fall.FBX",
                knockdownFbx = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                wakeupFbx = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                winFbx = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/SanpakuChan/BBProfile_C08_SanpakuChan.asset",
                "Assets/CombatData/BlazBlue/SanpakuChan/BBMoveSet_C08_SanpakuChan.asset",
                "Assets/CombatData/BlazBlue/SanpakuChan",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/SanpakuChan_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 3. C09 PICOCHAN (Grappler)
        // =========================================================================
        private static void PolishC09_PicoChan()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Heavy Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 14, 300, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null,
                    new Vector2(0.70f, 1.25f), new Vector2(0.40f, 0.30f)),
                // 5B
                new MoveSpec("5B", "Haymaker Hook", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 20, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook",
                    new Vector2(0.95f, 1.20f), new Vector2(0.50f, 0.35f)),
                // 5C
                new MoveSpec("5C", "Megaton Double Hammer", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 26, 900, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.15f, 1.15f), new Vector2(0.60f, 0.40f)),
                // 2A
                new MoveSpec("2A", "Low Punch", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.65f, 0.45f), new Vector2(0.35f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Shin Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", null,
                    new Vector2(0.85f, 0.40f), new Vector2(0.45f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Seismic Uppercut", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut",
                    new Vector2(0.95f, 0.60f), new Vector2(0.55f, 0.45f)),
                // j.A
                new MoveSpec("j.A", "Air Elbow", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null,
                    new Vector2(0.65f, 0.20f), new Vector2(0.40f, 0.30f)),
                // j.B
                new MoveSpec("j.B", "Air Straight", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null,
                    new Vector2(0.85f, 0.10f), new Vector2(0.45f, 0.30f)),
                // j.C
                new MoveSpec("j.C", "Air Body Press", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.00f, -0.10f), new Vector2(0.60f, 0.40f)),
                // 5D (Drive)
                new MoveSpec("5D", "Cyber Clinch", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Hit_A.fbx", null,
                    new Vector2(0.90f, 1.10f), new Vector2(0.50f, 0.40f)),
                // BC (Throw)
                new MoveSpec("BC", "Power Slam", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null,
                    new Vector2(0.65f, 1.00f), new Vector2(0.45f, 0.50f), BBAttribute.Throw),
                // 236A (Special 1)
                new MoveSpec("236A", "Rocket Punch", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    10, 3, 20, 950, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null,
                    new Vector2(1.20f, 1.10f), new Vector2(0.65f, 0.35f)),
                // 214B (Special 2)
                new MoveSpec("214B", "Crushing Impact", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1100, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Attack1",
                    new Vector2(1.25f, 1.15f), new Vector2(0.70f, 0.45f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Atomic Seismic Driver", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.40f, 1.10f), new Vector2(0.90f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@WalkForward.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@WalkBackward.FBX",
                crouchFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                landSubclip = "Land",
                dashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                backdashFbx = "Assets/Martial Art Animations Sample/Animations/Quickstep_B.fbx",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                hitFbx = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Fall.FBX",
                wakeupFbx = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                winFbx = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/PicoChan/BBProfile_C09_PicoChan.asset",
                "Assets/CombatData/BlazBlue/PicoChan/BBMoveSet_C09_PicoChan.asset",
                "Assets/CombatData/BlazBlue/PicoChan",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/PicoChan_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 4. C10 REIKA (Sword Rapier Duelist)
        // =========================================================================
        private static void PolishC10_Reika()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Rapier Fleche", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 14, 300, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", null,
                    new Vector2(0.85f, 1.20f), new Vector2(0.45f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Aristocratic Slash", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 3, 20, 600, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx", null,
                    new Vector2(1.05f, 1.15f), new Vector2(0.55f, 0.30f)),
                // 5C
                new MoveSpec("5C", "Noble Lunge", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 3, 26, 900, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(1.30f, 1.10f), new Vector2(0.65f, 0.30f)),
                // 2A
                new MoveSpec("2A", "Low Feint", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", null,
                    new Vector2(0.80f, 0.45f), new Vector2(0.40f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Low Arc Slash", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx", null,
                    new Vector2(1.00f, 0.40f), new Vector2(0.50f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Regal Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.15f, 0.25f), new Vector2(0.60f, 0.25f)),
                // j.A
                new MoveSpec("j.A", "Air Rapier Thrust", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", null,
                    new Vector2(0.80f, 0.15f), new Vector2(0.45f, 0.30f)),
                // j.B
                new MoveSpec("j.B", "Air Arc", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx", null,
                    new Vector2(0.95f, 0.05f), new Vector2(0.50f, 0.30f)),
                // j.C
                new MoveSpec("j.C", "Air Guillotine Slash", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1",
                    new Vector2(1.10f, -0.10f), new Vector2(0.60f, 0.35f)),
                // 5D (Drive)
                new MoveSpec("5D", "Veil Riposte", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave",
                    new Vector2(1.15f, 1.15f), new Vector2(0.60f, 0.35f)),
                // BC (Throw)
                new MoveSpec("BC", "Hilt Strike Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx", null,
                    new Vector2(0.65f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A (Special 1)
                new MoveSpec("236A", "Prismatic Stride", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    9, 3, 18, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash",
                    new Vector2(1.25f, 1.10f), new Vector2(0.65f, 0.35f)),
                // 214A (Special 2)
                new MoveSpec("214A", "Veil Shield", BBButton.A, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1050, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_ShieldBash",
                    new Vector2(1.15f, 1.15f), new Vector2(0.60f, 0.40f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Judgement Pierce", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    14, 4, 30, 2500, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Attack1",
                    new Vector2(1.45f, 1.10f), new Vector2(0.90f, 0.60f))
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@CombatIdle1H01.fbx",
                walkFwdFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx",
                walkBackFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx",
                jumpFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                landFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                dashFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Strafe/StrafeRun/HumanF@StrafeRun01_Right.fbx",
                backdashFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                blockFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                hitFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx",
                knockdownFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx",
                wakeupFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                winFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle03.fbx"
            };

            ConfigureMoveset(
                "Assets/CombatData/BlazBlue/Reika/BBProfile_C10_Reika.asset",
                "Assets/CombatData/BlazBlue/Reika/BBMoveSet_C10_Reika.asset",
                "Assets/CombatData/BlazBlue/Reika",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/Reika_BB_Override.overrideController"
            );
        }

        // =========================================================================
        // 5. C11 CASUAL1 (Striker)
        // =========================================================================
        private static void PolishC11_Casual1()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Casual Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 300, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null,
                    new Vector2(0.70f, 1.25f), new Vector2(0.35f, 0.25f)),
                // 5B
                new MoveSpec("5B", "Step Straight", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 600, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null,
                    new Vector2(0.90f, 1.20f), new Vector2(0.45f, 0.25f)),
                // 5C
                new MoveSpec("5C", "High Roundhouse", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 25, 900, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", null,
                    new Vector2(1.10f, 1.25f), new Vector2(0.55f, 0.30f)),
                // 2A
                new MoveSpec("2A", "Shin Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 14, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.65f, 0.45f), new Vector2(0.35f, 0.25f)),
                // 2B
                new MoveSpec("2B", "Low Sweep Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 3, 20, 550, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null,
                    new Vector2(0.85f, 0.40f), new Vector2(0.45f, 0.25f)),
                // 2C
                new MoveSpec("2C", "Street Trip", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 3, 26, 750, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.95f, 0.25f), new Vector2(0.60f, 0.25f), BBAttribute.Foot),
                // j.A
                new MoveSpec("j.A", "Air Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 3, 14, 320, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null,
                    new Vector2(0.65f, 0.20f), new Vector2(0.40f, 0.30f)),
                // j.B
                new MoveSpec("j.B", "Air Flying Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 3, 20, 620, false,
                    "Assets/Martial Art Animations Sample/Animations/Dodge_F.fbx", null,
                    new Vector2(0.85f, 0.05f), new Vector2(0.50f, 0.30f)),
                // j.C
                new MoveSpec("j.C", "Air Axe Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 3, 25, 880, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null,
                    new Vector2(1.05f, -0.05f), new Vector2(0.55f, 0.35f)),
                // 5D (Drive)
                new MoveSpec("5D", "Casual Counter Draw", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    18, 3, 32, 850, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", null,
                    new Vector2(1.10f, 1.15f), new Vector2(0.55f, 0.35f)),
                // BC (Throw)
                new MoveSpec("BC", "Street Takedown", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    7, 3, 22, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null,
                    new Vector2(0.60f, 1.00f), new Vector2(0.40f, 0.50f), BBAttribute.Throw),
                // 236A (Special 1)
                new MoveSpec("236A", "Bicycle Kick Rush", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    8, 3, 16, 950, false,
                    "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx", null,
                    new Vector2(1.15f, 1.05f), new Vector2(0.60f, 0.35f)),
                // 214B (Special 2)
                new MoveSpec("214B", "Slipping Uppercut", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 3, 22, 1100, false,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_B.fbx", null,
                    new Vector2(1.10f, 1.15f), new Vector2(0.60f, 0.40f)),
                // 236236C (Super)
                new MoveSpec("236236C", "Starlight Grand Finale", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
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
                "Assets/CombatData/BlazBlue/Archetypes/crossbow/BBProfile_C11_Casual1.asset",
                "Assets/CombatData/BlazBlue/Casual1/BBMoveSet_C11_Casual1.asset",
                "Assets/CombatData/BlazBlue/Casual1",
                specs, loco,
                "Assets/MDI_Characters/Runtime/BlazBlue/Casual1_BB_Override.overrideController"
            );
        }
    }
}
#endif