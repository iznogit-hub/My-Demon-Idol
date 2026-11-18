# Akio movelist

C01 Akio is the only fighter on this route. `CharacterDefinition_C01_Akio.akioDepthMovelist` is the opt-in. Every other fighter leaves that flag off, so their buttons, hitboxes, hurtboxes, and the shared `MDI_KI_Melee_Fighter` controller stay as they are.

Akio's animator is `Akio_KI_Melee_Override`, parented to `MDI_KI_Melee_Fighter`. The base controller is not edited. Many states share one KI clip, so a runtime copy of the override swaps that source clip immediately before the trigger. State speeds stay baked. `animator.speed` for a move is `(frames / 24) / (ticks / 60) / bakedStateSpeed`, then returns to 1 when she goes back to idle, walk, crouch, hitstun, or blockstun.

Clips are 24 fps windows inside the CMU takes. Get-up and the fall start at frame 8. `88_06` and the flip takes are not used. A full-take clip is kept on each edited FBX so the import still has a default clip.

Buttons stay P, M, and S. K (`InputButton.Medium`) is a second M for Akio only.

## Frame data

Ticks are startup / active / recovery at 60 Hz. On block is her frame advantage. Cancels require a hit or a block inside the move's cancel window (first active tick through six ticks of recovery). Rapid cancel is still 50 meter. Super is still 100 meter.

| Move | Input | Trigger | Clip | Frames | Ticks | Dmg | On block | Animator speed | Cancels on hit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 5P Jab | 5P | LightAttack | 144_20 110-122 | 13 | 6/3/8 | 30 | -1 | 0.4096 | 5M, 2M, 6S, 2S, 236P, 623S, super, rekka1 |
| 2P Crouch Jab | 2P | LowAttack | 144_20 196-214 | 19 | 7/3/11 | 28 | -2 | 0.6854 | 2M, 6S, 236P, super |
| j.P Jump Jab | j.P | AirLight | 143_23 42-51 | 10 | 5/3/8 | 32 | 0 | 0.4464 | jM |
| 5M Strong Straight | 5M | HeavyAttack | 144_20 123-136 | 14 | 8/4/12 | 70 | -3 | 0.5164 | rekka2, 6S, 2S, 236P, 623S, super |
| 6M Overhead | 6M | OverheadAttack | 144_20 148-166 | 19 | 12/4/16 | 75 | -6 | 0.7422 | super |
| 2M Sweep | 2M | LowSweepAttack | 135_05 100-120 | 21 | 9/4/16 | 70 | -8 | 0.8107 | super |
| j.M Jump Kick | j.M | AirHeavy | 75_16 40-58 | 19 | 8/6/14 | 80 | 0 | 0.5125 | jS |
| Rekka 1 | mash M from neutral | MediumAttack | 144_20 34-47 | 14 | 6/3/8 | 35 | -1 | 1.0294 | rekka2, 6S, super |
| Rekka 2 | M during rekka 1 or 5M | HeavyAttack | 144_20 123-136 | 14 | 7/3/11 | 45 | -2 | 0.5902 | rekka3, 6S, super |
| Rekka 3 | M during rekka 2 | LowSweepAttack | 135_05 100-120 | 21 | 9/4/14 | 60 | -6 | 0.8708 | 6S, super |
| 2S Launcher | 2S | LauncherAttack | 135_07 140-164 | 25 | 11/5/18 | 75 | -10 | 0.9417 | jump, super |
| j.S Air Finisher | j.S | JumpAttack | 75_16 44-60 | 17 | 9/6/16 | 90 | 0 | 0.5223 | none |
| 236P Fireball | 236P | ThrowCard | KI MagicAttackDirect1H01 (ThrowCard) | KI | 12/1/20 | 60 | -4 | 1 | super |
| 6S Rush | 6S | AdvancingSkill | 144_17 24-50 | 27 | 11/4/16 | 90 | -6 | 1.2212 | super |
| 623S Dragon Punch | 623S | DriveSkill | 135_07 15-34 | 20 | 6/6/22 | 100 | -18 | 0.6993 | none |
| Throw | P+M | Throw | 144_17 26-48 | 23 | 5/2/18 | 120 | 0 | 1.4059 | none |
| Super | super | DomainSuper | 135_06 48-80 | 33 | 14/8/22 | 280 | -12 | 0.9375 | none |

