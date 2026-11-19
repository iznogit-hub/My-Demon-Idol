#!/usr/bin/env python3
"""Appends C01 Akio's mocap clip animations to the CMU .fbx.meta files.

Conforms to constraints:
- C7: Append only, never modify existing clip entries.
- C9: 24 fps preserved, keepOriginalPositionY: 0, firstFrame >= 8 to prevent sunk hips.
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

AKIO_CLIPS = {
    "144/144_20": [
        {"name": "Akio_5A", "take": "144_20", "first": 110, "last": 122, "loop": 0},
        {"name": "Akio_2A", "take": "144_20", "first": 196, "last": 214, "loop": 0},
        {"name": "Akio_5B", "take": "144_20", "first": 123, "last": 136, "loop": 0},
        {"name": "Akio_2B", "take": "144_20", "first": 220, "last": 242, "loop": 0},
        {"name": "Akio_6B", "take": "144_20", "first": 148, "last": 166, "loop": 0},
        {"name": "Akio_5D", "take": "144_20", "first": 34, "last": 47, "loop": 0},
        {"name": "Akio_5D_2", "take": "144_20", "first": 123, "last": 136, "loop": 0},
        {"name": "Akio_CounterAssault", "take": "144_20", "first": 123, "last": 136, "loop": 0},
    ],
    "144/144_17": [
        {"name": "Akio_6A", "take": "144_17", "first": 60, "last": 84, "loop": 0},
        {"name": "Akio_5C", "take": "144_17", "first": 24, "last": 50, "loop": 0},
        {"name": "Akio_5D_4", "take": "144_17", "first": 24, "last": 50, "loop": 0},
        {"name": "Akio_6D", "take": "144_17", "first": 20, "last": 40, "loop": 0},
        {"name": "Akio_214B", "take": "144_17", "first": 40, "last": 62, "loop": 0},
        {"name": "Akio_236C", "take": "144_17", "first": 24, "last": 50, "loop": 0},
        {"name": "Akio_Throw", "take": "144_17", "first": 26, "last": 48, "loop": 0},
        {"name": "Akio_Dash", "take": "144_17", "first": 20, "last": 40, "loop": 0},
        {"name": "Akio_236A", "take": "144_17", "first": 20, "last": 36, "loop": 0},
        {"name": "Akio_236B", "take": "144_17", "first": 24, "last": 48, "loop": 0},
    ],
    "144/144_33": [
        {"name": "Akio_Walk", "take": "144_33", "first": 150, "last": 178, "loop": 1},
        {"name": "Akio_Intro", "take": "144_33", "first": 8, "last": 48, "loop": 0},
    ],
    "144/144_07": [
        {"name": "Akio_StandBlock", "take": "144_07", "first": 186, "last": 206, "loop": 0},
    ],
    "135/135_05": [
        {"name": "Akio_2C", "take": "135_05", "first": 100, "last": 120, "loop": 0},
        {"name": "Akio_5D_3", "take": "135_05", "first": 100, "last": 120, "loop": 0},
        {"name": "Akio_2D", "take": "135_05", "first": 85, "last": 105, "loop": 0},
        {"name": "Akio_Crouch", "take": "135_05", "first": 80, "last": 92, "loop": 1},
    ],
    "135/135_06": [
        {"name": "Akio_Super", "take": "135_06", "first": 48, "last": 80, "loop": 0},
        {"name": "Akio_632146D", "take": "135_06", "first": 48, "last": 80, "loop": 0},
        {"name": "Akio_ExceedAccel", "take": "135_06", "first": 48, "last": 80, "loop": 0},
        {"name": "Akio_CrushTrigger", "take": "135_06", "first": 48, "last": 75, "loop": 0},
    ],
    "135/135_07": [
        {"name": "Akio_3C", "take": "135_07", "first": 140, "last": 164, "loop": 0},
        {"name": "Akio_623C", "take": "135_07", "first": 15, "last": 34, "loop": 0},
    ],
    "135/135_10": [
        {"name": "Akio_CrouchBlock", "take": "135_10", "first": 300, "last": 320, "loop": 0},
    ],
    "143/143_23": [
        {"name": "Akio_jA", "take": "143_23", "first": 42, "last": 51, "loop": 0},
        {"name": "Akio_jD", "take": "143_23", "first": 60, "last": 80, "loop": 0},
        {"name": "Akio_AirThrow", "take": "143_23", "first": 42, "last": 62, "loop": 0},
    ],
    "75/75_16": [
        {"name": "Akio_jB", "take": "75_16", "first": 40, "last": 58, "loop": 0},
        {"name": "Akio_jC", "take": "75_16", "first": 44, "last": 60, "loop": 0},
        {"name": "Akio_Jump", "take": "75_16", "first": 32, "last": 50, "loop": 0},
    ],
    "90/90_18": [
        {"name": "Akio_Knockdown", "take": "90_18", "first": 8, "last": 45, "loop": 0},
    ],
    "111/111_06": [
        {"name": "Akio_GetUp", "take": "111_06", "first": 8, "last": 48, "loop": 0},
    ],
}


def apply_clips():
    total_added = 0
    for take_rel, clips in AKIO_CLIPS.items():
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
