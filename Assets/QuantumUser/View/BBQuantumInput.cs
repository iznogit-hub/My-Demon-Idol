namespace Quantum
{
    using DemonIdol.BlazBlue;
    using Photon.Deterministic;
    using UnityEngine;
#if ENABLE_INPUT_SYSTEM && QUANTUM_ENABLE_INPUTSYSTEM
    using UnityEngine.InputSystem;
#endif

    /// <summary>
    /// Polls local ABCD into Quantum's existing Input (Left, Right, Up, Fire=A)
    /// and keeps B, C, and D in BBQuantumSideband until the Input asset is extended.
    /// Does not read or write an App Id. Izno fills PhotonServerSettings himself.
    /// </summary>
    public class BBQuantumInput : QuantumMonoBehaviour
    {
        public static ushort Sideband { get; private set; }
        public static string LocalFighterId = "C01";

        void OnEnable()
        {
            QuantumCallback.Subscribe(this, (CallbackPollInput callback) => OnPollInput(callback));
        }

        void OnPollInput(CallbackPollInput callback)
        {
            var buttons = Read();
            Sideband = BBQuantumInputCodec.Pack(buttons);
            var input = new Input
            {
                Left = buttons.Left,
                Right = buttons.Right,
                Up = buttons.Up,
                Fire = buttons.A
            };
            callback.SetInput(input, DeterministicInputFlags.Repeatable);
        }

        public static bool HasAppId()
        {
            var settings = PhotonServerSettings.Global;
            return settings != null && settings.AppSettings != null && !string.IsNullOrEmpty(settings.AppSettings.AppIdQuantum);
        }

        static BBQuantumInputCodec.Buttons Read()
        {
            var buttons = new BBQuantumInputCodec.Buttons();
#if ENABLE_INPUT_SYSTEM && QUANTUM_ENABLE_INPUTSYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                buttons.Left = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                buttons.Right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                buttons.Up = kb.wKey.isPressed || kb.upArrowKey.isPressed;
                buttons.Down = kb.sKey.isPressed || kb.downArrowKey.isPressed;
                buttons.A = kb.jKey.isPressed;
                buttons.B = kb.kKey.isPressed;
                buttons.C = kb.lKey.isPressed;
                buttons.D = kb.iKey.isPressed;
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                var stick = pad.leftStick.ReadValue();
                if (stick.x < -0.5f || pad.dpad.left.isPressed) buttons.Left = true;
                if (stick.x > 0.5f || pad.dpad.right.isPressed) buttons.Right = true;
                if (stick.y > 0.5f || pad.dpad.up.isPressed) buttons.Up = true;
                if (stick.y < -0.5f || pad.dpad.down.isPressed) buttons.Down = true;
                if (pad.buttonWest.isPressed) buttons.A = true;
                if (pad.buttonNorth.isPressed) buttons.B = true;
                if (pad.buttonEast.isPressed) buttons.C = true;
                if (pad.buttonSouth.isPressed) buttons.D = true;
            }
#endif
            return buttons;
        }
    }
}
