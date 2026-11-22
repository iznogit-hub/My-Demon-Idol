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
    public static class Batch4CharacterPolishTool
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

        public static void ApplyBatch4Polish()
        {
            Debug.Log("<color=#00e676>[Batch4Polish] Starting character polish for SK05, SK06, SK08, EX01, EX02...</color>");

            PolishSK05_LordMorvath();
            PolishSK06_CommanderRen();
            PolishSK08_Torgrim();
            PolishEX01_Hugo();
            PolishEX02_Granite();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676>[Batch4Polish] Successfully polished all 5 fighters in Batch 4!</color>");
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName = null)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[Batch4Polish] No clips found in {fbxPath}");
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
                Debug.LogError($"[Batch4Polish] Missing profile ({profilePath})");
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
        // 1. SK05 LORD MORVATH (Caster / Tyrannical Death Magus)
        // =========================================================================
        private static void PolishSK05_LordMorvath()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Dark Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 12, 300, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null,
                    new Vector2(0.90f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Projectile),
                // 5B
                new MoveSpec("5B", "Shadow Bolt", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 4, 18, 600, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null,
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Projectile),
                // 5C
                new MoveSpec("5C", "Abyssal Beam", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 5, 24, 900, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null,
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Projectile),
                // 2A
                new MoveSpec("2A", "Crawl Hex", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 10, 250, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Cast.fbx", null,
                    new Vector2(0.85f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Projectile),
                // 2B
                new MoveSpec("2B", "Nether Wave", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 16, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Cast.fbx", null,
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Projectile),
                // 2C
                new MoveSpec("2C", "Staff Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 26, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_StaffPoke",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Load.fbx", null,
                    new Vector2(0.85f, -0.20f), new Vector2(0.55f, 0.65f), BBAttribute.Projectile),
                // j.B
                new MoveSpec("j.B", "Air Shadow Bolt", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    9, 5, 16, 550, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Load.fbx", null,
                    new Vector2(0.95f, -0.20f), new Vector2(0.60f, 0.65f), BBAttribute.Projectile),
                // j.C
                new MoveSpec("j.C", "Air Nether Beam", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Cast.fbx", null,
                    new Vector2(1.05f, -0.20f), new Vector2(0.65f, 0.70f), BBAttribute.Projectile),
                // 5D
                new MoveSpec("5D", "Soul Syphon", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    12, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_OrbCast",
                    new Vector2(1.05f, 1.00f), new Vector2(0.65f, 0.45f), BBAttribute.Projectile),
                // BC
                new MoveSpec("BC", "Soul Grip", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Grave Spikes", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    12, 6, 22, 850, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L - Cast.fbx", null,
                    new Vector2(1.00f, 0.85f), new Vector2(0.60f, 0.45f), BBAttribute.Projectile),
                // 214B
                new MoveSpec("214B", "Death Eruption", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    14, 6, 24, 950, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_Eruption",
                    new Vector2(1.05f, 1.10f), new Vector2(0.65f, 0.50f), BBAttribute.Projectile),
                // 236236C
                new MoveSpec("236236C", "Apocalypse Void", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    10, 12, 36, 2600, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null,
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Projectile)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingIdle01.fbx",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Jump.FBX",
                landSubclip = "Land",
                blockFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx",
                hitFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingDamage01.fbx",
                knockdownFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
                wakeupFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingExit01.fbx",
                winFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle03.fbx"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/mage/BBProfile_SK05_LordMorvath.asset",
                             "Assets/CombatData/BlazBlue/LordMorvath/BBMoveSet_SK05_LordMorvath.asset",
                             "Assets/CombatData/BlazBlue/LordMorvath",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK05_LordMorvath_Override.overrideController");
        }

        // =========================================================================
        // 2. SK06 COMMANDER REN (Sword / Disciplined Halberd / Military Commander)
        // =========================================================================
        private static void PolishSK06_CommanderRen()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Saber Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 300, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx", null,
                    new Vector2(0.85f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Tactical Cross", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 18, 600, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx", null,
                    new Vector2(0.95f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Commander's Execution", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    13, 5, 24, 950, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx", null,
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Low Stride", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 11, 250, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1",
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Ankle Sever", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    9, 4, 17, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Vanguard Trip", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    12, 5, 25, 800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Point", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust",
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Saber", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    8, 5, 16, 550, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1",
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    12, 6, 20, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Legion Pierce", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    11, 5, 22, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Pommel Strike", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Vanguard Charge", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    11, 6, 20, 800, false,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx", null,
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Order's Rebuke", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Victory March", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    9, 12, 36, 2600, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx", null,
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx",
                crouchFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Jump.FBX",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Fall.FBX",
                blockFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
                hitFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx",
                knockdownFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Fall.FBX",
                winFbx = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle02.fbx"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/twohand/BBProfile_SK06_CommanderRen.asset",
                             "Assets/CombatData/BlazBlue/CommanderRen/BBMoveSet_SK06_CommanderRen.asset",
                             "Assets/CombatData/BlazBlue/CommanderRen",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK06_CommanderRen_Override.overrideController");
        }

        // =========================================================================
        // 3. SK08 TORGRIM (Greatsword / Hammer / Mountain Warrior)
        // =========================================================================
        private static void PolishSK08_Torgrim()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Forge Strike", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 13, 350, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing",
                    new Vector2(0.90f, 0.95f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Mountain Cleave", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 4, 20, 650, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash",
                    new Vector2(1.00f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Anvil Slam", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    15, 5, 28, 1050, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash",
                    new Vector2(1.10f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Low Sweep", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep",
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Shin Cracker", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Earth Shatter", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 5, 28, 850, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Quake",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Forge Strike", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing",
                    new Vector2(0.85f, 0.95f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Mountain Cleave", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    10, 5, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash",
                    new Vector2(0.95f, 0.95f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Anvil Slam", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 6, 24, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Rune Quake", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    13, 5, 24, 800, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Dwarf Toss", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Avalanche Breaker", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    13, 6, 24, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Iron Cyclone", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    12, 6, 22, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Wrath of the Mountain", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    10, 12, 36, 2700, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Quake",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX",
                crouchFbx = "Assets/Kevin Iglesias/Dwarf Animations/Animations/Male/Idles/DwarfM@Idle01.fbx",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                landSubclip = "Land",
                blockFbx = "Assets/Kevin Iglesias/Dwarf Animations/Animations/Male/Movement/Jump/DwarfM@Jump01 - Land.fbx",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Fall.FBX",
                knockdownFbx = "Assets/Kevin Iglesias/Dwarf Animations/Animations/Male/Movement/Jump/DwarfM@Fall01.fbx",
                wakeupFbx = "Assets/Kevin Iglesias/Dwarf Animations/Animations/Male/Movement/Jump/DwarfM@Jump01 - Begin.fbx",
                winFbx = "Assets/Kevin Iglesias/Dwarf Animations/Animations/Male/Social/Conversation/DwarfM@Talk01.fbx"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/hammer/BBProfile_SK08_Torgrim.asset",
                             "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                             "Assets/CombatData/BlazBlue/Torgrim",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK08_Torgrim_Override.overrideController");
        }

        // =========================================================================
        // 4. EX01 HUGO (Brute / Cybernetic Gorilla Brawler)
        // =========================================================================
        private static void PolishEX01_Hugo()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Gorilla Hook", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 13, 350, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook",
                    new Vector2(0.90f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Titan Uppercut", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 4, 20, 700, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut",
                    new Vector2(1.00f, 1.05f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Primal Double Smash", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    15, 5, 28, 1100, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.10f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Gorilla Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    6, 3, 12, 280, false,
                    "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null,
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Knee Cracker", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    10, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Earth Splitter", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    14, 5, 28, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Gorilla Hook", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    6, 4, 12, 320, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook",
                    new Vector2(0.85f, 0.45f), new Vector2(0.55f, 0.65f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Uppercut", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    10, 5, 18, 650, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut",
                    new Vector2(0.95f, 0.45f), new Vector2(0.60f, 0.65f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Double Hammer", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    14, 6, 24, 1000, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.05f, 0.45f), new Vector2(0.65f, 0.70f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Berserk Roar", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    12, 5, 24, 800, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null,
                    new Vector2(1.05f, 1.00f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Gorilla Slam", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1100, true,
                    "Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null,
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Colossal Haymaker", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    12, 6, 22, 850, false,
                    "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null,
                    new Vector2(1.05f, 1.00f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Seismic Launcher", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    13, 6, 24, 950, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut",
                    new Vector2(1.05f, 1.10f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Extinction Slam", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    10, 12, 36, 2800, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Idle.FBX",
                crouchFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                landSubclip = "Land",
                blockFbx = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                wakeupSubclip = "Land",
                winFbx = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/brute/BBProfile_EX01_Hugo.asset",
                             "Assets/CombatData/BlazBlue/Hugo/BBMoveSet_EX01_Hugo.asset",
                             "Assets/CombatData/BlazBlue/Hugo",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX01_Hugo_Override.overrideController");
        }

        // =========================================================================
        // 5. EX02 GRANITE (Brute / Hammer / Stone Behemoth)
        // =========================================================================
        private static void PolishEX02_Granite()
        {
            var specs = new List<MoveSpec>
            {
                // 5A
                new MoveSpec("5A", "Granite Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    8, 3, 13, 350, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook",
                    new Vector2(0.90f, 1.00f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // 5B
                new MoveSpec("5B", "Monolith Swipe", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    12, 4, 20, 650, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing",
                    new Vector2(1.00f, 1.00f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // 5C
                new MoveSpec("5C", "Tectonic Smash", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    16, 5, 28, 1050, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash",
                    new Vector2(1.10f, 1.05f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 2A
                new MoveSpec("2A", "Rock Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, false, true,
                    7, 3, 12, 280, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut",
                    new Vector2(0.90f, 0.55f), new Vector2(0.50f, 0.30f), BBAttribute.Body),
                // 2B
                new MoveSpec("2B", "Stone Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, false, true,
                    11, 4, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing",
                    new Vector2(0.95f, 0.55f), new Vector2(0.55f, 0.35f), BBAttribute.Body),
                // 2C
                new MoveSpec("2C", "Tremor", BBButton.C, BBMoveKind.Normal, BBMotion.None, false, true,
                    15, 5, 28, 850, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Quake",
                    new Vector2(1.05f, 0.55f), new Vector2(0.65f, 0.40f), BBAttribute.Body),
                // j.A
                new MoveSpec("j.A", "Air Granite Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, true, false,
                    7, 4, 12, 300, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook",
                    new Vector2(0.85f, 0.95f), new Vector2(0.50f, 0.35f), BBAttribute.Body),
                // j.B
                new MoveSpec("j.B", "Air Monolith Swipe", BBButton.B, BBMoveKind.Normal, BBMotion.None, true, false,
                    11, 5, 18, 600, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing",
                    new Vector2(0.95f, 0.95f), new Vector2(0.55f, 0.40f), BBAttribute.Body),
                // j.C
                new MoveSpec("j.C", "Air Tectonic Drop", BBButton.C, BBMoveKind.Normal, BBMotion.None, true, false,
                    15, 6, 24, 950, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 5D
                new MoveSpec("5D", "Bedrock Impact", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, false, true,
                    14, 5, 24, 800, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam",
                    new Vector2(1.10f, 1.00f), new Vector2(0.70f, 0.45f), BBAttribute.Body),
                // BC
                new MoveSpec("BC", "Continental Crush", BBButton.B, BBMoveKind.Throw, BBMotion.None, false, true,
                    5, 3, 20, 1000, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam",
                    new Vector2(0.80f, 0.90f), new Vector2(0.50f, 0.40f), BBAttribute.Body),
                // 236A
                new MoveSpec("236A", "Cataclysm Cleave", BBButton.A, BBMoveKind.Special, BBMotion.M236, false, true,
                    13, 6, 24, 850, false,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash",
                    new Vector2(1.05f, 0.95f), new Vector2(0.65f, 0.45f), BBAttribute.Body),
                // 214B
                new MoveSpec("214B", "Faultline Eruption", BBButton.B, BBMoveKind.Special, BBMotion.M214, false, true,
                    14, 6, 24, 900, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Quake",
                    new Vector2(1.05f, 1.05f), new Vector2(0.65f, 0.50f), BBAttribute.Body),
                // 236236C
                new MoveSpec("236236C", "Meteor Crush", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, false, true,
                    11, 12, 36, 2700, true,
                    "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash",
                    new Vector2(1.10f, 1.05f), new Vector2(0.75f, 0.60f), BBAttribute.Body)
            };

            var loco = new LocomotionSpec
            {
                idleFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX",
                crouchFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Idle.FBX",
                walkFwdFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Walk.FBX",
                walkBackFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@WalkSlow.FBX",
                dashFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Run.FBX",
                jumpFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                jumpSubclip = "Jump",
                landFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                landSubclip = "Land",
                blockFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                hitFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Fall.FBX",
                knockdownFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Fall.FBX",
                wakeupFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                wakeupSubclip = "Land",
                winFbx = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX"
            };

            ConfigureMoveset("Assets/CombatData/BlazBlue/Archetypes/hammer/BBProfile_EX02_Granite.asset",
                             "Assets/CombatData/BlazBlue/Granite/BBMoveSet_EX02_Granite.asset",
                             "Assets/CombatData/BlazBlue/Granite",
                             specs, loco,
                             "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX02_Granite_Override.overrideController");
        }
    }
}
#endif