## Routes

- Gatling: 5P into 5M into 6S, on hit and on block. 5P also cancels into 2M on hit, and into 2S, 236P, 623S, and super.

- Normals cancel into specials and super on hit and on block where the list above says so. 623S does not cancel. 6M and 2M cancel only into super.

- Auto-combo: a second M inside 10 ticks from neutral starts Rekka 1, then Rekka 2, then Rekka 3. A single neutral M is 5M. 6M and 2M follow the stick. Rekka 2 also comes out of 5M on hit or block.

- 2S on hit jump-cancels. That jump opens one air chain, j.P into j.M into j.S. j.S knocks down. A normal jump still has a single air attack.

- 236P spends the projectile meter when it is accepted. The projectile spawns on the first active tick (after the 12-tick cast) and uses the existing Akio projectile at damage 60. Raw P is a jab and does not spend the meter. An empty meter on 236P whiffs. Other fighters still fire their meter projectile on P.

- 623S is strike-invulnerable on ticks 1 through 6. 2S remains the launcher when there is no dragon-punch motion.

- 214 and the back-charge (40 ticks, then forward) are parsed and not bound to a move.

- Dash is f,f or b,b inside 22 ticks, plus the existing 14-tick dash.

## Hitboxes and pushbox

Each Akio attack sets `akioApplyHitbox`. The prefab box is resized for that move and, when a bone is set, follows Right wrist (RightHand), Right ankle (RightFoot), or Chest during the active frames. The super and the rush each have two phases: fist, then foot. 236P does not enable her body box. The projectile is the hit.

Crouch pushbox: height 0.55 m, radius 0.22 m. Air pushbox: height 1.10 m, radius 0.22 m. Standing restores the controller that was on her when the match started. Crouch shrinks the leg hurtbox to 0.55, the torso to 0.75, and the head to 0.35 of their cached size. Air uses 0.85. This component is added at runtime and is not on the prefab.

## Locomotion and round flow

| Pose | Clip | Notes |
| --- | --- | --- |
| Walk forward | 144_33 frames 150-178, loop | Back walk stays the KI backward run |
| Dash | 144_17 frames 20-40 | 12 ticks of travel, animator speed 1.458 |
| Backdash | KI backward run | 14 ticks, unchanged |
| Jump | 75_16 frames 32-50 | The kick itself is j.M / j.S |
| Crouch | 135_05 frames 80-92, loop | |
| Stand block | 144_07 frames 186-206, loop | Hold back. Also blockstun |
| Crouch block | 135_10 frames 300-320, loop | Hold down-back |
| Knockdown | 90_18 frames 8-45 | Animator speed 1.620 over a 48-tick knockdown |
| Get-up | 111_06 frames 8-48 | Animator speed 0.664. Hip Y goes negative in the take. Preview the retarget. |
| Intro | 144_33 frames 2-48 | Fired from ResetToIdle instead of Idle, Akio only |
| Victory | KI Idle03 | Already fired on the winner |
| Taunt | KI Idle02 | No taunt button, and the shared controller has no Taunt trigger, so it is not wired |
| Hits | KI CombatDamage01 / CastingDamage01 | Unchanged |
| Rapid cancel | KI CastingExit01 | 50 meter, unchanged. The get-up override is cleared before this trigger so it does not play the get-up |

## Unity checklist

1. Reimport the twelve CMU FBX files listed above. Confirm the inspector says 24 fps and that each Akio sub-clip's length matches the frame window.

2. Open 111_06 `Akio_Getup` in the animation preview on Akio's avatar. The source hip goes below zero. If she sinks through the floor, the rotations may still stand her up. If she does not, that one clip needs a later retarget pass. Do not start it at frame 0.

3. Start a round as Akio. She should walk in on the intro, then idle. Victory still plays on a KO.

4. Test 5P, 2P, j.P, 5M, 6M, 2M, j.M, mash M (rekka), 2S, jump-cancel into j.P > j.M > j.S, 236P with meter, 236P without meter, raw P with a full meter (jab, no projectile), 623S, 6S, throw, super.

5. Hold back and down-back and confirm the two block poses. Crouch should shorten her pushbox. Dash with f,f and b,b.

6. Load Aika (or any other fighter) in the same build. P with a full meter still fires her projectile. P with an empty meter still whiffs. Her hitboxes, walk, and attacks should match the build from before this change.

