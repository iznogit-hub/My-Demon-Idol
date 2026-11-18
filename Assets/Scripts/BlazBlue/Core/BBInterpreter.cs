using MDI.Akio;

namespace DemonIdol.BlazBlue
{
    public struct BBQuery
    {
        public bool grounded;
        public bool guarding;
        public bool hitstun;
        public bool blockstun;
        public bool attacking;
        public bool madeContact;
        public bool airborne;
        public int moveTick;
        public int activeEnd;
        public int contactTick;
    }

    public struct BBResolved
    {
        public BBCommandKind kind;
        public int moveId;
    }

    public sealed class BBInputHistory
    {
        public const int Cap = 64;
        public readonly MotionEdge[] edges = new MotionEdge[Cap];
        public int count;
        public int consumedThrough;
        public readonly int[] pressTick = new int[5];
        public readonly int[] releaseTick = new int[5];
        public readonly int[] holdStart = new int[5];
        int _lastH = 99;
        int _lastV = 99;

        public void Clear()
        {
            count = 0;
            consumedThrough = 0;
            _lastH = 99;
            _lastV = 99;
            for (int i = 0; i < 5; i++)
            {
                pressTick[i] = -999;
                releaseTick[i] = -999;
                holdStart[i] = -999;
            }
        }

        public void CopyFrom(BBInputHistory src)
        {
            count = src.count;
            consumedThrough = src.consumedThrough;
            _lastH = src._lastH;
            _lastV = src._lastV;
            for (int i = 0; i < Cap; i++) edges[i] = src.edges[i];
            for (int i = 0; i < 5; i++)
            {
                pressTick[i] = src.pressTick[i];
                releaseTick[i] = src.releaseTick[i];
                holdStart[i] = src.holdStart[i];
            }
        }

        public void Push(BBInputFrame frame, int tick)
        {
            int h = 0;
            int v = 0;
            int p = frame.numpad;
            if (p == 6 || p == 3 || p == 9) h = 1;
            else if (p == 4 || p == 1 || p == 7) h = -1;
            if (p == 2 || p == 1 || p == 3) v = -1;
            else if (p == 8 || p == 7 || p == 9) v = 1;
            if (h != _lastH || v != _lastV)
            {
                if (count == Cap)
                {
                    for (int i = 1; i < Cap; i++) edges[i - 1] = edges[i];
                    count = Cap - 1;
                }
                edges[count] = new MotionEdge(h, v, tick);
                count++;
                _lastH = h;
                _lastV = v;
            }
            byte rose = frame.pressed;
            byte fell = frame.released;
            for (int i = 0; i < 4; i++)
            {
                byte bit = (byte)(1 << i);
                if ((rose & bit) != 0)
                {
                    pressTick[i] = tick;
                    holdStart[i] = tick;
                }
                if ((fell & bit) != 0) releaseTick[i] = tick;
            }
        }

        public bool RecentPress(BBButton button, int now, int window)
        {
            int i = Bit(button);
            if (i < 0) return false;
            return now - pressTick[i] <= window && pressTick[i] > consumedThrough;
        }

        public bool NegativeEdge(BBButton button, int now, BBTuning t)
        {
            int i = Bit(button);
            if (i < 0) return false;
            int rel = releaseTick[i];
            if (rel <= consumedThrough) return false;
            if (now - rel > t.negativeEdgeTicks) return false;
            return rel - holdStart[i] >= t.negativeEdgeTicks;
        }

        public bool Chord(BBButton a, BBButton b, int now, int window)
        {
            int ia = Bit(a);
            int ib = Bit(b);
            if (ia < 0 || ib < 0) return false;
            int ta = pressTick[ia];
            int tb = pressTick[ib];
            if (ta <= consumedThrough || tb <= consumedThrough) return false;
            if (now - ta > window || now - tb > window) return false;
            int gap = ta > tb ? ta - tb : tb - ta;
            return gap <= window;
        }

        public static int Bit(BBButton button)
        {
            if (button == BBButton.A) return 0;
            if (button == BBButton.B) return 1;
            if (button == BBButton.C) return 2;
            if (button == BBButton.D) return 3;
            return -1;
        }
    }

