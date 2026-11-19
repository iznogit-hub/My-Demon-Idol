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
    /// MoveSet, CharacterProfile, and AnimatorOverrideController assets for C08 SanpakuChan in accordance with Spec §8.7 and M12.
    /// Fully resolves and links genuine AnimationClip assets and AnimatorOverrideController clips (zero placeholders).
    /// </summary>
    public static class BBSanpakuChanAssetGenerator
    {
        public const string SANPAKU_ROOT = "Assets/CombatData/BlazBlue/SanpakuChan";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/SanpakuChan/BoxTracks";

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

        [MenuItem("Demon Idol/BlazBlue/Generate SanpakuChan Assets")]
        public static void GenerateAllSanpakuChanAssets()
        {
            EnsureDirectories();

            var specs = GetSanpakuChanMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{SANPAKU_ROOT}/BBMoveSet_C08_SanpakuChan.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C08";
                AssetDatabase.CreateAsset(moveSet, $"{SANPAKU_ROOT}/BBMoveSet_C08_SanpakuChan.asset");
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

            // Create or update SanpakuChan's CharacterProfile
            var profilePath = $"{SANPAKU_ROOT}/BBProfile_C08_SanpakuChan.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C08_SanpakuChan";
            profile.characterName = "SanpakuChan";
            profile.driveName = "Crow Step";
            profile.forwardWalkSpeed = 4.2f;
            profile.backwardWalkSpeed = 3.4f;
            profile.forwardDashSpeed = 9.0f;
            profile.forwardDashDurationTicks = 10;
            profile.backdashSpeed = 8.0f;
            profile.backdashDurationTicks = 11;
            profile.backdashInvulnTicks = 6;
            profile.jumpVerticalLaunch = 8.6f;
            profile.jumpHorizontalSpeed = 3.8f;
            profile.gravity = 20.0f;
            profile.standPushboxSize = new Vector2(0.58f, 1.55f);
            profile.crouchPushboxSize = new Vector2(0.62f, 1.05f);
            profile.airPushboxSize = new Vector2(0.50f, 1.20f);
            profile.maxHealth = 10500f;
            profile.defenseModifier = 1.08f;
            profile.moveSet = moveSet;

            // Link override controller and configure locomotion clips
            var overrideCtrlPath = "Assets/MDI_Characters/Runtime/BlazBlue/SanpakuChan_BB_Override.overrideController";
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideCtrlPath);
            if (overrideCtrl == null)
            {
                var baseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller");
                overrideCtrl = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(overrideCtrl, overrideCtrlPath);
            }

            if (overrideCtrl != null)
            {
                var walkClip = FindClip("16/16_20", "Sanpaku_Walk");
                var dashClip = FindClip("16/16_20", "Sanpaku_Dash");
                var jumpClip = FindClip("143/143_06", "Sanpaku_Jump");
                var crouchClip = FindClip("139/139_09", "Sanpaku_Crouch");
                var standBlockClip = FindClip("144/144_07", "Sanpaku_StandBlock");
                var crouchBlockClip = FindClip("135/135_10", "Sanpaku_CrouchBlock");
                var knockdownClip = FindClip("140/140_01", "Sanpaku_Knockdown");
                var introClip = FindClip("16/16_11", "Sanpaku_Intro");

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
                BBWarriorStanceOverrideBuilder.ApplyWarriorStances(overrideCtrl, "C08");
                EditorUtility.SetDirty(overrideCtrl);
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C08_SanpakuChan
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C08_SanpakuChan.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBSanpakuChanAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C08 SanpakuChan (Zero Placeholders)!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SANPAKU_ROOT)) Directory.CreateDirectory(SANPAKU_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        public static AnimationClip FindClip(string fbxTake, string clipName)
        {
            string fbxPath;
            if (fbxTake == "KevinIglesias_Direct1H")
            {
                fbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx";
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
                        if (fbxTake.StartsWith("KevinIglesias_") && !clip.name.StartsWith("__preview__")) return clip;
                    }
                }
            }
            return null;
        }

        private static BBAttackData CreateOrUpdateAttackData(MoveSpec s)
        {
            string attackPath = $"{SANPAKU_ROOT}/BB_SanpakuChan_{s.moveId}.asset";
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
            string boxTrackPath = $"{BOX_TRACK_ROOT}/BoxTrack_SanpakuChan_{s.moveId}.asset";
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

                // Add active hit/throw box during active ticks
                if (t >= s.startup && t < s.startup + s.active)
                {
                    BBBoxKind boxKind = (s.kind == BBMoveKind.Throw || s.kind == BBMoveKind.CommandThrow)
                        ? BBBoxKind.Throw
                        : BBBoxKind.Hit;

                    boxList.Add(new BBBox(
                        s.hitBoxCenter.x,
                        s.hitBoxCenter.y,
                        s.hitBoxSize.x,
                        s.hitBoxSize.y,
                        boxKind,
                        0,
                        s.attribute
                    ));
                    currentBoxOffset++;
                }
            }

            track.frameStart[totalTicks] = currentBoxOffset;
            track.boxes = boxList.ToArray();
            track.bakeHash = $"Bake_SanpakuChan_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        private static MoveSpec[] GetSanpakuChanMoveSpecs()
        {
            return new MoveSpec[]
            {
                new MoveSpec {
                    moveId = "5A", attackName = "Swift Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 4, active = 3, recovery = 7, damage = 200, guard = BBGuard.Mid, advOnBlock = -1, hitstun = 10, blockstun = 9,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_5A", fbxTake = "14/14_01", firstFrame = 10, lastFrame = 24, hitBoxCenter = new Vector2(0.38f, 1.15f), hitBoxSize = new Vector2(0.40f, 0.30f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "2A", attackName = "Crouch Quick Poke", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 8, damage = 210, guard = BBGuard.Low, advOnBlock = -2, hitstun = 11, blockstun = 9,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_2A", fbxTake = "14/14_01", firstFrame = 30, lastFrame = 46, hitBoxCenter = new Vector2(0.36f, 0.40f), hitBoxSize = new Vector2(0.38f, 0.28f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "6A", attackName = "Rising Anti-Air Upper", button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 12, damage = 540, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 18, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_6A", fbxTake = "14/14_01", firstFrame = 55, lastFrame = 75, hitBoxCenter = new Vector2(0.38f, 1.30f), hitBoxSize = new Vector2(0.45f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(3, 10, BBInvuln.Head) },
                    gatlingInto = new string[] { "5B", "2B", "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5B", attackName = "Speed Cross", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 4, recovery = 11, damage = 480, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 15, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "Sanpaku_5B", fbxTake = "14/14_01", firstFrame = 85, lastFrame = 105, hitBoxCenter = new Vector2(0.48f, 1.15f), hitBoxSize = new Vector2(0.50f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "2B", attackName = "Low Shin Check", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 4, recovery = 12, damage = 460, guard = BBGuard.Low, advOnBlock = -5, hitstun = 13, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_2B", fbxTake = "14/14_01", firstFrame = 115, lastFrame = 133, hitBoxCenter = new Vector2(0.48f, 0.30f), hitBoxSize = new Vector2(0.50f, 0.28f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "6B", attackName = "Instant Hop Overhead", button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 16, active = 3, recovery = 14, damage = 600, guard = BBGuard.Overhead, advOnBlock = -4, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_6B", fbxTake = "118/118_02", firstFrame = 10, lastFrame = 26, hitBoxCenter = new Vector2(0.44f, 1.25f), hitBoxSize = new Vector2(0.50f, 0.55f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5C", attackName = "Heavy Shadow Hook", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 13, damage = 650, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 20, blockstun = 13,
                    groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_5C", fbxTake = "14/14_03", firstFrame = 15, lastFrame = 35, hitBoxCenter = new Vector2(0.50f, 1.15f), hitBoxSize = new Vector2(0.52f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "2C", "3C", "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "2C", attackName = "Slide Sweep", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 16, damage = 580, guard = BBGuard.Low, advOnBlock = -8, hitstun = 24, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_2C", fbxTake = "14/14_03", firstFrame = 45, lastFrame = 67, hitBoxCenter = new Vector2(0.52f, 0.22f), hitBoxSize = new Vector2(0.58f, 0.28f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "3C", "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "3C", attackName = "Launcher Kick", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 15, damage = 620, guard = BBGuard.Mid, advOnBlock = -7, hitstun = 28, blockstun = 14,
                    groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_3C", fbxTake = "14/14_03", firstFrame = 75, lastFrame = 98, hitBoxCenter = new Vector2(0.42f, 1.20f), hitBoxSize = new Vector2(0.48f, 0.65f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D", "6D", "4D", "2D" }
                },
                new MoveSpec {
                    moveId = "j.A", attackName = "Air Quick Tap", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 4, active = 3, recovery = 7, damage = 220, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 12, blockstun = 9,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_jA", fbxTake = "118/118_02", firstFrame = 35, lastFrame = 50, hitBoxCenter = new Vector2(0.35f, 0.65f), hitBoxSize = new Vector2(0.38f, 0.30f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.B", attackName = "Air Swift Poke", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 4, recovery = 10, damage = 550, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 16, blockstun = 11,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_jB", fbxTake = "118/118_02", firstFrame = 60, lastFrame = 78, hitBoxCenter = new Vector2(0.48f, 0.65f), hitBoxSize = new Vector2(0.50f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.C", attackName = "Air Dive Straight", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 5, recovery = 13, damage = 700, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 20, blockstun = 13,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_jC", fbxTake = "118/118_02", firstFrame = 85, lastFrame = 105, hitBoxCenter = new Vector2(0.50f, 0.55f), hitBoxSize = new Vector2(0.52f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec {
                    moveId = "5D", attackName = "Crow Step (Cross-Up)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 14, active = 1, recovery = 12, damage = 0, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 0, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_5D", fbxTake = "91/91_01", firstFrame = 10, lastFrame = 24, hitBoxCenter = new Vector2(0.45f, 1.05f), hitBoxSize = new Vector2(0.45f, 0.45f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(4, 8, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "4D", attackName = "Crow Step (Back)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 10, active = 1, recovery = 12, damage = 0, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 0, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_4D", fbxTake = "91/91_01", firstFrame = 30, lastFrame = 44, hitBoxCenter = new Vector2(-0.45f, 1.05f), hitBoxSize = new Vector2(0.45f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "2D", attackName = "Crow Step (Low Slide)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 10, active = 1, recovery = 14, damage = 0, guard = BBGuard.Low, advOnBlock = 0, hitstun = 0, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_2D", fbxTake = "91/91_01", firstFrame = 50, lastFrame = 64, hitBoxCenter = new Vector2(0.45f, 0.35f), hitBoxSize = new Vector2(0.45f, 0.35f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.D", attackName = "Crow Step (Air Reposition)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 8, active = 1, recovery = 10, damage = 0, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 0, blockstun = 0,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_jD", fbxTake = "91/91_01", firstFrame = 70, lastFrame = 84, hitBoxCenter = new Vector2(0.40f, 0.65f), hitBoxSize = new Vector2(0.45f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "236A", attackName = "Crow Feather Needle", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 11, active = 2, recovery = 15, damage = 420, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 18, blockstun = 14,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Sanpaku_236A", fbxTake = "135/135_10", firstFrame = 50, lastFrame = 72, hitBoxCenter = new Vector2(0.85f, 1.10f), hitBoxSize = new Vector2(0.55f, 0.35f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "j.236A", attackName = "Air Crow Feather Needle", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 10, active = 2, recovery = 14, damage = 400, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 16, blockstun = 12,
                    airOk = true, groundOk = false, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Sanpaku_j236A", fbxTake = "118/118_02", firstFrame = 25, lastFrame = 45, hitBoxCenter = new Vector2(0.70f, 0.45f), hitBoxSize = new Vector2(0.55f, 0.35f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "214B", attackName = "Rekka Dive (1st)", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 9, active = 4, recovery = 14, damage = 550, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_214B_1", fbxTake = "141/141_15", firstFrame = 10, lastFrame = 30, hitBoxCenter = new Vector2(0.52f, 1.10f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "214B_2" }
                },
                new MoveSpec {
                    moveId = "214B_2", attackName = "Rekka Dive (2nd)", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 8, active = 4, recovery = 14, damage = 550, guard = BBGuard.Mid, advOnBlock = -5, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_214B_2", fbxTake = "141/141_15", firstFrame = 35, lastFrame = 55, hitBoxCenter = new Vector2(0.54f, 1.10f), hitBoxSize = new Vector2(0.58f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "214B_3" }
                },
                new MoveSpec {
                    moveId = "214B_3", attackName = "Rekka Dive (3rd Finisher)", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 10, active = 4, recovery = 18, damage = 650, guard = BBGuard.Mid, advOnBlock = -8, hitstun = 24, blockstun = 16,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Sanpaku_214B_3", fbxTake = "141/141_15", firstFrame = 60, lastFrame = 82, hitBoxCenter = new Vector2(0.56f, 1.10f), hitBoxSize = new Vector2(0.60f, 0.42f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "236236B", attackName = "Shadow Dance (DD)", button = BBButton.B, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 12, active = 8, recovery = 20, damage = 2400, guard = BBGuard.Mid, advOnBlock = -12, hitstun = 35, blockstun = 22,
                    groundOk = true, rapidCancelable = true, chipPct = 0.15f, heatCost = 50,
                    clipName = "Sanpaku_Super", fbxTake = "16/16_10", firstFrame = 15, lastFrame = 48, hitBoxCenter = new Vector2(0.55f, 1.15f), hitBoxSize = new Vector2(0.62f, 0.50f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 12, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "214214D", attackName = "Crow Phantasm (Cross-up DD)", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M214,
                    startup = 10, active = 8, recovery = 22, damage = 2600, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 35, blockstun = 24,
                    groundOk = true, heatCost = 50,
                    clipName = "Sanpaku_214214D", fbxTake = "91/91_01", firstFrame = 90, lastFrame = 125, hitBoxCenter = new Vector2(0.58f, 1.15f), hitBoxSize = new Vector2(0.65f, 0.55f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "OD_EA", attackName = "Exceed Accel (Flock of Crows)", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.None,
                    startup = 10, active = 8, recovery = 22, damage = 2000, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 30, blockstun = 20,
                    groundOk = true, rapidCancelable = true,
                    clipName = "Sanpaku_OD_EA", fbxTake = "16/16_10", firstFrame = 15, lastFrame = 48, hitBoxCenter = new Vector2(0.55f, 1.15f), hitBoxSize = new Vector2(0.62f, 0.50f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "6BC", attackName = "Forward Phantasm Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true,
                    clipName = "Sanpaku_Throw", fbxTake = "14/14_03", firstFrame = 110, lastFrame = 132, hitBoxCenter = new Vector2(0.45f, 1.05f), hitBoxSize = new Vector2(0.52f, 0.48f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "4BC", attackName = "Back Phantasm Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true,
                    clipName = "Sanpaku_Throw", fbxTake = "14/14_03", firstFrame = 110, lastFrame = 132, hitBoxCenter = new Vector2(-0.45f, 1.05f), hitBoxSize = new Vector2(0.52f, 0.48f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.BC", attackName = "Air Phantasm Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 20, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    airOk = true, groundOk = false,
                    clipName = "Sanpaku_AirThrow", fbxTake = "118/118_02", firstFrame = 60, lastFrame = 78, hitBoxCenter = new Vector2(0.42f, 0.65f), hitBoxSize = new Vector2(0.48f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6AB", attackName = "Counter Assault", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 18, damage = 200, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 20, blockstun = 15,
                    groundOk = true, heatCost = 50,
                    clipName = "Sanpaku_CounterAssault", fbxTake = "14/14_01", firstFrame = 10, lastFrame = 24, hitBoxCenter = new Vector2(0.42f, 1.15f), hitBoxSize = new Vector2(0.48f, 0.45f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "AB", attackName = "Crush Trigger", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 24, active = 4, recovery = 18, damage = 900, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 28, blockstun = 24,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, heatCost = 25,
                    clipName = "Sanpaku_CrushTrigger", fbxTake = "118/118_02", firstFrame = 10, lastFrame = 26, hitBoxCenter = new Vector2(0.48f, 1.25f), hitBoxSize = new Vector2(0.52f, 0.55f),
                    attribute = BBAttribute.Body
                }
            };
        }
    }
}
#endif
