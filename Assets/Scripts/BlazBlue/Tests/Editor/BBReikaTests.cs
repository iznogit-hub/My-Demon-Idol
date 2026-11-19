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
    public class BBReikaTests
    {
        [MenuItem("Demon Idol/BlazBlue/Run BBReika Tests")]
        public static void RunAllTestsMenu()
        {
            // Auto-run test suite after generator creates assets
            BBReikaAssetGenerator.GenerateAllReikaAssets();

            var suite = new BBReikaTests();
            suite.Test_Reika_Assets_Exist_And_Valid();
            suite.Test_Reika_Drive_Veil_GuardPointAndRiposte();
            suite.Test_Reika_SolidNormals_Properties();
            suite.Test_Reika_Specials_And_CounterDD();
            suite.Test_Reika_RevolverAction_Chains();
            suite.Test_Reika_10_Trial_Challenges();
            suite.Test_Reika_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBReikaTests] ALL 7 REIKA COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Reika_Assets_Exist_And_Valid()
        {
            BBReikaAssetGenerator.GenerateAllReikaAssets();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBReikaAssetGenerator.REIKA_ROOT}/BBMoveSet_C10_Reika.asset");
            Assert.IsNotNull(moveSet, "Reika BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 27, $"Reika should have at least 27 moves, found {moveSet.moves.Length}.");

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
        public void Test_Reika_Drive_Veil_GuardPointAndRiposte()
        {
            var go = new GameObject("ReikaTestGO");
            var drive = go.AddComponent<ReikaDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Veil", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.Stocks, drive.ResourceKind);
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.IsFalse(drive.InCounterStance);

            // 2. Standing 5D Veil Counter Stance
            drive.StartStance(BBGuard.Mid);
            Assert.IsTrue(drive.InCounterStance);

            // Default active window: ticks 4 to 14
            Assert.IsFalse(drive.CheckGuardPointActive(3, BBGuard.Mid));
            Assert.IsTrue(drive.CheckGuardPointActive(4, BBGuard.Mid));
            Assert.IsTrue(drive.CheckGuardPointActive(10, BBGuard.Overhead));
            Assert.IsTrue(drive.CheckGuardPointActive(14, BBGuard.Mid));
            Assert.IsFalse(drive.CheckGuardPointActive(15, BBGuard.Mid));
            Assert.IsFalse(drive.CheckGuardPointActive(8, BBGuard.Low), "5D does not guard low strikes.");

            // 3. Crouching 2D Veil Counter Stance
            drive.StartStance(BBGuard.Low);
            Assert.IsTrue(drive.CheckGuardPointActive(8, BBGuard.Low), "2D guards low strikes.");
            Assert.IsFalse(drive.CheckGuardPointActive(8, BBGuard.Mid), "2D does not guard mid strikes.");

            // 4. Riposte auto-punish
            Assert.IsTrue(drive.TriggerRiposte(null));
            Assert.AreEqual(1, drive.CurrentResource);
            Assert.IsFalse(drive.InCounterStance);

            // 5. Overdrive: window expands to ticks 2 to 20
            drive.InOverdrive = true;
            drive.StartStance(BBGuard.Mid);
            Assert.IsFalse(drive.CheckGuardPointActive(1, BBGuard.Mid));
            Assert.IsTrue(drive.CheckGuardPointActive(2, BBGuard.Mid));
            Assert.IsTrue(drive.CheckGuardPointActive(20, BBGuard.Mid));
            Assert.IsFalse(drive.CheckGuardPointActive(21, BBGuard.Mid));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Reika_SolidNormals_Properties()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBReikaAssetGenerator.REIKA_ROOT}/BBMoveSet_C10_Reika.asset");
            Assert.IsNotNull(moveSet);

            // 6A Anti-Air upper with Head invuln
            var move6A = FindMove(moveSet, "6A");
            Assert.IsNotNull(move6A, "6A High Rising Block Strike must exist.");
            Assert.AreEqual(9, move6A.startupFrames);
            Assert.IsTrue(move6A.invuln.Length > 0, "6A must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);

            // 5B Karate Straight Poke
            var move5B = FindMove(moveSet, "5B");
            Assert.IsNotNull(move5B, "5B Karate Straight Punch must exist.");
            Assert.AreEqual(8, move5B.startupFrames);
            Assert.AreEqual(-3, move5B.advantageOnBlock);
            Assert.IsTrue(move5B.jumpCancelOnHit, "5B allows jump cancel on hit.");

            // 5C Fatal Counter Chop
            var move5C = FindMove(moveSet, "5C");
            Assert.IsNotNull(move5C, "5C Heavy Karate Chop must exist.");
            Assert.IsTrue(move5C.fatalCounter, "5C must have Fatal Counter enabled.");

            // 6B Axe Kick Overhead
            var move6B = FindMove(moveSet, "6B");
            Assert.IsNotNull(move6B, "6B Axe Kick Overhead must exist.");
            Assert.AreEqual(BBGuard.Overhead, move6B.guard);
            Assert.AreEqual(19, move6B.startupFrames);

            // 3C Rising Counter Launcher
            var move3C = FindMove(moveSet, "3C");
            Assert.IsNotNull(move3C, "3C Rising Counter Launcher must exist.");
            Assert.IsTrue(move3C.jumpCancelOnHit, "3C must allow jump cancel on hit.");
        }

        [Test]
        public void Test_Reika_Specials_And_CounterDD()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBReikaAssetGenerator.REIKA_ROOT}/BBMoveSet_C10_Reika.asset");
            Assert.IsNotNull(moveSet);

            // 236B Crimson Crescent Wave
            var crescent = FindMove(moveSet, "236B");
            Assert.IsNotNull(crescent, "236B Crimson Crescent Wave must exist.");
            Assert.AreEqual(BBMotion.M236, crescent.motion);
            Assert.AreEqual(BBAttribute.Projectile, crescent.attribute);

            // 41236C Crimson Lunge Punch
            var lunge = FindMove(moveSet, "41236C");
            Assert.IsNotNull(lunge, "41236C Crimson Lunge Punch must exist.");
            Assert.AreEqual(BBMotion.M41236, lunge.motion);
            Assert.AreEqual(850, lunge.damage);

            // 623C Rising Strike DP
            var dp = FindMove(moveSet, "623C");
            Assert.IsNotNull(dp, "623C Rising Strike DP must exist.");
            Assert.AreEqual(BBMotion.M623, dp.motion);
            Assert.IsTrue(dp.invuln.Length > 0, "623C must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body, dp.invuln[0].mask);

            // 236236C Absolute Veil Counter DD
            var counterDD = FindMove(moveSet, "236236C");
            Assert.IsNotNull(counterDD, "236236C Absolute Veil Counter DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, counterDD.kind);
            Assert.AreEqual(50, counterDD.heatCost);
            Assert.AreEqual(2600, counterDD.damage);
            Assert.AreEqual(1, counterDD.startupFrames);

            // 632146C Crimson Kata DD
            var kataDD = FindMove(moveSet, "632146C");
            Assert.IsNotNull(kataDD, "632146C Crimson Kata DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, kataDD.kind);
            Assert.AreEqual(50, kataDD.heatCost);
            Assert.AreEqual(2700, kataDD.damage);
            Assert.IsTrue(kataDD.invuln.Length > 0, "632146C must have startup invulnerability.");
        }

        [Test]
        public void Test_Reika_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBReikaAssetGenerator.REIKA_ROOT}/BBMoveSet_C10_Reika.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 6;

            // 5A -> 5B valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 0, 6, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 5B -> 5C valid
            chain.MarkUsed(1); // 5A used
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 8, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");
        }

        [Test]
        public void Test_Reika_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBReikaAssetGenerator.REIKA_ROOT}/BBMoveSet_C10_Reika.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Karate Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Sweep Confirm
                new string[] { "5B", "3C", "j.A", "j.B", "j.C" },                         // Trial 3: Rising Launcher Air Juggle
                new string[] { "6B", "5C", "41236C" },                                    // Trial 4: Axe Kick Overhead Confirm
                new string[] { "5B", "236B" },                                            // Trial 5: Crescent Wave Mid-Range Poke
                new string[] { "6A", "623C" },                                            // Trial 6: Anti-Air Rising Strike Reversal
                new string[] { "5B", "5C", "41236C", "632146C" },                         // Trial 7: Lunge Punch into Crimson Kata DD
                new string[] { "5D", "VeilRiposte", "41236C" },                           // Trial 8: Veil Counter Riposte Punish
                new string[] { "2D", "VeilRiposte", "5B", "5C" },                         // Trial 9: Low Counter Riposte String
                new string[] { "236236C", "OD_EA" }                                       // Trial 10: Absolute Veil Counter Overdrive Exceed Accel
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in Reika move set.");
                }
            }
        }

        [Test]
        public void Test_Reika_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C10_Reika.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C10_Reika must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C10_Reika must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C10_Reika", profile.characterId);
            Assert.AreEqual("Veil", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link Reika_BB_Override controller.");
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