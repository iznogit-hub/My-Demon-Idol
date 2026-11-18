#!/usr/bin/env python3
"""BlazBlue gate. Classic files stay put, and the tuning asset keeps Astral off."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path("/workspace")

FORBIDDEN = {
    "Assets/Scripts/Combat/Hitbox.cs",
    "Assets/Scripts/Combat/Hurtbox.cs",
    "Assets/Scripts/Combat/AkioBodyFit.cs",
    "Assets/Scripts/Combat/AkioPresentation.cs",
    "Assets/Scripts/Combat/RapidCancel.cs",
    "Assets/Scripts/Combat/BarrierBlockSystem.cs",
    "Assets/Scripts/Combat/PsychicBurstSystem.cs",
    "Assets/Scripts/Combat/JuggleEngine.cs",
    "Assets/Scripts/Data/AttackData.cs",
    "Assets/Scripts/Data/MoveSet.cs",
    "Assets/Scripts/Data/AkioClipLibrary.cs",
    "Assets/Scripts/Input/InputBuffer.cs",
    "Assets/Scripts/Input/CommandParser.cs",
    "Assets/Scripts/Input/MobileFighterInput.cs",
    "Assets/Scripts/Input/TouchVirtualControls.cs",
    "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller",
}

ALLOWED_EDIT = {
    "Assets/Scripts/Combat/FighterController.cs",
    "Assets/Scripts/Combat/HomegrownCombatDirector.cs",
    "Assets/Scripts/Data/CharacterDefinition.cs",
    "Assets/Scripts/UI/SettingsPanel.cs",
    "tools/validate_akio_movelist.py",
    "Docs/MDI_BLAZBLUE_IMPLEMENTATION_SPEC.md",
    "Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C04_UnityChan.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C09_PicoChan.asset",
}

ALLOWED_NEW = (
    "Assets/Scripts/BlazBlue",
    "Assets/CombatData/BlazBlue",
    "Assets/MDI_Characters/Runtime/BlazBlue",
    "Assets/Timelines/BB/",
    "tools/validate_blazblue.py",
    "tools/run_classic_validators.sh",
    "tools/bb_core_check/",
)


GIRLS = [
    ("Akio", "CharacterDefinition_C01_Akio.asset", "akioDepthMovelist: 1", "c0a10e00000000000000000000000001", "c0bb0100000000000000000000000001", "Iron Rush", "AKIO_BB_MOVELIST.md", "Akio_BB_Override", "5C"),
    ("Aika", "CharacterDefinition_C02_Aika.asset", "depthMovelist: 1", "c0a11a00000000000000000000000001", "c0bb0200000000000000000000000001", "Resonance Nail", "AIKA_BB_MOVELIST.md", "Aika_BB_Override", "5C"),
    ("UnityChan", "CharacterDefinition_C04_UnityChan.asset", "depthMovelist: 1", "c0a12c00000000000000000000000001", "c0bb0300000000000000000000000001", "Aero Hop", "UNITYCHAN_BB_MOVELIST.md", "UnityChan_BB_Override", "5C"),
    ("PicoChan", "CharacterDefinition_C09_PicoChan.asset", "depthMovelist: 1", "c0a13c00000000000000000000000001", "c0bb0400000000000000000000000001", "Clinch", "PICOCHAN_BB_MOVELIST.md", "PicoChan_BB_Override", "5C"),
]


def load_clips():
    clips = {}
    roots = [ROOT / "Assets/Huge Mocap Library", ROOT / "Assets/Kevin Iglesias"]
    for base in roots:
        if not base.exists():
            continue
        for meta in base.rglob("*.fbx.meta"):
            body = meta.read_text(errors="ignore")
            for chunk in body.split("      name: ")[1:]:
                name = chunk.split("\n", 1)[0].strip()
                if "firstFrame:" not in chunk:
                    continue
                first = chunk.split("firstFrame: ", 1)[1].split("\n", 1)[0].strip()
                last = chunk.split("lastFrame: ", 1)[1].split("\n", 1)[0].strip()
                clips[name] = (first, last)
    return clips


def check_girls():
    errors = []
    clips = load_clips()
    for folder, definition_name, classic_flag, classic_guid, profile_guid, drive, doc_name, override_name, fatal in GIRLS:
        doc_path = ROOT / "Assets/MDI_Characters/Runtime/BlazBlue" / doc_name
        if not doc_path.exists():
            continue
        definition = (ROOT / "Assets/CombatData/Characters" / definition_name).read_text()
        if classic_flag not in definition:
            errors.append(folder + " Classic flag missing")
        if "guid: " + classic_guid not in definition:
            errors.append(folder + " Classic override missing")
        if "guid: " + profile_guid not in definition:
            errors.append(folder + " BlazBlue profile is not linked")
        profile = (ROOT / "Assets/CombatData/BlazBlue" / folder / f"{folder}_BBProfile.asset").read_text()
        if f"driveName: {drive}" not in profile:
            errors.append(folder + " drive name mismatch")
        override = (ROOT / "Assets/MDI_Characters/Runtime/BlazBlue" / f"{override_name}.overrideController").read_text()
        if "guid: b7c4e1a09d2f4c6e8a1b3d5f7091c2e4" not in override:
            errors.append(folder + " BB override is not on the shared controller")
        for line in doc_path.read_text().splitlines():
            if not line.startswith("| ") or line.startswith("| Move") or line.startswith("| ---"):
                continue
            cells = [c.strip() for c in line.strip("|").split("|")]
            if len(cells) < 4:
                continue
            move = cells[0]
            asset = ROOT / "Assets/CombatData/BlazBlue" / folder / f"BB_{folder}_{move}.asset"
            if not asset.exists():
                errors.append("missing asset for " + folder + " " + move)
                continue
            body = asset.read_text()
            if f"moveId: {move}" not in body:
                errors.append(move + " id mismatch")
            clip = cells[2].split()[0]
            if f"clipName: {clip}" not in body:
                errors.append(move + " clip " + clip)
            if clip not in clips:
                errors.append("clip missing " + clip)
            elif len(cells[2].split()) >= 4 and "-" in cells[2].split()[-1]:
                first, last = cells[2].split()[-1].split("-")
                if clips[clip] != (first, last):
                    errors.append(f"{clip} frames {clips[clip]} != {first}-{last}")
            startup, active, recovery = [int(x) for x in cells[3].split("/")]
            if f"boxTickCount: {startup + active + recovery}" not in body:
                errors.append(move + " box track length")
            if move == fatal and "fatalCounter: 1" not in body:
                errors.append(folder + " " + fatal + " is not a Fatal Counter")
    return errors


def main() -> int:
    errors = []
    changed = subprocess.check_output(["git", "diff", "--name-only", "main"], cwd=ROOT, text=True)
    untracked = subprocess.check_output(["git", "ls-files", "--others", "--exclude-standard"], cwd=ROOT, text=True)
    for path in changed.splitlines():
        path = path.strip().strip('"')
        if not path or "__pycache__" in path or path.endswith(".pyc"):
            continue
        if path in FORBIDDEN or path.startswith("Assets/MDI_Characters/Runtime/AkioCore/") or path.startswith("Assets/Prefabs/") or path.startswith("Assets/CombatData/Akio/") or path.startswith("Assets/CombatData/Aika/") or path.startswith("Assets/CombatData/UnityChan/") or path.startswith("Assets/CombatData/PicoChan/"):
            errors.append("classic file changed " + path)
            continue
        if path.endswith(".prefab") or path.endswith(".overrideController"):
            errors.append("prefab or existing override changed " + path)
            continue
        if path.endswith(".fbx.meta"):
            diff = subprocess.check_output(["git", "diff", "-U0", "main", "--", path], cwd=ROOT, text=True)
            for line in diff.splitlines():
                if line.startswith("-") and not line.startswith("---") and "name: " in line:
                    errors.append("existing clip renamed or removed in " + path)
                    break
            continue
        if path in ALLOWED_EDIT or any(path == p or path.startswith(p) for p in ALLOWED_NEW):
            continue
        errors.append("unexpected change " + path)
    for path in untracked.splitlines():
        path = path.strip().strip('"')
        if not path or "__pycache__" in path or "/bin/" in path or "/obj/" in path or path.endswith(".pyc"):
            continue
        if any(path == p or path.startswith(p) for p in ALLOWED_NEW):
            continue
        errors.append("unexpected new file " + path)

    tuning = (ROOT / "Assets/CombatData/BlazBlue/BBSystemTuning.asset").read_text()
    if "enableAstral: 0" not in tuning:
        errors.append("Astral Heat must stay off")
    fc = (ROOT / "Assets/Scripts/Combat/FighterController.cs").read_text()
    if "externalSimulation" not in fc or fc.count("if (externalSimulation) return;") < 2:
        errors.append("FighterController guard missing")
    definition = (ROOT / "Assets/Scripts/Data/CharacterDefinition.cs").read_text()
    if "blazBlueProfile" not in definition:
        errors.append("CharacterDefinition has no BlazBlue link")
    settings = (ROOT / "Assets/Scripts/UI/SettingsPanel.cs").read_text()
    if "controlScheme = 0" not in settings:
        errors.append("Classic is not the default scheme")
    if "StellarWitch" not in (ROOT / "Docs/MDI_BLAZBLUE_IMPLEMENTATION_SPEC.md").read_text():
        errors.append("slot 9 decision missing from the spec")
    errors.extend(check_girls())

    if errors:
        print("FAIL")
        for e in errors:
            print(" -", e)
        return 1
    print("PASS blazblue classic guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
