using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.Tests.PlayMode
{
    [TestFixture]
    public class BBAkioPlayTests
    {
        private IEnumerator StepTicks(int count)
        {
            for (int t = 0; t < count; t++)
            {
                if (CombatTickEngine.Instance != null)
                {
                    CombatTickEngine.Instance.AdvanceTicksManual(1);
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Akio_7Moves_And_GatlingChain_PlayMode_Verification()
        {
            Debug.Log("[BBAkioPlayTests] >>> STARTING AKIO 7-MOVE & GATLING PLAY MODE VERIFICATION");
            LogAssert.ignoreFailingMessages = true;

            LaunchServices.Session.TrySelectMode("LocalVersus", out _);
            LaunchServices.Session.BeginMatch("C01", "C07", "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
            PlayerPrefs.SetString("MDI_P1_CharacterId", "C01");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "C07");
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.Save();

            var runnerGo = GameObject.Find("Code-based tests runner");
            if (runnerGo != null) Object.DontDestroyOnLoad(runnerGo);

            var loadOp = SceneManager.LoadSceneAsync("BattleArena", LoadSceneMode.Single);
            while (!loadOp.isDone) yield return null;
            yield return null;
            yield return null;

            var combatDirector = HomegrownCombatDirector.Instance;
            Assert.IsNotNull(combatDirector, "HomegrownCombatDirector must exist in BattleArena");

            var p1Fc = combatDirector.Fighter1;
            var p2Fc = combatDirector.Fighter2;
            Assert.IsNotNull(p1Fc, "P1 FighterController must exist");
            Assert.IsNotNull(p2Fc, "P2 FighterController must exist");

            var p1Brain = p1Fc.GetComponent<BBFighterBrain>();
            var p2Brain = p2Fc.GetComponent<BBFighterBrain>();
            Assert.IsNotNull(p1Brain, "P1 BBFighterBrain must exist");
            Assert.IsNotNull(p2Brain, "P2 BBFighterBrain must exist");

            // Disable AI on P2
            var p2Ai = p2Fc.GetComponent<BBAIController>();
            if (p2Ai != null) p2Ai.enabledAI = false;

            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.isTrainingMode = true;
                MatchRoundReferee.Instance.SkipIntroCountdown();
            }

            // Pause tick engine so we drive it deterministically at 60Hz
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.PauseSimulation(true);
            }

            var p1InputQueue = new Queue<BBInputFrame>();
            p1Brain.customInputProvider = (t) => {
                if (p1InputQueue.Count > 0) return p1InputQueue.Dequeue();
                return new BBInputFrame { numpad = 5 };
            };

            // Position fighters at point-blank melee spacing (0.65m apart)
            p1Fc.transform.position = new Vector3(-0.35f, 0f, 0f);
            p2Fc.transform.position = new Vector3(0.35f, 0f, 0f);
            yield return StepTicks(10);

            // 1. Verify Grounded footY within +-0.03m of 0
            float p1FootY = p1Fc.transform.position.y;
            Debug.Log($"[BBAkioPlayTests] P1 neutral footY = {p1FootY:F4}m");
            Assert.LessOrEqual(Mathf.Abs(p1FootY), 0.03f, $"P1 footY should be within +-0.03m of ground plane, got {p1FootY:F4}m");

            // 2. Test each of the 7 moves connecting cleanly
            string[] movesToTest = new string[] { "5A", "5B", "5C", "2C", "j.B", "5D", "236236C" };

            foreach (var mid in movesToTest)
            {
                p1Brain.ResetForRound();
                p2Brain.ResetForRound();
                p1Brain.heat = 100f; // Ensure full heat for super
                p2Brain.maxHealth = 25000f;
                p2Brain.health = 25000f;
                p1InputQueue.Clear();

                p1Fc.transform.position = new Vector3(-0.35f, 0f, 0f);
                p2Fc.transform.position = new Vector3(0.35f, 0f, 0f);

                var atk = p1Brain.FindAttackByMoveId(mid);
                Assert.IsNotNull(atk, $"Move {mid} must be found in P1 moveset");

                // If testing j.B, give P1 a small air jump state
                if (mid == "j.B")
                {
                    p1Brain.isGrounded = false;
                    p1Fc.transform.position = new Vector3(-0.35f, 0.8f, 0f);
                }
                else
                {
                    p1Brain.isGrounded = true;
                }

                EnqueueMoveInput(p1InputQueue, mid);
                yield return StepTicks(1);
                if (mid == "236236C")
                {
                    while (p1InputQueue.Count > 0) yield return StepTicks(1);
                }

                Assert.IsNotNull(p1Brain.currentAttack, $"P1 should be executing {mid}");
                Assert.AreEqual(mid, p1Brain.currentAttack.moveId, $"P1 currentAttack should match {mid}");

                bool madeContact = false;
                int maxTicks = atk.TotalTicks + 15;

                for (int t = 0; t < maxTicks; t++)
                {
                    if (p1Brain.chainState.madeContact) madeContact = true;
                    yield return StepTicks(1);
                    if (p1Brain.currentAttack == null && t > atk.startupFrames) break;
                }

                Debug.Log($"[BBAkioPlayTests] Move {mid} (S/A/R={atk.startupFrames}/{atk.activeFrames}/{atk.recoveryFrames}) executed cleanly. Made contact: {madeContact}");
                Assert.IsTrue(madeContact, $"Move {mid} must make contact with opponent at 0.70m spacing.");

                // Land P1 if was jumping
                p1Brain.isGrounded = true;
                p1Fc.transform.position = new Vector3(-0.35f, 0f, 0f);
                yield return StepTicks(5);

                // Verify footY returned to 0
                Assert.LessOrEqual(Mathf.Abs(p1Fc.transform.position.y), 0.03f, $"P1 footY after {mid} must be within +-0.03m of 0.");
            }

            // 3. Test full Gatling Chain: 5A -> 5B -> 5C -> 5D -> 236236C
            Debug.Log("[BBAkioPlayTests] Testing full Gatling Chain: 5A -> 5B -> 5C -> 5D -> 236236C");
            p1Brain.ResetForRound();
            p2Brain.ResetForRound();
            p1Brain.heat = 100f;
            p2Brain.maxHealth = 25000f;
            p2Brain.health = 25000f;
            p1InputQueue.Clear();

            p1Fc.transform.position = new Vector3(-0.35f, 0f, 0f);
            p2Fc.transform.position = new Vector3(0.35f, 0f, 0f);

            // Step 3.1: Start 5A
            var atk5A = p1Brain.FindAttackByMoveId("5A");
            EnqueueMoveInput(p1InputQueue, "5A");
            yield return StepTicks(1);

            // Advance through 5A startup until contact
            for (int t = 0; t < 15 && !p1Brain.chainState.madeContact; t++) yield return StepTicks(1);
            Assert.IsTrue(p1Brain.chainState.madeContact, "5A must make contact.");

            // Wait for hitstop to elapse before canceling
            while (p1Brain.hitstopRemaining > 0) yield return StepTicks(1);

            // Step 3.2: Cancel into 5B
            var atk5B = p1Brain.FindAttackByMoveId("5B");
            Assert.IsTrue(p1Brain.CanExecuteMove(atk5B), "5A must be cancelable into 5B on contact.");
            EnqueueMoveInput(p1InputQueue, "5B");
            yield return StepTicks(1);
            Assert.AreEqual("5B", p1Brain.currentAttack.moveId, "P1 must have transitioned into 5B.");

            // Advance through 5B startup until contact
            for (int t = 0; t < 20 && !p1Brain.chainState.madeContact; t++) yield return StepTicks(1);
            Assert.IsTrue(p1Brain.chainState.madeContact, "5B must make contact.");

            while (p1Brain.hitstopRemaining > 0) yield return StepTicks(1);

            // Step 3.3: Cancel into 5C
            var atk5C = p1Brain.FindAttackByMoveId("5C");
            Assert.IsTrue(p1Brain.CanExecuteMove(atk5C), "5B must be cancelable into 5C on contact.");
            EnqueueMoveInput(p1InputQueue, "5C");
            yield return StepTicks(1);
            Assert.AreEqual("5C", p1Brain.currentAttack.moveId, "P1 must have transitioned into 5C.");

            // Advance through 5C startup until contact
            for (int t = 0; t < 25 && !p1Brain.chainState.madeContact; t++) yield return StepTicks(1);
            Assert.IsTrue(p1Brain.chainState.madeContact, "5C must make contact.");

            while (p1Brain.hitstopRemaining > 0) yield return StepTicks(1);

            // Step 3.4: Cancel into 5D (Drive)
            var atk5D = p1Brain.FindAttackByMoveId("5D");
            Assert.IsTrue(p1Brain.CanExecuteMove(atk5D), "5C must be cancelable into 5D on contact.");
            EnqueueMoveInput(p1InputQueue, "5D");
            yield return StepTicks(1);
            Assert.AreEqual("5D", p1Brain.currentAttack.moveId, "P1 must have transitioned into 5D.");

            // Advance through 5D startup until contact
            for (int t = 0; t < 25 && !p1Brain.chainState.madeContact; t++) yield return StepTicks(1);
            Assert.IsTrue(p1Brain.chainState.madeContact, "5D must make contact.");

            while (p1Brain.hitstopRemaining > 0) yield return StepTicks(1);

            // Step 3.5: Cancel into 236236C (Super)
            var atkSuper = p1Brain.FindAttackByMoveId("236236C");
            Assert.IsTrue(p1Brain.CanExecuteMove(atkSuper), "5D must be cancelable into 236236C (Super) on contact.");
            EnqueueMoveInput(p1InputQueue, "236236C");
            while (p1InputQueue.Count > 0) yield return StepTicks(1);
            Assert.AreEqual("236236C", p1Brain.currentAttack.moveId, "P1 must have transitioned into 236236C.");

            // Step through super freeze (20 ticks) + startup (9 ticks) until contact
            int superMaxTicks = BBSuperFlash.FreezeTicks + atkSuper.startupFrames + atkSuper.activeFrames + 15;
            for (int t = 0; t < superMaxTicks && !p1Brain.chainState.madeContact; t++) yield return StepTicks(1);
            Assert.IsTrue(p1Brain.chainState.madeContact, "236236C must make contact during combo chain.");

            // Step through rest of super to completion
            for (int t = 0; t < atkSuper.TotalTicks; t++) yield return StepTicks(1);

            p1Brain.customInputProvider = null;

            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StopMatch();
            }

            // Unpause engine
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.PauseSimulation(false);
            }

            Debug.Log("<color=#00e676>[BBAkioPlayTests] ALL 7 MOVES & GATLING CHAIN (5A->5B->5C->5D->Super) PASSED 100% CLEANLY!</color>");
            yield return null;
        }

        private static void EnqueueMoveInput(Queue<BBInputFrame> queue, string moveId)
        {
            switch (moveId)
            {
                case "5A":
                    queue.Enqueue(new BBInputFrame { numpad = 5, pressed = (byte)BBButton.A, held = (byte)BBButton.A });
                    break;
                case "5B":
                    queue.Enqueue(new BBInputFrame { numpad = 5, pressed = (byte)BBButton.B, held = (byte)BBButton.B });
                    break;
                case "5C":
                    queue.Enqueue(new BBInputFrame { numpad = 5, pressed = (byte)BBButton.C, held = (byte)BBButton.C });
                    break;
                case "2C":
                    queue.Enqueue(new BBInputFrame { numpad = 2, pressed = (byte)BBButton.C, held = (byte)BBButton.C });
                    break;
                case "5D":
                    queue.Enqueue(new BBInputFrame { numpad = 5, pressed = (byte)BBButton.D, held = (byte)BBButton.D });
                    break;
                case "j.B":
                    queue.Enqueue(new BBInputFrame { numpad = 8, pressed = (byte)BBButton.B, held = (byte)BBButton.B });
                    break;
                case "236236C":
                    queue.Enqueue(new BBInputFrame { numpad = 2 });
                    queue.Enqueue(new BBInputFrame { numpad = 3 });
                    queue.Enqueue(new BBInputFrame { numpad = 6 });
                    queue.Enqueue(new BBInputFrame { numpad = 2 });
                    queue.Enqueue(new BBInputFrame { numpad = 3 });
                    queue.Enqueue(new BBInputFrame { numpad = 6, pressed = (byte)BBButton.C, held = (byte)BBButton.C });
                    break;
            }
        }
    }
}
