# MDI LAUNCH 2/4: CORE RULES VERIFICATION REPORT

**Generated:** 2026-11-23 02:12:38 UTC
**Raw Log File:** `Logs/fightfeel/raw_core_rules.log`
**Total Simulation Ticks Recorded:** 679

## 1. Executive Summary & Gates Verification

| Gate # | Requirement | Specification | Measured Outcome | Status |
|---|---|---|---|---|
| 1A | Throws (C6) | 7f startup, 3 active, 30f Thrown duration, 1300 dmg, knockdown | P2 entered Thrown 30f, transitioned to DownWait, took 1300 dmg, recovered to Neutral | PASS |
| 1B | Throw Tech | 15f tech window with B+C, push apart to neutral, 0 dmg | P2 broke throw on frame 5, both returned to Neutral, 0 damage taken | PASS |
| 2A | Guard: 2B vs Stand | Low attack hits standing block | Clean hit, damage dealt, P2 entered Hitstun | PASS |
| 2B | Guard: 2B vs Crouch | Low attack blocked crouching | Blocked, 0 damage, P2 entered Blockstun | PASS |
| 2C | Guard: j.C vs Crouch | Overhead attack hits crouching block | Clean hit, damage dealt, P2 entered Hitstun | PASS |
| 2D | Guard: j.C vs Stand | Overhead attack blocked standing | Blocked, 0 damage, P2 entered Blockstun | PASS |
| 3 | OTG Invulnerability | DownWait + Wakeup strike/throw invulnerable unless otg=true | 5A whiffs on DownWait and Wakeup (0 damage dealt) | PASS |
| 4 | Input Buffer | Move buffered during hitstop executes frame 1 of recovery | 5B pressed in 5A hitstop executed immediately on recovery | PASS |
| 5 | Counter Hit | Hit during 236A startup gives extra hitstun and +5 hitstop | Level 4 counter-hit yielded hitstop >= 13 (+5) and hitstun >= 18 (+5) | PASS |
| 6 | Round 2 Reset | Referee reset starts round 2 with both at 10000 HP | Both reset to 10000 HP in Neutral, hitstop/stun cleared, timer=99 | PASS |
| 7 | Combat Audio SFX | CombatAudioDirector logs hit SFX on every hit | Verified [CombatAudioDirector] PlayHitLight SFX logged on hit | PASS |

## 2. Test Execution Ticks Breakdown

- **Throw & Knockdown Ticks:** 237
- **Throw Tech Ticks:** 100
- **High/Low Guard Ticks:** 130
- **OTG Invulnerability Ticks:** 139
- **Input Buffer Ticks:** 28
- **Counter Hit Ticks:** 23
- **Round 2 Reset Ticks:** 11
- **Combat Audio SFX Ticks:** 11

## 3. Strict Determinism & Zero Fake Verification

- All tests executed via `InputTestFixture` queuing real `KeyboardState` events on virtual Keyboard.
- Zero calls to TryStartMoveId, InjectFrame, or direct fighter state/health writes in test code.
- Spacing achieved via real walk inputs (`Key.D`).
- All state changes and damage resolved authoritatively through `BBMatchSim`.
