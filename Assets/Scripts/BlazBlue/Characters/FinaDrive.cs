using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C12 Fina's unique Drive mechanic: "Plasma Charge".
    /// Footsies / All-Rounder archetype (balanced reach, best 5B and 2B pokes in the cast).
    /// Holding D charges plasma energy across 3 tiers:
    /// - Level 1 (20 ticks): Standard Neo-Pink Plasma Burst.
    /// - Level 2 (45 ticks): Accelerated burst with +25% damage.
    /// - Level 3 (75 ticks): Heavy 2-hit, projectile-beating plasma beam.
    /// Releasing D fires the projectile (5D straight, 2D low ground shot, j.D air diagonal).
    /// Pressing A, B, or C during charge *stores* the charge level, empowering her next special move with +1 extra hit.
    /// In Overdrive ("OD Plasma"): All plasma charges come out instantly at Level 3.
    /// </summary>
    public class FinaDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Plasma Charge";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int LEVEL_1_TICKS = 20;
        public const int LEVEL_2_TICKS = 45;
        public const int LEVEL_3_TICKS = 75;

        [Header("Charge State")]
        public bool isCharging = false;
        public int chargeHoldTicks = 0;
        [SerializeField] private int _storedLevel = 0;

        public int StoredLevel => _storedLevel;
        public int CurrentChargeLevel
        {
            get
            {
                if (InOverdrive) return 3;
                if (chargeHoldTicks >= LEVEL_3_TICKS) return 3;
                if (chargeHoldTicks >= LEVEL_2_TICKS) return 2;
                if (chargeHoldTicks >= LEVEL_1_TICKS) return 1;
                return 0;
            }
        }

        public int CurrentResource => InOverdrive ? 3 : (_storedLevel > 0 ? _storedLevel : CurrentChargeLevel);
        public int MaxResource => 3;
        public bool InOverdrive { get; set; } = false;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            isCharging = false;
            chargeHoldTicks = 0;
            _storedLevel = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Charge is interrupted, but stored charge remains intact (spec §8.7)
            isCharging = false;
            chargeHoldTicks = 0;
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            // In air: j.D Air Plasma Shot
            if (!brain.isGrounded)
            {
                var airMove = brain.FindAttackByMoveId("j.D");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // Ground 6D: Overhead hop mixup (spec §8.7)
            if (facingNumpad == 6 || facingNumpad == 9)
            {
                var hopMove = brain.FindAttackByMoveId("6D");
                if (hopMove != null && brain.CanExecuteMove(hopMove))
                {
                    brain.StartAttack(hopMove);
                    return true;
                }
                return false;
            }

            // Ground 2D: Low Plasma Shot
            if (facingNumpad == 2 || facingNumpad == 1 || facingNumpad == 3)
            {
                var lowMove = brain.FindAttackByMoveId("2D");
                if (lowMove != null && brain.CanExecuteMove(lowMove))
                {
                    brain.StartAttack(lowMove);
                    return true;
                }
                return false;
            }

            // Ground 5D: Plasma Shot (or start charging)
            {
                var plasmaMove = brain.FindAttackByMoveId("5D");
                if (plasmaMove != null && brain.CanExecuteMove(plasmaMove))
                {
                    StartCharging();
                    brain.StartAttack(plasmaMove);
                    return true;
                }
            }

            return false;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            if (isCharging)
            {
                chargeHoldTicks++;
            }
        }

        public void StartCharging()
        {
            isCharging = true;
            chargeHoldTicks = 0;
        }

        public int ReleaseCharge()
        {
            int lvl = CurrentChargeLevel;
            isCharging = false;
            chargeHoldTicks = 0;
            return lvl > 0 ? lvl : 1;
        }

        public bool StoreCharge()
        {
            int lvl = CurrentChargeLevel;
            if (lvl > 0 || InOverdrive)
            {
                _storedLevel = InOverdrive ? 3 : lvl;
                isCharging = false;
                chargeHoldTicks = 0;
                return true;
            }
            return false;
        }

        public bool TryConsumeStoredCharge(out int level)
        {
            if (InOverdrive)
            {
                level = 3;
                return true;
            }
            if (_storedLevel > 0)
            {
                level = _storedLevel;
                _storedLevel = 0;
                return true;
            }
            level = 0;
            return false;
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Handled via projectile logic and special consumption
        }
    }
}
