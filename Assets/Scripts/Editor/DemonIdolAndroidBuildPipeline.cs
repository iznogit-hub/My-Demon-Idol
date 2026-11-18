#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Automated Build Pipeline for My Demon Idol.
    /// Provides one-click builds for Android (APK / Google Play AAB) and macOS Desktop Standalone.
    /// Ensures proper scene inclusion, IL2CPP scripting backend, ARM64 architecture, and keystore signing.
    /// </summary>
    public static class DemonIdolAndroidBuildPipeline
    {
        private static readonly string[] BuildScenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ArcadeArena.unity"
        };

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Build/Build macOS Standalone (Fast Local Test)", priority = 10)]
        public static void BuildMacOS()
        {
            string outDir = Path.GetFullPath("Builds/macOS");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol.app");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = outPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport
            };

            Debug.Log($"<color=#00e676>[BuildPipeline] Building macOS Standalone to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "macOS Standalone");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Build/Build Android APK (Development)", priority = 11)]
        public static void BuildAndroidApk()
        {
            PrepareAndroidPlayerSettings(isAppBundle: false);

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

            Debug.Log($"<color=#00e676>[BuildPipeline] Building Android APK to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "Android APK");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Build/Build Android AAB (Google Play Release)", priority = 12)]
        public static void BuildAndroidAab()
        {
            PrepareAndroidPlayerSettings(isAppBundle: true);

            string outDir = Path.GetFullPath("Builds/Android");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol_Release.aab");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e676>[BuildPipeline] Building Google Play Release AAB to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "Android AAB");
        }

        private static void PrepareAndroidPlayerSettings(bool isAppBundle)
        {
            EditorUserBuildSettings.buildAppBundle = isAppBundle;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;

            PlayerSettings.companyName = "Demon Idol Studios";
            PlayerSettings.productName = "My Demon Idol";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.celestialsoul.mydemonidol");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.colorSpace = ColorSpace.Linear;

            AndroidKeystoreConfig.ApplyKeystoreConfiguration();
        }

        private static void LogReport(BuildReport report, string targetName)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00e676><b>[BuildPipeline] SUCCESS:</b> {targetName} build completed in {report.summary.totalTime.TotalSeconds:F1}s! Output size: {report.summary.totalSize / (1024 * 1024)} MB</color>");
            }
            else
            {
                Debug.LogError($"[BuildPipeline] FAILED: {targetName} build finished with status: {report.summary.result}. Errors: {report.summary.totalErrors}");
            }
        }
    }
}
#endif
