#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using DemonIdol.Combat;
using DemonIdol.Launch;
using DemonIdol.Editor;
using DemonIdol.EditorTools;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master One-Click Automator for My Demon Idol.
    /// Programmatically executes all dropdown routines in sequence:
    /// 1. Attack Data & MoveSet Assets Generation
    /// 2. Asset & Prefab Rebinding
    /// 3. Master Armory, Prop & Mugshot Deployment
    /// 4. 6 Stages & SF6 Camera Generation
    /// 5. Unified Toon & Anime Shading Pass
    /// 6. Valorant / Suspense Post-Processing Calibration
    /// 7. Costumes & Uniform Styling Pass
    /// 8. Build Scene 0: Assets/Scenes/MainMenu.unity
    /// 9. Build Scene 1: Assets/Scenes/ArcadeArena.unity (C12 Underwear1 vs C07 StellarWitch)
    /// 10. Configure EditorBuildSettings & PlayerSettings (Store Compliance, SDK 34, ARM64)
    /// 11. Configure Keystore Security
    /// 12. Run Full Validation Suites & Log Status Report
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolMasterAutomator
    {
        private const string PREF_KEY = "DemonIdol_MasterPipeline_AutoRun_v3";
        private const string REPORT_PATH = "Assets/Logs/MasterAutomationReport.txt";

        static DemonIdolMasterAutomator()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!EditorPrefs.GetBool(PREF_KEY, false))
            {
                EditorPrefs.SetBool(PREF_KEY, true);
                Debug.Log("<color=#00e676>[MasterAutomator] Auto-triggering Full Pipeline...</color>");
                RunFullAutomatedPipeline();
            }
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Run Full Automated Pipeline", priority = -100)]
        public static void RunFullAutomatedPipeline()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[MasterAutomator] Exiting Play Mode before running automation pipeline...");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += RunFullAutomatedPipeline;
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("================================================================================");
            report.AppendLine("               MY DEMON IDOL - MASTER AUTOMATION PIPELINE REPORT               ");
            report.AppendLine($"               Executed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}                    ");
            report.AppendLine("================================================================================");
            report.AppendLine();

            int stepCount = 0;
            int passCount = 0;

            void LogStep(string name, Action action)
            {
                stepCount++;
                try
                {
                    Debug.Log($"<color=#00e676>[MasterAutomator] [{stepCount}/12] Executing: {name}...</color>");
                    action();
                    passCount++;
                    report.AppendLine($"[PASS] Step {stepCount}: {name}");
                    Debug.Log($"<color=#00e676>[MasterAutomator] [{stepCount}/12] SUCCESS: {name}</color>");
                }
                catch (Exception ex)
                {
                    report.AppendLine($"[FAIL] Step {stepCount}: {name} - Exception: {ex.Message}");
                    Debug.LogError($"[MasterAutomator] [{stepCount}/12] ERROR in {name}: {ex}");
                }
            }

            // Step 1: Create Default Attack Data Assets
            LogStep("Create Default Attack Data Assets", () =>
            {
                DemonIdolAssetFactory.CreateDefaultAttackData();
            });

            // Step 2: Rebind All Character Assets & Prefabs
            LogStep("Rebind Character Assets, Animators & Overrides", () =>
            {
                DemonIdolAssetFactory.RebindAllCharactersMenu();
                DemonIdol.Characters.Editor.DemonIdolRosterAnimatorHook.HookAll14FightersAndAnimators();
            });

            // Step 3: Deploy Master Armory & Stage Systems
            LogStep("Deploy Master Armory & Stage Systems", () =>
            {
                DemonIdolArmoryBuilder.DeployAll();
            });

            // Step 4: Build All 6 Stages & SF6 Camera
            LogStep("Build All 6 Stages & SF6 Camera", () =>
            {
                DemonIdolStageDirector.ExecuteFullStageAndCameraSetup();
            });

            // Step 5: Apply Unified Anime Toon Shaders
            LogStep("Apply Unified Anime Toon Shaders", () =>
            {
                DemonIdolToonUnifier.ExecuteToonify();
            });

            // Step 6: Apply Valorant / Suspense Aesthetics
            LogStep("Apply Valorant / Suspense Aesthetics", () =>
            {
                DemonIdolValorantAesthetics.ApplyValorantAesthetics();
            });

            // Step 7: Rebuild Character Costumes & Uniforms
            LogStep("Rebuild Character Costumes (Uniform Style)", () =>
            {
                DemonIdolUniformCustomizer.RebuildAllCostumes();
            });

            // Step 8: Build Scene 1 (ArcadeArena.unity)
            LogStep("Build Clean Arcade Arena (C12 Underwear1 vs C07 StellarWitch)", () =>
            {
                DemonIdolArcadeArenaBuilder.BuildArcadeArenaScene();
            });

            // Step 9: Build Scene 0 (MainMenu.unity)
            LogStep("Build Launch Main Menu Scene", () =>
            {
                DemonIdolMainMenuBuilder.BuildMainMenuScene();
            });

            // Step 10: Configure Build Settings & Store Compliance
            LogStep("Configure Build Settings & Store Compliance", () =>
            {
                // Register scenes in authoritative order:
                // Scene 0 = MainMenu.unity
                // Scene 1 = ArcadeArena.unity
                EditorBuildSettingsScene[] scenes = new[]
                {
                    new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                    new EditorBuildSettingsScene("Assets/Scenes/ArcadeArena.unity", true)
                };
                EditorBuildSettings.scenes = scenes;

                // Player settings
                PlayerSettings.companyName = "Demon Idol Studios";
                PlayerSettings.productName = "My Demon Idol";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.celestialsoul.mydemonidol");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.celestialsoul.mydemonidol");

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
            });

            // Step 11: Apply Keystore Security Configuration
            LogStep("Apply Keystore Security Configuration", () =>
            {
                AndroidKeystoreConfig.ApplyKeystoreConfiguration();
            });

            // Step 12: Run Content & MoveSet Validation Suites
            LogStep("Run Content & MoveSet Validation Suites", () =>
            {
                FighterMoveSetValidator.RunValidation();
                ContentValidationEditor.RunValidationBatch();
                DemonLordNarratorEditor.VerifyAllMediaFiles();
            });

            // Save and Refresh AssetDatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Write report
            report.AppendLine();
            report.AppendLine($"Results: {passCount} of {stepCount} steps succeeded ({passCount * 100 / stepCount}%).");
            report.AppendLine("================================================================================");

            string reportDir = Path.GetDirectoryName(REPORT_PATH);
            if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);
            File.WriteAllText(REPORT_PATH, report.ToString());

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log($"<color=#00e676><b>[MasterAutomator] COMPLETE:</b> {passCount}/{stepCount} Steps Finished Successfully!</color>");
            Debug.Log($"<color=#00e676>Full Report saved to: {REPORT_PATH}</color>");
            Debug.Log("<color=#00e676>Scenes Ready: MainMenu.unity (Index 0) -> ArcadeArena.unity (Index 1)</color>");
            Debug.Log("<color=#00e676>=============================================================</color>");
        }
    }
}
#endif

#endif
