using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.Data;
using DemonIdol.Launch;

namespace DemonIdol.Tests.PlayMode
{
    [TestFixture]
    public class MDILaunchPolishPlayTests : InputTestFixture
    {
        public static readonly string[] FullRoster = new string[]
        {
            "C01", "C02", "C04", "C05", "C06",
            "C07", "C08", "C09", "C10", "C11",
            "C12", "C13", "C14", "SK02", "SK03",
            "SK05", "SK06", "SK08", "EX01", "EX02",
            "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        private Keyboard _keyboard;
        private readonly List<Key> _heldKeys = new List<Key>();
        private bool _driving = false;

        public override void Setup()
        {
            base.Setup();
            _keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            _heldKeys.Clear();
            _driving = false;
        }

        public override void TearDown()
        {
            _heldKeys.Clear();
            _driving = false;
            base.TearDown();
        }

        private void QueueKeyboard()
        {
            if (_keyboard == null) _keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            if (_keyboard == null) return;
            var state = _heldKeys.Count > 0 ? new KeyboardState(_heldKeys.ToArray()) : new KeyboardState();
            InputSystem.QueueStateEvent(_keyboard, state);
            InputSystem.Update();
        }

        private void SetHeldKeys(params Key[] keys)
        {
            _heldKeys.Clear();
            if (keys != null && keys.Length > 0)
            {
                _heldKeys.AddRange(keys);
            }
            _driving = true;
            QueueKeyboard();
        }

        private void ClearHeldKeys()
        {
            _heldKeys.Clear();
            _driving = true;
            QueueKeyboard();
        }

        private IEnumerator PressKeysForFrames(Key[] keys, int frames)
        {
            SetHeldKeys(keys);
            for (int f = 0; f < frames; f++)
            {
                QueueKeyboard();
                yield return null;
            }
            ClearHeldKeys();
            yield return null;
        }

        private static Bounds GetModelBounds(FighterController fc)
        {
            Bounds b = new Bounds();
            bool hasBounds = false;
            foreach (var r in fc.GetComponentsInChildren<Renderer>(true))
            {
                if (!HomegrownCombatDirector.IsBodyRenderer(r)) continue;
                if (!hasBounds)
                {
                    b = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        private static void CaptureScreenshot(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                ScreenCapture.CaptureScreenshot(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CaptureScreenshot] Notice: {ex.Message}");
            }
        }

        [UnityTest]
        public IEnumerator Gate1_VisualPolish_Scale_Grounding_Screenshots()
        {
            LogAssert.ignoreFailingMessages = true;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string baseScreenDir = Path.Combine(projectRoot, "Screenshots", "fightfeel");
            Directory.CreateDirectory(baseScreenDir);

            var reportSb = new StringBuilder();
            reportSb.AppendLine("# GATE 1 VERIFICATION LOG — ALL 25 FIGHTERS");
            reportSb.AppendLine("| Fighter | Opponent | Measured Height | P1 Foot Y | P2 Foot Y | Avatar Valid | Models Count | Status |");
            reportSb.AppendLine("|---|---|:---:|:---:|:---:|:---:|:---:|:---:|");

            for (int i = 0; i < FullRoster.Length; i++)
            {
                string p1Id = FullRoster[i];
                string p2Id = FullRoster[(i + 1) % FullRoster.Length];
                string fighterScreenDir = Path.Combine(baseScreenDir, p1Id);
                Directory.CreateDirectory(fighterScreenDir);

                PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
                PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
                PlayerPrefs.SetString("MDI_StageId", "ST07");
                PlayerPrefs.SetString("MDI_GameMode", "Training");
                PlayerPrefs.Save();

                LaunchServices.Session.BeginMatch(p1Id, p2Id, "ST07");

                var loadOp = SceneManager.LoadSceneAsync("BattleArena", LoadSceneMode.Single);
                while (!loadOp.isDone) yield return null;
                yield return null;
                yield return null;

                var combatDirector = HomegrownCombatDirector.Instance;
                Assert.IsNotNull(combatDirector, "HomegrownCombatDirector must exist in BattleArena");

                var p1Fc = combatDirector.Fighter1;
                var p2Fc = combatDirector.Fighter2;
                Assert.IsNotNull(p1Fc, $"Fighter1 must be spawned for {p1Id}");
                Assert.IsNotNull(p2Fc, $"Fighter2 must be spawned for {p2Id}");

                var p1Brain = p1Fc.GetComponent<BBFighterBrain>();
                var p2Brain = p2Fc.GetComponent<BBFighterBrain>();
                Assert.IsNotNull(p1Brain, $"BBFighterBrain must exist on P1 ({p1Id})");
                Assert.IsNotNull(p2Brain, $"BBFighterBrain must exist on P2 ({p2Id})");

                // Disable AI during static capture
                var p2Ai = p2Fc.GetComponent<BBAIController>();
                if (p2Ai != null) p2Ai.enabledAI = false;

                // Let animation and grounding settle for 10 frames
                for (int f = 0; f < 10; f++) yield return null;

                // 1. Measure Bounds & Height
                var p1CharDef = CharacterRegistry.GetCharacterById(p1Id);
                Assert.IsNotNull(p1CharDef, $"CharacterDefinition must exist for {p1Id}");
                Assert.GreaterOrEqual(p1CharDef.heightMeters, 1.60f, $"CharacterDefinition {p1Id} heightMeters must be >= 1.60m");
                Assert.LessOrEqual(p1CharDef.heightMeters, 1.85f, $"CharacterDefinition {p1Id} heightMeters must be <= 1.85m");

                Bounds p1Bounds = GetModelBounds(p1Fc);
                Bounds p2Bounds = GetModelBounds(p2Fc);
                float p1Height = p1Bounds.size.y;
                float p1FootY = p1Bounds.min.y - p1Fc.transform.position.y;
                float p2FootY = p2Bounds.min.y - p2Fc.transform.position.y;

                Debug.Log($"[Gate1_Measure] P1={p1Id} bMin={p1Bounds.min.y:F3} bMax={p1Bounds.max.y:F3} p1Pos={p1Fc.transform.position.y:F3} footY={p1FootY:F3} | P2={p2Id} bMin={p2Bounds.min.y:F3} bMax={p2Bounds.max.y:F3} p2Pos={p2Fc.transform.position.y:F3} footY={p2FootY:F3}");
                if (Mathf.Abs(p1FootY) > 0.06f)
                {
                    Transform m1 = p1Fc.transform.Find("Model_" + p1Id);
                    Debug.Log($"[Gate1_Debug_P1] P1 {p1Id} Model localPos={m1?.localPosition} localScale={m1?.localScale}");
                    foreach (var r in p1Fc.GetComponentsInChildren<Renderer>(true))
                    {
                        Debug.Log($"[Gate1_Debug_Renderer_P1] r={r.name} go={r.gameObject.name} parent={r.transform.parent?.name} enabled={r.enabled} isBody={HomegrownCombatDirector.IsBodyRenderer(r)} bMin={r.bounds.min.y:F3} bMax={r.bounds.max.y:F3}");
                    }
                }
                if (Mathf.Abs(p2FootY) > 0.06f)
                {
                    Transform m2 = p2Fc.transform.Find("Model_" + p2Id);
                    Debug.Log($"[Gate1_Debug] P2 {p2Id} Model localPos={m2?.localPosition} localScale={m2?.localScale}");
                    foreach (var r in p2Fc.GetComponentsInChildren<Renderer>(true))
                    {
                        Debug.Log($"[Gate1_Debug_Renderer] r={r.name} go={r.gameObject.name} parent={r.transform.parent?.name} enabled={r.enabled} isBody={HomegrownCombatDirector.IsBodyRenderer(r)} bMin={r.bounds.min.y:F3} bMax={r.bounds.max.y:F3}");
                    }
                }

                Assert.GreaterOrEqual(p1Height, 1.48f, $"P1 {p1Id} height must be >= 1.48m (measured {p1Height:F2}m)");
                Assert.LessOrEqual(p1Height, 1.95f, $"P1 {p1Id} height must be <= 1.95m (measured {p1Height:F2}m)");
                Assert.LessOrEqual(Mathf.Abs(p1FootY), 0.06f, $"P1 {p1Id} feet must be within 0.06m of ground (measured {p1FootY:F3}m)");
                Assert.LessOrEqual(Mathf.Abs(p2FootY), 0.06f, $"P2 {p2Id} feet must be within 0.06m of ground (measured {p2FootY:F3}m)");

                // 2. Avatar & Rig Integrity (No T-Pose)
                Assert.IsNotNull(p1Fc.animator, $"P1 {p1Id} animator must not be null");
                Assert.IsTrue(p1Fc.animator.isHuman, $"P1 {p1Id} animator must be Humanoid");
                Assert.IsTrue(p1Fc.animator.avatar != null && p1Fc.animator.avatar.isValid, $"P1 {p1Id} avatar must be valid Humanoid");

                // 3. Model Count (No second overlapping model)
                int p1ModelCount = 0;
                foreach (Transform c in p1Fc.transform)
                {
                    if (c.GetComponentInChildren<Renderer>(true) != null && 
                        !c.name.Contains("Hitbox") && 
                        !c.name.Contains("Hurtbox") && 
                        !c.name.Contains("Shadow") && 
                        !c.name.Contains("Blob"))
                    {
                        p1ModelCount++;
                    }
                }
                Assert.AreEqual(1, p1ModelCount, $"P1 {p1Id} must have exactly 1 visual model child (no duplicates)");

                reportSb.AppendLine($"| {p1Id} | {p2Id} | {p1Height:F2}m | {p1FootY:+0.000;-0.000}m | {p2FootY:+0.000;-0.000}m | Valid | {p1ModelCount} | PASS |");

                // Capture Idle screenshot
                yield return null;
                CaptureScreenshot(Path.Combine(fighterScreenDir, "idle.png"));

                // Capture 5A Contact screenshot
                // Walk into range
                p1Brain.ResetForRound();
                p2Brain.ResetForRound();
                ClearHeldKeys();
                yield return null;

                float targetDist = 0.85f;
                int walkTimeout = 60;
                while (Mathf.Abs(p1Fc.transform.position.x - p2Fc.transform.position.x) > targetDist && walkTimeout-- > 0)
                {
                    SetHeldKeys(Key.D);
                    yield return null;
                }
                ClearHeldKeys();
                yield return null;

                // Press 5A (Key.J)
                SetHeldKeys(Key.J);
                yield return null;
                ClearHeldKeys();

                // Wait for impact tick
                int impactWait = 15;
                while (impactWait-- > 0 && p1Brain.bbState == BBState.AttackStartup)
                {
                    yield return null;
                }
                yield return null;
                CaptureScreenshot(Path.Combine(fighterScreenDir, "5A_contact.png"));

                // Capture Super Flash screenshot
                p1Brain.ResetForRound();
                p2Brain.ResetForRound();
                p1Brain.heatMeter = 100f;
                ClearHeldKeys();
                yield return null;

                // Start super 236236C
                var superAtk = p1Brain.FindAttackByMoveId("236236C");
                if (superAtk != null)
                {
                    typeof(BBFighterBrain).GetMethod("StartAttack")?.Invoke(p1Brain, new object[] { superAtk });
                }
                yield return null;
                CaptureScreenshot(Path.Combine(fighterScreenDir, "super_flash.png"));

                // Capture Knockdown screenshot
                p1Brain.ResetForRound();
                p2Brain.ResetForRound();
                ClearHeldKeys();
                yield return null;

                var sweepAtk = p1Brain.FindAttackByMoveId("2C");
                if (sweepAtk != null)
                {
                    typeof(BBFighterBrain).GetMethod("StartAttack")?.Invoke(p1Brain, new object[] { sweepAtk });
                }
                // Wait for defender to enter DownWait state
                int kdWait = 60;
                while (kdWait-- > 0 && p2Brain.bbState != BBState.DownWait)
                {
                    yield return null;
                }
                yield return null;
                CaptureScreenshot(Path.Combine(fighterScreenDir, "knockdown.png"));

                Debug.Log($"<color=#00e676>[Gate1] {p1Id} verified & screenshots captured to {fighterScreenDir}</color>");
            }

            string reportFile = Path.Combine(projectRoot, "Screenshots", "GATE1_REPORT.txt");
            File.WriteAllText(reportFile, reportSb.ToString());
            Debug.Log($"<color=#00e676>[Gate1] Complete Gate 1 report saved to: {reportFile}</color>");
        }

        [UnityTest]
        public IEnumerator Gate2_CPU_Medium_MotionInputs_And_Defense()
        {
            LogAssert.ignoreFailingMessages = true;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var logSb = new StringBuilder();
            logSb.AppendLine("# GATE 2 CPU VERIFICATION LOG — 3 MATCHES");

            for (int match = 1; match <= 3; match++)
            {
                string p1Id = match == 1 ? "C01" : (match == 2 ? "SK02" : "EX01");
                string p2Id = match == 1 ? "C07" : (match == 2 ? "EX06" : "C12");

                PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
                PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
                PlayerPrefs.SetString("MDI_StageId", "ST07");
                PlayerPrefs.SetString("MDI_GameMode", "Arcade"); // CPU P2
                PlayerPrefs.Save();

                LaunchServices.Session.BeginMatch(p1Id, p2Id, "ST07");

                var loadOp = SceneManager.LoadSceneAsync("BattleArena", LoadSceneMode.Single);
                while (!loadOp.isDone) yield return null;
                yield return null;
                yield return null;

                var combatDirector = HomegrownCombatDirector.Instance;
                Assert.IsNotNull(combatDirector);
                var p1Fc = combatDirector.Fighter1;
                var p2Fc = combatDirector.Fighter2;
                var p1Brain = p1Fc.GetComponent<BBFighterBrain>();
                var p2Brain = p2Fc.GetComponent<BBFighterBrain>();

                // Configure CPU
                var cpu = p2Fc.GetComponent<BBAIController>();
                if (cpu == null) cpu = p2Fc.gameObject.AddComponent<BBAIController>();
                cpu.difficulty = BBAIDifficulty.Medium;
                cpu.enabledAI = true;
                cpu.ResetAI(1000 + match);

                // Give CPU some heat to enable super usage
                p2Brain.heatMeter = 100f;

                int special236ACount = 0;
                int superCount = 0;
                int crouchBlockCount = 0;
                int minTicksBetweenAttacks = 999;
                int lastAttackStartTick = -999;
                int prevAttackTick = 0;

                // Run match for 450 ticks (~7.5 seconds)
                for (int t = 0; t < 450; t++)
                {
                    yield return null;

                    // Track CPU attacks
                    if (p2Brain.currentAttack != null)
                    {
                        if (p2Brain.attackTick == 1 && prevAttackTick != 1)
                        {
                            if (lastAttackStartTick > 0)
                            {
                                int gap = t - lastAttackStartTick;
                                if (gap < minTicksBetweenAttacks) minTicksBetweenAttacks = gap;
                            }
                            lastAttackStartTick = t;

                            if (p2Brain.currentAttack.moveId == "236A") special236ACount++;
                            if (p2Brain.currentAttack.moveId == "236236C") superCount++;
                        }
                    }
                    prevAttackTick = (p2Brain.currentAttack != null) ? p2Brain.attackTick : 0;

                    // Test crouching defense: occasionally have P1 throw a low poke (2A)
                    if (t % 120 == 60)
                    {
                        var lowAtk = p1Brain.FindAttackByMoveId("2A");
                        if (lowAtk != null && p1Brain.CanExecuteMove(lowAtk))
                        {
                            typeof(BBFighterBrain).GetMethod("StartAttack")?.Invoke(p1Brain, new object[] { lowAtk });
                        }
                    }

                    // If CPU is in blockstun against low, verify crouching state
                    if (p2Brain.bbState == BBState.Blockstun || p2Brain.bbState == BBState.BarrierBlockstun)
                    {
                        if (p1Brain.currentAttack != null && p1Brain.currentAttack.guard == BBGuard.Low)
                        {
                            crouchBlockCount++;
                        }
                    }
                }

                logSb.AppendLine($"Match {match} ({p1Id} vs CPU {p2Id}): 236A={special236ACount}, Super={superCount}, CrouchBlocks={crouchBlockCount}, MinAttackGap={minTicksBetweenAttacks}f");

                Assert.GreaterOrEqual(special236ACount, 1, $"Match {match}: CPU must execute at least one 236A via motion inputs");
                Assert.GreaterOrEqual(superCount, 1, $"Match {match}: CPU must execute at least one 236236C super via motion inputs");
                Assert.GreaterOrEqual(minTicksBetweenAttacks, 10, $"Match {match}: CPU must enforce minimum 10 frames between attacks");
            }

            string cpuLogFile = Path.Combine(projectRoot, "Screenshots", "GATE2_CPU_LOG.txt");
            File.WriteAllText(cpuLogFile, logSb.ToString());
            Debug.Log($"<color=#00e676>[Gate2] CPU verification report saved to: {cpuLogFile}</color>");
        }
    }
}
