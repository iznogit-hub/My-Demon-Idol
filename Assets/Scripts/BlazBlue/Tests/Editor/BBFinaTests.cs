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
    public class BBFinaTests
    {
        public static void RunAllTestsMenu()
        {
            // Auto-run test suite after generator creates assets
            // Assets already generated

            var suite = new BBFinaTests();
            suite.Test_Fina_Assets_Exist_And_Valid();
            suite.Test_Fina_Drive_PlasmaCharge_TiersAndStore();
            suite.Test_Fina_FootsieNormals_Properties();
            suite.Test_Fina_Specials_And_Reversal();
            suite.Test_Fina_RevolverAction_Chains();
            suite.Test_Fina_10_Trial_Challenges();
            suite.Test_Fina_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBFinaTests] ALL 7 FINA COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Fina_Assets_Exist_And_Valid()
        {
            // Assets already generated

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Fina"}/BBMoveSet_C12_Fina.asset");
            Assert.IsNotNull(moveSet, "Fina BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 28, $"Fina should have at least 28 moves, found {moveSet.moves.Length}.");

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
        public void Test_Fina_Drive_PlasmaCharge_TiersAndStore()
        {
            var go = new GameObject("FinaTestGO");
            var drive = go.AddComponent<FinaDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Plasma Charge", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.Stocks, drive.ResourceKind);
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.AreEqual(3, drive.MaxResource);
            Assert.AreEqual(0, drive.StoredLevel);

            // 2. Charging progression across tiers (20 / 45 / 75 ticks)
            drive.StartCharging();
            Assert.IsTrue(drive.isCharging);

            drive.chargeHoldTicks = 15;
            Assert.AreEqual(0, drive.CurrentChargeLevel);

            drive.chargeHoldTicks = 20;
            Assert.AreEqual(1, drive.CurrentChargeLevel);

            drive.chargeHoldTicks = 45;
            Assert.AreEqual(2, drive.CurrentChargeLevel);

            drive.chargeHoldTicks = 75;
            Assert.AreEqual(3, drive.CurrentChargeLevel);

            // 3. Storing charge
            Assert.IsTrue(drive.StoreCharge());
            Assert.AreEqual(3, drive.StoredLevel);
            Assert.IsFalse(drive.isCharging);

            // 4. Consuming stored charge for special empowerment
            Assert.IsTrue(drive.TryConsumeStoredCharge(out int consumedLvl));
            Assert.AreEqual(3, consumedLvl);
            Assert.AreEqual(0, drive.StoredLevel);

            // 5. Overdrive provides instant Level 3 charge
            drive.InOverdrive = true;
            Assert.AreEqual(3, drive.CurrentChargeLevel);
            Assert.AreEqual(3, drive.CurrentResource);
            Assert.IsTrue(drive.TryConsumeStoredCharge(out int odLvl));
            Assert.AreEqual(3, odLvl);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Fina_FootsieNormals_Properties()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Fina"}/BBMoveSet_C12_Fina.asset");
            Assert.IsNotNull(moveSet);

            var move5B = FindMove(moveSet, "5B");
            Assert.IsNotNull(move5B, "5B Precision Straight Poke must exist.");
            Assert.IsTrue(move5B.startupFrames > 0);

            var move2B = FindMove(moveSet, "2B");
            Assert.IsNotNull(move2B, "2B Low Footsie Poke must exist.");
            Assert.IsTrue(move2B.startupFrames > 0);
            Assert.AreEqual(BBGuard.Low, move2B.guard);

            var move5C = FindMove(moveSet, "5C");
            Assert.IsNotNull(move5C, "5C Heavy Cross must exist.");
            Assert.IsTrue(move5C.fatalCounter, "5C must be a Fatal Counter move.");

            var move3C = FindMove(moveSet, "3C");
            Assert.IsNotNull(move3C, "3C Rising Kick Launcher must exist.");
            Assert.IsTrue(move3C.jumpCancelOnHit, "3C must allow jump cancel on hit.");

            var move6A = FindMove(moveSet, "6A");
            Assert.IsNotNull(move6A, "6A Anti-Air Upper must exist.");
            Assert.IsTrue(move6A.invuln.Length > 0, "6A must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);
        }

        [Test]
        public void Test_Fina_Specials_And_Reversal()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Fina"}/BBMoveSet_C12_Fina.asset");
            Assert.IsNotNull(moveSet);

            var dp = FindMove(moveSet, "623C");
            Assert.IsNotNull(dp, "623C Rising Kick DP must exist.");
            Assert.AreEqual(BBMotion.M623, dp.motion);
            Assert.IsTrue(dp.invuln.Length > 0, "623C must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body, dp.invuln[0].mask);

            var advancingKick = FindMove(moveSet, "214B");
            Assert.IsNotNull(advancingKick, "214B Advancing Stepping Kick must exist.");
            Assert.AreEqual(BBMotion.M214, advancingKick.motion);

            var beamSuper = FindMove(moveSet, "236236C");
            Assert.IsNotNull(beamSuper, "236236C Neo-Pink Plasma Burst Beam DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, beamSuper.kind);
            Assert.AreEqual(50, beamSuper.heatCost);
            Assert.IsTrue(beamSuper.damage > 0);

            var rushSuper = FindMove(moveSet, "632146D");
            Assert.IsNotNull(rushSuper, "632146D Crimson Rush DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, rushSuper.kind);
            Assert.AreEqual(50, rushSuper.heatCost);
            Assert.IsTrue(rushSuper.damage > 0);
        }

        [Test]
        public void Test_Fina_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Fina"}/BBMoveSet_C12_Fina.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 5;

            // 5A -> 5B valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 0, 5, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 5B -> 5C valid
            chain.MarkUsed(1); // 5A used
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");
        }

        [Test]
        public void Test_Fina_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Fina"}/BBMoveSet_C12_Fina.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Footsie Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Footsie Confirm
                new string[] { "5B", "5C", "3C", "j.A", "j.B", "j.C" },                   // Trial 3: Launcher Aerial Juggle
                new string[] { "5B", "236A" },                                            // Trial 4: Footsie Poke into Plasma Fireball
                new string[] { "2B", "214B" },                                            // Trial 5: Low Confirm into Advancing Stepping Kick
                new string[] { "6A", "623C" },                                            // Trial 6: Anti-Air Reversal Conversion
                new string[] { "6D", "5B", "5C" },                                        // Trial 7: Overhead Hop Mixup Confirm
                new string[] { "5C", "236236C" },                                         // Trial 8: Fatal Counter into Neo-Pink Plasma Burst
                new string[] { "5B", "5C", "5D", "632146D" },                             // Trial 9: Plasma Shot into Crimson Rush DD
                new string[] { "5D", "236B", "236236C", "OD_EA" }                         // Trial 10: Stored Plasma Level 3 Overdrive Exceed Accel
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in Fina move set.");
                }
            }
        }

        [Test]
        public void Test_Fina_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C12_Fina.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C12_Fina must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C12_Fina must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C12_Fina", profile.characterId);
            Assert.AreEqual("Plasma Charge", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link Fina_BB_Override controller.");
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
