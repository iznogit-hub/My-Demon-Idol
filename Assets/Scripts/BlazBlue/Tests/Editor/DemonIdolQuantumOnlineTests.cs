#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
using Photon.Deterministic;
using Quantum;
using DemonIdol.Combat;
using DemonIdol.BlazBlue;
using DemonIdol.Network;

namespace DemonIdol.BlazBlue.Tests
{
    /// <summary>
    /// Verification and integrity tests for Photon Quantum Rollback Netcode integration.
    /// Covers 8-button deterministic codecs, fixed-point HP authority, rollback frame translation,
    /// QuantumCombatBridge lifecycle, and match settlement.
    /// </summary>
    public class DemonIdolQuantumOnlineTests
    {
        [Test]
        public void Test_QuantumAppIdValidation()
        {
            // Verify HasAppId executes safely without throwing or mutating settings
            bool hasAppId = BBQuantumInput.HasAppId();
            Debug.Log($"[DemonIdolQuantumOnlineTests] HasAppId check executed cleanly (App ID Configured: {hasAppId}).");
        }

        [Test]
        public void Test_QuantumInputCodec_Full8ButtonSync()
        {
            var original = new BBQuantumInputCodec.Buttons
            {
                Left = true,
                Right = false,
                Up = true,
                Down = true,
                A = true,
                B = false,
                C = true,
                D = true
            };

            ushort packed = BBQuantumInputCodec.Pack(original);
            var unpacked = BBQuantumInputCodec.Unpack(packed);

            Assert.AreEqual(original.Left, unpacked.Left, "Left direction mismatch");
            Assert.AreEqual(original.Right, unpacked.Right, "Right direction mismatch");
            Assert.AreEqual(original.Up, unpacked.Up, "Up direction mismatch");
            Assert.AreEqual(original.Down, unpacked.Down, "Down direction mismatch");
            Assert.AreEqual(original.A, unpacked.A, "Button A mismatch");
            Assert.AreEqual(original.B, unpacked.B, "Button B mismatch");
            Assert.AreEqual(original.C, unpacked.C, "Button C mismatch");
            Assert.AreEqual(original.D, unpacked.D, "Button D mismatch");
        }

        [Test]
        public void Test_QuantumInput_ToBBInputFrameConversion()
        {
            var buttons = new BBQuantumInputCodec.Buttons
            {
                Down = true,
                Right = true,
                A = true,
                C = true
            };

            // Down-Right is numpad 3
            byte prevHeld = (byte)BBButton.A; // A was already held, C is newly pressed
            BBInputFrame frame = BBQuantumInputCodec.ToBBInputFrame(buttons, prevHeld);

            Assert.AreEqual(3, frame.numpad, "Down-Right must map to numpad 3");
            Assert.IsTrue(frame.IsHeld(BBButton.A), "Button A must be held");
            Assert.IsTrue(frame.IsHeld(BBButton.C), "Button C must be held");
            Assert.IsFalse(frame.IsHeld(BBButton.B), "Button B must not be held");
            Assert.IsFalse(frame.IsHeld(BBButton.D), "Button D must not be held");

            // Edge latching: C is pressed, A is held (not pressed this tick)
            Assert.IsTrue(frame.IsPressed(BBButton.C), "Button C must register rising edge (pressed)");
            Assert.IsFalse(frame.IsPressed(BBButton.A), "Button A was already held, so pressed edge must be false");
            Assert.IsFalse(frame.IsReleased(BBButton.A), "Button A was not released");
        }

