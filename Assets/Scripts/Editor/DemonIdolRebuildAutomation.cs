#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Tests;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.Launch;

namespace DemonIdol.Editor
{
    [InitializeOnLoad]
    public static class DemonIdolRebuildAutomation
    {
        public const string TRIGGER_BUILD_SCENES = "build_scenes.trigger";
        public const string TRIGGER_GUARD_TESTS = "run_guards.trigger";
        public const string TRIGGER_ALL_TESTS = "run_all_tests.trigger";
        public const string TRIGGER_MATCH_LOOP = "run_match_loop.trigger";
        public const string TRIGGER_PLAY_PROOF = "run_play_proof.trigger";

        static DemonIdolRebuildAutomation()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (File.Exists(TRIGGER_BUILD_SCENES))
            {
                try { File.Delete(TRIGGER_BUILD_SCENES); } catch { }
                ExecuteBuildScenes();
            }

            if (File.Exists(TRIGGER_GUARD_TESTS))
            {
                try { File.Delete(TRIGGER_GUARD_TESTS); } catch { }
                ExecuteGuardTests();
            }

            if (File.Exists(TRIGGER_MATCH_LOOP))
            {
                try { File.Delete(TRIGGER_MATCH_LOOP); } catch { }
                ExecuteMatchLoop();
            }

            if (File.Exists(TRIGGER_PLAY_PROOF))
            {
                try { File.Delete(TRIGGER_PLAY_PROOF); } catch { }
                ExecutePlayProof();
            }
        }

        public static void ExecuteBuildScenes()
        {
            Debug.Log("<color=#00e676>[RebuildAutomation] Building all 3 scenes from code...</color>");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "rebuild");
            Directory.CreateDirectory(dir);

            try
            {
                DemonIdolSceneBuilder.BuildAllThreeScenes(false);
                File.WriteAllText(Path.Combine(dir, "build_scenes_status.txt"), "SUCCESS: All 3 scenes built and configured in EditorBuildSettings.");
                Debug.Log("<color=#00e676>[RebuildAutomation] Build scenes COMPLETED successfully!</color>");
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(dir, "build_scenes_status.txt"), $"FAIL: {ex.Message}\n{ex.StackTrace}");
                Debug.LogError($"[RebuildAutomation] Build scenes FAILED: {ex.Message}");
            }
        }

        public static void ExecuteGuardTests()
        {
            Debug.Log("<color=#00e676>[RebuildAutomation] Running Demon Idol Rebuild Guard Tests...</color>");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "rebuild");
            Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            int passed = 0;
            int failed = 0;

            var guards = new DemonIdolRebuildGuardTests();
            Action[] tests = new Action[]
            {
                guards.Guard_BattleArena_MatchHostAndComponents,
                guards.Guard_MainMenu_SingleEventSystemWithInputSystemUI,
                guards.Guard_CharacterSelect_SingleEventSystemWithInputSystemUI,
                guards.Guard_BuildSettings_ExactlyThreeScenesInOrder,
                guards.Guard_NoMenuItemOutsideDemonIdolMenu
            };

            foreach (var t in tests)
            {
                string name = t.Method.Name;
                try
                {
                    t.Invoke();
                    passed++;
                    sb.AppendLine($"PASS: {name}");
                    Debug.Log($"<color=#00e676>[RebuildGuard] PASS: {name}</color>");
                }
                catch (Exception ex)
                {
                    failed++;
                    sb.AppendLine($"FAIL: {name} - {ex.Message}");
                    Debug.LogError($"<color=#ff1744>[RebuildGuard] FAIL: {name} - {ex.Message}</color>");
                }
            }

            sb.AppendLine($"TOTAL: {passed} Passed, {failed} Failed out of {tests.Length} Guard Tests");
            File.WriteAllText(Path.Combine(dir, "guard_tests_status.txt"), sb.ToString());
            Debug.Log($"<color=#00e676><b>[RebuildGuard] FINISHED: {passed}/{tests.Length} passed.</b></color>");
        }

        public static void ExecuteMatchLoop()
        {
            Debug.Log("<color=#00e676>[RebuildAutomation] Triggering MatchLoopVerifier...</color>");
            MatchLoopVerifier.RunVerification();
        }

        public static void ExecutePlayProof()
        {
            Debug.Log("<color=#00e676>[RebuildAutomation] Starting Full Play Mode Proof Runner...</color>");
            DemonIdolPlayProofRunner.StartProof();
        }
    }
}
#endif
