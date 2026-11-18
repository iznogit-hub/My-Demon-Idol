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
            errors.append("mocap meta changed in the shared layer " + path)
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

    if errors:
        print("FAIL")
        for e in errors:
            print(" -", e)
        return 1
    print("PASS blazblue classic guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