    public static class BBCommandInterpreter
    {
        public static BBResolved Resolve(BBInputHistory hist, BBTuning t, BBMoveDef[] moves, int moveCount, BBQuery q, int now, int facing, bool consume)
        {
            BBResolved none = new BBResolved();
            int buf = t.bufferTicks;
            bool abcd = hist.Chord(BBButton.A, BBButton.B, now, t.chordWindowTicks)
                && hist.Chord(BBButton.C, BBButton.D, now, t.chordWindowTicks)
                && hist.Chord(BBButton.A, BBButton.C, now, t.chordWindowTicks);
            if (abcd && (hist.RecentPress(BBButton.A, now, buf) || hist.RecentPress(BBButton.D, now, buf)))
            {
                BBResolved r = new BBResolved();
                r.kind = (q.hitstun || q.blockstun) ? BBCommandKind.Burst : BBCommandKind.Overdrive;
                if (consume) hist.consumedThrough = now;
                return r;
            }
            bool abc = hist.Chord(BBButton.A, BBButton.B, now, t.chordWindowTicks) && hist.Chord(BBButton.A, BBButton.C, now, t.chordWindowTicks);
            if (abc && q.attacking && (q.madeContact || t.allowWhiffRC))
            {
                BBResolved r = new BBResolved();
                r.kind = BBCommandKind.RapidCancel;
                if (consume) hist.consumedThrough = now;
                return r;
            }
            bool holdingForward = LastPad(hist, now, facing, t.motionWindow236) == 6;
            if (q.blockstun && holdingForward && hist.Chord(BBButton.A, BBButton.B, now, t.chordWindowTicks))
            {
                BBResolved r = new BBResolved();
                r.kind = BBCommandKind.CounterAssault;
                if (consume) hist.consumedThrough = now;
                return r;
            }
            if (hist.Chord(BBButton.A, BBButton.B, now, t.chordWindowTicks) && q.guarding)
            {
                BBResolved r = new BBResolved();
                r.kind = BBCommandKind.Barrier;
                return r;
            }
            if (!q.guarding && !q.hitstun && !q.blockstun && hist.Chord(BBButton.A, BBButton.B, now, t.chordWindowTicks)
                && (hist.RecentPress(BBButton.A, now, buf) || hist.RecentPress(BBButton.B, now, buf)))
            {
                BBResolved r = new BBResolved();
                r.kind = BBCommandKind.CrushTrigger;
                if (consume) hist.consumedThrough = now;
                return r;
            }
            if (hist.Chord(BBButton.B, BBButton.C, now, t.chordWindowTicks))
            {
                BBResolved r = new BBResolved();
                r.kind = BBCommandKind.Throw;
                if (consume) hist.consumedThrough = now;
                return r;
            }

            int best = -1;
            int bestPri = -1;
            for (int i = 0; i < moveCount; i++)
            {
                BBMoveDef m = moves[i];
                if (m.kind != BBMoveKind.Special && m.kind != BBMoveKind.DistortionDrive && m.kind != BBMoveKind.AstralHeat && m.kind != BBMoveKind.CommandThrow)
                    continue;
                if (m.kind == BBMoveKind.AstralHeat && !t.enableAstral) continue;
                if (!MotionMatches(hist, t, m.motion, now, facing)) continue;
                if (!ButtonReady(hist, t, m.button, now, true)) continue;
                if (q.airborne && !m.airOk) continue;
                if (!q.airborne && !m.groundOk) continue;
                if (m.priority > bestPri)
                {
                    bestPri = m.priority;
                    best = i;
                }
            }
            if (best >= 0)
            {
                BBResolved r = new BBResolved();
                r.kind = moves[best].kind == BBMoveKind.DistortionDrive ? BBCommandKind.Distortion
                    : moves[best].kind == BBMoveKind.AstralHeat ? BBCommandKind.Astral
                    : moves[best].kind == BBMoveKind.CommandThrow ? BBCommandKind.Throw
                    : BBCommandKind.Special;
                r.moveId = moves[best].id;
                if (consume) hist.consumedThrough = now;
                return r;
            }

            for (int i = 0; i < moveCount; i++)
            {
                BBMoveDef m = moves[i];
                if (m.motion != BBMotion.None) continue;
                if (m.kind != BBMoveKind.Normal && m.kind != BBMoveKind.CommandNormal && m.kind != BBMoveKind.DriveMove && m.kind != BBMoveKind.Throw)
                    continue;
                if (!ButtonReady(hist, t, m.button, now, false)) continue;
                int pad = LastPad(hist, now, facing, buf);
                if (q.airborne)
                {
                    if (m.stance != 3) continue;
                }
                else if (m.stance == 3) continue;
                else if (m.stance == 2 && pad != 6) continue;
                else if (m.stance == 1 && pad != 2 && pad != 1 && pad != 3) continue;
                else if (m.stance == 4 && pad != 4) continue;
                else if (m.stance == 5 && pad != 3) continue;
                else if (m.stance == 0 && (pad == 2 || pad == 1 || pad == 3 || pad == 6)) continue;
                if (m.kind == BBMoveKind.CommandNormal && m.stance == 0) continue;
                BBResolved r = new BBResolved();
                r.kind = m.kind == BBMoveKind.Throw ? BBCommandKind.Throw : BBCommandKind.Normal;
                r.moveId = m.id;
                if (consume) hist.consumedThrough = now;
                return r;
            }
            return none;
        }

