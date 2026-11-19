# C12 Fina: The Footsie Specialist — BlazBlue Combat Specification & Movelist

**Archetype:** Footsies / All-Rounder (Precision Mid-Range Pokes, Multi-Tier Plasma Control, Space Dominance)  
**Base Health:** 11,000 HP  
**Movement:** Standard Dash (66), Super Jump (28/29), 2 Air Actions  
**Drive Ability:** **Plasma Charge** (Resource: Charge Level 1–3 & Stored State)

---

## 1. Drive Mechanic: Plasma Charge

Fina possesses the longest and crispest normal pokes in the cast (`5B` and `2B`), backed by versatile plasma manipulation.

- **Plasma Charging (Holding D):**
  - **Level 1 (20 ticks):** Standard Neo-Pink Plasma Burst (520 damage, fast startup projectile).
  - **Level 2 (45 ticks):** Accelerated high-density burst (+25% damage, 650 damage).
  - **Level 3 (75 ticks):** Heavy 2-hit, projectile-beating plasma beam (800 damage).
- **Directional Plasma Releases:**
  - `5D`: Fires plasma projectile horizontally across the arena.
  - `2D`: Fires low-angle ground plasma skimming the floor (hits Low).
  - `6D`: Overhead hop mixup punch (16/4/14, 680 damage, Overhead).
  - `j.D`: Mid-air angled plasma shot covering jump descents.
- **Charge Storing:**
  - Pressing `A`, `B`, or `C` while charging stores the current charge level instead of releasing it.
  - Stored charge empowers Fina's next special attack (`236A/B`, `623C`, `214B`) with **+1 extra hit and +25% bonus damage**.
