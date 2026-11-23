namespace DemonIdol.BlazBlue
{
    using Quantum;

    /// <summary>
    /// Deterministic pack and converter for 4-button fighting inputs (A, B, C, D) and 8 directions.
    /// Provides lossless round-trip encoding between Photon Quantum Input structs, bitpacked sideband words,
    /// and BlazBlue simulation BBInputFrame structs.
    /// </summary>
    public static class BBQuantumInputCodec
    {
        public struct Buttons
        {
            public bool Left;
            public bool Right;
            public bool Up;
            public bool Down;
            public bool A;
            public bool B;
            public bool C;
            public bool D;
        }

        public static ushort Pack(Buttons buttons)
        {
            ushort value = 0;
            if (buttons.Left) value |= 1;
            if (buttons.Right) value |= 2;
            if (buttons.Up) value |= 4;
            if (buttons.Down) value |= 8;
            if (buttons.A) value |= 16;
            if (buttons.B) value |= 32;
            if (buttons.C) value |= 64;
            if (buttons.D) value |= 128;
            return value;
        }

        public static Buttons Unpack(ushort value)
        {
            return new Buttons
            {
                Left = (value & 1) != 0,
                Right = (value & 2) != 0,
                Up = (value & 4) != 0,
                Down = (value & 8) != 0,
                A = (value & 16) != 0,
                B = (value & 32) != 0,
                C = (value & 64) != 0,
                D = (value & 128) != 0
            };
        }

        public static ushort Pack(in BBInputFrame frame)
        {
            return Pack(ToButtons(frame));
        }

        public static Buttons ToButtons(in BBInputFrame frame)
        {
            return new Buttons
            {
                Left = frame.numpad == 7 || frame.numpad == 4 || frame.numpad == 1,
                Right = frame.numpad == 9 || frame.numpad == 6 || frame.numpad == 3,
                Up = frame.numpad >= 7 && frame.numpad <= 9,
                Down = frame.numpad >= 1 && frame.numpad <= 3,
                A = frame.IsHeld(BBButton.A),
                B = frame.IsHeld(BBButton.B),
                C = frame.IsHeld(BBButton.C),
                D = frame.IsHeld(BBButton.D)
            };
        }

        public static BBInputFrame ToBBInputFrame(Buttons buttons, byte prevHeld = 0)
        {
            sbyte numpad = 5;
            if (buttons.Up)
            {
                if (buttons.Left) numpad = 7;
                else if (buttons.Right) numpad = 9;
                else numpad = 8;
            }
            else if (buttons.Down)
            {
                if (buttons.Left) numpad = 1;
                else if (buttons.Right) numpad = 3;
                else numpad = 2;
            }
            else
            {
                if (buttons.Left) numpad = 4;
                else if (buttons.Right) numpad = 6;
                else numpad = 5;
            }

            byte held = 0;
            if (buttons.A) held |= (byte)BBButton.A;
            if (buttons.B) held |= (byte)BBButton.B;
            if (buttons.C) held |= (byte)BBButton.C;
            if (buttons.D) held |= (byte)BBButton.D;

            byte pressed = (byte)(held & ~prevHeld);
            byte released = (byte)(prevHeld & ~held);

            return new BBInputFrame
            {
                numpad = numpad,
                held = held,
                pressed = pressed,
                released = released
            };
        }

        public static Buttons FromQuantumInput(Quantum.Input input)
        {
            return new Buttons
            {
                Left = input.Left.IsDown,
                Right = input.Right.IsDown,
                Up = input.Up.IsDown,
                Down = input.Down.IsDown,
                A = input.Fire.IsDown,
                B = input.B.IsDown,
                C = input.C.IsDown,
                D = input.D.IsDown
            };
        }

        public static BBInputFrame QuantumInputToBBInputFrame(Quantum.Input input, byte prevHeld = 0)
        {
            return ToBBInputFrame(FromQuantumInput(input), prevHeld);
        }
    }
}
