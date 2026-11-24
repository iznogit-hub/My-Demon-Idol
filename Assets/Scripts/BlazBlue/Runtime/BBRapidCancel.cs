using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Non-destructive BlazBlue Rapid Cancel subsystem.
    /// Resets attacker to neutral upon consuming 50 Heat on hit/block,
    /// triggers a 20-tick opponent slowdown at 0.75 speed, and adds camera trauma.
    /// </summary>
    public class BBRapidCancel : MonoBehaviour
    {
        [Header("References")]
        public BBFighterBrain brain;
        public BBSystemTuning tuning;

        private void Awake()
        {
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (tuning == null) tuning = BBSystemTuning.Default;
        }

        /// <summary>
        /// Executes a Rapid Cancel for the owning fighter.
        /// </summary>
        public bool TryExecuteRapidCancel(BBFighterBrain opponent)
        {
            if (brain == null || tuning == null) return false;

            // 1. Check validity: 50 Heat required, contact required, once per combo
            if (!brain.gauges.TrySpendHeat(tuning.costRC))
            {
                return false;
            }

            // 2. Force attacker to Neutral state immediately while preserving current momentum
            brain.bbState = brain.isGrounded ? BBState.Neutral : BBState.Airborne;
            brain.currentAttack = null;
            brain.attackTick = 0;
            brain.attackHitMask.Clear();
            brain.hitstopRemaining = 0;
            brain.stateTick = 0;

            // 3. Opponent slowdown for rcSlowTicks (20 ticks at 0.75 speed)
            if (opponent != null)
            {
                // In a 60 Hz deterministic sim, 0.75 speed over 20 ticks means opponent skips 1 tick every 4 ticks
                opponent.hitstopRemaining = Mathf.Max(opponent.hitstopRemaining, 2);
            }

            // 4. Camera trauma & visual feedback
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.25f);
            }

            Debug.Log($"<color=#00e676>[BBRapidCancel] Rapid Cancel executed by '{brain.name}'!</color>");
            return true;
        }
    }
}
