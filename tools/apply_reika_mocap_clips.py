#!/usr/bin/env python3
"""Appends C10 Reika's mocap clip animations to the CMU .fbx.meta files.

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

REIKA_CLIPS = {
    "144/144_01": [
        {"name": "Reika_5A", "take": "144_01", "first": 10, "last": 25, "loop": 0},
        {"name": "Reika_2A", "take": "144_01", "first": 30, "last": 48, "loop": 0},
        {"name": "Reika_5B", "take": "144_01", "first": 60, "last": 84, "loop": 0},
        {"name": "Reika_2B", "take": "144_01", "first": 95, "last": 118, "loop": 0},
        {"name": "Reika_6B", "take": "144_01", "first": 130, "last": 155, "loop": 0},
    ],
    "144/144_02": [
        {"name": "Reika_5C", "take": "144_02", "first": 15, "last": 42, "loop": 0},
        {"name": "Reika_2C", "take": "144_02", "first": 55, "last": 80, "loop": 0},
        {"name": "Reika_3C", "take": "144_02", "first": 95, "last": 122, "loop": 0},
        {"name": "Reika_6A", "take": "144_02", "first": 135, "last": 160, "loop": 0},
        {"name": "Reika_Throw", "take": "144_02", "first": 175, "last": 200, "loop": 0},
        {"name": "Reika_CounterAssault", "take": "144_02", "first": 135, "last": 160, "loop": 0},
        {"name": "Reika_CrushTrigger", "take": "144_02", "first": 15, "last": 42, "loop": 0},
    ],
    "144/144_03": [
        {"name": "Reika_5D", "take": "144_03", "first": 10, "last": 35, "loop": 0},
        {"name": "Reika_2D", "take": "144_03", "first": 45, "last": 70, "loop": 0},
        {"name": "Reika_jD", "take": "144_03", "first": 80, "last": 105, "loop": 0},
        {"name": "Reika_Riposte", "take": "144_03", "first": 115, "last": 145, "loop": 0},
    ],
    "118/118_05": [
        {"name": "Reika_jA", "take": "118_05", "first": 12, "last": 28, "loop": 0},
        {"name": "Reika_jB", "take": "118_05", "first": 35, "last": 55, "loop": 0},
        {"name": "Reika_jC", "take": "118_05", "first": 65, "last": 88, "loop": 0},
        {"name": "Reika_AirThrow", "take": "118_05", "first": 95, "last": 118, "loop": 0},
    ],
    "135/135_01": [
        {"name": "Reika_41236C", "take": "135_01", "first": 10, "last": 45, "loop": 0},
        {"name": "Reika_623C", "take": "135_01", "first": 60, "last": 90, "loop": 0},
        {"name": "Reika_236236C", "take": "135_01", "first": 100, "last": 140, "loop": 0},
        {"name": "Reika_632146C", "take": "135_01", "first": 155, "last": 200, "loop": 0},
    ],
    "16/16_22": [
        {"name": "Reika_Walk", "take": "16_22", "first": 10, "last": 38, "loop": 1},
        {"name": "Reika_Dash", "take": "16_22", "first": 50, "last": 72, "loop": 0},
    ],
    "143/143_08": [
        {"name": "Reika_Jump", "take": "143_08", "first": 10, "last": 32, "loop": 0},
    ],
    "139/139_11": [
        {"name": "Reika_Crouch", "take": "139_11", "first": 10, "last": 40, "loop": 1},
    ],
    "144/144_09": [
        {"name": "Reika_StandBlock", "take": "144_09", "first": 15, "last": 35, "loop": 0},
    ],
    "135/135_09": [
        {"name": "Reika_CrouchBlock", "take": "135_09", "first": 15, "last": 35, "loop": 0},
    ],
    "140/140_03": [
        {"name": "Reika_Knockdown", "take": "140_03", "first": 10, "last": 45, "loop": 0},
    ],
    "16/16_12": [
        {"name": "Reika_Intro", "take": "16_12", "first": 10, "last": 40, "loop": 0},
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
    print("Applying C10 Reika CMU mocap animation clips...")
    total_added = 0
    for rel_path, clips in REIKA_CLIPS.items():
        total_added += apply_clips_to_meta(rel_path, clips)

    print(f"Done! Successfully applied {total_added} clips across {len(REIKA_CLIPS)} FBX meta files.")
    return 0


if __name__ == "__main__":
    main()
