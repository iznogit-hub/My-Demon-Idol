using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Combat
{
    public enum TrainingResetPosition
    {
        Center,
        LeftCorner,
        RightCorner
    }

    /// <summary>
    /// Practice Dojo Lab Director.
    /// Provides position resets (Center, Left Corner, Right Corner),
    /// dummy state overrides (Block All, Random Guard, Counter-Hit, Reversal DP),
    /// and life/meter refill management.
    /// Supports hotkeys: [F1] Center Reset, [F2] Corner Reset, [F3] Toggle Frame Data, [F4] Toggle Hitboxes.
    /// </summary>
    public class PracticeDojoController : MonoBehaviour
    {
        public static PracticeDojoController Instance { get; private set; }

        [Header("Fighter References")]
        [SerializeField] private FighterController _player;
        [SerializeField] private FighterController _dummy;
        [SerializeField] private TrainingDummyAI _dummyAi;

        [Header("Lab Preferences")]
        public bool autoRefillHealth = true;
        public bool autoRefillSuperMeter = true;
        public int dummyRecoveryActionIndex = 0; // 0=None, 1=Reversal Light, 2=Reversal DP/Super, 3=Backdash

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindFightersIfMissing();
        }

        private void Update()
        {
            FindFightersIfMissing();

            // Lab Hotkeys
            HandleHotkeys();
        }

        private void FindFightersIfMissing()
        {
            if (_player == null || _dummy == null)
            {
                if (HomegrownCombatDirector.Instance != null)
                {
                    if (_player == null) _player = HomegrownCombatDirector.Instance.Fighter1;
                    if (_dummy == null) _dummy = HomegrownCombatDirector.Instance.Fighter2;
                }
                if (_player == null || _dummy == null)
                {
                    var fighters = FindObjectsByType<FighterController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (fighters != null && fighters.Length >= 2)
                    {
                        Array.Sort(fighters, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                        if (_player == null) _player = fighters[0];
                        if (_dummy == null) _dummy = fighters[1];
                    }
                }
            }

            if (_dummy != null && _dummyAi == null)
            {
                _dummyAi = _dummy.GetComponent<TrainingDummyAI>();
            }
        }

        private void HandleHotkeys()
        {
            // Reset Positions
            if (SafeGetKeyDown(KeyCode.F1) || SafeGetKeyDown(KeyCode.R))
            {
                ResetPositions(TrainingResetPosition.Center);
            }
            else if (SafeGetKeyDown(KeyCode.F2))
            {
                ResetPositions(TrainingResetPosition.LeftCorner);
            }

            // Toggle Lab HUD Visuals
            if (SafeGetKeyDown(KeyCode.F3))
            {
                DemonIdol.BlazBlue.BBFrameMeter.IsMeterVisible = !DemonIdol.BlazBlue.BBFrameMeter.IsMeterVisible;
            }
            if (SafeGetKeyDown(KeyCode.H) || SafeGetKeyDown(KeyCode.F4))
            {
                DemonIdol.BlazBlue.BBHitboxOverlay.IsOverlayActive = !DemonIdol.BlazBlue.BBHitboxOverlay.IsOverlayActive;
            }
        }

        private static bool SafeGetKeyDown(KeyCode key)
        {
            try { return UnityEngine.Input.GetKeyDown(key); }
            catch { return false; }
        }

        public void ResetPositions(TrainingResetPosition pos)
        {
            FindFightersIfMissing();
            if (_player == null || _dummy == null) return;

            Vector3 p1Pos = Vector3.zero;
            Vector3 p2Pos = Vector3.zero;

            switch (pos)
            {
                case TrainingResetPosition.Center:
                    p1Pos = new Vector3(-2.2f, 0, 0);
                    p2Pos = new Vector3(2.2f, 0, 0);
                    break;
                case TrainingResetPosition.LeftCorner:
                    p1Pos = new Vector3(-9.5f, 0, 0);
                    p2Pos = new Vector3(-7.5f, 0, 0);
                    break;
                case TrainingResetPosition.RightCorner:
                    p1Pos = new Vector3(7.5f, 0, 0);
                    p2Pos = new Vector3(9.5f, 0, 0);
                    break;
            }

            _player.transform.position = p1Pos;
            RefillOnReset(_player);
            _player.SetFacing(1);

            _dummy.transform.position = p2Pos;
            RefillOnReset(_dummy);
            _dummy.SetFacing(-1);

            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.ReturnToBaseCombatFrame();
                FighterCamera2D.Instance.ResetTrauma();
            }
        }

        private static void RefillOnReset(FighterController fighter)
        {
            if (fighter == null) return;
            fighter.ResetToIdle();
            var brain = fighter.GetComponent<DemonIdol.BlazBlue.BBFighterBrain>();
            if (brain != null)
            {
                brain.ResetForRound();
            }
        }
    }
}
