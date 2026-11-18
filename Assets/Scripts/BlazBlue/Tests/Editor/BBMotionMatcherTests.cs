#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBMotionMatcherTests
    {
        [MenuItem("Demon Idol/BlazBlue/Run BBMotionMatcher Tests")]
        public static void RunAllTestsMenu()
        {
            var suite = new BBMotionMatcherTests();
            suite.Test_236_QuarterCircleForward();
            suite.Test_214_QuarterCircleBack();
            suite.Test_623_DragonPunch_StandardAndShortcuts();
            suite.Test_421_ReverseDragonPunch();
            suite.Test_HalfCircles_41236_and_63214();
            suite.Test_236236_DoubleQuarterCircle();
            suite.Test_632146_DistortionDrive();
            suite.Test_360_CommandGrab();
            suite.Test_22_DownDown();
            suite.Test_DoubleTap_Dash();
            suite.Test_ChargeBackForward();
            suite.Test_InputHistory_NegativeEdge();
            suite.Test_CommandInterpreter_PriorityHierarchy();
            Debug.Log("<color=#00e676>[BBMotionMatcherTests] ALL 13 MOTION MATCHER & INPUT TESTS PASSED!</color>");
        }
        private BBMotionEdge[] CreateEdges(params (sbyte numpad, int tick)[] inputs)
        {
            var edges = new BBMotionEdge[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                // Inputs given in chronological order; store in reverse chronological (0 is newest)
                int revIdx = inputs.Length - 1 - i;
                edges[revIdx] = new BBMotionEdge(inputs[i].numpad, inputs[i].tick);
            }
            return edges;
        }

        [Test]
        public void Test_236_QuarterCircleForward()
        {
            // 2 (tick 10) -> 3 (tick 15) -> 6 (tick 20)
            var edges = CreateEdges((2, 10), (3, 15), (6, 20));
            Assert.IsTrue(BBMotionMatcher.Is236(edges, edges.Length, currentTick: 20));

            // Window exceeded (> 22 ticks)
            Assert.IsFalse(BBMotionMatcher.Is236(edges, edges.Length, currentTick: 45));
        }

        [Test]
        public void Test_214_QuarterCircleBack()
        {
            // 2 (tick 10) -> 1 (tick 14) -> 4 (tick 18)
            var edges = CreateEdges((2, 10), (1, 14), (4, 18));
            Assert.IsTrue(BBMotionMatcher.Is214(edges, edges.Length, currentTick: 18));
        }

        [Test]
        public void Test_623_DragonPunch_StandardAndShortcuts()
        {
            // Standard: 6 -> 2 -> 3
            var std = CreateEdges((6, 5), (2, 10), (3, 15));
            Assert.IsTrue(BBMotionMatcher.Is623(std, std.Length, currentTick: 15));

            // Running DP: 6 -> 3 -> 6
            var run = CreateEdges((6, 5), (3, 10), (6, 15));
            Assert.IsTrue(BBMotionMatcher.Is623(run, run.Length, currentTick: 15));

            // Crouch shortcut: 3 -> 2 -> 3
            var crouch = CreateEdges((3, 5), (2, 10), (3, 15));
            Assert.IsTrue(BBMotionMatcher.Is623(crouch, crouch.Length, currentTick: 15, allow323Shortcut: true));
        }

        [Test]
        public void Test_421_ReverseDragonPunch()
        {
            // 4 -> 2 -> 1
            var edges = CreateEdges((4, 5), (2, 10), (1, 15));
            Assert.IsTrue(BBMotionMatcher.Is421(edges, edges.Length, currentTick: 15));
        }

        [Test]
        public void Test_HalfCircles_41236_and_63214()
        {
            // 41236 (HCF)
            var hcf = CreateEdges((4, 5), (1, 8), (2, 11), (3, 15), (6, 20));
            Assert.IsTrue(BBMotionMatcher.Is41236(hcf, hcf.Length, currentTick: 20));

            // 63214 (HCB)
            var hcb = CreateEdges((6, 5), (3, 8), (2, 11), (1, 15), (4, 20));
            Assert.IsTrue(BBMotionMatcher.Is63214(hcb, hcb.Length, currentTick: 20));
        }

        [Test]
        public void Test_236236_DoubleQuarterCircle()
        {
            // 2 -> 3 -> 6 -> 2 -> 3 -> 6
            var dd = CreateEdges((2, 5), (3, 8), (6, 12), (2, 18), (3, 22), (6, 26));
            Assert.IsTrue(BBMotionMatcher.Is236236(dd, dd.Length, currentTick: 26));
        }

        [Test]
        public void Test_632146_DistortionDrive()
        {
            // 6 -> 3 -> 2 -> 1 -> 4 -> 6
            var dd = CreateEdges((6, 5), (3, 8), (2, 12), (1, 16), (4, 20), (6, 25));
            Assert.IsTrue(BBMotionMatcher.Is632146(dd, dd.Length, currentTick: 25));
        }

        [Test]
        public void Test_360_CommandGrab()
        {
            // 6 -> 2 -> 4 -> 8 (clockwise)
            var cw = CreateEdges((6, 5), (2, 10), (4, 15), (8, 20));
            Assert.IsTrue(BBMotionMatcher.Is360(cw, cw.Length, currentTick: 20));

            // 4 -> 2 -> 6 -> 9 (counter-clockwise with diagonal up jump forgiveness)
            var ccw = CreateEdges((4, 5), (2, 10), (6, 15), (9, 20));
            Assert.IsTrue(BBMotionMatcher.Is360(ccw, ccw.Length, currentTick: 20));
        }

        [Test]
        public void Test_22_DownDown()
        {
            // 2 -> 5 -> 2
            var dd = CreateEdges((2, 5), (5, 9), (2, 13));
            Assert.IsTrue(BBMotionMatcher.Is22(dd, dd.Length, currentTick: 13));
        }

        [Test]
        public void Test_DoubleTap_Dash()
        {
            // 6 -> 5 -> 6 within 14 ticks
            var dash = CreateEdges((6, 2), (5, 6), (6, 10));
            Assert.IsTrue(BBMotionMatcher.IsDoubleTap(dash, dash.Length, currentTick: 10, targetNumpad: 6));

            // Backdash: 4 -> 5 -> 4
            var back = CreateEdges((4, 2), (5, 6), (4, 10));
            Assert.IsTrue(BBMotionMatcher.IsDoubleTap(back, back.Length, currentTick: 10, targetNumpad: 4));
        }

        [Test]
        public void Test_ChargeBackForward()
        {
            // 4 held for 42 ticks (tick 10 to 52), then 6 at tick 55
            var valid = CreateEdges((4, 10), (6, 55));
            Assert.IsTrue(BBMotionMatcher.IsChargeBackForward(valid, valid.Length, currentTick: 55, holdTicks: 40, releaseTicks: 12));

            // 4 held for only 15 ticks (insufficient charge)
            var invalid = CreateEdges((4, 40), (6, 55));
            Assert.IsFalse(BBMotionMatcher.IsChargeBackForward(invalid, invalid.Length, currentTick: 55, holdTicks: 40, releaseTicks: 12));
        }

        [Test]
        public void Test_InputHistory_NegativeEdge()
        {
            var history = new BBInputHistory();

            // Tick 1-5: Button A held
            for (int t = 1; t <= 5; t++)
            {
                byte pressed = (t == 1) ? (byte)BBButton.A : (byte)0;
                history.PushFrame(new BBInputFrame { held = (byte)BBButton.A, pressed = pressed, numpad = 5 }, t, 1);
            }

            // Tick 6: Button A released
            history.PushFrame(new BBInputFrame { held = 0, pressed = 0, released = (byte)BBButton.A, numpad = 5 }, 6, 1);

            // Tick 7: Negative edge should be buffered
            Assert.IsTrue(history.IsNegativeEdgeBuffered(BBButton.A, negativeEdgeTicks: 3));

            // Tick 12: Negative edge should have expired
            history.PushFrame(new BBInputFrame { numpad = 5 }, 12, 1);
            Assert.IsFalse(history.IsNegativeEdgeBuffered(BBButton.A, negativeEdgeTicks: 3));
        }

        [Test]
        public void Test_CommandInterpreter_PriorityHierarchy()
        {
            var interpreter = new BBCommandInterpreter();
            var history = new BBInputHistory();

            // 1. Test Macro Priority: A+B+C+D should evaluate to BurstOrOverdrive
            history.PushFrame(new BBInputFrame
            {
                held = (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D),
                pressed = (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D),
                numpad = 5
            }, tick: 1, facingDirection: 1);

            var cmd = interpreter.Evaluate(history, 1, 1, isGrounded: true, isInAttack: false, isInBlockstun: false, isGuarding: false);
            Assert.AreEqual(BBCommandType.BurstOrOverdrive, cmd.commandType);

            // 2. Test 236 + C produces Special236
            history.Clear();
            history.PushFrame(new BBInputFrame { numpad = 2 }, 1, 1);
            history.PushFrame(new BBInputFrame { numpad = 3 }, 5, 1);
            history.PushFrame(new BBInputFrame { numpad = 6, pressed = (byte)BBButton.C, held = (byte)BBButton.C }, 9, 1);

            cmd = interpreter.Evaluate(history, 9, 1, isGrounded: true, isInAttack: false, isInBlockstun: false, isGuarding: false);
            Assert.AreEqual(BBCommandType.Special236, cmd.commandType);
            Assert.AreEqual(BBButton.C, cmd.button);

            // 3. Test Throw (B+C)
            history.Clear();
            history.PushFrame(new BBInputFrame
            {
                held = (byte)(BBButton.B | BBButton.C),
                pressed = (byte)(BBButton.B | BBButton.C),
                numpad = 6
            }, 10, 1);

            cmd = interpreter.Evaluate(history, 10, 1, isGrounded: true, isInAttack: false, isInBlockstun: false, isGuarding: false);
            Assert.AreEqual(BBCommandType.Throw, cmd.commandType);

            // 4. Test Standing Normal (5A)
            history.Clear();
            history.PushFrame(new BBInputFrame
            {
                held = (byte)BBButton.A,
                pressed = (byte)BBButton.A,
                numpad = 5
            }, 12, 1);

            cmd = interpreter.Evaluate(history, 12, 1, isGrounded: true, isInAttack: false, isInBlockstun: false, isGuarding: false);
            Assert.AreEqual(BBCommandType.Normal5A, cmd.commandType);
        }
    }
}
#endif
