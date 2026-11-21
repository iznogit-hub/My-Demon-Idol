#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Editor;
using DemonIdol.Characters.Editor;

namespace DemonIdol.EditorTools
{
    /// <summary>
    /// Master Unified Unity Editor Menu for Demon Idol.
    /// Provides a single, clean, organized hierarchy for all game operations:
    /// - One-Click Pipeline Execution & In-Editor Play
    /// - Scenes (MainMenu & ArcadeArena)
    /// - Roster & Animators (14 Playable Fighters, Override Controllers, Costumes, Shaders)
    /// - Combat & Stages (Armory, 6 Stages, Attack Data)
    /// - Multiplayer & Quantum (Config & Wiring)
    /// - Build Pipeline (macOS Standalone, Android APK, Android AAB)
    /// - Verification & Audit (374 Verification Suite, Media, Validation)
    /// </summary>
    public static class DemonIdolMasterMenu
    {
        // =====================================================================
        // 1. TOP-LEVEL ONE-CLICK ACTIONS
        // =====================================================================
        [MenuItem("Demon Idol/⚡ Run Full Automated Pipeline", priority = 0)]
        public static void RunFullPipeline()
        {
            DemonIdol.Editor.DemonIdolMasterAutomator.RunFullAutomatedPipeline();
        }

        [MenuItem("Demon Idol/🎮 Play Match in Editor", priority = 1)]
        public static void PlayMatchInEditor()
        {
            DemonIdolMainMenuBuilder.PlayMatchInEditor();
        }

        // =====================================================================
        // 2. SCENES
        // =====================================================================
        [MenuItem("Demon Idol/🎬 Scenes/Open Main Menu Scene (Build Index 0)", priority = 10)]
        public static void OpenMainMenuScene()
        {
            DemonIdolMainMenuBuilder.OpenMainMenuScene();
        }

        [MenuItem("Demon Idol/🎬 Scenes/Open Arcade Arena Scene (Build Index 1)", priority = 11)]
        public static void OpenArcadeArenaScene()
        {
            DemonIdolMainMenuBuilder.OpenArcadeArenaScene();
        }

        [MenuItem("Demon Idol/🎬 Scenes/Main Menu & UI Orchestrator Studio", priority = 19)]
        public static void OpenMenuOrchestrator()
        {
            DemonIdolMenuOrchestratorWindow.OpenWindow();
        }

        [MenuItem("Demon Idol/🎬 Scenes/Rebuild Main Menu Scene", priority = 20)]
        public static void RebuildMainMenuScene()
        {
            DemonIdolMainMenuBuilder.BuildMainMenuScene();
        }

        [MenuItem("Demon Idol/🎬 Scenes/Rebuild Arcade Arena Scene", priority = 21)]
        public static void RebuildArcadeArenaScene()
        {
            DemonIdolArcadeArenaBuilder.BuildArcadeArenaScene();
        }

        [MenuItem("Demon Idol/🎬 Scenes/Deploy College Raimon to All 3 Scenes", priority = 22)]
        public static void DeployCollegeRaimonAllScenes()
        {
            DemonIdolCollegeRaimonSetup.ExecuteFullSetup();
        }

