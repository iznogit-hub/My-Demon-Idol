# Akio BlazBlue movelist

Akio stays a rushdown puncher. Her Drive is **Iron Rush**: D is a three-step rush punch. Each D that hits or is blocked banks 1 Momentum, up to 3. 236C adds 150 damage per stock (900 at 0, 1350 at 3). 632146D adds 300 per stock. Stocks clear when she is knocked down. With the BlazBlue scheme on and Astral off, she has no Astral.

Classic Akio is unchanged. `akioDepthMovelist` still selects the 3-button kit. This profile is used only when the match scheme is BlazBlue **and both fighters have a blazBlueProfile**.

## Mirror match

A BlazBlue match starts only when both sides have a profile. Right now only Akio has one, so the way to see this kit is an Akio mirror:

1. Set both fighters to C01. In PlayerPrefs that is `MDI_P1_CharacterId` and `MDI_P2_CharacterId`, both `C01`.
2. Call `SettingsPanel.SetControlScheme(ControlScheme.BlazBlue4Button)`.
3. Start versus or training.

If the second fighter is anyone else, the match stays on Classic and Akio still plays her 3-button kit.

## Moves

| Move | Input | Clip | Ticks | Dmg | Guard | On block |
| --- | --- | --- | --- | --- | --- | --- |
| 5A | A | Akio_5P 144_20 110-122 | 6/3/8 | 300 | Mid | -1 |
| 2A | 2A | Akio_2P 144_20 196-214 | 7/3/11 | 280 | Low | -2 |
| 6A | 6A | Akio_BB_6A 144_20 230-250 | 10/4/16 | 600 | Mid | -6 |
| 5B | B | Akio_5M 144_20 123-136 | 8/4/12 | 600 | Mid | -3 |
| 2B | 2B | Akio_BB_2B 144_20 260-278 | 9/3/14 | 520 | Low | -5 |
| 6B | 6B | Akio_6M 144_20 148-166 | 20/4/16 | 750 | Overhead | -6 |
| 5C | C | Akio_6S 144_17 24-50 | 12/4/16 | 900 | Mid | -6 |
| 2C | 2C | Akio_2M 135_05 100-120 | 10/4/18 | 700 | Low | -10 |
| 3C | 3C | Akio_2S 135_07 140-164 | 12/5/18 | 750 | Mid | -10 |
| jA | j.A | Akio_jP 143_23 42-51 | 5/3/8 | 320 | Overhead | 0 |
| jB | j.B | Akio_jM 75_16 40-58 | 8/6/14 | 700 | Overhead | 0 |
| jC | j.C | Akio_jS 75_16 44-60 | 9/6/16 | 850 | Overhead | 0 |
| 5D | 5D | Akio_Rekka1 144_20 34-47 | 6/3/10 | 350 | Mid | -2 |
| 5D2 | D during 5D | Akio_5M 144_20 123-136 | 7/3/11 | 450 | Mid | -3 |
| 5D3 | D during 5D2 | Akio_2M 135_05 100-120 | 9/4/14 | 600 | Low | -6 |
| 2D | 2D | Akio_BB_2D 135_05 140-158 | 9/3/14 | 450 | Low | -4 |
| 6D | 6D | Akio_Dash 144_17 20-40 | 22/4/16 | 700 | Overhead | -5 |
| jD | j.D | Akio_BB_jD 143_23 70-90 | 10/4/12 | 650 | Overhead | 0 |
| 236A | 236A | Akio_5P 144_20 110-122 | 12/1/20 | 600 | Mid | -4 |
| 623C | 623C | Akio_623S 135_07 15-34 | 6/6/22 | 1000 | Mid | -18 |
| 214B | 214B | Akio_BB_214B 144_17 80-100 | 14/4/18 | 800 | Mid | -4 |
| 236C | 236C | Akio_6S 144_17 24-50 | 13/8/18 | 900 | Mid | -8 |
| 236236C | 236236C | Akio_Super 135_06 48-80 | 14/8/22 | 2800 | Mid | -12 |
| 632146D | 632146D | Akio_Super 135_06 48-80 | 10/24/26 | 3000 | Mid | -20 |
| EA | A+B+C during Overdrive | Akio_Super 135_06 48-80 | 12/8/24 | 2200 | Mid | -16 |
| 6BC | 6B+C | Akio_Throw 144_17 26-48 | 7/2/24 | 1200 | Throw | 0 |
| 4BC | 4B+C | Akio_Throw 144_17 26-48 | 7/2/24 | 1200 | Throw | 0 |
| jBC | j.B+C | Akio_BB_jBC 143_23 100-120 | 7/2/20 | 1000 | Throw | 0 |
| CA | 6A+B while blocking | Akio_5M 144_20 123-136 | 9/3/26 | 90 | Mid | 10 |
| CT | A+B in neutral | Akio_BB_CT 135_06 100-140 | 30/4/20 | 900 | Mid | -8 |

5C is a Fatal Counter. 3C and j.B jump-cancel on hit. 623C is Head and Body invulnerable on ticks 1-8. Iron Rush 2 and 3 reuse the straight and the sweep takes, which is what the old rekka clips already were. 236C and the second super are played from one clip each. The written sequence for 236C is the stepping cross then the straight. The written sequence for 632146D is the dash, then the jab-straight, then the barrage.

Kunai still uses the Shadow Ink Kunai preset when the projectile event is wired. This kit does not change that preset.

Locomotion stays on the clips already in `AkioClipLibrary`: walk 144_33 150-178, dash 144_17 20-40, jump 75_16 32-50, crouch 135_05 80-92, blocks 144_07 and 135_10, fall 90_18, get-up 111_06, intro 144_33.
