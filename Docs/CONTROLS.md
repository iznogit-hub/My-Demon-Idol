# My Demon Idol — Fighting Game Controls Reference (BlazBlue System)

This guide documents the authoritative controls, hardware bindings, macro shortcuts, and input notation conventions for *My Demon Idol* (BlazBlue 4-button combat path).

---

## 1. Fighting Game Directional Notation (Numpad System)

*My Demon Idol* uses standard fighting game numpad notation. Directions correspond to the 9 numbers on a computer keyboard numeric keypad, assuming your fighter is **facing RIGHT (Player 1 side)**:

```
 7 (Up-Back)    8 (Up)      9 (Up-Forward)
 4 (Back)       5 (Neutral) 6 (Forward)
 1 (Down-Back)  2 (Down)    3 (Down-Forward)
```

> [!NOTE]
> All directional notations are **facing-relative**:
> - **6 (Forward)** always means *towards the opponent*.
> - **4 (Back)** always means *away from the opponent* (also holds **Standing Guard**).
> - **1 (Down-Back)** holds **Crouching Guard** (blocks Lows and Mids; Overheads must be blocked standing with 4).
> - If your fighter switches sides to the right facing left, pressing Left becomes 6 (Forward) and pressing Right becomes 4 (Back).

### Common Motion Notations
- **236 (Quarter Circle Forward / QCF)**: Down (2) $\to$ Down-Forward (3) $\to$ Forward (6).
- **214 (Quarter Circle Back / QCB)**: Down (2) $\to$ Down-Back (1) $\to$ Back (4).
- **623 (Dragon Punch / "DP")**: Forward (6) $\to$ Down (2) $\to$ Down-Forward (3).
- **22 (Down-Down)**: Down (2) $\to$ Neutral (5) $\to$ Down (2).
- **236236 (Double Quarter Circle Forward)**: Standard Distortion Drive motion.
- **632146 (Half Circle Back then Forward / HCF)**: Advanced Distortion Drive command.
- **66 / 44**: Forward Dash / Backdash.

---

## 2. Four-Button Layout

The BlazBlue combat system is built around four primary buttons:

