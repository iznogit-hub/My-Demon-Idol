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
        public static void RunAllTestsMenu()
        {
            var suite = new BBAikaTests();
            suite.Test_Aika_7_Move_Kit_Assets_Valid();
            suite.Test_Aika_Drive_ResonanceNail_Stocks_And_Regen();
            suite.Test_Aika_Drive_Detonate_And_ActiveNails();
            suite.Test_Aika_Drive_Overdrive_Enhancements();
            suite.Test_Aika_Gatling_Chain_5A_to_Super();
            suite.Test_Aika_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBAikaTests] ALL AIKA 7-MOVE COMBAT TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Aika_7_Move_Kit_Assets_Valid()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/Aika/BBMoveSet_C02_Aika.asset");
            Assert.IsNotNull(moveSet, "Aika BBMoveSet asset must exist.");
            Assert.AreEqual(7, moveSet.moves.Length, $"Aika moveset must contain exactly 7 active attack moves, found {moveSet.moves.Length}.");

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
                Assert.IsNotNull(clip, $"{m.moveId} must have an assigned AnimationClip from the Ninja pack.");
                string clipAssetPath = AssetDatabase.GetAssetPath(clip);
                Assert.IsTrue(clipAssetPath.Contains("Ninja"), $"{m.moveId} clip path '{clipAssetPath}' must be from Ninja Warrior pack.");
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
        public void Test_Aika_Gatling_Chain_5A_to_Super()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>("Assets/CombatData/BlazBlue/Aika/BBMoveSet_C02_Aika.asset");
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
