using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Blittable struct representing a single active projectile in the deterministic simulation.
    /// Updated strictly on the 60 Hz simulation tick (Step 8 of BBMatchSim).
    /// </summary>
    [Serializable]
    public struct BBProjectileState
    {
        public bool active;
        public int id;
        public int ownerIndex; // 0 for P1, 1 for P2
        public Vector2 position;
        public Vector2 velocity;
        public float halfWidth;
        public float halfHeight;
        public float damage;
        public int hitstopAttacker;
        public int hitstopDefender;
        public int hitstun;
        public int blockstun;
        public BBGuard guard;
        public int lifetimeTicks;
        public int hitsRemaining;
        public byte hitIndex;
        public string visualKey;

        public BBWorldBox ToWorldBox()
        {
            return new BBWorldBox
            {
                kind = BBBoxKind.Hit,
                attribute = BBAttribute.Projectile,
                hitIndex = hitIndex,
                minX = position.x - halfWidth,
                maxX = position.x + halfWidth,
                minY = position.y - halfHeight,
                maxY = position.y + halfHeight
            };
        }
    }
}
