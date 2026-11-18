using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Tunable configuration asset for BlazBlue combat mechanics.
    /// Never modifies Classic constants.
    /// </summary>
    [CreateAssetMenu(fileName = "BBSystemTuning", menuName = "DemonIdol/BlazBlue/SystemTuning")]
    public class BBSystemTuning : ScriptableObject
    {
        [Header("Input Buffer & Windows (Ticks)")]
        [Tooltip("How long a button press stays usable in the buffer")]
        public int bufferTicks = 5;

        [Tooltip("Max tick difference between buttons to form a multi-button chord / macro")]
        public int chordWindowTicks = 3;

        [Tooltip("Quarter-circle window (236, 214)")]
        public int motionWindow236 = 22;

        [Tooltip("Dragon punch window (623, 421)")]
        public int motionWindow623 = 22;

        [Tooltip("Half-circle window (41236, 63214)")]
        public int motionWindowHalfCircle = 30;

        [Tooltip("360 command grab window")]
        public int motionWindow360 = 40;

        [Tooltip("Distortion double quarter-circle and 632146 window")]
        public int motionWindowDD = 36;

        [Tooltip("Down-down window (22)")]
        public int motionWindow22 = 16;

        [Tooltip("Charge hold duration in ticks before release ([4]6, [2]8)")]
        public int chargeHoldTicks = 40;

        [Tooltip("Max release window to input the forward/up direction after charging")]
        public int chargeReleaseTicks = 12;

        [Tooltip("How long a negative edge release stays usable for specials")]
        public int negativeEdgeTicks = 3;

        [Tooltip("Allow 323 shortcut for DP motions")]
        public bool allow323Shortcut = true;

        [Tooltip("Stick deadzone for analog controls")]
        public float stickDeadzone = 0.20f;

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

        [Header("Gauge & Resource Tuning")]
        public int rapidCancelCost = 50;
        public int barrierMax = 1000;
        public float barrierDrainPerSecond = 15f;
        public float barrierPushbackMultiplier = 2.2f;
        public int heatMax = 100;
        public int burstMax = 1000;
        public float burstRadius = 3.5f;

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
