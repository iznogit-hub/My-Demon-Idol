#!/usr/bin/env python3
"""Write MDI_KI_Melee_Fighter.controller and point the 11 girl prefabs at it.

Clip lengths use the importer lastFrame. Speeds assume 30 fps clips and the
game's 60 Hz ticks: speed = (frames/30) / (ticks/60). Confirm lengths in Unity.
"""
from __future__ import annotations

from pathlib import Path

ROOT = Path("/workspace")
CTRL = ROOT / "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller"
GUID = "b7c4e1a09d2f4c6e8a1b3d5f7091c2e4"
CLIP = "3094330708855449807"

# name, guid, fileID, frames
C = {
    "idle": ("0f69fd4f4ad106447896fb775199ed37", CLIP, 81),
    "idle3": ("34ced3481cc3833478728f31579a88c9", CLIP, 80),
    "runF": ("40786e67f3ceb094b9c00543f295cb5f", CLIP, 18),
    "runB": ("b7ff898919f115a4095d79af0129756f", CLIP, 18),
    "strafeR": ("257eb7fb2a8472f4f9302dd6a641828f", CLIP, 18),
    "atkR": ("408142c06e2d48c4db8065d1fbbcc8eb", CLIP, 42),
    "atkL": ("b3d13322675e10d47aa75892a620c7bc", CLIP, 33),
    "atk2": ("48fa84abc2c3a744e8561044d83beb93", CLIP, 48),
    "pole": ("8427f593ea039e74fb73c8088e896c9a", CLIP, 41),
    "shield": ("6683d02f31b83724daa4714c986bd1f5", CLIP, 27),
    "hit": ("33ead5f6ecd30db40bc9209ff361f049", CLIP, 30),
    "castHit": ("79cdddc1602686641a39ff115c631faa", CLIP, 33),
    "death": ("b09be4f98e05fa7489d148a376fc0f1c", "5957932628511814003", 22),
    "rise": ("b3b7631decc86f04bb15e95e6e0c6a0c", CLIP, 36),
    "hop": ("38a45c96315321348952eec2abacbd04", CLIP, 21),
    "magic2": ("afc0d7b05fc184d49a7e863d9eec0f9e", CLIP, 48),
    "magic1": ("5e777c22f4914c24bb1ab4df99f3832e", CLIP, 36),
    "magicO": ("6ce5c8cc7157f2542b78acd83e609475", CLIP, 47),
    "guard": ("c163a36c98396ee4488ff914e337cf3c", CLIP, 40),
    "idleCombat": ("6abe61edacb78c74489739f89ec126bc", CLIP, 40),
}


def spd(frames: int, ticks: int) -> float:
    return round((frames / 30.0) / (ticks / 60.0), 3)


def motion(key: str) -> str:
    guid, fid, _ = C[key]
    return f"{{fileID: {fid}, guid: {guid}, type: 3}}"


