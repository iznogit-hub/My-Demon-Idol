# Demon Idol Menu Audit & Migration Specification

**Audit Date**: 2026-11-22  
**Total Audited MenuItems**: 174 across 93 files  
**Archived MenuItems**: 145 across 69 files  
**Kept & Rerouted MenuItems**: 29 across 24 files  

## Archival Policy & Rules

As mandated by the Demon Idol Rebuild specification:

1. **Strict Single Entry Point**: `Assets/Scripts/Editor/DemonIdolMenu.cs` is the ONLY file allowed to register `[MenuItem("Demon Idol/...")]`.
2. **Removal of Legacy Roots**: The separate `MDI/` and `DemonIdol/` menu roots are permanently abolished.
3. **Non-Destructive Archival**: All archived scripts are relocated to `Assets/Editor_Archive/` and guarded with `#if MDI_ARCHIVE ... #endif` to ensure clean compilation with zero leftover menu items.
4. **Preserved Core Utilities**: Essential tools (`BBBoxEditorWindow`, `BBMovesetTrackerExporter`, `BBClipInventoryTool`, `MatchLoopVerifier`, and Build pipelines) retain full functionality via clean calls from `DemonIdolMenu.cs`.
5. **Pure Test Suites**: NUnit tests in `Assets/Scripts/BlazBlue/Tests/Editor/` have legacy `[MenuItem]` tags removed, enabling standard Test Runner and automated verification without polluting editor menus.

## Complete Menu Audit Matrix (174 Items)

