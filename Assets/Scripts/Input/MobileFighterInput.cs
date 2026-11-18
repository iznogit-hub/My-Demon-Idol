using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Three-Button Input Adapter for 2.5D Combat.
    /// Supports Joystick (neutral/8 directions, 0.20 deadzone, 0.15 exit threshold)
    /// and 3 Attack Buttons: LIGHT (L), HEAVY (H), SKILL (S).
    /// Enforces strict same-tick physical chords (L+H=Throw, H+S=Super, L+S=Burst, L+H+S=Suppress).
    /// Provides optional mobile single-finger slide gesture mode with release-to-commit.
    /// </summary>
    public class MobileFighterInput : MonoBehaviour
    {
        [Header("Target Fighter")]
        [SerializeField] private FighterController _fighter;

        [Header("Joystick Deadzone Tuning (Editable)")]
        [Range(0.05f, 0.40f)] public float joystickDeadZone = 0.20f;
        [Range(0.05f, 0.30f)] public float joystickExitThreshold = 0.15f;

        [Header("Touch & Virtual Controls")]
        [SerializeField] private Vector2 _rawVirtualJoystick = Vector2.zero;
        public bool isGestureModeEnabled = false; // Optional single-finger slide gesture mode

        // Single-frame touch triggers
        private bool _touchLightPressed = false;
        private bool _touchHeavyPressed = false;
        private bool _touchSkillPressed = false;
        private bool _touchThrowPressed = false;
        private bool _touchSuperPressed = false;
        private bool _touchBurstPressed = false;
        private bool _touchGrabBreakPressed = false;

        // Double-tap forward (66) tracking for Command Grab
        private int _lastForwardTapTick = -999;
        private bool _prevForwardInput = false;

        // Gesture Mode State Tracking
        private ThreeButtonCommand _gesturePreviewCommand = ThreeButtonCommand.None;
        private bool _isGesturePointerDown = false;
        public ThreeButtonCommand ActiveGesturePreview => _gesturePreviewCommand;

        private void Awake()
        {
            if (_fighter == null)
            {
                _fighter = GetComponent<FighterController>();
            }
        }

        private void Update()
        {
            if (_fighter == null || !_fighter.isPlayerControlled) return;

            int currentTick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : 0;

            // 1. Gather & Quantize Joystick Input
            Vector2 rawInput = GatherRawDirection();
            Vector2 quantizedDir = QuantizeDirection(rawInput);
            _fighter.SetMoveInput(quantizedDir);

            // 2. Strict Same-Tick Edge Sampling for 3 Buttons & Chords
            SampleAndDispatchCommands(currentTick, quantizedDir);
        }

        private Vector2 GatherRawDirection()
        {
            Vector2 keyboardDir = Vector2.zero;
            if (SafeGetKey(KeyCode.A) || SafeGetKey(KeyCode.LeftArrow)) keyboardDir.x -= 1f;
            if (SafeGetKey(KeyCode.D) || SafeGetKey(KeyCode.RightArrow)) keyboardDir.x += 1f;
            if (SafeGetKey(KeyCode.S) || SafeGetKey(KeyCode.DownArrow)) keyboardDir.y -= 1f;
            if (SafeGetKey(KeyCode.W) || SafeGetKey(KeyCode.UpArrow)) keyboardDir.y += 1f;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keyboardDir.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keyboardDir.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keyboardDir.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keyboardDir.y += 1f;
            }

            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                Vector2 dpad = gamepad.dpad.ReadValue();
                if (stick.sqrMagnitude > 0.04f) keyboardDir += stick;
                if (dpad.sqrMagnitude > 0.04f) keyboardDir += dpad;
            }
