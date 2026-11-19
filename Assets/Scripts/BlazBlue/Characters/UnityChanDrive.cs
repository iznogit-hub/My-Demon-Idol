using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C04 UnityChan's unique Drive mechanic: "Aero Hop".
    /// Short reach aerial hopper who controls the vertical airspace.
    /// 5D: Vertical hop (airborne from tick 4, Foot-invulnerable 4-18, actionable from tick 10).
    /// 6D: Forward leap (crosses up crouching opponents at close range).
    /// 4D: Back hop (airborne from tick 3, Projectile-invulnerable 3-10, actionable from tick 12).
    /// j.D: Extra air hop that does not consume an air action (once per jump, fires Aero Disc).
    /// In Overdrive ("OD Aero"): Unlimited j.D air hops, and every hop (5D, 6D, 4D, j.D) fires an Aero Disc.
    /// </summary>
    public class UnityChanDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Aero Hop";
        public DriveResourceKind ResourceKind => DriveResourceKind.None;

        public int CurrentResource => 0;
        public int MaxResource => 0;
        public bool InOverdrive { get; set; } = false;

        [Header("Air Hop State")]
        public bool usedAirHop = false;
        public int activeHopsCount = 0;

        private Vector2 GetBrainPos(BBFighterBrain brain) => brain != null ? (Vector2)brain.transform.position : Vector2.zero;
        private int GetBrainFacing(BBFighterBrain brain) => (brain != null && brain.fighter != null) ? brain.fighter.FacingDirection : 1;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            usedAirHop = false;
            activeHopsCount = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            usedAirHop = false;
            activeHopsCount = 0;
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            Vector2 bPos = GetBrainPos(brain);
            int facing = GetBrainFacing(brain);

            // Airborne: j.D Extra Air Hop
            if (!brain.isGrounded)
            {
                if (usedAirHop && !InOverdrive) return false;

                var airMove = brain.FindAttackByMoveId("j.D");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    usedAirHop = true;
                    activeHopsCount++;
                    // Upward air boost
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // Ground: 6D Forward Leap
            if (facingNumpad == 6 || facingNumpad == 9)
            {
                var leapMove = brain.FindAttackByMoveId("6D");
                if (leapMove != null && brain.CanExecuteMove(leapMove))
                {
                    activeHopsCount++;
                    brain.StartAttack(leapMove);
                    return true;
                }
                return false;
            }

            // Ground: 4D Back Hop
            if (facingNumpad == 4 || facingNumpad == 7 || facingNumpad == 1)
            {
                var backHopMove = brain.FindAttackByMoveId("4D");
                if (backHopMove != null && brain.CanExecuteMove(backHopMove))
                {
                    activeHopsCount++;
                    brain.StartAttack(backHopMove);
                    return true;
                }
                return false;
            }

            // Ground: 5D Vertical Hop
            {
                var hopMove = brain.FindAttackByMoveId("5D");
                if (hopMove != null && brain.CanExecuteMove(hopMove))
                {
                    activeHopsCount++;
                    brain.StartAttack(hopMove);
                    return true;
                }
            }

            return false;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            // Reset air hop when grounded
            if (brain != null && brain.isGrounded)
            {
                usedAirHop = false;
            }
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Confirmation hooks
        }
    }
}
