#!/usr/bin/env python3
"""
Adds a kinematic Rigidbody (isKinematic=1, useGravity=0, collisionDetection=2)
directly to the Hitbox GameObject across all 16 Hero prefabs if not already present.
"""

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

PREFABS = [
    f"Assets/Prefabs/Characters/Sidekicks/SK0{i}_{name}.prefab" for i, name in [
        (1, "Valen"), (2, "Galahad"), (3, "Vex"), (4, "Jin"),
        (5, "LordMorvath"), (6, "CommanderRen"), (7, "Malakar"), (8, "Torgrim")
    ]
] + [
    f"Assets/Prefabs/Characters/DotaHeroes/EX0{i}_{name}.prefab" for i, name in [
        (1, "Hugo"), (2, "Granite"), (3, "Viper"), (4, "OfficerK"),
        (5, "Kira"), (6, "Sentry"), (7, "Lyra"), (8, "Garrick")
    ]
]

def patch_prefab(path_rel: str):
    full_path = ROOT / path_rel
    if not full_path.exists():
        print(f"Skipping missing: {path_rel}")
        return

    content = full_path.read_text(encoding="utf-8")
    if "--- !u!54 &" in content and "m_CollisionDetection: 2" in content:
        print(f"Already has Rigidbody: {Path(path_rel).name}")
        return

    # Find the GameObject with m_Name: Hitbox
    # Regex to find:
    # --- !u!1 &<GO_ID>
    # GameObject:
    # ...
    #   m_Name: Hitbox
    pattern = re.compile(r"--- !u!1 &(\d+)\nGameObject:\n.*?\n  m_Component:\n(.*?)\n  m_Layer: \d+\n  m_Name: Hitbox\b", re.DOTALL)
    match = pattern.search(content)
    if not match:
        print(f"Could not find Hitbox GameObject in: {Path(path_rel).name}")
        return

    go_id = match.group(1)
    components_block = match.group(2)

    # Derive unique ID for Rigidbody by using go_id with prefix or hash
    rb_id = str(int(go_id) + 54000)
    # Ensure rb_id doesn't already exist in content
    while f"&{rb_id}" in content:
        rb_id = str(int(rb_id) + 1)

    # Add component entry to Hitbox GameObject
    new_components_block = components_block + f"\n  - component: {{fileID: {rb_id}}}"
    old_match_str = match.group(0)
    new_match_str = old_match_str.replace(components_block, new_components_block)

    # Construct the Rigidbody block
    rb_block = f"""
--- !u!54 &{rb_id}
Rigidbody:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 4
  m_Mass: 1
  m_Drag: 0
  m_AngularDrag: 0.05
  m_CenterOfMass: {{x: 0, y: 0, z: 0}}
  m_InertiaTensor: {{x: 1, y: 1, z: 1}}
  m_InertiaRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ImplicitCom: 1
  m_ImplicitTensor: 1
  m_UseGravity: 0
  m_IsKinematic: 1
  m_Interpolate: 0
  m_Constraints: 0
  m_CollisionDetection: 2
"""
    # Replace in content and append rb_block at end
    content = content.replace(old_match_str, new_match_str, 1)
    content = content + rb_block

    full_path.write_text(content, encoding="utf-8")
    print(f"Patched Rigidbody into: {Path(path_rel).name} (RB ID: {rb_id})")

def main():
    print("Patching Rigidbody into Hitboxes of 16 heroes...")
    for p in PREFABS:
        patch_prefab(p)
    print("Done!")

if __name__ == "__main__":
    main()
