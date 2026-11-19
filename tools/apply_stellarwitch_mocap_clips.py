#!/usr/bin/env python3
"""Appends C07 StellarWitch's mocap clip animations to the CMU .fbx.meta files.

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

STELLARWITCH_CLIPS = {
    "135/135_06": [
        {"name": "StellarWitch_5A", "take": "135_06", "first": 10, "last": 25, "loop": 0},
        {"name": "StellarWitch_2A", "take": "135_06", "first": 30, "last": 48, "loop": 0},
        {"name": "StellarWitch_5B", "take": "135_06", "first": 60, "last": 84, "loop": 0},
        {"name": "StellarWitch_2B", "take": "135_06", "first": 95, "last": 118, "loop": 0},
        {"name": "StellarWitch_6B", "take": "135_06", "first": 130, "last": 155, "loop": 0},
    ],
    "135/135_07": [
        {"name": "StellarWitch_5C", "take": "135_07", "first": 15, "last": 42, "loop": 0},
        {"name": "StellarWitch_2C", "take": "135_07", "first": 55, "last": 80, "loop": 0},
        {"name": "StellarWitch_3C", "take": "135_07", "first": 95, "last": 122, "loop": 0},
        {"name": "StellarWitch_6A", "take": "135_07", "first": 135, "last": 160, "loop": 0},
        {"name": "StellarWitch_Throw", "take": "135_07", "first": 175, "last": 200, "loop": 0},
        {"name": "StellarWitch_BackThrow", "take": "135_07", "first": 175, "last": 200, "loop": 0},
        {"name": "StellarWitch_CounterAssault", "take": "135_07", "first": 135, "last": 160, "loop": 0},
        {"name": "StellarWitch_CrushTrigger", "take": "135_07", "first": 15, "last": 42, "loop": 0},
    ],
    "135/135_08": [
        {"name": "StellarWitch_5D", "take": "135_08", "first": 10, "last": 35, "loop": 0},
        {"name": "StellarWitch_2D", "take": "135_08", "first": 45, "last": 70, "loop": 0},
        {"name": "StellarWitch_6D", "take": "135_08", "first": 80, "last": 105, "loop": 0},
        {"name": "StellarWitch_jD", "take": "135_08", "first": 115, "last": 140, "loop": 0},
        {"name": "StellarWitch_CometFire", "take": "135_08", "first": 150, "last": 175, "loop": 0},
    ],
    "135/135_04": [
        {"name": "StellarWitch_236A", "take": "135_04", "first": 10, "last": 40, "loop": 0},
        {"name": "StellarWitch_236B", "take": "135_04", "first": 50, "last": 80, "loop": 0},
        {"name": "StellarWitch_623C", "take": "135_04", "first": 90, "last": 125, "loop": 0},
        {"name": "StellarWitch_214B", "take": "135_04", "first": 135, "last": 165, "loop": 0},
        {"name": "StellarWitch_236C", "take": "135_04", "first": 175, "last": 210, "loop": 0},
    ],
    "135/135_05": [
        {"name": "StellarWitch_236236C", "take": "135_05", "first": 10, "last": 60, "loop": 0},
        {"name": "StellarWitch_632146C", "take": "135_05", "first": 70, "last": 130, "loop": 0},
        {"name": "StellarWitch_OD_EA", "take": "135_05", "first": 140, "last": 180, "loop": 0},
    ],
    "118/118_05": [
        {"name": "StellarWitch_jA", "take": "118_05", "first": 12, "last": 28, "loop": 0},
        {"name": "StellarWitch_jB", "take": "118_05", "first": 35, "last": 55, "loop": 0},
        {"name": "StellarWitch_jC", "take": "118_05", "first": 65, "last": 88, "loop": 0},
        {"name": "StellarWitch_AirThrow", "take": "118_05", "first": 95, "last": 118, "loop": 0},
    ],
    "16/16_22": [
        {"name": "StellarWitch_Walk", "take": "16_22", "first": 10, "last": 38, "loop": 1},
        {"name": "StellarWitch_Dash", "take": "16_22", "first": 50, "last": 72, "loop": 0},
    ],
    "143/143_08": [
        {"name": "StellarWitch_Jump", "take": "143_08", "first": 10, "last": 32, "loop": 0},
    ],
    "139/139_11": [
        {"name": "StellarWitch_Crouch", "take": "139_11", "first": 10, "last": 40, "loop": 1},
    ],
    "144/144_09": [
        {"name": "StellarWitch_StandBlock", "take": "144_09", "first": 15, "last": 35, "loop": 0},
    ],
    "135/135_09": [
        {"name": "StellarWitch_CrouchBlock", "take": "135_09", "first": 15, "last": 35, "loop": 0},
    ],
    "140/140_03": [
        {"name": "StellarWitch_Knockdown", "take": "140_03", "first": 10, "last": 45, "loop": 0},
    ],
    "16/16_12": [
        {"name": "StellarWitch_Intro", "take": "16_12", "first": 10, "last": 40, "loop": 0},
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
    return added_count


def main():
    print(f"=== Slicing C07 StellarWitch Mocap Clips into CMU Library ===")
    total_added = 0
    for rel_path, clips in STELLARWITCH_CLIPS.items():
        added = apply_clips_to_meta(rel_path, clips)
        print(f"  {rel_path}: added {added}/{len(clips)} clip(s).")
        total_added += added

    print(f"\nCompleted! Total new clips added: {total_added}")


if __name__ == "__main__":
    main()
