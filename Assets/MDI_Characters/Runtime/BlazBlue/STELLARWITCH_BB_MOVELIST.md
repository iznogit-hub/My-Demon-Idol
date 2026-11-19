# C07 StellarWitch: The Astral Sorceress — BlazBlue Combat Specification & Movelist

**Archetype:** Fullscreen Zoner / Space Trap Controller (Starlight Comets, Multi-Vector Star Traps, Fullscreen Cosmic Beams)  
**Base Health:** 10,500 HP (Defense Modifier: 1.05x)  
**Movement:** Ground Walk (Forward 3.2, Backward 2.6), Ground Dash (66, 14 ticks, 7.0 speed), Backdash (44, 14 ticks, 6 ticks invuln), Floaty Jump (7.8 vertical, 3.0 horizontal), 1 Air Dash  
**Drive Ability:** **Star Field** (Resource: Up to 3 Active Star Points [4 in OD], Comet Fire commands)

---

## 1. Drive Mechanic: Star Field

StellarWitch commands the celestial cosmos to dominate neutral and control space from fullscreen.

- **Star Point Placements (D Inputs):**
  - `5D` (Star Field: Alpha): Places a mid-height star point at mid-range in front of StellarWitch.
  - `2D` (Star Field: Beta): Places a low-height ground trap star point.
  - `6D` (Star Field: Gamma): Places a high-height anti-air trap star point.
  - `j.D` (Star Field: Delta): Mid-air star point placement.
  - **Lifetime:** Star points remain active on the battlefield for up to 720 ticks (12 seconds) or until detonated / fired.
- **Starlight Comets (Comet Fire):**
  - When star points are active, pressing `4D` (or `5D` when star points exist) commands every active star point to simultaneously fire a **Starlight Comet** (speed 13, 480 damage).
  - Traps the opponent in crossfire angles from anywhere on screen.
- **Supernova Detonation (236C):**
  - Detonates all active star points into massive cosmic explosions dealing 1400 damage each.
- **Overdrive "OD Star Field":**
  - Max active star points increased to **4**.
  - Active star points automatically fire Starlight Comets periodically every **60 ticks (1.0 second)** without being consumed!
  - Comet damage increased to 650.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `StellarWitch_5A` (135_06 f10–25) | 7 | 3 | 8 | 240 | Mid | −1 | Wand Swat; 2A, 5B, 2B, 5C, 2C, 5D |
