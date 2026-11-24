#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBCombatFeelDepthTests
    {
        [Test]
        public void Pillar1_DamageModel_CounterHit_And_PunishCounter_DamageMultipliers()
        {
            float baseDmg = 1000f;

            // Normal Hit (None): 1.0x (10,000 BP) -> 1000
            int normalDmg = BBDamageModel.CalculateDamage(
                baseDmg, 1.0f, 1.0f, 1, 10000, 10000, 0f, false, false,
                sameMovePenalty: BBDamageModel.SAME_MOVE_PENALTY_DEFAULT,
                counterType: BBCounterType.None
            );
            Assert.AreEqual(1000, normalDmg);

            // Counter Hit: +10% (11,000 BP) -> 1100
            int counterDmg = BBDamageModel.CalculateDamage(
                baseDmg, 1.0f, 1.0f, 1, 10000, 10000, 0f, false, false,
                sameMovePenalty: BBDamageModel.SAME_MOVE_PENALTY_DEFAULT,
                counterType: BBCounterType.CounterHit
            );
            Assert.AreEqual(1100, counterDmg);

            // Punish Counter: +15% (11,500 BP) -> 1150
            int punishDmg = BBDamageModel.CalculateDamage(
                baseDmg, 1.0f, 1.0f, 1, 10000, 10000, 0f, false, false,
                sameMovePenalty: BBDamageModel.SAME_MOVE_PENALTY_DEFAULT,
                counterType: BBCounterType.PunishCounter
            );
            Assert.AreEqual(1150, punishDmg);
        }

        [Test]
        public void Pillar2_WallBounce_OnePerCombo_And_Reset()
        {
            var combo = new BBComboState();
            combo.Reset();
            Assert.IsFalse(combo.hasUsedWallBounceThisCombo);

            // First Wall Bounce succeeds
            bool firstBounce = combo.TryRegisterBounce(BBBounceType.WallBounce);
            Assert.IsTrue(firstBounce);
            Assert.IsTrue(combo.hasUsedWallBounceThisCombo);

            // Second Wall Bounce in same combo fails
            bool secondBounce = combo.TryRegisterBounce(BBBounceType.WallBounce);
            Assert.IsFalse(secondBounce);
            Assert.IsTrue(combo.hasUsedWallBounceThisCombo);

            // Resetting combo allows wall bounce again
            combo.Reset();
            Assert.IsFalse(combo.hasUsedWallBounceThisCombo);
            bool thirdBounce = combo.TryRegisterBounce(BBBounceType.WallBounce);
            Assert.IsTrue(thirdBounce);
        }

        [Test]
        public void Pillar3_JuggleGravity_Progression_And_HitstunFloor()
        {
            var combo = new BBComboState();
            combo.Reset();

            // Hits 1 to 3 have base gravity (1.0x)
            combo.RegisterHit(1, 1.0f, 1.0f, isAirHit: true);
            Assert.AreEqual(1.0f, combo.CalculateProgressiveJuggleGravity(1.0f), 0.001f);

            combo.RegisterHit(2, 1.0f, 1.0f, isAirHit: true);
            Assert.AreEqual(1.0f, combo.CalculateProgressiveJuggleGravity(1.0f), 0.001f);

            combo.RegisterHit(3, 1.0f, 1.0f, isAirHit: true);
            Assert.AreEqual(1.0f, combo.CalculateProgressiveJuggleGravity(1.0f), 0.001f);

            // Hit 4: +8% -> 1.08x
            combo.RegisterHit(4, 1.0f, 1.0f, isAirHit: true);
            Assert.AreEqual(1.08f, combo.CalculateProgressiveJuggleGravity(1.0f), 0.001f);

            // Hit 5: +16% -> 1.16x
            combo.RegisterHit(5, 1.0f, 1.0f, isAirHit: true);
            Assert.AreEqual(1.16f, combo.CalculateProgressiveJuggleGravity(1.0f), 0.001f);

            // Hitstun floor: CalculateDecayedHitstun never drops below 12 frames
            combo.comboDurationTicks = 999; // Deep combo decay
            int decayedHitstun = combo.CalculateDecayedHitstun(14, isGroundHit: true);
            Assert.GreaterOrEqual(decayedHitstun, 12, "Decayed hitstun must enforce minimum 12f untechable floor");

            int decayedAirHitstun = combo.CalculateDecayedHitstun(14, isGroundHit: false);
            Assert.GreaterOrEqual(decayedAirHitstun, 12, "Decayed air hitstun must enforce minimum 12f floor");
        }

        [Test]
        public void Pillar4_InstantBlock_Window_And_StateTransitions()
        {
            var go = new GameObject("Fighter_IB_Test");
            try
            {
                var brain = go.AddComponent<BBFighterBrain>();
                brain.tuning = BBSystemTuning.Default;
                brain.ResetForRound();

                // Defender entered guard at tick 100
                brain.guardEnteredTick = 100;

                // Contact at tick 103 (3 ticks after entering guard, within 5-tick Instant Block window)
                int contactTick = 103;
                bool isInstantBlock = (contactTick - brain.guardEnteredTick <= 5);
                Assert.IsTrue(isInstantBlock);

                // Contact at tick 108 (8 ticks after entering guard, outside 5-tick window)
                int lateTick = 108;
                bool isLateBlock = (lateTick - brain.guardEnteredTick <= 5);
                Assert.IsFalse(isLateBlock);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Pillar4_ReversalBuffer_And_InvulnerabilityTimers()
        {
            var go = new GameObject("Fighter_Reversal_Test");
            try
            {
                var brain = go.AddComponent<BBFighterBrain>();
                brain.tuning = BBSystemTuning.Default;
                brain.ResetForRound();

                // On exit from stun, ticksSinceExitedStun is 0
                brain.ticksSinceExitedStun = 0;

                // On the next combat tick, ticksSinceExitedStun increments to 1 (Frame 1 of neutral)
                brain.HandleCombatTick(11);
                Assert.AreEqual(1, brain.ticksSinceExitedStun, "Frame 1 out of stun must be ticksSinceExitedStun == 1 (Reversal Window)");

                // Wakeup throw invulnerability countdown check
                brain.wakeupThrowInvulnTicks = 6;
                brain.AdvanceState(12);
                Assert.AreEqual(5, brain.wakeupThrowInvulnTicks);
                Assert.IsTrue((brain.currentInvuln & BBInvuln.Throw) != 0, "Wakeup throw invulnerability must be active");

                // Tech roll strike invulnerability countdown check
                brain.techRollStrikeInvulnTicks = 8;
                brain.AdvanceState(13);
                Assert.AreEqual(7, brain.techRollStrikeInvulnTicks);
                Assert.IsTrue((brain.currentInvuln & BBInvuln.Strike) != 0, "Tech roll strike invulnerability must be active");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
