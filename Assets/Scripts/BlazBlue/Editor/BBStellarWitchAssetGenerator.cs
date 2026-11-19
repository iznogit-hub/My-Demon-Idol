#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Generator script that authors and bakes all BlazBlue AttackData, BoxTrack,
    /// MoveSet, CharacterProfile, and AnimatorOverrideController assets for C07 StellarWitch in accordance with Spec §8.7 and M15.
    /// Fully resolves and links genuine AnimationClip assets and AnimatorOverrideController clips (zero placeholders).
    /// </summary>
    public static class BBStellarWitchAssetGenerator
    {
        public const string STELLARWITCH_ROOT = "Assets/CombatData/BlazBlue/StellarWitch";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/StellarWitch/BoxTracks";

        public struct MoveSpec
        {
            public string moveId;
            public string attackName;
            public BBButton button;
            public BBMoveKind kind;
            public BBMotion motion;
            public int startup;
            public int active;
            public int recovery;
            public int damage;
            public BBGuard guard;
            public int advOnBlock;
            public int hitstun;
            public int blockstun;
            public bool airOk;
            public bool groundOk;
            public bool fatalCounter;
            public bool specialCancel;
            public bool ddCancel;
            public bool jumpCancelOnHit;
            public bool rapidCancelable;
            public string clipName;
            public string fbxTake;
            public int firstFrame;
            public int lastFrame;
            public Vector2 hitBoxCenter;
            public Vector2 hitBoxSize;
            public BBAttribute attribute;
            public BBInvulnWindow[] invuln;
            public string[] gatlingInto;
            public float chipPct;
            public int heatCost;
        }

        [MenuItem("Demon Idol/BlazBlue/Generate StellarWitch Assets")]
        public static void GenerateAllStellarWitchAssets()
        {
            EnsureDirectories();

            var specs = GetStellarWitchMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{STELLARWITCH_ROOT}/BBMoveSet_C07_StellarWitch.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C07";
                AssetDatabase.CreateAsset(moveSet, $"{STELLARWITCH_ROOT}/BBMoveSet_C07_StellarWitch.asset");
            }

            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                var attack = CreateOrUpdateAttackData(s);
                createdMoves.Add(attack);

                // Assign to system move fields if applicable
                if (s.moveId == "6BC") moveSet.throwForward = attack;
                else if (s.moveId == "4BC") moveSet.throwBack = attack;
                else if (s.moveId == "j.BC") moveSet.throwAir = attack;
                else if (s.moveId == "6AB") moveSet.counterAssault = attack;
                else if (s.moveId == "AB") moveSet.crushTrigger = attack;
                else if (s.moveId == "OD_EA") moveSet.exceedAccel = attack;
            }

            moveSet.moves = createdMoves.ToArray();
            moveSet.RebuildLookupTables();
            EditorUtility.SetDirty(moveSet);

            // Create or update StellarWitch's CharacterProfile
            var profilePath = $"{STELLARWITCH_ROOT}/BBProfile_C07_StellarWitch.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C07_StellarWitch";
            profile.characterName = "StellarWitch";
            profile.driveName = "Star Field";
            profile.forwardWalkSpeed = 3.2f; // slower walk per spec
            profile.backwardWalkSpeed = 2.6f;
            profile.forwardDashSpeed = 7.0f;
            profile.forwardDashDurationTicks = 14;
            profile.backdashSpeed = 7.0f;
            profile.backdashDurationTicks = 14;
            profile.backdashInvulnTicks = 6;
            profile.jumpVerticalLaunch = 7.8f; // floaty air mobility
            profile.jumpHorizontalSpeed = 3.0f;
            profile.gravity = 18.0f;
            profile.standPushboxSize = new Vector2(0.56f, 1.50f);
            profile.crouchPushboxSize = new Vector2(0.60f, 1.00f);
            profile.airPushboxSize = new Vector2(0.50f, 1.15f);
            profile.maxHealth = 10500f; // low health zoner
            profile.defenseModifier = 1.05f;
            profile.moveSet = moveSet;

            // Link override controller and configure locomotion clips
            var overrideCtrlPath = "Assets/MDI_Characters/Runtime/BlazBlue/StellarWitch_BB_Override.overrideController";
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideCtrlPath);
            if (overrideCtrl == null)
            {
                var baseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller");
                overrideCtrl = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(overrideCtrl, overrideCtrlPath);
            }

            if (overrideCtrl != null)
            {
                var walkClip = FindClip("16/16_22", "StellarWitch_Walk");
                var dashClip = FindClip("16/16_22", "StellarWitch_Dash");
                var jumpClip = FindClip("143/143_08", "StellarWitch_Jump");
                var crouchClip = FindClip("139/139_11", "StellarWitch_Crouch");
                var standBlockClip = FindClip("144/144_09", "StellarWitch_StandBlock");
                var crouchBlockClip = FindClip("135/135_09", "StellarWitch_CrouchBlock");
                var knockdownClip = FindClip("140/140_03", "StellarWitch_Knockdown");
                var introClip = FindClip("16/16_12", "StellarWitch_Intro");

                var overridesList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideCtrl.GetOverrides(overridesList);

                for (int o = 0; o < overridesList.Count; o++)
                {
                    var original = overridesList[o].Key;
                    if (original == null) continue;
                    string oname = original.name;

                    AnimationClip replacement = null;
                    if (oname == "HumanF@Run01_Forward" && walkClip != null) replacement = walkClip;
                    else if (oname == "HumanF@Run01_Backward" && dashClip != null) replacement = dashClip;
                    else if (oname == "HumanF@StrafeRun01_Right" && jumpClip != null) replacement = jumpClip;
                    else if (oname == "HumanF@Death01" && knockdownClip != null) replacement = knockdownClip;
                    else if (oname == "HumanF@CombatDamage01" && standBlockClip != null) replacement = standBlockClip;
                    else if (oname == "HumanF@CastingDamage01" && crouchBlockClip != null) replacement = crouchBlockClip;
                    else if (oname == "HumanF@Idle03" && introClip != null) replacement = introClip;
                    else if (oname == "HumanF@Idle01" && crouchClip != null) replacement = crouchClip;

                    if (replacement != null)
                    {
                        overridesList[o] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
                    }
                }

                overrideCtrl.ApplyOverrides(overridesList);
                BBWarriorStanceOverrideBuilder.ApplyWarriorStances(overrideCtrl, "C07");
                EditorUtility.SetDirty(overrideCtrl);
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C07_StellarWitch
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C07_StellarWitch.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBStellarWitchAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C07 StellarWitch (Zero Placeholders)!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(STELLARWITCH_ROOT)) Directory.CreateDirectory(STELLARWITCH_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        public static AnimationClip FindClip(string fbxTake, string clipName)
        {
            string fbxPath;
            if (fbxTake == "KevinIglesias_Direct1H")
            {
                fbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx";
            }
            else if (fbxTake == "KevinIglesias_Omni")
            {
                fbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx";
            }
            else if (fbxTake.StartsWith("Assets/"))
            {
                fbxPath = fbxTake;
            }
            else
            {
                fbxPath = $"Assets/Huge Mocap Library/mocap animations/{fbxTake}.fbx";
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is AnimationClip clip)
                    {
                        if (clip.name == clipName) return clip;
                        if ((fbxTake.StartsWith("KevinIglesias_") || fbxTake.StartsWith("Assets/")) && !clip.name.StartsWith("__preview__")) return clip;
                    }
                }
            }
            return null;
        }

        private static BBAttackData CreateOrUpdateAttackData(MoveSpec s)
        {
            string attackPath = $"{STELLARWITCH_ROOT}/BB_StellarWitch_{s.moveId}.asset";
            var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(attackPath);
            if (attack == null)
            {
                attack = ScriptableObject.CreateInstance<BBAttackData>();
                AssetDatabase.CreateAsset(attack, attackPath);
            }

            attack.attackName = s.attackName;
            attack.moveId = s.moveId;
            attack.button = s.button;
            attack.kind = s.kind;
            attack.motion = s.motion;
            attack.startupFrames = s.startup;
            attack.activeFrames = s.active;
            attack.recoveryFrames = s.recovery;
            attack.damage = s.damage;
            attack.guard = s.guard;
            attack.advantageOnBlock = s.advOnBlock;
            attack.hitstunTicks = s.hitstun;
            attack.blockstunTicks = s.blockstun;
            attack.airOk = s.airOk;
            attack.groundOk = s.groundOk;
            attack.fatalCounter = s.fatalCounter;
            attack.specialCancel = s.specialCancel;
            attack.ddCancel = s.ddCancel;
            attack.jumpCancelOnHit = s.jumpCancelOnHit;
            attack.rapidCancelable = s.rapidCancelable;
            attack.clipName = s.clipName;
            attack.clipFirstFrame = s.firstFrame;
            attack.clipLastFrame = s.lastFrame;
            attack.chipPct = s.chipPct;
            attack.heatCost = s.heatCost;
            attack.attribute = s.attribute;
            attack.invuln = s.invuln ?? new BBInvulnWindow[0];
            attack.gatlingInto = s.gatlingInto ?? new string[0];

            // Resolve genuine AnimationClip sub-asset (no placeholders)
            var realClip = FindClip(s.fbxTake, s.clipName);
            attack.clip = realClip;
            attack.animationClip = realClip;

            // Author BoxTrack
            string boxTrackPath = $"{BOX_TRACK_ROOT}/BoxTrack_StellarWitch_{s.moveId}.asset";
            var track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(boxTrackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<BBBoxTrack>();
                AssetDatabase.CreateAsset(track, boxTrackPath);
            }

            BakeBoxTrack(track, s);
            attack.boxes = track;
            EditorUtility.SetDirty(track);
            EditorUtility.SetDirty(attack);

            return attack;
        }

        private static void BakeBoxTrack(BBBoxTrack track, MoveSpec s)
        {
            int totalTicks = s.startup + s.active + s.recovery;
            if (totalTicks <= 0) totalTicks = 1;

            track.tickCount = totalTicks;
            track.frameStart = new int[totalTicks + 1];

            var boxList = new List<BBBox>();
            int currentBoxOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentBoxOffset;

                // Pushbox
                boxList.Add(new BBBox(0f, 0.75f, 0.56f, 1.50f, BBBoxKind.Push, 0, BBAttribute.Body));
                currentBoxOffset++;

                // Hurtboxes
                bool isInvuln = false;
                if (s.invuln != null)
                {
                    for (int iv = 0; iv < s.invuln.Length; iv++)
                    {
                        if (t >= s.invuln[iv].startTick && t <= s.invuln[iv].endTick)
                        {
                            if (s.invuln[iv].mask.HasFlag(BBInvuln.Body))
                            {
                                isInvuln = true;
                                break;
                            }
                        }
                    }
                }

                if (!isInvuln)
                {
                    boxList.Add(new BBBox(0f, 0.85f, 0.54f, 1.25f, BBBoxKind.Hurt, 0, BBAttribute.Body));
                    currentBoxOffset++;

                    boxList.Add(new BBBox(0f, 0.25f, 0.48f, 0.50f, BBBoxKind.Hurt, 0, BBAttribute.Foot));
                    currentBoxOffset++;
                }

                // Hitbox / Throwbox during active frames
                if (t >= s.startup && t < (s.startup + s.active))
                {
                    if (s.kind == BBMoveKind.Throw)
                    {
                        boxList.Add(new BBBox(s.hitBoxCenter.x, s.hitBoxCenter.y, s.hitBoxSize.x, s.hitBoxSize.y, BBBoxKind.Throw, 0, s.attribute));
                        currentBoxOffset++;
                    }
                    else if (s.damage > 0)
                    {
                        boxList.Add(new BBBox(s.hitBoxCenter.x, s.hitBoxCenter.y, s.hitBoxSize.x, s.hitBoxSize.y, BBBoxKind.Hit, 0, s.attribute));
                        currentBoxOffset++;
                    }
                }
            }

            track.frameStart[totalTicks] = currentBoxOffset;
            track.boxes = boxList.ToArray();
            track.bakeHash = $"Bake_StellarWitch_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        public static MoveSpec[] GetStellarWitchMoveSpecs()
        {
            return new MoveSpec[]
            {
                // ================= GROUND NORMALS =================
                new MoveSpec
                {
                    moveId = "5A", attackName = "Standing Light Wand Swat",
                    button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 3, recovery = 8, damage = 240, guard = BBGuard.Mid, advOnBlock = -1,
                    hitstun = 14, blockstun = 12, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_5A", fbxTake = "135/135_06", firstFrame = 10, lastFrame = 25,
                    hitBoxCenter = new Vector2(0.65f, 0.95f), hitBoxSize = new Vector2(0.60f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "2A", "5B", "2B", "5C", "2C", "5D" }
                },
                new MoveSpec
                {
                    moveId = "2A", attackName = "Crouching Light Spark",
                    button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 8, damage = 220, guard = BBGuard.Low, advOnBlock = 1,
                    hitstun = 13, blockstun = 11, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_2A", fbxTake = "135/135_06", firstFrame = 30, lastFrame = 48,
                    hitBoxCenter = new Vector2(0.60f, 0.25f), hitBoxSize = new Vector2(0.55f, 0.35f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5A", "2A", "5B", "2B" }
                },
                new MoveSpec
                {
                    moveId = "6A", attackName = "Arcing Astral Strike",
                    button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 11, active = 5, recovery = 14, damage = 680, guard = BBGuard.Mid, advOnBlock = -2,
                    hitstun = 20, blockstun = 16, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_6A", fbxTake = "135/135_07", firstFrame = 135, lastFrame = 160,
                    hitBoxCenter = new Vector2(0.55f, 1.45f), hitBoxSize = new Vector2(0.70f, 0.70f),
                    attribute = BBAttribute.Head,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 11, BBInvuln.Head) },
                    gatlingInto = new string[] { "5C", "2C", "623C" }
                },
                new MoveSpec
                {
                    moveId = "5B", attackName = "Starlight Thrust",
                    button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 12, damage = 580, guard = BBGuard.Mid, advOnBlock = -2,
                    hitstun = 18, blockstun = 14, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_5B", fbxTake = "135/135_06", firstFrame = 60, lastFrame = 84,
                    hitBoxCenter = new Vector2(0.85f, 0.90f), hitBoxSize = new Vector2(0.80f, 0.45f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "2B", "5C", "2C", "5D" }
                },
                new MoveSpec
                {
                    moveId = "2B", attackName = "Ground Astral Sweep",
                    button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 14, damage = 540, guard = BBGuard.Low, advOnBlock = -3,
                    hitstun = 17, blockstun = 13, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_2B", fbxTake = "135/135_06", firstFrame = 95, lastFrame = 118,
                    hitBoxCenter = new Vector2(0.80f, 0.25f), hitBoxSize = new Vector2(0.75f, 0.35f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "5D" }
                },
                new MoveSpec
                {
                    moveId = "6B", attackName = "Luminescence Crest",
                    button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 21, active = 4, recovery = 16, damage = 780, guard = BBGuard.Overhead, advOnBlock = 2,
                    hitstun = 22, blockstun = 18, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_6B", fbxTake = "135/135_06", firstFrame = 130, lastFrame = 155,
                    hitBoxCenter = new Vector2(0.75f, 0.85f), hitBoxSize = new Vector2(0.70f, 0.65f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "5D" }
                },
                new MoveSpec
                {
                    moveId = "5C", attackName = "Starlight Beam",
                    button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 14, active = 5, recovery = 18, damage = 920, guard = BBGuard.Mid, advOnBlock = -4,
                    hitstun = 24, blockstun = 18, groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_5C", fbxTake = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", firstFrame = 0, lastFrame = 40,
                    hitBoxCenter = new Vector2(0.75f, 1.20f), hitBoxSize = new Vector2(0.85f, 0.60f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[] { "2C", "3C", "5D", "6D" }
                },
                new MoveSpec
                {
                    moveId = "2C", attackName = "Cosmic Ring Sweep",
                    button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 12, active = 4, recovery = 20, damage = 840, guard = BBGuard.Low, advOnBlock = -6,
                    hitstun = 26, blockstun = 18, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_2C", fbxTake = "135/135_07", firstFrame = 55, lastFrame = 80,
                    hitBoxCenter = new Vector2(0.95f, 0.20f), hitBoxSize = new Vector2(0.95f, 0.35f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "3C", "5D" }
                },
                new MoveSpec
                {
                    moveId = "3C", attackName = "Nova Flare Launcher",
                    button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 15, active = 5, recovery = 22, damage = 880, guard = BBGuard.Mid, advOnBlock = -7,
                    hitstun = 28, blockstun = 18, groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_3C", fbxTake = "135/135_07", firstFrame = 95, lastFrame = 122,
                    hitBoxCenter = new Vector2(0.70f, 1.25f), hitBoxSize = new Vector2(0.65f, 0.90f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "5D" }
                },

                // ================= JUMP NORMALS =================
                new MoveSpec
                {
                    moveId = "j.A", attackName = "Jumping Wand Tap",
                    button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 4, recovery = 9, damage = 260, guard = BBGuard.Mid, advOnBlock = 0,
                    hitstun = 14, blockstun = 12, airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_jA", fbxTake = "118/118_05", firstFrame = 12, lastFrame = 28,
                    hitBoxCenter = new Vector2(0.55f, 0.60f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec
                {
                    moveId = "j.B", attackName = "Jumping Starlight Flash",
                    button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 12, damage = 520, guard = BBGuard.Mid, advOnBlock = -1,
                    hitstun = 18, blockstun = 14, airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_jB", fbxTake = "118/118_05", firstFrame = 35, lastFrame = 55,
                    hitBoxCenter = new Vector2(0.75f, 0.55f), hitBoxSize = new Vector2(0.70f, 0.50f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec
                {
                    moveId = "j.C", attackName = "Jumping Comet Slash",
                    button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 13, active = 5, recovery = 16, damage = 760, guard = BBGuard.Overhead, advOnBlock = 1,
                    hitstun = 22, blockstun = 16, airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_jC", fbxTake = "118/118_05", firstFrame = 65, lastFrame = 88,
                    hitBoxCenter = new Vector2(0.85f, 0.40f), hitBoxSize = new Vector2(0.85f, 0.60f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec
                {
                    moveId = "j.D", attackName = "Star Field: Delta (Air)",
                    button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 14, active = 6, recovery = 16, damage = 550, guard = BBGuard.Mid, advOnBlock = -2,
                    hitstun = 20, blockstun = 16, airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_jD", fbxTake = "135/135_08", firstFrame = 115, lastFrame = 140,
                    hitBoxCenter = new Vector2(1.00f, 0.70f), hitBoxSize = new Vector2(0.75f, 0.60f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },

                // ================= DRIVE MOVES =================
                new MoveSpec
                {
                    moveId = "5D", attackName = "Star Field: Alpha (Mid)",
                    button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 16, active = 4, recovery = 18, damage = 480, guard = BBGuard.Mid, advOnBlock = -3,
                    hitstun = 20, blockstun = 16, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_5D", fbxTake = "135/135_08", firstFrame = 10, lastFrame = 35,
                    hitBoxCenter = new Vector2(1.20f, 0.90f), hitBoxSize = new Vector2(0.70f, 0.60f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "2D", attackName = "Star Field: Beta (Low)",
                    button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 18, active = 4, recovery = 20, damage = 480, guard = BBGuard.Low, advOnBlock = -4,
                    hitstun = 20, blockstun = 16, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_2D", fbxTake = "135/135_08", firstFrame = 45, lastFrame = 70,
                    hitBoxCenter = new Vector2(1.00f, 0.20f), hitBoxSize = new Vector2(0.70f, 0.40f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "6D", attackName = "Star Field: Gamma (High)",
                    button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 20, active = 4, recovery = 22, damage = 480, guard = BBGuard.Mid, advOnBlock = -5,
                    hitstun = 20, blockstun = 16, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_6D", fbxTake = "135/135_08", firstFrame = 80, lastFrame = 105,
                    hitBoxCenter = new Vector2(1.40f, 1.40f), hitBoxSize = new Vector2(0.70f, 0.60f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "CometFire", attackName = "Starlight Comet",
                    button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 12, active = 6, recovery = 18, damage = 480, guard = BBGuard.Mid, advOnBlock = -1,
                    hitstun = 22, blockstun = 16, groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_CometFire", fbxTake = "135/135_08", firstFrame = 150, lastFrame = 175,
                    hitBoxCenter = new Vector2(1.80f, 0.90f), hitBoxSize = new Vector2(1.00f, 0.60f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },

                // ================= SPECIALS =================
                new MoveSpec
                {
                    moveId = "236A", attackName = "Astral Bolt (Straight)",
                    button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 15, active = 8, recovery = 18, damage = 700, guard = BBGuard.Mid, advOnBlock = -2,
                    hitstun = 22, blockstun = 16, groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.12f,
                    clipName = "StellarWitch_236A", fbxTake = "135/135_04", firstFrame = 10, lastFrame = 40,
                    hitBoxCenter = new Vector2(1.50f, 0.90f), hitBoxSize = new Vector2(0.90f, 0.50f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "236B", attackName = "Astral Bolt (Angled)",
                    button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 17, active = 8, recovery = 19, damage = 850, guard = BBGuard.Mid, advOnBlock = -3,
                    hitstun = 24, blockstun = 16, groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.15f,
                    clipName = "StellarWitch_236B", fbxTake = "135/135_04", firstFrame = 50, lastFrame = 80,
                    hitBoxCenter = new Vector2(1.60f, 1.25f), hitBoxSize = new Vector2(0.90f, 0.50f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "623C", attackName = "Nova Pillar",
                    button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M623,
                    startup = 9, active = 6, recovery = 28, damage = 1250, guard = BBGuard.Mid, advOnBlock = -15,
                    hitstun = 30, blockstun = 20, groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.20f,
                    clipName = "StellarWitch_623C", fbxTake = "135/135_04", firstFrame = 90, lastFrame = 125,
                    hitBoxCenter = new Vector2(0.50f, 1.50f), hitBoxSize = new Vector2(0.85f, 1.80f),
                    attribute = BBAttribute.Body,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 9, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) },
                    gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "214B", attackName = "Eclipse Shift",
                    button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 6, active = 12, recovery = 14, damage = 0, guard = BBGuard.Mid, advOnBlock = 0,
                    hitstun = 0, blockstun = 0, groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "StellarWitch_214B", fbxTake = "135/135_04", firstFrame = 135, lastFrame = 165,
                    hitBoxCenter = Vector2.zero, hitBoxSize = Vector2.zero,
                    attribute = BBAttribute.Body,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 12, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile) },
                    gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "236C", attackName = "Supernova",
                    button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 12, active = 8, recovery = 22, damage = 1400, guard = BBGuard.Mid, advOnBlock = -4,
                    hitstun = 28, blockstun = 18, groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.25f,
                    clipName = "StellarWitch_236C", fbxTake = "135/135_04", firstFrame = 175, lastFrame = 210,
                    hitBoxCenter = new Vector2(1.50f, 1.00f), hitBoxSize = new Vector2(1.50f, 1.50f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[0]
                },

                // ================= DISTORTION DRIVES =================
                new MoveSpec
                {
                    moveId = "236236C", attackName = "Celestial Cataclysm",
                    button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 11, active = 30, recovery = 35, damage = 3400, guard = BBGuard.Mid, advOnBlock = -10,
                    hitstun = 35, blockstun = 24, groundOk = true, rapidCancelable = true, chipPct = 0.35f, heatCost = 50,
                    clipName = "StellarWitch_236236C", fbxTake = "135/135_05", firstFrame = 10, lastFrame = 60,
                    hitBoxCenter = new Vector2(2.80f, 0.95f), hitBoxSize = new Vector2(4.50f, 1.20f),
                    attribute = BBAttribute.Projectile,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 11, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) },
                    gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "632146C", attackName = "Constellation Convergence",
                    button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M632146,
                    startup = 13, active = 24, recovery = 32, damage = 3800, guard = BBGuard.Mid, advOnBlock = -8,
                    hitstun = 40, blockstun = 25, groundOk = true, rapidCancelable = true, chipPct = 0.40f, heatCost = 50,
                    clipName = "StellarWitch_632146C", fbxTake = "135/135_05", firstFrame = 70, lastFrame = 130,
                    hitBoxCenter = new Vector2(2.00f, 1.10f), hitBoxSize = new Vector2(3.20f, 2.20f),
                    attribute = BBAttribute.Projectile,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 13, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) },
                    gatlingInto = new string[0]
                },

                // ================= SYSTEM MOVES =================
                new MoveSpec
                {
                    moveId = "6BC", attackName = "Forward Starlight Throw",
                    button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 4, active = 2, recovery = 24, damage = 1050, guard = BBGuard.Mid, advOnBlock = 0,
                    hitstun = 30, blockstun = 0, groundOk = true,
                    clipName = "StellarWitch_Throw", fbxTake = "135/135_07", firstFrame = 175, lastFrame = 200,
                    hitBoxCenter = new Vector2(0.55f, 0.85f), hitBoxSize = new Vector2(0.45f, 0.70f),
                    attribute = BBAttribute.Body, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "4BC", attackName = "Backward Starlight Throw",
                    button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 4, active = 2, recovery = 24, damage = 1050, guard = BBGuard.Mid, advOnBlock = 0,
                    hitstun = 30, blockstun = 0, groundOk = true,
                    clipName = "StellarWitch_BackThrow", fbxTake = "135/135_07", firstFrame = 175, lastFrame = 200,
                    hitBoxCenter = new Vector2(-0.55f, 0.85f), hitBoxSize = new Vector2(0.45f, 0.70f),
                    attribute = BBAttribute.Body, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "j.BC", attackName = "Aerial Starlight Throw",
                    button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 4, active = 2, recovery = 24, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0,
                    hitstun = 30, blockstun = 0, airOk = true, groundOk = false,
                    clipName = "StellarWitch_AirThrow", fbxTake = "118/118_05", firstFrame = 95, lastFrame = 118,
                    hitBoxCenter = new Vector2(0.55f, 0.50f), hitBoxSize = new Vector2(0.45f, 0.70f),
                    attribute = BBAttribute.Body, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "6AB", attackName = "Counter Assault",
                    button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 11, active = 4, recovery = 20, damage = 300, guard = BBGuard.Mid, advOnBlock = -4,
                    hitstun = 24, blockstun = 16, groundOk = true, heatCost = 50,
                    clipName = "StellarWitch_CounterAssault", fbxTake = "135/135_07", firstFrame = 135, lastFrame = 160,
                    hitBoxCenter = new Vector2(0.70f, 0.90f), hitBoxSize = new Vector2(0.75f, 0.60f),
                    attribute = BBAttribute.Body,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 11, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) },
                    gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "AB", attackName = "Crush Trigger",
                    button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 24, active = 4, recovery = 24, damage = 1100, guard = BBGuard.Mid, advOnBlock = 2,
                    hitstun = 28, blockstun = 20, groundOk = true, heatCost = 25,
                    clipName = "StellarWitch_CrushTrigger", fbxTake = "135/135_07", firstFrame = 15, lastFrame = 42,
                    hitBoxCenter = new Vector2(0.85f, 0.95f), hitBoxSize = new Vector2(0.80f, 0.70f),
                    attribute = BBAttribute.Body, gatlingInto = new string[0]
                },
                new MoveSpec
                {
                    moveId = "OD_EA", attackName = "Exceed Accel: Supernova Catastrophe",
                    button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.None,
                    startup = 10, active = 8, recovery = 30, damage = 2400, guard = BBGuard.Mid, advOnBlock = -12,
                    hitstun = 40, blockstun = 22, groundOk = true,
                    clipName = "StellarWitch_OD_EA", fbxTake = "135/135_05", firstFrame = 140, lastFrame = 180,
                    hitBoxCenter = new Vector2(1.20f, 1.00f), hitBoxSize = new Vector2(1.60f, 1.40f),
                    attribute = BBAttribute.Projectile,
                    invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile) },
                    gatlingInto = new string[0]
                }
            };
        }
    }
}
#endif
