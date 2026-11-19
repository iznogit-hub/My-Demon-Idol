using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C09 PicoChan's unique Drive mechanic: "Clinch".
    /// Short grappler archetype (hips 0.76m, forearm 0.21m).
    /// Drive moves (5D, 2D, 6D, j.D) are unblockable grabs.
    /// 5D: Short unblockable grab (≈0.9m). Tech window 6 ticks.
    /// 2D: Low grab that only catches crouching or low-posture targets.
    /// 6D: Running grab (forward dash into grab, travels 2.0m).
    /// j.D: Air grab.
    /// Landing any D grab banks 1 Grip stock (max 2).
    /// While Grip >= 1: 360C command throw deals +20% damage (2160 dmg) and cannot be teched,
    /// consuming 1 Grip stock. Pico Press (632146C DD) gains +0.4m grab range.
    /// In Overdrive ("OD Clinch"): D grabs gain +0.4m range and armor on ticks 1–6.
    /// </summary>
    public class PicoChanDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Clinch";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int MAX_GRIP = 2;

        [Header("Grip Stocks")]
        [SerializeField] private int _gripStocks = 0;

        public int CurrentResource => InOverdrive ? MAX_GRIP : _gripStocks;
        public int MaxResource => MAX_GRIP;
        public bool InOverdrive { get; set; } = false;

        public bool HasGrip => InOverdrive || _gripStocks >= 1;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            _gripStocks = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Grip stocks are preserved across normal knockdowns (spec §8.6)
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            // Airborne: j.D Air Clinch
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

            // Ground: 6D Running Clinch
            if (facingNumpad == 6 || facingNumpad == 9)
            {
                var runMove = brain.FindAttackByMoveId("6D");
                if (runMove != null && brain.CanExecuteMove(runMove))
                {
                    brain.StartAttack(runMove);
                    return true;
                }
                return false;
            }

            // Ground: 2D Low Clinch
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

            // Ground: 5D Clinch (default)
            {
                var clinchMove = brain.FindAttackByMoveId("5D");
                if (clinchMove != null && brain.CanExecuteMove(clinchMove))
                {
                    brain.StartAttack(clinchMove);
                    return true;
                }
            }

            return false;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            // Grip is event-based on grab hits
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            if (move == null) return;

            // Any D grab connecting banks 1 Grip
            if (!isBlocked && (move.moveId == "5D" || move.moveId == "2D" || move.moveId == "6D" || move.moveId == "j.D"))
            {
                BankGrip(1);
            }
        }

        public void BankGrip(int count = 1)
        {
            _gripStocks = Mathf.Clamp(_gripStocks + count, 0, MAX_GRIP);
        }

        public bool TryConsumeGrip(int count = 1)
        {
            if (InOverdrive) return true; // OD provides infinite Grip enhancement
            if (_gripStocks >= count)
            {
                _gripStocks -= count;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns extra grab range bonus (e.g. +0.4m during Overdrive or for 632146C with Grip).
        /// </summary>
        public float GetGrabRangeBonus(string moveId)
        {
            float bonus = 0f;
            if (InOverdrive && (moveId == "5D" || moveId == "2D" || moveId == "6D" || moveId == "j.D"))
            {
                bonus += 0.4f;
            }
            if (moveId == "632146C" && HasGrip)
            {
                bonus += 0.4f;
            }
            return bonus;
        }

        /// <summary>
        /// Returns whether armor is active on this tick (OD D grabs gain armor on ticks 1–6).
        /// </summary>
        public bool HasArmor(int tickInMove, string moveId)
        {
            if (InOverdrive && tickInMove >= 1 && tickInMove <= 6)
            {
                if (moveId == "5D" || moveId == "2D" || moveId == "6D" || moveId == "j.D")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
