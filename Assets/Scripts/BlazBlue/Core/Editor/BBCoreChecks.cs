using System.Collections.Generic;
using MDI.Akio;

namespace DemonIdol.BlazBlue
{
    public static class BBCoreChecks
    {
        public static List<string> Run()
        {
            List<string> fails = new List<string>();
            Motions(fails);
            Chords(fails);
            Cancels(fails);
            Damage(fails);
            Hitstun(fails);
            Collision(fails);
            Determinism(fails);
            IronRushStocks(fails);
            return fails;
        }

        static void Motions(List<string> fails)
        {
            MotionEdge[] e = new MotionEdge[16];
            e[0] = new MotionEdge(0, -1, 10);
            e[1] = new MotionEdge(1, 0, 18);
            Expect(fails, MotionMatcher.Is236(e, 2, 20, 1), "236 down then forward");
            Expect(fails, !MotionMatcher.Is236(e, 2, 40, 1), "236 stale window");
            Expect(fails, MotionMatcher.Is236(new[] { new MotionEdge(0, -1, 10), new MotionEdge(-1, 0, 16) }, 2, 18, -1), "236 facing left");
            Expect(fails, MotionMatcher.Is623(new[] { new MotionEdge(1, 0, 8), new MotionEdge(0, -1, 12), new MotionEdge(1, -1, 16) }, 3, 18, 1), "623");
            Expect(fails, BBMotionMatcher.Is323(new[] { new MotionEdge(1, -1, 8), new MotionEdge(0, -1, 12), new MotionEdge(1, -1, 16) }, 3, 18, 1, 22), "323 shortcut");
            Expect(fails, BBMotionMatcher.Is41236(new[] { new MotionEdge(-1, 0, 4), new MotionEdge(0, -1, 8), new MotionEdge(1, -1, 12), new MotionEdge(1, 0, 16) }, 4, 18, 1, 30), "41236");
            Expect(fails, BBMotionMatcher.Is63214(new[] { new MotionEdge(1, 0, 4), new MotionEdge(0, -1, 8), new MotionEdge(-1, -1, 12), new MotionEdge(-1, 0, 16) }, 4, 18, 1, 30), "63214");
            Expect(fails, BBMotionMatcher.Is236236(new[] {
                new MotionEdge(0, -1, 2), new MotionEdge(1, 0, 6),
                new MotionEdge(0, -1, 10), new MotionEdge(1, 0, 14)
            }, 4, 16, 1, 36), "236236");
            Expect(fails, BBMotionMatcher.Is214214(new[] {
                new MotionEdge(0, -1, 2), new MotionEdge(-1, 0, 6),
                new MotionEdge(0, -1, 10), new MotionEdge(-1, 0, 14)
            }, 4, 16, 1, 36), "214214");
            Expect(fails, BBMotionMatcher.Is632146(new[] {
                new MotionEdge(1, 0, 2), new MotionEdge(1, -1, 4), new MotionEdge(0, -1, 6),
                new MotionEdge(-1, -1, 8), new MotionEdge(-1, 0, 10), new MotionEdge(1, 0, 12)
            }, 6, 14, 1, 36), "632146");
            Expect(fails, BBMotionMatcher.Is360(new[] {
                new MotionEdge(1, 0, 2), new MotionEdge(0, -1, 4), new MotionEdge(-1, 0, 6), new MotionEdge(0, 1, 8)
            }, 4, 10, 1, 40), "360 clockwise");
            Expect(fails, BBMotionMatcher.Is360(new[] {
                new MotionEdge(1, 0, 2), new MotionEdge(0, 1, 4), new MotionEdge(-1, 0, 6), new MotionEdge(0, -1, 8)
            }, 4, 10, 1, 40), "360 counter");
            Expect(fails, BBMotionMatcher.EndsOnUp(new[] { new MotionEdge(0, 1, 8) }, 1, 10, 1, 40), "360 ends on up");
            Expect(fails, !BBMotionMatcher.Is360(new[] { new MotionEdge(1, 0, 1), new MotionEdge(0, -1, 2) }, 2, 50, 1, 22), "360 stale");
            Expect(fails, BBMotionMatcher.Is22(new[] { new MotionEdge(0, -1, 2), new MotionEdge(0, 0, 6), new MotionEdge(0, -1, 10) }, 3, 12, 1, 16), "22");
            Expect(fails, BBMotionMatcher.IsChargeBackForward(new[] { new MotionEdge(-1, 0, 10), new MotionEdge(1, 0, 55) }, 2, 60, 1, 40, 12), "charge 46");
            Expect(fails, !BBMotionMatcher.IsChargeBackForward(new[] { new MotionEdge(-1, 0, 40), new MotionEdge(1, 0, 55) }, 2, 60, 1, 40, 12), "charge too short");
            Expect(fails, BBMotionMatcher.IsChargeDownUp(new[] { new MotionEdge(0, -1, 10), new MotionEdge(0, 1, 55) }, 2, 60, 1, 40, 12), "charge 28");

            BBTuning t = BBTuning.Default();
            BBMoveDef[] moves = PriorityMoves();
            BBInputHistory hist = new BBInputHistory();
            Push(hist, 1, 3);
            Push(hist, 2, 2);
            Push(hist, 3, 3);
            Press(hist, BBButton.C, 4);
            BBQuery q = Ground();
            BBResolved r = BBCommandInterpreter.Resolve(hist, t, moves, 3, q, 4, 1, false);
            Expect(fails, r.moveId == 2, "623 beats 236 when 323 matches, got " + r.moveId);
            t.allow323Shortcut = false;
            r = BBCommandInterpreter.Resolve(hist, t, moves, 3, q, 4, 1, false);
            Expect(fails, r.moveId == 1, "without 323 shortcut the quarter circle remains, got " + r.moveId);

            BBInputHistory spin = new BBInputHistory();
            Push(spin, 1, 6);
            Push(spin, 2, 2);
            Push(spin, 3, 4);
            Push(spin, 4, 8);
            Press(spin, BBButton.C, 5);
            r = BBCommandInterpreter.Resolve(spin, BBTuning.Default(), moves, 3, q, 5, 1, false);
            Expect(fails, r.moveId == 3, "360 beats 623 and 236, got " + r.moveId);
            int before = spin.consumedThrough;
            BBCommandInterpreter.Resolve(spin, BBTuning.Default(), moves, 3, q, 5, 1, false);
            Expect(fails, spin.consumedThrough == before, "probe does not consume");
            BBCommandInterpreter.Resolve(spin, BBTuning.Default(), moves, 3, q, 5, 1, true);
            Expect(fails, spin.consumedThrough == 5, "consume only when the move starts");

            BBInputHistory neg = new BBInputHistory();
            Push(neg, 1, 2);
            Push(neg, 4, 6);
            neg.holdStart[BBInputHistory.Bit(BBButton.A)] = 1;
            neg.releaseTick[BBInputHistory.Bit(BBButton.A)] = 6;
            BBMoveDef[] only = new BBMoveDef[1];
            only[0] = Move(9, BBButton.A, BBMoveKind.Special, BBMotion.M236, 0, 10);
            r = BBCommandInterpreter.Resolve(neg, BBTuning.Default(), only, 1, q, 7, 1, false);
            Expect(fails, r.moveId == 9, "negative edge fires a special");
        }

