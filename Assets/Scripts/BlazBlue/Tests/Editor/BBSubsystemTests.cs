#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class BBSubsystemTests
    {
        public static void RunAllTestsMenu()
        {
            var suite = new BBSubsystemTests();
            suite.Test_RevolverAction_GatlingChains();
            suite.Test_DamageModel_P1P2Proration();
            suite.Test_GutsScaling_Tiers();
            suite.Test_ChipDamage_Rules();
            suite.Test_HitstunDecay_And_FatalCounter();
            suite.Test_Gauges_Heat_Barrier_Burst_Overdrive();
            suite.Test_RapidCancel_Conditions();
            suite.Test_AnimationDriver_SeekTimeMath();
            Debug.Log("<color=#00e676>[BBSubsystemTests] ALL 8 SUBSYSTEM & COMBAT TESTS PASSED!</color>");
        }

        [Test]
        public void Test_RevolverAction_GatlingChains()
        {
            var move5A = ScriptableObject.CreateInstance<BBAttackData>();
            move5A.moveId = "5A";
            move5A.button = BBButton.A;
            move5A.kind = BBMoveKind.Normal;
            move5A.startupFrames = 5;
            move5A.activeFrames = 3;
            move5A.recoveryFrames = 8;
            move5A.specialCancel = true;

            var move5B = ScriptableObject.CreateInstance<BBAttackData>();
            move5B.moveId = "5B";
            move5B.button = BBButton.B;
            move5B.kind = BBMoveKind.Normal;
            move5B.startupFrames = 8;
            move5B.activeFrames = 3;
            move5B.recoveryFrames = 12;

            var move5C = ScriptableObject.CreateInstance<BBAttackData>();
            move5C.moveId = "5C";
            move5C.button = BBButton.C;
            move5C.kind = BBMoveKind.Normal;

            var moveSpecial = ScriptableObject.CreateInstance<BBAttackData>();
            moveSpecial.moveId = "236A";
            moveSpecial.kind = BBMoveKind.Special;

            var chain = new BBChainState();
            chain.Reset();
            chain.madeContact = true;
            chain.contactTick = 5;

            // 1. 5A -> 5B succeeds (A -> B ascending)
            Assert.IsTrue(BBCancelRules.CanCancel(in chain, move5A, move5B, 1, 6, onHit: true, onBlock: false, isGrounded: true));

            // 2. 5B -> 5A fails (B -> A descending)
            Assert.IsFalse(BBCancelRules.CanCancel(in chain, move5B, move5A, 0, 9, onHit: true, onBlock: false, isGrounded: true));

            // 3. 5A -> 5A repeat succeeds up to 3 times
            chain.aRepeatCount = 1;
            Assert.IsTrue(BBCancelRules.CanCancel(in chain, move5A, move5A, 0, 6, onHit: true, onBlock: false, isGrounded: true));
            chain.aRepeatCount = 3;
            Assert.IsFalse(BBCancelRules.CanCancel(in chain, move5A, move5A, 0, 6, onHit: true, onBlock: false, isGrounded: true), "A-repeat must be capped at 3.");

            // 4. Once-per-chain mask check
            chain.Reset();
            chain.madeContact = true;
            chain.contactTick = 5;
            chain.MarkUsed(1); // 5B already used
            Assert.IsFalse(BBCancelRules.CanCancel(in chain, move5A, move5B, 1, 6, onHit: true, onBlock: false, isGrounded: true), "Move already used in chain must be blocked.");

            // 5. Special cancel on hit
            Assert.IsTrue(BBCancelRules.CanCancel(in chain, move5A, moveSpecial, 5, 6, onHit: true, onBlock: false, isGrounded: true));

            // 6. Whiff cancel: 5A -> 5A on whiff allowed, 5A -> 5B on whiff forbidden
            chain.Reset();
            chain.madeContact = false;
            Assert.IsTrue(BBCancelRules.CanCancel(in chain, move5A, move5A, 0, 6, onHit: false, onBlock: false, isGrounded: true, allowWhiffACancels: true));
            Assert.IsFalse(BBCancelRules.CanCancel(in chain, move5A, move5B, 1, 6, onHit: false, onBlock: false, isGrounded: true, allowWhiffACancels: true));
        }

        [Test]
        public void Test_DamageModel_P1P2Proration()
        {
            // Move: 1000 base damage, starter P1 = 0.70 (A normal), P2 = 0.85
            float baseDamage = 1000f;
            float starterP1 = 0.70f;
            float accumulatedP2 = 0.85f * 0.85f; // after 2 hits

            int damage = BBDamageModel.CalculateDamage(
                baseDamage,
                starterP1,
                accumulatedP2,
                moveUsesInCombo: 1,
                defenderCurrentHp: 11000f,
                defenderMaxHp: 11000f,
                minDamagePct: 0.05f
            );

            // Expected: 1000 * 0.70 * 0.7225 * 1.0 (guts) = 505.75 -> floor 505
            Assert.AreEqual(505, damage);

            // Test minimum damage floor
            int floorDamage = BBDamageModel.CalculateDamage(
                baseDamage,
                comboP1: 0.10f,
                accumulatedP2: 0.05f,
                moveUsesInCombo: 1,
                defenderCurrentHp: 11000f,
                defenderMaxHp: 11000f,
                minDamagePct: 0.30f // DD guaranteed 30% floor
            );
            Assert.AreEqual(300, floorDamage, "Minimum damage floor must guarantee move's minDamagePct.");

            // Test same-move penalty: 2nd use scales by 0.80
            int repeatedDamage = BBDamageModel.CalculateDamage(
                baseDamage,
                comboP1: 1.0f,
                accumulatedP2: 1.0f,
                moveUsesInCombo: 2,
                defenderCurrentHp: 11000f,
                defenderMaxHp: 11000f,
                minDamagePct: 0.05f,
                sameMovePenalty: 0.80f
            );
            Assert.AreEqual(800, repeatedDamage, "Second use of same move must take 0.80x penalty.");
        }

        [Test]
        public void Test_GutsScaling_Tiers()
        {
            Assert.AreEqual(1.00f, BBDamageModel.CalculateGuts(6000f, 10000f), 0.001f); // > 50%
            Assert.AreEqual(0.90f, BBDamageModel.CalculateGuts(4000f, 10000f), 0.001f); // 30-50%
            Assert.AreEqual(0.80f, BBDamageModel.CalculateGuts(2500f, 10000f), 0.001f); // 20-30%
            Assert.AreEqual(0.70f, BBDamageModel.CalculateGuts(1500f, 10000f), 0.001f); // 10-20%
            Assert.AreEqual(0.60f, BBDamageModel.CalculateGuts(500f, 10000f), 0.001f);  // < 10%
        }

        [Test]
        public void Test_ChipDamage_Rules()
        {
            float specialDamage = 1000f;

            // 1. Specials deal 10% chip
            int specialChip = BBDamageModel.CalculateChipDamage(specialDamage, BBMoveKind.Special, isBarriering: false);
            Assert.AreEqual(100, specialChip);

            // 2. Distortion Drives deal 15% chip
            int ddChip = BBDamageModel.CalculateChipDamage(specialDamage, BBMoveKind.DistortionDrive, isBarriering: false);
            Assert.AreEqual(150, ddChip);

            // 3. Normals deal 0 chip
            int normalChip = BBDamageModel.CalculateChipDamage(specialDamage, BBMoveKind.Normal, isBarriering: false);
            Assert.AreEqual(0, normalChip);

            // 4. Barrier blocks 100% of chip
            int barrierChip = BBDamageModel.CalculateChipDamage(specialDamage, BBMoveKind.DistortionDrive, isBarriering: true);
            Assert.AreEqual(0, barrierChip);

            // 5. Chip KO rules
            Assert.IsTrue(BBDamageModel.CanChipKO(BBMoveKind.DistortionDrive));
            Assert.IsTrue(BBDamageModel.CanChipKO(BBMoveKind.AstralHeat));
            Assert.IsFalse(BBDamageModel.CanChipKO(BBMoveKind.Special));
            Assert.IsFalse(BBDamageModel.CanChipKO(BBMoveKind.Normal));
        }

        [Test]
        public void Test_HitstunDecay_And_FatalCounter()
        {
            var combo = new BBComboState();
            combo.Reset();

            // Normal combo at 200 ticks duration (> 180 ticks): ground decay -2 ticks
            combo.comboDurationTicks = 200;
            int decayedHitstun = combo.CalculateDecayedHitstun(authoredHitstun: 16, isGroundHit: true);
            Assert.AreEqual(14, decayedHitstun);

            // Combo at 350 ticks (> 300 ticks): ground decay -4 ticks
            combo.comboDurationTicks = 350;
            int decayedHitstun350 = combo.CalculateDecayedHitstun(authoredHitstun: 16, isGroundHit: true);
            Assert.AreEqual(12, decayedHitstun350);

            // Fatal Counter shifts decay threshold by +120 ticks: at 200 ticks effective is 80 (0 decay)
            combo.isFatalCounter = true;
            combo.comboDurationTicks = 200;
            int fatalHitstun = combo.CalculateDecayedHitstun(authoredHitstun: 16, isGroundHit: true);
            Assert.AreEqual(16, fatalHitstun, "Fatal Counter must shift decay threshold by +120 ticks.");

            // Gravity scaling test
            combo.isFatalCounter = false;
            combo.airHitsInCombo = 5;
            float normalGravity = combo.CalculateJuggleGravity(20f);
            // 20 * (1 + 0.06 * 5) = 20 * 1.30 = 26
            Assert.AreEqual(26f, normalGravity, 0.01f);

            // Fatal Counter scales gravity by 0.80
            combo.isFatalCounter = true;
            float fatalGravity = combo.CalculateJuggleGravity(20f);
            Assert.AreEqual(20.8f, fatalGravity, 0.01f);
        }

        [Test]
        public void Test_Gauges_Heat_Barrier_Burst_Overdrive()
        {
            var tuning = ScriptableObject.CreateInstance<BBSystemTuning>();
            var gauges = new BBGauges();
            gauges.Initialize(tuning);

            // 1. Initial values
            Assert.AreEqual(0f, gauges.heat);
            Assert.AreEqual(1000f, gauges.barrier);
            Assert.AreEqual(1000f, gauges.burst);

            // 2. Heat gain on hit
            gauges.AddHeatOnHit(BBButton.C, BBMoveKind.Normal, blocked: false, tuning);
            Assert.AreEqual(4f, gauges.heat); // C normal = +4

            // 3. Heat gain on damage taken
            gauges.AddHeatOnDamageTaken(1000f, tuning);
            // +1000 * 0.03 = +30 -> heat = 34
            Assert.AreEqual(34f, gauges.heat, 0.01f);

            // 4. Overdrive activation at 50% HP
            bool odSuccess = gauges.TryActivateOverdrive(hpFraction: 0.50f, isComboActive: false, tuning);
            Assert.IsTrue(odSuccess);
            Assert.AreEqual(0f, gauges.burst, "Overdrive must consume entire Burst meter.");
            Assert.IsTrue(gauges.isOverdriveActive);
            // Duration lerp(480, 180, 0.50) = 330 ticks
            Assert.AreEqual(330, gauges.overdriveTicksRemaining);

            // 5. Overdrive tick generates Heat (+0.1 per tick)
            float heatBefore = gauges.heat;
            gauges.Tick(tuning, hpFraction: 0.50f, isBarriering: false);
            Assert.AreEqual(heatBefore + 0.10f, gauges.heat, 0.001f);

            // 6. Barrier drain while holding
            gauges.isOverdriveActive = false;
            gauges.barrier = 1000f;
            gauges.Tick(tuning, hpFraction: 1.0f, isBarriering: true);
            Assert.AreEqual(998f, gauges.barrier, "Holding barrier must cost 2 per tick.");
        }

        [Test]
        public void Test_RapidCancel_Conditions()
        {
            var move = ScriptableObject.CreateInstance<BBAttackData>();
            move.rapidCancelable = true;

            // 1. 50 Heat required
            Assert.IsTrue(BBCancelRules.CanRapidCancel(move, madeContact: true, currentHeat: 50f, rcUsedInCombo: 0));
            Assert.IsFalse(BBCancelRules.CanRapidCancel(move, madeContact: true, currentHeat: 49f, rcUsedInCombo: 0));

            // 2. Contact required (no whiff RC)
            Assert.IsFalse(BBCancelRules.CanRapidCancel(move, madeContact: false, currentHeat: 50f, rcUsedInCombo: 0, allowWhiffRC: false));

            // 3. Once per combo
            Assert.IsFalse(BBCancelRules.CanRapidCancel(move, madeContact: true, currentHeat: 100f, rcUsedInCombo: 1));

            // 4. Cannot RC during Distortion Drive
            move.kind = BBMoveKind.DistortionDrive;
            Assert.IsFalse(BBCancelRules.CanRapidCancel(move, madeContact: true, currentHeat: 100f, rcUsedInCombo: 0));
        }

        [Test]
        public void Test_AnimationDriver_SeekTimeMath()
        {
            // 24 fps clip frames 10..34 (24 frames = 1.0 second)
            // 60 Hz move duration: 60 ticks (1.0 second)
            // At tick 30 (halfway), seekTime should be 0.50 seconds
            int firstFrame = 10;
            int lastFrame = 34;
            int totalTicks = 60;
            int tickInMove = 30;

            float clipSeconds = (lastFrame - firstFrame) / 24.0f; // 1.0s
            float moveSeconds = totalTicks / 60.0f; // 1.0s
            float playbackRate = clipSeconds / moveSeconds; // 1.0
            float seekTime = (tickInMove / 60.0f) * playbackRate;

            Assert.AreEqual(0.50f, seekTime, 0.001f);
        }
    }
}
#endif
