using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// ScriptableObject storing per-tick root movement displacement for an attack.
    /// Provides deterministic trajectory without using Unity root motion or PhysX.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMotionTrack", menuName = "DemonIdol/BlazBlue/Motion Track")]
    public class BBMotionTrack : ScriptableObject
    {
        [Tooltip("Total duration in 60 Hz ticks.")]
        public int tickCount = 1;

        [Tooltip("Per-tick displacement in meters relative to facing direction (X = forward/back, Y = up/down).")]
        public Vector2[] deltas = new Vector2[1] { Vector2.zero };

        /// <summary>
        /// Retrieves the world-space displacement vector for the given tick and facing direction.
        /// Facing right = +1, Facing left = -1.
        /// </summary>
        public Vector2 GetDelta(int tick, int facing)
        {
            if (deltas == null || deltas.Length == 0) return Vector2.zero;
            int clampedTick = Mathf.Clamp(tick, 0, deltas.Length - 1);
            Vector2 localDelta = deltas[clampedTick];
            return new Vector2(localDelta.x * facing, localDelta.y);
        }

        public static BBMotionTrack CreateStatic(int ticks, Vector2 constantVelocityPerTick)
        {
            var track = ScriptableObject.CreateInstance<BBMotionTrack>();
            track.tickCount = Mathf.Max(1, ticks);
            track.deltas = new Vector2[track.tickCount];
            for (int i = 0; i < track.tickCount; i++)
            {
                track.deltas[i] = constantVelocityPerTick;
            }
            return track;
        }

        public static BBMotionTrack CreateFromDeltas(Vector2[] sourceDeltas)
        {
            var track = ScriptableObject.CreateInstance<BBMotionTrack>();
            if (sourceDeltas == null || sourceDeltas.Length == 0)
            {
                track.tickCount = 1;
                track.deltas = new Vector2[1] { Vector2.zero };
                return track;
            }

            track.tickCount = sourceDeltas.Length;
            track.deltas = new Vector2[sourceDeltas.Length];
            Array.Copy(sourceDeltas, track.deltas, sourceDeltas.Length);
            return track;
        }
    }
}
