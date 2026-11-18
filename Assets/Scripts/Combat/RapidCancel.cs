using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Implements BlazBlue Rapid Cancel (Guilty Gear Roman Cancel).
    /// Spends 50% Super Meter to immediately abort any attack into Neutral (0 frames),
    /// making unsafe moves safe or extending combos infinitely.
    /// </summary>
    public static class RapidCancel
    {
        public const float RAPID_CANCEL_COST = 50.0f;

        public static bool CanExecute(FighterController fighter)
        {
            if (fighter == null) return false;

            bool isAttacking = fighter.State == FighterState.AttackStartup ||
                               fighter.State == FighterState.AttackActive ||
                               fighter.State == FighterState.AttackRecovery;

            return isAttacking && fighter.currentSuperMeter >= RAPID_CANCEL_COST;
        }

        public static bool Execute(FighterController fighter)
        {
            if (!CanExecute(fighter)) return false;

            fighter.SpendSuperMeter(RAPID_CANCEL_COST);
            fighter.ForceCancelToNeutral();

            // Trigger Rapid Cancel screen flash and trauma
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.25f);
            }

            return true;
        }
    }
}
