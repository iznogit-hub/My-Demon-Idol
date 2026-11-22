using System;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// A single timestamped directional edge in facing-relative numpad notation.
    /// </summary>
    public struct BBMotionEdge
    {
        public sbyte numpad; // 1..9 facing-relative
        public int tick;

        public BBMotionEdge(sbyte numpad, int tick)
        {
            this.numpad = numpad;
            this.tick = tick;
        }
    }

    /// <summary>
    /// 64-tick zero-allocation ring buffer for combat inputs.
    /// Manages negative edge retention (specials only), buffer leniency (5 ticks),
    /// and directional edge history for motion matching.
    /// </summary>
    public class BBInputHistory
    {
        public const int CAPACITY = 64;

        private readonly BBInputFrame[] _frames = new BBInputFrame[CAPACITY];
        private readonly int[] _frameTicks = new int[CAPACITY];

        // Direction edge history (facing-relative)
        private readonly BBMotionEdge[] _edges = new BBMotionEdge[CAPACITY];
        private int _edgeHead = 0;
        private int _edgeCount = 0;

        // Button press & negative edge tracking
        private readonly int[] _buttonPressTick = new int[5]; // Index by BBButton bit position (A=0, B=1, C=2, D=3, Taunt=4)
        private readonly int[] _buttonReleaseTick = new int[5];
        private readonly int[] _buttonHeldDuration = new int[5];
        private readonly bool[] _buttonConsumed = new bool[5];

        private int _head = 0;
        private int _currentTick = 0;
        private sbyte _lastFacingNumpad = 5;

        public int CurrentTick => _currentTick;

        /// <summary>
        /// Appends a new input frame for the given combat tick and facing direction.
        /// </summary>
        public void PushFrame(BBInputFrame frame, int tick, int facingDirection)
        {
            _currentTick = tick;
            _head = (_head + 1) % CAPACITY;
            _frames[_head] = frame;
            _frameTicks[_head] = tick;

            sbyte facingNumpad = BBInputFrame.WorldToFacingNumpad(frame.numpad, facingDirection);

            // Record directional state change
            if (_edgeCount == 0 || facingNumpad != _lastFacingNumpad)
            {
                _edgeHead = (_edgeHead + 1) % CAPACITY;
                _edges[_edgeHead] = new BBMotionEdge(facingNumpad, tick);
                if (_edgeCount < CAPACITY) _edgeCount++;
                _lastFacingNumpad = facingNumpad;
            }

            // Track button edges and durations
            for (int b = 0; b < 5; b++)
            {
                byte mask = (byte)(1 << b);
                bool isPressed = (frame.pressed & mask) != 0;
                bool isHeld = (frame.held & mask) != 0;
                bool isReleased = (frame.released & mask) != 0;

                if (isPressed)
                {
                    _buttonPressTick[b] = tick;
                    _buttonHeldDuration[b] = 0;
                    _buttonConsumed[b] = false;
                }
                else if (isHeld)
                {
                    _buttonHeldDuration[b]++;
                }

                if (isReleased)
                {
                    _buttonReleaseTick[b] = tick;
                }
            }
        }

        /// <summary>
        /// Returns the newest frame in the history.
        /// </summary>
        public BBInputFrame LatestFrame => _frames[_head];

        /// <summary>
        /// Copies up to maxCount recent directional edges in reverse chronological order (newest at index 0).
        /// Returns the number of edges copied.
        /// </summary>
        public int CopyEdges(BBMotionEdge[] dst, int maxCount)
        {
            int toCopy = Math.Min(maxCount, _edgeCount);
            for (int i = 0; i < toCopy; i++)
            {
                int idx = (_edgeHead - i + CAPACITY) % CAPACITY;
                dst[i] = _edges[idx];
            }
            return toCopy;
        }

        /// <summary>
        /// Freezes buffer aging by advancing button press ticks by delta ticks (used during hitstop).
        /// </summary>
        public void FreezeBufferAging(int deltaTicks = 1)
        {
            for (int b = 0; b < 5; b++)
            {
                if (_buttonPressTick[b] > 0)
                {
                    _buttonPressTick[b] += deltaTicks;
                }
                if (_buttonReleaseTick[b] > 0)
                {
                    _buttonReleaseTick[b] += deltaTicks;
                }
            }
        }

        /// <summary>
        /// Checks whether a button has an unconsumed press within bufferTicks.
        /// </summary>
        public bool IsButtonBuffered(BBButton btn, int bufferTicks = 5)
        {
            int b = GetButtonIndex(btn);
            if (b < 0) return false;
            if (_buttonConsumed[b]) return false;

            int pressTick = _buttonPressTick[b];
            return pressTick > 0 && (_currentTick - pressTick) <= bufferTicks;
        }

        /// <summary>
        /// Checks whether a button has a valid negative-edge release for specials.
        /// Requires button was held at least 3 ticks, and released within negativeEdgeTicks.
        /// </summary>
        public bool IsNegativeEdgeBuffered(BBButton btn, int negativeEdgeTicks = 3, int minHeldTicks = 3)
        {
            int b = GetButtonIndex(btn);
            if (b < 0) return false;
            if (_buttonConsumed[b]) return false;

            int relTick = _buttonReleaseTick[b];
            if (relTick <= 0 || (_currentTick - relTick) > negativeEdgeTicks) return false;

            return _buttonHeldDuration[b] >= minHeldTicks;
        }

        /// <summary>
        /// Checks whether two buttons were pressed within chordWindowTicks of each other.
        /// </summary>
        public bool IsChordBuffered(BBButton btn1, BBButton btn2, int chordWindowTicks = 3, int bufferTicks = 5)
        {
            int b1 = GetButtonIndex(btn1);
            int b2 = GetButtonIndex(btn2);
            if (b1 < 0 || b2 < 0) return false;
            if (_buttonConsumed[b1] || _buttonConsumed[b2]) return false;

            int t1 = _buttonPressTick[b1];
            int t2 = _buttonPressTick[b2];
            if (t1 <= 0 || t2 <= 0) return false;

            if (_currentTick - t1 > bufferTicks || _currentTick - t2 > bufferTicks) return false;
            return Math.Abs(t1 - t2) <= chordWindowTicks;
        }

        /// <summary>
        /// Checks whether three buttons were pressed within chordWindowTicks of each other.
        /// </summary>
        public bool IsTripleChordBuffered(BBButton btn1, BBButton btn2, BBButton btn3, int chordWindowTicks = 3, int bufferTicks = 5)
        {
            int b1 = GetButtonIndex(btn1);
            int b2 = GetButtonIndex(btn2);
            int b3 = GetButtonIndex(btn3);
            if (b1 < 0 || b2 < 0 || b3 < 0) return false;
            if (_buttonConsumed[b1] || _buttonConsumed[b2] || _buttonConsumed[b3]) return false;

            int t1 = _buttonPressTick[b1];
            int t2 = _buttonPressTick[b2];
            int t3 = _buttonPressTick[b3];
            if (t1 <= 0 || t2 <= 0 || t3 <= 0) return false;

            if (_currentTick - t1 > bufferTicks || _currentTick - t2 > bufferTicks || _currentTick - t3 > bufferTicks) return false;
            int maxDiff = Math.Max(Math.Max(Math.Abs(t1 - t2), Math.Abs(t2 - t3)), Math.Abs(t1 - t3));
            return maxDiff <= chordWindowTicks;
        }

        /// <summary>
        /// Checks whether all four buttons (A+B+C+D) were pressed within chordWindowTicks.
        /// </summary>
        public bool IsQuadChordBuffered(int chordWindowTicks = 3, int bufferTicks = 5)
        {
            int t0 = _buttonPressTick[0];
            int t1 = _buttonPressTick[1];
            int t2 = _buttonPressTick[2];
            int t3 = _buttonPressTick[3];

            if (t0 <= 0 || t1 <= 0 || t2 <= 0 || t3 <= 0) return false;
            if (_buttonConsumed[0] || _buttonConsumed[1] || _buttonConsumed[2] || _buttonConsumed[3]) return false;

            if (_currentTick - t0 > bufferTicks || _currentTick - t1 > bufferTicks ||
                _currentTick - t2 > bufferTicks || _currentTick - t3 > bufferTicks) return false;

            int minT = Math.Min(Math.Min(t0, t1), Math.Min(t2, t3));
            int maxT = Math.Max(Math.Max(t0, t1), Math.Max(t2, t3));
            return (maxT - minT) <= chordWindowTicks;
        }

        /// <summary>
        /// Marks a button input as consumed by a successfully executed attack or command.
        /// </summary>
        public void ConsumeButton(BBButton btn)
        {
            int b = GetButtonIndex(btn);
            if (b >= 0) _buttonConsumed[b] = true;
        }

        /// <summary>
        /// Marks multiple buttons as consumed.
        /// </summary>
        public void ConsumeButtons(params BBButton[] buttons)
        {
            foreach (var btn in buttons) ConsumeButton(btn);
        }

        public void Clear()
        {
            Array.Clear(_frames, 0, CAPACITY);
            Array.Clear(_frameTicks, 0, CAPACITY);
            Array.Clear(_edges, 0, CAPACITY);
            Array.Clear(_buttonPressTick, 0, 5);
            Array.Clear(_buttonReleaseTick, 0, 5);
            Array.Clear(_buttonHeldDuration, 0, 5);
            Array.Clear(_buttonConsumed, 0, 5);
            _head = 0;
            _edgeHead = 0;
            _edgeCount = 0;
            _currentTick = 0;
            _lastFacingNumpad = 5;
        }

        private static int GetButtonIndex(BBButton btn)
        {
            switch (btn)
            {
                case BBButton.A:     return 0;
                case BBButton.B:     return 1;
                case BBButton.C:     return 2;
                case BBButton.D:     return 3;
                case BBButton.Taunt: return 4;
                default:             return -1;
            }
        }
    }
}
