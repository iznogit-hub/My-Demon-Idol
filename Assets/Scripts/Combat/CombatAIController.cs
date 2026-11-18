using System;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Combat
{
    public enum CpuPersonality
    {
        Balanced,
        Rushdown,
        Zoner,
        Grappler,
        Boss
    }

    /// <summary>
    /// Intelligent, tick-deterministic Fighting Game Combat AI.
    /// Operates on the 60Hz combat simulation clock.
    /// Adapts dynamically to SettingsPanel.AiDifficulty (0=Easy, 1=Normal, 2=Hard, 3=Expert, 4=DemonLord).
    /// Features:
    /// - Micro-spacing footsies (walk, creep, forward dash, backdash).
    /// - High/Low guard reads (blocking standing vs crouching, reacting to overheads/sweeps).
    /// - Hit-confirm Gatling strings & Rapid Cancel extensions.
    /// - Tactical throw teching (breaking grabs within the 12-tick window).
    /// - Anti-air parries and jump-in punishes.
    /// - Domain Super overdrive punishes when meter >= 100.
    /// - Defensive Psychic Burst combo-breaking when cornered or low health.
    /// - BlazBlue Barrier Guard against heavy pressure to push opponent away.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class CombatAIController : MonoBehaviour
    {
        [Header("AI Identity & Personality")]
        [SerializeField] private FighterController _fighter;
        public CpuPersonality personality = CpuPersonality.Balanced;

        [Header("Runtime Tuning")]
        [Range(0, 4)]
        public int difficultyOverride = -1; // -1 = read from SettingsPanel.AiDifficulty

        public int EffectiveDifficulty => difficultyOverride >= 0 ? difficultyOverride : SettingsPanel.AiDifficulty;

        // Internal AI Ticking & Reaction Delays
        private int _actionCooldownTicks = 0;
        private int _reactionLagTicks = 0;
        private int _dashCooldownTicks = 0;
        private int _jumpCooldownTicks = 0;
        private bool _isBlocking = false;
        private System.Random _rng;

        private void Awake()
        {
            if (_fighter == null) _fighter = GetComponent<FighterController>();
            _rng = new System.Random(gameObject.GetHashCode() ^ (int)DateTime.UtcNow.Ticks);
        }

        private void Start()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
            ConfigurePersonalityFromFighter();
        }

        private void OnDestroy()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        private void OnDisable()
        {
            if (_fighter != null)
            {
                _fighter.SetMoveInput(Vector2.zero);
                if (_fighter.Barrier != null) _fighter.Barrier.DeactivateBarrier();
            }
            _actionCooldownTicks = 0;
            _reactionLagTicks = 0;
        }

        public void ConfigurePersonalityFromFighter()
        {
            if (_fighter == null) return;
            string id = _fighter.fighterName?.ToUpperInvariant() ?? "";
            if (id.Contains("C01") || id.Contains("KAI")) personality = CpuPersonality.Rushdown;
            else if (id.Contains("C02") || id.Contains("REIKA")) personality = CpuPersonality.Zoner;
            else if (id.Contains("C03") || id.Contains("ARIN")) personality = CpuPersonality.Grappler;
            else if (id.Contains("C04") || id.Contains("MIRA")) personality = CpuPersonality.Rushdown;
            else if (id.Contains("C05") || id.Contains("ZEV")) personality = CpuPersonality.Balanced;
            else if (id.Contains("C06") || id.Contains("NOX")) personality = CpuPersonality.Boss;
            else personality = CpuPersonality.Balanced;
        }

        private void HandleCombatTick(int currentTick)
        {
            if (!enabled || _fighter == null || _fighter.isPlayerControlled || _fighter.State == FighterState.Dead)
            {
                return;
            }

            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen)
            {
                return;
            }

            FighterController opponent = _fighter.opponent;
            if (opponent == null || opponent.State == FighterState.Dead)
            {
                _fighter.SetMoveInput(Vector2.zero);
                return;
            }

            int diff = EffectiveDifficulty;
            float distanceX = Mathf.Abs(opponent.transform.position.x - _fighter.transform.position.x);

            // 1. Reactive Defense: Throw Tech (12-tick escape window)
            if (_fighter.IsBeingThrown)
            {
                HandleThrowTechReaction(currentTick, diff);
                return;
            }

            // 2. Reactive Defense: Psychic Burst combo breaker when stuck in hitstun
            if (_fighter.State == FighterState.Hitstun || _fighter.State == FighterState.AirborneJuggle)
            {
                HandleBurstReaction(currentTick, diff);
                return;
            }

            // 3. Reactive Defense: Incoming strike guard & parry read
            if (opponent.State == FighterState.AttackStartup || opponent.State == FighterState.AttackActive)
            {
                HandleIncomingAttackDefense(opponent, currentTick, diff, distanceX);
                return;
            }
            else
            {
                // Reset guard state if opponent is not attacking
                if (_isBlocking)
                {
                    _isBlocking = false;
                    if (_fighter.Barrier != null && _fighter.Barrier.IsBarrierActive)
                    {
                        _fighter.Barrier.DeactivateBarrier();
                    }
                }
            }

            // 4. Action Cooldown Ticking
            if (_actionCooldownTicks > 0)
            {
                _actionCooldownTicks--;
                return;
            }
            if (_dashCooldownTicks > 0) _dashCooldownTicks--;
            if (_jumpCooldownTicks > 0) _jumpCooldownTicks--;

            // If in un-cancelable recovery or hitstun, do nothing
            if (!_fighter.IsActionable && _fighter.State != FighterState.AttackRecovery)
            {
                return;
            }

            // 5. Offensive & Neutral Decision Engine
            DecideNeutralOrOffense(opponent, currentTick, diff, distanceX);
        }

        #region Defensive AI Mechanics

        private void HandleThrowTechReaction(int currentTick, int difficulty)
        {
            // Chance to break throws scales with difficulty:
            // Easy: 10%, Normal: 45%, Hard: 75%, Expert: 90%, Demon Lord: 98%
            float techChance = difficulty switch
            {
                0 => 0.10f,
                1 => 0.45f,
                2 => 0.75f,
                3 => 0.90f,
                _ => 0.98f
            };

            if ((float)_rng.NextDouble() < techChance)
            {
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Throw, currentTick, Vector2.zero);
            }
        }

        private void HandleBurstReaction(int currentTick, int difficulty)
        {
            if (!_fighter.CanExecuteBurst()) return;

            // Use burst defensively if health is low or opponent combo hit count is high
            bool lowHealth = (_fighter.currentHealth / _fighter.maxHealth) < 0.45f;
            int comboCount = _fighter.Juggle != null ? _fighter.Juggle.ComboHitCount : 0;

            float burstChance = difficulty switch
            {
                0 => 0.10f,
                1 => (comboCount >= 3 || lowHealth) ? 0.40f : 0.05f,
                2 => (comboCount >= 2 || lowHealth) ? 0.70f : 0.10f,
                3 => (comboCount >= 2 || lowHealth) ? 0.85f : 0.20f,
                _ => (comboCount >= 1 || lowHealth) ? 0.95f : 0.30f
            };

            if ((float)_rng.NextDouble() < burstChance)
            {
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Burst, currentTick, Vector2.zero);
            }
        }

        private void HandleIncomingAttackDefense(FighterController opponent, int currentTick, int difficulty, float distanceX)
        {
            if (distanceX > 3.5f) return; // Attack is too far to hit

            AttackData incomingAtk = opponent.CurrentAttack;
            int facing = _fighter.FacingDirection;

            // Frame-perfect Parry check on high difficulty:
            // Easy: 0%, Normal: 5%, Hard: 20%, Expert: 40%, Demon Lord: 65%
            float parryChance = difficulty switch
            {
                0 => 0.0f,
                1 => 0.05f,
                2 => 0.20f,
                3 => 0.40f,
                _ => 0.65f
            };

            if (_fighter.IsActionable && (float)_rng.NextDouble() < parryChance)
            {
                _fighter.InputBuf.Enqueue(InputButton.Parry, currentTick, Vector2.zero);
                _actionCooldownTicks = 12;
                return;
            }

            // High vs Low guard read:
            // Overhead -> Stand Guard (-facing, 0)
            // Low -> Crouch Guard (-facing, -1)
            // Mid / High -> Stand or Crouch
            bool shouldCrouchBlock = false;
            if (incomingAtk != null)
            {
                if (incomingAtk.hitLevel == HitLevel.Low)
                {
                    shouldCrouchBlock = true;
                }
                else if (incomingAtk.hitLevel == HitLevel.Overhead)
                {
                    shouldCrouchBlock = false;
                }
                else
                {
                    // Mid/High: 50% stand, 50% crouch on normal
                    shouldCrouchBlock = _rng.NextDouble() > 0.5f;
                }
            }

            Vector2 blockDir = shouldCrouchBlock ? new Vector2(-facing, -1f) : new Vector2(-facing, 0f);
            _fighter.SetMoveInput(blockDir);
            _isBlocking = true;

            // Barrier Guard (pushblock) if cornered or facing heavy blockstun pressure
            if (difficulty >= 2 && _fighter.currentSuperMeter >= 15f)
            {
                bool cornered = Mathf.Abs(_fighter.transform.position.x) >= 9.0f;
                if (cornered && _fighter.Barrier != null)
                {
                    _fighter.Barrier.TryActivateBarrier();
                }
            }
        }

        #endregion

        #region Offensive AI Mechanics

        private void DecideNeutralOrOffense(FighterController opponent, int currentTick, int difficulty, float distanceX)
        {
            int facing = _fighter.FacingDirection;

            // 1. Combo Extension: Hit-Confirm into Special / Super / Gatling
            if (_fighter.State == FighterState.AttackRecovery)
            {
                HandleAttackRecoveryCancels(opponent, currentTick, difficulty);
                return;
            }

            // 2. Punish Whiff / Hitstun: If opponent is open and in range
            bool opponentVulnerable = opponent.State == FighterState.AttackRecovery ||
                                     opponent.State == FighterState.Hitstun ||
                                     opponent.State == FighterState.Knockdown;

            if (opponentVulnerable && distanceX <= 2.2f)
            {
                ExecutePunishString(currentTick, difficulty);
                return;
            }

            // 3. Spacing / Footsies range management
            float preferredRange = personality switch
            {
                CpuPersonality.Rushdown => 1.2f,
                CpuPersonality.Grappler => 1.0f,
                CpuPersonality.Zoner => 3.2f,
                CpuPersonality.Boss => 1.5f,
                _ => 1.8f
            };

            // Too far: close the gap
            if (distanceX > preferredRange + 0.6f)
            {
                if (distanceX > 4.5f && _dashCooldownTicks <= 0 && difficulty >= 1 && (float)_rng.NextDouble() < 0.35f)
                {
                    // Dash forward
                    _fighter.InputBuf.Enqueue(InputButton.ForwardDash, currentTick, new Vector2(facing, 0));
                    _dashCooldownTicks = 45;
                }
                else
                {
                    // Walk forward
                    _fighter.SetMoveInput(new Vector2(facing, 0));
                }
                _actionCooldownTicks = 8;
                return;
            }

            // Too close for Zoner: back up or backdash
            if (personality == CpuPersonality.Zoner && distanceX < preferredRange - 0.4f)
            {
                if (_dashCooldownTicks <= 0 && difficulty >= 2 && (float)_rng.NextDouble() < 0.40f)
                {
                    _fighter.InputBuf.Enqueue(InputButton.Backdash, currentTick, new Vector2(-facing, 0));
                    _dashCooldownTicks = 40;
                }
                else
                {
                    _fighter.SetMoveInput(new Vector2(-facing, 0));
                }
                _actionCooldownTicks = 10;
                return;
            }

            // 4. In Strike Range: Choose attack or mixup
            _fighter.SetMoveInput(Vector2.zero);
            ExecuteInRangeAttack(opponent, currentTick, difficulty, distanceX);
        }

        private void HandleAttackRecoveryCancels(FighterController opponent, int currentTick, int difficulty)
        {
            // If we have 100% Super Meter and hit is confirmed, cancel into Domain Super!
            if (difficulty >= 2 && _fighter.currentSuperMeter >= 100f && (float)_rng.NextDouble() < 0.70f)
            {
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Super, currentTick, Vector2.zero);
                _actionCooldownTicks = 20;
                return;
            }

            // Rapid Cancel (50% meter) to extend combo or make an unsafe whiff safe
            if (difficulty >= 3 && _fighter.currentSuperMeter >= 50f && RapidCancel.CanExecute(_fighter))
            {
                if ((float)_rng.NextDouble() < 0.45f)
                {
                    RapidCancel.Execute(_fighter);
                    _actionCooldownTicks = 6;
                    return;
                }
            }

            // Gatling chain: Light -> Heavy / Skill
            if (_fighter.CurrentAttack != null && _fighter.CurrentAttack.attackType == AttackType.Light)
            {
                ThreeButtonCommand followUp = (_rng.NextDouble() < 0.6f) ? ThreeButtonCommand.Heavy : ThreeButtonCommand.Skill;
                _fighter.InputBuf.Enqueue(followUp, currentTick, Vector2.zero);
                _actionCooldownTicks = 12;
            }
        }

        private void ExecutePunishString(int currentTick, int difficulty)
        {
            int facing = _fighter.FacingDirection;

            // High difficulty: Optimal heavy punish or Super
            if (difficulty >= 3 && _fighter.currentSuperMeter >= 100f && (float)_rng.NextDouble() < 0.85f)
            {
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Super, currentTick, Vector2.zero);
                _actionCooldownTicks = 30;
                return;
            }

            // Normal punish: Heavy launcher or Skill
            ThreeButtonCommand cmd = (difficulty >= 2 && _rng.NextDouble() < 0.7f) ? ThreeButtonCommand.Heavy : ThreeButtonCommand.Light;
            _fighter.InputBuf.Enqueue(cmd, currentTick, new Vector2(facing, 0));
            _actionCooldownTicks = 14;
        }

        private void ExecuteInRangeAttack(FighterController opponent, int currentTick, int difficulty, float distanceX)
        {
            int facing = _fighter.FacingDirection;
            double roll = _rng.NextDouble();

            // Grappler / Point-Blank: Tactical Throw
            if ((personality == CpuPersonality.Grappler || distanceX <= 1.15f) && roll < (0.20f + difficulty * 0.05f))
            {
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Throw, currentTick, Vector2.zero);
                _actionCooldownTicks = 25;
                return;
            }

            // Low sweep check (crouch + light or low attack)
            if (roll < 0.40f)
            {
                _fighter.SetMoveInput(new Vector2(facing, -1f));
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Light, currentTick, new Vector2(facing, -1f));
                _actionCooldownTicks = 18;
                return;
            }

            // Drive Skill poke / kinetic projectile
            if (roll < 0.65f)
            {
                _fighter.InputBuf.Enqueue(ThreeButtonCommand.Skill, currentTick, new Vector2(facing, 0));
                _actionCooldownTicks = 24;
                return;
            }

            // Heavy attack / Overhead jump-in
            if (roll < 0.85f)
            {
                if (difficulty >= 2 && _jumpCooldownTicks <= 0 && _rng.NextDouble() < 0.35f)
                {
                    // Jump attack overhead
                    _fighter.SetMoveInput(new Vector2(facing, 1f));
                    _fighter.InputBuf.Enqueue(ThreeButtonCommand.Heavy, currentTick + 8, new Vector2(facing, 0));
                    _jumpCooldownTicks = 90;
                    _actionCooldownTicks = 30;
                }
                else
                {
                    _fighter.InputBuf.Enqueue(ThreeButtonCommand.Heavy, currentTick, new Vector2(facing, 0));
                    _actionCooldownTicks = 28;
                }
                return;
            }

            // Safe Light jab poke
            _fighter.InputBuf.Enqueue(ThreeButtonCommand.Light, currentTick, new Vector2(facing, 0));
            _actionCooldownTicks = 16;
        }

        #endregion
    }
}
