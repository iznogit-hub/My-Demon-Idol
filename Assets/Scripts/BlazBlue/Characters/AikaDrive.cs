using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C02 Aika's unique Drive mechanic: "Resonance Nail".
    /// Controls space with occult nails and long karate kicks.
    /// Manages 5 Nail stocks, regaining 1 stock every 120 ticks (2 seconds at 60Hz).
    /// 5D: Straight nail projectile.
    /// 2D: Ground slide nail (hits low).
    /// j.D: Air nail (45° downwards, hits overhead).
    /// 6D: Detonate all active nails on screen (400 damage per nail).
    /// In Overdrive ("OD Resonance"): Nails cost 0 stocks, and each nail throw splits into two nails.
    /// </summary>
    public class AikaDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Resonance Nail";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int MAX_NAILS = 5;
        public const int REGEN_TICKS = 120;

        [Header("Nail Resource")]
        [SerializeField] private int _nailStocks = MAX_NAILS;
        [SerializeField] private int _regenTimer = 0;

        public int CurrentResource => InOverdrive ? MAX_NAILS : _nailStocks;
        public int MaxResource => MAX_NAILS;
        public bool InOverdrive { get; set; } = false;

        public struct ActiveNail
        {
            public Vector2 position;
            public Vector2 velocity;
            public int spawnTick;
            public int lifeTicks;
            public bool isGrounded;
        }

        [Header("Active Nails on Screen")]
        private readonly List<ActiveNail> _activeNails = new List<ActiveNail>(8);
        public int ActiveNailCount => _activeNails.Count;

        [Header("Feint / Delay")]
        public int holdDelayTicks = 0;
        public const int MAX_HOLD_DELAY = 20;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            _nailStocks = MAX_NAILS;
            _regenTimer = 0;
            _activeNails.Clear();
            holdDelayTicks = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Ammunition is preserved across knockdowns, but active nails clear on knockdown
            _activeNails.Clear();
            holdDelayTicks = 0;
        }

        public bool CanThrowNail()
        {
            return InOverdrive || _nailStocks > 0;
        }

        public bool SpendNail()
        {
            if (InOverdrive) return true;
            if (_nailStocks > 0)
            {
                _nailStocks--;
                return true;
            }
            return false;
        }

        public void AddNailStock(int count = 1)
        {
            _nailStocks = Mathf.Clamp(_nailStocks + count, 0, MAX_NAILS);
        }

        private Vector2 GetBrainPos(BBFighterBrain brain) => brain != null ? (Vector2)brain.transform.position : Vector2.zero;
        private int GetBrainFacing(BBFighterBrain brain) => (brain != null && brain.fighter != null) ? brain.fighter.FacingDirection : 1;

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            Vector2 bPos = GetBrainPos(brain);
            int facing = GetBrainFacing(brain);

            // In air: j.D Air Nail
            if (!brain.isGrounded)
            {
                if (!CanThrowNail()) return false;
                var airMove = brain.FindAttackByMoveId("j.D");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    SpendNail();
                    SpawnNail(bPos + new Vector2(facing * 0.4f, 1.2f),
                              new Vector2(facing * 9.0f, -9.0f), tick);
                    if (InOverdrive)
                    {
                        // Split nail
                        SpawnNail(bPos + new Vector2(facing * 0.4f, 1.4f),
                                  new Vector2(facing * 11.0f, -7.0f), tick);
                    }
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // 6D Detonate (does not cost stock)
            if (facingNumpad == 6 || facingNumpad == 9)
            {
                var detonateMove = brain.FindAttackByMoveId("6D");
                if (detonateMove != null && brain.CanExecuteMove(detonateMove))
                {
                    DetonateAllNails(brain, tick);
                    brain.StartAttack(detonateMove);
                    return true;
                }
                return false;
            }

            // 2D Ground Nail
            if (facingNumpad == 1 || facingNumpad == 2 || facingNumpad == 3)
            {
                if (!CanThrowNail()) return false;
                var groundMove = brain.FindAttackByMoveId("2D");
                if (groundMove != null && brain.CanExecuteMove(groundMove))
                {
                    SpendNail();
                    SpawnNail(bPos + new Vector2(facing * 0.5f, 0.2f),
                              new Vector2(facing * 11.0f, 0f), tick);
                    if (InOverdrive)
                    {
                        SpawnNail(bPos + new Vector2(facing * 0.5f, 0.35f),
                                  new Vector2(facing * 13.0f, 0f), tick);
                    }
                    brain.StartAttack(groundMove);
                    return true;
                }
                return false;
            }

            // 5D Straight Nail
            {
                if (!CanThrowNail()) return false;
                var straightMove = brain.FindAttackByMoveId("5D");
                if (straightMove != null && brain.CanExecuteMove(straightMove))
                {
                    SpendNail();
                    SpawnNail(bPos + new Vector2(facing * 0.6f, 0.95f),
                              new Vector2(facing * 13.0f, 0f), tick);
                    if (InOverdrive)
                    {
                        SpawnNail(bPos + new Vector2(facing * 0.6f, 1.15f),
                                  new Vector2(facing * 14.5f, 0f), tick);
                    }
                    brain.StartAttack(straightMove);
                    return true;
                }
            }

            return false;
        }

        public void SpawnNail(Vector2 pos, Vector2 vel, int tick)
        {
            if (_activeNails.Count >= 8) _activeNails.RemoveAt(0);

            _activeNails.Add(new ActiveNail
            {
                position = pos,
                velocity = vel,
                spawnTick = tick,
                lifeTicks = 180, // Remains active for 3 seconds unless detonated
                isGrounded = Mathf.Abs(vel.y) < 0.001f
            });
        }

        public int DetonateAllNails(BBFighterBrain brain, int tick)
        {
            int detonatedCount = _activeNails.Count;
            _activeNails.Clear();
            return detonatedCount;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            // Nail regeneration: 1 nail every 120 ticks when below max and not in OD
            if (_nailStocks < MAX_NAILS && !InOverdrive)
            {
                _regenTimer++;
                if (_regenTimer >= REGEN_TICKS)
                {
                    _nailStocks++;
                    _regenTimer = 0;
                }
            }
            else
            {
                _regenTimer = 0;
            }

            // Update active nails
            for (int i = _activeNails.Count - 1; i >= 0; i--)
            {
                var nail = _activeNails[i];
                nail.lifeTicks--;
                if (nail.lifeTicks <= 0)
                {
                    _activeNails.RemoveAt(i);
                    continue;
                }

                nail.position += nail.velocity * (1f / 60f);
                if (nail.position.y <= 0f)
                {
                    nail.position.y = 0f;
                    nail.velocity = Vector2.zero;
                    nail.isGrounded = true;
                }

                _activeNails[i] = nail;
            }
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Detonate or nail hit confirmation hooks
        }
    }
}
