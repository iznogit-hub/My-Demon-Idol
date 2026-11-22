#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBClipMappingTool
    {
        public const string CsvPath = "Docs/CLIP_MAPPING.csv";

        // Invoked via DemonIdolMenu
        public static void ApplyClipMapping()
        {
            Debug.Log("<color=#00e676>[BBClipMappingTool] Starting Clip Mapping & Cut Repair pass for all 25 fighters...</color>");

            // 1. First, measure impact frames if not yet measured
            BBImpactFrameMeasurer.MeasureAllAttackClips();

            var impactAsset = AssetDatabase.LoadAssetAtPath<BBClipImpactAsset>(BBImpactFrameMeasurer.AssetPath);

            // 2. Specifically apply Batch 2 Pilot Polish before scanning
            ApplyBatch2Specifics();

            // 3. Scan all BBAttackData assets and repair cuts & tiers
            int clampedCuts = 0;
            int repairedHugeMocap = 0;
            int totalMoves = 0;

            var sb = new StringBuilder();
            sb.AppendLine("Fighter,MoveId,ClipName,Pack,SourceFrames,Startup,Active,Recovery,Damage,Hitstop,Hitstun");

            var attackGuids = AssetDatabase.FindAssets("t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
            foreach (var guid in attackGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                if (attack == null) continue;

                totalMoves++;
                string fighterId = ExtractFighterId(path);

                if (attack.clip != null)
                {
                    int srcFrames = Mathf.Max(1, Mathf.RoundToInt(attack.clip.length * (attack.clip.frameRate > 0 ? attack.clip.frameRate : 30f)));

                    // Check for Huge Mocap range residue (> srcFrames + 10 or > 60 when clip is shorter)
                    if (attack.clipLastFrame > srcFrames)
                    {
                        if (attack.clipLastFrame > 80 && srcFrames <= 50)
                            repairedHugeMocap++;
                        else
                            clampedCuts++;

                        attack.clipLastFrame = srcFrames;
                        attack.clipFirstFrame = 0;
                    }

                    // Align frame data with real clip length and impact frame
                    int totalTicks = Mathf.Max(12, Mathf.RoundToInt(attack.clip.length * 60f));
                    int impactTick = Mathf.Clamp(Mathf.RoundToInt(totalTicks * 0.32f), 5, totalTicks - 4);

                    if (impactAsset != null && impactAsset.TryGetImpact(attack.clip.name, out var imp))
                    {
                        if (imp.impactFrame60 > 2 && imp.impactFrame60 < totalTicks - 2)
                        {
                            impactTick = imp.impactFrame60;
                        }
                    }

                    attack.startupFrames = impactTick;
                    attack.activeFrames = Mathf.Clamp(attack.activeFrames, 3, 5);

                    // Standardize tiers per spec 1.5
                    ApplyTierTuning(attack);

                    attack.recoveryFrames = Mathf.Max(4, totalTicks - attack.startupFrames - attack.activeFrames);

                    // Fix 5A point-blank reach and 2C sweep reach
                    if (attack.moveId == "5A" || attack.moveId == "2A")
                    {
                        attack.hitboxOffset = new Vector3(0.55f, 1.25f, 0f);
                        attack.hitboxSize = new Vector3(1.10f, 0.60f, 1.0f);
                        RepairBoxTrackPointBlank(attack);
                    }
                    else if (attack.moveId == "2C")
                    {
                        attack.hitboxOffset = new Vector3(0.65f, 0.35f, 0f);
                        attack.hitboxSize = new Vector3(1.30f, 0.50f, 1.0f);
                        RepairBoxTrackPointBlank(attack);
                    }

                    // Synchronize BoxTrack tickCount and frameStart with attack.TotalTicks
                    SyncBoxTrack(attack);

                    EditorUtility.SetDirty(attack);

                    string packName = IdentifyPack(attack.clip);
                    sb.AppendLine($"{fighterId},{attack.moveId},{attack.clip.name},{packName},{srcFrames},{attack.startupFrames},{attack.activeFrames},{attack.recoveryFrames},{attack.damage},{attack.hitstopAttacker},{attack.hitstunTicks}");
                }
            }

            // 4. Save CSV
            string docsDir = Path.GetDirectoryName(CsvPath);
            if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);
            File.WriteAllText(CsvPath, sb.ToString());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBClipMappingTool] Complete! Clamped {clampedCuts} frozen cuts, cleared {repairedHugeMocap} Huge Mocap ranges across {totalMoves} move slots. Wrote {CsvPath}</color>");
        }

        private static void ApplyTierTuning(BBAttackData atk)
        {
            if (atk.kind == BBMoveKind.DistortionDrive || atk.moveId == "236236C")
            {
                atk.startupFrames = Mathf.Clamp(atk.startupFrames, 16, 24);
                atk.damage = Mathf.Clamp(atk.damage, 2000, 3500);
                if (atk.damage < 2000) atk.damage = 2500;
                atk.hitstopAttacker = 16;
                atk.hitstopDefender = 16;
                atk.hitstunTicks = 32;
                atk.causesKnockdown = true;
                atk.hardKnockdown = true;
            }
            else if (atk.kind == BBMoveKind.Special || atk.moveId.StartsWith("236") || atk.moveId.StartsWith("214") || atk.moveId.StartsWith("623") || atk.moveId.StartsWith("22"))
            {
                atk.startupFrames = Mathf.Clamp(atk.startupFrames, 12, 18);
                atk.damage = Mathf.Clamp(atk.damage, 900, 1300);
                if (atk.damage < 900) atk.damage = 1000;
                atk.hitstopAttacker = 10;
                atk.hitstopDefender = 10;
                atk.hitstunTicks = 18;
            }
            else if (atk.moveId.Contains("D"))
            {
                atk.damage = Mathf.Clamp(atk.damage, 800, 1000);
                if (atk.damage < 800) atk.damage = 850;
                atk.hitstopAttacker = 10;
                atk.hitstopDefender = 10;
                atk.hitstunTicks = 18;
            }
            else if (atk.moveId.Contains("C"))
            {
                atk.startupFrames = Mathf.Clamp(atk.startupFrames, 12, 16);
                atk.damage = Mathf.Clamp(atk.damage, 850, 1100);
                if (atk.damage < 850) atk.damage = 900;
                atk.hitstopAttacker = 12;
                atk.hitstopDefender = 12;
                atk.hitstunTicks = 22;
                atk.screenShakeTrauma = 0.40f; // Screen shake trauma 0.4 per spec
            }
            else if (atk.moveId.Contains("B"))
            {
                atk.startupFrames = Mathf.Clamp(atk.startupFrames, 8, 11);
                atk.damage = Mathf.Clamp(atk.damage, 500, 650);
                if (atk.damage < 500) atk.damage = 600;
                atk.hitstopAttacker = 9;
                atk.hitstopDefender = 9;
                atk.hitstunTicks = 16;
                atk.screenShakeTrauma = 0.10f; // Light screen shake per spec
            }
            else if (atk.moveId.Contains("A"))
            {
                atk.startupFrames = Mathf.Clamp(atk.startupFrames, 5, 7);
                atk.damage = Mathf.Clamp(atk.damage, 250, 350);
                if (atk.damage < 250) atk.damage = 300;
                atk.hitstopAttacker = 6;
                atk.hitstopDefender = 6;
                atk.hitstunTicks = 12;
            }
        }

        private static void SyncBoxTrack(BBAttackData atk)
        {
            if (atk.boxes == null) return;
            int totalTicks = atk.TotalTicks;
            if (totalTicks <= 0) totalTicks = 1;

            atk.boxes.tickCount = totalTicks;

            BBBox hitBox = new BBBox(
                atk.hitboxOffset.x > 0 ? atk.hitboxOffset.x : 0.55f,
                atk.hitboxOffset.y > 0 ? atk.hitboxOffset.y : 1.25f,
                atk.hitboxSize.x > 0 ? atk.hitboxSize.x * 0.5f : 0.55f,
                atk.hitboxSize.y > 0 ? atk.hitboxSize.y * 0.5f : 0.30f,
                BBBoxKind.Hit,
                0,
                BBAttribute.Body
            );

            if (atk.boxes.boxes != null)
            {
                for (int i = 0; i < atk.boxes.boxes.Length; i++)
                {
                    if (atk.boxes.boxes[i].kind == BBBoxKind.Hit)
                    {
                        hitBox = atk.boxes.boxes[i];
                        break;
                    }
                }
            }

            if (atk.moveId == "5A" || atk.moveId == "2A" || atk.moveId == "2C")
            {
                hitBox.cx = atk.hitboxOffset.x;
                hitBox.hx = atk.hitboxSize.x * 0.5f;
                hitBox.cy = atk.hitboxOffset.y;
                hitBox.hy = atk.hitboxSize.y * 0.5f;
            }

            var boxList = new List<BBBox>();
            atk.boxes.frameStart = new int[totalTicks + 1];
            int currentBoxOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                atk.boxes.frameStart[t] = currentBoxOffset;

                // Hitbox active during active frames
                if (t >= atk.startupFrames && t < atk.startupFrames + atk.activeFrames)
                {
                    boxList.Add(hitBox);
                    currentBoxOffset++;
                }
            }

            atk.boxes.frameStart[totalTicks] = currentBoxOffset;
            atk.boxes.boxes = boxList.ToArray();
            atk.boxes.keyTicks = new int[] { atk.startupFrames };

            EditorUtility.SetDirty(atk.boxes);
        }

        private static void RepairBoxTrackPointBlank(BBAttackData atk)
        {
            if (atk.boxes == null) return;
            var boxes = atk.boxes.boxes;
            if (boxes == null || boxes.Length == 0) return;

            bool modified = false;
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i].kind == BBBoxKind.Hit)
                {
                    // Extend box to reach point-blank (starts at 0.0m)
                    boxes[i].cx = 0.55f;
                    boxes[i].hx = 0.55f;
                    modified = true;
                }
            }

            if (modified)
            {
                atk.boxes.boxes = boxes;
                EditorUtility.SetDirty(atk.boxes);
            }
        }

        private static void ApplyBatch2Specifics()
        {
            // 1. C07 StellarWitch
            ApplyC07Specifics();

            // 2. C08 SanpakuChan
            ApplyC08Specifics();

            // 3. C09 PicoChan
            ApplyC09Specifics();

            // 4. C10 Reika
            ApplyC10Specifics();

            // 5. C11 Fina (Casual1)
            ApplyC11Specifics();
        }

        private static void ApplyC07Specifics()
        {
            // Super on C07 uses HumanF@MagicAttackDirect2H01 (48 frames) - biggest spell clip
            var superAtk = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/StellarWitch/BB_StellarWitch_236236C.asset");
            var superClip = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", "HumanF@MagicAttackDirect2H01");
            if (superAtk != null && superClip != null)
            {
                superAtk.clip = superClip;
                superAtk.clipName = superClip.name;
                superAtk.clipFirstFrame = 0;
                superAtk.clipLastFrame = 48;
                superAtk.damage = 2600;
                superAtk.hitstopAttacker = 14;
                superAtk.hitstopDefender = 14;
                superAtk.hitstunTicks = 32;
                EditorUtility.SetDirty(superAtk);
            }

            // 236B on C07 uses HumanF@MagicAttackCall1H01_L (41 frames)
            var b236 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/StellarWitch/BB_StellarWitch_236B.asset");
            var clip236B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", "HumanF@MagicAttackCall1H01_L");
            if (b236 != null && clip236B != null)
            {
                b236.clip = clip236B;
                b236.clipName = clip236B.name;
                b236.clipFirstFrame = 0;
                b236.clipLastFrame = 41;
                b236.damage = 1050;
                EditorUtility.SetDirty(b236);
            }

            // 236C on C07 uses HumanF@MagicAttackOmni01 - Cast (39 frames)
            var c236 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/StellarWitch/BB_StellarWitch_236C.asset");
            var clip236C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01 - Cast.fbx", "HumanF@MagicAttackOmni01 - Cast");
            if (c236 != null && clip236C != null)
            {
                c236.clip = clip236C;
                c236.clipName = clip236C.name;
                c236.clipFirstFrame = 0;
                c236.clipLastFrame = 39;
                c236.damage = 1150;
                EditorUtility.SetDirty(c236);
            }

            // Override controller update for C07: real jump clip, real combat idle
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>("Assets/MDI_Characters/Runtime/BlazBlue/StellarWitch_BB_Override.overrideController");
            if (overrideCtrl != null)
            {
                var jumpClip = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Jump.FBX", "Jump");
                var idleClip = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx", "HumanF@CombatIdle01");

                var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideCtrl.GetOverrides(list);
                var updated = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                foreach (var pair in list)
                {
                    if (pair.Key != null && pair.Key.name.Contains("Jump") && jumpClip != null)
                    {
                        updated.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, jumpClip));
                    }
                    else if (pair.Key != null && pair.Key.name.Contains("CombatIdle01") && idleClip != null)
                    {
                        updated.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, idleClip));
                    }
                    else
                    {
                        updated.Add(pair);
                    }
                }
                overrideCtrl.ApplyOverrides(updated);
                EditorUtility.SetDirty(overrideCtrl);
            }
        }

        private static void ApplyC08Specifics()
        {
            // Replace Quickstep_F on 236A
            var atk236 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/SanpakuChan/BB_SanpakuChan_236A.asset");
            var clip236 = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1");
            if (atk236 != null && clip236 != null)
            {
                atk236.clip = clip236;
                atk236.clipName = clip236.name;
                atk236.clipFirstFrame = 0;
                atk236.clipLastFrame = 25;
                EditorUtility.SetDirty(atk236);
            }

            // Replace Dodge_F on j.B
            var atkjB = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/SanpakuChan/BB_SanpakuChan_j.B.asset");
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);
            if (atkjB != null && clipjB != null)
            {
                atkjB.clip = clipjB;
                atkjB.clipName = clipjB.name;
                atkjB.clipFirstFrame = 0;
                atkjB.clipLastFrame = 20;
                EditorUtility.SetDirty(atkjB);
            }
        }

        private static void ApplyC09Specifics()
        {
            // Replace Grab_Hit_A on 5D
            var atk5D = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/PicoChan/BB_PicoChan_5D.asset");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Attack1");
            if (atk5D != null && clip5D != null)
            {
                atk5D.clip = clip5D;
                atk5D.clipName = clip5D.name;
                atk5D.clipFirstFrame = 0;
                atk5D.clipLastFrame = 35;
                EditorUtility.SetDirty(atk5D);
            }
        }

        private static void ApplyC10Specifics()
        {
            // Ensure 5A reaches point-blank and real jump clip
            var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>("Assets/MDI_Characters/Runtime/BlazBlue/Reika_BB_Override.overrideController");
            if (overrideCtrl != null)
            {
                var jumpClip = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Jump.FBX", "Jump");
                if (jumpClip != null)
                {
                    var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideCtrl.GetOverrides(list);
                    var updated = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    foreach (var pair in list)
                    {
                        if (pair.Key != null && pair.Key.name.Contains("CastingEnter01"))
                            updated.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, jumpClip));
                        else
                            updated.Add(pair);
                    }
                    overrideCtrl.ApplyOverrides(updated);
                    EditorUtility.SetDirty(overrideCtrl);
                }
            }

            var atkjB = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Reika/BB_Reika_j.B.asset");
            if (atkjB != null)
            {
                atkjB.hitboxOffset = new Vector3(1.05f, -0.2f, 0f);
                atkjB.hitboxSize = new Vector3(1.2f, 1.0f, 1f);
                EditorUtility.SetDirty(atkjB);
            }
        }

        private static void ApplyC11Specifics()
        {
            // Replace Quickstep_F on 236A
            var atk236 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Casual1/BB_Casual1_236A.asset");
            var clip236 = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Attack1");
            if (atk236 != null && clip236 != null)
            {
                atk236.clip = clip236;
                atk236.clipName = clip236.name;
                atk236.clipFirstFrame = 0;
                atk236.clipLastFrame = 30;
                EditorUtility.SetDirty(atk236);
            }

            // Replace Dodge_F on j.B
            var atkjB = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Casual1/BB_Casual1_j.B.asset");
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);
            if (atkjB != null && clipjB != null)
            {
                atkjB.clip = clipjB;
                atkjB.clipName = clipjB.name;
                atkjB.clipFirstFrame = 0;
                atkjB.clipLastFrame = 20;
                EditorUtility.SetDirty(atkjB);
            }

            var atkFina236 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Fina/BB_Fina_236A.asset");
            if (atkFina236 != null && clip236 != null)
            {
                atkFina236.clip = clip236;
                atkFina236.clipName = clip236.name;
                atkFina236.clipFirstFrame = 0;
                atkFina236.clipLastFrame = 30;
                EditorUtility.SetDirty(atkFina236);
            }

            var atkFinajB = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Fina/BB_Fina_j.B.asset");
            if (atkFinajB != null && clipjB != null)
            {
                atkFinajB.clip = clipjB;
                atkFinajB.clipName = clipjB.name;
                atkFinajB.clipFirstFrame = 0;
                atkFinajB.clipLastFrame = 20;
                EditorUtility.SetDirty(atkFinajB);
            }
        }

        private static AnimationClip LoadSubclip(string path, string subclipName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null) return null;
            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
            if (clips.Count == 0) return null;
            if (string.IsNullOrEmpty(subclipName)) return clips[0];
            return clips.FirstOrDefault(c => string.Equals(c.name, subclipName, StringComparison.OrdinalIgnoreCase)) ?? clips[0];
        }

        private static string ExtractFighterId(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            var parts = fileName.Split('_');
            if (parts.Length >= 2) return parts[1];
            return fileName;
        }

        private static string IdentifyPack(AnimationClip clip)
        {
            string assetPath = AssetDatabase.GetAssetPath(clip);
            if (assetPath.Contains("Warrior Pack Bundle 1")) return "Warrior Bundle 1";
            if (assetPath.Contains("Warrior Pack Bundle 2")) return "Warrior Bundle 2";
            if (assetPath.Contains("Warrior Pack Bundle 3")) return "Warrior Bundle 3";
            if (assetPath.Contains("Fighter Pack Bundle")) return "Fighter Pack Bundle";
            if (assetPath.Contains("Dwarf Animations")) return "Dwarf Animations";
            if (assetPath.Contains("Martial Art Animations")) return "Martial Art Animations";
            if (assetPath.Contains("Human Animations")) return "Human Animations";
            return "Roster Bundled";
        }
    }
}
#endif
