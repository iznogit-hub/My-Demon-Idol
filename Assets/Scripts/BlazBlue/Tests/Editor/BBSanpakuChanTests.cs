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
            // Auto-run test suite after generator creates assets
            // Assets already generated

            var suite = new BBSanpakuChanTests();
            suite.Test_SanpakuChan_Assets_Exist_And_Valid();
            suite.Test_SanpakuChan_Drive_CrowStep_StocksAndRegen();
            suite.Test_SanpakuChan_SpeedNormals_And_Rekka();
            suite.Test_SanpakuChan_Specials_And_Teleports();
            suite.Test_SanpakuChan_RevolverAction_Chains();
            suite.Test_SanpakuChan_10_Trial_Challenges();
            suite.Test_SanpakuChan_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBSanpakuChanTests] ALL 7 SANPAKUCHAN COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_SanpakuChan_Assets_Exist_And_Valid()
        {
            // Assets already generated

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/SanpakuChan"}/BBMoveSet_C08_SanpakuChan.asset");
            Assert.IsNotNull(moveSet, "SanpakuChan BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 29, $"SanpakuChan should have at least 29 moves, found {moveSet.moves.Length}.");

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
        public void Test_SanpakuChan_SpeedNormals_And_Rekka()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/SanpakuChan"}/BBMoveSet_C08_SanpakuChan.asset");
            Assert.IsNotNull(moveSet);

            // Fastest overhead 6B (16 ticks)
            var move6B = FindMove(moveSet, "6B");
            Assert.IsNotNull(move6B, "6B Instant Hop Overhead must exist.");
            Assert.IsTrue(move6B.startupFrames > 0, "6B startup must be > 0 ticks.");
            Assert.IsTrue(move6B.guard == BBGuard.Mid || move6B.guard == BBGuard.Overhead);
            Assert.AreEqual(-4, move6B.advantageOnBlock);

            // 5C Fatal Counter
            var move5C = FindMove(moveSet, "5C");
            Assert.IsNotNull(move5C, "5C Heavy Shadow Hook must exist.");
            Assert.IsTrue(move5C.fatalCounter, "5C must have Fatal Counter enabled.");

            // 6A Anti-Air upper with Head invuln
            var move6A = FindMove(moveSet, "6A");
            Assert.IsNotNull(move6A, "6A Rising Anti-Air Upper must exist.");
            Assert.IsTrue(move6A.invuln.Length > 0, "6A must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);

            // 3-hit rekka sequence: 214B -> 214B_2 -> 214B_3
            var rekka1 = FindMove(moveSet, "214B");
            var rekka2 = FindMove(moveSet, "214B_2");
            var rekka3 = FindMove(moveSet, "214B_3");
            Assert.IsNotNull(rekka1, "Rekka 1 must exist.");
            Assert.IsNotNull(rekka2, "Rekka 2 must exist.");
            Assert.IsNotNull(rekka3, "Rekka 3 must exist.");

            Assert.IsTrue(Array.IndexOf(rekka1.gatlingInto, "214B_2") >= 0, "Rekka 1 must cancel into Rekka 2.");
            Assert.IsTrue(Array.IndexOf(rekka2.gatlingInto, "214B_3") >= 0, "Rekka 2 must cancel into Rekka 3.");
        }

        [Test]
        public void Test_SanpakuChan_Specials_And_Teleports()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/SanpakuChan"}/BBMoveSet_C08_SanpakuChan.asset");
            Assert.IsNotNull(moveSet);

            // Projectiles
            var needleGround = FindMove(moveSet, "236A");
            Assert.IsNotNull(needleGround, "236A Feather Needle must exist.");
            Assert.AreEqual(BBMotion.M236, needleGround.motion);
            Assert.AreEqual(BBAttribute.Projectile, needleGround.attribute);

            var needleAir = FindMove(moveSet, "j.236A");
            Assert.IsNotNull(needleAir, "j.236A Air Feather Needle must exist.");
            Assert.IsTrue(needleAir.airOk);
            Assert.IsFalse(needleAir.groundOk);

            // Crow Step teleports
            var move5D = FindMove(moveSet, "5D");
            Assert.IsNotNull(move5D, "5D Cross-Up Teleport must exist.");
            Assert.AreEqual(BBMoveKind.DriveMove, move5D.kind);
            Assert.IsTrue(move5D.startupFrames > 0);
            Assert.IsTrue(move5D.invuln.Length > 0, "5D must have strike invulnerability.");

            var move4D = FindMove(moveSet, "4D");
            Assert.IsNotNull(move4D, "4D Back Teleport must exist.");
            Assert.AreEqual(BBMoveKind.DriveMove, move4D.kind);

            var move2D = FindMove(moveSet, "2D");
            Assert.IsNotNull(move2D, "2D Low Slide Teleport must exist.");
            Assert.AreEqual(BBMoveKind.DriveMove, move2D.kind);

            var moveAirD = FindMove(moveSet, "j.D");
            Assert.IsNotNull(moveAirD, "j.D Air Teleport must exist.");
            Assert.AreEqual(BBMoveKind.DriveMove, moveAirD.kind);

            // Distortion Drives
            var superDance = FindMove(moveSet, "236236B");
            Assert.IsNotNull(superDance, "236236B Shadow Dance DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, superDance.kind);
            Assert.AreEqual(50, superDance.heatCost);
            Assert.IsTrue(superDance.damage > 0);

            var superPhantasm = FindMove(moveSet, "214214D");
            Assert.IsNotNull(superPhantasm, "214214D Crow Phantasm DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, superPhantasm.kind);
            Assert.AreEqual(50, superPhantasm.heatCost);
            Assert.IsTrue(superPhantasm.damage > 0);
            Assert.IsTrue(superPhantasm.invuln.Length > 0, "214214D must have startup invulnerability.");
        }

        [Test]
        public void Test_SanpakuChan_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/SanpakuChan"}/BBMoveSet_C08_SanpakuChan.asset");
            var move5A = FindMove(moveSet, "5A");
            var move5B = FindMove(moveSet, "5B");
            var move5C = FindMove(moveSet, "5C");

            var chain = new BBChainState();
            chain.madeContact = true;
            chain.contactTick = 4;

            // 5A -> 5B valid
            bool canGatling5B = BBCancelRules.CanCancel(in chain, move5A, move5B, 0, 4, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5B, "5A must cancel into 5B.");

            // 5B -> 5C valid
            chain.MarkUsed(1); // 5A used
            bool canGatling5C = BBCancelRules.CanCancel(in chain, move5B, move5C, 2, 6, onHit: true, onBlock: false, isGrounded: true);
            Assert.IsTrue(canGatling5C, "5B must cancel into 5C.");
        }

        [Test]
        public void Test_SanpakuChan_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/SanpakuChan"}/BBMoveSet_C08_SanpakuChan.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Speed Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Mixup Confirm
                new string[] { "6B", "5C", "214B" },                                      // Trial 3: Instant Hop Overhead Confirm
                new string[] { "5B", "3C", "j.A", "j.B", "j.C" },                         // Trial 4: Launcher Air Juggle
                new string[] { "5D", "5B", "5C" },                                        // Trial 5: Cross-Up Ambush Confirm
                new string[] { "5B", "5C", "214B", "214B_2", "214B_3" },                  // Trial 6: Complete 3-Hit Rekka Series
                new string[] { "236A", "5D", "5A" },                                      // Trial 7: Feather Needle Teleport Reset
                new string[] { "j.A", "j.236A", "j.D" },                                  // Trial 8: Aerial Needle Phasing Harass
                new string[] { "5B", "5C", "214B", "236236B" },                           // Trial 9: Rekka into Shadow Dance DD
                new string[] { "5C", "214214D", "OD_EA" }                                 // Trial 10: Fatal Counter Crow Phantasm Exceed Accel
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in SanpakuChan move set.");
                }
            }
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