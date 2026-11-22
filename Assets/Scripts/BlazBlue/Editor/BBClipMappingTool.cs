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

            // 2. Scan all BBAttackData assets and repair cuts & tiers
            int clampedCuts = 0;
            int repairedHugeMocap = 0;
            int totalMoves = 0;

            var attackGuids = AssetDatabase.FindAssets("t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
            foreach (var guid in attackGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                if (attack == null) continue;

                totalMoves++;

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

                    if (attack.moveId == "5A" || attack.moveId == "2A")
                    {
                        attack.hitboxOffset = new Vector3(0.65f, 1.25f, 0f);
                        attack.hitboxSize = new Vector3(1.30f, 0.60f, 1.0f);
                    }
                    else if (attack.moveId == "2C")
                    {
                        attack.hitboxOffset = new Vector3(0.85f, 0.35f, 0f);
                        attack.hitboxSize = new Vector3(1.40f, 0.50f, 1.0f);
                    }

                    SyncBoxTrack(attack);
                    EditorUtility.SetDirty(attack);
                }
            }

            // 3. Specifically apply Batch Polish overrides
            ApplyBatch1Specifics();
            ApplyBatch2Specifics();
            ApplyBatch3Specifics();
            ApplyBatch4Specifics();
            ApplyBatch5Specifics();

            // 4. Save CSV
            var sb = new StringBuilder();
            sb.AppendLine("Fighter,MoveId,ClipName,Pack,SourceFrames,Startup,Active,Recovery,Damage,Hitstop,Hitstun");
            foreach (var guid in attackGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                if (attack == null || attack.clip == null) continue;

                string fighterId = ExtractFighterId(path);
                int srcFrames = Mathf.Max(1, Mathf.RoundToInt(attack.clip.length * (attack.clip.frameRate > 0 ? attack.clip.frameRate : 30f)));
                string packName = IdentifyPack(attack.clip);
                sb.AppendLine($"{fighterId},{attack.moveId},{attack.clip.name},{packName},{srcFrames},{attack.startupFrames},{attack.activeFrames},{attack.recoveryFrames},{attack.damage},{attack.hitstopAttacker},{attack.hitstunTicks}");
            }

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
                atk.screenShakeTrauma = 0.40f;
            }
            else if (atk.moveId.Contains("B"))
            {
                atk.startupFrames = Mathf.Clamp(atk.startupFrames, 8, 11);
                atk.damage = Mathf.Clamp(atk.damage, 500, 650);
                if (atk.damage < 500) atk.damage = 600;
                atk.hitstopAttacker = 9;
                atk.hitstopDefender = 9;
                atk.hitstunTicks = 16;
                atk.screenShakeTrauma = 0.10f;
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

        private static void TuneMove(string path, AnimationClip clip, int startup, int active, int recovery, float damage, Vector3 offset, Vector3 size, int hitstop = 10, int hitstun = 18, bool knockdown = false, bool hardKnockdown = false)
        {
            var atk = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
            if (atk == null) return;
            if (clip != null)
            {
                atk.clip = clip;
                atk.clipName = clip.name;
                atk.clipFirstFrame = 0;
                atk.clipLastFrame = Mathf.Max(1, Mathf.RoundToInt(clip.length * (clip.frameRate > 0 ? clip.frameRate : 30f)));
            }
            if (startup > 0) atk.startupFrames = startup;
            if (active > 0) atk.activeFrames = active;
            if (recovery > 0) atk.recoveryFrames = recovery;
            if (damage > 0) atk.damage = damage;
            if (offset != Vector3.zero || size != Vector3.zero)
            {
                atk.hitboxOffset = offset;
                atk.hitboxSize = size;
            }
            atk.hitstopAttacker = hitstop;
            atk.hitstopDefender = hitstop;
            atk.hitstunTicks = hitstun;
            if (knockdown)
            {
                atk.causesKnockdown = true;
                if (hardKnockdown || damage >= 2000) atk.hardKnockdown = true;
            }
            SyncBoxTrack(atk);
            EditorUtility.SetDirty(atk);
        }

        private static void SyncBoxTrack(BBAttackData atk)
        {
            if (atk.boxes == null) return;
            int totalTicks = atk.TotalTicks;
            if (totalTicks <= 0) totalTicks = 1;

            atk.boxes.tickCount = totalTicks;

            BBBox hitBox = new BBBox(
                atk.hitboxOffset.x > 0 ? atk.hitboxOffset.x : 0.65f,
                atk.hitboxOffset.y != 0 ? atk.hitboxOffset.y : 1.25f,
                atk.hitboxSize.x > 0 ? atk.hitboxSize.x * 0.5f : 0.65f,
                atk.hitboxSize.y > 0 ? atk.hitboxSize.y * 0.5f : 0.30f,
                BBBoxKind.Hit,
                0,
                BBAttribute.Body
            );

            if (atk.hitboxSize.x > 0)
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

        public static void ApplyBatch1Specifics()
        {
            ApplyC01Specifics();
            ApplyC02Specifics();
            ApplyC04Specifics();
            ApplyC05Specifics();
            ApplyC06Specifics();
        }

        private static void ApplyC01Specifics()
        {
            // Unarmed Brawler - Fixed reach on all normals, unique frame data
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_5A.asset", null, 5, 3, 8, 280, new Vector3(0.65f, 1.25f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_5B.asset", null, 8, 3, 12, 550, new Vector3(0.80f, 1.20f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_5C.asset", null, 12, 4, 16, 950, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_2A.asset", null, 6, 3, 9, 260, new Vector3(0.65f, 0.40f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_2B.asset", null, 9, 3, 13, 520, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_2C.asset", null, 13, 4, 18, 900, new Vector3(0.95f, 0.35f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_5D.asset", null, 14, 4, 16, 850, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_236A.asset", null, 12, 4, 16, 1050, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_214B.asset", null, 14, 4, 18, 1150, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_236236C.asset", null, 18, 5, 28, 2500, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_BC.asset", null, 6, 3, 14, 600, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_j.A.asset", null, 6, 3, 10, 280, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_j.B.asset", null, 9, 3, 12, 550, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Akio/BB_Akio_j.C.asset", null, 13, 4, 16, 950, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyC02Specifics()
        {
            // Agile Female Striker - Real female attacks replacing Dodge/Quickstep
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null);
            var clipjA = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", null);
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);
            var clipjC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_5A.asset", null, 5, 3, 7, 270, new Vector3(0.65f, 1.25f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_5B.asset", null, 8, 3, 11, 540, new Vector3(0.80f, 1.20f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_5C.asset", null, 11, 4, 15, 920, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_2A.asset", null, 5, 3, 8, 250, new Vector3(0.65f, 0.40f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_2B.asset", null, 8, 3, 12, 510, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_2C.asset", null, 12, 4, 17, 880, new Vector3(0.95f, 0.35f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_5D.asset", null, 13, 4, 15, 830, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_C02_Aika_236A.asset", clip236A, 11, 4, 15, 1020, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_236A.asset", clip236A, 11, 4, 15, 1020, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_214B.asset", null, 13, 4, 17, 1120, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_236236C.asset", null, 17, 5, 26, 2400, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_BC.asset", null, 6, 3, 14, 580, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_j.A.asset", clipjA, 5, 3, 9, 270, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_j.B.asset", clipjB, 8, 3, 11, 530, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Aika/BB_Aika_j.C.asset", clipjC, 12, 4, 15, 930, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyC04Specifics()
        {
            // UnityChan: Own clips first, eliminate reused Punch
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null);
            var clipjA = LoadSubclip("Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_HANDUP00_R.fbx", null);
            var clip236A = LoadSubclip("Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_SLIDE00.fbx", null);
            var clip214B = LoadSubclip("Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_UMATOBI00.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_5A.asset", clip5A, 5, 3, 8, 260, new Vector3(0.65f, 1.25f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_5B.asset", null, 7, 3, 11, 530, new Vector3(0.80f, 1.20f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_5C.asset", null, 12, 4, 16, 910, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_2A.asset", null, 6, 3, 8, 240, new Vector3(0.65f, 0.40f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_2B.asset", null, 8, 3, 12, 500, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_2C.asset", null, 13, 4, 18, 870, new Vector3(0.95f, 0.35f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_5D.asset", null, 15, 4, 17, 860, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_236A.asset", clip236A, 13, 4, 16, 1040, new Vector3(0.95f, 0.50f, 0f), new Vector3(1.50f, 0.80f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_214B.asset", clip214B, 15, 4, 18, 1140, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.80f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_236236C.asset", null, 18, 5, 27, 2450, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_BC.asset", null, 6, 3, 14, 590, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_j.A.asset", clipjA, 6, 3, 9, 260, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_j.B.asset", null, 8, 3, 11, 520, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/UnityChan/BB_UnityChan_j.C.asset", null, 13, 4, 16, 920, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyC05Specifics()
        {
            // Akane: Spear Master - extended spear reach on hitboxes
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_5A.asset", null, 6, 3, 10, 300, new Vector3(0.80f, 1.25f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_5B.asset", null, 9, 3, 14, 580, new Vector3(1.10f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_5C.asset", null, 14, 4, 18, 1000, new Vector3(1.25f, 1.20f, 0f), new Vector3(1.80f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_2A.asset", null, 7, 3, 10, 280, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_2B.asset", null, 10, 3, 14, 550, new Vector3(1.10f, 0.40f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_2C.asset", null, 14, 4, 20, 950, new Vector3(1.25f, 0.35f, 0f), new Vector3(1.80f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_5D.asset", null, 16, 4, 18, 890, new Vector3(1.20f, 1.10f, 0f), new Vector3(1.70f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_236A.asset", null, 14, 4, 18, 1080, new Vector3(1.20f, 1.20f, 0f), new Vector3(1.70f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_214B.asset", null, 16, 4, 20, 1180, new Vector3(1.20f, 1.20f, 0f), new Vector3(1.70f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_236236C.asset", null, 20, 5, 30, 2550, new Vector3(1.30f, 1.25f, 0f), new Vector3(2.00f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_BC.asset", null, 7, 3, 15, 620, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_j.A.asset", null, 7, 3, 11, 300, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_j.B.asset", null, 10, 3, 14, 580, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Archetypes/spear/Moves_C05/BBMove_C05_j.C.asset", null, 14, 4, 18, 1000, new Vector3(1.25f, -0.20f, 0f), new Vector3(1.80f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyC06Specifics()
        {
            // Cultist: Male spell clips only (HumanM@), fixed reach
            var clip236A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L - Cast.fbx", null);
            var clip214B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01 - Cast.fbx", null);
            var clip236236C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null);
            var clip214A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Cast.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_5A.asset", null, 6, 3, 9, 290, new Vector3(0.65f, 1.25f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_5B.asset", null, 9, 3, 13, 560, new Vector3(0.80f, 1.20f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_5C.asset", null, 13, 4, 17, 980, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_2A.asset", null, 6, 3, 9, 270, new Vector3(0.65f, 0.40f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_2B.asset", null, 9, 3, 13, 540, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_2C.asset", null, 14, 4, 19, 920, new Vector3(0.95f, 0.35f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_5D.asset", null, 15, 4, 17, 880, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_C06_Cultist_236A.asset", clip236A, 13, 4, 17, 1060, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.80f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_236A.asset", clip236A, 13, 4, 17, 1060, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.80f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_214B.asset", clip214B, 15, 4, 19, 1160, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.80f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_214A.asset", clip214A, 14, 4, 18, 1080, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.80f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_236236C.asset", clip236236C, 19, 5, 29, 2500, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_BC.asset", null, 7, 3, 14, 610, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_j.A.asset", null, 6, 3, 10, 290, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_j.B.asset", null, 9, 3, 13, 560, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Cultist/BB_Cultist_j.C.asset", null, 14, 4, 17, 980, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 12, 22);
        }

        public static void ApplyBatch2Specifics()
        {
            ApplyC07Specifics();
            ApplyC08Specifics();
            ApplyC09Specifics();
            ApplyC10Specifics();
            ApplyC11Specifics();
        }

        private static void ApplyC07Specifics()
        {
            var superClip = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", "HumanF@MagicAttackDirect2H01");
            var clip236B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", "HumanF@MagicAttackCall1H01_L");
            var clip236C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01 - Cast.fbx", "HumanF@MagicAttackOmni01 - Cast");

            TuneMove("Assets/CombatData/BlazBlue/StellarWitch/BB_StellarWitch_236236C.asset", superClip, 18, 5, 25, 2600, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 14, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/StellarWitch/BB_StellarWitch_236B.asset", clip236B, 13, 4, 24, 1050, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/StellarWitch/BB_StellarWitch_236C.asset", clip236C, 14, 4, 21, 1150, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);

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
                        updated.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, jumpClip));
                    else if (pair.Key != null && pair.Key.name.Contains("CombatIdle01") && idleClip != null)
                        updated.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, idleClip));
                    else
                        updated.Add(pair);
                }
                overrideCtrl.ApplyOverrides(updated);
                EditorUtility.SetDirty(overrideCtrl);
            }
        }

        private static void ApplyC08Specifics()
        {
            var clip236 = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1");
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/SanpakuChan/BB_SanpakuChan_236A.asset", clip236, 12, 4, 15, 1050, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/SanpakuChan/BB_SanpakuChan_j.B.asset", clipjB, 8, 3, 11, 540, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
        }

        private static void ApplyC09Specifics()
        {
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Attack1");
            TuneMove("Assets/CombatData/BlazBlue/PicoChan/BB_PicoChan_5D.asset", clip5D, 14, 4, 17, 870, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
        }

        private static void ApplyC10Specifics()
        {
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

            TuneMove("Assets/CombatData/BlazBlue/Reika/BB_Reika_j.B.asset", null, 9, 3, 12, 550, new Vector3(1.05f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
        }

        private static void ApplyC11Specifics()
        {
            var clip236 = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Attack1");
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/Casual1/BB_Casual1_236A.asset", clip236, 12, 4, 14, 1020, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Casual1/BB_Casual1_j.B.asset", clipjB, 8, 3, 11, 520, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_236A.asset", clip236, 12, 4, 14, 1020, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_j.B.asset", clipjB, 8, 3, 11, 520, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
        }

        public static void ApplyBatch3Specifics()
        {
            ApplyC12Specifics();
            ApplyC13Specifics();
            ApplyC14Specifics();
            ApplySK02Specifics();
            ApplySK03Specifics();
        }

        private static void ApplyC12Specifics()
        {
            // C12 Fina: Agile Female Striker / Ninja Operative
            // Replace Dodge/Quickstep attacks with real female martial arts attacks. Fix reach so 5A hits at point blank.
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse");
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", "Punch");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm");

            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_5A.asset", null, 5, 3, 7, 270, new Vector3(0.65f, 1.25f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_5B.asset", null, 8, 3, 11, 540, new Vector3(0.80f, 1.20f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_5C.asset", null, 11, 4, 15, 920, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_2A.asset", null, 5, 3, 8, 250, new Vector3(0.65f, 0.40f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_2B.asset", null, 8, 3, 12, 510, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_2C.asset", clip2C, 12, 4, 17, 880, new Vector3(0.95f, 0.35f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_5D.asset", null, 13, 4, 15, 830, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_236A.asset", null, 11, 4, 15, 1020, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_214B.asset", clip214B, 13, 4, 17, 1120, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_236236C.asset", clipSuper, 17, 5, 26, 2450, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_BC.asset", null, 6, 3, 14, 580, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_j.A.asset", null, 5, 3, 9, 270, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_j.B.asset", null, 8, 3, 11, 530, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Fina/BB_Fina_j.C.asset", null, 12, 4, 15, 930, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyC13Specifics()
        {
            // C13 SapphiArtchan: Cute Sorceress / Magic Caster
            // Real attacks only (no hit reactions like CombatDamage01 as attacks). Real aerial attacks.
            var clipjA = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", null);
            var clipjB = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx", null);
            var clipjC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_5A.asset", null, 5, 3, 8, 260, new Vector3(0.65f, 1.25f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_5B.asset", null, 8, 3, 12, 520, new Vector3(0.80f, 1.20f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_5C.asset", null, 12, 4, 16, 910, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_2A.asset", null, 6, 3, 8, 240, new Vector3(0.65f, 0.40f, 0f), new Vector3(1.30f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_2B.asset", null, 8, 3, 12, 500, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_2C.asset", null, 13, 4, 18, 870, new Vector3(0.95f, 0.35f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_5D.asset", null, 14, 4, 16, 840, new Vector3(0.95f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_236A.asset", null, 12, 4, 16, 1030, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_214B.asset", null, 14, 4, 18, 1130, new Vector3(0.95f, 1.15f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_236236C.asset", null, 18, 5, 27, 2480, new Vector3(1.10f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_BC.asset", null, 6, 3, 14, 570, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_j.A.asset", clipjA, 6, 3, 9, 260, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_j.B.asset", clipjB, 8, 3, 11, 520, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/SapphiArtchan/BB_SapphiArtchan_j.C.asset", clipjC, 13, 4, 16, 920, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyC14Specifics()
        {
            // C14 Muryotaisu: Colossal Two-Handed Giant
            // Renders tiny: keep prefab scale (1.3193). Extended heavy reach.
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");

            var atk214 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_214A.asset");
            if (atk214 != null)
            {
                atk214.moveId = "214B";
                atk214.button = BBButton.B;
                EditorUtility.SetDirty(atk214);
            }

            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_5A.asset", null, 7, 3, 10, 310, new Vector3(0.80f, 1.25f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_5B.asset", null, 10, 3, 14, 590, new Vector3(1.05f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_5C.asset", null, 15, 4, 19, 980, new Vector3(1.20f, 1.20f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_2A.asset", null, 7, 3, 10, 290, new Vector3(0.80f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_2B.asset", null, 10, 3, 14, 560, new Vector3(1.05f, 0.40f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_2C.asset", null, 15, 4, 20, 930, new Vector3(1.20f, 0.35f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_5D.asset", null, 16, 4, 18, 880, new Vector3(1.15f, 1.10f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_236A.asset", null, 14, 4, 18, 1070, new Vector3(1.20f, 1.15f, 0f), new Vector3(1.70f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_214A.asset", null, 16, 4, 20, 1170, new Vector3(1.20f, 1.15f, 0f), new Vector3(1.70f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_236236C.asset", clipSuper, 20, 5, 30, 2600, new Vector3(1.30f, 1.25f, 0f), new Vector3(1.90f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_BC.asset", null, 7, 3, 15, 620, new Vector3(0.70f, 1.10f, 0f), new Vector3(1.30f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_j.A.asset", null, 7, 3, 11, 310, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_j.B.asset", null, 10, 3, 14, 590, new Vector3(1.05f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Muryotaisu/BB_Muryotaisu_j.C.asset", null, 15, 4, 18, 980, new Vector3(1.20f, -0.20f, 0f), new Vector3(1.70f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplySK02Specifics()
        {
            // SK02 Galahad: Sun Templar Knight (Sword & Shield)
            // Remove second overlapping model. Calibrate knight hitboxes.
            var atk214 = AssetDatabase.LoadAssetAtPath<BBAttackData>("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_214A.asset");
            if (atk214 != null)
            {
                atk214.moveId = "214B";
                atk214.button = BBButton.B;
                EditorUtility.SetDirty(atk214);
            }

            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_5A.asset", null, 6, 3, 9, 300, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_5B.asset", null, 9, 3, 13, 580, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_5C.asset", null, 13, 4, 17, 950, new Vector3(1.10f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_2A.asset", null, 6, 3, 9, 280, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_2B.asset", null, 9, 3, 13, 550, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_2C.asset", null, 14, 4, 19, 890, new Vector3(1.10f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_5D.asset", null, 15, 4, 17, 850, new Vector3(1.00f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_236A.asset", null, 13, 4, 17, 1040, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_214A.asset", null, 15, 4, 19, 1140, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_236236C.asset", null, 19, 5, 29, 2500, new Vector3(1.20f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_BC.asset", null, 7, 3, 14, 600, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_j.A.asset", null, 6, 3, 10, 300, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_j.B.asset", null, 9, 3, 13, 580, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Galahad/BB_Galahad_j.C.asset", null, 14, 4, 17, 950, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplySK03Specifics()
        {
            // SK03 Vex: Dune Raider Desert Marauder
            // Super was Atk_K_3; assign dramatic heavy super clip (Warrior Bundle 2: TwoHanded_Slam)
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");

            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_5A.asset", null, 6, 3, 9, 290, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_5B.asset", null, 9, 3, 13, 570, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_5C.asset", null, 13, 4, 17, 930, new Vector3(1.10f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_2A.asset", null, 6, 3, 9, 260, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_2B.asset", null, 9, 3, 13, 530, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_2C.asset", null, 14, 4, 19, 870, new Vector3(1.10f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_5D.asset", null, 15, 4, 17, 840, new Vector3(1.00f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_236A.asset", null, 13, 4, 17, 1030, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_214B.asset", null, 15, 4, 19, 1130, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_236236C.asset", clipSuper, 19, 5, 29, 2550, new Vector3(1.25f, 1.25f, 0f), new Vector3(1.80f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_BC.asset", null, 7, 3, 14, 580, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.20f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_j.A.asset", null, 6, 3, 10, 290, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.40f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_j.B.asset", null, 9, 3, 13, 570, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Vex/BB_Vex_j.C.asset", null, 14, 4, 17, 930, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        public static void ApplyBatch4Specifics()
        {
            ApplySK05Specifics();
            ApplySK06Specifics();
            ApplySK08Specifics();
            ApplyEX01Specifics();
            ApplyEX02Specifics();
        }

        private static void ApplySK05Specifics()
        {
            // SK05 Lord Morvath: Tyrannical Death Magus / Caster
            // Clean spellcasting animations, removed 2H greatsword weapon rig (pure spellcaster), 5A point blank reach
            var clip5A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx", null);
            var clip5B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx", null);
            var clip5C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx", null);
            var clip2A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Cast.fbx", null);
            var clip2B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Cast.fbx", null);
            var clip2C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Cast.fbx", null);
            var clip5D = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01 - Load.fbx", null);
            var clipBC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L.fbx", null);
            var clip236A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L - Cast.fbx", null);
            var clip214B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01 - Cast.fbx", null);
            var clipSuper = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx", null);
            var clipjA = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R - Load.fbx", null);
            var clipjB = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L - Load.fbx", null);
            var clipjC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01 - Load.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_5A.asset", clip5A, 6, 3, 8, 280, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_5B.asset", clip5B, 9, 3, 13, 560, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_5C.asset", clip5C, 13, 4, 18, 940, new Vector3(1.05f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_2A.asset", clip2A, 6, 3, 8, 260, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_2B.asset", clip2B, 9, 3, 13, 530, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_2C.asset", clip2C, 14, 4, 19, 870, new Vector3(1.05f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_5D.asset", clip5D, 14, 4, 17, 830, new Vector3(1.00f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_236A.asset", clip236A, 12, 4, 16, 1040, new Vector3(1.05f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_214B.asset", clip214B, 15, 4, 19, 1140, new Vector3(1.05f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_236236C.asset", clipSuper, 17, 5, 28, 2520, new Vector3(1.20f, 1.25f, 0f), new Vector3(1.85f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_BC.asset", clipBC, 6, 3, 14, 570, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_j.A.asset", clipjA, 6, 3, 10, 280, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_j.B.asset", clipjB, 9, 3, 13, 550, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/LordMorvath/BB_LordMorvath_j.C.asset", clipjC, 14, 4, 17, 920, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplySK06Specifics()
        {
            // SK06 Commander Ren: Halberd / Saber Military Commander
            // Unique clips without duplicate reuse, distinct military commander frame data
            var clip5A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx", null);
            var clip5B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx", null);
            var clip5C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx", null);
            var clip2A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1");
            var clip2B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1");
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance");
            var clipBC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx", null);
            var clip236A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx", null);
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");
            var clipjA = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust");
            var clipjB = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash");
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave");

            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_5A.asset", clip5A, 6, 3, 8, 290, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_5B.asset", clip5B, 8, 3, 12, 570, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_5C.asset", clip5C, 13, 4, 17, 950, new Vector3(1.10f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_2A.asset", clip2A, 6, 3, 9, 260, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_2B.asset", clip2B, 9, 3, 13, 540, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_2C.asset", clip2C, 13, 4, 19, 860, new Vector3(1.10f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_5D.asset", clip5D, 14, 4, 17, 840, new Vector3(1.05f, 1.10f, 0f), new Vector3(1.55f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_236A.asset", clip236A, 12, 4, 16, 1050, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_214B.asset", clip214B, 15, 4, 19, 1150, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_236236C.asset", clipSuper, 18, 5, 28, 2580, new Vector3(1.25f, 1.25f, 0f), new Vector3(1.85f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_BC.asset", clipBC, 6, 3, 14, 580, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_j.A.asset", clipjA, 6, 3, 10, 290, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_j.B.asset", clipjB, 9, 3, 13, 560, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/CommanderRen/BB_CommanderRen_j.C.asset", clipjC, 14, 4, 17, 930, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplySK08Specifics()
        {
            // SK08 Torgrim: Hammer / Heavy Bruiser / Mountain Warrior
            // Warhammer & heavy warrior impacts, high hit damage, super damage 2720
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing");
            var clip5B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash");
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash");
            var clip2A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep");
            var clip2B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut");
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Quake");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave");
            var clipBC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx", null);
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave");
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");
            var clipjA = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx", null);
            var clipjB = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook");
            var clipjC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_5A.asset", clip5A, 7, 3, 10, 310, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.45f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_5B.asset", clip5B, 10, 3, 14, 610, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_5C.asset", clip5C, 15, 4, 20, 990, new Vector3(1.15f, 1.20f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_2A.asset", clip2A, 7, 3, 10, 280, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.45f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_2B.asset", clip2B, 10, 3, 14, 570, new Vector3(0.95f, 0.40f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_2C.asset", clip2C, 15, 4, 20, 900, new Vector3(1.15f, 0.35f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_5D.asset", clip5D, 16, 4, 18, 870, new Vector3(1.10f, 1.10f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_236A.asset", clip236A, 13, 4, 17, 1080, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_214B.asset", clip214B, 16, 4, 20, 1180, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_236236C.asset", clipSuper, 19, 5, 30, 2720, new Vector3(1.30f, 1.25f, 0f), new Vector3(1.90f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_BC.asset", clipBC, 7, 3, 15, 620, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_j.A.asset", clipjA, 7, 3, 11, 310, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_j.B.asset", clipjB, 10, 3, 14, 590, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.55f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Torgrim/BB_Torgrim_j.C.asset", clipjC, 15, 4, 18, 960, new Vector3(1.15f, -0.20f, 0f), new Vector3(1.70f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyEX01Specifics()
        {
            // EX01 Hugo: Cybernetic Gorilla Brawler / Brute
            // Heavy brawler animations, scale tuned to 0.52 in prefab, super damage 2760
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook");
            var clip5B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut");
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam");
            var clip2A = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null);
            var clip2B = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null);
            var clip2C = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null);
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm");
            var clipBC = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null);
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null);
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", null);
            var clipjA = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", null);
            var clipjB = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null);
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse");

            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_5A.asset", clip5A, 6, 3, 9, 320, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.45f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_5B.asset", clip5B, 9, 3, 13, 620, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_5C.asset", clip5C, 14, 4, 19, 1020, new Vector3(1.15f, 1.20f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_2A.asset", clip2A, 6, 3, 9, 290, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.45f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_2B.asset", clip2B, 9, 3, 13, 580, new Vector3(0.95f, 0.40f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_2C.asset", clip2C, 14, 4, 19, 910, new Vector3(1.15f, 0.35f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_5D.asset", clip5D, 15, 4, 18, 890, new Vector3(1.10f, 1.10f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_236A.asset", clip236A, 13, 4, 17, 1090, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_214B.asset", clip214B, 15, 4, 19, 1190, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_236236C.asset", clipSuper, 18, 5, 29, 2760, new Vector3(1.30f, 1.25f, 0f), new Vector3(1.90f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_BC.asset", clipBC, 6, 3, 14, 630, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.30f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_j.A.asset", clipjA, 6, 3, 10, 320, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_j.B.asset", clipjB, 9, 3, 13, 600, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.55f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Hugo/BB_Hugo_j.C.asset", clipjC, 14, 4, 18, 980, new Vector3(1.15f, -0.20f, 0f), new Vector3(1.70f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyEX02Specifics()
        {
            // EX02 Granite: Heavy Golem / Stone Hammer Bruiser
            // Standard pass, solid ground impact, super damage 2680
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Swing");
            var clip5B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Smash");
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");
            var clip2A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut");
            var clip2B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep");
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX", "Hammer_Quake");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave");
            var clipBC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam");
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave");
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash");
            var clipjA = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook");
            var clipjB = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null);
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance");

            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_5A.asset", clip5A, 7, 3, 10, 300, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_5B.asset", clip5B, 10, 3, 14, 600, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_5C.asset", clip5C, 15, 4, 20, 980, new Vector3(1.15f, 1.20f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_2A.asset", clip2A, 7, 3, 10, 270, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_2B.asset", clip2B, 10, 3, 14, 560, new Vector3(0.95f, 0.40f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_2C.asset", clip2C, 15, 4, 20, 890, new Vector3(1.15f, 0.35f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_5D.asset", clip5D, 15, 4, 18, 860, new Vector3(1.10f, 1.10f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_236A.asset", clip236A, 13, 4, 17, 1070, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_214B.asset", clip214B, 16, 4, 20, 1170, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_236236C.asset", clipSuper, 19, 5, 30, 2680, new Vector3(1.30f, 1.25f, 0f), new Vector3(1.90f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_BC.asset", clipBC, 7, 3, 15, 610, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_j.A.asset", clipjA, 7, 3, 11, 300, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_j.B.asset", clipjB, 10, 3, 14, 580, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.55f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Granite/BB_Granite_j.C.asset", clipjC, 15, 4, 18, 950, new Vector3(1.15f, -0.20f, 0f), new Vector3(1.70f, 1.40f, 1.0f), 12, 22);
        }

        public static void ApplyBatch5Specifics()
        {
            ApplyEX03Specifics();
            ApplyEX04Specifics();
            ApplyEX05Specifics();
            ApplyEX06Specifics();
            ApplyEX07Specifics();
        }

        private static void ApplyEX03Specifics()
        {
            // EX03 Viper: Shadow Operative / Ninja Assassin / Dual Blades
            // 14 unique clips without duplicate reuse, high speed / agile assassin archetype
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash1");
            var clip5B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Slash2");
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1");
            var clip2A = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null);
            var clip2B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX", "Ninja_Cross");
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash");
            var clipBC = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null);
            var clip236A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx", null);
            var clip214B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx", null);
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");
            var clipjA = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust");
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse");

            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_5A.asset", clip5A, 5, 3, 8, 270, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_5B.asset", clip5B, 8, 3, 12, 540, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_5C.asset", clip5C, 12, 4, 16, 920, new Vector3(1.05f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_2A.asset", clip2A, 5, 3, 8, 250, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_2B.asset", clip2B, 8, 3, 12, 510, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_2C.asset", clip2C, 13, 4, 18, 860, new Vector3(1.05f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_5D.asset", clip5D, 13, 4, 16, 820, new Vector3(1.00f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_236A.asset", clip236A, 11, 4, 15, 1020, new Vector3(1.05f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_214B.asset", clip214B, 14, 4, 18, 1120, new Vector3(1.05f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_236236C.asset", clipSuper, 17, 5, 27, 2550, new Vector3(1.20f, 1.25f, 0f), new Vector3(1.85f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_BC.asset", clipBC, 6, 3, 14, 570, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_j.A.asset", clipjA, 5, 3, 9, 270, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_j.B.asset", clipjB, 8, 3, 12, 530, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Viper/BB_Viper_j.C.asset", clipjC, 12, 4, 16, 910, new Vector3(1.05f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyEX04Specifics()
        {
            // EX04 Officer K: Security Enforcer / Karate / Tactical Brawler
            // 14 unique unarmed martial arts clips, heavy tactical impact and crisp strikes
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Jab");
            var clip5B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Palm");
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX", "Karate_Roundhouse");
            var clip2A = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", null);
            var clip2B = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", null);
            var clip2C = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", null);
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", null);
            var clipBC = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx", null);
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", null);
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Hook");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_DoubleSlam");
            var clipjA = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", null);
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", null);
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX", "Brute_Uppercut");

            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_5A.asset", clip5A, 6, 3, 8, 290, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_5B.asset", clip5B, 9, 3, 13, 580, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_5C.asset", clip5C, 13, 4, 17, 960, new Vector3(1.10f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_2A.asset", clip2A, 6, 3, 8, 270, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_2B.asset", clip2B, 9, 3, 13, 540, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_2C.asset", clip2C, 14, 4, 19, 880, new Vector3(1.10f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_5D.asset", clip5D, 14, 4, 17, 850, new Vector3(1.05f, 1.10f, 0f), new Vector3(1.55f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_236A.asset", clip236A, 12, 4, 16, 1060, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_214B.asset", clip214B, 15, 4, 19, 1160, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_236236C.asset", clipSuper, 18, 5, 28, 2620, new Vector3(1.25f, 1.25f, 0f), new Vector3(1.85f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_BC.asset", clipBC, 6, 3, 14, 600, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_j.A.asset", clipjA, 6, 3, 10, 290, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_j.B.asset", clipjB, 9, 3, 13, 570, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/OfficerK/BB_OfficerK_j.C.asset", clipjC, 13, 4, 17, 940, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyEX05Specifics()
        {
            // EX05 Kira: Apex Huntress / Cyber Archer
            // 14 unique clips from Archer & Crossbow packs, swift ranger reach & piercing damage
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", "Archer_QuickDraw");
            var clip5B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", "Archer_PowerShot");
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX", "Archer_Volley");
            var clip2A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX", "Crossbow_Snapshot");
            var clip2B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX", "Crossbow_Burst");
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX", "Crossbow_Scatter");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Slash1");
            var clipBC = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Grab_Break_A.fbx", null);
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash");
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");
            var clipjA = LoadSubclip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", null);
            var clipjB = LoadSubclip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", null);
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust");

            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_5A.asset", clip5A, 6, 3, 9, 280, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_5B.asset", clip5B, 9, 3, 13, 560, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_5C.asset", clip5C, 14, 4, 18, 940, new Vector3(1.10f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_2A.asset", clip2A, 6, 3, 9, 260, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_2B.asset", clip2B, 9, 3, 13, 530, new Vector3(0.95f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_2C.asset", clip2C, 14, 4, 19, 870, new Vector3(1.10f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_5D.asset", clip5D, 15, 4, 17, 840, new Vector3(1.05f, 1.10f, 0f), new Vector3(1.55f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_236A.asset", clip236A, 13, 4, 17, 1050, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_214B.asset", clip214B, 16, 4, 20, 1150, new Vector3(1.10f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_236236C.asset", clipSuper, 18, 5, 29, 2580, new Vector3(1.25f, 1.25f, 0f), new Vector3(1.85f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_BC.asset", clipBC, 6, 3, 14, 580, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_j.A.asset", clipjA, 6, 3, 10, 280, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_j.B.asset", clipjB, 9, 3, 13, 550, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Kira/BB_Kira_j.C.asset", clipjC, 13, 4, 17, 920, new Vector3(1.10f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyEX06Specifics()
        {
            // EX06 Sentry: Phalanx Sentinel / Spearman / Halberd Pike
            // 14 unique spear/polearm clips, long range defensive control
            var clip5A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Thrust1");
            var clip5B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx", null);
            var clip5C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Lance");
            var clip2A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX", "Spearman_Sweep");
            var clip2B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx", null);
            var clip2C = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Sweep");
            var clip5D = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Cleave");
            var clipBC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx", null);
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX", "Knight_Slash");
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Cleave");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slam");
            var clipjA = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx", null);
            var clipjB = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX", "Swordsman_Thrust");
            var clipjC = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX", "TwoHanded_Slash");

            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_5A.asset", clip5A, 7, 3, 9, 300, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.45f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_5B.asset", clip5B, 10, 3, 13, 590, new Vector3(0.95f, 1.20f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_5C.asset", clip5C, 15, 4, 19, 970, new Vector3(1.15f, 1.20f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_2A.asset", clip2A, 7, 3, 9, 280, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.45f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_2B.asset", clip2B, 10, 3, 13, 560, new Vector3(0.95f, 0.40f, 0f), new Vector3(1.55f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_2C.asset", clip2C, 15, 4, 20, 890, new Vector3(1.15f, 0.35f, 0f), new Vector3(1.70f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_5D.asset", clip5D, 15, 4, 17, 860, new Vector3(1.10f, 1.10f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_236A.asset", clip236A, 13, 4, 17, 1070, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_214B.asset", clip214B, 16, 4, 20, 1170, new Vector3(1.15f, 1.15f, 0f), new Vector3(1.65f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_236236C.asset", clipSuper, 19, 5, 29, 2650, new Vector3(1.30f, 1.25f, 0f), new Vector3(1.90f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_BC.asset", clipBC, 6, 3, 14, 610, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_j.A.asset", clipjA, 7, 3, 10, 300, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_j.B.asset", clipjB, 10, 3, 13, 580, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.55f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Sentry/BB_Sentry_j.C.asset", clipjC, 14, 4, 18, 950, new Vector3(1.15f, -0.20f, 0f), new Vector3(1.70f, 1.40f, 1.0f), 12, 22);
        }

        private static void ApplyEX07Specifics()
        {
            // EX07 Lyra: Arcane Valkyrie / Mystic Sorceress
            // 14 unique female spellcasting & sorceress clips, celestial radiant magic
            var clip5A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", null);
            var clip5B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", null);
            var clip5C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", null);
            var clip2A = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast.fbx", null);
            var clip2B = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Cast.fbx", null);
            var clip2C = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Cast.fbx", null);
            var clip5D = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01 - Load.fbx", null);
            var clipBC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", null);
            var clip236A = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Burst");
            var clip214B = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Wave");
            var clipSuper = LoadSubclip("Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX", "Sorceress_Nova");
            var clipjA = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Load.fbx", null);
            var clipjB = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L - Load.fbx", null);
            var clipjC = LoadSubclip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Load.fbx", null);

            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_5A.asset", clip5A, 6, 3, 8, 270, new Vector3(0.75f, 1.25f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_5B.asset", clip5B, 8, 3, 12, 530, new Vector3(0.90f, 1.20f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_5C.asset", clip5C, 13, 4, 17, 930, new Vector3(1.05f, 1.20f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_2A.asset", clip2A, 6, 3, 8, 250, new Vector3(0.75f, 0.40f, 0f), new Vector3(1.40f, 0.60f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_2B.asset", clip2B, 8, 3, 12, 510, new Vector3(0.90f, 0.40f, 0f), new Vector3(1.50f, 0.60f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_2C.asset", clip2C, 13, 4, 18, 860, new Vector3(1.05f, 0.35f, 0f), new Vector3(1.60f, 0.60f, 1.0f), 12, 22, true);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_5D.asset", clip5D, 14, 4, 16, 830, new Vector3(1.00f, 1.10f, 0f), new Vector3(1.50f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_236A.asset", clip236A, 12, 4, 16, 1030, new Vector3(1.05f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_214B.asset", clip214B, 15, 4, 19, 1130, new Vector3(1.05f, 1.15f, 0f), new Vector3(1.60f, 0.70f, 1.0f), 10, 18);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_236236C.asset", clipSuper, 18, 5, 28, 2500, new Vector3(1.20f, 1.25f, 0f), new Vector3(1.85f, 0.90f, 1.0f), 16, 32, true, true);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_BC.asset", clipBC, 6, 3, 14, 570, new Vector3(0.65f, 1.10f, 0f), new Vector3(1.25f, 0.70f, 1.0f), 8, 14);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_j.A.asset", clipjA, 6, 3, 9, 270, new Vector3(0.85f, -0.20f, 0f), new Vector3(1.45f, 1.40f, 1.0f), 6, 12);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_j.B.asset", clipjB, 8, 3, 12, 530, new Vector3(0.95f, -0.20f, 0f), new Vector3(1.50f, 1.40f, 1.0f), 9, 16);
            TuneMove("Assets/CombatData/BlazBlue/Lyra/BB_Lyra_j.C.asset", clipjC, 13, 4, 16, 920, new Vector3(1.05f, -0.20f, 0f), new Vector3(1.60f, 1.40f, 1.0f), 12, 22);
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