#endif

            return (_rawVirtualJoystick != Vector2.zero) ? _rawVirtualJoystick : keyboardDir;
        }

        private static bool SafeGetKey(KeyCode key)
        {
            try { return UnityEngine.Input.GetKey(key); }
            catch { return false; }
        }

        private static bool SafeGetKeyDown(KeyCode key)
        {
            try { return UnityEngine.Input.GetKeyDown(key); }
            catch { return false; }
        }

        public Vector2 QuantizeDirection(Vector2 raw)
        {
            float mag = raw.magnitude;
            if (mag < joystickDeadZone)
            {
                return Vector2.zero;
            }

            // Normalize and quantize to 8 directions
            float angle = Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // 8-Way Sectors: 0=E, 45=NE, 90=N, 135=NW, 180=W, 225=SW, 270=S, 315=SE
            if (angle >= 337.5f || angle < 22.5f) return new Vector2(1f, 0f);
            if (angle >= 22.5f && angle < 67.5f) return new Vector2(1f, 1f);
            if (angle >= 67.5f && angle < 112.5f) return new Vector2(0f, 1f);
            if (angle >= 112.5f && angle < 157.5f) return new Vector2(-1f, 1f);
            if (angle >= 157.5f && angle < 202.5f) return new Vector2(-1f, 0f);
            if (angle >= 202.5f && angle < 247.5f) return new Vector2(-1f, -1f);
            if (angle >= 247.5f && angle < 292.5f) return new Vector2(0f, -1f);
            return new Vector2(1f, -1f);
        }

        private void SampleAndDispatchCommands(int currentTick, Vector2 direction)
        {
            // Sample physical key edges
            bool pressL = SafeGetKeyDown(KeyCode.J) || _touchLightPressed;
            bool pressH = SafeGetKeyDown(KeyCode.K) || _touchHeavyPressed;
            bool pressS = SafeGetKeyDown(KeyCode.L) || _touchSkillPressed;

            // Dedicated / remapped shortcut keys
            bool pressThrowKey = SafeGetKeyDown(KeyCode.Space) || SafeGetKeyDown(KeyCode.T) || _touchThrowPressed;
            bool pressSuperKey = SafeGetKeyDown(KeyCode.O) || SafeGetKeyDown(KeyCode.U) || _touchSuperPressed;
            bool pressBurstKey = SafeGetKeyDown(KeyCode.B) || _touchBurstPressed || _touchGrabBreakPressed;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                pressL = pressL || kb.jKey.wasPressedThisFrame;
                pressH = pressH || kb.kKey.wasPressedThisFrame;
                pressS = pressS || kb.lKey.wasPressedThisFrame;
                pressThrowKey = pressThrowKey || kb.spaceKey.wasPressedThisFrame || kb.tKey.wasPressedThisFrame;
                pressSuperKey = pressSuperKey || kb.uKey.wasPressedThisFrame || kb.oKey.wasPressedThisFrame;
                pressBurstKey = pressBurstKey || kb.bKey.wasPressedThisFrame;
            }

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
            {
                // Face buttons: X/Square = Projectile (P), Y/Triangle = Melee (M), B/Circle = Special (S), A/Cross = Throw/Grab
                pressL = pressL || pad.xButton.wasPressedThisFrame;
                pressH = pressH || pad.yButton.wasPressedThisFrame;
                pressS = pressS || pad.bButton.wasPressedThisFrame;
                pressThrowKey = pressThrowKey || pad.aButton.wasPressedThisFrame;

                // Shoulder & Triggers: Right Bumper / Trigger = Super/Ultra, Left Bumper / Trigger = Grab Break / Burst
                pressSuperKey = pressSuperKey || pad.rightShoulder.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame;
                pressBurstKey = pressBurstKey || pad.leftShoulder.wasPressedThisFrame || pad.leftTrigger.wasPressedThisFrame;
            }