| Button | Role | Description |
|:---:|---|---|
| **A** | Light Attack | Quick, low-recovery jabs and checks. Great for anti-mash and tick throws. |
| **B** | Medium Attack | Solid midrange pokes, combo extenders, and advancing strikes. |
| **C** | Heavy Attack | Long-range weapons, launchers, sweeps, and knockdown starters. |
| **D** | Drive | Unique character-specific trait/mechanic (e.g. Akio's Iron Rush, Aika's Resonance Nail, StellarWitch's Star Field). |

---

## 3. Player 1 Bindings (Keyboard & Gamepad)

### Movement (P1)
| Action | Keyboard Key | Gamepad (PS / Xbox) |
|---|:---:|:---:|
| **Walk Forward** | `D` | `D-Pad Right` / `Left Stick Right` |
| **Walk Backward / Guard** | `A` | `D-Pad Left` / `Left Stick Left` |
| **Crouch** | `S` | `D-Pad Down` / `Left Stick Down` |
| **Jump Neutral** | `W` | `D-Pad Up` / `Left Stick Up` |
| **Jump Forward** | `W + D` | `Up-Right Diagonal` |
| **Jump Backward** | `W + A` | `Up-Left Diagonal` |
| **Forward Dash** | `D, D` (Double-tap) | `Right, Right` |
| **Backdash (Invulnerable)** | `A, A` (Double-tap) | `Left, Left` |

### Combat Actions & Attacks (P1)
| Command | Button | Keyboard Key | PlayStation Pad | Xbox Controller |
|---|:---:|:---:|:---:|:---:|
| **Light Normal (5A / 2A / j.A)** | **A** | `J` | $\square$ (Square) | `X` |
| **Medium Normal (5B / 2B / j.B)** | **B** | `K` | $\triangle$ (Triangle) | `Y` |
| **Heavy Normal (5C / 2C / j.C)** | **C** | `L` | `R1` (Shoulder) | `RB` (Bumper) |
| **Drive Attack (5D / 2D / j.D)** | **D** | `Space` or `I` | $\bigcirc$ (Circle) | `B` |
| **Taunt** | **Taunt** | `T` | `Touchpad` / `Share` | `View` / `Back` |

### System Macros (P1)
| System Mechanic | Raw Input | Dedicated Keyboard Macro | Dedicated Gamepad Binding | Description |
|---|:---:|:---:|:---:|---|
| **Barrier Block** | `4/1 + A + B` *(while holding back)* | Hold `A/S` + `U` | Hold `Back` + `L1 / LB` | Green shield, drains Barrier, 0% chip damage, increased pushback. |
| **Crush Trigger** | `A + B` *(in neutral)* | `U` | `L1` / `LB` | Spends 25% Heat to break standard block with guard crush. |
| **Counter Assault** | `6 + A + B` *(during blockstun)* | `D` + `U` | `Forward` + `L1 / LB` | Spends 50% Heat; invincible guard-cancel strike escaping pressure. |
| **Throw (Forward / Back)** | `B + C` | `O` | `R2` / `RT` | Unblockable close-range grapple. Hold 4+BC for back throw. Techable window. |
| **Break Burst** | `A + B + C + D` *(in hitstun/blockstun)* | `P` | `L2` / `LT` | Defensive combo-breaker shockwave (consumes 100% Burst Gauge). |
| **Overdrive** | `A + B + C + D` *(in neutral/attack)* | `;` (Semicolon) or `P` | `R3` or `L2` / `LT` | Freezes time and enhances Drive attacks; scales longer at low HP. |
| **Rapid Cancel** | `A + B + C` *(during attack hit/block)* | `J + K + L` | `$\square$ + $\triangle$ + R1` | Spends 50% Heat to instantly reset attack recovery back to Neutral. |

---

## 4. Player 2 Bindings (Local Versus)

### Movement (P2)
| Action | Keyboard Key | Gamepad 2 |
|---|:---:|:---:|
| **Walk Forward** | `Left Arrow` (facing left) | `D-Pad Left` |
| **Walk Backward / Guard** | `Right Arrow` (facing left) | `D-Pad Right` |
| **Crouch** | `Down Arrow` | `D-Pad Down` |
| **Jump Neutral** | `Up Arrow` | `D-Pad Up` |
| **Jump Forward** | `Up + Left` | `Up-Left Diagonal` |
| **Jump Backward** | `Up + Right` | `Up-Right Diagonal` |
| **Forward Dash** | `Left, Left` (Double-tap) | `Left, Left` |
| **Backdash** | `Right, Right` (Double-tap) | `Right, Right` |

### Combat Actions & Attacks (P2)
| Command | Button | Keyboard Key | Gamepad 2 |
|---|:---:|:---:|:---:|
| **Light Normal (A)** | **A** | `Numpad 1` | $\square$ / `X` |
| **Medium Normal (B)** | **B** | `Numpad 2` | $\triangle$ / `Y` |
| **Heavy Normal (C)** | **C** | `Numpad 3` | `R1` / `RB` |
| **Drive Attack (D)** | **D** | `Numpad 0` or `Numpad Enter` | $\bigcirc$ / `B` |

### System Macros (P2)
| System Mechanic | Raw Input | Keyboard Key | Gamepad 2 |
|---|:---:|:---:|:---:|
| **Barrier Block** | `4/1 + A + B` | `Right/Down + Numpad 4` | `Back + L1 / LB` |
| **Crush Trigger** | `A + B` *(in neutral)* | `Numpad 4` | `L1` / `LB` |
| **Counter Assault** | `6 + A + B` *(during blockstun)* | `Left + Numpad 4` | `Forward + L1 / LB` |
| **Throw** | `B + C` | `Numpad 5` | `R2` / `RT` |
| **Break Burst** | `A + B + C + D` *(in hitstun/blockstun)* | `Numpad 6` | `L2` / `LT` |
| **Overdrive** | `A + B + C + D` *(in neutral/attack)* | `Numpad +` or `Numpad 6` | `R3` or `L2` / `LT` |

---

## 5. In-Game Debug & Diagnostic Shortcuts

| Key | Feature | Description |
|:---:|---|---|
| **F1** | **Debug Overlays Toggle** | Toggles all hurtbox/hitbox/pushbox wireframes and debug shapes ON/OFF (Default: OFF). |
| **F2** | **Live Input Tracker HUD** | Toggles real-time P1/P2 scrolling input history, direction, phase, and move ID displays (Default: OFF). |
| **F3** | **CSV Input Session Logger** | Starts/stops streaming live move executions to `Logs/input_session_<date>.csv`. |
| **Esc / Start** | **Pause Menu** | Opens match pause menu with moveset list and audio settings. |
