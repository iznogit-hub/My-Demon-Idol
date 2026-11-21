#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.BlazBlue.Tests
{
    public class ArcadeArenaSceneIntegrityTest
    {
        const string SceneRelative = "Scenes/ArcadeArena.unity";

        [TearDown]
        public void TearDown()
        {
            LaunchServices.ResetForTests();
            PlayerPrefs.DeleteKey("MDI_P1_CharacterId");
            PlayerPrefs.DeleteKey("MDI_P2_CharacterId");
            PlayerPrefs.DeleteKey("MDI_StageId");
            PlayerPrefs.DeleteKey("MDI_GameMode");
        }

        [Test]
        public void ArcadeArena_MatchLoopObjectsArePresentAndEnabled()
        {
            string yaml = File.ReadAllText(Path.Combine(Application.dataPath, SceneRelative));

            AssertScriptEnabled(yaml, "51682106a16d249b794275978a434f21", "HomegrownCombatDirector");
            AssertScriptEnabled(yaml, "661dfb35443d94aa5b68c6333d760fea", "CombatTickEngine");
            AssertScriptEnabled(yaml, "7666ab0c2fea34bc8aa251c3dedb53e3", "MatchRoundReferee");
            AssertScriptEnabled(yaml, "1a2a90a7753a9473a8c2f4b1bba2a717", "BBMatchBootstrap");
            AssertScriptEnabled(yaml, "76c392e42b5098c458856cdf6ecaaaa1", "EventSystem");
            AssertNamedActive(yaml, "EventSystem");
            AssertNamedActive(yaml, "Battle_HUD_Canvas");
            AssertBuiltInComponentEnabled(yaml, "Battle_HUD_Canvas", "223");

            string director = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Combat/HomegrownCombatDirector.cs"));
            StringAssert.Contains("BBMatchBootstrap.EnsureMatch", director);
        }

        [Test]
        public void VerificationTools_DoNotSaveShippedScenes()
        {
            string verifier = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/BlazBlue/Editor/BBPlayModeConsolidationVerifier.cs"));
            string fixPass = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Editor/DemonIdolArcadeArenaFixPass.cs"));
            Assert.IsFalse(verifier.Contains("SaveScene"), "BBPlayModeConsolidationVerifier must not save a scene");
            Assert.IsFalse(verifier.Contains("MarkSceneDirty"), "BBPlayModeConsolidationVerifier must not dirty a scene");
            Assert.IsFalse(fixPass.Contains("SaveScene"), "Arcade arena fix pass must not save a scene");
            Assert.IsFalse(fixPass.Contains("MarkSceneDirty"), "Arcade arena fix pass must not dirty a scene");
            StringAssert.Contains("ShippedSceneGuard.DiscardEdits", verifier);
            StringAssert.Contains("ShippedSceneGuard.DiscardEdits", fixPass);
        }

        [Test]
        public void PlayerPrefsPick_IsTheRosterThatSpawns()
        {
            LaunchServices.ResetForTests();
            PlayerPrefs.SetString("MDI_P1_CharacterId", "SK02");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "C12");
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");

            HomegrownCombatDirector.MatchLaunchRoster roster = HomegrownCombatDirector.ResolveLaunchRoster();
            Assert.AreEqual("SK02", roster.P1Id);
            Assert.AreEqual("C12", roster.P2Id);
            Assert.AreEqual("ST07", roster.StageId);
            Assert.IsFalse(roster.P2IsCpu);

            GameObject host = new GameObject("roster-spawn-test");
            host.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                FighterController fighter = host.AddComponent<FighterController>();
                GameObject stale = new GameObject("Model_C01");
                stale.hideFlags = HideFlags.HideAndDontSave;
                stale.transform.SetParent(host.transform, false);
                stale.AddComponent<MeshRenderer>();

                HomegrownCombatDirector.ConfigureFighter(fighter, roster.P1Id, true);

                Assert.IsNotNull(fighter.appliedDefinition);
                Assert.AreEqual("SK02", fighter.appliedDefinition.id);
                Assert.IsNull(host.transform.Find("Model_C01"));
                Transform spawned = host.transform.Find("Model_SK02");
                Assert.IsNotNull(spawned, "Picked fighter should replace the baked Akio model");
                foreach (Transform child in host.GetComponentsInChildren<Transform>(true))
                    child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ArcadeBeginMatch_KeepsThePickedOpponent()
        {
            LaunchServices.ResetForTests();
            Assert.IsTrue(LaunchServices.Session.TrySelectMode("Arcade", out _));
            LaunchServices.Session.BeginMatch("C07", "EX01", "ST07");

            HomegrownCombatDirector.MatchLaunchRoster roster = HomegrownCombatDirector.ResolveLaunchRoster();
            Assert.AreEqual("C07", roster.P1Id);
            Assert.AreEqual("EX01", roster.P2Id);
            Assert.IsTrue(roster.P2IsCpu);
            Assert.AreEqual("EX01", LaunchServices.Session.Player2Id);
        }

        [Test]
        public void FreshSessionDefaults_DoNotOverridePlayerPrefs()
        {
            LaunchServices.ResetForTests();
            Assert.IsTrue(string.IsNullOrEmpty(LaunchServices.Session.MatchId));
            PlayerPrefs.SetString("MDI_P1_CharacterId", "C07");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "EX01");
            PlayerPrefs.SetString("MDI_GameMode", "Arcade");

            HomegrownCombatDirector.MatchLaunchRoster roster = HomegrownCombatDirector.ResolveLaunchRoster();
            Assert.AreEqual("C07", roster.P1Id);
            Assert.AreEqual("EX01", roster.P2Id);
            Assert.AreNotEqual("C01", roster.P1Id);
        }

        [Test]
        public void PlayerIndex_FollowsDirectorSlots()
        {
            var p1 = new GameObject("named-like-p2").AddComponent<FighterController>();
            var p2 = new GameObject("named-like-p1").AddComponent<FighterController>();
            p1.gameObject.hideFlags = HideFlags.HideAndDontSave;
            p2.gameObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Assert.AreEqual(0, BBMatchBootstrap.ResolvePlayerIndex(p1, p1, p2));
                Assert.AreEqual(1, BBMatchBootstrap.ResolvePlayerIndex(p2, p1, p2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(p1.gameObject);
                UnityEngine.Object.DestroyImmediate(p2.gameObject);
            }
        }

        [Test]
        public void TouchControls_ShowOnMobileSimulatorOrTouchHardware()
        {
            Assert.IsTrue(BBTouchLayout.ShouldShowTouchControls(true, false, false));
            Assert.IsTrue(BBTouchLayout.ShouldShowTouchControls(false, true, false));
            Assert.IsTrue(BBTouchLayout.ShouldShowTouchControls(false, false, true));
            Assert.IsFalse(BBTouchLayout.ShouldShowTouchControls(false, false, false));
        }

        static void AssertScriptEnabled(string yaml, string guid, string label)
        {
            string needle = "guid: " + guid;
            int at = yaml.IndexOf(needle, StringComparison.Ordinal);
            Assert.IsTrue(at >= 0, label + " is missing from ArcadeArena.unity");
            int enabled = yaml.LastIndexOf("m_Enabled:", at, StringComparison.Ordinal);
            Assert.IsTrue(enabled >= 0 && at - enabled < 400, label + " has no enabled flag");
            string window = yaml.Substring(enabled, Math.Min(24, yaml.Length - enabled));
            Assert.IsTrue(window.StartsWith("m_Enabled: 1", StringComparison.Ordinal), label + " is disabled: " + window);
        }

        static void AssertNamedActive(string yaml, string objectName)
        {
            int at = IndexOfName(yaml, objectName);
            Assert.IsTrue(at >= 0, objectName + " is missing from ArcadeArena.unity");
            int active = yaml.IndexOf("m_IsActive:", at, StringComparison.Ordinal);
            Assert.IsTrue(active >= 0 && active - at < 280, objectName + " has no active flag");
            string window = yaml.Substring(active, Math.Min(20, yaml.Length - active));
            Assert.IsTrue(window.StartsWith("m_IsActive: 1", StringComparison.Ordinal), objectName + " is inactive: " + window);
        }

        static void AssertBuiltInComponentEnabled(string yaml, string objectName, string unityClassId)
        {
            int nameAt = IndexOfName(yaml, objectName);
            int header = yaml.LastIndexOf("--- !u!1 &", nameAt, StringComparison.Ordinal);
            Assert.IsTrue(header >= 0, objectName + " GameObject header missing");
            Match idMatch = Regex.Match(yaml.Substring(header, 40), @"--- !u!1 &(\d+)");
            Assert.IsTrue(idMatch.Success, objectName + " fileID missing");
            string fileId = idMatch.Groups[1].Value;
            string classHeader = "--- !u!" + unityClassId + " ";
            int search = 0;
            bool found = false;
            while (search < yaml.Length)
            {
                int block = yaml.IndexOf(classHeader, search, StringComparison.Ordinal);
                if (block < 0) break;
                int next = yaml.IndexOf("\n--- ", block + 4, StringComparison.Ordinal);
                if (next < 0) next = yaml.Length;
                string body = yaml.Substring(block, next - block);
                if (body.Contains("m_GameObject: {fileID: " + fileId + "}"))
                {
                    Assert.IsTrue(body.Contains("m_Enabled: 1"), objectName + " component " + unityClassId + " is disabled");
                    found = true;
                    break;
                }
                search = next;
            }
            Assert.IsTrue(found, objectName + " has no component " + unityClassId);
        }

        static int IndexOfName(string yaml, string objectName)
        {
            int at = yaml.IndexOf("m_Name: " + objectName + "\n", StringComparison.Ordinal);
            if (at < 0)
                at = yaml.IndexOf("m_Name: " + objectName + "\r", StringComparison.Ordinal);
            return at;
        }
    }
}
#endif
