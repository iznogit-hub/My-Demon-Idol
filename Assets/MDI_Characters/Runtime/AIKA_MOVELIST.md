# Aika movelist

C02 Aika is a karate zoner. Her hips sit a little lower than Akio's and her shin is the long limb, so the normals are kicks and planted boxing rather than Akio's rushdown punches. She opts in with `depthMovelist`. Akio keeps `akioDepthMovelist` and does not set this flag. Everyone else sets neither.

The depth code is the same path Akio already uses. New motions run only when her moveset fills them: `depthQuarterBack` is 214S, `depthCharge` is [4]6P. Empty chain-id strings still mean 5M, rekka1, and rekka2, which is Akio's merged route. Her pushbox numbers are on the character definition. Zero means the original Akio sizes, so Akio's body fit does not change.

Her animator is `Aika_KI_Melee_Override` on `MDI_KI_Melee_Fighter`. The base controller is not edited. Clips are 24 fps. Get-up and the fall start at frame 8 or later. No flips, no `88_06`, no cartwheels.

Fist boxes are 0.24 x 0.20 x 0.22 m, 0.10 m ahead of the wrist. Her forearm is shorter than Akio's, so the fist is smaller. Foot boxes are 0.56 x 0.18 x 0.26 m, 0.22 m ahead of the ankle, for the longer shin. The super punches, then kicks.

## Frame data

Ticks are startup / active / recovery at 60 Hz.

| Move | Input | Clip | Frames | Ticks | Dmg | On block | Speed | Cancels on hit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 5P Jab | 5P | 80_10 52-68 | 17 | 5/3/9 | 26 | -2 | 0.5357 | 5M, 2M, 6S, 214S, 236P, super |
| 2P Low Fist | 2P | 135_02 462-480 | 19 | 7/3/12 | 28 | -3 | 0.6543 | 2M, 214S, 236P, super |
| j.P | j.P | 13_18 280-296 | 17 | 5/3/8 | 28 | 0 | 0.7589 | jM |
| 5M Cross | 5M | 13_18 73-91 | 19 | 8/4/14 | 64 | -5 | 0.6469 | rekka2, 6S, 214S, 623S, 236P, super |
| 6M Overhead Kick | 6M | 143_24 164-184 | 21 | 13/4/18 | 80 | -8 | 0.75 | 214S, super |
| 2M Sweep | 2M | 135_02 492-514 | 23 | 10/4/16 | 72 | -10 | 0.8583 | super |
| j.M | j.M | 141_14 44-64 | 21 | 8/5/14 | 78 | 0 | 0.5874 | jS |
| Rekka 1 | mash M | 80_10 36-52 | 17 | 6/3/8 | 32 | -2 | 1.25 | rekka2, 214S |
| Rekka 2 | M during rekka 1 or 5M | 79_08 54-74 | 21 | 7/4/12 | 48 | -4 | 0.8083 | rekka3, 6S, super |
| Rekka 3 | M during rekka 2 | 113_13 245-265 | 21 | 9/4/15 | 64 | -8 | 0.8397 | super |
| 2S Launcher | 2S | 113_13 219-243 | 25 | 12/5/20 | 78 | -12 | 0.8654 | jump, super |
| j.S Finisher | j.S | 141_14 80-102 | 23 | 9/6/16 | 92 | 0 | 0.7066 | none |
| 236P Nail | 236P | KI MagicAttackDirect1H01 | KI | 13/1/18 | 55 | -3 | 1 | super |
| 623S Rising Kick | 623S | 135_11 62-84 | 23 | 7/6/24 | 108 | -22 | 0.739 | none |
| 214S Reap | 214S | 143_24 34-54 | 21 | 10/5/18 | 86 | -8 | 0.8923 | super |
| 6S Step Kick | 6S | 111_19 57-75 | 19 | 11/4/18 | 84 | -8 | 0.8073 | 214S, super |
| [4]6P Side Kick | [4]6P | 135_11 168-192 | 25 | 18/6/22 | 112 | -14 | 0.6461 | super |
| Throw | P+M | 111_19 36-58 | 23 | 5/2/20 | 120 | 0 | 1.3017 | none |
| Super | super | 13_18 276-316 | 41 | 16/10/24 | 260 | -16 | 1.025 | none |

## How she differs from Akio

- Akio is a punch rushdown. Aika's overhead, sweep, launcher, and both air kicks are kicks. Her jab and cross are planted boxing from subjects 80 and 13, not subject 144.

- 214S is a low reap that knocks down. Akio parses 214 and does not use it.

- [4]6P is a charged side kick. Hold back for 40 ticks, then forward and P. Akio has no charge move.

- 236P is her nail at 55 damage and speed 13, on block -3. Akio's projectile stays 60 damage and speed 16, on block -4.

- 623S is a rising side kick with strike invulnerability on ticks 1-7, and it is more minus than Akio's dragon punch.

- 2S on hit jump-cancels into j.P > j.M > j.S. A normal jump is still one air attack.

- Crouch pushbox is 0.50 m tall and 0.24 m radius. Air pushbox is 1.05 m tall and 0.20 m radius. Akio stays 0.55 / 0.22 and 1.10 / 0.22.

- Intro is the stiff walk from 74_01, frames 8-48. Victory is a settle from 144_08 frames 560-584, not Akio's KI idle. Get-up is 114_02 frames 211-261. The source hip on that take goes negative, so preview the retarget.

- Hits, back walk, backdash, and the fireball cast stay on the KI clips. Rapid cancel is still 50 meter and clears the get-up override first. Super is still 100 meter.


## Unity checklist

1. Reimport the Aika CMU files: 80_10, 79_08, 13_18, 74_01, 135_02, 135_09, 135_11, 141_14, 143_24, 113_13, 111_19, 144_08, 114_02. Confirm 24 fps and the windows above.

2. Preview Aika_Getup on her avatar. Do not start it before frame 211.

3. Play Aika: intro, 5P, 2P, 5M, 6M, 2M, mash M, 2S into j.P j.M j.S, 236P, 214S, 623S, hold back then [4]6P, 6S, throw, super, both blocks, crouch, dash.

4. Play Akio in the same build. 5P is still the jab, 236P still spends meter, 214 still does nothing, and there is still no charge kick.

5. Play a third fighter, Unity-chan or Fina. Full-meter P still fires her projectile. Empty-meter P still whiffs.

