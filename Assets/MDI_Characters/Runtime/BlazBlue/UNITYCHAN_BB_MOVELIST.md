# C04 UnityChan: The Aerial Hopper — BlazBlue Combat Specification & Movelist

**Archetype:** Aerial Hopper (High Mobility & Vertical Zone Dominance)  
**Base Health:** 11,000 HP  
**Movement:** Hop Dash (66), Super Jump (28/29), 2 Air Actions (Air Dash / Double Jump)  
**Drive Ability:** **Aero Hop** (Resource: None / Uncapped Aerial Movement)

---

## 1. Drive Mechanic: Aero Hop

UnityChan dominates the vertical space with agile hopping maneuvers and projectile discs that manipulate aerial momentum.

- **Directional Hops:**
  - `5D` (Vertical Hop): Leaps straight up (airborne from tick 4, **Foot-invulnerable ticks 4–18**, actionable from tick 10 to punish low sweeps).
  - `6D` (Forward Leap): Advances forward with an agile hop (10/5/14, 760 damage, cross-up capability against crouching defenders).
  - `4D` (Back Hop): Retreats defensively (airborne from tick 3, **Projectile-invulnerable ticks 3–10**, actionable from tick 12).
- **Aerial Drive (`j.D`):**
  - Performs an **extra air hop** in mid-air that does **not** consume one of her two standard air actions.
  - Can be executed once per jump (resets immediately upon landing).
  - Fires an **Aero Disc** projectile forward at point-blank range (300 damage).
- **Overdrive "OD Aero":**
  - **Unlimited `j.D` air hops** (can chain multiple air hops to stall or maneuver across the entire screen ceiling).
  - Every hop (`5D`, `6D`, `4D`, `j.D`) automatically releases an **Aero Disc**, creating a barrage of zoning projectiles.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `UChan_5P` (02_05 f92–108) | 4 | 3 | 8 | 220 | Mid | −1 | Ultra-fast tap jab; 5A/2A (x3), all B/C, Specials, DD |