        // =====================================================================
        // 3. ROSTER & ANIMATORS
        // =====================================================================
        [MenuItem("Demon Idol/👥 Roster & Animators/Fighter & Asset Customization Studio", priority = 29)]
        public static void OpenCustomizationStudio()
        {
            DemonIdolAssetCustomizerWindow.OpenWindow();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Hook All 14 Playable Fighters", priority = 30)]
        public static void HookAll14Fighters()
        {
            DemonIdolRosterAnimatorHook.HookAll14FightersAndAnimators();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Rebind All Character Assets & Prefabs", priority = 31)]
        public static void RebindCharacterAssets()
        {
            DemonIdolAssetFactory.RebindAllCharactersMenu();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Validate 14-Fighter MoveSets", priority = 32)]
        public static void ValidateMoveSets()
        {
            FighterMoveSetValidator.ValidateAndLog();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Apply Anime Toon Aesthetics to Characters", priority = 33)]
        public static void ApplyAesthetics()
        {
            DemonIdolJujutsuAesthetics.ApplyAesthetics();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Rebuild Character Costumes (Uniform Style)", priority = 34)]
        public static void RebuildCostumes()
        {
            DemonIdolUniformCustomizer.RebuildAllCostumes();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Strip All Props & Weapons from Characters", priority = 35)]
        public static void StripAllProps()
        {
            DemonIdolCharacterPropSanitizer.StripAllPropsAndWeaponsFromAllCharacters();
        }

        [MenuItem("Demon Idol/👥 Roster & Animators/Unify 16 Dota Heroes & Weapons to Anime Toon", priority = 36)]
        public static void UnifyDotaHeroesAndWeaponsToAnimeToon()
        {
            DemonIdol.BlazBlue.Editor.BBDotaToonUnifier.ExecuteDotaToonify();
        }

        // =====================================================================
        // 4. COMBAT & STAGES
        // =====================================================================
        [MenuItem("Demon Idol/⚔️ Combat & Stages/Stage & Arena Customization Studio", priority = 39)]
        public static void OpenStageStudio()
        {
            DemonIdolStageCustomizerWindow.OpenWindow();
        }

        [MenuItem("Demon Idol/⚔️ Combat & Stages/Deploy Master Armory & Stage Systems", priority = 40)]
        public static void DeployArmory()
        {
            DemonIdolArmoryBuilder.DeployAll();
        }

        [MenuItem("Demon Idol/⚔️ Combat & Stages/Build All 6 Stages and SF6 Camera", priority = 41)]
        public static void BuildStages()
        {
            DemonIdolStageDirector.ExecuteFullStageAndCameraSetup();
        }

        [MenuItem("Demon Idol/⚔️ Combat & Stages/Create Default Attack Data Assets", priority = 42)]
        public static void CreateAttackData()
        {
            DemonIdolAssetFactory.CreateDefaultAttackData();
        }

        [MenuItem("Demon Idol/🎥 Cameras/Setup All 7 Stage Cameras (College Raimon)", priority = 45)]
        public static void SetupAllStageCameras()
        {
            DemonIdolArcadeArenaBuilder.EnsureAll7StageCamerasInScene();
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST01 Central Courtyard Camera", priority = 46)]
        public static void PreviewST01Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST01");
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST02 Sakura Main Gate Camera", priority = 47)]
        public static void PreviewST02Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST02");
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST03 Rooftop Observatory Camera", priority = 48)]
        public static void PreviewST03Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST03");
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST04 Martial Arts Dojo Camera", priority = 49)]
        public static void PreviewST04Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST04");
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST05 Athletic Stadium Camera", priority = 50)]
        public static void PreviewST05Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST05");
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST06 Transit Promenade Camera", priority = 51)]
        public static void PreviewST06Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST06");
        }

        [MenuItem("Demon Idol/🎥 Cameras/Preview ST07 Midnight Courtyard Camera", priority = 52)]
        public static void PreviewST07Camera()
        {
            DemonIdol.CameraControl.StageCameraManager.Instance?.SwitchToStageCamera("ST07");
        }

        // =====================================================================
        // 5. MULTIPLAYER & QUANTUM
        // =====================================================================
        [MenuItem("Demon Idol/🌐 Multiplayer & Quantum/Configure Photon Quantum", priority = 50)]
        public static void ConfigureQuantum()
        {
            DemonIdolQuantumSetup.ConfigureQuantum();
        }

        [MenuItem("Demon Idol/🌐 Multiplayer & Quantum/Wire Quantum In All Scenes", priority = 51)]
        public static void WireQuantum()
        {
            DemonIdolQuantumSetup.WireQuantumSceneObjects();
        }

        // =====================================================================
        // 6. BUILD PIPELINE
        // =====================================================================
        [MenuItem("Demon Idol/📱 Build Pipeline/Build macOS Standalone (Fast Local Test)", priority = 60)]
        public static void BuildMacOS()
        {
            DemonIdolAndroidBuildPipeline.BuildMacOS();
        }

        [MenuItem("Demon Idol/📱 Build Pipeline/Build Android APK (Development)", priority = 61)]
        public static void BuildAndroidApk()
        {
            DemonIdolAndroidBuildPipeline.BuildAndroidApk();
        }

        [MenuItem("Demon Idol/📱 Build Pipeline/Build Android AAB (Google Play Release)", priority = 62)]
        public static void BuildAndroidAab()
        {
            DemonIdolAndroidBuildPipeline.BuildAndroidAab();
        }

        [MenuItem("Demon Idol/📱 Build Pipeline/Apply Local Keystore Configuration", priority = 63)]
        public static void ApplyKeystore()
        {
            AndroidKeystoreConfig.ApplyKeystoreConfiguration();
        }

        // =====================================================================
        // 7. VERIFICATION & AUDIT
        // =====================================================================
        [MenuItem("Demon Idol/🧪 Verification & Audit/Run Match Simulation Verification Suite (374 Tests)", priority = 70)]
        public static void RunVerificationSuite()
        {
            MatchSimulationVerificationTest.RunAllTestsMenu();
        }

        [MenuItem("Demon Idol/🧪 Verification & Audit/Validate Content Definitions", priority = 71)]
        public static void ValidateContent()
        {
            ContentValidationEditor.RunValidationBatch();
        }

        [MenuItem("Demon Idol/🧪 Verification & Audit/Verify Cutscenes & Media Files", priority = 72)]
        public static void VerifyMedia()
        {
            DemonLordNarratorEditor.VerifyAllMediaFiles();
        }

        [MenuItem("Demon Idol/🧪 Verification & Audit/Capture Showcase Screenshot", priority = 73)]
        public static void CaptureScreenshot()
        {
            DemonIdolCharacterShowcaseCapture.CaptureRosterShowcase();
        }

        [MenuItem("Demon Idol/🧪 Verification & Audit/Capture Online Match Screenshots", priority = 77)]
        public static void CaptureOnlineMatch()
        {
            DemonIdolOnlineMatchCapture.CaptureOnlineMatchScreens();
        }
    }
}
#endif
