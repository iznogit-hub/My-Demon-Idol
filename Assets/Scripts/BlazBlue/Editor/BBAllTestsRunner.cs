#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue.Tests;

namespace DemonIdol.BlazBlue.Editor
{
    [InitializeOnLoad]
    public static class BBAllTestsRunner
    {
        public const string TRIGGER_FILE = "run_all_tests.trigger";

        static BBAllTestsRunner()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                RunAllSuites();
            }
        }

        [MenuItem("Demon Idol/BlazBlue/Run Master Test Suite")]
        public static void RunAllSuites()
        {
            Debug.Log("<color=#00e676>[BBAllTestsRunner] STARTING MASTER COMBAT & FIGHTER TEST RUNNER...</color>");

            int passedSuites = 0;
            int failedSuites = 0;

            Action[] suites = new Action[]
            {
                BBAkioTests.RunAllTestsMenu,
                BBAikaTests.RunAllTestsMenu,
                BBUnityChanTests.RunAllTestsMenu,
                BBCultistTests.RunAllTestsMenu,
                BBStellarWitchTests.RunAllTestsMenu,
                BBSanpakuChanTests.RunAllTestsMenu,
                BBPicoChanTests.RunAllTestsMenu,
                BBReikaTests.RunAllTestsMenu,
                BBFinaTests.RunAllTestsMenu,
                BBArenaCleanupTests.RunAllTestsMenu,
                BBCollisionTests.RunAllTestsMenu,
                BBMotionMatcherTests.RunAllTestsMenu,
                BBPhase6Tests.RunAllTestsMenu,
                BBSubsystemTests.RunAllTestsMenu
            };

            foreach (var suite in suites)
            {
                string name = suite.Method.DeclaringType.Name;
                try
                {
                    suite.Invoke();
                    passedSuites++;
                    Debug.Log($"<color=#00e676>[BBAllTestsRunner] SUITE PASSED: {name}</color>");
                }
                catch (Exception ex)
                {
                    failedSuites++;
                    Debug.LogError($"<color=#ff1744>[BBAllTestsRunner] SUITE FAILED: {name} - {ex.Message}\n{ex.StackTrace}</color>");
                }
            }

            // Master Remap & Export Tracker to guarantee 0 collisions & clean state
            BBWarriorMasterRemapper.ExecuteMasterRemap();

            Debug.Log($"<color=#00e676><b>[BBAllTestsRunner] TEST RUN COMPLETE! {passedSuites} Passed, {failedSuites} Failed out of {suites.Length} Suites!</b></color>");
        }
    }
}
#endif