| **2A** | 2A | `StellarWitch_2A` (135_06 f30–48) | 6 | 3 | 8 | 220 | Low | +1 | Crouching Spark; low poke; chainable |
| **6A** | 6A | `StellarWitch_6A` (135_07 f135–160)| 11 | 5 | 14 | 680 | Mid | −2 | Anti-Air Astral Strike; **Head-Invuln f1–11**; 5C, 2C, 623C |
| **5B** | B | `StellarWitch_5B` (135_06 f60–84) | 10 | 4 | 12 | 580 | Mid | −2 | Starlight Thrust; 2B, 5C, 2C, 5D |
| **2B** | 2B | `StellarWitch_2B` (135_06 f95–118) | 9 | 4 | 14 | 540 | Low | −3 | Ground Astral Sweep; low poke; 5C, 2C, 5D |
| **6B** | 6B | `StellarWitch_6B` (135_06 f130–155)| 21 | 4 | 16 | 780 | Overhead | +2 | Luminescence Crest; Overhead wave; 5C, 5D |
| **5C** | C | `StellarWitch_5C` (135_07 f15–42) | 14 | 5 | 18 | 920 | Mid | −4 | Starlight Beam; **Fatal Counter**; 2C, 3C, 5D, 6D, specials |
| **2C** | 2C | `StellarWitch_2C` (135_07 f55–80) | 12 | 4 | 20 | 840 | Low | −6 | Cosmic Ring Sweep; Knockdown; 3C, 5D, specials |
| **3C** | 3C | `StellarWitch_3C` (135_07 f95–122) | 15 | 5 | 22 | 880 | Mid | −7 | Nova Flare Launcher; Jump cancel on hit; 5D, specials |
| **j.A** | j.A | `StellarWitch_jA` (118_05 f12–28) | 6 | 4 | 9 | 260 | Mid | 0 | Aerial Wand Tap; j.B, j.C, j.D |
| **j.B** | j.B | `StellarWitch_jB` (118_05 f35–55) | 9 | 4 | 12 | 520 | Mid | −1 | Aerial Starlight Flash; j.C, j.D |
| **j.C** | j.C | `StellarWitch_jC` (118_05 f65–88) | 13 | 5 | 16 | 760 | Overhead | +1 | Aerial Comet Slash; Overhead; j.D |
| **j.D** | j.D | `StellarWitch_jD` (135_08 f115) | 14 | 6 | 16 | 550 | Mid | −2 | Star Field Delta (Air placement) |
| **5D** | D | `StellarWitch_5D` (135_08 f10–35) | 16 | 4 | 18 | 480 | Mid | −3 | Star Field Alpha (Mid placement); fires comets if full |
| **2D** | 2D | `StellarWitch_2D` (135_08 f45–70) | 18 | 4 | 20 | 480 | Low | −4 | Star Field Beta (Low ground trap) |
| **6D** | 6D | `StellarWitch_6D` (135_08 f80–105)| 20 | 4 | 22 | 480 | Mid | −5 | Star Field Gamma (High anti-air trap) |
| **CometFire**| 4D | `StellarWitch_CometFire` (135_08)| 12 | 6 | 18 | 480 | Mid | −1 | Starlight Comet (Fires comets from all active star points) |
| **236A**| 236A| `StellarWitch_236A` (135_04 f10) | 15 | 8 | 18 | 700 | Mid | −2 | Astral Bolt Straight (Linear magic projectile) |
| **236B**| 236B| `StellarWitch_236B` (135_04 f50) | 17 | 8 | 19 | 850 | Mid | −3 | Astral Bolt Angled (High trajectory magic projectile) |
| **623C**| 623C| `StellarWitch_623C` (135_04 f90) | 9 | 6 | 28 | 1250 | Mid | −15 | Nova Pillar (Reversal geyser; Full Invuln f1–9) |
| **214B**| 214B| `StellarWitch_214B` (135_04 f135)| 6 | 12 | 14 | 0 | Mid | 0 | Eclipse Shift (Phase teleport escape; Full Invuln f1–12) |
| **236C**| 236C| `StellarWitch_236C` (135_04 f175)| 12 | 8 | 22 | 1400 | Mid | −4 | Supernova (Detonates all active star points) |
| **236236C**| 236236C| `StellarWitch_236236C` (135_05)| 11 | 30 | 35 | 3400 | Mid | −10 | Celestial Cataclysm (Fullscreen 18-hit cosmic beam; 50 Heat) |
| **632146C**| 632146C| `StellarWitch_632146C` (135_05)| 13 | 24 | 32 | 3800 | Mid | −8 | Constellation Convergence (Gravitational vortex; 50 Heat) |
| **OD_EA**| ABCD| `StellarWitch_OD_EA` (135_05) | 10 | 8 | 30 | 2400 | Mid | −12 | Exceed Accel: Supernova Catastrophe (Ends Overdrive) |
| **6BC** | 6B+C | `StellarWitch_Throw` (135_07) | 4 | 2 | 24 | 1050 | Throw | — | Forward Starlight Throw |
| **4BC** | 4B+C | `StellarWitch_BackThrow` (135_07)| 4 | 2 | 24 | 1050 | Throw | — | Backward Starlight Throw |
| **j.BC**| j.B+C| `StellarWitch_AirThrow` (118_05)| 4 | 2 | 24 | 1000 | Throw | — | Aerial Starlight Throw |
| **6AB** | 6A+B | `StellarWitch_CounterAssault` (135_07)| 11 | 4 | 20 | 300 | Mid | −4 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `StellarWitch_CrushTrigger` (135_07)| 24 | 4 | 24 | 1100 | Mid | +2 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Astral Gatling:** `5A > 5B > 5C` (3 hits, 1740 damage)
2. **Trial 2 — Low Trap Confirm:** `2A > 2B > 2C` (3 hits, low knockdown confirm)
3. **Trial 3 — Nova Flare Air Launcher:** `5B > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
4. **Trial 4 — Overhead Luminescence Confirm:** `6B (Overhead) > 5C > 236A (Astral Bolt)` (Overhead confirm string)
5. **Trial 5 — Star Field Setup into Comet Crossfire:** `5D (Place Star) > 236A (Astral Bolt) > 4D (Comet Fire)` (Zoning trap setup)
6. **Trial 6 — Anti-Air Reversal Conversion:** `6A (Anti-Air) > 623C (Nova Pillar)` (Full invulnerable reversal combo)
7. **Trial 7 — Multi-Point Supernova Detonation:** `5D > 2D > 236C (Supernova Detonation)` (Multi-point setplay trap)
8. **Trial 8 — Cosmic Beam Distortion Confirm:** `5B > 5C > 236236C (Celestial Cataclysm)` (Fullscreen super confirm)
9. **Trial 9 — Gravitational Vortex Convergence:** `3C > sj > j.C > land > 632146C (Constellation Convergence)` (High damage vortex conversion)
10. **Trial 10 — Overdrive Star Rain Exceed Accel:** `OD Activate > 5D > 6D > 236236C > OD_EA (Exceed Accel)` (Max damage Overdrive finisher)