        [Test]
        public void Test_FixedPointBBMatchStateHPAuthority()
        {
            var hostGo = new GameObject("Test_OnlineSyncHost");
            try
            {
                var view = hostGo.AddComponent<BBMatchQuantumView>();

                var p1BrainGo = new GameObject("Test_P1Brain");
                p1BrainGo.transform.SetParent(hostGo.transform);
                var p1Brain = p1BrainGo.AddComponent<BBFighterBrain>();
                var p1Fc = p1BrainGo.AddComponent<FighterController>();
                p1Brain.fighter = p1Fc;
                p1Brain.maxHealth = 11000f;
                p1Brain.SetAuthoritativeHealth(11000f);

                var p2BrainGo = new GameObject("Test_P2Brain");
                p2BrainGo.transform.SetParent(hostGo.transform);
                var p2Brain = p2BrainGo.AddComponent<BBFighterBrain>();
                var p2Fc = p2BrainGo.AddComponent<FighterController>();
                p2Brain.fighter = p2Fc;
                p2Brain.maxHealth = 11000f;
                p2Brain.SetAuthoritativeHealth(11000f);

                view.BindBrains(p1Brain, p2Brain);

                var state = new BBMatchState
                {
                    Fighter1HP = (FP)10700,
                    Fighter2HP = (FP)9200,
                    Round = 1,
                    Fighter1Id = "C01",
                    Fighter2Id = "C02"
                };

                view.DriveBrains(state);

                Assert.AreEqual(10700f, p1Brain.health, 0.01f, "P1 health must match authoritative fixed-point FP 10700");
                Assert.AreEqual(9200f, p2Brain.health, 0.01f, "P2 health must match authoritative fixed-point FP 9200");
                Assert.AreEqual(10700f, p1Fc.currentHealth, 0.01f, "P1 FighterController must mirror authoritative health");
                Assert.AreEqual(9200f, p2Fc.currentHealth, 0.01f, "P2 FighterController must mirror authoritative health");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void Test_QuantumCombatBridge_LifecycleAndRollbackState()
        {
            var hostGo = new GameObject("Test_BridgeHost");
            try
            {
                var bridge = hostGo.AddComponent<QuantumCombatBridge>();
                var p1Go = new GameObject("Fighter_P1");
                p1Go.transform.SetParent(hostGo.transform);
                var fc1 = p1Go.AddComponent<FighterController>();
                var brain1 = p1Go.AddComponent<BBFighterBrain>();
                brain1.fighter = fc1;

                var p2Go = new GameObject("Fighter_P2");
                p2Go.transform.SetParent(hostGo.transform);
                var fc2 = p2Go.AddComponent<FighterController>();
                var brain2 = p2Go.AddComponent<BBFighterBrain>();
                brain2.fighter = fc2;

                bridge.BindFighters(fc1, fc2);
                bridge.InitializeQuantumMatch();

                Assert.IsTrue(bridge.IsQuantumActive, "IsQuantumActive must be true after InitializeQuantumMatch");

                // Test presentation resync from snapshot
                var p1Snap = new BBFighterSnapshot
                {
                    position = new Vector3(-2f, 0f, 0f),
                    facingDirection = 1,
                    state = BBState.Neutral,
                    isGrounded = true
                };
                var p2Snap = new BBFighterSnapshot
                {
                    position = new Vector3(2f, 0f, 0f),
                    facingDirection = -1,
                    state = BBState.Crouching,
                    isGrounded = true
                };

                bridge.ResyncPresentationFromSnapshot(p1Snap, p2Snap);
                Assert.AreEqual(-2f, fc1.transform.position.x, 0.001f, "P1 position must match snapshot");
                Assert.AreEqual(2f, fc2.transform.position.x, 0.001f, "P2 position must match snapshot");

                bridge.ResetQuantumMatch();
                Assert.IsFalse(bridge.IsQuantumActive, "IsQuantumActive must be false after ResetQuantumMatch");
                Assert.IsNull(brain1.customInputProvider, "P1 customInputProvider must be cleared on match reset");
                Assert.IsNull(brain2.customInputProvider, "P2 customInputProvider must be cleared on match reset");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void Test_PvPMatchSettlement_RoundAndMMR()
        {
            var ctrlGo = new GameObject("Test_PvPMatchCtrl");
            try
            {
                var ctrl = ctrlGo.AddComponent<PvPMatchController>();
                int startingMMR = ctrl.currentMMR;

                bool eventFired = false;
                bool reportedWon = false;
                int reportedDelta = 0;

                ctrl.OnMatchSettled += (won, delta) =>
                {
                    eventFired = true;
                    reportedWon = won;
                    reportedDelta = delta;
                };

                // Simulate P1 victory (local slot 0 = index 1)
                ctrl.HandleMatchFinished(winnerIndex: 1);

                Assert.IsTrue(eventFired, "OnMatchSettled event must fire");
                Assert.IsTrue(reportedWon, "LocalPlayerWon must be true");
                Assert.AreEqual(25, reportedDelta, "MMR delta on win must be +25");
                Assert.AreEqual(startingMMR + 25, ctrl.currentMMR, "Current MMR must increase by 25");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ctrlGo);
            }
        }

        public static void RunAllTestsMenu()
        {
            var suite = new DemonIdolQuantumOnlineTests();
            suite.Test_QuantumAppIdValidation();
            suite.Test_QuantumInputCodec_Full8ButtonSync();
            suite.Test_QuantumInput_ToBBInputFrameConversion();
            suite.Test_FixedPointBBMatchStateHPAuthority();
            suite.Test_QuantumCombatBridge_LifecycleAndRollbackState();
            suite.Test_PvPMatchSettlement_RoundAndMMR();
            Debug.Log("<color=#00e676>[DemonIdolQuantumOnlineTests] All 6 Photon Quantum netcode tests PASSED.</color>");
        }
    }
}
#endif
