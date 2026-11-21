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
    public class BBPicoChanTests
    {
        public static void RunAllTestsMenu()
        {
            // Auto-run test suite after generator creates assets
            // Assets already generated

            var suite = new BBPicoChanTests();
            suite.Test_PicoChan_Assets_Exist_And_Valid();
            suite.Test_PicoChan_Drive_Clinch_StocksAndOD();
            suite.Test_PicoChan_CommandThrows_Properties();
            suite.Test_PicoChan_FatalCounter_And_Specials();
            suite.Test_PicoChan_RevolverAction_Chains();
            suite.Test_PicoChan_10_Trial_Challenges();
            suite.Test_PicoChan_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBPicoChanTests] ALL 7 PICOCHAN COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_PicoChan_Assets_Exist_And_Valid()
        {
            // Assets already generated

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/PicoChan"}/BBMoveSet_C09_PicoChan.asset");
            Assert.IsNotNull(moveSet, "PicoChan BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 29, $"PicoChan should have at least 29 moves, found {moveSet.moves.Length}.");

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
        public void Test_PicoChan_Drive_Clinch_StocksAndOD()
        {
            var go = new GameObject("PicoChanTestGO");
            var drive = go.AddComponent<PicoChanDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Clinch", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.Stocks, drive.ResourceKind);
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.AreEqual(2, drive.MaxResource);
            Assert.IsFalse(drive.HasGrip);

            // 2. Banking Grip
            drive.BankGrip(1);
            Assert.AreEqual(1, drive.CurrentResource);
            Assert.IsTrue(drive.HasGrip);

            drive.BankGrip(1);
            Assert.AreEqual(2, drive.CurrentResource);

            // Cap at 2
            drive.BankGrip(1);
            Assert.AreEqual(2, drive.CurrentResource);

            // 3. Consuming Grip
            Assert.IsTrue(drive.TryConsumeGrip(1));
            Assert.AreEqual(1, drive.CurrentResource);

            Assert.IsTrue(drive.TryConsumeGrip(1));
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.IsFalse(drive.HasGrip);
            Assert.IsFalse(drive.TryConsumeGrip(1));

            // 4. Overdrive enhancements
            drive.InOverdrive = true;
            Assert.AreEqual(2, drive.CurrentResource);
            Assert.IsTrue(drive.HasGrip);
            Assert.IsTrue(drive.TryConsumeGrip(1), "OD provides infinite Grip consumption.");
            Assert.AreEqual(0.4f, drive.GetGrabRangeBonus("5D"), "OD gives +0.4m range bonus on D grabs.");
            Assert.IsTrue(drive.HasArmor(3, "5D"), "OD gives armor on ticks 1-6.");
            Assert.IsFalse(drive.HasArmor(8, "5D"), "OD armor expires after tick 6.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_PicoChan_CommandThrows_Properties()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/PicoChan"}/BBMoveSet_C09_PicoChan.asset");
            Assert.IsNotNull(moveSet);

            var move5D = FindMove(moveSet, "5D");
            Assert.IsNotNull(move5D, "5D Clinch must exist.");
            Assert.IsTrue(move5D.kind == BBMoveKind.DriveMove || move5D.kind == BBMoveKind.CommandThrow);
            Assert.IsTrue(move5D.startupFrames > 0);

            var move2D = FindMove(moveSet, "2D");
            Assert.IsNotNull(move2D, "2D Low Clinch must exist.");
            Assert.IsTrue(move2D.kind == BBMoveKind.DriveMove || move2D.kind == BBMoveKind.CommandThrow);
            Assert.AreEqual(BBGuard.Low, move2D.guard);

            var move6D = FindMove(moveSet, "6D");
            Assert.IsNotNull(move6D, "6D Run Clinch must exist.");
            Assert.IsTrue(move6D.kind == BBMoveKind.DriveMove || move6D.kind == BBMoveKind.CommandThrow);

            var move360C = FindMove(moveSet, "360C");
            Assert.IsNotNull(move360C, "360C Big Slam Throw must exist.");
            Assert.IsTrue(move360C.kind == BBMoveKind.Special || move360C.kind == BBMoveKind.CommandThrow);
            Assert.IsTrue(move360C.startupFrames > 0);
            Assert.IsTrue(move360C.damage > 0);

            var movePress = FindMove(moveSet, "632146C");
            Assert.IsNotNull(movePress, "632146C Pico Press DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, movePress.kind);
            Assert.IsTrue(movePress.startupFrames > 0);
            Assert.IsTrue(movePress.damage > 0);
        }

        [Test]
        public void Test_PicoChan_FatalCounter_And_Specials()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/PicoChan"}/BBMoveSet_C09_PicoChan.asset");
            Assert.IsNotNull(moveSet);

            var move5C = FindMove(moveSet, "5C");
            Assert.IsNotNull(move5C, "5C Heavy Hook must exist.");
            Assert.IsTrue(move5C.fatalCounter, "5C must be a Fatal Counter move.");

            var dp = FindMove(moveSet, "623C");
            Assert.IsNotNull(dp, "623C Vicious Uppercut must exist.");
            Assert.IsTrue(dp.invuln.Length > 0, "623C must have invulnerability.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body, dp.invuln[0].mask);

            var charge = FindMove(moveSet, "46C");
            Assert.IsNotNull(charge, "[4]6C Charged Straight must exist.");
            Assert.AreEqual(BBMotion.Charge46, charge.motion);

            var stepHook = FindMove(moveSet, "63214B");
            Assert.IsNotNull(stepHook, "63214B Step In Hook must exist.");
            Assert.IsTrue(stepHook.specialCancel, "63214B must allow special cancel into 5D on hit.");
        }

        [Test]
        public void Test_PicoChan_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/PicoChan"}/BBMoveSet_C09_PicoChan.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");
            var move3C = FindMove(moveSet, "3C");
            var move6A = FindMove(moveSet, "6A");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 5;

            // 5A -> 5B valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 0, 5, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 5B -> 5C valid
            chain.MarkUsed(1); // 5A used
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 8, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");

            // 3C launcher has jump cancel on hit
            Assert.IsTrue(move3C.jumpCancelOnHit, "3C knee launcher must have jump cancel on hit.");

            // 6A anti-air has Head invulnerability
            Assert.IsNotNull(move6A.invuln);
            Assert.IsTrue(move6A.invuln.Length > 0);
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);
            Assert.AreEqual(4, move6A.invuln[0].startTick);
            Assert.AreEqual(12, move6A.invuln[0].endTick);
        }

        [Test]
        public void Test_PicoChan_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/PicoChan"}/BBMoveSet_C09_PicoChan.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Grappler Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Sweep Confirm
                new string[] { "5B", "5C", "3C", "j.A", "j.B", "j.C" },                   // Trial 3: Knee Launch Air Chain
                new string[] { "5A", "5B", "5D" },                                        // Trial 4: Tick Clinch Grab
                new string[] { "2A", "2B", "236A" },                                      // Trial 5: Lariat Knockdown
                new string[] { "63214B", "5D" },                                          // Trial 6: Step Hook to Clinch Cancel
                new string[] { "6A", "623C" },                                            // Trial 7: Anti-Air Uppercut Reversal
                new string[] { "5C", "236236B" },                                         // Trial 8: Fatal Counter Close Flurry
                new string[] { "5B", "3C", "j.A", "j.D" },                                // Trial 9: Air Clinch Reset
                new string[] { "5D", "360C", "632146C" }                                  // Trial 10: Grip Bank into Empowered Pico Press
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in PicoChan move set.");
                }
            }
        }

        [Test]
        public void Test_PicoChan_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C09_PicoChan.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C09_PicoChan must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C09_PicoChan must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C09_PicoChan", profile.characterId);
            Assert.AreEqual("Clinch", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link PicoChan_BB_Override controller.");
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