        static void Chords(List<string> fails)
        {
            BBTuning t = BBTuning.Default();
            BBInputHistory hist = new BBInputHistory();
            Press(hist, BBButton.B, 10);
            Press(hist, BBButton.C, 12);
            BBQuery q = Ground();
            BBResolved r = BBCommandInterpreter.Resolve(hist, t, Empty(), 0, q, 12, 1, false);
            Expect(fails, r.kind == BBCommandKind.Throw, "B+C within 3 is a throw");

            hist.Clear();
            Press(hist, BBButton.B, 10);
            Press(hist, BBButton.C, 14);
            r = BBCommandInterpreter.Resolve(hist, t, Empty(), 0, q, 14, 1, false);
            Expect(fails, r.kind != BBCommandKind.Throw, "B then C at 4 ticks is not a chord");

            hist.Clear();
            Press(hist, BBButton.A, 10);
            Press(hist, BBButton.B, 11);
            q.guarding = true;
            r = BBCommandInterpreter.Resolve(hist, t, Empty(), 0, q, 11, 1, false);
            Expect(fails, r.kind == BBCommandKind.Barrier, "A+B while guarding is barrier");
            q.guarding = false;
            r = BBCommandInterpreter.Resolve(hist, t, Empty(), 0, q, 11, 1, false);
            Expect(fails, r.kind == BBCommandKind.CrushTrigger, "A+B in neutral is crush trigger");

            hist.Clear();
            Press(hist, BBButton.A, 20);
            Press(hist, BBButton.B, 20);
            Press(hist, BBButton.C, 20);
            Press(hist, BBButton.D, 20);
            q.hitstun = true;
            r = BBCommandInterpreter.Resolve(hist, t, Empty(), 0, q, 20, 1, false);
            Expect(fails, r.kind == BBCommandKind.Burst, "ABCD in hitstun is burst");
            q.hitstun = false;
            r = BBCommandInterpreter.Resolve(hist, t, Empty(), 0, q, 20, 1, false);
            Expect(fails, r.kind == BBCommandKind.Overdrive, "ABCD in neutral is overdrive");
        }