        public static bool ButtonReady(BBInputHistory hist, BBTuning t, BBButton button, int now, bool special)
        {
            if (hist.RecentPress(button, now, t.bufferTicks)) return true;
            return special && hist.NegativeEdge(button, now, t);
        }

        public static bool MotionMatches(BBInputHistory hist, BBTuning t, BBMotion motion, int now, int facing)
        {
            MotionEdge[] e = hist.edges;
            int n = hist.count;
            int win = t.motionWindow236;
            if (motion == BBMotion.None) return true;
            if (motion == BBMotion.M236) return MotionMatcher.Is236(e, n, now, facing, win);
            if (motion == BBMotion.M214) return MotionMatcher.Is214(e, n, now, facing, win);
            if (motion == BBMotion.M623) return MotionMatcher.Is623(e, n, now, facing, win) || (t.allow323Shortcut && BBMotionMatcher.Is323(e, n, now, facing, win));
            if (motion == BBMotion.M41236) return BBMotionMatcher.Is41236(e, n, now, facing, t.motionWindowHalfCircle);
            if (motion == BBMotion.M63214) return BBMotionMatcher.Is63214(e, n, now, facing, t.motionWindowHalfCircle);
            if (motion == BBMotion.M236236) return BBMotionMatcher.Is236236(e, n, now, facing, t.motionWindowDD);
            if (motion == BBMotion.M214214) return BBMotionMatcher.Is214214(e, n, now, facing, t.motionWindowDD);
            if (motion == BBMotion.M632146) return BBMotionMatcher.Is632146(e, n, now, facing, t.motionWindowDD);
            if (motion == BBMotion.M360) return BBMotionMatcher.Is360(e, n, now, facing, t.motionWindow360);
            if (motion == BBMotion.M22) return BBMotionMatcher.Is22(e, n, now, facing, t.motionWindow22);
            if (motion == BBMotion.Charge46) return BBMotionMatcher.IsChargeBackForward(e, n, now, facing, t.chargeHoldTicks, t.chargeReleaseTicks);
            if (motion == BBMotion.Charge28) return BBMotionMatcher.IsChargeDownUp(e, n, now, facing, t.chargeHoldTicks, t.chargeReleaseTicks);
            return false;
        }

        static int LastPad(BBInputHistory hist, int now, int facing, int window)
        {
            int last = 5;
            for (int i = 0; i < hist.count; i++)
            {
                if (now - hist.edges[i].Tick > window || hist.edges[i].Tick <= 0) continue;
                int pad = MotionMatcher.Numpad(hist.edges[i].H, hist.edges[i].V, facing);
                if (pad != 5) last = pad;
            }
            return last;
        }
    }
}