#endif

            // Clear one-frame touch triggers
            _touchLightPressed = false;
            _touchHeavyPressed = false;
            _touchSkillPressed = false;
            _touchThrowPressed = false;
            _touchSuperPressed = false;
            _touchBurstPressed = false;
            _touchGrabBreakPressed = false;

            // 66 Command Grab Detection (Double-tap forward at close range <= 1.4m)
            int facing = _fighter != null ? _fighter.FacingDirection : 1;
            bool isForwardHeld = (direction.x * facing) > 0.45f;
            bool forwardTapped = isForwardHeld && !_prevForwardInput;
            _prevForwardInput = isForwardHeld;

            bool is66CommandGrab = false;
            if (forwardTapped)
            {
                if (currentTick - _lastForwardTapTick <= 15) // ~250ms double-tap window
                {
                    float dist = (_fighter != null && _fighter.opponent != null) 
                        ? Mathf.Abs(_fighter.transform.position.x - _fighter.opponent.transform.position.x) 
                        : 2.0f;
                    if (dist <= 1.4f)
                    {
                        is66CommandGrab = true;
                    }
                }
                _lastForwardTapTick = currentTick;
            }

            // CHORD RESOLUTION LOGIC: Strict same-tick sampling before dispatch
            // Rule 1: Three-Button Ultra Chord (P + M + S) or dedicated Super/Ultra macro
            // Frame 1 invincible interrupt that stops any opponent action!
            if (pressSuperKey || (pressL && pressH && pressS))
            {
                DispatchCommand(ThreeButtonCommand.Ultra, currentTick, direction);
                return;
            }

            // Rule 2: Grab Break Tech Chord (P + M) or dedicated Burst/Break button
            // Breaks command grab during 12-frame tech escape window
            if (pressBurstKey || (pressL && pressH))
            {
                DispatchCommand(ThreeButtonCommand.GrabBreak, currentTick, direction);
                return;
            }

            // Rule 3: Command Grab (66 double-tap forward at close range or dedicated grab button)
            if (pressThrowKey || is66CommandGrab)
            {
                DispatchCommand(ThreeButtonCommand.CommandGrab, currentTick, direction);
                return;
            }

            // Rule 4: Single Button Dispatches (only if no chord was formed)
            if (pressL)
            {
                DispatchCommand(ThreeButtonCommand.Projectile, currentTick, direction);
                return;
            }

            if (pressH)
            {
                DispatchCommand(ThreeButtonCommand.Melee, currentTick, direction);
                return;
            }

            if (pressS)
            {
                DispatchCommand(ThreeButtonCommand.Special, currentTick, direction);
                return;
            }
        }

        private void DispatchCommand(ThreeButtonCommand cmd, int tick, Vector2 direction)
        {
            if (_fighter != null && _fighter.InputBuf != null)
            {
                _fighter.InputBuf.Enqueue(cmd, tick, direction);
            }
        }

        private void OnDisable()
        {
            _rawVirtualJoystick = Vector2.zero;
            _gesturePreviewCommand = ThreeButtonCommand.None;
            _isGesturePointerDown = false;
            if (_fighter != null)
            {
                _fighter.SetMoveInput(Vector2.zero);
                if (_fighter.InputBuf != null) _fighter.InputBuf.Clear();
            }
        }

        #region Public Methods for Mobile UI Controls

        public void SetVirtualJoystick(Vector2 direction)
        {
            if (!enabled || _fighter == null)
            {
                _rawVirtualJoystick = Vector2.zero;
                return;
            }
            _rawVirtualJoystick = direction;
        }

        public void OnLightAttackPressed()
        {
            if (!enabled) return;
            if (isGestureModeEnabled) { StartGesture(ThreeButtonCommand.Light); return; }
            _touchLightPressed = true;
        }

        public void OnHeavyAttackPressed()
        {
            if (!enabled) return;
            if (isGestureModeEnabled) { StartGesture(ThreeButtonCommand.Heavy); return; }
            _touchHeavyPressed = true;
        }

        public void OnDriveSkillPressed()
        {
            if (!enabled) return;
            if (isGestureModeEnabled) { StartGesture(ThreeButtonCommand.Skill); return; }
            _touchSkillPressed = true;
        }

        public void OnThrowPressed()
        {
            if (!enabled) return;
            _touchThrowPressed = true;
        }

        public void OnDomainSuperPressed()
        {
            if (!enabled) return;
            _touchSuperPressed = true;
        }

        public void OnBurstPressed()
        {
            if (!enabled) return;
            _touchBurstPressed = true;
        }

        // 3-Button Master Action Bindings (Projectile, Melee, Special, Ultra, Grab, Break)
        public void OnProjectilePressed() => OnLightAttackPressed();
        public void OnMeleePressed() => OnHeavyAttackPressed();
        public void OnSpecialPressed() => OnDriveSkillPressed();
        public void OnUltraPressed() => OnDomainSuperPressed();
        public void OnGrabBreakPressed()
        {
            if (!enabled) return;
            _touchGrabBreakPressed = true;
            _touchBurstPressed = true;
        }
        public void OnCommandGrabPressed() => OnThrowPressed();

        // Legacy / accessibility adapters for TouchVirtualControls
        public void OnMediumAttackPressed() => OnHeavyAttackPressed();
        public void OnParryPressed() { if (_fighter != null) _fighter.ExecuteParry(); }
        public void OnBackdashPressed() { if (_fighter != null) _fighter.ExecuteBackdash(); }
        public void OnBarrierHoldStart() { if (_fighter != null && _fighter.Barrier != null) _fighter.Barrier.ActivateBarrier(); }
        public void OnBarrierHoldEnd() { if (_fighter != null && _fighter.Barrier != null) _fighter.Barrier.DeactivateBarrier(); }
        public void OnSidestepUpPressed() { if (_fighter != null) _fighter.ExecuteSidestep(1f); }
        public void OnSidestepDownPressed() { if (_fighter != null) _fighter.ExecuteSidestep(-1f); }

        #endregion

        #region Gesture Mode (Slide-to-Chord with Release-to-Commit)

        public void StartGesture(ThreeButtonCommand initialButton)
        {
            _isGesturePointerDown = true;
            _gesturePreviewCommand = initialButton;
        }

        public void UpdateGestureSlide(ThreeButtonCommand hoveredButton)
        {
            if (!_isGesturePointerDown) return;

            // Sliding between buttons selects chord preview
            if ((_gesturePreviewCommand == ThreeButtonCommand.Light && hoveredButton == ThreeButtonCommand.Heavy) ||
                (_gesturePreviewCommand == ThreeButtonCommand.Heavy && hoveredButton == ThreeButtonCommand.Light))
            {
                _gesturePreviewCommand = ThreeButtonCommand.Throw;
            }
            else if ((_gesturePreviewCommand == ThreeButtonCommand.Heavy && hoveredButton == ThreeButtonCommand.Skill) ||
                     (_gesturePreviewCommand == ThreeButtonCommand.Skill && hoveredButton == ThreeButtonCommand.Heavy))
            {
                _gesturePreviewCommand = ThreeButtonCommand.Super;
            }
            else if ((_gesturePreviewCommand == ThreeButtonCommand.Light && hoveredButton == ThreeButtonCommand.Skill) ||
                     (_gesturePreviewCommand == ThreeButtonCommand.Skill && hoveredButton == ThreeButtonCommand.Light))
            {
                _gesturePreviewCommand = ThreeButtonCommand.Burst;
            }
        }

        public void CommitGesture()
        {
            if (!_isGesturePointerDown) return;
            _isGesturePointerDown = false;

            int tick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : 0;
            Vector2 dir = QuantizeDirection(GatherRawDirection());

            if (_gesturePreviewCommand != ThreeButtonCommand.None)
            {
                DispatchCommand(_gesturePreviewCommand, tick, dir);
            }

            _gesturePreviewCommand = ThreeButtonCommand.None;
        }

        public void CancelGesture()
        {
            _isGesturePointerDown = false;
            _gesturePreviewCommand = ThreeButtonCommand.None;
        }

        #endregion
    }
}
