#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Master Consolidated Build Pipeline for My Demon Idol.
    /// Provides headless store packaging, performance certification, and development builds:
    /// - macOS Desktop Standalone (Universal ARM64 + x64)
    /// - Android Development APK & Google Play Release AAB (IL2CPP, ARM64, Target API 34+)
    /// - iOS Archive (Xcode Project, Metal, ARM64)
    /// All editor menu items route strictly through DemonIdolMenu.cs.
    /// </summary>
    public static class MDIMobileBuildPipeline
    {
        public const string BUILD_OUTPUT_DIR = "Builds";
        private const string DEFAULT_KEYALIAS = "demonidol";
        private const string LOCAL_CONFIG_PATH = "Keystore/signing.local.json";

        public static readonly string[] BuildScenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/CharacterSelect.unity",
            "Assets/Scenes/BattleArena.unity",
            "Assets/Scenes/ArcadeArena.unity"
        };

        public static bool VerifyMobileCertificationBaseline()
        {
            Debug.Log("<color=#00e676>[MDIMobileBuildPipeline] Verifying Mobile Certification Baseline...</color>");

            bool pass = true;

            // 1. Verify Landscape orientation
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.LandscapeLeft &&
                PlayerSettings.defaultInterfaceOrientation != UIOrientation.AutoRotation)
            {
                Debug.LogWarning("[MDIMobileBuildPipeline] Mobile orientation should be set to Landscape or AutoRotation.");
            }

            // 2. Verify Color Space (Linear required for URP)
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                Debug.LogWarning("[MDIMobileBuildPipeline] Color space is not Linear. Linear is strongly recommended for URP 17.x.");
            }

            // 3. Verify All 9 Roster Profiles Exist
            string[] characterIds = { "C01", "C02", "C04", "C06", "C07", "C08", "C09", "C10", "C12" };
            for (int i = 0; i < characterIds.Length; i++)
            {
                string id = characterIds[i];
                var assets = AssetDatabase.FindAssets($"BBProfile_{id}_");
                if (assets == null || assets.Length == 0)
                {
                    Debug.LogError($"[MDIMobileBuildPipeline] Missing BBProfile for {id}!");
                    pass = false;
                }
            }

            if (pass)
            {
                Debug.Log("<color=#00e676>[MDIMobileBuildPipeline] All 9 roster profiles verified. Mobile Certification Baseline PASSED!</color>");
            }
            return pass;
        }

        #region macOS Standalone

        public static void PrepareMacOSPlayerSettings()
        {
            PlayerSettings.companyName = "Celestial Soul";
            PlayerSettings.productName = "My Demon Idol";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.celestialsoul.mydemonidol");
            try
            {
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 2); // 2 = Universal (ARM64 + x64)
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MDIMobileBuildPipeline] SetArchitecture(Standalone, 2) warning: " + ex.Message);
            }
        }

        public static void ApplyMacOSSettingsMenu()
        {
            PrepareMacOSPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Applied macOS Standalone Settings: bundleId={PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone)}, version={PlayerSettings.bundleVersion}, arch={PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone)}</color>");
        }

        public static bool BuildMacStandalone()
        {
            PrepareMacOSPlayerSettings();

            string outDir = Path.GetFullPath(Path.Combine(BUILD_OUTPUT_DIR, "macOS"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol.app");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = GetEnabledBuildScenes(),
                locationPathName = outPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Building macOS Standalone to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "macOS Standalone");
            return report.summary.result == BuildResult.Succeeded;
        }

        #endregion

        #region Android

        public static void PrepareAndroidPlayerSettings(bool isAppBundle)
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

            ApplyKeystoreConfiguration();
        }

        public static bool BuildAndroidApk()
        {
            PrepareAndroidPlayerSettings(isAppBundle: false);

            string outDir = Path.GetFullPath(Path.Combine(BUILD_OUTPUT_DIR, "Android"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol_Dev.apk");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = GetEnabledBuildScenes(),
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Building Android APK to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "Android APK");
            return report.summary.result == BuildResult.Succeeded;
        }

        public static bool BuildAndroidAab()
        {
            if (!VerifyMobileCertificationBaseline())
            {
                Debug.LogError("[MDIMobileBuildPipeline] Pre-build verification failed! Aborting Android build.");
                return false;
            }

            PrepareAndroidPlayerSettings(isAppBundle: true);

            string outDir = Path.GetFullPath(Path.Combine(BUILD_OUTPUT_DIR, "Android"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "MyDemonIdol_Release.aab");

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = GetEnabledBuildScenes(),
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Building Google Play Release AAB to: {outPath}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "Android AAB");
            return report.summary.result == BuildResult.Succeeded;
        }

        private static void ApplyKeystoreConfiguration()
        {
            string keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            string keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            string keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
            string keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");

            if (string.IsNullOrEmpty(keystorePass) && File.Exists(LOCAL_CONFIG_PATH))
            {
                try
                {
                    string json = File.ReadAllText(LOCAL_CONFIG_PATH);
                    var cfg = JsonUtility.FromJson<LocalKeystoreData>(json);
                    if (cfg != null)
                    {
                        if (string.IsNullOrEmpty(keystorePath)) keystorePath = cfg.keystorePath;
                        if (string.IsNullOrEmpty(keystorePass)) keystorePass = cfg.keystorePass;
                        if (string.IsNullOrEmpty(keyaliasName)) keyaliasName = cfg.keyaliasName;
                        if (string.IsNullOrEmpty(keyaliasPass)) keyaliasPass = cfg.keyaliasPass;
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(keyaliasName)) keyaliasName = DEFAULT_KEYALIAS;
            PlayerSettings.Android.keyaliasName = keyaliasName;

            if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
            }

            if (!string.IsNullOrEmpty(keystorePass)) PlayerSettings.Android.keystorePass = keystorePass;
            if (!string.IsNullOrEmpty(keyaliasPass)) PlayerSettings.Android.keyaliasPass = keyaliasPass;
        }

        [Serializable]
        private class LocalKeystoreData
        {
            public string keystorePath;
            public string keystorePass;
            public string keyaliasName;
            public string keyaliasPass;
        }

        #endregion

        #region iOS

        public static bool BuildIosProject()
        {
            if (!VerifyMobileCertificationBaseline())
            {
                Debug.LogError("[MDIMobileBuildPipeline] Pre-build verification failed! Aborting iOS build.");
                return false;
            }

            PlayerSettings.companyName = "Celestial Soul";
            PlayerSettings.productName = "My Demon Idol";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.celestialsoul.mydemonidol");

            string outDir = Path.GetFullPath(Path.Combine(BUILD_OUTPUT_DIR, "iOS"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = GetEnabledBuildScenes(),
                locationPathName = outDir,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00e676>[MDIMobileBuildPipeline] Compiling iOS Xcode project to: {outDir}...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            LogReport(report, "iOS");
            return report.summary.result == BuildResult.Succeeded;
        }

        public static bool BuildIOS() => BuildIosProject();

        #endregion

        private static string[] GetEnabledBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var list = new List<string>();
            if (scenes != null && scenes.Length > 0)
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (scenes[i].enabled)
                    {
                        list.Add(scenes[i].path);
                    }
                }
            }
            if (list.Count == 0)
            {
                list.AddRange(BuildScenes);
            }
            return list.ToArray();
        }

        private static void LogReport(BuildReport report, string targetName)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00e676><b>[MDIMobileBuildPipeline] SUCCESS:</b> {targetName} build completed in {report.summary.totalTime.TotalSeconds:F1}s! Output size: {report.summary.totalSize / (1024 * 1024)} MB</color>");
            }
            else
            {
                Debug.LogError($"[MDIMobileBuildPipeline] FAILED: {targetName} build finished with status: {report.summary.result}. Errors: {report.summary.totalErrors}");
            }
        }
    }

    /// <summary>
    /// Backwards compatibility stub for BBBuildPipeline.
    /// </summary>
    [Obsolete("Use MDIMobileBuildPipeline instead.")]
    public static class BBBuildPipeline
    {
        public static bool VerifyMobileCertificationBaseline() => MDIMobileBuildPipeline.VerifyMobileCertificationBaseline();
        public static void BuildAndroidAAB() => MDIMobileBuildPipeline.BuildAndroidAab();
        public static void BuildIOS() => MDIMobileBuildPipeline.BuildIosProject();
    }
}
#endif
