using System;

namespace MDI.Akio
{
    /// <summary>
    /// Facing-relative motion tests for Akio. Edges store world-space stick
    /// steps (h/v in -1, 0, 1) stamped with the 60 Hz tick they began.
    /// Lenient window is 22 ticks (~367 ms). Charge is optional and longer.
    /// </summary>
    public struct MotionEdge
    {
        public int H;
        public int V;
        public int Tick;

        public MotionEdge(int h, int v, int tick)
        {
            H = h;
            V = v;
            Tick = tick;
        }
    }

    public static class MotionMatcher
    {
        public const int LenientWindow = 22;
        public const int ChargeHoldTicks = 40;
        public const int ChargeReleaseTicks = 12;
        public const int ChargeLookback = 90;

        public static int Numpad(int h, int v, int facing)
        {
            if (facing == 0) facing = 1;
            int rel = 0;
            if (h == facing) rel = 1;
            else if (h != 0 && h == -facing) rel = -1;

            if (v < 0 && rel > 0) return 3;
            if (v < 0 && rel < 0) return 1;
            if (v < 0) return 2;
            if (v > 0 && rel > 0) return 9;
            if (v > 0 && rel < 0) return 7;
            if (v > 0) return 8;
            if (rel > 0) return 6;
            if (rel < 0) return 4;
            return 5;
        }

        public static bool Is236(MotionEdge[] edges, int count, int now, int facing, int window = LenientWindow)
        {
            // Down (2/1/3) then a later forward (6/3/9). The diagonal alone is not enough.
            int stage = 0;
            int last = 5;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                if (now - edges[i].Tick > window || edges[i].Tick <= 0) continue;
                int pad = Numpad(edges[i].H, edges[i].V, facing);
                if (pad != 5) last = pad;
                any = true;
                if (stage == 0 && (pad == 2 || pad == 1)) stage = 1;
                else if (stage == 1 && (pad == 6 || pad == 3 || pad == 9)) stage = 2;
            }
            // Neutral after the forward still counts. A later back does not.
            return any && stage == 2 && (last == 6 || last == 3 || last == 9);
        }

        public static bool Is214(MotionEdge[] edges, int count, int now, int facing, int window = LenientWindow)
        {
            int stage = 0;
            int last = 5;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                if (now - edges[i].Tick > window || edges[i].Tick <= 0) continue;
                int pad = Numpad(edges[i].H, edges[i].V, facing);
                if (pad != 5) last = pad;
                any = true;
                if (stage == 0 && (pad == 2 || pad == 3)) stage = 1;
                else if (stage == 1 && (pad == 4 || pad == 1)) stage = 2;
            }
            return any && stage == 2 && (last == 4 || last == 1);
        }

        public static bool Is623(MotionEdge[] edges, int count, int now, int facing, int window = LenientWindow)
        {
            // Forward, then down, then down-forward. 6, 2, 3 is the strict shape; 6, 2, 6 is accepted.
            int stage = 0;
            int last = 5;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                if (now - edges[i].Tick > window || edges[i].Tick <= 0) continue;
                int pad = Numpad(edges[i].H, edges[i].V, facing);
                if (pad != 5) last = pad;
                any = true;
                if (stage == 0 && (pad == 6 || pad == 9)) stage = 1;
                else if (stage == 1 && (pad == 2 || pad == 1)) stage = 2;
                else if (stage == 2 && (pad == 3 || pad == 6)) stage = 3;
            }
            return any && stage == 3 && (last == 3 || last == 6);
        }

        public static bool IsDoubleTap(MotionEdge[] edges, int count, int now, int facing, bool forward, int window = LenientWindow)
        {
            int taps = 0;
            int want = forward ? 6 : 4;
            for (int i = 0; i < count; i++)
            {
                if (now - edges[i].Tick > window || edges[i].Tick <= 0) continue;
                if (Numpad(edges[i].H, edges[i].V, facing) == want) taps++;
            }
            return taps >= 2;
        }

        /// <summary>
        /// Back (or down-back) held at least 40 ticks, then forward within 12 ticks.
        /// No move is bound to this. It is here so a charge special can opt in later.
        /// </summary>
        public static bool IsChargeBackForward(MotionEdge[] edges, int count, int now, int facing, int minHold = ChargeHoldTicks, int releaseWindow = ChargeReleaseTicks)
        {
            for (int i = 0; i < count; i++)
            {
                if (edges[i].Tick <= 0) continue;
                if (now - edges[i].Tick > ChargeLookback) continue;
                int pad = Numpad(edges[i].H, edges[i].V, facing);
                if (pad != 4 && pad != 1) continue;

                int end = now;
                for (int j = i + 1; j < count; j++)
                {
                    if (edges[j].Tick <= 0) continue;
                    end = edges[j].Tick;
                    break;
                }
                int held = end - edges[i].Tick;
                if (held < minHold) continue;

                for (int j = i + 1; j < count; j++)
                {
                    if (edges[j].Tick <= 0) continue;
                    if (now - edges[j].Tick > releaseWindow) continue;
                    int next = Numpad(edges[j].H, edges[j].V, facing);
                    if (next == 6 || next == 3 || next == 9) return true;
                }
            }
            return false;
        }
    }
}
