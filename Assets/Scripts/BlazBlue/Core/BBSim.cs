namespace DemonIdol.BlazBlue
{
    public struct BBFighterState
    {
        public float x, y, vx, vy;
        public int facing;
        public int hp;
        public int maxHp;
        public int moveId;
        public int moveTick;
        public int contactTick;
        public int stunLeft;
        public int airActions;
        public int airTime;
        public sbyte prevPad;
        public int heatMilli;
        public int barrier;
        public int burst;
        public int dangerLeft;
        public int comboTime;
        public int airHits;
        public float starterP1;
        public float p2Product;
        public ulong usedMask;
        public int aRepeats;
        public int hitstop;
        public int stocks;
        public int guardCrush;
        public int odTicks;
        public int rcSlow;
        public int lastBarrierTick;
        public int hitIndex;
        public bool grounded;
        public bool madeContact;
        public bool rcUsed;
        public bool fatalCombo;
        public bool stylish;
        public bool barrierHeld;
        public byte phase;
    }

    public sealed class BBSim
    {
        public const int Neutral = 0;
        public const int Attack = 1;
        public const int Hitstun = 2;
        public const int Blockstun = 3;
        public const int Knockdown = 4;

        public int tick;
        public BBFighterState a;
        public BBFighterState b;
        public readonly BBInputHistory histA = new BBInputHistory();
        public readonly BBInputHistory histB = new BBInputHistory();
        public BBMoveDef[] moves;
        public int moveCount;
        public BBTuning tuning;
        public int wall = 9800;

        public void Reset(BBMoveDef[] moveTable, int count, BBTuning t)
        {
            moves = moveTable;
            moveCount = count;
            tuning = t;
            tick = 0;
            a = Fresh(1, -1.2f);
            b = Fresh(-1, 1.2f);
            histA.Clear();
            histB.Clear();
        }

        static BBFighterState Fresh(int facing, float x)
        {
            BBFighterState f = new BBFighterState();
            f.facing = facing;
            f.x = x;
            f.hp = 11000;
            f.maxHp = 11000;
            f.moveId = -1;
            f.contactTick = -1;
            f.airActions = 1;
            f.barrier = 1000;
            f.burst = 1000;
            f.starterP1 = 0f;
            f.p2Product = 1f;
            f.grounded = true;
            f.phase = Neutral;
            return f;
        }

        public void CopyFightersTo(ref BBFighterState da, ref BBFighterState db, BBInputHistory ha, BBInputHistory hb, out int savedTick)
        {
            da = a;
            db = b;
            ha.CopyFrom(histA);
            hb.CopyFrom(histB);
            savedTick = tick;
        }

        public void Restore(in BBFighterState sa, in BBFighterState sb, BBInputHistory ha, BBInputHistory hb, int savedTick)
        {
            a = sa;
            b = sb;
            histA.CopyFrom(ha);
            histB.CopyFrom(hb);
            tick = savedTick;
        }

        public void Tick(in BBInputFrame inA, in BBInputFrame inB)
        {
            tick++;
            if (a.hitstop > 0) a.hitstop--;
            else Step(ref a, histA, inA);
            if (b.hitstop > 0) b.hitstop--;
            else Step(ref b, histB, inB);
            ResolveStrike(ref a, ref b);
            ResolveStrike(ref b, ref a);
            Regen(ref a);
            Regen(ref b);
        }

        void Step(ref BBFighterState f, BBInputHistory hist, in BBInputFrame input)
        {
            hist.Push(input, tick);
            if (f.phase == Hitstun || f.phase == Blockstun || f.phase == Knockdown)
            {
                f.stunLeft--;
                if (f.stunLeft <= 0 && f.grounded) f.phase = Neutral;
                Integrate(ref f, input);
                return;
            }
            BBQuery q = new BBQuery();
            q.grounded = f.grounded;
            q.airborne = !f.grounded;
            int back = f.facing >= 0 ? 4 : 6;
            int backDown = f.facing >= 0 ? 1 : 3;
            int backUp = f.facing >= 0 ? 7 : 9;
            q.guarding = f.grounded && (input.numpad == back || input.numpad == backDown || input.numpad == backUp);
            q.hitstun = f.phase == Hitstun;
            q.blockstun = f.phase == Blockstun;
            q.attacking = f.phase == Attack;
            q.madeContact = f.madeContact;
            q.contactTick = f.contactTick;
            BBResolved r = BBCommandInterpreter.Resolve(hist, tuning, moves, moveCount, q, tick, f.facing, false);
            bool started = false;
            if (r.kind == BBCommandKind.RapidCancel && f.phase == Attack && f.madeContact && !f.rcUsed && f.heatMilli >= tuning.costRC * 1000)
            {
                f.heatMilli -= tuning.costRC * 1000;
                f.rcUsed = true;
                f.phase = Neutral;
                f.moveId = -1;
                hist.consumedThrough = tick;
            }
            else if (r.kind != BBCommandKind.None && r.kind != BBCommandKind.Barrier && f.phase == Neutral)
            {
                StartMove(ref f, r.moveId, r.kind);
                hist.consumedThrough = tick;
                started = true;
            }
            else if (f.phase == Attack)
            {
                BBMoveDef cur = Find(f.moveId);
                if (r.moveId > 0)
                {
                    BBMoveDef next = Find(r.moveId);
                    if (BBRules.CanGatling(cur, next, f.madeContact, false, f.aRepeats, f.usedMask, f.contactTick, f.moveTick, tuning))
                    {
                        StartMove(ref f, r.moveId, r.kind);
                        hist.consumedThrough = tick;
                        started = true;
                    }
                }
                if (!started)
                {
                    f.moveTick++;
                    BBMoveDef live = Find(f.moveId);
                    int total = live.startup + live.active + live.recovery;
                    if (f.moveTick >= total)
                    {
                        f.phase = Neutral;
                        f.moveId = -1;
                    }
                }
            }
            bool up = input.numpad == 7 || input.numpad == 8 || input.numpad == 9;
            bool wasUp = f.prevPad == 7 || f.prevPad == 8 || f.prevPad == 9;
            if (f.grounded && up && !wasUp && f.phase == Neutral)
            {
                f.vy = 8f;
                f.grounded = false;
                f.airTime = 0;
                f.airActions = 1;
            }
            else if (!f.grounded && f.airActions > 0 && up && !wasUp && f.airTime >= 8)
            {
                f.vy = 7f;
                f.airActions--;
            }
            f.prevPad = input.numpad;
            Integrate(ref f, input);
        }

        void StartMove(ref BBFighterState f, int id, BBCommandKind kind)
        {
            if (id < 0 && kind != BBCommandKind.Throw) return;
            bool fresh = f.phase == Neutral;
            f.phase = Attack;
            f.moveId = id;
            f.moveTick = 0;
            f.madeContact = false;
            f.contactTick = -1;
            f.hitIndex++;
            if (fresh)
            {
                f.starterP1 = 0f;
                f.p2Product = 1f;
                f.usedMask = 0;
                f.aRepeats = 0;
                f.comboTime = 0;
                f.fatalCombo = false;
                f.rcUsed = false;
            }
            if (id >= 0 && id < 64) f.usedMask |= 1UL << id;
            BBMoveDef m = Find(id);
            if (m.button == BBButton.A && m.kind == BBMoveKind.Normal) f.aRepeats++;
        }

        BBMoveDef Find(int id)
        {
            for (int i = 0; i < moveCount; i++)
            {
                if (moves[i].id == id) return moves[i];
            }
            return new BBMoveDef();
        }

        void Integrate(ref BBFighterState f, in BBInputFrame input)
        {
            if (f.grounded && f.phase == Neutral)
            {
                int pad = input.numpad;
                int rel = pad;
                if (f.facing < 0)
                {
                    if (pad == 6) rel = 4;
                    else if (pad == 4) rel = 6;
                    else if (pad == 3) rel = 1;
                    else if (pad == 1) rel = 3;
                    else if (pad == 9) rel = 7;
                    else if (pad == 7) rel = 9;
                }
                if (rel == 6 || rel == 3 || rel == 9) f.vx = 3.6f * f.facing;
                else if (rel == 4 || rel == 1 || rel == 7) f.vx = -2.8f * f.facing;
                else f.vx = 0f;
            }
            if (!f.grounded)
            {
                f.vy -= 20f / 60f;
                f.airTime++;
            }
            f.x += f.vx / 60f;
            f.y += f.vy / 60f;
            if (f.y <= 0f && f.vy <= 0f)
            {
                f.y = 0f;
                f.vy = 0f;
                if (!f.grounded) f.airActions = 1;
                f.grounded = true;
            }
            float limit = wall / 1000f;
            if (f.x > limit) f.x = limit;
            if (f.x < -limit) f.x = -limit;
        }

        void ResolveStrike(ref BBFighterState atk, ref BBFighterState def)
        {
            if (atk.phase != Attack || atk.moveId < 0) return;
            BBMoveDef m = Find(atk.moveId);
            if (atk.moveTick < m.startup || atk.moveTick >= m.startup + m.active) return;
            float ax0, ax1, ay0, ay1, bx0, bx1, by0, by1;
            BBRules.WorldBox(atk.x, atk.facing, m.hitX, m.hitY, m.hitW, m.hitH, out ax0, out ax1, out ay0, out ay1);
            BBRules.WorldBox(def.x, def.facing, 0f, 0.8f, 0.5f, 1.6f, out bx0, out bx1, out by0, out by1);
            if (!BBRules.Overlaps(ax0, ax1, ay0, ay1, bx0, bx1, by0, by1)) return;
            if (atk.madeContact) return;
            atk.madeContact = true;
            atk.contactTick = atk.moveTick;
            bool counter = def.phase == Attack;
            float hpFrac = def.maxHp <= 0 ? 1f : (float)def.hp / def.maxHp;
            if (atk.starterP1 == 0f) atk.starterP1 = m.p1;
            int dmg = BBRules.ScaleDamage(m.damage, atk.starterP1, atk.p2Product, 1, tuning.sameMovePenalty, def.dangerLeft > 0, atk.stylish, hpFrac, m.minPct, tuning.stylishDamageScale, tuning.dangerDamageScale);
            if (counter) dmg = (int)(dmg * 1.1f);
            if (dmg > def.hp) dmg = def.hp;
            def.hp -= dmg;
            atk.p2Product *= m.p2;
            int decay = BBRules.HitstunDecayGround(atk.comboTime, atk.fatalCombo || (counter && m.fatalCounter), tuning.fatalCounterDecayShift);
            int stun = m.hitstun;
            if (counter) stun = (int)(stun * tuning.counterHitHitstunScale);
            def.stunLeft = BBRules.ApplyHitstun(stun, decay);
            def.phase = Hitstun;
            def.vx = -def.facing * 1.2f;
            atk.heatMilli += (int)(BBRules.HeatForButton(m.button, m.kind, tuning) * 1000f);
            if (atk.heatMilli > tuning.heatMax * 1000) atk.heatMilli = tuning.heatMax * 1000;
            def.heatMilli += (int)(dmg * tuning.heatPerDamageTaken * 1000f);
            if (counter && m.fatalCounter) atk.fatalCombo = true;
            atk.comboTime += def.stunLeft;
        }

        void Regen(ref BBFighterState f)
        {
            float hpFrac = f.maxHp <= 0 ? 1f : (float)f.hp / f.maxHp;
            if (hpFrac <= tuning.lowHpThreshold)
                f.heatMilli += (int)(tuning.lowHpRegenPerTick * 1000f);
            if (f.heatMilli > tuning.heatMax * 1000) f.heatMilli = tuning.heatMax * 1000;
            if (f.barrierHeld)
            {
                f.barrier -= tuning.barrierHoldCost;
                f.lastBarrierTick = tick;
                if (f.barrier <= 0)
                {
                    f.barrier = 0;
                    f.dangerLeft = tuning.dangerTicks;
                }
            }
            else if (tick - f.lastBarrierTick >= tuning.barrierRegenDelay && f.barrier < tuning.barrierMax)
            {
                f.barrier += tuning.barrierRegen;
                if (f.barrier > tuning.barrierMax) f.barrier = tuning.barrierMax;
            }
            if (f.dangerLeft > 0)
            {
                f.dangerLeft--;
                if (f.barrier >= 300) f.dangerLeft = 0;
            }
            if (f.odTicks > 0) f.odTicks--;
            else if (f.burst < tuning.burstMax) f.burst += tuning.burstRegenPerTick;
        }

        public int Hash()
        {
            unchecked
            {
                int h = unchecked((int)2166136261);
                h = Mix(h, tick);
                h = MixFighter(h, a);
                h = MixFighter(h, b);
                return h;
            }
        }

        static int MixFighter(int h, BBFighterState f)
        {
            h = Mix(h, (int)(f.x * 10000f));
            h = Mix(h, (int)(f.y * 10000f));
            h = Mix(h, f.hp);
            h = Mix(h, f.phase);
            h = Mix(h, f.moveId);
            h = Mix(h, f.moveTick);
            h = Mix(h, f.heatMilli);
            h = Mix(h, f.barrier);
            h = Mix(h, f.facing);
            return h;
        }

        static int Mix(int h, int v)
        {
            unchecked
            {
                h ^= v;
                h *= 16777619;
                return h;
            }
        }
    }

    public static class BBRng
    {
        public static uint Next(ref uint state)
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }
    }
}
