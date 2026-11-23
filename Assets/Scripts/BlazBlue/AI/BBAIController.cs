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
    /// Uses seeded deterministic RNG, reaction delay against incoming attacks, High/Low blocking,
    /// anti-air reactions, and minimum 10-frame spacing between attacks.
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

        private readonly Queue<MotionStep> _motionQueue = new Queue<MotionStep>();
        private System.Random _rng = new System.Random(1337);
        private int _lastReactedAttackTick = -1;
        private bool _isReactingToBlock = false;
        private bool _blockDecisionSuccessful = false;
        private bool _hasUsedSpecialThisMatch = false;

        public void ResetAI(int seed = 1337)
        {
            _decisionDelayTicks = 0;
            _desiredNumpad = 5;
            _desiredButtons = BBButton.None;
            _prevHeld = 0;
            _attackCooldownTicks = 0;
            _reactionDelayTicks = 0;
            _lastReactedAttackTick = -1;
            _isReactingToBlock = false;
            _blockDecisionSuccessful = false;
            _hasUsedSpecialThisMatch = false;
            _motionQueue.Clear();
            _rng = new System.Random(seed);
        }

        private int GetFacing(BBFighterBrain brain)
        {
            if (brain == null) return 1;
            if (brain.fighter != null) return brain.fighter.FacingDirection >= 0 ? 1 : -1;
            return brain.transform.localScale.x >= 0 ? 1 : -1;
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

            if (self.currentAttack != null)
            {
                _attackCooldownTicks = 10;
            }
            else if (_attackCooldownTicks > 0)
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
                }

                return ConstructInputFrame();
            }

            // 2. Incoming Attack Reaction & Defensive Guarding
            bool oppAttacking = opponent != null && opponent.currentAttack != null &&
                (opponent.bbState == BBState.AttackStartup || opponent.bbState == BBState.AttackActive || opponent.bbState == BBState.AttackRecovery);

            if (oppAttacking)
            {
                float dist = Mathf.Abs(self.transform.position.x - opponent.transform.position.x);
                if (dist <= 3.2f)
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
                            bool isOverhead = atk.guard == BBGuard.Overhead || atk.moveId == "214B" || !opponent.isGrounded;

                            // Stand block (4) vs Overhead/High, Crouch block (1) vs Low/Mid
                            sbyte blockFacing = isOverhead ? (sbyte)4 : (sbyte)1;
                            _desiredNumpad = BBInputFrame.FacingToWorldNumpad(blockFacing, facing);
                            _desiredButtons = BBButton.None;
                            return ConstructInputFrame();
                        }
                    }
                }
            }
            else
            {
                _isReactingToBlock = false;
            }

            // 3. Periodic Decision Interval
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

            // Enforce minimum 10 frames between attacks
            if (_attackCooldownTicks > 0)
            {
                _desiredButtons = BBButton.None;
            }
            else if (_desiredButtons != BBButton.None)
            {
                _attackCooldownTicks = 10;
            }

            return ConstructInputFrame();
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
                case BBAIDifficulty.Hard: return 10;
                case BBAIDifficulty.Boss: return 6;
                default: return 14;
            }
        }

        private double GetBlockChance(BBAIDifficulty diff)
        {
            switch (diff)
            {
                case BBAIDifficulty.Easy: return 0.30;
                case BBAIDifficulty.Medium: return 0.55;
                case BBAIDifficulty.Hard: return 0.75;
                case BBAIDifficulty.Boss: return 0.90;
                default: return 0.55;
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

            // Anti-Air: airborne opponent in strike range
            if (oppAirborne && dist <= 2.2f && self.isGrounded && _attackCooldownTicks <= 0)
            {
                var antiAir = self.FindAttackByMoveId("6A");
                if (antiAir != null && self.CanExecuteMove(antiAir))
                {
                    _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
                    _desiredButtons = BBButton.A;
                    return;
                }
                var altB = self.FindAttackByMoveId("5B");
                if (altB != null && self.CanExecuteMove(altB))
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.B;
                    return;
                }
                var altC = self.FindAttackByMoveId("5C");
                if (altC != null && self.CanExecuteMove(altC))
                {
                    _desiredNumpad = 5;
                    _desiredButtons = BBButton.C;
                    return;
                }
            }

            // Motion Input Attacks (Super 236236C & Special 236A)
            if (dist <= 3.5f && _attackCooldownTicks <= 0)
            {
                if (!_hasUsedSpecialThisMatch)
                {
                    var sp = self.FindAttackByMoveId("236A");
                    if (sp != null && self.CanExecuteMove(sp))
                    {
                        _hasUsedSpecialThisMatch = true;
                        Enqueue236A(facing);
                        return;
                    }
                }
                else if (self.heatMeter >= 50f)
                {
                    var superAtk = self.FindAttackByMoveId("236236C");
                    if (superAtk != null && self.CanExecuteMove(superAtk))
                    {
                        Enqueue236236C(facing);
                        return;
                    }
                }
                else
                {
                    double roll = _rng.NextDouble();
                    if (roll < 0.65)
                    {
                        var sp = self.FindAttackByMoveId("236A");
                        if (sp != null && self.CanExecuteMove(sp))
                        {
                            Enqueue236A(facing);
                            return;
                        }
                    }
                }
            }

            // Neutral Spacing and Pokes
            if (dist > 1.8f)
            {
                // Advance forward
                _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
            }
            else if (dist <= 1.2f)
            {
                // Close range
                if (_attackCooldownTicks <= 0)
                {
                    double roll = _rng.NextDouble();
                    if (roll < 0.35)
                    {
                        _desiredNumpad = 5;
                        _desiredButtons = BBButton.A;
                    }
                    else if (roll < 0.65)
                    {
                        _desiredNumpad = 2;
                        _desiredButtons = BBButton.A;
                    }
                    else if (roll < 0.85)
                    {
                        _desiredNumpad = 2;
                        _desiredButtons = BBButton.B;
                    }
                    else
                    {
                        // Throw (6BC)
                        _desiredNumpad = BBInputFrame.FacingToWorldNumpad(6, facing);
                        _desiredButtons = BBButton.B | BBButton.C;
                    }
                }
            }
            else
            {
                // Mid range: 5B, 2B, 5C
                if (_attackCooldownTicks <= 0)
                {
                    double roll = _rng.NextDouble();
                    if (roll < 0.40)
                    {
                        _desiredNumpad = 5;
                        _desiredButtons = BBButton.B;
                    }
                    else if (roll < 0.70)
                    {
                        _desiredNumpad = 5;
                        _desiredButtons = BBButton.C;
                    }
                    else
                    {
                        _desiredNumpad = 2;
                        _desiredButtons = BBButton.C;
                    }
                }
            }
        }

        private void Enqueue236A(int facing)
        {
            _motionQueue.Clear();
            _motionQueue.Enqueue(new MotionStep(2, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(3, BBButton.None));
            _motionQueue.Enqueue(new MotionStep(6, BBButton.A));

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
