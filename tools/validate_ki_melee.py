#!/usr/bin/env python3
"""Static checks for the KI melee controller and the 11 girl roster prefabs."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path("/workspace")
CTRL = ROOT / "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller"
GUID = "b7c4e1a09d2f4c6e8a1b3d5f7091c2e4"
ROSTER = [
    "C01_Akio", "C02_Aika", "C04_UnityChan", "C05_Akane", "C06_Cultist",
    "C07_StellarWitch", "C08_SanpakuChan", "C09_PicoChan", "C10_Reika",
    "C12_Fina", "C14_Muryotaisu",
]
REQUIRED = [
    "Hips", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg",
    "LeftFoot", "RightFoot", "Spine", "Chest", "Neck", "Head",
    "LeftShoulder", "RightShoulder", "LeftUpperArm", "RightUpperArm",
    "LeftLowerArm", "RightLowerArm", "LeftHand", "RightHand",
]
# Akane's rig has no chest bone. The map cannot invent one.
CHEST_WAIVER = {"C05_Akane"}


def guids():
    out = {}
    for meta in (ROOT / "Assets").rglob("*.meta"):
        head = meta.read_text(errors="ignore")[:160]
        m = re.search(r"^guid: ([0-9a-f]+)", head, re.M)
        if m:
            out[m.group(1)] = str(meta)[:-5]
    return out


def main():
    errors = []
    warnings = []
    text = CTRL.read_text()
    params = set(re.findall(r"m_Name: ([A-Za-z0-9_]+)\n    m_Type:", text))
    code = (ROOT / "Assets/Scripts").read_text() if False else ""
    triggers = set()
    for path in (ROOT / "Assets/Scripts").rglob("*.cs"):
        src = path.read_text(errors="ignore")
        triggers.update(re.findall(r'TriggerAnimation\(\s*"([A-Za-z0-9_]+)"\s*\)', src))
    moves = (ROOT / "Assets/Scripts/Data/MoveSet.cs").read_text()
    # last string arg of CreateAttack calls inside CreateSamplePreset is the trigger
    sample = re.findall(r'CreateAttack\([^;]*?"([A-Za-z0-9_]+)"\);', moves)
    kai = re.findall(r"animationTrigger: ([A-Za-z0-9_]+)", (ROOT / "Assets/CombatData").read_text() if False else "")
    for asset in (ROOT / "Assets/CombatData").glob("Kai_*.asset"):
        m = re.search(r"animationTrigger: ([A-Za-z0-9_]+)", asset.read_text())
        if m:
            kai.append(m.group(1))
    needed = set(triggers) | set(sample) | set(kai) | {"Speed", "MoveX", "IsGrounded", "IsCrouch", "IsBlocking"}
    missing = sorted(n for n in needed if n not in params)
    if missing:
        errors.append("controller missing parameters: " + ", ".join(missing))

    guid_map = guids()
    motions = re.findall(r"m_Motion: \{fileID: (-?\d+), guid: ([0-9a-f]+), type: 3\}", text)
    if not motions:
        errors.append("no external clip motions")
    for fid, g in motions:
        path = guid_map.get(g)
        if not path or not Path(path).exists():
            errors.append("motion guid missing " + g)
            continue
        meta = Path(path + ".meta").read_text(errors="ignore")
        if f"internalID: {fid}" not in meta and f"internalID: {fid}\n" not in meta:
            # allow the id to appear
            if fid not in meta:
                errors.append(f"fileID {fid} not in {path}")
        if "Attack" in Path(path).name or "Damage" in Path(path).name or "Death" in Path(path).name or "Cast" in Path(path).name:
            if re.search(r"loopTime: 1", meta) and "Load" not in Path(path).name and "Idle" not in Path(path).name:
                warnings.append("attack-like clip loops: " + Path(path).name)

    for state in ("Defeat", "Victory", "Knockdown"):
        block = re.search(rf"m_Name: {state}\n  m_Speed: .+\n  m_CycleOffset: 0\n  m_Transitions: (\[\]|.+)", text)
        if not block or block.group(1) != "[]":
            errors.append(state + " must stay on its last frame (no exit transition)")

    for name in ROSTER:
        pref = (ROOT / "Assets/Prefabs/Characters" / f"{name}.prefab").read_text()
        if GUID not in pref:
            errors.append(name + " animator is not the KI melee controller")
        if "m_ApplyRootMotion: 1" in pref:
            errors.append(name + " has root motion on")
        av = re.search(r"m_Avatar: \{fileID: (-?\d+), guid: ([0-9a-f]+)", pref)
        if not av or av.group(1) != "9000000":
            errors.append(name + " avatar fileID is not the model avatar 9000000")
            continue
        fbx = guid_map.get(av.group(2))
        meta = Path(fbx + ".meta").read_text(errors="ignore") if fbx else ""
        if "animationType: 3" not in meta:
            errors.append(name + " import is not Humanoid (animationType 3)")
        humans = dict(re.findall(r"boneName: (.+)\n\s+humanName: (.+)", meta))
        by_h = {}
        for bone, human in re.findall(r"boneName: (.+)\n\s+humanName: (.+)", meta):
            by_h.setdefault(human.strip(), bone.strip())
        absent = [r for r in REQUIRED if r not in by_h]
        if name in CHEST_WAIVER:
            absent = [a for a in absent if a != "Chest"]
            if "Chest" not in by_h:
                warnings.append("Akane has no chest bone in the rig; spine twist spreads over Spine and Neck")
        if absent:
            errors.append(name + " missing human bones " + ", ".join(absent))
        if name == "C07_StellarWitch":
            if by_h.get("Hips") != "Hip":
                errors.append("StellarWitch Hips is not the pelvis bone Hip")
            if "parentName: Hip" not in meta:
                errors.append("StellarWitch legs are not parented to Hip")

    reika = (ROOT / "Assets/Prefabs/Characters/C10_Reika.prefab").read_text()
    if "guid: e923af2996153434c9c3d00932f0bd12" not in reika:
        errors.append("Reika PlayerController reference missing")
    if not re.search(r"m_Enabled: 0\n  m_EditorHideFlags: 0\n  m_Script: \{fileID: 11500000, guid: e923af2996153434c9c3d00932f0bd12", reika):
        errors.append("Reika PlayerController is still enabled")
    if "m_IsKinematic: 1" not in reika:
        errors.append("Reika Rigidbody is still dynamic")
    muryo = (ROOT / "Assets/Prefabs/Characters/C14_Muryotaisu.prefab").read_text()
    if not re.search(r"m_Enabled: 0\n  m_EditorHideFlags: 0\n  m_Script: \{fileID: 11500000, guid: 0ec9bb896a697864dbb7ffe898debea8", muryo):
        errors.append("MuryotaisuController is still enabled")
    pico = (ROOT / "Assets/Prefabs/Characters/C09_PicoChan.prefab").read_text()
    if pico.count("Animator:") < 2:
        warnings.append("PicoChan no longer has two Animator components to compare")
    if "m_GameObject: {fileID: 2867569409786181483}\n  m_Enabled: 0" not in pico:
        errors.append("PicoChan child Animator is still enabled")

    for excluded in ("C11_Casual1", "C13_SapphiArtchan"):
        pref = (ROOT / "Assets/Prefabs/Characters" / f"{excluded}.prefab").read_text()
        if GUID in pref:
            errors.append(excluded + " was switched onto the KI controller")

    print("parameters", len(params), "code triggers", sorted(triggers))
    print("kai triggers", kai)
    print("sample triggers", sample)
    for w in warnings:
        print("WARN", w)
    for e in errors:
        print("FAIL", e)
    if errors:
        print(f"{len(errors)} failure(s)")
        return 1
    print("PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
