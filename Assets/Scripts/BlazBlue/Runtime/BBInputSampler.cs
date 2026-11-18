using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Latches keyboard, gamepad and touch edges between combat ticks.
    /// The sim reads only the frame returned by Consume.
    /// </summary>
    public class BBInputSampler : MonoBehaviour
    {
        public bool touchA;
        public bool touchB;
        public bool touchC;
        public bool touchD;
        public bool touchThrow;
        public bool touchBarrier;
        public bool touchRapid;
        public bool touchBurst;
        public Vector2 touchStick;

        byte _prev;
        byte _held;
        byte _pressed;
        byte _released;
        sbyte _pad = 5;

        public BBInputFrame Consume()
        {
            BBInputFrame frame = new BBInputFrame();
            frame.held = _held;
            frame.pressed = _pressed;
            frame.released = _released;
            frame.numpad = _pad;
            _pressed = 0;
            _released = 0;
            return frame;
        }

        void Update()
        {
            if (DemonIdol.UI.SettingsPanel.ControlScheme != ControlScheme.BlazBlue4Button) return;
            byte held = ReadHeld();
            _pressed |= (byte)(held & ~_prev);
            _released |= (byte)(_prev & ~held);
            _prev = held;
            _held = held;
            _pad = ReadPad();
        }

        byte ReadHeld()
        {
            byte held = 0;
            if (Down(KeyCode.J) || touchA) held |= (byte)BBButton.A;
            if (Down(KeyCode.K) || touchB) held |= (byte)BBButton.B;
            if (Down(KeyCode.L) || touchC) held |= (byte)BBButton.C;
            if (Down(KeyCode.I) || touchD) held |= (byte)BBButton.D;
            if (Down(KeyCode.Semicolon) || touchThrow) held |= (byte)((byte)BBButton.B | (byte)BBButton.C);
            if (Down(KeyCode.U) || touchBarrier) held |= (byte)((byte)BBButton.A | (byte)BBButton.B);
            if (Down(KeyCode.O) || touchRapid) held |= (byte)((byte)BBButton.A | (byte)BBButton.B | (byte)BBButton.C);
            if (Down(KeyCode.P) || touchBurst) held |= (byte)((byte)BBButton.A | (byte)BBButton.B | (byte)BBButton.C | (byte)BBButton.D);
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonWest.isPressed) held |= (byte)BBButton.A;
                if (pad.buttonNorth.isPressed) held |= (byte)BBButton.B;
                if (pad.buttonEast.isPressed) held |= (byte)BBButton.C;
                if (pad.buttonSouth.isPressed) held |= (byte)BBButton.D;
                if (pad.rightShoulder.isPressed) held |= (byte)((byte)BBButton.B | (byte)BBButton.C);
                if (pad.leftShoulder.isPressed) held |= (byte)((byte)BBButton.A | (byte)BBButton.B);
                if (pad.rightTrigger.isPressed) held |= (byte)((byte)BBButton.A | (byte)BBButton.B | (byte)BBButton.C);
                if (pad.leftTrigger.isPressed) held |= (byte)((byte)BBButton.A | (byte)BBButton.B | (byte)BBButton.C | (byte)BBButton.D);
            }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.jKey.isPressed) held |= (byte)BBButton.A;
                if (kb.kKey.isPressed) held |= (byte)BBButton.B;
                if (kb.lKey.isPressed) held |= (byte)BBButton.C;
                if (kb.iKey.isPressed) held |= (byte)BBButton.D;
                if (kb.semicolonKey.isPressed) held |= (byte)((byte)BBButton.B | (byte)BBButton.C);
                if (kb.uKey.isPressed) held |= (byte)((byte)BBButton.A | (byte)BBButton.B);
                if (kb.oKey.isPressed) held |= (byte)((byte)BBButton.A | (byte)BBButton.B | (byte)BBButton.C);
                if (kb.pKey.isPressed) held |= (byte)((byte)BBButton.A | (byte)BBButton.B | (byte)BBButton.C | (byte)BBButton.D);
            }
#endif
            return held;
        }

        sbyte ReadPad()
        {
            Vector2 raw = touchStick;
            if (Down(KeyCode.A) || Down(KeyCode.LeftArrow)) raw.x -= 1f;
            if (Down(KeyCode.D) || Down(KeyCode.RightArrow)) raw.x += 1f;
            if (Down(KeyCode.S) || Down(KeyCode.DownArrow)) raw.y -= 1f;
            if (Down(KeyCode.W) || Down(KeyCode.UpArrow)) raw.y += 1f;
#if ENABLE_INPUT_SYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                Vector2 dpad = pad.dpad.ReadValue();
                if (stick.sqrMagnitude > 0.04f) raw += stick;
                if (dpad.sqrMagnitude > 0.04f) raw += dpad;
            }
#endif
            if (raw.magnitude < 0.20f) return 5;
            float angle = Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            if (angle >= 337.5f || angle < 22.5f) return 6;
            if (angle < 67.5f) return 9;
            if (angle < 112.5f) return 8;
            if (angle < 157.5f) return 7;
            if (angle < 202.5f) return 4;
            if (angle < 247.5f) return 1;
            if (angle < 292.5f) return 2;
            return 3;
        }

        static bool Down(KeyCode key)
        {
            try { return Input.GetKey(key); }
            catch { return false; }
        }
    }
}
