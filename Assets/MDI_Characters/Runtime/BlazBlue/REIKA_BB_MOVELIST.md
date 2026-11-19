# C10 Reika: The Crimson Aegis — BlazBlue Combat Specification & Movelist

**Archetype:** Counter / Stance Specialist (Solid Mid-Range Karate Pokes, Guard-Point Stances, Lethal Ripostes)  
**Base Health:** 11,000 HP (Defense Modifier: 1.00x)  
**Movement:** Standard Dash (66, 12 ticks, 8.0 speed), Backdash (44, 13 ticks, 6 ticks invuln), Double Jump, 1 Air Dash  
**Drive Ability:** **Veil** (Resource: Successful Counter Stocks, Guard Point active ticks 4–14)

---

## 1. Drive Mechanic: Veil

Reika is a disciplined counter-striker who neutralizes aggressive opponents with precision Guard Points and devastating Strike-invulnerable auto-ripostes.

- **Veil Counter Stances (D Inputs):**
  - `5D` (Standing Veil Counter): 4 ticks startup, active ticks 4–14. Protects against **Mid and Overhead (Head & Body)** strikes.
  - `2D` (Crouching Veil Counter): 4 ticks startup, active ticks 4–14. Protects against **Low (Foot)** strikes.
  - `j.D` (Air Veil Counter): 4 ticks startup, active ticks 4–14. Protects against airborne attacks.
  - **Whiff Recovery:** A whiffed Veil stance has **22 ticks of vulnerable recovery**.
- **Veil Riposte (Auto-Counter Punish):**
  - When an incoming strike makes contact during an active Guard Point, Reika instantly absorbs the blow and executes **Veil Riposte**.
  - **1,200 damage**, fully **Strike-invulnerable on ticks 1–8**, +4 advantage on block.
