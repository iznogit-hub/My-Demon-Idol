#!/usr/bin/env python3
"""Appends C08 SanpakuChan's mocap clip animations to the CMU .fbx.meta files.

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

SANPAKUCHAN_CLIPS = {
    "14/14_01": [
        {"name": "Sanpaku_5A", "take": "14_01", "first": 10, "last": 24, "loop": 0},
        {"name": "Sanpaku_2A", "take": "14_01", "first": 30, "last": 46, "loop": 0},
        {"name": "Sanpaku_6A", "take": "14_01", "first": 55, "last": 75, "loop": 0},
        {"name": "Sanpaku_5B", "take": "14_01", "first": 85, "last": 105, "loop": 0},
        {"name": "Sanpaku_2B", "take": "14_01", "first": 115, "last": 133, "loop": 0},
        {"name": "Sanpaku_CounterAssault", "take": "14_01", "first": 10, "last": 24, "loop": 0},
    ],
    "14/14_03": [
        {"name": "Sanpaku_5C", "take": "14_03", "first": 15, "last": 35, "loop": 0},
        {"name": "Sanpaku_2C", "take": "14_03", "first": 45, "last": 67, "loop": 0},
        {"name": "Sanpaku_3C", "take": "14_03", "first": 75, "last": 98, "loop": 0},
        {"name": "Sanpaku_Throw", "take": "14_03", "first": 110, "last": 132, "loop": 0},
    ],
    "118/118_02": [
        {"name": "Sanpaku_6B", "take": "118_02", "first": 10, "last": 26, "loop": 0},
        {"name": "Sanpaku_CrushTrigger", "take": "118_02", "first": 10, "last": 26, "loop": 0},
        {"name": "Sanpaku_jA", "take": "118_02", "first": 35, "last": 50, "loop": 0},
        {"name": "Sanpaku_jB", "take": "118_02", "first": 60, "last": 78, "loop": 0},
        {"name": "Sanpaku_jC", "take": "118_02", "first": 85, "last": 105, "loop": 0},
        {"name": "Sanpaku_AirThrow", "take": "118_02", "first": 60, "last": 78, "loop": 0},
    ],
    "91/91_01": [
        {"name": "Sanpaku_5D", "take": "91_01", "first": 10, "last": 24, "loop": 0},
        {"name": "Sanpaku_4D", "take": "91_01", "first": 30, "last": 44, "loop": 0},
        {"name": "Sanpaku_2D", "take": "91_01", "first": 50, "last": 64, "loop": 0},
        {"name": "Sanpaku_jD", "take": "91_01", "first": 70, "last": 84, "loop": 0},
        {"name": "Sanpaku_214214D", "take": "91_01", "first": 90, "last": 125, "loop": 0},
    ],
    "141/141_15": [
        {"name": "Sanpaku_214B_1", "take": "141_15", "first": 10, "last": 30, "loop": 0},
        {"name": "Sanpaku_214B_2", "take": "141_15", "first": 35, "last": 55, "loop": 0},
        {"name": "Sanpaku_214B_3", "take": "141_15", "first": 60, "last": 82, "loop": 0},
    ],
    "16/16_10": [
        {"name": "Sanpaku_Super", "take": "16_10", "first": 15, "last": 48, "loop": 0},
        {"name": "Sanpaku_OD_EA", "take": "16_10", "first": 15, "last": 48, "loop": 0},
    ],
    "16/16_11": [
        {"name": "Sanpaku_Intro", "take": "16_11", "first": 10, "last": 40, "loop": 0},
        {"name": "Sanpaku_Victory", "take": "16_11", "first": 50, "last": 90, "loop": 0},
    ],
    "16/16_20": [
        {"name": "Sanpaku_Walk", "take": "16_20", "first": 12, "last": 38, "loop": 1},
        {"name": "Sanpaku_Dash", "take": "16_20", "first": 45, "last": 65, "loop": 0},
    ],
    "143/143_06": [
        {"name": "Sanpaku_Jump", "take": "143_06", "first": 12, "last": 32, "loop": 0},
    ],
    "139/139_09": [
        {"name": "Sanpaku_Crouch", "take": "139_09", "first": 110, "last": 130, "loop": 1},
    ],
    "113/113_08": [
        {"name": "Sanpaku_GetUp", "take": "113_08", "first": 300, "last": 342, "loop": 0},
    ],
    "140/140_01": [
        {"name": "Sanpaku_Knockdown", "take": "140_01", "first": 8, "last": 40, "loop": 0},
    ],
    "144/144_07": [
        {"name": "Sanpaku_StandBlock", "take": "144_07", "first": 186, "last": 206, "loop": 0},
    ],
    "135/135_10": [
        {"name": "Sanpaku_CrouchBlock", "take": "135_10", "first": 40, "last": 60, "loop": 0},
        {"name": "Sanpaku_236A", "take": "135_10", "first": 50, "last": 72, "loop": 0},
    ],
    "118/118_02": [
        {"name": "Sanpaku_j236A", "take": "118_02", "first": 25, "last": 45, "loop": 0},
    ],
}


def apply_clips():
    total_added = 0
    for take_rel, clips in SANPAKUCHAN_CLIPS.items():
        meta_file = BASE_MOCAP_DIR / f"{take_rel}.fbx.meta"
        if not meta_file.is_file():
            print(f"ERROR: Missing meta file: {meta_file}")
            continue

        text = meta_file.read_text(encoding="utf-8")
        if "    clipAnimations: []" in text:
            entries_yaml = "\n".join(CLIP_ENTRY_TEMPLATE.format(**c) for c in clips)
            new_text = text.replace("    clipAnimations: []", "    clipAnimations:\n" + entries_yaml)
            meta_file.write_text(new_text, encoding="utf-8")
            print(f"Added {len(clips)} clip(s) to {take_rel}.fbx.meta")
            total_added += len(clips)
        elif "    clipAnimations:" in text:
            new_entries = []
            for c in clips:
                if f"name: {c['name']}" not in text:
                    new_entries.append(CLIP_ENTRY_TEMPLATE.format(**c))
            if new_entries:
                entries_yaml = "\n" + "\n".join(new_entries)
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
