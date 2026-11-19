#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Editor;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBAikaTests
    {
        [MenuItem("Demon Idol/BlazBlue/Run BBAika Tests")]
        public static void RunAllTestsMenu()
        {
            // Re-run test suite after asset generation
            // Ensure assets are generated first
            BBAikaAssetGenerator.GenerateAllAikaAssets();

            var suite = new BBAikaTests();
            suite.Test_Aika_Assets_Exist_And_Valid();
            suite.Test_Aika_Drive_ResonanceNail_Stocks_And_Regen();
            suite.Test_Aika_Drive_Detonate_And_ActiveNails();
            suite.Test_Aika_Drive_Overdrive_Enhancements();
            suite.Test_Aika_Special_Properties();
            suite.Test_Aika_RevolverAction_Chains();
            suite.Test_Aika_10_Trial_Challenges();
            suite.Test_Aika_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBAikaTests] ALL 8 AIKA COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Aika_Assets_Exist_And_Valid()
        {
            BBAikaAssetGenerator.GenerateAllAikaAssets();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBAikaAssetGenerator.AIKA_ROOT}/BBMoveSet_C02_Aika.asset");
            Assert.IsNotNull(moveSet, "Aika BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 27, $"Aika should have at least 27 moves, found {moveSet.moves.Length}.");

            for (int i = 0; i < moveSet.moves.Length; i++)
            {
                var m = moveSet.moves[i];
                Assert.IsNotNull(m, $"Move at index {i} must not be null.");
                Assert.IsFalse(string.IsNullOrEmpty(m.moveId), $"Move at index {i} must have a valid moveId.");
                Assert.IsTrue(m.startupFrames > 0, $"{m.moveId} startupFrames must be > 0.");
                Assert.IsTrue(m.activeFrames > 0, $"{m.moveId} activeFrames must be > 0.");
                Assert.IsTrue(m.recoveryFrames > 0, $"{m.moveId} recoveryFrames must be > 0.");
                Assert.IsNotNull(m.boxes, $"{m.moveId} must have an attached BBBoxTrack.");
                Assert.AreEqual(m.TotalTicks, m.boxes.tickCount, $"{m.moveId} TotalTicks must match box track tickCount.");
                Assert.IsNotNull(m.clip, $"{m.moveId} must have an attached genuine AnimationClip sub-asset (no placeholders).");
            }
        }

        [Test]
        public void Test_Aika_Drive_ResonanceNail_Stocks_And_Regen()
        {
            var go = new GameObject("AikaTestGO");
            var drive = go.AddComponent<AikaDrive>();
            drive.ResetResource();

            // 1. Initial state: 5 nail stocks
            Assert.AreEqual(5, drive.CurrentResource);
            Assert.AreEqual(5, drive.MaxResource);
            Assert.AreEqual("Resonance Nail", drive.DriveName);
            Assert.IsTrue(drive.CanThrowNail());

            // 2. Spend nails
            Assert.IsTrue(drive.SpendNail());
            Assert.AreEqual(4, drive.CurrentResource);
            Assert.IsTrue(drive.SpendNail());
            Assert.AreEqual(3, drive.CurrentResource);

            // 3. Regeneration: 1 stock every 120 ticks
            for (int t = 0; t < 119; t++)
            {
                drive.TickDrive(null, t);
            }
            Assert.AreEqual(3, drive.CurrentResource, "Should not regen before 120 ticks.");

            drive.TickDrive(null, 119); // 120th tick
            Assert.AreEqual(4, drive.CurrentResource, "Must regen 1 nail at 120 ticks.");

            // 4. Knockdown preserves ammunition
            drive.OnKnockdown(null);
            Assert.AreEqual(4, drive.CurrentResource, "Nail ammunition must be preserved on knockdown.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Aika_Drive_Detonate_And_ActiveNails()
        {
            var go = new GameObject("AikaTestGO");
            var drive = go.AddComponent<AikaDrive>();
            drive.ResetResource();

            // Spawn active nails
            drive.SpawnNail(new Vector2(1.5f, 0f), Vector2.zero, 10);
            drive.SpawnNail(new Vector2(2.5f, 0f), Vector2.zero, 12);
            drive.SpawnNail(new Vector2(3.5f, 0.5f), new Vector2(5f, -5f), 14);
            Assert.AreEqual(3, drive.ActiveNailCount);

            // Detonate
            int detonated = drive.DetonateAllNails(null, 20);
            Assert.AreEqual(3, detonated, "Must detonate all 3 active nails.");
            Assert.AreEqual(0, drive.ActiveNailCount, "Active nails list must be cleared after detonation.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Aika_Drive_Overdrive_Enhancements()
        {
            var go = new GameObject("AikaTestGO");
            var drive = go.AddComponent<AikaDrive>();
            drive.ResetResource();

            // Spend all nails down to 0
            for (int i = 0; i < 5; i++) drive.SpendNail();
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.IsFalse(drive.CanThrowNail());

            // Activate Overdrive: infinite nails
            drive.InOverdrive = true;
            Assert.AreEqual(5, drive.CurrentResource);
            Assert.IsTrue(drive.CanThrowNail(), "In Overdrive, Aika can always throw nails.");

            // Spend in Overdrive does not decrement
            Assert.IsTrue(drive.SpendNail());
            Assert.AreEqual(5, drive.CurrentResource);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Aika_Special_Properties()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBAikaAssetGenerator.AIKA_ROOT}/BBMoveSet_C02_Aika.asset");
            Assert.IsNotNull(moveSet);

            var dp = FindMove(moveSet, "623C");
            Assert.IsNotNull(dp, "623C Rising Dragon Kick must exist.");
            Assert.IsTrue(dp.invuln.Length > 0, "623C must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body, dp.invuln[0].mask);

            var fc = FindMove(moveSet, "5C");
            Assert.IsNotNull(fc, "5C Roundhouse must exist.");
            Assert.IsTrue(fc.fatalCounter, "5C must be a Fatal Counter move.");

            var lowSweep = FindMove(moveSet, "214B");
            Assert.IsNotNull(lowSweep, "214B Reap must exist.");
            Assert.AreEqual(BBGuard.Low, lowSweep.guard);

            var stepKick = FindMove(moveSet, "236B");
            Assert.IsNotNull(stepKick, "236B Step Thrust Kick must exist.");
            Assert.AreEqual(BBButton.B, stepKick.button);
            Assert.AreEqual(BBMotion.M236, stepKick.motion);
        }

        [Test]
        public void Test_Aika_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBAikaAssetGenerator.AIKA_ROOT}/BBMoveSet_C02_Aika.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");
            var move3C = FindMove(moveSet, "3C");
            var move6A = FindMove(moveSet, "6A");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 7;

            // 5A -> 5B valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 0, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 5B -> 5C valid
            chain.MarkUsed(1); // 5A used
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");

            // 3C launcher has jump cancel on hit
            Assert.IsTrue(move3C.jumpCancelOnHit, "3C launcher must have jump cancel on hit.");

            // 6A anti-air has Head invulnerability
            Assert.IsNotNull(move6A.invuln);
            Assert.IsTrue(move6A.invuln.Length > 0);
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);
            Assert.AreEqual(4, move6A.invuln[0].startTick);
            Assert.AreEqual(12, move6A.invuln[0].endTick);
        }

        [Test]
        public void Test_Aika_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBAikaAssetGenerator.AIKA_ROOT}/BBMoveSet_C02_Aika.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Footsies String
                new string[] { "5B", "5C", "3C", "j.A", "j.B", "j.C" },                   // Trial 3: Air Juggle
                new string[] { "5B", "2B", "5D", "6D" },                                  // Trial 4: Nail Setplay
                new string[] { "2A", "2B", "2D", "214B" },                                // Trial 5: Low Nail Surprise
                new string[] { "6A", "623C" },                                            // Trial 6: Anti-Air DP
                new string[] { "5C", "236B", "46C" },                                     // Trial 7: Step Kick Wall Carry
                new string[] { "5C", "236236C" },                                         // Trial 8: Fatal Counter DD
                new string[] { "5C", "3C", "j.A", "j.B", "j.D", "632146D" },              // Trial 9: Nail Rain Juggle
                new string[] { "5D", "2D", "6D", "236236C", "OD_EA" }                     // Trial 10: Overdrive Dual-Nail
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in Aika move set.");
                }
            }
        }

        [Test]
        public void Test_Aika_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C02_Aika must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C02_Aika must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C02_Aika", profile.characterId);
            Assert.AreEqual("Resonance Nail", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link Aika_BB_Override controller.");
        }

        private BBAttackData FindMove(BBMoveSet moveSet, string moveId)
        {
            for (int i = 0; i < moveSet.moves.Length; i++)
            {
                if (moveSet.moves[i] != null && moveSet.moves[i].moveId == moveId)
                    return moveSet.moves[i];
            }
            return null;
        }
    }
}
#endif
