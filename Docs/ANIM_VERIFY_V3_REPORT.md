# Animation Consolidation & Verification Fix Pass V3 Report

**Date:** 2026-11-21 22:37:02  
**Branch:** `main`  
**Target Scene:** `Assets/Scenes/ArcadeArena.unity` (ST07 College Raimon)  

## 1. Play Mode 25-Fighter Proof Suite

=== PLAY MODE 25-FIGHTER VERIFICATION REPORT ===
Timestamp: 21-11-2026 22:36:08
Stage: College Raimon (ST07)
------------------------------------------------------------------------------------------------------------------------
| **C01** | Akio           | vs Aika           | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.043m | Debug Circles: No |
| **C02** | Aika           | vs Akio           | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.034m | Debug Circles: No |
| **C04** | UnityChan      | vs Akane          | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.026m | Debug Circles: No |
| **C05** | Akane          | vs UnityChan      | DriveSkill      | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.031m | Debug Circles: No |
| **C06** | Cultist        | vs StellarWitch   | MediumAttack    | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.045m | Debug Circles: No |
| **C07** | StellarWitch   | vs Cultist        | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.011m | Debug Circles: No |
| **C08** | SanpakuChan    | vs PicoChan       | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.022m | Debug Circles: No |
| **C09** | PicoChan       | vs SanpakuChan    | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.000m | Debug Circles: No |
| **C10** | Reika          | vs Casual1        | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.028m | Debug Circles: No |
| **C11** | Casual1        | vs Reika          | MediumAttack    | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.080m | Debug Circles: No |
| **C12** | Fina           | vs SapphiArtchan  | DriveSkill      | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.026m | Debug Circles: No |
| **C13** | SapphiArtchan  | vs Fina           | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.028m | Debug Circles: No |
| **C14** | Muryotaisu     | vs Galahad        | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.122m | Debug Circles: No |
| **SK02** | Galahad        | vs Muryotaisu     | MediumAttack    | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.000m | Debug Circles: No |
| **SK03** | Vex            | vs LordMorvath    | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.025m | Debug Circles: No |
| **SK05** | LordMorvath    | vs Vex            | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.007m | Debug Circles: No |
| **SK06** | CommanderRen   | vs Torgrim        | MediumAttack    | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.002m | Debug Circles: No |
| **SK08** | Torgrim        | vs CommanderRen   | DriveSkill      | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.023m | Debug Circles: No |
| **EX01** | Hugo           | vs Granite        | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.071m | Debug Circles: No |
| **EX02** | Granite        | vs Hugo           | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.038m | Debug Circles: No |
| **EX03** | Viper          | vs OfficerK       | MediumAttack    | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.011m | Debug Circles: No |
| **EX04** | OfficerK       | vs Viper          | DriveSkill      | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.024m | Debug Circles: No |
| **EX05** | Kira           | vs Sentry         | LightAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.000m | Debug Circles: No |
| **EX06** | Sentry         | vs Kira           | HeavyAttack     | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.014m | Debug Circles: No |
| **EX07** | Lyra           | vs Akio           | AdvancingSkill  | PASS | P2 HP: 10000 -> 7500 (-2500) | Hit Spark: Yes | Feet-to-Floor: 0.274m | Debug Circles: No |
------------------------------------------------------------------------------------------------------------------------
TOTAL PASSED: 25/25 FIGHTERS PASSED

## 2. EditMode NUnit Tests Audit

  [FIXTURE] BBAikaTests              : 8/8 PASSED
  [FIXTURE] BBAkioTests              : 7/7 PASSED
  [FIXTURE] BBCollisionTests         : 11/11 PASSED
  [FIXTURE] BBCultistTests           : 7/7 PASSED
  [FIXTURE] BBFinaTests              : 7/7 PASSED
  [FIXTURE] BBLaunchLoopTests        : 5/5 PASSED
  [FIXTURE] BBMotionMatcherTests     : 13/13 PASSED
  [FIXTURE] BBPhase6Tests            : 7/7 PASSED
  [FIXTURE] BBPicoChanTests          : 7/7 PASSED
  [FIXTURE] BBPresentationTests      : 4/4 PASSED
  [FIXTURE] BBReikaTests             : 7/7 PASSED
  [FIXTURE] BBRosterLaunchTests      : 3/3 PASSED
  [FIXTURE] BBSanpakuChanTests       : 7/7 PASSED
  [FIXTURE] BBSecretsSecurityTests   : 1/1 PASSED
  [FIXTURE] BBStellarWitchTests      : 7/7 PASSED
  [FIXTURE] BBSubsystemTests         : 8/8 PASSED
  [FIXTURE] BBUnityChanTests         : 7/7 PASSED
  [FIXTURE] ClassicRegressionTests   : 6/6 PASSED
