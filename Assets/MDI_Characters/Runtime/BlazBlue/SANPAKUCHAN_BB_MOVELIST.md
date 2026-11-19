# C08 SanpakuChan: The Shadow Feather — BlazBlue Combat Specification & Movelist

**Archetype:** Speed / Mixup Ninja (Fastest 16t Overhead, Cross-Up Phasing, 3-Hit Rekkas, Multi-Angle Teleports)  
**Base Health:** 10,500 HP (Defense Modifier: 1.08x, Low damage scaling 0.85x)  
**Movement:** Ninja Step Dash (66, 10 ticks, 9.0 speed), Backdash (44, 11 ticks, 6 ticks invuln), Double Jump, 2 Air Dashes  
**Drive Ability:** **Crow Step** (Resource: 3 Feather Stocks, 150-tick regeneration)

---

## 1. Drive Mechanic: Crow Step

SanpakuChan possesses high movement agility and tricky spatial phasing, using her feather stocks to reposition across the screen and confuse the opponent's guard.

- **Feather Stocks:**
  - Manages **3 Feather Stocks**.
  - Passively regenerates 1 feather every **150 ticks** (2.5 seconds at 60 FPS).
- **Directional Crow Steps (D Inputs):**
  - `5D` (Forward Cross-Up Teleport): 14 ticks startup. Disappears on tick 4, reappears behind the opponent. **Strike-invulnerable on ticks 4–8**.
  - `4D` (Back Teleport): 10 ticks startup. Fast retreat escape to fullscreen safety.
  - `2D` (Low Dash-Slide): 10 ticks startup. Teleport slide ducking under high projectiles and standing jabs.
  - `j.D` (Air Teleport): 8 ticks startup. Instant mid-air repositioning to alter jump trajectory and avoid anti-airs.
