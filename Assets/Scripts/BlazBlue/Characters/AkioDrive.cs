using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C01 Akio's unique Drive mechanic: "Iron Rush".
    /// D button executes a 3-step rush-punch chain: 5D(1) -> 5D(2) -> 5D(3), 2D (Low), 6D (Step).
    /// Each D hit or block banks 1 Momentum stock (max 3).
    /// Spending Momentum empowers 236C (+1 hit per pip) and 632146D (+300 damage per pip).
    /// Stocks reset to 0 upon knockdown.
    /// In Overdrive: infinite Momentum (always 3), chain gets a 4th hit (Iron Finisher),
    /// and D hits are +2 on block.
    /// </summary>
    public class AkioDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Iron Rush";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        [Header("Momentum Stocks")]
        [SerializeField] private int _momentum = 0;
        public const int MAX_MOMENTUM = 3;

        public int CurrentResource => InOverdrive ? MAX_MOMENTUM : _momentum;
        public int MaxResource => MAX_MOMENTUM;
        public bool InOverdrive { get; set; } = false;

        [Header("Drive State")]
        public int rushChainStep = 0; // 0 = none, 1 = 5D_1, 2 = 5D_2, 3 = 5D_3, 4 = 5D_4 (OD only)
        public int lastDriveTick = -1;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            _momentum = 0;
            rushChainStep = 0;
            lastDriveTick = -1;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Spec §8.3: Stocks drop to 0 on knockdown
            _momentum = 0;
            rushChainStep = 0;
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            // In air: j.D Dive Punch (or fallback to j.B)
            if (!brain.isGrounded)
            {
                var airMove = brain.FindAttackByMoveId("j.D") ?? brain.FindAttackByMoveId("j.B");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // Ground D moves
            BBAttackData targetMove = null;
            if (facingNumpad == 1 || facingNumpad == 2 || facingNumpad == 3)
            {
                // 2D Low Rush (falls back to 5D if not mapped)
                targetMove = brain.FindAttackByMoveId("2D");
            }
            else if (facingNumpad == 6 || facingNumpad == 9)
            {
                // 6D Rush Step (falls back to 5D if not mapped)
                targetMove = brain.FindAttackByMoveId("6D");
            }
            else
            {
                // 5D Rush chain: determines which step
                if (rushChainStep == 0)
                {
                    targetMove = brain.FindAttackByMoveId("5D");
                }
                else if (rushChainStep == 1)
                {
                    targetMove = brain.FindAttackByMoveId("5D_2");
                }
                else if (rushChainStep == 2)
                {
                    targetMove = brain.FindAttackByMoveId("5D_3");
                }
                else if (rushChainStep == 3 && InOverdrive)
                {
                    // 4th hit in Overdrive
                    targetMove = brain.FindAttackByMoveId("5D_4");
                }
            }

            // Graceful fallback: If follow-up/directional D move is not mapped, execute primary 5D
            if (targetMove == null)
            {
                targetMove = brain.FindAttackByMoveId("5D");
            }

            if (targetMove != null && brain.CanExecuteMove(targetMove))
            {
                brain.StartAttack(targetMove);
                AdvanceRushStep(targetMove.moveId);
                lastDriveTick = tick;
                return true;
            }

            return false;
        }

        private void AdvanceRushStep(string moveId)
        {
            switch (moveId)
            {
                case "5D":
                    rushChainStep = 1;
                    break;
                case "5D_2":
                    rushChainStep = 2;
                    break;
                case "5D_3":
                    rushChainStep = 3;
                    break;
                case "5D_4":
                default:
                    rushChainStep = 0;
                    break;
            }
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            // Reset rush chain step when returning to neutral
            if (brain != null && brain.bbState == BBState.Neutral && rushChainStep > 0)
            {
                if (tick - lastDriveTick > 30)
                {
                    rushChainStep = 0;
                }
            }
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            if (move == null) return;

            // Bank 1 Momentum on hit or block of any Drive move
            if (move.button == BBButton.D || move.kind == BBMoveKind.DriveMove)
            {
                if (!InOverdrive)
                {
                    _momentum = Mathf.Min(MAX_MOMENTUM, _momentum + 1);
                }
            }
        }

        /// <summary>
        /// Spends all accumulated Momentum stocks. Returns count of stocks spent (0 to 3).
        /// If in Overdrive, returns 3 without decrementing.
        /// </summary>
        public int SpendAllMomentum()
        {
            if (InOverdrive)
            {
                return MAX_MOMENTUM;
            }

            int spent = _momentum;
            _momentum = 0;
            return spent;
        }

        /// <summary>
        /// Set momentum directly (for testing and trials).
        /// </summary>
        public void SetMomentum(int count)
        {
            _momentum = Mathf.Clamp(count, 0, MAX_MOMENTUM);
        }
    }
}