- **Overdrive "OD Plasma":**
  - All plasma charges release **instantly at Level 3** with no charging delay required.
  - Stored specials always trigger at maximum power.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Fina_5A` (79_01 f10–26) | 5 | 3 | 8 | 280 | Mid | −1 | Precision lead jab; 5A/2A (x3), all B/C, Specials, DD |
| **2A** | 2A | `Fina_2A` (79_01 f35–53) | 6 | 3 | 9 | 290 | Low | −2 | Crouch low jab; 5A/2A (x3), all B/C, Specials, DD |
| **6A** | 6A | `Fina_6A` (79_01 f60–80) | 9 | 4 | 14 | 620 | Mid | −6 | Anti-Air upper; Head-Invulnerable f3–11; Specials, DD |
| **5B** | B | `Fina_5B` (79_01 f90–112) | 7 | 4 | 12 | 600 | Mid | −3 | Precision straight poke; Best 5B reach in cast; C, 3C, Specials, DD, Jump Cancel on hit |
| **2B** | 2B | `Fina_2B` (79_01 f120–140) | 7 | 4 | 13 | 580 | Low | −6 | Low footsie poke; Best 2B reach in cast; C, 3C, Specials, DD |
| **6B** | 6B | `Fina_6B` (80_01 f15–38) | 19 | 4 | 16 | 720 | Overhead | −6 | Step overhead punch; Specials on hit, DD |
| **5C** | C | `Fina_5C` (2Handed@Attack1 f0–40) | 10 | 4 | 14 | 780 | Mid | −4 | Greatsword Cleave; **Fatal Counter**, Specials, DD, 2C, 3C |
| **2C** | 2C | `Fina_2C` (80_01 f85–109) | 10 | 4 | 18 | 680 | Low | −9 | Low sweep knockdown; Specials, DD |
| **3C** | 3C | `Fina_3C` (80_01 f120–145) | 9 | 4 | 16 | 720 | Mid | −8 | Rising kick launcher; Jump cancel on hit |
| **j.A** | j.A | `Fina_jA` (80_02 f12–30) | 5 | 3 | 8 | 260 | Overhead | — | Air quick tap; j.B, j.C, j.D |
| **j.B** | j.B | `Fina_jB` (80_02 f38–58) | 7 | 5 | 12 | 650 | Overhead | — | Air horizontal poke; j.C, j.D |
| **j.C** | j.C | `Fina_jC` (80_02 f68–90) | 9 | 5 | 14 | 800 | Overhead | — | Air heavy straight; Knockdown on air hit; j.D |
| **5D** | D | `MagicAttackDirect1H01_R` | 12 | 2 | 16 | 520 | Mid | −2 | Plasma Shot; Hold D to charge (L1: 20t, L2: 45t, L3: 75t) |
| **2D** | 2D | `MagicAttackDirect1H01_R` | 12 | 2 | 18 | 460 | Low | −4 | Low Plasma Shot (Ground skimmer) |
| **6D** | 6D | `Fina_6D` (16_02 f10–32) | 16 | 4 | 14 | 680 | Overhead | −5 | Overhead hop mixup; Specials on hit |
| **j.D** | j.D | `MagicAttackDirect1H01_R` | 10 | 2 | 14 | 420 | Overhead | — | Air Plasma Shot (45° downwards) |
| **236A** | 236A | `MagicAttackDirect1H01_R` | 11 | 2 | 16 | 520 | Mid | −2 | Plasma Fireball A (Speed 16) |
| **236B** | 236B | `MagicAttackDirect2H01` | 14 | 3 | 18 | 680 | Mid | −3 | Plasma Fireball B (2-hit heavy burst) |
| **623C** | 623C | `Fina_623C` (135_03 f15–38) | 6 | 5 | 22 | 980 | Mid | −16 | Rising Kick DP; Head & Body Invuln f1–6; Hard knockdown |
| **214B** | 214B | `Fina_214B` (135_03 f50–75) | 10 | 4 | 15 | 760 | Mid | −5 | Advancing Stepping Kick; Gap closer poke |
| **236236C**| 236236C| `MagicAttackOmni01` | 12 | 10 | 22 | 2600 | Mid | −12 | Neo-Pink Plasma Burst (Beam DD); Invuln f1–12; 50 Heat |
| **632146D**| 632146D| `Fina_Super` (80_01 f160–195)| 14 | 8 | 22 | 2700 | Mid | −14 | Crimson Rush (DD); Invuln f1–14; 50 Heat |
| **OD_EA**| ABC (OD)| `Fina_OD_EA` (80_01 f160–195)| 10 | 8 | 22 | 2100 | Mid | −14 | Exceed Accel; Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Fina_Throw` (135_03 f90–114) | 7 | 2 | 24 | 1300 | Throw | — | Forward Footsie Throw |
| **4BC** | 4B+C | `Fina_Throw` (135_03 f90–114) | 7 | 2 | 24 | 1300 | Throw | — | Back Footsie Throw |
| **j.BC**| j.B+C| `Fina_AirThrow` (80_02 f38–58)| 7 | 2 | 20 | 1100 | Throw | — | Air Footsie Throw |
| **6AB** | 6A+B | `Fina_CounterAssault` (79_01) | 9 | 3 | 18 | 200 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Fina_CrushTrigger` (16_02) | 24 | 4 | 18 | 1000 | Mid | 0 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Footsie Gatling:** `5A > 5B > 5C` (3 hits, 1660 damage)
2. **Trial 2 — Low Footsie Confirm:** `2A > 2B > 2C` (3 hits, low knockdown confirm)
3. **Trial 3 — Launcher Aerial Juggle:** `5B > 5C > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Footsie Poke into Plasma Fireball:** `5B > 236A (Plasma Fireball)` (Mid-range zoning confirm)
5. **Trial 5 — Low Confirm into Advancing Stepping Kick:** `2B > 214B (Stepping Kick)` (Corner carry footsie string)
6. **Trial 6 — Anti-Air Reversal Conversion:** `6A (Anti-Air Upper) > 623C (Rising Kick DP)` (Invulnerable anti-air confirm)
7. **Trial 7 — Overhead Hop Mixup Confirm:** `6D (Overhead Hop) > 5B > 5C` (High/low mixup confirm)
8. **Trial 8 — Fatal Counter into Neo-Pink Plasma Burst:** `5C (Fatal Counter) > 236236C (Beam DD)` (2600 damage super confirm)
9. **Trial 9 — Plasma Shot into Crimson Rush DD:** `5B > 5C > 5D > 632146D (Crimson Rush)` (Full combo into rush Distortion Drive)
10. **Trial 10 — Stored Plasma Level 3 Overdrive Exceed Accel:** `5D (Store Level 3) > 236B (Empowered 3-Hit Fireball) > 236236C > Exceed Accel` (Maximum overdrive burst punish)
