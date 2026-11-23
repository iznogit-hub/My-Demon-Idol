using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C06 Cultist's unique Drive mechanic: "Void Orb".
    /// Setplay and summoner archetype:
    /// - 5D: Places a Void Skull Orb in front of her (max 2 active on screen, 3 in Overdrive).
    /// - 6D: Commands placed orbs to dash forward toward the opponent.
    /// - 2D: Commands placed orbs to drop downwards to trap low space.
    /// - 4D: Recalls active orbs back toward Cultist.
    /// - j.D: Mid-air placement of Void Skull Orb.
    /// - 22B / 22C: Teleports Cultist next to Orb 1 or Orb 2.
    /// - 236236C (Void Cataclysm): Detonates all active orbs into dark energy explosions.
    /// In Overdrive ("OD Void"): Max 3 orbs, and orbs automatically fire skull projectiles.
    /// Zero-allocation, fixed struct array.
    /// </summary>
    public class CultistDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Void Orb";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int MAX_ORBS_DEFAULT = 2;
        public const int MAX_ORBS_OD = 3;
        public const int ORB_MAX_LIFETIME_TICKS = 600; // 10 seconds
        public const int OD_AUTOFIRE_INTERVAL = 60; // 1 second

        [Serializable]
        public struct VoidOrbState
        {
            public bool active;
            public Vector2 position;
            public Vector2 velocity;
            public int lifetimeTicks;
            public int health; // 1 hit hurtbox
        }

        [Header("Void Orbs")]
        [SerializeField] private VoidOrbState[] _orbs = new VoidOrbState[MAX_ORBS_OD];
        [SerializeField] private int _activeOrbCount = 0;
        [SerializeField] private int _autoFireTimer = 0;

        public int CurrentResource => _activeOrbCount;
        public int MaxResource => InOverdrive ? MAX_ORBS_OD : MAX_ORBS_DEFAULT;
        public bool InOverdrive { get; set; } = false;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                _orbs[i].active = false;
                _orbs[i].position = Vector2.zero;
                _orbs[i].velocity = Vector2.zero;
                _orbs[i].lifetimeTicks = 0;
                _orbs[i].health = 0;
            }
            _activeOrbCount = 0;
            _autoFireTimer = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Orbs remain on screen through knockdown for setplay traps
        }

        private Vector2 GetBrainPos(BBFighterBrain brain) => brain != null ? (Vector2)brain.transform.position : Vector2.zero;
        private int GetBrainFacing(BBFighterBrain brain) => (brain != null && brain.fighter != null) ? brain.fighter.FacingDirection : 1;

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            Vector2 bPos = GetBrainPos(brain);
            int facing = GetBrainFacing(brain);

            // In air: j.D Air Orb Placement (or fallback to j.B)
            if (!brain.isGrounded)
            {
                var airMove = brain.FindAttackByMoveId("j.D") ?? brain.FindAttackByMoveId("j.B");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    Vector2 spawnPos = bPos + new Vector2(facing * 1.0f, 0.8f);
                    PlaceOrb(spawnPos);
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // 6D: Command orbs to dash forward
            if (facingNumpad == 6 || facingNumpad == 9 || facingNumpad == 3)
            {
                var dashCmd = brain.FindAttackByMoveId("6D") ?? brain.FindAttackByMoveId("5D");
                if (dashCmd != null && brain.CanExecuteMove(dashCmd))
                {
                    CommandOrbsDash(facing);
                    brain.StartAttack(dashCmd);
                    return true;
                }
            }

            // 2D: Command orbs to drop low
            if (facingNumpad == 2 || facingNumpad == 1)
            {
                var dropCmd = brain.FindAttackByMoveId("2D") ?? brain.FindAttackByMoveId("5D");
                if (dropCmd != null && brain.CanExecuteMove(dropCmd))
                {
                    CommandOrbsDrop();
                    brain.StartAttack(dropCmd);
                    return true;
                }
            }

            // 4D: Command orbs to recall
            if (facingNumpad == 4 || facingNumpad == 7)
            {
                var recallCmd = brain.FindAttackByMoveId("4D") ?? brain.FindAttackByMoveId("5D");
                if (recallCmd != null && brain.CanExecuteMove(recallCmd))
                {
                    CommandOrbsRecall(bPos);
                    brain.StartAttack(recallCmd);
                    return true;
                }
            }

            // 5D: Place Void Orb in front of Cultist (default)
            {
                var placeMove = brain.FindAttackByMoveId("5D");
                if (placeMove != null && brain.CanExecuteMove(placeMove))
                {
                    Vector2 spawnPos = bPos + new Vector2(facing * 1.2f, 1.1f);
                    PlaceOrb(spawnPos);
                    brain.StartAttack(placeMove);
                    return true;
                }
            }

            // Graceful fallback: If follow-up/directional D move is not mapped, execute primary 5D
            var fallbackMove = brain.FindAttackByMoveId("5D");
            if (fallbackMove != null && brain.CanExecuteMove(fallbackMove))
            {
                brain.StartAttack(fallbackMove);
                return true;
            }

            return false;
        }

        public void PlaceOrb(Vector2 spawnPosition)
        {
            int maxAllowed = MaxResource;

            // Find an inactive slot
            int targetIndex = -1;
            for (int i = 0; i < maxAllowed; i++)
            {
                if (!_orbs[i].active)
                {
                    targetIndex = i;
                    break;
                }
            }

            // If all slots are full, overwrite the oldest orb
            if (targetIndex == -1)
            {
                int maxLifetime = -1;
                for (int i = 0; i < maxAllowed; i++)
                {
                    if (_orbs[i].lifetimeTicks > maxLifetime)
                    {
                        maxLifetime = _orbs[i].lifetimeTicks;
                        targetIndex = i;
                    }
                }
            }

            if (targetIndex >= 0 && targetIndex < _orbs.Length)
            {
                _orbs[targetIndex].active = true;
                _orbs[targetIndex].position = spawnPosition;
                _orbs[targetIndex].velocity = Vector2.zero;
                _orbs[targetIndex].lifetimeTicks = 0;
                _orbs[targetIndex].health = 1;
                RecountActiveOrbs();
            }
        }

        public void CommandOrbsDash(int facing)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].active)
                {
                    _orbs[i].velocity = new Vector2(facing * 8.0f, 0f);
                }
            }
        }

        public void CommandOrbsDrop()
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].active)
                {
                    _orbs[i].velocity = new Vector2(0f, -8.0f);
                }
            }
        }

        public void CommandOrbsRecall(Vector2 cultistPos)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].active)
                {
                    Vector2 dir = (cultistPos - _orbs[i].position).normalized;
                    _orbs[i].velocity = dir * 8.0f;
                }
            }
        }

        public bool TryGetOrbPosition(int index, out Vector2 pos)
        {
            pos = Vector2.zero;
            if (index < 0 || index >= _orbs.Length) return false;
            if (!_orbs[index].active) return false;
            pos = _orbs[index].position;
            return true;
        }

        public int DetonateAllOrbs()
        {
            int count = 0;
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].active)
                {
                    _orbs[i].active = false;
                    _orbs[i].velocity = Vector2.zero;
                    count++;
                }
            }
            RecountActiveOrbs();
            return count;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            float dt = 1.0f / 60.0f;
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].active)
                {
                    _orbs[i].lifetimeTicks++;
                    _orbs[i].position += _orbs[i].velocity * dt;

                    // Floor boundary
                    if (_orbs[i].position.y < 0.2f)
                    {
                        _orbs[i].position = new Vector2(_orbs[i].position.x, 0.2f);
                        _orbs[i].velocity = new Vector2(_orbs[i].velocity.x * 0.5f, 0f);
                    }

                    // Expire if lifetime exceeded
                    if (_orbs[i].lifetimeTicks >= ORB_MAX_LIFETIME_TICKS)
                    {
                        _orbs[i].active = false;
                    }
                }
            }

            RecountActiveOrbs();

            // Overdrive auto-firing
            if (InOverdrive && _activeOrbCount > 0)
            {
                _autoFireTimer++;
                if (_autoFireTimer >= OD_AUTOFIRE_INTERVAL)
                {
                    _autoFireTimer = 0;
                    // Auto-fire projectile pulse from active orbs
                }
            }
            else
            {
                _autoFireTimer = 0;
            }
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Void Orbs generate setplay pressure on hit/block
        }

        private void RecountActiveOrbs()
        {
            int count = 0;
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i].active) count++;
            }
            _activeOrbCount = count;
        }
    }
}