| # | Menu Path | Source File | Line | Decision | Rationale |
|---|---|---|---|---|---|
| 1 | `Demon Idol/MDI/Build Animation Template & Overrides` | `Assets/MDI_Characters/Editor/MDI_AnimationTemplateBuilder.cs` | 16 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 2 | `Demon Idol/MDI/Assemble Selected Fighter` | `Assets/MDI_Characters/Editor/MDI_FighterAssembler.cs` | 24 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 3 | `Demon Idol/MDI/Assemble All 18 Fighters` | `Assets/MDI_Characters/Editor/MDI_FighterAssembler.cs` | 37 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 4 | `Demon Idol/MDI/Mobile Import Setup` | `Assets/MDI_Characters/Editor/MDI_MobileImportSetup.cs` | 20 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 5 | `Demon Idol/MDI/Bind Launch Six Animations (Phase 3)` | `Assets/MDI_Characters/Editor/MDI_RosterAnimationBinder.cs` | 730 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 6 | `Demon Idol/MDI/Bind Remaining 12 Animations (Phase 4)` | `Assets/MDI_Characters/Editor/MDI_RosterAnimationBinder.cs` | 750 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 7 | `Demon Idol/MDI/Bind All 18 Roster Animations` | `Assets/MDI_Characters/Editor/MDI_RosterAnimationBinder.cs` | 770 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 8 | `Demon Idol/MDI/Disable Free Slash Realtime Lights` | `Assets/MDI_Characters/Editor/MDI_RosterAnimationBinder.cs` | 789 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 9 | `Demon Idol/MDI/Generate All Roster Definitions` | `Assets/MDI_Characters/Editor/MDI_RosterAssetCreator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 10 | `DemonIdol/BlazBlue/Generate Aika Assets` | `Assets/Scripts/BlazBlue/Editor/BBAikaAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 11 | `Demon Idol/BlazBlue/Generate C01 Akio Complete Assets` | `Assets/Scripts/BlazBlue/Editor/BBAkioAssetGenerator.cs` | 55 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 12 | `Demon Idol/BlazBlue/Run Master Test Suite` | `Assets/Scripts/BlazBlue/Editor/BBAllTestsRunner.cs` | 30 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 13 | `Demon Idol/Animations/Consolidate All 25 Fighter Animations` | `Assets/Scripts/BlazBlue/Editor/BBAnimationConsolidationManager.cs` | 681 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 14 | `Demon Idol/Animations/Execute Verification Suite` | `Assets/Scripts/BlazBlue/Editor/BBAnimationConsolidationManager.cs` | 836 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 15 | `MDI/Execute Arena Cleanup and Convert Rain Shaders` | `Assets/Scripts/BlazBlue/Editor/BBArenaCleanupTool.cs` | 37 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 16 | `Demon Idol/BlazBlue/Box Editor` | `Assets/Scripts/BlazBlue/Editor/BBBoxEditorWindow.cs` | 17 | **KEEP** | KEPT: Authoritative fighter hitbox/hurtbox editor window; routed to Demon Idol/Fighters/Box Editor. |
| 17 | `Demon Idol/BlazBlue/Build/Verify Mobile Certification Baseline` | `Assets/Scripts/BlazBlue/Editor/BBBuildPipeline.cs` | 22 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 18 | `Demon Idol/BlazBlue/Build/Build Android AAB (Headless Test)` | `Assets/Scripts/BlazBlue/Editor/BBBuildPipeline.cs` | 62 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 19 | `Demon Idol/BlazBlue/Build/Build iOS Xcode Project (Headless Test)` | `Assets/Scripts/BlazBlue/Editor/BBBuildPipeline.cs` | 96 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 20 | `MDI/Generate Animation Clip Inventory` | `Assets/Scripts/BlazBlue/Editor/BBClipInventoryTool.cs` | 42 | **KEEP** | KEPT: Authoritative animation clip inventory tool; routed to Demon Idol/Fighters/Clip Inventory. |
| 21 | `Demon Idol/BlazBlue/Generate Cultist Assets` | `Assets/Scripts/BlazBlue/Editor/BBCultistAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 22 | `Demon Idol/🚀 Playability/Assemble 8 External Dota Heroes` | `Assets/Scripts/BlazBlue/Editor/BBDotaHeroAssembler.cs` | 159 | **ARCHIVE** | ARCHIVE: References removed content (Dota / Synty / Sidekick external characters). |
| 23 | `Demon Idol/🎨 Shaders/Unify 16 Dota Heroes & Weapons To Anime Toon` | `Assets/Scripts/BlazBlue/Editor/BBDotaToonUnifier.cs` | 38 | **ARCHIVE** | ARCHIVE: References removed content (Dota / Synty / Sidekick external characters). |
| 24 | `Demon Idol/🚀 Playability/Build 16 Dota Stances & Controllers` | `Assets/Scripts/BlazBlue/Editor/BBDotaWarriorStanceBuilder.cs` | 80 | **ARCHIVE** | ARCHIVE: References removed content (Dota / Synty / Sidekick external characters). |
| 25 | `DemonIdol/BlazBlue/Generate Fina Assets` | `Assets/Scripts/BlazBlue/Editor/BBFinaAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 26 | `Demon Idol/BlazBlue/Run Final Animation Proof (All 25 Fighters)` | `Assets/Scripts/BlazBlue/Editor/BBFinalAnimProofRunner.cs` | 50 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 27 | `MDI/Export Moveset Tracker` | `Assets/Scripts/BlazBlue/Editor/BBMovesetTrackerExporter.cs` | 98 | **KEEP** | KEPT: Authoritative 25-fighter moveset matrix exporter; routed to Demon Idol/Fighters/Export Moveset Tracker. |
| 28 | `DemonIdol/BlazBlue/Generate PicoChan Assets` | `Assets/Scripts/BlazBlue/Editor/BBPicoChanAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 29 | `Demon Idol/Animations/Run Verification Fix Pass V3` | `Assets/Scripts/BlazBlue/Editor/BBPlayModeConsolidationVerifier.cs` | 142 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 30 | `Demon Idol/BlazBlue/Generate Reika Assets` | `Assets/Scripts/BlazBlue/Editor/BBReikaAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 31 | `Demon Idol/BlazBlue/Bind Roster Clips And Avatars` | `Assets/Scripts/BlazBlue/Editor/BBRosterClipBinder.cs` | 18 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 32 | `Demon Idol/BlazBlue/Generate SanpakuChan Assets` | `Assets/Scripts/BlazBlue/Editor/BBSanpakuChanAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 33 | `Demon Idol/BlazBlue/Apply Signature Clips` | `Assets/Scripts/BlazBlue/Editor/BBSignatureClipTool.cs` | 10 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 34 | `Demon Idol/BlazBlue/Generate StellarWitch Assets` | `Assets/Scripts/BlazBlue/Editor/BBStellarWitchAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 35 | `Demon Idol/🚀 Playability/Assemble 8 Sidekick Heroes` | `Assets/Scripts/BlazBlue/Editor/BBSyntyCharacterAssembler.cs` | 179 | **ARCHIVE** | ARCHIVE: References removed content (Dota / Synty / Sidekick external characters). |
| 36 | `DemonIdol/BlazBlue/Generate UnityChan Assets` | `Assets/Scripts/BlazBlue/Editor/BBUnityChanAssetGenerator.cs` | 56 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 37 | `Demon Idol/BlazBlue/Execute Phase 3 (Warrior Attack and BoxTrack Alignment)` | `Assets/Scripts/BlazBlue/Editor/BBWarriorAttackBuilder.cs` | 286 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 38 | `Demon Idol/BlazBlue/Execute Phase 4 (Weapon Visual FX, Audio and Proportions)` | `Assets/Scripts/BlazBlue/Editor/BBWarriorFXBuilder.cs` | 122 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 39 | `Demon Idol/BlazBlue/Execute Master Remap (All 25 Fighters)` | `Assets/Scripts/BlazBlue/Editor/BBWarriorMasterRemapper.cs` | 131 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 40 | `Demon Idol/BlazBlue/Execute Phase 5 (Live Match Validation and Final Polish)` | `Assets/Scripts/BlazBlue/Editor/BBWarriorMatchValidator.cs` | 43 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 41 | `Demon Idol/BlazBlue/Audit Weapon and Character Proportions` | `Assets/Scripts/BlazBlue/Editor/BBWarriorProportionsAuditor.cs` | 11 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 42 | `Demon Idol/BlazBlue/Execute Phase 2 (Locomotion and Stance Overrides)` | `Assets/Scripts/BlazBlue/Editor/BBWarriorStanceOverrideBuilder.cs` | 139 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 43 | `Demon Idol/BlazBlue/Build All Warrior Weapon Prefabs` | `Assets/Scripts/BlazBlue/Editor/BBWarriorWeaponBuilder.cs` | 155 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 44 | `Demon Idol/BlazBlue/Configure All Character Weapon Rigs` | `Assets/Scripts/BlazBlue/Editor/BBWarriorWeaponBuilder.cs` | 258 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 45 | `Demon Idol/BlazBlue/Execute Phase 1 (Weapons and Rigs)` | `Assets/Scripts/BlazBlue/Editor/BBWarriorWeaponBuilder.cs` | 337 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 46 | `Demon Idol/BlazBlue/Run BBAika Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBAikaTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 47 | `Demon Idol/BlazBlue/Run BBAkio Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBAkioTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 48 | `Demon Idol/BlazBlue/Run BBArena Cleanup Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBArenaCleanupTests.cs` | 36 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 49 | `Demon Idol/BlazBlue/Run BBCollision Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBCollisionTests.cs` | 31 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 50 | `Demon Idol/BlazBlue/Run BBCultist Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBCultistTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 51 | `Demon Idol/BlazBlue/Run BBFina Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBFinaTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 52 | `Demon Idol/BlazBlue/Run BBMotionMatcher Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBMotionMatcherTests.cs` | 12 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 53 | `Demon Idol/BlazBlue/Run BBPhase6 Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBPhase6Tests.cs` | 14 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 54 | `Demon Idol/BlazBlue/Run BBPicoChan Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBPicoChanTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 55 | `Demon Idol/BlazBlue/Run BBReika Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBReikaTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 56 | `Demon Idol/BlazBlue/Run BBSanpakuChan Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBSanpakuChanTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 57 | `Demon Idol/BlazBlue/Run BBStellarWitch Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBStellarWitchTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 58 | `Demon Idol/BlazBlue/Run BBSubsystem Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBSubsystemTests.cs` | 12 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 59 | `Demon Idol/BlazBlue/Run BBUnityChan Tests` | `Assets/Scripts/BlazBlue/Tests/Editor/BBUnityChanTests.cs` | 15 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 60 | `Demon Idol/BlazBlue/Record 6 Classic Golden Matches` | `Assets/Scripts/BlazBlue/Tests/Editor/ClassicMatchRecorder.cs` | 236 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 61 | `Demon Idol/BlazBlue/Run Classic Regression Suite` | `Assets/Scripts/BlazBlue/Tests/Editor/ClassicRegressionTests.cs` | 12 | **KEEP** | KEPT: NUnit test runner suite; MenuItem stripped to comply with single DemonIdolMenu root requirement. |
| 62 | `Demon Idol/Security/Apply Local Keystore Configuration` | `Assets/Scripts/Editor/AndroidKeystoreConfig.cs` | 25 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 63 | `Demon Idol/Validate Content Definitions` | `Assets/Scripts/Editor/ContentValidationEditor.cs` | 27 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 64 | `Demon Idol/Build/Build macOS Standalone (Fast Local Test)` | `Assets/Scripts/Editor/DemonIdolAndroidBuildPipeline.cs` | 24 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 65 | `Demon Idol/📱 Build Pipeline/Apply macOS Standalone Settings` | `Assets/Scripts/Editor/DemonIdolAndroidBuildPipeline.cs` | 46 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 66 | `Demon Idol/Build/Build Android APK (Development)` | `Assets/Scripts/Editor/DemonIdolAndroidBuildPipeline.cs` | 70 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 67 | `Demon Idol/Build/Build Android AAB (Google Play Release)` | `Assets/Scripts/Editor/DemonIdolAndroidBuildPipeline.cs` | 92 | **KEEP** | KEPT: Mobile & desktop build pipeline methods; routed to Demon Idol/Build/*. |
| 68 | `Demon Idol/Arena/Build Clean Arcade Arena (C12 Underwear vs C07 StellarWitch)` | `Assets/Scripts/Editor/DemonIdolArcadeArenaBuilder.cs` | 47 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 69 | `Demon Idol/Fix Pass/Execute All ArcadeArena Fixes and Capture` | `Assets/Scripts/Editor/DemonIdolArcadeArenaFixPass.cs` | 42 | **ARCHIVE** | ARCHIVE: One-off migration / phase / fix-pass verification script. |
| 70 | `Demon Idol/🧪 Verification & Audit/Profile Arcade Ladder (2 Fighters)` | `Assets/Scripts/Editor/DemonIdolArcadeProfiler.cs` | 38 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 71 | `Demon Idol/Scenes/Open Universal Fighter Demo Scene` | `Assets/Scripts/Editor/DemonIdolArenaBuilder.cs` | 17 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 72 | `Demon Idol/Setup Battle Arena In Scene` | `Assets/Scripts/Editor/DemonIdolArenaBuilder.cs` | 23 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 73 | `Demon Idol/Setup Flooded Grounds with Imported Characters` | `Assets/Scripts/Editor/DemonIdolArenaBuilder.cs` | 258 | **ARCHIVE** | ARCHIVE: References removed hero / stage content. |
| 74 | `Demon Idol/Deploy Master Armory And Stage System` | `Assets/Scripts/Editor/DemonIdolArmoryBuilder.cs` | 47 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 75 | `Demon Idol/Armory/Deploy All Systems` | `Assets/Scripts/Editor/DemonIdolArmoryBuilder.cs` | 48 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 76 | `Demon Idol/Rebind All Character Assets & Prefabs` | `Assets/Scripts/Editor/DemonIdolAssetFactory.cs` | 13 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 77 | `Demon Idol/Create Default Attack Data Assets` | `Assets/Scripts/Editor/DemonIdolAssetFactory.cs` | 19 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 78 | `Demon Idol/Verification/Capture Current Roster Showcase` | `Assets/Scripts/Editor/DemonIdolCharacterShowcaseCapture.cs` | 9 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 79 | `Demon Idol/Stages/Upgrade College Raimon to Grim Dark Shaders` | `Assets/Scripts/Editor/DemonIdolGrimDarkShaderUpgrader.cs` | 46 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 80 | `Demon Idol/Stages/Revert College Raimon Shaders to Original` | `Assets/Scripts/Editor/DemonIdolGrimDarkShaderUpgrader.cs` | 137 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 81 | `Demon Idol/Stages/Capture Grim Dark Storm Screenshots` | `Assets/Scripts/Editor/DemonIdolGrimDarkShaderUpgrader.cs` | 312 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 82 | `Demon Idol/Home/Setup Demon Idol Game In Home Scene` | `Assets/Scripts/Editor/DemonIdolHomeGameSetup.cs` | 37 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 83 | `Demon Idol/Home/Generate 14 Character & MoveSet Assets` | `Assets/Scripts/Editor/DemonIdolHomeGameSetup.cs` | 90 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 84 | `Demon Idol/Home/Setup Home Scene Properly (Clean & Calibrate)` | `Assets/Scripts/Editor/DemonIdolHomeSceneProperSetup.cs` | 39 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 85 | `Demon Idol/Characters/Apply Suspense Chromatic Aberration & Remove Outlines` | `Assets/Scripts/Editor/DemonIdolJujutsuAesthetics.cs` | 18 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 86 | `Demon Idol/Characters/Verify Jujutsu Aesthetics In Scene` | `Assets/Scripts/Editor/DemonIdolJujutsuAesthetics.cs` | 260 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 87 | `Demon Idol/Characters/Apply Jujutsu & Bleach Aesthetics to All 14 Characters` | `Assets/Scripts/Editor/DemonIdolJujutsuAesthetics.cs` | 338 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 88 | `Demon Idol/Play Match in Editor` | `Assets/Scripts/Editor/DemonIdolMainMenuBuilder.cs` | 54 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 89 | `Demon Idol/Scenes/Open Main Menu Scene` | `Assets/Scripts/Editor/DemonIdolMainMenuBuilder.cs` | 60 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 90 | `Demon Idol/Scenes/Open Arcade Arena Scene` | `Assets/Scripts/Editor/DemonIdolMainMenuBuilder.cs` | 66 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 91 | `Demon Idol/Scenes/Build Launch Main Menu Scene` | `Assets/Scripts/Editor/DemonIdolMainMenuBuilder.cs` | 72 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 92 | `Demon Idol/Run Full Automated Pipeline` | `Assets/Scripts/Editor/DemonIdolMasterAutomator.cs` | 53 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 93 | `Demon Idol/🚀 Playability/Master Grounding, Avatar & Animation Fix (All 14)` | `Assets/Scripts/Editor/DemonIdolMasterGroundingAndAvatarHook.cs` | 23 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 94 | `Demon Idol/⚡ Run Full Automated Pipeline` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 26 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 95 | `Demon Idol/🎮 Play Match in Editor` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 32 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 96 | `Demon Idol/🎬 Scenes/Open Main Menu Scene (Build Index 0)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 41 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 97 | `Demon Idol/🎬 Scenes/Open Arcade Arena Scene (Build Index 1)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 47 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 98 | `Demon Idol/🎬 Scenes/Main Menu & UI Orchestrator Studio` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 53 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 99 | `Demon Idol/🎬 Scenes/Rebuild Main Menu Scene` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 59 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 100 | `Demon Idol/🎬 Scenes/Rebuild Arcade Arena Scene` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 65 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 101 | `Demon Idol/🎬 Scenes/Deploy College Raimon to All 3 Scenes` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 71 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 102 | `Demon Idol/👥 Roster & Animators/Fighter & Asset Customization Studio` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 80 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 103 | `Demon Idol/👥 Roster & Animators/Hook All 14 Playable Fighters` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 86 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 104 | `Demon Idol/👥 Roster & Animators/Rebind All Character Assets & Prefabs` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 92 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 105 | `Demon Idol/👥 Roster & Animators/Validate 14-Fighter MoveSets` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 98 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 106 | `Demon Idol/👥 Roster & Animators/Apply Anime Toon Aesthetics to Characters` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 104 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 107 | `Demon Idol/👥 Roster & Animators/Rebuild Character Costumes (Uniform Style)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 110 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 108 | `Demon Idol/👥 Roster & Animators/Strip All Props & Weapons from Characters` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 116 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 109 | `Demon Idol/👥 Roster & Animators/Unify 16 Dota Heroes & Weapons to Anime Toon` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 122 | **ARCHIVE** | ARCHIVE: References removed content (Dota / Synty / Sidekick external characters). |
| 110 | `Demon Idol/⚔️ Combat & Stages/Stage & Arena Customization Studio` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 131 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 111 | `Demon Idol/⚔️ Combat & Stages/Deploy Master Armory & Stage Systems` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 137 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 112 | `Demon Idol/⚔️ Combat & Stages/Build All 6 Stages and SF6 Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 143 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 113 | `Demon Idol/⚔️ Combat & Stages/Create Default Attack Data Assets` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 149 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 114 | `Demon Idol/🎥 Cameras/Setup All 7 Stage Cameras (College Raimon)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 155 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 115 | `Demon Idol/🎥 Cameras/Preview ST01 Central Courtyard Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 161 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 116 | `Demon Idol/🎥 Cameras/Preview ST02 Sakura Main Gate Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 167 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 117 | `Demon Idol/🎥 Cameras/Preview ST03 Rooftop Observatory Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 173 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 118 | `Demon Idol/🎥 Cameras/Preview ST04 Martial Arts Dojo Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 179 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 119 | `Demon Idol/🎥 Cameras/Preview ST05 Athletic Stadium Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 185 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 120 | `Demon Idol/🎥 Cameras/Preview ST06 Transit Promenade Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 191 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 121 | `Demon Idol/🎥 Cameras/Preview ST07 Midnight Courtyard Camera` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 197 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 122 | `Demon Idol/🌐 Multiplayer & Quantum/Configure Photon Quantum` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 206 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 123 | `Demon Idol/🌐 Multiplayer & Quantum/Wire Quantum In All Scenes` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 212 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 124 | `Demon Idol/📱 Build Pipeline/Build macOS Standalone (Fast Local Test)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 221 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 125 | `Demon Idol/📱 Build Pipeline/Build Android APK (Development)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 227 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 126 | `Demon Idol/📱 Build Pipeline/Build Android AAB (Google Play Release)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 233 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 127 | `Demon Idol/📱 Build Pipeline/Apply Local Keystore Configuration` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 239 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 128 | `Demon Idol/🧪 Verification & Audit/Run Match Simulation Verification Suite (374 Tests)` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 248 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 129 | `Demon Idol/🧪 Verification & Audit/Validate Content Definitions` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 254 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 130 | `Demon Idol/🧪 Verification & Audit/Verify Cutscenes & Media Files` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 260 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 131 | `Demon Idol/🧪 Verification & Audit/Capture Showcase Screenshot` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 266 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 132 | `Demon Idol/🧪 Verification & Audit/Capture Online Match Screenshots` | `Assets/Scripts/Editor/DemonIdolMasterMenu.cs` | 272 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 133 | `Demon Idol/🎬 Scenes/Main Menu & UI Orchestrator Studio` | `Assets/Scripts/Editor/DemonIdolMenuOrchestratorWindow.cs` | 76 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 134 | `Demon Idol/BlazBlue/Capture Online Match Screens` | `Assets/Scripts/Editor/DemonIdolOnlineMatchCapture.cs` | 19 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 135 | `Demon Idol/🧪 Verification & Audit/Run Online Mock Test` | `Assets/Scripts/Editor/DemonIdolOnlineMockTest.cs` | 33 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 136 | `Demon Idol/Polish/1. Polish All Clan Materials & Shaders` | `Assets/Scripts/Editor/DemonIdolPolishDirector.cs` | 22 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 137 | `Demon Idol/Polish/2. Polish SampleScene Characters & Environment` | `Assets/Scripts/Editor/DemonIdolPolishDirector.cs` | 167 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 138 | `Demon Idol/Polish/3. Polish UniversalFighter_Demo Scene` | `Assets/Scripts/Editor/DemonIdolPolishDirector.cs` | 173 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 139 | `Demon Idol/Polish/4. Run Full Batch Polish & Audit` | `Assets/Scripts/Editor/DemonIdolPolishDirector.cs` | 179 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 140 | `Demon Idol/🚀 Playability/Connect All Character Prefab Scripts` | `Assets/Scripts/Editor/DemonIdolPrefabScriptConnector.cs` | 158 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 141 | `Demon Idol/Quantum/Configure Photon Quantum` | `Assets/Scripts/Editor/DemonIdolQuantumSetup.cs` | 13 | **ARCHIVE** | ARCHIVE: Deprecated networking test/setup script. |
| 142 | `Demon Idol/Quantum/Wire Quantum In All Scenes` | `Assets/Scripts/Editor/DemonIdolQuantumSetup.cs` | 82 | **ARCHIVE** | ARCHIVE: Deprecated networking test/setup script. |
| 143 | `Demon Idol/BlazBlue/Print Roster Proof Table` | `Assets/Scripts/Editor/DemonIdolRosterProof.cs` | 23 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 144 | `Demon Idol/Build Stage and Fighters (Kai, Arin & Whispering Grove)` | `Assets/Scripts/Editor/DemonIdolStageAndFighterBuilder.cs` | 36 | **ARCHIVE** | ARCHIVE: References removed hero / stage content. |
| 145 | `Demon Idol/Capture In-Game Screenshot` | `Assets/Scripts/Editor/DemonIdolStageAndFighterBuilder.cs` | 42 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 146 | `Demon Idol/⚔️ Combat & Stages/Stage & Arena Customization Studio` | `Assets/Scripts/Editor/DemonIdolStageCustomizerWindow.cs` | 68 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 147 | `Demon Idol/Stages/Build All 6 Stages and SF6 Camera` | `Assets/Scripts/Editor/DemonIdolStageDirector.cs` | 59 | **ARCHIVE** | ARCHIVE: Deprecated legacy menu / orchestrator replaced by unified DemonIdolMenu and scene builders. |
| 148 | `Demon Idol/Characters/Inspect Home Scene Hierarchy` | `Assets/Scripts/Editor/DemonIdolToonUnifier.cs` | 32 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 149 | `Demon Idol/Characters/Toonify & Fix All 14 Characters in Home Scene` | `Assets/Scripts/Editor/DemonIdolToonUnifier.cs` | 75 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 150 | `Demon Idol/Build Complete Master UI Canvas (With Roster & Battle HUD)` | `Assets/Scripts/Editor/DemonIdolUIBuilder.cs` | 84 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 151 | `Demon Idol/Prefabs/Build All Character and Stage Prefabs` | `Assets/Scripts/Editor/DemonIdolUIBuilder.cs` | 1531 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 152 | `Demon Idol/Shaders/Apply 90s Anime Aesthetics to Character Materials` | `Assets/Scripts/Editor/DemonIdolUIBuilder.cs` | 1985 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 153 | `Demon Idol/Shaders/Capture Showcase Character Image` | `Assets/Scripts/Editor/DemonIdolUIBuilder.cs` | 2055 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 154 | `Demon Idol/Costumes/Focus Characters Folder` | `Assets/Scripts/Editor/DemonIdolUniformCustomizer.cs` | 15 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 155 | `Demon Idol/Costumes/Rebuild All Character Costumes (Trending Uniform Style)` | `Assets/Scripts/Editor/DemonIdolUniformCustomizer.cs` | 27 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 156 | `Demon Idol/Aesthetics/Apply Valorant Post-Processing & Fine-Tune Characters` | `Assets/Scripts/Editor/DemonIdolValorantAesthetics.cs` | 194 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 157 | `Demon Idol/Aesthetics/Capture Game View With Post-Processing` | `Assets/Scripts/Editor/DemonIdolValorantAesthetics.cs` | 236 | **ARCHIVE** | ARCHIVE: Deprecated visual/aesthetic override script. |
| 158 | `Demon Idol/Narrator & Cutscenes/Play Game Intro (Demon Lord)` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 15 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 159 | `Demon Idol/Narrator & Cutscenes/Play Daily Routine (Demon Lord)` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 29 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 160 | `Demon Idol/Narrator & Cutscenes/Play Commentary to Camera` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 43 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 161 | `Demon Idol/Narrator & Cutscenes/Play Soul Drain (Domain Expansion)` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 57 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 162 | `Demon Idol/Narrator & Cutscenes/Play Dark Energy Blast (Demon Beast)` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 71 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 163 | `Demon Idol/Narrator & Cutscenes/Play Game Over (Defeat)` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 85 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 164 | `Demon Idol/Narrator & Cutscenes/Verify All Media Files Exist` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 99 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 165 | `Demon Idol/Narrator & Cutscenes/Log Announcer Roster Commentary` | `Assets/Scripts/Editor/DemonLordNarratorEditor.cs` | 147 | **ARCHIVE** | ARCHIVE: Deprecated narrator/cutscene trigger script. |
| 166 | `Demon Idol/Combat/Validate 14-Fighter MoveSets` | `Assets/Scripts/Editor/FighterMoveSetValidator.cs` | 38 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 167 | `Demon Idol/Characters/Rebuild Variant Prefabs From Recipe` | `Assets/Scripts/Editor/MDICharacterVariantBuilder.cs` | 20 | **ARCHIVE** | ARCHIVE: References removed hero / stage content. |
| 168 | `Demon Idol/Animation/Rebuild KI Melee Controller` | `Assets/Scripts/Editor/MDIKIMeleeControllerBuilder.cs` | 27 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 169 | `Demon Idol/Animations/Deploy Pure Mecanim Humanoid Clips` | `Assets/Scripts/Editor/MDI_PureHumanoidClipDeployer.cs` | 43 | **ARCHIVE** | ARCHIVE: Deprecated moveset/asset generator; authoritative serialized assets exist in CombatData. |
| 170 | `Demon Idol/Verify/Match Loop` | `Assets/Scripts/Editor/MatchLoopVerifier.cs` | 29 | **KEEP** | KEPT: Updated per Step 5 and routed to Demon Idol/Verify/Match Loop. |
| 171 | `Demon Idol/Run Match & State Verification Suite` | `Assets/Scripts/Editor/MatchSimulationVerificationTest.cs` | 37 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 172 | `Demon Idol/🧪 Verification & Audit/Probe Real Photon Cloud Connection` | `Assets/Scripts/Editor/RealPhotonConnectionProbe.cs` | 14 | **ARCHIVE** | ARCHIVE: Deprecated networking test/setup script. |
| 173 | `Demon Idol/Audit All Three Scenes` | `Assets/Scripts/Editor/SceneDiagnosticAuditor.cs` | 37 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |
| 174 | `Demon Idol/Fix All Dangling References Across Scenes` | `Assets/Scripts/Editor/SceneDiagnosticAuditor.cs` | 348 | **ARCHIVE** | ARCHIVE: Deprecated legacy editor tool replaced by clean rebuild architecture. |

## New Unified Menu Architecture (`DemonIdolMenu.cs`)

```
Demon Idol/
  Scenes/Build Main Menu
  Scenes/Build Character Select
  Scenes/Build Battle Arena (College Raimon)
  Scenes/Build All Three + Set Build Order
  Play/Play From Main Menu
  Verify/Match Loop                 (via MatchLoopVerifier)
  Verify/Scene Integrity            (via BattleArenaSceneIntegrityTest + Guard Tests)
  Verify/Run All EditMode Tests     (via BBAllTestsRunner)
  Fighters/Box Editor               (via BBBoxEditorWindow)
  Fighters/Export Moveset Tracker   (via BBMovesetTrackerExporter)
  Fighters/Clip Inventory           (via BBClipInventoryTool)
  Build/Mac App
  Build/iOS Xcode Project
  Build/Android AAB
```
