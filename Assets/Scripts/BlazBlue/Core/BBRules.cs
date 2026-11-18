namespace DemonIdol.BlazBlue
{
    public static class BBRules
    {
        public static int ScaleDamage(int baseDamage, float starterP1, float p2Product, int uses, float sameMovePenalty, bool danger, bool stylish, float hpFraction, float minPct, float stylishScale, float dangerScale)
        {
            float guts = 1f;
            if (hpFraction <= 0.5f) guts = 0.90f;
            if (hpFraction < 0.3f) guts = 0.80f;
            if (hpFraction < 0.2f) guts = 0.70f;
            if (hpFraction < 0.1f) guts = 0.60f;
            float penalty = 1f;
            int extra = uses - 1;
            for (int i = 0; i < extra; i++) penalty *= sameMovePenalty;
            float scaled = baseDamage * starterP1 * p2Product * penalty * (danger ? dangerScale : 1f) * (stylish ? stylishScale : 1f) * guts;
            float min = baseDamage * minPct;
            if (scaled < min) scaled = min;
            if (scaled < 0f) scaled = 0f;
            return (int)scaled;
        }

        public static int HitstunDecayGround(int comboTime, bool fatal, int shift)
        {
            int t = fatal ? comboTime - shift : comboTime;
            if (t < 0) t = 0;
            if (t <= 180) return 0;
            if (t <= 300) return 2;
            if (t <= 420) return 4;
            if (t <= 540) return 6;
            return 8;
        }

        public static int HitstunDecayAir(int comboTime, bool fatal, int shift)
        {
            int t = fatal ? comboTime - shift : comboTime;
            if (t < 0) t = 0;
            if (t <= 180) return 0;
            if (t <= 300) return 6;
            if (t <= 420) return 12;
            if (t <= 540) return 18;
            return 24;
        }

        public static int ApplyHitstun(int baseStun, int decay)
        {
            int v = baseStun - decay;
            if (v < 6) v = 6;
            return v;
        }

        public static float GravityScale(int airHits, float perHit, float cap)
        {
            float s = 1f + perHit * airHits;
            if (s > cap) s = cap;
            return s;
        }

        public static int OverdriveTicks(float hpFraction, int minTicks, int maxTicks)
        {
            if (hpFraction < 0f) hpFraction = 0f;
            if (hpFraction > 1f) hpFraction = 1f;
            return (int)(maxTicks + (minTicks - maxTicks) * hpFraction);
        }

        public static float HeatForButton(BBButton button, BBMoveKind kind, BBTuning t)
        {
            if (kind == BBMoveKind.Special || kind == BBMoveKind.DriveMove) return t.heatOnHitSpecial;
            if (button == BBButton.A) return t.heatOnHitA;
            if (button == BBButton.B) return t.heatOnHitB;
            if (button == BBButton.C) return t.heatOnHitC;
            if (button == BBButton.D) return t.heatOnHitD;
            return 0f;
        }

        public static bool CanGatling(BBMoveDef cur, BBMoveDef next, bool hit, bool blocked, int aRepeats, ulong used, int contactTick, int now, BBTuning t)
        {
            bool whiff = !hit && !blocked;
            if (whiff)
            {
                if (!(t.allowWhiffACancels && cur.button == BBButton.A && next.button == BBButton.A && next.kind == BBMoveKind.Normal))
                    return false;
            }
            if (contactTick < 0 || now < contactTick + 1) return false;
            if (!Listed(cur, next.id) && !(next.kind == BBMoveKind.Special && cur.specialCancel) && !(next.kind == BBMoveKind.DistortionDrive && cur.ddCancel))
                return false;
            if (next.button == BBButton.A && next.kind == BBMoveKind.Normal)
            {
                if (aRepeats >= t.maxARepeats) return false;
            }
            else if (next.id >= 0 && next.id < 64 && (used & (1UL << next.id)) != 0)
            {
                return false;
            }
            return true;
        }

        public static bool Listed(BBMoveDef cur, int id)
        {
            return cur.gatling0 == id || cur.gatling1 == id || cur.gatling2 == id || cur.gatling3 == id
                || cur.gatling4 == id || cur.gatling5 == id || cur.gatling6 == id || cur.gatling7 == id;
        }

        public static bool Overlaps(float ax0, float ax1, float ay0, float ay1, float bx0, float bx1, float by0, float by1)
        {
            return ax0 < bx1 && bx0 < ax1 && ay0 < by1 && by0 < ay1;
        }

        public static void WorldBox(float originX, int facing, float cx, float cy, float w, float h, out float x0, out float x1, out float y0, out float y1)
        {
            float wx = originX + facing * cx;
            float hw = w * 0.5f;
            float hh = h * 0.5f;
            x0 = wx - hw;
            x1 = wx + hw;
            y0 = cy - hh;
            y1 = cy + hh;
        }
    }
}
