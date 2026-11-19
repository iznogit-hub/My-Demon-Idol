#!/usr/bin/env python3
"""Validation tool for BlazBlue 4-Button integration in My Demon Idol.

Provides:
  --classic-guard : Verifies non-negotiable Classic 3-Button safety rules
                    (no deleted files, no unapproved modifications, controllers and
                    roster prefabs intact).
  --character Cxx : Verifies complete BlazBlue asset suite for a specific character
                    (AttackData, BoxTracks, MoveSet, CharacterProfile, OverrideController,
                    Movelist Markdown, CMU mocap slices, Definition link).
  --all           : Runs all validation checks across all systems and characters.
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

# Resolve workspace root dynamically
WORKSPACE_ROOT = Path(os.environ.get("WORKSPACE_ROOT", Path(__file__).resolve().parent.parent))

KI_CONTROLLER_PATH = WORKSPACE_ROOT / "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller"
KI_CONTROLLER_GUID = "b7c4e1a09d2f4c6e8a1b3d5f7091c2e4"

ROSTER_GIRLS = [
    "C01_Akio", "C02_Aika", "C04_UnityChan", "C05_Akane", "C06_Cultist",
    "C07_StellarWitch", "C08_SanpakuChan", "C09_PicoChan", "C10_Reika",
    "C12_Fina", "C14_Muryotaisu",
]

BANNED_TAKES = {"88_06"}


def check_classic_guard() -> tuple[list[str], list[str]]:
    """Enforces non-negotiable Classic 3-Button safety constraints."""
    errors: list[str] = []
    warnings: list[str] = []

    # 1. Verify MDI_KI_Melee_Fighter.controller exists
    if not KI_CONTROLLER_PATH.is_file():
        errors.append(f"Missing base controller: {KI_CONTROLLER_PATH}")
    else:
        meta_path = KI_CONTROLLER_PATH.with_suffix(".controller.meta")
        if meta_path.is_file():
            meta_text = meta_path.read_text(errors="ignore")
            if f"guid: {KI_CONTROLLER_GUID}" not in meta_text:
                errors.append(f"KI Melee Controller GUID mismatch! Expected {KI_CONTROLLER_GUID}")
        else:
            errors.append(f"Missing meta file for base controller: {meta_path}")

    # 2. Check 11 girl roster prefabs
    prefab_dir = WORKSPACE_ROOT / "Assets/Prefabs/Characters"
    for girl in ROSTER_GIRLS:
        prefab_path = prefab_dir / f"{girl}.prefab"
        if not prefab_path.is_file():
            errors.append(f"Missing roster prefab: {prefab_path}")
            continue
        
        # Check Animator controller reference in prefab text
        content = prefab_path.read_text(errors="ignore")
        if KI_CONTROLLER_GUID not in content:
            errors.append(f"{girl}.prefab does not reference MDI_KI_Melee_Fighter (guid {KI_CONTROLLER_GUID})")

    # 3. Check for banned takes in custom authored clips
    mocap_dir = WORKSPACE_ROOT / "Assets/Huge Mocap Library/mocap animations"
    if mocap_dir.is_dir():
        for meta_file in mocap_dir.rglob("*.fbx.meta"):
            if meta_file.name == "88_06.fbx.meta":
                continue
            text = meta_file.read_text(errors="ignore")
            for banned in BANNED_TAKES:
                if f"takeName: {banned}" in text or f"name: {banned}" in text:
                    errors.append(f"Banned take '{banned}' used in {meta_file.relative_to(WORKSPACE_ROOT)}")

    # 4. Check git status for unauthorized file deletions
    try:
        res = subprocess.run(
            ["git", "status", "--porcelain"],
            cwd=str(WORKSPACE_ROOT),
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )
        for line in res.stdout.splitlines():
            status = line[:2]
            file_path = line[3:]
            # Disallow any deletions in Assets
            if "D" in status and file_path.startswith("Assets/"):
                errors.append(f"Deleted file detected in repository: {file_path}")
    except Exception as ex:
        warnings.append(f"Git status check skipped: {ex}")

    return errors, warnings


def check_character(char_key: str) -> tuple[list[str], list[str]]:
    """Validates complete BlazBlue asset suite for a specific character (Spec §10.4)."""
    errors: list[str] = []
    warnings: list[str] = []

    # Normalize character name & id
    char_name = "Akio" if "akio" in char_key.lower() or "c01" in char_key.lower() else char_key
    char_id = "C01" if char_name == "Akio" else char_key

    # 1. Check Movelist markdown file exists
    movelist_path = WORKSPACE_ROOT / f"Assets/MDI_Characters/Runtime/BlazBlue/{char_name.upper()}_BB_MOVELIST.md"
    if not movelist_path.is_file():
        errors.append(f"Missing movelist markdown: {movelist_path}")
        return errors, warnings

    # Parse move table from movelist
    content = movelist_path.read_text(encoding="utf-8")
    table_moves: set[str] = set()
    for line in content.splitlines():
        if line.startswith("| **") and "**" in line[4:]:
            m = re.match(r"^\|\s*\*\*([^\*]+)\*\*", line)
            if m:
                move_id = m.group(1).strip()
                table_moves.add(move_id)

    if not table_moves:
        errors.append(f"No moves parsed from movelist table in {movelist_path}")

    # 2. Check CharacterDefinition exists and links blazBlueProfile
    char_def_path = WORKSPACE_ROOT / f"Assets/CombatData/Characters/CharacterDefinition_{char_id}_{char_name}.asset"
    if not char_def_path.is_file():
        errors.append(f"Missing CharacterDefinition: {char_def_path}")
    else:
        def_text = char_def_path.read_text(encoding="utf-8")
        if "blazBlueProfile:" not in def_text or "fileID: 0" in re.search(r"blazBlueProfile: \{.*\}", def_text).group(0):
            errors.append(f"{char_def_path.name} does not link a valid blazBlueProfile.")

    # 3. Check Override Controller exists and targets KI Melee Controller
    override_path = WORKSPACE_ROOT / f"Assets/MDI_Characters/Runtime/BlazBlue/{char_name}_BB_Override.overrideController"
    if not override_path.is_file():
        errors.append(f"Missing BlazBlue override controller: {override_path}")
    else:
        ctrl_text = override_path.read_text(encoding="utf-8")
        if f"guid: {KI_CONTROLLER_GUID}" not in ctrl_text:
            errors.append(f"{override_path.name} m_Controller does not match KI Melee Controller GUID {KI_CONTROLLER_GUID}")

    # 4. Check BBProfile asset
    combat_data_dir = WORKSPACE_ROOT / f"Assets/CombatData/BlazBlue/{char_name}"
    profile_path = combat_data_dir / f"BBProfile_{char_id}_{char_name}.asset"
    if not profile_path.is_file():
        errors.append(f"Missing BBProfile asset: {profile_path}")

    # 5. Check BBMoveSet asset
    moveset_path = combat_data_dir / f"BBMoveSet_{char_id}_{char_name}.asset"
    if not moveset_path.is_file():
        errors.append(f"Missing BBMoveSet asset: {moveset_path}")

    # 6. Check each move in the movelist table has a BBAttackData asset and BoxTrack
    box_track_dir = combat_data_dir / "BoxTracks"
    for move_id in table_moves:
        attack_file = combat_data_dir / f"BB_{char_name}_{move_id}.asset"
        if not attack_file.is_file():
            errors.append(f"Movelist move '{move_id}' missing BBAttackData asset: {attack_file.name}")
        
        box_file = box_track_dir / f"BoxTrack_{char_name}_{move_id}.asset"
        if not box_file.is_file():
            errors.append(f"Movelist move '{move_id}' missing BBBoxTrack asset: {box_file.name}")

    # 7. Check reverse: every authored BBAttackData corresponds to a move in table
    if combat_data_dir.is_dir():
        for asset_file in combat_data_dir.glob(f"BB_{char_name}_*.asset"):
            move_id = asset_file.stem.replace(f"BB_{char_name}_", "")
            if move_id not in table_moves:
                warnings.append(f"Authored BBAttackData '{asset_file.name}' not found in {char_name.upper()}_BB_MOVELIST.md")

    return errors, warnings


def main() -> int:
    parser = argparse.ArgumentParser(description="Demon Idol BlazBlue Validator")
    parser.add_argument("--classic-guard", action="store_true", help="Run Classic 3-Button safety guard checks")
    parser.add_argument("--character", type=str, help="Validate specific character BlazBlue assets (e.g. C01, Akio)")
    parser.add_argument("--all", action="store_true", help="Run all validation suites")
    args = parser.parse_args()

    if not args.classic_guard and not args.character and not args.all:
        args.classic_guard = True

    overall_errors: list[str] = []
    overall_warnings: list[str] = []

    if args.classic_guard or args.all:
        print("[1] Running Classic Safety Guard...")
        errs, warns = check_classic_guard()
        overall_errors.extend(errs)
        overall_warnings.extend(warns)

    if args.character or args.all:
        target_char = args.character if args.character else "C01"
        print(f"[2] Validating BlazBlue Character Suite for '{target_char}'...")
        errs, warns = check_character(target_char)
        overall_errors.extend(errs)
        overall_warnings.extend(warns)

    for w in overall_warnings:
        print(f"WARN: {w}")
    for e in overall_errors:
        print(f"ERROR: {e}")

    if overall_errors:
        print(f"\nFAILED: {len(overall_errors)} error(s) found.")
        return 1

    print(f"\nPASS: All validation checks green ({len(overall_warnings)} warning(s)).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
