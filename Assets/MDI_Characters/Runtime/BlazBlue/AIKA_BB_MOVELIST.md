# C02 Aika: The Karate Zoner — BlazBlue Combat Specification & Movelist

**Archetype:** Karate Zoner (Long Kicks & Occult Nails)  
**Base Health:** 11,000 HP  
**Movement:** Step Dash (66), Super Jump (28/29), Air Dash (66 / 44)  
**Drive Ability:** **Resonance Nail** (Resource: 5 Nail Stocks, 120-Tick Regen)

---

## 1. Drive Mechanic: Resonance Nail

Aika controls space with long-reach karate kicks and throws occult nails that impale the battlefield.

- **Nail Stocks (Max 5):**
  - Starts the match with **5 Nail Stocks**.
  - Passively regains **1 stock every 120 ticks** (2 seconds at 60Hz) when below maximum.
  - Nail stocks are preserved across knockdowns.
- **Drive Throws:**
  - `5D` (Resonance Nail): Throws a straight occult nail (speed 13, dmg 550, costs 1 stock, 8% chip damage).
  - `2D` (Ground Slide Nail): Slides a low nail across the floor (dmg 500, costs 1 stock, hits low).
  - `j.D` (Air Resonance Nail): Fires a nail diagonally downward at 45° (dmg 500, costs 1 stock, hits overhead).
  - Holding `D` delays the throw by up to 20 ticks (feint).
- **Detonation (`6D`):**
  - Detonates all active nails on screen (costs **0 stock**).
  - Each nail explodes for **400 disjoint damage**, controlling entire zones of the screen.
