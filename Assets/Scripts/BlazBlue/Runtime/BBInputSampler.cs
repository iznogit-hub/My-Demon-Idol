using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Samples player hardware devices (Keyboard, Gamepad, Touch) every render frame,
    /// accumulating rising/falling edges and stick values.
    /// Emits exactly one clean, deterministic BBInputFrame per 60 Hz combat tick.
    /// </summary>
    public class BBInputSampler : MonoBehaviour
    {
        public bool isPlayerControlled = true;
        public int playerIndex = 0; // 0 = P1, 1 = P2

        [Header("Virtual Touch Controls Override")]
        public Vector2 virtualStickInput = Vector2.zero;
        public byte virtualHeldMask = 0;
        public byte virtualPressedMask = 0;
        public byte virtualReleasedMask = 0;

        // Accumulated edges between combat ticks
        private byte _latchedPressed = 0;
        private byte _latchedReleased = 0;
        private Vector2 _currentStick = Vector2.zero;
        private byte _currentHeld = 0;

        /// <summary>
        /// Cleans opposing directional inputs according to standard tournament fighting game SOCD rules:
        /// Left + Right = Neutral (x = 0).
        /// Down + Up = Up (y = 1) (Standard fighting game SOCD priority).
        /// </summary>
        public static Vector2 CleanSOCD(bool up, bool down, bool left, bool right)
        {
            float x = 0f;
            if (left && right) x = 0f;
            else if (left) x = -1f;
            else if (right) x = 1f;

            float y = 0f;
            if (down && up) y = 1f;
            else if (up) y = 1f;
            else if (down) y = -1f;

            return new Vector2(x, y);
        }

        private void Update()
        {
            if (!isPlayerControlled) return;

            SampleHardware();
        }

        private void SampleHardware()
        {
            // Input hygiene: strictly ignore and zero inputs when paused, during round intro countdown, or after KO
            if (Time.timeScale == 0f || (MatchRoundReferee.Instance != null && MatchRoundReferee.Instance.State != MatchState.Fighting))
            {
                ResetLatches();
                return;
            }

            Vector2 stick = Vector2.zero;
            byte held = 0;

            // 1. Keyboard Input
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (playerIndex == 0)
                {
                    // P1 Directions: WASD with tournament SOCD cleaning
                    stick = CleanSOCD(kb.wKey.isPressed, kb.sKey.isPressed, kb.aKey.isPressed, kb.dKey.isPressed);

                    // P1 Buttons: J=A, K=B, L=C, Space/I=D, T=Taunt
                    if (kb.jKey.isPressed) held |= (byte)BBButton.A;
                    if (kb.kKey.isPressed) held |= (byte)BBButton.B;
                    if (kb.lKey.isPressed) held |= (byte)BBButton.C;
                    if (kb.spaceKey.isPressed || kb.iKey.isPressed) held |= (byte)BBButton.D;
                    if (kb.tKey.isPressed) held |= (byte)BBButton.Taunt;

                    // P1 Keyboard Macros:
                    // U = A+B (Crush Trigger / Barrier)
                    if (kb.uKey.isPressed) held |= (byte)(BBButton.A | BBButton.B);
                    // O = B+C (Throw)
                    if (kb.oKey.isPressed) held |= (byte)(BBButton.B | BBButton.C);
                    // P = A+B+C+D (Burst / Overdrive Unified Macro)
                    if (kb.pKey.isPressed) held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
                    // Semicolon (;) = A+B+C+D (Overdrive)
                    if (kb.semicolonKey.isPressed) held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
                }
                else
                {
                    // P2 Directions: Arrow Keys with tournament SOCD cleaning
                    stick = CleanSOCD(kb.upArrowKey.isPressed, kb.downArrowKey.isPressed, kb.leftArrowKey.isPressed, kb.rightArrowKey.isPressed);

                    // P2 Buttons: Numpad 1=A, Numpad 2=B, Numpad 3=C, Numpad 0/Enter=D
                    if (kb.numpad1Key.isPressed) held |= (byte)BBButton.A;
                    if (kb.numpad2Key.isPressed) held |= (byte)BBButton.B;
                    if (kb.numpad3Key.isPressed) held |= (byte)BBButton.C;
                    if (kb.numpad0Key.isPressed || kb.numpadEnterKey.isPressed) held |= (byte)BBButton.D;

                    // P2 Macros:
                    // Numpad 4 = A+B (Crush Trigger / Barrier)
                    if (kb.numpad4Key.isPressed) held |= (byte)(BBButton.A | BBButton.B);
                    // Numpad 5 = B+C (Throw)
                    if (kb.numpad5Key.isPressed) held |= (byte)(BBButton.B | BBButton.C);
                    // Numpad 6 = A+B+C+D (Burst / Overdrive Unified Macro)
                    if (kb.numpad6Key.isPressed) held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
                    // Numpad Plus = A+B+C+D (Overdrive)
                    if (kb.numpadPlusKey.isPressed) held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
                }
            }

            // 2. Gamepad Input. P1 reads pad 0. P2 reads pad 1 when a second pad is connected.
            var pad = SelectGamepad(playerIndex);
            if (pad != null)
            {
                // D-pad & Left Stick with SOCD cleaning
                Vector2 padStickRaw = pad.leftStick.ReadValue();
                bool padUp = pad.dpad.up.isPressed || padStickRaw.y > 0.5f;
                bool padDown = pad.dpad.down.isPressed || padStickRaw.y < -0.5f;
                bool padLeft = pad.dpad.left.isPressed || padStickRaw.x < -0.5f;
                bool padRight = pad.dpad.right.isPressed || padStickRaw.x > 0.5f;

                Vector2 padStick = CleanSOCD(padUp, padDown, padLeft, padRight);

                if (padStick.sqrMagnitude > stick.sqrMagnitude)
                {
                    stick = padStick;
                }

                // Standard Fighting Game Layout:
                // West / South = A (Square/X on PS, X/A on Xbox)
                if (pad.buttonWest.isPressed || pad.buttonSouth.isPressed) held |= (byte)BBButton.A;
                // North = B (Triangle/Y)
                if (pad.buttonNorth.isPressed) held |= (byte)BBButton.B;
                // Right Shoulder = C (R1/RB)
                if (pad.rightShoulder.isPressed) held |= (byte)BBButton.C;
                // East = D (Circle/B)
                if (pad.buttonEast.isPressed)  held |= (byte)BBButton.D;

                // Gamepad Macros:
                // L1 / LB = A+B (Crush Trigger / Barrier)
                if (pad.leftShoulder.isPressed) held |= (byte)(BBButton.A | BBButton.B);
                // R2 / RT = B+C (Throw)
                if (pad.rightTrigger.isPressed) held |= (byte)(BBButton.B | BBButton.C);
                // L2 / LT = A+B+C+D (Burst / Overdrive Unified Macro)
                if (pad.leftTrigger.isPressed)  held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
                // R3 = A+B+C+D (Overdrive)
                if (pad.rightStickButton.isPressed) held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
            }

            // 3. Virtual Touch Overlay Input with SOCD cleaning
            if (virtualStickInput.sqrMagnitude > 0.04f)
            {
                bool touchUp = virtualStickInput.y > 0.5f;
                bool touchDown = virtualStickInput.y < -0.5f;
                bool touchLeft = virtualStickInput.x < -0.5f;
                bool touchRight = virtualStickInput.x > 0.5f;
                Vector2 touchClean = CleanSOCD(touchUp, touchDown, touchLeft, touchRight);
                if (touchClean.sqrMagnitude > stick.sqrMagnitude)
                {
                    stick = touchClean;
                }
            }
            held |= virtualHeldMask;

            // Compute rising and falling edges
            byte newlyPressed = (byte)(held & ~_currentHeld);
            byte newlyReleased = (byte)(~held & _currentHeld);

            // Accumulate latched edges across render frames
            _latchedPressed |= newlyPressed;
            _latchedPressed |= virtualPressedMask;
            _latchedReleased |= newlyReleased;
            _latchedReleased |= virtualReleasedMask;

            _currentHeld = held;
            _currentStick = stick;

            // Clear one-shot virtual masks
            virtualPressedMask = 0;
            virtualReleasedMask = 0;
        }

        /// <summary>
        /// Pad 0 belongs to player index 0 (or player 1 yields pad 0 to player 2 in 2-player local versus).
        /// Later indexes only see their own pad.
        /// </summary>
        public static Gamepad SelectGamepad(int playerIndex)
        {
            if (playerIndex < 0) return null;
            var pads = Gamepad.all;
            if (pads.Count == 0) return null;
            if (playerIndex < pads.Count && pads.Count > 1) return pads[playerIndex];

            if (pads.Count == 1)
            {
                var director = HomegrownCombatDirector.Instance;
                bool p2IsHuman = director != null && director.Fighter2 != null && director.Fighter2.isPlayerControlled;
                if (playerIndex == 1)
                {
                    return pads[0];
                }
                if (playerIndex == 0)
                {
                    // If P2 is human, P1 plays on keyboard and P2 gets the only gamepad
                    if (p2IsHuman) return null;
                    return pads[0];
                }
            }

            return null;
        }

        /// <summary>
        /// Consumes latched edges and stick direction, producing a single deterministic BBInputFrame for this combat tick.
        /// </summary>
        public BBInputFrame SampleFrameForTick(int tick, int facingDirection)
        {
            if (!isPlayerControlled || Time.timeScale == 0f || (MatchRoundReferee.Instance != null && MatchRoundReferee.Instance.State != MatchState.Fighting))
            {
                ResetLatches();
                return new BBInputFrame { held = 0, pressed = 0, released = 0, numpad = 5 };
            }

            SampleHardware();

            sbyte worldNumpad = BBInputFrame.VectorToWorldNumpad(_currentStick);

            var frame = new BBInputFrame
            {
                held = _currentHeld,
                pressed = _latchedPressed,
                released = _latchedReleased,
                numpad = worldNumpad
            };

            // Reset latches for next tick
            _latchedPressed = 0;
            _latchedReleased = 0;

            return frame;
        }

        /// <summary>
        /// Clears all input latches, directional stick state, and virtual inputs.
        /// </summary>
        public void ResetLatches()
        {
            _latchedPressed = 0;
            _latchedReleased = 0;
            _currentStick = Vector2.zero;
            _currentHeld = 0;
            virtualStickInput = Vector2.zero;
            virtualHeldMask = 0;
            virtualPressedMask = 0;
            virtualReleasedMask = 0;
        }

        /// <summary>
        /// Injects a synthetic frame (useful for automated tests and replay playback).
        /// </summary>
        public void InjectFrame(BBInputFrame frame)
        {
            _currentHeld = frame.held;
            _latchedPressed |= frame.pressed;
            _latchedReleased |= frame.released;
        }
    }
}
