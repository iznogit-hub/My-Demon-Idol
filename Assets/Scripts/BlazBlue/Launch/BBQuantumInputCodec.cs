namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Deterministic pack of A B C D and directions.
    /// Quantum's generated Input still only has Left, Right, Up, and Fire.
    /// This codec is the contract the online view and the tests share until that Input asset is regenerated.
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
    }
}
