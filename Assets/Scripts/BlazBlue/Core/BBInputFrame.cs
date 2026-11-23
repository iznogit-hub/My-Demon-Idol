using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    [Flags]
    public enum BBButton : byte
    {
        None  = 0,
        A     = 1,       // Weak normal (fast, short)
        B     = 2,       // Medium normal (pokes, anti-air)
        C     = 4,       // Heavy normal (launchers, big buttons)
        D     = 8,       // Drive (unique character mechanic)
        Taunt = 16       // Taunt
    }

    public enum ControlScheme
    {
        Classic3Button  = 0,
        BlazBlue4Button = 1
    }

    /// <summary>
    /// 4-byte blittable, deterministic input frame. Exactly 1 frame per combat tick (60 Hz).
    /// Stores raw world numpad direction and edge-latched buttons.
    /// </summary>
    [Serializable]
    public struct BBInputFrame
    {
        public byte held;      // Bitmask of BBButton currently held down
        public byte pressed;   // Rising edges (just pressed this tick)
        public byte released;  // Falling edges (just released this tick)
        public sbyte numpad;   // World numpad direction: 1..9 (5 = neutral)

        public bool IsHeld(BBButton btn) => (held & (byte)btn) != 0;
        public bool IsPressed(BBButton btn) => (pressed & (byte)btn) != 0;
        public bool IsReleased(BBButton btn) => (released & (byte)btn) != 0;

        /// <summary>
        /// Converts an analog or 2D vector stick input into a 1..9 world numpad value.
        /// </summary>
        public static sbyte VectorToWorldNumpad(Vector2 raw, float deadzoneX = 0.35f, float deadzoneY = 0.45f)
        {
            int h = raw.x > deadzoneX ? 1 : (raw.x < -deadzoneX ? -1 : 0);
            int v = raw.y > deadzoneY ? 1 : (raw.y < -deadzoneY ? -1 : 0);

            switch (v)
            {
                case 1:  return (sbyte)(h == -1 ? 7 : (h == 1 ? 9 : 8));
                case -1: return (sbyte)(h == -1 ? 1 : (h == 1 ? 3 : 2));
                default: return (sbyte)(h == -1 ? 4 : (h == 1 ? 6 : 5));
            }
        }

        /// <summary>
        /// Translates a world numpad direction to a fighter's facing-relative numpad direction.
        /// When facing left (-1), horizontal directions are mirrored (4 <-> 6, 1 <-> 3, 7 <-> 9).
        /// </summary>
        public static sbyte WorldToFacingNumpad(sbyte worldNumpad, int facingDirection)
        {
            if (facingDirection >= 0) return worldNumpad;

            switch (worldNumpad)
            {
                case 1: return 3;
                case 3: return 1;
                case 4: return 6;
                case 6: return 4;
                case 7: return 9;
                case 9: return 7;
                default: return worldNumpad; // 2, 5, 8
            }
        }

        public static sbyte FacingToWorldNumpad(sbyte facingNumpad, int facingDirection) => WorldToFacingNumpad(facingNumpad, facingDirection);

        /// <summary>
        /// Returns true if the facing numpad direction corresponds to an up / jump direction (7, 8, 9).
        /// </summary>
        public static bool IsUp(sbyte facingNumpad) => facingNumpad >= 7 && facingNumpad <= 9;

        /// <summary>
        /// Returns true if the facing numpad direction corresponds to a down / crouch direction (1, 2, 3).
        /// </summary>
        public static bool IsDown(sbyte facingNumpad) => facingNumpad >= 1 && facingNumpad <= 3;

        /// <summary>
        /// Returns true if the facing numpad direction corresponds to forward (6, 3, 9).
        /// </summary>
        public static bool IsForward(sbyte facingNumpad) => facingNumpad == 6 || facingNumpad == 3 || facingNumpad == 9;

        /// <summary>
        /// Returns true if the facing numpad direction corresponds to back / guard (4, 1, 7).
        /// </summary>
        public static bool IsBack(sbyte facingNumpad) => facingNumpad == 4 || facingNumpad == 1 || facingNumpad == 7;

        /// <summary>
        /// Returns true if the facing numpad direction is neutral (5).
        /// </summary>
        public static bool IsNeutral(sbyte facingNumpad) => facingNumpad == 5;
    }
}
