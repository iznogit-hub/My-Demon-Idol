#!/usr/bin/env python3
"""Create shared BlazBlue archetype moves and profiles for fighters that had none.

Also fills empty AnimatorOverrideController slots with warrior locomotion and
Kevin Iglesias attack clips so the KI base controller has no empty match states.
"""
from __future__ import annotations

import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "Assets"
ATTACK_SCRIPT = "1bb693230ba4b4e77b7a5e75144ef62d"
MOVESET_SCRIPT = "c39fbfec8de3c4b33b34c085baa785d9"
BOX_SCRIPT = "32b1969e770e84471af5a83235a29816"
PROFILE_SCRIPT = "08f8045e2503643a2baf0c575397e980"
KI = "b7c4e1a09d2f4c6e8a1b3d5f7091c2e4"

ARCH = {
    "unarmed": dict(startup=5, active=3, recovery=8, damage=280, reach=0.55, hip=0.92, warrior="Karate", ki="Attack"),
    "sword": dict(startup=7, active=4, recovery=12, damage=340, reach=0.85, hip=0.95, warrior="Swordsman", ki="Attack1H"),
    "dual": dict(startup=6, active=3, recovery=10, damage=300, reach=0.75, hip=0.90, warrior="Ninja", ki="Attack1H"),
    "knight": dict(startup=8, active=4, recovery=14, damage=360, reach=0.80, hip=1.05, warrior="Knight", ki="AttackShield"),
    "twohand": dict(startup=12, active=5, recovery=18, damage=480, reach=1.05, hip=1.00, warrior="2Handed", ki="Attack2H"),
    "hammer": dict(startup=14, active=5, recovery=20, damage=520, reach=1.10, hip=1.10, warrior="Hammer", ki="Attack2H"),
    "spear": dict(startup=9, active=3, recovery=14, damage=340, reach=1.15, hip=0.95, warrior="Spearman", ki="Polearm"),
    "mage": dict(startup=10, active=4, recovery=16, damage=300, reach=0.90, hip=1.00, warrior="Mage", ki="MagicAttack"),
    "sorceress": dict(startup=9, active=4, recovery=15, damage=310, reach=0.90, hip=0.94, warrior="Sorceress", ki="MagicAttack"),
    "archer": dict(startup=11, active=3, recovery=16, damage=280, reach=1.20, hip=0.92, warrior="Archer", ki="Polearm"),
    "crossbow": dict(startup=12, active=3, recovery=16, damage=300, reach=1.15, hip=0.92, warrior="Crossbow", ki="Polearm"),
    "brute": dict(startup=13, active=5, recovery=18, damage=500, reach=0.95, hip=1.05, warrior="Brute", ki="Attack2H"),
}

