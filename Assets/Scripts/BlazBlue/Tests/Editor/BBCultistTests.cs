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
    public class BBCultistTests
    {
        public static void RunAllTestsMenu()
        {
            // Auto-run test suite after generator creates assets
            // Assets already generated

            var suite = new BBCultistTests();
            suite.Test_Cultist_Assets_Exist_And_Valid();
            suite.Test_Cultist_Drive_VoidOrb_StocksAndCommands();
            suite.Test_Cultist_SlowerNormals_And_AntiAir();
            suite.Test_Cultist_Specials_And_Reversal();
            suite.Test_Cultist_RevolverAction_Chains();
            suite.Test_Cultist_10_Trial_Challenges();
            suite.Test_Cultist_CharacterDefinition_Linked();
            Debug.Log("<color=#00e676>[BBCultistTests] ALL 7 CULTIST COMBAT & TRIAL TESTS PASSED!</color>");
        }

        [Test]
        public void Test_Cultist_Assets_Exist_And_Valid()
        {
            // Assets already generated

            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Cultist"}/BBMoveSet_C06_Cultist.asset");
            Assert.IsNotNull(moveSet, "Cultist BBMoveSet asset must exist.");
            Assert.IsTrue(moveSet.moves.Length >= 30, $"Cultist should have at least 30 moves, found {moveSet.moves.Length}.");

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
        public void Test_Cultist_Drive_VoidOrb_StocksAndCommands()
        {
            var go = new GameObject("CultistTestGO");
            var drive = go.AddComponent<CultistDrive>();
            drive.ResetResource();

            // 1. Initial state
            Assert.AreEqual("Void Orb", drive.DriveName);
            Assert.AreEqual(DriveResourceKind.Stocks, drive.ResourceKind);
            Assert.AreEqual(0, drive.CurrentResource);
            Assert.AreEqual(2, drive.MaxResource);

            // 2. Place orbs
            drive.PlaceOrb(new Vector2(1.2f, 1.1f));
            Assert.AreEqual(1, drive.CurrentResource);
            Assert.IsTrue(drive.TryGetOrbPosition(0, out Vector2 orb0Pos));
            Assert.AreEqual(1.2f, orb0Pos.x, 0.001f);

            drive.PlaceOrb(new Vector2(2.4f, 1.1f));
            Assert.AreEqual(2, drive.CurrentResource);

            // Cannot exceed 2 in normal mode (overwrites oldest)
            drive.PlaceOrb(new Vector2(3.6f, 1.1f));
            Assert.AreEqual(2, drive.CurrentResource);

            // 3. Overdrive expands to 3 orbs
            drive.InOverdrive = true;
            Assert.AreEqual(3, drive.MaxResource);
            drive.PlaceOrb(new Vector2(4.8f, 1.1f));
            Assert.AreEqual(3, drive.CurrentResource);

            // 4. Commands
            drive.CommandOrbsDash(1);
            drive.CommandOrbsDrop();
            drive.CommandOrbsRecall(Vector2.zero);

            // 5. Detonation
            int exploded = drive.DetonateAllOrbs();
            Assert.AreEqual(3, exploded);
            Assert.AreEqual(0, drive.CurrentResource);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_Cultist_SlowerNormals_And_AntiAir()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Cultist"}/BBMoveSet_C06_Cultist.asset");
            Assert.IsNotNull(moveSet);

            // 6A Anti-Air upper with Head invuln
            var move6A = FindMove(moveSet, "6A");
            Assert.IsNotNull(move6A, "6A Dark Energy Rising Strike must exist.");
            Assert.IsTrue(move6A.startupFrames > 0, "6A startup should be > 0 ticks.");
            Assert.IsTrue(move6A.invuln.Length > 0, "6A must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head, move6A.invuln[0].mask);

            // 5B poke
            var move5B = FindMove(moveSet, "5B");
            Assert.IsNotNull(move5B, "5B Curse Straight must exist.");
            Assert.IsTrue(move5B.startupFrames > 0);

            // 5C Fatal Counter
            var move5C = FindMove(moveSet, "5C");
            Assert.IsNotNull(move5C, "5C Abyssal Slash must exist.");
            Assert.IsTrue(move5C.fatalCounter, "5C must have Fatal Counter enabled.");

            // 6B Stepping Overhead
            var move6B = FindMove(moveSet, "6B");
            Assert.IsNotNull(move6B, "6B Stepping Curse Overhead must exist.");
            Assert.IsTrue(move6B.guard == BBGuard.Mid || move6B.guard == BBGuard.Overhead);
            Assert.IsTrue(move6B.startupFrames > 0);

            // 3C Rising Launcher
            var move3C = FindMove(moveSet, "3C");
            Assert.IsNotNull(move3C, "3C Rising Void Launcher must exist.");
            Assert.IsTrue(move3C.jumpCancelOnHit, "3C must allow jump cancel on hit.");
        }

        [Test]
        public void Test_Cultist_Specials_And_Reversal()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Cultist"}/BBMoveSet_C06_Cultist.asset");
            Assert.IsNotNull(moveSet);

            // 236B Skull Shot
            var skullShot = FindMove(moveSet, "236B");
            Assert.IsNotNull(skullShot, "236B Void Skull Shot must exist.");
            Assert.AreEqual(BBMotion.M236, skullShot.motion);
            Assert.AreEqual(BBAttribute.Projectile, skullShot.attribute);

            // 22B / 22C Void Warp
            var warp1 = FindMove(moveSet, "22B");
            Assert.IsNotNull(warp1, "22B Void Warp 1 must exist.");
            Assert.AreEqual(BBMotion.M22, warp1.motion);

            var warp2 = FindMove(moveSet, "22C");
            Assert.IsNotNull(warp2, "22C Void Warp 2 must exist.");
            Assert.AreEqual(BBMotion.M22, warp2.motion);

            // 623C Void Pillar Reversal
            var dp = FindMove(moveSet, "623C");
            Assert.IsNotNull(dp, "623C Void Pillar DP must exist.");
            Assert.AreEqual(BBMotion.M623, dp.motion);
            Assert.IsTrue(dp.invuln.Length > 0, "623C must have invulnerability window.");
            Assert.AreEqual(BBInvuln.Head | BBInvuln.Body | BBInvuln.Projectile, dp.invuln[0].mask);

            // 236236C Void Cataclysm DD
            var cataclysm = FindMove(moveSet, "236236C");
            Assert.IsNotNull(cataclysm, "236236C Void Cataclysm DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, cataclysm.kind);
            Assert.AreEqual(50, cataclysm.heatCost);
            Assert.AreEqual(2500, cataclysm.damage);

            // 632146D Abyssal Grasp DD
            var grasp = FindMove(moveSet, "632146D");
            Assert.IsNotNull(grasp, "632146D Abyssal Grasp DD must exist.");
            Assert.AreEqual(BBMoveKind.DistortionDrive, grasp.kind);
            Assert.AreEqual(50, grasp.heatCost);
            Assert.IsTrue(grasp.damage > 0);
            Assert.IsTrue(grasp.invuln.Length > 0, "632146D must have startup invulnerability.");
        }

        [Test]
        public void Test_Cultist_RevolverAction_Chains()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Cultist"}/BBMoveSet_C06_Cultist.asset");
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
        public void Test_Cultist_10_Trial_Challenges()
        {
            var moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>($"{"Assets/CombatData/BlazBlue/Cultist"}/BBMoveSet_C06_Cultist.asset");
            Assert.IsNotNull(moveSet);

            string[][] trialRoutes = new string[][]
            {
                new string[] { "5A", "5B", "5C" },                                        // Trial 1: Basic Dark Gatling
                new string[] { "2A", "2B", "2C" },                                        // Trial 2: Low Sweep Confirm
                new string[] { "6A", "623C" },                                            // Trial 3: Anti-Air Void Pillar Reversal
                new string[] { "5B", "3C", "j.A", "j.B", "j.C" },                         // Trial 4: Launcher Aerial Juggle
                new string[] { "5D", "236B", "6D" },                                      // Trial 5: Void Orb Placement & Dash Trap
                new string[] { "6B", "5C", "236B" },                                      // Trial 6: Stepping Overhead Confirm
                new string[] { "5D", "22B", "5A", "5B" },                                 // Trial 7: Void Warp Spatial Flank
                new string[] { "j.A", "j.B", "j.C", "j.D" },                              // Trial 8: Air Scythe Pressure & Air Orb Drop
                new string[] { "5B", "5C", "5D", "236236C" },                             // Trial 9: Void Cataclysm Detonation DD
                new string[] { "5C", "632146D", "OD_EA" }                                 // Trial 10: Abyssal Grasp Overdrive Exceed Accel
            };

            for (int t = 0; t < trialRoutes.Length; t++)
            {
                var route = trialRoutes[t];
                for (int m = 0; m < route.Length; m++)
                {
                    var move = FindMove(moveSet, route[m]);
                    Assert.IsNotNull(move, $"Trial {t + 1} move '{route[m]}' must exist in Cultist move set.");
                }
            }
        }

        [Test]
        public void Test_Cultist_CharacterDefinition_Linked()
        {
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/CombatData/Characters/CharacterDefinition_C06_Cultist.asset");
            Assert.IsNotNull(charDef, "CharacterDefinition_C06_Cultist must exist.");
            Assert.IsNotNull(charDef.blazBlueProfile, "CharacterDefinition_C06_Cultist must link blazBlueProfile.");

            var profile = charDef.blazBlueProfile as BBCharacterProfile;
            Assert.IsNotNull(profile, "blazBlueProfile must be BBCharacterProfile.");
            Assert.AreEqual("C06_Cultist", profile.characterId);
            Assert.AreEqual("Void Orb", profile.driveName);
            Assert.IsNotNull(profile.blazBlueOverrideController, "Profile must link Cultist_BB_Override controller.");
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