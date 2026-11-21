#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    [InitializeOnLoad]
    public static class BBWarriorMasterRemapper
    {
        public const string TRIGGER_FILE = "remap_all.trigger";

        static BBWarriorMasterRemapper()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                ExecuteMasterRemap();
            }
            if (File.Exists("run_all_tests.trigger"))
            {
                try { File.Delete("run_all_tests.trigger"); } catch { }
                BBAllTestsRunner.RunAllSuites();
            }
        }

        public struct MoveRemapDef
        {
            public string moveId;
            public string attackName;
            public BBButton button;
            public BBMoveKind kind;
            public BBMotion motion;
            public string fbxPath;
            public string clipName;
            public int damage;
            public BBGuard guard;
            public Vector2 hitboxCenter;
            public Vector2 hitboxSize;
        }

        private static MoveRemapDef M(string id, string name, BBButton btn, BBMoveKind kind, BBMotion motion, string fbx, string clip, int dmg, BBGuard g, float cx, float cy, float sx, float sy)
        {
            return new MoveRemapDef
            {
                moveId = id, attackName = name, button = btn, kind = kind, motion = motion,
                fbxPath = fbx, clipName = clip, damage = dmg, guard = g,
                hitboxCenter = new Vector2(cx, cy), hitboxSize = new Vector2(sx, sy)
            };
        }

        public static AnimationClip LoadClip(string fbxPath, string clipName = null)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null || assets.Length == 0) return null;

            if (!string.IsNullOrEmpty(clipName))
            {
                foreach (var a in assets)
                {
                    if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    {
                        if (c.name.Equals(clipName, StringComparison.OrdinalIgnoreCase))
                            return c;
                    }
                }
                return null;
            }

            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    return c;
            }
            return null;
        }

        public static AnimationClip GetClipForFighterMove(string fighterId, string moveIdOrClipName)
        {
            if (string.IsNullOrEmpty(fighterId) || string.IsNullOrEmpty(moveIdOrClipName)) return null;

            string moveId = moveIdOrClipName;
            int idx = moveId.IndexOf('_');
            if (idx >= 0)
            {
                string prefix = moveId.Substring(0, idx);
                if (prefix.Equals("Akio", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("Aika", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("UnityChan", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("Cultist", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("StellarWitch", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("Sanpaku", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("SanpakuChan", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("Pico", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("PicoChan", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("Reika", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Equals("Fina", StringComparison.OrdinalIgnoreCase))
                {
                    moveId = moveId.Substring(idx + 1);
                }
            }

            foreach (var def in BBMovesetTrackerExporter.Roster)
            {
                if (def.id.Equals(fighterId, StringComparison.OrdinalIgnoreCase))
                {
                    var moves = GetMovesForFighter(def);
                    foreach (var m in moves)
                    {
                        if (m.moveId.Equals(moveId, StringComparison.OrdinalIgnoreCase))
                        {
                            return LoadClip(m.fbxPath, m.clipName);
                        }
                    }
                    break;
                }
            }
            return null;
        }

        [MenuItem("Demon Idol/BlazBlue/Execute Master Remap (All 25 Fighters)")]
        public static void ExecuteMasterRemap()
        {
            Debug.Log("<color=#00e676>[BBWarriorMasterRemapper] Starting master remap for all 25 fighters...</color>");

            EnsureApprovedSubClips();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int totalFighters = BBMovesetTrackerExporter.Roster.Length;
            int processed = 0;

            foreach (var def in BBMovesetTrackerExporter.Roster)
            {
                try
                {
                    ProcessFighter(def);
                    processed++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBWarriorMasterRemapper] Failed on {def.id} {def.name}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BBMovesetTrackerExporter.ExportMovesetTracker();

            Debug.Log($"<color=#00e676><b>[BBWarriorMasterRemapper] MASTER REMAP COMPLETED! {processed}/{totalFighters} fighters remapped with distinct clips!</b></color>");
        }

        private static void EnsureApprovedSubClips()
        {
            var fbxTargets = new (string path, (string name, float start, float end)[] slices)[]
            {
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", new[] {
                    ("Karate_Jab", 0f, 22f),
                    ("Karate_Palm", 22f, 45f),
                    ("Karate_Roundhouse", 45f, 68f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", new[] {
                    ("Brute_Hook", 0f, 28f),
                    ("Brute_Uppercut", 28f, 58f),
                    ("Brute_DoubleSlam", 58f, 90f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", new[] {
                    ("Ninja_Slash1", 0f, 18f),
                    ("Ninja_Slash2", 18f, 36f),
                    ("Ninja_Cross", 36f, 52f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", new[] {
                    ("Sorceress_Burst", 0f, 26f),
                    ("Sorceress_Wave", 26f, 52f),
                    ("Sorceress_Nova", 52f, 80f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", new[] {
                    ("Knight_Slash", 0f, 26f),
                    ("Knight_ShieldBash", 26f, 52f),
                    ("Knight_Cleave", 52f, 80f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", new[] {
                    ("TwoHanded_Sweep", 0f, 26f),
                    ("TwoHanded_Slash", 26f, 52f),
                    ("TwoHanded_Slam", 52f, 80f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", new[] {
                    ("Archer_QuickDraw", 0f, 24f),
                    ("Archer_PowerShot", 24f, 48f),
                    ("Archer_Volley", 48f, 70f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", new[] {
                    ("Mage_StaffPoke", 0f, 26f),
                    ("Mage_OrbCast", 26f, 52f),
                    ("Mage_Eruption", 52f, 80f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", new[] {
                    ("Swordsman_Slash1", 0f, 26f),
                    ("Swordsman_Thrust", 26f, 52f),
                    ("Swordsman_Cleave", 52f, 80f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", new[] {
                    ("Spearman_Thrust1", 0f, 26f),
                    ("Spearman_Sweep", 26f, 52f),
                    ("Spearman_Lance", 52f, 80f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", new[] {
                    ("Hammer_Swing", 0f, 28f),
                    ("Hammer_Smash", 28f, 58f),
                    ("Hammer_Quake", 58f, 90f)
                }),
                ("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX", new[] {
                    ("Crossbow_Snapshot", 0f, 24f),
                    ("Crossbow_Burst", 24f, 48f),
                    ("Crossbow_Scatter", 48f, 70f)
                })
            };

            foreach (var (path, slices) in fbxTargets)
            {
                if (!File.Exists(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                var existing = importer.clipAnimations;
                if (existing == null || existing.Length == 0) existing = importer.defaultClipAnimations;
                var list = new List<ModelImporterClipAnimation>();
                if (existing != null)
                {
                    foreach (var c in existing) list.Add(c);
                }

                bool modified = false;
                string take = list.Count > 0 ? list[0].takeName : "Take 001";

                foreach (var (sname, start, end) in slices)
                {
                    if (list.Exists(x => x.name == sname)) continue;

                    list.Add(new ModelImporterClipAnimation
                    {
                        name = sname,
                        takeName = take,
                        firstFrame = start,
                        lastFrame = end,
                        loopTime = false,
                        keepOriginalPositionY = true,
                        keepOriginalPositionXZ = false,
                        lockRootRotation = true,
                        lockRootHeightY = true,
                        lockRootPositionXZ = true
                    });
                    modified = true;
                }

                if (modified)
                {
                    importer.clipAnimations = list.ToArray();
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ProcessFighter(BBMovesetTrackerExporter.FighterDef def)
        {
            var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(def.profilePath);
            if (profile == null) return;

            BBMoveSet moveSet = profile.moveSet as BBMoveSet;
            if (moveSet == null) return;

            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(def.overridePath);

            var moveDefs = GetMovesForFighter(def);
            var movesMap = new Dictionary<string, BBAttackData>();
            if (moveSet.moves != null)
            {
                foreach (var m in moveSet.moves)
                {
                    if (m != null && !string.IsNullOrEmpty(m.moveId))
                        movesMap[m.moveId] = m;
                }
            }

            var allMovesList = new List<BBAttackData>();

            AnimationClip clip5A = null, clip5B = null, clip5C = null, clip5D = null;
            AnimationClip clipSpecial = null, clipSuper = null, clipThrow = null;

            foreach (var mdef in moveDefs)
            {
                movesMap.TryGetValue(mdef.moveId, out var attack);
                if (attack == null)
                {
                    string folder = Path.GetDirectoryName(def.profilePath);
                    string charName = def.name;
                    string assetPath = $"{folder}/BB_{charName}_{mdef.moveId}.asset";
                    attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(assetPath);
                    if (attack == null)
                    {
                        attack = ScriptableObject.CreateInstance<BBAttackData>();
                        attack.moveId = mdef.moveId;
                        AssetDatabase.CreateAsset(attack, assetPath);
                    }
                }

                var clip = LoadClip(mdef.fbxPath, mdef.clipName);
                if (clip != null)
                {
                    attack.clip = clip;
                    attack.animationClip = clip;

                    int totalFrames = Mathf.Max(1, Mathf.RoundToInt(clip.length * 60f));
                    int hitFrame = Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.32f), 4, totalFrames - 2);

                    attack.attackName = mdef.attackName;
                    attack.button = mdef.button;
                    attack.kind = mdef.kind;
                    attack.motion = mdef.motion;
                    attack.startupFrames = hitFrame;
                    attack.activeFrames = 3;
                    attack.recoveryFrames = Mathf.Max(1, totalFrames - hitFrame - 3);
                    attack.damage = mdef.damage;
                    attack.guard = mdef.guard;
                    attack.hitboxOffset = new Vector3(mdef.hitboxCenter.x, mdef.hitboxCenter.y, 0f);
                    attack.hitboxSize = new Vector3(mdef.hitboxSize.x, mdef.hitboxSize.y, 1f);

                    EnsureBoxTrack(attack, folderForFighter(def), def.name, mdef.moveId, attack.startupFrames, attack.activeFrames, attack.recoveryFrames, mdef.hitboxCenter, mdef.hitboxSize);

                    EditorUtility.SetDirty(attack);
                }
                else
                {
                    Debug.LogWarning($"[BBWarriorMasterRemapper] Missing clip for {def.id} move {mdef.moveId}: {mdef.fbxPath} ({mdef.clipName})");
                }

                movesMap[mdef.moveId] = attack;
                if (!allMovesList.Contains(attack)) allMovesList.Add(attack);

                if (mdef.moveId == "5A") clip5A = attack.clip;
                else if (mdef.moveId == "5B") clip5B = attack.clip;
                else if (mdef.moveId == "5C") clip5C = attack.clip;
                else if (mdef.moveId == "5D") clip5D = attack.clip;
                else if (mdef.moveId == "236A" || mdef.moveId == "623B" || mdef.moveId == "214A" || mdef.moveId == "623C") clipSpecial = attack.clip;
                else if (mdef.moveId == "236236C" || mdef.moveId == "236236A") clipSuper = attack.clip;
                else if (mdef.moveId == "BC" || mdef.moveId == "6BC") clipThrow = attack.clip;
            }

            if (movesMap.TryGetValue("6BC", out var tf)) moveSet.throwForward = tf;
            else if (movesMap.TryGetValue("BC", out tf)) moveSet.throwForward = tf;
            if (movesMap.TryGetValue("4BC", out var tb)) moveSet.throwBack = tb;
            if (movesMap.TryGetValue("j.BC", out var ta)) moveSet.throwAir = ta;
            if (movesMap.TryGetValue("6AB", out var ca)) moveSet.counterAssault = ca;
            if (movesMap.TryGetValue("AB", out var ct)) moveSet.crushTrigger = ct;

            moveSet.moves = allMovesList.ToArray();
            moveSet.RebuildLookupTables();
            EditorUtility.SetDirty(moveSet);

            if (overrideCtrl != null)
            {
                var overridesList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideCtrl.GetOverrides(overridesList);

                for (int i = 0; i < overridesList.Count; i++)
                {
                    var orig = overridesList[i].Key;
                    if (orig == null) continue;
                    string oname = orig.name;

                    AnimationClip targetClip = null;
                    if ((oname == "HumanF@Attack1H01_R" || oname == "HumanM@Attack1H01_R") && clip5A != null) targetClip = clip5A;
                    else if ((oname == "HumanF@Attack1H01_L" || oname == "HumanM@Attack1H01_L") && clip5B != null) targetClip = clip5B;
                    else if ((oname == "HumanF@Attack2H01" || oname == "HumanM@Attack2H01") && clip5C != null) targetClip = clip5C;
                    else if ((oname == "HumanF@AttackPolearm01" || oname == "HumanM@AttackPolearm01") && clip5D != null) targetClip = clip5D;
                    else if ((oname == "HumanF@AttackShield01" || oname == "HumanM@AttackShield01") && clipThrow != null) targetClip = clipThrow;
                    else if ((oname == "HumanF@MagicAttackDirect1H01_R" || oname == "HumanM@MagicAttackDirect1H01_R") && clipSpecial != null) targetClip = clipSpecial;
                    else if ((oname == "HumanF@MagicAttackDirect2H01" || oname == "HumanM@MagicAttackDirect2H01") && clipSuper != null) targetClip = clipSuper;
                    else if ((oname == "HumanF@MagicAttackOmni01" || oname == "HumanM@MagicAttackOmni01") && clipSuper != null) targetClip = clipSuper;

                    if (targetClip != null)
                    {
                        overridesList[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, targetClip);
                    }
                }

                overrideCtrl.ApplyOverrides(overridesList);
                EditorUtility.SetDirty(overrideCtrl);
            }

            string presPath = $"Assets/Resources/BBPresentation/BBPresentation_{def.id}.asset";
            var pres = AssetDatabase.LoadAssetAtPath<BBCharacterPresentation>(presPath);
            if (pres != null)
            {
                if (clip5D != null) pres.driveClip = clip5D;
                if (clipSpecial != null) pres.specialClip = clipSpecial;
                if (clipSuper != null) pres.superClip = clipSuper;
                EditorUtility.SetDirty(pres);
            }

            EditorUtility.SetDirty(profile);
        }

        private static string folderForFighter(BBMovesetTrackerExporter.FighterDef def)
        {
            return Path.GetDirectoryName(def.profilePath);
        }

        private static void EnsureBoxTrack(BBAttackData attack, string folder, string charName, string moveId, int startup, int active, int recovery, Vector2 hitCenter, Vector2 hitSize)
        {
            string boxDir = $"{folder}/BoxTracks";
            if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);

            string trackPath = $"{boxDir}/BoxTrack_{charName}_{moveId}.asset";
            var track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(trackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<BBBoxTrack>();
                AssetDatabase.CreateAsset(track, trackPath);
            }

            int total = startup + active + recovery;
            track.tickCount = total;
            track.frameStart = new int[total + 1];

            var boxes = new List<BBBox>();
            for (int t = 0; t < total; t++)
            {
                track.frameStart[t] = boxes.Count;
                boxes.Add(new BBBox(0f, 0.85f, 0.25f, 0.85f, BBBoxKind.Hurt));

                if (t >= startup && t < startup + active)
                {
                    boxes.Add(new BBBox(hitCenter.x, hitCenter.y, hitSize.x * 0.5f, hitSize.y * 0.5f, BBBoxKind.Hit));
                }
            }
            track.frameStart[total] = boxes.Count;
            track.boxes = boxes.ToArray();
            EditorUtility.SetDirty(track);
            attack.boxes = track;
        }

        public static List<MoveRemapDef> GetMovesForFighter(BBMovesetTrackerExporter.FighterDef def)
        {
            var moves = new List<MoveRemapDef>();
            string p = def.primaryPack;
            string id = def.id;

            const string MA_DIR = "Assets/Martial Art Animations Sample/Animations";
            const string FP_DIR = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters";
            const string HF_COMBAT = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat";
            const string HM_COMBAT = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat";
            const string W1_DIR = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE";
            const string W2_DIR = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE";
            const string W3_DIR = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE";

            if (id == "C01") // Akio (34 moves)
            {
                moves.Add(M("5A", "Standing Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 300, BBGuard.Mid, 0.55f, 1.25f, 0.5f, 0.35f));
                moves.Add(M("5B", "Straight Punch", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 600, BBGuard.Mid, 0.65f, 1.2f, 0.6f, 0.4f));
                moves.Add(M("5C", "Heavy Hook", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 900, BBGuard.Mid, 0.7f, 1.15f, 0.7f, 0.5f));
                moves.Add(M("5D", "Iron Rush 1", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Crouch Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Trip Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Low Rush", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Axe Fist", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Lunging Elbow", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Rising Launcher", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Jumping Light", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 320, BBGuard.Overhead, 0.5f, 0.8f, 0.5f, 0.4f));
                moves.Add(M("j.B", "Jumping Medium", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 620, BBGuard.Overhead, 0.6f, 0.8f, 0.6f, 0.45f));
                moves.Add(M("j.C", "Jumping Heavy", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 880, BBGuard.Overhead, 0.7f, 0.75f, 0.7f, 0.5f));
                moves.Add(M("j.D", "Jumping Drive", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("BC", "Ground Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("5D_2", "Iron Rush 2", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook", 850, BBGuard.Mid, 0.75f, 1.15f, 0.75f, 0.55f));
                moves.Add(M("5D_3", "Iron Rush 3", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut", 900, BBGuard.Mid, 0.75f, 1.25f, 0.75f, 0.6f));
                moves.Add(M("5D_4", "Iron Finisher", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam", 1200, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.65f));
                moves.Add(M("6D", "Rush Step", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("236A", "Shadow Kunai Slow", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 950, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("236B", "Shadow Kunai Fast", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("236C", "Rush Combination", BBButton.C, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null, 1300, BBGuard.Mid, 0.8f, 1.15f, 0.9f, 0.6f));
                moves.Add(M("623C", "Rising Fist DP", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1200, BBGuard.Mid, 0.6f, 1.5f, 0.7f, 0.8f));
                moves.Add(M("214B", "Shoulder Rush", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{FP_DIR}/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null, 1050, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("236236C", "Iron Barrage", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab", 2500, BBGuard.Mid, 0.8f, 1.25f, 0.9f, 0.65f));
                moves.Add(M("632146D", "Crimson Shadow Rush", BBButton.D, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2", 2800, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 3500, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.System, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.System, BBMotion.None, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm", 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C02") // Aika (30 moves)
            {
                moves.Add(M("5A", "Standing Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 300, BBGuard.Mid, 0.55f, 1.25f, 0.5f, 0.35f));
                moves.Add(M("5B", "Karate Cross", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 600, BBGuard.Mid, 0.65f, 1.2f, 0.6f, 0.4f));
                moves.Add(M("5C", "Roundhouse Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 900, BBGuard.Mid, 0.7f, 1.15f, 0.7f, 0.5f));
                moves.Add(M("5D", "Resonance Nail", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Low Fist", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Front Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Karate Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Ground Slide Nail", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Rising Knee", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Overhead Axe Kick", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Launcher Kick", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Jump Fist", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 320, BBGuard.Overhead, 0.5f, 0.8f, 0.5f, 0.4f));
                moves.Add(M("j.B", "Jump Side Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 620, BBGuard.Overhead, 0.6f, 0.8f, 0.6f, 0.45f));
                moves.Add(M("j.C", "Jump Heel Drop", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 880, BBGuard.Overhead, 0.7f, 0.75f, 0.7f, 0.5f));
                moves.Add(M("j.D", "Air Resonance Nail", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("6D", "Resonance Detonate", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("214B", "Reap", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 1050, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("46C", "Side Kick", BBButton.C, BBMoveKind.Special, BBMotion.Charge46, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("623C", "Rising Dragon Kick", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1200, BBGuard.Mid, 0.6f, 1.5f, 0.7f, 0.8f));
                moves.Add(M("236B", "Step Thrust Kick", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm", 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("236236C", "Hundred Kicks", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab", 2500, BBGuard.Mid, 0.8f, 1.25f, 0.9f, 0.65f));
                moves.Add(M("632146D", "Nail Rain", BBButton.D, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2", 2800, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.D, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 3500, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("BC", "Karate Sweep Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Karate Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{FP_DIR}/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null, 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C04") // UnityChan (33 moves)
            {
                moves.Add(M("5A", "Standing Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 300, BBGuard.Mid, 0.55f, 1.25f, 0.5f, 0.35f));
                moves.Add(M("5B", "Straight Punch", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 600, BBGuard.Mid, 0.65f, 1.2f, 0.6f, 0.4f));
                moves.Add(M("5C", "Heavy Hook", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 900, BBGuard.Mid, 0.7f, 1.15f, 0.7f, 0.5f));
                moves.Add(M("5D", "Drive Strike", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Crouch Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Low Sweep", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Trip Kick", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Axe Fist", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Lunging Elbow", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Rising Launcher", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Jumping Light", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 320, BBGuard.Overhead, 0.5f, 0.8f, 0.5f, 0.4f));
                moves.Add(M("j.B", "Jumping Medium", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 620, BBGuard.Overhead, 0.6f, 0.8f, 0.6f, 0.45f));
                moves.Add(M("j.C", "Jumping Heavy", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 880, BBGuard.Overhead, 0.7f, 0.75f, 0.7f, 0.5f));
                moves.Add(M("j.D", "Jumping Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("6D", "Forward Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("4D", "Back Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{FP_DIR}/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null, 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("236A", "Rush A", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 950, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("236B", "Rush B", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null, 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("623C", "Dragon Kick", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1200, BBGuard.Mid, 0.6f, 1.5f, 0.7f, 0.8f));
                moves.Add(M("214B", "Spiral Hook", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook", 1050, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("46C", "Power Uppercut", BBButton.C, BBMoveKind.Special, BBMotion.Charge46, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut", 1150, BBGuard.Mid, 0.75f, 1.3f, 0.85f, 0.6f));
                moves.Add(M("j.214B", "Aerial Slam", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam", 1200, BBGuard.Overhead, 0.75f, 0.8f, 0.85f, 0.6f));
                moves.Add(M("236236C", "Super Combo", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab", 2500, BBGuard.Mid, 0.8f, 1.25f, 0.9f, 0.65f));
                moves.Add(M("j.236236C", "Air Super Combo", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm", 2500, BBGuard.Overhead, 0.8f, 0.8f, 0.9f, 0.65f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 3500, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("BC", "Ground Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C06") // Cultist (32 moves)
            {
                moves.Add(M("5A", "Arcane Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null, 300, BBGuard.Mid, 0.7f, 1.25f, 0.6f, 0.4f));
                moves.Add(M("5B", "Astral Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null, 600, BBGuard.Mid, 0.85f, 1.2f, 0.7f, 0.45f));
                moves.Add(M("5C", "Dual Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null, 900, BBGuard.Mid, 0.95f, 1.15f, 0.85f, 0.55f));
                moves.Add(M("5D", "Drive Nova", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null, 850, BBGuard.Mid, 0.9f, 1.1f, 1.1f, 1.1f));
                moves.Add(M("2A", "Low Glyph", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Cast.fbx", null, 280, BBGuard.Low, 0.6f, 0.35f, 0.55f, 0.35f));
                moves.Add(M("2B", "Ground Rift", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Cast.fbx", null, 550, BBGuard.Low, 0.75f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2C", "Abyssal Geyser", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Cast.fbx", null, 750, BBGuard.Low, 0.85f, 0.3f, 0.8f, 0.45f));
                moves.Add(M("2D", "Low Resonance", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L.fbx", null, 800, BBGuard.Low, 0.9f, 0.4f, 0.9f, 0.6f));
                moves.Add(M("6A", "Celestial Pillar", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null, 650, BBGuard.Overhead, 0.75f, 1.4f, 0.65f, 0.6f));
                moves.Add(M("6B", "Void Orb", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null, 700, BBGuard.Mid, 0.85f, 1.3f, 0.75f, 0.55f));
                moves.Add(M("6C", "Rift Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null, 850, BBGuard.Mid, 0.95f, 1.25f, 0.85f, 0.65f));
                moves.Add(M("3C", "Stellar Ascension", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", null, 800, BBGuard.Mid, 0.65f, 1.5f, 0.7f, 0.85f));
                moves.Add(M("j.A", "Aerial Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null, 320, BBGuard.Overhead, 0.7f, 0.8f, 0.6f, 0.4f));
                moves.Add(M("j.B", "Aerial Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null, 620, BBGuard.Overhead, 0.85f, 0.8f, 0.7f, 0.45f));
                moves.Add(M("j.C", "Aerial Dual Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null, 880, BBGuard.Overhead, 0.95f, 0.75f, 0.85f, 0.55f));
                moves.Add(M("j.D", "Aerial Nova", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null, 850, BBGuard.Overhead, 0.9f, 0.75f, 1.1f, 1.1f));
                moves.Add(M("6D", "Forward Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", null, 800, BBGuard.Mid, 0.85f, 1.25f, 0.85f, 0.6f));
                moves.Add(M("4D", "Back Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast.fbx", null, 800, BBGuard.Mid, 0.85f, 1.25f, 0.85f, 0.6f));
                moves.Add(M("236B", "Sorcery Burst", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst", 1100, BBGuard.Mid, 0.95f, 1.25f, 1.0f, 0.6f));
                moves.Add(M("22B", "Sorcery Wave", BBButton.B, BBMoveKind.Special, BBMotion.M22, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave", 1050, BBGuard.Low, 0.85f, 0.5f, 0.95f, 0.7f));
                moves.Add(M("22C", "Sorcery Nova", BBButton.C, BBMoveKind.Special, BBMotion.M22, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova", 1150, BBGuard.Low, 0.85f, 0.5f, 0.95f, 0.7f));
                moves.Add(M("623C", "Mage Orb", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_OrbCast", 1200, BBGuard.Mid, 0.7f, 1.5f, 0.85f, 0.85f));
                moves.Add(M("214A", "Staff Poke", BBButton.A, BBMoveKind.Special, BBMotion.M214, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_StaffPoke", 1050, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.5f));
                moves.Add(M("236236C", "Mage Eruption", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_Eruption", 2500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("632146D", "Dark Vortex", BBButton.D, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Cast.fbx", null, 2800, BBGuard.Mid, 1.0f, 1.25f, 1.2f, 1.2f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Cast.fbx", null, 3500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("BC", "Throw Start", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/2H/HumanM@Attack2H01.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_R.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_R.fbx", null, 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C07") // StellarWitch (32 moves)
            {
                moves.Add(M("5A", "Arcane Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null, 300, BBGuard.Mid, 0.7f, 1.25f, 0.6f, 0.4f));
                moves.Add(M("5B", "Astral Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null, 600, BBGuard.Mid, 0.85f, 1.2f, 0.7f, 0.45f));
                moves.Add(M("5C", "Dual Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null, 900, BBGuard.Mid, 0.95f, 1.15f, 0.85f, 0.55f));
                moves.Add(M("5D", "Drive Nova", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", null, 850, BBGuard.Mid, 0.9f, 1.1f, 1.1f, 1.1f));
                moves.Add(M("2A", "Low Glyph", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast.fbx", null, 280, BBGuard.Low, 0.6f, 0.35f, 0.55f, 0.35f));
                moves.Add(M("2B", "Ground Rift", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Cast.fbx", null, 550, BBGuard.Low, 0.75f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2C", "Abyssal Geyser", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Cast.fbx", null, 750, BBGuard.Low, 0.85f, 0.3f, 0.8f, 0.45f));
                moves.Add(M("2D", "Low Resonance", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", null, 800, BBGuard.Low, 0.9f, 0.4f, 0.9f, 0.6f));
                moves.Add(M("6A", "Celestial Pillar", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null, 650, BBGuard.Overhead, 0.75f, 1.4f, 0.65f, 0.6f));
                moves.Add(M("6B", "Void Orb", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null, 700, BBGuard.Mid, 0.85f, 1.3f, 0.75f, 0.55f));
                moves.Add(M("6C", "Rift Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null, 850, BBGuard.Mid, 0.95f, 1.25f, 0.85f, 0.65f));
                moves.Add(M("3C", "Stellar Ascension", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null, 800, BBGuard.Mid, 0.65f, 1.5f, 0.7f, 0.85f));
                moves.Add(M("j.A", "Aerial Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null, 320, BBGuard.Overhead, 0.7f, 0.8f, 0.6f, 0.4f));
                moves.Add(M("j.B", "Aerial Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null, 620, BBGuard.Overhead, 0.85f, 0.8f, 0.7f, 0.45f));
                moves.Add(M("j.C", "Aerial Dual Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null, 880, BBGuard.Overhead, 0.95f, 0.75f, 0.85f, 0.55f));
                moves.Add(M("j.D", "Aerial Nova", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", null, 850, BBGuard.Overhead, 0.9f, 0.75f, 1.1f, 1.1f));
                moves.Add(M("6D", "Forward Call", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L.fbx", null, 800, BBGuard.Mid, 0.85f, 1.25f, 0.85f, 0.6f));
                moves.Add(M("CometFire", "Comet Fire", BBButton.D, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst", 950, BBGuard.Mid, 0.85f, 1.25f, 0.9f, 0.55f));
                moves.Add(M("236A", "Star Bolt", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave", 950, BBGuard.Mid, 0.85f, 1.25f, 0.9f, 0.55f));
                moves.Add(M("236B", "Arcane Surge", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova", 1100, BBGuard.Mid, 0.95f, 1.25f, 1.0f, 0.6f));
                moves.Add(M("623C", "Supernova Flare", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_OrbCast", 1200, BBGuard.Mid, 0.7f, 1.5f, 0.85f, 0.85f));
                moves.Add(M("214B", "Shadow Tether", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_StaffPoke", 1050, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.5f));
                moves.Add(M("236C", "Cosmic Cataclysm", BBButton.C, BBMoveKind.Special, BBMotion.M236, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_Eruption", 1300, BBGuard.Mid, 1.05f, 1.2f, 1.15f, 0.7f));
                moves.Add(M("236236C", "Final Cataclysm", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Cast.fbx", null, 2500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("632146C", "Stellar Super", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Cast.fbx", null, 2800, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Cast.fbx", null, 3500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("BC", "Throw Start", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/2H/HumanF@Attack2H01.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_R.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_R.fbx", null, 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C08") // SanpakuChan (31 moves)
            {
                moves.Add(M("5A", "Quick Slash", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 300, BBGuard.Mid, 0.65f, 1.25f, 0.6f, 0.4f));
                moves.Add(M("5B", "Fierce Slash", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2", 600, BBGuard.Mid, 0.75f, 1.2f, 0.7f, 0.45f));
                moves.Add(M("5C", "Cross Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 900, BBGuard.Mid, 0.85f, 1.15f, 0.85f, 0.55f));
                moves.Add(M("5D", "Drive Strike", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Low Punch", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Sweep Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Trip Kick", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Overhead Fist", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Lunging Elbow", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Rising Launcher", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Aerial Slash 1", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 320, BBGuard.Overhead, 0.65f, 0.8f, 0.6f, 0.4f));
                moves.Add(M("j.B", "Aerial Slash 2", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2", 620, BBGuard.Overhead, 0.75f, 0.8f, 0.7f, 0.45f));
                moves.Add(M("j.C", "Aerial Cross", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 880, BBGuard.Overhead, 0.85f, 0.75f, 0.85f, 0.55f));
                moves.Add(M("j.D", "Aerial Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("4D", "Back Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("236A", "Karate Jab", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab", 950, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("j.236A", "Air Palm", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm", 950, BBGuard.Overhead, 0.7f, 0.8f, 0.8f, 0.5f));
                moves.Add(M("214B", "Roundhouse", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1050, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("214B_2", "Brute Hook", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook", 1100, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("214B_3", "Brute Uppercut", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut", 1150, BBGuard.Mid, 0.8f, 1.3f, 0.85f, 0.6f));
                moves.Add(M("236236B", "Double Slam", BBButton.B, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam", 2500, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.65f));
                moves.Add(M("214214D", "Sorcery Burst", BBButton.D, BBMoveKind.DistortionDrive, BBMotion.M214, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst", 2800, BBGuard.Mid, 0.95f, 1.25f, 1.0f, 0.6f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova", 3500, BBGuard.Mid, 1.0f, 1.25f, 1.15f, 0.7f));
                moves.Add(M("BC", "Ground Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave", 850, BBGuard.Mid, 0.85f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C09") // PicoChan (31 moves)
            {
                moves.Add(M("5A", "Heavy Punch", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 300, BBGuard.Mid, 0.55f, 1.25f, 0.5f, 0.35f));
                moves.Add(M("5B", "Straight Punch", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 600, BBGuard.Mid, 0.65f, 1.2f, 0.6f, 0.4f));
                moves.Add(M("5C", "Berserker Punch", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null, 900, BBGuard.Mid, 0.7f, 1.15f, 0.7f, 0.5f));
                moves.Add(M("5D", "Drive Strike", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Male Punch", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Kick", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Sweep Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Trip Kick", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Overhead Fist", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Lunging Elbow", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Rising Launcher", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Jumping Heavy Punch", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 320, BBGuard.Overhead, 0.5f, 0.8f, 0.5f, 0.4f));
                moves.Add(M("j.B", "Jumping Medium", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 620, BBGuard.Overhead, 0.6f, 0.8f, 0.6f, 0.45f));
                moves.Add(M("j.C", "Jumping Berserker", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{FP_DIR}/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null, 880, BBGuard.Overhead, 0.7f, 0.75f, 0.7f, 0.5f));
                moves.Add(M("j.D", "Jumping Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("6D", "Forward Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("236A", "Female Punch", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 950, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("623C", "Dragon Roundhouse", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1200, BBGuard.Mid, 0.6f, 1.5f, 0.7f, 0.8f));
                moves.Add(M("46C", "Jab Special", BBButton.C, BBMoveKind.Special, BBMotion.Charge46, $"{MA_DIR}/Atk_P_1.fbx", null, 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("63214B", "Hook Special", BBButton.B, BBMoveKind.Special, BBMotion.M632146, $"{MA_DIR}/Atk_P_3.fbx", null, 1150, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.6f));
                moves.Add(M("360C", "Brute Hook Command", BBButton.C, BBMoveKind.Special, BBMotion.M632146, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook", 1300, BBGuard.Unblockable, 0.65f, 1.15f, 0.75f, 0.6f));
                moves.Add(M("236236B", "Double Slam Super", BBButton.B, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam", 2500, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.65f));
                moves.Add(M("632146C", "Brute Uppercut Super", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut", 2800, BBGuard.Mid, 0.85f, 1.3f, 0.95f, 0.7f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 3500, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("BC", "Ground Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            if (id == "C10") // Reika (29 moves)
            {
                moves.Add(M("5A", "Quick Thrust", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_R.fbx", null, 300, BBGuard.Mid, 0.65f, 1.25f, 0.6f, 0.4f));
                moves.Add(M("5B", "Fierce Slash", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_L.fbx", null, 600, BBGuard.Mid, 0.75f, 1.2f, 0.7f, 0.45f));
                moves.Add(M("5C", "Heavy Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/2H/HumanF@Attack2H01.fbx", null, 900, BBGuard.Mid, 0.85f, 1.15f, 0.85f, 0.55f));
                moves.Add(M("5D", "Drive Polearm", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Polearm/HumanF@AttackPolearm01.fbx", null, 850, BBGuard.Mid, 0.9f, 1.1f, 0.95f, 0.55f));
                moves.Add(M("2A", "Low Thrust", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_R.fbx", null, 280, BBGuard.Low, 0.6f, 0.35f, 0.55f, 0.35f));
                moves.Add(M("2B", "Under Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_L.fbx", null, 550, BBGuard.Low, 0.7f, 0.3f, 0.65f, 0.4f));
                moves.Add(M("2C", "Ground Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/2H/HumanM@Attack2H01.fbx", null, 750, BBGuard.Low, 0.8f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("2D", "Low Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HM_COMBAT}/Polearm/HumanM@AttackPolearm01.fbx", null, 800, BBGuard.Low, 0.85f, 0.35f, 0.8f, 0.5f));
                moves.Add(M("6A", "Knight Slash", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{W2_DIR}/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash", 650, BBGuard.Overhead, 0.75f, 1.4f, 0.65f, 0.6f));
                moves.Add(M("6B", "Shield Bash", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{W2_DIR}/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_ShieldBash", 700, BBGuard.Mid, 0.85f, 1.25f, 0.75f, 0.55f));
                moves.Add(M("6C", "Knight Cleave", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{W2_DIR}/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave", 850, BBGuard.Mid, 0.9f, 1.2f, 0.85f, 0.65f));
                moves.Add(M("3C", "Upward Thrust", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 800, BBGuard.Mid, 0.65f, 1.45f, 0.7f, 0.8f));
                moves.Add(M("j.A", "Aerial Thrust", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_R.fbx", null, 320, BBGuard.Overhead, 0.65f, 0.8f, 0.6f, 0.4f));
                moves.Add(M("j.B", "Aerial Slash", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_L.fbx", null, 620, BBGuard.Overhead, 0.75f, 0.8f, 0.7f, 0.45f));
                moves.Add(M("j.C", "Aerial Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/2H/HumanF@Attack2H01.fbx", null, 880, BBGuard.Overhead, 0.85f, 0.75f, 0.85f, 0.55f));
                moves.Add(M("j.D", "Aerial Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{HF_COMBAT}/Polearm/HumanF@AttackPolearm01.fbx", null, 850, BBGuard.Overhead, 0.9f, 0.75f, 0.95f, 0.55f));
                moves.Add(M("VeilRiposte", "Veil Riposte", BBButton.D, BBMoveKind.Special, BBMotion.None, $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep", 1000, BBGuard.Mid, 0.85f, 1.2f, 0.9f, 0.55f));
                moves.Add(M("236B", "Two-Handed Slash", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash", 1100, BBGuard.Mid, 0.95f, 1.25f, 1.0f, 0.6f));
                moves.Add(M("41236C", "Two-Handed Slam", BBButton.C, BBMoveKind.Special, BBMotion.M236, $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam", 1250, BBGuard.Mid, 1.0f, 1.25f, 1.1f, 0.65f));
                moves.Add(M("623C", "Archer Quickdraw", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W2_DIR}/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", "Archer_QuickDraw", 1200, BBGuard.Mid, 0.7f, 1.55f, 0.85f, 0.9f));
                moves.Add(M("236236C", "Archer PowerShot", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W2_DIR}/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", "Archer_PowerShot", 2500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("632146C", "Archer Volley", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{W2_DIR}/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", "Archer_Volley", 2800, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_StaffPoke", 3500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
                moves.Add(M("BC", "Throw Start", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_OrbCast", 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@CombatIdle1H01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HM_COMBAT}/HumanM@CombatDamage01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_Eruption", 850, BBGuard.Mid, 0.85f, 0.5f, 0.95f, 0.7f));
                return moves;
            }

            if (id == "C12") // Fina (30 moves)
            {
                moves.Add(M("5A", "Standing Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 300, BBGuard.Mid, 0.55f, 1.25f, 0.5f, 0.35f));
                moves.Add(M("5B", "Straight Punch", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 600, BBGuard.Mid, 0.65f, 1.2f, 0.6f, 0.4f));
                moves.Add(M("5C", "Heavy Hook", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 900, BBGuard.Mid, 0.7f, 1.15f, 0.7f, 0.5f));
                moves.Add(M("5D", "Drive Strike", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Crouch Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Trip Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Crouch Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Axe Fist", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Lunging Elbow", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Rising Launcher", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Jumping Light", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 320, BBGuard.Overhead, 0.5f, 0.8f, 0.5f, 0.4f));
                moves.Add(M("j.B", "Jumping Medium", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 620, BBGuard.Overhead, 0.6f, 0.8f, 0.6f, 0.45f));
                moves.Add(M("j.C", "Jumping Heavy", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 880, BBGuard.Overhead, 0.7f, 0.75f, 0.7f, 0.5f));
                moves.Add(M("j.D", "Jumping Drive", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("6D", "Forward Kick", BBButton.D, BBMoveKind.DriveMove, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1", 800, BBGuard.Mid, 0.75f, 1.2f, 0.75f, 0.5f));
                moves.Add(M("236A", "Female Punch", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 950, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("236B", "Heavy Punch", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("623C", "Dragon Kick", BBButton.C, BBMoveKind.Special, BBMotion.M623, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1200, BBGuard.Mid, 0.6f, 1.5f, 0.7f, 0.8f));
                moves.Add(M("214B", "Male Punch", BBButton.B, BBMoveKind.Special, BBMotion.M214, $"{FP_DIR}/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null, 1050, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("236236C", "Karate Jab Super", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab", 2500, BBGuard.Mid, 0.8f, 1.25f, 0.9f, 0.65f));
                moves.Add(M("632146D", "Ninja Slash Super", BBButton.D, BBMoveKind.DistortionDrive, BBMotion.M632146, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2", 2800, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("OD_EA", "Exceed Accel", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross", 3500, BBGuard.Mid, 0.85f, 1.25f, 0.95f, 0.7f));
                moves.Add(M("BC", "Ground Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6AB", "Counter Assault", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 500, BBGuard.Mid, 0.5f, 1.1f, 0.5f, 0.5f));
                moves.Add(M("AB", "Crush Trigger", BBButton.A, BBMoveKind.Special, BBMotion.None, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm", 850, BBGuard.Mid, 0.75f, 1.2f, 0.8f, 0.6f));
                return moves;
            }

            // General archetype moves for remaining 16 fighters (Unarmed, Spellcaster, Weapon)
            if (p == "Martial Art Animations" || p == "Fighter Pack Bundle" || id == "EX04")
            {
                moves.Add(M("5A", "Standing Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 300, BBGuard.Mid, 0.55f, 1.25f, 0.5f, 0.35f));
                moves.Add(M("5B", "Straight Punch", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 600, BBGuard.Mid, 0.65f, 1.2f, 0.6f, 0.4f));
                moves.Add(M("5C", "Heavy Hook", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 900, BBGuard.Mid, 0.7f, 1.15f, 0.7f, 0.5f));
                moves.Add(M("5D", "Drive Strike", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Mid, 0.8f, 1.1f, 0.75f, 0.55f));
                moves.Add(M("2A", "Crouch Jab", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_1.fbx", null, 280, BBGuard.Low, 0.5f, 0.4f, 0.5f, 0.35f));
                moves.Add(M("2B", "Low Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_2.fbx", null, 550, BBGuard.Low, 0.6f, 0.35f, 0.65f, 0.4f));
                moves.Add(M("2C", "Trip Kick", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Grab_Break_A.fbx", null, 750, BBGuard.Low, 0.65f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2D", "Crouch Drive", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Grab_Break_B.fbx", null, 800, BBGuard.Low, 0.7f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("6A", "Axe Fist", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_F.fbx", null, 650, BBGuard.Overhead, 0.6f, 1.35f, 0.6f, 0.5f));
                moves.Add(M("6B", "Lunging Elbow", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Quickstep_B.fbx", null, 700, BBGuard.Mid, 0.65f, 1.25f, 0.65f, 0.5f));
                moves.Add(M("6C", "Guard Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_F.fbx", null, 850, BBGuard.Mid, 0.7f, 1.2f, 0.75f, 0.55f));
                moves.Add(M("3C", "Rising Launcher", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{MA_DIR}/Dodge_B.fbx", null, 800, BBGuard.Mid, 0.55f, 1.4f, 0.6f, 0.7f));
                moves.Add(M("j.A", "Jumping Light", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_1.fbx", null, 320, BBGuard.Overhead, 0.5f, 0.8f, 0.5f, 0.4f));
                moves.Add(M("j.B", "Jumping Medium", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_2.fbx", null, 620, BBGuard.Overhead, 0.6f, 0.8f, 0.6f, 0.45f));
                moves.Add(M("j.C", "Jumping Heavy", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_P_3.fbx", null, 880, BBGuard.Overhead, 0.7f, 0.75f, 0.7f, 0.5f));
                moves.Add(M("j.D", "Jumping Drive", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{MA_DIR}/Atk_K_3.fbx", null, 850, BBGuard.Overhead, 0.75f, 0.75f, 0.75f, 0.55f));
                moves.Add(M("BC", "Ground Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Start.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_A.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{MA_DIR}/Grab_Hit_B.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Air Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("236A", "Flurry Rush", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null, 950, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("236B", "Power Thrust", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null, 1100, BBGuard.Mid, 0.75f, 1.2f, 0.85f, 0.55f));
                moves.Add(M("236C", "Haymaker Impact", BBButton.C, BBMoveKind.Special, BBMotion.M236, $"{FP_DIR}/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null, 1300, BBGuard.Mid, 0.8f, 1.15f, 0.9f, 0.6f));
                moves.Add(M("214A", "Slipping Counter", BBButton.A, BBMoveKind.Special, BBMotion.M214, $"{FP_DIR}/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null, 1050, BBGuard.Mid, 0.7f, 1.2f, 0.8f, 0.5f));
                moves.Add(M("623B", "Rising Dragon", BBButton.B, BBMoveKind.Special, BBMotion.M623, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse", 1200, BBGuard.Mid, 0.6f, 1.5f, 0.7f, 0.8f));
                moves.Add(M("22C", "Earth Quake", BBButton.C, BBMoveKind.Special, BBMotion.M22, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm", 1150, BBGuard.Low, 0.75f, 0.5f, 0.85f, 0.6f));
                moves.Add(M("236236C", "Acrobatic Storm", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{W1_DIR}/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab", 2500, BBGuard.Mid, 0.8f, 1.25f, 0.9f, 0.65f));
            }
            else if (p.Contains("Spell") || id == "C13" || id == "SK05" || id == "EX07")
            {
                moves.Add(M("5A", "Arcane Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null, 300, BBGuard.Mid, 0.7f, 1.25f, 0.6f, 0.4f));
                moves.Add(M("5B", "Astral Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null, 600, BBGuard.Mid, 0.85f, 1.2f, 0.7f, 0.45f));
                moves.Add(M("5C", "Dual Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null, 900, BBGuard.Mid, 0.95f, 1.15f, 0.85f, 0.55f));
                moves.Add(M("5D", "Drive Nova", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", null, 850, BBGuard.Mid, 0.9f, 1.1f, 1.1f, 1.1f));
                moves.Add(M("2A", "Low Glyph", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast.fbx", null, 280, BBGuard.Low, 0.6f, 0.35f, 0.55f, 0.35f));
                moves.Add(M("2B", "Ground Rift", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Cast.fbx", null, 550, BBGuard.Low, 0.75f, 0.3f, 0.7f, 0.4f));
                moves.Add(M("2C", "Abyssal Geyser", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Cast.fbx", null, 750, BBGuard.Low, 0.85f, 0.3f, 0.8f, 0.45f));
                moves.Add(M("2D", "Low Resonance", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", null, 800, BBGuard.Low, 0.9f, 0.4f, 0.9f, 0.6f));
                moves.Add(M("6A", "Celestial Pillar", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null, 650, BBGuard.Overhead, 0.75f, 1.4f, 0.65f, 0.6f));
                moves.Add(M("6B", "Void Orb", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null, 700, BBGuard.Mid, 0.85f, 1.3f, 0.75f, 0.55f));
                moves.Add(M("6C", "Rift Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null, 850, BBGuard.Mid, 0.95f, 1.25f, 0.85f, 0.65f));
                moves.Add(M("3C", "Stellar Ascension", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null, 800, BBGuard.Mid, 0.65f, 1.5f, 0.7f, 0.85f));
                moves.Add(M("j.A", "Aerial Spark", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null, 320, BBGuard.Overhead, 0.7f, 0.8f, 0.6f, 0.4f));
                moves.Add(M("j.B", "Aerial Lance", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null, 620, BBGuard.Overhead, 0.85f, 0.8f, 0.7f, 0.45f));
                moves.Add(M("j.C", "Aerial Dual Burst", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null, 880, BBGuard.Overhead, 0.95f, 0.75f, 0.85f, 0.55f));
                moves.Add(M("j.D", "Aerial Nova", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", null, 850, BBGuard.Overhead, 0.9f, 0.75f, 1.1f, 1.1f));
                moves.Add(M("BC", "Aether Grasp", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Force Pulse Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Vortex Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_R.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Aerial Vortex", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_R.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("236A", "Star Bolt", BBButton.A, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst", 950, BBGuard.Mid, 0.85f, 1.25f, 0.9f, 0.55f));
                moves.Add(M("236B", "Arcane Surge", BBButton.B, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave", 1100, BBGuard.Mid, 0.95f, 1.25f, 1.0f, 0.6f));
                moves.Add(M("236C", "Cosmic Cataclysm", BBButton.C, BBMoveKind.Special, BBMotion.M236, $"{W1_DIR}/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova", 1300, BBGuard.Mid, 1.05f, 1.2f, 1.15f, 0.7f));
                moves.Add(M("214A", "Shadow Tether", BBButton.A, BBMoveKind.Special, BBMotion.M214, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_StaffPoke", 1050, BBGuard.Mid, 0.8f, 1.2f, 0.85f, 0.5f));
                moves.Add(M("623B", "Supernova Flare", BBButton.B, BBMoveKind.Special, BBMotion.M623, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_OrbCast", 1200, BBGuard.Mid, 0.7f, 1.5f, 0.85f, 0.85f));
                moves.Add(M("22C", "Abyssal Maw", BBButton.C, BBMoveKind.Special, BBMotion.M22, $"{W2_DIR}/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX", "Mage_Eruption", 1150, BBGuard.Low, 0.85f, 0.5f, 0.95f, 0.7f));
                moves.Add(M("236236C", "Final Cataclysm", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, $"{HF_COMBAT}/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01 - Cast.fbx", "HumanF@MagicAttackOmni01 - Cast", 2500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
            }
            else
            {
                string mainFbx = $"{W3_DIR}/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX";
                string pfx = "Swordsman";

                if (p.Contains("Spearman") || id == "C05" || id == "EX06")
                {
                    mainFbx = $"{W3_DIR}/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX";
                    pfx = "Spearman";
                }
                else if (p.Contains("Hammer") || id == "SK08" || id == "EX02")
                {
                    mainFbx = $"{W3_DIR}/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX";
                    pfx = "Hammer";
                }
                else if (p.Contains("Knight") || id == "SK02")
                {
                    mainFbx = $"{W2_DIR}/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX";
                    pfx = "Knight";
                }
                else if (p.Contains("2 Handed") || id == "C14")
                {
                    mainFbx = $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX";
                    pfx = "TwoHanded";
                }
                else if (p.Contains("Archer") || p.Contains("Crossbow") || id == "EX05" || id == "C11")
                {
                    mainFbx = $"{W2_DIR}/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX";
                    pfx = "Archer";
                }
                else if (p.Contains("Ninja") || id == "SK03")
                {
                    mainFbx = $"{W1_DIR}/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX";
                    pfx = "Ninja";
                }
                else if (p.Contains("Brute") || id == "EX01")
                {
                    mainFbx = $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX";
                    pfx = "Brute";
                }

                string s1 = (pfx == "Swordsman" || pfx == "Ninja") ? $"{pfx}_Slash1" : (pfx == "Spearman" ? $"{pfx}_Thrust1" : (pfx == "Brute" ? $"{pfx}_Hook" : (pfx == "Knight" ? $"{pfx}_Slash" : (pfx == "TwoHanded" ? $"{pfx}_Sweep" : (pfx == "Hammer" ? $"{pfx}_Swing" : $"{pfx}_QuickDraw")))));
                string s2 = pfx == "Swordsman" ? $"{pfx}_Thrust" : (pfx == "Spearman" ? $"{pfx}_Sweep" : (pfx == "Brute" ? $"{pfx}_Uppercut" : (pfx == "Knight" ? $"{pfx}_ShieldBash" : (pfx == "TwoHanded" ? $"{pfx}_Slash" : (pfx == "Hammer" ? $"{pfx}_Smash" : (pfx == "Ninja" ? $"{pfx}_Slash2" : $"{pfx}_PowerShot"))))));
                string s3 = (pfx == "Swordsman" || pfx == "Knight") ? $"{pfx}_Cleave" : (pfx == "Spearman" ? $"{pfx}_Lance" : (pfx == "Brute" ? $"{pfx}_DoubleSlam" : (pfx == "TwoHanded" ? $"{pfx}_Slam" : (pfx == "Hammer" ? $"{pfx}_Quake" : (pfx == "Ninja" ? $"{pfx}_Cross" : $"{pfx}_Volley")))));

                moves.Add(M("5A", "Quick Strike", BBButton.A, BBMoveKind.Normal, BBMotion.None, mainFbx, s1, 300, BBGuard.Mid, 0.65f, 1.25f, 0.6f, 0.4f));
                moves.Add(M("5B", "Fierce Cleave", BBButton.B, BBMoveKind.Normal, BBMotion.None, mainFbx, s2, 600, BBGuard.Mid, 0.75f, 1.2f, 0.7f, 0.45f));
                moves.Add(M("5C", "Mighty Slash", BBButton.C, BBMoveKind.Normal, BBMotion.None, mainFbx, s3, 900, BBGuard.Mid, 0.85f, 1.15f, 0.85f, 0.55f));
                moves.Add(M("5D", "Warrior Drive", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Polearm/HumanF@AttackPolearm01.fbx", null, 850, BBGuard.Mid, 0.9f, 1.1f, 0.95f, 0.55f));
                moves.Add(M("2A", "Low Poke", BBButton.A, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_R.fbx", null, 280, BBGuard.Low, 0.6f, 0.35f, 0.55f, 0.35f));
                moves.Add(M("2B", "Under Sweep", BBButton.B, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@Attack1H01_L.fbx", null, 550, BBGuard.Low, 0.7f, 0.3f, 0.65f, 0.4f));
                moves.Add(M("2C", "Ground Cleave", BBButton.C, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/2H/HumanF@Attack2H01.fbx", null, 750, BBGuard.Low, 0.8f, 0.3f, 0.75f, 0.45f));
                moves.Add(M("2D", "Low Armor Drive", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{HM_COMBAT}/Polearm/HumanM@AttackPolearm01.fbx", null, 800, BBGuard.Low, 0.85f, 0.35f, 0.8f, 0.5f));
                moves.Add(M("6A", "Overhead Smash", BBButton.A, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/2H/HumanM@Attack2H01.fbx", null, 650, BBGuard.Overhead, 0.75f, 1.4f, 0.65f, 0.6f));
                moves.Add(M("6B", "Step Lunge", BBButton.B, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_R.fbx", null, 700, BBGuard.Mid, 0.85f, 1.25f, 0.75f, 0.55f));
                moves.Add(M("6C", "Shield Breaker", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@Attack1H01_L.fbx", null, 850, BBGuard.Mid, 0.9f, 1.2f, 0.85f, 0.65f));
                moves.Add(M("3C", "Upward Thrust", BBButton.C, BBMoveKind.CommandNormal, BBMotion.None, $"{HF_COMBAT}/Shield/HumanF@AttackShield01.fbx", null, 800, BBGuard.Mid, 0.65f, 1.45f, 0.7f, 0.8f));
                moves.Add(M("j.A", "Aerial Strike", BBButton.A, BBMoveKind.Normal, BBMotion.None, mainFbx, s1, 320, BBGuard.Overhead, 0.65f, 0.8f, 0.6f, 0.4f));
                moves.Add(M("j.B", "Aerial Cleave", BBButton.B, BBMoveKind.Normal, BBMotion.None, mainFbx, s2, 620, BBGuard.Overhead, 0.75f, 0.8f, 0.7f, 0.45f));
                moves.Add(M("j.C", "Aerial Heavy", BBButton.C, BBMoveKind.Normal, BBMotion.None, mainFbx, s3, 880, BBGuard.Overhead, 0.85f, 0.75f, 0.85f, 0.55f));
                moves.Add(M("j.D", "Aerial Drive", BBButton.D, BBMoveKind.Normal, BBMotion.None, $"{HF_COMBAT}/Polearm/HumanF@AttackPolearm01.fbx", null, 850, BBGuard.Overhead, 0.9f, 0.75f, 0.95f, 0.55f));
                moves.Add(M("BC", "Hilt Bash Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/Shield/HumanM@AttackShield01.fbx", null, 1100, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("6BC", "Forward Stance Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HF_COMBAT}/1H/HumanF@CombatIdle1H01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("4BC", "Back Takedown", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/1H/HumanM@CombatIdle1H01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));
                moves.Add(M("j.BC", "Aerial Slam Throw", BBButton.B, BBMoveKind.Throw, BBMotion.None, $"{HM_COMBAT}/HumanM@CombatDamage01.fbx", null, 1200, BBGuard.Unblockable, 0.45f, 1.1f, 0.5f, 0.6f));

                string spec236A_fbx = $"{W2_DIR}/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX";
                string spec236A_clip = "Knight_Slash";
                string spec236B_fbx = $"{W2_DIR}/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX";
                string spec236B_clip = "Knight_Cleave";
                string spec236C_fbx = $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX";
                string spec236C_clip = "TwoHanded_Slash";
                string spec214A_fbx = $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX";
                string spec214A_clip = "TwoHanded_Sweep";
                string spec623B_fbx = $"{W3_DIR}/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX";
                string spec623B_clip = "Spearman_Lance";
                string spec22C_fbx = $"{W3_DIR}/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX";
                string spec22C_clip = "Hammer_Quake";
                string specSuper_fbx = $"{W2_DIR}/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX";
                string specSuper_clip = "TwoHanded_Slam";

                if (pfx == "Knight")
                {
                    spec236A_fbx = $"{W3_DIR}/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX";
                    spec236A_clip = "Swordsman_Slash1";
                    spec236B_fbx = $"{W3_DIR}/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX";
                    spec236B_clip = "Swordsman_Cleave";
                }
                else if (pfx == "TwoHanded")
                {
                    spec236C_fbx = $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX";
                    spec236C_clip = "Brute_Hook";
                    spec214A_fbx = $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX";
                    spec214A_clip = "Brute_Uppercut";
                    specSuper_fbx = $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX";
                    specSuper_clip = "Brute_DoubleSlam";
                }
                else if (pfx == "Spearman")
                {
                    spec623B_fbx = $"{W3_DIR}/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX";
                    spec623B_clip = "Swordsman_Thrust";
                }
                else if (pfx == "Hammer")
                {
                    spec22C_fbx = $"{W1_DIR}/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX";
                    spec22C_clip = "Brute_DoubleSlam";
                }

                moves.Add(M("236A", "Advancing Flurry", BBButton.A, BBMoveKind.Special, BBMotion.M236, spec236A_fbx, spec236A_clip, 950, BBGuard.Mid, 0.85f, 1.25f, 0.9f, 0.55f));
                moves.Add(M("236B", "Cross Cleave", BBButton.B, BBMoveKind.Special, BBMotion.M236, spec236B_fbx, spec236B_clip, 1100, BBGuard.Mid, 0.95f, 1.25f, 1.0f, 0.6f));
                moves.Add(M("236C", "Finishing Execution", BBButton.C, BBMoveKind.Special, BBMotion.M236, spec236C_fbx, spec236C_clip, 1300, BBGuard.Mid, 1.05f, 1.2f, 1.15f, 0.7f));
                moves.Add(M("214A", "Spinning Perimeter", BBButton.A, BBMoveKind.Special, BBMotion.M214, spec214A_fbx, spec214A_clip, 1050, BBGuard.Mid, 0.85f, 1.2f, 0.95f, 0.5f));
                moves.Add(M("623B", "Sky Splitter", BBButton.B, BBMoveKind.Special, BBMotion.M623, spec623B_fbx, spec623B_clip, 1200, BBGuard.Mid, 0.7f, 1.55f, 0.85f, 0.9f));
                moves.Add(M("22C", "Earth Shatter", BBButton.C, BBMoveKind.Special, BBMotion.M22, spec22C_fbx, spec22C_clip, 1150, BBGuard.Low, 0.85f, 0.5f, 0.95f, 0.7f));
                moves.Add(M("236236C", "Cataclysmic Onslaught", BBButton.C, BBMoveKind.DistortionDrive, BBMotion.M236236, specSuper_fbx, specSuper_clip, 2500, BBGuard.Mid, 1.0f, 1.25f, 1.3f, 1.3f));
            }

            return moves;
        }
    }
}
#endif

#endif
