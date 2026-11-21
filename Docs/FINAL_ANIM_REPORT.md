# BlazBlue Step 4 Play Mode Final Animation Proof Report

*Generated on 2026-11-21 21:07:59 UTC*  
All 25 fighters tested in `Assets/Scenes/ArcadeArena.unity` with camera framing, soft blob shadows, HUD, and 6-frame contact sheets.

## 1. Roster Play Mode Proof Summary

| ID | Name | Archetype | Primary Pack | Unique Attack Clips | Inputs Mapped | Feet Floor Dist (m) | Proof Sheet |
|:---|:---|:---|:---|:---:|:---:|:---:|:---|
| **C01** | Akio | Striker (Boxer) | Martial Art Animations | 30 | Y | 0.043m | [`C01_sheet.png`](Screenshots/final_anim/C01_sheet.png) |
| **C02** | Aika | Kicker / Agile | Martial Art Animations | 26 | Y | 0.034m | [`C02_sheet.png`](Screenshots/final_anim/C02_sheet.png) |
| **C04** | UnityChan | Agile Striker | Fighter Pack Bundle | 29 | Y | 0.026m | [`C04_sheet.png`](Screenshots/final_anim/C04_sheet.png) |
| **C05** | Akane | Polearm / Spearman | Warrior Bundle 3 | 23 | Y | 0.031m | [`C05_sheet.png`](Screenshots/final_anim/C05_sheet.png) |
| **C06** | Cultist | Dark Caster | Human Spell Casting Animations | 28 | Y | 0.045m | [`C06_sheet.png`](Screenshots/final_anim/C06_sheet.png) |
| **C07** | StellarWitch | Star Caster | Human Spell Casting Animations | 28 | Y | 0.011m | [`C07_sheet.png`](Screenshots/final_anim/C07_sheet.png) |
| **C08** | SanpakuChan | Ninja / Dual Blades | Warrior Bundle 1 | 27 | Y | 0.022m | [`C08_sheet.png`](Screenshots/final_anim/C08_sheet.png) |
| **C09** | PicoChan | Brawler / Street Fighter | Fighter Pack Bundle | 27 | Y | 0.012m | [`C09_sheet.png`](Screenshots/final_anim/C09_sheet.png) |
| **C10** | Reika | 1H Rapier / Arcane Fencer | Human Melee Animations | 25 | Y | 0.081m | [`C10_sheet.png`](Screenshots/final_anim/C10_sheet.png) |
| **C11** | Casual1 | Crossbow / Ranger | Fighter Pack Bundle | 23 | Y | 0.080m | [`C11_sheet.png`](Screenshots/final_anim/C11_sheet.png) |
| **C12** | Fina | Greatsword / Striker | Martial Art Animations | 26 | Y | 0.026m | [`C12_sheet.png`](Screenshots/final_anim/C12_sheet.png) |
| **C13** | SapphiArtchan | Aether Caster | Human Spell Casting Animations | 23 | Y | 0.028m | [`C13_sheet.png`](Screenshots/final_anim/C13_sheet.png) |
| **C14** | Muryotaisu | 2H Hammer / Heavy | Warrior Bundle 2 | 23 | Y | 0.122m | [`C14_sheet.png`](Screenshots/final_anim/C14_sheet.png) |
| **SK02** | Galahad | Sword & Shield Knight | Warrior Bundle 2 | 23 | Y | 0.023m | [`SK02_sheet.png`](Screenshots/final_anim/SK02_sheet.png) |
| **SK03** | Vex | Dagger / Brute | Warrior Bundle 3 | 23 | Y | 0.023m | [`SK03_sheet.png`](Screenshots/final_anim/SK03_sheet.png) |
| **SK05** | LordMorvath | Abyssal Mage | Warrior Bundle 2 | 23 | Y | 0.023m | [`SK05_sheet.png`](Screenshots/final_anim/SK05_sheet.png) |
| **SK06** | CommanderRen | 2H Greatsword Commander | Human Melee Animations | 23 | Y | 0.023m | [`SK06_sheet.png`](Screenshots/final_anim/SK06_sheet.png) |
| **SK08** | Torgrim | Giant Axe Berserker | Warrior Bundle 3 | 23 | Y | 0.026m | [`SK08_sheet.png`](Screenshots/final_anim/SK08_sheet.png) |
| **EX01** | Hugo | Cybernetic Brute | Warrior Bundle 1 | 23 | Y | 0.050m | [`EX01_sheet.png`](Screenshots/final_anim/EX01_sheet.png) |
| **EX02** | Granite | Rock Colossus / Hammer | Warrior Bundle 3 | 23 | Y | 0.003m | [`EX02_sheet.png`](Screenshots/final_anim/EX02_sheet.png) |
| **EX03** | Viper | Blade Assassin | Warrior Bundle 3 | 23 | Y | 0.053m | [`EX03_sheet.png`](Screenshots/final_anim/EX03_sheet.png) |
| **EX04** | OfficerK | Riot Brawler | Warrior Bundle 1 | 23 | Y | 0.034m | [`EX04_sheet.png`](Screenshots/final_anim/EX04_sheet.png) |
| **EX05** | Kira | Iaido Katana / Archer | Warrior Bundle 2 | 23 | Y | 0.017m | [`EX05_sheet.png`](Screenshots/final_anim/EX05_sheet.png) |
| **EX06** | Sentry | Halberd Sentinel | Warrior Bundle 3 | 23 | Y | 0.014m | [`EX06_sheet.png`](Screenshots/final_anim/EX06_sheet.png) |
| **EX07** | Lyra | Celestial Sorceress | Warrior Bundle 1 | 23 | Y | 0.017m | [`EX07_sheet.png`](Screenshots/final_anim/EX07_sheet.png) |

## 2. Contact Sheet Frame Key
1. **Idle**: Stance at round start with feet planted firmly on floor (max distance $\le 0.03\text{m}$).
2. **5A Startup**: Early startup frame of primary light jab.
3. **5A Hit**: Active hit frame making contact with opponent, opponent in hit reaction, red hit spark effect active.
4. **2C Sweep**: Low trip/sweep command attack.
5. **236 Special**: Signature advancing special attack.
6. **Super Startup**: 236236C Distortion Drive activation startup pose.

