#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBAkioKarateKitSetup
    {
        public const string KARATE_ATTACK_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX";
        public const string KARATE_IDLE_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX";
        public const string KARATE_WALK_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Walk.FBX";
        public const string KARATE_WALKSLOW_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@WalkSlow.FBX";
        public const string KARATE_RUN_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Run.FBX";
        public const string KARATE_JUMP_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX";
        public const string KARATE_FALL_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX";

        public static void ConfigureKarateKit()
        {
            Debug.Log("<color=#00e676>[BBAkioKarateKitSetup] Starting Akio Karate 7-Move Kit Configuration...</color>");

            // 1. Configure sub-clips on Karate@Attack1.FBX
            SetupKarateAttackClips();

            // 2. Ensure locomotion clips have root motion off & grounded
            SanitizeLocomotionFBX(KARATE_IDLE_FBX);
            SanitizeLocomotionFBX(KARATE_WALK_FBX);
            SanitizeLocomotionFBX(KARATE_WALKSLOW_FBX);
            SanitizeLocomotionFBX(KARATE_RUN_FBX);
            SanitizeLocomotionFBX(KARATE_JUMP_FBX);
            SanitizeLocomotionFBX(KARATE_FALL_FBX);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Configure the 7 AttackData ScriptableObjects & BoxTracks
            Setup7AttackDataAssets();

            // 4. Update BBMoveSet_C01_Akio.asset & BBProfile_C01_Akio.asset
            UpdateAkioMoveSetAndProfile();

            // 5. Update Akio_BB_Override.overrideController
            UpdateAkioOverrideController();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00e676>[BBAkioKarateKitSetup] Successfully configured C01 Akio 7-Move Karate Kit!</color>");
        }

        private static void SetupKarateAttackClips()
        {
            var importer = AssetImporter.GetAtPath(KARATE_ATTACK_FBX) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[BBAkioKarateKitSetup] Could not find ModelImporter for {KARATE_ATTACK_FBX}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            // Define the 7 attack sub-clips
            // Take 001 in Karate@Attack1.FBX spans frames 0 to 68 (at 60fps) / 0 to 34 (at 30fps)
            var clipDefs = new List<ModelImporterClipAnimation>();

            // Full combo / Attack1
            clipDefs.Add(CreateClip("Attack1", "Take 001", 0, 34, false));

            // Slot 1: 5A - Light Jab (frames 0 to 22 at 60fps)
            clipDefs.Add(CreateClip("Karate_Jab", "Take 001", 0, 22, false));

            // Slot 2: 5B - Medium Strike / Palm (frames 22 to 45 at 60fps)
            clipDefs.Add(CreateClip("Karate_Palm", "Take 001", 22, 45, false));

            // Slot 3: 5C - Heavy Strike / Roundhouse (frames 45 to 68 at 60fps)
            clipDefs.Add(CreateClip("Karate_Roundhouse", "Take 001", 45, 68, false));

            // Slot 4: 2C - Low Sweep (crouched sweep rotation, frames 45 to 64)
            clipDefs.Add(CreateClip("Karate_Sweep", "Take 001", 45, 64, false));

            // Slot 5: j.B - Jump Attack / Air Kick (strike frames 10 to 32)
            clipDefs.Add(CreateClip("Karate_AirKick", "Take 001", 10, 32, false));

            // Slot 6: 5D - Signature Drive / Iron Rush (driving punch sequence, frames 15 to 40)
            clipDefs.Add(CreateClip("Karate_IronRush", "Take 001", 15, 40, false));

            // Slot 7: 236236C - Signature Finisher / Super (full flurry combo 0 to 68)
            clipDefs.Add(CreateClip("Karate_Super", "Take 001", 0, 68, false));

            importer.clipAnimations = clipDefs.ToArray();
            importer.SaveAndReimport();
            Debug.Log("[BBAkioKarateKitSetup] Sliced 7 attack clips in Karate@Attack1.FBX");
        }

        private static ModelImporterClipAnimation CreateClip(string name, string takeName, int firstFrame, int lastFrame, bool loop)
        {
            var clip = new ModelImporterClipAnimation
            {
                name = name,
                takeName = takeName,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = loop,
                loop = loop,
                keepOriginalOrientation = true,
                keepOriginalPositionY = false, // ground to feet
                keepOriginalPositionXZ = true,
                heightFromFeet = false,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true
            };
            return clip;
        }

        private static void SanitizeLocomotionFBX(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].keepOriginalPositionY = false;
                    clips[i].keepOriginalOrientation = true;
                    clips[i].lockRootRotation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].lockRootPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private struct MoveConfig
        {
            public string moveId;
            public string attackName;
            public BBButton button;
            public BBMoveKind kind;
            public BBMotion motion;
            public int s, a, r;
            public int onBlock;
            public int damage;
            public int level;
            public BBGuard guard;
            public string clipSubName;
            public bool causesKnockdown;
            public bool airOk;
            public bool groundOk;
            public int heatCost;
            public string[] gatlingTargets;
        }

        private static void Setup7AttackDataAssets()
        {
            // Load the newly sliced clips from Karate@Attack1.FBX
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(KARATE_ATTACK_FBX);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var a in allAssets)
            {
                if (a is AnimationClip c)
                {
                    clips[c.name] = c;
                }
            }

            var configs = new MoveConfig[]
            {
                // Slot 1: 5A (Light Jab) S/A/R 5 / 3 / 8, OnBlock +1, 280 dmg. Gatlings into 5B, 5C, 2C, 5D, 236236C
                new MoveConfig
                {
                    moveId = "5A", attackName = "Light Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 5, a = 3, r = 8, onBlock = 1, damage = 280, level = 1, guard = BBGuard.Mid,
                    clipSubName = "Karate_Jab", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "5B", "5C", "2C", "5D", "236236C" }
                },
                // Slot 2: 5B (Medium Strike) S/A/R 7 / 4 / 12, OnBlock -2, 520 dmg. Gatlings into 5C, 2C, 5D, 236236C
                new MoveConfig
                {
                    moveId = "5B", attackName = "Medium Strike", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 7, a = 4, r = 12, onBlock = -2, damage = 520, level = 2, guard = BBGuard.Mid,
                    clipSubName = "Karate_Palm", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "5C", "2C", "5D", "236236C" }
                },
                // Slot 3: 5C (Heavy Strike) S/A/R 11 / 4 / 17, OnBlock -4, 820 dmg. Gatlings into 5D, 236236C
                new MoveConfig
                {
                    moveId = "5C", attackName = "Heavy Strike", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 11, a = 4, r = 17, onBlock = -4, damage = 820, level = 3, guard = BBGuard.Mid,
                    clipSubName = "Karate_Roundhouse", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "5D", "236236C" }
                },
                // Slot 4: 2C (Low Sweep) S/A/R 10 / 3 / 22, OnBlock -8, 700 dmg. Hard knockdown on hit
                new MoveConfig
                {
                    moveId = "2C", attackName = "Low Sweep", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    s = 10, a = 3, r = 22, onBlock = -8, damage = 700, level = 3, guard = BBGuard.Low,
                    clipSubName = "Karate_Sweep", causesKnockdown = true, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "236236C" }
                },
                // Slot 5: j.B (Jump Attack) S/A/R 7 / 4 / 14, OnBlock 0, 520 dmg. Overhead
                new MoveConfig
                {
                    moveId = "j.B", attackName = "Jump Attack", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 7, a = 4, r = 14, onBlock = 0, damage = 520, level = 2, guard = BBGuard.Overhead,
                    clipSubName = "Karate_AirKick", causesKnockdown = false, airOk = true, groundOk = false, heatCost = 0,
                    gatlingTargets = new string[0]
                },
                // Slot 6: 5D (Signature Drive / Iron Rush) S/A/R 13 / 4 / 17, OnBlock -4, 750 dmg. Cancels into 236236C
                new MoveConfig
                {
                    moveId = "5D", attackName = "Iron Rush", button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    s = 13, a = 4, r = 17, onBlock = -4, damage = 750, level = 3, guard = BBGuard.Mid,
                    clipSubName = "Karate_IronRush", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "236236C" }
                },
                // Slot 7: 236236C (Signature Finisher / Super) S/A/R 9 / 4 / 26, OnBlock -14, 2200 dmg. Spends 50% Heat
                new MoveConfig
                {
                    moveId = "236236C", attackName = "Signature Finisher", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    s = 9, a = 4, r = 26, onBlock = -14, damage = 2200, level = 5, guard = BBGuard.Mid,
                    clipSubName = "Karate_Super", causesKnockdown = true, airOk = false, groundOk = true, heatCost = 50,
                    gatlingTargets = new string[0]
                }
            };

            foreach (var cfg in configs)
            {
                string assetPath = $"Assets/CombatData/BlazBlue/Akio/BB_Akio_{cfg.moveId}.asset";
                var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(assetPath);
                if (attack == null)
                {
                    attack = ScriptableObject.CreateInstance<BBAttackData>();
                    AssetDatabase.CreateAsset(attack, assetPath);
                }

                attack.moveId = cfg.moveId;
                attack.attackName = cfg.attackName;
                attack.button = cfg.button;
                attack.kind = cfg.kind;
                attack.motion = cfg.motion;
                attack.startupFrames = cfg.s;
                attack.activeFrames = cfg.a;
                attack.recoveryFrames = cfg.r;
                attack.damage = cfg.damage;
                attack.attackLevel = cfg.level;
                attack.guard = cfg.guard;
                attack.causesKnockdown = cfg.causesKnockdown;
                attack.hardKnockdown = cfg.causesKnockdown;
                attack.airOk = cfg.airOk;
                attack.groundOk = cfg.groundOk;
                attack.heatCost = cfg.heatCost;
                attack.gatlingInto = cfg.gatlingTargets;

                // Dustloop Blockstun formula:
                // recovery = blockstun - (active - 1) - onBlock
                // => blockstun = recovery + (active - 1) + onBlock
                int blockstun = cfg.r + (cfg.a - 1) + cfg.onBlock;
                attack.blockstunTicks = blockstun;
                attack.advantageOnBlock = cfg.onBlock;

                // Hitstun and advantage on hit
                int hitstun = cfg.causesKnockdown ? 36 : (blockstun + 2);
                attack.hitstunTicks = hitstun;
                attack.advantageOnHit = hitstun - (cfg.a - 1 + cfg.r);

                // Hitstops
                int hitstop = cfg.level == 1 ? 9 : (cfg.level == 2 ? 10 : (cfg.level == 3 ? 11 : 13));
                attack.hitstopAttacker = hitstop;
                attack.hitstopDefender = hitstop;
                attack.hitstopOnBlock = hitstop;
                attack.counterHitStopBonus = 4;

                // Cancels
                attack.specialCancel = true;
                attack.ddCancel = true;
                attack.rapidCancelable = cfg.kind != BBMoveKind.DistortionDrive;

                // Clip mapping
                AnimationClip assignedClip = null;
                if (clips.TryGetValue(cfg.clipSubName, out var foundClip))
                {
                    assignedClip = foundClip;
                }
                else if (clips.TryGetValue("Attack1", out var fallbackClip))
                {
                    assignedClip = fallbackClip;
                }

                attack.clip = assignedClip;
                attack.animationClip = assignedClip;
                attack.clipName = assignedClip != null ? assignedClip.name : cfg.clipSubName;
                attack.clipFirstFrame = 0;
                attack.clipLastFrame = assignedClip != null ? Mathf.RoundToInt(assignedClip.length * 60f) : 30;
                attack.measuredImpactFrame = cfg.s; // peakLimbTick == firstActiveTick

                // Bake BoxTrack
                BakeBoxTrack(attack, cfg);

                EditorUtility.SetDirty(attack);
                Debug.Log($"[BBAkioKarateKitSetup] Updated attack {cfg.moveId} with clip {attack.clipName}, S/A/R={cfg.s}/{cfg.a}/{cfg.r}, onBlock={cfg.onBlock}");
            }
        }

        private static void BakeBoxTrack(BBAttackData attack, MoveConfig cfg)
        {
            string boxDir = "Assets/CombatData/BlazBlue/Akio/BoxTracks";
            if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);

            string boxPath = $"{boxDir}/BoxTrack_Akio_{cfg.moveId}.asset";
            var track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(boxPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<BBBoxTrack>();
                AssetDatabase.CreateAsset(track, boxPath);
            }

            int totalTicks = cfg.s + cfg.a + cfg.r;
            track.tickCount = totalTicks;

            // Box dimensions based on move role & height
            float cy = 1.05f;
            float hy = 0.35f;
            float farEdge = 1.10f;

            if (cfg.moveId == "2C")
            {
                cy = 0.45f;
                hy = 0.25f;
                farEdge = 1.35f;
            }
            else if (cfg.moveId == "j.B")
            {
                cy = 0.75f;
                hy = 0.35f;
                farEdge = 1.10f;
            }
            else if (cfg.moveId == "5A")
            {
                farEdge = 0.95f;
            }
            else if (cfg.moveId == "5B")
            {
                farEdge = 1.15f;
            }
            else if (cfg.moveId == "5C")
            {
                farEdge = 1.55f;
            }
            else if (cfg.moveId == "5D")
            {
                farEdge = 1.95f;
            }
            else if (cfg.moveId == "236236C")
            {
                farEdge = 2.45f;
            }

            float minX = 0.15f;
            float hx = (farEdge - minX) * 0.5f;
            float cx = minX + hx;

            var activeBox = new BBBox(cx, cy, hx, hy, BBBoxKind.Hit, 0, BBAttribute.Body);

            track.frameStart = new int[totalTicks + 1];
            var boxList = new List<BBBox>();
            int currentOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentOffset;
                if (t >= cfg.s && t < cfg.s + cfg.a)
                {
                    boxList.Add(activeBox);
                    currentOffset++;
                }
            }
            track.frameStart[totalTicks] = currentOffset;
            track.boxes = boxList.ToArray();

            // First active tick is exactly keyTicks[0] == peakLimbTick
            track.keyTicks = new int[] { cfg.s };
            track.bakeHash = $"Bake_C01_{cfg.moveId}_{cfg.s}_{cfg.a}_{cfg.r}";

            EditorUtility.SetDirty(track);
            attack.boxes = track;
        }

        private static void UpdateAkioMoveSetAndProfile()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/Akio/BBMoveSet_C01_Akio.asset");
            if (moveSet == null)
            {
                Debug.LogError("[BBAkioKarateKitSetup] BBMoveSet_C01_Akio.asset not found!");
                return;
            }

            string[] sevenMoveIds = new string[] { "5A", "5B", "5C", "2C", "j.B", "5D", "236236C" };
            var moveList = new List<BBAttackData>();

            foreach (var mid in sevenMoveIds)
            {
                var atk = AssetDatabase.LoadAssetAtPath<BBAttackData>($"Assets/CombatData/BlazBlue/Akio/BB_Akio_{mid}.asset");
                if (atk != null)
                {
                    moveList.Add(atk);
                }
            }

            moveSet.moves = moveList.ToArray();
            EditorUtility.SetDirty(moveSet);
            Debug.Log($"[BBAkioKarateKitSetup] BBMoveSet_C01_Akio mapped exclusively to {moveSet.moves.Length} active attacks.");

            // Profile
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>("Assets/CombatData/BlazBlue/Akio/BBProfile_C01_Akio.asset");
            if (profile != null)
            {
                var atk5D = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Akio/BB_Akio_5D.asset");
                var atkSuper = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Akio/BB_Akio_236236C.asset");

                if (atk5D != null && atk5D.clip != null) profile.signatureDriveClip = atk5D.clip;
                if (atkSuper != null && atkSuper.clip != null) profile.signatureSuperClip = atkSuper.clip;

                EditorUtility.SetDirty(profile);
            }
        }

        private static void UpdateAkioOverrideController()
        {
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>("Assets/MDI_Characters/Runtime/BlazBlue/Akio_BB_Override.overrideController");
            if (overrideCtrl == null)
            {
                Debug.LogError("[BBAkioKarateKitSetup] Akio_BB_Override.overrideController not found!");
                return;
            }

            var idleFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>(KARATE_IDLE_FBX);
            var walkFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>(KARATE_WALK_FBX);
            var walkSlowFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>(KARATE_WALKSLOW_FBX);
            var runFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>(KARATE_RUN_FBX);
            var fallFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>(KARATE_FALL_FBX);

            var jumpAssets = AssetDatabase.LoadAllAssetsAtPath(KARATE_JUMP_FBX);
            AnimationClip jumpClip = null;
            AnimationClip landClip = null;
            foreach (var a in jumpAssets)
            {
                if (a is AnimationClip c)
                {
                    if (c.name.Contains("Land")) landClip = c;
                    else if (c.name.Contains("Jump") || jumpClip == null) jumpClip = c;
                }
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideCtrl.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                var orig = overrides[i].Key;
                if (orig == null) continue;
                string name = orig.name.ToLowerInvariant();

                if (name.Contains("idle"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, idleFBX != null ? idleFBX : overrides[i].Value);
                }
                else if (name.Contains("walk") || name.Contains("locomotion") || name.Contains("forward"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, walkFBX != null ? walkFBX : overrides[i].Value);
                }
                else if (name.Contains("run") || name.Contains("dash"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, runFBX != null ? runFBX : overrides[i].Value);
                }
                else if (name.Contains("jump"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, jumpClip != null ? jumpClip : overrides[i].Value);
                }
                else if (name.Contains("fall"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, fallFBX != null ? fallFBX : overrides[i].Value);
                }
                else if (name.Contains("land"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, landClip != null ? landClip : overrides[i].Value);
                }
                else if (name.Contains("guard") || name.Contains("block"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, idleFBX != null ? idleFBX : overrides[i].Value);
                }
            }

            overrideCtrl.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideCtrl);
            Debug.Log("[BBAkioKarateKitSetup] Akio_BB_Override locomotion mapped to Karate pack.");
        }
    }
}
#endif
