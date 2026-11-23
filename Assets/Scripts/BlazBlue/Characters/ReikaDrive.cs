using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C10 Reika's unique Drive mechanic: "Veil".
    /// Counter and stance archetype:
    /// - 5D: Standing Veil Counter Stance. Guard Point (Head | Body) on ticks 4–14.
    /// - 2D: Crouching Veil Counter Stance. Guard Point (Foot / Low) on ticks 4–14.
    /// - j.D: Aerial Veil Counter Stance. Guard Point (Head | Body) on ticks 4–14.
    /// - On Strike Absorption: Immediately auto-counters with a 1200-damage Strike-invulnerable punish ("Veil Riposte").
    /// - Whiffed Veil: 22 ticks recovery.
    /// - In Overdrive ("OD Veil"): Guard Point active window expands to ticks 2–20.
    /// </summary>
    public class ReikaDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Veil";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int DEFAULT_GUARD_START = 4;
        public const int DEFAULT_GUARD_END = 14;
        public const int OD_GUARD_START = 2;
        public const int OD_GUARD_END = 20;

        [Header("Veil State")]
        [SerializeField] private bool _inCounterStance = false;
        [SerializeField] private int _stanceTick = 0;
        [SerializeField] private BBGuard _guardedZone = BBGuard.Mid;
        [SerializeField] private int _successfulCounters = 0;

        public int CurrentResource => _successfulCounters;
        public int MaxResource => 99;
        public bool InOverdrive { get; set; } = false;

        public bool InCounterStance => _inCounterStance;
        public int GuardStartTick => InOverdrive ? OD_GUARD_START : DEFAULT_GUARD_START;
        public int GuardEndTick => InOverdrive ? OD_GUARD_END : DEFAULT_GUARD_END;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            _inCounterStance = false;
            _stanceTick = 0;
            _guardedZone = BBGuard.Mid;
            _successfulCounters = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            _inCounterStance = false;
            _stanceTick = 0;
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            // In air: j.D Air Veil Counter (or fallback to j.B)
            if (!brain.isGrounded)
            {
                var airMove = brain.FindAttackByMoveId("j.D") ?? brain.FindAttackByMoveId("j.B");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    StartStance(BBGuard.Mid);
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // 2D: Low Veil Counter
            if (facingNumpad == 2 || facingNumpad == 1 || facingNumpad == 3)
            {
                var lowMove = brain.FindAttackByMoveId("2D") ?? brain.FindAttackByMoveId("5D");
                if (lowMove != null && brain.CanExecuteMove(lowMove))
                {
                    StartStance(BBGuard.Low);
                    brain.StartAttack(lowMove);
                    return true;
                }
            }

            // 5D: Standing Veil Counter (default)
            {
                var standMove = brain.FindAttackByMoveId("5D");
                if (standMove != null && brain.CanExecuteMove(standMove))
                {
                    StartStance(BBGuard.Mid);
                    brain.StartAttack(standMove);
                    return true;
                }
            }

            // Graceful fallback: If follow-up/directional D move is not mapped, execute primary 5D
            var fallbackMove = brain.FindAttackByMoveId("5D");
            if (fallbackMove != null && brain.CanExecuteMove(fallbackMove))
            {
                StartStance(BBGuard.Mid);
                brain.StartAttack(fallbackMove);
                return true;
            }

            return false;
        }

        public void StartStance(BBGuard zone)
        {
            _inCounterStance = true;
            _stanceTick = 0;
            _guardedZone = zone;
        }

        public void EndStance()
        {
            _inCounterStance = false;
            _stanceTick = 0;
        }

        public bool CheckGuardPointActive(int tickInMove, BBGuard incomingGuard)
        {
            if (!_inCounterStance) return false;
            if (tickInMove < GuardStartTick || tickInMove > GuardEndTick) return false;

            // 2D counters Low
            if (_guardedZone == BBGuard.Low)
            {
                return incomingGuard == BBGuard.Low;
            }

            // 5D and j.D counter Mid and Overhead (Head & Body)
            return incomingGuard == BBGuard.Mid || incomingGuard == BBGuard.Overhead;
        }

        public bool TriggerRiposte(BBFighterBrain brain)
        {
            if (!_inCounterStance) return false;

            _successfulCounters++;
            EndStance();

            if (brain != null)
            {
                var riposteMove = brain.FindAttackByMoveId("VeilRiposte");
                if (riposteMove != null)
                {
                    brain.StartAttack(riposteMove);
                    return true;
                }
            }
            return true;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            if (_inCounterStance)
            {
                _stanceTick++;
                int endTick = GuardEndTick + 22; // Active window + 22 ticks whiff recovery
                if (_stanceTick > endTick)
                {
                    EndStance();
                }
            }
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Riposte hits deal lethal strike-invulnerable damage
        }
    }
}
