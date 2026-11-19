using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    public enum BBDummyAction
    {
        Stand = 0,
        Crouch = 1,
        Jump = 2,
        NoGuard = 3,
        GuardAll = 4,
        GuardFirst = 5,
        ReversalDP = 6
    }

    public enum BBMeterMode
    {
        Normal = 0,
        InfiniteHeat = 1,
        InfiniteBarrier = 2,
        InfiniteBurst = 3,
        FullMeters = 4
    }

    /// <summary>
    /// Training Dojo Settings & Menu Controller for BlazBlue 4-Button (Spec §11.3, M12).
    /// Provides customizable dummy behaviors, overlay toggles, meter replenishment rules,
    /// and instant reset functionality.
    /// </summary>
    public class BBTrainingMenu : MonoBehaviour
    {
        private static BBTrainingMenu _instance;
        public static BBTrainingMenu Instance => _instance;

        [Header("Target Fighters")]
        public BBFighterBrain playerBrain;
        public BBFighterBrain dummyBrain;

        [Header("Dummy Settings")]
        public BBDummyAction dummyAction = BBDummyAction.Stand;
        public BBMeterMode meterMode = BBMeterMode.FullMeters;

        [Header("Overlay Toggles")]
        public bool showHitboxes = true;
        public bool showFrameMeter = true;
        public bool showInputDisplay = true;

        [Header("Saved State")]
        private Vector2 _savedP1Pos;
        private Vector2 _savedP2Pos;
        private bool _hasSavedState = false;

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            ApplyTrainingRules();
        }

        public void ApplyTrainingRules()
        {
            // 1. Meter replenish rules
            if (meterMode == BBMeterMode.FullMeters || meterMode == BBMeterMode.InfiniteHeat)
            {
                if (playerBrain != null) playerBrain.heatMeter = 100f;
                if (dummyBrain != null) dummyBrain.heatMeter = 100f;
            }

            if (meterMode == BBMeterMode.FullMeters || meterMode == BBMeterMode.InfiniteBarrier)
            {
                if (playerBrain != null) { playerBrain.barrierMeter = 1000f; playerBrain.inDanger = false; }
                if (dummyBrain != null) { dummyBrain.barrierMeter = 1000f; dummyBrain.inDanger = false; }
            }

            if (meterMode == BBMeterMode.FullMeters || meterMode == BBMeterMode.InfiniteBurst)
            {
                if (playerBrain != null && !playerBrain.inOverdrive) playerBrain.burstMeter = 1000f;
                if (dummyBrain != null && !dummyBrain.inOverdrive) dummyBrain.burstMeter = 1000f;
            }

            // Health regeneration in training mode
            if (dummyBrain != null && dummyBrain.currentHealth < 11000f && dummyBrain.hitstunRemaining <= 0)
            {
                dummyBrain.currentHealth = 11000f;
            }
        }

        public void ResetPositions()
        {
            if (playerBrain != null)
            {
                playerBrain.transform.position = _hasSavedState ? (Vector3)_savedP1Pos : new Vector3(-2.0f, 0f, 0f);
                playerBrain.currentHealth = 11000f;
                playerBrain.heatMeter = (meterMode == BBMeterMode.Normal) ? 0f : 100f;
                playerBrain.barrierMeter = 1000f;
                playerBrain.burstMeter = 1000f;
                playerBrain.inDanger = false;
            }

            if (dummyBrain != null)
            {
                dummyBrain.transform.position = _hasSavedState ? (Vector3)_savedP2Pos : new Vector3(2.0f, 0f, 0f);
                dummyBrain.currentHealth = 11000f;
                dummyBrain.heatMeter = (meterMode == BBMeterMode.Normal) ? 0f : 100f;
                dummyBrain.barrierMeter = 1000f;
                dummyBrain.burstMeter = 1000f;
                dummyBrain.inDanger = false;
            }
        }

        public void SaveState()
        {
            if (playerBrain != null) _savedP1Pos = playerBrain.transform.position;
            if (dummyBrain != null) _savedP2Pos = dummyBrain.transform.position;
            _hasSavedState = true;
        }

        public void SetDummyAction(BBDummyAction action)
        {
            dummyAction = action;
        }

        public void SetMeterMode(BBMeterMode mode)
        {
            meterMode = mode;
        }

        public void ToggleHitboxes(bool enabled)
        {
            showHitboxes = enabled;
            var overlay = FindFirstObjectByType<BBHitboxOverlay>();
            if (overlay != null) overlay.enabled = enabled;
        }

        public void ToggleFrameMeter(bool enabled)
        {
            showFrameMeter = enabled;
            var meter = FindFirstObjectByType<BBFrameMeter>();
            if (meter != null) meter.enabled = enabled;
        }
    }
}
