using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Implements Tekken grounded Okizeme choices:
    /// Tech Roll, Ground Low Kick (High-Crush), Ground Mid Kick, or Stand Up Back.
    /// </summary>
    public class OkizemeWakeup
    {
        public const int GROUNDED_REST_TICKS = 24; // Time defender stays grounded before forced wakeup
        public const int TECH_ROLL_INVINCIBLE_TICKS = 14;
        public const int STAND_UP_SAFE_TICKS = 12;

        public bool IsGrounded { get; private set; } = false;
        private int _groundedTicksRemaining = 0;

        public event Action<WakeupAction> OnWakeupExecuted;

        public void EnterGroundedState()
        {
            IsGrounded = true;
            _groundedTicksRemaining = GROUNDED_REST_TICKS;
        }

        public void TickGrounded(int currentTick, out bool mustForceStand)
        {
            mustForceStand = false;
            if (!IsGrounded) return;

            if (_groundedTicksRemaining > 0)
            {
                _groundedTicksRemaining--;
                if (_groundedTicksRemaining <= 0)
                {
                    mustForceStand = true;
                    IsGrounded = false;
                }
            }
        }

        public bool TryExecuteWakeupAction(WakeupAction action, out int invincibilityTicks)
        {
            invincibilityTicks = 0;
            if (!IsGrounded) return false;

            IsGrounded = false;
            _groundedTicksRemaining = 0;

            switch (action)
            {
                case WakeupAction.QuickTechRoll:
                case WakeupAction.SidestepTechRollUp:
                case WakeupAction.SidestepTechRollDown:
                    invincibilityTicks = TECH_ROLL_INVINCIBLE_TICKS;
                    break;
                case WakeupAction.StandUpBack:
                    invincibilityTicks = STAND_UP_SAFE_TICKS;
                    break;
                case WakeupAction.WakeupLowKick:
                case WakeupAction.WakeupMidKick:
                    invincibilityTicks = 0; // Wakeup kicks have no invulnerability - vulnerable to meaty counter-hits
                    break;
            }

            OnWakeupExecuted?.Invoke(action);
            return true;
        }

        public void Reset()
        {
            IsGrounded = false;
            _groundedTicksRemaining = 0;
        }
    }
}