        static void Cancels(List<string> fails)
        {
            BBTuning t = BBTuning.Default();
            BBMoveDef a = Move(1, BBButton.A, BBMoveKind.Normal, BBMotion.None, 0, 1);
            a.gatling0 = 1;
            a.gatling1 = 2;
            a.specialCancel = true;
            BBMoveDef b = Move(2, BBButton.B, BBMoveKind.Normal, BBMotion.None, 0, 2);
            BBMoveDef special = Move(8, BBButton.A, BBMoveKind.Special, BBMotion.M236, 0, 20);
            Expect(fails, BBRules.CanGatling(a, b, true, false, 0, 0, 6, 7, t), "A gatlings to B on hit");
            Expect(fails, !BBRules.CanGatling(a, b, false, false, 0, 0, 6, 7, t), "whiff does not gatling to B");
            Expect(fails, BBRules.CanGatling(a, a, false, false, 0, 0, 6, 7, t), "whiff A into A");
            Expect(fails, !BBRules.CanGatling(a, a, true, false, 3, 0, 6, 7, t), "A repeat cap is 3");
            Expect(fails, !BBRules.CanGatling(a, b, true, false, 0, 1UL << 2, 6, 7, t), "B is once per chain");
            Expect(fails, !BBRules.CanGatling(a, b, true, false, 0, 0, -1, 7, t), "no cancel before contact");
            Expect(fails, BBRules.CanGatling(a, special, true, false, 0, 0, 6, 7, t), "special cancel");
            Expect(fails, !t.enableAstral, "astral ships off");
        }

        static void Damage(List<string> fails)
        {
            BBTuning t = BBTuning.Default();
            int first = BBRules.ScaleDamage(300, 0.70f, 1f, 1, t.sameMovePenalty, false, false, 1f, 0.05f, t.stylishDamageScale, t.dangerDamageScale);
            Expect(fails, first == 210, "first hit 300 * 0.70 = 210, got " + first);
            int second = BBRules.ScaleDamage(300, 0.70f, 0.85f, 2, t.sameMovePenalty, false, false, 1f, 0.05f, t.stylishDamageScale, t.dangerDamageScale);
            Expect(fails, second == 142, "same-move penalty floors to 142, got " + second);
            int danger = BBRules.ScaleDamage(300, 1f, 1f, 1, 1f, true, false, 1f, 0f, t.stylishDamageScale, t.dangerDamageScale);
            Expect(fails, danger == 375, "danger x1.25, got " + danger);
            int stylish = BBRules.ScaleDamage(300, 1f, 1f, 1, 1f, false, true, 1f, 0f, 0.8f, t.dangerDamageScale);
            Expect(fails, stylish == 240, "stylish x0.8, got " + stylish);
            int guts = BBRules.ScaleDamage(100, 1f, 1f, 1, 1f, false, false, 0.05f, 0f, t.stylishDamageScale, t.dangerDamageScale);
            Expect(fails, guts == 60, "guts under 10% is 0.60, got " + guts);
            int floorMin = BBRules.ScaleDamage(100, 0.1f, 0.1f, 1, 1f, false, false, 1f, 0.30f, t.stylishDamageScale, t.dangerDamageScale);
            Expect(fails, floorMin == 30, "min damage floor, got " + floorMin);
            Expect(fails, BBRules.OverdriveTicks(1f, 180, 480) == 180, "full hp overdrive is short");
            Expect(fails, BBRules.OverdriveTicks(0f, 180, 480) == 480, "low hp overdrive is long");
        }

