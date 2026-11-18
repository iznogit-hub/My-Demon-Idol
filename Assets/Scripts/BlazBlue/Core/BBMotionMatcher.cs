using MDI.Akio;

namespace DemonIdol.BlazBlue
{
    public static class BBMotionMatcher
    {
        public static bool Is41236(MotionEdge[] e, int n, int now, int facing, int window)
        {
            return Sequence(e, n, now, facing, window, 4, 4, 2, 3, 6, 0, 0);
        }

        public static bool Is63214(MotionEdge[] e, int n, int now, int facing, int window)
        {
            return Sequence(e, n, now, facing, window, 4, 6, 2, 1, 4, 0, 0);
        }

        public static bool Is632146(MotionEdge[] e, int n, int now, int facing, int window)
        {
            return Sequence(e, n, now, facing, window, 6, 6, 3, 2, 1, 4, 6);
        }

        public static bool Is236236(MotionEdge[] e, int n, int now, int facing, int window)
        {
            return DoubleQuarter(e, n, now, facing, window, true);
        }

        public static bool Is214214(MotionEdge[] e, int n, int now, int facing, int window)
        {
            return DoubleQuarter(e, n, now, facing, window, false);
        }

        public static bool Is360(MotionEdge[] e, int n, int now, int facing, int window)
        {
            int prev = -1;
            int dir = 0;
            int steps = 0;
            for (int i = 0; i < n; i++)
            {
                if (now - e[i].Tick > window || e[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                int card = Cardinal(pad);
                if (card < 0 || card == prev) continue;
                if (prev >= 0)
                {
                    int step = (card - prev + 4) % 4;
                    if (step != 1 && step != 3) return false;
                    int signed = step == 1 ? 1 : -1;
                    if (dir == 0) dir = signed;
                    else if (dir != signed) return false;
                    steps++;
                }
                prev = card;
            }
            return steps >= 3;
        }

        public static bool EndsOnUp(MotionEdge[] e, int n, int now, int facing, int window)
        {
            int last = 5;
            for (int i = 0; i < n; i++)
            {
                if (now - e[i].Tick > window || e[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                if (pad != 5) last = pad;
            }
            return last == 7 || last == 8 || last == 9;
        }

        public static bool Is22(MotionEdge[] e, int n, int now, int facing, int window)
        {
            int stage = 0;
            for (int i = 0; i < n; i++)
            {
                if (now - e[i].Tick > window || e[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                bool down = pad == 2 || pad == 1 || pad == 3;
                if (stage == 0 && down) stage = 1;
                else if (stage == 1 && pad == 5) stage = 2;
                else if (stage == 2 && down) stage = 3;
            }
            return stage == 3;
        }

        public static bool Is323(MotionEdge[] e, int n, int now, int facing, int window)
        {
            int stage = 0;
            int last = 5;
            for (int i = 0; i < n; i++)
            {
                if (now - e[i].Tick > window || e[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                if (pad != 5) last = pad;
                if (stage == 0 && pad == 3) stage = 1;
                else if (stage == 1 && pad == 2) stage = 2;
                else if (stage == 2 && pad == 3) stage = 3;
            }
            return stage == 3 && last == 3;
        }

        public static bool IsChargeDownUp(MotionEdge[] e, int n, int now, int facing, int hold, int release)
        {
            for (int i = 0; i < n; i++)
            {
                if (e[i].Tick <= 0 || now - e[i].Tick > 90) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                if (pad != 2 && pad != 1 && pad != 3) continue;
                int end = now;
                for (int j = i + 1; j < n; j++)
                {
                    if (e[j].Tick <= 0) continue;
                    end = e[j].Tick;
                    break;
                }
                if (end - e[i].Tick < hold) continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (e[j].Tick <= 0 || now - e[j].Tick > release) continue;
                    int next = MotionMatcher.Numpad(e[j].H, e[j].V, facing);
                    if (next == 7 || next == 8 || next == 9) return true;
                }
            }
            return false;
        }

        public static bool IsChargeBackForward(MotionEdge[] e, int n, int now, int facing, int hold, int release)
        {
            return MotionMatcher.IsChargeBackForward(e, n, now, facing, hold, release);
        }

        static int Cardinal(int pad)
        {
            if (pad == 6) return 0;
            if (pad == 2 || pad == 1 || pad == 3) return 1;
            if (pad == 4) return 2;
            if (pad == 8 || pad == 7 || pad == 9) return 3;
            return -1;
        }

        static bool Sequence(MotionEdge[] e, int n, int now, int facing, int window, int count, int w0, int w1, int w2, int w3, int w4, int w5)
        {
            int stage = 0;
            int last = 5;
            for (int i = 0; i < n; i++)
            {
                if (now - e[i].Tick > window || e[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                if (pad != 5) last = pad;
                if (stage < count && Accept(Want(stage, w0, w1, w2, w3, w4, w5), pad)) stage++;
            }
            int end = Want(count - 1, w0, w1, w2, w3, w4, w5);
            return stage == count && (last == end || (end == 6 && (last == 3 || last == 9)) || (end == 4 && (last == 1 || last == 7)));
        }

        static int Want(int stage, int w0, int w1, int w2, int w3, int w4, int w5)
        {
            if (stage == 0) return w0;
            if (stage == 1) return w1;
            if (stage == 2) return w2;
            if (stage == 3) return w3;
            if (stage == 4) return w4;
            return w5;
        }

        static bool Accept(int want, int pad)
        {
            if (want == 6) return pad == 6 || pad == 3 || pad == 9;
            if (want == 4) return pad == 4 || pad == 1 || pad == 7;
            if (want == 2) return pad == 2 || pad == 1 || pad == 3;
            if (want == 8) return pad == 8 || pad == 7 || pad == 9;
            if (want == 3) return pad == 3 || pad == 2 || pad == 6;
            if (want == 1) return pad == 1 || pad == 2 || pad == 4;
            return pad == want;
        }

        static bool DoubleQuarter(MotionEdge[] e, int n, int now, int facing, int window, bool forward)
        {
            int firstDone = -1;
            int stage = 0;
            int stage2 = 0;
            int last = 5;
            for (int i = 0; i < n; i++)
            {
                if (now - e[i].Tick > window || e[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(e[i].H, e[i].V, facing);
                if (firstDone < 0)
                {
                    if (stage == 0 && DownStart(pad, forward)) stage = 1;
                    else if (stage == 1 && ForwardEnd(pad, forward))
                    {
                        stage = 2;
                        firstDone = e[i].Tick;
                    }
                }
                else if (e[i].Tick > firstDone)
                {
                    if (pad != 5) last = pad;
                    if (stage2 == 0 && DownStart(pad, forward)) stage2 = 1;
                    else if (stage2 == 1 && ForwardEnd(pad, forward)) stage2 = 2;
                }
            }
            if (stage2 != 2) return false;
            return forward ? (last == 6 || last == 3 || last == 9) : (last == 4 || last == 1 || last == 7);
        }

        static bool DownStart(int pad, bool forward)
        {
            return forward ? (pad == 2 || pad == 1) : (pad == 2 || pad == 3);
        }

        static bool ForwardEnd(int pad, bool forward)
        {
            return forward ? (pad == 6 || pad == 3 || pad == 9) : (pad == 4 || pad == 1 || pad == 7);
        }
    }
}
