# My Demon Idol: Iron Legacy — BlazBlue 4-Button Implementation Specification

**Audience:** the AI coder implementing the system in Unity.
**Status:** final hand-off spec, launch-ready target.
**Engine:** Unity 6000.6.8f1, URP 17.6.0, Input System 1.20.1 (`activeInputHandler: 2`, both old and new input), Test Framework 1.8.0.
**Source of truth used:** `origin/main` of `iznogit-hub/My-Demon-Idol` at merge commit `018767d` (PicoChan). Every class, field and path below was read from that commit. Anything marked **NEW** does not exist yet and is a proposal.
**Spec date:** 18 Nov 2026 (IST).

---

## 0. How to read this document

- **EXISTS** = in the repo now, with a real path.
- **NEW** = you create it. Names are suggestions; keep them consistent once chosen.
- **TBD** = needs a measurement in Unity, a clip search, or a decision by Izno.
- Ticks are the 60 Hz combat ticks from `CombatTickEngine` (`TICK_RATE = 60`). CMU clips are 24 fps. **1 clip frame = 2.5 ticks.**
- Numpad notation: 7 8 9 / 4 5 6 / 1 2 3, facing right. `[4]6` = hold back, then forward. `j.` = in the air.
- Buttons: **A** weak, **B** medium, **C** heavy, **D** Drive (each girl's unique mechanic).

---

## 1. Goals, non-goals, hard constraints, definition of done

### 1.1 Goals
1. Add a full BlazBlue-style (Central Fiction inspired) **A/B/C/D** control scheme, with keyboard, gamepad and phone touch layouts. iOS and Android are the main targets.
2. Add the core BlazBlue systems: Revolver Action gatlings, double jump and air dash, Heat gauge, Rapid Cancel, Overdrive, Burst, Crush Trigger, Barrier and the Barrier gauge, Counter Assault, Distortion Drives, an optional Astral Heat, throws and throw tech, Fatal Counter, proration, hitstun decay, gravity scaling, wall and ground bounce, wakeup and tech, guard crush and chip damage.
3. Build each girl one at a time, at SF6/BlazBlue depth: her own Drive, motion specials, cancel routes, air combos, per-tick hitboxes, Distortion Drives, intro and victory.
4. Make the hitbox, hurtbox and pushbox pipeline deterministic, allocation-free and fast on mid-range phones.
5. Ship launch features: arcade, versus, training with frame data and a hitbox view, tutorial and challenges, AI per character, a HUD for the new gauges, and settings.

### 1.2 Non-goals for this pass
- Online play. Section 9.12 covers rollback-ready notes only.
- Lore, story text and branding, which are on hold.
- New character models, rigs or Blender work. Casual1 and SapphiArtchan stay out.
- Replacing or editing the Classic 3-button game.

### 1.3 Hard constraints (non-negotiable)

| # | Rule |
|---|---|
| C1 | **Nothing already built may break.** The Classic P/M/S game and the current depth movelists stay byte-for-byte unchanged in behaviour. Akio uses `akioDepthMovelist`; Aika, UnityChan and PicoChan use `depthMovelist`. |
| C2 | BlazBlue is added **alongside** Classic and can be switched on. Use a NEW `ControlScheme { Classic3Button, BlazBlue4Button }` setting. The default stays `Classic3Button` until Izno flips it. |
| C3 | **Add-only.** Never delete files. Edits to existing files are allowed only as small, guarded hooks that do nothing when the scheme is Classic. |
| C4 | Do not touch avatars, model prefabs, the 56 character variants (`Assets/Prefabs/Characters/Variants/`), or the base controller `Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller` (guid `b7c4e1a09d2f4c6e8a1b3d5f7091c2e4`). |
| C5 | Each fighter keeps her own AnimatorOverrideController on top of `MDI_KI_Melee_Fighter`. BlazBlue adds a NEW per-fighter override and leaves the existing one alone. |
| C6 | All playable characters are girls. C03 Ren is skipped (grey Kevin Iglesias dummy). C11 Casual1 and C13 SapphiArtchan are excluded for rig problems. |
| C7 | Animation comes from the CMU "Huge Mocap Library" (`Assets/Huge Mocap Library/mocap animations/<subject>/<take>.fbx`, 2,534 Humanoid FBX files, 24 fps, one long take each) and the Kevin Iglesias Human Melee and Human Spellcasting free packs. New moves are cut only as **NEW `clipAnimations` entries in the `.fbx.meta` files**. Never edit an existing clip entry. |
| C8 | Moves combine takes. No tiny moves, no flips, no cartwheels, and no `88_06`. |
| C9 | Clips must stay 24 fps after reimport. A take whose hip sinks below the floor uses `keepOriginalPositionY: 0` and/or a `firstFrame` of 8 or later. |
| C10 | Git: `main` only. Use short-lived feature branches, one PR per milestone or character, merge, then delete the branch. The repo is private and must stay private because of the Sonniss and UCL licences. |
| C11 | Never put secrets, keys or tokens in code, docs or commits. |

### 1.4 Definition of done ("launch ready")
A build counts as launch ready only when **all** of these are true:
1. **Classic regression is green:** every test in section 10.3 passes, and Classic golden replays produce identical state hashes before and after the BlazBlue code lands.
2. **The BlazBlue scheme works end to end** for at least the 9 roster girls (section 8). Every move on each movelist table is authored with data, a clip and per-tick boxes, and is reachable in training mode.
3. **All core systems in section 4 are implemented.** Each tunable lives in one NEW `BBSystemTuning` asset, and each system has an EditMode test.
4. **Determinism:** the same input log gives the same per-tick state hash 100 times in a row in EditMode, and across two devices (one Android, one iOS) for 3 recorded matches.
5. **Performance:** combat simulation is ≤ 1.0 ms per tick for both fighters together on the reference Android device, with zero GC allocation per tick. Render holds 60 fps on reference devices (section 6.9).
6. **Touch:** a first-time tester can do 236 and 623 specials, a B+C throw and an A+B barrier on a 6.1" phone with the default layout. Specials have a ≥ 90% success rate in the tutorial challenge.
7. **Content:** every per-character Unity checklist (section 10.5) is signed off. Clips are 24 fps, no fighter sinks into the floor, and foot slide is under 3 cm on walk loops.
8. **Store builds:** Android AAB and iOS archive build from CI or the Editor with no compile warnings in `Assets/Scripts/BlazBlue/**` (NEW folder).

---

## 2. Current architecture (as found on `origin/main`)

### 2.1 Folder map (game code only)

| Area | Path | Notes |
|---|---|---|
| Combat core | `Assets/Scripts/Combat/` | `FighterController.cs` (1,975 lines), `Hitbox.cs`, `Hurtbox.cs`, `CombatTickEngine.cs`, `JuggleEngine.cs`, `BarrierBlockSystem.cs`, `PsychicBurstSystem.cs`, `RapidCancel.cs`, `DomainExpansion.cs`, `OkizemeWakeup.cs`, `PushbackSolver.cs`, `HitboxVisualizer.cs`, `FrameDataDisplay.cs`, `ProjectileFactory.cs`, `MDIProjectile.cs`, `HomegrownCombatDirector.cs`, `MatchRoundReferee.cs`, `CombatAIController.cs`, `TrainingDummyAI.cs`, `PracticeDojoController.cs`, `MatchReplaySession.cs`, `GameplaySimulationAuthority.cs`, `CombatEnums.cs`, `AkioPresentation.cs`, `AkioBodyFit.cs`, `NodeTeleportSkill.cs`, `CrossPlatformOptimizer.cs` |
| Data | `Assets/Scripts/Data/` | `AttackData.cs`, `MoveSet.cs`, `CharacterDefinition.cs`, `CharacterRegistry.cs`, `AkioClipLibrary.cs`, `StageDefinition.cs`, `VideoBindings.cs` |
| Input | `Assets/Scripts/Input/` | `InputBuffer.cs`, `CommandParser.cs`, `MobileFighterInput.cs`, `TouchVirtualControls.cs`, `VirtualJoystickZone.cs`, `SuperVirtualJoysticksController.cs`, `MobileHaptics.cs` |
| Depth core | `Assets/MDI_Characters/Runtime/AkioCore/` | `MotionMatcher.cs`, `CancelQuery.cs` (namespace `MDI.Akio`), `Editor/AkioMotionEditModeTests.cs` (12 tests) |
| Movelist docs | `Assets/MDI_Characters/Runtime/` | `AKIO_MOVELIST.md`, `AIKA_MOVELIST.md`, `UNITYCHAN_MOVELIST.md`, `PICOCHAN_MOVELIST.md`, `KI_MELEE_ANIMATION.md` |
| Animators | `Assets/MDI_Characters/Runtime/` | `MDI_KI_Melee_Fighter.controller` (base), `Akio_KI_Melee_Override`, `Aika_KI_Melee_Override`, `UnityChan_KI_Melee_Override`, `PicoChan_KI_Melee_Override` (`.overrideController`, each with `m_Clips: []`, so all swaps happen at runtime) |
| Combat data | `Assets/CombatData/` | `Characters/CharacterDefinition_C01..C14_*.asset`; `Akio/` (18 AttackData + `AkioMoveSet`); `Aika/`, `UnityChan/`, `PicoChan/` (20 AttackData + MoveSet each); `Kai_*.asset` legacy presets; `Stages/` ST01–ST06 |
| UI | `Assets/Scripts/UI/` | `BattleHUDController.cs`, `SettingsPanel.cs`, `CharacterSelectUI.cs`, `StageSelectUI.cs`, `MainMenuController.cs`, `SafeAreaFitter.cs`, ... |
| Flow | `Assets/Scripts/Launch/` | `DemonIdolGameFlowController.cs`, `OfflineMatchSession.cs` (`PlayModeKind { Story, Arcade, LocalVersus, Training, OnlineUnavailable }`), `LaunchServices.cs`, `MatchResultsOverlay.cs`, `AtomicFileSave.cs` |
| Cutscenes | `Assets/Scripts/Cutscenes/` | `CutsceneManager.cs`, `VideoPathResolver.cs`, `DemonLordNarratorManager.cs` (VideoPlayer, 62 `.mp4` files in the repo) |
| Online (unused) | `Assets/Scripts/Network/`, `Assets/Photon/Quantum/`, `Assets/QuantumUser/` | Photon Quantum SDK, `QuantumCombatBridge`, `QuantumFighterInput`, `RollbackQualityProbe` |
| Tools | `tools/` | `validate_akio_movelist.py` (467 lines), `validate_ki_melee.py`, `build_ki_melee_controller.py`, `generate_mdi_variants.py` |
| Editor | `Assets/Scripts/Editor/`, `Assets/MDI_Characters/Editor/` | `FighterMoveSetValidator.cs`, `MatchSimulationVerificationTest.cs`, `LaunchSystemVerification.cs`, `MDIKIMeleeControllerBuilder.cs`, `MDI_RosterAnimationBinder.cs`, `MDI_FighterAssembler.cs`, ... |

**Universal Fighting Engine:** no UFE package files are on `origin/main`. `GameplaySimulationAuthority.IsPackageNative => false`, and that class labels UFE 2 as deprecated. The only UFE leftover is `CharacterDefinition.ufeCharacterName` and `GetEffectiveUFEName()`, a legacy name mapping that has no gameplay effect. The homegrown engine is the baseline, so build BlazBlue on it.

### 2.2 Simulation loop
- `CombatTickEngine` (singleton) builds up `Time.unscaledDeltaTime`, clamps the build-up at 0.2 s, and fires `OnTick(int tick)` at 60 Hz. `IsSuperfrozen` and `TriggerSuperfreeze(ticks)` exist. `InterpolationAlpha` is available for rendering.
- `FighterController.HandleCombatTick(tick)` checks in this order: superfreeze, superflash, hitstop (each freezes `InputBuf` age), recovery protection, being thrown (`UpdateThrowBreakState`), dead. It then latches facing in neutral, records the stick with `CmdParser.RecordDirection(_moveInput, tick, FacingDirection)`, then runs `ProcessBufferedInputs`, `UpdateStateMachine` and `ApplyMovementPhysics`.
- Movement uses a Unity `CharacterController.Move(_velocity * TICK_DELTA_TIME)`, with gravity 20 or `Juggle.GetCurrentGravity()` while juggled.
- **Hit detection is physics-driven:** `Hitbox` is a trigger `Collider` that uses `OnTriggerEnter` against `Hurtbox` trigger colliders. Physics runs at **Fixed Timestep 0.02 (50 Hz)** (`ProjectSettings/TimeManager.asset`), not at the 60 Hz combat tick. This is the biggest determinism gap; section 6 replaces it for BlazBlue mode.
- The layer matrix is all ones (`ffff…`), and there are no custom layers in `TagManager.asset`.

### 2.3 Input flow today (Classic)
```
Keyboard / Gamepad / Touch
  -> MobileFighterInput (samples edges each frame; strict same-tick chords)
       J=P (Light)  K=M (Heavy)  L=S (Skill)
       Space/T=Throw  U/O=Super  B=Burst
       Pad: X=P  Y=M  B=S  A=Throw  RB/RT=Super  LB/LT=Burst
       Chords: P+M+S => Ultra ; P+M => GrabBreak(Burst) ; 66 within 1.4 m => CommandGrab
  -> FighterController.InputBuf.Enqueue(ThreeButtonCommand, tick, dir)   (InputBuffer: 16-slot ring, 5-tick leniency)
  -> FighterController.SetMoveInput(dir) -> CmdParser.RecordDirection (32-slot edge history)
  -> ProcessBufferedInputs:
       Knockdown wakeup -> Ultra/Super -> GrabBreak/Burst -> CommandGrab/Throw
       -> (hitstun/blockstun/juggle: stop)
       -> S: UsesAkioMovelist ? TryAkioSpecial : neutralS/driveSkill (or NodeTeleportSkill)
       -> M: UsesAkioMovelist ? TryAkioMelee  : 3-step M combo (standingH -> forwardH -> crouchingH), air airH
       -> P: UsesAkioMovelist ? TryAkioProjectile : meter projectile (projectileMeter >= 0.95)
  -> MoveSet.ResolveAttack(ThreeButtonCommand, grounded, dir, facing)   (Classic quantised: down / forward / neutral-back)
  -> StartAttack(AttackData) -> AttackStartup -> EnterAttackActive (activeHitbox.Activate; projectile for "236P")
     -> EnterAttackRecovery
  -> Hitbox.OnTriggerEnter -> Owner.OnHitboxLanded -> defender.ReceiveIncomingStrike -> HitResult
```

### 2.4 The depth path (Akio, Aika, UnityChan, PicoChan)
- The gate is `CharacterDefinition.UsesDepthMovelist => CancelQuery.IsOptedIn(akioDepthMovelist, depthMovelist)`, mirrored as `FighterController.UsesAkioMovelist`.
- Motions come from `MotionMatcher` (`Is236`, `Is214`, `Is623`, `IsDoubleTap`, `IsChargeBackForward`; `LenientWindow = 22`, `ChargeHoldTicks = 40`, `ChargeReleaseTicks = 12`, `ChargeLookback = 90`), wrapped by `CommandParser.CheckQuarterCircleForward/Back`, `CheckDragonPunch`, `CheckLenientDash` and `CheckChargeBackForward`.
- Cancels come from `CancelQuery.Allows(akioCancelOnHit, akioCancelOnBlock, nextId, hit, blocked)` inside `cancelWindow` (a `Vector2Int` in ticks), through `FighterController.AkioCancelAllowed`.
- Move slots on `MoveSet` are `akioRekka1..3`, `akioFireball`, `akioDragonPunch`, `depthQuarterBack` (214S), `depthQuarterForward` (236 override, used by PicoChan's lariat), `depthCharge` ([4]6P) and `depthChainA/B/C` (string ids; empty means `5M`, `rekka1`, `rekka2`).
- Per-move `AttackData` fields: `akioApplyHitbox`, `akioAuthoredCancels`, `akioStrikeInvuln`, `akioKeepRecovery`, `akioFollowBone`, `akioBone` (`HumanBodyBones`), `akioMoveId`, `akioClipSlot`, `akioAnimatorSpeed`, `akioCancelOnHit[]`, `akioCancelOnBlock[]`, `akioHitPhases[]` (`AkioHitPhase{startTick,endTick,offset,size,followBone,bone}`).
- Hitbox: `Hitbox.ApplyAuthored(attack, tick, owner)` resizes the prefab's BoxCollider each tick and optionally snaps it to a bone in `LateUpdate`, using `bone.position + forward*x + up*y + right*z`.
- Body fit: `AkioBodyFit` is added at runtime. It handles crouch and air pushbox (`CharacterController` height and radius) and scales the hurtboxes. `CharacterDefinition.depthCrouchHeight/Radius`, `depthAirHeight/Radius`, `depthCrouchLeg/Torso/HeadScale` and `depthAirHurtScale` are used, and 0 means Akio's constants.
- Presentation: `AkioPresentation.Prepare(fc, trigger)` swaps a source clip in a runtime copy of the override (`AkioRuntimeOverride`) and sets `animator.speed = (frames/24)/(ticks/60)/bakedStateSpeed`. `AkioClipLibrary` holds the KI source keys and the CMU sub-clips (walk, dash, jump, intro, crouch, stand/crouch block, fall, get-up, victory).
- `HomegrownCombatDirector.ConfigureFighter` applies the definition, assigns `charDef.animatorController` and `charDef.avatar` to the model's Animator, then calls `fc.InstallAkioPresentationIfEnabled()`.

### 2.5 Existing "BlazBlue-named" pieces (useful, but not BlazBlue rules yet)

| Class | What it does now | Gap versus this spec |
|---|---|---|
| `RapidCancel` | 50 Super Meter, `ForceCancelToNeutral()` from any attack state | Needs a hit/block/whiff rule, a BlazBlue-mode Heat cost, a slowdown and a cancel window |
| `BarrierBlockSystem` | Drains **Super Meter** at 15 per second; pushback ×2.2; no chip | Needs its own Barrier gauge, Danger state and Instant Barrier |
| `PsychicBurstSystem` | Burst gauge 0–100, passive +2 per second, +0.08 per damage, Blue/Gold burst, 3.5 m radius | Needs a BlazBlue cost model, Overdrive link and a per-round rule |
| `JuggleEngine` | 6-point juggle budget, gravity +0.15 per hit, damage scale 100→30%, wall/OTG once per combo | Needs P1/P2 proration and hitstun decay by combo time |
| `DomainExpansion` | 100 meter cinematic super, 20 freeze ticks | Use as the Astral Heat presentation hook |
| `OkizemeWakeup` | Tekken-style wakeup choices | Needs BlazBlue air/ground tech |
| `FighterState` / `HitResult` enums | `BurstActive`, `BarrierActive`, `BarrierBlocked`, `BurstBreak`, `Teched` already exist | Reuse them; add NEW states only through new enums |

### 2.6 Roster data on `origin/main`

| Id | Name | `archetype` field | Depth flag | `moveSet` | Definition `animatorController` points to | Rig note (KI_MELEE_ANIMATION.md) |
|---|---|---|---|---|---|---|
| C01 | Akio | Rushdown / Mixup | `akioDepthMovelist: 1` | AkioMoveSet | `Akio_KI_Melee_Override` | 55 bones |
| C02 | Aika | Karate Zoner | `depthMovelist: 1` | AikaMoveSet | `Aika_KI_Melee_Override` | 55 bones |
| C03 | Ren | — | — | none | `AnimatorOverride_C03_Ren` | grey dummy, **skip** |
| C04 | UnityChan | Aerial Hopper | `depthMovelist: 1` | UnityChanMoveSet | `UnityChan_KI_Melee_Override` | 51 bones, no upper chest/jaw/eyes |
| C05 | Akane | Rushdown / Mixup | — | none | Akane demo `AnimationController.controller` | 53 bones, **no chest bone** |
| C06 | Cultist | Rushdown / Mixup | — | none | `MDI_FighterTemplate.controller` | 53 bones, no upper chest/jaw |
| C07 | StellarWitch | Rushdown / Mixup | — | none | `MDI_FighterTemplate.controller` | hips remapped to `Hip`; **reimport FBX so avatar rebuilds** |
| C08 | SanpakuChan | Rushdown / Mixup | — | none | `MDI_FighterTemplate.controller` | 54 bones, no jaw |
| C09 | PicoChan | Close Grappler | `depthMovelist: 1` | PicoChanMoveSet | `PicoChan_KI_Melee_Override` | 55 bones; second Animator disabled |
| C10 | Reika | Rushdown / Mixup | — | none | Kiki Xmas Girl `Animator Controller` | 50 bones, hips bone named `pevis`; Kiki `PlayerController` disabled |
| C11 | Casual1 | — | — | none | AnimeGirls `AC.controller` | empty bone map, **excluded** |
| C12 | Fina | Rushdown / Mixup | — | none | `finna.controller` | 55 bones |
| C13 | SapphiArtchan | — | — | none | `SapphiArtchanAnimController` | Generic rig, **excluded** |
| C14 | Muryotaisu | Rushdown / Mixup | — | none | `Muryotaisu.controller` | 51 bones, no upper chest/toes/jaw; `MuryotaisuController` disabled |

> **Important finding:** `KI_MELEE_ANIMATION.md` says the 11 girl *prefabs* point at `MDI_KI_Melee_Fighter`. But for C05–C08, C10, C12 and C14 the *CharacterDefinition* `animatorController` points at vendor demo controllers or `MDI_FighterTemplate`, and `HomegrownCombatDirector.ConfigureFighter` assigns `charDef.animatorController` at match start. So in a real match, those fighters may run the vendor controller instead of the KI one. Verify this in Play Mode before building them. Section 12 lists the fix as a decision (a NEW per-fighter override, assigned only in BlazBlue mode).

### 2.7 Classic numbers that must stay frozen
- Default `InputBuffer` leniency is 5 ticks. `SettingsPanel.InputBufferFrames` defaults to 4 but is **not read anywhere else**, so it isn't wired.
- `RapidCancel.RAPID_CANCEL_COST = 50`. `DomainExpansion.DOMAIN_COST = 100`. `BarrierBlockSystem.METER_DRAIN_PER_SECOND = 15` and `BARRIER_PUSHBACK_MULTIPLIER = 2.2`.
- `JuggleEngine`: `TOTAL_JUGGLE_BUDGET = 6`, `GRAVITY_SCALING_PER_HIT = 0.15`, `BASE_GRAVITY = 20`.
- `PushbackSolver.ARENA_LIMIT_X = 10.5`, `WALL_COLLISION_THRESHOLD = 9.8`.
- `FighterController` locomotion: walk 3.6/2.8, jump 8.0/3.2, gravity 20, dash 6.5 for 12 ticks, backdash 7.0 for 14 ticks (5 invulnerable), parry 5/14/18.
- `OkizemeWakeup`: rest 24, tech-roll invulnerability 14, stand-up safe 12.

BlazBlue mode reads its own copies of these from the NEW `BBSystemTuning` and **never writes** the Classic constants.

---

## 3. Input system (BlazBlue4Button)

### 3.1 Architecture

```
Device layer (Input System actions, NEW asset MDI_BB_Input.inputactions)
   -> BBInputSampler (NEW, MonoBehaviour, reads devices every render frame, latches edges)
   -> BBInputFrame (NEW struct, one per combat tick, the only thing the sim sees)
   -> BBInputHistory (NEW, 64-tick ring per fighter, allocation-free)
   -> BBCommandInterpreter (NEW, motions + buttons -> BBCommand, priority rules)
   -> BBMoveResolver (NEW, BBCommand + state + cancel rules -> BBAttackData)
   -> FighterController (existing state machine, via the guarded hook in 11.2)
```

Classic keeps `MobileFighterInput` → `InputBuffer` → `CommandParser` exactly as it is. When `ControlScheme == BlazBlue4Button`, `MobileFighterInput` is disabled on that fighter, `BBInputSampler` is enabled, and the fighter's `BBFighterBrain` (NEW) feeds the sim. They are never both active.

```csharp
// NEW: Assets/Scripts/BlazBlue/Core/BBInputFrame.cs  (MDI.BlazBlue.Core)
namespace DemonIdol.BlazBlue
{
    [System.Flags] public enum BBButton : byte { None = 0, A = 1, B = 2, C = 4, D = 8, Taunt = 16 }

    public struct BBInputFrame               // 4 bytes, blittable, rollback-friendly
    {
        public byte held;                    // BBButton mask held this tick
        public byte pressed;                 // rising edges this tick
        public byte released;                // falling edges this tick (negative edge)
        public sbyte numpad;                 // 1..9 facing-relative is computed later; store WORLD numpad (5 = neutral)
    }

    public enum ControlScheme { Classic3Button = 0, BlazBlue4Button = 1 }   // NEW, persisted by SettingsPanel
}
```

**World versus facing-relative:** store the *world* numpad in the frame. Convert to facing-relative only in `BBCommandInterpreter`, using `MotionMatcher.Numpad(h, v, facing)`, which is EXISTING and already correct. This keeps replays valid across side switches.

### 3.2 Button roles

| Button | Role | Classic mapping it reuses internally |
|---|---|---|
| A | Weak normal (fast, short) | `InputButton.Light` |
| B | Medium normal (pokes, anti-air on 6B for some) | `InputButton.Medium` |
| C | Heavy normal (launchers, big buttons) | `InputButton.Heavy` |
| D | Drive, the character's unique mechanic | `InputButton.DriveSkill` |

| Macro (2+ buttons) | Action |
|---|---|
| B+C | Throw (6BC / 4BC / j.BC) and throw tech |
| A+B (while guarding) | Barrier (hold) |
| A+B (neutral, not guarding) | Crush Trigger (hold to charge) |
| 6+A+B (in blockstun) | Counter Assault |
| A+B+C | Rapid Cancel (during an attack) |
| A+B+C+D | Burst (in hitstun or neutral) or Overdrive (neutral, burst gauge available), see 4.6/4.7 |
| D (hold) | Character specific, defined per fighter in section 8 |

### 3.3 Keyboard (default; rebindable)

| Action | Key | Note |
|---|---|---|
| Move | W A S D, plus arrow keys | Same as Classic. `S` and `D` are directions, so no button sits on letter D |
| A / B / C / D | **J / K / L / I** | J, K, L match Classic P/M/S positions; I is above K for D |
| Throw macro (B+C) | **;** (semicolon) | |
| Barrier / Crush Trigger macro (A+B) | **U** | Classic U is Super, but Classic input is disabled in BlazBlue mode |
| Rapid Cancel macro (A+B+C) | **O** | |
| Burst / Overdrive macro (A+B+C+D) | **P** | |
| Taunt | **T** | |
| Pause | Esc | |

### 3.4 Gamepad (Input System names, Xbox / PlayStation)

The face-button layout matches the BlazBlue console default (□ A, △ B, ○ C, × D).

| Action | Input System path | Xbox | PlayStation |
|---|---|---|---|
| A | `<Gamepad>/buttonWest` | X | □ |
| B | `<Gamepad>/buttonNorth` | Y | △ |
| C | `<Gamepad>/buttonEast` | B | ○ |
| D | `<Gamepad>/buttonSouth` | A | × |
| Barrier / Crush (A+B) | `<Gamepad>/leftShoulder` | LB | L1 |
| Throw (B+C) | `<Gamepad>/rightShoulder` | RB | R1 |
| Burst / OD (A+B+C+D) | `<Gamepad>/leftTrigger` | LT | L2 |
| Rapid Cancel (A+B+C) | `<Gamepad>/rightTrigger` | RT | R2 |
| Move | `<Gamepad>/dpad` and `<Gamepad>/leftStick` | | |
| Pause | `<Gamepad>/start` | | |

Stick quantisation: reuse the 8-way angle sectors from `MobileFighterInput.QuantizeDirection` (22.5° boundaries) and its 0.20 deadzone. Make the deadzone tunable in `BBSystemTuning.stickDeadzone`.

### 3.5 Touch layout (phones, landscape)

Design canvas: `CanvasScaler` set to Scale With Screen Size, reference **1920×1080**, match 0.5. All sizes below are reference pixels. Wrap the root in the EXISTING `SafeAreaFitter`.

```
+--------------------------------------------------------------------------------+
| [HP P1 ][Heat][Burst]          99          [Burst][Heat][ HP P2 ]   [II pause] |
|                                                                                |
|                                                                                |
|                                                        [RC 96]   [BURST 96]    |
|                                                  [THROW 110]  [BARRIER 110]    |
|   ( joystick zone )                                   [ B 150 ]  [ C 150 ]     |
|   ( floating, 340 px )                              [ A 150 ]  [ D 170 ]       |
+--------------------------------------------------------------------------------+
```

| Element | Size (ref px) | Anchor and position (from bottom-right unless stated) | Thumb zone |
|---|---|---|---|
| Floating joystick zone | 760×620 area, 340 px ring, 140 px knob | bottom-left. Pressing anywhere in the zone spawns the ring under the thumb (reuse `VirtualJoystickZone`) | left thumb, natural arc |
| A | 150 circle | (-470, 150) | right thumb, resting |
| B | 150 circle | (-430, 330) | right thumb, one step up |
| C | 150 circle | (-250, 300) | right thumb, one step up-right |
| D | **170** circle | (-230, 120) | right thumb, the strongest reach, bottom-right |
| THROW (B+C) | 110 pill | (-470, 500) | stretch |
| BARRIER / CT (A+B) | 110 pill | (-250, 480) | stretch |
| RC (A+B+C) | 96 circle | (-430, 640) | far, used deliberately |
| BURST / OD (A+B+C+D) | 96 circle | (-250, 640) | far, so it isn't pressed by accident |
| Pause | 80 | top-right inside safe area | — |

Rules:
- Minimum physical size is **9 mm** for A/B/C/D and **7 mm** for macros. Compute this at runtime from `Screen.dpi` and clamp the scale. Fall back to 160 dpi when `Screen.dpi == 0`.
- Hit areas are 15% bigger than the visuals, with no overlap between hit areas. Where two areas touch, the closest centre wins.
- **Slide-chording:** a finger sliding from A onto B within 3 ticks counts as A+B. The gesture-chord idea already exists in `MobileFighterInput` ("Slide-to-Chord with Release-to-Commit"); BlazBlue mode uses *press-to-commit* for lower latency.
- Multi-touch: up to 4 simultaneous touches on the right half. Use `UnityEngine.InputSystem.EnhancedTouch`. Do not use `Input.touches` in the BlazBlue path.
- Settings reuse the EXISTING `SettingsPanel.TouchControlsOpacity`, `TouchControlsScale` and `LeftHandedMode`; left-handed mode mirrors the whole layout. There is a NEW per-button drag-to-move editor screen whose positions are saved in the settings JSON.
- Haptics: a light tap on a press and a medium tap on hit-confirm, through the EXISTING `MobileHaptics`, gated by `SettingsPanel.HapticsEnabled`.
- Touch reuses the EXISTING slots `TouchVirtualControls._btnLight/_btnMedium/_btnHeavy/_btnDrive/_btnBarrier/_btnBurst/_btnThrow`. Build a NEW prefab `TouchControls_BB.prefab` instead of editing the runtime-built Classic overlay (`EnsureRuntimeTouchControls`).

### 3.6 Buffers and windows (all in `BBSystemTuning`)

| Tunable | Default (ticks) | Meaning |
|---|---|---|
| `bufferTicks` | **5** | A press stays usable this long (matches Classic `DEFAULT_LENIENCY_TICKS`). Wire `SettingsPanel.InputBufferFrames` into this, clamped to 3–8 |
| `chordWindowTicks` | **3** | Presses within this many ticks of each other make a macro (Classic is strict same-tick, which is too strict for touch) |
| `motionWindow236` | **22** | = `MotionMatcher.LenientWindow` |
| `motionWindow623` | 22 | |
| `motionWindowHalfCircle` (41236/63214) | **30** | |
| `motionWindow360` | **40** | Any 4 cardinal directions in order (6,2,4,8 or the reverse); 8 may be replaced by 7 or 9 (jump-start forgiveness) |
| `motionWindowDD` (236236, 214214, 632146) | **36** | |
| `motionWindow22` | **16** | Down, neutral, down |
| `chargeHoldTicks` | **40** | = `MotionMatcher.ChargeHoldTicks`; charge keeps building during blockstun and attacks |
| `chargeReleaseTicks` | **12** | = `MotionMatcher.ChargeReleaseTicks` |
| `negativeEdgeTicks` | **3** | A release counts as a press for **specials only** if that button was held at least 3 ticks and was not released again inside the window |
| `dashDoubleTapTicks` | 14 strict / 22 lenient | Same as Classic |
| `jumpPrejumpTicks` | **4** | |

### 3.7 Motion matchers (extend `MotionMatcher`, add-only)

EXISTING `Is236`, `Is214`, `Is623`, `IsDoubleTap` and `IsChargeBackForward` stay. NEW methods go in a **new static class** so the Akio file is untouched:

```csharp
// NEW: Assets/Scripts/BlazBlue/Core/BBMotionMatcher.cs  (MDI.BlazBlue.Core, namespace DemonIdol.BlazBlue, uses MDI.Akio.MotionEdge)
public static class BBMotionMatcher
{
    public static bool Is41236(MotionEdge[] e, int n, int now, int facing, int window);   // 4 -> (1|2) -> (2|3) -> 6
    public static bool Is63214(MotionEdge[] e, int n, int now, int facing, int window);   // 6 -> (3|2) -> (2|1) -> 4
    public static bool Is236236(MotionEdge[] e, int n, int now, int facing, int window);  // two 236 shapes in order
    public static bool Is214214(MotionEdge[] e, int n, int now, int facing, int window);
    public static bool Is632146(MotionEdge[] e, int n, int now, int facing, int window);  // 6 3 2 1 4 6 (diagonals optional)
    public static bool Is360(MotionEdge[] e, int n, int now, int facing, int window);     // 4 cardinals in a rotation, either direction
    public static bool Is22(MotionEdge[] e, int n, int now, int facing, int window);      // 2, 5, 2 (diagonals 1/3 count as 2)
    public static bool IsChargeDownUp(MotionEdge[] e, int n, int now, int facing, int hold, int release); // [2]8, 7/8/9 count as up
    public static bool IsChargeBackForward(MotionEdge[] e, int n, int now, int facing, int hold, int release)
        => MDI.Akio.MotionMatcher.IsChargeBackForward(e, n, now, facing, hold, release);
}
```

Edge source: copy the private `CommandParser.CopyEdges()` logic into NEW `BBInputHistory.CopyEdges(MotionEdge[] dst)`, which reads the 64-tick ring. Do not change `CommandParser`.

Shortcut rules (BlazBlue standard, tunable):
- 623 accepts 6-2-3 and 6-3-6 shapes. EXISTING `Is623` already accepts 6,2,6. Add 3-2-3 as NEW, tunable `allow323Shortcut = true`.
- 236 must end on 6, 3 or 9 as the last non-neutral direction (EXISTING rule).
- 360: the last direction may be an up direction. When it is, the fighter must **not** leave the ground; jump startup is cancelled into the 360 if it matches within `jumpPrejumpTicks`.

### 3.8 Priority and disambiguation (`BBCommandInterpreter`)

On each tick, look at the newest press (or a negative-edge release) inside `bufferTicks` and test in this order. The first match that the current state can legally perform wins.

1. **System macros:** Burst/Overdrive (ABCD), then Rapid Cancel (ABC, only during an attack), then Counter Assault (6AB in blockstun), then Barrier (AB held while guarding), then Crush Trigger (AB in neutral), then Throw (BC).
2. **Astral Heat** (only if its conditions in 4.11 are met).
3. **Distortion Drives:** 632146, then 236236 / 214214.
4. **Specials, most complex motion first:** 360, then 632146-less half circles 41236 / 63214, then 623, then 236 / 214, then 22, then charge [4]6 / [2]8.
5. **Command normals:** 6A/6B/6C/6D, 3C, 4B (per character).
6. **Normals:** 5X / 2X / j.X.

Tie-breaks:
- **623 versus 236:** if both match, take 623 only when the earliest qualifying forward-6 edge comes before the down edge. This is what `Is623` stage 0 does.
- **Throw (BC) versus a B or C normal:** if B and C land within `chordWindowTicks`, it is a throw. A lone B followed by C 4+ ticks later is a gatling.
- **Barrier versus Crush Trigger:** if the fighter is guarding (holding 4/1 with an attack in range or a projectile incoming, or in blockstun), A+B is Barrier. Otherwise it is Crush Trigger.
- **Burst versus Overdrive:** in hitstun or blockstun it is Burst. In neutral with a full Burst gauge, Overdrive is the default and Gold Burst is not used (Central Fiction style). Make this tunable with `neutralABCD = Overdrive`.
- A motion is **consumed** only when its move actually starts, using the same pattern as Akio's `consume: false` probe followed by `consume: true`.

### 3.9 Stylish / simple mode (optional, ships off by default)
- Reuse `SettingsPanel.SimpleSpecials` (EXISTING bool) as the switch.
- Stylish mapping: D plus a direction gives specials (5D, 6D, 2D, 4D map to that character's four core specials). Mashing A runs an auto-combo, and the existing Classic auto-combo idea is used as the template. Holding D while pressing C gives the Distortion Drive (needs 50 Heat).
- Stylish damage: ×0.80 to all damage dealt (`BBSystemTuning.stylishDamageScale`). Ranked or online would force it off.
- In BlazBlue 4-button mode the D button can't double as a stylish button, so stylish turns D into a modifier. Each character's real Drive moves move to **A+D**, which is shown on the touch layout.

### 3.10 Input determinism rules
- The sim reads **only** `BBInputFrame`, never `Input`, `Keyboard.current` or touches.
- `BBInputSampler` latches presses that happen between ticks. A press and release inside one render frame still produces `pressed` on the next tick.
- During hitstop, superflash and superfreeze, **ages freeze** (same as Classic `InputBuf.FreezeAge(1)`), so buffered motions survive hitstop. This is the BlazBlue "buffer through hitstop" behaviour.

---

## 4. Core BlazBlue systems

All numbers live in **one** NEW ScriptableObject, `Assets/CombatData/BlazBlue/BBSystemTuning.asset` (class `DemonIdol.BlazBlue.BBSystemTuning`). The values below are the MDI defaults. They are inspired by BlazBlue: Central Fiction but **tuned for this game**, not copied frame-for-frame. Change them only through that asset.

Gauge units: Heat 0–100 (shown as 100%), Barrier 0–1000, Burst 0–1000 (one full bar = 1000).

### 4.1 Revolver Action (gatlings)
- The standard chain is **A → B → C → D**. A normal can cancel into a stronger normal of the same family (ground into ground, air into air) on **hit or block**, inside its cancel window.
- Same-strength rule: 5A and 2A can chain into **themselves or each other** up to `maxARepeats = 3` total. B and C cannot repeat.
- **Each normal can be used once per chain** (except the A-repeat rule). Track a 64-bit `usedMask` per chain (NEW in `BBChainState`).
- Command normals (6A, 6B, 6C, 3C) are in the chain only if the character's `BBAttackData.gatlingInto` lists them.
- A normal can always cancel into a special, a Distortion Drive or an Astral if its `specialCancel` flag is true (the default true for all A/B/C normals except overheads and 3C).
- **Jump cancel:** normals flagged `jumpCancelOnHit` (usually 5B or the launcher) can cancel into a jump or super jump on hit. This replaces the Classic "2S launcher jump-cancel" idea for BlazBlue mode.
- Whiff cancels: only A normals into A normals (BlazBlue standard). Tunable with `allowWhiffACancels = true`.
- The window is the move's authored `cancelWindow` (the EXISTING field), counted from **contact tick + 1** to `cancelWindow.y`, as Akio does today. There is a hard minimum of 6 ticks after contact, so hitstop doesn't swallow it.

### 4.2 Movement

| Action | Input | Default |
|---|---|---|
| Walk | 4 / 6 | Read per character from `CharacterDefinition.speed`, the same formula as Classic (`speed/50` clamped 0.7–1.4) × 3.6 forward / 2.8 back |
| Dash | 66 | Two dash types per character (`BBCharacterProfile.dashType`): **Run** (hold 6 to keep running, minimum 8 ticks, skids 6 ticks) or **Step** (fixed 14 ticks). Akio runs; Aika and PicoChan step; UnityChan hops (a step with an airborne frame) |
| Backdash | 44 | Invulnerable ticks 1–6, airborne ticks 4–14 (can't be thrown), 22 total |
| Jump | 7 / 8 / 9 | 4 prejump ticks (throw-invulnerable after tick 1), apex at ~22 ticks, lands in 3-tick landing recovery |
| Super jump | 2 then 7/8/9 within 10 ticks | ×1.25 jump height, no double jump after it |
| Double jump | 7/8/9 in the air | `airActions = 1` default (UnityChan 2) |
| Air dash | 66 / 44 in the air | Forward 16 ticks with no gravity; back 12 ticks. Not before 8 ticks after takeoff (`airDashMinHeightTicks = 8`). Uses up an air action |
| Landing | — | 3 ticks after a normal jump. On an air normal that whiffs, the move's `landingRecovery` (default 4) |

Air action count resets on landing and on a jump cancel.

### 4.3 Heat gauge

| Event | Heat change |
|---|---|
| Hit with an A / B / C / D normal | +2 / +3 / +4 / +3 |
| Hit with a special | +5 |
| Normal or special blocked by the opponent (attacker) | half of the on-hit gain |
| Taking damage (defender) | + damage × 0.03 (≈ +6 from a 200-damage combo) |
| Instant Block (guarding within 8 ticks before a hit lands) | +5 for the defender |
| Low-health regen | When HP is at or below 35%, +0.05 per tick (+3 per second) |
| Distortion Drive or Rapid Cancel hit | 0 (no gain while spending) |
| Round start | 0 (carries over only in arcade, if `heatCarryOver` is true; default false) |

| Cost | Heat |
|---|---|
| Rapid Cancel | 50 |
| Distortion Drive | 50 |
| Counter Assault | 50 |
| Crush Trigger | 25 |
| Astral Heat | 100 |
| Exceed Accel (during Overdrive) | 0, it ends Overdrive instead |

Classic `currentSuperMeter` (0–100) is **not** reused for Heat. Add a NEW `BBGauges` component with its own `heat` field. This keeps Classic meter logic (`RapidCancel`, `BarrierBlockSystem` drain, `DomainExpansion`) untouched.

### 4.4 Rapid Cancel
- Input: A+B+C during an attack.
- Legal when the current move has `canRapidCancel` (EXISTING field) and **made contact** (hit or block), from contact tick to the end of the move's active frames + 4. A whiff RC is off by default (`allowWhiffRC = false`).
- Effect: the fighter returns to neutral (in the air if she was airborne) and keeps her momentum. There is a 20-tick slowdown of the opponent at 0.75 speed (`rcSlowTicks`, `rcSlowFactor`), done by skipping every 4th opponent tick (deterministic, not `Time.timeScale`). Rapid Cancel flashes the screen and adds `FighterCamera2D.Instance.AddTrauma(0.25f)` (EXISTING call).
- A Rapid Cancel can't be done during a Distortion Drive or Astral, and only once per combo (`rcPerCombo = 1`).
- Implement this as NEW `BBRapidCancel` and **do not edit** `RapidCancel.cs`.

### 4.5 Barrier and the Barrier gauge
- Input: hold A+B while guarding. It works on the ground and in the air (air guard is allowed against everything except ground-only moves flagged `airUnblockable`).
- **Barrier gauge** 0–1000 starts full each round. Holding barrier costs −2 per tick. Each blocked hit costs −(damage × 0.5). Regen is +3 per tick after 30 ticks of not barriering.
- Barrier effects: **no chip damage** and pushback ×2.2 (reuse `BarrierBlockSystem.BARRIER_PUSHBACK_MULTIPLIER` as the default number). Barrier can block air-unblockable moves.
- **Instant Barrier** (barrier pressed within 8 ticks before a hit): blockstun −2 ticks, pushback ×2.6, a white flash, and Heat +3.
- **Danger:** when the gauge hits 0, the fighter is in Danger for 300 ticks or until the gauge refills to 300. In Danger she takes ×1.25 damage, can't barrier, and the HUD gauge flashes red.
- Normal guard on its own takes chip and costs no barrier.
- Implement as NEW `BBBarrier` (the Classic `BarrierBlockSystem` stays on the GameObject but disabled in BlazBlue mode).

### 4.6 Burst
- **Burst gauge** 0–1000. Each round starts at 1000 if the player lost the previous round, otherwise at the carried-over value (`burstStartRule`). Regen is +1 per tick (full in ~17 s), and there is no regen during Overdrive.
- Input: A+B+C+D.
- **Defensive Burst** (in hitstun or blockstun): a shockwave that is invulnerable from tick 1 to 26 and launches on hit. Cost is a full gauge (1000). If the Burst is guarded, it still pushes away. Hitbox radius is 1.8 m (`PsychicBurstSystem.BURST_RADIUS = 3.5` is Classic; BlazBlue mode uses its own value).
- **Neutral ABCD** goes to Overdrive (4.7) by default.
- Burst can't be used during throws, Distortion Drive super-freeze or Astral.
- A Burst that is **baited** (blocked or whiffed) gives the opponent Heat +25.
- Implement as NEW `BBBurst`. It may call the EXISTING `FighterState.BurstActive` and `HitResult.BurstBreak` values.

### 4.7 Overdrive and Exceed Accel
- Input: A+B+C+D in neutral with a full Burst gauge. Cost is the full Burst gauge.
- There is a 20-tick super-flash (`CombatTickEngine.Instance.TriggerSuperfreeze(20)`, EXISTING). Ticks 1–20 are invulnerable, and the start knocks the opponent back with no damage.
- Duration depends on remaining HP: `ticks = lerp(480, 180, hpFraction)`, so lower HP gives a longer Overdrive. The round timer pauses while it is active.
- Effects: each character's **Drive gets its OD version** (defined per fighter in section 8). Heat also regenerates +0.1 per tick, and the low-health regen rule still applies.
- **Exceed Accel:** press A+B+C (or the Distortion Drive input) during Overdrive. The fighter does her OD finisher (a super-like move), and Overdrive ends. Exceed Accel can be cancelled into from normals and specials on hit.
- Overdrive can be **started during a combo** (Active Overdrive) from any normal or special on hit. Doing so costs Overdrive duration ×0.6 (`activeODPenalty`).

### 4.8 Crush Trigger and guard crush
- Input: A+B in neutral (not guarding). Cost is 25 Heat.
- Startup is 30 ticks, and A+B can be held to charge up to 60. It has armor against 1 hit during ticks 1–20 (`crushArmorTicks`).
- **On block it is a guard crush:** the defender's barrier gauge takes −300, and she is put into **guard crush stun** of 40 ticks, which is comboable (`HitResult.Crushed` exists; use the NEW state `BBState.GuardCrush`).
- On hit: 15% proration starter (P1 0.6), wall stick near the corner.
- Guard crush also happens if the Barrier gauge empties *while barriering* (a 30-tick crush).

### 4.9 Counter Assault
- Input: 6+A+B during blockstun (ground). Cost is 50 Heat.
- Invulnerable ticks 1–15. It knocks the opponent back and is non-lethal: it can't KO and does 0 damage on block. Damage is 10% of the user's standard 5C, and minimum damage doesn't apply.
- If the opponent blocks the Counter Assault, the attacker gets +10 frame advantage (so it is not free).
- One Counter Assault per blockstring. The window opens on the 2nd tick of blockstun.

### 4.10 Distortion Drives
- Two or more per character, at 50 Heat. Inputs come from section 8 (usually 236236X, 214214X, 632146X).
- There is a super-flash of 20 ticks (`TriggerSuperfreeze`). The input buffer freezes, and the opponent can block during the flash only if she was holding guard before it.
- Invulnerability is per move (see `BBAttackData.invuln`).
- Proration: `minDamage` of 25–35% is guaranteed even late in a combo.
- They can be cancelled into from any special on hit (if the special's `ddCancel` flag is true) and from normals.
- Hook for the cinematic: the EXISTING `FighterController.OnDomainTriggered` event and `FighterCamera2D.Instance.TriggerUltimateEntry/Exit`. Cutscene MP4s are **not** used; see 9.7.

### 4.11 Astral Heat (optional, `enableAstral` in tuning, default on for arcade/versus)
- Conditions: the user is at **match point** (one round from winning), has **100 Heat**, and the opponent is at **≤ 35% HP**.
- On hit it ends the round as a KO with a special banner. The NEW `MatchRoundReferee` hook records a "special finish". It leaves the referee file alone apart from one event subscription (section 11.2).
- Each character gets 1 (inputs in section 8). If Izno wants it off, set `enableAstral = false` and the moves are removed from the command list.
- The EXISTING `DomainExpansion` (100 cost, 20 freeze) is the presentation template.

### 4.12 Throws and throw tech

| Throw | Input | Range | Startup | Notes |
|---|---|---|---|---|
| Forward throw | 6BC or 5BC | 1.0 m (character tunable) | 7 | Can't be used on a target in hitstun or blockstun, or within 6 ticks after blockstun or wakeup (EXISTING `RecoveryProtectionTicks` idea) |
| Back throw | 4BC | same | 7 | Side switch |
| Air throw | j.BC | 1.0 m box, both airborne | 7 | Throw-invulnerable after an air tech for 6 ticks |
| Command throw | per character | per character | per character | Can't be teched (`throwTechable = false`) unless authored |

- **Throw tech:** press BC (or A+B+C+D's BC part) within **13 ticks** (`throwTechTicks`) of the grab connecting. Both fighters are pushed back to 1.6 m, and the tecched fighter is +0.
- A **Counter Hit throw** (grabbing during the opponent's startup) narrows the tech window to 6 ticks.
- `SettingsPanel.AutoTechThrows` (EXISTING) can make the tech automatic in training or stylish mode only.
- A whiffed throw has 24 ticks of recovery and a fixed animation.
- Reuse the EXISTING tech state machine idea in `FighterController.UpdateThrowBreakState`. BlazBlue mode has its own NEW `BBThrowResolver`, because the Classic window is 12 ticks and it consumes `InputButton.Burst`.

### 4.13 Counter Hit and Fatal Counter
- **Counter Hit:** a move that hits a target in her startup or active ticks (EXISTING `IsCounterHitVulnerable` covers startup and active; add recovery for BlazBlue). It gives +50% hitstun on the first hit, untechable time ×1.5, hitstop +4 (`counterHitStopBonus`, EXISTING field), damage ×1.1 and a red "COUNTER" banner.
- **Fatal Counter:** moves flagged `fatalCounter = true` (usually 1–3 per character, such as a big poke or a reversal punish) that land as Counter Hits become a Fatal Counter. Effects: **gravity ×0.8 for the whole combo**, untechable time +10, the combo's hitstun decay starts 120 ticks later, and a gold "FATAL COUNTER" banner. P2 proration is not applied to the second hit.

### 4.14 Damage, proration and minimum damage

Per hit:

```
scaledDamage = baseDamage
             × P1 (first hit of combo only applies its own P1 to the WHOLE combo)
             × product(P2 of every previous hit in this combo, including same-move repeats)
             × sameMovePenalty^(usesOfThisMoveInCombo - 1)      // default 0.80
             × (defender in Danger ? 1.25 : 1)
             × (stylish ? 0.80 : 1)
             × guts(defender HP)                                  // see table
finalDamage  = max(scaledDamage, baseDamage × minDamagePct)       // move's guaranteed portion
finalDamage  = floor(finalDamage)   // integer HP to stay deterministic
```

| Guts (defender HP remaining) | Multiplier |
|---|---|
| > 50% | 1.00 |
| 30–50% | 0.90 |
| 20–30% | 0.80 |
| 10–20% | 0.70 |
| < 10% | 0.60 |

Default proration per strength (override per move):

| Move kind | P1 (starter) | P2 (per hit) | minDamagePct |
|---|---|---|---|
| A normal | 0.70 | 0.85 | 0.05 |
| B normal | 0.80 | 0.89 | 0.08 |
| C normal | 0.90 | 0.92 | 0.10 |
| D normal | 0.85 | 0.90 | 0.08 |
| Throw | 0.60 | 1.00 | 0.00 |
| Special | 0.90 | 0.90 | 0.12 |
| Distortion Drive | 1.00 | 0.95 | 0.30 |
| Astral | — | — | 1.00 (instant KO) |

Mapping to existing fields: EXISTING `AttackData.initialProration` = **P1**, and EXISTING `AttackData.comboDamageDecay` = **P2**. BlazBlue adds only `minDamagePct` and `fatalCounter` (NEW, on `BBAttackData`). The Classic `JuggleEngine.GetDamageScale` table is untouched and isn't used in BlazBlue mode.

HP: Classic `maxHealth = 1000`. BlazBlue defaults to **11,000 HP** shown as a bar, with integer HP (`BBCharacterProfile.maxHp`, per character 10,000–12,500). Scale move damages in the asset; don't multiply at runtime.

### 4.15 Hitstun decay (combo time)

The combo timer starts on the first hit and counts ticks the defender spends in hit or juggle states.

| Combo time (ticks) | Ground hitstun | Untechable time (air) |
|---|---|---|
| 0–180 | 100% | 100% |
| 181–300 | −2 ticks | −6 |
| 301–420 | −4 ticks | −12 |
| 421–540 | −6 ticks | −18 |
| 541+ | −8 ticks (minimum 6) | −24 (minimum 6) |

A Fatal Counter shifts every threshold by +120.

### 4.16 Gravity scaling
- Juggle gravity is `g = baseGravity × (1 + 0.06 × airHitsInCombo)`, capped at ×1.9 (`gravityScalePerHit = 0.06`, `gravityCap = 1.9`). BlazBlue mode doesn't reuse the Classic `GRAVITY_SCALING_PER_HIT = 0.15`.
- Per-move `launchVelocity` (EXISTING field) sets the launch. BlazBlue adds NEW `airHitVelocity` for hits on an already-airborne target.

### 4.17 Wall and ground bounce, wall stick, slide
- New fields on the move: `groundBounce`, `wallBounce`, `wallStick` (the EXISTING `causesWallBounce` and `causesWallSplat` are mapped to these) and `groundSlide`.
- **Each can happen once per combo** (EXISTING `JuggleEngine.TryRegisterWallInteraction` is the Classic version; BlazBlue uses the NEW `BBComboState.usedBounceMask`).
- The wall is at `PushbackSolver.WALL_COLLISION_THRESHOLD` (9.8, EXISTING) unless the stage overrides it (`PushbackSolver.SetArenaBounds`, EXISTING).
- A second bounce in the same combo becomes a normal knockdown.

### 4.18 Knockdown, wakeup and tech

| Situation | Rule |
|---|---|
| Air tech | Press any of A/B/C/D after **untechable time** ends to recover in the air (neutral, forward or back). Invulnerable 6 ticks, and can't be air-thrown for 6 more |
| Ground tech | On landing from a techable knockdown, press a button within 12 ticks to roll back, roll forward or quick-rise. 20 ticks of invulnerability |
| Hard knockdown | Moves flagged `hardKnockdown` give 40 ticks on the ground with no tech |
| Wakeup | After lying down (24 ticks, = EXISTING `OkizemeWakeup.GROUNDED_REST_TICKS`), the fighter stands up with 16 invulnerable ticks. Strike-invulnerable reversals can be buffered |
| OTG | Moves flagged `otg` can hit a downed target once per combo. 0.5 damage, and the target then gets a hard knockdown |

These live in NEW `BBTech`. Classic `OkizemeWakeup` stays for Classic.

### 4.19 Chip damage
- Specials, Distortion Drives and Drive moves deal chip on normal guard: `chip = damage × chipPct` (defaults: special 0.10, DD 0.15, Drive 0.08). Normals deal 0. The EXISTING field `chipDamageOnBlock` holds an authored override.
- Barrier blocks all chip.
- Chip can KO only with a Distortion Drive or Astral (`chipCanKO = DDOnly`).

### 4.20 Negative penalty (optional, off by default)
For running away or holding back for 6 seconds without attacking: the fighter takes ×1.2 damage for 10 seconds. Not needed for launch. It is listed so the coder can add the flag without redesigning later.

### 4.21 System data shape

```csharp
// NEW: Assets/Scripts/BlazBlue/Core/BBSystemTuning.cs
[CreateAssetMenu(menuName = "DemonIdol/BlazBlue/System Tuning")]
public class BBSystemTuning : ScriptableObject
{
    [Header("Input")] public int bufferTicks = 5, chordWindowTicks = 3, negativeEdgeTicks = 3;
    public int motionWindow236 = 22, motionWindowHalfCircle = 30, motionWindow360 = 40, motionWindowDD = 36, motionWindow22 = 16;
    public int chargeHoldTicks = 40, chargeReleaseTicks = 12; public float stickDeadzone = 0.20f; public bool allow323Shortcut = true;
    [Header("Heat")] public int heatMax = 100, costRC = 50, costDD = 50, costCA = 50, costCT = 25, costAstral = 100;
    public float heatOnHitA = 2, heatOnHitB = 3, heatOnHitC = 4, heatOnHitD = 3, heatOnHitSpecial = 5, heatPerDamageTaken = 0.03f;
    public float lowHpThreshold = 0.35f, lowHpRegenPerTick = 0.05f;
    [Header("Barrier")] public int barrierMax = 1000, barrierHoldCost = 2, barrierRegen = 3, barrierRegenDelay = 30, dangerTicks = 300;
    public float barrierHitCostPerDamage = 0.5f, barrierPushback = 2.2f, instantBarrierPushback = 2.6f, dangerDamageScale = 1.25f;
    public int instantBlockWindow = 8;
    [Header("Burst / Overdrive")] public int burstMax = 1000, burstRegenPerTick = 1, burstInvulnTicks = 26; public float burstRadius = 1.8f;
    public int odMinTicks = 180, odMaxTicks = 480; public float activeODPenalty = 0.6f;
    [Header("Rapid Cancel")] public bool allowWhiffRC = false; public int rcSlowTicks = 20; public float rcSlowFactor = 0.75f; public int rcPerCombo = 1;
    [Header("Throw")] public int throwTechTicks = 13, throwTechTicksCH = 6, throwWhiffRecovery = 24, throwProtectTicks = 6; public float throwRange = 1.0f;
    [Header("Combo")] public float sameMovePenalty = 0.80f, gravityScalePerHit = 0.06f, gravityCap = 1.9f;
    public int[] hitstunDecayThresholds = { 180, 300, 420, 540 }; public int[] hitstunDecayGround = { 0, 2, 4, 6, 8 }; public int[] hitstunDecayAir = { 0, 6, 12, 18, 24 };
    public int fatalCounterDecayShift = 120; public float fatalCounterGravity = 0.8f; public float counterHitHitstunScale = 1.5f;
    [Header("Optional")] public bool enableAstral = true; public bool negativePenalty = false; public float stylishDamageScale = 0.8f;
}
```

---

## 5. Frame data model

### 5.1 Conventions (unchanged from `AttackData`)
- `startupFrames` = the ticks **before** the first active tick. EXISTING convention: active starts at tick S. In BlazBlue notation, "startup 7" means the first active tick is 7. Pick one convention for display. Store as now, and in `FrameDataDisplay` show `startup = startupFrames + 1` labelled "BB style". Document this in the HUD tooltip.
- `activeFrames`, `recoveryFrames`, `hitstunTicks`, `blockstunTicks` and `hitstop*` are all in 60 Hz ticks.
- Advantage: EXISTING `CalculateHitAdvantage()` = `hitstunTicks − ((activeFrames − 1) + recoveryFrames)`, and the block version is the same. The `advantageOnHit/Block/CounterHit` fields are **authored display values**. A validator (section 10) must assert that they equal the calculated values within ±1. A mismatch fails CI.

### 5.2 Invulnerability and properties

```csharp
// NEW: Assets/Scripts/BlazBlue/Core/BBEnums.cs  (in MDI.BlazBlue.Core so collision structs can use it)
[System.Flags] public enum BBInvuln : ushort
{
    None = 0, Strike = 1, Throw = 2, Projectile = 4, Head = 8 /*upper body (BB "head attribute")*/, Body = 16,
    Foot = 32 /*low/foot attribute*/, Armor1 = 64, ArmorMulti = 128, Guard = 256 /*guard point: counters on contact*/
}
[System.Flags] public enum BBAttribute : byte { None = 0, Head = 1, Body = 2, Foot = 4, Projectile = 8, Throw = 16 } // which invuln beats it
public enum BBGuard : byte { Mid, Low, Overhead, Unblockable, AirUnblockable, Throw }
public struct BBInvulnWindow { public int start, end; public BBInvuln mask; }   // ticks from move start
```

- BlazBlue attribute system: every hit has an `attribute` (H/B/F/P/T). A move's invulnerability masks the attributes it ignores. For example, a dragon punch is `Head|Body` invulnerable ticks 1–8, so it beats air (Head) attacks and mid-body strikes but loses to Foot (low) hits.
- Mapping from EXISTING: `GuardRequirement.Normal/Low/Overhead/Throw` → `BBGuard.Mid/Low/Overhead/Throw`. `isAirborneStrikeProtected` → `BBInvuln.Head` window. `akioStrikeInvuln` → `BBInvuln.Strike` window. `hasArmor` → `Armor1`.

### 5.3 NEW `BBAttackData` (extends the EXISTING `AttackData`)

Subclassing keeps the EXISTING hit path (`ReceiveIncomingStrike(FighterController, AttackData, Hurtbox)`, `FrameDataDisplay` and `HitboxVisualizer`) type-compatible, and `AttackData.cs` is not edited.

```csharp
// NEW: Assets/Scripts/BlazBlue/Data/BBAttackData.cs
using UnityEngine; using DemonIdol.Combat;
namespace DemonIdol.BlazBlue
{
    [CreateAssetMenu(fileName = "BB_Move", menuName = "DemonIdol/BlazBlue/Attack Data")]
    public class BBAttackData : AttackData
    {
        [Header("BB Identity")]
        public string moveId = "5A";              // unique per character: 5A 2A j.A 5B ... 236A 632146C AH
        public BBButton button = BBButton.A;
        public BBMoveKind kind = BBMoveKind.Normal; // Normal, CommandNormal, Special, DriveMove, DistortionDrive, AstralHeat, Throw, CommandThrow, System
        public BBMotion motion = BBMotion.None;     // None, M236, M214, M623, M421, M41236, M63214, M236236, M214214, M632146, M360, M720, M22, Charge46, Charge28
        public bool airOk = false, groundOk = true;

        [Header("BB Guard / Attribute / Invuln")]
        public BBGuard guard = BBGuard.Mid;
        public BBAttribute attribute = BBAttribute.Body;
        public BBInvulnWindow[] invuln = new BBInvulnWindow[0];
        public bool fatalCounter = false;
        public bool hardKnockdown = false, otg = false, groundBounce = false, wallBounce = false, wallStick = false, groundSlide = false;
        public int untechableTicks = 18;            // air hit
        public int untechableTicksCH = 27;
        public Vector2 airHitVelocity = new Vector2(1.5f, 5.0f);
        public int landingRecovery = 4;

        [Header("BB Damage")]
        [Range(0f, 1f)] public float minDamagePct = 0.08f;   // P1 = initialProration, P2 = comboDamageDecay (inherited)
        public float chipPct = 0f;
        public int heatGainOverride = -1;          // -1 = use BBSystemTuning default for kind/button
        public int heatCost = 0;                   // 50 for DD, 100 for AH

        [Header("BB Cancels")]
        public string[] gatlingInto = new string[0];      // moveIds (Revolver Action)
        public bool specialCancel = true, ddCancel = true, jumpCancelOnHit = false, jumpCancelOnBlock = false;
        public bool rapidCancelable = true;               // inherits canRapidCancel default; this is the BB-mode switch
        public string[] followUps = new string[0];        // rekka/stance follow-up moveIds
        public Vector2Int followUpWindow = new Vector2Int(0, 0);

        [Header("BB Boxes (per tick, baked)")]
        public BBBoxTrack boxes;                    // see section 6
        public BBMotionTrack motionTrack;           // per-tick root displacement (deterministic movement)

        [Header("BB Presentation")]
        public string clipName = "";                // e.g. "Akio_BB_5A" (meta clip name), or reuse "Akio_5P"
        public AnimationClip clip;                  // the sub-clip asset (fileID inside the FBX)
        public int clipFirstFrame, clipLastFrame;   // 24 fps source window, for validators
        public BBEvent[] events = new BBEvent[0];   // SFX/VFX/camera/projectile spawn at tick N (data, not AnimationEvents)
    }

    public enum BBMoveKind : byte { Normal, CommandNormal, Special, DriveMove, DistortionDrive, AstralHeat, Throw, CommandThrow, System }
    public enum BBMotion : byte { None, M236, M214, M623, M421, M41236, M63214, M236236, M214214, M632146, M360, M720, M22, Charge46, Charge28 }

    [System.Serializable] public struct BBEvent { public int tick; public BBEventType type; public string key; public Vector3 offset; }
    public enum BBEventType : byte { Sfx, Vfx, CameraShake, CameraZoom, SpawnProjectile, Voice, Hitstop, SuperFlash }
}
```

### 5.4 NEW `BBMoveSet` and character profile

```csharp
// NEW: Assets/Scripts/BlazBlue/Data/BBMoveSet.cs
[CreateAssetMenu(menuName = "DemonIdol/BlazBlue/Move Set")]
public class BBMoveSet : ScriptableObject
{
    public string characterId = "C01";
    public BBAttackData[] moves;                 // all moves; looked up by moveId
    // Fast lookup tables built in OnEnable into arrays (no Dictionary in hot path):
    [System.NonSerialized] public BBAttackData[] groundNormals; // index = (button,stance) e.g. 5A,2A,5B,2B,5C,2C,5D,2D
    [System.NonSerialized] public BBAttackData[] airNormals;    // j.A j.B j.C j.D
    [System.NonSerialized] public BBAttackData[] commandNormals;// 6A 6B 6C 3C 4B 6D...
    [System.NonSerialized] public BBAttackData[] specialsByPriority; // sorted by motion complexity (section 3.8)
    public BBAttackData throwForward, throwBack, throwAir;
    public BBAttackData counterAssault, crushTrigger, burst, overdriveStart, exceedAccel, astralHeat;
    public BBAttackData Find(string moveId);     // linear scan at load only; runtime uses cached indices
}

// NEW: Assets/Scripts/BlazBlue/Data/BBCharacterProfile.cs
[CreateAssetMenu(menuName = "DemonIdol/BlazBlue/Character Profile")]
public class BBCharacterProfile : ScriptableObject
{
    public string characterId;                   // matches CharacterDefinition.id (C01...)
    public BBMoveSet moveSet;
    public int maxHp = 11000;
    public BBDashType dashType = BBDashType.Run;  // Run, Step, Hop
    public int airActions = 1;
    public float walkF = 3.6f, walkB = 2.8f, jumpV = 8.0f, jumpH = 3.2f, gravity = 20f;
    public BBBodyBoxes body;                     // stand/crouch/air hurt+push boxes (section 6.4)
    public RuntimeAnimatorController bbAnimator; // NEW <Name>_BB_Override.overrideController
    public string driveName = "Drive";           // HUD label for D
    public DriveResourceKind driveResource;      // None, Stocks, Gauge, Stance, Install (section 8)
    public int driveResourceMax;
}
```

**CharacterDefinition link (the only edit to that file):** add two NEW fields at the end of `CharacterDefinition.cs`:

```csharp
[Header("BlazBlue 4-button (NEW, ignored in Classic)")]
public DemonIdol.BlazBlue.BBCharacterProfile blazBlueProfile;   // null = not available in BB mode
public bool BlazBlueReady => blazBlueProfile != null && blazBlueProfile.moveSet != null;
```

Adding serialized fields with a default of null leaves the existing assets valid (Unity fills missing fields with defaults), and `UsesDepthMovelist` is untouched. The Classic validator's `ALLOWED_MODIFIED` already lists `CharacterDefinition.cs`. The BlazBlue validator must assert that the only diff in that file is the appended block.

### 5.5 Per-move frame data table format (what the coder authors for each move)

| Field | Example (Akio 5A) |
|---|---|
| moveId | 5A |
| startup / active / recovery | 6 / 3 / 8 (same ticks as Akio 5P, so the same clip is reused) |
| hitstun / blockstun / hitstop | 14 / 9 / 8 |
| on hit / on block / on CH | +4 / −1 / +8 (validated) |
| guard / attribute | Mid / Body |
| damage / P1 / P2 / min% | 300 / 0.70 / 0.85 / 0.05 |
| cancels | gatlingInto [5A, 2A, 5B, 2B, 5C, 2C, 6A], specialCancel, rapidCancelable |
| invuln | none |
| boxes | hit: RightHand-follow 0.28×0.22 m @ +0.12 m, ticks 6–8 (baked) |
| clip | `Akio_5P` (144_20 frames 110–122), speed from section 7.6 |

### 5.6 P1/P2 sides
"P1/P2" means two different things. Don't confuse them in code:
- **Proration P1/P2:** the starter scaling and the per-hit scaling (section 4.14).
- **Player 1 / Player 2:** facing and side. Side switching uses the EXISTING `SetFacing` / `UpdateFacingDirection`. In BlazBlue mode, facing latches only in neutral **and** when landing from a jump (cross-ups are allowed), so facing isn't updated mid-air.

---

## 6. Hitbox, hurtbox and pushbox efficiency

### 6.1 What exists and why BlazBlue mode replaces it
- Classic uses one `Hitbox` trigger BoxCollider per fighter (`FighterController.activeHitbox`), several `Hurtbox` trigger colliders with `BodyZone { Head, Torso, Legs }`, and a `CharacterController` capsule as the pushbox. Resizing happens through `AkioBodyFit`.
- Hits are found by `OnTriggerEnter`. That runs on the **50 Hz physics step** (Fixed Timestep 0.02), while the combat sim runs at **60 Hz**. A 1-tick active move can be missed or doubled, the result depends on frame timing, and it can't be rolled back.
- Bone-follow (`Hitbox.SnapToBone` in `LateUpdate`) reads the *rendered* pose, which changes with render frame rate and Animator timing, so it isn't deterministic.

**Decision:** in BlazBlue mode, collision is **physics-free**. It uses 2D axis-aligned boxes (AABB) on the Z = 0 fight plane, checked inside the combat tick in a fixed order. Box positions come from **baked per-tick data**, not live bones. Classic keeps its colliders and isn't touched. In BlazBlue mode the Classic `Hitbox` and `Hurtbox` colliders are disabled on that fighter only, by a NEW `BBFighterBrain.OnEnable` that remembers and restores the original `enabled` states.

### 6.2 Box kinds

| Kind | Purpose | Count per fighter per tick (max) |
|---|---|---|
| Hurt | Can be hit | 6 |
| Hit | Deals damage | 4 |
| Push | Body collision, keeps the fighters apart | 1 |
| Throw | Throw range check (only during the throw's active ticks) | 1 |
| ThrowHurt | Can be thrown (usually the standing body, absent while airborne or in startup invulnerability) | 1 |
| Projectile | Owned by projectile entities | 2 per projectile |
| Guard proximity | Triggers proximity guard (holding back blocks instead of walking) | 1 |

### 6.3 Data layout (baked, per move, per tick)

```csharp
// NEW: Assets/Scripts/BlazBlue/Core/BBBox.cs  (MDI.BlazBlue.Core)
public enum BBBoxKind : byte { Hurt, Hit, Push, Throw, ThrowHurt, GuardProximity }

[System.Serializable] public struct BBBox             // 20 bytes, fighter-local, facing-right space, metres
{
    public float cx, cy, hx, hy;                       // centre + half extents on the X/Y fight plane
    public BBBoxKind kind; public byte hitIndex;       // hitIndex groups multi-hit boxes (each index can hit once)
    public BBAttribute attribute;                      // H/B/F per hurtbox (Head/Body/Foot) for invuln tests
    public byte flags;                                 // bit0 = "disjoint" (no hurtbox with it), bit1 = "counter-hit only"
}

[System.Serializable] public class BBBoxTrack          // one per move; also one for each body state (stand/crouch/air)
{
    public int tickCount;                              // = startup + active + recovery
    public int[] frameStart;                           // length tickCount+1 ; boxes of tick t are boxes[frameStart[t] .. frameStart[t+1])
    public BBBox[] boxes;                              // flat array, no per-tick allocations
    public int[] keyTicks;                             // authoring keys (editor only); runtime uses the baked arrays
}
```

- **Run-length compression:** consecutive ticks with the same boxes share one entry. In that case `frameStart[t]` points to the same range, so memory stays small.
- **Facing flip:** world X = `posX + facing * cx`. The data never stores the flipped copy.
- **Fixed point (optional, needed for online):** store `short` centimetres (cx, cy, hx, hy in cm) and convert to world int-cm space. Section 9.12 explains why ints are needed for cross-device determinism. Default to `float` offline, but write the collision code so it compiles against a NEW `BBFix` alias (`using BBReal = float;`) and can be swapped.

### 6.4 Body boxes per character (stand, crouch, air)

These come from the **measurements already in the repo**. Everything not listed is TBD and has to be measured with the NEW editor tool (6.10) on the avatar in T-pose and in the stand, crouch and jump poses.

| Fighter | Hip height (m) | Limbs (m) | Fist hitbox (m) | Foot hitbox (m) | Grab box (m) | Crouch push h/r | Air push h/r | Source |
|---|---|---|---|---|---|---|---|---|
| Akio C01 | ~0.95 | — | 0.28×0.22×0.26, +0.12 ahead of RightHand | 0.46–0.50×0.20–0.24×0.28–0.32, +0.16–0.18 ahead of RightFoot | 0.45×0.60×0.40, +0.35 at Chest | 0.55 / 0.22 | 1.10 / 0.22 | `AKIO_MOVELIST.md`, `Akio_*.asset`, `AkioBodyFit` consts |
| Aika C02 | ~0.92 | long shin | 0.24×0.20×0.22, +0.10 | 0.56×0.18×0.26, +0.22 | 0.40×0.55×0.36, +0.32 | 0.50 / 0.24 | 1.05 / 0.20 | `AIKA_MOVELIST.md`, `Aika_*.asset` |
| UnityChan C04 | ~0.87 | forearm ~0.16 | 0.20×0.16×0.18, +0.08 | 0.38×0.16×0.22, +0.14 | 0.36×0.48×0.32, +0.28 | 0.46 / 0.18 | 1.02 / 0.16 | `UNITYCHAN_MOVELIST.md`, `UChan_*.asset` |
| PicoChan C09 | 0.76 | thigh 0.28, shin 0.36, upper arm 0.18, forearm 0.21 | 0.26×0.18×0.20, +0.12 | 0.42×0.16×0.22, +0.16 | 0.42×0.50×0.36, +0.30 | 0.42 / 0.22 | 0.90 / 0.20 | `PICOCHAN_MOVELIST.md`, `Pico_*.asset` |
| Others | TBD | TBD | rule below | rule below | rule below | rule below | rule below | measure |

Akio's crouch hurtbox scales (from `AkioBodyFit` / `AKIO_MOVELIST.md`) are legs 0.55, torso 0.75 and head 0.35 of standing, with air at 0.85.

Crouch and air hurtbox scales on the definitions (`depthCrouchLegScale / TorsoScale / HeadScale / depthAirHurtScale`), as seeds for BlazBlue crouch and air body tracks:

| Fighter | Leg | Torso | Head | Air |
|---|---|---|---|---|
| Akio (AkioBodyFit constants) | 0.55 | 0.75 | 0.35 | 0.85 |
| Aika | 0.62 | 0.70 | 0.40 | 0.80 |
| UnityChan | 0.50 | 0.70 | 0.40 | 0.90 |
| PicoChan | 0.48 | 0.65 | 0.35 | 0.80 |

**Sizing rules derived from the four built fighters** (use these for new girls, then tune in training mode):
- Fist box length (X) ≈ **1.25 × forearm length**. PicoChan is 0.26 / 0.21 = 1.24 and UnityChan is 0.20 / 0.16 = 1.25. Height and depth are about 0.8 × length.
- Fist box forward offset ≈ **0.5 × forearm** (PicoChan 0.12 / 0.21, UnityChan 0.08 / 0.16).
- Foot box length ≈ **1.15–1.2 × shin** (PicoChan 0.42 / 0.36 = 1.17).
- Grab box ≈ 0.42 m deep × 0.55 × hip height tall, centred at Chest, offset about 0.30 m.
- Crouch pushbox height ≈ **0.55–0.58 × hip height** (PicoChan 0.42 / 0.76 = 0.55, UnityChan 0.46 / 0.87 = 0.53, Akio 0.55 / 0.95 = 0.58).
- Air pushbox height ≈ **1.16–1.18 × hip height** (Akio 1.10 / 0.95 = 1.16, PicoChan 0.90 / 0.76 = 1.18).
- Standing hurtboxes: Head = top 18% of height, Body = shoulders to hips, Foot = hips to floor. The **Foot hurtbox extends 0.05 m in front of the leading foot** (BlazBlue convention: lows should hit when the toes are near).
- During an attack's active ticks, add a hurtbox over the attacking limb that is **70% the size of the hitbox and sits 0.04 m behind its leading edge**. This gives every move a whiff-punish window. Disjoint moves (projectiles, some drive effects) set `flags.bit0` and skip this.
- On 2D the Z size of the existing 3D boxes is ignored.

### 6.5 Bone-follow versus authored offsets
- **Authoring** may use bone-follow: the editor tool samples the clip and reads `HumanBodyBones` positions, which keeps authoring fast and accurate.
- **Runtime** uses only baked boxes. The bake samples the move's `AnimationClip` with `AnimationClip.SampleAnimation(go, t)` on a hidden preview copy of the fighter's avatar, at `t = tick / 60 × clipRate` (the clip rate from 7.6). It writes the bone position relative to the root plus the authored offset.
- **Rebake** whenever the clip window, speed or avatar changes. Store a hash of `(clip guid, firstFrame, lastFrame, speed, avatar guid)` in the track. The validator fails CI if the hash is stale.
- The Classic `Hitbox.ApplyAuthored` bone-follow is left exactly as it is.

### 6.6 Deterministic tick order (BlazBlue mode)

Run once per `CombatTickEngine.OnTick`, by a single NEW `BBMatchSim` that owns both fighters, so fighters are **not** ticked by separate subscriptions in random order:

```
1. Read BBInputFrame for P1, P2 (from sampler or replay or network)
2. For each fighter in fixed order (P1 then P2): interpret commands, advance state timers, choose new move (no side-effects on the opponent yet)
3. Integrate movement (velocity from BBMotionTrack or physics constants), clamp to arena (PushbackSolver.ClampToArena)
4. Pushbox resolve (symmetric, see 6.7)
5. Build world-space box lists for this tick (from baked tracks + current tick index)
6. Throw checks (throw boxes vs throwHurt), both directions, resolve simultaneous throws as a throw tech
7. Strike checks: P1 hits vs P2 hurts, P2 hits vs P1 hurts; collect results FIRST, then apply both (trades are possible)
8. Projectile vs projectile (cancel by hit count), projectile vs hurt
9. Apply hits: hitstop, stun, damage, gauges, juggle, events (VFX/SFX/camera queued, not executed inline)
10. Facing latch, round checks (KO, time)
11. Compute state hash (training/replay/debug builds) — see 10.2
```

Presentation (Animator, VFX, camera) runs in `Update` / `LateUpdate` and **reads** sim state. It never writes it. Render interpolation uses `CombatTickEngine.InterpolationAlpha` (EXISTING).

### 6.7 Collision maths (physics-free)

```csharp
// NEW: Assets/Scripts/BlazBlue/Core/BBCollision.cs  (MDI.BlazBlue.Core, static, no allocations)
public static class BBCollision
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool Overlap(in BBWorldBox a, in BBWorldBox b)
        => a.minX < b.maxX && b.minX < a.maxX && a.minY < b.maxY && b.minY < a.maxY;   // strict: touching edges don't hit

    // Returns number of contacts written into 'results' (preallocated, capacity 16).
    public static int StrikePass(BBBoxBuffer attacker, BBBoxBuffer defender, BBHitMask alreadyHit, BBContact[] results);

    // Pushbox: overlap depth split 50/50; if one is cornered, the other takes 100%; in the air vs ground,
    // the airborne fighter is pushed (prevents corner cross-up glitches). Deterministic tie-break: P1 pushed left on exact centre tie.
    public static void ResolvePush(ref float p1x, ref float p2x, in BBWorldBox p1, in BBWorldBox p2, float arenaMin, float arenaMax);
}
public struct BBWorldBox { public float minX, minY, maxX, maxY; public BBBoxKind kind; public byte hitIndex; public BBAttribute attribute; public byte flags; }
```

- **Broad phase:** none needed. At most 2 fighters × (6 hurt + 4 hit) plus ≤ 8 projectiles gives fewer than 200 pair tests per tick. Use a straight loop with an early-out on a per-fighter bounding box.
- **Multi-hit:** `BBHitMask` is a 32-bit mask per (attacker move instance × defender). Each `hitIndex` can connect once. It resets when the move starts or when a `hitIndex` is marked `rehit` at a set tick (for multi-hit supers).
- **Priority of results in one tick:** throw beats strike if both connect on the same tick (both startup windows overlap). Strike versus strike trades both. Projectile versus strike follows the projectile's attribute against invulnerability.
- **OBB:** not needed in 2.5D. If an angled box is truly required (a diagonal dive kick), approximate it with two AABBs.

### 6.8 Pooling and zero-GC rules
- No `new` in the tick path. All lists are pre-sized arrays: `BBBoxBuffer` (capacity 16), `BBContact[16]`, projectiles from a NEW `BBProjectilePool` (capacity 8 per fighter, warmed at match load). EXISTING `ProjectileFactory.SpawnProjectileForCharacter` instantiates at runtime, so BlazBlue mode wraps its preset data (`ProjectileFactory.GetPresetForFighter`) and spawns from the pool.
- No LINQ, no `foreach` over `List<T>` in the hot path, no boxing (`enum.HasFlag` boxes on older runtimes, so use `(mask & flag) != 0`), no string compares in the hot path. Move ids resolve to `int` indices at load (Classic compares `akioMoveId` strings each cancel check; BlazBlue mustn't).
- No `GetComponent` in the tick. Cache everything in `OnEnable`.
- No `Debug.Log` in the tick unless `#if MDI_BB_TRACE`.
- Events (SFX/VFX) go into a pre-sized ring (`BBEventQueue`, 64 entries) that presentation drains.

### 6.9 Mobile performance budget

| Item | Budget | Measure |
|---|---|---|
| `BBMatchSim.Tick` total (both fighters, collision, gauges) | ≤ 1.0 ms on the reference Android device (Snapdragon 7-series class, e.g. a 2022 mid-range phone), ≤ 0.5 ms on iPhone 12 | Unity Profiler marker `BB.Tick` (NEW `ProfilerMarker`) |
| Collision pass | ≤ 0.10 ms | marker `BB.Collide` |
| GC allocation in the tick | **0 B** | Profiler "GC Alloc" column; the EditMode test in 10.1 uses `Recorder` |
| Render | 60 fps stable; 120 fps optional on ProMotion (EXISTING `CrossPlatformOptimizer`) | Device frame timing |
| Fighters | ≤ 25k tris LOD0, 1 SkinnedMeshRenderer, ≤ 75 bones (EXISTING budget in `ROSTER.md`) | |
| Animator | Animator culling `AlwaysAnimate` for fighters only; `Animator.keepAnimatorStateOnDisable = true` | |
| Memory | Baked box tracks for all 9 fighters < 2 MB | |

### 6.10 Debug overlay and editor tool
- **Runtime overlay (training mode):** extend presentation only. NEW `BBHitboxOverlay` draws the boxes of the current tick with `GL` lines or one pooled mesh. Hurt is green, hit red, push yellow, throw blue, invulnerable boxes grey and armor orange. It respects `SettingsPanel.HighContrastHitboxes` (EXISTING). The EXISTING `HitboxVisualizer` stays for Classic.
- **Frame meter (training mode):** a SF6-style bar under the HUD. Startup is green, active red, recovery blue, hitstun or blockstun yellow, invulnerable white, and frame advantage is shown as a number. It is fed by the sim, not by the Animator. NEW `BBFrameMeter`, which can reuse layout code from the EXISTING `FrameDataDisplay`.
- **Editor tool:** NEW `Assets/Scripts/BlazBlue/Editor/BBBoxEditorWindow.cs`, menu **Demon Idol/BlazBlue/Box Editor**:
  - Pick a character, then a move. The tool loads the avatar, the clip and the `BBAttackData`.
  - A timeline scrubber in **ticks** (60 Hz) also shows the matching 24 fps source frame (`tick / 2.5`).
  - Drag handles in the Scene view on the Z = 0 plane create, move and resize boxes. Keys are set at ticks and hold until the next key.
  - "Follow bone" helper: choose a `HumanBodyBones` and an offset, then press **Bake**, and the tool writes boxes for every tick.
  - Onion-skin the opponent's standing and crouching hurtboxes at a chosen distance to check spacing.
  - Readout: startup, active, recovery, advantage on hit and on block (computed), range (max hitbox X from origin), and whether it hits crouchers (lowest hit Y versus crouch hurt top).
  - Validate button: runs the same checks as the CI validator for this move.
  - Save writes **only** the `BBAttackData` asset. It never touches the FBX, the prefab or the avatar.

### 6.11 Hitbox efficiency prompt (copy-paste for the AI coder)

```text
You are implementing the BlazBlue-mode collision system for the Unity 6 2.5D fighter "My Demon Idol: Iron Legacy".
Read Assets/Scripts/Combat/Hitbox.cs, Hurtbox.cs, FighterController.cs (StartAttack, EnterAttackActive, OnHitboxLanded,
ReceiveIncomingStrike, ApplyMovementPhysics), AkioBodyFit.cs, PushbackSolver.cs, CombatTickEngine.cs first.

Hard rules:
- Do NOT edit Hitbox.cs, Hurtbox.cs, AkioBodyFit.cs, any prefab, avatar, the 56 variants, or MDI_KI_Melee_Fighter.controller.
  Classic mode must behave exactly as before. All new code goes in Assets/Scripts/BlazBlue/** and runs only when
  ControlScheme == BlazBlue4Button and CharacterDefinition.blazBlueProfile != null.
- Physics-free: no Collider, Rigidbody, OnTrigger*, Physics.* calls in the BB tick. 2D AABBs on the Z=0 plane.
- Deterministic: one BBMatchSim drives both fighters in fixed order P1 then P2 inside CombatTickEngine.OnTick.
  Collect all contacts for the tick first, then apply them (trades allowed). Throw beats strike on the same tick.
- Baked data: per-move BBBoxTrack (flat BBBox[] + frameStart[] index, run-length shared ranges). Runtime never reads bones.
  Build an editor bake that samples AnimationClip.SampleAnimation on a hidden avatar copy at t = tick/60 * clipRate and
  writes bone-relative boxes; store a hash (clip guid, firstFrame, lastFrame, speed, avatar guid) and fail validation if stale.
- Zero GC per tick: preallocated BBBoxBuffer(16), BBContact[16], BBHitMask (32-bit per move instance), BBProjectilePool(8/fighter),
  BBEventQueue(64). No LINQ, no foreach over List, no string compares, no GetComponent, no Debug.Log (unless MDI_BB_TRACE).
- Box kinds: Hurt(<=6), Hit(<=4), Push(1), Throw(1), ThrowHurt(1), GuardProximity(1); each box carries BBAttribute (Head/Body/Foot)
  for BlazBlue attribute invulnerability, a hitIndex for multi-hit, and flags (disjoint, CH-only).
- Facing: worldX = posX + facing * cx. Never store mirrored copies.
- Pushbox: split overlap 50/50; cornered fighter does not move; airborne vs grounded pushes the airborne one;
  exact tie pushes P1 left. Clamp with PushbackSolver.ClampToArena.
- Seed body boxes from the measured numbers in the spec section 6.4 (Akio, Aika, UnityChan, PicoChan) and the sizing rules there.
  Add an "extended hurtbox" over the attacking limb during active ticks (70% of hitbox, 0.04 m behind its leading edge) unless disjoint.

Deliver:
1. BBBox, BBBoxTrack, BBWorldBox, BBBoxBuffer, BBContact, BBHitMask, BBCollision (Overlap, StrikePass, ThrowPass, ResolvePush).
2. BBMatchSim tick order exactly as spec section 6.6, with ProfilerMarkers BB.Tick and BB.Collide.
3. BBHitboxOverlay (training) + BBFrameMeter, respecting SettingsPanel.HighContrastHitboxes.
4. BBBoxEditorWindow (menu Demon Idol/BlazBlue/Box Editor): tick scrubber, Scene handles, follow-bone bake, opponent onion skin,
   frame readout, validate, save to BBAttackData only.
5. EditMode tests: overlap edge cases (touching edges = no hit), facing flip, multi-hit hitIndex once-only, trade, throw-beats-strike,
   push tie-break, zero-GC (UnityEngine.Profiling.Recorder "GC.Alloc" == 0 over 600 simulated ticks), determinism
   (same input log -> identical per-tick hash x100).
6. Performance: BB.Tick <= 1.0 ms on a mid Android phone, BB.Collide <= 0.1 ms; report Profiler captures.
Keep every change add-only; open a short-lived branch, PR to main, merge, delete branch. Report what you could not verify in Unity.
```

---

## 7. Animation pipeline

### 7.1 Sources
- **CMU Huge Mocap Library:** `Assets/Huge Mocap Library/mocap animations/<subject>/<subject>_<take>.fbx`. There are 2,534 FBX files, each a Humanoid import (`animationType: 3`) at 24 fps with one long take. Their `clipAnimations` were empty until the depth builds added entries.
- **Kevin Iglesias Human Melee Animations (female free pack)** and **Human Spellcasting Animations 2.0 FREE**: `Assets/Kevin Iglesias/Human Animations/...`. Clip names in use (from `KI_MELEE_ANIMATION.md`) include `Attack1H01_R/L`, `Attack2H01`, `AttackPolearm01`, `AttackShield01`, `CombatIdle01`, `CombatIdle1H01`, `CombatIdle2H01`, `CombatDamage01`, `Death01`, `Idle01–03`, `Run01` (in-place), `CastingEnter01/Exit01/Idle01/Damage01` and `MagicAttackDirect1H01_R`, `MagicAttackDirect2H01`, `MagicAttackOmni01`.
- **Useful CMU subjects:** 144 (female punching and blocks), 135 (karate), 13 / 14 / 15 / 17 / 79 / 80 (boxing), 139 / 140 / 111 (get-ups), 75_16 (jump kick), 90_18 (fall), 16 (jumps), 118 (hops), 02_05 (jabs), 91_39 (leaps), 141 / 143 / 113 / 86 (mixed martial and athletic takes used by the depth builds).
- **Banned:** `88_06`, flips, cartwheels and tiny moves (under about 10 frames for anything except jabs).

### 7.2 Cutting clips (`.fbx.meta` `clipAnimations`, add-only)

The existing pattern (seen in `140_01.fbx.meta`, `144_20.fbx.meta`, `15_13.fbx.meta` and `13_18.fbx.meta`):

```yaml
    clipAnimations:
    - serializedVersion: 16
      name: UChan_140_01_Full      # keep a "<Prefix>_<take>_Full" clip so the import still has a default clip
      takeName: 140_01
      firstFrame: 0
      lastFrame: 165
      loopTime: 0
      keepOriginalOrientation: 1
      keepOriginalPositionY: 1
      keepOriginalPositionXZ: 0
      heightFromFeet: 0
      mirror: 0
      ...
    - serializedVersion: 16
      name: UChan_Getup            # a sunk-hip take: planted root height
      takeName: 140_01
      firstFrame: 70
      lastFrame: 130
      keepOriginalPositionY: 0     # <- sunk-hip fix
      ...
```

Rules:
1. **Append** new list items only. Never reorder or edit existing items, because the existing `AttackData.animationClip` fileIDs are tied to the clip names (Unity hashes the name into the sub-asset fileID). Renaming one breaks Classic.
2. **Name** BlazBlue clips `<Prefix>_BB_<MoveId>`, with prefix `Akio`, `Aika`, `UChan`, `Pico`, or a new 4–6 letter prefix for new girls. Move id characters are limited to `[0-9A-Za-z]`, with `j` for air and no dots. Examples: `Akio_BB_5A`, `Akio_BB_j2C`, `Pico_BB_360C`, `Aika_BB_632146D`.
3. **Reuse before adding.** If a Classic depth clip already covers the motion (for example `Akio_5P` for 5A), point `BBAttackData.clip` at that existing sub-clip and set `clipName` to the existing name. Add a `_BB_` clip only when the window must differ.
4. One take can feed many clips. **Combos of takes** are expressed as *several BBAttackData phases* (`followUps`), or as one move with a *clip sequence* (NEW `BBClipSequence`: `[clip, startTick]` pairs played back-to-back by the animation driver). Clips are never merged into a new asset.
5. Keep `resampleCurves: 1` and `animationCompression` as the importer sets them (3 = Optimal on the existing metas). Don't change the importer's frame rate. The CMU FBX are 24 fps. After reimport, open one clip and confirm its length is `(lastFrame − firstFrame) / 24` s.
6. `loopTime: 1` only on idle, walk, run, crouch, stand block, crouch block and the OD idle. Set `loopBlend: 1` too, and check the loop pose match in the importer (green dot). Everything else uses `loopTime: 0`.
7. Never edit `animationType`, the avatar, `humanDescription` or anything outside `clipAnimations`.

### 7.3 Root motion versus in-place
- Root motion stays **off** (`applyRootMotion = false`, as `HomegrownCombatDirector` sets it). All travel comes from data: `BBAttackData.motionTrack` (per-tick X/Y displacement, baked from the clip's root curve by the editor tool, then hand-tuned), or from character constants for walk, dash and jump.
- `keepOriginalPositionXZ: 0` and `keepOriginalOrientation: 1` (existing defaults) keep clips facing the authored direction. If a take drifts in Z (depth), its baked `motionTrack` drops Z, and the renderer clamps the model's local Z to 0.
- **Foot sliding:** for walk loops, bake the clip's average root speed (m/s) and set the walk speed to match it (or set `animator.speed = walkSpeed / clipRootSpeed`). The target is under 3 cm of slide per step, checked in the editor tool's ground-contact view. Akio's walk (144_33 150–178) is the reference.

### 7.4 Sunk-hip and floor fixes
- Some takes have the hip below the floor at the start (CMU get-ups and falls). Use `firstFrame ≥ 8` (Akio's get-up and fall start at frame 8) and/or `keepOriginalPositionY: 0` on that clip only (as on `UChan_Fall`, `UChan_Getup` and Pico's get-up from `113_08` frames 300–342).
- Known risky takes: `111_06` (Akio get-up, hip Y negative), `114_02` (Aika get-up, frames 211–261), `140_01` (UnityChan) and `113_08` (PicoChan get-up, hip near 10 cm).
- At runtime in BlazBlue mode, the NEW `BBAnimationDriver` adds a per-clip `rootYOffset` (data) if the preview still shows sinking. It never moves the CharacterController.

### 7.5 Data-driven frames versus Animation Events
- The sim **never** reads Animation Events. Hit timing, projectile spawns, invulnerability and sounds come from `BBAttackData` ticks (`events[]`).
- The KI clips are shared with the pack demo scenes, so the existing doc says not to add events to them. BlazBlue mode doesn't need any.

### 7.6 Playback speed (24 fps clips on 60 Hz ticks)

```
clipSeconds      = (lastFrame - firstFrame) / 24
moveSeconds      = totalTicks / 60            // startup + active + recovery
playbackRate     = clipSeconds / moveSeconds
// Classic depth path (EXISTING, AkioPresentation): animator.speed = playbackRate / bakedStateSpeed
// BB path (NEW BBAnimationDriver): AnimationClipPlayable.SetTime(tickInMove / 60 * playbackRate) every tick (seek, not play)
```

**Align the active frame:** choose `firstFrame` so that the *contact pose* lands on the first active tick: `contactFrame ≈ firstFrame + startupTicks / 60 × 24 × playbackRate`. The editor tool shows this marker.

### 7.7 Mapping clips to controllers (BlazBlue mode)
The base `MDI_KI_Melee_Fighter` has a fixed set of triggers and states (`LightAttack`, `MediumAttack`, `HeavyAttack`, `DriveSkill`, `AdvancingSkill`, `LauncherAttack`, `LowAttack`, `LowSweepAttack`, `OverheadAttack`, `AirLight`, `AirHeavy`, `JumpAttack`, `ThrowCard`, `Throw`, `DomainSuper`, `Burst`/`BurstShockwave`, `RapidCancel`, `Parry`, `Sidestep`, `ForwardDash`, `Backdash`, `Jump`, `Knockdown`, `StandUpBack`, `Launch`, `WallSplat`, `BoundTornado`, `HitHigh/Mid/Low`, `Hit`, `Defeat`, `Victory`, `Intro`, `StandBlock`, `CrouchBlock`, plus params `MoveX`, `Speed`, `IsBlocking`, `IsCrouch`, `IsGrounded`). That isn't enough states for a 40-move BlazBlue kit, and it can't be edited.

**Decision:**
1. Each girl gets a NEW `Assets/MDI_Characters/Runtime/BlazBlue/<Name>_BB_Override.overrideController`, parented to `MDI_KI_Melee_Fighter`. It holds her **locomotion, guard, hit reactions, knockdown, get-up, intro and victory** CMU clips, filling the existing override slots in the asset so nothing is swapped at runtime. The Classic `<Name>_KI_Melee_Override` stays untouched.
2. **Attacks** play through the NEW `BBAnimationDriver`, which uses the Playables API on the same Animator:
   - `PlayableGraph` → `AnimationLayerMixerPlayable` (input 0 = `AnimatorControllerPlayable` of the BB override, input 1 = `AnimationClipPlayable` for the current move).
   - When a move starts, swap input 1's clip (from a pooled set of 2 clip playables, ping-ponged for a 2–3 tick blend) and set weight 1. Each tick, `SetTime` the playable deterministically. When the move ends, blend the weight to 0 over 3 ticks.
   - Clip playables are created at match load for every move clip, then kept, so there is no allocation mid-match.
   - **Assigning in BlazBlue mode only:** `BBFighterBrain` sets `animator.runtimeAnimatorController = profile.bbAnimator` and builds the graph. On disable it restores the controller that was there before and destroys the graph.
3. The Classic `AkioPresentation` / `AkioRuntimeOverride` path is never called in BlazBlue mode.

### 7.8 Retarget checks (per clip)
- Preview on the girl's avatar in the Animation window. Check knees don't hyperextend, hands don't pass through the head, and feet aren't below Y = 0 by more than 2 cm.
- Watch for missing bones. Akane has no Chest, so spine twist spreads. UnityChan has no UpperChest, Jaw or Eyes, and Muryotaisu has no UpperChest, Toes or Jaw. Choose takes without big chest twists for those fighters.
- Arm length: the CMU actors are adult-sized, so on short girls (PicoChan, UnityChan) punches look shorter. Hitboxes come from the baked bones on *her* avatar, so they're correct automatically.
- Turn on Foot IK only on the BB override's locomotion states, if needed (an import setting on the clip; don't change the avatar).

### 7.9 Per-character animation checklist (do for every girl)
1. List every move from her section-8 table with its source take and frame window, or TBD.
2. Add the clip entries (add-only) and reimport only those FBX files.
3. Confirm 24 fps and the lengths match the windows.
4. Preview each on her avatar and fix sinking (7.4).
5. Bake root `motionTrack` and boxes (6.10). Hand-tune them.
6. Build `<Name>_BB_Override` for locomotion, guard, reactions, intro and victory.
7. Check walk foot-slide is under 3 cm and loops are seamless.
8. Run `tools/validate_blazblue.py --character <Id>` (NEW) until it is green.
9. Play every move in training with the hitbox overlay and frame meter, and sign off section 10.5.

---

## 8. Per-character BlazBlue design

### 8.1 Roster for launch (target: 9 playable)

| Slot | Id | Girl | Status | Archetype (identity kept) | Drive (D) |
|---|---|---|---|---|---|
| 1 | C01 | Akio | Classic depth built | Rushdown puncher | **Iron Rush** (rush-punch chain that banks Momentum) |
| 2 | C02 | Aika | Classic depth built | Karate zoner (kicks) | **Resonance Nail** (nail projectiles plus detonation) |
| 3 | C04 | UnityChan | Classic depth built | Aerial hopper | **Aero Hop** (directional hops with extra air actions) |
| 4 | C09 | PicoChan | Classic depth built | Close grappler | **Clinch** (D-button grabs that bank Grip) |
| 5 | C12 | Fina | good model (55 bones) | All-rounder / footsies | **Plasma Charge** (hold-to-charge levels) |
| 6 | C08 | SanpakuChan | good model (54 bones) | Speed and mixup | **Crow Step** (short teleports and cross-ups) |
| 7 | C06 | Cultist | good model (53 bones) | Setplay / summoner | **Void Orb** (place and command orbs) |
| 8 | C10 | Reika | good model (50 bones; `pevis` hips; Kiki script disabled) | Counter / stance | **Veil** (guard-point counter stance) |
| 9 | C07 | StellarWitch | **chosen by Izno** (18 Nov 2026) | Fullscreen zoner | **Star Field** |

**Slot 9 decision (Izno, 18 Nov 2026): C07 StellarWitch.** Akane and Muryotaisu are not slot 9.
- **StellarWitch (C07):** the hips were remapped to `Hip`, and the FBX must be reimported so the avatar rebuilds before her kit is built. She is the fullscreen zoner (Drive **Star Field**).
- **Akane (C05):** there is no chest bone between `Spine` and `Neck`, so spine twist spreads. Not in the launch-nine.
- **Muryotaisu (C14):** 51 bones with no upper chest, toes or jaw, and her `MuryotaisuController` is disabled. Not in the launch-nine.
- **Reika (C10)** is listed as playable, but her Kiki Rigidbody and capsule setup needs a Play Mode check first (`KI_MELEE_ANIMATION.md`).
- Excluded: Ren C03 (dummy), Casual1 C11 (empty bone map; needs a Blender reparent) and SapphiArtchan C13 (Generic rig).
- **Check that the definition animator is correct** for every non-built girl (finding in 2.6) before building her.

### 8.2 Common skeleton (every girl has these slots)

Ticks are S/A/R, damage is on an 11,000 HP scale, and "adv" is on-block advantage. These are the targets, and each girl's table overrides them.

| Slot | Input | Role | Target ticks | Guard | Target adv | Default cancels |
|---|---|---|---|---|---|---|
| 5A | A | fastest jab | 5–7 / 2–3 / 7–10 | Mid | −1 to +1 | 5A 2A (×3), all B/C, specials, DD |
| 2A | 2A | low or crouching jab | 6–7 / 3 / 9–11 | Low or Mid | −2 to 0 | same |
| 6A | 6A | anti-air (Head-invulnerable 4–12) | 9–11 / 4 / 14–18 | Mid | −6 | specials, DD |
| 5B | B | main poke | 8–10 / 3–4 / 12–15 | Mid | −3 to −5 | C, specials, DD (jump cancel on hit for some) |
| 2B | 2B | low poke | 8–10 / 3 / 13–16 | Low | −4 to −6 | C, specials, DD |
| 6B | 6B | overhead | 18–24 / 3–4 / 16–20 | Overhead | −6 to −8 | specials (on hit), DD |
| 5C | C | big button | 11–14 / 4–5 / 16–20 | Mid | −5 to −7 | specials, DD, 3C |
| 2C | 2C | sweep (knockdown) | 10–12 / 4 / 18–22 | Low | −10 | specials, DD |
| 3C | 3C | launcher (jump cancel on hit) | 12–16 / 4–5 / 18–22 | Mid | −10 to −12 | jump, specials, DD |
| j.A / j.B / j.C | air | air normals, overhead | 5–6 / 9–10 / 9–12 | Overhead | — | j.A → j.B → j.C → j.D, jump cancel j.B/j.C on hit |
| 5D / 2D / 6D / j.D | D | Drive moves | per girl | per girl | per girl | per girl |
| Specials ×4–6 | motions | identity | per girl | | | DD |
| DD ×2 | 236236X / 214214X / 632146X | 50 Heat | 7–16 / … | | very minus | — |
| Astral ×1 | per girl | 100 Heat, match point | | | | — |
| Throws | 6BC / 4BC / j.BC | | 7 / 2 / 24 | Throw | | — |
| CA / CT / Burst / OD / EA | system | section 4 | | | | |

### 8.3 C01 Akio: rushdown puncher

**Identity:** walks people down with punches and Drive rush chains. Uses a run dash. Hips about 0.95 m.
**Drive "Iron Rush":** D is a three-step rush-punch chain, D → D → D. Each D hit (or block) banks 1 **Momentum** (max 3; shown on the HUD as 3 pips). Spending Momentum powers up 236C (+1 hit per pip) and the 632146D Distortion Drive. **OD Iron Rush:** Momentum stays at 3, the chain gets a 4th hit, and D hits are +2 on block.
**Resource:** `DriveResourceKind.Stocks`, max 3. Stocks drop to 0 on knockdown.

| Move | Input | Source (24 fps window) | Ticks S/A/R | Dmg | Guard | Adv | Notes / cancels |
|---|---|---|---|---|---|---|---|
| Jab | 5A | `Akio_5P` 144_20 110–122 (reuse) | 6/3/8 | 300 | Mid | −1 | 5A/2A ×3, all B/C, specials, DD |
| Crouch jab | 2A | `Akio_2P` 144_20 196–214 (reuse) | 7/3/11 | 280 | Low | −2 | same |
| Anti-air elbow | 6A | TBD: subject 144 uppercut/elbow (search 144_1x/144_2x) | 10/4/16 | 600 | Mid | −6 | Head-invulnerable 4–12; specials |
| Strong straight | 5B | `Akio_5M` 144_20 123–136 (reuse) | 8/4/12 | 600 | Mid | −3 | C, 3C, specials, DD |
| Low body hook | 2B | TBD: 144 crouching hook, or 13/14 boxing body shot | 9/3/14 | 520 | Low | −5 | C, specials |
| Overhead | 6B | `Akio_6M` 144_20 148–166 (reuse) | 20/4/16 | 750 | Overhead | −6 | specials on hit, DD |
| Stepping cross | 5C | `Akio_6S` 144_17 24–50 (reuse) | 12/4/16 | 900 | Mid | −6 | specials, DD, 3C. **Fatal Counter** |
| Sweep | 2C | `Akio_2M` 135_05 100–120 (reuse) | 10/4/18 | 700 | Low | −10 | knockdown; specials |
| Launcher | 3C | `Akio_2S` 135_07 140–164 (reuse) | 12/5/18 | 750 | Mid | −10 | jump cancel on hit, specials |
| j.A | j.A | `Akio_jP` 143_23 42–51 | 5/3/8 | 320 | Overhead | — | j.B j.C j.D |
| j.B | j.B | `Akio_jM` 75_16 40–58 | 8/6/14 | 700 | Overhead | — | j.C, jump cancel on hit |
| j.C | j.C | `Akio_jS` 75_16 44–60 | 9/6/16 | 850 | Overhead | — | knockdown on air hit |
| Iron Rush 1 | 5D | `Akio_Rekka1` 144_20 34–47 | 6/3/10 | 350 | Mid | −2 | → 5D (2), specials; +1 Momentum |
| Iron Rush 2 | 5D (after 1) | `Akio_Rekka2` 144_20 123–136 | 7/3/11 | 450 | Mid | −3 | → 5D (3) or 2D; +1 Momentum |
| Iron Rush 3 | 5D (after 2) | `Akio_Rekka3` 135_05 100–120 | 9/4/14 | 600 | Low | −6 | knockdown; DD; +1 Momentum |
| Low Rush | 2D | TBD: 135 low punch (135_05 near 100) | 9/3/14 | 450 | Low | −4 | → 5D (2) |
| Rush Step | 6D | sequence `Akio_Dash` 144_17 20–40, then `Akio_6M` | 22/4/16 | 700 | Overhead | −5 | moves 1.6 m |
| Dive Punch | j.D | TBD: 143_23 later window, or 144 downward punch | 10/4/12 | 650 | Overhead | — | angled down 30°; grounded hit bounces (ground bounce once) |
| Shadow Kunai | 236A / 236B | KI `MagicAttackDirect1H01_R` (`ThrowCard`) | 12/1/20 | 600 | Mid | −4 | A slow (speed 12), B fast (16); `ProjectileFactory` "Shadow Ink Kunai"; chip 10% |
| Rising Fist | 623C | `Akio_623S` 135_07 15–34 | 6/6/22 | 1000 | Mid | −18 | Head and Body invulnerable 1–8; knockdown |
| Shoulder Rush | 214B | TBD: 144_17 shoulder or 13/14 boxing lunge | 14/4/18 | 800 | Mid | −4 | goes through projectiles 6–14 (Projectile invulnerable) |
| Rush Combination | 236C | `Akio_6S` 144_17 24–50 followed by `Akio_5M` | 13/4+4/18 | 900 +150 per Momentum | Mid | −8 | spends all Momentum |
| Iron Barrage (DD) | 236236C | `Akio_Super` 135_06 48–80 | 14/8/22 | 2800 | Mid | −12 | Strike invulnerable 1–14; min 30% |
| Crimson Shadow Rush (DD) | 632146D | sequence 144_17 20–40, 144_20 110–136, 135_06 48–80 | 10/24/26 | 3000 (+300 per Momentum) | Mid | −20 | Throw invulnerable; on hit, a cinematic chain |
| OD Exceed Accel | ABC in OD | 135_06 48–80 (slowed ×0.8) | 12/8/24 | 2200 | Mid | −16 | ends OD |
| Astral "Iron Legacy" | 222C | TBD: 135 karate finisher plus 144 combination | 8/—/— | KO | Mid | — | optional |
| Forward / back throw | 6BC / 4BC | `Akio_Throw` 144_17 26–48 | 7/2/24 | 1200 | Throw | — | |
| Air throw | j.BC | TBD: 143_23 | 7/2/20 | 1000 | Throw | — | |
| Counter Assault | 6AB (blockstun) | reuse `Akio_5M` | 9/3/26 | 0 / non-lethal | Mid | — | |
| Crush Trigger | AB | TBD: 135_06 wind-up + 144_20 straight | 30–60/4/20 | 900 | guard crush | — | |

Locomotion (from `AKIO_MOVELIST.md`, referenced by `Assets/MDI_Characters/Runtime/Akio/AkioClipLibrary.asset`): walk 144_33 150–178 (loop); dash 144_17 20–40; jump 75_16 32–50; crouch 135_05 80–92 (loop); stand block 144_07 186–206; crouch block 135_10 300–320; knockdown 90_18 8–45; get-up 111_06 8–48 (preview the hip); intro 144_33 2–48; victory KI `Idle03`.

### 8.4 C02 Aika: karate zoner (kicks)

**Identity:** controls space with long kicks and Nails. Her shin is the long limb. Step dash. Hips about 0.92 m.
**Drive "Resonance Nail":** D throws an occult nail (the existing "Resonance Occult Nail" preset, speed 13, damage 55 in Classic). She has **5 Nail stocks**, regaining 1 every 120 ticks. 5D is a straight nail, 2D slides along the ground (hits low), j.D goes diagonally down, and 6D **detonates** every nail on screen (hits around each nail). Holding D delays the throw by up to 20 ticks (feints). **OD Resonance:** nails cost no stock, and each nail splits into two.
**Resource:** `DriveResourceKind.Stocks`, max 5.

| Move | Input | Source | Ticks S/A/R | Dmg | Guard | Adv | Notes |
|---|---|---|---|---|---|---|---|
| Jab | 5A | `Aika_5P` 80_10 52–68 | 5/3/9 | 260 | Mid | −2 | |
| Low fist | 2A | `Aika_2P` 135_02 462–480 | 7/3/12 | 280 | Low | −3 | |
| Rising knee (AA) | 6A | TBD: 135 karate knee (135_09 or 135_11 early window) | 10/4/16 | 600 | Mid | −6 | Head-invulnerable 4–12 |
| Cross | 5B | `Aika_5M` 13_18 73–91 | 8/4/14 | 640 | Mid | −5 | |
| Low front kick | 2B | TBD: 135_02, a window just before 492 | 9/3/15 | 560 | Low | −5 | long range (foot box 0.56 m) |
| Overhead kick | 6B | `Aika_6M` 143_24 164–184 | 20/4/18 | 800 | Overhead | −8 | |
| Roundhouse | 5C | `Aika_Rekka3` 113_13 245–265 | 12/4/16 | 820 | Mid | −7 | **Fatal Counter** |
| Sweep | 2C | `Aika_2M` 135_02 492–514 | 10/4/16 | 720 | Low | −10 | knockdown |
| Launcher kick | 3C | `Aika_2S` 113_13 219–243 | 12/5/20 | 780 | Mid | −12 | jump cancel |
| j.A | j.A | `Aika_jP` 13_18 280–296 | 5/3/8 | 280 | Overhead | — | |
| j.B | j.B | `Aika_jM` 141_14 44–64 | 8/5/14 | 780 | Overhead | — | |
| j.C | j.C | `Aika_jS` 141_14 80–102 | 9/6/16 | 920 | Overhead | — | |
| Nail | 5D | KI `MagicAttackDirect1H01_R` | 13/1/18 | 550 | Mid | −3 | costs 1 stock; chip 8% |
| Ground Nail | 2D | KI `MagicAttackDirect2H01` | 15/1/20 | 500 | Low | −4 | |
| Detonate | 6D | KI `MagicAttackOmni01` | 18/3/20 | 400 per nail | Mid | −6 | disjoint hitbox at each nail |
| Air Nail | j.D | TBD: KI cast in the air, or 141_14 arm window | 11/1/16 | 500 | Overhead | — | 45° down |
| Reap | 214B | `Aika_214S` 143_24 34–54 | 10/5/18 | 860 | Low | −8 | knockdown |
| Side kick | [4]6C | `Aika_Charge` 135_11 168–192 | 18/6/22 | 1120 | Mid | −14 | wall bounce in corner |
| Rising kick | 623C | `Aika_623S` 135_11 62–84 | 7/6/24 | 1080 | Mid | −22 | Head and Body invulnerable 1–7 |
| Step kick | 236B | `Aika_6S` 111_19 57–75 | 11/4/18 | 840 | Mid | −8 | moves 1.0 m |
| Hundred Kicks (DD) | 236236C | `Aika_Super` 13_18 276–316 | 16/10/24 | 2600 | Mid | −16 | Strike invulnerable 1–16 |
| Nail Rain (DD) | 632146D | KI `MagicAttackOmni01`, then 135_11 62–84 | 12/30/28 | 2400 | Mid | −10 | full-screen nails, chip 15% |
| OD Exceed Accel | ABC in OD | 13_18 276–316 window 2 | 12/8/24 | 2200 | Mid | −16 | |
| Astral "Thousand Nails" | 214214D | TBD: 135 kata finisher | 10/—/— | KO | Mid | — | optional |
| Throws | 6BC / 4BC | `Aika_Throw` 111_19 36–58 | 7/2/24 | 1200 | Throw | — | |
| Air throw | j.BC | TBD: 141_14 | 7/2/20 | 1000 | Throw | — | |

Locomotion: intro 74_01 8–48; victory 144_08 560–584; get-up 114_02 211–261 (preview the hip); crouch, blocks and the other locomotion clips are already referenced by `Assets/MDI_Characters/Runtime/Aika/AikaClipLibrary.asset` (an `AkioClipLibrary` instance). Reuse them from there.

### 8.5 C04 UnityChan: aerial hopper

**Identity:** short reach (forearm about 0.16 m), so she wins by leaving the ground. Hop dash. 2 air actions. Hips about 0.87 m.
**Drive "Aero Hop":** 5D is a vertical hop (16_01). It is airborne from tick 4, so it beats lows (Foot-invulnerable 4–18), and she can act from tick 10. 6D is a forward leap (91_39 24–44), 4D a back hop, and j.D an **extra air hop** that doesn't use an air action (once per jump) and fires a small disc. **OD Aero:** unlimited j.D, and every hop fires a disc.
**Resource:** none (`DriveResourceKind.None`).

| Move | Input | Source | Ticks S/A/R | Dmg | Guard | Adv | Notes |
|---|---|---|---|---|---|---|---|
| Tap | 5A | `UChan_5P` 02_05 92–108 | 4/3/8 | 220 | Mid | −1 | |
| Dip | 2A | `UChan_2P` 02_05 212–230 | 6/3/10 | 240 | Low | −2 | |
| Hop knee (AA) | 6A | TBD: 118 hop with knee, or 16_05 early | 9/4/14 | 560 | Mid | −6 | Head-invulnerable 3–12 |
| Cross | 5B | `UChan_5M` 02_05 144–162 | 7/3/12 | 550 | Mid | −3 | |
| Low check | 2B | `UChan_2M` 14_02 630–648 | 8/3/14 | 580 | Low | −7 | |
| Hop overhead | 6B | `UChan_6M` 16_05 20–40 | 18/4/14 | 680 | Overhead | −6 | |
| Triple punch end | 5C | `UChan_Rekka3` 02_05 148–168 | 11/4/14 | 650 | Mid | −5 | |
| Sweep | 2C | TBD: 14_02 low window, or 02_05 crouch | 10/4/18 | 620 | Low | −10 | knockdown |
| Hop launcher | 3C | `UChan_2S` 16_01 16–40 | 9/5/16 | 700 | Mid | −8 | jump cancel; airborne 6+ |
| j.A | j.A | `UChan_jP` 02_05 124–140 | 4/3/8 | 240 | Overhead | — | |
| j.B | j.B | `UChan_jM` 118_01 88–106 | 7/5/12 | 700 | Overhead | — | |
| j.C | j.C | `UChan_jS` 118_05 64–82 | 8/5/14 | 820 | Overhead | — | |
| Hop | 5D | 16_01 16–40 (or `UChan_2S`) | 4 → actionable 10 | 0 | — | — | Foot-invulnerable 4–18 |
| Leap | 6D | `UChan_6S` 91_39 24–44 | 10/5/14 | 760 | Mid | −5 | crosses up a crouching opponent at close range |
| Back hop | 4D | TBD: 16_35 or 118 backward hop | 3 → actionable 12 | 0 | — | — | Projectile-invulnerable 3–10 |
| Air hop + disc | j.D | 118_01 rise window (TBD) + KI cast | 6/1/10 | 300 | Mid | — | once per jump |
| Aero Disc | 236A / 236B | KI `MagicAttackDirect1H01_R` | 11/1/16 | 440 | Mid | −2 | "Aero Plasma Disc" preset; B version angles up 30° |
| Rising hop | 623C | `UChan_623S` 16_05 30–50 | 6/5/20 | 900 | Mid | −14 | Head and Body invulnerable 1–6 |
| Bound kick | 214B | `UChan_214S` 91_39 50–70 | 9/4/16 | 740 | Mid | −6 | |
| Hop stomp | [4]6C | `UChan_Charge` 118_05 47–70 | 14/5/18 | 880 | Overhead | −9 | knockdown |
| Dive | j.214B | TBD: 118_05 descent window | 8/until land/12 | 700 | Overhead | — | ground bounce on air hit |
| Flurry (DD) | 236236C | `UChan_Super` 14_02 811–845 | 14/8/20 | 2400 | Mid | −12 | Strike invulnerable 1–14 |
| Disc Storm (DD) | j.236236C | TBD: 118 air pose + KI `MagicAttackOmni01` | 10/20/— | 2200 | Mid | — | air-only DD |
| OD Exceed Accel | ABC in OD | 16_01 + 14_02 811–845 | 10/8/22 | 2000 | Mid | −14 | |
| Astral | 632146D | TBD: 16 high jump + 118 stomp | 8/—/— | KO | Mid | — | optional |
| Throws | 6BC / 4BC | `UChan_Throw` 02_05 72–94 | 7/2/24 | 1100 | Throw | — | |
| Air throw | j.BC | TBD: 118 | 7/2/20 | 1000 | Throw | — | |

Locomotion: intro 16_15 8–40; victory 16_01 48–64; fall 140_01 8–40 and get-up 140_01 70–130 (both `keepOriginalPositionY: 0`); `16_35` is also on her reimport list (use it for walk or back hop; TBD).

### 8.6 C09 PicoChan: close grappler

**Identity:** the shortest girl (hips 0.76 m), with a long forearm (0.21 m) on a short body. She crowds, hooks and grabs. Step dash.
**Drive "Clinch":** D moves are grabs. 5D is a short unblockable grab (≈ 0.9 m). 2D is a low grab that only catches crouching or low-posture targets. 6D is a running grab (dash 17_10 355–373 into the grab), and j.D is an air grab. Landing a D grab banks 1 **Grip** (max 2). While Grip ≥ 1, her 360C does +20% damage and can't be teched, and her 632146C DD gains range. **OD Clinch:** D grabs gain +0.4 m range and armor on ticks 1–6.
**Resource:** `DriveResourceKind.Stocks`, max 2.

| Move | Input | Source | Ticks S/A/R | Dmg | Guard | Adv | Notes |
|---|---|---|---|---|---|---|---|
| Hook | 5A | `Pico_5P` 15_13 638–654 | 5/3/9 | 320 | Mid | −1 | |
| Body hook | 2A | `Pico_2P` 15_13 376–394 | 6/3/11 | 340 | Mid | −2 | |
| Upper (AA) | 6A | `Pico_6M` 15_13 400–418 | 11/4/16 | 700 | Mid | −7 | Head-invulnerable 4–12 |
| Hook | 5B | `Pico_5M` 15_13 1256–1274 | 8/4/13 | 720 | Mid | −4 | |
| Low | 2B | `Pico_2M` 15_13 344–362 | 9/4/15 | 680 | Low | −8 | |
| Overhead | 6B | TBD: 15/17 boxing overhand | 20/4/18 | 760 | Overhead | −7 | |
| Heavy hook | 5C | `Pico_Rekka3` 86_08 556–574 | 11/4/14 | 800 | Mid | −5 | **Fatal Counter** |
| Sweep | 2C | TBD: 15_13 low window or 86_08 | 11/4/20 | 700 | Low | −10 | knockdown |
| Knee launcher | 3C | `Pico_2S` 15_13 288–308 | 10/4/18 | 740 | Mid | −9 | jump cancel |
| j.A | j.A | `Pico_jP` 15_13 765–783 | 5/3/8 | 300 | Overhead | — | |
| j.B | j.B | `Pico_jM` 86_08 312–330 | 8/5/13 | 740 | Overhead | — | |
| j.C | j.C | `Pico_jS` 86_08 944–962 | 9/5/15 | 860 | Overhead | — | |
| Clinch | 5D | `Pico_214S` 141_23 11–33 | 6/3/18 | 1200 | Unblockable grab | — | +1 Grip; tech window 6 |
| Low Clinch | 2D | TBD: 139_09 duck + 141_23 grab | 8/3/22 | 1000 | grabs crouchers only | — | |
| Run Clinch | 6D | sequence `Pico_Dash` 17_10 355–373 then 141_23 | 14/3/24 | 1100 | Unblockable grab | — | travels 2.0 m |
| Air Clinch | j.D | TBD: 86_08 arm window | 5/3/16 | 1000 | air grab | — | |
| Lariat | 236A | `Pico_236S` 17_10 37–57 | 8/4/16 | 840 | Mid | −4 | |
| Uppercut | 623C | `Pico_623S` 17_10 149–169 | 6/5/22 | 1040 | Mid | −18 | Head and Body invulnerable 1–6 |
| Charged straight | [4]6C | `Pico_Charge` 86_08 1502–1524 | 16/5/20 | 1020 | Mid | −12 | armor on ticks 4–12 |
| Step hook | 63214B | `Pico_6S` 17_10 173–193 | 11/4/16 | 860 | Mid | −6 | cancels into 5D on hit |
| Big Throw | 360C | sequence `Pico_Throw` 111_33 314–336 + TBD 141 slam | 2/2/40 | 1800 (+20% with Grip) | command throw | — | not techable with Grip ≥ 1; range 1.0 m |
| Close Flurry (DD) | 236236B | `Pico_Super` 15_13 820–854 | 15/8/22 | 2800 | Mid | −14 | Strike invulnerable 1–15 |
| Pico Press (DD) | 632146C (720 optional) | TBD: 141_23 + 111_33 + 140 slam | 1/2/45 | 3400 | command throw DD | — | invulnerable 1–3 |
| OD Exceed Accel | ABC in OD | 15_13 820–854 | 12/8/24 | 2200 | Mid | −16 | |
| Astral | 360360D | TBD: subject 141 or 140 slam | 1/—/— | KO | throw | — | optional |
| Throws | 6BC / 4BC | `Pico_Throw` 111_33 314–336 | 7/2/24 | 1500 | Throw | — | |
| Air throw | j.BC | TBD: 86_08 | 7/2/20 | 1200 | Throw | — | |

Locomotion: intro 111_34 8–40; victory (bow) 111_02 17–77; walk 111_34 82–110; dash 17_10 355–373; jump 143_06 12–32; crouch 139_09 110–130; get-up 113_08 300–342 (`keepOriginalPositionY: 0`).

### 8.7 New girls (C12 Fina, C08 SanpakuChan, C06 Cultist, C10 Reika, C07 StellarWitch)

These use the 8.2 skeleton. Only their identity moves are listed. Every source is **TBD**, and each girl must use **mostly different CMU takes** from the four built girls (Akio is mostly subject 144; Aika uses 80, 13, 135, 141, 143, 113 and 111; UnityChan uses 02, 14, 16, 118 and 91; PicoChan uses 15, 17, 86, 111, 141, 143 and 139). The hints say where to look first. Measure hips, forearm and shin on her avatar before sizing boxes (6.4).

**C12 Fina: all-rounder / footsies.** Drive **Plasma Charge**: hold D to charge in 3 levels (20 / 45 / 75 ticks), and release to fire the Neo-Pink Plasma Burst (Classic preset speed 16, damage 52) at that level. Level 3 is a 2-hit, Projectile-beating shot. Hold D, then press A, B or C to *store* the level instead, giving the next special +1 hit. OD: all charges come out instantly at level 3.

| Move | Input | Role | Search hint |
|---|---|---|---|
| 5A–3C, j.A–j.C | skeleton | balanced reach, best 5B and 2B pokes in the cast | boxing 79 / 80 (takes not used by Aika), karate 135 (unused windows) |
| Plasma Shot | D (hold/release), 2D (low), j.D | projectile, 3 levels | KI `MagicAttackDirect1H01_R` / `MagicAttackDirect2H01` |
| Fireball | 236A/B | standard projectile | KI cast |
| Anti-air | 623C | invulnerable reversal | 135 rising kick windows other than 135_11 |
| Advancing kick | 214B | gap closer | 135 / 80 stepping kick |
| Overhead hop | 6D | mixup | 16 (not 16_01/05/15/35) |
| DD | 236236C, 632146D | beam super / rush super | KI `MagicAttackOmni01` + 135 |
| Astral | 214214C | optional | — |

**C08 SanpakuChan: speed and mixup.** Drive **Crow Step**: 5D teleports behind the opponent (14 ticks, invisible 4–10, Strike-invulnerable 4–8), 4D is a back-teleport, 2D a low dash-slide, and j.D an air reposition. Each Crow Step costs 1 of 3 **Feathers** (regen 1 per 150 ticks). The "Crow Feather Needle" preset (speed 16, damage 42) is her 236A. OD: Crow Steps are free and come out 6 ticks faster.

| Move | Input | Role | Search hint |
|---|---|---|---|
| 5A–3C | skeleton | fast, short, low damage (×0.85) | 13 / 14 boxing combinations not used by UnityChan (14_02 used) |
| Crow Step | 5D / 4D / 2D / j.D | teleports | 91 leaps (not 91_39), 16 run takes; no flips |
| Feather Needle | 236A, j.236A | projectile | KI cast |
| Rekka dive | 214B → B → B | three-part rekka | 141 (not 141_14 / 141_23), 113 (not 113_08 / 113_13) |
| Overhead | 6B (fastest in cast, 16 ticks) | mixup | 118 hops other than 118_01 / 118_05 |
| DD | 236236B, 214214D (cross-up teleport DD) | | 91 / 16 |
| Astral | 632146D | optional | — |

**C06 Cultist: setplay / summoner.** Drive **Void Orb**: 5D places a Void Skull Orb in front of her (max 2 on screen; the "Void Skull Orb" preset speed 12, damage 50 is used for the shot). 6D makes orbs dash forward, 2D makes them drop, and 4D recalls them. Orbs have a 1-hit hurtbox (they can be destroyed). OD: 3 orbs, and orbs fire automatically.

| Move | Input | Role | Search hint |
|---|---|---|---|
| 5A–3C | skeleton | slower normals (+1 tick), strong anti-air 6A | 135 karate windows not used, 79 boxing |
| Orb control | 5D / 6D / 2D / 4D / j.D | set-play | KI Spellcasting `CastingEnter01` / `CastingIdle01` / `MagicAttackOmni01` |
| Skull shot | 236B | projectile | KI cast |
| Teleport | 22B / 22C | reposition next to an orb | 91 / 16 short leap (no flips) |
| Reversal | 623C | Projectile-invulnerable | 135 |
| DD | 236236C (orb explosion), 632146D | | KI Omni |
| Astral | 214214D | optional | — |

**C10 Reika: counter / stance.** Drive **Veil**: 5D is a counter stance with a Guard point (Head|Body) on ticks 4–14. If a strike hits it, she auto-counters with a 1200-damage punish that is Strike-invulnerable. 2D is the same against lows (Foot), and j.D is an air counter. A whiffed Veil has 22 recovery. The "Crimson Crescent Wave" preset (speed 14, damage 48) is her 236B. OD: the Veil window is ticks 2–20.

| Move | Input | Role | Search hint |
|---|---|---|---|
| 5A–3C | skeleton | solid mid-range; 5C with long recovery | 135 karate (blocks/parries), 144 *block* takes not used by Akio (Akio uses 144_07 186–206) |
| Veil counters | 5D / 2D / j.D | guard point | 135 block-and-strike, 144 parry |
| Crescent Wave | 236B | projectile | KI cast |
| Lunge | 41236C | long-range special | 135 lunge punch |
| Rising strike | 623C | reversal | 135 |
| DD | 236236C (counter DD, Guard point 1–20), 632146C | | 135 kata |
| Astral | 214214D | optional | — |

**Slot 9 is C07 StellarWitch (Izno, 18 Nov 2026).** Akane and Muryotaisu are not built as slot 9.
- *StellarWitch (C07), fullscreen zoner:* Drive **Star Field** places up to 3 star points; D fires Starlight Comets (preset speed 13, damage 48) from every point. Her normals are slow and her air mobility weak. Search hints: KI Spellcasting, plus 135 for normals. Reimport her FBX so the `Hip` remap rebuilds the avatar before cutting clips.

### 8.8 Per-character asset list (what to create for each girl)

| Asset (NEW) | Path |
|---|---|
| `BBCharacterProfile` | `Assets/CombatData/BlazBlue/<Name>/<Name>_BBProfile.asset` |
| `BBMoveSet` | `Assets/CombatData/BlazBlue/<Name>/<Name>_BBMoveSet.asset` |
| `BBAttackData` (one per move) | `Assets/CombatData/BlazBlue/<Name>/BB_<Name>_<MoveId>.asset` |
| Override controller | `Assets/MDI_Characters/Runtime/BlazBlue/<Name>_BB_Override.overrideController` |
| Movelist doc | `Assets/MDI_Characters/Runtime/BlazBlue/<NAME>_BB_MOVELIST.md` |
| Drive behaviour | `Assets/Scripts/BlazBlue/Characters/<Name>Drive.cs` (implements `IBBDrive`) |
| AI profile | `Assets/CombatData/BlazBlue/<Name>/<Name>_BBAI.asset` |
| Clip entries | appended to the CMU `.fbx.meta` files that the table names |
| Definition link | `CharacterDefinition_Cxx_<Name>.asset` → `blazBlueProfile` set (the only edit to that asset) |

```csharp
// NEW: drive plug-in contract
public interface IBBDrive
{
    void OnMatchStart(BBFighterBrain me);
    bool TryStart(BBFighterBrain me, BBButton dir5246, int tick);    // called when D (with direction) is interpreted
    void Tick(BBFighterBrain me, int tick);                          // resource regen, placed objects
    void OnHit(BBFighterBrain me, BBAttackData move, HitResult r);   // bank Momentum/Grip etc.
    void OnOverdrive(BBFighterBrain me, bool active);
    int  Resource { get; } int ResourceMax { get; }                  // for HUD
    void WriteState(ref BBDriveState s); void ReadState(in BBDriveState s); // for replay/rollback hashing
}
```

---

## 9. Game flow and launch features

### 9.1 Modes

| Mode | Existing hook | BlazBlue-mode work |
|---|---|---|
| Arcade | `PlayModeKind.Arcade`, `DemonIdolGameFlowController`, `OfflineMatchSession` | 8 fights against the other girls in random order, the last against a boss-tuned AI (Expert). Heat doesn't carry between fights. There is a continue screen and a score (time, damage, combo, specials, Astral bonus) |
| Versus (local) | `PlayModeKind.LocalVersus` | Pad versus pad, or pad versus keyboard. Two touch players on one device is not supported |
| Training | `PlayModeKind.Training`, `PracticeDojoController` (`ResetPositions`), `TrainingDummyAI`, `FrameDataDisplay`, `HitboxVisualizer` | NEW `BBTrainingMenu` offers: dummy stance (stand / crouch / jump / CPU), guard (none / all / after first hit / random), barrier, tech (none / neutral / back / forward / random), Burst on hit N, gauges (infinite Heat / Burst / Barrier), reset positions (mid / left corner / right corner, plus swap). It also has record and playback of the dummy (5 slots × 10 s, from `BBInputFrame` logs), the hitbox overlay, the frame meter, input display, damage readout (last hit, combo, proration %), and a reversal option for the dummy |
| Tutorial | `OnboardingDirector`, `DemonIdolGameFlowController` "10-Second Autoplay Tutorial" | NEW `BBTutorial`: 20 lessons (move, guard, the 4 buttons, gatling, specials, DD, throws, tech, barrier, Instant Barrier, Rapid Cancel, OD, Burst, Crush Trigger, Counter Assault, air dash, Fatal Counter, combo scaling, okizeme, character Drive). Each lesson is a scripted dummy plus a success check |
| Challenges | — | NEW `BBChallenge` asset per girl: 10 combo trials, from easy (5A > 5B > 5C > 236A) to hard (a corner OD route), validated by move-id sequence and hit count |
| Story | `PlayModeKind.Story`, `StoryCheckpointManager` | Lore is on hold, so ship it locked or as a "Coming soon" tile |
| Online | `PlayModeKind.OnlineUnavailable`, Photon Quantum SDK present | Not at launch (9.12) |

### 9.2 Round flow
- Reuse `MatchRoundReferee`: the 99-second timer (`SettingsPanel.RoundTimer`), `RoundsToWin` default 2, the intro → "ROUND 1" → "FIGHT" → KO / time → winner → next round sequence, and `Rematch` / `StopMatch`.
- BlazBlue additions (NEW `BBRoundRules`, which subscribes to referee events and doesn't edit it):
  - At round start, Heat is 0, Barrier full, Burst at the 4.6 rule, and Drive resources reset.
  - The timer pauses during Overdrive and super-flash.
  - Time out: the higher HP % wins. An exact tie is a draw and both players get a round. In the final round, a tie goes to sudden death with 1 HP.
  - KO types are normal, Distortion finish ("DISTORTION FINISH") and Astral ("ASTRAL FINISH"), each with its own banner and slowdown.
  - Match point is shown by an icon. It enables the Astral check.

### 9.3 HUD (NEW `BBBattleHUD`; Classic `BattleHUDController` stays)

| Element | Position (phone) | Data |
|---|---|---|
| Health with white "lag" bar | top, mirrored per side | `BBFighterState.hp` / `maxHp` (the EXISTING `BattleHUDController` already does a lag bar, so reuse its drain coroutine style) |
| Round pips, timer | top centre | referee |
| Heat gauge (0–100, a mark at 50) | bottom corners, above the touch buttons, safe area aware | `BBGauges.heat` |
| Barrier gauge (0–1000, Danger flashes) | under the health bar | `BBGauges.barrier` |
| Burst gauge (round icon) | next to the portrait | `BBGauges.burst` |
| Drive resource (pips / bar / stance icon) | under Heat | `IBBDrive.Resource` |
| OD timer ring | around the Burst icon while active | |
| Combo counter, damage, "COUNTER" / "FATAL COUNTER" | side of screen near the attacker | sim events (the EXISTING `_comboCounterText` pattern) |
| Input display (training) | left edge | `BBInputFrame` history |

All HUD numbers are read once per render frame from a sim state snapshot struct, not from events, so there is no drift.

### 9.4 Character select
Reuse `CharacterSelectUI`. In BlazBlue mode, show only definitions where `BlazBlueReady` is true, and grey out the others with a "Classic only" tag. Show the archetype, the Drive name and a difficulty from 1 to 5 stars. Colour and costume variants come from the 56 existing variant prefabs (their `MDIVariantLook` handles blend shapes and accessories, and combat code doesn't reference it). Don't edit them.

### 9.5 Intros and victories
Each girl has an intro and victory clip on her BB override (from section 8). Special intros against a specific girl are optional data: `BBCharacterProfile.introVs[]`.

### 9.6 Camera
Reuse `FighterCamera2D`: `TriggerHitImpactZoom`, `TriggerCounterHitImpactZoom`, `TriggerLaunchTrack`, `TriggerWallAccent`, `TriggerThrowCinematic`, `TriggerUltimateEntry/Exit`, `AddTrauma`, `ApplyStageCameraLimits`. In BlazBlue mode the camera is driven from `BBEventQueue` events. Distortion Drives use a short dolly. Reduced motion (`SettingsPanel.ReducedMotion`) turns off zooms and shake, and `ScreenShakeIntensity` scales shake.

### 9.7 Hitstop, shake, VFX, SFX
- Hitstop comes from data (`hitstopAttacker/Defender/OnBlock`, `counterHitStopBonus`). The default is 5/8/9/12 by strength, the same as Classic `MoveSet.CreateAttack`.
- VFX: `HitEffectCoordinator`, `ShockwaveDistortionEmitter`, `InkBloodSplatterManager` and the JMO Cartoon FX packs (existing). Spawn them from the pool through `BBEvent`.
- SFX: `CombatAudioDirector` (existing, used today with `PlayBlock`). Add the NEW keys `bb_hit_A/B/C/D`, `bb_block`, `bb_barrier`, `bb_ib`, `bb_rc`, `bb_burst`, `bb_od`, `bb_dd_flash`, `bb_fatal` and `bb_astral`.
- **Cutscenes:** the decision is **in-engine cinematics instead of MP4s**. Today `FighterController.OnHitboxLanded` plays `ultimate_astral_kai` through `CutsceneManager.PlayCutsceneById` when a `DomainSuper` hits. BlazBlue mode never calls this. DDs and the Astral use NEW Timeline assets (`Assets/Timelines/BB/<Name>_DD1.playable`, etc.) driven by `BBCinematicDirector`, which pauses the sim with `CombatTickEngine.PauseSimulation` / `TriggerSuperfreeze`. The 62 MP4s stay in the repo and are not deleted.

### 9.8 Save and settings
- `SettingsPanel` already stores JSON in `PlayerPrefs` (`PREFS_KEY`). Add the NEW fields `controlScheme` (default 0 = Classic), `bbTouchLayout` (positions), `bbStylish`, `bbInputBuffer` (wired to `InputBufferFrames`) and `bbShowFrameMeter`. Old saves have no such fields, so default them on load.
- Progress (challenges cleared, arcade clears, unlocked colours) goes through the EXISTING `AtomicFileSave` into a NEW `bb_progress.json`.

### 9.9 Accessibility
Reuse the existing settings: `ReducedMotion`, `ReducedFlashing` (dims super-flash and the RC flash), `CaptionsEnabled`, `ColorblindMode` (gauge colours plus shapes), `HighContrastHitboxes`, `HapticsEnabled`, `LeftHandedMode` and `SimpleSpecials` (stylish). Add hold-to-toggle for Barrier on touch, and an option to show motion inputs as on-screen arrows in training.

### 9.10 Localisation hooks
Put every user-facing string (move names, banners, tutorial text, menu labels) in a NEW `BBStrings` table (CSV, or Unity Localization if Izno adds that package later; it isn't in the manifest today). Code reads keys like `bb.move.C01.236236C`. There are no hard-coded English strings in the BlazBlue UI.

### 9.11 AI opponents
- Base: the EXISTING `CombatAIController` (difficulty from `SettingsPanel.AiDifficulty` 0–4) runs on the 60 Hz tick but speaks Classic commands. BlazBlue needs a NEW `BBAIController` that outputs `BBInputFrame` (so the AI is replay- and rollback-safe and uses exactly the player's input path).
- Per girl, a `BBAIProfile` asset sets aggression (0–1), preferred range (m), anti-air rate, a list of pokes, BnB combos as move-id lists (from the challenge assets), reversal use, barrier use, Burst rules (use at combo hit ≥ 6 if the gauge is full), OD rules and Astral rules.
- Reaction times per difficulty: Easy 30 ticks, Normal 20, Hard 14, Expert 10, DemonLord 8, with random spread. Combo drop chance is 40% / 20% / 8% / 2% / 0%.
- Behaviour shape: a utility-scored state machine (Neutral / Pressure / Defend / Punish / Okizeme) evaluated every 4 ticks.
- The training dummy is the separate `BBTrainingDummy`, with no AI unless "CPU" is chosen.

### 9.12 Online (optional; rollback-ready notes)
- Photon Quantum is in the repo (`Assets/Photon/Quantum`, `QuantumCombatBridge`, `QuantumFighterInput`) but isn't connected to the homegrown sim. Online is **not** part of launch.
- To keep the door open:
  1. The sim reads only `BBInputFrame`.
  2. All sim state lives in plain structs (`BBFighterState`, `BBGauges`, `BBComboState`, `BBDriveState`, projectile pool slots) that can be **copied out and restored** (`SaveState(ref BBSimSnapshot)` / `LoadState(in BBSimSnapshot)`).
  3. There is no `UnityEngine.Random` in the sim; a NEW `BBRng` (xorshift32) is seeded per match.
  4. Floats are deterministic on one device, but **not guaranteed across ARM and x86 or compilers**. For real cross-play rollback, switch `BBReal` to fixed point (Q16.16 int) and replace `CharacterController.Move` with sim-owned integer positions (BlazBlue mode already integrates its own positions from `BBMotionTrack`, so the CharacterController is just a follower).
  5. Presentation must be able to re-simulate up to 8 ticks without side effects (events are queued and confirmed only on the confirmed tick).
- Write a `RollbackQualityProbe`-style harness (one already exists for Quantum) for the BlazBlue sim: run the sim, take snapshots, roll back 7 ticks, re-simulate, and compare hashes.

---

## 10. Testing and validation

### 10.1 EditMode tests (NEW assembly `MDI.BlazBlue.EditModeTests`, referencing `MDI.BlazBlue.Core`; see 11.4)
Model them on the EXISTING `AkioMotionEditModeTests` (`MDI.Akio.EditModeTests.asmdef` referencing `MDI.Akio.asmdef`; NUnit `[Test]`, pure static calls, no scene). Tests that need `BBAttackData` assets or `FighterController` (data validation, PlayMode) live in Assembly-CSharp-Editor folders (`Assets/Scripts/BlazBlue/Editor/Tests/`), because an asmdef can't reference Assembly-CSharp.

| Suite | Must cover |
|---|---|
| `BBMotionTests` | 236 / 214 / 623 (plus the 323 shortcut), 41236, 63214, 236236, 214214, 632146, 360 (both directions, the jump-start edge case), 22, [4]6, [2]8, stale-window rejection, facing-left mirroring, negative edge (held ≥ 3 ticks), priority order from 3.8 (360 > 623 > 236 > normal), and consume-only-on-start |
| `BBChordTests` | B+C within 3 ticks is a throw, B then C at 4 ticks is a gatling, A+B while guarding is Barrier and in neutral is Crush Trigger, ABCD in hitstun is Burst and in neutral is OD |
| `BBCancelTests` | Revolver order, A-repeat cap of 3, once-per-chain mask, jump cancel on hit only, special/DD cancels, the RC window (contact required), RC once per combo |
| `BBGaugeTests` | Heat gain and costs, low-HP regen, Barrier drain, regen delay, Danger entry and exit, Burst regen and bait penalty, OD duration formula |
| `BBDamageTests` | P1 applied once, P2 product, same-move penalty, min damage floor, guts table, Danger ×1.25, stylish ×0.8, integer floor, chip rules, chip-KO only from DD |
| `BBHitstunTests` | Decay table thresholds, Fatal Counter shift, CH hitstun ×1.5, gravity scaling cap |
| `BBCollisionTests` | Touching edges don't hit, facing flip, hitIndex once-only, trade, throw beats strike, push tie-break, corner push, airborne push |
| `BBDeterminismTests` | A 600-tick scripted input log run 100 times gives identical per-tick hashes, and snapshot → 7 ticks → restore → re-sim gives identical hashes |
| `BBAllocationTests` | `Recorder.Get("GC.Alloc")` over 600 simulated ticks reads **0** |
| `BBDataTests` | Every `BBAttackData` in `Assets/CombatData/BlazBlue/**`: advantage fields match the computed values ±1, the box track `tickCount` equals S+A+R, the bake hash is current, the clip exists and is 24 fps, move ids are unique per set, cancel targets exist |

### 10.2 PlayMode tests (NEW, folder `Assets/Scripts/BlazBlue/Tests/Play/`)
- These tests touch `FighterController` (Assembly-CSharp), so either turn on Test Runner's "Enable playmode tests for all assemblies" (the `testables` setting), or run them as an Editor batch harness, as the EXISTING `Assets/Scripts/Editor/MatchSimulationVerificationTest.cs` does.
- Load the training scene, spawn Akio versus Aika in BlazBlue mode, feed scripted `BBInputFrame` logs, and assert outcomes: 5A hits at tick 6, a 5A > 5B > 5C > 236A combo deals the expected damage, a throw tech at tick 10 is teched and at tick 14 is not, and Burst during a combo pushes away.
- **State hash per tick:** NEW `BBSimHash` (FNV-1a over the fighter structs, gauges, combo state and positions rounded to 0.1 mm). Training and debug builds can print it. Golden logs live in `Assets/Scripts/BlazBlue/Tests/Golden/*.bbrec`.
- Reuse the EXISTING `MatchReplaySession.cs` concept, but record `BBInputFrame` instead of Classic commands.

### 10.3 Classic regression (proves nothing already built changed)
1. **Static diff gate** (NEW `tools/validate_blazblue.py --classic-guard`):
   - Compute the git diff of the PR against `origin/main` at the commit the work started from.
   - **Fail** if any of these changed: `Assets/Scripts/Combat/Hitbox.cs`, `Hurtbox.cs`, `AkioBodyFit.cs`, `AkioPresentation.cs`, `RapidCancel.cs`, `BarrierBlockSystem.cs`, `PsychicBurstSystem.cs`, `JuggleEngine.cs`, `Assets/Scripts/Data/AttackData.cs`, `MoveSet.cs`, `AkioClipLibrary.cs`, `Assets/Scripts/Input/InputBuffer.cs`, `CommandParser.cs`, `Assets/MDI_Characters/Runtime/AkioCore/**`, `MDI_KI_Melee_Fighter.controller`, `*_KI_Melee_Override.overrideController`, `Assets/CombatData/{Akio,Aika,UnityChan,PicoChan}/**`, `Assets/MDI_Characters/Runtime/*/…ClipLibrary.asset`, any `*.prefab` or avatar, `Assets/Prefabs/Characters/Variants/**`, or any **existing** `clipAnimations` item in any `.fbx.meta` (compare item by item by `name`; new items are allowed).
   - **Allow, with checks:**
     - `FighterController.cs`: only the hook lines from 11.2 may change. Check that the diff adds lines and changes no others, apart from the single guarded `if`.
     - `CharacterDefinition.cs`: only the appended BlazBlue block.
     - `CharacterDefinition_Cxx_*.asset`: only the `blazBlueProfile` line.
     - `SettingsPanel.cs`: only appended fields and properties.
     - `MobileFighterInput.cs` / `TouchVirtualControls.cs`: only an `enabled` guard, if needed.
2. **Existing validators still pass.** `tools/validate_akio_movelist.py` and `tools/validate_ki_melee.py` both use `ROOT = Path("/workspace")`. Run them from a checkout at that path, or add a NEW `--root` argument in a *new* wrapper script (`tools/run_classic_validators.sh`) and leave the originals unedited. `validate_akio_movelist.py` keeps an `ALLOWED_MODIFIED` set, so the BlazBlue PRs must not touch anything outside it *except* new files, which the script is expected to ignore (confirm by running it).
3. **Existing EditMode tests stay green**, especially all 12 `AkioMotionEditModeTests`.
4. **Classic golden replays:** before the first BlazBlue PR, record 6 Classic matches (Akio, Aika, UnityChan, PicoChan, Fina and one more) as Classic input logs. Write a NEW tiny Classic recorder that taps `InputBuf.Enqueue` and `SetMoveInput`. After each PR, replay them with `ControlScheme = Classic3Button` and assert identical HP, positions (±1 mm) and state sequences.
5. **Manual Classic smoke test**, from the existing movelist checklists: Akio's 236P spends meter; with a full meter, raw P is a jab; Fina's full-meter P still fires her projectile and her empty-meter P whiffs; Aika's [4]6P side kick works; PicoChan's 236P lariat fires no laser.

### 10.4 Validator script (NEW `tools/validate_blazblue.py`)
- `--character C01` checks that character's BlazBlue assets:
  - every move in the `<NAME>_BB_MOVELIST.md` table has a `BBAttackData` and the reverse
  - each clip name exists in the named `.fbx.meta` with matching first/last frames
  - the clip entry was appended, not edited
  - the 24 fps assumption: each CMU meta's take `lastFrame` is consistent with the `_Full` clip
  - advantage maths and box-track lengths
  - no move uses take `88_06`
  - the profile is linked from the definition
  - the override controller's `m_Controller` guid is `b7c4e1a09d2f4c6e8a1b3d5f7091c2e4`
- `--classic-guard`: section 10.3.1.
- `--all`: everything, used in CI. It exits non-zero on failure and prints a short table.
- Parse YAML with a light regex approach, like the existing validators, so there is no PyYAML dependency.

### 10.5 Per-character Unity checklist (sign off each)
1. Reimport only that girl's CMU FBX files. Each new clip is 24 fps, and its length is (last − first) / 24 s.
2. Preview the fall and get-up on her avatar: no sinking, and her feet are within 2 cm of the floor.
3. Walk loop slide is under 3 cm, and every loop is seamless.
4. In training with the hitbox overlay and frame meter, play **every** move in her table: startup, active and recovery match, and on-block matches the table.
5. Specials come out from keyboard, pad and touch, including 360 without jumping.
6. Gatling routes and jump cancels work. RC, OD, Exceed Accel, Burst, CA, CT, Barrier, IB and throw tech work.
7. Each DD and the Astral (if on) play their in-engine cinematic and give the KO banner.
8. Intro and victory play.
9. Mirror match: no box asymmetry on either side.
10. **Switch to Classic:** she plays exactly as before (section 10.3.5).
11. Run the AI on Normal and Expert for 3 matches each with no errors in the log.

### 10.6 Device performance tests
- Reference devices: 1 mid Android (Snapdragon 7-series class, 6 GB), 1 low Android (4 GB, Mali), iPhone 12 and iPhone 15.
- Scenario: a 3-round mirror match with both AI on Expert, the most VFX-heavy girl (Cultist with orbs, or Aika with nails), and OD plus DD.
- Pass when: `BB.Tick` p99 ≤ 1.0 ms (mid Android), 0 GC per tick, render p95 ≤ 16.6 ms, the device doesn't thermal-throttle below 60 fps for 10 minutes, memory ≤ 1.2 GB on the 4 GB device, and touch latency (touch to first sim tick) ≤ 2 ticks, measured with the input display.
- Capture a Unity Profiler session plus the Android GPU Inspector or Xcode Instruments, and attach the screenshots to the PR.

---

## 11. Implementation order and milestones

### 11.1 Branch and PR rules
- Work from `main` only. Create `bb/<milestone>` or `bb/char-<name>` branches, open one PR per milestone or character, run the validators and tests, merge, then **delete the branch**.
- Small PRs (≤ about 1,500 changed lines of code plus assets). Each PR description lists the files changed, the test results, what was checked in Unity, and what wasn't.
- The repo is about 2.9 GB with **no Git LFS** (no `.gitattributes` on `main`; the local `.git` pack is about 1.4 GB). Don't add large binaries. Timelines and assets are YAML. If audio or video must be added, raise it with Izno first.
- Never commit secrets. `Assets/Scripts/Editor/AndroidKeystoreConfig.cs` already reads signing values from environment variables (`ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`). Keep it that way.

### 11.2 The only edits to existing code (guarded hooks)

```csharp
// FighterController.cs — add ONE public field and ONE guard as the first line of HandleCombatTick.
// NEW field (append near other public fields):
[System.NonSerialized] public bool externalSimulation = false;   // true only while a BBFighterBrain drives this fighter

private void HandleCombatTick(int currentTick)
{
    if (externalSimulation) return;          // NEW guard: BlazBlue sim owns this fighter; Classic path unchanged when false
    // ... existing body unchanged ...
}
// Update() also has an early superfreeze return; add the same `if (externalSimulation) return;` as its first line.
```

- `CharacterDefinition.cs`: append the block from 5.4.
- `SettingsPanel.cs`: append `controlScheme` and the 9.8 fields. Add `public static ControlScheme ControlScheme => Instance != null ? (ControlScheme)Instance._current.controlScheme : ControlScheme.Classic3Button;` and wire `InputBufferFrames` into BlazBlue only.
- `HomegrownCombatDirector.cs`: **no edit needed**. NEW `BBMatchBootstrap` subscribes after `InitializeHomegrownMatch` and, when the scheme is BlazBlue and both definitions are `BlazBlueReady`, does the following:
  - adds a `BBFighterBrain` to each fighter and sets `externalSimulation = true`
  - disables that fighter's Classic `MobileFighterInput`, `Hitbox` and `Hurtbox` colliders and the `AkioBodyFit` effects (it restores them on disable)
  - swaps in the BB animator as in 7.7
  - mirrors HP for the referee: `fc.currentHealth = fc.maxHealth * hp / maxHp` (the referee checks `currentHealth <= 0` and compares HP)
  - calls `fc.SetState(FighterState.Dead)` on KO.

  If a hook into the director is unavoidable, it must be a single `OnMatchInitialized` event invoke at the end of `InitializeHomegrownMatch`.

### 11.4 Assembly layout (follow the existing `MDI.Akio` pattern)
- The repo already splits pure logic into `Assets/MDI_Characters/Runtime/AkioCore/MDI.Akio.asmdef` (`MotionMatcher`, `CancelQuery`), which Assembly-CSharp references. Its tests are in `MDI.Akio.EditModeTests.asmdef`.
- **NEW `MDI.BlazBlue.Core.asmdef`** (`Assets/Scripts/BlazBlue/Core/`) holds Unity-light, pure code: `BBInputFrame`, `BBMotionMatcher` (it references `MDI.Akio` for `MotionEdge`/`MotionMatcher`), `BBCollision` and box structs, damage and proration maths, gauge maths, hitstun decay, `BBRng`, `BBSimHash` and the snapshot structs. There are no references to `AttackData` or `FighterController`.
- **NEW `MDI.BlazBlue.EditModeTests.asmdef`** references `MDI.BlazBlue.Core` and holds most of the tests in 10.1.
- **Assembly-CSharp (no asmdef)**, under `Assets/Scripts/BlazBlue/`: `BBAttackData : AttackData`, `BBMoveSet`, `BBCharacterProfile`, `BBSystemTuning`, `BBFighterBrain`, `BBMatchSim`, `BBMatchBootstrap`, the drives, HUD, AI and the animation driver. This must sit in Assembly-CSharp because it derives from or uses `AttackData` and `FighterController`, and because `CharacterDefinition` references `BBCharacterProfile`. Asmdef code can't reference Assembly-CSharp.
- Editor tools (`BBBoxEditorWindow`, the bake, data-validation tests) go in `Assets/Scripts/BlazBlue/Editor/` (Assembly-CSharp-Editor).

### 11.3 Milestones

| # | Milestone | Main deliverables | Acceptance criteria |
|---|---|---|---|
| M0 | Baseline and safety net | Classic golden recorder and 6 golden logs; `tools/validate_blazblue.py --classic-guard`; `tools/run_classic_validators.sh`; a CI job (or Editor batch script) running all tests | Classic goldens replay identically twice; the existing validators and the 12 Akio tests pass |
| M1 | BlazBlue scaffolding | Assembly layout from 11.4, the `ControlScheme` setting, `BBSystemTuning`, `BBAttackData`, `BBMoveSet`, `BBCharacterProfile`, the CharacterDefinition link, the FighterController guard, `BBMatchBootstrap`, `BBFighterBrain` (idle/walk/jump only) | Scheme Classic: the goldens are identical. Scheme BlazBlue with no profiles: falls back to Classic with a log warning |
| M2 | Input | `BBInputSampler`, `MDI_BB_Input.inputactions`, `BBInputFrame`/`History`, `BBMotionMatcher`, `BBCommandInterpreter`, `TouchControls_BB.prefab` and the layout editor | All `BBMotionTests` and `BBChordTests` green; touch DoD item 6 met on one phone |
| M3 | Sim core and collision | `BBMatchSim` tick order, physics-free collision, pushbox, baked tracks, `BBHitboxOverlay`, `BBFrameMeter`, `BBBoxEditorWindow` (6.11 prompt) | Collision, determinism and allocation tests green; perf budget met on the reference Android |
| M4 | Systems I | Revolver Action, movement (dash types, double jump, air dash, super jump), throws and tech, Counter Hit and Fatal Counter, proration, hitstun decay, gravity, bounces, wakeup and tech, chip | Cancel, damage and hitstun tests green; a sample "test girl" (Akio data) can do a full ground-to-air combo |
| M5 | Systems II | Heat, Barrier and Danger, IB, RC, Burst, OD and EA, Crush Trigger and guard crush, Counter Assault, DD super-flash, Astral (flag), `BBRoundRules` | Gauge tests green; every system triggers in training with correct HUD feedback |
| M6 | Animation driver | `BBAnimationDriver` (Playables), `<Name>_BB_Override` pattern, `BBClipSequence`, `BBCinematicDirector` (Timeline) | No allocation mid-match; the attack clip seeks in sync with the frame meter; the Classic animator is restored when switching back |
| M7 | Akio BlazBlue | Section 8.3 complete, `AKIO_BB_MOVELIST.md`, AI profile, 10 challenges | Checklist 10.5 signed; Classic Akio golden identical |
| M8 | Aika BlazBlue | 8.4 | same |
| M9 | UnityChan BlazBlue | 8.5 | same |
| M10 | PicoChan BlazBlue | 8.6 | same |
| M11 | Fina, SanpakuChan, Cultist, Reika, StellarWitch C07 (one PR each) | 8.7, measure first | same, for each |
| M12 | HUD, modes, AI | `BBBattleHUD`, arcade, versus, the training menu, tutorial, challenges, character select filter, `BBAIController` | All modes are playable start to finish on phone and pad |
| M13 | Polish and launch | VFX/SFX pass, accessibility, localisation keys, device perf tests (10.6), store builds | Definition of done (1.4) is fully met |

---

## 12. Risks and open decisions for Izno

### 12.1 Risks (found in the repo)

| # | Risk | Impact | Mitigation in this spec |
|---|---|---|---|
| R1 | Classic hit detection uses `OnTriggerEnter` on a **50 Hz** physics step while combat ticks at **60 Hz** | Missed or double hits, not deterministic | BlazBlue mode is physics-free (6.6–6.7). Classic stays as is |
| R2 | Bone-follow hitboxes read the rendered pose in `LateUpdate` | Results depend on frame rate | Baked boxes in BlazBlue mode (6.5) |
| R3 | For C05–C08, C10, C12 and C14, `CharacterDefinition.animatorController` points to vendor demo controllers or `MDI_FighterTemplate`, and `HomegrownCombatDirector` assigns it at match start. The KI doc says their prefabs use `MDI_KI_Melee_Fighter` | In Classic matches those girls may not run the KI controller at all | **Verify in Play Mode.** BlazBlue mode assigns `<Name>_BB_Override` itself, so it is unaffected. Fixing Classic is Izno's call (it would change Classic behaviour) |
| R4 | The repo is about 2.9 GB with no Git LFS | Slow clones, push limits | No new large binaries; consider LFS for audio and video later (Izno decides) |
| R5 | `KI_MELEE_ANIMATION.md` assumes the KI clips are 30 fps when computing state speeds | KI-based moves may play at the wrong speed | Check the KI clip lengths on import; BlazBlue mode computes speed from real clip length |
| R6 | Docs say "Throw: P+M", but in code P+M goes to `ThreeButtonCommand.GrabBreak` (Burst when available). The throw is Space/T, pad A, or a close 66 | Possible confusion in Classic; not fixed here | BlazBlue uses B+C. Flag it to Izno and don't change Classic |
| R7 | `SettingsPanel.InputBufferFrames` (default 4) isn't read by the sim (Classic leniency is a hard-coded 5) | The setting does nothing in Classic | Wire it for BlazBlue only |
| R8 | Validators hard-code `ROOT = Path("/workspace")` | Fail on other checkouts | A wrapper script; the originals stay unedited |
| R9 | `ROSTER.md` describes an old 18-fighter clan roster with male fighters | Misleading | Ignore it (lore is on hold). This spec's roster table is the plan |
| R10 | Floats aren't bit-exact across devices | Blocks future online play | `BBReal` alias and the fixed-point plan (9.12) |
| R11 | Reika's Kiki setup (kinematic Rigidbody, disabled capsule) and StellarWitch's hip remap need a Unity check | Those girls could be blocked | Check them before building. Slot 9 is StellarWitch |
| R12 | Adult-sized CMU actors on small girls | Punches look short | Hitboxes are baked from her own bones; choose takes per girl |
| R13 | The Classic `CutsceneManager` plays an MP4 on a `DomainSuper` hit | Not used by BlazBlue | In-engine Timeline cinematics |

### 12.2 Open decisions (please choose)
1. **Slot 9:** decided. C07 StellarWitch. Not Akane, not Muryotaisu.
2. **Default scheme at launch:** Classic3Button or BlazBlue4Button? (The spec keeps Classic as the default until you flip it.)
3. **Astral Heats:** on (arcade and versus) or off?
4. **Stylish mode** at launch: yes or no? If yes, should D move to A+D in stylish?
5. **HP scale:** 11,000 (BlazBlue-like) versus keeping 1,000 with decimal damage.
6. **Neutral ABCD:** Overdrive (the default here) or Gold Burst?
7. **Classic animator mismatch (R3):** leave Classic as is, or fix the definitions (a behaviour change for Classic)?
8. **Git LFS** for future audio and video?
9. **Online:** keep it out of launch (the spec assumes yes)?
10. Each girl's **Drive concepts** in section 8: approve or rename.

---

## 13. Master copy-paste prompt for the AI coder

```text
ROLE
You are the implementation engineer for "My Demon Idol: Iron Legacy", a Unity 6000.6.8f1 URP 2.5D anime fighter
(private repo iznogit-hub/My-Demon-Idol, branch main). Implement the BlazBlue-style 4-button system (A weak, B medium,
C heavy, D Drive) described in docs MDI_BLAZBLUE_IMPLEMENTATION_SPEC.md. Read that spec fully first, then read the real code:
Assets/Scripts/Combat/{FighterController,Hitbox,Hurtbox,CombatTickEngine,JuggleEngine,BarrierBlockSystem,PsychicBurstSystem,
RapidCancel,DomainExpansion,OkizemeWakeup,PushbackSolver,HomegrownCombatDirector,MatchRoundReferee,ProjectileFactory,
AkioPresentation,AkioBodyFit,CombatAIController,FrameDataDisplay,HitboxVisualizer}.cs, Assets/Scripts/Data/{AttackData,MoveSet,
CharacterDefinition,AkioClipLibrary}.cs, Assets/Scripts/Input/{InputBuffer,CommandParser,MobileFighterInput,TouchVirtualControls}.cs,
Assets/MDI_Characters/Runtime/AkioCore/**, the four *_MOVELIST.md files, KI_MELEE_ANIMATION.md, tools/validate_*.py.

NON-NEGOTIABLE
1. Nothing already built may break. Classic 3-button P/M/S and the depth movelists (akioDepthMovelist on C01; depthMovelist on
   C02, C04, C09) must behave identically. BlazBlue runs only when SettingsPanel ControlScheme == BlazBlue4Button AND the
   CharacterDefinition has blazBlueProfile. Default stays Classic3Button.
2. Add-only. Never delete files. Allowed edits to existing files: FighterController (one NonSerialized bool externalSimulation +
   one early-return guard in HandleCombatTick and Update), CharacterDefinition (append blazBlueProfile block), SettingsPanel
   (append fields), CharacterDefinition_Cxx assets (blazBlueProfile line only). Everything else is NEW under
   Assets/Scripts/BlazBlue/**, Assets/CombatData/BlazBlue/**, Assets/MDI_Characters/Runtime/BlazBlue/**, Assets/Timelines/BB/**, tools/.
3. Never touch avatars, prefabs, the 56 variants, MDI_KI_Melee_Fighter.controller, existing *_KI_Melee_Override controllers,
   existing AttackData/MoveSet/ClipLibrary assets, or any existing clipAnimations item in a .fbx.meta. New clips = NEW appended
   clipAnimations items named <Prefix>_BB_<MoveId> (or reuse an existing depth clip by reference). 24 fps CMU clips;
   sunk-hip takes use keepOriginalPositionY: 0 and/or firstFrame >= 8. No flips, cartwheels, tiny moves, or take 88_06.
4. All playable characters are girls. Skip C03 Ren; exclude C11 Casual1 and C13 SapphiArtchan.
5. main only; short-lived branches bb/<milestone> or bb/char-<name>; one PR each; merge; delete branch. No secrets in code.
   Repo has no Git LFS (~2.9 GB): add no large binaries.

ARCHITECTURE TO BUILD (spec sections in brackets)
- Input [3]: Input System actions (keyboard J/K/L/I = A/B/C/D, ; = BC, U = AB, O = ABC, P = ABCD; pad West/North/East/South =
  A/B/C/D, LB = AB, RB = BC, LT = ABCD, RT = ABC; touch diamond A/B/C/D + macro buttons, 1920x1080 reference, >= 9 mm buttons,
  SafeAreaFitter, EnhancedTouch). BBInputFrame {held, pressed, released, numpad}; 64-tick history; BBMotionMatcher (41236, 63214,
  236236, 214214, 632146, 360, 22, [2]8) on top of MDI.Akio.MotionMatcher; priority order and chord window 3 ticks; buffer 5;
  negative edge for specials; motion consumed only when the move starts.
- Systems [4]: BBSystemTuning asset with every number; Revolver Action; run/step/hop dash, double jump, air dash, super jump;
  Heat (RC 50, DD 50, CA 50, CT 25, AH 100); Barrier gauge + Danger + Instant Barrier; Burst; Overdrive + Exceed Accel;
  Crush Trigger guard crush; Counter Assault; Distortion Drives; optional Astral; throws 6BC/4BC/j.BC with 13-tick tech;
  Counter Hit and Fatal Counter; P1/P2/same-move/min-damage/guts proration with integer HP (11,000 default); hitstun decay by
  combo time; gravity scaling; once-per-combo wall/ground bounce; air/ground tech; chip rules. Implement as NEW components
  (BBGauges, BBRapidCancel, BBBarrier, BBBurst, BBThrowResolver, BBTech, BBComboState) — do not edit the Classic equivalents.
- Data [5]: BBAttackData : AttackData (moveId, button, kind, motion, guard, attribute, invuln windows, fatalCounter, bounces,
  untechable ticks, minDamagePct, chipPct, heat cost/gain, gatlingInto, cancel flags, followUps, BBBoxTrack, BBMotionTrack,
  clip + window, BBEvent[]); BBMoveSet with load-time index tables (no string compares at runtime); BBCharacterProfile;
  validator asserts advantage fields == computed +/-1.
- Collision [6]: physics-free 2D AABB, baked per-tick boxes (bake from AnimationClip.SampleAnimation; hash-checked), single
  BBMatchSim tick in fixed P1->P2 order, collect-then-apply, throw beats strike, zero GC, pooled projectiles, ProfilerMarkers,
  BBHitboxOverlay, BBFrameMeter, BBBoxEditorWindow. Seed body boxes from spec 6.4 measurements and sizing rules.
- Animation [7]: per-girl <Name>_BB_Override (parent guid b7c4e1a09d2f4c6e8a1b3d5f7091c2e4) for locomotion/guard/reactions/
  intro/victory; attacks through BBAnimationDriver (Playables layer mixer, clip playables seeked each tick:
  time = tickInMove/60 * ((last-first)/24) / (totalTicks/60)); root motion off, travel from BBMotionTrack; foot slide < 3 cm.
- Characters [8]: build in order Akio (rushdown puncher, Drive Iron Rush), Aika (karate zoner, Resonance Nail), UnityChan
  (aerial hopper, Aero Hop), PicoChan (close grappler, Clinch), then Fina, SanpakuChan, Cultist, Reika, and C07 StellarWitch
  (slot 9, Izno 18 Nov 2026; not Akane or Muryotaisu). Use the movelist tables in section 8; reuse the documented CMU windows for the first four; for the rest measure the
  avatar first and use mostly different CMU takes (search hints in 8.7). Each girl: BBCharacterProfile, BBMoveSet, BBAttackData
  per move, IBBDrive implementation, override controller, <NAME>_BB_MOVELIST.md, AI profile, 10 challenges.
- Game [9]: BBBattleHUD (HP, Heat, Barrier, Burst, Drive resource, OD ring, combo/CH/FC banners), arcade, versus, training menu
  (record/playback, frame meter, hitbox view, gauges), tutorial (20 lessons), challenges, character select filter
  (BlazBlueReady), in-engine Timeline cinematics for DD/AH instead of MP4, settings fields, accessibility, localisation keys,
  BBAIController emitting BBInputFrame, rollback-ready state snapshots (online not in launch).
- Hook [11.2]: BBMatchBootstrap sets externalSimulation, disables Classic input/colliders/body-fit on that fighter, swaps the
  animator, mirrors HP to FighterController.currentHealth (scaled to maxHealth) and SetState(Dead) on KO so MatchRoundReferee works;
  restores everything when leaving BlazBlue mode.

TESTS AND GATES [10]
- EditMode suites: motions, chords, cancels, gauges, damage, hitstun, collision, determinism (100x identical hashes; rollback
  7 ticks), allocation (0 B over 600 ticks), data validation. PlayMode scripted matches with golden .bbrec logs.
- Classic regression: tools/validate_blazblue.py --classic-guard (forbidden-file diff list in 10.3), existing
  tools/validate_akio_movelist.py and tools/validate_ki_melee.py via a wrapper (they hard-code ROOT=/workspace), all 12
  AkioMotionEditModeTests, Classic golden replays identical, manual Classic smoke list.
- Per-girl Unity checklist 10.5 and device perf tests 10.6 (BB.Tick <= 1.0 ms mid Android, 0 GC, 60 fps).

ORDER [11.3]
M0 safety net -> M1 scaffolding -> M2 input -> M3 sim+collision -> M4 systems I -> M5 systems II -> M6 animation driver ->
M7 Akio -> M8 Aika -> M9 UnityChan -> M10 PicoChan -> M11 new girls (one PR each) -> M12 HUD/modes/AI -> M13 polish/launch.
Do not start a milestone until the previous one's acceptance criteria pass.

REPORTING
For every PR: list files added/changed, test and validator output, what you verified inside Unity (and on which device),
and what you could NOT verify. Never claim something works in-game unless you ran it. Raise the open decisions in spec 12.2
instead of guessing. Slot 9 is already decided: C07 StellarWitch. Remaining defaults: Classic scheme, Astral off for this pass, stylish off, HP 11,000, neutral ABCD is Overdrive, Classic animator mismatch left as-is, no Git LFS.
```

---

*End of specification.*
