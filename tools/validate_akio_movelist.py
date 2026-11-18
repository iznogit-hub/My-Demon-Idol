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
    "Assets/Scripts/Data/MoveSet.cs",
    "Assets/Scripts/Input/CommandParser.cs",
    "Assets/Scripts/Input/InputBuffer.cs",
    "Assets/Scripts/Combat/AkioBodyFit.cs",
    "Assets/Scripts/Combat/AkioPresentation.cs",
    "Assets/Scripts/Data/AkioClipLibrary.cs",
    "Assets/CombatData/Characters/CharacterDefinition_C01_Akio.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C02_Aika.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C04_UnityChan.asset",
    "Assets/CombatData/Characters/CharacterDefinition_C09_PicoChan.asset",
    "Assets/Scripts/Combat/FighterController.cs",
    "Assets/Scripts/Data/MoveSet.cs",
    "Assets/Scripts/Data/CharacterDefinition.cs",
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
    "Assets/Huge Mocap Library/mocap animations/02/02_05.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/14/14_02.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/16/16_01.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/16/16_05.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/16/16_15.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/16/16_35.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/91/91_39.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/118/118_01.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/118/118_05.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/140/140_01.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/15/15_13.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/17/17_10.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/86/86_08.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/111/111_02.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/111/111_33.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/111/111_34.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/113/113_08.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/139/139_09.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/141/141_23.fbx.meta",
    "Assets/Huge Mocap Library/mocap animations/143/143_06.fbx.meta",
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
    "Assets/MDI_Characters/Runtime/UnityChan/",
    "Assets/MDI_Characters/Runtime/UNITYCHAN_MOVELIST.md",
    "Assets/MDI_Characters/Runtime/UnityChan_KI_Melee_Override.overrideController",
    "Assets/CombatData/UnityChan/",
    "Assets/CombatData/PicoChan/",
    "Assets/MDI_Characters/Runtime/PicoChan/",
    "Assets/MDI_Characters/Runtime/PICOCHAN_MOVELIST.md",
    "Assets/MDI_Characters/Runtime/PicoChan_KI_Melee_Override.overrideController",
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
        elif path.name == "CharacterDefinition_C04_UnityChan.asset":
            if flagged:
                errors.append("UnityChan must not use Akio's opt-in flag")
            if not depth:
                errors.append("UnityChan is not opted into the depth movelist")
            if "guid: c0a12c00000000000000000000000001" not in body:
                errors.append("UnityChan is not on her own override")
            if "guid: c0a10e00000000000000000000000002" in body or "guid: c0a11a00000000000000000000000002" in body:
                errors.append("UnityChan is pointing at another fighter's moveset")
        elif path.name == "CharacterDefinition_C09_PicoChan.asset":
            if flagged:
                errors.append("PicoChan must not use Akio's opt-in flag")
            if not depth:
                errors.append("PicoChan is not opted into the depth movelist")
            if "guid: c0a13c00000000000000000000000001" not in body:
                errors.append("PicoChan is not on her own override")
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

    uchan_windows = {
        "UChan_5P": (92, 108), "UChan_2P": (212, 230), "UChan_5M": (144, 162), "UChan_jP": (124, 140),
        "UChan_Rekka1": (55, 71), "UChan_Rekka2": (126, 144), "UChan_Rekka3": (148, 168),
        "UChan_Throw": (72, 94), "UChan_StandBlock": (8, 28), "UChan_2S": (16, 40),
        "UChan_Jump": (14, 42), "UChan_Victory": (48, 64), "UChan_6M": (20, 40), "UChan_623S": (30, 50),
        "UChan_Walk": (35, 63), "UChan_Intro": (8, 40), "UChan_Dash": (8, 32), "UChan_2M": (630, 648),
        "UChan_Super": (811, 845), "UChan_jM": (88, 106), "UChan_jS": (64, 82), "UChan_Crouch": (27, 43),
        "UChan_Charge": (47, 70), "UChan_CrouchBlock": (27, 43), "UChan_6S": (24, 44), "UChan_214S": (50, 70),
        "UChan_Fall": (8, 40), "UChan_Getup": (70, 130),
    }
    seen_uchan = set()
    for meta in (ROOT / "Assets/Huge Mocap Library").rglob("*.fbx.meta"):
        body = meta.read_text(errors="ignore")
        if "UChan_" not in body:
            continue
        if "Akio_" in body or "Aika_" in body:
            errors.append(meta.name + " mixes UnityChan clips with Akio or Aika")
        for name, (first, last) in uchan_windows.items():
            m = re.search(
                rf"name: {name}\n      takeName: .+\n      internalID: (\d+)\n      firstFrame: (\d+)\n      lastFrame: (\d+)",
                body,
            )
            if not m:
                continue
            seen_uchan.add(name)
            if int(m.group(2)) != first or int(m.group(3)) != last:
                errors.append(f"{name} range {m.group(2)}-{m.group(3)} != {first}-{last}")
            if name in ("UChan_Getup", "UChan_Fall") and "keepOriginalPositionY: 0" not in body:
                errors.append(name + " still keeps the sunk root height")
    if seen_uchan != set(uchan_windows):
        errors.append("missing UnityChan windows " + ", ".join(sorted(set(uchan_windows) - seen_uchan)))
    factory = (ROOT / "Assets/Scripts/Combat/ProjectileFactory.cs").read_text()
    if 'name = "Aero Plasma Disc", speed = 15f, damage = 44f' not in factory:
        errors.append("UnityChan flag-off projectile numbers changed")
    if 'name = "Shadow Ink Kunai", speed = 16f, damage = 60f' not in factory:
        errors.append("Akio projectile numbers changed")
    if 'name = "Resonance Occult Nail", speed = 13f, damage = 55f' not in factory:
        errors.append("Aika projectile numbers changed")
    uchan_ms = (ROOT / "Assets/CombatData/UnityChan/UnityChanMoveSet.asset").read_text()
    if "depthQuarterBack:" not in uchan_ms or "depthCharge:" not in uchan_ms:
        errors.append("UnityChan moveset does not bind 214 and charge")
    uchan_override = (ROOT / "Assets/MDI_Characters/Runtime/UnityChan_KI_Melee_Override.overrideController").read_text()
    if "guid: b7c4e1a09d2f4c6e8a1b3d5f7091c2e4" not in uchan_override:
        errors.append("UnityChan override is not parented to the shared controller")
    if "depthQuarterForward:" in (ROOT / "Assets/CombatData/Akio/AkioMoveSet.asset").read_text():
        errors.append("Akio moveset gained a non-projectile 236")
    if "depthQuarterForward:" in (ROOT / "Assets/CombatData/Aika/AikaMoveSet.asset").read_text():
        errors.append("Aika moveset gained a non-projectile 236")
    if "depthQuarterForward:" in (ROOT / "Assets/CombatData/UnityChan/UnityChanMoveSet.asset").read_text():
        errors.append("UnityChan moveset gained a non-projectile 236")

    pico_windows = {
        "Pico_5P": (638, 654), "Pico_2P": (376, 394), "Pico_5M": (1256, 1274), "Pico_6M": (400, 418),
        "Pico_2M": (344, 362), "Pico_jP": (765, 783), "Pico_Rekka1": (35, 53), "Pico_Rekka2": (465, 483),
        "Pico_StandBlock": (8, 28), "Pico_2S": (288, 308), "Pico_623S": (149, 169), "Pico_236S": (37, 57),
        "Pico_6S": (173, 193), "Pico_Dash": (355, 373), "Pico_jM": (312, 330), "Pico_jS": (944, 962),
        "Pico_Charge": (1502, 1524), "Pico_Rekka3": (556, 574), "Pico_Victory": (17, 77),
        "Pico_Throw": (314, 336), "Pico_Walk": (82, 110), "Pico_Intro": (8, 40),
        "Pico_Fall": (8, 36), "Pico_Getup": (300, 342), "Pico_Crouch": (110, 130),
        "Pico_CrouchBlock": (42, 62), "Pico_214S": (11, 33), "Pico_Jump": (12, 32),
        "Pico_Super": (820, 854),
    }
    seen_pico = set()
    for meta in (ROOT / "Assets/Huge Mocap Library").rglob("*.fbx.meta"):
        body = meta.read_text(errors="ignore")
        if "Pico_" not in body:
            continue
        if "Akio_" in body or "Aika_" in body or "UChan_" in body:
            errors.append(meta.name + " mixes PicoChan clips with another fighter")
        for name, (first, last) in pico_windows.items():
            m = re.search(
                rf"name: {name}\n      takeName: .+\n      internalID: (\d+)\n      firstFrame: (\d+)\n      lastFrame: (\d+)",
                body,
            )
            if not m:
                continue
            seen_pico.add(name)
            if int(m.group(2)) != first or int(m.group(3)) != last:
                errors.append(f"{name} range {m.group(2)}-{m.group(3)} != {first}-{last}")
            if name == "Pico_Getup" and "keepOriginalPositionY: 0" not in body:
                errors.append("Pico get-up still keeps the sunk root")
    if seen_pico != set(pico_windows):
        errors.append("missing Pico windows " + ", ".join(sorted(set(pico_windows) - seen_pico)))
    pico_ms = (ROOT / "Assets/CombatData/PicoChan/PicoChanMoveSet.asset").read_text()
    if "akioFireball: {fileID: 0}" not in pico_ms:
        errors.append("Pico 236 would spend the laser meter")
    if "depthQuarterForward:" not in pico_ms or "depthQuarterBack:" not in pico_ms or "depthCharge:" not in pico_ms:
        errors.append("Pico moveset is missing 236, 214, or charge")
    if "akioMoveId: 236S" not in (ROOT / "Assets/CombatData/PicoChan/Pico_236S.asset").read_text():
        errors.append("Pico lariat id would spawn a projectile")
    factory = (ROOT / "Assets/Scripts/Combat/ProjectileFactory.cs").read_text()
    if 'name = "Cyber Laser Cannon", speed = 20f, damage = 44f' not in factory:
        errors.append("Pico flag-off laser numbers changed")
    pico_override = (ROOT / "Assets/MDI_Characters/Runtime/PicoChan_KI_Melee_Override.overrideController").read_text()
    if "guid: b7c4e1a09d2f4c6e8a1b3d5f7091c2e4" not in pico_override:
        errors.append("Pico override is not parented to the shared controller")

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
