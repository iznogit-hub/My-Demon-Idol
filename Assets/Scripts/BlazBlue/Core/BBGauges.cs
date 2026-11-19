using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Blittable struct tracking all BlazBlue meters:
    /// Heat (0..100), Barrier (0..1000), Burst (0..1000), and Overdrive timer.
    /// Pure deterministic mathematical updates; zero allocations.
    /// </summary>
    [Serializable]
    public struct BBGauges
    {
        [Header("Heat Meter (0..100)")]
        public float heat;

        [Header("Barrier Meter (0..1000)")]
        public float barrier;
        public int ticksSinceLastBarrier;
        public bool isDanger;
        public int dangerTicksRemaining;

        [Header("Burst Meter (0..1000) & Overdrive")]
        public float burst;
        public bool isOverdriveActive;
        public int overdriveTicksRemaining;
        public int overdriveTotalTicks;

        public void Initialize(BBSystemTuning tuning)
        {
            heat = 0f;
            barrier = tuning != null ? tuning.barrierMax : 1000f;
            burst = tuning != null ? tuning.burstMax : 1000f;
            ticksSinceLastBarrier = 100;
            isDanger = false;
            dangerTicksRemaining = 0;
            isOverdriveActive = false;
            overdriveTicksRemaining = 0;
            overdriveTotalTicks = 0;
        }

        /// <summary>
        /// Fixed per-tick gauge updates: passive regen, low-HP heat regen, and OD timer countdown.
        /// </summary>
        public void Tick(BBSystemTuning tuning, float hpFraction, bool isBarriering)
        {
            if (tuning == null) return;

            // 1. Overdrive Countdown & Passive Heat Generation
            if (isOverdriveActive)
            {
                if (overdriveTicksRemaining > 0)
                {
                    overdriveTicksRemaining--;
                    // Heat regenerates +0.1 per tick during Overdrive
                    heat = Mathf.Min(tuning.heatMax, heat + 0.10f);
                    if (overdriveTicksRemaining <= 0)
                    {
                        isOverdriveActive = false;
                    }
                }
            }
            else
            {
                // Passive Burst Regen (+1 per tick when not in Overdrive)
                burst = Mathf.Min(tuning.burstMax, burst + tuning.burstRegenPerTick);
            }

            // 2. Low-HP Heat Regeneration (at or below lowHpThreshold e.g. 35%)
            if (hpFraction <= tuning.lowHpThreshold)
            {
                heat = Mathf.Min(tuning.heatMax, heat + tuning.lowHpRegenPerTick);
            }

            // 3. Barrier Drain, Danger State, and Regeneration
            if (isBarriering && !isDanger)
            {
                ticksSinceLastBarrier = 0;
                barrier = Mathf.Max(0f, barrier - tuning.barrierHoldCost);
                if (barrier <= 0f)
                {
                    // Barrier broke! Enter Danger state
                    isDanger = true;
                    dangerTicksRemaining = tuning.dangerTicks;
                }
            }
            else
            {
                ticksSinceLastBarrier++;

                if (isDanger)
                {
                    dangerTicksRemaining--;
                    // Danger ends after dangerTicks or if barrier recovers to 300
                    if (dangerTicksRemaining <= 0 || barrier >= 300f)
                    {
                        isDanger = false;
                        dangerTicksRemaining = 0;
                    }
                }

                // Barrier passive regen after delay
                if (ticksSinceLastBarrier >= tuning.barrierRegenDelay && barrier < tuning.barrierMax)
                {
                    barrier = Mathf.Min(tuning.barrierMax, barrier + tuning.barrierRegen);
                }
            }
        }

        public void AddHeatOnHit(BBButton button, BBMoveKind kind, bool blocked, BBSystemTuning tuning)
        {
            if (tuning == null) return;

            float gain = 0f;
            if (kind == BBMoveKind.Special) gain = tuning.heatOnHitSpecial;
            else if ((button & BBButton.C) != 0) gain = tuning.heatOnHitC;
            else if ((button & BBButton.B) != 0) gain = tuning.heatOnHitB;
            else if ((button & BBButton.D) != 0) gain = tuning.heatOnHitD;
            else gain = tuning.heatOnHitA;

            if (blocked) gain *= 0.5f;

            heat = Mathf.Min(tuning.heatMax, heat + gain);
        }

        public void AddHeatOnDamageTaken(float damage, BBSystemTuning tuning)
        {
            if (tuning == null) return;
            heat = Mathf.Min(tuning.heatMax, heat + (damage * tuning.heatPerDamageTaken));
        }

        public void AddHeatInstantBlock(BBSystemTuning tuning)
        {
            if (tuning == null) return;
            heat = Mathf.Min(tuning.heatMax, heat + 5f);
        }

        public bool TrySpendHeat(float cost)
        {
            if (heat >= cost)
            {
                heat -= cost;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Activates Overdrive based on current HP fraction.
        /// Duration: lerp(480, 180, hpFraction). If mid-combo, scaled by activeODPenalty (0.6x).
        /// </summary>
        public bool TryActivateOverdrive(float hpFraction, bool isComboActive, BBSystemTuning tuning)
        {
            if (burst < (tuning != null ? tuning.burstMax : 1000f)) return false;

            burst = 0f;
            isOverdriveActive = true;

            int minT = tuning != null ? tuning.odMinTicks : 180;
            int maxT = tuning != null ? tuning.odMaxTicks : 480;
            float duration = Mathf.Lerp(maxT, minT, Mathf.Clamp01(hpFraction));

            if (isComboActive && tuning != null)
            {
                duration *= tuning.activeODPenalty;
            }

            overdriveTotalTicks = Mathf.RoundToInt(duration);
            overdriveTicksRemaining = overdriveTotalTicks;
            return true;
        }

        /// <summary>
        /// Spends the Burst meter for a Defensive Burst shockwave.
        /// </summary>
        public bool TrySpendBurst()
        {
            if (burst >= 1000f)
            {
                burst = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies barrier block damage deduction.
        /// </summary>
        public void ApplyBarrierBlockDamage(float damage, BBSystemTuning tuning)
        {
            float cost = damage * (tuning != null ? tuning.barrierHitCostPerDamage : 0.5f);
            barrier = Mathf.Max(0f, barrier - cost);

            if (barrier <= 0f)
            {
                isDanger = true;
                dangerTicksRemaining = tuning != null ? tuning.dangerTicks : 300;
            }
        }
    }
}
