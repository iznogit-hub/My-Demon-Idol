#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Tests
{
    public class BBRosterLaunchTests
    {
        static readonly string[] Ids =
        {
            "C01", "C02", "C04", "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12", "C13", "C14",
            "SK02", "SK03", "SK05", "SK06", "SK08",
            "EX01", "EX02", "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        [Test]
        public void EveryPlayableIdHasProfileMovesAndKiOverride()
        {
            Assert.AreEqual(25, Ids.Length);
            for (int i = 0; i < Ids.Length; i++)
            {
                var def = LoadDefinition(Ids[i]);
                Assert.IsNotNull(def, Ids[i] + " definition");
                Assert.Greater(def.speed, 1f, Ids[i] + " speed");
                var profile = def.blazBlueProfile as BBCharacterProfile;
                Assert.IsNotNull(profile, Ids[i] + " profile");
                Assert.Greater(profile.maxHealth, 1000f, Ids[i] + " health");
                var moves = profile.moveSet as BBMoveSet;
                Assert.IsNotNull(moves, Ids[i] + " moves");
                Assert.Greater(moves.moves.Length, 15, Ids[i] + " move count");
                Assert.IsNotNull(profile.blazBlueOverrideController, Ids[i] + " override");
                string parent = AssetDatabase.GetAssetPath(profile.blazBlueOverrideController.runtimeAnimatorController);
                Assert.IsTrue(parent.EndsWith("MDI_KI_Melee_Fighter.controller"), Ids[i] + " parent " + parent);
                for (int m = 0; m < moves.moves.Length; m++)
                {
                    var move = moves.moves[m];
                    Assert.IsNotNull(move, Ids[i] + " null move");
                    Assert.IsNotNull(move.boxes, move.moveId + " box track");
                    Assert.AreEqual(move.TotalTicks, move.boxes.tickCount, move.moveId + " tick count");
                    Assert.AreEqual(move.boxes.tickCount + 1, move.boxes.frameStart.Length, move.moveId + " frameStart");
                }
            }
        }

        [Test]
        public void APressWithProfileStartsAttack()
        {
            var attack = ScriptableObject.CreateInstance<BBAttackData>();
            attack.moveId = "5A";
            attack.groundOk = true;
            attack.airOk = false;
            attack.startupFrames = 5;
            attack.activeFrames = 3;
            attack.recoveryFrames = 8;
            var moves = ScriptableObject.CreateInstance<BBMoveSet>();
            moves.moves = new[] { attack };
            var profile = ScriptableObject.CreateInstance<BBCharacterProfile>();
            profile.moveSet = moves;
            var go = new GameObject("BBRosterLaunch_A");
            var fighter = go.AddComponent<FighterController>();
            var brain = go.AddComponent<BBFighterBrain>();
            brain.fighter = fighter;
            brain.profile = profile;
            brain.isGrounded = true;
            brain.StartAttack(attack);
            Assert.AreEqual("5A", brain.currentAttack.moveId);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AuthoritativeDamageReducesBothHealthReadouts()
        {
            var go = new GameObject("BBRosterLaunch_HP");
            var fighter = go.AddComponent<FighterController>();
            var brain = go.AddComponent<BBFighterBrain>();
            brain.fighter = fighter;
            brain.maxHealth = 10000f;
            brain.SetAuthoritativeHealth(10000f);
            brain.ReceiveAuthoritativeDamage(300f);
            Assert.AreEqual(9700f, brain.health, 0.1f);
            Assert.AreEqual(brain.health, fighter.currentHealth, 0.1f);
            Assert.AreEqual(brain.maxHealth, fighter.maxHealth, 0.1f);
            Object.DestroyImmediate(go);
        }

        static CharacterDefinition LoadDefinition(string id)
        {
            string[] found = AssetDatabase.FindAssets("CharacterDefinition_" + id + " t:CharacterDefinition");
            for (int i = 0; i < found.Length; i++)
            {
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(found[i]));
                if (def != null && def.id == id) return def;
            }
            return null;
        }
    }
}
#endif
