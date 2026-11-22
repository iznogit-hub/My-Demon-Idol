using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    [CreateAssetMenu(fileName = "ClipImpact", menuName = "Demon Idol/BlazBlue/Clip Impact Data")]
    public class BBClipImpactAsset : ScriptableObject
    {
        [Serializable]
        public struct ImpactEntry
        {
            public string clipName;
            public int impactFrame60;
            public float impactTime;
            public float maxVelocity;
        }

        public List<ImpactEntry> entries = new List<ImpactEntry>();

        public bool TryGetImpact(string clipName, out ImpactEntry result)
        {
            result = default;
            if (string.IsNullOrEmpty(clipName) || entries == null) return false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].clipName, clipName, StringComparison.OrdinalIgnoreCase))
                {
                    result = entries[i];
                    return true;
                }
            }
            return false;
        }
    }
}
