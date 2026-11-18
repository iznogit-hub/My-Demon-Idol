using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// ScriptableObject storing baked per-tick AABB boxes with run-length indexing.
    /// Provides zero-allocation runtime queries for hitboxes, hurtboxes, and pushboxes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoxTrack", menuName = "DemonIdol/BlazBlue/Box Track")]
    public class BBBoxTrack : ScriptableObject
    {
        [Tooltip("Total duration of the track in 60 Hz combat ticks (startup + active + recovery).")]
        public int tickCount = 1;

        [Tooltip("Frame start indices. Length is tickCount + 1. Tick t contains boxes[frameStart[t] .. frameStart[t+1]).")]
        public int[] frameStart = new int[2] { 0, 0 };

        [Tooltip("Flat pre-baked array of all boxes across the entire track.")]
        public BBBox[] boxes = new BBBox[0];

        [Tooltip("Keyframe ticks used during authoring (Editor only).")]
        public int[] keyTicks = new int[0];

        [Tooltip("Hash of source clip parameters (clip guid, firstFrame, lastFrame, speed, avatar guid).")]
        public string bakeHash = "";

        /// <summary>
        /// Retrieves the world-space boxes for a specific tick into a preallocated array.
        /// Returns the number of boxes written. Zero allocation.
        /// </summary>
        public int GetBoxes(int tick, BBWorldBox[] dst, float posX, float posY, int facingDirection)
        {
            if (dst == null || dst.Length == 0 || tickCount <= 0 || frameStart == null || boxes == null)
            {
                return 0;
            }

            int clampedTick = Mathf.Clamp(tick, 0, tickCount - 1);
            if (clampedTick + 1 >= frameStart.Length)
            {
                return 0;
            }

            int start = frameStart[clampedTick];
            int end = frameStart[clampedTick + 1];
            int writeCount = 0;

            for (int i = start; i < end && writeCount < dst.Length; i++)
            {
                dst[writeCount++] = BBWorldBox.FromLocal(in boxes[i], posX, posY, facingDirection);
            }

            return writeCount;
        }

        /// <summary>
        /// Populates a zero-allocation BBBoxBuffer with world-space boxes for the given tick.
        /// </summary>
        public void GetBoxes(int tick, BBBoxBuffer dst, float posX, float posY, int facingDirection)
        {
            if (dst == null || tickCount <= 0 || frameStart == null || boxes == null)
            {
                return;
            }

            int clampedTick = Mathf.Clamp(tick, 0, tickCount - 1);
            if (clampedTick + 1 >= frameStart.Length)
            {
                return;
            }

            int start = frameStart[clampedTick];
            int end = frameStart[clampedTick + 1];

            for (int i = start; i < end; i++)
            {
                dst.AddLocal(in boxes[i], posX, posY, facingDirection);
            }
        }

        /// <summary>
        /// Factory helper to build a static track holding identical boxes for all ticks.
        /// </summary>
        public static BBBoxTrack CreateStatic(int ticks, BBBox[] staticBoxes)
        {
            var track = ScriptableObject.CreateInstance<BBBoxTrack>();
            track.tickCount = Mathf.Max(1, ticks);
            int boxCount = staticBoxes != null ? staticBoxes.Length : 0;
            track.boxes = new BBBox[boxCount];
            if (boxCount > 0)
            {
                Array.Copy(staticBoxes, track.boxes, boxCount);
            }

            track.frameStart = new int[track.tickCount + 1];
            for (int t = 0; t <= track.tickCount; t++)
            {
                track.frameStart[t] = t == 0 ? 0 : boxCount; // Run-length reference
            }
            // For simple range [start..end), all ticks 0..tickCount-1 refer to 0..boxCount
            for (int t = 0; t < track.tickCount; t++)
            {
                track.frameStart[t] = 0;
            }
            track.frameStart[track.tickCount] = boxCount;

            track.keyTicks = new int[] { 0 };
            return track;
        }

        /// <summary>
        /// Factory helper to build a keyed track from per-tick box arrays.
        /// </summary>
        public static BBBoxTrack CreateFromPerTick(BBBox[][] perTickBoxes)
        {
            var track = ScriptableObject.CreateInstance<BBBoxTrack>();
            if (perTickBoxes == null || perTickBoxes.Length == 0)
            {
                track.tickCount = 1;
                track.frameStart = new int[2] { 0, 0 };
                track.boxes = new BBBox[0];
                return track;
            }

            track.tickCount = perTickBoxes.Length;
            track.frameStart = new int[track.tickCount + 1];

            int totalBoxes = 0;
            for (int t = 0; t < track.tickCount; t++)
            {
                if (perTickBoxes[t] != null) totalBoxes += perTickBoxes[t].Length;
            }

            track.boxes = new BBBox[totalBoxes];
            int currentOffset = 0;

            for (int t = 0; t < track.tickCount; t++)
            {
                track.frameStart[t] = currentOffset;
                var currentList = perTickBoxes[t];
                if (currentList != null && currentList.Length > 0)
                {
                    Array.Copy(currentList, 0, track.boxes, currentOffset, currentList.Length);
                    currentOffset += currentList.Length;
                }
            }
            track.frameStart[track.tickCount] = currentOffset;
            return track;
        }
    }
}
