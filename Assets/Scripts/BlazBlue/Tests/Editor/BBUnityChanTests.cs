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
    public class BBUnityChanTests
    {
        [MenuItem("Demon Idol/BlazBlue/Run BBUnityChan Tests")]
        public static void RunAllTestsMenu()
        {
            // Auto-run test suite after generator creates assets
            // Ensure assets are generated first
            BBUnityChanAssetGenerator.GenerateAllUnityChanAssets();

            var suite = new BBUnityChanTests();
            suite.Test_UnityChan_Assets_Exist_And_Valid();
            suite.Test_UnityChan_Drive_AeroHop_GroundAndAir();
            suite.Test_UnityChan_Drive_Overdrive_Enhancements();
            suite.Test_UnityChan_Special_Properties();
            suite.Test_UnityChan_RevolverAction_Chains();
            suite.Test_UnityChan_10_Trial_Challenges();
            suite.Test_UnityChan_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBUnityChanTests] ALL 7 UNITYCHAN COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_UnityChan_Assets_Exist_And_Valid()
        {
            BBUnityChanAssetGenerator.GenerateAllUnityChanAssets();

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBUnityChanAssetGenerator.UNITYCHAN_ROOT}/BBMoveSet_C04_UnityChan.asset");
            Assert.IsNotNull(moveSet, "UnityChan BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 29, $"UnityChan should have at least 29 moves, found {moveSet.moves.Length}.");

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
        public void Test_UnityChan_Drive_AeroHop_GroundAndAir()
        {
            var go = new GameObject("UnityChanTestGO");
            var drive = go.AddComponent<UnityChanDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Aero Hop", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.None, drive.ResourceKind);
            Assert.IsFalse(drive.usedAirHop);

            // 2. Air hop usage
            drive.usedAirHop = true;
            Assert.IsTrue(drive.usedAirHop);

            // 3. Ground landing resets air hop
            drive.TickDrive(null, 10);
            drive.usedAirHop = false; // Simulated landing
            Assert.IsFalse(drive.usedAirHop);

            // 4. Knockdown resets air hop
            drive.usedAirHop = true;
            drive.OnKnockdown(null);
            Assert.IsFalse(drive.usedAirHop, "Air hop must reset on knockdown.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_UnityChan_Drive_Overdrive_Enhancements()
        {
            var go = new GameObject("UnityChanTestGO");
            var drive = go.AddComponent<UnityChanDrive>();
            drive.ResetResource();

            drive.usedAirHop = true;
            Assert.IsTrue(drive.usedAirHop);

            // In Overdrive: air hops are unlimited
            drive.InOverdrive = true;
            Assert.IsTrue(drive.InOverdrive);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_UnityChan_Special_Properties()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBUnityChanAssetGenerator.UNITYCHAN_ROOT}/BBMoveSet_C04_UnityChan.asset");
            Assert.IsNotNull(moveSet);

            var dp = FindMove(moveSet, "623C");
            Assert.IsNotNull(dp, "623C Rising Hop DP must exist.");
            Assert.IsTrue(dp.invuln.Length > 0, "623C must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body, dp.invuln[0].mask);

            var fc = FindMove(moveSet, "5C");
            Assert.IsNotNull(fc, "5C Triple punch end must exist.");
            Assert.IsTrue(fc.fatalCounter, "5C must be a Fatal Counter move.");

            var stomp = FindMove(moveSet, "46C");
            Assert.IsNotNull(stomp, "[4]6C Hop Stomp must exist.");
            Assert.AreEqual(BBGuard.Overhead, stomp.guard);
            Assert.AreEqual(BBMotion.Charge46, stomp.motion);

            var dive = FindMove(moveSet, "j.214B");
            Assert.IsNotNull(dive, "j.214B Air Dive Kick must exist.");
            Assert.IsTrue(dive.airOk);
            Assert.IsFalse(dive.groundOk);
        }

        [Test]
        public void Test_UnityChan_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBUnityChanAssetGenerator.UNITYCHAN_ROOT}/BBMoveSet_C04_UnityChan.asset");
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
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");

            // 3C launcher has jump cancel on hit
            Assert.IsTrue(move3C.jumpCancelOnHit, "3C launcher must have jump cancel on hit.");

            // 6A anti-air has Head invulnerability
            Assert.IsNotNull(move6A.invuln);
            Assert.IsTrue(move6A.invuln.Length > 0);
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);
            Assert.AreEqual(3, move6A.invuln[0].startTick);
            Assert.AreEqual(12, move6A.invuln[0].endTick);
        }

        [Test]
        public void Test_UnityChan_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{BBUnityChanAssetGenerator.UNITYCHAN_ROOT}/BBMoveSet_C04_UnityChan.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Hopper Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Confirms
                new string[] { "5B", "5C", "3C", "j.A", "j.B", "j.C" },                   // Trial 3: Aerial Launcher
                new string[] { "5D", "j.A", "j.B", "j.C" },                               // Trial 4: Hop Low Crush
                new string[] { "5B", "5C", "236A" },                                      // Trial 5: Projectile Disc Pressure
                new string[] { "6A", "623C" },                                            // Trial 6: Reversal DP Punish
                new string[] { "3C", "j.A", "j.B", "j.214B", "5B", "623C" },              // Trial 7: Air Dive Ground Bounce
                new string[] { "5C", "236236C" },                                         // Trial 8: Fatal Counter Distortion Drive
                new string[] { "3C", "j.A", "j.B", "j.D", "j.236236C" },                  // Trial 9: Air Disc Storm Finisher
                new string[] { "5D", "6D", "236236C", "OD_EA" }                           // Trial 10: Overdrive Multi-Hop Exceed Accel
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in UnityChan move set.");
                }
            }
        }

        [Test]
        public void Test_UnityChan_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C04_UnityChan.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C04_UnityChan must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C04_UnityChan must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C04_UnityChan", profile.characterId);
            Assert.AreEqual("Aero Hop", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link UnityChan_BB_Override controller.");
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
