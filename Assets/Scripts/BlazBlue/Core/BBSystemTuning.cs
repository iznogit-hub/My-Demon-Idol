using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Tunable configuration asset for BlazBlue combat mechanics.
    /// Strictly adheres to section 4.21 of the specification.
    /// Never modifies Classic constants.
    /// </summary>
    [CreateAssetMenu(fileName = "BBSystemTuning", menuName = "DemonIdol/BlazBlue/System Tuning")]
    public class BBSystemTuning : ScriptableObject
    {
        [Header("Input")]
        public int bufferTicks = 5;
        public int chordWindowTicks = 3;
        public int negativeEdgeTicks = 3;
        public int motionWindow236 = 22;
        public int motionWindow623 = 22;
        public int motionWindowHalfCircle = 30;
        public int motionWindow360 = 40;
        public int motionWindowDD = 36;
        public int motionWindow22 = 16;
        public int chargeHoldTicks = 40;
        public int chargeReleaseTicks = 12;
        public float stickDeadzone = 0.20f;
        public bool allow323Shortcut = true;

        [Header("Heat")]
        public int heatMax = 100;
        public int costRC = 50;
        public int costDD = 50;
        public int costCA = 50;
        public int costCT = 25;
        public int costAstral = 100;
        public float heatOnHitA = 2;
        public float heatOnHitB = 3;
        public float heatOnHitC = 4;
        public float heatOnHitD = 3;
        public float heatOnHitSpecial = 5;
        public float heatPerDamageTaken = 0.03f;
        public float lowHpThreshold = 0.35f;
        public float lowHpRegenPerTick = 0.05f;

        [Header("Barrier")]
        public int barrierMax = 1000;
        public int barrierHoldCost = 2;
        public int barrierRegen = 3;
        public int barrierRegenDelay = 30;
        public int dangerTicks = 300;
        public float barrierDrainPerSecond = 15f;
        public float barrierHitCostPerDamage = 0.5f;
        public float barrierPushback = 2.2f;
        public float instantBarrierPushback = 2.6f;
        public float dangerDamageScale = 1.25f;
        public int instantBlockWindow = 8;

        [Header("Burst / Overdrive")]
        public int burstMax = 1000;
        public int burstRegenPerTick = 1;
        public int burstInvulnTicks = 26;
        public float burstRadius = 1.8f;
        public int odMinTicks = 180;
        public int odMaxTicks = 480;
        public float activeODPenalty = 0.6f;

        [Header("Rapid Cancel")]
        public bool allowWhiffRC = false;
        public int rcSlowTicks = 20;
        public float rcSlowFactor = 0.75f;
        public int rcPerCombo = 1;
        public int rapidCancelCost = 50;

        [Header("Throw")]
        public int throwTechTicks = 13;
        public int throwTechTicksCH = 6;
        public int throwWhiffRecovery = 24;
        public int throwProtectTicks = 6;
        public float throwRange = 1.0f;

        [Header("Combo")]
        public float sameMovePenalty = 0.80f;
        public float gravityScalePerHit = 0.06f;
        public float gravityCap = 1.9f;
        public int[] hitstunDecayThresholds = { 180, 300, 420, 540 };
        public int[] hitstunDecayGround = { 0, 2, 4, 6, 8 };
        public int[] hitstunDecayAir = { 0, 6, 12, 18, 24 };
        public int fatalCounterDecayShift = 120;
        public float fatalCounterGravity = 0.8f;
        public float counterHitHitstunScale = 1.5f;

        [Header("Optional")]
        public bool enableAstral = true;
        public bool negativePenalty = false;
        public float stylishDamageScale = 0.8f;

        [Header("Locomotion Defaults")]
        public float walkForwardSpeed = 3.6f;
        public float walkBackwardSpeed = 2.8f;
        public float dashSpeed = 6.5f;
        public int dashDurationTicks = 12;
        public float backdashSpeed = 7.0f;
        public int backdashDurationTicks = 14;
        public int backdashInvulnTicks = 5;
        public float jumpVerticalLaunch = 8.0f;
        public float jumpHorizontalSpeed = 3.2f;
        public float gravity = 20.0f;
        public int jumpPrejumpTicks = 4;

        private static BBSystemTuning _defaultInstance;
        public static BBSystemTuning Default
        {
            get
            {
                if (_defaultInstance == null)
                {
                    _defaultInstance = CreateInstance<BBSystemTuning>();
                }
                return _defaultInstance;
            }
        }
    }
}