| **2A** | 2A | `UChan_2P` (02_05 f212–230) | 6 | 3 | 10 | 240 | Low | −2 | Crouch poke; 5A/2A (x3), all B/C, Specials, DD |
| **6A** | 6A | `UChan_6A` (16_05 f10–26) | 9 | 4 | 14 | 560 | Mid | −6 | Hop knee; Head-Invulnerable f3–12; Specials, DD |
| **5B** | B | `UChan_5M` (02_05 f144–162) | 7 | 3 | 12 | 550 | Mid | −3 | Hopping straight; C, 3C, Specials, DD, Jump Cancel on hit |
| **2B** | 2B | `UChan_2M` (14_02 f630–648) | 8 | 3 | 14 | 580 | Low | −7 | Low check kick; C, 3C, Specials, DD |
| **6B** | 6B | `UChan_6M` (16_05 f20–40) | 18 | 4 | 14 | 680 | Overhead | −6 | Hop overhead; Specials on hit, DD |
| **5C** | C | `UnityChan_5C` (Archer@Attack1 f0–30)| 11 | 4 | 14 | 650 | Mid | −5 | Archer shot; **Fatal Counter**, Specials, DD, 2C, 3C |
| **2C** | 2C | `UChan_2C` (14_02 f635–655) | 10 | 4 | 18 | 620 | Low | −10 | Low sweep knockdown; Specials, DD |
| **3C** | 3C | `UChan_2S` (16_01 f16–40) | 9 | 5 | 16 | 700 | Mid | −8 | Hop launcher; Airborne f6+; Jump cancel on hit |
| **j.A** | j.A | `UChan_jP` (02_05 f124–140) | 4 | 3 | 8 | 240 | Overhead | — | Air tap; j.B, j.C, j.D |
| **j.B** | j.B | `UChan_jM` (118_01 f88–106) | 7 | 5 | 12 | 700 | Overhead | — | Air side kick; j.C, j.D |
| **j.C** | j.C | `UChan_jS` (118_05 f64–82) | 8 | 5 | 14 | 820 | Overhead | — | Air stomp; Knockdown on air hit; j.D |
| **5D** | D | `UChan_5D` (16_01 f16–40) | 4 | 1 | 12 | 0 | Mid | 0 | Vertical Aero Hop; Foot-Invulnerable f4–18 |
| **6D** | 6D | `UChan_6D` (91_39 f24–44) | 10 | 5 | 14 | 760 | Mid | −5 | Forward Aero Leap; Cross-up on crouch |
| **4D** | 4D | `UChan_4D` (16_35 f10–30) | 3 | 1 | 14 | 0 | Mid | 0 | Aero Back Hop; Projectile-Invulnerable f3–10 |
| **j.D** | j.D | `UChan_jD` (118_01 f95–115) | 6 | 1 | 10 | 300 | Mid | — | Extra Air Hop + Aero Disc; Once per jump (Unlimited in OD) |
| **236A** | 236A | `MagicAttackDirect1H01_R` | 11 | 1 | 16 | 440 | Mid | −2 | Aero Plasma Disc (Horizontal, speed 14) |
| **236B** | 236B | `MagicAttackDirect1H01_R` | 11 | 1 | 16 | 440 | Mid | −2 | Aero Plasma Disc (Angled 30° upward) |
| **623C** | 623C | `UChan_623S` (16_05 f30–50) | 6 | 5 | 20 | 900 | Mid | −14 | Rising Hop DP; Invuln f1–6; Hard knockdown |
| **214B** | 214B | `UChan_214S` (91_39 f50–70) | 9 | 4 | 16 | 740 | Mid | −6 | Bound kick; Mid-range hop poke |
| **46C** | [4]6C | `UChan_Charge` (118_05 f47–70)| 14 | 5 | 18 | 880 | Overhead | −9 | Hop Stomp; Overhead knockdown |
| **j.214B**| j.214B| `UChan_j214B` (118_05 f70–92)| 8 | 6 | 12 | 700 | Overhead | — | Air Dive Kick; Ground bounce on air hit |
| **236236C**| 236236C| `UChan_Super` (14_02 f811–845)| 14 | 8 | 20 | 2400 | Mid | −12 | Aero Flurry (DD); Strike Invuln f1–14; 50 Heat |
| **j.236236C**| j.236236C| `UChan_j236236C` (118_01 f88–118)| 10 | 20 | 18 | 2200 | Mid | — | Disc Storm (Air DD); Full-screen air barrage; 50 Heat |
| **OD_EA**| ABC (OD)| `UChan_OD_EA` (14_02 f811–845)| 10 | 8 | 22 | 2000 | Mid | −14 | Exceed Accel; Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `UChan_Throw` (02_05 f72–94) | 7 | 2 | 24 | 1100 | Throw | — | Forward Throw |
| **4BC** | 4B+C | `UChan_Throw` (02_05 f72–94) | 7 | 2 | 24 | 1100 | Throw | — | Back Throw |
| **j.BC**| j.B+C| `UChan_AirThrow` (118_01) | 7 | 2 | 20 | 1000 | Throw | — | Air Throw |
| **6AB** | 6A+B | `UChan_CounterAssault` (02_05)| 11 | 3 | 18 | 0 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `UChan_CrushTrigger` (16_05) | 24 | 4 | 20 | 1200 | Mid | −2 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Hopper Gatling:** `5A > 5B > 5C` (3 hits, 1420 damage)
2. **Trial 2 — Low Confirms:** `2A > 2B > 2C` (3 hits, low sweep knockdown)
3. **Trial 3 — Aerial Launcher:** `5B > 5C > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Hop Low Crush:** `5D (Vertical Hop) > j.A > j.B > j.C` (4 hits, evades low sweep)
5. **Trial 5 — Projectile Disc Pressure:** `5B > 5C > 236A (Aero Plasma Disc)` (3 hits, blockstring & confirm)
6. **Trial 6 — Reversal DP Punish:** `6A (Anti-Air Knee) > 623C (Rising Hop Dragon)` (2 hits, DP confirm)
7. **Trial 7 — Air Dive Ground Bounce:** `3C > j.A > j.B > j.214B (Air Dive) > 5B > 623C` (6 hits, ground bounce juggle)
8. **Trial 8 — Fatal Counter Distortion Drive:** `5C (Fatal Counter) > 236236C (Aero Flurry)` (Fatal Counter hitstun into 2400 damage super)
9. **Trial 9 — Air Disc Storm Finisher:** `3C > j.A > j.B > j.D (Air Hop) > Rapid Cancel > j.236236C (Disc Storm)` (6 hits, full ceiling air juggle)
10. **Trial 10 — Overdrive Multi-Hop Exceed Accel:** `Overdrive Activation > 5D > 6D > 236236C (Aero Flurry) > Exceed Accel`
