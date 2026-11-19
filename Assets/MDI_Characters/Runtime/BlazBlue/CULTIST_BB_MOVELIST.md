# C06 Cultist: The Void Weaver — BlazBlue Combat Specification & Movelist

**Archetype:** Setplay / Summoner (Disjointed Dark Energy Pokes, Void Skull Orb Spatial Control, Multi-Angle Teleports)  
**Base Health:** 11,000 HP (Defense Modifier: 1.00x)  
**Movement:** Standard Dash (66, 12 ticks, 7.5 speed), Backdash (44, 13 ticks, 6 ticks invuln), Double Jump, 1 Air Dash  
**Drive Ability:** **Void Orb** (Resource: Void Skull Orbs, max 2 active on screen, 3 in Overdrive)

---

## 1. Drive Mechanic: Void Orb

Cultist controls spacetime by summoning dark energy orbs that hover in the arena, restricting the opponent's movement options and setting up lethal sandwich traps.

- **Void Orb Management:**
  - Can sustain up to **2 active Void Orbs** simultaneously (expands to **3 orbs in Overdrive**).
  - Orbs persist on screen for up to 600 ticks (10 seconds) or until detonated / destroyed.
  - Orbs possess a 1-hit hurtbox and can be dispelled by opponent strikes.
- **Directional Void Commands (D Inputs):**
  - `5D` (Place Void Orb): 14 ticks startup. Summons an orb directly ahead of Cultist at torso height.
  - `6D` (Dash Command): 10 ticks startup. Commands all active orbs to surge forward at speed 8.0 across the screen.
  - `2D` (Drop Command): 10 ticks startup. Commands all active orbs to drop rapidly to the floor, creating low tripwire traps.
  - `4D` (Recall Command): 10 ticks startup. Recalls all active orbs back toward Cultist's current position.
  - `j.D` (Air Place): 12 ticks startup. Summons an orb in mid-air to restrict airborne approaches.
- **Orb Synergies:**
  - `22B / 22C` (Void Warp): Teleports Cultist instantly to the location of Orb 1 or Orb 2.
  - `236236C` (Void Cataclysm): Detonates all active orbs into massive dark energy explosions (2500 base + 500 bonus per orb).
- **Overdrive "OD Void":**
  - Maximum active orbs increased to **3**.
  - Active orbs **automatically fire** Void Skull projectiles every 60 ticks without requiring manual command inputs.

---

## 2. Complete Movelist & Frame Data

