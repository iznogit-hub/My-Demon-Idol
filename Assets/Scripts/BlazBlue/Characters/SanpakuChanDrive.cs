using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C08 SanpakuChan's unique Drive mechanic: "Crow Step".
    /// Speed and mixup ninja archetype (fastest overhead 6B in 16 ticks, low damage scaling x0.85).
    /// Teleports with feathered spatial phasing:
    /// - 5D: Teleports behind the opponent (14 ticks, invisible 4-10, Strike-invulnerable 4-8).
    /// - 4D: Back-teleport escape.
    /// - 2D: Low dash-slide teleport underneath high attacks.
    /// - j.D: Mid-air spatial reposition.
    /// Manages 3 Feather stocks (regens 1 feather every 150 ticks).
    /// In Overdrive ("OD Crow"): Crow Steps cost 0 feathers and startup 6 ticks faster.
    /// </summary>
    public class SanpakuChanDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Crow Step";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int MAX_FEATHERS = 3;
        public const int REGEN_TICKS = 150;

        [Header("Feather Stocks")]
        [SerializeField] private int _feathers = MAX_FEATHERS;
        [SerializeField] private int _regenTimer = 0;

        public int CurrentResource => InOverdrive ? MAX_FEATHERS : _feathers;
        public int MaxResource => MAX_FEATHERS;
        public bool InOverdrive { get; set; } = false;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            _feathers = MAX_FEATHERS;
            _regenTimer = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Feathers do not deplete on knockdown
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            // Check feather cost (free in Overdrive)
            if (!InOverdrive && _feathers <= 0) return false;

            // In air: j.D Air Teleport
            if (!brain.isGrounded)
            {
                var airMove = brain.FindAttackByMoveId("j.D");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    ConsumeFeather();
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // Ground 4D: Back-teleport
            if (facingNumpad == 4 || facingNumpad == 7 || facingNumpad == 1)
            {
                var backMove = brain.FindAttackByMoveId("4D");
                if (backMove != null && brain.CanExecuteMove(backMove))
                {
                    ConsumeFeather();
                    brain.StartAttack(backMove);
                    return true;
                }
                return false;
            }

            // Ground 2D: Low dash slide teleport
            if (facingNumpad == 2 || facingNumpad == 3)
            {
                var lowMove = brain.FindAttackByMoveId("2D");
                if (lowMove != null && brain.CanExecuteMove(lowMove))
                {
                    ConsumeFeather();
                    brain.StartAttack(lowMove);
                    return true;
                }
                return false;
            }

            // Ground 5D: Forward Cross-up Teleport (default)
            {
                var crossMove = brain.FindAttackByMoveId("5D");
                if (crossMove != null && brain.CanExecuteMove(crossMove))
                {
                    ConsumeFeather();
                    brain.StartAttack(crossMove);
                    return true;
                }
            }

            return false;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            if (_feathers < MAX_FEATHERS)
            {
                _regenTimer++;
                if (_regenTimer >= REGEN_TICKS)
                {
                    _regenTimer = 0;
                    _feathers = Mathf.Min(MAX_FEATHERS, _feathers + 1);
                }
            }
            else
            {
                _regenTimer = 0;
            }
        }

        public void ConsumeFeather(int count = 1)
        {
            if (InOverdrive) return;
            _feathers = Mathf.Max(0, _feathers - count);
        }

        public void AddFeathers(int count = 1)
        {
            _feathers = Mathf.Clamp(_feathers + count, 0, MAX_FEATHERS);
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Spatial reposition teleports don't deal direct strike damage unless canceling into normals
        }
    }
}