- **Overdrive "OD Veil":**
  - Guard Point active window vastly expands to **ticks 2–20** (nearly instantaneous activation and doubled duration).
  - Whiff recovery reduced to 14 ticks.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Reika_5A` (144_01 f10–25) | 6 | 3 | 8 | 250 | Mid | −1 | Mid palm jab; 5A/2A, B/C normals, Veil Stances, Specials, DD |
| **2A** | 2A | `Reika_2A` (144_01 f30–48) | 6 | 3 | 9 | 240 | Low | −2 | Low palm jab; low poke; chainable |
| **6A** | 6A | `Reika_6A` (144_02 f135–160)| 9 | 4 | 15 | 620 | Mid | −6 | High Rising Block Strike; **Head-Invuln f4–12**; Anti-Air; B/C, Specials, DD |
| **5B** | B | `Reika_5B` (144_01 f60–84) | 8 | 4 | 12 | 550 | Mid | −3 | Karate Straight Punch; C, 3C, Veil Stances, Specials, DD; Jump cancel on hit |
| **2B** | 2B | `Reika_2B` (144_01 f95–118) | 8 | 4 | 13 | 520 | Low | −5 | Low Sweep Thrust; C, 3C, Veil Stances, Specials, DD |
| **6B** | 6B | `Reika_6B` (144_01 f130–155)| 19 | 4 | 15 | 680 | Overhead | −5 | Axe Kick Overhead; Specials on hit, DD |
| **5C** | C | `Reika_5C` (144_02 f15–42) | 11 | 4 | 18 | 780 | Mid | −6 | Heavy Karate Chop; **Fatal Counter**; Specials, DD, 2C, 3C |
| **2C** | 2C | `Reika_2C` (144_02 f55–80) | 10 | 4 | 18 | 650 | Low | −8 | Low Sweeping Kick; Knockdown; Specials, DD |
| **3C** | 3C | `Reika_3C` (144_02 f95–122) | 9 | 4 | 16 | 700 | Mid | −7 | Rising Counter Launcher; Jump cancel on hit; airborne followups |
| **j.A** | j.A | `Reika_jA` (118_05 f12–28) | 5 | 3 | 8 | 240 | Overhead | 0 | Air palm strike; j.B, j.C, j.D |
| **j.B** | j.B | `Reika_jB` (118_05 f35–55) | 7 | 4 | 11 | 580 | Overhead | 0 | Air straight kick; j.C, j.D |
| **j.C** | j.C | `Reika_jC` (118_05 f65–88) | 9 | 5 | 14 | 750 | Overhead | 0 | Air heavy down-chop; Knockdown; j.D |
| **5D** | D | `Reika_5D` (144_03 f10–35) | 4 | 11 | 22 | 0 | Mid | −12 | Standing Veil Counter; Guard Point Head/Body f4–14 |
| **2D** | 2D | `Reika_2D` (144_03 f45–70) | 4 | 11 | 22 | 0 | Low | −12 | Low Veil Counter; Guard Point Foot (Low) f4–14 |
| **j.D** | j.D | `Reika_jD` (144_03 f80–105) | 4 | 11 | 22 | 0 | Overhead | −12 | Air Veil Counter; Guard Point Head/Body f4–14 |
| **VeilRiposte**| — | `Reika_Riposte` (144_03 f115)| 5 | 4 | 16 | 1200 | Mid | +4 | Veil Riposte Auto-Punish; Strike-Invuln f1–8 |
| **236B** | 236B | `HumanF@MagicAttackDirect1H01_R`| 12 | 3 | 16 | 480 | Mid | −2 | Crimson Crescent Wave (Speed 14 projectile); DD cancel |
| **41236C**| 41236C| `Reika_41236C` (135_01 f10–45)| 14 | 4 | 18 | 850 | Mid | −4 | Crimson Lunge Punch; Long-range dash strike |
| **623C** | 623C | `Reika_623C` (135_01 f60–90) | 7 | 5 | 22 | 980 | Mid | −16 | Rising Strike DP; Head & Body Invuln f1–8; Reversal |
| **236236C**| 236236C| `Reika_236236C` (135_01 f100)| 1 | 20 | 24 | 2600 | Mid | −12 | Absolute Veil Counter (DD); Guard Point f1–20; 50 Heat |
| **632146C**| 632146C| `Reika_632146C` (135_01 f155)| 12 | 8 | 22 | 2700 | Mid | −14 | Crimson Kata (DD); Full Invuln f1–12; 50 Heat |
| **OD_EA**| ABC (OD)| `HumanF@MagicAttackOmni01` | 10 | 8 | 22 | 2100 | Mid | −14 | Exceed Accel (True Veil Strike); Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Reika_Throw` (144_02 f175) | 7 | 2 | 24 | 1200 | Throw | — | Forward Counter Throw |
| **4BC** | 4B+C | `Reika_Throw` (144_02 f175) | 7 | 2 | 24 | 1200 | Throw | — | Back Counter Throw |
| **j.BC**| j.B+C| `Reika_AirThrow` (118_05) | 7 | 2 | 20 | 1050 | Throw | — | Air Counter Throw |
| **6AB** | 6A+B | `Reika_CounterAssault` (144_02)| 9 | 3 | 18 | 200 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Reika_CrushTrigger` (144_02)| 24 | 4 | 18 | 950 | Mid | 0 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Karate Gatling:** `5A > 5B > 5C` (3 hits, 1580 damage)
2. **Trial 2 — Low Sweep Confirm:** `2A > 2B > 2C` (3 hits, low knockdown confirm)
3. **Trial 3 — Rising Launcher Air Juggle:** `5B > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Axe Kick Overhead Confirm:** `6B (Axe Kick) > 5C > 41236C (Lunge Punch)` (High mixup string)
5. **Trial 5 — Crescent Wave Mid-Range Poke:** `5B > 236B (Crescent Wave)` (Safe poke string)
6. **Trial 6 — Anti-Air Rising Strike Reversal:** `6A (Rising Upper) > 623C (Rising Strike DP)` (Invulnerable anti-air conversion)
7. **Trial 7 — Lunge Punch into Crimson Kata DD:** `5B > 5C > 41236C > 632146C (Crimson Kata)` (Full combo into super)
8. **Trial 8 — Veil Counter Riposte Punish:** `5D (Guard Point) > VeilRiposte > 41236C` (Counter punish conversion)
9. **Trial 9 — Low Counter Riposte String:** `2D (Low Guard Point) > VeilRiposte > 5B > 5C` (Low counter into full gatling string)
10. **Trial 10 — Absolute Veil Counter Overdrive Exceed Accel:** `236236C (Counter DD) > Exceed Accel` (Maximum counter-strike burst)
