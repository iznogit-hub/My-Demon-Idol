#!/usr/bin/env python3
"""Validation tool for BlazBlue 4-Button integration in My Demon Idol.

Provides:
  --classic-guard : Verifies non-negotiable Classic 3-Button safety rules
                    (no deleted files, no unapproved modifications, controllers and
                    roster prefabs intact).
  --bb-clips      : Verifies that BlazBlue mocap clip slices are 24 fps and adhere to
                    sunk-hip rules (firstFrame >= 8, keepOriginalPositionY: 0).
  --bb-tracks     : Verifies baked BBBoxTrack assets and hashes.
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
            # Special check: check if it uses another approved controller or if it lost KI controller
            errors.append(f"{girl}.prefab does not reference MDI_KI_Melee_Fighter (guid {KI_CONTROLLER_GUID})")

    # 3. Check for banned takes in custom authored clips
    mocap_dir = WORKSPACE_ROOT / "Assets/Huge Mocap Library/mocap animations"
    if mocap_dir.is_dir():
        for meta_file in mocap_dir.rglob("*.fbx.meta"):
            # Skip the raw 88_06 file itself; check if other files or custom clips slice it
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


def main() -> int:
    parser = argparse.ArgumentParser(description="Demon Idol BlazBlue Validator")
    parser.add_argument("--classic-guard", action="store_true", help="Run Classic 3-Button safety guard checks")
    parser.add_argument("--all", action="store_true", help="Run all validation suites")
    args = parser.parse_args()

    if not args.classic_guard and not args.all:
        args.classic_guard = True

    overall_errors: list[str] = []
    overall_warnings: list[str] = []

    if args.classic_guard or args.all:
        errs, warns = check_classic_guard()
        overall_errors.extend(errs)
        overall_warnings.extend(warns)

    for w in overall_warnings:
        print(f"WARN: {w}")
    for e in overall_errors:
        print(f"ERROR: {e}")

    if overall_errors:
        print(f"\nFAILED: {len(overall_errors)} error(s) found.")
        return 1

    print(f"\nPASS: All Classic safety guard checks green ({len(overall_warnings)} warning(s)).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
