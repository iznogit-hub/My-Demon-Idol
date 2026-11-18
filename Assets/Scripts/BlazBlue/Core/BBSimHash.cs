using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Deterministic FNV-1a 32-bit and 64-bit state hashing for simulation verification and replay integrity.
    /// Hashes fighter positions rounded to 0.1 mm (10,000 units/meter), discrete states, and gauges.
    /// </summary>
    public static class BBSimHash
    {
        private const uint FNV_PRIME_32 = 16777619;
        private const uint FNV_OFFSET_32 = 2166136261;

        private const ulong FNV_PRIME_64 = 1099511628211;
        private const ulong FNV_OFFSET_64 = 14695981039346656037;

        public static uint HashValue(uint currentHash, int value)
        {
            unchecked
            {
                currentHash ^= (uint)(value & 0xFF);
                currentHash *= FNV_PRIME_32;
                currentHash ^= (uint)((value >> 8) & 0xFF);
                currentHash *= FNV_PRIME_32;
                currentHash ^= (uint)((value >> 16) & 0xFF);
                currentHash *= FNV_PRIME_32;
                currentHash ^= (uint)((value >> 24) & 0xFF);
                currentHash *= FNV_PRIME_32;
                return currentHash;
            }
        }

        public static uint HashValue(uint currentHash, float value)
        {
            // Quantize to 0.1 mm precision (10,000 units per meter)
            int quantized = Mathf.RoundToInt(value * 10000f);
            return HashValue(currentHash, quantized);
        }

        public static uint HashValue(uint currentHash, byte value)
        {
            unchecked
            {
                currentHash ^= value;
                currentHash *= FNV_PRIME_32;
                return currentHash;
            }
        }

        /// <summary>
        /// Generates a deterministic 32-bit state hash for a combat tick.
        /// </summary>
        public static uint ComputeTickHash(
            int tick,
            float p1x, float p1y, int p1Facing, byte p1State, int p1Hp, int p1Heat,
            float p2x, float p2y, int p2Facing, byte p2State, int p2Hp, int p2Heat,
            int comboHitCount)
        {
            uint hash = FNV_OFFSET_32;
            hash = HashValue(hash, tick);

            // Fighter 1
            hash = HashValue(hash, p1x);
            hash = HashValue(hash, p1y);
            hash = HashValue(hash, p1Facing);
            hash = HashValue(hash, p1State);
            hash = HashValue(hash, p1Hp);
            hash = HashValue(hash, p1Heat);

            // Fighter 2
            hash = HashValue(hash, p2x);
            hash = HashValue(hash, p2y);
            hash = HashValue(hash, p2Facing);
            hash = HashValue(hash, p2State);
            hash = HashValue(hash, p2Hp);
            hash = HashValue(hash, p2Heat);

            // Global combo counter
            hash = HashValue(hash, comboHitCount);

            return hash;
        }
    }
}
