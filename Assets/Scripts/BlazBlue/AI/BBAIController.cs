// 60 Hz Deterministic AI Controller for BlazBlue
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    public enum BBAIDifficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Boss = 3
    }

    /// <summary>
    /// 60 Hz Deterministic AI Controller for BlazBlue 4-Button combat.
    /// Emits frame-accurate real motion inputs over ticks for special and super moves.
    /// Standardized for the 7-move launch kits (5A, 5B, 5C, 2C, j.B, 5D, 236236C).
    /// Features:
    /// - Archetype-aware behavior (Rushdown, Zoner, Grappler/Powerhouse, Swordsman, AllRounder)
    /// - Gatling combo chains (5A -> 5B -> 5C -> 2C / 5D -> 236236C) on hit confirms
    /// - High/Low guard reads, barrier pushback, and defensive burst escapes
    /// - Frame-perfect anti-airs (5C/2C) and jump-ins (9 -> j.B)
    /// - Seeded deterministic RNG and strictly enforced >=10-frame spacing between attack startups
    /// </summary>
    public class BBAIController : MonoBehaviour
    {
        public struct MotionStep
        {
            public sbyte facingNumpad; // 1..9 in facing-relative coordinates
            public BBButton buttons;
            public MotionStep(sbyte numpad, BBButton btn)
            {
                facingNumpad = numpad;
                buttons = btn;
            }
        }

        [Header("AI Settings")]
        public BBAIDifficulty difficulty = BBAIDifficulty.Medium;
        public bool enabledAI = true;

        [Header("State Tracking")]
        [SerializeField] private int _decisionDelayTicks = 0;
        [SerializeField] private sbyte _desiredNumpad = 5;
        [SerializeField] private BBButton _desiredButtons = BBButton.None;
        [SerializeField] private byte _prevHeld = 0;
        [SerializeField] private int _attackCooldownTicks = 0;
        [SerializeField] private int _reactionDelayTicks = 0;
        [SerializeField] private int _lastAttackTick = -100;

        private readonly Queue<MotionStep> _motionQueue = new Queue<MotionStep>();
        private System.Random _rng = new System.Random(1337);
        private int _lastReactedAttackTick = -1;
        private bool _isReactingToBlock = false;
        private bool _blockDecisionSuccessful = false;

        public void ResetAI(int seed = 1337)
        {
            _decisionDelayTicks = 0;
            _desiredNumpad = 5;
            _desiredButtons = BBButton.None;
            _prevHeld = 0;
            _attackCooldownTicks = 0;
            _reactionDelayTicks = 0;
            _lastAttackTick = -100;
            _lastReactedAttackTick = -1;
            _isReactingToBlock = false;
            _blockDecisionSuccessful = false;
            _motionQueue.Clear();
            _rng = new System.Random(seed);
        }

        private int GetFacing(BBFighterBrain brain)
        {
            if (brain == null) return 1;
            if (brain.fighter != null) return brain.fighter.FacingDirection >= 0 ? 1 : -1;
            return brain.transform.localScale.x >= 0 ? 1 : -1;
        }

        private BBArchetypeClass GetArchetype(BBFighterBrain self)
        {
            if (self == null) return BBArchetypeClass.Rushdown;
            string charId = self.fighter != null && self.fighter.appliedDefinition != null
                ? self.fighter.appliedDefinition.id
                : (self.profile != null ? self.profile.characterId : "C01");
            return BBRosterArchetype.Get(charId).archetypeClass;
        }

        public BBInputFrame GenerateInput(BBFighterBrain self, BBFighterBrain opponent, int currentTick)
        {
            if (!enabledAI || self == null)
            {
                _prevHeld = 0;
                return new BBInputFrame {
                    numpad = 5,
                    held = 0,
                    pressed = 0,
                    released = 0
                };
            }

            int facing = GetFacing(self);

            if (_attackCooldownTicks > 0)
            {
                _attackCooldownTicks--;
            }

            if (_reactionDelayTicks > 0)
            {
                _reactionDelayTicks--;
            }

            // 1. If executing a multi-frame motion input macro, emit next step
            if (_motionQueue.Count > 0)
            {
                var step = _motionQueue.Dequeue();
                _desiredNumpad = BBInputFrame.FacingToWorldNumpad(step.facingNumpad, facing);
                _desiredButtons = step.buttons;

                if (step.buttons != BBButton.None)
                {
                    _attackCooldownTicks = Mathf.Max(_attackCooldownTicks, 10);
                    _lastAttackTick = currentTick;
                }

                return ConstructInputFrame();
            }

            // 2. Defensive Burst Break: If trapped in combo/hitstun with low health, burst out
            if ((difficulty == BBAIDifficulty.Hard || difficulty == BBAIDifficulty.Boss) &&
                self.bbState == BBState.Hitstun && self.burstMeter >= 100f && self.health < 4000f)
            {
                if (_rng.NextDouble() < (difficulty == BBAIDifficulty.Boss ? 0.75 : 0.40))
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.A | BBButton.B | BBButton.C | BBButton.D;
                    return ConstructInputFrame();
                }
            }

            // 3. Incoming Attack Reaction & Defensive Guarding
            bool oppAttacking = opponent != null && (opponent.currentAttack != null ||
                opponent.bbState == BBState.AttackStartup || opponent.bbState == BBState.AttackActive || opponent.bbState == BBState.AttackRecovery);

            if (oppAttacking)
            {
                float dist = Mathf.Abs(self.transform.position.x - opponent.transform.position.x);
                if (dist <= 3.5f)
                {
                    // New attack detected: start reaction delay
                    if (_lastReactedAttackTick != opponent.attackTick)
                    {
                        _lastReactedAttackTick = opponent.attackTick;
                        _reactionDelayTicks = GetReactionDelayTicks(difficulty);
                        _blockDecisionSuccessful = _rng.NextDouble() <= GetBlockChance(difficulty);
                        _isReactingToBlock = true;
                    }

                    // Once reaction delay has elapsed, hold appropriate High/Low block
                    if (_reactionDelayTicks <= 0 && _isReactingToBlock)
                    {
                        if (_blockDecisionSuccessful)
                        {
                            var atk = opponent.currentAttack;
                            bool isLow = atk != null && atk.guard == BBGuard.Low;

                            // Crouch block (1) vs Low, Stand block (4) vs Overhead/High/Mid
                            sbyte blockFacing = isLow ? (sbyte)1 : (sbyte)4;
                            _desiredNumpad = BBInputFrame.FacingToWorldNumpad(blockFacing, facing);

                            // Barrier Guard on Hard/Boss against heavy moves or supers
                            if ((difficulty == BBAIDifficulty.Hard || difficulty == BBAIDifficulty.Boss) &&
                                self.barrierMeter > 25f && atk != null &&
                                (atk.kind == BBMoveKind.DistortionDrive || atk.damage >= 1500f))
                            {
                                _desiredButtons = BBButton.A | BBButton.B;
                            }
                            else
                            {
                                _desiredButtons = BBButton.None;
                            }
                            return ConstructInputFrame();
                        }
                    }
                }
            }
            else
            {
                _isReactingToBlock = false;
            }

            // 4. Gatling Combos: If an attack connected, cancel into next move
            if (self.currentAttack != null && (self.bbState == BBState.AttackActive || self.bbState == BBState.AttackRecovery))
            {
                if (self.chainState.madeContact && (currentTick - _lastAttackTick >= 10) && _attackCooldownTicks <= 0)
                {
                    if (TryEvaluateGatlingCancel(self, opponent, facing, currentTick))
                    {
                        _attackCooldownTicks = 10;
                        _lastAttackTick = currentTick;
                        return ConstructInputFrame();
                    }
                }
            }

            // 5. Airborne Combat: Execute j.B overhead jump attack if in range
            if (!self.isGrounded)
            {
                float dist = opponent != null ? Mathf.Abs(self.transform.position.x - opponent.transform.position.x) : 99f;
                if (dist <= 2.2f && (currentTick - _lastAttackTick >= 10) && _attackCooldownTicks <= 0 && self.currentAttack == null)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // j.B Jump Attack
                    _attackCooldownTicks = 10;
                    _lastAttackTick = currentTick;
                    return ConstructInputFrame();
                }
                return ConstructInputFrame();
            }

            // 6. Periodic Ground Decision Interval
            int checkInterval = GetCheckInterval();
            if (_decisionDelayTicks <= 0 || (currentTick % checkInterval == 0))
            {
                EvaluateCombatSituation(self, opponent, currentTick);
                _decisionDelayTicks = checkInterval;
            }
            else
            {
                _decisionDelayTicks--;
            }

            // Strictly enforce minimum 10 frames between attack startups
            if (_desiredButtons != BBButton.None)
            {
                if (currentTick - _lastAttackTick < 10 || _attackCooldownTicks > 0)
                {
                    _desiredButtons = BBButton.None;
                }
                else
                {
                    _attackCooldownTicks = 10;
                    _lastAttackTick = currentTick;
                }
            }

            return ConstructInputFrame();
        }

        private bool TryEvaluateGatlingCancel(BBFighterBrain self, BBFighterBrain opponent, int facing, int currentTick)
        {
            double comboRoll = _rng.NextDouble();
            double comboChance = GetGatlingChance(difficulty);
            if (comboRoll > comboChance) return false;

            string curId = self.currentAttack != null ? self.currentAttack.moveId : "";

            // Chain: 5A -> 5B or 2C
            if (curId == "5A")
            {
                if (_rng.NextDouble() < 0.65)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // 5B
                    return true;
                }
                else
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C Low Sweep
                    return true;
                }
            }

            // Chain: 5B -> 5C
            if (curId == "5B")
            {
                _desiredNumpad = 5;
                _desiredButtons = BBButton.C; // 5C Heavy Strike
                return true;
            }

            // Chain: 5C -> 2C (Knockdown), 5D (Drive), or 236236C (Super confirm)
            if (curId == "5C")
            {
                if (self.heatMeter >= 50f && (difficulty >= BBAIDifficulty.Medium) && _rng.NextDouble() < 0.70)
                {
                    Enqueue236236C(facing);
                    return true;
                }
                if (_rng.NextDouble() < 0.50)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D Signature Drive
                    return true;
                }
                _desiredNumpad = 2;
                _desiredButtons = BBButton.C; // 2C Low Sweep knockdown
                return true;
            }

            // Chain: 5D -> 236236C Super confirm
            if (curId == "5D")
            {
                if (self.heatMeter >= 50f && _rng.NextDouble() < 0.85)
                {
                    Enqueue236236C(facing);
                    return true;
                }
            }

            return false;
        }

        private BBInputFrame ConstructInputFrame()
        {
            byte currentHeld = (byte)_desiredButtons;
            byte pressed = (byte)(currentHeld & ~_prevHeld);
            byte released = (byte)(_prevHeld & ~currentHeld);
            _prevHeld = currentHeld;

            return new BBInputFrame {
                numpad = _desiredNumpad,
                held = currentHeld,
                pressed = pressed,
                released = released
            };
        }

        private int GetReactionDelayTicks(BBAIDifficulty diff)
        {
            switch (diff)
            {
                case BBAIDifficulty.Easy: return 20;
                case BBAIDifficulty.Medium: return 14;
                case BBAIDifficulty.Hard: return 8;
                case BBAIDifficulty.Boss: return 4;
                default: return 14;
            }
        }

        private double GetBlockChance(BBAIDifficulty diff)
        {
            switch (diff)
            {
                case BBAIDifficulty.Easy: return 0.30;
                case BBAIDifficulty.Medium: return 0.60;
                case BBAIDifficulty.Hard: return 0.80;
                case BBAIDifficulty.Boss: return 0.92;
                default: return 0.60;
            }
        }

        private double GetGatlingChance(BBAIDifficulty diff)
        {
            switch (diff)
            {
                case BBAIDifficulty.Easy: return 0.25;
                case BBAIDifficulty.Medium: return 0.60;
                case BBAIDifficulty.Hard: return 0.85;
                case BBAIDifficulty.Boss: return 0.95;
                default: return 0.60;
            }
        }

        private int GetCheckInterval()
        {
            switch (difficulty)
            {
                case BBAIDifficulty.Easy: return 12;
                case BBAIDifficulty.Medium: return 6;
                case BBAIDifficulty.Hard: return 3;
                case BBAIDifficulty.Boss: return 1;
                default: return 6;
            }
        }

        private void EvaluateCombatSituation(BBFighterBrain self, BBFighterBrain opponent, int tick)
        {
            _desiredNumpad = 5;
            _desiredButtons = BBButton.None;

            if (opponent == null || self.currentAttack != null) return;

            int facing = GetFacing(self);
            float selfX = self.transform.position.x;
            float oppX = opponent.transform.position.x;
            float dist = Mathf.Abs(selfX - oppX);
            bool oppAirborne = !opponent.isGrounded;

            // 1. Anti-Air Reaction: Airborne opponent approaching strike range
            if (oppAirborne && dist <= 2.2f && self.isGrounded)
            {
                // Frame-perfect 5C or 2C anti-air
                double aaRoll = _rng.NextDouble();
                if (aaRoll < 0.60)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C Anti-Air
                }
                else
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C Low Anti-Air Sweep
                }
                return;
            }

            // 2. Archetype-Specific Combat Strategy
            var archetype = GetArchetype(self);
            switch (archetype)
            {
                case BBArchetypeClass.Rushdown:
                    EvaluateRushdown(self, opponent, facing, dist);
                    break;
                case BBArchetypeClass.Zoner:
                    EvaluateZoner(self, opponent, facing, dist);
                    break;
                case BBArchetypeClass.Grappler:
                    EvaluatePowerhouse(self, opponent, facing, dist);
                    break;
                case BBArchetypeClass.Swordsman:
                    EvaluateSwordsman(self, opponent, facing, dist);
                    break;
                case BBArchetypeClass.AllRounder:
                default:
                    EvaluateAllRounder(self, opponent, facing, dist);
                    break;
            }
        }

        /// <summary>
        /// Rushdown Archetype (Akio, Aika, SanpakuChan):
        /// Relentless forward aggression, forward dashes, rapid jab checks, jump-in overheads, and drive pressure.
        /// </summary>
        private void EvaluateRushdown(BBFighterBrain self, BBFighterBrain opponent, int facing, float dist)
        {
            if (dist > 2.0f)
            {
                // Long range: Forward Dash or Run
                if (_rng.NextDouble() < 0.40)
                    EnqueueForwardDash(facing);
                else
                    _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
            }
            else if (dist > 1.3f)
            {
                // Mid range: Jump-in overhead or advance
                double roll = _rng.NextDouble();
                if (roll < 0.35)
                {
                    EnqueueJumpAttack(facing);
                }
                else
                {
                    _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
                }
            }
            else
            {
                // In close: Rapid pokes and mixups
                if (self.heatMeter >= 50f && _rng.NextDouble() < 0.40)
                {
                    Enqueue236236C(facing);
                    return;
                }

                double roll = _rng.NextDouble();
                if (roll < 0.40)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.A; // 5A Jab
                }
                else if (roll < 0.65)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // 5B Medium
                }
                else if (roll < 0.85)
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C Low Sweep
                }
                else
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D Signature Drive
                }
            }
        }

        /// <summary>
        /// Zoner Archetype (Akane, Cultist, PicoChan, Fina):
        /// Keepaway spacing, backdashes when crowded, defensive sweeps, and long-range Drive projectiles/spells.
        /// </summary>
        private void EvaluateZoner(BBFighterBrain self, BBFighterBrain opponent, int facing, float dist)
        {
            if (dist < 1.5f)
            {
                // Opponent in face: Backdash, defensive sweep 2C, or jab
                double roll = _rng.NextDouble();
                if (roll < 0.40)
                {
                    EnqueueBackdash(facing);
                }
                else if (roll < 0.70)
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C Defensive Sweep Knockdown
                }
                else
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.A; // 5A Panic jab
                }
            }
            else if (dist >= 1.5f && dist <= 2.6f)
            {
                // Mid range: 5C poke, 5D Drive spell/shot, or retreat
                double roll = _rng.NextDouble();
                if (roll < 0.45)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C Long-range poke
                }
                else if (roll < 0.75)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D Drive Projectile
                }
                else
                {
                    _desiredNumpad = BBInputFrame.FacingToWorldNumpad(4, facing); // Walk backward
                }
            }
            else
            {
                // Fullscreen/long range: Drive zoning & Super
                if (self.heatMeter >= 50f && _rng.NextDouble() < 0.50)
                {
                    Enqueue236236C(facing);
                    return;
                }

                double roll = _rng.NextDouble();
                if (roll < 0.60)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D Projectile/Orb/Volley
                }
                else if (roll < 0.85)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C Heavy Poke
                }
                else
                {
                    _desiredNumpad = 5; // Hold spacing
                }
            }
        }

        /// <summary>
        /// Grappler / Powerhouse Archetype (OfficerK, Torgrim):
        /// Heavy armored advance, colossal pokes (5C, 5D), and devastating 236236C super punishes.
        /// </summary>
        private void EvaluatePowerhouse(BBFighterBrain self, BBFighterBrain opponent, int facing, float dist)
        {
            if (dist > 1.8f)
            {
                // Steady forward advance
                _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
            }
            else if (dist > 1.2f)
            {
                // Mid range: Heavy blows
                if (self.heatMeter >= 50f && _rng.NextDouble() < 0.45)
                {
                    Enqueue236236C(facing);
                    return;
                }

                double roll = _rng.NextDouble();
                if (roll < 0.45)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C Colossal Maul/Hammer Strike
                }
                else if (roll < 0.75)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D Heavy Drive
                }
                else
                {
                    _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
                }
            }
            else
            {
                // In close: Heavy 5B, 5C, or 2C Sweep
                double roll = _rng.NextDouble();
                if (roll < 0.35)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // 5B
                }
                else if (roll < 0.70)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C
                }
                else
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C Sweep
                }
            }
        }

        /// <summary>
        /// Swordsman Archetype (Muryotaisu, Hugo):
        /// Takes full advantage of long reach (1.25m-1.35m) to out-space opponents with 5B and 5C.
        /// </summary>
        private void EvaluateSwordsman(BBFighterBrain self, BBFighterBrain opponent, int facing, float dist)
        {
            if (dist > 2.2f)
            {
                _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
            }
            else if (dist >= 1.4f)
            {
                // Prime swordsman range: 5B thrust & 5C cleave
                if (self.heatMeter >= 50f && _rng.NextDouble() < 0.40)
                {
                    Enqueue236236C(facing);
                    return;
                }

                double roll = _rng.NextDouble();
                if (roll < 0.45)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // 5B Long Reach Thrust
                }
                else if (roll < 0.80)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C Great Cleave
                }
                else
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D Drive
                }
            }
            else
            {
                // Close range
                double roll = _rng.NextDouble();
                if (roll < 0.45)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.A; // 5A
                }
                else if (roll < 0.75)
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C Sweep
                }
                else
                {
                    EnqueueBackdash(facing);
                }
            }
        }

        /// <summary>
        /// All-Rounder Archetype (Casual1):
        /// Balanced spacing, mid-range pokes, jump-in overheads, sweeps, and drive.
        /// </summary>
        private void EvaluateAllRounder(BBFighterBrain self, BBFighterBrain opponent, int facing, float dist)
        {
            if (dist > 2.0f)
            {
                _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
            }
            else if (dist > 1.3f)
            {
                double roll = _rng.NextDouble();
                if (roll < 0.25)
                {
                    EnqueueJumpAttack(facing);
                }
                else if (roll < 0.60)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // 5B
                }
                else
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C; // 5C
                }
            }
            else
            {
                if (self.heatMeter >= 50f && _rng.NextDouble() < 0.40)
                {
                    Enqueue236236C(facing);
                    return;
                }

                double roll = _rng.NextDouble();
                if (roll < 0.35)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.A; // 5A
                }
                else if (roll < 0.65)
                {
                    _desiredNumpad = 2;
                    _desiredButtons = BBButton.C; // 2C
                }
                else if (roll < 0.85)
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.D; // 5D
                }
                else
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B; // 5B
                }
            }
        }

        private void EnqueueForwardDash(int facing)
        {
            _motionQueue.Clear();
            _motionQueue.Enqueue(new MotionStep(6, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(5, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(6, BBButton.None));

            var first = _motionQueue.Dequeue();
            _desiredNumpad = BBInputFrame.FacingToWorldNumpad(first.facingNumpad, facing);
            _desiredButtons = first.buttons;
        }

        private void EnqueueBackdash(int facing)
        {
            _motionQueue.Clear();
            _motionQueue.Enqueue(new MotionStep(4, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(5, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(4, BBButton.None));

            var first = _motionQueue.Dequeue();
            _desiredNumpad = BBInputFrame.FacingToWorldNumpad(first.facingNumpad, facing);
            _desiredButtons = first.buttons;
        }

        private void EnqueueJumpAttack(int facing)
        {
            _motionQueue.Clear();
            _motionQueue.Enqueue(new MotionStep(9, BBButton.None)); // Jump Forward
            _motionQueue.Enqueue(new MotionStep(5, BBButton.None));

            var first = _motionQueue.Dequeue();
            _desiredNumpad = BBInputFrame.FacingToWorldNumpad(first.facingNumpad, facing);
            _desiredButtons = first.buttons;
        }

        private void Enqueue236236C(int facing)
        {
            _motionQueue.Clear();
            _motionQueue.Enqueue(new MotionStep(2, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(3, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(6, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(2, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(3, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(6, BBButton.C));

            var first = _motionQueue.Dequeue();
            _desiredNumpad = BBInputFrame.FacingToWorldNumpad(first.facingNumpad, facing);
            _desiredButtons = first.buttons;
        }
    }
}
