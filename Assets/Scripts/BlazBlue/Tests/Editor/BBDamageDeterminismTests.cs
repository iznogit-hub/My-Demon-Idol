#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBDamageDeterminismTests
    {
        [Test]
        public void ComboScaling_BoundaryRounding_And_Caps()
        {
            // First 2 hits: 100% (10,000 BP)
            Assert.AreEqual(10_000L, BBDamageModel.CalculateComboScalingBP(1));
            Assert.AreEqual(10_000L, BBDamageModel.CalculateComboScalingBP(2));
            Assert.AreEqual(1.0f, BBDamageModel.CalculateComboScaling(1), 0.0001f);
            Assert.AreEqual(1.0f, BBDamageModel.CalculateComboScaling(2), 0.0001f);

            // Hit 3: 90% (9,000 BP)
            Assert.AreEqual(9_000L, BBDamageModel.CalculateComboScalingBP(3));
            Assert.AreEqual(0.9f, BBDamageModel.CalculateComboScaling(3), 0.0001f);

            // Hit 4: 80% (8,000 BP)
            Assert.AreEqual(8_000L, BBDamageModel.CalculateComboScalingBP(4));

            // Hit 10: 20% (2,000 BP) floor
            Assert.AreEqual(2_000L, BBDamageModel.CalculateComboScalingBP(10));
            Assert.AreEqual(0.2f, BBDamageModel.CalculateComboScaling(10), 0.0001f);

            // Hits > 10: Hard floor at 20% (2,000 BP)
            Assert.AreEqual(2_000L, BBDamageModel.CalculateComboScalingBP(11));
            Assert.AreEqual(2_000L, BBDamageModel.CalculateComboScalingBP(25));
            Assert.AreEqual(2_000L, BBDamageModel.CalculateComboScalingBP(100));
        }

        [Test]
        public void SameMovePenalty_ExponentialDecay_IntegerQuantized()
        {
            float baseDmg = 1000f;

            // 1st use: 100%
            int dmg1 = BBDamageModel.CalculateDamage(baseDmg, 1.0f, 1.0f, 1, 10000, 10000, 0f);
            Assert.AreEqual(1000, dmg1);

            // 2nd use: 0.80x (8,000 BP) -> 800
            int dmg2 = BBDamageModel.CalculateDamage(baseDmg, 1.0f, 1.0f, 2, 10000, 10000, 0f);
            Assert.AreEqual(800, dmg2);

            // 3rd use: (8000 * 8000)/10000 = 6400 BP -> 640
            int dmg3 = BBDamageModel.CalculateDamage(baseDmg, 1.0f, 1.0f, 3, 10000, 10000, 0f);
            Assert.AreEqual(640, dmg3);

            // 4th use: (6400 * 8000)/10000 = 5120 BP -> 512
            int dmg4 = BBDamageModel.CalculateDamage(baseDmg, 1.0f, 1.0f, 4, 10000, 10000, 0f);
            Assert.AreEqual(512, dmg4);

            // 5th use: (5120 * 8000)/10000 = 4096 BP -> 409
            int dmg5 = BBDamageModel.CalculateDamage(baseDmg, 1.0f, 1.0f, 5, 10000, 10000, 0f);
            Assert.AreEqual(409, dmg5);
        }

        [Test]
        public void Guts_Threshold_ExactBoundaries()
        {
            int maxHp = 10000;

            // > 50%: 1.00x (10,000 BP)
            Assert.AreEqual(10_000L, BBDamageModel.CalculateGutsBP(5001, maxHp));
            Assert.AreEqual(1.00f, BBDamageModel.CalculateGuts(5001, maxHp), 0.0001f);

            // Exactly 50%: falls into tier 2 (0.90x / 9,000 BP)
            Assert.AreEqual(9_000L, BBDamageModel.CalculateGutsBP(5000, maxHp));
            Assert.AreEqual(0.90f, BBDamageModel.CalculateGuts(5000, maxHp), 0.0001f);

            // > 30%: 0.90x (9,000 BP)
            Assert.AreEqual(9_000L, BBDamageModel.CalculateGutsBP(3001, maxHp));

            // Exactly 30%: falls into tier 3 (0.80x / 8,000 BP)
            Assert.AreEqual(8_000L, BBDamageModel.CalculateGutsBP(3000, maxHp));
            Assert.AreEqual(0.80f, BBDamageModel.CalculateGuts(3000, maxHp), 0.0001f);

            // > 20%: 0.80x (8,000 BP)
            Assert.AreEqual(8_000L, BBDamageModel.CalculateGutsBP(2001, maxHp));

            // Exactly 20%: falls into tier 4 (0.70x / 7,000 BP)
            Assert.AreEqual(7_000L, BBDamageModel.CalculateGutsBP(2000, maxHp));
            Assert.AreEqual(0.70f, BBDamageModel.CalculateGuts(2000, maxHp), 0.0001f);

            // > 10%: 0.70x (7,000 BP)
            Assert.AreEqual(7_000L, BBDamageModel.CalculateGutsBP(1001, maxHp));

            // Exactly 10% and below: tier 5 (0.60x / 6,000 BP)
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(1000, maxHp));
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(500, maxHp));
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(1, maxHp));
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(0, maxHp));
            Assert.AreEqual(0.60f, BBDamageModel.CalculateGuts(0, maxHp), 0.0001f);
        }

        [Test]
        public void CalculateGutsBP_PureInteger_BoundaryCases()
        {
            const int maxHp = 10_000;

            // 100% HP: 10,000 BP (1.00x)
            Assert.AreEqual(10_000L, BBDamageModel.CalculateGutsBP(10_000, maxHp));

            // 50% HP threshold: exactly 50% = 9,000 BP (0.90x), 50.01% = 10,000 BP (1.00x)
            Assert.AreEqual(9_000L, BBDamageModel.CalculateGutsBP(5_000, maxHp));
            Assert.AreEqual(10_000L, BBDamageModel.CalculateGutsBP(5_001, maxHp));

            // 10% HP threshold: exactly 10% = 6,000 BP (0.60x), 10.01% = 7,000 BP (0.70x)
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(1_000, maxHp));
            Assert.AreEqual(7_000L, BBDamageModel.CalculateGutsBP(1_001, maxHp));

            // 1 HP: lowest positive integer -> 6,000 BP (0.60x)
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(1, maxHp));

            // 0 HP: dead/critical -> 6,000 BP (0.60x)
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(0, maxHp));

            // Overkill / negative current HP -> clamps safely to 0 HP -> 6,000 BP (0.60x)
            Assert.AreEqual(6_000L, BBDamageModel.CalculateGutsBP(-500, maxHp));

            // Zero or invalid max HP -> safe default 10,000 BP (1.00x)
            Assert.AreEqual(10_000L, BBDamageModel.CalculateGutsBP(100, 0));
            Assert.AreEqual(10_000L, BBDamageModel.CalculateGutsBP(100, -100));
        }

        [Test]
        public void Modifiers_Danger_And_Stylish_Combined()
        {
            float baseDmg = 1000f;

            // Neutral: 1000
            int neutral = BBDamageModel.CalculateDamage(baseDmg, 1f, 1f, 1, 10000, 10000, 0f, false, false);
            Assert.AreEqual(1000, neutral);

            // Danger (+25% -> 12,500 BP): 1250
            int danger = BBDamageModel.CalculateDamage(baseDmg, 1f, 1f, 1, 10000, 10000, 0f, isDefenderInDanger: true, isAttackerStylish: false);
            Assert.AreEqual(1250, danger);

            // Stylish (-20% -> 8,000 BP): 800
            int stylish = BBDamageModel.CalculateDamage(baseDmg, 1f, 1f, 1, 10000, 10000, 0f, isDefenderInDanger: false, isAttackerStylish: true);
            Assert.AreEqual(800, stylish);

            // Both (1.25 * 0.80 = 1.00): exactly 1000
            int both = BBDamageModel.CalculateDamage(baseDmg, 1f, 1f, 1, 10000, 10000, 0f, isDefenderInDanger: true, isAttackerStylish: true);
            Assert.AreEqual(1000, both);
        }

        [Test]
        public void MinimumDamageFloor_GuaranteesAuthoredFloor()
        {
            float baseDmg = 2500f; // Distortion Drive

            // Even under catastrophic scaling (P1 10%, P2 1%, repeated 10 times, deep guts 60%):
            int heavyDecayDmg = BBDamageModel.CalculateDamage(
                baseDmg,
                comboP1: 0.10f,
                accumulatedP2: 0.01f,
                moveUsesInCombo: 10,
                defenderCurrentHp: 500f,
                defenderMaxHp: 10000f,
                minDamagePct: 0.30f // DD 30% floor
            );

            // 2500 * 30% = 750 guaranteed
            Assert.AreEqual(750, heavyDecayDmg);

            // Minimum 1 damage on any contact
            int tinyDmg = BBDamageModel.CalculateDamage(10f, 0.1f, 0.01f, 10, 500, 10000, 0f);
            Assert.AreEqual(1, tinyDmg);
        }

        [Test]
        public void ChipDamage_Barrier_ZeroChip_And_MoveTiers()
        {
            float baseDmg = 1000f;

            // Barrier active: 100% chip negation for all move kinds
            Assert.AreEqual(0, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.Normal, isBarriering: true));
            Assert.AreEqual(0, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.DriveMove, isBarriering: true));
            Assert.AreEqual(0, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.Special, isBarriering: true));
            Assert.AreEqual(0, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.DistortionDrive, isBarriering: true));

            // Without barrier:
            // Normals: 0% chip
            Assert.AreEqual(0, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.Normal, isBarriering: false));

            // Drive Moves: 8% chip (800 BP) -> 80
            Assert.AreEqual(80, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.DriveMove, isBarriering: false));

            // Specials: 10% chip (1,000 BP) -> 100
            Assert.AreEqual(100, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.Special, isBarriering: false));

            // Distortion Drives: 15% chip (1,500 BP) -> 150
            Assert.AreEqual(150, BBDamageModel.CalculateChipDamage(baseDmg, BBMoveKind.DistortionDrive, isBarriering: false));

            // Fractional base damage truncation (1005 base):
            // Special: (1005 * 1000) / 10000 = 100
            // DD: (1005 * 1500) / 10000 = 150
            // Drive: (1005 * 800) / 10000 = 80
            Assert.AreEqual(100, BBDamageModel.CalculateChipDamage(1005f, BBMoveKind.Special, isBarriering: false));
            Assert.AreEqual(150, BBDamageModel.CalculateChipDamage(1005f, BBMoveKind.DistortionDrive, isBarriering: false));
            Assert.AreEqual(80, BBDamageModel.CalculateChipDamage(1005f, BBMoveKind.DriveMove, isBarriering: false));
        }

        [Test]
        public void CrossPlatform_BitIdentical_5HitComboChain()
        {
            // Starter: 5A (base 400, P1 0.70, P2 0.85, min 5%)
            // Hit 1: 5A
            int h1 = BBDamageModel.CalculateDamage(400f, 0.70f, 1.0f, 1, 10000, 10000, 0.05f);
            // 400 * 7000 / 10000 = 280
            Assert.AreEqual(280, h1);

            // Hit 2: 5B (base 800, P2 accumulated = 0.85, min 8%)
            int h2 = BBDamageModel.CalculateDamage(800f, 0.70f, 0.85f, 1, 9720, 10000, 0.08f);
            // Multiplier = (10000 * 7000 / 10000 * 8500 / 10000) = 5950 BP
            // 800 * 5950 / 10000 = 476
            Assert.AreEqual(476, h2);

            // Hit 3: 5C (base 1200, P2 accumulated = 0.85 * 0.89 = 0.7565, min 10%)
            int h3 = BBDamageModel.CalculateDamage(1200f, 0.70f, 0.7565f, 1, 9244, 10000, 0.10f);
            // Multiplier = 7000 * 7565 / 10000 = 5295 BP
            // 1200 * 5295 / 10000 = 635
            Assert.AreEqual(635, h3);

            // Hit 4: 2C repeated move (base 1000, P2 = 0.6960, uses = 2, min 10%)
            int h4 = BBDamageModel.CalculateDamage(1000f, 0.70f, 0.6960f, 2, 8609, 10000, 0.10f);
            // Multiplier = 7000 * 6960 / 10000 = 4872 BP; 4872 * 8000 / 10000 = 3897 BP
            // 1000 * 3897 / 10000 = 389
            Assert.AreEqual(389, h4);

            // Hit 5: Distortion Drive (base 2500, P2 = 0.6264, min 30%, in Danger)
            int h5 = BBDamageModel.CalculateDamage(2500f, 0.70f, 0.6264f, 1, 8220, 10000, 0.30f, isDefenderInDanger: true);
            // 7000 * 6264 / 10000 = 4384 BP; 4384 * 12500 / 10000 = 5480 BP
            // 2500 * 5480 / 10000 = 1370. Min floor 2500 * 0.30 = 750. 1370 > 750 -> 1370.
            Assert.AreEqual(1370, h5);
        }
    }
}
#endif
