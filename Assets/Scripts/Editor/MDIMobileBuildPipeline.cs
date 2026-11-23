#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DemonIdol.Editor
{
    public static class MDIMobileBuildPipeline
    {
        public static readonly string[] BuildScenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/CharacterSelect.unity",
            "Assets/Scenes/BattleArena.unity"
        };

        [MenuItem("Tools/MDI/Build Mac Standalone (Development)")]
        public static bool BuildMacStandalone()
        {
            PlayerSettings.companyName = "Celestial Soul";
            PlayerSettings.productName = "My Demon Idol";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.celestialsoul.mydemonidol");

            string outDir = Path.GetFullPath("Builds/macOS");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol.app");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = outPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Building Mac Standalone to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            return report.summary.result == BuildResult.Succeeded;
        }

        [MenuItem("Tools/MDI/Build Android APK (Development)")]
        public static bool BuildAndroidApk()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;

            PlayerSettings.companyName = "Celestial Soul";
            PlayerSettings.productName = "My Demon Idol";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.celestialsoul.mydemonidol");

            string outDir = Path.GetFullPath("Builds/Android");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol_Dev.apk");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Compiling Android APK to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            Debug.Log($"[MDIMobileBuildPipeline] Android build result: {report.summary.result}, errors: {report.summary.totalErrors}");
            return report.summary.result == BuildResult.Succeeded;
        }

        [MenuItem("Tools/MDI/Build iOS Xcode Project")]
        public static bool BuildiOSProject()
        {
            PlayerSettings.companyName = "Celestial Soul";
            PlayerSettings.productName = "My Demon Idol";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.celestialsoul.mydemonidol");

            string outDir = Path.GetFullPath("Builds/iOS");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = outDir,
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Compiling iOS Xcode project to: {outDir}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            Debug.Log($"[MDIMobileBuildPipeline] iOS build result: {report.summary.result}, errors: {report.summary.totalErrors}");
            return report.summary.result == BuildResult.Succeeded;
        }
    }
}
#endif
