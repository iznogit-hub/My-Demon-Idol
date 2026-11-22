#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Editor;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.Editor
{
    [InitializeOnLoad]
    public static class BBBatchProofRunner
    {
        public const string TRIGGER_BATCH1 = "run_batch1_proof.trigger";
        public const string TRIGGER_BATCH2 = "run_batch2_proof.trigger";
        public const string TRIGGER_BATCH3 = "run_batch3_proof.trigger";
        public const string TRIGGER_BATCH4 = "run_batch4_proof.trigger";
        public const string TRIGGER_BATCH5 = "run_batch5_proof.trigger";
        public const string TRIGGER_APPLY_MAPPING = "apply_clip_mapping.trigger";
        public const string TRIGGER_EXPORT_TRACKER = "export_tracker.trigger";
        public const string TRIGGER_MEASURE_IMPACT = "measure_impact.trigger";

        public static string CurrentScreenshotBaseDir = "Screenshots/fightfeel/batch1";

        public static readonly string[] Batch1Fighters = new string[] { "C01", "C02", "C04", "C05", "C06" };
        public static readonly string[] Batch2Fighters = new string[] { "C07", "C08", "C09", "C10", "C11" };
        public static readonly string[] Batch3Fighters = new string[] { "C12", "C13", "C14", "SK02", "SK03" };
        public static readonly string[] Batch4Fighters = new string[] { "SK05", "SK06", "SK08", "EX01", "EX02" };
        public static readonly string[] Batch5Fighters = new string[] { "EX03", "EX04", "EX05", "EX06", "EX07" };

        public static readonly string[] RequiredMoves = new string[]
        {
            "5A", "5B", "5C", "2A", "2B", "2C", "j.A", "j.B", "j.C", "5D", "BC", "236A", "214B", "236236C"
        };

        public class MoveLogResult
        {
            public string fighterId;
            public string moveId;
            public string clipName;
            public int frames;
            public float damage;
            public bool hit;
            public string rawLine;
        }

        public static List<MoveLogResult> RawResults = new List<MoveLogResult>();

        static BBBatchProofRunner()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_APPLY_MAPPING))
            {
                try { File.Delete(TRIGGER_APPLY_MAPPING); } catch { }
                BBClipMappingTool.ApplyClipMapping();
            }
            if (File.Exists(TRIGGER_EXPORT_TRACKER))
            {
                try { File.Delete(TRIGGER_EXPORT_TRACKER); } catch { }
                BBMovesetTrackerExporter.ExportMovesetTracker();
            }
            if (File.Exists(TRIGGER_MEASURE_IMPACT))
            {
                try { File.Delete(TRIGGER_MEASURE_IMPACT); } catch { }
                DemonIdol.BlazBlue.Editor.BBImpactFrameMeasurer.MeasureAllAttackClips();
            }
            if (File.Exists(TRIGGER_BATCH1))
            {
                try { File.Delete(TRIGGER_BATCH1); } catch { }
                RunBatch1Proof();
            }
            if (File.Exists(TRIGGER_BATCH2))
            {
                try { File.Delete(TRIGGER_BATCH2); } catch { }
                RunBatch2Proof();
            }
            if (File.Exists(TRIGGER_BATCH3))
            {
                try { File.Delete(TRIGGER_BATCH3); } catch { }
                RunBatch3Proof();
            }
            if (File.Exists(TRIGGER_BATCH4))
            {
                try { File.Delete(TRIGGER_BATCH4); } catch { }
                RunBatch4Proof();
            }
            if (File.Exists(TRIGGER_BATCH5))
            {
                try { File.Delete(TRIGGER_BATCH5); } catch { }
                RunBatch5Proof();
            }
        }

        public static void RunBatch1Proof()
        {
            CurrentScreenshotBaseDir = "Screenshots/fightfeel/batch1";
            RunBatchProofGeneric("BATCH 1", Batch1Fighters);
        }

        public static void RunBatch2Proof()
        {
            CurrentScreenshotBaseDir = "Screenshots/fightfeel/batch2";
            RunBatchProofGeneric("BATCH 2", Batch2Fighters);
        }

        public static void RunBatch3Proof()
        {
            CurrentScreenshotBaseDir = "Screenshots/fightfeel/batch3";
            RunBatchProofGeneric("BATCH 3", Batch3Fighters);
        }

        public static void RunBatch4Proof()
        {
            CurrentScreenshotBaseDir = "Screenshots/fightfeel/batch4";
            RunBatchProofGeneric("BATCH 4", Batch4Fighters);
        }

        public static void RunBatch5Proof()
        {
            CurrentScreenshotBaseDir = "Screenshots/fightfeel/batch5";
            RunBatchProofGeneric("BATCH 5", Batch5Fighters);
        }

        private static void RunBatchProofGeneric(string batchName, string[] fighters)
        {
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] ====================================================</color>");
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] STARTING {batchName} FIGHT-FEEL PROOF RUNNER...</color>");
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] Testing {string.Join(", ", fighters)} in BOTH P1 and P2 slots</color>");
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] ====================================================</color>");

            RawResults.Clear();

            foreach (var fighterId in fighters)
            {
                RunFighterSlotProof(fighterId, 0); // As Player 1
                RunFighterSlotProof(fighterId, 1); // As Player 2
            }

            Debug.Log("<color=#00e676>[BBBatchProofRunner] ==================== RAW LOG REPORT ====================</color>");
            int total = RawResults.Count;
            int hits = 0;
            foreach (var r in RawResults)
            {
                if (r.hit) hits++;
                Debug.Log(r.rawLine);
            }
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] TOTAL HIT CONFIRMATIONS: {hits} / {total} ({(float)hits / total * 100f:F1}%)</color>");
        }

        public static void RunFighterSlotProof(string fighterId, int slot)
        {
            string dummyId = fighterId == "C07" ? "C01" : "C07";
            string p1Id = slot == 0 ? fighterId : dummyId;
            string p2Id = slot == 1 ? fighterId : dummyId;

            PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
            PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "Versus");
            LaunchServices.ResetForTests();

            string scenePath = "Assets/Scenes/BattleArena.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var director = UnityEngine.Object.FindAnyObjectByType<HomegrownCombatDirector>();
            if (director == null)
            {
                Debug.LogError("[BBBatchProofRunner] HomegrownCombatDirector missing in BattleArena!");
                return;
            }

            director.InitializeHomegrownMatch();

            var p1 = director.Fighter1;
            var p2 = director.Fighter2;
            var targetFighter = slot == 0 ? p1 : p2;
            var dummyFighter = slot == 0 ? p2 : p1;

            var targetBrain = targetFighter.GetComponent<BBFighterBrain>();
            var dummyBrain = dummyFighter.GetComponent<BBFighterBrain>();
            var targetSampler = targetFighter.GetComponent<BBInputSampler>();
            var sim = BBMatchSim.Instance;

            // Check nested FighterController removal
            var targetModelChild = targetFighter.transform.Find("Model_" + fighterId);
            if (targetModelChild != null)
            {
                var nestedFc = targetModelChild.GetComponent<FighterController>();
                if (nestedFc != null)
                {
                    Debug.LogError($"[BBBatchProofRunner] VIOLATION: Model_{fighterId} still has nested FighterController!");
                }
                else
                {
                    Debug.Log($"<color=#00e676>[BBBatchProofRunner] PASS: Nested FighterController on Model_{fighterId} is confirmed removed.</color>");
                }
            }

            // Check for multiple/overlapping model children
            int modelCount = 0;
            for (int i = 0; i < targetFighter.transform.childCount; i++)
            {
                var child = targetFighter.transform.GetChild(i);
                if (child.name.StartsWith("Model_")) modelCount++;
            }
            if (modelCount > 1)
            {
                Debug.LogError($"[BBBatchProofRunner] VIOLATION: {fighterId} has {modelCount} overlapping models in slot P{slot + 1}!");
            }
            else
            {
                Debug.Log($"<color=#00e676>[BBBatchProofRunner] PASS: {fighterId} has exactly {modelCount} model child (no overlapping models).</color>");
            }

            // Check foot height from floor (floor is Y = 0)
            float footY = targetFighter.transform.position.y;
            var anim = targetFighter.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                var leftToes = anim.GetBoneTransform(HumanBodyBones.LeftToes);
                var rightToes = anim.GetBoneTransform(HumanBodyBones.RightToes);
                if (leftToes != null && rightToes != null)
                {
                    footY = Mathf.Min(leftToes.position.y, rightToes.position.y);
                }
                else
                {
                    var leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                    if (leftFoot != null && rightFoot != null)
                    {
                        footY = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - 0.08f;
                    }
                }
            }
            float modelScale = targetFighter.transform.localScale.x;
            if (anim != null && anim.transform != targetFighter.transform)
            {
                modelScale = anim.transform.localScale.x;
            }
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] {fighterId} foot height in slot P{slot + 1}: {footY:F3}m (allowed <= 0.05m)</color>");
            Debug.Log($"<color=#00e676>[BBBatchProofRunner] {fighterId} scale in slot P{slot + 1}: {modelScale:F4}</color>");

            // Capture Idle screenshot
            string charDir = Path.Combine(CurrentScreenshotBaseDir, fighterId);
            if (!Directory.Exists(charDir)) Directory.CreateDirectory(charDir);

            if (slot == 0)
            {
                CaptureViewportScreenshot(Path.Combine(charDir, "idle.png"));
            }

            int currentTick = 1;

            // Test each required move
            foreach (var moveId in RequiredMoves)
            {
                var attackData = targetBrain.FindAttackByMoveId(moveId);
                string clipName = attackData != null && attackData.clip != null ? attackData.clip.name : (attackData != null ? attackData.clipName : "None");
                int srcFrames = attackData != null && attackData.clip != null ? Mathf.RoundToInt(attackData.clip.length * 30f) : (attackData != null ? attackData.clipLastFrame : 20);
                float damage = attackData != null ? attackData.damage : 300f;

                // Position fighters for clean engagement
                float separation = (moveId.Contains("j.") || moveId == "BC" || moveId == "2C") ? 0.95f : 1.20f;
                if (slot == 0)
                {
                    targetFighter.transform.position = new Vector3(-separation * 0.5f, 0f, 0f);
                    dummyFighter.transform.position = new Vector3(separation * 0.5f, 0f, 0f);
                    targetFighter.SetFacing(1);
                    dummyFighter.SetFacing(-1);
                }
                else
                {
                    targetFighter.transform.position = new Vector3(separation * 0.5f, 0f, 0f);
                    dummyFighter.transform.position = new Vector3(-separation * 0.5f, 0f, 0f);
                    targetFighter.SetFacing(-1);
                    dummyFighter.SetFacing(1);
                }

                // Reset dummy state
                dummyBrain.health = dummyBrain.maxHealth;
                dummyBrain.hitstopRemaining = 0;
                dummyBrain.blockstunRemaining = 0;
                targetBrain.hitstopRemaining = 0;

                // Execute move input cleanly through BBInputSampler
                bool hitConfirmed = ExecuteMoveProof(targetSampler, targetBrain, dummyBrain, sim, moveId, slot, attackData, ref currentTick, charDir);

                string raw = $"{fighterId} (P{slot + 1}) | {moveId,-7} | {clipName,-32} | {srcFrames,3}f | dmg:{damage,4} | Hit:{hitConfirmed}";
                RawResults.Add(new MoveLogResult
                {
                    fighterId = fighterId,
                    moveId = moveId,
                    clipName = clipName,
                    frames = srcFrames,
                    damage = damage,
                    hit = hitConfirmed,
                    rawLine = raw
                });
            }

            // Capture Block and Knockdown screenshots
            if (slot == 0)
            {
                targetFighter.transform.position = new Vector3(-1.25f * 0.5f, 0f, 0f);
                dummyFighter.transform.position = new Vector3(1.25f * 0.5f, 0f, 0f);
                dummyBrain.health = dummyBrain.maxHealth;

                var dummySampler = dummyFighter.GetComponent<BBInputSampler>();
                if (dummySampler != null)
                {
                    dummySampler.virtualStickInput = new Vector2(1f, 0f); // Back for P2 facing left
                }
                targetSampler.virtualStickInput = Vector2.zero;
                targetSampler.virtualPressedMask = (byte)BBButton.C;
                targetSampler.virtualHeldMask = (byte)BBButton.C;

                for (int t = 0; t < 15; t++) { currentTick++; sim.Tick(currentTick); }
                CaptureViewportScreenshot(Path.Combine(charDir, "block.png"));

                // Knockdown setup: hit with sweep (2C)
                if (dummySampler != null) dummySampler.virtualStickInput = Vector2.zero;
                targetSampler.virtualStickInput = new Vector2(0f, -1f);
                targetSampler.virtualPressedMask = (byte)BBButton.C;
                targetSampler.virtualHeldMask = (byte)BBButton.C;
                for (int t = 0; t < 25; t++) { currentTick++; sim.Tick(currentTick); }
                CaptureViewportScreenshot(Path.Combine(charDir, "knockdown.png"));
            }
        }

        private static bool ExecuteMoveProof(
            BBInputSampler sampler,
            BBFighterBrain targetBrain,
            BBFighterBrain dummyBrain,
            BBMatchSim sim,
            string moveId,
            int slot,
            BBAttackData attackData,
            ref int currentTick,
            string charDir)
        {
            if (sampler == null) return false;

            sampler.virtualStickInput = Vector2.zero;
            sampler.virtualPressedMask = 0;
            sampler.virtualHeldMask = 0;

            float fwd = slot == 0 ? 1f : -1f;
            float back = slot == 0 ? -1f : 1f;

            if (moveId.StartsWith("j."))
            {
                // Ensure target is grounded and neutral before jump
                for (int w = 0; w < 60; w++)
                {
                    if (targetBrain.isGrounded && targetBrain.bbState == BBState.Neutral) break;
                    currentTick++;
                    sim.Tick(currentTick);
                }

                // Jump to airborne
                sampler.virtualStickInput = new Vector2(0f, 1f);
                for (int i = 0; i < 20; i++)
                {
                    currentTick++;
                    sim.Tick(currentTick);
                    if (targetBrain.bbState == BBState.Airborne)
                    {
                        currentTick++;
                        sim.Tick(currentTick);
                        break;
                    }
                }
                byte airBtn = moveId == "j.A" ? (byte)BBButton.A : (moveId == "j.B" ? (byte)BBButton.B : (byte)BBButton.C);
                sampler.virtualStickInput = Vector2.zero;
                sampler.virtualPressedMask = airBtn;
                sampler.virtualHeldMask = airBtn;
            }
            else if (moveId == "236A")
            {
                sampler.virtualStickInput = new Vector2(0f, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(fwd, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(fwd, 0f);
                sampler.virtualPressedMask = (byte)BBButton.A;
                sampler.virtualHeldMask = (byte)BBButton.A;
            }
            else if (moveId == "214B")
            {
                sampler.virtualStickInput = new Vector2(0f, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(back, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(back, 0f);
                sampler.virtualPressedMask = (byte)BBButton.B;
                sampler.virtualHeldMask = (byte)BBButton.B;
            }
            else if (moveId == "236236C")
            {
                sampler.virtualStickInput = new Vector2(0f, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(fwd, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(fwd, 0f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(0f, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(fwd, -1f); currentTick++; sim.Tick(currentTick);
                sampler.virtualStickInput = new Vector2(fwd, 0f);
                sampler.virtualPressedMask = (byte)BBButton.C;
                sampler.virtualHeldMask = (byte)BBButton.C;
            }
            else if (moveId == "2A")
            {
                sampler.virtualStickInput = new Vector2(0f, -1f);
                sampler.virtualPressedMask = (byte)BBButton.A;
                sampler.virtualHeldMask = (byte)BBButton.A;
            }
            else if (moveId == "2B")
            {
                sampler.virtualStickInput = new Vector2(0f, -1f);
                sampler.virtualPressedMask = (byte)BBButton.B;
                sampler.virtualHeldMask = (byte)BBButton.B;
            }
            else if (moveId == "2C")
            {
                sampler.virtualStickInput = new Vector2(0f, -1f);
                sampler.virtualPressedMask = (byte)BBButton.C;
                sampler.virtualHeldMask = (byte)BBButton.C;
            }
            else if (moveId == "BC")
            {
                sampler.virtualStickInput = Vector2.zero;
                sampler.virtualPressedMask = (byte)(BBButton.B | BBButton.C);
                sampler.virtualHeldMask = (byte)(BBButton.B | BBButton.C);
            }
            else
            {
                byte b = moveId == "5A" ? (byte)BBButton.A : (moveId == "5B" ? (byte)BBButton.B : (moveId == "5C" ? (byte)BBButton.C : (byte)BBButton.D));
                sampler.virtualStickInput = Vector2.zero;
                sampler.virtualPressedMask = b;
                sampler.virtualHeldMask = b;
            }

            float initialDummyHp = dummyBrain.health;
            bool hitConfirmed = false;
            int maxTicks = attackData != null ? (attackData.startupFrames + attackData.activeFrames + (moveId.Contains("236236") ? 40 : 15)) : 35;

            for (int t = 0; t < maxTicks; t++)
            {
                currentTick++;
                sim.Tick(currentTick);

                if (targetBrain.chainState.madeContact || dummyBrain.health < initialDummyHp)
                {
                    hitConfirmed = true;
                    if (slot == 0)
                    {
                        if (moveId == "5A" && !File.Exists(Path.Combine(charDir, "normal.png")))
                            CaptureViewportScreenshot(Path.Combine(charDir, "normal.png"));
                        else if (moveId == "236A" && !File.Exists(Path.Combine(charDir, "special.png")))
                            CaptureViewportScreenshot(Path.Combine(charDir, "special.png"));
                        else if (moveId == "236236C" && !File.Exists(Path.Combine(charDir, "super.png")))
                            CaptureViewportScreenshot(Path.Combine(charDir, "super.png"));
                    }
                    break;
                }
            }

            // Clear inputs and recover
            sampler.virtualStickInput = Vector2.zero;
            sampler.virtualPressedMask = 0;
            sampler.virtualHeldMask = 0;

            for (int w = 0; w < 60; w++)
            {
                if (targetBrain.bbState == BBState.Neutral && dummyBrain.bbState == BBState.Neutral && targetBrain.isGrounded) break;
                currentTick++;
                sim.Tick(currentTick);
            }

            return hitConfirmed;
        }

        private static void CaptureViewportScreenshot(string savePath)
        {
            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            int width = 960;
            int height = 540;
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string dir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(savePath, bytes);
        }
    }
}
#endif
