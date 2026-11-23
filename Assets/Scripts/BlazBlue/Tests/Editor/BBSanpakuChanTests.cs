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
    public class BBSanpakuChanTests
    {
        public static void RunAllTestsMenu()
        {
            var suite = new BBSanpakuChanTests();
            suite.Test_SanpakuChan_7_Move_Kit_Assets_Valid();
            suite.Test_SanpakuChan_Drive_CrowStep_StocksAndRegen();
            suite.Test_SanpakuChan_Gatling_Chain_5A_to_Super();
            suite.Test_SanpakuChan_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBSanpakuChanTests] ALL SANPAKUCHAN 7-MOVE COMBAT TESTS PASSED!</color>");
        }

        [Test]
        public void Test_SanpakuChan_7_Move_Kit_Assets_Valid()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/SanpakuChan/BBMoveSet_C08_SanpakuChan.asset");
            Assert.IsNotNull(moveSet, "SanpakuChan BBMoveSet asset must exist.");
            Assert.AreEqual(7, moveSet.moves.Length, $"SanpakuChan moveset must contain exactly 7 active attack moves, found {moveSet.moves.Length}.");

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
        public void Test_SanpakuChan_Drive_CrowStep_StocksAndRegen()
        {
            var go = new GameObject("SanpakuChanTestGO");
            var drive = go.AddComponent<SanpakuChanDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Crow Step", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.Stocks, drive.ResourceKind);
            Assert.AreEqual(3, drive.CurrentResource);
            Assert.AreEqual(3, drive.MaxResource);

            // 2. Consume feather stocks
            drive.ConsumeFeather(1);
            Assert.AreEqual(2, drive.CurrentResource);

            drive.ConsumeFeather(2);
            Assert.AreEqual(0, drive.CurrentResource);

            // Cannot go below 0
            drive.ConsumeFeather(1);
            Assert.AreEqual(0, drive.CurrentResource);

            // 3. Regen over 150 ticks
            for (int t = 0; t < 149; t++)
            {
                drive.TickDrive(null, t);
            }
            Assert.AreEqual(0, drive.CurrentResource);

            drive.TickDrive(null, 149); // 150th tick
            Assert.AreEqual(1, drive.CurrentResource);

            // 4. Add feathers up to max
            drive.AddFeathers(2);
            Assert.AreEqual(3, drive.CurrentResource);
            drive.AddFeathers(1); // Clamped at 3
            Assert.AreEqual(3, drive.CurrentResource);

            // 5. Overdrive: free feathers
            drive.InOverdrive = true;
            Assert.AreEqual(3, drive.CurrentResource);
            drive.ConsumeFeather(3);
            Assert.AreEqual(3, drive.CurrentResource, "During Overdrive, feather consumption must be ignored/free.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_SanpakuChan_Gatling_Chain_5A_to_Super()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/SanpakuChan/BBMoveSet_C08_SanpakuChan.asset");
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
        public void Test_SanpakuChan_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C08_SanpakuChan.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C08_SanpakuChan must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C08_SanpakuChan must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C08_SanpakuChan", profile.characterId);
            Assert.AreEqual("Crow Step", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link SanpakuChan_BB_Override controller.");
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