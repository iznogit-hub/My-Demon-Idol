using System;
using System.Collections.Generic;

namespace DemonIdol.BlazBlue
{
    [Flags]
    public enum BBBounceType : byte
    {
        None = 0,
        GroundBounce = 1 << 0,
        WallBounce = 1 << 1,
        WallStick = 1 << 2,
        GroundSlide = 1 << 3
    }

    /// <summary>
    /// Encapsulates full combo state, proration, hitstun decay, gravity scaling,
    /// and bounce interactions for a combo instance.
    /// Zero allocation during active combat.
    /// </summary>
    [Serializable]
    public struct BBComboState
    {
        public int hitCount;
        public int comboDurationTicks;
        public int airHitsInCombo;
        public float comboStarterP1;
        public float accumulatedP2;
        public bool isFatalCounter;
        public BBBounceType usedBounceMask;
        public int rapidCancelCount;

        // Up to 16 distinct moves tracked for same-move penalty
        private const int MAX_TRACKED_MOVES = 16;
        public int trackedMoveCount;
        public int moveHash0, moveHash1, moveHash2, moveHash3, moveHash4, moveHash5, moveHash6, moveHash7;
        public byte moveUses0, moveUses1, moveUses2, moveUses3, moveUses4, moveUses5, moveUses6, moveUses7;

        public void Reset()
        {
            hitCount = 0;
            comboDurationTicks = 0;
            airHitsInCombo = 0;
            comboStarterP1 = 1.0f;
            accumulatedP2 = 1.0f;
            isFatalCounter = false;
            usedBounceMask = BBBounceType.None;
            rapidCancelCount = 0;
            trackedMoveCount = 0;

            moveHash0 = moveHash1 = moveHash2 = moveHash3 = moveHash4 = moveHash5 = moveHash6 = moveHash7 = 0;
            moveUses0 = moveUses1 = moveUses2 = moveUses3 = moveUses4 = moveUses5 = moveUses6 = moveUses7 = 0;
        }

        public void RegisterHit(
            int moveHash,
            float moveP1,
            float moveP2,
            bool isAirHit,
            bool isFatalStarter = false)
        {
            hitCount++;

            if (hitCount == 1)
            {
                comboStarterP1 = moveP1;
                accumulatedP2 = 1.0f;
                isFatalCounter = isFatalStarter;
            }
            else
            {
                // Fatal Counter skips P2 decay on the 2nd hit
                if (!(isFatalCounter && hitCount == 2))
                {
                    accumulatedP2 *= moveP2;
                }
            }

            if (isAirHit)
            {
                airHitsInCombo++;
            }

            IncrementMoveUsage(moveHash);
        }

        public int GetMoveUsageCount(int moveHash)
        {
            if (moveHash == 0) return 1;
            if (moveHash0 == moveHash) return moveUses0;
            if (moveHash1 == moveHash) return moveUses1;
            if (moveHash2 == moveHash) return moveUses2;
            if (moveHash3 == moveHash) return moveUses3;
            if (moveHash4 == moveHash) return moveUses4;
            if (moveHash5 == moveHash) return moveUses5;
            if (moveHash6 == moveHash) return moveUses6;
            if (moveHash7 == moveHash) return moveUses7;
            return 1;
        }

        private void IncrementMoveUsage(int moveHash)
        {
            if (moveHash == 0) return;

            if (moveHash0 == moveHash) { moveUses0++; return; }
            if (moveHash1 == moveHash) { moveUses1++; return; }
            if (moveHash2 == moveHash) { moveUses2++; return; }
            if (moveHash3 == moveHash) { moveUses3++; return; }
            if (moveHash4 == moveHash) { moveUses4++; return; }
            if (moveHash5 == moveHash) { moveUses5++; return; }
            if (moveHash6 == moveHash) { moveUses6++; return; }
            if (moveHash7 == moveHash) { moveUses7++; return; }

            // Add new move slot
            if (moveHash0 == 0) { moveHash0 = moveHash; moveUses0 = 1; return; }
            if (moveHash1 == 0) { moveHash1 = moveHash; moveUses1 = 1; return; }
            if (moveHash2 == 0) { moveHash2 = moveHash; moveUses2 = 1; return; }
            if (moveHash3 == 0) { moveHash3 = moveHash; moveUses3 = 1; return; }
            if (moveHash4 == 0) { moveHash4 = moveHash; moveUses4 = 1; return; }
            if (moveHash5 == 0) { moveHash5 = moveHash; moveUses5 = 1; return; }
            if (moveHash6 == 0) { moveHash6 = moveHash; moveUses6 = 1; return; }
            if (moveHash7 == 0) { moveHash7 = moveHash; moveUses7 = 1; return; }
        }

        /// <summary>
        /// Calculates remaining ground hitstun factoring in combo duration decay and Fatal Counter shift.
        /// </summary>
        public int CalculateDecayedHitstun(int authoredHitstun, bool isGroundHit)
        {
            int effectiveTime = comboDurationTicks;
            if (isFatalCounter)
            {
                // Fatal Counter shifts decay thresholds by +120 ticks
                effectiveTime = Math.Max(0, effectiveTime - 120);
            }

            int decay = 0;
            if (isGroundHit)
            {
                if (effectiveTime > 540) decay = 8;
                else if (effectiveTime > 420) decay = 6;
                else if (effectiveTime > 300) decay = 4;
                else if (effectiveTime > 180) decay = 2;
                return Math.Max(6, authoredHitstun - decay);
            }
            else
            {
                if (effectiveTime > 540) decay = 24;
                else if (effectiveTime > 420) decay = 18;
                else if (effectiveTime > 300) decay = 12;
                else if (effectiveTime > 180) decay = 6;
                return Math.Max(6, authoredHitstun - decay);
            }
        }

        /// <summary>
        /// Calculates current juggle gravity scaling.
        /// Base gravity increases by 0.06 per air hit, capped at 1.9x.
        /// Fatal Counter applies 0.8x gravity modifier.
        /// </summary>
        public float CalculateJuggleGravity(float baseGravity, float scalePerHit = 0.06f, float cap = 1.9f)
        {
            float scale = 1.0f + (scalePerHit * airHitsInCombo);
            scale = Math.Min(cap, scale);

            if (isFatalCounter)
            {
                scale *= 0.80f;
            }

            return baseGravity * scale;
        }

        /// <summary>
        /// Attempts to register a bounce interaction (ground bounce, wall bounce, wall stick, ground slide).
        /// Each type can only trigger ONCE per combo.
        /// </summary>
        public bool TryRegisterBounce(BBBounceType bounceType)
        {
            if (bounceType == BBBounceType.None) return false;

            if ((usedBounceMask & bounceType) != 0)
            {
                return false; // Already used this bounce in current combo
            }

            usedBounceMask |= bounceType;
            return true;
        }
    }
}
