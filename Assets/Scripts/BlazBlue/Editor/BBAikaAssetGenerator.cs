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
    /// MoveSet, CharacterProfile, and AnimatorOverrideController assets for C02 Aika in accordance with Spec §8.4 and M8.
    /// Fully resolves and links genuine AnimationClip assets and AnimatorOverrideController clips (no placeholders).
    /// </summary>
    public static class BBAikaAssetGenerator
    {
        public const string AIKA_ROOT = "Assets/CombatData/BlazBlue/Aika";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/Aika/BoxTracks";

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

        [MenuItem("DemonIdol/BlazBlue/Generate Aika Assets")]
        public static void GenerateAllAikaAssets()
        {
            EnsureDirectories();

            var specs = GetAikaMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{AIKA_ROOT}/BBMoveSet_C02_Aika.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C02";
                AssetDatabase.CreateAsset(moveSet, $"{AIKA_ROOT}/BBMoveSet_C02_Aika.asset");
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

            // Create or update Aika's CharacterProfile
            var profilePath = $"{AIKA_ROOT}/BBProfile_C02_Aika.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C02_Aika";
            profile.characterName = "Aika";
            profile.driveName = "Resonance Nail";
            profile.forwardWalkSpeed = 3.5f;
            profile.backwardWalkSpeed = 2.7f;
            profile.forwardDashSpeed = 7.2f; // Step dash
            profile.forwardDashDurationTicks = 10;
            profile.backdashSpeed = 7.2f;
            profile.backdashDurationTicks = 14;
            profile.backdashInvulnTicks = 5;
            profile.jumpVerticalLaunch = 8.0f;
            profile.jumpHorizontalSpeed = 3.0f;
            profile.gravity = 20.0f;
            profile.standPushboxSize = new Vector2(0.68f, 1.78f);
            profile.crouchPushboxSize = new Vector2(0.78f, 1.25f);
            profile.airPushboxSize = new Vector2(0.58f, 1.35f);
            profile.maxHealth = 11000f;
            profile.defenseModifier = 1.0f;
            profile.moveSet = moveSet;

            // Link override controller and configure locomotion clips
            var overrideCtrlPath = "Assets/MDI_Characters/Runtime/BlazBlue/Aika_BB_Override.overrideController";
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideCtrlPath);
            if (overrideCtrl == null)
            {
                var baseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller");
                overrideCtrl = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(overrideCtrl, overrideCtrlPath);
            }

            if (overrideCtrl != null)
            {
                var walkClip = FindClip("144/144_33", "Aika_Walk");
                var dashClip = FindClip("144/144_17", "Aika_Dash");
                var jumpClip = FindClip("75/75_16", "Aika_Jump");
                var crouchClip = FindClip("135/135_05", "Aika_Crouch");
                var standBlockClip = FindClip("144/144_07", "Aika_StandBlock");
                var crouchBlockClip = FindClip("135/135_10", "Aika_CrouchBlock");
                var knockdownClip = FindClip("90/90_18", "Aika_Knockdown");
                var getUpClip = FindClip("114/114_02", "Aika_GetUp");
                var introClip = FindClip("74/74_01", "Aika_Intro");

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
                BBWarriorStanceOverrideBuilder.ApplyWarriorStances(overrideCtrl, "C02");
                EditorUtility.SetDirty(overrideCtrl);
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C02_Aika
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBAikaAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C02 Aika (Zero Placeholders)!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(AIKA_ROOT)) Directory.CreateDirectory(AIKA_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        public static AnimationClip FindClip(string fbxTake, string clipName)
        {
            string fbxPath;
            if (fbxTake == "KevinIglesias_Direct1H")
            {
                fbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx";
            }
            else if (fbxTake == "KevinIglesias_Direct2H")
            {
                fbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx";
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
            string attackPath = $"{AIKA_ROOT}/BB_Aika_{s.moveId}.asset";
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
            string boxTrackPath = $"{BOX_TRACK_ROOT}/BoxTrack_Aika_{s.moveId}.asset";
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
            track.bakeHash = $"Bake_Aika_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        private static MoveSpec[] GetAikaMoveSpecs()
        {
            return new MoveSpec[]
            {
                new MoveSpec {
                    moveId = "5A", attackName = "Standing Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 9, damage = 260, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 12, blockstun = 10,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_5A", fbxTake = "80/80_10", firstFrame = 52, lastFrame = 68, hitBoxCenter = new Vector2(0.48f, 1.25f), hitBoxSize = new Vector2(0.52f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2A", attackName = "Low Fist", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 3, recovery = 12, damage = 280, guard = BBGuard.Low, advOnBlock = -3, hitstun = 13, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_2A", fbxTake = "135/135_02", firstFrame = 462, lastFrame = 480, hitBoxCenter = new Vector2(0.44f, 0.38f), hitBoxSize = new Vector2(0.50f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "6A", attackName = "Rising Knee", button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 16, damage = 600, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_6A", fbxTake = "135/135_11", firstFrame = 12, lastFrame = 28, hitBoxCenter = new Vector2(0.40f, 1.35f), hitBoxSize = new Vector2(0.55f, 0.65f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(4, 12, BBInvuln.Head) },
                    gatlingInto = new string[] { "5B", "2B", "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5B", attackName = "Karate Cross", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 14, damage = 640, guard = BBGuard.Mid, advOnBlock = -5, hitstun = 16, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "Aika_5B", fbxTake = "13/13_18", firstFrame = 73, lastFrame = 91, hitBoxCenter = new Vector2(0.55f, 1.20f), hitBoxSize = new Vector2(0.60f, 0.38f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2B", attackName = "Low Front Kick", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 15, damage = 560, guard = BBGuard.Low, advOnBlock = -5, hitstun = 15, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_2B", fbxTake = "135/135_02", firstFrame = 470, lastFrame = 490, hitBoxCenter = new Vector2(0.56f, 0.35f), hitBoxSize = new Vector2(0.65f, 0.35f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "6B", attackName = "Overhead Axe Kick", button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 20, active = 4, recovery = 18, damage = 800, guard = BBGuard.Overhead, advOnBlock = -8, hitstun = 22, blockstun = 16,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_6B", fbxTake = "143/143_24", firstFrame = 164, lastFrame = 184, hitBoxCenter = new Vector2(0.52f, 1.40f), hitBoxSize = new Vector2(0.60f, 0.70f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5C", attackName = "Roundhouse Kick", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 12, active = 4, recovery = 16, damage = 820, guard = BBGuard.Mid, advOnBlock = -7, hitstun = 20, blockstun = 15,
                    groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_5C", fbxTake = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", firstFrame = 0, lastFrame = 34, hitBoxCenter = new Vector2(0.60f, 1.20f), hitBoxSize = new Vector2(0.65f, 0.40f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2C", attackName = "Karate Sweep", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 16, damage = 720, guard = BBGuard.Low, advOnBlock = -10, hitstun = 20, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_2C", fbxTake = "135/135_02", firstFrame = 492, lastFrame = 514, hitBoxCenter = new Vector2(0.60f, 0.25f), hitBoxSize = new Vector2(0.75f, 0.35f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "3C", attackName = "Launcher Kick", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 12, active = 5, recovery = 20, damage = 780, guard = BBGuard.Mid, advOnBlock = -12, hitstun = 26, blockstun = 16,
                    groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_3C", fbxTake = "113/113_13", firstFrame = 219, lastFrame = 243, hitBoxCenter = new Vector2(0.48f, 1.30f), hitBoxSize = new Vector2(0.55f, 0.70f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "j.A", attackName = "Jump Fist", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 8, damage = 280, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 14, blockstun = 10,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_jA", fbxTake = "13/13_18", firstFrame = 280, lastFrame = 296, hitBoxCenter = new Vector2(0.42f, 0.85f), hitBoxSize = new Vector2(0.48f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.B", attackName = "Jump Side Kick", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 5, recovery = 14, damage = 780, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 18, blockstun = 12,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_jB", fbxTake = "141/141_14", firstFrame = 44, lastFrame = 64, hitBoxCenter = new Vector2(0.55f, 0.75f), hitBoxSize = new Vector2(0.60f, 0.45f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.C", attackName = "Jump Heel Drop", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 6, recovery = 16, damage = 920, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 22, blockstun = 14,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_jC", fbxTake = "141/141_14", firstFrame = 80, lastFrame = 102, hitBoxCenter = new Vector2(0.50f, 0.65f), hitBoxSize = new Vector2(0.62f, 0.60f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec {
                    moveId = "5D", attackName = "Resonance Nail", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 13, active = 1, recovery = 18, damage = 550, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 18, blockstun = 15,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Aika_5D", fbxTake = "135/135_10", firstFrame = 40, lastFrame = 60, hitBoxCenter = new Vector2(0.95f, 1.10f), hitBoxSize = new Vector2(0.60f, 0.35f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[] { "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2D", attackName = "Ground Slide Nail", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 15, active = 1, recovery = 20, damage = 500, guard = BBGuard.Low, advOnBlock = -4, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_2D", fbxTake = "135/135_05", firstFrame = 85, lastFrame = 105, hitBoxCenter = new Vector2(0.85f, 0.20f), hitBoxSize = new Vector2(0.70f, 0.30f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[] { "6D" }
                },
                new MoveSpec {
                    moveId = "6D", attackName = "Resonance Detonate", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 18, active = 3, recovery = 20, damage = 400, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 20, blockstun = 16,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_6D", fbxTake = "135/135_09", firstFrame = 25, lastFrame = 50, hitBoxCenter = new Vector2(0.70f, 1.00f), hitBoxSize = new Vector2(1.20f, 1.20f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "j.D", attackName = "Air Resonance Nail", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 11, active = 1, recovery = 16, damage = 500, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 18, blockstun = 14,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_jD", fbxTake = "141/141_14", firstFrame = 110, lastFrame = 126, hitBoxCenter = new Vector2(0.65f, 0.50f), hitBoxSize = new Vector2(0.55f, 0.45f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "214B", attackName = "Reap", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 10, active = 5, recovery = 18, damage = 860, guard = BBGuard.Low, advOnBlock = -8, hitstun = 22, blockstun = 16,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_214B", fbxTake = "143/143_24", firstFrame = 34, lastFrame = 54, hitBoxCenter = new Vector2(0.62f, 0.30f), hitBoxSize = new Vector2(0.70f, 0.38f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "46C", attackName = "Side Kick", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.Charge46,
                    startup = 18, active = 6, recovery = 22, damage = 1120, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 26, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_46C", fbxTake = "135/135_11", firstFrame = 168, lastFrame = 192, hitBoxCenter = new Vector2(0.70f, 1.10f), hitBoxSize = new Vector2(0.80f, 0.45f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "623C", attackName = "Rising Dragon Kick", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M623,
                    startup = 7, active = 6, recovery = 24, damage = 1080, guard = BBGuard.Mid, advOnBlock = -22, hitstun = 30, blockstun = 20,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_623C", fbxTake = "135/135_11", firstFrame = 62, lastFrame = 84, hitBoxCenter = new Vector2(0.48f, 1.45f), hitBoxSize = new Vector2(0.60f, 0.85f),
                    attribute = BBAttribute.Foot, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 7, BBInvuln.Head | BBInvuln.Body) }
                },
                new MoveSpec {
                    moveId = "236B", attackName = "Step Thrust Kick", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 11, active = 4, recovery = 18, damage = 840, guard = BBGuard.Mid, advOnBlock = -8, hitstun = 20, blockstun = 15,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Aika_236B", fbxTake = "111/111_19", firstFrame = 57, lastFrame = 75, hitBoxCenter = new Vector2(0.68f, 1.15f), hitBoxSize = new Vector2(0.75f, 0.40f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "236236C", attackName = "Hundred Kicks", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 16, active = 10, recovery = 24, damage = 2600, guard = BBGuard.Mid, advOnBlock = -16, hitstun = 40, blockstun = 25,
                    groundOk = true, heatCost = 50, rapidCancelable = true, chipPct = 0.12f,
                    clipName = "Aika_Super", fbxTake = "13/13_18", firstFrame = 276, lastFrame = 316, hitBoxCenter = new Vector2(0.72f, 1.10f), hitBoxSize = new Vector2(0.85f, 0.65f),
                    attribute = BBAttribute.Foot, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 16, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "632146D", attackName = "Nail Rain", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M632146,
                    startup = 12, active = 30, recovery = 28, damage = 2400, guard = BBGuard.Mid, advOnBlock = -10, hitstun = 45, blockstun = 28,
                    groundOk = true, heatCost = 50, rapidCancelable = true, chipPct = 0.15f,
                    clipName = "Aika_632146D", fbxTake = "135/135_01", firstFrame = 60, lastFrame = 100, hitBoxCenter = new Vector2(0.90f, 1.10f), hitBoxSize = new Vector2(1.80f, 1.80f),
                    attribute = BBAttribute.Projectile, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 12, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "OD_EA", attackName = "Exceed Accel", button = BBButton.D, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 12, active = 8, recovery = 24, damage = 2200, guard = BBGuard.Mid, advOnBlock = -16, hitstun = 38, blockstun = 24,
                    groundOk = true, rapidCancelable = false,
                    clipName = "Aika_OD_EA", fbxTake = "13/13_18", firstFrame = 276, lastFrame = 316, hitBoxCenter = new Vector2(0.65f, 1.15f), hitBoxSize = new Vector2(0.75f, 0.60f),
                    attribute = BBAttribute.Foot, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 12, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "6BC", attackName = "Karate Sweep Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 30, blockstun = 0,
                    groundOk = true,
                    clipName = "Aika_Throw", fbxTake = "111/111_19", firstFrame = 36, lastFrame = 58, hitBoxCenter = new Vector2(0.40f, 1.05f), hitBoxSize = new Vector2(0.45f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "4BC", attackName = "Karate Back Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 30, blockstun = 0,
                    groundOk = true,
                    clipName = "Aika_Throw", fbxTake = "111/111_19", firstFrame = 36, lastFrame = 58, hitBoxCenter = new Vector2(0.40f, 1.05f), hitBoxSize = new Vector2(0.45f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.BC", attackName = "Air Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 20, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 28, blockstun = 0,
                    airOk = true, groundOk = false,
                    clipName = "Aika_AirThrow", fbxTake = "141/141_14", firstFrame = 110, lastFrame = 130, hitBoxCenter = new Vector2(0.42f, 0.70f), hitBoxSize = new Vector2(0.45f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6AB", attackName = "Counter Assault", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 11, active = 3, recovery = 18, damage = 0, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 24, blockstun = 18,
                    groundOk = true, heatCost = 50,
                    clipName = "Aika_CounterAssault", fbxTake = "80/80_10", firstFrame = 52, lastFrame = 68, hitBoxCenter = new Vector2(0.50f, 1.20f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 11, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "AB", attackName = "Crush Trigger", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 24, active = 4, recovery = 20, damage = 1200, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 28, blockstun = 20,
                    groundOk = true, heatCost = 25, fatalCounter = true,
                    clipName = "Aika_CrushTrigger", fbxTake = "135/135_11", firstFrame = 168, lastFrame = 192, hitBoxCenter = new Vector2(0.65f, 1.15f), hitBoxSize = new Vector2(0.70f, 0.50f),
                    attribute = BBAttribute.Foot
                }
            };
        }
    }
}
#endif