# name, clip, speed, triggers (any-state), kind
# kind: loco, action (exit time back), stay, block, crouch, getup
STATES = [
    ("Locomotion", None, 1.0, [], "loco"),
    ("Crouch", "idleCombat", 1.0, [], "crouch"),
    ("Block", "guard", 1.0, ["StandBlock", "CrouchBlock"], "block"),
    ("LightAttack", "atkR", spd(42, 18), ["LightAttack"], "action"),
    ("MediumAttack", "atkL", spd(33, 33), ["MediumAttack"], "action"),
    ("HeavyAttack", "atk2", spd(48, 34), ["HeavyAttack"], "action"),
    ("LowAttack", "atkL", spd(33, 20), ["LowAttack"], "action"),
    ("LowSweepAttack", "atk2", spd(48, 43), ["LowSweepAttack"], "action"),
    ("OverheadAttack", "atk2", spd(48, 48), ["OverheadAttack"], "action"),
    ("DriveSkill", "pole", spd(41, 39), ["DriveSkill"], "action"),
    ("AdvancingSkill", "pole", spd(41, 46), ["AdvancingSkill"], "action"),
    ("LauncherAttack", "pole", spd(41, 42), ["LauncherAttack"], "action"),
    ("ThrowCard", "magic1", spd(36, 38), ["ThrowCard"], "action"),
    ("Throw", "shield", spd(27, 33), ["Throw"], "action"),
    ("JumpAttack", "atkR", spd(42, 32), ["JumpAttack"], "action"),
    ("AirLight", "atkR", spd(42, 24), ["AirLight"], "action"),
    ("AirHeavy", "atk2", spd(48, 29), ["AirHeavy"], "action"),
    ("DomainSuper", "magic2", spd(48, 48), ["DomainSuper"], "action"),
    ("Burst", "magicO", spd(47, 39), ["Burst", "BurstShockwave"], "action"),
    ("Jump", "hop", 1.0, ["Jump"], "action"),
    ("ForwardDash", "runF", spd(18, 12), ["ForwardDash"], "action"),
    ("Backdash", "runB", spd(18, 14), ["Backdash"], "action"),
    ("Parry", "atkR", spd(42, 19), ["Parry"], "action"),
    ("Sidestep", "strafeR", spd(18, 14), ["Sidestep"], "action"),
    ("RapidCancel", "rise", spd(36, 12), ["RapidCancel"], "action"),
    ("HitMid", "hit", spd(30, 16), ["HitMid", "Hit"], "action"),
    ("HitLow", "castHit", spd(33, 16), ["HitLow"], "action"),
    ("HitHigh", "hit", spd(30, 16), ["HitHigh"], "action"),
    ("Launch", "hit", spd(30, 20), ["Launch"], "action"),
    ("BoundTornado", "castHit", spd(33, 24), ["BoundTornado"], "action"),
    ("WallSplat", "hit", spd(30, 20), ["WallSplat"], "action"),
    ("Knockdown", "death", spd(22, 36), ["Knockdown"], "stay"),
    ("StandUpBack", "rise", spd(36, 14), ["StandUpBack"], "getup"),
    ("Defeat", "death", 1.0, ["Defeat"], "stay"),
    ("Victory", "idle3", 1.0, ["Victory"], "stay"),
    ("Intro", "hop", 1.0, ["Intro"], "action"),
]

FLOATS = ["Speed", "MoveX"]
BOOLS = ["IsGrounded", "IsCrouch", "IsBlocking"]


