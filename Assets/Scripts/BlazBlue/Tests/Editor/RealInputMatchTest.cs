#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.BlazBlue.Tests
{
    public class RealInputMatchTest : InputTestFixture
    {
        private Keyboard _virtualKeyboard;
        private Gamepad _virtualGamepad;

        public override void Setup()
        {
            base.Setup();
            _virtualKeyboard = InputSystem.AddDevice<Keyboard>();
            _virtualGamepad = InputSystem.AddDevice<Gamepad>();
        }

        public override void TearDown()
        {
            base.TearDown();
            PlayerPrefs.DeleteKey("MDI_P1_CharacterId");
            PlayerPrefs.DeleteKey("MDI_P2_CharacterId");
            PlayerPrefs.DeleteKey("MDI_StageId");
            PlayerPrefs.DeleteKey("MDI_GameMode");
            LaunchServices.ResetForTests();
        }

        [Test]
        public void RealInput_Versus_P1KeyboardJ_And_P2GamepadSouth_TriggerAttacks()
        {
            // 1. Configure Versus mode C07 vs C01
            PlayerPrefs.SetString("MDI_P1_CharacterId", "C07");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "C01");
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "Versus");
            LaunchServices.ResetForTests();

            // Load BattleArena scene
            string scenePath = "Assets/Scenes/BattleArena.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "BattleArena scene must be valid");

            var director = UnityEngine.Object.FindAnyObjectByType<HomegrownCombatDirector>();
            Assert.IsNotNull(director, "HomegrownCombatDirector must exist in BattleArena");

            director.InitializeHomegrownMatch();

            var p1 = director.Fighter1;
            var p2 = director.Fighter2;
            Assert.IsNotNull(p1, "Player 1 fighter must exist");
            Assert.IsNotNull(p2, "Player 2 fighter must exist");

            var p1Brain = p1.GetComponent<BBFighterBrain>();
            var p2Brain = p2.GetComponent<BBFighterBrain>();
            var p1Sampler = p1.GetComponent<BBInputSampler>();
            var p2Sampler = p2.GetComponent<BBInputSampler>();

            Assert.IsNotNull(p1Brain, "P1 must have BBFighterBrain");
            Assert.IsNotNull(p2Brain, "P2 must have BBFighterBrain");
            Assert.IsNotNull(p1Sampler, "P1 must have BBInputSampler");
            Assert.IsNotNull(p2Sampler, "P2 must have BBInputSampler");

            var sim = BBMatchSim.Instance;
            Assert.IsNotNull(sim, "BBMatchSim must exist");

            // 2. Press J on virtual keyboard for P1 (5A attack)
            Press(_virtualKeyboard.jKey);

            // Sample and tick
            sim.Tick(1);

            // Assert P1 enters attack state
            bool p1Attacking = p1Brain.bbState == BBState.AttackStartup ||
                               p1Brain.bbState == BBState.AttackActive ||
                               p1Brain.bbState == BBState.AttackRecovery;

            Assert.IsTrue(p1Attacking, $"P1 must enter attack state after pressing keyboard J. Current state: {p1Brain.bbState}");
            Assert.IsNotNull(p1Brain.currentAttack, "P1 currentAttack must not be null");
            Assert.AreEqual("5A", p1Brain.currentAttack.moveId, "P1 should have started 5A move");

            Release(_virtualKeyboard.jKey);

            // 3. Press South on virtual gamepad for P2 (5A attack)
            Press(_virtualGamepad.buttonSouth);

            sim.Tick(2);

            bool p2Attacking = p2Brain.bbState == BBState.AttackStartup ||
                               p2Brain.bbState == BBState.AttackActive ||
                               p2Brain.bbState == BBState.AttackRecovery;

            Assert.IsTrue(p2Attacking, $"P2 must enter attack state after pressing gamepad South. Current state: {p2Brain.bbState}");
            Assert.IsNotNull(p2Brain.currentAttack, "P2 currentAttack must not be null");

            Release(_virtualGamepad.buttonSouth);
            Debug.Log("<color=#00e676>[RealInputMatchTest] PASS: Keyboard J and Gamepad South successfully trigger attacks via BBInputSampler!</color>");
        }

        [Test]
        public void RealInput_Arcade_CpuAttacksWithin5Seconds_And_NotMoreThanOncePer10Frames()
        {
            // Configure Arcade mode C07 vs C01
            PlayerPrefs.SetString("MDI_P1_CharacterId", "C07");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "C01");
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "Arcade");
            LaunchServices.ResetForTests();

            string scenePath = "Assets/Scenes/BattleArena.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var director = UnityEngine.Object.FindAnyObjectByType<HomegrownCombatDirector>();
            Assert.IsNotNull(director);
            director.InitializeHomegrownMatch();

            var p2 = director.Fighter2;
            Assert.IsNotNull(p2);
            var p2Brain = p2.GetComponent<BBFighterBrain>();
            var p2Ai = p2.GetComponent<BBAIController>();
            Assert.IsNotNull(p2Brain);
            Assert.IsNotNull(p2Ai, "CPU fighter must have BBAIController in Arcade mode");

            var sim = BBMatchSim.Instance;
            Assert.IsNotNull(sim);

            int attackCount = 0;
            int lastAttackTick = -100;
            int minSpacing = int.MaxValue;

            // Simulate up to 300 ticks (5 seconds @ 60 Hz)
            for (int tick = 1; tick <= 300; tick++)
            {
                sim.Tick(tick);

                if ((p2Brain.bbState == BBState.AttackStartup || p2Brain.bbState == BBState.AttackActive) && p2Brain.stateTick <= 1)
                {
                    attackCount++;
                    int spacing = tick - lastAttackTick;
                    if (spacing < minSpacing) minSpacing = spacing;
                    lastAttackTick = tick;
                }
            }

            Assert.GreaterOrEqual(attackCount, 1, "CPU must start at least 1 attack within 5 seconds (300 ticks)");
            Assert.GreaterOrEqual(minSpacing, 10, $"CPU must not attack more than once per 10 frames. Minimum spacing observed: {minSpacing} frames");

            Debug.Log($"<color=#00e676>[RealInputMatchTest] PASS: CPU attacked {attackCount} times within 300 ticks with minimum spacing of {minSpacing} frames!</color>");
        }

        public static void RunAllTests()
        {
            var test = new RealInputMatchTest();
            test.Setup();
            try
            {
                test.RealInput_Versus_P1KeyboardJ_And_P2GamepadSouth_TriggerAttacks();
                test.RealInput_Arcade_CpuAttacksWithin5Seconds_And_NotMoreThanOncePer10Frames();
            }
            finally
            {
                test.TearDown();
            }
        }
    }
}
#endif
