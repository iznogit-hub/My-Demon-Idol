# KI melee animation hookup

Unity was not opened. Nothing here was imported or played. `tools/validate_ki_melee.py` is the static check (it passed when this file was written). Confirm clip lengths in the Animation preview on first import: speeds assume each clip's `lastFrame` is a 30 fps take and combat ticks are 60 Hz (`speed = (frames / 30) / (ticks / 60)`). If Unity reports 60 fps clips, halve every attack speed.

The shared controller is `Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller`. The 11 girl prefabs point their Animator at it. Root motion stays off. The 56 prefab variants do not override the Animator, so they inherit this controller.

Menu **Demon Idol/Animation/Rebuild KI Melee Controller** reassigns those prefabs. It rebuilds the controller only when the asset is missing. It does not delete files.

## Clips that exist (female free pack)

Melee: `Attack1H01_R`, `Attack1H01_L`, `Attack2H01`, `AttackPolearm01`, `AttackShield01`, `CombatIdle01`, `CombatIdle1H01`, `CombatIdle2H01`, `CombatIdlePolearm01`, `CombatDamage01`, `Death01`.

Movement: `Idle01`, `Idle02`, `Idle03` and the idle-to-idle bridges, in-place `Run01` forward/back/strafe. Root-motion copies exist and are not used.

Spellcasting fallback: `CastingEnter01`, `CastingExit01`, `CastingIdle01`, `CastingDamage01`, `MagicAttackDirect1H01_R`, `MagicAttackDirect2H01`, `MagicAttackOmni01`.

## Slots with no KI clip

| Slot | What plays | Why |
| --- | --- | --- |
| Walk | In-place `Run01` forward and back, blended on `MoveX` | Free pack has run, not walk |
| Crouch | Looping `CombatIdle01` while `IsCrouch` | No crouch clip |
| Block hold | Looping `CombatIdle1H01` while `IsBlocking` | No block loop. `AttackShield01` is a bash, used for Throw |
| Jump | `CastingEnter01` | No jump |
| Get-up | `CastingExit01` on `StandUpBack` | No get-up |
| Knockdown | `Death01` holds its last frame until `StandUpBack` | No knockdown. Same clip as defeat, different state |
| Defeat | `Death01` holds. No transition back to idle | Stays down until `Idle` (round reset) |
| Victory | Looping `Idle03` until `Idle` | No victory pose |
| Intro | `CastingEnter01` | Nothing in code fires `Intro` |

Hit reactions reuse `CombatDamage01` and `CastingDamage01`. Hits stay frame-data driven. Footstep and swing events were not added: the KI clips are shared with the pack's demo scenes.

## Code that only feeds the animator

`FighterController` sets `IsBlocking` from the existing `Blockstun` state, and the winner gets `Victory` when the loser plays `Defeat`. Frame data, damage, and advantage are unchanged.

`Kai_LowAttack.animationTrigger` is `LowAttack` (it was `HitLow`, so the attacker played a hit). `Kai_Throw.animationTrigger` is `Throw` (it was `Knockdown`, so the thrower played the fall). Tick counts on those assets are unchanged. `Kai_DriveSkill` still fires `ThrowCard`; `DriveSkill` is also a state because `NodeTeleportSkill` and the sample moveset use that name.

## Per character

| Fighter | Humanoid | Other fix | Controller |
| --- | --- | --- | --- |
| Aika | 55 bones, hips `Hips` under Armature, chest, upper chest, toes, jaw, eyes | None | KI melee, root motion off |
| Fina | 55 bones, hips `pelvis` under root, full optional set | None | KI melee |
| Akio | 55 bones, hips `Hips`, full optional set | None | KI melee (was an override controller) |
| Cultist | 53 bones, hips `Hips`, chest, toes, eyes. No upper chest or jaw bone | None | KI melee |
| StellarWitch | Hips remapped from floor `Root` to pelvis `Hip` (parent of both legs and Spine). Upper chest `Spine2`, toes, jaw, eyes already mapped | Import setting only | KI melee. Reimport the FBX so the avatar rebuilds |
| Akane | 53 bones. No chest bone between `Spine` and `Neck`, so Chest stays unmapped | None. Spine twist will spread | KI melee |
| Muryotaisu | 51 bones, chest present. No upper chest, toes, or jaw | `MuryotaisuController` disabled | KI melee |
| SanpakuChan | 54 bones, hips `Hips`, upper chest and toes. No jaw | None | KI melee |
| Reika | 50 bones, hips bone is named `pevis` (the pelvis). Chest and upper chest mapped. No toe or jaw bones | Kiki `PlayerController` disabled. Rigidbody set kinematic and gravity off. The demo capsule collider on that object disabled so it does not fight `CharacterController` | KI melee |
| UnityChan | 51 bones, hips `Character1_Hips`, toes present. No upper chest, jaw, or eyes | None | KI melee |
| PicoChan | 55 bones, full optional set | Second Animator, on `pico_chan_chr_pico_00`, disabled. The root Animator stays enabled | Both Animators reference the KI controller. Only the root one runs |

## Left unchanged

| Model | Why |
| --- | --- |
| Ren | Grey Kevin Iglesias dummy, not a roster girl |
| Casual1 | Humanoid import with an empty bone map. Thighs and spine are siblings of `Hip` under `Armature`, so mapping Hips to either bone is the floor-pivot problem. That needs a Blender reparent, not a meta edit |
| SapphiArtchan | Generic (`animationType` 2), empty human map. A full humanoid map was not authored blind |

## First import

1. Reimport `StellarWitch.fbx` and confirm the avatar's Hips bone is `Hip`, not `Root`.
2. Select `MDI_KI_Melee_Fighter` and check every state has a clip (no missing motion).
3. Read one attack clip's length. If it is about half of `lastFrame/30`, the file is 60 fps and the state speeds should be halved.
4. Play Aika or Akio: idle at `MoveX` 0, forward run when `MoveX` is positive, back run when negative. Blockstun should hold `CombatIdle1H01` and release when blockstun ends. Defeat should stay down. `StandUpBack` should leave knockdown.
5. Reika should not walk herself from the Kiki script. Pico should have one active Animator.
