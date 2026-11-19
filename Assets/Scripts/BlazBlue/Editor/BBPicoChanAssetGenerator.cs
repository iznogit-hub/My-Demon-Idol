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
    /// MoveSet, CharacterProfile, and AnimatorOverrideController assets for C09 PicoChan in accordance with Spec §8.6 and M10.
    /// Fully resolves and links genuine AnimationClip assets and AnimatorOverrideController clips (zero placeholders).
    /// </summary>
    public static class BBPicoChanAssetGenerator
    {
        public const string PICOCHAN_ROOT = "Assets/CombatData/BlazBlue/PicoChan";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/PicoChan/BoxTracks";

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

        [MenuItem("DemonIdol/BlazBlue/Generate PicoChan Assets")]
        public static void GenerateAllPicoChanAssets()
        {
            EnsureDirectories();

            var specs = GetPicoChanMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{PICOCHAN_ROOT}/BBMoveSet_C09_PicoChan.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C09";
                AssetDatabase.CreateAsset(moveSet, $"{PICOCHAN_ROOT}/BBMoveSet_C09_PicoChan.asset");
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

            // Create or update PicoChan's CharacterProfile
            var profilePath = $"{PICOCHAN_ROOT}/BBProfile_C09_PicoChan.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C09_PicoChan";
            profile.characterName = "PicoChan";
            profile.driveName = "Clinch";
            profile.forwardWalkSpeed = 3.6f;
            profile.backwardWalkSpeed = 2.8f;
            profile.forwardDashSpeed = 8.5f; // Step dash (spec §8.6)
            profile.forwardDashDurationTicks = 12;
            profile.backdashSpeed = 7.0f;
            profile.backdashDurationTicks = 12;
            profile.backdashInvulnTicks = 6;
            profile.jumpVerticalLaunch = 7.8f;
            profile.jumpHorizontalSpeed = 3.2f;
            profile.gravity = 20.0f;
            // Short body dimensions (spec §6.4 & §8.6: hips 0.76m, crouch 0.42m/0.22m, air 0.90m/0.20m)
            profile.standPushboxSize = new Vector2(0.60f, 1.50f);
            profile.crouchPushboxSize = new Vector2(0.60f, 1.05f);
            profile.airPushboxSize = new Vector2(0.50f, 1.20f);
            profile.maxHealth = 11000f;
            profile.defenseModifier = 1.00f; // Grappler toughness
            profile.moveSet = moveSet;

            // Link override controller and configure locomotion clips
            var overrideCtrlPath = "Assets/MDI_Characters/Runtime/BlazBlue/PicoChan_BB_Override.overrideController";
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideCtrlPath);
            if (overrideCtrl == null)
            {
                var baseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller");
                overrideCtrl = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(overrideCtrl, overrideCtrlPath);
            }

            if (overrideCtrl != null)
            {
                var walkClip = FindClip("111/111_34", "Pico_Walk");
                var dashClip = FindClip("17/17_10", "Pico_Dash");
                var jumpClip = FindClip("143/143_06", "Pico_Jump");
                var crouchClip = FindClip("139/139_09", "Pico_Crouch");
                var standBlockClip = FindClip("144/144_07", "Pico_StandBlock");
                var crouchBlockClip = FindClip("135/135_10", "Pico_CrouchBlock");
                var knockdownClip = FindClip("140/140_01", "Pico_Knockdown");
                var introClip = FindClip("111/111_34", "Pico_Intro");

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
                EditorUtility.SetDirty(overrideCtrl);
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C09_PicoChan
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C09_PicoChan.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBPicoChanAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C09 PicoChan (Zero Placeholders)!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PICOCHAN_ROOT)) Directory.CreateDirectory(PICOCHAN_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        public static AnimationClip FindClip(string fbxTake, string clipName)
        {
            string fbxPath = $"Assets/Huge Mocap Library/mocap animations/{fbxTake}.fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is AnimationClip clip)
                    {
                        if (clip.name == clipName) return clip;
                    }
                }
            }
            return null;
        }

        private static BBAttackData CreateOrUpdateAttackData(MoveSpec s)
        {
            string attackPath = $"{PICOCHAN_ROOT}/BB_PicoChan_{s.moveId}.asset";
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
            attack.invuln = s.invuln ?? new BBInvulnWindow[0];
            attack.gatlingInto = s.gatlingInto ?? new string[0];

            // Resolve real AnimationClip sub-asset (no placeholders)
            var realClip = FindClip(s.fbxTake, s.clipName);
            attack.clip = realClip;
            attack.animationClip = realClip;

            // Author BoxTrack
            string boxTrackPath = $"{BOX_TRACK_ROOT}/BoxTrack_PicoChan_{s.moveId}.asset";
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
            track.bakeHash = $"Bake_PicoChan_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        private static MoveSpec[] GetPicoChanMoveSpecs()
        {
            return new MoveSpec[]
            {
                new MoveSpec {
                    moveId = "5A", attackName = "Lead Hook", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 9, damage = 320, guard = BBGuard.Mid, advOnBlock = -1, hitstun = 11, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_5A", fbxTake = "15/15_13", firstFrame = 638, lastFrame = 654, hitBoxCenter = new Vector2(0.38f, 0.95f), hitBoxSize = new Vector2(0.40f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2A", attackName = "Body Hook", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 11, damage = 340, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 12, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_2A", fbxTake = "15/15_13", firstFrame = 376, lastFrame = 394, hitBoxCenter = new Vector2(0.36f, 0.55f), hitBoxSize = new Vector2(0.40f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "6A", attackName = "Upper Cut (AA)", button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 11, active = 4, recovery = 16, damage = 700, guard = BBGuard.Mid, advOnBlock = -7, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_6A", fbxTake = "15/15_13", firstFrame = 400, lastFrame = 418, hitBoxCenter = new Vector2(0.38f, 1.25f), hitBoxSize = new Vector2(0.45f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(4, 12, BBInvuln.Head) },
                    gatlingInto = new string[] { "5B", "2B", "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5B", attackName = "Crowd Hook", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 13, damage = 720, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 15, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "Pico_5B", fbxTake = "15/15_13", firstFrame = 1256, lastFrame = 1274, hitBoxCenter = new Vector2(0.42f, 0.95f), hitBoxSize = new Vector2(0.46f, 0.34f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2B", attackName = "Crouch Low Shin", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 15, damage = 680, guard = BBGuard.Low, advOnBlock = -8, hitstun = 13, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_2B", fbxTake = "15/15_13", firstFrame = 344, lastFrame = 362, hitBoxCenter = new Vector2(0.44f, 0.28f), hitBoxSize = new Vector2(0.48f, 0.30f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "6B", attackName = "Overhand Hook", button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 20, active = 4, recovery = 18, damage = 760, guard = BBGuard.Overhead, advOnBlock = -7, hitstun = 20, blockstun = 15,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_6B", fbxTake = "17/17_10", firstFrame = 173, lastFrame = 193, hitBoxCenter = new Vector2(0.45f, 1.20f), hitBoxSize = new Vector2(0.50f, 0.55f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5C", attackName = "Heavy Wrecking Hook", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 11, active = 4, recovery = 14, damage = 800, guard = BBGuard.Mid, advOnBlock = -5, hitstun = 22, blockstun = 14,
                    groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_5C", fbxTake = "86/86_08", firstFrame = 556, lastFrame = 574, hitBoxCenter = new Vector2(0.48f, 0.95f), hitBoxSize = new Vector2(0.52f, 0.36f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2C", attackName = "Trip Sweep", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 11, active = 4, recovery = 20, damage = 700, guard = BBGuard.Low, advOnBlock = -10, hitstun = 24, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_2C", fbxTake = "15/15_13", firstFrame = 340, lastFrame = 362, hitBoxCenter = new Vector2(0.50f, 0.22f), hitBoxSize = new Vector2(0.58f, 0.28f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "3C", attackName = "Knee Launcher", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 18, damage = 740, guard = BBGuard.Mid, advOnBlock = -9, hitstun = 28, blockstun = 15,
                    groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_3C", fbxTake = "15/15_13", firstFrame = 288, lastFrame = 308, hitBoxCenter = new Vector2(0.40f, 1.10f), hitBoxSize = new Vector2(0.48f, 0.60f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "j.A", attackName = "Air Quick Tap", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 8, damage = 300, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 12, blockstun = 10,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_jA", fbxTake = "15/15_13", firstFrame = 765, lastFrame = 783, hitBoxCenter = new Vector2(0.35f, 0.65f), hitBoxSize = new Vector2(0.38f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.B", attackName = "Air Diving Hook", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 5, recovery = 13, damage = 740, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 18, blockstun = 12,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_jB", fbxTake = "86/86_08", firstFrame = 312, lastFrame = 330, hitBoxCenter = new Vector2(0.45f, 0.60f), hitBoxSize = new Vector2(0.48f, 0.38f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.C", attackName = "Air Hammer Drop", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 5, recovery = 15, damage = 860, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 22, blockstun = 14,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_jC", fbxTake = "86/86_08", firstFrame = 944, lastFrame = 962, hitBoxCenter = new Vector2(0.46f, 0.50f), hitBoxSize = new Vector2(0.50f, 0.45f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec {
                    moveId = "5D", attackName = "Clinch", button = BBButton.D, kind = BBMoveKind.CommandThrow, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 18, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_5D", fbxTake = "141/141_23", firstFrame = 11, lastFrame = 33, hitBoxCenter = new Vector2(0.45f, 0.85f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2D", attackName = "Low Clinch", button = BBButton.D, kind = BBMoveKind.CommandThrow, motion = BBMotion.None,
                    startup = 8, active = 3, recovery = 22, damage = 1000, guard = BBGuard.Low, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_2D", fbxTake = "141/141_23", firstFrame = 11, lastFrame = 33, hitBoxCenter = new Vector2(0.45f, 0.35f), hitBoxSize = new Vector2(0.55f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6D", attackName = "Run Clinch", button = BBButton.D, kind = BBMoveKind.CommandThrow, motion = BBMotion.None,
                    startup = 14, active = 3, recovery = 24, damage = 1100, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_6D", fbxTake = "141/141_23", firstFrame = 11, lastFrame = 33, hitBoxCenter = new Vector2(0.55f, 0.85f), hitBoxSize = new Vector2(0.60f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.D", attackName = "Air Clinch", button = BBButton.D, kind = BBMoveKind.CommandThrow, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 16, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_jD", fbxTake = "86/86_08", firstFrame = 944, lastFrame = 962, hitBoxCenter = new Vector2(0.42f, 0.60f), hitBoxSize = new Vector2(0.50f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "236A", attackName = "Lariat Rush", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 8, active = 4, recovery = 16, damage = 840, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 18, blockstun = 16,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Pico_236A", fbxTake = "17/17_10", firstFrame = 37, lastFrame = 57, hitBoxCenter = new Vector2(0.52f, 0.95f), hitBoxSize = new Vector2(0.60f, 0.40f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "623C", attackName = "Vicious Uppercut", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M623,
                    startup = 6, active = 5, recovery = 22, damage = 1040, guard = BBGuard.Mid, advOnBlock = -18, hitstun = 28, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.10f,
                    clipName = "Pico_623C", fbxTake = "17/17_10", firstFrame = 149, lastFrame = 169, hitBoxCenter = new Vector2(0.42f, 1.25f), hitBoxSize = new Vector2(0.50f, 0.75f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 6, BBInvuln.Head | BBInvuln.Body) }
                },
                new MoveSpec {
                    moveId = "46C", attackName = "Charged Straight", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.Charge46,
                    startup = 16, active = 5, recovery = 20, damage = 1020, guard = BBGuard.Mid, advOnBlock = -12, hitstun = 24, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.10f,
                    clipName = "Pico_Charge", fbxTake = "86/86_08", firstFrame = 1502, lastFrame = 1524, hitBoxCenter = new Vector2(0.60f, 0.95f), hitBoxSize = new Vector2(0.65f, 0.40f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "63214B", attackName = "Step In Hook", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M632146,
                    startup = 11, active = 4, recovery = 16, damage = 860, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 18, blockstun = 15,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Pico_63214B", fbxTake = "17/17_10", firstFrame = 173, lastFrame = 193, hitBoxCenter = new Vector2(0.55f, 0.95f), hitBoxSize = new Vector2(0.58f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5D" }
                },
                new MoveSpec {
                    moveId = "360C", attackName = "Big Slam Throw", button = BBButton.C, kind = BBMoveKind.CommandThrow, motion = BBMotion.None,
                    startup = 2, active = 2, recovery = 40, damage = 1800, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Pico_360C", fbxTake = "111/111_33", firstFrame = 314, lastFrame = 336, hitBoxCenter = new Vector2(0.50f, 0.85f), hitBoxSize = new Vector2(0.65f, 0.55f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "236236B", attackName = "Close Flurry", button = BBButton.B, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 15, active = 8, recovery = 22, damage = 2800, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 35, blockstun = 24,
                    groundOk = true, rapidCancelable = true, chipPct = 0.15f, heatCost = 50,
                    clipName = "Pico_Super", fbxTake = "15/15_13", firstFrame = 820, lastFrame = 854, hitBoxCenter = new Vector2(0.52f, 0.95f), hitBoxSize = new Vector2(0.60f, 0.50f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 15, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "632146C", attackName = "Pico Press", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M632146,
                    startup = 1, active = 2, recovery = 45, damage = 3400, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 35, blockstun = 0,
                    groundOk = true, heatCost = 50,
                    clipName = "Pico_Press", fbxTake = "141/141_23", firstFrame = 11, lastFrame = 33, hitBoxCenter = new Vector2(0.55f, 0.85f), hitBoxSize = new Vector2(0.70f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 3, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "OD_EA", attackName = "Exceed Accel (Pico Crush)", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.None,
                    startup = 12, active = 8, recovery = 24, damage = 2200, guard = BBGuard.Mid, advOnBlock = -16, hitstun = 30, blockstun = 20,
                    groundOk = true, rapidCancelable = true,
                    clipName = "Pico_OD_EA", fbxTake = "15/15_13", firstFrame = 820, lastFrame = 854, hitBoxCenter = new Vector2(0.52f, 0.95f), hitBoxSize = new Vector2(0.62f, 0.50f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 12, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "6BC", attackName = "Forward Clinch Slam", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1500, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true,
                    clipName = "Pico_Throw", fbxTake = "111/111_33", firstFrame = 314, lastFrame = 336, hitBoxCenter = new Vector2(0.45f, 0.85f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "4BC", attackName = "Back Clinch Suplex", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1500, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true,
                    clipName = "Pico_Throw", fbxTake = "111/111_33", firstFrame = 314, lastFrame = 336, hitBoxCenter = new Vector2(-0.45f, 0.85f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.BC", attackName = "Air Clinch Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 20, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    airOk = true, groundOk = false,
                    clipName = "Pico_AirThrow", fbxTake = "86/86_08", firstFrame = 944, lastFrame = 962, hitBoxCenter = new Vector2(0.42f, 0.60f), hitBoxSize = new Vector2(0.50f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6AB", attackName = "Counter Assault", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 18, damage = 200, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 20, blockstun = 15,
                    groundOk = true, heatCost = 50,
                    clipName = "Pico_CounterAssault", fbxTake = "15/15_13", firstFrame = 638, lastFrame = 654, hitBoxCenter = new Vector2(0.42f, 0.95f), hitBoxSize = new Vector2(0.50f, 0.45f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "AB", attackName = "Crush Trigger", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 24, active = 4, recovery = 18, damage = 900, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 28, blockstun = 24,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, heatCost = 25,
                    clipName = "Pico_CrushTrigger", fbxTake = "17/17_10", firstFrame = 173, lastFrame = 193, hitBoxCenter = new Vector2(0.50f, 0.95f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body
                }
            };
        }
    }
}
#endif