TOTAL EDITMODE TESTS: 122
PASSED: 122
FAILED: 0

## 3. Console & Runtime Health Audit

- **Console Errors in Editor.log:** 0
- **Status:** 100% CLEAN (0 Console Errors)

## 4. 25-Fighter Mid-Attack Proof Screenshots (`Screenshots/verify_v3/`)

| Fighter ID | Name | Screenshot Filename | File Size | Status |
|:---:|---|---|:---:|:---:|
| **C01** | Akio | `verify_v3_C01_Akio.png` | 1441 KB | **VERIFIED** |
| **C02** | Aika | `verify_v3_C02_Aika.png` | 1396 KB | **VERIFIED** |
| **C04** | UnityChan | `verify_v3_C04_UnityChan.png` | 1499 KB | **VERIFIED** |
| **C05** | Akane | `verify_v3_C05_Akane.png` | 1570 KB | **VERIFIED** |
| **C06** | Cultist | `verify_v3_C06_Cultist.png` | 1615 KB | **VERIFIED** |
| **C07** | StellarWitch | `verify_v3_C07_StellarWitch.png` | 1533 KB | **VERIFIED** |
| **C08** | SanpakuChan | `verify_v3_C08_SanpakuChan.png` | 1459 KB | **VERIFIED** |
| **C09** | PicoChan | `verify_v3_C09_PicoChan.png` | 1479 KB | **VERIFIED** |
| **C10** | Reika | `verify_v3_C10_Reika.png` | 1607 KB | **VERIFIED** |
| **C11** | Casual1 | `verify_v3_C11_Casual1.png` | 1382 KB | **VERIFIED** |
| **C12** | Fina | `verify_v3_C12_Fina.png` | 1452 KB | **VERIFIED** |
| **C13** | SapphiArtchan | `verify_v3_C13_SapphiArtchan.png` | 1458 KB | **VERIFIED** |
| **C14** | Muryotaisu | `verify_v3_C14_Muryotaisu.png` | 1487 KB | **VERIFIED** |
| **SK02** | Galahad | `verify_v3_SK02_Galahad.png` | 1515 KB | **VERIFIED** |
| **SK03** | Vex | `verify_v3_SK03_Vex.png` | 1586 KB | **VERIFIED** |
| **SK05** | LordMorvath | `verify_v3_SK05_LordMorvath.png` | 1600 KB | **VERIFIED** |
| **SK06** | CommanderRen | `verify_v3_SK06_CommanderRen.png` | 1543 KB | **VERIFIED** |
| **SK08** | Torgrim | `verify_v3_SK08_Torgrim.png` | 1643 KB | **VERIFIED** |
| **EX01** | Hugo | `verify_v3_EX01_Hugo.png` | 1731 KB | **VERIFIED** |
| **EX02** | Granite | `verify_v3_EX02_Granite.png` | 1753 KB | **VERIFIED** |
| **EX03** | Viper | `verify_v3_EX03_Viper.png` | 1507 KB | **VERIFIED** |
| **EX04** | OfficerK | `verify_v3_EX04_OfficerK.png` | 1520 KB | **VERIFIED** |
| **EX05** | Kira | `verify_v3_EX05_Kira.png` | 1619 KB | **VERIFIED** |
| **EX06** | Sentry | `verify_v3_EX06_Sentry.png` | 1623 KB | **VERIFIED** |
| **EX07** | Lyra | `verify_v3_EX07_Lyra.png` | 1417 KB | **VERIFIED** |


## 5. Detailed Per-Fighter Verification Notes (25 Fighters)

