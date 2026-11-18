#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    [InitializeOnLoad]
    public static class BBCollisionTestAutoRunner
    {
        static BBCollisionTestAutoRunner()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    BBCollisionTests.RunAllTestsMenu();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBCollisionTests] Test run failed: {ex}");
                }
            };
        }
    }

    [TestFixture]
    public class BBCollisionTests
    {
        [MenuItem("Demon Idol/BlazBlue/Run BBCollision Tests")]
        public static void RunAllTestsMenu()
        {
            var suite = new BBCollisionTests();
            suite.Test_Overlap_StrictInequality();
            suite.Test_FacingFlip_WorldTransformation();
            suite.Test_MultiHit_HitMask();
            suite.Test_Mutual_Trades();
            suite.Test_Throw_Pass_And_Invulnerability();
            suite.Test_Push_ExactTieBreak();
            suite.Test_Push_CornerClamp_100Percent();
            suite.Test_Push_Airborne_Pushed();
            suite.Test_ZeroAllocation_600Ticks();
            suite.Test_Determinism_100Runs();
            Debug.Log("<color=#00e676>[BBCollisionTests] ALL 10 SIMULATION & COLLISION TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Overlap_StrictInequality()
        {
            // Fully overlapping boxes
            var a = new BBWorldBox(0f, 0f, 2f, 2f, BBBoxKind.Hit);
            var b = new BBWorldBox(1f, 1f, 3f, 3f, BBBoxKind.Hurt);
            Assert.IsTrue(BBCollision.Overlap(in a, in b), "Overlapping boxes must return true.");

            // Disjoint boxes
            var c = new BBWorldBox(5f, 5f, 6f, 6f, BBBoxKind.Hurt);
            Assert.IsFalse(BBCollision.Overlap(in a, in c), "Disjoint boxes must return false.");

            // Touching edge on X (a.maxX == d.minX) -> Strict inequality means NO hit
            var d = new BBWorldBox(2f, 0f, 4f, 2f, BBBoxKind.Hurt);
            Assert.IsFalse(BBCollision.Overlap(in a, in d), "Touching edges on X must return false (no hit).");

            // Touching edge on Y (a.maxY == e.minY) -> Strict inequality means NO hit
            var e = new BBWorldBox(0f, 2f, 2f, 4f, BBBoxKind.Hurt);
            Assert.IsFalse(BBCollision.Overlap(in a, in e), "Touching edges on Y must return false (no hit).");
        }

        [Test]
        public void Test_FacingFlip_WorldTransformation()
        {
            // Local box: center=(0.4, 1.0), halfExtents=(0.2, 0.3)
            var local = new BBBox(0.4f, 1.0f, 0.2f, 0.3f, BBBoxKind.Hit);

            // Facing right (+1) at pos (1.0, 0.0)
            // World center X = 1.0 + 0.4 = 1.4 -> minX = 1.2, maxX = 1.6
            BBWorldBox worldRight = BBWorldBox.FromLocal(in local, 1.0f, 0.0f, 1);
            Assert.AreEqual(1.2f, worldRight.minX, 0.0001f);
            Assert.AreEqual(1.6f, worldRight.maxX, 0.0001f);
            Assert.AreEqual(0.7f, worldRight.minY, 0.0001f);
            Assert.AreEqual(1.3f, worldRight.maxY, 0.0001f);

            // Facing left (-1) at pos (1.0, 0.0)
            // World center X = 1.0 - 0.4 = 0.6 -> minX = 0.4, maxX = 0.8
            BBWorldBox worldLeft = BBWorldBox.FromLocal(in local, 1.0f, 0.0f, -1);
            Assert.AreEqual(0.4f, worldLeft.minX, 0.0001f);
            Assert.AreEqual(0.8f, worldLeft.maxX, 0.0001f);
            Assert.AreEqual(0.7f, worldLeft.minY, 0.0001f);
            Assert.AreEqual(1.3f, worldLeft.maxY, 0.0001f);
        }

        [Test]
        public void Test_MultiHit_HitMask()
        {
            var attackerBuffer = new BBBoxBuffer();
            attackerBuffer.Add(new BBWorldBox(0f, 0f, 1f, 1f, BBBoxKind.Hit, hitIndex: 1));

            var defenderBuffer = new BBBoxBuffer();
            defenderBuffer.Add(new BBWorldBox(0.5f, 0.5f, 1.5f, 1.5f, BBBoxKind.Hurt));

            var results = new BBContact[16];
            int contactCount = 0;
            var hitMask = new BBHitMask();

            // First strike pass connects
            int hits1 = BBCollision.StrikePass(attackerBuffer, defenderBuffer, ref hitMask, results, ref contactCount, 0, 1);
            Assert.AreEqual(1, hits1, "First strike pass must hit.");
            Assert.IsTrue(hitMask.HasHit(1), "hitIndex 1 must be marked in hit mask.");

            // Second strike pass with same mask must NOT hit again
            int hits2 = BBCollision.StrikePass(attackerBuffer, defenderBuffer, ref hitMask, results, ref contactCount, 0, 1);
            Assert.AreEqual(0, hits2, "Second strike pass with same hitIndex must be filtered out.");

            // Resetting mask allows connection again
            hitMask.Clear();
            int hits3 = BBCollision.StrikePass(attackerBuffer, defenderBuffer, ref hitMask, results, ref contactCount, 0, 1);
            Assert.AreEqual(1, hits3, "Clearing hit mask must allow hit to connect again.");
        }

        [Test]
        public void Test_Mutual_Trades()
        {
            // Both P1 and P2 have hitboxes overlapping each other's hurtboxes
            var p1Buffer = new BBBoxBuffer();
            p1Buffer.Add(new BBWorldBox(0f, 0f, 1.5f, 1.5f, BBBoxKind.Hit, hitIndex: 0));
            p1Buffer.Add(new BBWorldBox(0f, 0f, 1.0f, 1.0f, BBBoxKind.Hurt));

            var p2Buffer = new BBBoxBuffer();
            p2Buffer.Add(new BBWorldBox(0.5f, 0.5f, 2.0f, 2.0f, BBBoxKind.Hit, hitIndex: 0));
            p2Buffer.Add(new BBWorldBox(0.5f, 0.5f, 1.5f, 1.5f, BBBoxKind.Hurt));

            var results = new BBContact[16];
            int contactCount = 0;
            var p1Mask = new BBHitMask();
            var p2Mask = new BBHitMask();

            // P1 strikes P2
            int p1Hits = BBCollision.StrikePass(p1Buffer, p2Buffer, ref p1Mask, results, ref contactCount, 0, 1);
            // P2 strikes P1
            int p2Hits = BBCollision.StrikePass(p2Buffer, p1Buffer, ref p2Mask, results, ref contactCount, 1, 0);

            Assert.AreEqual(1, p1Hits, "P1 must hit P2.");
            Assert.AreEqual(1, p2Hits, "P2 must hit P1 (mutual trade).");
            Assert.AreEqual(2, contactCount, "Two contacts must be gathered in the single tick buffer.");
        }

        [Test]
        public void Test_Throw_Pass_And_Invulnerability()
        {
            var thrower = new BBBoxBuffer();
            thrower.Add(new BBWorldBox(0f, 0f, 1.0f, 1.0f, BBBoxKind.Throw));

            var target = new BBBoxBuffer();
            target.Add(new BBWorldBox(0.5f, 0f, 1.5f, 1.0f, BBBoxKind.ThrowHurt));

            var results = new BBContact[16];
            int contactCount = 0;

            // Target has Throw invulnerability
            int hitsInvuln = BBCollision.ThrowPass(thrower, target, results, ref contactCount, 0, 1, BBInvuln.Throw);
            Assert.AreEqual(0, hitsInvuln, "Throw must fail against throw invulnerability.");

            // Normal throw connects
            int hits = BBCollision.ThrowPass(thrower, target, results, ref contactCount, 0, 1, BBInvuln.None);
            Assert.AreEqual(1, hits, "Throw must connect when target is vulnerable.");
            Assert.AreEqual(BBContactType.Throw, results[0].contactType);
        }

        [Test]
        public void Test_Push_ExactTieBreak()
        {
            float p1x = 0.0f;
            float p2x = 0.0f;

            var p1Push = new BBWorldBox(-0.35f, 0f, 0.35f, 1.8f, BBBoxKind.Push);
            var p2Push = new BBWorldBox(-0.35f, 0f, 0.35f, 1.8f, BBBoxKind.Push);

            BBCollision.ResolvePush(ref p1x, ref p2x, in p1Push, in p2Push, -10.5f, 10.5f);

            // On exact center tie, P1 pushed left, P2 pushed right
            Assert.Less(p1x, 0.0f, "P1 must be pushed left on tie-break.");
            Assert.Greater(p2x, 0.0f, "P2 must be pushed right on tie-break.");
            Assert.AreEqual(0.70f, p2x - p1x, 0.0001f, "Total separation must equal pushbox width.");
        }

        [Test]
        public void Test_Push_CornerClamp_100Percent()
        {
            // P1 is pinned against left arena wall at -10.5
            float p1x = -10.15f; // halfWidth = 0.35 -> minX = -10.50
            float p2x = -9.80f;  // overlaps with P1

            var p1Push = new BBWorldBox(-10.50f, 0f, -9.80f, 1.8f, BBBoxKind.Push);
            var p2Push = new BBWorldBox(-10.15f, 0f, -9.45f, 1.8f, BBBoxKind.Push);

            BBCollision.ResolvePush(ref p1x, ref p2x, in p1Push, in p2Push, -10.5f, 10.5f);

            // P1 cannot move past -10.15f (left edge pinned to -10.5f)
            Assert.AreEqual(-10.15f, p1x, 0.001f, "Cornered P1 must not move through the arena wall.");
            // P2 absorbs 100% of the displacement
            Assert.GreaterOrEqual(p2x, -9.45f, "P2 must absorb 100% of the push away from the wall.");
        }

        [Test]
        public void Test_Push_Airborne_Pushed()
        {
            float p1x = 1.0f;
            float p2x = 1.2f;

            var p1Push = new BBWorldBox(0.65f, 0f, 1.35f, 1.8f, BBBoxKind.Push);
            var p2Push = new BBWorldBox(0.85f, 0f, 1.55f, 1.8f, BBBoxKind.Push);

            // P1 grounded, P2 airborne
            BBCollision.ResolvePush(ref p1x, ref p2x, in p1Push, in p2Push, -10.5f, 10.5f, p1Grounded: true, p2Grounded: false);

            Assert.AreEqual(1.0f, p1x, 0.0001f, "Grounded fighter P1 must NOT be pushed by airborne fighter.");
            Assert.Greater(p2x, 1.2f, "Airborne fighter P2 must absorb 100% of the displacement.");
        }

        [Test]
        public void Test_ZeroAllocation_600Ticks()
        {
            var p1Buffer = new BBBoxBuffer();
            var p2Buffer = new BBBoxBuffer();
            var contactResults = new BBContact[16];
            var p1Mask = new BBHitMask();
            var p2Mask = new BBHitMask();

            var localHit = new BBBox(0.4f, 1.0f, 0.25f, 0.25f, BBBoxKind.Hit, 0);
            var localHurt = new BBBox(0.0f, 1.0f, 0.30f, 0.70f, BBBoxKind.Hurt, 0);

            // Warmup pass
            for (int i = 0; i < 20; i++)
            {
                p1Buffer.Clear();
                p2Buffer.Clear();
                p1Buffer.AddLocal(in localHit, 0f, 0f, 1);
                p2Buffer.AddLocal(in localHurt, 0.6f, 0f, -1);
                int count = 0;
                BBCollision.StrikePass(p1Buffer, p2Buffer, ref p1Mask, contactResults, ref count, 0, 1);
            }

            // Measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryBefore = GC.GetTotalMemory(true);

            float p1x = -1.0f;
            float p2x = 1.0f;
            uint tickHash = 0;

            for (int t = 0; t < 600; t++)
            {
                p1Buffer.Clear();
                p2Buffer.Clear();
                int count = 0;

                p1Buffer.AddLocal(in localHit, p1x, 0f, 1);
                p2Buffer.AddLocal(in localHurt, p2x, 0f, -1);

                BBWorldBox p1Push = new BBWorldBox(p1x - 0.35f, 0f, p1x + 0.35f, 1.8f, BBBoxKind.Push);
                BBWorldBox p2Push = new BBWorldBox(p2x - 0.35f, 0f, p2x + 0.35f, 1.8f, BBBoxKind.Push);

                BBCollision.ResolvePush(ref p1x, ref p2x, in p1Push, in p2Push, -10.5f, 10.5f);
                BBCollision.StrikePass(p1Buffer, p2Buffer, ref p1Mask, contactResults, ref count, 0, 1);

                tickHash = BBSimHash.ComputeTickHash(t, p1x, 0f, 1, 0, 11000, 50, p2x, 0f, -1, 0, 11000, 50, 0);
            }

            long memoryAfter = GC.GetTotalMemory(false);
            long allocated = memoryAfter - memoryBefore;

            Assert.AreEqual(0, allocated, $"Simulating 600 ticks must allocate 0 bytes of GC memory! (Allocated: {allocated} bytes)");
        }

        [Test]
        public void Test_Determinism_100Runs()
        {
            uint firstRunFinalHash = 0;

            for (int run = 0; run < 100; run++)
            {
                float p1x = -0.1f;
                float p2x = 0.1f;
                uint runningHash = 2166136261;

                var p1Buffer = new BBBoxBuffer();
                var p2Buffer = new BBBoxBuffer();
                var contactResults = new BBContact[16];
                var p1Mask = new BBHitMask();

                var hitBox = new BBBox(0.3f, 1.0f, 0.2f, 0.2f, BBBoxKind.Hit, 0);
                var hurtBox = new BBBox(0.0f, 1.0f, 0.3f, 0.7f, BBBoxKind.Hurt, 0);

                for (int t = 0; t < 600; t++)
                {
                    p1Buffer.Clear();
                    p2Buffer.Clear();
                    int contactCount = 0;

                    p1Buffer.AddLocal(in hitBox, p1x, 0f, 1);
                    p2Buffer.AddLocal(in hurtBox, p2x, 0f, -1);

                    var p1Push = new BBWorldBox(p1x - 0.35f, 0f, p1x + 0.35f, 1.8f, BBBoxKind.Push);
                    var p2Push = new BBWorldBox(p2x - 0.35f, 0f, p2x + 0.35f, 1.8f, BBBoxKind.Push);

                    BBCollision.ResolvePush(ref p1x, ref p2x, in p1Push, in p2Push, -10.5f, 10.5f);
                    BBCollision.StrikePass(p1Buffer, p2Buffer, ref p1Mask, contactResults, ref contactCount, 0, 1);

                    uint tickHash = BBSimHash.ComputeTickHash(t, p1x, 0f, 1, 0, 11000, 0, p2x, 0f, -1, 0, 10000, 0, contactCount);
                    runningHash = BBSimHash.HashValue(runningHash, (int)tickHash);
                }

                if (run == 0)
                {
                    firstRunFinalHash = runningHash;
                }
                else
                {
                    Assert.AreEqual(firstRunFinalHash, runningHash, $"Run {run} produced differing hash! Simulation must be 100% deterministic.");
                }
            }
        }
    }
}
#endif
