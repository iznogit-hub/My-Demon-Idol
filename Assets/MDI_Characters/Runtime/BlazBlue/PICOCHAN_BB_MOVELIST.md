# C09 PicoChan: The Close Grappler — BlazBlue Combat Specification & Movelist

**Archetype:** Close Grappler (Crowding Hooks, Unblockable Clinch Grabs, Devastating Command Throws)  
**Base Health:** 11,000 HP  
**Movement:** Step Dash (66), Super Jump (28/29), 2 Air Actions  
**Drive Ability:** **Clinch** (Resource: Grip Stocks, Max 2)

---

## 1. Drive Mechanic: Clinch

PicoChan is the shortest fighter in the cast (hip height 0.76m, forearm 0.21m), designed to crowd opponents with heavy hooks and enter devastating close clinches.

- **Clinch Grabs (`5D`, `2D`, `6D`, `j.D`):**
  - `5D` (Clinch): Short unblockable command grab (≈0.9m range, startup 6 ticks, tech window 6 ticks, 1200 damage). Banks **+1 Grip stock** on hit.
  - `2D` (Low Clinch): Low-profile grab that catches crouching or low-posture defenders (startup 8 ticks, 1000 damage). Banks **+1 Grip stock** on hit.
  - `6D` (Run Clinch): Forward step dash into unblockable clinch grab (startup 14 ticks, travels 2.0m, 1100 damage). Banks **+1 Grip stock** on hit.
  - `j.D` (Air Clinch): Mid-air interception grab (startup 5 ticks, 1000 damage). Banks **+1 Grip stock** on hit.
- **Grip Stocks (Max 2):**
  - Landing any D clinch grab banks 1 Grip stock.
  - While **Grip ≥ 1**:
    - **Big Slam Throw (`360C`)**: Deals **+20% bonus damage** (2160 total) and **cannot be teched**, consuming 1 Grip stock.
    - **Pico Press (`632146C DD`)**: Gains **+0.4m extended grab vacuum range** (reaching 1.1m).
