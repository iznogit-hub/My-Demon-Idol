# UnityChan movelist

C04 UnityChan is an aerial hopper. Her hips sit at about 0.87 m, under Akio (about 0.95 m) and Aika (about 0.92 m), and her forearm is only about 0.16 m. She does not outrange Aika's shin or Akio's punches. Subject 16 is a jump-and-run take whose vertical hop (`16_01`) rises from hip 63 cm to 110 cm with about 5 cm of slide, so the kit is built around leaving the ground.

She opts in with `depthMovelist` on her own character definition. Akio still uses only `akioDepthMovelist`. Aika keeps her own `depthMovelist` asset. Neither of their clip windows, movesets, or projectile numbers are edited. With UnityChan's flag off she is not on this moveset, and full-meter P is still the Aero Plasma Disc at 44 damage and speed 15.

The get-up and the fall come from `140_01`, whose hip sits below zero in the source. Those two sub-clips set `keepOriginalPositionY` to 0 so the humanoid plant ignores that sunk root. Every other UnityChan clip keeps the original height, and Akio's and Aika's clip entries are untouched.

Fist boxes are 0.20 x 0.16 x 0.18 m. Foot boxes on the hops are 0.38 x 0.16 x 0.22 m, shorter than Aika's. Crouch pushbox is 0.46 m tall, radius 0.18 m. Air pushbox is 1.02 m tall, radius 0.16 m.

## Frame data

| Move | Input | Clip | Frames | Ticks | Dmg | On block | Speed | Cancels on hit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 5P Tap | 5P | 02_05 92-108 | 17 | 4/3/8 | 22 | -1 | 0.6071 | 5M, 6S, 236P, 214S, super |
| 2P Dip | 2P | 02_05 212-230 | 19 | 6/3/10 | 24 | -2 | 0.7576 | 2M, 236P, super |
| j.P | j.P | 02_05 124-140 | 17 | 4/3/8 | 24 | 0 | 0.8095 | jM |
| 5M | 5M | 02_05 144-162 | 19 | 7/3/12 | 55 | -3 | 0.7646 | rekka2, 6S, 623S, 236P, super |
| 6M Hop Overhead | 6M | 16_05 20-40 | 21 | 11/4/14 | 68 | -6 | 0.9052 | 623S, super |
| 2M Check | 2M | 14_02 630-648 | 19 | 8/3/14 | 58 | -7 | 0.8509 | super |
| j.M | j.M | 118_01 88-106 | 19 | 7/5/12 | 70 | 0 | 0.5979 | jS |
| Rekka 1 | mash M | 02_05 55-71 | 17 | 5/3/8 | 28 | -1 | 1.3281 | rekka2, 236P |
| Rekka 2 | M during rekka 1 or 5M | 02_05 126-144 | 19 | 6/3/10 | 40 | -2 | 0.8853 | rekka3, 6S, super |
| Rekka 3 | M during rekka 2 | 02_05 148-168 | 21 | 8/4/12 | 55 | -5 | 0.9796 | 6S, super |
| 2S Hop Launcher | 2S | 16_01 16-40 | 25 | 9/5/16 | 70 | -8 | 1.0673 | jump, super |
| j.S | j.S | 118_05 64-82 | 19 | 8/5/14 | 82 | 0 | 0.6702 | none |
| 236P Disc | 236P | KI MagicAttackDirect1H01 | KI | 11/1/16 | 44 | -2 | 1 | super |
| 623S Rising Hop | 623S | 16_05 30-50 | 21 | 6/5/20 | 90 | -14 | 0.8053 | none |
| 214S Bound | 214S | 91_39 50-70 | 21 | 9/4/16 | 74 | -6 | 1.0153 | super |
| 6S Leap | 6S | 91_39 24-44 | 21 | 10/5/14 | 76 | -5 | 1.0153 | 214S, super |
| [4]6P Hop Stomp | [4]6P | 118_05 47-70 | 24 | 14/5/18 | 88 | -9 | 0.7711 | super |
| Throw | P+M | 02_05 72-94 | 23 | 5/2/18 | 110 | 0 | 1.4059 | none |
| Super | super | 14_02 811-845 | 35 | 14/8/20 | 240 | -12 | 1.0417 | none |

## Routes

5P gatling into 5M into 6S, on hit and on block. Mash M from neutral is rekka 1-3, three short punches. 2S on hit jump-cancels into j.P, j.M, j.S. j.S knocks down. A normal jump is still one air attack.

236P spends the projectile meter and fires the existing Aero Plasma Disc on the first active tick. The disc stays 44 damage and speed 15, the same numbers as her flag-off projectile. Raw P is the jab and does not spend meter.

623S is a rising hop with strike invulnerability on ticks 1-6. 214S is a bounding kick, not Aika's low reap. [4]6P is a hop stomp after holding back for 40 ticks. 6S is the forward leap.

Intro is the walk from `16_15` frames 8-40. Victory is the settle at the end of the vertical hop, `16_01` frames 48-64. Fall is `140_01` frames 8-40 and get-up is frames 70-130, both with the root height planted. Hits, back walk, and backdash stay on the KI clips.

## Files

New clip entries, and no edits to existing ones, on: `02_05`, `14_02`, `16_01`, `16_05`, `16_15`, `16_35`, `91_39`, `118_01`, `118_05`, `140_01`.

