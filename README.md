# My Demon Idol (MDI)

[![Unity 6000.6.8f1](https://img.shields.io/badge/Unity-6000.6.8f1-blue.svg)](https://unity.com/)
[![Universal Render Pipeline](https://img.shields.io/badge/URP-17.6.0-green.svg)](https://unity.com/srp/universal-render-pipeline)
[![Photon Quantum](https://img.shields.io/badge/Rollback-Photon%20Quantum%203.0-orange.svg)](https://www.photonengine.com/quantum)
[![BlazBlue Style 4-Button](https://img.shields.io/badge/Engine-BlazBlue%20Central%20Fiction%204--Button-red.svg)](Docs/CONTROLS.md)
[![Status](https://img.shields.io/badge/Release%20Status-v1.0.0--rc1-gold.svg)]()

*My Demon Idol* is a fast-paced, competitive 2.5D anime fighting game inspired by *BlazBlue: Central Fiction*, *Guilty Gear*, and *Street Fighter 6*. Built from the ground up in Unity 6, it pairs traditional Sumi-e ink wash aesthetics with modern tactical UI, a complete 25-fighter roster, deterministic 60 Hz rollback simulation authority, and a zero-allocation combat engine.

---

## Key Features

- **25 Fully-Playable Fighters:**
  - **Main Cast (13):** C01 Akio, C02 Aika, C04 UnityChan, C05 Akane, C06 Cultist, C07 StellarWitch, C08 SanpakuChan, C09 PicoChan, C10 Reika, C11 Casual1, C12 Fina, C13 SapphiArtchan, C14 Muryotaisu.
  - **Shadow Knights (5):** SK02 Galahad, SK03 Vex, SK05 Lord Morvath, SK06 Commander Ren, SK08 Torgrim.
  - **Expansion Cast (7):** EX01 Hugo, EX02 Granite, EX03 Viper, EX04 Officer K, EX05 Kira, EX06 Sentry, EX07 Lyra.
- **BlazBlue 4-Button Combat Engine:** Standardized A (Light), B (Medium), C (Heavy), and D (Drive) mechanics with Revolver Action gatling cancels, Instant/Barrier blocking, Rapid Cancel (50% Heat), Overdrive, Defensive Burst, Counter Assault, and Command Throws.
- **Bit-Identical Rollback Determinism:** 64-bit integer basis-point (`10,000 = 1.0000x`) damage proration model (`BBDamageModel`), eliminating floating-point divergence between ARM64 and x86-64.
- **Modern Sumi-e Front-End:** Tactile asymmetric main menu hub with live 3D hero stage backdrop, dual live 3D face-off character select pedestals with mirror-match border clarity (`C01 vs C01`), and slide-over tactical drawers.
- **Offline-First Playability:** Zero mandatory cloud prerequisites. Instant local Arcade, Local Versus (P1 Keyboard + P2 Gamepad), and Training modes work out of the box.

---

## System Requirements & Setup

### Environment
- **Unity Version:** `Unity 6000.6.8f1-arm64` (Universal Render Pipeline 17.6.0)
- **Target Architectures:**
  - macOS: Universal (Apple Silicon ARM64 + Intel x86-64)
  - Windows: x86-64
  - Android: ARM64-v8a (IL2CPP)
  - iOS: ARM64

### Quickstart (Offline Local Play)
1. Clone the repository:
   ```bash
   git clone https://github.com/iznogit-hub/My-Demon-Idol.git
   cd My-Demon-Idol
   ```
2. Open the project in **Unity 6000.6.8f1**.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
4. Use standard controls to navigate the top pillar (`FIGHT`, `DOJO`, `ROSTER`, `SETTINGS`), choose **Arcade** or **Versus**, pick your fighter on the dual pedestals, and battle in `BattleArena.unity`.

### Local Photon Quantum Rollback Setup (Without Committing Secrets)
The project connects to Photon Quantum 3.0 for competitive 1v1 online rollback. To run online PvP matches locally:
1. Obtain a free Quantum App ID from the [Photon Dashboard](https://dashboard.photonengine.com/).
2. In the Unity Editor, open the Quantum Hub (`Window > Quantum > Quantum Hub`) or inspect `Assets/QuantumUser/Resources/PhotonServerSettings.asset`.
3. Paste your App ID into the `AppIdQuantum` field.
> **Note:** `PhotonServerSettings.asset` is tracked in `.gitignore` with empty App IDs. `DemonIdolReleaseBuildGuard` will strictly block non-development release builds if hardcoded App IDs or secrets are detected.

---

## 4-Button Controls Summary

Detailed frame data and mechanics specifications are documented in [Docs/CONTROLS.md](Docs/CONTROLS.md).

| Command | Action | Keyboard (P1) | Gamepad (P1/P2) | System Rules |
| :--- | :--- | :--- | :--- | :--- |
| **A** | Light Attack | `J` | `X` (Xbox) / `Square` (PS) | Fast 5–7f poke; gatlings into B, C, D |
| **B** | Medium Attack | `K` | `Y` (Xbox) / `Triangle` (PS) | Mid-range button; gatlings into C, D |
| **C** | Heavy Attack | `L` | `B` (Xbox) / `Circle` (PS) | Heavy damage; knockdown / wall bounce |
| **D** | Drive Move | `;` | `A` (Xbox) / `Cross` (PS) | Unique character Drive mechanic |
| **B + C** | Normal Throw | `K + L` | `RB` / `R1` | 4f startup, techable within 14f |
| **A + B + C** | Rapid Cancel | `J + K + L` | `LB` / `L1` | Cancels recovery on hit/block; costs 50% Heat |
| **A + B + C + D** | Overdrive / Burst | `J + K + L + ;` | `LT` / `L2` | Neutral: Overdrive / Hitstun: Gold/Blue Burst |
| **6 + A + B** | Counter Assault | `D + J + K` | `Forward + LB` | Pushback counter during blockstun; costs 50% Heat |
| **4 (Hold) + D** | Barrier Guard | `A (Hold) + ;` | `Back (Hold) + A` | 100% chip negation, prevents pushback push |

---

## Architecture Overview

```
Assets/
├── Scripts/
│   ├── BlazBlue/
│   │   ├── Core/         # Pure mathematical & deterministic simulation logic
│   │   │   ├── BBDamageModel.cs          # 64-bit integer basis-point proration engine
│   │   │   ├── BBCommandInterpreter.cs   # Numpad notation & SOCD resolution
│   │   │   ├── BBCancelRules.cs          # Gatling, whiff-cancel, and special cancel rules
│   │   │   └── BBRosterArchetype.cs      # Roster stats, reach, and archetype taxonomy
│   │   ├── Runtime/      # 60 Hz deterministic simulation runtime
│   │   │   ├── BBMatchSim.cs             # Single match simulation authority
│   │   │   ├── BBAnimationDriver.cs      # Two-layer zero-GC Playables animation driver
│   │   │   ├── BBBarrier.cs              # Barrier defense & instant block simulation
│   │   │   ├── BBRapidCancel.cs          # Rapid cancel freeze & recovery bypass
│   │   │   └── BBOverdrive.cs            # Overdrive timer and burst state authority
│   │   └── Visual/       # Cosmetic visual presentation
│   │       ├── BBWeaponRig.cs            # Weapon bone attachment sockets & trails
│   │       └── BBContactPresentation.cs  # Hit sparks, super flash cut-ins & audio cues
│   ├── Network/          # Photon Quantum 3.0 rollback integration
│   │   ├── QuantumPvPManager.cs          # Real cloud & loopback matchmaking controller
│   │   ├── QuantumCombatBridge.cs        # Rollback sim-to-view interpolation bridge
│   │   └── NetworkGatekeeper.cs          # 5G/LAN jitter & ping quality gatekeeper
│   ├── UI/               # Sumi-e styled tactical front-end
│   │   ├── SumiStyleTokens.cs            # Canonical color palette, alpha, and ease timings
│   │   ├── DemonIdolMainMenuController.cs # Asymmetric tactical hub & slide-over drawer
│   │   └── DemonIdolCharacterSelectManager.cs # Dual 3D pedestals & categorized 25-fighter dock
│   ├── Launch/           # Offline session and rewards orchestration
│   └── Editor/           # Build automation, scene builders, and release build security guards
│       ├── DemonIdolMenu.cs              # Authoritative unified "Demon Idol/..." menu
│       ├── DemonIdolSceneBuilder.cs      # Headless & interactive modern scene builder
│       ├── DemonIdolPortraitGenerator.cs # Automated 25-fighter bust portrait renderer
│       └── DemonIdolReleaseBuildGuard.cs # Release build secret scan & mock-auth blocker
```

---

## Technical Reports & Documentation

- [Docs/CONTROLS.md](Docs/CONTROLS.md) — Complete 4-button input, gatling table, and system mechanics reference.
- [Docs/MOVESET_TRACKER.md](Docs/MOVESET_TRACKER.md) — 25-fighter moveset inventory, humanoid clip mappings, and hit frame windows.
- [Docs/FIGHTFEEL_AND_LOOP_REPORT.md](Docs/FIGHTFEEL_AND_LOOP_REPORT.md) — 700/700 hit proof, hitstop weights, barrier sparks, and round-reset verification.
- [Docs/MDI_CODEBASE_ANALYSIS.md](Docs/MDI_CODEBASE_ANALYSIS.md) — Architectural deep dive and component dependency analysis.
- [Docs/MENU_AUDIT.md](Docs/MENU_AUDIT.md) — Single authoritative menu root rule and UI design audit.

---

## License & Credits

Developed by **Celestial Soul Studio** for *My Demon Idol*. Built with Unity 6, Universal Render Pipeline, and Photon Quantum.