def main():
    triggers = []
    for _n, _c, _s, trigs, _k in STATES:
        for t in trigs:
            if t not in triggers:
                triggers.append(t)
    for extra in ("Idle",):
        if extra not in triggers:
            triggers.append(extra)

    sid = {}
    n = 110200001
    for name, *_ in STATES:
        sid[name] = n
        n += 1
    blend_id = 206000001
    loco_id = sid["Locomotion"]
    parts = []
    any_ids = []
    exit_ids = []

    def transition(dst, conditions, has_exit, exit_time, duration, can_self, fixed=1):
        nonlocal n
        tid = 110100000 + len(any_ids) + len(exit_ids) + 1
        # unique: use a counter stored on function
        transition.i += 1
        tid = 110100000 + transition.i
        cond = ""
        if conditions:
            lines = []
            for mode, event, thresh in conditions:
                lines.append(
                    "  - m_ConditionMode: %d\n    m_ConditionEvent: %s\n    m_EventTreshold: %s"
                    % (mode, event, thresh)
                )
            cond = "\n".join(lines) + "\n"
        else:
            cond = " []\n" if False else ""
        # conditions block
        if conditions:
            cond_yaml = "\n" + "\n".join(
                "  - m_ConditionMode: %d\n    m_ConditionEvent: %s\n    m_EventTreshold: %s" % c
                for c in conditions
            )
        else:
            cond_yaml = " []"
        parts.append(
            f"""--- !u!1101 &{tid}
AnimatorStateTransition:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: 
  m_Conditions:{cond_yaml}
  m_DstStateMachine: {{fileID: 0}}
  m_DstState: {{fileID: {dst}}}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: {duration}
  m_TransitionOffset: 0
  m_ExitTime: {exit_time}
  m_HasExitTime: {1 if has_exit else 0}
  m_HasFixedDuration: {fixed}
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: {1 if can_self else 0}
"""
        )
        return tid

    transition.i = 0

    # any-state triggers first
    for name, _clip, _speed, trigs, kind in STATES:
        for trig in trigs:
            any_ids.append(transition(sid[name], [(1, trig, 0)], False, 0.9, 0.04, True))
    # Idle returns to locomotion (round reset)
    any_ids.append(transition(loco_id, [(1, "Idle", 0)], False, 0.9, 0.05, True))
    # blocking / crouch after attacks so an attack trigger wins the same frame
    any_ids.append(transition(sid["Block"], [(1, "IsBlocking", 0)], False, 0.9, 0.05, False))
    any_ids.append(transition(sid["Crouch"], [(1, "IsCrouch", 0)], False, 0.9, 0.08, False))

    state_yaml = []
    child_states = []
    y = 0
    for name, clip, speed, _trigs, kind in STATES:
        exits = []
        if kind == "action":
            exits.append(transition(loco_id, [], True, 0.92, 0.06, False))
        elif kind == "getup":
            exits.append(transition(loco_id, [], True, 0.9, 0.08, False))
        elif kind == "block":
            exits.append(transition(loco_id, [(2, "IsBlocking", 0)], False, 0.9, 0.06, False))
        elif kind == "crouch":
            exits.append(transition(loco_id, [(2, "IsCrouch", 0)], False, 0.9, 0.08, False))
        # stay: no exit (defeat, victory, knockdown holds last frame)
        if kind == "loco":
            motion_ref = f"{{fileID: {blend_id}}}"
        else:
            motion_ref = motion(clip)
        trans_yaml = "\n".join(f"  - {{fileID: {e}}}" for e in exits) if exits else " []"
        if exits:
            trans_block = "\n" + trans_yaml
        else:
            trans_block = " []"
        state_yaml.append(
            f"""--- !u!1102 &{sid[name]}
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Speed: {speed}
  m_CycleOffset: 0
  m_Transitions:{trans_block}
  m_StateMachineBehaviours: []
  m_Position: {{x: 50, y: 50, z: 0}}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {motion_ref}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
"""
        )
        child_states.append(
            f"""  - serializedVersion: 1
    m_State: {{fileID: {sid[name]}}}
    m_Position: {{x: {240 if kind != 'loco' else 0}, y: {y}, z: 0}}"""
        )
        y += 40

    params = []
    for p in FLOATS:
        params.append(
            f"""  - m_Name: {p}
    m_Type: 1
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {{fileID: 9100000}}"""
        )
    for p in BOOLS:
        params.append(
            f"""  - m_Name: {p}
    m_Type: 4
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {{fileID: 9100000}}"""
        )
    for p in triggers:
        params.append(
            f"""  - m_Name: {p}
    m_Type: 9
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {{fileID: 9100000}}"""
        )

    any_yaml = "\n".join(f"  - {{fileID: {i}}}" for i in any_ids)
    blend = f"""--- !u!206 &{blend_id}
BlendTree:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: WalkBlend
  m_Childs:
  - serializedVersion: 2
    m_Motion: {motion('runB')}
    m_Threshold: -1
    m_Position: {{x: 0, y: 0}}
    m_TimeScale: 1
    m_CycleOffset: 0
    m_DirectBlendParameter: 
    m_Mirror: 0
  - serializedVersion: 2
    m_Motion: {motion('idle')}
    m_Threshold: 0
    m_Position: {{x: 0, y: 0}}
    m_TimeScale: 1
    m_CycleOffset: 0
    m_DirectBlendParameter: 
    m_Mirror: 0
  - serializedVersion: 2
    m_Motion: {motion('runF')}
    m_Threshold: 1
    m_Position: {{x: 0, y: 0}}
    m_TimeScale: 1
    m_CycleOffset: 0
    m_DirectBlendParameter: 
    m_Mirror: 0
  m_BlendParameter: MoveX
  m_BlendParameterY: Speed
  m_MinThreshold: -1
  m_MaxThreshold: 1
  m_UseAutomaticThresholds: 0
  m_NormalizedBlendValues: 0
  m_BlendType: 0
"""
    sm = f"""--- !u!1107 &110700000
AnimatorStateMachine:
  serializedVersion: 7
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Base Layer
  m_ChildStates:
{chr(10).join(child_states)}
  m_ChildStateMachines: []
  m_AnyStateTransitions:
{any_yaml}
  m_EntryTransitions: []
  m_StateMachineTransitions: {{}}
  m_StateMachineBehaviours: []
  m_AnyStatePosition: {{x: -80, y: 0, z: 0}}
  m_EntryPosition: {{x: -80, y: 80, z: 0}}
  m_ExitPosition: {{x: 520, y: 80, z: 0}}
  m_ParentStateMachinePosition: {{x: 520, y: 0, z: 0}}
  m_DefaultState: {{fileID: {loco_id}}}
"""
    header = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!91 &9100000
