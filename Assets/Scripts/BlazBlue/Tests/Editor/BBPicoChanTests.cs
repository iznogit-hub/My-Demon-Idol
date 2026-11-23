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
            var suite = new BBPicoChanTests();
            suite.Test_PicoChan_7_Move_Kit_Assets_Valid();
            suite.Test_PicoChan_Drive_Clinch_StocksAndOD();
            suite.Test_PicoChan_Gatling_Chain_5A_to_Super();
            suite.Test_PicoChan_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBPicoChanTests] ALL PICOCHAN 7-MOVE COMBAT TESTS PASSED!</color>");
        }

        [Test]
        public void Test_PicoChan_7_Move_Kit_Assets_Valid()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/PicoChan/BBMoveSet_C09_PicoChan.asset");
            Assert.IsNotNull(moveSet, "PicoChan BBMoveSet asset must exist.");
            Assert.AreEqual(7, moveSet.moves.Length, $"PicoChan moveset must contain exactly 7 active attack moves, found {moveSet.moves.Length}.");

            string[] expectedIds = new string[] { "5A", "5B", "5C", "2C", "j.B", "5D", "236236C" };

            for (int i = 0; i < moveSet.moves.Length; i++)
            {
                var m = moveSet.moves[i];
                Assert.IsNotNull(m, $"Move at index {i} must not be null.");
                Assert.AreEqual(expectedIds[i], m.moveId, $"Move at index {i} must be {expectedIds[i]}, got {m.moveId}.");
                Assert.IsTrue(m.startupFrames > 0, $"{m.moveId} startupFrames must be > 0.");
                Assert.IsTrue(m.activeFrames > 0, $"{m.moveId} activeFrames must be > 0.");
                Assert.IsTrue(m.recoveryFrames > 0, $"{m.moveId} recoveryFrames must be > 0.");
                Assert.IsNotNull(m.boxes, $"{m.moveId} must have an attached BBBoxTrack.");
                Assert.AreEqual(m.TotalTicks, m.boxes.tickCount, $"{m.moveId} TotalTicks must match box track tickCount.");

                int expectedRecovery = m.blockstunTicks - (m.activeFrames - 1) - m.advantageOnBlock;
                Assert.AreEqual(expectedRecovery, m.recoveryFrames, $"{m.moveId} Dustloop recovery math must match: expected {expectedRecovery}, got {m.recoveryFrames}.");

                Assert.IsNotNull(m.boxes.keyTicks, $"{m.moveId} box track keyTicks must exist.");
                Assert.IsTrue(m.boxes.keyTicks.Length > 0, $"{m.moveId} box track keyTicks must not be empty.");
                Assert.AreEqual(m.startupFrames, m.boxes.keyTicks[0], $"{m.moveId} peakLimbTick must equal firstActiveTick ({m.startupFrames}).");

                var clip = m.clip != null ? m.clip : m.animationClip;
                Assert.IsNotNull(clip, $"{m.moveId} must have an assigned AnimationClip from the Karate pack.");
                string clipAssetPath = AssetDatabase.GetAssetPath(clip);
                Assert.IsTrue(clipAssetPath.Contains("Karate"), $"{m.moveId} clip path '{clipAssetPath}' must be from Karate Warrior pack.");
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
        public void Test_PicoChan_Gatling_Chain_5A_to_Super()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/PicoChan/BBMoveSet_C09_PicoChan.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");
            var move5D = FindMove(moveSet, "5D");
            var moveSuper = FindMove(moveSet, "236236C");

            Assert.IsNotNull(move5A, "5A must exist.");
            Assert.IsNotNull(move5B, "5B must exist.");
            Assert.IsNotNull(move5C, "5C must exist.");
            Assert.IsNotNull(move5D, "5D must exist.");
            Assert.IsNotNull(moveSuper, "236236C must exist.");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 7;

            // 1. 5A -> 5B is valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 1, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 2. 5B -> 5C is valid
            chain.MarkUsed(0); // 5A used
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");

            // 3. 5C -> 5D is valid
            chain.MarkUsed(1); // 5B used
            bool canGatling5D = BBCancelRules.CanCancel(in chain, move5C, move5D, 5, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5D, "5C must cancel into 5D.");

            // 4. 5D -> 236236C (Super) is valid
            chain.MarkUsed(5); // 5D used
            bool canCancelSuper = BBCancelRules.CanCancel(in chain, move5D, moveSuper, 6, 7, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canCancelSuper, "5D must cancel into 236236C (Super).");
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
