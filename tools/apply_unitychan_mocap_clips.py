#!/usr/bin/env python3
"""Appends C04 UnityChan's mocap clip animations to the CMU .fbx.meta files.

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

UNITYCHAN_CLIPS = {
    "02/02_05": [
        {"name": "UChan_5A", "take": "02_05", "first": 92, "last": 108, "loop": 0},
        {"name": "UChan_2A", "take": "02_05", "first": 212, "last": 230, "loop": 0},
        {"name": "UChan_5B", "take": "02_05", "first": 144, "last": 162, "loop": 0},
        {"name": "UChan_5C", "take": "02_05", "first": 148, "last": 168, "loop": 0},
        {"name": "UChan_jA", "take": "02_05", "first": 124, "last": 140, "loop": 0},
        {"name": "UChan_Throw", "take": "02_05", "first": 72, "last": 94, "loop": 0},
        {"name": "UChan_CounterAssault", "take": "02_05", "first": 92, "last": 108, "loop": 0},
    ],
    "14/14_02": [
        {"name": "UChan_2B", "take": "14_02", "first": 630, "last": 648, "loop": 0},
        {"name": "UChan_2C", "take": "14_02", "first": 635, "last": 655, "loop": 0},
        {"name": "UChan_Super", "take": "14_02", "first": 811, "last": 845, "loop": 0},
        {"name": "UChan_OD_EA", "take": "14_02", "first": 811, "last": 845, "loop": 0},
    ],
    "16/16_01": [
        {"name": "UChan_3C", "take": "16_01", "first": 16, "last": 40, "loop": 0},
        {"name": "UChan_5D", "take": "16_01", "first": 16, "last": 40, "loop": 0},
        {"name": "UChan_Victory", "take": "16_01", "first": 48, "last": 64, "loop": 0},
    ],
    "16/16_05": [
        {"name": "UChan_6A", "take": "16_05", "first": 10, "last": 26, "loop": 0},
        {"name": "UChan_6B", "take": "16_05", "first": 20, "last": 40, "loop": 0},
        {"name": "UChan_623C", "take": "16_05", "first": 30, "last": 50, "loop": 0},
        {"name": "UChan_CrushTrigger", "take": "16_05", "first": 20, "last": 40, "loop": 0},
    ],
    "16/16_15": [
        {"name": "UChan_Intro", "take": "16_15", "first": 8, "last": 40, "loop": 0},
    ],
    "16/16_35": [
        {"name": "UChan_4D", "take": "16_35", "first": 10, "last": 30, "loop": 0},
        {"name": "UChan_Dash", "take": "16_35", "first": 10, "last": 30, "loop": 0},
    ],
    "91/91_39": [
        {"name": "UChan_6D", "take": "91_39", "first": 24, "last": 44, "loop": 0},
        {"name": "UChan_214B", "take": "91_39", "first": 50, "last": 70, "loop": 0},
    ],
    "118/118_01": [
        {"name": "UChan_236A", "take": "118_01", "first": 15, "last": 35, "loop": 0},
        {"name": "UChan_jB", "take": "118_01", "first": 88, "last": 106, "loop": 0},
        {"name": "UChan_jD", "take": "118_01", "first": 95, "last": 115, "loop": 0},
        {"name": "UChan_j236236C", "take": "118_01", "first": 88, "last": 118, "loop": 0},
        {"name": "UChan_AirThrow", "take": "118_01", "first": 90, "last": 110, "loop": 0},
    ],
    "118/118_05": [
        {"name": "UChan_236B", "take": "118_05", "first": 20, "last": 45, "loop": 0},
        {"name": "UChan_jC", "take": "118_05", "first": 64, "last": 82, "loop": 0},
        {"name": "UChan_46C", "take": "118_05", "first": 47, "last": 70, "loop": 0},
        {"name": "UChan_j214B", "take": "118_05", "first": 70, "last": 92, "loop": 0},
    ],
    "140/140_01": [
        {"name": "UChan_Knockdown", "take": "140_01", "first": 8, "last": 40, "loop": 0},
        {"name": "UChan_GetUp", "take": "140_01", "first": 70, "last": 130, "loop": 0},
    ],
    "144/144_33": [
        {"name": "UChan_Walk", "take": "144_33", "first": 150, "last": 178, "loop": 1},
    ],
    "144/144_07": [
        {"name": "UChan_StandBlock", "take": "144_07", "first": 186, "last": 206, "loop": 0},
    ],
    "135/135_10": [
        {"name": "UChan_CrouchBlock", "take": "135_10", "first": 300, "last": 320, "loop": 0},
    ],
    "135/135_05": [
        {"name": "UChan_Crouch", "take": "135_05", "first": 80, "last": 92, "loop": 1},
    ],
    "75/75_16": [
        {"name": "UChan_Jump", "take": "75_16", "first": 32, "last": 50, "loop": 0},
    ],
}


def apply_clips():
    total_added = 0
    for take_rel, clips in UNITYCHAN_CLIPS.items():
        meta_file = BASE_MOCAP_DIR / f"{take_rel}.fbx.meta"
        if not meta_file.is_file():
            print(f"ERROR: Missing meta file: {meta_file}")
            continue

        text = meta_file.read_text(encoding="utf-8")
        if "    clipAnimations: []" in text:
            # Empty list: replace with new entries
            entries_yaml = "\n".join(CLIP_ENTRY_TEMPLATE.format(**c) for c in clips)
            new_text = text.replace("    clipAnimations: []", "    clipAnimations:\n" + entries_yaml)
            meta_file.write_text(new_text, encoding="utf-8")
            print(f"Added {len(clips)} clip(s) to {take_rel}.fbx.meta")
            total_added += len(clips)
        elif "    clipAnimations:" in text:
            # Already has clipAnimations list, check for clips already present or append
            new_entries = []
            for c in clips:
                if f"name: {c['name']}" not in text:
                    new_entries.append(CLIP_ENTRY_TEMPLATE.format(**c))
            if new_entries:
                entries_yaml = "\n" + "\n".join(new_entries)
                # insert after clipAnimations:
                idx = text.find("    clipAnimations:\n")
                if idx != -1:
                    insert_pos = idx + len("    clipAnimations:\n")
                    new_text = text[:insert_pos] + entries_yaml.lstrip("\n") + "\n" + text[insert_pos:]
                    meta_file.write_text(new_text, encoding="utf-8")
                    print(f"Appended {len(new_entries)} clip(s) to {take_rel}.fbx.meta")
                    total_added += len(new_entries)
            else:
                print(f"All clips already present in {take_rel}.fbx.meta")
        else:
            print(f"WARN: Could not locate clipAnimations in {take_rel}.fbx.meta")

    print(f"\nDone! Total clips added/updated: {total_added}")


if __name__ == "__main__":
    apply_clips()