AnimatorController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MDI_KI_Melee_Fighter
  serializedVersion: 6
  m_AnimatorParameters:
{chr(10).join(params)}
  m_AnimatorLayers:
  - serializedVersion: 5
    m_Name: Base Layer
    m_StateMachine: {{fileID: 110700000}}
    m_Mask: {{fileID: 0}}
    m_Motions: []
    m_Behaviours: []
    m_BlendingMode: 0
    m_SyncedLayerIndex: -1
    m_DefaultWeight: 0
    m_IKPass: 0
    m_SyncedLayerAffectsTiming: 0
    m_Controller: {{fileID: 9100000}}
  m_EvaluateTransitionsOnStart: 0
"""
    text = header + "\n" + sm + "\n" + "\n".join(state_yaml) + "\n" + blend + "\n" + "\n".join(parts)
    CTRL.write_text(text)
    meta = CTRL.with_suffix(CTRL.suffix + ".meta")
    if not meta.exists():
        meta.write_text(
            f"""fileFormatVersion: 2
guid: {GUID}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 9100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        )
    new = f"m_Controller: {{fileID: 9100000, guid: {GUID}, type: 2}}"
    replacements = {
        "C01_Akio.prefab": "m_Controller: {fileID: 22100000, guid: a1a1a1a1b1b1b1b1c1c1c1c1d1d1d1d1, type: 2}",
        "C02_Aika.prefab": "m_Controller: {fileID: 9100000, guid: d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6, type: 2}",
        "C04_UnityChan.prefab": "m_Controller: {fileID: 9100000, guid: 8d5a0e0d58f4e4176a844a1a03976c19, type: 2}",
        "C05_Akane.prefab": "m_Controller: {fileID: 9100000, guid: 6cd923470842dba4389833b007642aeb, type: 2}",
        "C06_Cultist.prefab": "m_Controller: {fileID: 9100000, guid: d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6, type: 2}",
        "C07_StellarWitch.prefab": "m_Controller: {fileID: 9100000, guid: d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6, type: 2}",
        "C08_SanpakuChan.prefab": "m_Controller: {fileID: 9100000, guid: d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6, type: 2}",
        "C09_PicoChan.prefab": "m_Controller: {fileID: 9100000, guid: d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6, type: 2}",
        "C10_Reika.prefab": "m_Controller: {fileID: 9100000, guid: c6d4f450a67e9844c888277a1e29bde3, type: 2}",
        "C12_Fina.prefab": "m_Controller: {fileID: 9100000, guid: 7eee5fd5c768b41ac843cfa3f2adb30e, type: 2}",
        "C14_Muryotaisu.prefab": "m_Controller: {fileID: 9100000, guid: 54a83325115ce584ea067268c4a12301, type: 2}",
    }
    for fn, old in replacements.items():
        path = ROOT / "Assets/Prefabs/Characters" / fn
        textp = path.read_text()
        count = textp.count(old)
        if count < 1:
            raise SystemExit(f"controller line missing in {fn}")
        path.write_text(textp.replace(old, new))
        print(f"{fn}: replaced {count} controller ref(s)")
    print("controller", CTRL, "bytes", CTRL.stat().st_size, "triggers", len(triggers))


if __name__ == "__main__":
    main()
