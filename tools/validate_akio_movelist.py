#!/usr/bin/env python3
"""Checks Akio's opt-in movelist and that the shared roster assets were not rewritten."""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path("/workspace")
CTRL = ROOT / "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller"

ALLOWED_MODIFIED = {
    "Assets/Scripts/Combat/FighterController.cs",
    "Assets/Scripts/Combat/Hitbox.cs",
    "Assets/Scripts/Combat/HomegrownCombatDirector.cs",
    "Assets/Scripts/Combat/ProjectileFactory.cs",
    "Assets/Scripts/Data/AttackData.cs",
    "Assets/Scripts/Data/CharacterDefinition.cs",
    "Assets/Scripts/Data/MoveSet.cs",
    "Assets/Scripts/Input/CommandParser.cs",
    "Assets/Scripts/Input/InputBuffer.cs",
    "Assets/Scripts/Combat/AkioBodyFit.cs",
    "Assets/Scripts/Combat/AkioPresentation.cs",
    "Assets/Scripts/Data/AkioClipLibrary.cs",
    "Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset",
    "tools/validate_ki_melee.py",
    "tools/validate_akio_movelist.py",
    "Assets/Huge Mocap Library/mocap animations/144/144_20.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/144/144_07.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/144/144_17.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/144/144_33.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/143/143_23.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_05.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_06.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_07.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_10.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/75/75_16.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/90/90_18.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/111/111_06.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/80/80_10.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/79/79_08.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/74/74_01.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/13/13_18.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_02.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_09.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/135/135_11.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/141/141_14.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/143/143_24.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/113/113_13.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/111/111_19.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/144/144_08.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/114/114_02.fbx.meta",
}

ALLOWED_ADDED_PREFIXES = (
    "Assets/CombatData/Akio/",
    "Assets/MDI_Characters/Runtime/Akio/",
    "Assets/MDI_Characters/Runtime/AkioCore/",
    "Assets/MDI_Characters/Runtime/AKIO_MOVELIST.md",
    "Assets/MDI_Characters/Runtime/Akio_KI_Melee_Override.overrideController",
    "Assets/MDI_Characters/Runtime/Aika_KI_Melee_Override.overrideController",
    "Assets/MDI_Characters/Runtime/AIKA_MOVELIST.md",
    "Assets/CombatData/Aika/",
    "Assets/MDI_Characters/Runtime/Aika/",
    "Assets/Scripts/Combat/AkioPresentation.cs",
    "Assets/Scripts/Combat/AkioBodyFit.cs",
    "Assets/Scripts/Data/AkioClipLibrary.cs",
    "tools/validate_akio_movelist.py",
)

WINDOWS = {
    "Akio_5P": (110, 122),
    "Akio_2P": (196, 214),
    "Akio_5M": (123, 136),
    "Akio_6M": (148, 166),
    "Akio_Rekka1": (34, 47),
    "Akio_jP": (42, 51),
    "Akio_2M": (100, 120),
    "Akio_Crouch": (80, 92),
    "Akio_jM": (40, 58),
    "Akio_jS": (44, 60),
    "Akio_Jump": (32, 50),
    "Akio_2S": (140, 164),
    "Akio_623S": (15, 34),
    "Akio_6S": (24, 50),
    "Akio_Throw": (26, 48),
    "Akio_Dash": (20, 40),
    "Akio_Super": (48, 80),
    "Akio_StandBlock": (186, 206),
    "Akio_CrouchBlock": (300, 320),
    "Akio_Fall": (8, 45),
    "Akio_Getup": (8, 48),
    "Akio_Walk": (150, 178),
    "Akio_Intro": (2, 48),
}

TRIGGERS = [
    "LightAttack", "LowAttack", "AirLight", "HeavyAttack", "OverheadAttack", "LowSweepAttack",
    "AirHeavy", "MediumAttack", "LauncherAttack", "JumpAttack", "ThrowCard", "AdvancingSkill",
    "DriveSkill", "Throw", "DomainSuper", "BurstShockwave", "Intro", "StandUpBack", "Jump",
    "ForwardDash", "Knockdown", "Defeat", "RapidCancel", "Idle",
]


