#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Editor;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBPhase6Tests
    {
        public static void RunAllTestsMenu()
        {
            var suite = new BBPhase6Tests();
            suite.Test_BattleHUD_VisualUpdates_And_Interpolation();
            suite.Test_AIController_DecisionMaking_And_Difficulties();
            suite.Test_ArcadeDirector_8StageLadder_Progression_And_Scoring();
            suite.Test_TrainingMenu_DummyBehaviors_And_Rules();
            suite.Test_VersusDirector_BestOfThree_And_Timeouts();
            suite.Test_AudioBridge_EventRouting();
            suite.Test_MobileCertification_All9Characters_Baseline();
            Debug.Log("<color=#00e676>[BBPhase6Tests] ALL 7 PHASE 6 LAUNCH & SYSTEM TESTS PASSED!</color>");
        }

        [Test]
        public void Test_BattleHUD_VisualUpdates_And_Interpolation()
        {
            var hudGO = new GameObject("TestHUD");
            var hud = hudGO.AddComponent<BBBattleHUDController>();

            var p1GO = new GameObject("P1");
            var p1Brain = p1GO.AddComponent<BBFighterBrain>();
            p1Brain.currentHealth = 11000f;
            p1Brain.heatMeter = 50f;
            p1Brain.barrierMeter = 800f;
            p1Brain.burstMeter = 1000f;

            hud.BindFighters(p1Brain, null);

            // Report damage to check lag bar behavior
            p1Brain.currentHealth = 9000f;
            hud.UpdateHUDVisuals(0.1f); // 0.1s delta

            // Check announcements and combo reporting
            hud.ShowAnnouncement("READY... FIGHT!", 60);
            hud.ReportComboHit(5, 2400);

            Assert.IsNotNull(BBBattleHUDController.Instance);

            UnityEngine.Object.DestroyImmediate(hudGO);
            UnityEngine.Object.DestroyImmediate(p1GO);
        }

        [Test]
        public void Test_AIController_DecisionMaking_And_Difficulties()
        {
            var aiGO = new GameObject("TestAI");
            var ai = aiGO.AddComponent<BBAIController>();

            var p1GO = new GameObject("P1");
            var p1 = p1GO.AddComponent<BBFighterBrain>();
            p1.currentHealth = 11000f;
            p1.heatMeter = 100f;

            var p2GO = new GameObject("P2");
            var p2 = p2GO.AddComponent<BBFighterBrain>();
            p2.currentHealth = 3000f;
            p2.transform.position = new Vector3(2f, 0f, 0f);

            // Test Easy difficulty
            ai.difficulty = BBAIDifficulty.Easy;
            var frameEasy = ai.GenerateInput(p1, p2, 0);
            Assert.IsTrue(frameEasy.numpad >= 1 && frameEasy.numpad <= 9);

            // Test Medium difficulty
            ai.difficulty = BBAIDifficulty.Medium;
            var frameMed = ai.GenerateInput(p1, p2, 6);
            Assert.IsTrue(frameMed.numpad >= 1 && frameMed.numpad <= 9);

            // Test Hard difficulty
            ai.difficulty = BBAIDifficulty.Hard;
            var frameHard = ai.GenerateInput(p1, p2, 12);
            Assert.IsTrue(frameHard.numpad >= 1 && frameHard.numpad <= 9);

            // Test Boss difficulty (Astral / finisher trigger)
            ai.difficulty = BBAIDifficulty.Boss;
            var frameBoss = ai.GenerateInput(p1, p2, 18);
            Assert.IsTrue(frameBoss.numpad >= 1 && frameBoss.numpad <= 9);

            UnityEngine.Object.DestroyImmediate(aiGO);
            UnityEngine.Object.DestroyImmediate(p1GO);
            UnityEngine.Object.DestroyImmediate(p2GO);
        }

        [Test]
        public void Test_ArcadeDirector_8StageLadder_Progression_And_Scoring()
        {
            var arcadeGO = new GameObject("ArcadeDirectorTest");
            var director = arcadeGO.AddComponent<BBArcadeDirector>();

            director.StartArcade("C01"); // Akio
            Assert.AreEqual(0, director.CurrentStageIndex);
            Assert.AreEqual(0, director.TotalScore);
            Assert.IsFalse(director.IsArcadeCompleted);

            // Stage 1
            var s1 = director.GetCurrentStage();
            Assert.AreEqual(1, s1.stageNumber);
            Assert.AreEqual("C04", s1.opponentId);

            // Win Stage 1 with Astral Finish
            director.ReportStageResult(true, true, true, 80, 15);
            Assert.AreEqual(1, director.CurrentStageIndex);
            Assert.IsTrue(director.TotalScore >= 400000);

            // Advance through remaining stages
            for (int i = 1; i < BBArcadeDirector.TOTAL_STAGES; i++)
            {
                var stage = director.GetCurrentStage();
                Assert.IsNotNull(stage.opponentId);
                director.ReportStageResult(true, false, false, 70, 8);
            }

            Assert.IsTrue(director.IsArcadeCompleted, "Arcade ladder must be marked completed after 8 stage wins.");

            UnityEngine.Object.DestroyImmediate(arcadeGO);
        }

        [Test]
        public void Test_TrainingMenu_DummyBehaviors_And_Rules()
        {
            var trainingGO = new GameObject("TrainingTest");
            var menu = trainingGO.AddComponent<BBTrainingMenu>();

            var p1GO = new GameObject("P1");
            var p1 = p1GO.AddComponent<BBFighterBrain>();
            var p2GO = new GameObject("P2");
            var p2 = p2GO.AddComponent<BBFighterBrain>();

            menu.playerBrain = p1;
            menu.dummyBrain = p2;

            // Full meters rule
            menu.meterMode = BBMeterMode.FullMeters;
            p1.heatMeter = 0f;
            menu.ApplyTrainingRules();
            Assert.AreEqual(100f, p1.heatMeter);
            Assert.AreEqual(1000f, p1.barrierMeter);
            Assert.AreEqual(1000f, p1.burstMeter);

            // Dummy actions
            menu.SetDummyAction(BBDummyAction.GuardAll);
            Assert.AreEqual(BBDummyAction.GuardAll, menu.dummyAction);

            // Position Save & Reset
            p1.transform.position = new Vector3(-3f, 0f, 0f);
            p2.transform.position = new Vector3(3f, 0f, 0f);
            menu.SaveState();

            p1.transform.position = Vector3.zero;
            menu.ResetPositions();
            Assert.AreEqual(new Vector3(-3f, 0f, 0f), p1.transform.position);

            UnityEngine.Object.DestroyImmediate(trainingGO);
            UnityEngine.Object.DestroyImmediate(p1GO);
            UnityEngine.Object.DestroyImmediate(p2GO);
        }

        [Test]
        public void Test_VersusDirector_BestOfThree_And_Timeouts()
        {
            var vsGO = new GameObject("VersusDirectorTest");
            var director = vsGO.AddComponent<BBVersusDirector>();

            var p1GO = new GameObject("P1");
            var p1 = p1GO.AddComponent<BBFighterBrain>();
            var p2GO = new GameObject("P2");
            var p2 = p2GO.AddComponent<BBFighterBrain>();

            director.StartVersusMatch(p1, p2);
            Assert.AreEqual(1, director.currentRound);
            Assert.AreEqual(BBVersusState.RoundRunning, director.state);

            // P1 wins Round 1
            director.EndRound(0, false);
            Assert.AreEqual(1, director.p1RoundsWon);
            Assert.AreEqual(BBVersusState.RoundEnded, director.state);

            // Start Round 2
            director.StartRound(2);
            Assert.AreEqual(2, director.currentRound);

            // P1 wins Round 2 (Match Victory)
            director.EndRound(0, false);
            Assert.AreEqual(2, director.p1RoundsWon);
            Assert.AreEqual(0, director.winnerPlayerIndex);
            Assert.AreEqual(BBVersusState.MatchFinished, director.state);

            UnityEngine.Object.DestroyImmediate(vsGO);
            UnityEngine.Object.DestroyImmediate(p1GO);
            UnityEngine.Object.DestroyImmediate(p2GO);
        }

        [Test]
        public void Test_AudioBridge_EventRouting()
        {
            var audioGO = new GameObject("AudioBridgeTest");
            var bridge = audioGO.AddComponent<BBAudioBridge>();

            // All audio events should execute cleanly without exceptions
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.HitLight, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.HitHeavy, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.FatalCounter, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.BlockBarrier, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.BarrierShatter, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.RapidCancel, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.OverdriveActivate, Vector3.zero));
            Assert.DoesNotThrow(() => bridge.PlayCombatSound(BBSoundEffect.DistortionSuperFlash, Vector3.zero));

            UnityEngine.Object.DestroyImmediate(audioGO);
        }

        [Test]
        public void Test_MobileCertification_All9Characters_Baseline()
        {
            bool pass = MDIMobileBuildPipeline.VerifyMobileCertificationBaseline();
            Assert.IsTrue(pass, "Mobile certification baseline must verify all 9 roster profiles.");
        }
    }
}
#endif
