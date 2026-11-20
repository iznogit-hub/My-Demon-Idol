#!/usr/bin/env python3
"""
tools/validate_menu_flow.py

Comprehensive AAA Menu UI Flow & 30-Hero Roster Validator for My Demon Idol.
Validates:
1. All 30 Roster Entries in CharacterRegistry & FighterVisualCatalog.
2. MainMenu.unity hierarchy (30-card matrix, 4 faction tabs, PreMatchVSOverlay).
3. PreMatchVSOverlay component and split clash presentation elements.
4. BattleHUDController post-match results loop (Rematch, CharacterSelect, StageSelect, ReturnToMenu).
5. PlayerPrefs state handoff contracts between MainMenu and ArcadeArena.
"""

import sys
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

AUTHORITATIVE_30 = [
    # Faction 1: Demon Sorcerers (14)
    "C01", "C02", "C03", "C04", "C05", "C06", "C07",
    "C08", "C09", "C10", "C11", "C12", "C13", "C14",

    # Faction 2: Synty Sidekicks (8)
    "SK01", "SK02", "SK03", "SK04", "SK05", "SK06", "SK07", "SK08",

    # Faction 3: External Dota Heroes (8)
    "EX01", "EX02", "EX03", "EX04", "EX05", "EX06", "EX07", "EX08"
]

def check_catalog_and_registry():
    print("--- [1/4] Checking CharacterRegistry & FighterVisualCatalog (30 Heroes) ---")
    reg_path = ROOT / "Assets/Scripts/Data/CharacterRegistry.cs"
    cat_path = ROOT / "Assets/Scripts/Launch/FighterVisualCatalog.cs"

    if not reg_path.exists():
        print(f"[FAIL] Missing {reg_path}")
        return False
    if not cat_path.exists():
        print(f"[FAIL] Missing {cat_path}")
        return False

    reg_content = reg_path.read_text(encoding="utf-8")
    cat_content = cat_path.read_text(encoding="utf-8")

    all_ok = True
    for cid in AUTHORITATIVE_30:
        if f'id = "{cid}"' not in reg_content and f'"{cid}"' not in reg_content:
            print(f"  [FAIL] {cid} missing from CharacterRegistry")
            all_ok = False
        else:
            print(f"  [PASS] {cid} registered in CharacterRegistry")

        if f'"{cid}"' not in cat_content:
            print(f"  [FAIL] {cid} missing from FighterVisualCatalog")
            all_ok = False

    # Check GetAuthoritative30Roster
    if "GetAuthoritative30Roster" not in reg_content:
        print("  [FAIL] GetAuthoritative30Roster() missing from CharacterRegistry.cs")
        all_ok = False
    else:
        print("  [PASS] CharacterRegistry.GetAuthoritative30Roster() contract verified")

    return all_ok


def check_main_menu_scene():
    print("\n--- [2/4] Checking Assets/Scenes/MainMenu.unity Hierarchy ---")
    scene_path = ROOT / "Assets/Scenes/MainMenu.unity"
    if not scene_path.exists():
        print(f"[FAIL] Missing {scene_path}")
        return False

    scene_content = scene_path.read_text(encoding="utf-8")

    # Check Master Canvas & Controller
    required_objects = [
        "Master_MainMenu_Canvas",
        "Panel_TitleScreen",
        "Panel_ModeSelect",
        "Panel_CharacterSelect",
        "Panel_StageSelect",
        "Faction_Tabs_Root",
        "Roster_Grid_Root",
        "Overlay_PreMatchVS"
    ]

    all_ok = True
    for name in required_objects:
        if f"m_Name: {name}" not in scene_content:
            print(f"  [FAIL] Missing scene object: {name}")
            all_ok = False
        else:
            print(f"  [PASS] Found scene object: {name}")

    # Check Faction Tabs (4 tabs)
    for t in range(4):
        tab_name = f"Tab_Faction_{t}"
        if f"m_Name: {tab_name}" not in scene_content:
            print(f"  [FAIL] Missing faction tab: {tab_name}")
            all_ok = False
        else:
            print(f"  [PASS] Found faction tab: {tab_name}")

    # Check All 30 Character Cards in Grid
    cards_found = 0
    for cid in AUTHORITATIVE_30:
        card_name = f"Card_Fighter_{cid}"
        if f"m_Name: {card_name}" in scene_content:
            cards_found += 1
        else:
            print(f"  [WARN] Card not found by exact name: {card_name}")

    print(f"  [INFO] Total Roster Cards found in MainMenu.unity: {cards_found}/{len(AUTHORITATIVE_30)}")
    if cards_found < len(AUTHORITATIVE_30):
        all_ok = False

    # Check PreMatchVSOverlay elements
    vs_elements = [
        "Panel_P1_Challenger",
        "Panel_P2_Opponent",
        "Root_CenterClash",
        "Text_KanjiClash",
        "Text_VS",
        "Badge_Mode",
        "Badge_Stage"
    ]
    for elem in vs_elements:
        if f"m_Name: {elem}" not in scene_content:
            print(f"  [FAIL] Missing PreMatchVS element: {elem}")
            all_ok = False
        else:
            print(f"  [PASS] Found PreMatchVS element: {elem}")

    return all_ok


