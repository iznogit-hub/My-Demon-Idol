#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBMacDevBuildRunner
    {
        [MenuItem("Tools/MDI/Build Mac Development Player")]
        public static bool BuildMacPlayer()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir = Path.Combine(projectRoot, "Builds", "Mac");
            Directory.CreateDirectory(buildDir);

            string appPath = Path.Combine(buildDir, "MDI.app");

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/CharacterSelect.unity",
                "Assets/Scenes/BattleArena.unity"
            };

            Debug.Log($"[BBMacDevBuildRunner] Starting macOS Standalone Development Build -> {appPath}...");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = appPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00e676>[BBMacDevBuildRunner] BUILD SUCCEEDED: {summary.totalSize / (1024 * 1024)} MB in {summary.totalTime.TotalSeconds:F1} seconds at {appPath}</color>");
                return true;
            }
            else
            {
                Debug.LogError($"[BBMacDevBuildRunner] BUILD FAILED: {summary.totalErrors} errors.");
                return false;
            }
        }
    }
}
#endif
