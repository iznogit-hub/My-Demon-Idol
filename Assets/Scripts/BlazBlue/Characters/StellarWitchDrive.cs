using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// C07 StellarWitch's unique Drive mechanic: "Star Field".
    /// Fullscreen zoner archetype:
    /// - 5D: Places Star Point Alpha (mid-height at mid-range). If star points already exist, fires Starlight Comets from all active points.
    /// - 2D: Places Star Point Beta (low-height ground trap).
    /// - 6D: Places Star Point Gamma (high-height anti-air trap).
    /// - j.D: Places Star Point Delta in mid-air.
    /// - 4D / Comet Fire: Commands all active star points to fire Starlight Comets (speed 13, damage 480) toward the opponent.
    /// - 236C (Supernova): Detonates all active star points into massive cosmic explosions (1400 damage each).
    /// In Overdrive ("OD Star Field"): Max 4 star points; active star points auto-fire comets periodically every 60 ticks without being consumed!
    /// Zero-allocation, fixed struct array.
    /// </summary>
    public class StellarWitchDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => "Star Field";
        public DriveResourceKind ResourceKind => DriveResourceKind.Stocks;

        public const int MAX_STARS_DEFAULT = 3;
        public const int MAX_STARS_OD = 4;
        public const int STAR_MAX_LIFETIME_TICKS = 720; // 12 seconds
        public const int OD_AUTOFIRE_INTERVAL = 60; // 1 second
        public const int COMET_SPEED = 13;
        public const int COMET_DAMAGE_NORMAL = 480;
        public const int COMET_DAMAGE_OD = 650;

        public enum StarPointType
        {
            Alpha = 0, // Mid
            Beta = 1,  // Low
            Gamma = 2, // High
            Delta = 3  // Air
        }

        [Serializable]
        public struct StarPointState
        {
            public bool active;
            public Vector2 position;
            public int lifetimeTicks;
            public StarPointType type;
        }

        [Header("Star Field")]
        [SerializeField] private StarPointState[] _stars = new StarPointState[MAX_STARS_OD];
        [SerializeField] private int _activeStarCount = 0;
        [SerializeField] private int _autoFireTimer = 0;
        [SerializeField] private int _totalCometsFired = 0;

        public int CurrentResource => _activeStarCount;
        public int MaxResource => InOverdrive ? MAX_STARS_OD : MAX_STARS_DEFAULT;
        public bool InOverdrive { get; set; } = false;
        public int TotalCometsFired => _totalCometsFired;

        public void OnMatchStart(BBFighterBrain brain)
        {
            ResetResource();
        }

        public void ResetResource()
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].active = false;
                _stars[i].position = Vector2.zero;
                _stars[i].lifetimeTicks = 0;
                _stars[i].type = StarPointType.Alpha;
            }
            _activeStarCount = 0;
            _autoFireTimer = 0;
            _totalCometsFired = 0;
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            // Star points remain active through knockdown for fullscreen zoning control
        }

        private Vector2 GetBrainPos(BBFighterBrain brain) => brain != null ? (Vector2)brain.transform.position : Vector2.zero;
        private int GetBrainFacing(BBFighterBrain brain) => (brain != null && brain.fighter != null) ? brain.fighter.FacingDirection : 1;

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;

            Vector2 bPos = GetBrainPos(brain);
            int facing = GetBrainFacing(brain);

            // In air: j.D Air Star Point (or fallback to j.B)
            if (!brain.isGrounded)
            {
                var airMove = brain.FindAttackByMoveId("j.D") ?? brain.FindAttackByMoveId("j.B");
                if (airMove != null && brain.CanExecuteMove(airMove))
                {
                    Vector2 spawnPos = bPos + new Vector2(facing * 1.8f, 1.6f);
                    PlaceStarPoint(spawnPos, StarPointType.Delta);
                    brain.StartAttack(airMove);
                    return true;
                }
                return false;
            }

            // 4D: Direct Comet Fire command from all active stars
            if (facingNumpad == 4 || facingNumpad == 1 || facingNumpad == 7)
            {
                if (_activeStarCount > 0)
                {
                    var fireMove = brain.FindAttackByMoveId("CometFire") ?? brain.FindAttackByMoveId("5D");
                    if (fireMove != null && brain.CanExecuteMove(fireMove))
                    {
                        FireComets();
                        brain.StartAttack(fireMove);
                        return true;
                    }
                }
            }

            // 6D: Place Star Point Gamma (High / Anti-Air Trap)
            if (facingNumpad == 6 || facingNumpad == 9 || facingNumpad == 3)
            {
                var gammaMove = brain.FindAttackByMoveId("6D") ?? brain.FindAttackByMoveId("5D");
                if (gammaMove != null && brain.CanExecuteMove(gammaMove))
                {
                    Vector2 spawnPos = bPos + new Vector2(facing * 3.0f, 2.0f);
                    PlaceStarPoint(spawnPos, StarPointType.Gamma);
                    brain.StartAttack(gammaMove);
                    return true;
                }
            }

            // 2D: Place Star Point Beta (Low / Ground Trap)
            if (facingNumpad == 2)
            {
                var betaMove = brain.FindAttackByMoveId("2D") ?? brain.FindAttackByMoveId("5D");
                if (betaMove != null && brain.CanExecuteMove(betaMove))
                {
                    Vector2 spawnPos = bPos + new Vector2(facing * 1.4f, 0.2f);
                    PlaceStarPoint(spawnPos, StarPointType.Beta);
                    brain.StartAttack(betaMove);
                    return true;
                }
            }

            // 5D: Standing Drive
            // If active stars exist and are at max, fire comets; otherwise place Star Point Alpha (Mid)
            if (_activeStarCount >= MaxResource)
            {
                var fireMove = brain.FindAttackByMoveId("CometFire") ?? brain.FindAttackByMoveId("5D");
                if (fireMove != null && brain.CanExecuteMove(fireMove))
                {
                    FireComets();
                    brain.StartAttack(fireMove);
                    return true;
                }
            }
            else
            {
                var standMove = brain.FindAttackByMoveId("5D");
                if (standMove != null && brain.CanExecuteMove(standMove))
                {
                    Vector2 spawnPos = bPos + new Vector2(facing * 2.2f, 1.2f);
                    PlaceStarPoint(spawnPos, StarPointType.Alpha);
                    brain.StartAttack(standMove);
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

        public bool PlaceStarPoint(Vector2 worldPos, StarPointType type)
        {
            int max = MaxResource;

            // Find free slot
            int targetIdx = -1;
            for (int i = 0; i < max; i++)
            {
                if (!_stars[i].active)
                {
                    targetIdx = i;
                    break;
                }
            }

            // If full, replace oldest star point
            if (targetIdx == -1)
            {
                int oldestIdx = 0;
                int maxLifetime = -1;
                for (int i = 0; i < max; i++)
                {
                    if (_stars[i].lifetimeTicks > maxLifetime)
                    {
                        maxLifetime = _stars[i].lifetimeTicks;
                        oldestIdx = i;
                    }
                }
                targetIdx = oldestIdx;
            }

            _stars[targetIdx].active = true;
            _stars[targetIdx].position = worldPos;
            _stars[targetIdx].lifetimeTicks = 0;
            _stars[targetIdx].type = type;

            RecountActiveStars();
            return true;
        }

        public int FireComets()
        {
            if (_activeStarCount == 0) return 0;

            int fired = 0;
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i].active)
                {
                    fired++;
                    _totalCometsFired++;

                    // In non-OD, firing comets consumes the star point
                    if (!InOverdrive)
                    {
                        _stars[i].active = false;
                    }
                }
            }

            RecountActiveStars();
            return fired;
        }

        public int DetonateAllStars()
        {
            int detonated = _activeStarCount;
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].active = false;
            }
            _activeStarCount = 0;
            return detonated;
        }

        public void TickDrive(BBFighterBrain brain, int tick)
        {
            int max = MaxResource;

            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i].active)
                {
                    _stars[i].lifetimeTicks++;

                    if (_stars[i].lifetimeTicks >= STAR_MAX_LIFETIME_TICKS)
                    {
                        _stars[i].active = false;
                    }
                }
            }

            RecountActiveStars();

            // In Overdrive, active star points auto-fire comets periodically!
            if (InOverdrive && _activeStarCount > 0)
            {
                _autoFireTimer++;
                if (_autoFireTimer >= OD_AUTOFIRE_INTERVAL)
                {
                    _autoFireTimer = 0;
                    FireComets();
                }
            }
            else
            {
                _autoFireTimer = 0;
            }
        }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            // Fullscreen zoning pressure on hit/block
        }

        public bool IsStarActive(int index)
        {
            if (index < 0 || index >= _stars.Length) return false;
            return _stars[index].active;
        }

        public Vector2 GetStarPosition(int index)
        {
            if (index < 0 || index >= _stars.Length) return Vector2.zero;
            return _stars[index].position;
        }

        private void RecountActiveStars()
        {
            int count = 0;
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i].active) count++;
            }
            _activeStarCount = count;
        }
    }
}
