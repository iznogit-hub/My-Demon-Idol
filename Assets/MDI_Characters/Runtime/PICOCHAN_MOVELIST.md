# PicoChan movelist

C09 PicoChan is a close grappler. Her hips sit at 0.76 m, the lowest of the four built fighters (Akio about 0.95, Aika about 0.92, UnityChan about 0.87). Her thigh is 0.28 m and her shin is 0.36 m, so the leg is long for her height but still shorter than Aika's shin. Her upper arm is 0.18 m and her forearm is 0.21 m, a long forearm on a short body. She crowds and hooks. She does not try to out-kick Aika or out-hop UnityChan.

She opts in with `depthMovelist` on her own definition. Akio, Aika, and UnityChan assets are not edited. With the flag off, full-meter P is still the Cyber Laser Cannon at 44 damage and speed 20.

236P is a lariat, not a laser. The move id is `236S`, so the projectile spawner does not fire. It does not spend the projectile meter. 214S is a command grab: the existing throw hit rule makes it unblockable inside about a metre, with a 6-tick tech. 6S on hit cancels into that grab.

The get-up is `113_08` frames 300-342. That take lies on the floor with the hip near 10 cm, so this sub-clip sets `keepOriginalPositionY` to 0 and the humanoid plant ignores the sunk root. The fall window itself stays above the floor and keeps its height.

Fist boxes are 0.26 x 0.18 x 0.20 m, ahead of the long forearm. Foot boxes are 0.42 x 0.16 x 0.22 m. Grab boxes are 0.42 x 0.50 x 0.36 m on the chest. Crouch pushbox is 0.42 m tall, radius 0.22 m. Air pushbox is 0.90 m tall, radius 0.20 m.

## Frame data

| Move | Input | Clip | Frames | Ticks | Dmg | On block | Speed | Cancels on hit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 5P Hook | 5P | 15_13 638-654 | 17 | 5/3/9 | 32 | -1 | 0.5357 | 5M, 2M, 6S, 236S, super |
| 2P Body Hook | 2P | 15_13 376-394 | 19 | 6/3/11 | 34 | -2 | 0.7197 | 2M, 236S, super |
| j.P | j.P | 15_13 765-783 | 19 | 5/3/8 | 30 | 0 | 0.8482 | jM |
| 5M Hook | 5M | 15_13 1256-1274 | 19 | 8/4/13 | 72 | -4 | 0.6728 | rekka2, 6S, 236S, 623S, super |
| 6M Upper | 6M | 15_13 400-418 | 19 | 12/4/16 | 78 | -7 | 0.7422 | super |
| 2M Low | 2M | 15_13 344-362 | 19 | 9/4/15 | 68 | -8 | 0.7597 | super |
| j.M | j.M | 86_08 312-330 | 19 | 8/5/13 | 74 | 0 | 0.5519 | jS |
| Rekka 1 | mash M | 15_13 35-53 | 19 | 5/3/8 | 36 | -1 | 1.4844 | rekka2, 236S |
| Rekka 2 | M during rekka 1 or 5M | 15_13 465-483 | 19 | 7/3/11 | 50 | -3 | 0.801 | rekka3, 6S, 236S, super |
| Rekka 3 | M during rekka 2 | 86_08 556-574 | 19 | 8/4/12 | 62 | -5 | 0.7008 | 6S, 214S, super |
| j.S | j.S | 86_08 944-962 | 19 | 9/5/15 | 86 | 0 | 0.624 | none |
| 2S Knee | 2S | 15_13 288-308 | 21 | 10/4/18 | 74 | -9 | 0.8405 | jump, super |
| 236P Lariat | 236P | 17_10 37-57 | 21 | 8/4/16 | 84 | -4 | 1.0516 | super, 214S |
| 623S Uppercut | 623S | 17_10 149-169 | 21 | 6/5/22 | 104 | -18 | 0.7565 | none |
| 214S Command Grab | 214S | 141_23 11-33 | 23 | 6/3/18 | 140 | 0 | 1.3017 | none |
| 6S Step Hook | 6S | 17_10 173-193 | 21 | 11/4/16 | 86 | -6 | 0.9498 | 214S, super |
| [4]6P Straight | [4]6P | 86_08 1502-1524 | 23 | 16/5/20 | 102 | -12 | 0.6669 | super |
| Throw | P+M | 111_33 314-336 | 23 | 5/2/18 | 150 | 0 | 1.4059 | none |
| Super | super | 15_13 820-854 | 35 | 15/8/22 | 280 | -14 | 0.9722 | none |

214S uses the same S-button check as Aika. The motion is 214. The grab uses the existing throw rule: both fighters grounded, about a metre apart, 6-tick tech.

Intro is the walk from `111_34` frames 8-40. Victory is the bow `111_02` frames 17-77. Walk loops `111_34` 82-110. Dash is `17_10` 355-373. Jump is the small rise in `143_06` frames 12-32, hip about 75 to 95 cm, not UnityChan's taller hop. Crouch is the duck in `139_09` 110-130. Hits, back walk, and backdash stay on the Kevin Iglesias clips.

2S on hit jump-cancels into j.P, j.M, j.S. j.S knocks down. A normal jump is still one air attack.

