# C01 Akio: The Crimson Shadow — BlazBlue Combat Specification & Movelist

**Archetype:** Rushdown / Mixup  
**Base Health:** 11,000 HP  
**Movement:** Run Dash (66), Super Jump (28/29), Air Dash (66 / 44)  
**Drive Ability:** **Iron Rush** (Resource: 3 Momentum Stocks)

---

## 1. Drive Mechanic: Iron Rush

Akio's `D` button initiates a relentless rush-punch combination that overwhelms defending opponents and banks offensive kinetic energy.

- **Rush Chain:** `5D (1) -> 5D (2) -> 5D (3)`
- **Low & Step Variations:** `2D` (Low Rush) and `6D` (Rush Step, advancing 1.6m).
- **Aerial Drive:** `j.D` (Dive Punch, causes single-use ground bounce on airborne opponents).
- **Momentum Stocks (Max 3):**
  - Connecting with any Drive hit (or block) generates **+1 Momentum Stock** (displayed as 3 HUD pips).
  - Spending Momentum empowers `236C` (Rush Combination: +1 hit and +150 damage per pip) and `632146D` (Crimson Shadow Rush: +300 damage per pip).
  - All banked Momentum drops to **0 upon knockdown**.
- **Overdrive "OD Iron Rush":**
  - Momentum is treated as maxed (infinite 3 stocks throughout OD duration).
  - Chain expands with a lethal 4th strike: `5D (4)` Iron Finisher.
  - All Drive attacks gain **+2 frame advantage on block**.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Akio_5A` (144_20 f110–122) | 6 | 3 | 8 | 300 | Mid | −1 | 5A/2A (x3), all B/C, Specials, DD |
| **2A** | 2A | `Akio_2A` (144_20 f196–214) | 7 | 3 | 11 | 280 | Low | −2 | 5A/2A (x3), all B/C, Specials, DD |
| **6A** | 6A | `Akio_6A` (144_17 f60–84) | 10 | 4 | 16 | 600 | Mid | −6 | Head-Invulnerable f4–12; Specials, DD |
| **5B** | B | `Akio_5B` (144_20 f123–136) | 8 | 4 | 12 | 600 | Mid | −3 | C, 3C, Specials, DD |
| **2B** | 2B | `Akio_2B` (144_20 f220–242) | 9 | 3 | 14 | 520 | Low | −5 | C, 3C, Specials, DD |
| **6B** | 6B | `Akio_6B` (144_20 f148–166) | 20 | 4 | 16 | 750 | Overhead | −6 | Specials on hit, DD |
| **5C** | C | `Akio_5C` (Swordsman@Attack1 f0–35) | 12 | 4 | 16 | 900 | Mid | −6 | **Fatal Counter**, Specials, DD, 3C |
| **2C** | 2C | `Akio_2C` (135_05 f100–120) | 10 | 4 | 18 | 700 | Low | −10 | Sweep knockdown; Specials, DD |
| **3C** | 3C | `Akio_3C` (135_07 f140–164) | 12 | 5 | 18 | 750 | Mid | −10 | Launcher; Jump cancel on hit, Specials, DD |
| **j.A** | j.A | `Akio_jA` (143_23 f42–51) | 5 | 3 | 8 | 320 | Overhead | — | j.B, j.C, j.D |
| **j.B** | j.B | `Akio_jB` (75_16 f40–58) | 8 | 6 | 14 | 700 | Overhead | — | j.C, Jump cancel on hit |
| **j.C** | j.C | `Akio_jC` (75_16 f44–60) | 9 | 6 | 16 | 850 | Overhead | — | Knockdown on air hit; j.D |
| **j.D** | j.D | `Akio_jD` (143_23 f60–80) | 10 | 4 | 12 | 650 | Overhead | — | Dive Punch; Ground bounce (once) |
| **5D** | D | `Akio_5D` (144_20 f34–47) | 6 | 3 | 10 | 350 | Mid | −2 | Iron Rush 1; -> 5D(2), Specials; +1 Momentum |
| **5D_2**| 5D (2) | `Akio_5D_2` (144_20 f123–136)| 7 | 3 | 11 | 450 | Mid | −3 | Iron Rush 2; -> 5D(3) or 2D; +1 Momentum |
| **5D_3**| 5D (3) | `Akio_5D_3` (135_05 f100–120)| 9 | 4 | 14 | 600 | Low | −6 | Iron Rush 3; Knockdown; DD; +1 Momentum |
| **5D_4**| 5D (4) | `Akio_5D_4` (144_17 f24–50) | 8 | 4 | 14 | 750 | Mid | +2 | Iron Finisher (Overdrive Only); Knockdown |
| **2D** | 2D | `Akio_2D` (135_05 f85–105) | 9 | 3 | 14 | 450 | Low | −4 | Low Rush; -> 5D(2) |
| **6D** | 6D | `Akio_6D` (144_17 f20–40) | 22 | 4 | 16 | 700 | Overhead | −5 | Rush Step; Advances 1.6m |
| **236A**| 236A | `Akio_236A` (ThrowCard) | 12 | 1 | 20 | 600 | Mid | −4 | Shadow Kunai (Slow, speed 12, chip 10%) |
| **236B**| 236B | `Akio_236B` (ThrowCard) | 12 | 1 | 20 | 600 | Mid | −4 | Shadow Kunai (Fast, speed 16, chip 10%) |
| **623C**| 623C | `Akio_623C` (135_07 f15–34) | 6 | 6 | 22 | 1000 | Mid | −18 | Rising Fist DP; Invuln f1–8; Knockdown |
| **214B**| 214B | `Akio_214B` (144_17 f40–62) | 14 | 4 | 18 | 800 | Mid | −4 | Shoulder Rush; Projectile Invuln f6–14 |
| **236C**| 236C | `Akio_236C` (144_17 f24–50) | 13 | 8 | 18 | 900+ | Mid | −8 | Rush Combo; +1 hit & +150 dmg per Momentum |
| **236236C**| 236236C| `Akio_Super` (135_06 f48–80)| 14 | 8 | 22 | 2800 | Mid | −12 | Iron Barrage (DD); Strike Invuln f1–14; Min 30% |
| **632146D**| 632146D| `Akio_632146D` (135_06 f48–80)| 10 | 24 | 26 | 3000+| Mid | −20 | Crimson Shadow Rush (DD); +300 dmg per Momentum |
| **OD_EA**| ABC (OD)| `Akio_ExceedAccel` (135_06) | 12 | 8 | 24 | 2200 | Mid | −16 | Exceed Accel; Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Akio_Throw` (144_17 f26–48) | 7 | 2 | 24 | 1200 | Throw | — | Forward Throw |
| **4BC** | 4B+C | `Akio_Throw` (144_17 f26–48) | 7 | 2 | 24 | 1200 | Throw | — | Back Throw |
| **j.BC**| j.B+C| `Akio_AirThrow` (143_23) | 7 | 2 | 20 | 1000 | Throw | — | Air Throw |
| **6AB** | 6A+B | `Akio_CounterAssault` (144_20)| 9 | 3 | 26 | 0 | Mid | — | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Akio_CrushTrigger` (135_06) | 30 | 4 | 20 | 900 | Mid | — | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Gatling:** `5A > 5B > 5C` (3 hits, 1500 damage)
2. **Trial 2 — Low / Sweep Confirms:** `2A > 2B > 2C` (3 hits, sweep knockdown)
3. **Trial 3 — Aerial Launcher:** `5B > 5C > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Momentum Builder:** `5A > 5B > 5D > 5D > 5D` (5 hits, banks 3 Momentum stocks)
5. **Trial 5 — Special Frame Trap:** `5B > 5C > 236A` (Shadow Kunai projectile blockstring / hit confirm)
6. **Trial 6 — Reversal DP Punish:** `6A (Anti-Air) > 623C (Rising Fist)` (2 hits, hard knockdown)
7. **Trial 7 — Momentum Spend Combo:** `5B > 2B > 5D > 5D > 236C` (spends 2 Momentum, 7 hits, 2400 damage)
8. **Trial 8 — Fatal Counter Distortion Drive:** `5C (Fatal Counter) > 236236C (Iron Barrage)` (Fatal Counter hitstun into 2800 damage super)
9. **Trial 9 — Rapid Cancel Juggle Extender:** `5C > 623C > Rapid Cancel > 5C > 3C > j.A > j.B > j.C > j.D (Ground Bounce) > 5C > 236236C` (full corner carry)
10. **Trial 10 — Overdrive Exceed Accel Finisher:** `Overdrive Activation > 5D > 5D > 5D > 5D (Iron Finisher) > 632146D (Crimson Shadow Rush) > Exceed Accel`