def main() -> int:
    errors = []
    changed = subprocess.check_output(["git", "diff", "--name-only", "main"], cwd=ROOT, text=True)
    untracked = subprocess.check_output(["git", "ls-files", "--others", "--exclude-standard"], cwd=ROOT, text=True)
    for path in changed.splitlines():
        path = path.strip().strip('"')
        if not path or "__pycache__" in path:
            continue
        if path in ALLOWED_MODIFIED:
            continue
        if any(path == p or path.startswith(p) for p in ALLOWED_ADDED_PREFIXES):
            continue
        errors.append("unexpected change " + path)
    for path in untracked.splitlines():
        path = path.strip().strip('"')
        if not path or "__pycache__" in path or path.endswith(".pyc"):
            continue
        if any(path == p or path.startswith(p) for p in ALLOWED_ADDED_PREFIXES):
            continue
        errors.append("unexpected new file " + path)

    ctrl_diff = subprocess.check_output(
        ["git", "diff", "--name-only", "HEAD", "--", str(CTRL.relative_to(ROOT))],
        cwd=ROOT, text=True,
    ).strip()
    if ctrl_diff:
        errors.append("MDI_KI_Melee_Fighter.controller was edited")

    text = CTRL.read_text()
    params = set(re.findall(r"m_Name: ([A-Za-z0-9_]+)\n    m_Type:", text))
    missing = [t for t in TRIGGERS if t not in params]
    if missing:
        errors.append("controller missing triggers " + ", ".join(missing))

    defs = list((ROOT / "Assets/CombatData/Characters").glob("CharacterDefinition_*.asset"))
    for path in defs:
        body = path.read_text()
        flagged = "akioDepthMovelist: 1" in body
        depth = "depthMovelist: 1" in body
        if path.name == "CharacterDefinition_C01_Akio.asset":
            if not flagged:
                errors.append("C01 is not opted into the Akio movelist")
            if depth:
                errors.append("C01 should keep akioDepthMovelist and not depthMovelist")
            if "guid: c0a10e00000000000000000000000001" not in body:
                errors.append("C01 is not on Akio_KI_Melee_Override")
            if "guid: c0a10e00000000000000000000000002" not in body:
                errors.append("C01 has no Akio moveset")
            if "depthCrouchHeight:" in body:
                errors.append("C01 pushbox override would change Akio's body fit")
        elif path.name == "CharacterDefinition_C02_Aika.asset":
            if flagged:
                errors.append("Aika must not use Akio's opt-in flag")
            if not depth:
                errors.append("Aika is not opted into the depth movelist")
            if "guid: c0a11a00000000000000000000000001" not in body:
                errors.append("Aika is not on her own override")
            if "guid: c0a10e00000000000000000000000002" in body:
                errors.append("Aika is pointing at Akio's moveset")
        elif flagged or depth:
            errors.append(path.name + " opted into a depth movelist")

    fc = (ROOT / "Assets/Scripts/Combat/FighterController.cs").read_text()
    for needle in (
        "UsesAkioMovelist",
        "TryAkioProjectile",
        "TryAkioMelee",
        "TryAkioSpecial",
        "AkioCancelAllowed",
        "akioKeepRecovery",
        "akioMoveId == \"236P\"",
        "TriggerAnimation(\"Intro\")",
        "TriggerAnimation(\"StandUpBack\")",
    ):
        if needle not in fc:
            errors.append("FighterController missing " + needle)
    if "if (UsesAkioMovelist) return;" not in fc:
        errors.append("legacy P/M/S still runs for Akio")

    override = (ROOT / "Assets/MDI_Characters/Runtime/Akio_KI_Melee_Override.overrideController").read_text()
    if "guid: b7c4e1a09d2f4c6e8a1b3d5f7091c2e4" not in override:
        errors.append("override is not parented to MDI_KI_Melee_Fighter")

    seen = set()
    for meta in (ROOT / "Assets/Huge Mocap Library").rglob("*.fbx.meta"):
        body = meta.read_text(errors="ignore")
        if "88_06" in meta.name and "Akio_" in body:
            errors.append("88_06 was given an Akio clip")
        if "Akio_" not in body:
            continue
        fbx = meta.with_suffix("")  # .fbx.meta -> .fbx? with_suffix on .fbx.meta removes .meta
        # Path('x.fbx.meta').with_suffix('') == 'x.fbx'
        fbx_text = Path(str(meta)[:-5]).read_text(errors="ignore")
        if 'FrameRate: "24"' not in fbx_text:
            errors.append(meta.name + " is not 24 fps")
        for name, (first, last) in WINDOWS.items():
            m = re.search(
                rf"name: {name}\n      takeName: .+\n      internalID: (\d+)\n      firstFrame: (\d+)\n      lastFrame: (\d+)",
                body,
            )
            if not m:
                continue
            seen.add(name)
            if int(m.group(2)) != first or int(m.group(3)) != last:
                errors.append(f"{name} range {m.group(2)}-{m.group(3)} != {first}-{last}")
            if name in ("Akio_Getup", "Akio_Fall") and int(m.group(2)) < 8:
                errors.append(name + " starts before frame 8")
            if last - first + 1 < 8:
                errors.append(name + " is a tiny clip")
            if name.endswith("Block") or name in ("Akio_Walk", "Akio_Crouch"):
                if f"name: {name}\n      takeName:" not in body:
                    errors.append(name + " missing")
    missing_windows = sorted(set(WINDOWS) - seen)
    if missing_windows:
        errors.append("missing clip windows " + ", ".join(missing_windows))

    attacks = list((ROOT / "Assets/CombatData/Akio").glob("Akio_*.asset"))
    if len(attacks) < 18:
        errors.append("expected Akio attack assets, found %d" % len(attacks))
    for asset in attacks:
        body = asset.read_text()
        if asset.name == "Akio_Burst.asset":
            if "akioApplyHitbox: 1" in body or "akioAuthoredCancels: 1" in body:
                errors.append("burst opted into Akio hitboxes")
            continue
        if "akioAuthoredCancels: 1" not in body:
            errors.append(asset.name + " has no authored cancels")
        if "akioMoveId:" not in body:
            errors.append(asset.name + " has no move id")
        m = re.search(r"startupFrames: (\d+)\n  activeFrames: (\d+)\n  recoveryFrames: (\d+)", body)
        if not m or int(m.group(1)) < 5:
            errors.append(asset.name + " startup is tiny or missing")
    if "akioMoveId: 236P" not in (ROOT / "Assets/CombatData/Akio/Akio_236P.asset").read_text():
        errors.append("fireball id missing")
    if "akioStrikeInvuln: 1" not in (ROOT / "Assets/CombatData/Akio/Akio_623S.asset").read_text():
        errors.append("DP has no strike invuln flag")
    super_body = (ROOT / "Assets/CombatData/Akio/Akio_Super.asset").read_text()
    if super_body.count("followBone: 1") < 2:
        errors.append("super is missing the punch and kick phases")
    if "jump" not in (ROOT / "Assets/CombatData/Akio/Akio_2S.asset").read_text():
        errors.append("launcher cannot jump-cancel")
    if "jM" not in (ROOT / "Assets/CombatData/Akio/Akio_jP.asset").read_text():
        errors.append("j.P does not cancel to j.M")
    if "jS" not in (ROOT / "Assets/CombatData/Akio/Akio_jM.asset").read_text():
        errors.append("j.M does not cancel to j.S")
    akio_jab = (ROOT / "Assets/CombatData/Akio/Akio_5P.asset").read_text()
    if "startupFrames: 6" not in akio_jab or "akioMoveId: 5P" not in akio_jab or "akioAnimatorSpeed: 0.4096" not in akio_jab:
        errors.append("Akio 5P frame data changed")
    if "depthQuarterBack:" in (ROOT / "Assets/CombatData/Akio/AkioMoveSet.asset").read_text():
        errors.append("Akio moveset gained a 214")

    aika_windows = {
        "Aika_5P": (52, 68), "Aika_Rekka1": (36, 52), "Aika_2P": (462, 480), "Aika_2M": (492, 514),
        "Aika_5M": (73, 91), "Aika_jP": (280, 296), "Aika_Super": (276, 316),
        "Aika_214S": (34, 54), "Aika_6M": (164, 184), "Aika_Jump": (48, 70),
        "Aika_jM": (44, 64), "Aika_jS": (80, 102), "Aika_2S": (219, 243), "Aika_Rekka3": (245, 265),
        "Aika_623S": (62, 84), "Aika_Charge": (168, 192), "Aika_Dash": (17, 35),
        "Aika_Throw": (36, 58), "Aika_6S": (57, 75), "Aika_Rekka2": (54, 74),
        "Aika_Walk": (28, 56), "Aika_Intro": (8, 48), "Aika_Crouch": (208, 224),
        "Aika_CrouchBlock": (240, 258), "Aika_StandBlock": (300, 324), "Aika_Victory": (560, 584),
        "Aika_Fall": (8, 48), "Aika_Getup": (211, 261),
    }
    seen_aika = set()
    for meta in (ROOT / "Assets/Huge Mocap Library").rglob("*.fbx.meta"):
        body = meta.read_text(errors="ignore")
        if "Aika_" not in body:
            continue
        if "Akio_" in body:
            errors.append(meta.name + " mixes Akio and Aika clips")
        fbx_text = Path(str(meta)[:-5]).read_text(errors="ignore")
        if 'FrameRate: "24"' not in fbx_text:
            errors.append(meta.name + " is not 24 fps")
        for name, (first, last) in aika_windows.items():
            m = re.search(
                rf"name: {name}\n      takeName: .+\n      internalID: (\d+)\n      firstFrame: (\d+)\n      lastFrame: (\d+)",
                body,
            )
            if not m:
                continue
            seen_aika.add(name)
            if int(m.group(2)) != first or int(m.group(3)) != last:
                errors.append(f"{name} range {m.group(2)}-{m.group(3)} != {first}-{last}")
            if last - first + 1 < 8:
                errors.append(name + " is a tiny clip")
            if name in ("Aika_Getup", "Aika_Fall", "Aika_Intro") and int(m.group(2)) < 8:
                errors.append(name + " starts before frame 8")
    if seen_aika != set(aika_windows):
        errors.append("missing Aika windows " + ", ".join(sorted(set(aika_windows) - seen_aika)))
    aika_ms = (ROOT / "Assets/CombatData/Aika/AikaMoveSet.asset").read_text()
    if "depthQuarterBack:" not in aika_ms or "depthCharge:" not in aika_ms:
        errors.append("Aika moveset does not bind 214 and charge")
    if "akioMoveId: 214S" not in (ROOT / "Assets/CombatData/Aika/Aika_214S.asset").read_text():
        errors.append("214S id missing")
    if "akioMoveId: chargeP" not in (ROOT / "Assets/CombatData/Aika/Aika_Charge.asset").read_text():
        errors.append("charge id missing")
    if "akioStrikeInvuln: 1" not in (ROOT / "Assets/CombatData/Aika/Aika_623S.asset").read_text():
        errors.append("Aika DP has no strike invuln")
    aika_override = (ROOT / "Assets/MDI_Characters/Runtime/Aika_KI_Melee_Override.overrideController").read_text()
    if "guid: b7c4e1a09d2f4c6e8a1b3d5f7091c2e4" not in aika_override:
        errors.append("Aika override is not parented to the shared controller")

    other = (ROOT / "Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset").read_text()
    if "akioDepthMovelist: 1" in other or "c0a10e00000000000000000000000002" in other:
        errors.append("Aika definition was pointed at Akio data")

    for e in errors:
        print("FAIL", e)
    if errors:
        print(f"{len(errors)} failure(s)")
        return 1
    print("PASS", len(seen), "clip windows,", len(attacks), "attack assets")
    return 0


if __name__ == "__main__":
    sys.exit(main())
