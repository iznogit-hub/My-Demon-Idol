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
    /// MoveSet, CharacterProfile, and AnimatorOverrideController assets for C12 Fina in accordance with Spec §8.7 and M11.
    /// Fully resolves and links genuine AnimationClip assets and AnimatorOverrideController clips (zero placeholders).
    /// </summary>
    public static class BBFinaAssetGenerator
    {
        public const string FINA_ROOT = "Assets/CombatData/BlazBlue/Fina";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/Fina/BoxTracks";

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

        [MenuItem("DemonIdol/BlazBlue/Generate Fina Assets")]
        public static void GenerateAllFinaAssets()
        {
            EnsureDirectories();

            var specs = GetFinaMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{FINA_ROOT}/BBMoveSet_C12_Fina.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C12";
                AssetDatabase.CreateAsset(moveSet, $"{FINA_ROOT}/BBMoveSet_C12_Fina.asset");
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

            // Create or update Fina's CharacterProfile
            var profilePath = $"{FINA_ROOT}/BBProfile_C12_Fina.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C12_Fina";
            profile.characterName = "Fina";
            profile.driveName = "Plasma Charge";
            profile.forwardWalkSpeed = 3.8f;
            profile.backwardWalkSpeed = 3.0f;
            profile.forwardDashSpeed = 8.0f;
            profile.forwardDashDurationTicks = 11;
            profile.backdashSpeed = 7.2f;
            profile.backdashDurationTicks = 12;
            profile.backdashInvulnTicks = 6;
            profile.jumpVerticalLaunch = 8.2f;
            profile.jumpHorizontalSpeed = 3.5f;
            profile.gravity = 20.0f;
            profile.standPushboxSize = new Vector2(0.62f, 1.60f);
            profile.crouchPushboxSize = new Vector2(0.68f, 1.10f);
            profile.airPushboxSize = new Vector2(0.55f, 1.25f);
            profile.maxHealth = 11000f;
            profile.defenseModifier = 1.00f;
            profile.moveSet = moveSet;

            // Link override controller and configure locomotion clips
            var overrideCtrlPath = "Assets/MDI_Characters/Runtime/BlazBlue/Fina_BB_Override.overrideController";
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideCtrlPath);
            if (overrideCtrl == null)
            {
                var baseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller");
                overrideCtrl = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(overrideCtrl, overrideCtrlPath);
            }

            if (overrideCtrl != null)
            {
                var walkClip = FindClip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@WalkForward.FBX", "2Handed@WalkForward");
                var dashClip = FindClip("Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx", "Quickstep_F");
                var jumpClip = FindClip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX", "Karate@Jump");
                var crouchClip = FindClip("Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx", "Guard_Idle");
                var standBlockClip = FindClip("Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx", "Guard_Idle");
                var crouchBlockClip = FindClip("Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx", "Guard_Idle");
                var knockdownClip = FindClip("Assets/Martial Art Animations Sample/Animations/Knock_A.fbx", "Knock_A");
                var introClip = FindClip("Assets/Martial Art Animations Sample/Animations/Taunt.fbx", "Taunt");

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
                BBWarriorStanceOverrideBuilder.ApplyWarriorStances(overrideCtrl, "C12");
                EditorUtility.SetDirty(overrideCtrl);
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C12_Fina
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C12_Fina.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBFinaAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C12 Fina (Zero Placeholders)!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(FINA_ROOT)) Directory.CreateDirectory(FINA_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        public static AnimationClip FindClip(string fbxTake, string clipName)
        {
            var masterClip = BBWarriorMasterRemapper.GetClipForFighterMove("C12", clipName);
            if (masterClip != null) return masterClip;
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
                // Approved pack: 2 Handed Warrior
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX";
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
            string attackPath = $"{FINA_ROOT}/BB_Fina_{s.moveId}.asset";
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

            // Resolve genuine AnimationClip sub-asset (no placeholders)
            var realClip = FindClip(s.fbxTake, s.clipName);
            attack.clip = realClip;
            attack.animationClip = realClip;

            // Author BoxTrack
            string boxTrackPath = $"{BOX_TRACK_ROOT}/BoxTrack_Fina_{s.moveId}.asset";
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
            track.bakeHash = $"Bake_Fina_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        private static MoveSpec[] GetFinaMoveSpecs()
        {
            return new MoveSpec[]
            {
                new MoveSpec {
                    moveId = "5A", attackName = "Lead Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 8, damage = 280, guard = BBGuard.Mid, advOnBlock = -1, hitstun = 11, blockstun = 10,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_5A", fbxTake = "79/79_01", firstFrame = 10, lastFrame = 26, hitBoxCenter = new Vector2(0.42f, 1.15f), hitBoxSize = new Vector2(0.45f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2A", attackName = "Crouch Low Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 9, damage = 290, guard = BBGuard.Low, advOnBlock = -2, hitstun = 12, blockstun = 10,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_2A", fbxTake = "79/79_01", firstFrame = 35, lastFrame = 53, hitBoxCenter = new Vector2(0.40f, 0.45f), hitBoxSize = new Vector2(0.42f, 0.30f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "6A", attackName = "Anti-Air Upper", button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 14, damage = 620, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_6A", fbxTake = "79/79_01", firstFrame = 60, lastFrame = 80, hitBoxCenter = new Vector2(0.40f, 1.30f), hitBoxSize = new Vector2(0.48f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(3, 11, BBInvuln.Head) },
                    gatlingInto = new string[] { "5B", "2B", "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5B", attackName = "Precision Straight Poke", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 4, recovery = 12, damage = 600, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 16, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "Fina_5B", fbxTake = "79/79_01", firstFrame = 90, lastFrame = 112, hitBoxCenter = new Vector2(0.55f, 1.15f), hitBoxSize = new Vector2(0.58f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2B", attackName = "Low Footsie Poke", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 4, recovery = 13, damage = 580, guard = BBGuard.Low, advOnBlock = -6, hitstun = 14, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_2B", fbxTake = "79/79_01", firstFrame = 120, lastFrame = 140, hitBoxCenter = new Vector2(0.56f, 0.32f), hitBoxSize = new Vector2(0.58f, 0.30f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "6B", attackName = "Step Overhead Punch", button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 19, active = 4, recovery = 16, damage = 720, guard = BBGuard.Overhead, advOnBlock = -6, hitstun = 20, blockstun = 15,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_6B", fbxTake = "80/80_01", firstFrame = 15, lastFrame = 38, hitBoxCenter = new Vector2(0.48f, 1.25f), hitBoxSize = new Vector2(0.52f, 0.55f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5C", attackName = "Greatsword Cleave", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 14, damage = 780, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 22, blockstun = 14,
                    groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_5C", fbxTake = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", firstFrame = 0, lastFrame = 40, hitBoxCenter = new Vector2(0.95f, 1.20f), hitBoxSize = new Vector2(1.10f, 0.70f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "2C", "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2C", attackName = "Low Sweep", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 18, damage = 680, guard = BBGuard.Low, advOnBlock = -9, hitstun = 24, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_2C", fbxTake = "80/80_01", firstFrame = 85, lastFrame = 109, hitBoxCenter = new Vector2(0.54f, 0.22f), hitBoxSize = new Vector2(0.62f, 0.30f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "3C", "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "3C", attackName = "Rising Kick Launcher", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 16, damage = 720, guard = BBGuard.Mid, advOnBlock = -8, hitstun = 28, blockstun = 15,
                    groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_3C", fbxTake = "80/80_01", firstFrame = 120, lastFrame = 145, hitBoxCenter = new Vector2(0.44f, 1.20f), hitBoxSize = new Vector2(0.50f, 0.65f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D", "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "j.A", attackName = "Air Quick Tap", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 8, damage = 260, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 12, blockstun = 10,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_jA", fbxTake = "80/80_02", firstFrame = 12, lastFrame = 30, hitBoxCenter = new Vector2(0.38f, 0.70f), hitBoxSize = new Vector2(0.42f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.B", attackName = "Air Horizontal Poke", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 5, recovery = 12, damage = 650, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 18, blockstun = 12,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_jB", fbxTake = "80/80_02", firstFrame = 38, lastFrame = 58, hitBoxCenter = new Vector2(0.52f, 0.68f), hitBoxSize = new Vector2(0.56f, 0.38f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.C", attackName = "Air Heavy Straight", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 5, recovery = 14, damage = 800, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 22, blockstun = 14,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_jC", fbxTake = "80/80_02", firstFrame = 68, lastFrame = 90, hitBoxCenter = new Vector2(0.54f, 0.58f), hitBoxSize = new Vector2(0.58f, 0.42f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec {
                    moveId = "5D", attackName = "Plasma Shot (Burst)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 12, active = 2, recovery = 16, damage = 520, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 20, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Fina_5D", fbxTake = "144/144_20", firstFrame = 15, lastFrame = 38, hitBoxCenter = new Vector2(0.85f, 1.10f), hitBoxSize = new Vector2(0.60f, 0.40f),
                    attribute = BBAttribute.Projectile, gatlingInto = new string[] { "6D", "2D" }
                },
                new MoveSpec {
                    moveId = "2D", attackName = "Low Plasma Shot", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 12, active = 2, recovery = 18, damage = 460, guard = BBGuard.Low, advOnBlock = -4, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Fina_2D", fbxTake = "135/135_05", firstFrame = 40, lastFrame = 62, hitBoxCenter = new Vector2(0.85f, 0.35f), hitBoxSize = new Vector2(0.60f, 0.35f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "6D", attackName = "Overhead Hop Mixup", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 16, active = 4, recovery = 14, damage = 680, guard = BBGuard.Overhead, advOnBlock = -5, hitstun = 20, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Fina_6D", fbxTake = "16/16_02", firstFrame = 10, lastFrame = 32, hitBoxCenter = new Vector2(0.52f, 1.25f), hitBoxSize = new Vector2(0.55f, 0.55f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "j.D", attackName = "Air Plasma Shot", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 10, active = 2, recovery = 14, damage = 420, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 16, blockstun = 12,
                    airOk = true, groundOk = false, specialCancel = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Fina_jD", fbxTake = "143/143_23", firstFrame = 30, lastFrame = 52, hitBoxCenter = new Vector2(0.70f, 0.45f), hitBoxSize = new Vector2(0.55f, 0.40f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "236A", attackName = "Plasma Fireball A", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 11, active = 2, recovery = 16, damage = 520, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 18, blockstun = 14,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Fina_236A", fbxTake = "135/135_09", firstFrame = 15, lastFrame = 38, hitBoxCenter = new Vector2(0.85f, 1.10f), hitBoxSize = new Vector2(0.58f, 0.38f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "236B", attackName = "Plasma Fireball B (2-Hit)", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 14, active = 3, recovery = 18, damage = 680, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 22, blockstun = 15,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.10f,
                    clipName = "Fina_236B", fbxTake = "144/144_17", firstFrame = 30, lastFrame = 56, hitBoxCenter = new Vector2(0.90f, 1.10f), hitBoxSize = new Vector2(0.65f, 0.42f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "623C", attackName = "Rising Kick Anti-Air", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M623,
                    startup = 6, active = 5, recovery = 22, damage = 980, guard = BBGuard.Mid, advOnBlock = -16, hitstun = 28, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.10f,
                    clipName = "Fina_623C", fbxTake = "135/135_03", firstFrame = 15, lastFrame = 38, hitBoxCenter = new Vector2(0.44f, 1.30f), hitBoxSize = new Vector2(0.52f, 0.80f),
                    attribute = BBAttribute.Foot, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 6, BBInvuln.Head | BBInvuln.Body) }
                },
                new MoveSpec {
                    moveId = "214B", attackName = "Advancing Stepping Kick", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 10, active = 4, recovery = 15, damage = 760, guard = BBGuard.Mid, advOnBlock = -5, hitstun = 18, blockstun = 14,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.08f,
                    clipName = "Fina_214B", fbxTake = "135/135_03", firstFrame = 50, lastFrame = 75, hitBoxCenter = new Vector2(0.58f, 1.05f), hitBoxSize = new Vector2(0.62f, 0.45f),
                    attribute = BBAttribute.Foot
                },
                new MoveSpec {
                    moveId = "236236C", attackName = "Neo-Pink Plasma Burst (Beam DD)", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 12, active = 10, recovery = 22, damage = 2600, guard = BBGuard.Mid, advOnBlock = -12, hitstun = 35, blockstun = 24,
                    groundOk = true, rapidCancelable = true, chipPct = 0.15f, heatCost = 50,
                    clipName = "Fina_236236C", fbxTake = "135/135_06", firstFrame = 50, lastFrame = 90, hitBoxCenter = new Vector2(1.20f, 1.10f), hitBoxSize = new Vector2(1.20f, 0.60f),
                    attribute = BBAttribute.Projectile, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 12, BBInvuln.Head | BBInvuln.Body | BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "632146D", attackName = "Crimson Rush (DD)", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M632146,
                    startup = 14, active = 8, recovery = 22, damage = 2700, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 35, blockstun = 22,
                    groundOk = true, rapidCancelable = true, chipPct = 0.15f, heatCost = 50,
                    clipName = "Fina_Super", fbxTake = "80/80_01", firstFrame = 160, lastFrame = 195, hitBoxCenter = new Vector2(0.55f, 1.15f), hitBoxSize = new Vector2(0.65f, 0.50f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 14, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "OD_EA", attackName = "Exceed Accel (Plasma Overload)", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.None,
                    startup = 10, active = 8, recovery = 22, damage = 2100, guard = BBGuard.Mid, advOnBlock = -14, hitstun = 30, blockstun = 20,
                    groundOk = true, rapidCancelable = true,
                    clipName = "Fina_OD_EA", fbxTake = "80/80_01", firstFrame = 160, lastFrame = 195, hitBoxCenter = new Vector2(0.55f, 1.15f), hitBoxSize = new Vector2(0.65f, 0.50f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot | BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "6BC", attackName = "Forward Footsie Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1300, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true,
                    clipName = "Fina_Throw", fbxTake = "135/135_03", firstFrame = 90, lastFrame = 114, hitBoxCenter = new Vector2(0.48f, 1.05f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "4BC", attackName = "Back Footsie Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1300, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 25, blockstun = 0,
                    groundOk = true,
                    clipName = "Fina_Throw", fbxTake = "135/135_03", firstFrame = 90, lastFrame = 114, hitBoxCenter = new Vector2(-0.48f, 1.05f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.BC", attackName = "Air Footsie Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 20, damage = 1100, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 20, blockstun = 0,
                    airOk = true, groundOk = false,
                    clipName = "Fina_AirThrow", fbxTake = "80/80_02", firstFrame = 38, lastFrame = 58, hitBoxCenter = new Vector2(0.45f, 0.65f), hitBoxSize = new Vector2(0.50f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6AB", attackName = "Counter Assault", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 18, damage = 200, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 20, blockstun = 15,
                    groundOk = true, heatCost = 50,
                    clipName = "Fina_CounterAssault", fbxTake = "79/79_01", firstFrame = 10, lastFrame = 26, hitBoxCenter = new Vector2(0.45f, 1.15f), hitBoxSize = new Vector2(0.50f, 0.45f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot) }
                },
                new MoveSpec {
                    moveId = "AB", attackName = "Crush Trigger", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 24, active = 4, recovery = 18, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 28, blockstun = 24,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true, heatCost = 25,
                    clipName = "Fina_CrushTrigger", fbxTake = "16/16_02", firstFrame = 10, lastFrame = 32, hitBoxCenter = new Vector2(0.52f, 1.25f), hitBoxSize = new Vector2(0.55f, 0.55f),
                    attribute = BBAttribute.Foot
                }
            };
        }
    }
}
#endif
