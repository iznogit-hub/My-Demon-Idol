#!/usr/bin/env python3
"""Appends C06 Cultist's mocap clip animations to the CMU .fbx.meta files.

Conforms to constraints:
- C7: Append only, never modify existing clip entries.
- C9: 24 fps preserved, keepOriginalPositionY: 0, firstFrame >= 8 to prevent sunk hips.
- C8: No banned takes (88_06).
"""
from __future__ import annotations
import os
from pathlib import Path

WORKSPACE_ROOT = Path(os.environ.get("WORKSPACE_ROOT", Path(__file__).resolve().parent.parent))
BASE_MOCAP_DIR = WORKSPACE_ROOT / "Assets/Huge Mocap Library/mocap animations"

CLIP_ENTRY_TEMPLATE = """    - serializedVersion: 16
      name: {name}
      takeName: {take}
      internalID: 0
      firstFrame: {first}
      lastFrame: {last}
      wrapMode: 0
      orientationOffsetY: 0
      level: 0
      cycleOffset: 0
      loop: {loop}
      hasAdditiveReferencePose: 0
      loopTime: {loop}
      loopBlend: {loop}
      loopBlendOrientation: {loop}
      loopBlendPositionY: {loop}
      loopBlendPositionXZ: 0
      keepOriginalOrientation: 1
      keepOriginalPositionY: 0
      keepOriginalPositionXZ: 1
      heightFromFeet: 0
      mirror: 0
      bodyMask: 01000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000
      curves: []
      events: []
      transformMask: []
      maskType: 3
      maskSource: {{instanceID: 0}}
      additiveReferencePoseFrame: 0"""

CULTIST_CLIPS = {
    "79/79_04": [
        {"name": "Cultist_5A", "take": "79_04", "first": 10, "last": 25, "loop": 0},
        {"name": "Cultist_2A", "take": "79_04", "first": 30, "last": 48, "loop": 0},
        {"name": "Cultist_5B", "take": "79_04", "first": 60, "last": 84, "loop": 0},
        {"name": "Cultist_2B", "take": "79_04", "first": 95, "last": 118, "loop": 0},
        {"name": "Cultist_6B", "take": "79_04", "first": 130, "last": 155, "loop": 0},
    ],
    "135/135_04": [
        {"name": "Cultist_6A", "take": "135_04", "first": 15, "last": 38, "loop": 0},
        {"name": "Cultist_5C", "take": "135_04", "first": 50, "last": 75, "loop": 0},
        {"name": "Cultist_2C", "take": "135_04", "first": 90, "last": 116, "loop": 0},
        {"name": "Cultist_3C", "take": "135_04", "first": 130, "last": 158, "loop": 0},
        {"name": "Cultist_623C", "take": "135_04", "first": 170, "last": 200, "loop": 0},
        {"name": "Cultist_632146D", "take": "135_04", "first": 210, "last": 250, "loop": 0},
        {"name": "Cultist_Throw", "take": "135_04", "first": 260, "last": 285, "loop": 0},
        {"name": "Cultist_CounterAssault", "take": "135_04", "first": 15, "last": 38, "loop": 0},
        {"name": "Cultist_CrushTrigger", "take": "135_04", "first": 50, "last": 75, "loop": 0},
    ],
    "118/118_04": [
        {"name": "Cultist_jA", "take": "118_04", "first": 12, "last": 28, "loop": 0},
        {"name": "Cultist_jB", "take": "118_04", "first": 35, "last": 55, "loop": 0},
        {"name": "Cultist_jC", "take": "118_04", "first": 65, "last": 88, "loop": 0},
        {"name": "Cultist_AirThrow", "take": "118_04", "first": 95, "last": 118, "loop": 0},
    ],
    "91/91_03": [
        {"name": "Cultist_22B", "take": "91_03", "first": 10, "last": 28, "loop": 0},
        {"name": "Cultist_22C", "take": "91_03", "first": 35, "last": 55, "loop": 0},
    ],
    "16/16_21": [
        {"name": "Cultist_Walk", "take": "16_21", "first": 10, "last": 38, "loop": 1},
        {"name": "Cultist_Dash", "take": "16_21", "first": 50, "last": 75, "loop": 0},
    ],
    "143/143_07": [
        {"name": "Cultist_Jump", "take": "143_07", "first": 10, "last": 35, "loop": 0},
    ],
    "139/139_10": [
        {"name": "Cultist_Crouch", "take": "139_10", "first": 10, "last": 40, "loop": 1},
    ],
    "144/144_08": [
        {"name": "Cultist_StandBlock", "take": "144_08", "first": 15, "last": 35, "loop": 0},
    ],
    "135/135_08": [
        {"name": "Cultist_CrouchBlock", "take": "135_08", "first": 15, "last": 35, "loop": 0},
    ],
    "140/140_02": [
        {"name": "Cultist_Knockdown", "take": "140_02", "first": 10, "last": 45, "loop": 0},
    ]
}


def apply_clips_to_meta(rel_path: str, clips: list[dict]) -> int:
    meta_path = BASE_MOCAP_DIR / f"{rel_path}.fbx.meta"
    if not meta_path.is_file():
        print(f"ERROR: {meta_path} does not exist.")
        return 0

    content = meta_path.read_text(encoding="utf-8")

    added_count = 0
    new_entries: list[str] = []

    for c in clips:
        clip_name = c["name"]
        if f"name: {clip_name}" in content:
            print(f"  [SKIPPED] {clip_name} already present in {rel_path}.fbx.meta")
            continue

        entry = CLIP_ENTRY_TEMPLATE.format(
            name=c["name"],
            take=c["take"],
            first=c["first"],
            last=c["last"],
            loop=c["loop"]
        )
        new_entries.append(entry)
        added_count += 1

    if not new_entries:
        return 0

    insertion_text = "\n".join(new_entries)

    if "    clipAnimations: []" in content:
        content = content.replace("    clipAnimations: []", "    clipAnimations:\n" + insertion_text)
    elif "    clipAnimations:" in content:
        marker = "    clipAnimations:\n"
        pos = content.find(marker)
        if pos != -1:
            split_idx = pos + len(marker)
            content = content[:split_idx] + insertion_text + "\n" + content[split_idx:]
    else:
        print(f"ERROR: could not find clipAnimations section in {meta_path}")
        return 0

    meta_path.write_text(content, encoding="utf-8")
    print(f"  [OK] Added {added_count} clips to {rel_path}.fbx.meta")
    return added_count


def main() -> int:
    print("Applying C06 Cultist CMU mocap animation clips...")
    total_added = 0
    for rel_path, clips in CULTIST_CLIPS.items():
        total_added += apply_clips_to_meta(rel_path, clips)

    print(f"Done! Successfully applied {total_added} clips across {len(CULTIST_CLIPS)} FBX meta files.")
    return 0


if __name__ == "__main__":
    main()