- **Overdrive "OD Clinch":**
  - Infinite Grip enhancement (all moves act as if Grip is available).
  - D clinch grabs gain **+0.4m additional range**.
  - D clinch grabs gain **Hyper Armor on startup ticks 1–6**, blowing through defensive pokes.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Pico_5A` (15_13 f638–654) | 5 | 3 | 9 | 320 | Mid | −1 | Lead hook; 5A/2A (x3), all B/C, Specials, DD |
| **2A** | 2A | `Pico_2A` (15_13 f376–394) | 6 | 3 | 11 | 340 | Mid | −2 | Body hook; 5A/2A (x3), all B/C, Specials, DD |
| **6A** | 6A | `Pico_6A` (15_13 f400–418) | 11 | 4 | 16 | 700 | Mid | −7 | Upper cut (AA); Head-Invulnerable f4–12; Specials, DD |
| **5B** | B | `Pico_5B` (15_13 f1256–1274) | 8 | 4 | 13 | 720 | Mid | −4 | Crowd hook; C, 3C, Specials, DD, Jump Cancel on hit |
| **2B** | 2B | `Pico_2B` (15_13 f344–362) | 9 | 4 | 15 | 680 | Low | −8 | Crouch low shin; C, 3C, Specials, DD |
| **6B** | 6B | `Pico_6B` (17_10 f173–193) | 20 | 4 | 18 | 760 | Overhead | −7 | Overhand hook; Specials on hit, DD |
| **5C** | C | `Pico_5C` (Hammer@Attack1 f0–52) | 11 | 4 | 14 | 800 | Mid | −5 | Warhammer Smash; **Fatal Counter**, Specials, DD, 2C, 3C |
| **2C** | 2C | `Pico_2C` (15_13 f340–362) | 11 | 4 | 20 | 700 | Low | −10 | Trip sweep knockdown; Specials, DD |
| **3C** | 3C | `Pico_3C` (15_13 f288–308) | 10 | 4 | 18 | 740 | Mid | −9 | Knee launcher; Jump cancel on hit |
| **j.A** | j.A | `Pico_jA` (15_13 f765–783) | 5 | 3 | 8 | 300 | Overhead | — | Air quick tap; j.B, j.C, j.D |
| **j.B** | j.B | `Pico_jB` (86_08 f312–330) | 8 | 5 | 13 | 740 | Overhead | — | Air diving hook; j.C, j.D |
| **j.C** | j.C | `Pico_jC` (86_08 f944–962) | 9 | 5 | 15 | 860 | Overhead | — | Air hammer drop; Knockdown on air hit; j.D |
| **5D** | D | `Pico_5D` (141_23 f11–33) | 6 | 3 | 18 | 1200 | Unblockable | 0 | Clinch grab; +1 Grip; Tech window 6 ticks |
| **2D** | 2D | `Pico_2D` (141_23 f11–33) | 8 | 3 | 22 | 1000 | Low Grab | 0 | Low Clinch; Grabs crouchers only; +1 Grip |
| **6D** | 6D | `Pico_6D` (141_23 f11–33) | 14 | 3 | 24 | 1100 | Unblockable | 0 | Run Clinch; Advances 2.0m; +1 Grip |
| **j.D** | j.D | `Pico_jD` (86_08 f944–962) | 5 | 3 | 16 | 1000 | Air Grab | 0 | Air Clinch; +1 Grip |
| **236A** | 236A | `Pico_236A` (17_10 f37–57) | 8 | 4 | 16 | 840 | Mid | −4 | Lariat Rush; Rapid cancelable; Chip damage |
| **623C** | 623C | `Pico_623C` (17_10 f149–169) | 6 | 5 | 22 | 1040 | Mid | −18 | Vicious Uppercut; Head & Body Invuln f1–6 |
| **46C** | [4]6C | `Pico_Charge` (86_08 f1502–1524)| 16 | 5 | 20 | 1020 | Mid | −12 | Charged Straight; Armor f4–12; Wall bounce |
| **63214B**| 63214B | `Pico_63214B` (17_10 f173–193)| 11 | 4 | 16 | 860 | Mid | −6 | Step In Hook; Cancels into 5D Clinch on hit |
| **360C** | 360C | `Pico_360C` (111_33 f314–336)| 2 | 2 | 40 | 1800 | Unblockable | 0 | Big Slam Throw; +20% dmg & untechable with Grip |
| **236236B**| 236236B| `Pico_Super` (15_13 f820–854)| 15 | 8 | 22 | 2800 | Mid | −14 | Close Flurry (DD); Strike Invuln f1–15; 50 Heat |
| **632146C**| 632146C| `Pico_Press` (141_23 f11–33) | 1 | 2 | 45 | 3400 | Unblockable | 0 | Pico Press (DD); Full Invuln f1–3; +0.4m range with Grip; 50 Heat |
| **OD_EA**| ABC (OD)| `Pico_OD_EA` (15_13 f820–854)| 12 | 8 | 24 | 2200 | Mid | −16 | Exceed Accel; Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Pico_Throw` (111_33 f314–336)| 7 | 2 | 24 | 1500 | Throw | — | Forward Clinch Slam |
| **4BC** | 4B+C | `Pico_Throw` (111_33 f314–336)| 7 | 2 | 24 | 1500 | Throw | — | Back Clinch Suplex |
| **j.BC**| j.B+C| `Pico_AirThrow` (86_08 f944–962)| 7 | 2 | 20 | 1200 | Throw | — | Air Clinch Throw |
| **6AB** | 6A+B | `Pico_CounterAssault` (15_13) | 9 | 3 | 18 | 200 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Pico_CrushTrigger` (17_10) | 24 | 4 | 18 | 900 | Mid | 0 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Grappler Gatling:** `5A > 5B > 5C` (3 hits, 1840 damage)
2. **Trial 2 — Low Sweep Confirm:** `2A > 2B > 2C` (3 hits, low knockdown confirm)
3. **Trial 3 — Knee Launch Air Chain:** `5B > 5C > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Tick Clinch Grab:** `5A > 5B > 5D (Clinch)` (Tick throw setup into +1 Grip bank)
5. **Trial 5 — Lariat Knockdown:** `2A > 2B > 236A (Lariat Rush)` (3 hits, mid-range confirm)
6. **Trial 6 — Step Hook to Clinch Cancel:** `63214B (Step In Hook) > 5D (Clinch)` (Special into Drive command grab cancel)
7. **Trial 7 — Anti-Air Uppercut Reversal:** `6A (Anti-Air Upper) > 623C (Vicious Uppercut)` (Invulnerable anti-air conversion)
8. **Trial 8 — Fatal Counter Close Flurry:** `5C (Fatal Counter) > 236236B (Close Flurry DD)` (Fatal Counter hitstun into 2800 damage super)
9. **Trial 9 — Air Clinch Reset:** `5B > 3C > j.A > j.D (Air Clinch)` (Airborne reset into Grip bank)
10. **Trial 10 — Grip Bank into Empowered Pico Press:** `5D (Clinch, +1 Grip) > 360C (Big Slam Throw) > 632146C (Pico Press DD)` (Empowered command grab super chain)
