#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Editor;
using DemonIdol.BlazBlue.Tests;

namespace DemonIdol.Editor
{
    /// <summary>
    /// The ONLY authoritative entry point for "Demon Idol/" editor commands.
    /// Consolidated and rebuilt from scratch per MDI Rebuild Specification.
    /// Strict policy:
    /// - Only scene build commands here are authorized to save scenes (with confirmation + legacy backup).
    /// - No other MenuItem starting with "Demon Idol/" or "MDI/" is permitted anywhere in the project.
    /// </summary>
    public static class DemonIdolMenu
    {
        // =========================================================================
        // SCENES
        // =========================================================================
        [MenuItem("Demon Idol/Scenes/Build Main Menu", priority = 10)]
        public static void BuildMainMenu()
        {
            DemonIdolSceneBuilder.BuildMainMenuScene(true);
        }

        [MenuItem("Demon Idol/Scenes/Build Character Select", priority = 11)]
        public static void BuildCharacterSelect()
        {
            DemonIdolSceneBuilder.BuildCharacterSelectScene(true);
        }

        [MenuItem("Demon Idol/Scenes/Build Battle Arena (College Raimon)", priority = 12)]
        public static void BuildBattleArena()
        {
            DemonIdolSceneBuilder.BuildBattleArenaScene(true);
        }

        [MenuItem("Demon Idol/Scenes/Build All Three + Set Build Order", priority = 13)]
        public static void BuildAllThree()
        {
            DemonIdolSceneBuilder.BuildAllThreeScenes(true);
        }

        // =========================================================================
        // PLAY
        // =========================================================================
        [MenuItem("Demon Idol/Play/Play From Main Menu", priority = 20)]
        public static void PlayFromMainMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(DemonIdolSceneBuilder.SCENE_MAIN_MENU, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
        }

        // =========================================================================
        // VERIFY
        // =========================================================================
        [MenuItem("Demon Idol/Verify/Match Loop", priority = 30)]
        public static void VerifyMatchLoop()
        {
            MatchLoopVerifier.RunVerification();
        }

        [MenuItem("Demon Idol/Verify/Scene Integrity", priority = 31)]
        public static void VerifySceneIntegrity()
        {
            DemonIdolRebuildGuardTests.RunSceneIntegrityCheck();
        }

        [MenuItem("Demon Idol/Verify/Run All EditMode Tests", priority = 32)]
        public static void RunAllEditModeTests()
        {
            BBAllTestsRunner.RunAllSuites();
        }

        [MenuItem("Demon Idol/Verify/Run Batch 1 Proof", priority = 33)]
        public static void RunBatch1Proof()
        {
            Batch1PolishProofRunner.StartProof();
        }

        // =========================================================================
        // FIGHTERS
        // =========================================================================
        [MenuItem("Demon Idol/Fighters/Box Editor", priority = 40)]
        public static void OpenBoxEditor()
        {
            BBBoxEditorWindow.OpenWindow();
        }

        [MenuItem("Demon Idol/Fighters/Export Moveset Tracker", priority = 41)]
        public static void ExportMovesetTracker()
        {
            BBMovesetTrackerExporter.ExportMovesetTracker();
        }

        [MenuItem("Demon Idol/Fighters/Clip Inventory", priority = 42)]
        public static void RunClipInventory()
        {
            BBClipInventoryTool.GenerateInventory();
        }

        [MenuItem("Demon Idol/Fighters/Apply Batch 1 Polish", priority = 43)]
        public static void ApplyBatch1Polish()
        {
            Batch1CharacterPolishTool.ApplyBatch1Polish();
        }

        // =========================================================================
        // BUILD
        // =========================================================================
        [MenuItem("Demon Idol/Build/Mac App", priority = 50)]
        public static void BuildMacApp()
        {
            DemonIdolAndroidBuildPipeline.BuildMacOS();
        }

        [MenuItem("Demon Idol/Build/iOS Xcode Project", priority = 51)]
        public static void BuildIosXcode()
        {
            BBBuildPipeline.BuildIOS();
        }

        [MenuItem("Demon Idol/Build/Android AAB", priority = 52)]
        public static void BuildAndroidAab()
        {
            BBBuildPipeline.BuildAndroidAAB();
        }
    }
}
#endif
