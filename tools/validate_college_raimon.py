#!/usr/bin/env python3
"""
tools/validate_college_raimon.py

Comprehensive test suite validating College Raimon integration:
1. Legal Compliance & Licensing
2. Asset & Prefab Integrity (CollegeRaimon.prefab & ST07_CollegeRaimon.prefab)
3. Code Registries (StageRegistry, MainMenuController, BBArcadeDirector, etc.)
4. Scene 1 (MainMenu.unity): Campus backdrop, 7 stage cards, 7 StageButtons/Highlights
5. Scene 2 (ArcadeArena.unity): ST07_CollegeRaimon Backdrop_Diorama under Arcade_Stage_Root
6. Scene 3 (RosterShowcase.unity): CollegeRaimon_Backdrop under Pedestal_Environment & Showcase_Director
"""

import os
import sys
import re

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT_DIR)

PASS_COUNT = 0
FAIL_COUNT = 0

def check(condition, message):
    global PASS_COUNT, FAIL_COUNT
    if condition:
        print(f"  [PASS] {message}")
        PASS_COUNT += 1
    else:
        print(f"  [FAIL] {message}")
        FAIL_COUNT += 1


def validate_licensing():
    print("\n--- [1/6] Validating Legal Compliance & Licensing ---")
    credits_path = "Assets/Scripts/UI/CreditsUI.cs"
    legal_path = "Assets/Scripts/UI/LegalComplianceUI.cs"

    check(os.path.exists(credits_path), f"Found {credits_path}")
    if os.path.exists(credits_path):
        with open(credits_path, "r", encoding="utf-8") as f:
            c_text = f.read()
        check("BuildVerse" in c_text and "College Raimon" in c_text and "Unity Asset Store EULA" in c_text,
              "CreditsUI.cs includes BuildVerse 'College Raimon' Unity Asset Store EULA attribution")

    check(os.path.exists(legal_path), f"Found {legal_path}")
    if os.path.exists(legal_path):
        with open(legal_path, "r", encoding="utf-8") as f:
            l_text = f.read()
        check("BuildVerse: College Raimon" in l_text and "Unity Asset Store EULA" in l_text,
              "LegalComplianceUI.cs includes BuildVerse College Raimon commercial licensor terms")


def validate_assets():
    print("\n--- [2/6] Validating Asset & Stage Prefab Integrity ---")
    raimon_prefab = "Assets/CollegeRaimon/Prefabs/CollegeRaimon.prefab"
    st07_prefab = "Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab"
    st07_asset = "Assets/CombatData/Stages/ST07_ST07_CollegeRaimon.asset"

    check(os.path.exists(raimon_prefab), f"Found {raimon_prefab}")
    check(os.path.exists(st07_prefab), f"Found {st07_prefab}")
    check(os.path.exists(st07_asset), f"Found {st07_asset}")

    # Check prefab meta GUIDs
    with open(raimon_prefab + ".meta", "r") as f:
        check("guid: e74149b2c38522a49903fabd76c6afe7" in f.read(), "CollegeRaimon.prefab matches expected GUID")

    with open(st07_prefab + ".meta", "r") as f:
        check("guid: d707ca11e9e041bb848696298f2a0cb7" in f.read(), "ST07_CollegeRaimon.prefab matches expected GUID")

    with open(st07_asset + ".meta", "r") as f:
        check("guid: e707ca11e9e041bb848696298f2a0cb8" in f.read(), "ST07_ST07_CollegeRaimon.asset matches expected GUID")

    # Check stage prefab structure
    with open(st07_prefab, "r", encoding="utf-8") as f:
        st_text = f.read()
    check("Ground_CombatPlatform" in st_text, "ST07_CollegeRaimon has Ground_CombatPlatform")
    check("Boundary_LeftWall" in st_text and "Boundary_RightWall" in st_text, "ST07_CollegeRaimon has left & right boundaries")
    check("SpawnPoint_P1" in st_text and "SpawnPoint_P2" in st_text, "ST07_CollegeRaimon has P1 & P2 spawn points")
    check("Environment_BackdropDiorama" in st_text, "ST07_CollegeRaimon nests backdrop diorama")
    check("Stage_AccentLight" in st_text, "ST07_CollegeRaimon has sakura cherry blossom accent light")


def validate_code_registries():
    print("\n--- [3/6] Validating Code & Registry Integrations ---")
    with open("Assets/Scripts/Data/StageRegistry.cs", "r", encoding="utf-8") as f:
        sr_text = f.read()
    check("ST07" in sr_text and "College Raimon" in sr_text, "StageRegistry registers ST07 / College Raimon")
    check("Prefabs/Stages/ST07_CollegeRaimon" in sr_text, "StageRegistry references ST07_CollegeRaimon prefab")

    with open("Assets/Scripts/UI/MainMenuController.cs", "r", encoding="utf-8") as f:
        mc_text = f.read()
    check('"ST07"' in mc_text, "MainMenuController StageIds includes ST07")
    check("COLLEGE RAIMON" in mc_text, "MainMenuController StageNames includes COLLEGE RAIMON")

    with open("Assets/Scripts/BlazBlue/Launch/BBArcadeDirector.cs", "r", encoding="utf-8") as f:
        bb_text = f.read()
    check('"ST07"' in bb_text, "BBArcadeDirector associates arcade stage with ST07")

    with open("Assets/Scripts/Combat/HomegrownCombatDirector.cs", "r", encoding="utf-8") as f:
        hc_text = f.read()
    check('"ST07"' in hc_text, "HomegrownCombatDirector handles ST07 dynamic backdrop loading")


