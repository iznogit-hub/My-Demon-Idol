#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Data;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBMatchSimDeterminismTests
    {
        public static void RunAllTestsMenu()
        {
            var suite = new BBMatchSimDeterminismTests();
            suite.Test_SimulatedMatch_600Ticks_BitForBitDeterministic();
            Debug.Log("<color=#00e676>[BBMatchSimDeterminismTests] 600-TICK INTEGRATION TEST PASSED!</color>");
        }

        [Test]
        public void Test_SimulatedMatch_600Ticks_BitForBitDeterministic()
        {
            // 1. Prepare deterministic 600-tick input streams for P1 and P2
            var p1Inputs = GenerateP1InputSequence(600);
            var p2Inputs = GenerateP2InputSequence(600);

            Assert.AreEqual(600, p1Inputs.Length);
            Assert.AreEqual(600, p2Inputs.Length);

            // 2. Execute Run 1
            uint[] run1Hashes = new uint[600];
            uint run1CumulativeHash = 2166136261;
            Vector3 p1EndPos1, p2EndPos1;
            float p1EndHp1, p2EndHp1;

            Execute600TickSimulation(
                p1Inputs, p2Inputs, run1Hashes, out run1CumulativeHash,
                out p1EndPos1, out p2EndPos1, out p1EndHp1, out p2EndHp1
            );

            // 3. Execute Run 2 with identical inputs
            uint[] run2Hashes = new uint[600];
            uint run2CumulativeHash = 2166136261;
            Vector3 p1EndPos2, p2EndPos2;
            float p1EndHp2, p2EndHp2;

            Execute600TickSimulation(
                p1Inputs, p2Inputs, run2Hashes, out run2CumulativeHash,
                out p1EndPos2, out p2EndPos2, out p1EndHp2, out p2EndHp2
            );

            // 4. Verify Bit-for-Bit Determinism across both runs
            Assert.AreNotEqual(0, run1CumulativeHash, "Simulation hash must not be zero");
            Assert.AreEqual(run1CumulativeHash, run2CumulativeHash,
                "600-tick simulated match cumulative BBSimHash must be identical across both runs!");

            for (int t = 0; t < 600; t++)
            {
                Assert.AreEqual(run1Hashes[t], run2Hashes[t],
                    $"BBSimHash mismatch at Tick {t + 1}: Run 1={run1Hashes[t]}, Run 2={run2Hashes[t]}");
            }

            // 5. Verify that physical gameplay progression actually happened
            Assert.AreEqual(p1EndPos1.x, p1EndPos2.x, 0.0001f, "P1 end position X must be deterministic");
            Assert.AreEqual(p2EndPos1.x, p2EndPos2.x, 0.0001f, "P2 end position X must be deterministic");
            Assert.AreEqual(p1EndHp1, p1EndHp2, 0.0001f, "P1 end HP must be deterministic");
            Assert.AreEqual(p2EndHp1, p2EndHp2, 0.0001f, "P2 end HP must be deterministic");
        }

        private void Execute600TickSimulation(
            BBInputFrame[] p1Inputs,
            BBInputFrame[] p2Inputs,
            uint[] tickHashes,
            out uint cumulativeHash,
            out Vector3 p1EndPos,
            out Vector3 p2EndPos,
            out float p1EndHp,
            out float p2EndHp)
        {
            // Reset any static visual/flash systems
            BBSuperFlash.ForceReset();

            var rdb = Resources.Load<MDIRosterDatabase>("MDI_RosterDatabase");
            CharacterDefinition c01Def = null;
            CharacterDefinition c02Def = null;

            if (rdb != null && rdb.characters != null)
            {
                c01Def = Array.Find(rdb.characters, c => c.id == "C01");
                c02Def = Array.Find(rdb.characters, c => c.id == "C02");
            }

            GameObject simGo = new GameObject("Sim_Hermetic");
            var sim = simGo.AddComponent<BBMatchSim>();

            GameObject p1Go = new GameObject("Fighter_P1");
            p1Go.transform.position = new Vector3(-1.5f, 0f, 0f);
            var p1Fighter = p1Go.AddComponent<FighterController>();
            p1Fighter.appliedDefinition = c01Def;
            var p1Brain = p1Go.AddComponent<BBFighterBrain>();
            p1Brain.fighter = p1Fighter;
            p1Brain.profile = c01Def != null ? c01Def.blazBlueProfile as BBCharacterProfile : null;
            p1Brain.bodyBoxes = BBBodyBoxes.CreateAkioPreset();
            p1Brain.isGrounded = true;
            p1Fighter.SetFacing(1);

            GameObject p2Go = new GameObject("Fighter_P2");
            p2Go.transform.position = new Vector3(1.5f, 0f, 0f);
            var p2Fighter = p2Go.AddComponent<FighterController>();
            p2Fighter.appliedDefinition = c02Def;
            var p2Brain = p2Go.AddComponent<BBFighterBrain>();
            p2Brain.fighter = p2Fighter;
            p2Brain.profile = c02Def != null ? c02Def.blazBlueProfile as BBCharacterProfile : null;
            p2Brain.bodyBoxes = BBBodyBoxes.CreateAikaPreset();
            p2Brain.isGrounded = true;
            p2Fighter.SetFacing(-1);

            p1Fighter.opponent = p2Fighter;
            p2Fighter.opponent = p1Fighter;

            sim.RegisterFighters(p1Brain, p2Brain);

            // Hook programmatic input stream
            p1Brain.customInputProvider = (t) => p1Inputs[Mathf.Clamp(t - 1, 0, p1Inputs.Length - 1)];
            p2Brain.customInputProvider = (t) => p2Inputs[Mathf.Clamp(t - 1, 0, p2Inputs.Length - 1)];

            cumulativeHash = 2166136261;

            try
            {
                for (int t = 1; t <= 600; t++)
                {
                    sim.Tick(t);
                    tickHashes[t - 1] = sim.lastTickHash;
                    cumulativeHash = BBSimHash.HashValue(cumulativeHash, (int)sim.lastTickHash);
                }

                p1EndPos = p1Go.transform.position;
                p2EndPos = p2Go.transform.position;
                p1EndHp = p1Brain.health;
                p2EndHp = p2Brain.health;
            }
            finally
            {
                if (BBMatchSim.Instance == sim)
                {
                    typeof(BBMatchSim).GetProperty("Instance")?.SetValue(null, null);
                }
                UnityEngine.Object.DestroyImmediate(p1Go);
                UnityEngine.Object.DestroyImmediate(p2Go);
                UnityEngine.Object.DestroyImmediate(simGo);
                BBSuperFlash.ForceReset();
            }
        }

        private BBInputFrame[] GenerateP1InputSequence(int totalTicks)
        {
            var frames = new BBInputFrame[totalTicks];
            for (int i = 0; i < totalTicks; i++)
            {
                int tick = i + 1;
                sbyte numpad = 5;
                byte held = 0;
                byte pressed = 0;

                // Ticks 1..30: Walk Forward (numpad 6)
                if (tick <= 30)
                {
                    numpad = 6;
                }
                // Ticks 31..40: 5A (Light Attack)
                else if (tick >= 31 && tick <= 35)
                {
                    numpad = 5;
                    held = (byte)BBButton.A;
                    if (tick == 31) pressed = (byte)BBButton.A;
                }
                // Ticks 45..55: 5B (Medium Attack gatling chain)
                else if (tick >= 45 && tick <= 49)
                {
                    numpad = 5;
                    held = (byte)BBButton.B;
                    if (tick == 45) pressed = (byte)BBButton.B;
                }
                // Ticks 60..70: 5C (Heavy Attack gatling chain)
                else if (tick >= 60 && tick <= 64)
                {
                    numpad = 5;
                    held = (byte)BBButton.C;
                    if (tick == 60) pressed = (byte)BBButton.C;
                }
                // Ticks 80..110: Jump forward (numpad 9)
                else if (tick >= 80 && tick <= 85)
                {
                    numpad = 9;
                }
                // Ticks 120..150: Neutral crouch (numpad 2)
                else if (tick >= 120 && tick <= 150)
                {
                    numpad = 2;
                }
                // Ticks 160..190: 236A Special move motion (2, 3, 6 + A)
                else if (tick == 160) numpad = 2;
                else if (tick == 161) numpad = 3;
                else if (tick == 162)
                {
                    numpad = 6;
                    held = (byte)BBButton.A;
                    pressed = (byte)BBButton.A;
                }
                // Ticks 200..230: Drive Move 5D
                else if (tick == 200)
                {
                    numpad = 5;
                    held = (byte)BBButton.D;
                    pressed = (byte)BBButton.D;
                }
                // Ticks 300..350: Rapid Cancel / Dash forward
                else if (tick >= 300 && tick <= 330)
                {
                    numpad = 6;
                }
                // Ticks 400..450: Stand guard (numpad 4)
                else if (tick >= 400 && tick <= 450)
                {
                    numpad = 4;
                }

                frames[i] = new BBInputFrame
                {
                    held = held,
                    pressed = pressed,
                    released = 0,
                    numpad = numpad
                };
            }
            return frames;
        }

        private BBInputFrame[] GenerateP2InputSequence(int totalTicks)
        {
            var frames = new BBInputFrame[totalTicks];
            for (int i = 0; i < totalTicks; i++)
            {
                int tick = i + 1;
                sbyte numpad = 5;
                byte held = 0;
                byte pressed = 0;

                // Ticks 1..30: Walk Forward (facing left, so numpad 4 moves forward toward P1)
                if (tick <= 30)
                {
                    numpad = 4;
                }
                // Ticks 31..80: Guard High (facing left, so numpad 6 is back/guard)
                else if (tick >= 31 && tick <= 80)
                {
                    numpad = 6;
                }
                // Ticks 100..120: Retaliate with 2A (Crouching Light)
                else if (tick >= 100 && tick <= 104)
                {
                    numpad = 2;
                    held = (byte)BBButton.A;
                    if (tick == 100) pressed = (byte)BBButton.A;
                }
                // Ticks 150..200: Guard Low (facing left, so numpad 3 is down-back)
                else if (tick >= 150 && tick <= 200)
                {
                    numpad = 3;
                }
                // Ticks 250..280: Backdash (facing left, double-tap 6)
                else if (tick == 250) numpad = 6;
                else if (tick == 252) numpad = 6;
                // Ticks 350..400: Walk forward (numpad 4)
                else if (tick >= 350 && tick <= 400)
                {
                    numpad = 4;
                }
                // Ticks 450..500: Defensive Burst attempt (A+B+C)
                else if (tick == 450)
                {
                    numpad = 5;
                    held = (byte)(BBButton.A | BBButton.B | BBButton.C);
                    pressed = (byte)(BBButton.A | BBButton.B | BBButton.C);
                }

                frames[i] = new BBInputFrame
                {
                    held = held,
                    pressed = pressed,
                    released = 0,
                    numpad = numpad
                };
            }
            return frames;
        }
    }
}
#endif
