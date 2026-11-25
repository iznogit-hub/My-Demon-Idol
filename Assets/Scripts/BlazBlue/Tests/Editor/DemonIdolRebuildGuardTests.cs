#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    /// <summary>
    /// EditMode Guard Tests for Demon Idol Rebuild:
    /// 1. BattleArena has Match_Host with all 4 components enabled, no baked fighters,
    ///    no CombatAudioDirector, Battle_HUD_Canvas and EventSystem active.
    /// 2. MainMenu and CharacterSelect have exactly one EventSystem with InputSystemUIInputModule.
    /// 3. Build settings = exactly the 3 scenes in order (MainMenu, CharacterSelect, BattleArena).
    /// 4. No MenuItem outside DemonIdolMenu.cs starts with "Demon Idol/" or "MDI/".
    /// </summary>
    public class DemonIdolRebuildGuardTests
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string CharSelectScenePath = "Assets/Scenes/CharacterSelect.unity";
        private const string BattleArenaScenePath = "Assets/Scenes/BattleArena.unity";

        // Component GUIDs
        private const string GuidDirector = "51682106a16d249b794275978a434f21"; // HomegrownCombatDirector
        private const string GuidTickEngine = "661dfb35443d94aa5b68c6333d760fea"; // CombatTickEngine
        private const string GuidReferee = "7666ab0c2fea34bc8aa251c3dedb53e3"; // MatchRoundReferee
        private const string GuidBootstrap = "1a2a90a7753a9473a8c2f4b1bba2a717"; // BBMatchBootstrap
        private const string GuidAudioDirector = "e605fa62b21ee4bbab009ea82cf118d3"; // CombatAudioDirector
        private const string GuidInputSystemUI = "01614664b831546d2ae94a42149d80ac"; // InputSystemUIInputModule

        [Test]
        public void Guard_BattleArena_MatchHostAndComponents()
        {
            Assert.IsTrue(File.Exists(BattleArenaScenePath), $"BattleArena scene must exist at {BattleArenaScenePath}");
            string yaml = File.ReadAllText(BattleArenaScenePath);

            // 1. Match_Host exists and has all 4 combat components enabled
            StringAssert.Contains("m_Name: Match_Host", yaml, "Match_Host GameObject missing from BattleArena");
            AssertScriptEnabled(yaml, GuidDirector, "HomegrownCombatDirector");
            AssertScriptEnabled(yaml, GuidTickEngine, "CombatTickEngine");
            AssertScriptEnabled(yaml, GuidReferee, "MatchRoundReferee");
            AssertScriptEnabled(yaml, GuidBootstrap, "BBMatchBootstrap");

            // 2. NO CombatAudioDirector in BattleArena
            Assert.IsFalse(yaml.Contains("guid: " + GuidAudioDirector), "BattleArena must NOT contain CombatAudioDirector (it persists from MainMenu)");

            // 3. NO Baked Fighters (Fighter_P1_Akio, Fighter_P2_Aika, Model_C01, Model_C02)
            Assert.IsFalse(yaml.Contains("m_Name: Fighter_P1_Akio"), "Baked fighter Fighter_P1_Akio must not exist in BattleArena");
            Assert.IsFalse(yaml.Contains("m_Name: Fighter_P2_Aika"), "Baked fighter Fighter_P2_Aika must not exist in BattleArena");
            Assert.IsFalse(yaml.Contains("m_Name: Model_C01"), "Baked model Model_C01 must not exist in BattleArena");
            Assert.IsFalse(yaml.Contains("m_Name: Model_C02"), "Baked model Model_C02 must not exist in BattleArena");

            // 4. Battle_HUD_Canvas active and enabled
            AssertNamedActive(yaml, "Battle_HUD_Canvas");
            AssertBuiltInComponentEnabled(yaml, "Battle_HUD_Canvas", "223"); // Canvas component

            // 5. EventSystem active and enabled with InputSystemUIInputModule
            AssertNamedActive(yaml, "EventSystem");
            AssertScriptEnabled(yaml, GuidInputSystemUI, "InputSystemUIInputModule");
        }

        [Test]
        public void Guard_MainMenu_SingleEventSystemWithInputSystemUI()
        {
            Assert.IsTrue(File.Exists(MainMenuScenePath), $"MainMenu scene must exist at {MainMenuScenePath}");
            string yaml = File.ReadAllText(MainMenuScenePath);

            int esCount = CountRegexMatches(yaml, @"m_Name:\s*EventSystem\b");
            Assert.AreEqual(1, esCount, $"MainMenu must have exactly 1 EventSystem (found {esCount})");

            AssertNamedActive(yaml, "EventSystem");
            AssertScriptEnabled(yaml, GuidInputSystemUI, "InputSystemUIInputModule");
        }

        [Test]
        public void Guard_CharacterSelect_SingleEventSystemWithInputSystemUI()
        {
            Assert.IsTrue(File.Exists(CharSelectScenePath), $"CharacterSelect scene must exist at {CharSelectScenePath}");
            string yaml = File.ReadAllText(CharSelectScenePath);

            int esCount = CountRegexMatches(yaml, @"m_Name:\s*EventSystem\b");
            Assert.AreEqual(1, esCount, $"CharacterSelect must have exactly 1 EventSystem (found {esCount})");

            AssertNamedActive(yaml, "EventSystem");
            AssertScriptEnabled(yaml, GuidInputSystemUI, "InputSystemUIInputModule");
        }

        [Test]
        public void Guard_BuildSettings_ExactlyThreeScenesInOrder()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(3, scenes.Length, $"EditorBuildSettings must have exactly 3 scenes (found {scenes.Length})");

            Assert.AreEqual(MainMenuScenePath, scenes[0].path, "Scene 0 must be MainMenu.unity");
            Assert.IsTrue(scenes[0].enabled, "Scene 0 (MainMenu) must be enabled");

            Assert.AreEqual(CharSelectScenePath, scenes[1].path, "Scene 1 must be CharacterSelect.unity");
            Assert.IsTrue(scenes[1].enabled, "Scene 1 (CharacterSelect) must be enabled");

            Assert.AreEqual(BattleArenaScenePath, scenes[2].path, "Scene 2 must be BattleArena.unity");
            Assert.IsTrue(scenes[2].enabled, "Scene 2 (BattleArena) must be enabled");
        }

        [Test]
        public void Guard_NoMenuItemOutsideDemonIdolMenu()
        {
            string assetsDir = Application.dataPath;
            var csFiles = Directory.GetFiles(assetsDir, "*.cs", SearchOption.AllDirectories);
            var menuRegex = new Regex(@"\[MenuItem\s*\(\s*""(Demon Idol/|MDI/|DemonIdol/)", RegexOptions.Compiled);

            int violations = 0;
            string violationReport = "";

            foreach (var file in csFiles)
            {
                // Normalize path
                string rel = file.Replace('\\', '/');
                if (rel.EndsWith("Assets/Scripts/Editor/DemonIdolMenu.cs") || rel.EndsWith("Assets/Scripts/Editor/DemonIdolLaunchSync.cs"))
                    continue;
                if (rel.Contains("/Editor_Archive/"))
                    continue;

                string text = File.ReadAllText(file);
                var matches = menuRegex.Matches(text);
                if (matches.Count > 0)
                {
                    violations += matches.Count;
                    violationReport += $"\nFile: {rel} ({matches.Count} matches)";
                }
            }

            Assert.AreEqual(0, violations, $"Found unauthorized MenuItem definitions outside DemonIdolMenu.cs: {violationReport}");
        }

        public static void RunSceneIntegrityCheck()
        {
            Debug.Log("<color=#00e676>[DemonIdolGuardTests] RUNNING SCENE INTEGRITY AUDIT...</color>");
            var suite = new DemonIdolRebuildGuardTests();
            int passed = 0;
            int failed = 0;

            Action[] tests = new Action[]
            {
                suite.Guard_BattleArena_MatchHostAndComponents,
                suite.Guard_MainMenu_SingleEventSystemWithInputSystemUI,
                suite.Guard_CharacterSelect_SingleEventSystemWithInputSystemUI,
                suite.Guard_BuildSettings_ExactlyThreeScenesInOrder,
                suite.Guard_NoMenuItemOutsideDemonIdolMenu
            };

            foreach (var test in tests)
            {
                try
                {
                    test.Invoke();
                    passed++;
                    Debug.Log($"<color=#00e676>[GuardTest PASS] {test.Method.Name}</color>");
                }
                catch (Exception ex)
                {
                    failed++;
                    Debug.LogError($"<color=#ff1744>[GuardTest FAIL] {test.Method.Name}: {ex.Message}</color>");
                }
            }

            // Also run ArcadeArenaSceneIntegrityTest on BattleArena
            try
            {
                var legacySuite = new ArcadeArenaSceneIntegrityTest();
                legacySuite.ArcadeArena_MatchLoopObjectsArePresentAndEnabled();
                legacySuite.ArcadeBeginMatch_KeepsThePickedOpponent();
                legacySuite.FreshSessionDefaults_DoNotOverridePlayerPrefs();
                legacySuite.PlayerPrefsPick_IsTheRosterThatSpawns();
                passed += 4;
                Debug.Log("<color=#00e676>[GuardTest PASS] BattleArena MatchLoop & Roster Integrity Tests</color>");
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"<color=#ff1744>[GuardTest FAIL] BattleArena MatchLoop Tests: {ex.Message}</color>");
            }

            Debug.Log($"<color=#00e676><b>[DemonIdolGuardTests] SCENE INTEGRITY COMPLETE: {passed} Passed, {failed} Failed.</b></color>");
        }

        private static int CountRegexMatches(string text, string pattern)
        {
            return Regex.Matches(text, pattern).Count;
        }

        private static void AssertScriptEnabled(string yaml, string guid, string label)
        {
            string needle = "guid: " + guid;
            int at = yaml.IndexOf(needle, StringComparison.Ordinal);
            Assert.IsTrue(at >= 0, label + " is missing from scene YAML");
            int enabled = yaml.LastIndexOf("m_Enabled:", at, StringComparison.Ordinal);
            Assert.IsTrue(enabled >= 0 && at - enabled < 400, label + " has no enabled flag");
            string window = yaml.Substring(enabled, Math.Min(24, yaml.Length - enabled));
            Assert.IsTrue(window.StartsWith("m_Enabled: 1", StringComparison.Ordinal), label + " is disabled: " + window);
        }

        private static void AssertNamedActive(string yaml, string objectName)
        {
            int at = yaml.IndexOf("m_Name: " + objectName + "\n", StringComparison.Ordinal);
            if (at < 0) at = yaml.IndexOf("m_Name: " + objectName + "\r", StringComparison.Ordinal);
            Assert.IsTrue(at >= 0, objectName + " is missing from scene YAML");
            int active = yaml.IndexOf("m_IsActive:", at, StringComparison.Ordinal);
            Assert.IsTrue(active >= 0 && active - at < 280, objectName + " has no active flag");
            string window = yaml.Substring(active, Math.Min(20, yaml.Length - active));
            Assert.IsTrue(window.StartsWith("m_IsActive: 1", StringComparison.Ordinal), objectName + " is inactive: " + window);
        }

        private static void AssertBuiltInComponentEnabled(string yaml, string objectName, string unityClassId)
        {
            int nameAt = yaml.IndexOf("m_Name: " + objectName + "\n", StringComparison.Ordinal);
            if (nameAt < 0) nameAt = yaml.IndexOf("m_Name: " + objectName + "\r", StringComparison.Ordinal);
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
    }
}
#endif