def validate_main_menu_scene():
    print("\n--- [4/6] Validating Scene 1: Assets/Scenes/MainMenu.unity ---")
    scene_path = "Assets/Scenes/MainMenu.unity"
    with open(scene_path, "r", encoding="utf-8") as f:
        text = f.read()

    check("CollegeRaimon_Environment" in text or "CollegeRaimon" in text, "MainMenu.unity has College Raimon campus backdrop")
    
    # 7 stage cards
    for i in range(7):
        check(f"Card_Stage_{i:02d}" in text, f"MainMenu.unity contains Card_Stage_{i:02d}")

    check("ST07: COLLEGE RAIMON" in text, "MainMenu.unity contains ST07: COLLEGE RAIMON label")

    # Check StageButtons count in MainMenuController
    m_btns = re.search(r"StageButtons:\n((?:  - \{fileID: \d+\}\n)+)", text)
    if m_btns:
        btn_count = len(re.findall(r"- \{fileID: \d+\}", m_btns.group(1)))
        check(btn_count == 7, f"MainMenuController has {btn_count}/7 StageButtons wired")
    else:
        check(False, "Could not find StageButtons in MainMenuController")

    # Check StageHighlights count in MainMenuController
    m_hls = re.search(r"StageHighlights:\n((?:  - \{fileID: \d+\}\n)+)", text)
    if m_hls:
        hl_count = len(re.findall(r"- \{fileID: \d+\}", m_hls.group(1)))
        check(hl_count == 7, f"MainMenuController has {hl_count}/7 StageHighlights wired")
    else:
        check(False, "Could not find StageHighlights in MainMenuController")


def validate_arcade_arena_scene():
    print("\n--- [5/6] Validating Scene 2: Assets/Scenes/ArcadeArena.unity ---")
    scene_path = "Assets/Scenes/ArcadeArena.unity"
    with open(scene_path, "r", encoding="utf-8") as f:
        text = f.read()

    check("Arcade_Stage_Root" in text, "ArcadeArena.unity contains Arcade_Stage_Root")
    check("Backdrop_Diorama" in text, "ArcadeArena.unity contains Backdrop_Diorama")
    check("d707ca11e9e041bb848696298f2a0cb7" in text, "ArcadeArena.unity Backdrop_Diorama references ST07_CollegeRaimon.prefab GUID")
    check("Ground_CombatPlatform" in text, "ArcadeArena.unity has Ground_CombatPlatform")
    check("Boundary_LeftWall" in text and "Boundary_RightWall" in text, "ArcadeArena.unity has left and right wall boundaries")


def validate_roster_showcase_scene():
    print("\n--- [6/6] Validating Scene 3: Assets/Scenes/RosterShowcase.unity ---")
    scene_path = "Assets/Scenes/RosterShowcase.unity"
    with open(scene_path, "r", encoding="utf-8") as f:
        text = f.read()

    check("Pedestal_Environment" in text, "RosterShowcase.unity contains Pedestal_Environment")
    check("CollegeRaimon_Backdrop" in text, "RosterShowcase.unity contains CollegeRaimon_Backdrop")
    check("e74149b2c38522a49903fabd76c6afe7" in text, "RosterShowcase.unity CollegeRaimon_Backdrop references CollegeRaimon.prefab GUID")
    check("Showcase_Director" in text, "RosterShowcase.unity contains Showcase_Director")
    check("MDI_RosterShowcaseController" in text, "RosterShowcase.unity attaches MDI_RosterShowcaseController")
    check("5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f" in text, "RosterShowcase.unity references MDI_RosterShowcaseController script GUID")
    check("1431690367" in text, "RosterShowcase.unity wires Main Camera to showcaseCamera")


def main():
    print("=============================================================")
    print("  MY DEMON IDOL: COLLEGE RAIMON 3-SCENE VALIDATION SUITE     ")
    print("  Workspace: /Users/amber/My Demon Idol                      ")
    print("=============================================================")

    validate_licensing()
    validate_assets()
    validate_code_registries()
    validate_main_menu_scene()
    validate_arcade_arena_scene()
    validate_roster_showcase_scene()

    print("\n=============================================================")
    print(f"  RESULTS: {PASS_COUNT} PASSED, {FAIL_COUNT} FAILED")
    print("=============================================================")

    if FAIL_COUNT == 0:
        print("  >>> ALL COLLEGE RAIMON TESTS PASSED 100% (ZERO DEFECTS) <<<")
        sys.exit(0)
    else:
        print("  >>> SOME TESTS FAILED. PLEASE FIX DEFECTS ABOVE. <<<")
        sys.exit(1)


if __name__ == "__main__":
    main()
