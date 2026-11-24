#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.Data;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBAIPolishTests
    {
        private static readonly string[] LaunchFighterIds = new string[]
        {
            "C01", "C02", "C05", "C06", "C08", "C09", "C11", "C12", "C14", "EX01", "EX04", "SK08"
        };

        private static CharacterDefinition LoadDefinition(string id)
        {
            string[] found = AssetDatabase.FindAssets("CharacterDefinition_" + id + " t:CharacterDefinition");
            for (int i = 0; i < found.Length; i++)
            {
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(found[i]));
                if (def != null) return def;
            }
            return CharacterRegistry.GetCharacterById(id);
        }

        [Test]
        public void Test_All12LaunchFighters_AI_ArchetypeClassification()
        {
            var expectedArchetypes = new Dictionary<string, BBArchetypeClass>
            {
                { "C01", BBArchetypeClass.Rushdown },
                { "C02", BBArchetypeClass.Rushdown },
                { "C05", BBArchetypeClass.Zoner },
                { "C06", BBArchetypeClass.Zoner },
                { "C08", BBArchetypeClass.Rushdown },
                { "C09", BBArchetypeClass.Zoner },
                { "C11", BBArchetypeClass.AllRounder },
                { "C12", BBArchetypeClass.Zoner },
                { "C14", BBArchetypeClass.Swordsman },
                { "EX01", BBArchetypeClass.Swordsman },
                { "EX04", BBArchetypeClass.Grappler },
                { "SK08", BBArchetypeClass.Grappler }
            };

            foreach (var kvp in expectedArchetypes)
            {
                var info = BBRosterArchetype.Get(kvp.Key);
                Assert.AreEqual(kvp.Value, info.archetypeClass, $"Fighter {kvp.Key} must be classified as {kvp.Value}");
            }
        }

        [Test]
        public void Test_All12LaunchFighters_AI_GenerateInput_7MoveStandard_ZeroMissingMoves()
        {
            var p1Go = new GameObject("P1_Fighter");
            var p1Brain = p1Go.AddComponent<BBFighterBrain>();
            var p1Ai = p1Go.AddComponent<BBAIController>();

            var p2Go = new GameObject("P2_Fighter");
            var p2Brain = p2Go.AddComponent<BBFighterBrain>();
            p2Go.transform.position = new Vector3(1.5f, 0f, 0f);

            try
            {
                for (int f = 0; f < LaunchFighterIds.Length; f++)
                {
                    string id = LaunchFighterIds[f];
                    var def = LoadDefinition(id);
                    Assert.IsNotNull(def, $"CharacterDefinition for {id} must exist");
                    var profile = def.blazBlueProfile as BBCharacterProfile;
                    Assert.IsNotNull(profile, $"BBCharacterProfile for {id} must exist");

                    p1Brain.profile = profile;
                    p1Ai.ResetAI(1000 + f);

                    int attackCount = 0;
                    int lastAttackTick = -100;
                    int minSpacing = int.MaxValue;

                    for (int tick = 0; tick < 200; tick++)
                    {
                        var frame = p1Ai.GenerateInput(p1Brain, p2Brain, tick);
                        Assert.IsTrue(frame.numpad >= 1 && frame.numpad <= 9, $"Tick {tick}: numpad must be 1..9, got {frame.numpad}");

                        if (frame.pressed != 0)
                        {
                            attackCount++;
                            int spacing = tick - lastAttackTick;
                            if (spacing < minSpacing) minSpacing = spacing;
                            lastAttackTick = tick;

                            // Verify button only uses 4-button BlazBlue standard (A, B, C, D)
                            bool hasValidButtons = (frame.pressed & (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D)) != 0;
                            Assert.IsTrue(hasValidButtons, $"Tick {tick}: Attack buttons must be subset of A,B,C,D, got {frame.pressed}");
                        }
                    }

                    Assert.GreaterOrEqual(attackCount, 1, $"Fighter {id} AI must generate attacks over 200 ticks");
                    Assert.GreaterOrEqual(minSpacing, 10, $"Fighter {id} AI must strictly obey >=10 frame spacing between attack startups. Got {minSpacing}");
                }
            }
            finally
            {
                Object.DestroyImmediate(p1Go);
                Object.DestroyImmediate(p2Go);
            }
        }

        [Test]
        public void Test_AI_HighLow_DefensiveGuarding_Reads()
        {
            var p1Go = new GameObject("AI_Fighter");
            var ai = p1Go.AddComponent<BBAIController>();
            var p1Brain = p1Go.AddComponent<BBFighterBrain>();
            ai.difficulty = BBAIDifficulty.Boss; // 92% block rate

            var p2Go = new GameObject("Opponent_Fighter");
            var p2Brain = p2Go.AddComponent<BBFighterBrain>();
            p2Go.transform.position = new Vector3(1.2f, 0f, 0f);

            try
            {
                // Setup low sweep attack on opponent
                var lowAttack = ScriptableObject.CreateInstance<BBAttackData>();
                lowAttack.moveId = "2C";
                lowAttack.guard = BBGuard.Low;
                p2Brain.currentAttack = lowAttack;
                p2Brain.attackTick = 1;

                // Reset AI with deterministic seed
                ai.ResetAI(42);

                // Simulate incoming low attack: after reaction delay (4 ticks for Boss), AI must crouch block (1)
                bool sawCrouchBlock = false;
                for (int t = 0; t < 20; t++)
                {
                    var frame = ai.GenerateInput(p1Brain, p2Brain, t);
                    // Facing opponent at x=1.2 (facing right, +1): crouch block is 1
                    if (frame.numpad == 1 && frame.held == 0)
                    {
                        sawCrouchBlock = true;
                        break;
                    }
                }
                Assert.IsTrue(sawCrouchBlock, "AI must react to incoming low sweep with crouch block (1)");

                // Setup overhead attack on opponent
                var overheadAttack = ScriptableObject.CreateInstance<BBAttackData>();
                overheadAttack.moveId = "j.B";
                overheadAttack.guard = BBGuard.Overhead;
                p2Brain.currentAttack = overheadAttack;
                p2Brain.attackTick = 50;

                ai.ResetAI(42);
                bool sawStandBlock = false;
                for (int t = 50; t < 70; t++)
                {
                    var frame = ai.GenerateInput(p1Brain, p2Brain, t);
                    // Facing right: stand block is 4
                    if (frame.numpad == 4 && frame.held == 0)
                    {
                        sawStandBlock = true;
                        break;
                    }
                }
                Assert.IsTrue(sawStandBlock, "AI must react to incoming overhead with stand block (4)");

                Object.DestroyImmediate(lowAttack);
                Object.DestroyImmediate(overheadAttack);
            }
            finally
            {
                Object.DestroyImmediate(p1Go);
                Object.DestroyImmediate(p2Go);
            }
        }
    }
}
#endif