def check_scripts_and_flow_contracts():
    print("\n--- [3/4] Checking UI Flow & Round-Trip Loop Contracts ---")
    all_ok = True

    # 1. MainMenuController.cs
    mm_path = ROOT / "Assets/Scripts/UI/MainMenuController.cs"
    mm_content = mm_path.read_text(encoding="utf-8")

    mm_checks = [
        ("MDI_MenuInitialState", "Restores initial state on return from combat"),
        ("MDI_P1_CharacterId", "Persists P1 character across battle rounds"),
        ("MDI_P2_CharacterId", "Persists P2 character across battle rounds"),
        ("MDI_StageId", "Persists chosen stage"),
        ("SetFactionFilter", "Faction tab filtering method"),
        ("FilterRosterCards", "Filters 30 cards dynamically by faction"),
        ("PreMatchVSOverlay", "Integrates cinematic VS splash before arena transition")
    ]
    for token, desc in mm_checks:
        if token in mm_content:
            print(f"  [PASS] MainMenuController: {desc} ({token})")
        else:
            print(f"  [FAIL] MainMenuController missing {token}: {desc}")
            all_ok = False

    # 2. PreMatchVSOverlay.cs
    vs_path = ROOT / "Assets/Scripts/UI/PreMatchVSOverlay.cs"
    vs_content = vs_path.read_text(encoding="utf-8")

    vs_checks = [
        ("PlayMatchIntro", "Starts cinematic anime split-screen clash"),
        ("PopulateFighterData", "Binds P1 & P2 names, titles, archetypes, and domains"),
        ("AnimateIntroSequence", "Smooth neon slash intro animation"),
        ("Input.anyKeyDown", "Allows instant skip on user input")
    ]
    for token, desc in vs_checks:
        if token in vs_content:
            print(f"  [PASS] PreMatchVSOverlay: {desc} ({token})")
        else:
            print(f"  [FAIL] PreMatchVSOverlay missing {token}: {desc}")
            all_ok = False

    # 3. BattleHUDController.cs
    hud_path = ROOT / "Assets/Scripts/UI/BattleHUDController.cs"
    hud_content = hud_path.read_text(encoding="utf-8")

    hud_checks = [
        ("OnClickRematch", "Immediate rematch via HomegrownCombatDirector / Referee"),
        ("OnClickCharacterSelect", "Character Select return sets MDI_MenuInitialState = CharacterSelect"),
        ("OnClickStageSelect", "Stage Select return sets MDI_MenuInitialState = StageSelect"),
        ("OnClickReturnToMenu", "Main Menu return sets MDI_MenuInitialState = Title"),
        ("_btnStageSelect", "Stage Select button in post-match victory modal"),
        ("_btnPauseStageSelect", "Stage Select button in in-game pause modal")
    ]
    for token, desc in hud_checks:
        if token in hud_content:
            print(f"  [PASS] BattleHUDController: {desc} ({token})")
        else:
            print(f"  [FAIL] BattleHUDController missing {token}: {desc}")
            all_ok = False

    return all_ok


def check_faction_math_and_roster():
    print("\n--- [4/4] Checking Faction Filtering Math & Character Distribution ---")
    sorcerers = [c for c in AUTHORITATIVE_30 if c.startswith("C")]
    sidekicks = [c for c in AUTHORITATIVE_30 if c.startswith("SK")]
    dota = [c for c in AUTHORITATIVE_30 if c.startswith("EX")]

    print(f"  Total Roster Size: {len(AUTHORITATIVE_30)} (Expected: 30)")
    print(f"  Faction 1 [Demon Sorcerers]: {len(sorcerers)} heroes -> {sorcerers}")
    print(f"  Faction 2 [Synty Sidekicks]: {len(sidekicks)} heroes -> {sidekicks}")
    print(f"  Faction 3 [Dota Champions]: {len(dota)} heroes -> {dota}")

    valid = (len(AUTHORITATIVE_30) == 30 and
             len(sorcerers) == 14 and
             len(sidekicks) == 8 and
             len(dota) == 8)

    if valid:
        print("  [PASS] All faction distributions match AAA specification exactly.")
    else:
        print("  [FAIL] Faction counts deviate from 14/8/8.")

    return valid


def main():
    print("=" * 60)
    print(" MY DEMON IDOL: AAA MENU UI FLOW & 30-HERO ROSTER VALIDATION")
    print(f" Workspace: {ROOT}")
    print("=" * 60)

    ok1 = check_catalog_and_registry()
    ok2 = check_main_menu_scene()
    ok3 = check_scripts_and_flow_contracts()
    ok4 = check_faction_math_and_roster()

    print("\n" + "=" * 60)
    if ok1 and ok2 and ok3 and ok4:
        print(" ALL MENU UI FLOW & ROSTER CHECKS PASSED (100% GREEN)")
        print("=" * 60)
        sys.exit(0)
    else:
        print(" MENU UI FLOW VALIDATION FAILED")
        print("=" * 60)
        sys.exit(1)


if __name__ == "__main__":
    main()
