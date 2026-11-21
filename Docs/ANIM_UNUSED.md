# Unreferenced Animation Assets & Unused Packs Audit

**Project:** *My Demon Idol* (BlazBlue Combat Path)  
**Specification:** Unified Animation Consolidation — Step 6 Cleanup & Audit  
**Branch:** `anim-consolidation`  

---

## 1. Executive Summary

Following successful completion of **Step 5 Verification** (wherein all 25 fighters in the BlazBlue roster passed full Play Mode simulation, 0 T-pose, 0 foot sliding, 6 mid-attack screenshots verified, and all 122 EditMode NUnit tests passed with 0 console errors), an exhaustive audit of animation controllers, override assets, and animation packs outside the approved 11 consolidation packs was conducted across the entire repository.

In strict accordance with Step 6 safety constraints:
- **No models, avatars, FBX files, mocap libraries, or assets used by menus/cutscenes were deleted.**
- **Only unreferenced animator controllers outside the approved packs were deleted.**
- All candidate unused animation packs are inventoried below with exact sizes for user decision.

---

## 2. Deleted Unreferenced Animator Controllers

The following **30 animator controllers** outside the approved packs had **zero references** across all scenes (`Assets/Scenes/`), character prefabs (`Assets/Prefabs/`), scriptable objects, UI definitions, and code. These controllers have been safely deleted along with their `.meta` tracking files.

| # | File Path | Asset Type | Size (Bytes) | Reference Count | Status |
|:---:|---|:---:|:---:|:---:|:---:|
| 1 | `Assets/Stylized3DMonster/Monster07_FreeTrial/Anim/Monster07_AC.controller` | Animator Controller | 4,129 | 0 | **DELETED** |
| 2 | `Assets/Stylized3DMonster/Monster08_FreeTrial/Anim/Monster08_AC.controller` | Animator Controller | 4,139 | 0 | **DELETED** |
| 3 | `Assets/Stylized3DMonster/Monster09_FreeTrial/Anim/Monster09_AC.controller` | Animator Controller | 4,129 | 0 | **DELETED** |
| 4 | `Assets/Stylized3DMonster/Monster10_FreeTrial/Anim/Monster10_AC.controller` | Animator Controller | 4,137 | 0 | **DELETED** |
| 5 | `Assets/Stylized3DMonster/Monster11_FreeTrial/Anim/Monster11_AC.controller` | Animator Controller | 4,138 | 0 | **DELETED** |
| 6 | `Assets/Stylized3DMonster/Monster12_FreeTrial/Anim/Monster12_AC.controller` | Animator Controller | 4,135 | 0 | **DELETED** |
| 7 | `Assets/Stylized3DMonster/Monster13_FreeTrial/Anim/Monster13_AC.controller` | Animator Controller | 4,135 | 0 | **DELETED** |
| 8 | `Assets/Stylized3DMonster/Monster14_FreeTrial/Anim/Monster14_AC.controller` | Animator Controller | 4,135 | 0 | **DELETED** |
| 9 | `Assets/Stylized3DMonster/Monster15_FreeTrial/Anim/Monster15_AC.controller` | Animator Controller | 4,157 | 0 | **DELETED** |
| 10 | `Assets/Stylized3DMonster/Monster16_FreeTrial/Anim/Monster16_AC.controller` | Animator Controller | 4,144 | 0 | **DELETED** |
| 11 | `Assets/Stylized3DMonster/Monster17_FreeTrial/Anim/Monster17_AC.controller` | Animator Controller | 4,139 | 0 | **DELETED** |
| 12 | `Assets/Stylized3DMonster/Monster18_FreeTrial/Anim/Monster18_AC.controller` | Animator Controller | 4,140 | 0 | **DELETED** |
| 13 | `Assets/Stylized3DMonster/Monster19_FreeTrial/Anim/Monster19_AC.controller` | Animator Controller | 4,132 | 0 | **DELETED** |
| 14 | `Assets/Stylized3DMonster/Monster20_FreeTrial/Anim/Monster20_AC.controller` | Animator Controller | 4,132 | 0 | **DELETED** |
| 15 | `Assets/Stylized3DMonster/Monster21_FreeTrial/Anim/Monster21_AC.controller` | Animator Controller | 4,145 | 0 | **DELETED** |
| 16 | `Assets/Stylized3DMonster/Monster22_FreeTrial/Anim/Monster22_AC.controller` | Animator Controller | 4,148 | 0 | **DELETED** |
| 17 | `Assets/Stylized3DMonster/Monster23_FreeTrial/Anim/Monster23_AC.controller` | Animator Controller | 4,137 | 0 | **DELETED** |
| 18 | `Assets/Stylized3DMonster/Monster24_FreeTrial/Anim/Monster24_AC.controller` | Animator Controller | 4,134 | 0 | **DELETED** |
| 19 | `Assets/Stylized3DMonster/Monster25_FreeTrial/Anim/Monster25_AC.controller` | Animator Controller | 4,136 | 0 | **DELETED** |
| 20 | `Assets/Stylized3DMonster/Monster26_FreeTrial/Anim/Monster26_AC.controller` | Animator Controller | 4,126 | 0 | **DELETED** |
| 21 | `Assets/Stylized3DMonster/Monster27_FreeTrial/Anim/Monster27_AC.controller` | Animator Controller | 4,152 | 0 | **DELETED** |
| 22 | `Assets/Stylized3DMonster/Monster28_FreeTrial/Anim/Monster28_AC.controller` | Animator Controller | 4,153 | 0 | **DELETED** |
| 23 | `Assets/Stylized3DMonster/Monster29_FreeTrial/Anim/Monster29_AC.controller` | Animator Controller | 4,140 | 0 | **DELETED** |
| 24 | `Assets/Stylized3DMonster/Monster30_FreeTrial/Anim/Monster30_AC.controller` | Animator Controller | 4,139 | 0 | **DELETED** |
| 25 | `Assets/Stylized3DMonster/Monster31_FreeTrial/Anim/Monster31_AC.controller` | Animator Controller | 4,141 | 0 | **DELETED** |
| 26 | `Assets/Stylized3DMonster/Monster32_FreeTrial/Anim/Monster32_AC.controller` | Animator Controller | 4,143 | 0 | **DELETED** |
| 27 | `Assets/Stylized3DMonster/Monster33_FreeTrial/Anim/Monster33_AC.controller` | Animator Controller | 4,152 | 0 | **DELETED** |
| 28 | `Assets/Stylized3DMonster/Monster34_FreeTrial/Anim/Monster34_AC.controller` | Animator Controller | 4,147 | 0 | **DELETED** |
| 29 | `Assets/Stylized3DMonster/Monster35_FreeTrial/Anim/Monster35_AC.controller` | Animator Controller | 4,165 | 0 | **DELETED** |
| 30 | `Assets/Stylized3DMonster/Monster36_FreeTrial/Anim/Monster36_AC.controller` | Animator Controller | 4,150 | 0 | **DELETED** |

