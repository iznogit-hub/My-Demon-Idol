using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Implements 2.5D air juggle physics, dynamic gravity scaling,
    /// a 6-point juggle budget (launcher spends 2, follow-ups spend 1),
    /// strict combo damage scaling (100%..30%, min 40% super),
    /// and 1-per-combo wall and OTG limits.
    /// </summary>
    [System.Obsolete("Superseded by BBComboState in Assets/Scripts/BlazBlue/. Do not use in new code.", false)]
    public class JuggleEngine
    {
        public const float BASE_GRAVITY = 20f;
        public const float GRAVITY_SCALING_PER_HIT = 0.15f;
        public const int TOTAL_JUGGLE_BUDGET = 6;
        public const int LAUNCHER_BUDGET_COST = 2;
        public const int FOLLOWUP_BUDGET_COST = 1;

        public int JuggleBudgetRemaining { get; private set; } = TOTAL_JUGGLE_BUDGET;
        public int ComboHitCount { get; private set; } = 0;
        public bool HasUsedWallInteraction { get; private set; } = false;
        public bool HasUsedOTGHit { get; private set; } = false;
        public bool IsAirborne { get; private set; } = false;

        public event Action<int> OnHitIncremented;
        public event Action OnComboReset;

        public void RegisterHit()
        {
            ComboHitCount++;
            OnHitIncremented?.Invoke(ComboHitCount);
        }

        public void RegisterLaunch(Vector2 launchVelocity)
        {
            IsAirborne = true;
            ComboHitCount = 1;
            JuggleBudgetRemaining = TOTAL_JUGGLE_BUDGET - LAUNCHER_BUDGET_COST;
            HasUsedWallInteraction = false;
            HasUsedOTGHit = false;
            OnHitIncremented?.Invoke(ComboHitCount);
        }

        public bool TryRegisterJuggleHit(AttackData attack, out Vector2 resultingVelocity, out bool forcesKnockdown)
        {
            resultingVelocity = Vector2.zero;
            forcesKnockdown = false;

            if (!IsAirborne) return false;

            ComboHitCount++;
            OnHitIncremented?.Invoke(ComboHitCount);

            if (JuggleBudgetRemaining < FOLLOWUP_BUDGET_COST)
            {
                // Juggle budget exhausted: force unextendable knockdown
                forcesKnockdown = true;
                resultingVelocity = new Vector2(attack.launchVelocity.x * 0.3f, -4f);
                return true;
            }

            JuggleBudgetRemaining -= FOLLOWUP_BUDGET_COST;

            float liftDecay = Mathf.Max(0.35f, 1.0f - (ComboHitCount * 0.10f));
            resultingVelocity = new Vector2(
                attack.launchVelocity.x * liftDecay,
                (attack.launchVelocity.y > 0 ? attack.launchVelocity.y : 3.5f) * liftDecay
            );

            if (JuggleBudgetRemaining <= 0)
            {
                forcesKnockdown = true;
            }

            return true;
        }

        /// <summary>
        /// Strict combo damage scaling by hit:
        /// 100%, 90%, 80%, 70%, 60%, 50%, 40%, then minimum 30%.
        /// Super finisher has a 40% minimum damage floor.
        /// </summary>
        public static float GetDamageScale(int hitIndex, bool isSuper)
        {
            float scale;
            switch (hitIndex)
            {
                case 1: scale = 1.00f; break;
                case 2: scale = 0.90f; break;
                case 3: scale = 0.80f; break;
                case 4: scale = 0.70f; break;
                case 5: scale = 0.60f; break;
                case 6: scale = 0.50f; break;
                case 7: scale = 0.40f; break;
                default: scale = 0.30f; break;
            }
            if (isSuper) scale = Mathf.Max(0.40f, scale);
            return scale;
        }

        public bool TryRegisterWallInteraction()
        {
            if (HasUsedWallInteraction) return false;
            HasUsedWallInteraction = true;
            return true;
        }

        public bool TryRegisterOTGHit(out float otgMultiplier)
        {
            otgMultiplier = 0.50f;
            if (HasUsedOTGHit) return false;
            HasUsedOTGHit = true;
            return true;
        }

        public float GetCurrentDamageScale(float initialProration = 1.0f)
        {
            return GetDamageScale(Mathf.Max(1, ComboHitCount), false);
        }

        public float GetCurrentGravity()
        {
            return BASE_GRAVITY * (1.0f + (ComboHitCount * GRAVITY_SCALING_PER_HIT));
        }

        public void OnLandedOnGround()
        {
            IsAirborne = false;
        }

        public void ForceResetCombo()
        {
            IsAirborne = false;
            ComboHitCount = 0;
            JuggleBudgetRemaining = TOTAL_JUGGLE_BUDGET;
            HasUsedWallInteraction = false;
            HasUsedOTGHit = false;
            OnComboReset?.Invoke();
        }
    }
}