- **Overdrive "OD Resonance":**
  - Nails cost **0 stock** (infinite nails throughout Overdrive).
  - Every nail throw **splits into two nails**, doubling zone coverage and projectile pressure.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Aika_5A` (80_10 f52–68) | 5 | 3 | 9 | 260 | Mid | −2 | 5A/2A (x3), all B/C, Specials, DD |
| **2A** | 2A | `Aika_2A` (135_02 f462–480) | 7 | 3 | 12 | 280 | Low | −3 | 5A/2A (x3), all B/C, Specials, DD |
| **6A** | 6A | `Aika_6A` (135_11 f12–28) | 10 | 4 | 16 | 600 | Mid | −6 | Rising knee; Head-Invulnerable f4–12; Specials, DD |
| **5B** | B | `Aika_5B` (13_18 f73–91) | 8 | 4 | 14 | 640 | Mid | −5 | Karate cross; C, 3C, Specials, DD, Jump Cancel on hit |
| **2B** | 2B | `Aika_2B` (135_02 f470–490) | 9 | 3 | 15 | 560 | Low | −5 | Low front kick; Foot reach 0.56m; C, 3C, Specials, DD |
| **6B** | 6B | `Aika_6B` (143_24 f164–184) | 20 | 4 | 18 | 800 | Overhead | −8 | Overhead axe kick; Specials on hit, DD |
| **5C** | C | `Aika_5C` (113_13 f245–265) | 12 | 4 | 16 | 820 | Mid | −7 | Roundhouse kick; **Fatal Counter**, Specials, DD, 2C, 3C |
| **2C** | 2C | `Aika_2C` (135_02 f492–514) | 10 | 4 | 16 | 720 | Low | −10 | Karate sweep knockdown; Specials, DD |
| **3C** | 3C | `Aika_3C` (113_13 f219–243) | 12 | 5 | 20 | 780 | Mid | −12 | Launcher kick; Jump cancel on hit, Specials, DD |
| **j.A** | j.A | `Aika_jA` (13_18 f280–296) | 5 | 3 | 8 | 280 | Overhead | — | j.B, j.C, j.D |
| **j.B** | j.B | `Aika_jB` (141_14 f44–64) | 8 | 5 | 14 | 780 | Overhead | — | Jump side kick; j.C, j.D |
| **j.C** | j.C | `Aika_jC` (141_14 f80–102) | 9 | 6 | 16 | 920 | Overhead | — | Jump heel drop; Knockdown on air hit; j.D |
| **5D** | D | `MagicAttackDirect1H01_R` | 13 | 1 | 18 | 550 | Mid | −3 | Resonance Nail (Straight); Costs 1 stock; 8% chip |
| **2D** | 2D | `MagicAttackDirect2H01` | 15 | 1 | 20 | 500 | Low | −4 | Ground Slide Nail; Costs 1 stock; Hits low |
| **6D** | 6D | `MagicAttackOmni01` | 18 | 3 | 20 | 400 | Mid | −6 | Resonance Detonate; Detonates all active nails |
| **j.D** | j.D | `Aika_jD` (141_14 f110–126) | 11 | 1 | 16 | 500 | Overhead | — | Air Resonance Nail; 45° down; Costs 1 stock |
| **214B** | 214B | `Aika_214S` (143_24 f34–54) | 10 | 5 | 18 | 860 | Low | −8 | Reap; Low sweep kick; Hard knockdown |
| **46C** | [4]6C | `Aika_Charge` (135_11 f168–192)| 18 | 6 | 22 | 1120 | Mid | −14 | Side kick; Wall bounce in corner |
| **623C** | 623C | `Aika_623S` (135_11 f62–84) | 7 | 6 | 24 | 1080 | Mid | −22 | Rising Dragon Kick; Invuln f1–7; Knockdown DP |
| **236B** | 236B | `Aika_6S` (111_19 f57–75) | 11 | 4 | 18 | 840 | Mid | −8 | Step Thrust Kick; Advances 1.0m |
| **236236C**| 236236C| `Aika_Super` (13_18 f276–316)| 16 | 10 | 24 | 2600 | Mid | −16 | Hundred Kicks (DD); Strike Invuln f1–16; 50 Heat |
| **632146D**| 632146D| `MagicAttackOmni01` | 12 | 30 | 28 | 2400 | Mid | −10 | Nail Rain (DD); Full-screen nails; 15% chip; 50 Heat |
| **OD_EA**| ABC (OD)| `Aika_OD_EA` (13_18 f276–316)| 12 | 8 | 24 | 2200 | Mid | −16 | Exceed Accel; Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Aika_Throw` (111_19 f36–58) | 7 | 2 | 24 | 1200 | Throw | — | Forward Throw |
| **4BC** | 4B+C | `Aika_Throw` (111_19 f36–58) | 7 | 2 | 24 | 1200 | Throw | — | Back Throw |
| **j.BC**| j.B+C| `Aika_AirThrow` (141_14) | 7 | 2 | 20 | 1000 | Throw | — | Air Throw |
| **6AB** | 6A+B | `Aika_CounterAssault` (80_10) | 11 | 3 | 18 | 0 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Aika_CrushTrigger` (135_11) | 24 | 4 | 20 | 1200 | Mid | −2 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Karate Gatling:** `5A > 5B > 5C` (3 hits, 1720 damage)
2. **Trial 2 — Low Footsies String:** `2A > 2B > 2C` (3 hits, long shin reach, sweep knockdown)
3. **Trial 3 — Aerial Launcher:** `5B > 5C > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Nail Setplay:** `5B > 2B > 5D (Nail) > 6D (Detonate)` (4 hits, zoning confirm)
5. **Trial 5 — Low Nail Surprise:** `2A > 2B > 2D (Ground Nail) > 214B (Reap)` (4 hits, double low knockdown)
6. **Trial 6 — Reversal DP Punish:** `6A (Anti-Air Knee) > 623C (Rising Dragon Kick)` (2 hits, hard knockdown)
7. **Trial 7 — Step Kick Wall Carry:** `5C > 236B (Step Thrust Kick) > Rapid Cancel > 46C (Side Kick Wall Bounce)` (3 hits, corner splat)
8. **Trial 8 — Fatal Counter Distortion Drive:** `5C (Fatal Counter) > 236236C (Hundred Kicks)` (Fatal Counter hitstun into 2600 damage DD)
9. **Trial 9 — Rapid Cancel Nail Rain Juggle:** `5C > 3C > j.A > j.B > j.D (Air Nail) > Rapid Cancel > 632146D (Nail Rain)` (7 hits, full screen nail barrage)
10. **Trial 10 — Overdrive Dual-Nail Exceed Accel:** `Overdrive Activation > 5D (Twin Split Nails) > 2D (Twin Split Nails) > 6D (Quad Detonate) > 236236C (Hundred Kicks) > Exceed Accel`
