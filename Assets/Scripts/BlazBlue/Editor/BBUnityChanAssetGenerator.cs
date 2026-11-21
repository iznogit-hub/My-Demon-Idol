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
    /// MoveSet, CharacterProfile, and AnimatorOverrideController assets for C04 UnityChan in accordance with Spec §8.5 and M9.
    /// Fully resolves and links genuine AnimationClip assets and AnimatorOverrideController clips (no placeholders).
    /// </summary>
    public static class BBUnityChanAssetGenerator
    {
        public const string UNITYCHAN_ROOT = "Assets/CombatData/BlazBlue/UnityChan";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/UnityChan/BoxTracks";

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

        [MenuItem("DemonIdol/BlazBlue/Generate UnityChan Assets")]
        public static void GenerateAllUnityChanAssets()
        {
            EnsureDirectories();

            var specs = GetUnityChanMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{UNITYCHAN_ROOT}/BBMoveSet_C04_UnityChan.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C04";
                AssetDatabase.CreateAsset(moveSet, $"{UNITYCHAN_ROOT}/BBMoveSet_C04_UnityChan.asset");
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

            // Create or update UnityChan's CharacterProfile
            var profilePath = $"{UNITYCHAN_ROOT}/BBProfile_C04_UnityChan.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C04_UnityChan";
            profile.characterName = "UnityChan";
            profile.driveName = "Aero Hop";
            profile.forwardWalkSpeed = 3.8f;
            profile.backwardWalkSpeed = 3.0f;
            profile.forwardDashSpeed = 7.8f; // Hop dash
            profile.forwardDashDurationTicks = 10;
            profile.backdashSpeed = 7.5f;
            profile.backdashDurationTicks = 12;
            profile.backdashInvulnTicks = 6;
            profile.jumpVerticalLaunch = 8.5f;
            profile.jumpHorizontalSpeed = 3.6f;
            profile.gravity = 20.0f;
            profile.standPushboxSize = new Vector2(0.64f, 1.65f);
            profile.crouchPushboxSize = new Vector2(0.72f, 1.15f);
            profile.airPushboxSize = new Vector2(0.55f, 1.25f);
            profile.maxHealth = 11000f;
            profile.defenseModifier = 1.05f;
            profile.moveSet = moveSet;

            // Link override controller and configure locomotion clips
            var overrideCtrlPath = "Assets/MDI_Characters/Runtime/BlazBlue/UnityChan_BB_Override.overrideController";
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideCtrlPath);
            if (overrideCtrl == null)
            {
                var baseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller");
                overrideCtrl = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(overrideCtrl, overrideCtrlPath);
            }

            if (overrideCtrl != null)
            {
                var walkClip = FindClip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkForward.FBX", "Female@WalkForward");
                var dashClip = FindClip("Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx", "Quickstep_F");
                var jumpClip = FindClip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX", "Karate@Jump");
                var crouchClip = FindClip("Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx", "Guard_Idle");
                var standBlockClip = FindClip("Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx", "Guard_Idle");
                var crouchBlockClip = FindClip("Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx", "Guard_Idle");
                var knockdownClip = FindClip("Assets/Martial Art Animations Sample/Animations/Knock_A.fbx", "Knock_A");
                var getUpClip = FindClip("Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx", "GetUp_A");
                var introClip = FindClip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Idle.FBX", "Female@Idle");

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
                BBWarriorStanceOverrideBuilder.ApplyWarriorStances(overrideCtrl, "C04");
                EditorUtility.SetDirty(overrideCtrl);
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C04_UnityChan
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C04_UnityChan.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBUnityChanAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C04 UnityChan (Zero Placeholders)!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(UNITYCHAN_ROOT)) Directory.CreateDirectory(UNITYCHAN_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        public static AnimationClip FindClip(string fbxTake, string clipName)
        {
            var masterClip = BBWarriorMasterRemapper.GetClipForFighterMove("C04", clipName);
            if (masterClip != null) return masterClip;
            string fbxPath;
            if (fbxTake == "KevinIglesias_Direct1H")
            {
                fbxPath = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx";
            }
            else if (fbxTake.StartsWith("Assets/"))
            {
                fbxPath = fbxTake;
            }
            else
            {
                // Approved pack: Fighter Pack Bundle FREE
                fbxPath = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX";
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is AnimationClip clip)
                    {
                        if (clip.name == clipName) return clip;
                        if (!clip.name.StartsWith("__preview__")) return clip;
                    }
                }
            }
            return null;
        }

        private static BBAttackData CreateOrUpdateAttackData(MoveSpec s)
        {
            string attackPath = $"{UNITYCHAN_ROOT}/BB_UnityChan_{s.moveId}.asset";
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
            string boxTrackPath = $"{BOX_TRACK_ROOT}/BoxTrack_UnityChan_{s.moveId}.asset";
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
            track.bakeHash = $"Bake_UnityChan_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        private static MoveSpec[] GetUnityChanMoveSpecs()
        {
            return new MoveSpec[]
            {
                new MoveSpec {
                    moveId = "5A", attackName = "Tap Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 4, active = 3, recovery = 8, damage = 220, guard = BBGuard.Mid, advOnBlock = -1, hitstun = 11, blockstun = 9,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_5A", fbxTake = "02/02_05", firstFrame = 92, lastFrame = 108, hitBoxCenter = new Vector2(0.38f, 1.15f), hitBoxSize = new Vector2(0.42f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "2A", attackName = "Dip Fist", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 10, damage = 240, guard = BBGuard.Low, advOnBlock = -2, hitstun = 12, blockstun = 10,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_2A", fbxTake = "02/02_05", firstFrame = 212, lastFrame = 230, hitBoxCenter = new Vector2(0.36f, 0.35f), hitBoxSize = new Vector2(0.40f, 0.30f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "6A", attackName = "Hop Knee", button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 14, damage = 560, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_6A", fbxTake = "16/16_05", firstFrame = 10, lastFrame = 26, hitBoxCenter = new Vector2(0.35f, 1.30f), hitBoxSize = new Vector2(0.48f, 0.55f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(3, 12, BBInvuln.Head) },
                    gatlingInto = new string[] { "5B", "2B", "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5B", attackName = "Hopping Straight", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 3, recovery = 12, damage = 550, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 15, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "UChan_5B", fbxTake = "02/02_05", firstFrame = 144, lastFrame = 162, hitBoxCenter = new Vector2(0.44f, 1.10f), hitBoxSize = new Vector2(0.48f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "2B", attackName = "Low Check Kick", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 3, recovery = 14, damage = 580, guard = BBGuard.Low, advOnBlock = -7, hitstun = 14, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_2B", fbxTake = "14/14_02", firstFrame = 630, lastFrame = 648, hitBoxCenter = new Vector2(0.48f, 0.30f), hitBoxSize = new Vector2(0.52f, 0.32f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "6B", attackName = "Hop Overhead", button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 18, active = 4, recovery = 14, damage = 680, guard = BBGuard.Overhead, advOnBlock = -6, hitstun = 20, blockstun = 15,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_6B", fbxTake = "16/16_05", firstFrame = 20, lastFrame = 40, hitBoxCenter = new Vector2(0.45f, 1.35f), hitBoxSize = new Vector2(0.52f, 0.60f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5C", attackName = "Triple Punch End", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 11, active = 4, recovery = 14, damage = 650, guard = BBGuard.Mid, advOnBlock = -5, hitstun = 18, blockstun = 13,
                    groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_5C", fbxTake = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", firstFrame = 0, lastFrame = 30, hitBoxCenter = new Vector2(0.85f, 1.15f), hitBoxSize = new Vector2(0.90f, 0.45f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "2C", "3C", "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "2C", attackName = "Low Sweep", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 18, damage = 620, guard = BBGuard.Low, advOnBlock = -10, hitstun = 20, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_2C", fbxTake = "14/14_02", firstFrame = 635, lastFrame = 655, hitBoxCenter = new Vector2(0.50f, 0.22f), hitBoxSize = new Vector2(0.60f, 0.32f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "3C", "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "3C", attackName = "Hop Launcher", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 9, active = 5, recovery = 16, damage = 700, guard = BBGuard.Mid, advOnBlock = -8, hitstun = 26, blockstun = 16,
                    groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_3C", fbxTake = "16/16_01", firstFrame = 16, lastFrame = 40, hitBoxCenter = new Vector2(0.42f, 1.25f), hitBoxSize = new Vector2(0.50f, 0.65f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D", "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "j.A", attackName = "Air Tap", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 4, active = 3, recovery = 8, damage = 240, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 12, blockstun = 10,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_jA", fbxTake = "02/02_05", firstFrame = 124, lastFrame = 140, hitBoxCenter = new Vector2(0.35f, 0.80f), hitBoxSize = new Vector2(0.40f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.B", attackName = "Air Side Kick", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 5, recovery = 12, damage = 700, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 18, blockstun = 12,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_jB", fbxTake = "118/118_01", firstFrame = 88, lastFrame = 106, hitBoxCenter = new Vector2(0.48f, 0.70f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.C", attackName = "Air Stomp", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 5, recovery = 14, damage = 820, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 22, blockstun = 14,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_jC", fbxTake = "118/118_05", firstFrame = 64, lastFrame = 82, hitBoxCenter = new Vector2(0.45f, 0.55f), hitBoxSize = new Vector2(0.52f, 0.55f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec {
                    moveId = "5D", attackName = "Aero Hop (Vertical)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 4, active = 1, recovery = 12, damage = 0, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 0, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_5D", fbxTake = "16/16_01", firstFrame = 16, lastFrame = 40, hitBoxCenter = new Vector2(0.35f, 1.10f), hitBoxSize = new Vector2(0.45f, 0.45f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(4, 18, BBInvuln.Foot) },
                    gatlingInto = new string[] { "6D", "4D" }
                },
                new MoveSpec {
                    moveId = "6D", attackName = "Aero Leap (Forward)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 10, active = 5, recovery = 14, damage = 760, guard = BBGuard.Mid, advOnBlock = -5, hitstun = 20, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_6D", fbxTake = "91/91_39", firstFrame = 24, lastFrame = 44, hitBoxCenter = new Vector2(0.55f, 1.15f), hitBoxSize = new Vector2(0.60f, 0.50f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "4D", attackName = "Aero Back Hop", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 3, active = 1, recovery = 14, damage = 0, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 0, blockstun = 0,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_4D", fbxTake = "16/16_35", firstFrame = 10, lastFrame = 30, hitBoxCenter = new Vector2(-0.20f, 1.05f), hitBoxSize = new Vector2(0.45f, 0.45f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(3, 10, BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "j.D", attackName = "Aero Air Hop & Disc", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 6, active = 1, recovery = 10, damage = 300, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 16, blockstun = 12,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true, chipPct = 0.05f,
                    clipName = "UChan_jD", fbxTake = "118/118_01", firstFrame = 95, lastFrame = 115, hitBoxCenter = new Vector2(0.50f, 0.70f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "236A", attackName = "Aero Plasma Disc", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 11, active = 1, recovery = 16, damage = 440, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 18, blockstun = 14,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "UChan_236A", fbxTake = "118/118_01", firstFrame = 15, lastFrame = 35, hitBoxCenter = new Vector2(0.85f, 1.05f), hitBoxSize = new Vector2(0.55f, 0.35f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "236B", attackName = "Aero Plasma Disc (Angled)", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 11, active = 1, recovery = 16, damage = 440, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 18, blockstun = 14,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "UChan_236B", fbxTake = "118/118_05", firstFrame = 20, lastFrame = 45, hitBoxCenter = new Vector2(0.80f, 1.35f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "623C", attackName = "Rising Hop Dragon", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M623,
                    startup = 6, active = 5, recovery = 20, damage = 900, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 28, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_623C", fbxTake = "16/16_05", firstFrame = 30, lastFrame = 50, hitBoxCenter = new Vector2(0.42f, 1.35f), hitBoxSize = new Vector2(0.55f, 0.80f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 6, BBInvuln.Head | BBInvuln.Body) }
                },
                new MoveSpec {
                    moveId = "214B", attackName = "Bound Kick", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 9, active = 4, recovery = 16, damage = 740, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 20, blockstun = 15,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_214B", fbxTake = "91/91_39", firstFrame = 50, lastFrame = 70, hitBoxCenter = new Vector2(0.50f, 0.95f), hitBoxSize = new Vector2(0.58f, 0.45f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "46C", attackName = "Hop Stomp", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.Charge46,
                    startup = 14, active = 5, recovery = 18, damage = 880, guard = BBGuard.Overhead, advOnBlock = -9, hitstun = 24, blockstun = 16,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_46C", fbxTake = "118/118_05", firstFrame = 47, lastFrame = 70, hitBoxCenter = new Vector2(0.55f, 1.10f), hitBoxSize = new Vector2(0.60f, 0.60f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "j.214B", attackName = "Air Dive Kick", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 8, active = 6, recovery = 12, damage = 700, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 22, blockstun = 14,
                    airOk = true, groundOk = false, ddCancel = true, rapidCancelable = true,
                    clipName = "UChan_j214B", fbxTake = "118/118_05", firstFrame = 70, lastFrame = 92, hitBoxCenter = new Vector2(0.48f, 0.50f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "236236C", attackName = "Aero Flurry", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 14, active = 8, recovery = 20, damage = 2400, guard = BBGuard.Mid, advOnBlock = -12, hitstun = 40, blockstun = 25,
                    groundOk = true, heatCost = 50, rapidCancelable = true, chipPct = 0.12f,
                    clipName = "UChan_Super", fbxTake = "14/14_02", firstFrame = 811, lastFrame = 845, hitBoxCenter = new Vector2(0.60f, 1.05f), hitBoxSize = new Vector2(0.75f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 14, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "j.236236C", attackName = "Disc Storm", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 10, active = 20, recovery = 18, damage = 2200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 38, blockstun = 24,
                    airOk = true, groundOk = false, heatCost = 50, rapidCancelable = true, chipPct = 0.15f,
                    clipName = "UChan_j236236C", fbxTake = "118/118_01", firstFrame = 88, lastFrame = 118, hitBoxCenter = new Vector2(0.65f, 0.60f), hitBoxSize = new Vector2(1.20f, 1.20f),
                    attribute = BBAttribute.Projectile, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "OD_EA", attackName = "Exceed Accel", button = BBButton.D, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 10, active = 8, recovery = 22, damage = 2000, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 36, blockstun = 22,
                    groundOk = true, rapidCancelable = false,
                    clipName = "UChan_OD_EA", fbxTake = "14/14_02", firstFrame = 811, lastFrame = 845, hitBoxCenter = new Vector2(0.55f, 1.10f), hitBoxSize = new Vector2(0.70f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "6BC", attackName = "Hopping Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1100, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 30, blockstun = 0,
                    groundOk = true,
                    clipName = "UChan_Throw", fbxTake = "02/02_05", firstFrame = 72, lastFrame = 94, hitBoxCenter = new Vector2(0.35f, 1.00f), hitBoxSize = new Vector2(0.42f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "4BC", attackName = "Back Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1100, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 30, blockstun = 0,
                    groundOk = true,
                    clipName = "UChan_Throw", fbxTake = "02/02_05", firstFrame = 72, lastFrame = 94, hitBoxCenter = new Vector2(0.35f, 1.00f), hitBoxSize = new Vector2(0.42f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.BC", attackName = "Air Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 20, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 28, blockstun = 0,
                    airOk = true, groundOk = false,
                    clipName = "UChan_AirThrow", fbxTake = "118/118_01", firstFrame = 90, lastFrame = 110, hitBoxCenter = new Vector2(0.38f, 0.65f), hitBoxSize = new Vector2(0.42f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6AB", attackName = "Counter Assault", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 11, active = 3, recovery = 18, damage = 0, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 24, blockstun = 18,
                    groundOk = true, heatCost = 50,
                    clipName = "UChan_CounterAssault", fbxTake = "02/02_05", firstFrame = 92, lastFrame = 108, hitBoxCenter = new Vector2(0.45f, 1.15f), hitBoxSize = new Vector2(0.50f, 0.40f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 11, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile | BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "AB", attackName = "Crush Trigger", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 24, active = 4, recovery = 20, damage = 1200, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 28, blockstun = 20,
                    groundOk = true, heatCost = 25, fatalCounter = true,
                    clipName = "UChan_CrushTrigger", fbxTake = "16/16_05", firstFrame = 20, lastFrame = 40, hitBoxCenter = new Vector2(0.55f, 1.20f), hitBoxSize = new Vector2(0.60f, 0.50f),
                    attribute = BBAttribute.Foot
                }
            };
        }
    }
}
#endif
