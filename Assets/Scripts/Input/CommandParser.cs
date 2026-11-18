using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    public struct DirectionalInput
    {
        public int Horizontal; // -1, 0, 1
        public int Vertical;   // -1, 0, 1
        public int TickTimestamp;

        public DirectionalInput(int h, int v, int tick)
        {
            Horizontal = h;
            Vertical = v;
            TickTimestamp = tick;
        }

        public bool IsNeutral => Horizontal == 0 && Vertical == 0;
        public bool IsForward(int facingDir) => Horizontal == facingDir;
        public bool IsBack(int facingDir) => Horizontal == -facingDir;
        public bool IsDown => Vertical == -1;
        public bool IsDownBack(int facingDir) => Vertical == -1 && Horizontal == -facingDir;
        public bool IsDownForward(int facingDir) => Vertical == -1 && Horizontal == facingDir;
    }

    /// <summary>
    /// Motion detector for Tekken & Anime fighter directional commands:
    /// Double-tap dash (f, f), Backdash (b, b), KBD loop (b, b, d/b, b),
    /// and special motions (d, d/f, f).
    /// </summary>
    public class CommandParser
    {
        private const int HISTORY_SIZE = 32;
        private readonly DirectionalInput[] _history = new DirectionalInput[HISTORY_SIZE];
        private int _head = 0;

        public void RecordDirection(Vector2 rawDir, int currentTick, int facingDirection)
        {
            int h = 0;
            if (rawDir.x > 0.35f) h = 1;
            else if (rawDir.x < -0.35f) h = -1;

            int v = 0;
            if (rawDir.y > 0.45f) v = 1;
            else if (rawDir.y < -0.45f) v = -1;

            // Only record state changes to preserve tick boundaries
            DirectionalInput latest = _history[_head];
            if (latest.Horizontal != h || latest.Vertical != v)
            {
                _head = (_head + 1) % HISTORY_SIZE;
                _history[_head] = new DirectionalInput(h, v, currentTick);
            }
        }

        /// <summary>
        /// Checks for double-tap forward (f, n, f) within maxSpanTicks.
        /// </summary>
        public bool CheckForwardDash(int currentTick, int facingDirection, int maxSpanTicks = 14)
        {
            int forwardCount = 0;
            bool sawNeutral = false;

            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                int idx = (_head - i + HISTORY_SIZE) % HISTORY_SIZE;
                DirectionalInput d = _history[idx];

                if (currentTick - d.TickTimestamp > maxSpanTicks) break;

                if (d.IsForward(facingDirection))
                {
                    if (forwardCount == 0)
                    {
                        forwardCount++;
                    }
                    else if (forwardCount == 1 && sawNeutral)
                    {
                        ConsumeHistory(idx);
                        return true;
                    }
                }
                else if (d.IsNeutral && forwardCount == 1)
                {
                    sawNeutral = true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks for double-tap backdash (b, n, b).
        /// </summary>
        public bool CheckBackdash(int currentTick, int facingDirection, int maxSpanTicks = 14)
        {
            int backCount = 0;
            bool sawNeutral = false;

            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                int idx = (_head - i + HISTORY_SIZE) % HISTORY_SIZE;
                DirectionalInput d = _history[idx];

                if (currentTick - d.TickTimestamp > maxSpanTicks) break;

                if (d.IsBack(facingDirection))
                {
                    if (backCount == 0)
                    {
                        backCount++;
                    }
                    else if (backCount == 1 && sawNeutral)
                    {
                        ConsumeHistory(idx);
                        return true;
                    }
                }
                else if (d.IsNeutral && backCount == 1)
                {
                    sawNeutral = true;
                }
            }

            return false;
        }

        /// <summary>
        /// Detects Korean Backdash Cancel (KBD): b, n, b, d/b cancel into b.
        /// Enforces strict chronological order (d/b followed by b) occurring after sinceTick,
        /// and consumes the pattern on success to prevent infinite invulnerability looping.
        /// </summary>
        public bool CheckKBDCancel(int currentTick, int facingDirection, int sinceTick = -1, int maxSpanTicks = 20)
        {
            int backTick = -1;

            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                int idx = (_head - i + HISTORY_SIZE) % HISTORY_SIZE;
                DirectionalInput d = _history[idx];

                if (currentTick - d.TickTimestamp > maxSpanTicks) break;
                if (d.TickTimestamp <= sinceTick) break;

                // Scanning newest to oldest: must observe Back first
                if (backTick == -1)
                {
                    if (d.IsBack(facingDirection))
                    {
                        backTick = d.TickTimestamp;
                    }
                }
                else
                {
                    // Must observe DownBack that occurred strictly before Back, but after sinceTick
                    if (d.IsDownBack(facingDirection) && d.TickTimestamp < backTick)
                    {
                        ConsumeHistory(idx);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Detects double-tap Up (u, n, u) for Tekken 3D Sidestep into background (+Z).
        /// </summary>
        public bool CheckSidestepUp(int currentTick, int maxSpanTicks = 14)
        {
            int upCount = 0;
            bool sawNeutral = false;

            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                int idx = (_head - i + HISTORY_SIZE) % HISTORY_SIZE;
                DirectionalInput d = _history[idx];

                if (currentTick - d.TickTimestamp > maxSpanTicks) break;

                if (d.Vertical == 1 && d.Horizontal == 0)
                {
                    if (upCount == 0) upCount++;
                    else if (upCount == 1 && sawNeutral)
                    {
                        ConsumeHistory(idx);
                        return true;
                    }
                }
                else if (d.IsNeutral && upCount == 1)
                {
                    sawNeutral = true;
                }
            }

            return false;
        }

        /// <summary>
        /// Detects double-tap Down (d, n, d) for Tekken 3D Sidestep into foreground (-Z).
        /// </summary>
        public bool CheckSidestepDown(int currentTick, int maxSpanTicks = 14)
        {
            int downCount = 0;
            bool sawNeutral = false;

            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                int idx = (_head - i + HISTORY_SIZE) % HISTORY_SIZE;
                DirectionalInput d = _history[idx];

                if (currentTick - d.TickTimestamp > maxSpanTicks) break;

                if (d.Vertical == -1 && d.Horizontal == 0)
                {
                    if (downCount == 0) downCount++;
                    else if (downCount == 1 && sawNeutral)
                    {
                        ConsumeHistory(idx);
                        return true;
                    }
                }
                else if (d.IsNeutral && downCount == 1)
                {
                    sawNeutral = true;
                }
            }

            return false;
        }

        private void ConsumeHistory(int fromIndex)
        {
            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                _history[i] = new DirectionalInput(0, 0, -999);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                _history[i] = default;
            }
        }
    }
}
