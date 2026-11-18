using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private void Update()
        {
            if (!isPlayerControlled) return;

            SampleHardware();
        }

        private void SampleHardware()
        {
            Vector2 stick = Vector2.zero;
            byte held = 0;

            // 1. Keyboard Input
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Directions
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) stick.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) stick.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) stick.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) stick.x += 1f;

                // Buttons: J=A, K=B, L=C, I=D, T=Taunt
                if (kb.jKey.isPressed) held |= (byte)BBButton.A;
                if (kb.kKey.isPressed) held |= (byte)BBButton.B;
                if (kb.lKey.isPressed) held |= (byte)BBButton.C;
                if (kb.iKey.isPressed) held |= (byte)BBButton.D;
                if (kb.tKey.isPressed) held |= (byte)BBButton.Taunt;

                // Keyboard Macros
                // Semicolon (;) = Throw (B+C)
                if (kb.semicolonKey.isPressed)
                {
                    held |= (byte)(BBButton.B | BBButton.C);
                }
                // U = Barrier / Crush Trigger (A+B)
                if (kb.uKey.isPressed)
                {
                    held |= (byte)(BBButton.A | BBButton.B);
                }
                // O = Rapid Cancel (A+B+C)
                if (kb.oKey.isPressed)
                {
                    held |= (byte)(BBButton.A | BBButton.B | BBButton.C);
                }
                // P = Burst / Overdrive (A+B+C+D)
                if (kb.pKey.isPressed)
                {
                    held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D);
                }
            }

            // 2. Gamepad Input
            var pad = Gamepad.current;
            if (pad != null)
            {
                // D-pad & Left Stick
                Vector2 padStick = pad.leftStick.ReadValue();
                if (pad.dpad.up.isPressed) padStick.y = 1f;
                else if (pad.dpad.down.isPressed) padStick.y = -1f;
                if (pad.dpad.right.isPressed) padStick.x = 1f;
                else if (pad.dpad.left.isPressed) padStick.x = -1f;

                if (padStick.sqrMagnitude > stick.sqrMagnitude)
                {
                    stick = padStick;
                }

                // Face Buttons: West=A (Square/X), North=B (Triangle/Y), East=C (Circle/B), South=D (Cross/A)
                if (pad.buttonWest.isPressed)  held |= (byte)BBButton.A;
                if (pad.buttonNorth.isPressed) held |= (byte)BBButton.B;
                if (pad.buttonEast.isPressed)  held |= (byte)BBButton.C;
                if (pad.buttonSouth.isPressed) held |= (byte)BBButton.D;

                // Shoulders & Triggers (Macros)
                if (pad.leftShoulder.isPressed)  held |= (byte)(BBButton.A | BBButton.B); // Barrier / Crush
                if (pad.rightShoulder.isPressed) held |= (byte)(BBButton.B | BBButton.C); // Throw
                if (pad.leftTrigger.isPressed)   held |= (byte)(BBButton.A | BBButton.B | BBButton.C | BBButton.D); // Burst / OD
                if (pad.rightTrigger.isPressed)  held |= (byte)(BBButton.A | BBButton.B | BBButton.C); // Rapid Cancel
            }

            // 3. Virtual Touch Overlay Input
            if (virtualStickInput.sqrMagnitude > stick.sqrMagnitude)
            {
                stick = virtualStickInput;
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
        /// Consumes latched edges and stick direction, producing a single deterministic BBInputFrame for this combat tick.
        /// </summary>
        public BBInputFrame SampleFrameForTick(int tick, int facingDirection)
        {
            if (!isPlayerControlled)
            {
                return new BBInputFrame { held = 0, pressed = 0, released = 0, numpad = 5 };
            }

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
