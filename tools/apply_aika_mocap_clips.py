#!/usr/bin/env python3
"""Appends C02 Aika's mocap clip animations to the CMU .fbx.meta files.

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

AIKA_CLIPS = {
    "80/80_10": [
        {"name": "Aika_5A", "take": "80_10", "first": 52, "last": 68, "loop": 0},
        {"name": "Aika_CounterAssault", "take": "80_10", "first": 52, "last": 68, "loop": 0},
    ],
    "135/135_02": [
        {"name": "Aika_2A", "take": "135_02", "first": 462, "last": 480, "loop": 0},
        {"name": "Aika_2B", "take": "135_02", "first": 470, "last": 490, "loop": 0},
        {"name": "Aika_2C", "take": "135_02", "first": 492, "last": 514, "loop": 0},
    ],
    "135/135_11": [
        {"name": "Aika_6A", "take": "135_11", "first": 12, "last": 28, "loop": 0},
        {"name": "Aika_623C", "take": "135_11", "first": 62, "last": 84, "loop": 0},
        {"name": "Aika_46C", "take": "135_11", "first": 168, "last": 192, "loop": 0},
        {"name": "Aika_CrushTrigger", "take": "135_11", "first": 168, "last": 192, "loop": 0},
    ],
    "13/13_18": [
        {"name": "Aika_5B", "take": "13_18", "first": 73, "last": 91, "loop": 0},
        {"name": "Aika_jA", "take": "13_18", "first": 280, "last": 296, "loop": 0},
        {"name": "Aika_Super", "take": "13_18", "first": 276, "last": 316, "loop": 0},
        {"name": "Aika_OD_EA", "take": "13_18", "first": 276, "last": 316, "loop": 0},
    ],
    "143/143_24": [
        {"name": "Aika_6B", "take": "143_24", "first": 164, "last": 184, "loop": 0},
        {"name": "Aika_214B", "take": "143_24", "first": 34, "last": 54, "loop": 0},
    ],
    "113/113_13": [
        {"name": "Aika_5C", "take": "113_13", "first": 245, "last": 265, "loop": 0},
        {"name": "Aika_3C", "take": "113_13", "first": 219, "last": 243, "loop": 0},
    ],
    "141/141_14": [
        {"name": "Aika_jB", "take": "141_14", "first": 44, "last": 64, "loop": 0},
        {"name": "Aika_jC", "take": "141_14", "first": 80, "last": 102, "loop": 0},
        {"name": "Aika_jD", "take": "141_14", "first": 110, "last": 126, "loop": 0},
        {"name": "Aika_AirThrow", "take": "141_14", "first": 110, "last": 130, "loop": 0},
    ],
    "111/111_19": [
        {"name": "Aika_236B", "take": "111_19", "first": 57, "last": 75, "loop": 0},
        {"name": "Aika_Throw", "take": "111_19", "first": 36, "last": 58, "loop": 0},
    ],
    "74/74_01": [
        {"name": "Aika_Intro", "take": "74_01", "first": 8, "last": 48, "loop": 0},
    ],
    "144/144_08": [
        {"name": "Aika_Victory", "take": "144_08", "first": 560, "last": 584, "loop": 0},
    ],
    "114/114_02": [
        {"name": "Aika_GetUp", "take": "114_02", "first": 211, "last": 261, "loop": 0},
    ],
    "144/144_33": [
        {"name": "Aika_Walk", "take": "144_33", "first": 150, "last": 178, "loop": 1},
    ],
    "144/144_17": [
        {"name": "Aika_Dash", "take": "144_17", "first": 20, "last": 40, "loop": 0},
    ],
    "75/75_16": [
        {"name": "Aika_Jump", "take": "75_16", "first": 32, "last": 50, "loop": 0},
    ],
    "135/135_05": [
        {"name": "Aika_Crouch", "take": "135_05", "first": 80, "last": 92, "loop": 1},
    ],
    "144/144_07": [
        {"name": "Aika_StandBlock", "take": "144_07", "first": 186, "last": 206, "loop": 0},
    ],
    "135/135_10": [
        {"name": "Aika_CrouchBlock", "take": "135_10", "first": 300, "last": 320, "loop": 0},
    ],
    "90/90_18": [
        {"name": "Aika_Knockdown", "take": "90_18", "first": 8, "last": 45, "loop": 0},
    ],
}


def apply_clips():
    total_added = 0
    for take_rel, clips in AIKA_CLIPS.items():
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
