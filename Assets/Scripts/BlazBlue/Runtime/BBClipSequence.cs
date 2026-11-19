using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    [Serializable]
    public struct BBClipEntry
    {
        public AnimationClip clip;
        public int startTick;
        public int firstFrame;
        public int lastFrame;
    }

    /// <summary>
    /// Sequential clip player asset for complex cinematic Distortion Drives and multi-take combos.
    /// Played back-to-back by BBAnimationDriver without creating new merged animation assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewClipSequence", menuName = "DemonIdol/BlazBlue/Clip Sequence")]
    public class BBClipSequence : ScriptableObject
    {
        public BBClipEntry[] entries = new BBClipEntry[0];

        /// <summary>
        /// Finds the active clip and local tick offset within that clip for the given total tick.
        /// </summary>
        public bool GetClipAtTick(int tick, out AnimationClip clip, out int localTick, out int firstFrame, out int lastFrame)
        {
            clip = null;
            localTick = 0;
            firstFrame = 0;
            lastFrame = 0;

            if (entries == null || entries.Length == 0) return false;

            for (int i = entries.Length - 1; i >= 0; i--)
            {
                if (tick >= entries[i].startTick)
                {
                    clip = entries[i].clip;
                    localTick = tick - entries[i].startTick;
                    firstFrame = entries[i].firstFrame;
                    lastFrame = entries[i].lastFrame;
                    return clip != null;
                }
            }

            // Fallback to first entry
            clip = entries[0].clip;
            localTick = tick;
            firstFrame = entries[0].firstFrame;
            lastFrame = entries[0].lastFrame;
            return clip != null;
        }
    }
}