| Move ID | Input | Animation Clip / Take | Startup | Active | Recovery | Damage | Guard | On-Block Adv | Cancels & Notes |
|---|---|---|---|---|---|---|---|---|---|
| **5A** | A | `Cultist_5A` (79_04 f10–25) | 6 | 3 | 8 | 220 | Mid | −1 | Dark lead jab; 5A/2A, B/C normals, Void Commands, Specials, DD |
| **2A** | 2A | `Cultist_2A` (79_04 f30–48) | 7 | 3 | 9 | 230 | Low | −2 | Low dark poke; chainable |
| **6A** | 6A | `Cultist_6A` (135_04 f15–38) | 10 | 4 | 15 | 650 | Mid | −6 | Dark Energy Rising Strike; **Head-Invuln f4–12**; Anti-Air; B/C, Specials, DD |
| **5B** | B | `Cultist_5B` (79_04 f60–84) | 8 | 4 | 13 | 520 | Mid | −4 | Curse Straight; Disjointed dark fist; C, 3C, Void Commands, Specials, DD; Jump cancel on hit |
| **2B** | 2B | `Cultist_2B` (79_04 f95–118) | 8 | 4 | 14 | 500 | Low | −5 | Low Curse Sweep Hook; C, 3C, Void Commands, Specials, DD |
| **6B** | 6B | `Cultist_6B` (79_04 f130–155)| 20 | 4 | 16 | 700 | Overhead | −6 | Stepping Curse Overhead; Specials on hit, DD |
| **5C** | C | `Cultist_5C` (135_04 f50–75) | 11 | 4 | 16 | 750 | Mid | −5 | Abyssal Slash; **Fatal Counter**; Void Commands, Specials, DD, 2C, 3C |
| **2C** | 2C | `Cultist_2C` (135_04 f90–116)| 10 | 4 | 19 | 620 | Low | −9 | Low Ground Scythe; Knockdown; Void Commands, Specials, DD |
| **3C** | 3C | `Cultist_3C` (135_04 f130–158)| 10 | 4 | 17 | 680 | Mid | −8 | Rising Void Launcher; Jump cancel on hit; airborne followups |
| **j.A** | j.A | `Cultist_jA` (118_04 f12–28) | 5 | 3 | 8 | 240 | Overhead | 0 | Air quick rune; j.B, j.C, j.D |
| **j.B** | j.B | `Cultist_jB` (118_04 f35–55) | 7 | 4 | 11 | 580 | Overhead | 0 | Air curse poke; j.C, j.D |
| **j.C** | j.C | `Cultist_jC` (118_04 f65–88) | 9 | 5 | 14 | 750 | Overhead | 0 | Air Abyssal Scythe; Heavy airborne knockdown; j.D |
| **5D** | D | `HumanF@CastingEnter01` | 14 | 2 | 16 | 0 | Mid | −2 | Void Orb (Place); Places hovering orb ahead |
| **6D** | 6D | `HumanF@CastingIdle01` | 10 | 2 | 14 | 0 | Mid | 0 | Void Orb (Dash Command); Accelerates active orbs forward |
| **2D** | 2D | `HumanF@CastingEnter01` | 10 | 2 | 14 | 0 | Low | 0 | Void Orb (Drop Command); Drops active orbs to floor |
| **4D** | 4D | `HumanF@CastingIdle01` | 10 | 2 | 14 | 0 | Mid | 0 | Void Orb (Recall Command); Pulls active orbs back |
| **j.D** | j.D | `HumanF@CastingEnter01` | 12 | 2 | 14 | 0 | Overhead | 0 | Void Orb (Air Place); Places mid-air orb |
| **236B** | 236B | `HumanF@MagicAttackDirect2H01`| 13 | 3 | 18 | 550 | Mid | −3 | Void Skull Shot (Speed 12, damage 550 projectile); DD cancel |
| **22B** | 22B | `Cultist_22B` (91_03 f10–28) | 12 | 1 | 14 | 0 | Mid | 0 | Void Warp (Orb 1); Teleports next to Orb 1 |
| **22C** | 22C | `Cultist_22C` (91_03 f35–55) | 12 | 1 | 14 | 0 | Mid | 0 | Void Warp (Orb 2); Teleports next to Orb 2 |
| **623C** | 623C | `Cultist_623C` (135_04 f170–200)| 7 | 5 | 24 | 950 | Mid | −18 | Void Pillar (DP Reversal); Head, Body & Projectile Invuln f1–8 |
| **214A** | 214A | `HumanF@CastingIdle01` | 15 | 20 | 12 | 0 | Mid | 0 | Dark Ritual (Channeling stance; cancels to Specials/DD) |
| **236236C**| 236236C| `HumanF@MagicAttackOmni01` | 12 | 10 | 22 | 2500 | Mid | −12 | Void Cataclysm (DD); Detonates all active orbs; Invuln f1–12; 50 Heat |
| **632146D**| 632146D| `Cultist_632146D` (135_04 f210–250)| 14 | 8 | 24 | 2700 | Mid | −15 | Abyssal Grasp (DD); Full screen erupting super; Invuln f1–14; 50 Heat |
| **OD_EA**| ABC (OD)| `HumanF@MagicAttackOmni01` | 10 | 8 | 22 | 2100 | Mid | −14 | Exceed Accel (Dark Singularity); Superfreeze; Ends Overdrive |
| **6BC** | 6B+C | `Cultist_Throw` (135_04 f260–285)| 7 | 2 | 24 | 1200 | Throw | — | Forward Void Rift Throw |
| **4BC** | 4B+C | `Cultist_Throw` (135_04 f260–285)| 7 | 2 | 24 | 1200 | Throw | — | Back Void Rift Throw |
| **j.BC**| j.B+C| `Cultist_AirThrow` (118_04)| 7 | 2 | 20 | 1050 | Throw | — | Air Void Rift Throw |
| **6AB** | 6A+B | `Cultist_CounterAssault` (135_04)| 9 | 3 | 18 | 200 | Mid | −6 | Counter Assault (Alpha Counter, 50 Heat) |
| **AB** | A+B | `Cultist_CrushTrigger` (135_04)| 24 | 4 | 18 | 950 | Mid | 0 | Crush Trigger (Guard Crush, 25 Heat) |

---

## 3. 10 Trial Combo Challenges

1. **Trial 1 — Basic Dark Gatling:** `5A > 5B > 5C` (3 hits, 1490 damage)
2. **Trial 2 — Low Sweep Confirm:** `2A > 2B > 2C` (3 hits, low knockdown confirm)
3. **Trial 3 — Anti-Air Void Pillar Reversal:** `6A (Anti-Air Upper) > 623C (Void Pillar DP)` (Invulnerable anti-air confirm)
4. **Trial 4 — Launcher Aerial Juggle:** `5B > 3C > sj > j.A > j.B > j.C` (6 hits, airborne juggle)
5. **Trial 5 — Void Orb Placement & Dash Trap:** `5D (Place Orb) > 236B (Skull Shot) > 6D (Dash Command)` (Projectile sandwich trap)
6. **Trial 6 — Stepping Overhead Confirm:** `6B (Stepping Overhead) > 5C > 236B` (High mixup confirm)
7. **Trial 7 — Void Warp Spatial Flank:** `5D (Place Orb) > 22B (Void Warp) > 5A > 5B` (Teleport cross-up reset)
8. **Trial 8 — Air Scythe Pressure & Air Orb Drop:** `j.A > j.B > j.C > j.D (Air Orb Drop)` (Air-to-ground trap setup)
9. **Trial 9 — Void Cataclysm Detonation DD:** `5B > 5C > 5D > 236236C (Void Cataclysm)` (Orb placement into detonation super)
10. **Trial 10 — Abyssal Grasp Overdrive Exceed Accel:** `5C (Fatal Counter) > 632146D (Abyssal Grasp DD) > Exceed Accel` (Maximum dark summoner punish)
