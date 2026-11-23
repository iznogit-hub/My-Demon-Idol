using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Non-destructive BlazBlue Overdrive subsystem.
    /// Manages A+B+C+D activation, 20-tick super-flash, HP-scaled duration,
    /// invulnerability ticks, and Exceed Accel finishers.
    /// </summary>
    public class BBOverdrive : MonoBehaviour
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
        /// Attempts to trigger Overdrive.
        /// </summary>
        public bool TryTriggerOverdrive(BBFighterBrain opponent, bool isComboActive)
        {
            if (brain == null || tuning == null) return false;

            float hpFraction = brain.health / brain.maxHealth;
            if (!brain.gauges.TryActivateOverdrive(hpFraction, isComboActive, tuning))
            {
                return false;
            }

            // 1. 20-tick super-flash & freeze
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.TriggerSuperfreeze(20);
            }

            // 2. Invulnerability during ticks 1-20
            brain.currentInvuln = BBInvuln.Strike | BBInvuln.Throw | BBInvuln.Projectile;

            // 3. Initial startup pushback to create space in neutral
            if (opponent != null && !isComboActive)
            {
                BBMatchSim.ApplyPushback(brain.transform, opponent.transform, 1.2f, brain.FacingDirection, 1.0f);
            }

            Debug.Log($"<color=#00e676>[BBOverdrive] Overdrive activated on '{brain.name}'! Duration: {brain.gauges.overdriveTicksRemaining} ticks.</color>");
            return true;
        }

        /// <summary>
        /// Fires Exceed Accel, which terminates Overdrive.
        /// </summary>
        public bool TryTriggerExceedAccel(BBAttackData exceedAccelMove)
        {
            if (brain == null || !brain.gauges.isOverdriveActive || exceedAccelMove == null) return false;

            // End Overdrive immediately
            brain.gauges.isOverdriveActive = false;
            brain.gauges.overdriveTicksRemaining = 0;

            // Start Exceed Accel move
            brain.StartAttack(exceedAccelMove);
            return true;
        }
    }
}