- **Overdrive "OD Crow":**
  - All Crow Step teleports cost **0 feathers** and startup **6 ticks faster**.
  - Teleport recovery is cut by 4 ticks, allowing immediate attack follow-ups on exit.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Sanpaku_5A` (14_01 f10–24) | 4 | 3 | 7 | 200 | Mid | −1 | Swift jab; 5A/2A, B/C normals, Crow Steps, Specials, DD |
| **2A** | 2A | `Sanpaku_2A` (14_01 f30–46) | 5 | 3 | 8 | 210 | Low | −2 | Crouch quick poke; low check; chainable |
| **6A** | 6A | `Sanpaku_6A` (14_01 f55–75) | 8 | 4 | 12 | 540 | Mid | −6 | Rising Anti-Air Upper; Head-Invuln f3–10; B/C, Specials, DD |
| **5B** | B | `Sanpaku_5B` (14_01 f85–105) | 6 | 4 | 11 | 480 | Mid | −3 | Speed Cross; C, 3C, Crow Steps, Specials, DD; Jump cancel on hit |
| **2B** | 2B | `Sanpaku_2B` (14_01 f115–133) | 6 | 4 | 12 | 460 | Low | −5 | Low Shin Check; C, 3C, Crow Steps, Specials, DD |
| **6B** | 6B | `Sanpaku_6B` (118_02 f10–26) | 16 | 3 | 14 | 600 | Overhead | −4 | Instant Hop Overhead (Fastest in cast!); C, Specials, DD |
| **5C** | C | `Sanpaku_5C` (Ninja@Attack1 f0–26) | 9 | 4 | 13 | 650 | Mid | −4 | Twin Ninjato Slice; **Fatal Counter**; Specials, DD, Crow Steps |
| **2C** | 2C | `Sanpaku_2C` (14_03 f45–67) | 8 | 4 | 16 | 580 | Low | −8 | Slide Sweep; Knockdown; Specials, DD, Crow Steps |
| **3C** | 3C | `Sanpaku_3C` (14_03 f75–98) | 8 | 4 | 15 | 620 | Mid | −7 | Launcher Kick; Jump cancel on hit; airborne followups |
| **j.A** | j.A | `Sanpaku_jA` (118_02 f35–50) | 4 | 3 | 7 | 220 | Overhead | 0 | Air quick tap; j.B, j.C, j.D, j.236A |
| **j.B** | j.B | `Sanpaku_jB` (118_02 f60–78) | 6 | 4 | 10 | 550 | Overhead | 0 | Air swift poke; j.C, j.D, j.236A |
| **j.C** | j.C | `Sanpaku_jC` (118_02 f85–105) | 8 | 5 | 13 | 700 | Overhead | 0 | Air dive straight; j.D, j.236A |
| **5D** | D | `Sanpaku_5D` (91_01 f10–24) | 14 | 1 | 12 | 0 | Mid | 0 | Crow Step (Cross-up teleport); Invuln f4–8; Costs 1 Feather |
| **4D** | 4D | `Sanpaku_4D` (91_01 f30–44) | 10 | 1 | 12 | 0 | Mid | 0 | Crow Step (Back teleport retreat); Costs 1 Feather |
| **2D** | 2D | `Sanpaku_2D` (91_01 f50–64) | 10 | 1 | 14 | 0 | Low | 0 | Crow Step (Low slide teleport); Costs 1 Feather |
| **j.D** | j.D | `Sanpaku_jD` (91_01 f70–84) | 8 | 1 | 10 | 0 | Mid | 0 | Crow Step (Air reposition); Costs 1 Feather |
| **236A** | 236A | `HumanF@MagicAttackDirect1H01_R` | 11 | 2 | 15 | 420 | Mid | −2 | Crow Feather Needle (Horizontal projectile); DD cancel |
| **j.236A**| j.236A| `HumanF@MagicAttackDirect1H01_R` | 10 | 2 | 14 | 400 | Overhead | 0 | Air Feather Needle (45° downwards needle); DD cancel |
| **214B** | 214B | `Sanpaku_214B_1` (141_15 f10–30)| 9 | 4 | 14 | 550 | Mid | −4 | Rekka Dive 1st; Cancels into 214B_2, DD |
| **214B_2**| 214B | `Sanpaku_214B_2` (141_15 f35–55)| 8 | 4 | 14 | 550 | Mid | −5 | Rekka Dive 2nd; Cancels into 214B_3, DD |
| **214B_3**| 214B | `Sanpaku_214B_3` (141_15 f60–82)| 10 | 4 | 18 | 650 | Mid | −8 | Rekka Dive 3rd Finisher; Hard knockdown, DD |
| **236236B**| 236236B| `Sanpaku_Super` (16_10 f15–48) | 12 | 8 | 20 | 2400 | Mid | −12 | Shadow Dance (DD); Full Invuln f1–12; 50 Heat |
| **214214D**| 214214D| `Sanpaku_214214D` (91_01 f90–125)| 10 | 8 | 22 | 2600 | Mid | −14 | Crow Phantasm (Cross-up DD); Full Invuln f1–10; 50 Heat |
| **OD_EA**| ABC (OD)| `Sanpaku_OD_EA` (16_10 f15–48) | 10 | 8 | 22 | 2000 | Mid | −14 | Exceed Accel; Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Sanpaku_Throw` (14_03 f110–132)| 7 | 2 | 24 | 1200 | Throw | — | Forward Phantasm Throw |
| **4BC** | 4B+C | `Sanpaku_Throw` (14_03 f110–132)| 7 | 2 | 24 | 1200 | Throw | — | Back Phantasm Throw |
| **j.BC**| j.B+C| `Sanpaku_AirThrow` (118_02) | 7 | 2 | 20 | 1000 | Throw | — | Air Phantasm Throw |
| **6AB** | 6A+B | `Sanpaku_CounterAssault` (14_01) | 9 | 3 | 18 | 200 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Sanpaku_CrushTrigger` (118_02) | 24 | 4 | 18 | 900 | Mid | 0 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Speed Gatling:** `5A > 5B > 5C` (3 hits, 1330 damage)
2. **Trial 2 — Low Mixup Confirm:** `2A > 2B > 2C` (3 hits, low knockdown confirm)
3. **Trial 3 — Instant Hop Overhead Confirm:** `6B > 5C > 214B` (Fast 16t overhead into Rekka 1)
4. **Trial 4 — Launcher Air Juggle:** `5B > 3C > sj > j.A > j.B > j.C` (5 hits, launcher aerial sequence)
5. **Trial 5 — Cross-Up Ambush Confirm:** `5D (Cross-Up Crow Step) > 5B > 5C` (Spatial phasing crossup confirm)
6. **Trial 6 — Complete 3-Hit Rekka Series:** `5B > 5C > 214B > 214B_2 > 214B_3` (Full rekka corner carry)
7. **Trial 7 — Feather Needle Teleport Reset:** `236A > 5D > 5A` (Projectile into crossup restand)
8. **Trial 8 — Aerial Needle Phasing Harass:** `j.A > j.236A > j.D` (Air hit into needle into air teleport escape)
9. **Trial 9 — Rekka into Shadow Dance DD:** `5B > 5C > 214B > 236236B (Shadow Dance)` (Super cancel confirm)
10. **Trial 10 — Fatal Counter Crow Phantasm Exceed Accel:** `5C (Fatal Counter) > 214214D (Crow Phantasm DD) > Exceed Accel` (Maximum ninja burst punish)
