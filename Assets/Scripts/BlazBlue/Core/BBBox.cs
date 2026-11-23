using System;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Functional type of a 2D combat box on the Z=0 plane.
    /// </summary>
    public enum BBBoxKind : byte
    {
        Hurt,           // Vulnerable area that can receive damage
        Hit,            // Offensive area that deals damage
        Push,           // Solid body volume preventing fighters from clipping
        Throw,          // Grab range check (active during throw startup/active)
        ThrowHurt,      // Vulnerable area that can be grabbed (absent while airborne or throw invulnerable)
        GuardProximity  // Triggers proximity guard (holding back blocks instead of walking)
    }

    /// <summary>
    /// Authoring box definition in local coordinates facing right (20 bytes).
    /// </summary>
    [Serializable]
    public struct BBBox
    {
        public float cx;             // Center X relative to fighter origin (meters)
        public float cy;             // Center Y relative to fighter origin (meters)
        public float hx;             // Half-width X (meters)
        public float hy;             // Half-height Y (meters)
        public BBBoxKind kind;       // Functional category
        public byte hitIndex;        // Groups multi-hit boxes (each index can hit once)
        public BBAttribute attribute;// Head/Body/Foot/Projectile/Throw
        public byte flags;           // Bit 0 = disjoint (no hurtbox), Bit 1 = CH only

        public const byte FLAG_DISJOINT = 1 << 0;
        public const byte FLAG_COUNTER_HIT_ONLY = 1 << 1;

        public bool IsDisjoint => (flags & FLAG_DISJOINT) != 0;
        public bool IsCounterHitOnly => (flags & FLAG_COUNTER_HIT_ONLY) != 0;

        public BBBox(float cx, float cy, float hx, float hy, BBBoxKind kind, byte hitIndex = 0, BBAttribute attribute = BBAttribute.Body, byte flags = 0)
        {
            this.cx = cx;
            this.cy = cy;
            this.hx = hx;
            this.hy = hy;
            this.kind = kind;
            this.hitIndex = hitIndex;
            this.attribute = attribute;
            this.flags = flags;
        }

        public override string ToString() => $"[BBBox {kind} c=({cx:F2},{cy:F2}) h=({hx:F2},{hy:F2}) idx={hitIndex}]";
    }

    /// <summary>
    /// World-space Axis-Aligned Bounding Box (AABB) on the Z=0 plane.
    /// </summary>
    [Serializable]
    public struct BBWorldBox
    {
        public float minX;
        public float minY;
        public float maxX;
        public float maxY;
        public BBBoxKind kind;
        public byte hitIndex;
        public BBAttribute attribute;
        public byte flags;

        public float CenterX => (minX + maxX) * 0.5f;
        public float CenterY => (minY + maxY) * 0.5f;
        public float HalfWidth => (maxX - minX) * 0.5f;
        public float HalfHeight => (maxY - minY) * 0.5f;

        public BBWorldBox(float minX, float minY, float maxX, float maxY, BBBoxKind kind, byte hitIndex = 0, BBAttribute attribute = BBAttribute.Body, byte flags = 0)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            this.kind = kind;
            this.hitIndex = hitIndex;
            this.attribute = attribute;
            this.flags = flags;
        }

        /// <summary>
        /// Transforms a local-space BBBox into world-space given fighter position and facing direction.
        /// Facing right = +1, Facing left = -1.
        /// </summary>
        public static BBWorldBox FromLocal(in BBBox local, float posX, float posY, int facing)
        {
            float worldCx = posX + (facing * local.cx);
            float worldCy = posY + local.cy;
            float absHx = Math.Abs(local.hx);
            float absHy = Math.Abs(local.hy);

            return new BBWorldBox(
                worldCx - absHx,
                worldCy - absHy,
                worldCx + absHx,
                worldCy + absHy,
                local.kind,
                local.hitIndex,
                local.attribute,
                local.flags
            );
        }

        public override string ToString() => $"[BBWorldBox {kind} X=({minX:F2}..{maxX:F2}) Y=({minY:F2}..{maxY:F2})]";
    }

    /// <summary>
    /// Fixed-capacity zero-allocation buffer holding up to 16 BBWorldBox instances per fighter per tick.
    /// </summary>
    public sealed class BBBoxBuffer
    {
        public const int CAPACITY = 16;
        public readonly BBWorldBox[] boxes = new BBWorldBox[CAPACITY];
        public int count = 0;

        public void Clear()
        {
            count = 0;
        }

        public void Add(in BBWorldBox box)
        {
            if (count < CAPACITY)
            {
                boxes[count++] = box;
            }
        }

        public void AddLocal(in BBBox local, float posX, float posY, int facing)
        {
            if (count < CAPACITY)
            {
                boxes[count++] = BBWorldBox.FromLocal(in local, posX, posY, facing);
            }
        }

        public ref BBWorldBox this[int index] => ref boxes[index];
    }

    /// <summary>
    /// 32-bit bitmask tracking which hit indices have connected during an attack instance.
    /// Prevents active frames of a single swing from hitting more than once unless explicitly reset.
    /// </summary>
    public struct BBHitMask
    {
        public uint mask;

        public bool HasHit(byte hitIndex)
        {
            if (hitIndex >= 32) return false;
            return (mask & (1u << hitIndex)) != 0;
        }

        public void MarkHit(byte hitIndex)
        {
            if (hitIndex < 32)
            {
                mask |= (1u << hitIndex);
            }
        }

        public void ClearHit(byte hitIndex)
        {
            if (hitIndex < 32)
            {
                mask &= ~(1u << hitIndex);
            }
        }

        public void Clear()
        {
            mask = 0u;
        }

        public bool IsEmpty => mask == 0u;
    }

    /// <summary>
    /// Contact event record between two fighters during a combat tick.
    /// </summary>
    public enum BBContactType : byte
    {
        Strike,
        Throw
    }

    public struct BBContact
    {
        public BBContactType contactType;
        public int attackerId; // 0 for P1, 1 for P2
        public int defenderId; // 0 for P1, 1 for P2
        public byte hitIndex;
        public BBAttribute attribute;
        public float contactPointX;
        public float contactPointY;
        public BBWorldBox hitBox;
        public BBWorldBox hurtBox;

        public BBContact(BBContactType contactType, int attackerId, int defenderId, byte hitIndex, BBAttribute attribute, float contactPointX, float contactPointY, in BBWorldBox hitBox, in BBWorldBox hurtBox)
        {
            this.contactType = contactType;
            this.attackerId = attackerId;
            this.defenderId = defenderId;
            this.hitIndex = hitIndex;
            this.attribute = attribute;
            this.contactPointX = contactPointX;
            this.contactPointY = contactPointY;
            this.hitBox = hitBox;
            this.hurtBox = hurtBox;
        }
    }
}
