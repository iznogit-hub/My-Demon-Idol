#!/usr/bin/env python3
"""Appends C12 Fina's mocap clip animations to the CMU .fbx.meta files.

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

FINA_CLIPS = {
    "79/79_01": [
        {"name": "Fina_5A", "take": "79_01", "first": 10, "last": 26, "loop": 0},
        {"name": "Fina_2A", "take": "79_01", "first": 35, "last": 53, "loop": 0},
        {"name": "Fina_6A", "take": "79_01", "first": 60, "last": 80, "loop": 0},
        {"name": "Fina_5B", "take": "79_01", "first": 90, "last": 112, "loop": 0},
        {"name": "Fina_2B", "take": "79_01", "first": 120, "last": 140, "loop": 0},
        {"name": "Fina_CounterAssault", "take": "79_01", "first": 10, "last": 26, "loop": 0},
    ],
    "80/80_01": [
        {"name": "Fina_6B", "take": "80_01", "first": 15, "last": 38, "loop": 0},
        {"name": "Fina_5C", "take": "80_01", "first": 50, "last": 72, "loop": 0},
        {"name": "Fina_2C", "take": "80_01", "first": 85, "last": 109, "loop": 0},
        {"name": "Fina_3C", "take": "80_01", "first": 120, "last": 145, "loop": 0},
        {"name": "Fina_Super", "take": "80_01", "first": 160, "last": 195, "loop": 0},
        {"name": "Fina_OD_EA", "take": "80_01", "first": 160, "last": 195, "loop": 0},
    ],
    "80/80_02": [
        {"name": "Fina_jA", "take": "80_02", "first": 12, "last": 30, "loop": 0},
        {"name": "Fina_jB", "take": "80_02", "first": 38, "last": 58, "loop": 0},
        {"name": "Fina_jC", "take": "80_02", "first": 68, "last": 90, "loop": 0},
        {"name": "Fina_AirThrow", "take": "80_02", "first": 38, "last": 58, "loop": 0},
    ],
    "135/135_03": [
        {"name": "Fina_623C", "take": "135_03", "first": 15, "last": 38, "loop": 0},
        {"name": "Fina_214B", "take": "135_03", "first": 50, "last": 75, "loop": 0},
        {"name": "Fina_Throw", "take": "135_03", "first": 90, "last": 114, "loop": 0},
    ],
    "16/16_02": [
        {"name": "Fina_6D", "take": "16_02", "first": 10, "last": 32, "loop": 0},
        {"name": "Fina_CrushTrigger", "take": "16_02", "first": 10, "last": 32, "loop": 0},
    ],
    "16/16_03": [
        {"name": "Fina_Intro", "take": "16_03", "first": 10, "last": 42, "loop": 0},
        {"name": "Fina_Victory", "take": "16_03", "first": 50, "last": 100, "loop": 0},
    ],
    "16/16_04": [
        {"name": "Fina_Walk", "take": "16_04", "first": 12, "last": 42, "loop": 1},
        {"name": "Fina_Dash", "take": "16_04", "first": 50, "last": 70, "loop": 0},
    ],
    "16/16_06": [
        {"name": "Fina_Jump", "take": "16_06", "first": 10, "last": 30, "loop": 0},
    ],
    "139/139_09": [
        {"name": "Fina_Crouch", "take": "139_09", "first": 110, "last": 130, "loop": 1},
    ],
    "113/113_08": [
        {"name": "Fina_GetUp", "take": "113_08", "first": 300, "last": 342, "loop": 0},
    ],
    "140/140_01": [
        {"name": "Fina_Knockdown", "take": "140_01", "first": 8, "last": 40, "loop": 0},
    ],
    "144/144_07": [
        {"name": "Fina_StandBlock", "take": "144_07", "first": 186, "last": 206, "loop": 0},
    ],
    "135/135_10": [
        {"name": "Fina_CrouchBlock", "take": "135_10", "first": 40, "last": 60, "loop": 0},
    ],
}


def apply_clips():
    total_added = 0
    for take_rel, clips in FINA_CLIPS.items():
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
