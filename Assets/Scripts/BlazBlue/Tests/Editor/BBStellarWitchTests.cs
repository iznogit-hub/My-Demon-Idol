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
    public class BBStellarWitchTests
    {
        public static void RunAllTestsMenu()
        {
            // Auto-run test suite after generator creates assets
            // Assets already generated

            var suite = new BBStellarWitchTests();
            suite.Test_StellarWitch_Assets_Exist_And_Valid();
            suite.Test_StellarWitch_Drive_StarField_PlacementsAndComets();
            suite.Test_StellarWitch_SolidNormals_Properties();
            suite.Test_StellarWitch_Specials_And_Supernova();
            suite.Test_StellarWitch_RevolverAction_Chains();
            suite.Test_StellarWitch_10_Trial_Challenges();
            suite.Test_StellarWitch_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBStellarWitchTests] ALL 7 STELLARWITCH COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_StellarWitch_Assets_Exist_And_Valid()
        {
            // Assets already generated

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/StellarWitch"}/BBMoveSet_C07_StellarWitch.asset");
            Assert.IsNotNull(moveSet, "StellarWitch BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 28, $"StellarWitch should have at least 28 moves, found {moveSet.moves.Length}.");

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
        public void Test_StellarWitch_Drive_StarField_PlacementsAndComets()
        {
            var go = new GameObject("StellarWitchTestGO");
            var drive = go.AddComponent<StellarWitchDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Star Field", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.Stocks, drive.ResourceKind);
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.AreEqual(3, drive.MaxResource);

            // 2. Place star points
            Assert.IsTrue(drive.PlaceStarPoint(new Vector2(2f, 1f), StellarWitchDrive.StarPointType.Alpha));
            Assert.AreEqual(1, drive.CurrentResource);
            Assert.IsTrue(drive.IsStarActive(0));
            Assert.AreEqual(new Vector2(2f, 1f), drive.GetStarPosition(0));

            Assert.IsTrue(drive.PlaceStarPoint(new Vector2(1.5f, 0.2f), StellarWitchDrive.StarPointType.Beta));
            Assert.AreEqual(2, drive.CurrentResource);

            Assert.IsTrue(drive.PlaceStarPoint(new Vector2(3f, 2f), StellarWitchDrive.StarPointType.Gamma));
            Assert.AreEqual(3, drive.CurrentResource);

            // 3. Firing comets in non-OD consumes the stars
            int fired = drive.FireComets();
            Assert.AreEqual(3, fired);
            Assert.AreEqual(3, drive.TotalCometsFired);
            Assert.AreEqual(0, drive.CurrentResource);

            // 4. In Overdrive: Max resource is 4, and comets do NOT consume the stars
            drive.InOverdrive = true;
            Assert.AreEqual(4, drive.MaxResource);

            drive.PlaceStarPoint(new Vector2(2f, 1f), StellarWitchDrive.StarPointType.Alpha);
            drive.PlaceStarPoint(new Vector2(1.5f, 0.2f), StellarWitchDrive.StarPointType.Beta);
            drive.PlaceStarPoint(new Vector2(3f, 2f), StellarWitchDrive.StarPointType.Gamma);
            drive.PlaceStarPoint(new Vector2(2.5f, 1.5f), StellarWitchDrive.StarPointType.Delta);
            Assert.AreEqual(4, drive.CurrentResource);

            fired = drive.FireComets();
            Assert.AreEqual(4, fired);
            Assert.AreEqual(4, drive.CurrentResource, "In Overdrive, star points persist after comet fire.");

            // 5. Detonating all stars
            int detonated = drive.DetonateAllStars();
            Assert.AreEqual(4, detonated);
            Assert.AreEqual(0, drive.CurrentResource);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_StellarWitch_SolidNormals_Properties()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/StellarWitch"}/BBMoveSet_C07_StellarWitch.asset");
            Assert.IsNotNull(moveSet);

            var n5A = FindMove(moveSet, "5A");
            Assert.IsNotNull(n5A);
            Assert.AreEqual(BBButton.A, n5A.button);
            Assert.IsTrue(n5A.startupFrames > 0);
            Assert.AreEqual(BBGuard.Mid, n5A.guard);

            var n2A = FindMove(moveSet, "2A");
            Assert.IsNotNull(n2A);
            Assert.AreEqual(BBGuard.Low, n2A.guard);

            var n6A = FindMove(moveSet, "6A");
            Assert.IsNotNull(n6A);
            Assert.AreEqual(BBAttribute.Head, n6A.attribute);
            Assert.IsTrue(n6A.invuln.Length > 0, "6A must have head invulnerability window.");
            Assert.AreEqual(BBInvuln.Head, n6A.invuln[0].mask);

            var n5C = FindMove(moveSet, "5C");
            Assert.IsNotNull(n5C);
            Assert.IsTrue(n5C.fatalCounter, "5C must be a Fatal Counter move.");
            Assert.IsTrue(n5C.damage > 0);

            var n2C = FindMove(moveSet, "2C");
            Assert.IsNotNull(n2C);
            Assert.AreEqual(BBGuard.Low, n2C.guard);

            var n6B = FindMove(moveSet, "6B");
            Assert.IsNotNull(n6B);
            Assert.IsTrue(n6B.guard == BBGuard.Mid || n6B.guard == BBGuard.Overhead);

            var n3C = FindMove(moveSet, "3C");
            Assert.IsNotNull(n3C);
            Assert.IsTrue(n3C.jumpCancelOnHit, "3C launcher must have jumpCancelOnHit enabled.");
        }

        [Test]
        public void Test_StellarWitch_Specials_And_Supernova()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/StellarWitch"}/BBMoveSet_C07_StellarWitch.asset");
            Assert.IsNotNull(moveSet);

            var sp236A = FindMove(moveSet, "236A");
            Assert.IsNotNull(sp236A);
            Assert.AreEqual(BBMotion.M236, sp236A.motion);
            Assert.AreEqual(BBAttribute.Projectile, sp236A.attribute);

            var sp623C = FindMove(moveSet, "623C");
            Assert.IsNotNull(sp623C);
            Assert.AreEqual(BBMotion.M623, sp623C.motion);
            Assert.IsTrue(sp623C.invuln.Length > 0, "623C Nova Pillar must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body | BBInvuln.Foot, sp623C.invuln[0].mask);

            var sp214B = FindMove(moveSet, "214B");
            Assert.IsNotNull(sp214B);
            Assert.AreEqual(BBMotion.M214, sp214B.motion);
            Assert.IsTrue(sp214B.invuln.Length > 0, "214B Eclipse Shift must have full invulnerability.");

            var sp236C = FindMove(moveSet, "236C");
            Assert.IsNotNull(sp236C);
            Assert.IsTrue(sp236C.damage > 0);

            var ddBeam = FindMove(moveSet, "236236C");
            Assert.IsNotNull(ddBeam);
            Assert.IsTrue(ddBeam.damage > 0);
            Assert.AreEqual(50, ddBeam.heatCost);

            var ddVortex = FindMove(moveSet, "632146C");
            Assert.IsNotNull(ddVortex);
            Assert.IsTrue(ddVortex.damage > 0);
            Assert.AreEqual(50, ddVortex.heatCost);
        }

        [Test]
        public void Test_StellarWitch_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/StellarWitch"}/BBMoveSet_C07_StellarWitch.asset");
            Assert.IsNotNull(moveSet);

            var m5A = FindMove(moveSet, "5A");
            var m5B = FindMove(moveSet, "5B");
            var m5C = FindMove(moveSet, "5C");
            var m5D = FindMove(moveSet, "5D");

            Assert.IsTrue(Array.IndexOf(m5A.gatlingInto, "5B") >= 0, "5A must cancel into 5B.");
            Assert.IsTrue(Array.IndexOf(m5B.gatlingInto, "5C") >= 0, "5B must cancel into 5C.");
            Assert.IsTrue(Array.IndexOf(m5C.gatlingInto, "5D") >= 0, "5C must cancel into 5D.");
        }

        [Test]
        public void Test_StellarWitch_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/StellarWitch"}/BBMoveSet_C07_StellarWitch.asset");
            Assert.IsNotNull(moveSet);

            string[][] trials = new string[][]
            {
                new string[] { "5A", "5B", "5C" },
                new string[] { "2A", "2B", "2C" },
                new string[] { "5B", "3C", "j.A", "j.B", "j.C" },
                new string[] { "6B", "5C", "236A" },
                new string[] { "5D", "236A", "CometFire" },
                new string[] { "6A", "623C" },
                new string[] { "5D", "2D", "236C" },
                new string[] { "5B", "5C", "236236C" },
                new string[] { "3C", "j.C", "632146C" },
                new string[] { "5D", "6D", "236236C", "OD_EA" }
            };

            for (int t = 0; t < trials.Length; t++)
            {
                for (int m = 0; m < trials[t].Length; m++)
                {
                    var moveId = trials[t][m];
                    var move = FindMove(moveSet, moveId);
                    Assert.IsNotNull(move, $"Trial {t + 1} step {m + 1} move '{moveId}' must exist in moveSet.");
                }
            }
        }

        [Test]
        public void Test_StellarWitch_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C07_StellarWitch.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C07_StellarWitch.asset must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "StellarWitch CharacterDefinition must link to blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C07_StellarWitch", profile.characterId);
            Assert.AreEqual("Star Field", profile.driveName);
            Assert.IsNotNull(profile.moveSet, "Profile must link to BBMoveSet.");
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link to StellarWitch_BB_Override.");
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
