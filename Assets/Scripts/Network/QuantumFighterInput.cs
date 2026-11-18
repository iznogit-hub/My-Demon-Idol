using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Network
{
    /// <summary>
    /// Blittable 16-bit input struct transmitted over Photon Quantum.
    /// Packed for ultra-low latency UDP transport with zero floating-point drift.
    /// </summary>
    [Serializable]
    public struct QuantumFighterInput
    {
        public sbyte Horizontal; // -1 (Back), 0 (Neutral), 1 (Forward)
        public sbyte Vertical;   // -1 (Crouch), 0 (Neutral), 1 (Up)
        public ushort Buttons;   // Bitmask of InputButton enum

        public void Pack(Vector2 dir, InputButton buttons)
        {
            Horizontal = (sbyte)Mathf.Clamp(Mathf.RoundToInt(dir.x), -1, 1);
            Vertical = (sbyte)Mathf.Clamp(Mathf.RoundToInt(dir.y), -1, 1);
            Buttons = (ushort)buttons;
        }

        public bool HasButton(InputButton btn) => ((InputButton)Buttons & btn) != 0;
        public Vector2 GetDirection() => new Vector2(Horizontal, Vertical);
    }
}
