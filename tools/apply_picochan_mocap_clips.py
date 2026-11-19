#!/usr/bin/env python3
"""Appends C09 PicoChan's mocap clip animations to the CMU .fbx.meta files.

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

PICOCHAN_CLIPS = {
    "15/15_13": [
        {"name": "Pico_5A", "take": "15_13", "first": 638, "last": 654, "loop": 0},
        {"name": "Pico_2A", "take": "15_13", "first": 376, "last": 394, "loop": 0},
        {"name": "Pico_6A", "take": "15_13", "first": 400, "last": 418, "loop": 0},
        {"name": "Pico_5B", "take": "15_13", "first": 1256, "last": 1274, "loop": 0},
        {"name": "Pico_2B", "take": "15_13", "first": 344, "last": 362, "loop": 0},
        {"name": "Pico_2C", "take": "15_13", "first": 340, "last": 362, "loop": 0},
        {"name": "Pico_3C", "take": "15_13", "first": 288, "last": 308, "loop": 0},
        {"name": "Pico_jA", "take": "15_13", "first": 765, "last": 783, "loop": 0},
        {"name": "Pico_Super", "take": "15_13", "first": 820, "last": 854, "loop": 0},
        {"name": "Pico_OD_EA", "take": "15_13", "first": 820, "last": 854, "loop": 0},
        {"name": "Pico_CounterAssault", "take": "15_13", "first": 638, "last": 654, "loop": 0},
    ],
    "86/86_08": [
        {"name": "Pico_5C", "take": "86_08", "first": 556, "last": 574, "loop": 0},
        {"name": "Pico_jB", "take": "86_08", "first": 312, "last": 330, "loop": 0},
        {"name": "Pico_jC", "take": "86_08", "first": 944, "last": 962, "loop": 0},
        {"name": "Pico_jD", "take": "86_08", "first": 944, "last": 962, "loop": 0},
        {"name": "Pico_AirThrow", "take": "86_08", "first": 944, "last": 962, "loop": 0},
        {"name": "Pico_Charge", "take": "86_08", "first": 1502, "last": 1524, "loop": 0},
    ],
    "141/141_23": [
        {"name": "Pico_5D", "take": "141_23", "first": 11, "last": 33, "loop": 0},
        {"name": "Pico_2D", "take": "141_23", "first": 11, "last": 33, "loop": 0},
        {"name": "Pico_6D", "take": "141_23", "first": 11, "last": 33, "loop": 0},
        {"name": "Pico_Press", "take": "141_23", "first": 11, "last": 33, "loop": 0},
    ],
    "17/17_10": [
        {"name": "Pico_6B", "take": "17_10", "first": 173, "last": 193, "loop": 0},
        {"name": "Pico_CrushTrigger", "take": "17_10", "first": 173, "last": 193, "loop": 0},
        {"name": "Pico_236A", "take": "17_10", "first": 37, "last": 57, "loop": 0},
        {"name": "Pico_623C", "take": "17_10", "first": 149, "last": 169, "loop": 0},
        {"name": "Pico_63214B", "take": "17_10", "first": 173, "last": 193, "loop": 0},
        {"name": "Pico_Dash", "take": "17_10", "first": 355, "last": 373, "loop": 0},
    ],
    "111/111_33": [
        {"name": "Pico_360C", "take": "111_33", "first": 314, "last": 336, "loop": 0},
        {"name": "Pico_Throw", "take": "111_33", "first": 314, "last": 336, "loop": 0},
    ],
    "111/111_34": [
        {"name": "Pico_Intro", "take": "111_34", "first": 8, "last": 40, "loop": 0},
        {"name": "Pico_Walk", "take": "111_34", "first": 82, "last": 110, "loop": 1},
    ],
    "111/111_02": [
        {"name": "Pico_Victory", "take": "111_02", "first": 17, "last": 77, "loop": 0},
    ],
    "143/143_06": [
        {"name": "Pico_Jump", "take": "143_06", "first": 12, "last": 32, "loop": 0},
    ],
    "139/139_09": [
        {"name": "Pico_Crouch", "take": "139_09", "first": 110, "last": 130, "loop": 1},
    ],
    "113/113_08": [
        {"name": "Pico_GetUp", "take": "113_08", "first": 300, "last": 342, "loop": 0},
    ],
    "140/140_01": [
        {"name": "Pico_Knockdown", "take": "140_01", "first": 8, "last": 40, "loop": 0},
    ],
    "144/144_07": [
        {"name": "Pico_StandBlock", "take": "144_07", "first": 186, "last": 206, "loop": 0},
    ],
    "135/135_10": [
        {"name": "Pico_CrouchBlock", "take": "135_10", "first": 40, "last": 60, "loop": 0},
    ],
}


def apply_clips():
    total_added = 0
    for take_rel, clips in PICOCHAN_CLIPS.items():
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