# id, name, archetype, drive, stats (speed power reach defense), override path or None to create
FIGHTERS = [
    ("C05", "Akane", "spear", "Echoing Mandate", (86, 76, 88, 74), "Assets/MDI_Characters/Runtime/BlazBlue/Akane_BB_Override.overrideController"),
    ("C11", "Casual1", "crossbow", "Casual Draw", (84, 74, 80, 72), "Assets/MDI_Characters/Runtime/BlazBlue/Casual1_BB_Override.overrideController"),
    ("C13", "SapphiArtchan", "unarmed", "Idol Beat", (88, 78, 80, 70), None),
    ("C14", "Muryotaisu", "twohand", "Shrine Slash", (96, 99, 92, 94), None),
    ("SK02", "Galahad", "knight", "Aegis", (74, 88, 82, 95), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK02_Galahad_Override.overrideController"),
    ("SK03", "Vex", "brute", "Dune Cleaver", (82, 90, 80, 84), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK03_Vex_Override.overrideController"),
    ("SK05", "LordMorvath", "mage", "Abyss Orbit", (76, 94, 88, 75), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK05_LordMorvath_Override.overrideController"),
    ("SK06", "CommanderRen", "twohand", "Bastion", (72, 95, 90, 92), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK06_CommanderRen_Override.overrideController"),
    ("SK08", "Torgrim", "hammer", "Storm Break", (68, 98, 88, 90), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK08_Torgrim_Override.overrideController"),
    ("EX01", "Hugo", "brute", "Highland Swing", (70, 96, 84, 90), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX01_Hugo_Override.overrideController"),
    ("EX02", "Granite", "hammer", "Granite Drop", (64, 99, 86, 96), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX02_Granite_Override.overrideController"),
    ("EX03", "Viper", "sword", "Viper Edge", (90, 84, 82, 74), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX03_Viper_Override.overrideController"),
    ("EX04", "OfficerK", "unarmed", "Discipline", (86, 82, 74, 80), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX04_OfficerK_Override.overrideController"),
    ("EX05", "Kira", "archer", "Starfall", (91, 86, 95, 72), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX05_Kira_Override.overrideController"),
    ("EX06", "Sentry", "spear", "Phalanx", (75, 88, 98, 90), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX06_Sentry_Override.overrideController"),
    ("EX07", "Lyra", "sorceress", "Aether", (88, 89, 84, 78), "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX07_Lyra_Override.overrideController"),
]

MOVES = [
    ("5A", 1, 0, 0, "LightAttack", 0.70, 0),
    ("5B", 2, 0, 0, "MediumAttack", 1.00, 2),
    ("5C", 4, 0, 0, "HeavyAttack", 1.35, 4),
    ("2A", 1, 0, 0, "LowAttack", 0.65, 0),
    ("2B", 2, 0, 0, "LowAttack", 0.90, 1),
    ("2C", 4, 0, 0, "LowSweepAttack", 1.20, 3),
    ("6A", 1, 1, 0, "OverheadAttack", 0.85, 2),
    ("6B", 2, 1, 0, "OverheadAttack", 1.05, 3),
    ("6C", 4, 1, 0, "LauncherAttack", 1.30, 4),
    ("3C", 4, 1, 0, "LowSweepAttack", 1.15, 3),
    ("j.A", 1, 0, 0, "AirLight", 0.70, 0),
    ("j.B", 2, 0, 0, "JumpAttack", 1.00, 1),
    ("j.C", 4, 0, 0, "AirHeavy", 1.25, 2),
    ("5D", 8, 3, 0, "DriveSkill", 1.10, 2),
    ("2D", 8, 3, 0, "DriveSkill", 0.95, 2),
    ("6D", 8, 3, 0, "AdvancingSkill", 1.15, 3),
    ("j.D", 8, 3, 0, "JumpAttack", 1.05, 2),
    ("236A", 1, 2, 1, "DriveSkill", 1.20, 4),
    ("214A", 1, 2, 2, "AdvancingSkill", 1.15, 4),
    ("623B", 2, 2, 3, "LauncherAttack", 1.40, 6),
    ("22C", 4, 2, 12, "HeavyAttack", 1.30, 5),
    ("236236C", 4, 4, 7, "DomainSuper", 2.40, 8),
    ("BC", 6, 6, 0, "Throw", 1.50, 2),
]


def guid_for(text: str) -> str:
    return uuid.uuid5(uuid.UUID("c3a7e1b9-4d2f-4a68-b5e0-d7c81f6a4902"), text).hex


def meta(guid: str, main_id: str) -> str:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        f"{main_id}\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def native_meta(guid: str) -> str:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 22100000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def write(path: Path, text: str, guid: str, native=False) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text)
    meta_path = Path(str(path) + ".meta")
    if not meta_path.exists():
        meta_path.write_text(native_meta(guid) if native else meta(guid, "NativeFormatImporter:" if native else "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"))


def mono_meta(guid: str) -> str:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "MonoImporter:\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 2\n"
        "  defaultReferences: []\n"
        "  executionOrder: 0\n"
        "  icon: {instanceID: 0}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def index_guids() -> dict:
    out = {}
    for meta_path in ASSETS.rglob("*.meta"):
        text = meta_path.read_text(errors="ignore")
        m = re.search(r"^guid: ([0-9a-f]{32})", text, re.M)
        if not m:
            continue
        asset = meta_path.with_suffix("")
        out[m.group(1)] = asset
    return out


def find_clip(pred) -> str | None:
    for path in ASSETS.rglob("*.FBX"):
        if pred(path.name):
            meta_path = Path(str(path) + ".meta")
            if meta_path.exists():
                m = re.search(r"^guid: ([0-9a-f]{32})", meta_path.read_text(errors="ignore"), re.M)
                if m:
                    return m.group(1)
    for path in ASSETS.rglob("*.fbx"):
        if pred(path.name):
            meta_path = Path(str(path) + ".meta")
            if meta_path.exists():
                m = re.search(r"^guid: ([0-9a-f]{32})", meta_path.read_text(errors="ignore"), re.M)
                if m:
                    return m.group(1)
    return None


def warrior_set(token: str) -> dict:
    def one(suffix):
        return find_clip(lambda n: token in n and suffix in n)

    return {
        "idle": one("Idle"),
        "walk": one("Walk.FBX") or one("Walk."),
        "jump": one("Jump"),
        "fall": one("Fall"),
        "attack": one("Attack1"),
    }


def ki_clip(token: str) -> str | None:
    root = ASSETS / "Kevin Iglesias"
    if not root.exists():
        return None
    for path in root.rglob("*.fbx"):
        if token.lower() in path.name.lower() and "Mask" not in str(path):
            meta_path = Path(str(path) + ".meta")
            m = re.search(r"^guid: ([0-9a-f]{32})", meta_path.read_text(errors="ignore"), re.M)
            if m:
                return m.group(1)
    return None


def clip_ref(guid: str) -> str:
    return "{fileID: 7400000, guid: " + guid + ", type: 3}"


def attack_yaml(name, move_id, button, kind, motion, trigger, startup, active, recovery, damage, reach, hip, clip_guid, box_guid) -> str:
    air = 1 if move_id.startswith("j.") else 0
    ground = 0 if move_id.startswith("j.") else 1
    guard = 1 if move_id.startswith("2") else (2 if move_id.startswith("6") else 0)
    attr = 4 if move_id.startswith("2") else 2
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ATTACK_SCRIPT}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: Assembly-CSharp::DemonIdol.BlazBlue.BBAttackData
  attackName: {move_id}
  attackType: 0
  hitLevel: 1
  guardRequirement: {guard}
  isDuckableHigh: 0
  crush: 0
  crushStartTick: 0
  crushEndTick: 0
  hasArmor: 0
  isAirborneStrikeProtected: 0
  protectionStartTick: 0
  protectionEndTick: 0
  startupFrames: {startup}
  activeFrames: {active}
  recoveryFrames: {recovery}
  hitstunTicks: {12 + active}
  blockstunTicks: {8 + active}
  knockdownDurationTicks: 36
  hitstopAttacker: 6
  hitstopDefender: 6
  hitstopOnBlock: 4
  counterHitStopBonus: 4
  advantageOnBlock: -2
  advantageOnHit: 2
  advantageOnCounterHit: 6
  damage: {damage}
  chipDamageOnBlock: 0
  initialProration: 0.9
  comboDamageDecay: 0.92
  pushbackOnHit: 1
  pushbackOnBlock: 0.6
  isLauncher: 0
  isBoundTornado: 0
  causesWallSplat: 0
  causesWallBounce: 0
  causesKnockdown: 0
  isLinear: 1
  throwTechWindowTicks: 6
  launchVelocity: {{x: 0, y: 0}}
  screenShakeTrauma: 0.1
  canRapidCancel: 1
  gatlingCancels: 
  animationClip: {clip_ref(clip_guid)}
  animationTrigger: {trigger}
  hitboxOffset: {{x: {reach:.2f}, y: {hip:.2f}, z: 0}}
  hitboxSize: {{x: 0.8, y: 0.6, z: 0.6}}
  cancelWindow: {{x: {startup}, y: {startup + active}}}
  archetype: 0
  moveId: {move_id}
  button: {button}
  kind: {kind}
  motion: {motion}
  airOk: {air}
  groundOk: {ground}
  guard: {guard}
  attribute: {attr}
  invuln: []
  fatalCounter: 0
  hardKnockdown: 0
  otg: 0
  groundBounce: 0
  wallBounce: 0
  wallStick: 0
  groundSlide: 0
  untechableTicks: 18
  untechableTicksCH: 27
  airHitVelocity: {{x: 1.2, y: 4.5}}
  landingRecovery: 4
  minDamagePct: 0.08
  chipPct: 0
  heatGainOverride: -1
  heatCost: 0
  gatlingInto:
  - 5A
  - 5B
  - 5C
  - 2A
  - 236A
  - 236236C
  specialCancel: 1
  ddCancel: 1
  jumpCancelOnHit: 0
  jumpCancelOnBlock: 0
  rapidCancelable: 1
  followUps: []
  followUpWindow: {{x: 0, y: 0}}
  boxes: {{fileID: 11400000, guid: {box_guid}, type: 2}}
  motionTrack: {{fileID: 0}}
  clipName: {name}
  clip: {clip_ref(clip_guid)}
  clipFirstFrame: 0
  clipLastFrame: {startup + active + recovery}
  events: []
"""


def box_yaml(name, startup, active, recovery, reach, hip) -> str:
    total = startup + active + recovery
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
             "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}",
             "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}",
             "  m_Enabled: 1", "  m_EditorHideFlags: 0",
             f"  m_Script: {{fileID: 11500000, guid: {BOX_SCRIPT}, type: 3}}",
             f"  m_Name: {name}",
             "  m_EditorClassIdentifier: MDI.BlazBlue.Core::DemonIdol.BlazBlue.BBBoxTrack",
             f"  tickCount: {total}", "  frameStart:"]
    cursor = 0
    starts = []
    boxes = []
    for t in range(total):
        starts.append(cursor)
        if startup <= t < startup + active:
            boxes.append((reach, hip, 0.28 + reach * 0.2, 0.32))
            cursor += 1
    starts.append(cursor)
    for value in starts:
        lines.append(f"  - {value}")
    lines.append("  boxes:")
    for cx, cy, hx, hy in boxes:
        lines.append(f"  - cx: {cx:.2f}")
        lines.append(f"    cy: {cy:.2f}")
        lines.append(f"    hx: {hx:.2f}")
        lines.append(f"    hy: {hy:.2f}")
        lines.append("    kind: 1")
        lines.append("    hitIndex: 0")
        lines.append("    attribute: 2")
        lines.append("    flags: 0")
    lines.append("  keyTicks: []")
    lines.append("  bakeHash: archetype")
    return "\n".join(lines) + "\n"


def build_archetype(key: str, spec: dict, ki_guid: str, warrior_attack: str) -> str:
    folder = ASSETS / "CombatData" / "BlazBlue" / "Archetypes" / key
    move_guids = []
    clip = ki_guid or warrior_attack
    for move_id, button, kind, motion, trigger, scale, extra in MOVES:
        startup = spec["startup"] + extra
        active = spec["active"]
        recovery = spec["recovery"]
        damage = max(40, int(spec["damage"] * scale))
        safe = move_id.replace(".", "_")
        box_guid = guid_for("box:" + key + ":" + move_id)
        atk_guid = guid_for("atk:" + key + ":" + move_id)
        box_path = folder / "BoxTracks" / f"Box_{key}_{safe}.asset"
        atk_path = folder / f"BB_{key}_{safe}.asset"
        box_path.parent.mkdir(parents=True, exist_ok=True)
        box_path.write_text(box_yaml(f"Box_{key}_{safe}", startup, active, recovery, spec["reach"], spec["hip"]))
        Path(str(box_path) + ".meta").write_text(mono_meta(box_guid))
        atk_path.write_text(attack_yaml(f"BB_{key}_{safe}", move_id, button, kind, motion, trigger, startup, active, recovery, damage, spec["reach"], spec["hip"], clip, box_guid))
        Path(str(atk_path) + ".meta").write_text(mono_meta(atk_guid))
        move_guids.append(atk_guid)
    ms_guid = guid_for("moveset:" + key)
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
             "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}",
             "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}",
             "  m_Enabled: 1", "  m_EditorHideFlags: 0",
             f"  m_Script: {{fileID: 11500000, guid: {MOVESET_SCRIPT}, type: 3}}",
             f"  m_Name: BBMoveSet_{key}",
             "  m_EditorClassIdentifier: Assembly-CSharp::DemonIdol.BlazBlue.BBMoveSet",
             f"  characterId: {key}", "  moves:"]
    for g in move_guids:
        lines.append(f"  - {{fileID: 11400000, guid: {g}, type: 2}}")
    for field in ("throwForward", "throwBack", "throwAir", "counterAssault", "crushTrigger", "burst", "overdriveStart", "exceedAccel", "astralHeat"):
        lines.append(f"  {field}: {{fileID: 0}}")
    ms_path = folder / f"BBMoveSet_{key}.asset"
    ms_path.write_text("\n".join(lines) + "\n")
    Path(str(ms_path) + ".meta").write_text(mono_meta(ms_guid))
    return ms_guid


def ensure_override(path: Path, name: str) -> str:
    meta_path = Path(str(path) + ".meta")
    if path.exists() and meta_path.exists():
        m = re.search(r"^guid: ([0-9a-f]{32})", meta_path.read_text(), re.M)
        return m.group(1)
    template = (ASSETS / "MDI_Characters/Runtime/BlazBlue/Casual1_BB_Override.overrideController").read_text()
    template = template.replace("m_Name: Casual1_BB_Override", f"m_Name: {name}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(template)
    guid = guid_for("override:" + name)
    meta_path.write_text(native_meta(guid))
    return guid


def fill_overrides(by_guid: dict, pools: dict) -> int:
    filled = 0
    for path in (ASSETS / "MDI_Characters/Runtime/BlazBlue").rglob("*.overrideController"):
        key = "unarmed"
        for fid, _name, arch, *_rest in FIGHTERS:
            if fid in path.name or _name.split("Art")[0] in path.name:
                key = arch
        text = path.read_text()
        pool = [g for g in pools.get(key, []) if g]
        if not pool:
            continue
        idx = 0

        def repl(match):
            nonlocal idx, filled
            original = match.group(1)
            asset = by_guid.get(original)
            name = asset.name.lower() if asset else ""
            chosen = pool[idx % len(pool)]
            if "idle" in name and pools[key][0]:
                chosen = pools[key][0]
            elif "walk" in name or "run" in name:
                chosen = pools[key][1] or chosen
            elif "jump" in name:
                chosen = pools[key][2] or chosen
            elif "fall" in name or "land" in name:
                chosen = pools[key][3] or chosen
            else:
                idx += 1
            filled += 1
            return match.group(0).replace("{fileID: 0}", clip_ref(chosen).replace("{", "{").split("fileID: ")[0] and clip_ref(chosen))

        # The replacement above is messy. Do a line-oriented pass instead.
        lines = text.splitlines()
        out = []
        pending_original = None
        for line in lines:
            m = re.search(r"m_OriginalClip: \{fileID: [^,]+, guid: ([0-9a-f]{32})", line)
            if m:
                pending_original = m.group(1)
            if "m_OverrideClip: {fileID: 0}" in line and pending_original is not None:
                asset = by_guid.get(pending_original)
                name = asset.name.lower() if asset else ""
                chosen = pool[idx % len(pool)]
                if "idle" in name and len(pool) > 0:
                    chosen = pool[0]
                elif any(tok in name for tok in ("walk", "run")) and len(pool) > 1:
                    chosen = pool[1]
                elif "jump" in name and len(pool) > 2:
                    chosen = pool[2]
                elif any(tok in name for tok in ("fall", "land")) and len(pool) > 3:
                    chosen = pool[3]
                else:
                    chosen = pool[4 + (idx % max(1, len(pool) - 4))] if len(pool) > 4 else pool[idx % len(pool)]
                    idx += 1
                line = f"    m_OverrideClip: {clip_ref(chosen)}"
                filled += 1
                pending_original = None
            out.append(line)
        path.write_text("\n".join(out) + "\n")
    return filled


def patch_definition(fid: str, profile_guid: str, stats) -> None:
    matches = list((ASSETS / "CombatData/Characters").glob(f"CharacterDefinition_{fid}_*.asset"))
    if not matches:
        raise SystemExit("missing definition " + fid)
    path = matches[0]
    text = path.read_text()
    speed, power, reach, defense = stats
    text = re.sub(r"speed: \d+", f"speed: {speed}", text, count=1)
    text = re.sub(r"power: \d+", f"power: {power}", text, count=1)
    text = re.sub(r"reach: \d+", f"reach: {reach}", text, count=1)
    text = re.sub(r"defense: \d+", f"defense: {defense}", text, count=1)
    profile_line = f"  blazBlueProfile: {{fileID: 11400000, guid: {profile_guid}, type: 2}}"
    if "blazBlueProfile:" in text:
        text = re.sub(r"  blazBlueProfile: \{fileID: 0\}", profile_line, text)
        text = re.sub(r"  blazBlueProfile: \{fileID: 11400000, guid: [0-9a-f]+, type: 2\}", profile_line, text)
    else:
        text = text.replace("  ufeCharacterName:", profile_line + "\n  ufeCharacterName:")
    path.write_text(text)


def main() -> None:
    by_guid = index_guids()
    pools = {}
    movesets = {}
    for key, spec in ARCH.items():
        w = warrior_set(spec["warrior"])
        ki = ki_clip(spec["ki"]) or w["attack"]
        pools[key] = [w["idle"], w["walk"], w["jump"], w["fall"], w["attack"], ki]
        pools[key] = [g for g in pools[key] if g]
        movesets[key] = build_archetype(key, spec, ki, w["attack"] or ki)
        print(key, "clips", len(pools[key]), "moveset", movesets[key])

    for fid, name, arch, drive, stats, override in FIGHTERS:
        if override is None:
            filename = f"{name}_BB_Override.overrideController"
            override_path = ASSETS / "MDI_Characters/Runtime/BlazBlue" / filename
            override_guid = ensure_override(override_path, Path(filename).stem)
        else:
            override_path = ROOT / override
            override_guid = ensure_override(override_path, override_path.stem)
        profile_guid = guid_for("profile:" + fid)
        profile = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {PROFILE_SCRIPT}, type: 3}}
  m_Name: BBProfile_{fid}_{name}
  m_EditorClassIdentifier: MDI.BlazBlue.Core::DemonIdol.BlazBlue.BBCharacterProfile
  characterId: {fid}_{name}
  characterName: {name}
  driveName: {drive}
  forwardWalkSpeed: 3.4
  backwardWalkSpeed: 2.6
  forwardDashSpeed: 6.2
  forwardDashDurationTicks: 12
  backdashSpeed: 6.8
  backdashDurationTicks: 14
  backdashInvulnTicks: 5
  jumpVerticalLaunch: 8
  jumpHorizontalSpeed: 3
  gravity: 20
  standPushboxSize: {{x: 0.7, y: 1.8}}
  crouchPushboxSize: {{x: 0.8, y: 1.2}}
  airPushboxSize: {{x: 0.6, y: 1.3}}
  blazBlueOverrideController: {{fileID: 22100000, guid: {override_guid}, type: 2}}
  moveSet: {{fileID: 11400000, guid: {movesets[arch]}, type: 2}}
  maxHealth: 11000
  defenseModifier: 1
"""
        profile_path = ASSETS / "CombatData/BlazBlue/Archetypes" / arch / f"BBProfile_{fid}_{name}.asset"
        profile_path.write_text(profile)
        Path(str(profile_path) + ".meta").write_text(mono_meta(profile_guid))
        patch_definition(fid, profile_guid, stats)
        print("profile", fid, profile_guid)

    # Reindex after new metas, then fill every BlazBlue override.
    by_guid = index_guids()
    filled = fill_overrides(by_guid, pools)
    print("filled override slots", filled)


if __name__ == "__main__":
    main()