*Total Unreferenced Controller Footprint Reclaimed:* **124,204 bytes (~124 KB)**.

---

## 3. Retained Controllers (Active in Classic Mode, UI, or Demos)

The following non-approved controllers were **retained** because they are actively referenced by Classic mode character definitions, showcase scenes, or demo environments:
- `Assets/AnimeGirls/BaseCharacter/Demo files/AC.controller`: Referenced by `Assets/Scenes/MainMenu.unity` and casual prefabs.
- `Assets/3d-character_animeGirlAkane/DemoScene/Animation/AnimationController.controller`: Referenced by `SampleScene_Akane.unity` and `CharacterDefinition_C05_Akane.asset` (Classic mode).
- `Assets/Kiki/Xmas Girl/Animator/Animator Controller.controller`: Referenced by `CharacterDefinition_C10_Reika.asset` (Classic mode).
- `Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/finna.controller`: Referenced by `FinaShowCase.unity` and `CharacterDefinition_C12_Fina.asset` (Classic mode).
- `Assets/unity-chan!/Unity-chan! Model/.../UnityChanLocomotions.controller`: Referenced by UnityChan demo scenes.
- `Assets/MDI_Characters/Runtime/AnimatorOverride_C01_Akio.overrideController`: Referenced by `CharacterDefinition_C01_Akio.asset` (Classic mode).
- `Assets/MDI_Characters/Clans/.../AnimatorOverride_*.overrideController`: Clan character definitions (Ash, Demon, Medieval, Neon, Occult, Orc).

---

## 4. Pack Dependency Audit & Status (Step 6 Verification Fix Pass)

> [!IMPORTANT]
> **No packs are deleted this round.** All candidate packs remain on disk awaiting explicit user decision. An exhaustive recheck of all 4 candidate packs for prefab, model, material, and code dependencies was conducted:

| Candidate Pack | Directory Path | Size | Dependency Recheck Findings | Classification & Status |
|---|---|:---:|---|---|
| **Bionic Cartoon Zombie Gorilla** | `Assets/GAMWILL Character Pack Monster  Bionic Cartoon Zombie Gorilla` | **39 MB** | **REQUIRED FOR EX01 HUGO**: Provides the primary 3D mesh (`HUGO_T_Pose.fbx`), skeleton, materials (`HUGO_DIFFUSE.mat`, `HUGO_NORMAL.mat`), and prefab components for fighter `EX01 Hugo`. Deleting this pack would break EX01 Hugo's model and rendering. | **REQUIRED** — **DO NOT DELETE** |
| **SimpleAssets Guard Pack** | `Assets/SimpleAssets` | **222 MB** | **REQUIRED FOR EX06 SENTRY**: Provides the 3D character mesh (`Guard_Rig.fbx`), weapon models, and mobile materials (`Guard_Yellow (Mobile).mat`, `Guard_Weapons_Yellow (Mobile).mat`) for fighter `EX06 Sentry` (`EX06_Sentry.prefab`, `CharacterDefinition_EX06_Sentry.asset`). | **REQUIRED** — **DO NOT DELETE** |
| **Stylized 3D Monster Pack** | `Assets/Stylized3DMonster` | **569 MB** | **Zero external references**: No scenes, prefabs, materials, or scripts reference any asset in this directory. 30 unreferenced controllers were cleaned up. Ready for safe removal when user decides. | **Unreferenced / Safe to Delete** (Awaiting user decision) |
| **Huge Mocap Library** | `Assets/Huge Mocap Library` | **2.7 GB** | **Zero active model/material references**: All 25 fighter animation sets have been migrated to the 11 approved packs. All editor tools (`BB*AssetGenerator`, `BBRosterClipBinder`, `BBSignatureClipTool`, `CreditsUI`) have been purged of mocap paths. Ready for safe removal when user decides. | **Unreferenced / Safe to Delete** (Awaiting user decision) |
