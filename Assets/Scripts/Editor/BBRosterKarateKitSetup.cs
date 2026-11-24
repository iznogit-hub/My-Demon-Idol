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
    public static class BBRosterKarateKitSetup
    {
        public const string KARATE_ATTACK_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX";
        public const string KARATE_IDLE_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX";
        public const string KARATE_WALK_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Walk.FBX";
        public const string KARATE_WALKSLOW_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@WalkSlow.FBX";
        public const string KARATE_RUN_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Run.FBX";
        public const string KARATE_JUMP_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX";
        public const string KARATE_FALL_FBX = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX";

        public static readonly string[] LaunchFighterIds = new string[]
        {
            "C01", "C02", "C05", "C06", "C08", "C09", "C11", "C12", "C14", "EX01", "EX04", "SK08"
        };

        public static void ConfigureLaunchRoster()
        {
            Debug.Log("<color=#00e676>[BBRosterKarateKitSetup] === Starting 12 Launch Fighters 7-Move Karate Kit Configuration ===</color>");

            // Ensure Karate attack sub-clips exist
            EnsureKarateAttackClips();

            // Sanitize locomotion FBXs
            SanitizeLocomotionFBX(KARATE_IDLE_FBX);
            SanitizeLocomotionFBX(KARATE_WALK_FBX);
            SanitizeLocomotionFBX(KARATE_WALKSLOW_FBX);
            SanitizeLocomotionFBX(KARATE_RUN_FBX);
            SanitizeLocomotionFBX(KARATE_JUMP_FBX);
            SanitizeLocomotionFBX(KARATE_FALL_FBX);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Load sliced attack clips
            var attackClips = LoadKarateAttackClips();

            int successCount = 0;
            for (int i = 0; i < LaunchFighterIds.Length; i++)
            {
                string id = LaunchFighterIds[i];
                try
                {
                    ConfigureFighter(id, attackClips);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBRosterKarateKitSetup] Failed to configure {id}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676>[BBRosterKarateKitSetup] Successfully configured {successCount}/{LaunchFighterIds.Length} Launch Fighters with 7-Move Karate Kit!</color>");
        }

        public static void EnsureKarateAttackClips()
        {
            var importer = AssetImporter.GetAtPath(KARATE_ATTACK_FBX) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[BBRosterKarateKitSetup] Could not find ModelImporter for {KARATE_ATTACK_FBX}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            var existingClips = importer.clipAnimations;
            bool needsReimport = false;
            if (existingClips == null || existingClips.Length < 7)
            {
                needsReimport = true;
            }

            if (needsReimport)
            {
                var clipDefs = new List<ModelImporterClipAnimation>();
                clipDefs.Add(CreateClip("Attack1", "Take 001", 0, 34, false));
                clipDefs.Add(CreateClip("Karate_Jab", "Take 001", 0, 22, false));
                clipDefs.Add(CreateClip("Karate_Palm", "Take 001", 22, 45, false));
                clipDefs.Add(CreateClip("Karate_Roundhouse", "Take 001", 45, 68, false));
                clipDefs.Add(CreateClip("Karate_Sweep", "Take 001", 45, 64, false));
                clipDefs.Add(CreateClip("Karate_AirKick", "Take 001", 10, 32, false));
                clipDefs.Add(CreateClip("Karate_IronRush", "Take 001", 15, 40, false));
                clipDefs.Add(CreateClip("Karate_Super", "Take 001", 0, 68, false));

                importer.clipAnimations = clipDefs.ToArray();
                importer.SaveAndReimport();
                Debug.Log("[BBRosterKarateKitSetup] Sliced 7 attack clips in Karate@Attack1.FBX");
            }
        }

        private static ModelImporterClipAnimation CreateClip(string name, string takeName, int firstFrame, int lastFrame, bool loop)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                takeName = takeName,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = loop,
                loop = loop,
                keepOriginalOrientation = true,
                keepOriginalPositionY = false,
                keepOriginalPositionXZ = true,
                heightFromFeet = false,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true
            };
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

        private static Dictionary<string, AnimationClip> LoadKarateAttackClips()
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(KARATE_ATTACK_FBX);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var a in allAssets)
            {
                if (a is AnimationClip c)
                {
                    clips[c.name] = c;
                }
            }
            return clips;
        }

        private struct MoveConfig
        {
            public string moveId;
            public string defaultAttackName;
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

        public static void ConfigureFighter(string id, Dictionary<string, AnimationClip> attackClips = null)
        {
            if (attackClips == null) attackClips = LoadKarateAttackClips();

            var def = FindDefinition(id);
            if (def == null)
            {
                Debug.LogError($"[BBRosterKarateKitSetup] CharacterDefinition for {id} not found!");
                return;
            }

            var profile = def.blazBlueProfile as BBCharacterProfile;
            if (profile == null)
            {
                Debug.LogError($"[BBRosterKarateKitSetup] BBCharacterProfile for {id} is null!");
                return;
            }

            var moveSet = profile.moveSet as BBMoveSet;
            if (moveSet == null)
            {
                Debug.LogError($"[BBRosterKarateKitSetup] BBMoveSet for {id} is null!");
                return;
            }

            var overrideCtrl = profile.blazBlueOverrideController as AnimatorOverrideController;
            if (overrideCtrl == null && def.animatorController is AnimatorOverrideController aoc)
            {
                overrideCtrl = aoc;
                profile.blazBlueOverrideController = overrideCtrl;
                EditorUtility.SetDirty(profile);
            }

            var archInfo = BBRosterArchetype.Get(id);
            float reach = archInfo.weaponReach > 0.1f ? archInfo.weaponReach : 0.85f;
            string driveName = !string.IsNullOrEmpty(archInfo.driveName) ? archInfo.driveName : (profile.driveName ?? "Drive");

            // Determine target asset directory for moves and boxtracks
            string moveSetPath = AssetDatabase.GetAssetPath(moveSet);
            string baseDir = Path.GetDirectoryName(moveSetPath);

            // If moveSet is in Archetypes, check if fighter has their own folder in Assets/CombatData/BlazBlue/{Name}
            string displayName = !string.IsNullOrEmpty(def.displayName) ? def.displayName.Replace(" ", "") : id;
            string customDir = $"Assets/CombatData/BlazBlue/{displayName}";
            if (Directory.Exists(customDir))
            {
                baseDir = customDir;
            }

            string boxDir = $"{baseDir}/BoxTracks";
            if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);

            // Existing moves dictionary to reuse assets when present
            var existingMoves = new Dictionary<string, BBAttackData>();
            if (moveSet.moves != null)
            {
                foreach (var m in moveSet.moves)
                {
                    if (m != null && !string.IsNullOrEmpty(m.moveId))
                    {
                        existingMoves[m.moveId] = m;
                    }
                }
            }

            var configs = new MoveConfig[]
            {
                // Slot 1: 5A (Light Jab) S/A/R 5 / 3 / 8, OnBlock +1, 280 dmg. Gatlings into 5B, 5C, 2C, 5D, 236236C
                new MoveConfig
                {
                    moveId = "5A", defaultAttackName = "Light Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 5, a = 3, r = 8, onBlock = 1, damage = 280, level = 1, guard = BBGuard.Mid,
                    clipSubName = "Karate_Jab", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "5B", "5C", "2C", "5D", "236236C" }
                },
                // Slot 2: 5B (Medium Strike) S/A/R 7 / 4 / 12, OnBlock -2, 520 dmg. Gatlings into 5C, 2C, 5D, 236236C
                new MoveConfig
                {
                    moveId = "5B", defaultAttackName = "Medium Strike", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 7, a = 4, r = 12, onBlock = -2, damage = 520, level = 2, guard = BBGuard.Mid,
                    clipSubName = "Karate_Palm", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "5C", "2C", "5D", "236236C" }
                },
                // Slot 3: 5C (Heavy Strike) S/A/R 11 / 4 / 17, OnBlock -4, 820 dmg. Gatlings into 5D, 236236C
                new MoveConfig
                {
                    moveId = "5C", defaultAttackName = "Heavy Strike", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 11, a = 4, r = 17, onBlock = -4, damage = 820, level = 3, guard = BBGuard.Mid,
                    clipSubName = "Karate_Roundhouse", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "5D", "236236C" }
                },
                // Slot 4: 2C (Low Sweep) S/A/R 10 / 3 / 22, OnBlock -8, 700 dmg. Hard knockdown on hit
                new MoveConfig
                {
                    moveId = "2C", defaultAttackName = "Low Sweep", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None,
                    s = 10, a = 3, r = 22, onBlock = -8, damage = 700, level = 3, guard = BBGuard.Low,
                    clipSubName = "Karate_Sweep", causesKnockdown = true, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "236236C" }
                },
                // Slot 5: j.B (Jump Attack) S/A/R 7 / 4 / 14, OnBlock 0, 520 dmg. Overhead
                new MoveConfig
                {
                    moveId = "j.B", defaultAttackName = "Jump Attack", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None,
                    s = 7, a = 4, r = 14, onBlock = 0, damage = 520, level = 2, guard = BBGuard.Overhead,
                    clipSubName = "Karate_AirKick", causesKnockdown = false, airOk = true, groundOk = false, heatCost = 0,
                    gatlingTargets = new string[0]
                },
                // Slot 6: 5D (Signature Drive) S/A/R 13 / 4 / 17, OnBlock -4, 750 dmg. Cancels into 236236C
                new MoveConfig
                {
                    moveId = "5D", defaultAttackName = driveName, button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None,
                    s = 13, a = 4, r = 17, onBlock = -4, damage = 750, level = 3, guard = BBGuard.Mid,
                    clipSubName = "Karate_IronRush", causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0,
                    gatlingTargets = new string[] { "236236C" }
                },
                // Slot 7: 236236C (Signature Finisher / Super) S/A/R 9 / 4 / 26, OnBlock -14, 2200 dmg. Spends 50% Heat
                new MoveConfig
                {
                    moveId = "236236C", defaultAttackName = "Signature Finisher", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236,
                    s = 9, a = 4, r = 26, onBlock = -14, damage = 2200, level = 5, guard = BBGuard.Mid,
                    clipSubName = "Karate_Super", causesKnockdown = true, airOk = false, groundOk = true, heatCost = 50,
                    gatlingTargets = new string[0]
                }
            };

            var finalSevenMoves = new List<BBAttackData>();

            foreach (var cfg in configs)
            {
                BBAttackData attack = null;
                if (existingMoves.TryGetValue(cfg.moveId, out var existing))
                {
                    attack = existing;
                }

                if (attack == null)
                {
                    // Check standard file name patterns
                    string[] possiblePaths = new string[]
                    {
                        $"{baseDir}/BB_{displayName}_{cfg.moveId}.asset",
                        $"{baseDir}/BBMove_{id}_{cfg.moveId}.asset",
                        $"{baseDir}/BB_{cfg.moveId}.asset"
                    };

                    foreach (var p in possiblePaths)
                    {
                        attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(p);
                        if (attack != null) break;
                    }
                }

                if (attack == null)
                {
                    string assetPath = $"{baseDir}/BB_{displayName}_{cfg.moveId}.asset";
                    attack = ScriptableObject.CreateInstance<BBAttackData>();
                    AssetDatabase.CreateAsset(attack, assetPath);
                }

                attack.moveId = cfg.moveId;
                attack.attackName = cfg.defaultAttackName;
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
                // blockstun = recovery + (active - 1) + onBlock
                int blockstun = cfg.r + (cfg.a - 1) + cfg.onBlock;
                attack.blockstunTicks = blockstun;
                attack.advantageOnBlock = cfg.onBlock;

                int hitstun = cfg.causesKnockdown ? 36 : (blockstun + 2);
                attack.hitstunTicks = hitstun;
                attack.advantageOnHit = hitstun - (cfg.a - 1 + cfg.r);

                int hitstop = cfg.level == 1 ? 9 : (cfg.level == 2 ? 10 : (cfg.level == 3 ? 11 : 13));
                attack.hitstopAttacker = hitstop;
                attack.hitstopDefender = hitstop;
                attack.hitstopOnBlock = hitstop;
                attack.counterHitStopBonus = 4;

                attack.specialCancel = true;
                attack.ddCancel = true;
                attack.rapidCancelable = cfg.kind != BBMoveKind.DistortionDrive;

                // Animation Clip mapping
                AnimationClip assignedClip = null;
                if (attackClips.TryGetValue(cfg.clipSubName, out var foundClip))
                {
                    assignedClip = foundClip;
                }
                else if (attackClips.TryGetValue("Attack1", out var fallbackClip))
                {
                    assignedClip = fallbackClip;
                }

                attack.clip = assignedClip;
                attack.animationClip = assignedClip;
                attack.clipName = assignedClip != null ? assignedClip.name : cfg.clipSubName;
                attack.clipFirstFrame = 0;
                attack.clipLastFrame = assignedClip != null ? Mathf.RoundToInt(assignedClip.length * 60f) : 30;
                attack.measuredImpactFrame = cfg.s;

                // Bake BoxTrack with archetype reach scaling
                BakeBoxTrack(attack, cfg, baseDir, displayName, id, reach);

                EditorUtility.SetDirty(attack);
                finalSevenMoves.Add(attack);
            }

            // Update BBMoveSet
            moveSet.moves = finalSevenMoves.ToArray();
            EditorUtility.SetDirty(moveSet);

            // Update BBProfile
            var atk5D = finalSevenMoves.Find(m => m.moveId == "5D");
            var atkSuper = finalSevenMoves.Find(m => m.moveId == "236236C");
            if (atk5D != null && atk5D.clip != null) profile.signatureDriveClip = atk5D.clip;
            if (atkSuper != null && atkSuper.clip != null) profile.signatureSuperClip = atkSuper.clip;
            EditorUtility.SetDirty(profile);

            // Update AnimatorOverrideController
            if (overrideCtrl != null)
            {
                UpdateOverrideController(overrideCtrl);
            }

            Debug.Log($"<color=#00e676>[BBRosterKarateKitSetup] Configured {id} ({displayName}) with 7-Move Karate Kit (Reach: {reach:F2}m)</color>");
        }

        private static void BakeBoxTrack(BBAttackData attack, MoveConfig cfg, string baseDir, string charName, string charId, float weaponReach)
        {
            string boxDir = $"{baseDir}/BoxTracks";
            if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);

            string boxPath = $"{boxDir}/BoxTrack_{charName}_{cfg.moveId}.asset";
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

            // Archetype reach delta: if weapon reach is longer than base 0.85m, extend farEdge
            float reachDelta = Mathf.Max(0f, weaponReach - 0.85f);

            if (cfg.moveId == "2C")
            {
                cy = 0.45f;
                hy = 0.25f;
                farEdge = 1.35f + reachDelta;
            }
            else if (cfg.moveId == "j.B")
            {
                cy = 0.15f;
                hy = 0.70f;
                farEdge = 1.10f + reachDelta;
            }
            else if (cfg.moveId == "5A")
            {
                farEdge = 0.95f + reachDelta * 0.5f;
            }
            else if (cfg.moveId == "5B")
            {
                farEdge = 1.15f + reachDelta * 0.7f;
            }
            else if (cfg.moveId == "5C")
            {
                farEdge = 1.55f + reachDelta;
            }
            else if (cfg.moveId == "5D")
            {
                farEdge = 1.95f + reachDelta;
            }
            else if (cfg.moveId == "236236C")
            {
                farEdge = 2.45f + reachDelta;
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

            track.keyTicks = new int[] { cfg.s };
            track.bakeHash = $"Bake_{charId}_{cfg.moveId}_{cfg.s}_{cfg.a}_{cfg.r}";

            EditorUtility.SetDirty(track);
            attack.boxes = track;
        }

        private static void UpdateOverrideController(AnimatorOverrideController overrideCtrl)
        {
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
        }

        private static CharacterDefinition FindDefinition(string id)
        {
            string[] found = AssetDatabase.FindAssets($"CharacterDefinition_{id}_ t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
            if (found.Length == 0)
            {
                found = AssetDatabase.FindAssets($"CharacterDefinition_{id} t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
            }

            if (found.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(found[0]);
                return AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            }
            return null;
        }
    }
}
#endif
