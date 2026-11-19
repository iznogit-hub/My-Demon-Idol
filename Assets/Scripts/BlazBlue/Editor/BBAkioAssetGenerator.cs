#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Generator script that authors and bakes all 32 BlazBlue AttackData, BoxTrack,
    /// MoveSet, and CharacterProfile assets for C01 Akio in accordance with Spec §8.3 and M7.
    /// </summary>
    public static class BBAkioAssetGenerator
    {
        public const string AKIO_ROOT = "Assets/CombatData/BlazBlue/Akio";
        public const string BOX_TRACK_ROOT = "Assets/CombatData/BlazBlue/Akio/BoxTracks";

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
            public int firstFrame;
            public int lastFrame;
            public Vector2 hitBoxCenter;
            public Vector2 hitBoxSize;
            public BBAttribute attribute;
            public float chipPct;
            public BBInvulnWindow[] invuln;
            public string[] gatlingInto;
        }

        [MenuItem("Demon Idol/BlazBlue/Generate C01 Akio Complete Assets")]
        public static void GenerateAllAkioAssets()
        {
            EnsureDirectories();

            var specs = GetAkioMoveSpecs();
            var createdMoves = new List<BBAttackData>();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{AKIO_ROOT}/BBMoveSet_C01_Akio.asset");
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<BBMoveSet>();
                moveSet.characterId = "C01";
                AssetDatabase.CreateAsset(moveSet, $"{AKIO_ROOT}/BBMoveSet_C01_Akio.asset");
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

            // Create or update Akio's CharacterProfile
            var profilePath = $"{AKIO_ROOT}/BBProfile_C01_Akio.asset";
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.characterId = "C01_Akio";
            profile.characterName = "Akio";
            profile.driveName = "Iron Rush";
            profile.forwardWalkSpeed = 3.6f;
            profile.backwardWalkSpeed = 2.8f;
            profile.forwardDashSpeed = 6.5f;
            profile.forwardDashDurationTicks = 12;
            profile.backdashSpeed = 7.0f;
            profile.backdashDurationTicks = 14;
            profile.backdashInvulnTicks = 5;
            profile.jumpVerticalLaunch = 8.0f;
            profile.jumpHorizontalSpeed = 3.2f;
            profile.gravity = 20.0f;
            profile.standPushboxSize = new Vector2(0.70f, 1.80f);
            profile.crouchPushboxSize = new Vector2(0.80f, 1.30f);
            profile.airPushboxSize = new Vector2(0.60f, 1.40f);
            profile.maxHealth = 11000f;
            profile.defenseModifier = 1.0f;
            profile.moveSet = moveSet;

            // Link override controller
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(
                "Assets/MDI_Characters/Runtime/BlazBlue/Akio_BB_Override.overrideController");
            if (overrideCtrl != null)
            {
                profile.blazBlueOverrideController = overrideCtrl;
            }

            EditorUtility.SetDirty(profile);

            // Link to CharacterDefinition_C01_Akio
            var charDefPath = "Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
            if (charDef != null)
            {
                charDef.blazBlueProfile = profile;
                EditorUtility.SetDirty(charDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBAkioAssetGenerator] Generated {createdMoves.Count} moves, BoxTracks, MoveSet, and Profile for C01 Akio!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(AKIO_ROOT)) Directory.CreateDirectory(AKIO_ROOT);
            if (!Directory.Exists(BOX_TRACK_ROOT)) Directory.CreateDirectory(BOX_TRACK_ROOT);
        }

        private static BBAttackData CreateOrUpdateAttackData(MoveSpec s)
        {
            string attackPath = $"{AKIO_ROOT}/BB_Akio_{s.moveId}.asset";
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
            attack.invuln = s.invuln ?? new BBInvulnWindow[0];
            attack.gatlingInto = s.gatlingInto ?? new string[0];

            // Bake BoxTrack asset
            string trackPath = $"{BOX_TRACK_ROOT}/BoxTrack_Akio_{s.moveId}.asset";
            var track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(trackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<BBBoxTrack>();
                AssetDatabase.CreateAsset(track, trackPath);
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
            track.bakeHash = $"Bake_Akio_{s.moveId}_{s.startup}_{s.active}_{s.recovery}";
        }

        private static MoveSpec[] GetAkioMoveSpecs()
        {
            return new MoveSpec[]
            {
                new MoveSpec {
                    moveId = "5A", attackName = "Standing Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 8, damage = 300, guard = BBGuard.Mid, advOnBlock = -1, hitstun = 12, blockstun = 10,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_5A", firstFrame = 110, lastFrame = 122, hitBoxCenter = new Vector2(0.45f, 1.25f), hitBoxSize = new Vector2(0.50f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2A", attackName = "Crouch Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 7, active = 3, recovery = 11, damage = 280, guard = BBGuard.Low, advOnBlock = -2, hitstun = 13, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_2A", firstFrame = 196, lastFrame = 214, hitBoxCenter = new Vector2(0.42f, 0.40f), hitBoxSize = new Vector2(0.48f, 0.32f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5A", "2A", "5B", "2B", "6B", "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "6A", attackName = "Anti-Air Elbow", button = BBButton.A, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 16, damage = 600, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_6A", firstFrame = 60, lastFrame = 84, hitBoxCenter = new Vector2(0.35f, 1.55f), hitBoxSize = new Vector2(0.55f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(4, 12, BBInvuln.Head) },
                    gatlingInto = new string[] { "5B", "2B", "5C", "2C", "3C", "5D" }
                },
                new MoveSpec {
                    moveId = "5B", attackName = "Strong Straight", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 12, damage = 600, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 16, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "Akio_5B", firstFrame = 123, lastFrame = 136, hitBoxCenter = new Vector2(0.55f, 1.20f), hitBoxSize = new Vector2(0.60f, 0.38f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2B", attackName = "Low Body Hook", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 14, damage = 520, guard = BBGuard.Low, advOnBlock = -5, hitstun = 15, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_2B", firstFrame = 220, lastFrame = 242, hitBoxCenter = new Vector2(0.50f, 0.45f), hitBoxSize = new Vector2(0.52f, 0.35f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "6B", attackName = "Overhead Axe Fist", button = BBButton.B, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 20, active = 4, recovery = 16, damage = 750, guard = BBGuard.Overhead, advOnBlock = -6, hitstun = 18, blockstun = 14,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_6B", firstFrame = 148, lastFrame = 166, hitBoxCenter = new Vector2(0.48f, 1.30f), hitBoxSize = new Vector2(0.55f, 0.50f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5C", "2C", "3C" }
                },
                new MoveSpec {
                    moveId = "5C", attackName = "Stepping Cross", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 12, active = 4, recovery = 16, damage = 900, guard = BBGuard.Mid, advOnBlock = -6, hitstun = 20, blockstun = 14,
                    groundOk = true, fatalCounter = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_5C", firstFrame = 24, lastFrame = 50, hitBoxCenter = new Vector2(0.68f, 1.15f), hitBoxSize = new Vector2(0.75f, 0.45f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "3C", "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "2C", attackName = "Sweep Kick", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 18, damage = 700, guard = BBGuard.Low, advOnBlock = -10, hitstun = 22, blockstun = 12,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_2C", firstFrame = 100, lastFrame = 120, hitBoxCenter = new Vector2(0.60f, 0.25f), hitBoxSize = new Vector2(0.75f, 0.30f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "3C", attackName = "Rising Launcher", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    startup = 12, active = 5, recovery = 18, damage = 750, guard = BBGuard.Mid, advOnBlock = -10, hitstun = 24, blockstun = 13,
                    groundOk = true, jumpCancelOnHit = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_3C", firstFrame = 140, lastFrame = 164, hitBoxCenter = new Vector2(0.45f, 1.10f), hitBoxSize = new Vector2(0.55f, 0.85f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5D", "2D", "6D" }
                },
                new MoveSpec {
                    moveId = "j.A", attackName = "Air Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 5, active = 3, recovery = 8, damage = 320, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 12, blockstun = 10,
                    airOk = true, groundOk = false, rapidCancelable = true,
                    clipName = "Akio_jA", firstFrame = 42, lastFrame = 51, hitBoxCenter = new Vector2(0.38f, 0.85f), hitBoxSize = new Vector2(0.45f, 0.35f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "j.B", "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.B", attackName = "Air Kick", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 8, active = 6, recovery = 14, damage = 700, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 16, blockstun = 12,
                    airOk = true, groundOk = false, jumpCancelOnHit = true, rapidCancelable = true,
                    clipName = "Akio_jB", firstFrame = 40, lastFrame = 58, hitBoxCenter = new Vector2(0.50f, 0.75f), hitBoxSize = new Vector2(0.60f, 0.42f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "j.C", "j.D" }
                },
                new MoveSpec {
                    moveId = "j.C", attackName = "Air Heavy Punch", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    startup = 9, active = 6, recovery = 16, damage = 850, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 20, blockstun = 14,
                    airOk = true, groundOk = false, rapidCancelable = true,
                    clipName = "Akio_jC", firstFrame = 44, lastFrame = 60, hitBoxCenter = new Vector2(0.58f, 0.70f), hitBoxSize = new Vector2(0.70f, 0.50f),
                    attribute = BBAttribute.Head, gatlingInto = new string[] { "j.D" }
                },
                new MoveSpec {
                    moveId = "j.D", attackName = "Dive Punch", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 10, active = 4, recovery = 12, damage = 650, guard = BBGuard.Overhead, advOnBlock = 0, hitstun = 22, blockstun = 14,
                    airOk = true, groundOk = false, rapidCancelable = true,
                    clipName = "Akio_jD", firstFrame = 60, lastFrame = 80, hitBoxCenter = new Vector2(0.45f, 0.30f), hitBoxSize = new Vector2(0.65f, 0.55f),
                    attribute = BBAttribute.Head
                },
                new MoveSpec {
                    moveId = "5D", attackName = "Iron Rush 1", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 6, active = 3, recovery = 10, damage = 350, guard = BBGuard.Mid, advOnBlock = -2, hitstun = 15, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_5D", firstFrame = 34, lastFrame = 47, hitBoxCenter = new Vector2(0.52f, 1.22f), hitBoxSize = new Vector2(0.55f, 0.36f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5D_2" }
                },
                new MoveSpec {
                    moveId = "5D_2", attackName = "Iron Rush 2", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 7, active = 3, recovery = 11, damage = 450, guard = BBGuard.Mid, advOnBlock = -3, hitstun = 16, blockstun = 11,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_5D_2", firstFrame = 123, lastFrame = 136, hitBoxCenter = new Vector2(0.58f, 1.18f), hitBoxSize = new Vector2(0.62f, 0.38f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5D_3", "2D" }
                },
                new MoveSpec {
                    moveId = "5D_3", attackName = "Iron Rush 3", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 9, active = 4, recovery = 14, damage = 600, guard = BBGuard.Low, advOnBlock = -6, hitstun = 20, blockstun = 12,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_5D_3", firstFrame = 100, lastFrame = 120, hitBoxCenter = new Vector2(0.65f, 0.35f), hitBoxSize = new Vector2(0.70f, 0.40f),
                    attribute = BBAttribute.Body, gatlingInto = new string[] { "5D_4" }
                },
                new MoveSpec {
                    moveId = "5D_4", attackName = "Iron Finisher (OD)", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 8, active = 4, recovery = 14, damage = 750, guard = BBGuard.Mid, advOnBlock = 2, hitstun = 24, blockstun = 16,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_5D_4", firstFrame = 24, lastFrame = 50, hitBoxCenter = new Vector2(0.72f, 1.15f), hitBoxSize = new Vector2(0.78f, 0.48f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "2D", attackName = "Low Rush", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 14, damage = 450, guard = BBGuard.Low, advOnBlock = -4, hitstun = 16, blockstun = 13,
                    groundOk = true, specialCancel = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_2D", firstFrame = 85, lastFrame = 105, hitBoxCenter = new Vector2(0.50f, 0.35f), hitBoxSize = new Vector2(0.58f, 0.35f),
                    attribute = BBAttribute.Foot, gatlingInto = new string[] { "5D_2" }
                },
                new MoveSpec {
                    moveId = "6D", attackName = "Rush Step", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    startup = 22, active = 4, recovery = 16, damage = 700, guard = BBGuard.Overhead, advOnBlock = -5, hitstun = 20, blockstun = 15,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_6D", firstFrame = 20, lastFrame = 40, hitBoxCenter = new Vector2(0.60f, 1.25f), hitBoxSize = new Vector2(0.65f, 0.45f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "236A", attackName = "Shadow Kunai (Slow)", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 12, active = 1, recovery = 20, damage = 600, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 20, blockstun = 17,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.10f,
                    clipName = "Akio_236A", firstFrame = 0, lastFrame = 33, hitBoxCenter = new Vector2(0.80f, 1.10f), hitBoxSize = new Vector2(0.50f, 0.30f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "236B", attackName = "Shadow Kunai (Fast)", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 12, active = 1, recovery = 20, damage = 600, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 20, blockstun = 17,
                    groundOk = true, ddCancel = true, rapidCancelable = true, chipPct = 0.10f,
                    clipName = "Akio_236B", firstFrame = 0, lastFrame = 33, hitBoxCenter = new Vector2(0.80f, 1.10f), hitBoxSize = new Vector2(0.50f, 0.30f),
                    attribute = BBAttribute.Projectile
                },
                new MoveSpec {
                    moveId = "623C", attackName = "Rising Fist DP", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M623,
                    startup = 6, active = 6, recovery = 22, damage = 1000, guard = BBGuard.Mid, advOnBlock = -18, hitstun = 28, blockstun = 10,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_623C", firstFrame = 15, lastFrame = 34, hitBoxCenter = new Vector2(0.42f, 1.45f), hitBoxSize = new Vector2(0.65f, 0.90f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 8, BBInvuln.Head | BBInvuln.Body) }
                },
                new MoveSpec {
                    moveId = "214B", attackName = "Shoulder Rush", button = BBButton.B, kind = BBMoveKind.Special, motion = BBMotion.M214,
                    startup = 14, active = 4, recovery = 18, damage = 800, guard = BBGuard.Mid, advOnBlock = -4, hitstun = 22, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_214B", firstFrame = 40, lastFrame = 62, hitBoxCenter = new Vector2(0.62f, 1.05f), hitBoxSize = new Vector2(0.70f, 0.60f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(6, 14, BBInvuln.Projectile) }
                },
                new MoveSpec {
                    moveId = "236C", attackName = "Rush Combination", button = BBButton.C, kind = BBMoveKind.Special, motion = BBMotion.M236,
                    startup = 13, active = 8, recovery = 18, damage = 900, guard = BBGuard.Mid, advOnBlock = -8, hitstun = 24, blockstun = 18,
                    groundOk = true, ddCancel = true, rapidCancelable = true,
                    clipName = "Akio_236C", firstFrame = 24, lastFrame = 50, hitBoxCenter = new Vector2(0.68f, 1.15f), hitBoxSize = new Vector2(0.75f, 0.50f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "236236C", attackName = "Iron Barrage (DD)", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    startup = 14, active = 8, recovery = 22, damage = 2800, guard = BBGuard.Mid, advOnBlock = -12, hitstun = 36, blockstun = 18,
                    groundOk = true, rapidCancelable = true, chipPct = 0.20f,
                    clipName = "Akio_Super", firstFrame = 48, lastFrame = 80, hitBoxCenter = new Vector2(0.75f, 1.20f), hitBoxSize = new Vector2(0.90f, 0.70f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 14, BBInvuln.Strike) }
                },
                new MoveSpec {
                    moveId = "632146D", attackName = "Crimson Shadow Rush (DD)", button = BBButton.D, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M632146,
                    startup = 10, active = 24, recovery = 26, damage = 3000, guard = BBGuard.Mid, advOnBlock = -20, hitstun = 40, blockstun = 16,
                    groundOk = true, rapidCancelable = true, chipPct = 0.20f,
                    clipName = "Akio_632146D", firstFrame = 48, lastFrame = 80, hitBoxCenter = new Vector2(0.80f, 1.15f), hitBoxSize = new Vector2(0.95f, 0.75f),
                    attribute = BBAttribute.Body, invuln = new BBInvulnWindow[] { new BBInvulnWindow(1, 10, BBInvuln.Throw) }
                },
                new MoveSpec {
                    moveId = "OD_EA", attackName = "Exceed Accel", button = BBButton.A, kind = BBMoveKind.Special, motion = BBMotion.None,
                    startup = 12, active = 8, recovery = 24, damage = 2200, guard = BBGuard.Mid, advOnBlock = -16, hitstun = 32, blockstun = 16,
                    groundOk = true, rapidCancelable = false,
                    clipName = "Akio_ExceedAccel", firstFrame = 48, lastFrame = 80, hitBoxCenter = new Vector2(0.70f, 1.20f), hitBoxSize = new Vector2(0.85f, 0.65f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "6BC", attackName = "Forward Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 30, blockstun = 0,
                    groundOk = true, rapidCancelable = false,
                    clipName = "Akio_Throw", firstFrame = 26, lastFrame = 48, hitBoxCenter = new Vector2(0.42f, 1.10f), hitBoxSize = new Vector2(0.42f, 0.42f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "4BC", attackName = "Back Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 24, damage = 1200, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 30, blockstun = 0,
                    groundOk = true, rapidCancelable = false,
                    clipName = "Akio_Throw", firstFrame = 26, lastFrame = 48, hitBoxCenter = new Vector2(-0.42f, 1.10f), hitBoxSize = new Vector2(0.42f, 0.42f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "j.BC", attackName = "Air Throw", button = BBButton.B, kind = BBMoveKind.Throw, motion = BBMotion.None,
                    startup = 7, active = 2, recovery = 20, damage = 1000, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 26, blockstun = 0,
                    airOk = true, groundOk = false, rapidCancelable = false,
                    clipName = "Akio_AirThrow", firstFrame = 42, lastFrame = 62, hitBoxCenter = new Vector2(0.40f, 0.85f), hitBoxSize = new Vector2(0.42f, 0.42f),
                    attribute = BBAttribute.Head
                },
                new MoveSpec {
                    moveId = "6AB", attackName = "Counter Assault", button = BBButton.A, kind = BBMoveKind.System, motion = BBMotion.None,
                    startup = 9, active = 3, recovery = 26, damage = 0, guard = BBGuard.Mid, advOnBlock = 0, hitstun = 28, blockstun = 18,
                    groundOk = true, rapidCancelable = false,
                    clipName = "Akio_CounterAssault", firstFrame = 123, lastFrame = 136, hitBoxCenter = new Vector2(0.55f, 1.20f), hitBoxSize = new Vector2(0.60f, 0.40f),
                    attribute = BBAttribute.Body
                },
                new MoveSpec {
                    moveId = "AB", attackName = "Crush Trigger", button = BBButton.A, kind = BBMoveKind.System, motion = BBMotion.None,
                    startup = 30, active = 4, recovery = 20, damage = 900, guard = BBGuard.Mid, advOnBlock = 2, hitstun = 28, blockstun = 18,
                    groundOk = true, rapidCancelable = true,
                    clipName = "Akio_CrushTrigger", firstFrame = 48, lastFrame = 75, hitBoxCenter = new Vector2(0.65f, 1.20f), hitBoxSize = new Vector2(0.70f, 0.50f),
                    attribute = BBAttribute.Body
                }
            };
        }
    }
}
#endif