        static void Hitstun(List<string> fails)
        {
            Expect(fails, BBRules.HitstunDecayGround(100, false, 120) == 0, "early combo no decay");
            Expect(fails, BBRules.HitstunDecayGround(200, false, 120) == 2, "181-300 decays 2");
            Expect(fails, BBRules.HitstunDecayGround(200, true, 120) == 0, "fatal shifts the threshold");
            Expect(fails, BBRules.ApplyHitstun(10, 8) == 6, "hitstun minimum is 6");
            Expect(fails, BBRules.HitstunDecayAir(400, false, 120) == 12, "air decay");
            float g = BBRules.GravityScale(20, 0.06f, 1.9f);
            Expect(fails, g == 1.9f, "gravity caps at 1.9, got " + g);
        }

        static void IronRushStocks(List<string> fails)
        {
            BBSim sim = new BBSim();
            BBMoveDef[] moves = new BBMoveDef[1];
            moves[0] = new BBMoveDef();
            moves[0].id = 1;
            moves[0].button = BBButton.D;
            moves[0].kind = BBMoveKind.DriveMove;
            moves[0].groundOk = true;
            moves[0].startup = 1;
            moves[0].active = 2;
            moves[0].recovery = 4;
            moves[0].damage = 350;
            moves[0].p1 = 0.85f;
            moves[0].p2 = 0.9f;
            moves[0].hitstun = 12;
            moves[0].hitX = 0.4f;
            moves[0].hitY = 1f;
            moves[0].hitW = 1.2f;
            moves[0].hitH = 1.2f;
            sim.Reset(moves, 1, BBTuning.Default());
            sim.a.x = 0f;
            sim.b.x = 0.3f;
            BBInputFrame press = new BBInputFrame();
            press.numpad = 5;
            press.pressed = (byte)BBButton.D;
            press.held = (byte)BBButton.D;
            BBInputFrame idle = new BBInputFrame();
            idle.numpad = 5;
            sim.Tick(press, idle);
            for (int i = 0; i < 6; i++) sim.Tick(idle, idle);
            Expect(fails, sim.a.stocks == 1, "Iron Rush banks one stock on hit, got " + sim.a.stocks);
            sim.a.stocks = 3;
            sim.a.phase = 0;
            sim.a.moveId = -1;
            sim.a.madeContact = false;
            sim.b.phase = 0;
            sim.b.hp = 11000;
            sim.Tick(press, idle);
            for (int i = 0; i < 6; i++) sim.Tick(idle, idle);
            Expect(fails, sim.a.stocks == 3, "Iron Rush caps at 3");
        }

        static void Collision(List<string> fails)
        {
            Expect(fails, !BBRules.Overlaps(0f, 1f, 0f, 1f, 1f, 2f, 0f, 1f), "touching edges do not hit");
            Expect(fails, BBRules.Overlaps(0f, 1f, 0f, 1f, 0.9f, 2f, 0f, 1f), "overlap hits");
            float x0, x1, y0, y1;
            BBRules.WorldBox(0f, -1, 0.8f, 1f, 0.4f, 0.4f, out x0, out x1, out y0, out y1);
            Expect(fails, x1 < 0f, "facing left flips the box");
            BBRules.WorldBox(0f, 1, 0.8f, 1f, 0.4f, 0.4f, out x0, out x1, out y0, out y1);
            Expect(fails, x0 > 0f, "facing right keeps the box forward");
        }

        static void Determinism(List<string> fails)
        {
            int[] first = RunScript();
            int[] second = RunScript();
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    fails.Add("hash diverged at tick " + i);
                    return;
                }
            }
            BBSim sim = NewSim();
            BBInputFrame walk = Frame(6, 0);
            for (int i = 0; i < 20; i++) sim.Tick(walk, walk);
            BBFighterState sa = new BBFighterState();
            BBFighterState sb = new BBFighterState();
            BBInputHistory ha = new BBInputHistory();
            BBInputHistory hb = new BBInputHistory();
            int saved;
            sim.CopyFightersTo(ref sa, ref sb, ha, hb, out saved);
            int[] forwardHashes = new int[7];
            for (int i = 0; i < 7; i++)
            {
                sim.Tick(walk, walk);
                forwardHashes[i] = sim.Hash();
            }
            sim.Restore(sa, sb, ha, hb, saved);
            for (int i = 0; i < 7; i++)
            {
                sim.Tick(walk, walk);
                if (sim.Hash() != forwardHashes[i]) fails.Add("rollback hash mismatch at +" + i);
            }

