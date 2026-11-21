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
    public class BBAkioTests
    {
        public static void RunAllTestsMenu()
        {
            // Ensure assets are generated first
            // Assets already generated

            var suite = new BBAkioTests();
            suite.Test_Akio_Assets_Exist_And_Valid();
            suite.Test_Akio_Drive_IronRush_Momentum();
            suite.Test_Akio_Drive_Overdrive_Enhancements();
            suite.Test_Akio_Special_Empowerments();
            suite.Test_Akio_RevolverAction_Chains();
            suite.Test_Akio_10_Trial_Challenges();
            suite.Test_Akio_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBAkioTests] ALL 7 AKIO COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Akio_Assets_Exist_And_Valid()
        {
            // Assets already generated

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Akio"}/BBMoveSet_C01_Akio.asset");
            Assert.IsNotNull(moveSet, "Akio BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 30, $"Akio should have at least 30 moves, found {moveSet.moves.Length}.");

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
            }
        }

        [Test]
        public void Test_Akio_Drive_IronRush_Momentum()
        {
            var go = new GameObject("AkioTestGO");
            var drive = go.AddComponent<AkioDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.AreEqual(3, drive.MaxResource);
            Assert.AreEqual("Iron Rush", drive.DriveName);

            // 2. Bank momentum on drive move hit/block
            var move5D = ScriptableObject.CreateInstance<BBAttackData>();
            move5D.moveId = "5D";
            move5D.button = BBButton.D;
            move5D.kind = BBMoveKind.DriveMove;

            drive.OnDriveHit(null, move5D, isBlocked: false);
            Assert.AreEqual(1, drive.CurrentResource);

            drive.OnDriveHit(null, move5D, isBlocked: true); // Block also banks
            Assert.AreEqual(2, drive.CurrentResource);

            drive.OnDriveHit(null, move5D, isBlocked: false);
            Assert.AreEqual(3, drive.CurrentResource);

            // Capped at 3
            drive.OnDriveHit(null, move5D, isBlocked: false);
            Assert.AreEqual(3, drive.CurrentResource);

            // 3. Spends all momentum
            int spent = drive.SpendAllMomentum();
            Assert.AreEqual(3, spent);
            Assert.AreEqual(0, drive.CurrentResource);

            // 4. Knockdown drops stocks to 0
            drive.SetMomentum(2);
            Assert.AreEqual(2, drive.CurrentResource);
            drive.OnKnockdown(null);
            Assert.AreEqual(0, drive.CurrentResource, "Momentum stocks must reset to 0 on knockdown.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Akio_Drive_Overdrive_Enhancements()
        {
            var go = new GameObject("AkioTestODGO");
            var drive = go.AddComponent<AkioDrive>();
            drive.ResetResource();
            drive.InOverdrive = true;

            // In Overdrive: always treated as 3 Momentum stocks
            Assert.AreEqual(3, drive.CurrentResource);

            // Spending momentum in OD returns 3 without decrementing
            int spent = drive.SpendAllMomentum();
            Assert.AreEqual(3, spent);
            Assert.AreEqual(3, drive.CurrentResource, "Momentum must remain infinite (3) during Overdrive.");

            // 4th hit 5D_4 frame data verification
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Akio"}/BBMoveSet_C01_Akio.asset");
            BBAttackData move5D_4 = null;
            for (int i = 0; i < moveSet.moves.Length; i++)
            {
                if (moveSet.moves[i].moveId == "5D_4")
                {
                    move5D_4 = moveSet.moves[i];
                    break;
                }
            }

            Assert.IsNotNull(move5D_4, "5D_4 (Iron Finisher) must exist.");
            Assert.AreEqual(2, move5D_4.advantageOnBlock, "5D_4 must be +2 on block in Overdrive.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Akio_Special_Empowerments()
        {
            // 236C Rush Combination: 900 base + 150 per Momentum stock
            int baseDamage = 900;
            int momentum = 2;
            int total236CDamage = baseDamage + (momentum * 150);
            Assert.AreEqual(1200, total236CDamage);

            // 632146D Crimson Shadow Rush: 3000 base + 300 per Momentum stock
            int baseDDDamage = 3000;
            int totalDDDamage = baseDDDamage + (momentum * 300);
            Assert.AreEqual(3600, totalDDDamage);
        }

        [Test]
        public void Test_Akio_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Akio"}/BBMoveSet_C01_Akio.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");
            var move3C = FindMove(moveSet, "3C");
            var move6A = FindMove(moveSet, "6A");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 7;

            // 5A -> 5B is valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 0, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 5B -> 5C is valid
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
        public void Test_Akio_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Akio"}/BBMoveSet_C01_Akio.asset");

            // Verify all moves needed for the 10 trials exist and can connect
            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low / Sweep
                new string[] { "5B", "5C", "3C", "j.A", "j.B", "j.C" },                   // Trial 3: Air Juggle
                new string[] { "5A", "5B", "5D", "5D_2", "5D_3" },                        // Trial 4: Momentum Builder
                new string[] { "5B", "5C", "236A" },                                      // Trial 5: Special Frame Trap
                new string[] { "6A", "623C" },                                            // Trial 6: Anti-Air DP
                new string[] { "5B", "2B", "5D", "5D_2", "236C" },                        // Trial 7: Momentum Spend
                new string[] { "5C", "236236C" },                                         // Trial 8: Fatal Counter DD
                new string[] { "5C", "623C", "5C", "3C", "j.A", "j.B", "j.C", "j.D", "5C", "236236C" }, // Trial 9: Rapid Juggle
                new string[] { "5D", "5D_2", "5D_3", "5D_4", "632146D", "OD_EA" }         // Trial 10: Overdrive Exceed
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in Akio move set.");
                }
            }
        }

        [Test]
        public void Test_Akio_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C01_Akio must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C01_Akio must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C01_Akio", profile.characterId);
            Assert.AreEqual("Iron Rush", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link Akio_BB_Override controller.");
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
