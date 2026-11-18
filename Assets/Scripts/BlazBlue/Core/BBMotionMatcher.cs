using System;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure, zero-allocation numpad motion matcher for BlazBlue & Anime fighting mechanics.
    /// Operates on reverse-chronological arrays of BBMotionEdge (index 0 is newest).
    /// Directions are facing-relative numpad (1..9):
    /// 7 8 9
    /// 4 5 6  (6 = forward, 4 = back, 2 = down, 8 = up, 5 = neutral)
    /// 1 2 3
    /// </summary>
    public static class BBMotionMatcher
    {
        public const int DEFAULT_WINDOW_236 = 22;
        public const int DEFAULT_WINDOW_623 = 22;
        public const int DEFAULT_WINDOW_HC = 30;
        public const int DEFAULT_WINDOW_DD = 36;
        public const int DEFAULT_WINDOW_360 = 40;
        public const int DEFAULT_WINDOW_22 = 16;
        public const int DEFAULT_CHARGE_HOLD = 40;
        public const int DEFAULT_CHARGE_RELEASE = 12;

        /// <summary>
        /// Quarter-Circle Forward (236): Down (2) -> Down-Forward (3) -> Forward (6).
        /// </summary>
        public static bool Is236(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_236)
        {
            if (count < 3) return false;

            int step = 0; // 0 = looking for 6, 1 = looking for 3, 2 = looking for 2
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 6 || e.numpad == 3 || e.numpad == 9)
                        {
                            endTick = e.tick;
                            step = 1;
                        }
                        break;
                    case 1:
                        if (e.numpad == 3 || e.numpad == 2)
                        {
                            step = 2;
                        }
                        break;
                    case 2:
                        if (e.numpad == 2 || e.numpad == 1 || e.numpad == 3)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Quarter-Circle Back (214): Down (2) -> Down-Back (1) -> Back (4).
        /// </summary>
        public static bool Is214(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_236)
        {
            if (count < 3) return false;

            int step = 0; // 0 = looking for 4, 1 = looking for 1, 2 = looking for 2
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 4 || e.numpad == 1 || e.numpad == 7)
                        {
                            endTick = e.tick;
                            step = 1;
                        }
                        break;
                    case 1:
                        if (e.numpad == 1 || e.numpad == 2)
                        {
                            step = 2;
                        }
                        break;
                    case 2:
                        if (e.numpad == 2 || e.numpad == 3 || e.numpad == 1)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Dragon Punch (623): Forward (6) -> Down (2) -> Down-Forward (3).
        /// Also accepts 636 and optional 323 shortcut.
        /// </summary>
        public static bool Is623(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_623, bool allow323Shortcut = true)
        {
            if (count < 3) return false;

            // Check standard 623 / 636
            int step = 0; // 0 = looking for 3, 1 = looking for 2, 2 = looking for 6
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 3 || e.numpad == 6)
                        {
                            endTick = e.tick;
                            step = 1;
                        }
                        break;
                    case 1:
                        if (e.numpad == 2 || e.numpad == 1 || e.numpad == 3)
                        {
                            step = 2;
                        }
                        break;
                    case 2:
                        if (e.numpad == 6 || (allow323Shortcut && e.numpad == 3))
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Reverse Dragon Punch (421): Back (4) -> Down (2) -> Down-Back (1).
        /// </summary>
        public static bool Is421(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_623)
        {
            if (count < 3) return false;

            int step = 0;
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 1 || e.numpad == 4)
                        {
                            endTick = e.tick;
                            step = 1;
                        }
                        break;
                    case 1:
                        if (e.numpad == 2 || e.numpad == 3 || e.numpad == 1)
                        {
                            step = 2;
                        }
                        break;
                    case 2:
                        if (e.numpad == 4)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Half-Circle Forward (41236): 4 -> (1|2) -> (2|3) -> 6.
        /// </summary>
        public static bool Is41236(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_HC)
        {
            if (count < 3) return false;

            int step = 0; // 0: find 6, 1: find 3 or 2, 2: find 1 or 2, 3: find 4
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 6 || e.numpad == 3)
                        {
                            endTick = e.tick;
                            step = 1;
                        }
                        break;
                    case 1:
                        if (e.numpad == 3 || e.numpad == 2) step = 2;
                        break;
                    case 2:
                        if (e.numpad == 1 || e.numpad == 2) step = 3;
                        break;
                    case 3:
                        if (e.numpad == 4 || e.numpad == 1)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Half-Circle Back (63214): 6 -> (3|2) -> (2|1) -> 4.
        /// </summary>
        public static bool Is63214(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_HC)
        {
            if (count < 3) return false;

            int step = 0;
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 4 || e.numpad == 1)
                        {
                            endTick = e.tick;
                            step = 1;
                        }
                        break;
                    case 1:
                        if (e.numpad == 1 || e.numpad == 2) step = 2;
                        break;
                    case 2:
                        if (e.numpad == 3 || e.numpad == 2) step = 3;
                        break;
                    case 3:
                        if (e.numpad == 6 || e.numpad == 3)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Double Quarter-Circle Forward (236236).
        /// </summary>
        public static bool Is236236(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_DD)
        {
            if (count < 5) return false;

            int step = 0; // 0: 6, 1: 3/2, 2: 2/1, 3: 6, 4: 3/2, 5: 2/1
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 6 || e.numpad == 3) { endTick = e.tick; step = 1; }
                        break;
                    case 1:
                        if (e.numpad == 3 || e.numpad == 2) step = 2;
                        break;
                    case 2:
                        if (e.numpad == 2 || e.numpad == 1) step = 3;
                        break;
                    case 3:
                        if (e.numpad == 6 || e.numpad == 3) step = 4;
                        break;
                    case 4:
                        if (e.numpad == 3 || e.numpad == 2) step = 5;
                        break;
                    case 5:
                        if (e.numpad == 2 || e.numpad == 1)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Double Quarter-Circle Back (214214).
        /// </summary>
        public static bool Is214214(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_DD)
        {
            if (count < 5) return false;

            int step = 0;
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 4 || e.numpad == 1) { endTick = e.tick; step = 1; }
                        break;
                    case 1:
                        if (e.numpad == 1 || e.numpad == 2) step = 2;
                        break;
                    case 2:
                        if (e.numpad == 2 || e.numpad == 3) step = 3;
                        break;
                    case 3:
                        if (e.numpad == 4 || e.numpad == 1) step = 4;
                        break;
                    case 4:
                        if (e.numpad == 1 || e.numpad == 2) step = 5;
                        break;
                    case 5:
                        if (e.numpad == 2 || e.numpad == 3)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Distortion Drive Motion: 632146 (Half-Circle Back + Forward).
        /// </summary>
        public static bool Is632146(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_DD)
        {
            if (count < 4) return false;

            int step = 0; // 0: 6, 1: 4/1, 2: 2/1/3, 3: 6/3
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                switch (step)
                {
                    case 0:
                        if (e.numpad == 6 || e.numpad == 3) { endTick = e.tick; step = 1; }
                        break;
                    case 1:
                        if (e.numpad == 4 || e.numpad == 1) step = 2;
                        break;
                    case 2:
                        if (e.numpad == 2 || e.numpad == 1 || e.numpad == 3) step = 3;
                        break;
                    case 3:
                        if (e.numpad == 6 || e.numpad == 3)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// 360-Degree Motion: Any 4 cardinal directions in rotational sequence.
        /// Accepts clockwise or counter-clockwise. Up (8) may be substituted by 7 or 9 for jump forgiveness.
        /// </summary>
        public static bool Is360(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_360)
        {
            if (count < 4) return false;

            int mask = 0; // bit 0: Forward (6), bit 1: Down (2), bit 2: Back (4), bit 3: Up (8/7/9)
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                if (endTick == -1) endTick = e.tick;

                if (e.numpad == 6 || e.numpad == 3) mask |= 1;
                if (e.numpad == 2 || e.numpad == 1 || e.numpad == 3) mask |= 2;
                if (e.numpad == 4 || e.numpad == 1) mask |= 4;
                if (e.numpad == 8 || e.numpad == 7 || e.numpad == 9) mask |= 8;

                if (mask == 15) // all 4 quadrants present
                {
                    return (endTick - e.tick) <= window;
                }
            }

            return false;
        }

        /// <summary>
        /// Down-Down Motion (22): Down -> Neutral / Non-down -> Down.
        /// </summary>
        public static bool Is22(BBMotionEdge[] edges, int count, int currentTick, int window = DEFAULT_WINDOW_22)
        {
            if (count < 3) return false;

            int step = 0; // 0: down, 1: neutral/non-down, 2: down
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > window) break;

                bool isDown = e.numpad == 2 || e.numpad == 1 || e.numpad == 3;

                switch (step)
                {
                    case 0:
                        if (isDown) { endTick = e.tick; step = 1; }
                        break;
                    case 1:
                        if (!isDown) step = 2;
                        break;
                    case 2:
                        if (isDown)
                        {
                            return (endTick - e.tick) <= window;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Double-tap direction motion (e.g. 66 dash, 44 backdash).
        /// </summary>
        public static bool IsDoubleTap(BBMotionEdge[] edges, int count, int currentTick, sbyte targetNumpad, int maxSpanTicks = 14)
        {
            if (count < 3) return false;

            int tapCount = 0;
            bool sawNeutral = false;
            int endTick = -1;

            for (int i = 0; i < count; i++)
            {
                var e = edges[i];
                if (currentTick - e.tick > maxSpanTicks) break;

                if (e.numpad == targetNumpad)
                {
                    if (tapCount == 0)
                    {
                        endTick = e.tick;
                        tapCount++;
                    }
                    else if (tapCount == 1 && sawNeutral)
                    {
                        return (endTick - e.tick) <= maxSpanTicks;
                    }
                }
                else if (e.numpad != targetNumpad && tapCount == 1)
                {
                    sawNeutral = true;
                }
            }

            return false;
        }

        /// <summary>
        /// Charge Back -> Forward ([4]6): Back held for holdTicks, then Forward released within releaseTicks.
        /// </summary>
        public static bool IsChargeBackForward(BBMotionEdge[] edges, int count, int currentTick, int holdTicks = DEFAULT_CHARGE_HOLD, int releaseTicks = DEFAULT_CHARGE_RELEASE)
        {
            if (count < 2) return false;

            // Newest edge must be Forward (6, 3, 9) within releaseTicks
            var latest = edges[0];
            if (currentTick - latest.tick > releaseTicks) return false;
            if (latest.numpad != 6 && latest.numpad != 3 && latest.numpad != 9) return false;

            // Look back for Back (4, 1, 7) held for at least holdTicks
            for (int i = 1; i < count; i++)
            {
                var e = edges[i];
                if (e.numpad == 4 || e.numpad == 1 || e.numpad == 7)
                {
                    // If we find an edge before it or if it spanned holdTicks
                    int holdDuration = (i + 1 < count) ? (edges[i - 1].tick - e.tick) : (currentTick - e.tick);
                    if (holdDuration >= holdTicks) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Charge Down -> Up ([2]8): Down held for holdTicks, then Up released within releaseTicks.
        /// </summary>
        public static bool IsChargeDownUp(BBMotionEdge[] edges, int count, int currentTick, int holdTicks = DEFAULT_CHARGE_HOLD, int releaseTicks = DEFAULT_CHARGE_RELEASE)
        {
            if (count < 2) return false;

            var latest = edges[0];
            if (currentTick - latest.tick > releaseTicks) return false;
            if (latest.numpad != 8 && latest.numpad != 7 && latest.numpad != 9) return false;

            for (int i = 1; i < count; i++)
            {
                var e = edges[i];
                if (e.numpad == 2 || e.numpad == 1 || e.numpad == 3)
                {
                    int holdDuration = (i + 1 < count) ? (edges[i - 1].tick - e.tick) : (currentTick - e.tick);
                    if (holdDuration >= holdTicks) return true;
                }
            }

            return false;
        }
    }
}
