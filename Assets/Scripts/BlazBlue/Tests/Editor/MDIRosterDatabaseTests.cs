using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using DemonIdol.Combat;
using DemonIdol.Data;
using DemonIdol.BlazBlue.Editor;
using DemonIdol.BlazBlue;

namespace DemonIdol.BlazBlue.Tests
{
    public class MDIRosterDatabaseTests
    {
        [Test]
        public void Test01_BuildAndVerifyRosterDatabase()
        {
            var db = MDIRosterDatabaseBuilder.BuildDatabase();
            Assert.IsNotNull(db, "MDIRosterDatabase must not be null");
            Assert.AreEqual(25, db.characters.Length, "Must have exactly 25 roster characters");

            for (int i = 0; i < db.characters.Length; i++)
            {
                var def = db.characters[i];
                Assert.IsNotNull(def, $"Character at {i} must not be null");
                Assert.IsFalse(string.IsNullOrEmpty(def.id), $"Character at {i} must have valid id");
                Assert.IsNotNull(def.modelPrefab, $"{def.id} must have modelPrefab bound");
                Assert.IsNotNull(def.avatar, $"{def.id} must have avatar bound");
                Assert.IsNotNull(def.animatorController, $"{def.id} must have animatorController bound");
                Assert.IsNotNull(def.blazBlueProfile, $"{def.id} must have blazBlueProfile bound");

                var profile = def.blazBlueProfile as BBCharacterProfile;
                Assert.IsNotNull(profile, $"{def.id} blazBlueProfile must be BBCharacterProfile");
                var moveSet = profile.moveSet as BBMoveSet;
                Assert.IsNotNull(moveSet, $"{def.id} moveSet must not be null");
                Assert.GreaterOrEqual(moveSet.moves.Length, 7, $"{def.id} must have at least 7 moves");
            }

            // ST07 Stage
            Assert.IsNotNull(db.st07Stage, "ST07 StageDefinition must be bound");
            Assert.AreEqual("ST07", db.st07Stage.id, "ST07 ID must match");
            Assert.IsNotNull(db.st07Stage.stagePrefab, "ST07 stagePrefab must be bound");

            // Sparks & Controller
            Assert.IsNotNull(db.defaultAnimatorController, "defaultAnimatorController must be bound");
            Assert.IsNotNull(db.hitSparkLightPrefab, "hitSparkLightPrefab must be bound");
            Assert.IsNotNull(db.hitSparkHeavyPrefab, "hitSparkHeavyPrefab must be bound");
            Assert.IsNotNull(db.hitSparkSuperPrefab, "hitSparkSuperPrefab must be bound");
            Assert.IsNotNull(db.guardSparkPrefab, "guardSparkPrefab must be bound");

            // Verify Resources.Load works
            var loadedFromResources = Resources.Load<MDIRosterDatabase>("MDI_RosterDatabase");
            Assert.IsNotNull(loadedFromResources, "Must be loadable via Resources.Load<MDIRosterDatabase>('MDI_RosterDatabase')");
            Assert.AreEqual(25, loadedFromResources.characters.Length);

            // Verify CharacterRegistry loads all 25
            CharacterRegistry.ReloadRegistry();
            Assert.AreEqual(25, CharacterRegistry.GetAllCharacters().Count, "CharacterRegistry must resolve all 25 characters");

            // Verify StageRegistry loads ST07
            StageRegistry.ReloadRegistry();
            var st07Loaded = StageRegistry.GetStageById("ST07");
            Assert.IsNotNull(st07Loaded, "StageRegistry must resolve ST07 from MDIRosterDatabase");
        }

        [Test]
        public void Test02_FixAndVerifyAllThrowMoveAssets()
        {
            BBThrowDataFixer.FixAllThrows();

            string[] guids = AssetDatabase.FindAssets("t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
            int checkedThrows = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("_BC.asset", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("_BC.asset") ||
                    path.Contains("BC.asset"))
                {
                    var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                    if (attack != null && (attack.moveId == "BC" || attack.name.EndsWith("_BC") || attack.attackName.Contains("Throw")))
                    {
                        Assert.AreEqual(7, attack.startupFrames, $"{attack.name} startup must be 7f");
                        Assert.AreEqual(3, attack.activeFrames, $"{attack.name} active must be 3f");
                        Assert.AreEqual(23, attack.recoveryFrames, $"{attack.name} recovery must be 23f");
                        Assert.AreEqual(1300f, attack.damage, $"{attack.name} damage must be 1300");
                        Assert.AreEqual(1.0f, attack.minDamagePct, $"{attack.name} minDamagePct must be 100%");
                        checkedThrows++;
                    }
                }
            }
            Assert.Greater(checkedThrows, 10, "Must have checked and verified at least 10 throw move assets");
        }
    }
}
