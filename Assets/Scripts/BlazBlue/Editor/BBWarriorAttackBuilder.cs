using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Phase 3: Attack Animation & Hitbox Integration.
    /// Slices and authors the authentic warrior @Attack1.FBX clips into character movesets
    /// as signature 5C weapon attacks, and bakes calibrated 60fps bounding box tracks.
    /// </summary>
    public static class BBWarriorAttackBuilder
    {
        public class WarriorAttackSpec
        {
            public string CharacterId;
            public string CharacterName;
            public string WarriorName;
            public string AttackName;
            public string MoveId;
            public string FbxPath;
            public int FirstFrame;
            public int LastFrame;
            public Vector2 HitBoxCenter;
            public Vector2 HitBoxSize;
            public int Startup;
            public int Active;
            public int Recovery;
            public float Damage;
            public BBGuard Guard;
            public int AdvOnBlock;
            public int Hitstun;
            public int Blockstun;
        }

        public static readonly WarriorAttackSpec[] SPECS = new[]
        {
            // C01 Akio -> Swordsman
            new WarriorAttackSpec
            {
                CharacterId = "C01",
                CharacterName = "Akio",
                WarriorName = "Swordsman",
                AttackName = "Swordsman Slash",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 35,
                HitBoxCenter = new Vector2(0.75f, 1.15f),
                HitBoxSize = new Vector2(0.85f, 0.50f),
                Startup = 12,
                Active = 4,
                Recovery = 16,
                Damage = 900,
                Guard = BBGuard.Mid,
                AdvOnBlock = -6,
                Hitstun = 20,
                Blockstun = 14
            },
            // C02 Aika -> Karate
            new WarriorAttackSpec
            {
                CharacterId = "C02",
                CharacterName = "Aika",
                WarriorName = "Karate",
                AttackName = "Karate Thrust",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 34,
                HitBoxCenter = new Vector2(0.60f, 1.20f),
                HitBoxSize = new Vector2(0.65f, 0.40f),
                Startup = 12,
                Active = 4,
                Recovery = 16,
                Damage = 820,
                Guard = BBGuard.Mid,
                AdvOnBlock = -7,
                Hitstun = 20,
                Blockstun = 14
            },
            // C04 UnityChan -> Archer
            new WarriorAttackSpec
            {
                CharacterId = "C04",
                CharacterName = "UnityChan",
                WarriorName = "Archer",
                AttackName = "Archer Shot",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 30,
                HitBoxCenter = new Vector2(0.85f, 1.15f),
                HitBoxSize = new Vector2(0.90f, 0.45f),
                Startup = 11,
                Active = 4,
                Recovery = 14,
                Damage = 650,
                Guard = BBGuard.Mid,
                AdvOnBlock = -5,
                Hitstun = 18,
                Blockstun = 13
            },
            // C05 Akane -> Spearman
            new WarriorAttackSpec
            {
                CharacterId = "C05",
                CharacterName = "Akane",
                WarriorName = "Spearman",
                AttackName = "Spear Thrust",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 34,
                HitBoxCenter = new Vector2(1.10f, 1.15f),
                HitBoxSize = new Vector2(1.20f, 0.40f),
                Startup = 13,
                Active = 4,
                Recovery = 17,
                Damage = 850,
                Guard = BBGuard.Mid,
                AdvOnBlock = -7,
                Hitstun = 21,
                Blockstun = 14
            },
            // C06 Cultist -> Mage
            new WarriorAttackSpec
            {
                CharacterId = "C06",
                CharacterName = "Cultist",
                WarriorName = "Mage",
                AttackName = "Mage Staff Strike",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 40,
                HitBoxCenter = new Vector2(0.80f, 1.25f),
                HitBoxSize = new Vector2(0.90f, 0.55f),
                Startup = 11,
                Active = 4,
                Recovery = 16,
                Damage = 750,
                Guard = BBGuard.Mid,
                AdvOnBlock = -5,
                Hitstun = 19,
                Blockstun = 14
            },
            // C07 StellarWitch -> Sorceress
            new WarriorAttackSpec
            {
                CharacterId = "C07",
                CharacterName = "StellarWitch",
                WarriorName = "Sorceress",
                AttackName = "Sorceress Wave",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 40,
                HitBoxCenter = new Vector2(0.75f, 1.20f),
                HitBoxSize = new Vector2(0.85f, 0.60f),
                Startup = 14,
                Active = 5,
                Recovery = 18,
                Damage = 920,
                Guard = BBGuard.Mid,
                AdvOnBlock = -4,
                Hitstun = 22,
                Blockstun = 15
            },
            // C08 SanpakuChan -> Ninja
            new WarriorAttackSpec
            {
                CharacterId = "C08",
                CharacterName = "SanpakuChan",
                WarriorName = "Ninja",
                AttackName = "Twin Ninjato Slice",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 26,
                HitBoxCenter = new Vector2(0.75f, 1.15f),
                HitBoxSize = new Vector2(0.85f, 0.50f),
                Startup = 9,
                Active = 4,
                Recovery = 13,
                Damage = 650,
                Guard = BBGuard.Mid,
                AdvOnBlock = -4,
                Hitstun = 18,
                Blockstun = 13
            },
            // C09 PicoChan -> Hammer
            new WarriorAttackSpec
            {
                CharacterId = "C09",
                CharacterName = "PicoChan",
                WarriorName = "Hammer",
                AttackName = "Warhammer Smash",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 52,
                HitBoxCenter = new Vector2(0.80f, 1.05f),
                HitBoxSize = new Vector2(0.95f, 0.70f),
                Startup = 11,
                Active = 4,
                Recovery = 14,
                Damage = 800,
                Guard = BBGuard.Mid,
                AdvOnBlock = -5,
                Hitstun = 20,
                Blockstun = 14
            },
            // C10 Reika -> Knight
            new WarriorAttackSpec
            {
                CharacterId = "C10",
                CharacterName = "Reika",
                WarriorName = "Knight",
                AttackName = "Knight Blade Cleave",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 25,
                HitBoxCenter = new Vector2(0.75f, 1.15f),
                HitBoxSize = new Vector2(0.85f, 0.55f),
                Startup = 11,
                Active = 4,
                Recovery = 18,
                Damage = 780,
                Guard = BBGuard.Mid,
                AdvOnBlock = -6,
                Hitstun = 20,
                Blockstun = 14
            },
            // C11 Casual1 -> Crossbow
            new WarriorAttackSpec
            {
                CharacterId = "C11",
                CharacterName = "Casual1",
                WarriorName = "Crossbow",
                AttackName = "Crossbow Burst",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 34,
                HitBoxCenter = new Vector2(0.90f, 1.15f),
                HitBoxSize = new Vector2(1.00f, 0.40f),
                Startup = 12,
                Active = 4,
                Recovery = 16,
                Damage = 750,
                Guard = BBGuard.Mid,
                AdvOnBlock = -5,
                Hitstun = 19,
                Blockstun = 14
            },
            // C12 Fina -> TwoHanded
            new WarriorAttackSpec
            {
                CharacterId = "C12",
                CharacterName = "Fina",
                WarriorName = "2Handed",
                AttackName = "Greatsword Cleave",
                MoveId = "5C",
                FbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX",
                FirstFrame = 0,
                LastFrame = 40,
                HitBoxCenter = new Vector2(0.95f, 1.20f),
                HitBoxSize = new Vector2(1.10f, 0.70f),
                Startup = 10,
                Active = 4,
                Recovery = 14,
                Damage = 780,
                Guard = BBGuard.Mid,
                AdvOnBlock = -4,
                Hitstun = 20,
                Blockstun = 14
            }
        };

        [MenuItem("Demon Idol/BlazBlue/Execute Phase 3 (Warrior Attack and BoxTrack Alignment)")]
        public static void ExecutePhase3()
        {
            Debug.Log("================================================================");
            Debug.Log("[BBWarriorAttackBuilder] Starting Phase 3 Execution...");
            Debug.Log("================================================================");

            int successCount = 0;
            int warningCount = 0;

            foreach (var s in SPECS)
            {
                try
                {
                    bool ok = ProcessAttackSpec(s);
                    if (ok) successCount++;
                    else warningCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBWarriorAttackBuilder] Exception for {s.CharacterId} ({s.CharacterName}): {ex.Message}\n{ex.StackTrace}");
                    warningCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("================================================================");
            Debug.Log($"[BBWarriorAttackBuilder] Phase 3 Finished. Success: {successCount}/{SPECS.Length}, Warnings: {warningCount}");
            Debug.Log("================================================================");
        }

        private static bool ProcessAttackSpec(WarriorAttackSpec s)
        {
            string charCombatDir = $"Assets/CombatData/BlazBlue/{s.CharacterName}";
            string boxDir = $"{charCombatDir}/BoxTracks";

            if (!Directory.Exists(charCombatDir))
            {
                Directory.CreateDirectory(charCombatDir);
            }
            if (!Directory.Exists(boxDir))
            {
                Directory.CreateDirectory(boxDir);
            }

            string attackPath = $"{charCombatDir}/BB_{s.CharacterName}_{s.MoveId}.asset";
            string boxTrackPath = $"{boxDir}/BoxTrack_{s.CharacterName}_{s.MoveId}.asset";

            var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(attackPath);
            if (attack == null)
            {
                attack = ScriptableObject.CreateInstance<BBAttackData>();
                AssetDatabase.CreateAsset(attack, attackPath);
                Debug.Log($"[BBWarriorAttackBuilder] Created BBAttackData: {attackPath}");
            }

            var track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(boxTrackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<BBBoxTrack>();
                AssetDatabase.CreateAsset(track, boxTrackPath);
                Debug.Log($"[BBWarriorAttackBuilder] Created BBBoxTrack: {boxTrackPath}");
            }

            // 1. Load authentic warrior AnimationClip
            AnimationClip attackClip = LoadWarriorClip(s.FbxPath);
            if (attackClip == null)
            {
                Debug.LogWarning($"[BBWarriorAttackBuilder] Could not load clip from {s.FbxPath}");
                return false;
            }

            // 2. Configure AttackData
            attack.attackName = s.AttackName;
            attack.moveId = s.MoveId;
            attack.button = BBButton.C;
            attack.kind = BBMoveKind.Normal;
            attack.motion = BBMotion.None;
            attack.startupFrames = s.Startup;
            attack.activeFrames = s.Active;
            attack.recoveryFrames = s.Recovery;
            attack.damage = s.Damage;
            attack.guard = s.Guard;
            attack.advantageOnBlock = s.AdvOnBlock;
            attack.hitstunTicks = s.Hitstun;
            attack.blockstunTicks = s.Blockstun;
            attack.airOk = false;
            attack.groundOk = true;
            attack.fatalCounter = true;
            attack.specialCancel = true;
            attack.ddCancel = true;
            attack.rapidCancelable = true;
            attack.clip = attackClip;
            attack.animationClip = attackClip;
            attack.clipName = $"{s.CharacterName}_{s.MoveId}";
            attack.clipFirstFrame = s.FirstFrame;
            attack.clipLastFrame = s.LastFrame;
            attack.boxes = track;

            // 3. Bake BoxTrack
            int totalTicks = s.Startup + s.Active + s.Recovery;
            track.tickCount = totalTicks;
            track.frameStart = new int[totalTicks + 1];

            var boxList = new List<BBBox>();
            int currentOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentOffset;

                // Active hitbox window
                if (t >= s.Startup && t < s.Startup + s.Active)
                {
                    boxList.Add(new BBBox(
                        s.HitBoxCenter.x,
                        s.HitBoxCenter.y,
                        s.HitBoxSize.x,
                        s.HitBoxSize.y,
                        BBBoxKind.Hit,
                        0,
                        BBAttribute.Body
                    ));
                    currentOffset++;
                }
            }

            track.frameStart[totalTicks] = currentOffset;
            track.boxes = boxList.ToArray();
            track.bakeHash = $"Bake_{s.CharacterName}_{s.MoveId}_{s.Startup}_{s.Active}_{s.Recovery}";

            EditorUtility.SetDirty(track);
            EditorUtility.SetDirty(attack);

            Debug.Log($"  {s.CharacterId} {s.CharacterName} ({s.WarriorName}): 5C mapped to {attackClip.name} (f{s.FirstFrame}-{s.LastFrame}), Box reach: Center {s.HitBoxCenter}, Size {s.HitBoxSize}, TotalTicks: {totalTicks}");
            return true;
        }

        private static AnimationClip LoadWarriorClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets != null && assets.Length > 0)
            {
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        return clip;
                    }
                }
            }
            return null;
        }
    }
}
