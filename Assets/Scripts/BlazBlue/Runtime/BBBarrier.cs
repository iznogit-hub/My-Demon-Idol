using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Non-destructive BlazBlue Barrier subsystem.
    /// Manages A+B barrier guarding, Instant Barrier detection, zero chip mitigation,
    /// 2.2x/2.6x pushback multiplication, and Danger state.
    /// </summary>
    public class BBBarrier : MonoBehaviour
    {
        [Header("References")]
        public BBFighterBrain brain;
        public BBSystemTuning tuning;

        private int _barrierPressTick = -999;

        private void Awake()
        {
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (tuning == null) tuning = BBSystemTuning.Default;
        }

        public void NotifyBarrierPressed(int currentTick)
        {
            _barrierPressTick = currentTick;
        }

        /// <summary>
        /// Checks whether incoming block qualifies as an Instant Barrier (pressed <= 8 ticks before impact).
        /// </summary>
        public bool IsInstantBarrier(int currentTick)
        {
            int window = tuning != null ? tuning.instantBlockWindow : 8;
            return (currentTick - _barrierPressTick) >= 0 && (currentTick - _barrierPressTick) <= window;
        }

        /// <summary>
        /// Applies barrier block results to the defender.
        /// </summary>
        public void ApplyBarrierBlock(float damage, int currentTick, out float pushbackMultiplier, out int blockstunReduction)
        {
            bool isIB = IsInstantBarrier(currentTick);

            if (isIB)
            {
                pushbackMultiplier = tuning != null ? tuning.instantBarrierPushback : 2.6f;
                blockstunReduction = 2; // -2 ticks blockstun
                if (brain != null)
                {
                    brain.gauges.heat = Mathf.Min(tuning != null ? tuning.heatMax : 100f, brain.gauges.heat + 3f);
                }
            }
            else
            {
                pushbackMultiplier = tuning != null ? tuning.barrierPushback : 2.2f;
                blockstunReduction = 0;
            }

            // Deduct barrier meter
            if (brain != null)
            {
                brain.gauges.ApplyBarrierBlockDamage(damage, tuning);
            }
        }
    }
}