            BBSim warm = NewSim();
            for (int i = 0; i < 30; i++) warm.Tick(walk, Frame(4, 0));
            long before = System.GC.GetTotalAllocatedBytes(true);
            for (int i = 0; i < 600; i++) warm.Tick(walk, Frame(4, 0));
            long after = System.GC.GetTotalAllocatedBytes(true);
            if (after != before) fails.Add("sim allocated " + (after - before) + " bytes over 600 ticks");
        }

        static int[] RunScript()
        {
            BBSim sim = NewSim();
            int[] hashes = new int[40];
            for (int i = 0; i < 40; i++)
            {
                BBInputFrame atk = Frame(6, i == 8 ? (byte)BBButton.A : (byte)0);
                sim.Tick(atk, Frame(4, 0));
                hashes[i] = sim.Hash();
            }
            return hashes;
        }

        static BBSim NewSim()
        {
            BBSim sim = new BBSim();
            BBMoveDef[] moves = new BBMoveDef[1];
            moves[0] = Move(1, BBButton.A, BBMoveKind.Normal, BBMotion.None, 0, 1);
            moves[0].startup = 4;
            moves[0].active = 3;
            moves[0].recovery = 6;
            moves[0].damage = 300;
            moves[0].p1 = 0.7f;
            moves[0].p2 = 0.85f;
            moves[0].minPct = 0.05f;
            moves[0].hitstun = 14;
            moves[0].hitX = 0.7f;
            moves[0].hitY = 1f;
            moves[0].hitW = 0.6f;
            moves[0].hitH = 0.4f;
            sim.Reset(moves, 1, BBTuning.Default());
            sim.a.x = -0.4f;
            sim.b.x = 0.4f;
            return sim;
        }

        static BBMoveDef[] PriorityMoves()
        {
            BBMoveDef[] m = new BBMoveDef[3];
            m[0] = Move(1, BBButton.C, BBMoveKind.Special, BBMotion.M236, 0, 10);
            m[1] = Move(2, BBButton.C, BBMoveKind.Special, BBMotion.M623, 0, 20);
            m[2] = Move(3, BBButton.C, BBMoveKind.Special, BBMotion.M360, 0, 30);
            return m;
        }

        static BBMoveDef[] Empty()
        {
            return new BBMoveDef[0];
        }

        static BBMoveDef Move(int id, BBButton button, BBMoveKind kind, BBMotion motion, byte stance, int priority)
        {
            BBMoveDef m = new BBMoveDef();
            m.id = id;
            m.button = button;
            m.kind = kind;
            m.motion = motion;
            m.stance = stance;
            m.groundOk = true;
            m.priority = priority;
            m.startup = 5;
            m.active = 3;
            m.recovery = 8;
            return m;
        }

        static BBQuery Ground()
        {
            BBQuery q = new BBQuery();
            q.grounded = true;
            return q;
        }

        static void Push(BBInputHistory hist, int tick, int numpad)
        {
            BBInputFrame f = new BBInputFrame();
            f.numpad = (sbyte)numpad;
            hist.Push(f, tick);
        }

        static void Press(BBInputHistory hist, BBButton button, int tick)
        {
            BBInputFrame f = new BBInputFrame();
            f.numpad = 5;
            f.pressed = (byte)button;
            f.held = (byte)button;
            hist.Push(f, tick);
        }

        static BBInputFrame Frame(int numpad, byte pressed)
        {
            BBInputFrame f = new BBInputFrame();
            f.numpad = (sbyte)numpad;
            f.pressed = pressed;
            f.held = pressed;
            return f;
        }

        static void Expect(List<string> fails, bool ok, string message)
        {
            if (!ok) fails.Add(message);
        }
    }
}