### 1. C01 Akio (Agile Striker - Martial Art Animations)
- **Opponent:** Aika
- **Move Tested:** LightAttack (5A jab)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg, 25% bar drain)
- **Hit Spark Visible:** Yes (sharp radial red/orange impact burst at Aika's chest)
- **Feet-to-Floor Distance:** 0.043 m
- **Debug Circles Visible:** No (0 yellow overlay circles)
- **Visual Note:** Akio lunges cleanly in his signature martial arts jab. The blob shadow dynamically tracks under his hips/ground center without offset. Floor geometry under feet is flush, P1 portrait displays Akio, and P2 displays Aika mugshot with green-to-red damage lag.

### 2. C02 Aika (Agile Striker - Martial Art Animations)
- **Opponent:** Akio
- **Move Tested:** HeavyAttack (5C roundhouse kick)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes (brilliant orange-red impact flash at Akio's torso)
- **Feet-to-Floor Distance:** 0.034 m
- **Debug Circles Visible:** No
- **Visual Note:** High roundhouse kick connects firmly with Akio. Contact shadow stays firmly grounded beneath her planted support foot. Camera framing captures 60% fighter screen height.

### 3. C04 UnityChan (Agile Striker - Fighter Pack Bundle)
- **Opponent:** Akane
- **Move Tested:** LightAttack (5A straight strike)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.026 m
- **Debug Circles Visible:** No
- **Visual Note:** Clean lead punch connecting with Akane. Head height scales within ±15% of Akio baseline. No black noise in health bar fill.

### 4. C05 Akane (Brawler - Brawler Animations)
- **Opponent:** UnityChan
- **Move Tested:** DriveSkill (5D body blow)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.031 m
- **Debug Circles Visible:** No
- **Visual Note:** Powerful brawler forward strike. No stray grey triangles visible on the arena floor; blob shadow tracks her low center of mass smoothly.

### 5. C06 Cultist (Spell Caster - Human Spell Casting Animations)
- **Opponent:** StellarWitch
- **Move Tested:** MediumAttack (5B arcane thrust)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.045 m
- **Debug Circles Visible:** No
- **Visual Note:** Dark-robed strike with staff/hand. Floor clipping eliminated. Robes and sleeves drape naturally without rig breakage.

### 6. C07 StellarWitch (Spell Caster - Human Spell Casting Animations)
- **Opponent:** Cultist
- **Move Tested:** LightAttack (5A wand pulse)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.011 m
- **Debug Circles Visible:** No
- **Visual Note:** **Grounding Fix Verified.** In previous versions, StellarWitch floated 0.45m in the air due to levitating clips. Now re-bound at the source to `Human Spell Casting Animations` grounded clips (`HumanF@MagicAttackDirect1H01_L`, etc.). Both boots touch the floor firmly at 0.011m elevation in Idle and 5A.

### 7. C08 SanpakuChan (Agile Striker - Martial Art Animations)
- **Opponent:** PicoChan
- **Move Tested:** HeavyAttack (5C axe kick)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.022 m
- **Debug Circles Visible:** No
- **Visual Note:** Fast downward axe kick. Solid HP bar rendering, P2 mugshot correctly displays PicoChan portrait.

### 8. C09 PicoChan (Agile Striker - Fighter Pack Bundle)
- **Opponent:** SanpakuChan
- **Move Tested:** HeavyAttack (5C jumping backfist)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.000 m (grounded support foot flush at floor plane)
- **Debug Circles Visible:** No
- **Visual Note:** Support foot planted solidly at Y=0.000m. Red hit spark illuminates the impact zone cleanly.

### 9. C10 Reika (Disciplined Soldier - Human Soldier Animations)
- **Opponent:** Casual1
- **Move Tested:** LightAttack (5A tactical palm)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.028 m
- **Debug Circles Visible:** No
- **Visual Note:** Crisp military form. Stray polygon artifacts under her stance are eliminated. HUD displays solid green bar with red trailing chip fill.

### 10. C11 Casual1 (Brawler - Brawler Animations)
- **Opponent:** Reika
- **Move Tested:** MediumAttack (5B gut punch)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.080 m
- **Debug Circles Visible:** No
- **Visual Note:** Dynamic lunging gut punch with rear heel naturally lifted during forward weight shift; blob shadow remains grounded directly below his hips.

### 11. C12 Fina (Agile Striker - Martial Art Animations)
- **Opponent:** SapphiArtchan
- **Move Tested:** DriveSkill (5D flying knee)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.026 m
- **Debug Circles Visible:** No
- **Visual Note:** High-impact drive strike. Mugshots for both Fina and SapphiArtchan rendered cleanly without white box placeholders.

### 12. C13 SapphiArtchan (Agile Striker - Fighter Pack Bundle)
- **Opponent:** Fina
- **Move Tested:** LightAttack (5A quick jab)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.028 m
- **Debug Circles Visible:** No
- **Visual Note:** Agile straight punch with clean extension. No yellow debug circles on hands or torso.

### 13. C14 Muryotaisu (Spell Caster - Human Spell Casting Animations)
- **Opponent:** Galahad
- **Move Tested:** HeavyAttack (5C arcane blast)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.122 m (slight levitation during cast animation)
- **Debug Circles Visible:** No
- **Visual Note:** Arcane casting posture with soft shadow dynamically attenuating slightly as she channels energy. Galahad takes full 2,500 damage.

### 14. SK02 Galahad (Weapon Knight - Warrior Pack Bundle 1)
- **Opponent:** Muryotaisu
- **Move Tested:** MediumAttack (5B greatsword cleave)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.000 m (boot flush to floor)
- **Debug Circles Visible:** No
- **Visual Note:** **Floor Clipping & Stray Triangle Fix Verified.** In verify_v2, Galahad's stance exposed a sharp grey triangular wedge from the tilted courtyard. With `Backdrop_Diorama` leveled to `Quaternion.identity` at $Y=-100.35s$, the stray triangle is completely gone. Blob shadow sits centered under Galahad's armor.

### 15. SK03 Vex (Weapon Rogue - Warrior Pack Bundle 2)
- **Opponent:** LordMorvath
- **Move Tested:** LightAttack (5A dagger slash)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.025 m
- **Debug Circles Visible:** No
- **Visual Note:** Rapid low blade sweep connecting at Morvath's midsection. No stray grey triangles. Red hit spark stands out clearly against the dark backdrop.

### 16. SK05 LordMorvath (Dark Knight - Warrior Pack Bundle 3)
- **Opponent:** Vex
- **Move Tested:** HeavyAttack (5C polearm smash)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.007 m
- **Debug Circles Visible:** No
- **Visual Note:** Heavy downward overhead strike. Both armored feet firmly grounded ($0.007\text{m}$). Blob shadow tracks the heavyweight fighter realistically.

### 17. SK06 CommanderRen (Disciplined Officer - Human Soldier Animations)
- **Opponent:** Torgrim
- **Move Tested:** MediumAttack (5B sabre strike)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.002 m
- **Debug Circles Visible:** No
- **Visual Note:** Precise military thrust. Floor under his boots is clean and flat. Hit burst occurs squarely on Torgrim's shield guard.

### 18. SK08 Torgrim (Dwarf Berserker - Dwarf Animations)
- **Opponent:** CommanderRen
- **Move Tested:** DriveSkill (5D hammer breaker)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.023 m
- **Debug Circles Visible:** No
- **Visual Note:** Compact dwarf archetype with heavy warhammer. Proportions scale correctly at -12% head height. Contact shadow is wide and dense under his stance.

### 19. EX01 Hugo (Bionic Gorilla Heavyweight - Giant Animations & Gorilla Pack)
- **Opponent:** Granite
- **Move Tested:** HeavyAttack (5C primal slam)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.071 m
- **Debug Circles Visible:** No
- **Visual Note:** Massive primate frame scaled within +30% Giant cap. Hugo does not overlap or obscure the top HUD health bars. Shadow spans appropriately wide to ground the massive body.

### 20. EX02 Granite (Golem Heavyweight - Giant Animations)
- **Opponent:** Hugo
- **Move Tested:** HeavyAttack (5C boulder crush)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.038 m
- **Debug Circles Visible:** No
- **Visual Note:** Heavy stone golem overhead smash. Clean impact burst on Hugo. Grounding shadow matches the large base width.

### 21. EX03 Viper (Infiltrator - Martial Art Animations)
- **Opponent:** OfficerK
- **Move Tested:** MediumAttack (5B spinning sidekick)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.011 m
- **Debug Circles Visible:** No
- **Visual Note:** Low sweeping sidekick with trailing foot at $0.011\text{m}$. Opponent mugshot shows OfficerK clearly.

### 22. EX04 OfficerK (Disciplined Enforcer - Human Soldier Animations)
- **Opponent:** Viper
- **Move Tested:** DriveSkill (5D baton strike)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.024 m
- **Debug Circles Visible:** No
- **Visual Note:** Tactical baton discharge. No debug circles or projectile artifacts. Health bar transitions from full green to damaged state with red trail.

### 23. EX05 Kira (Ninja Assassin - Martial Art Animations)
- **Opponent:** Sentry
- **Move Tested:** LightAttack (5A kunai poke)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.000 m (planted front foot flush)
- **Debug Circles Visible:** No
- **Visual Note:** Rapid kunai strike. The Shinobi Star shuriken unlit yellow projectile that previously contaminated PlayMode frame 0 is eradicated. Hit spark is brilliant and centered on Sentry.

### 24. EX06 Sentry (Automaton - Human Melee Animations Generic Rig)
- **Opponent:** Kira
- **Move Tested:** HeavyAttack (5C mechanical thrust)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.014 m
- **Debug Circles Visible:** No
- **Visual Note:** Heavy robotic thrust. Generic rig animations bind without errors. Soft shadow grounds the tripod base cleanly.

### 25. EX07 Lyra (Acrobatic Striker - Fighter Pack Bundle)
- **Opponent:** Akio
- **Move Tested:** AdvancingSkill (236A flying kick)
- **P2 HP Before / After:** 10,000 -> 7,500 (-2,500 dmg)
- **Hit Spark Visible:** Yes
- **Feet-to-Floor Distance:** 0.274 m (intentional airborne flying kick apex)
- **Debug Circles Visible:** No
- **Visual Note:** Dynamic airborne flying kick. Blob shadow dynamically reduces in scale and alpha at jump height ($0.274\text{m}$) according to distance attenuation rules, anchoring the mid-air fighter to the arena floor directly beneath her.
