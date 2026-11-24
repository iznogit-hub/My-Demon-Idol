# BlazBlue Moveset & Input Tracker (12 Launch Fighters & 13 Vaulted Fighters)

*Standardized by `BBWarriorBundleMapper` on 2026-11-24*  
Authoritative project assets: `BBCharacterProfile`, `BBMoveSet`, `AnimatorOverrideController`, `BBAttackData`, `BBBoxTrack`, `BBWeaponRig`.

---

## 1. Main Launch Roster (12 Fighters — 7-Move Standardized Template)

All 12 launch fighters are standardized onto the clean 7-move playable kit (`5A`, `5B`, `5C`, `2C`, `j.B`, `5D`, `236236C`) powered exclusively and isolated 1-to-1 by their assigned ExplosiveLLC Warrior Pack:

| Fighter ID | Fighter Name | Dedicated Warrior Pack | Archetype Class | Weapon Prop & Socket | Target Scale | Projectile? | Moves | Ready |
|---|---|---|---|---|:---:|:---:|:---:|:---:|
| **C01** | Akio | Bundle 1 — Karate | Rushdown | Unarmed (Fists / Feet) | N/A | No | 7 / 7 OK | **Y** |
| **C02** | Aika | Bundle 1 — Ninja | Rushdown | Ninja Sword -> RightHand | 0.80m | No | 7 / 7 OK | **Y** |
| **C05** | Akane | Bundle 1 — Sorceress | Zoner | Arcane Staff -> RightHand | 1.60m | Yes (5D/Super) | 7 / 7 OK | **Y** |
| **EX04** | OfficerK | Bundle 1 — Brute | Powerhouse | Heavy Riot Maul -> RightHand | 1.20m | No | 7 / 7 OK | **Y** |
| **C11** | Casual1 | Bundle 2 — Knight | All-Rounder | Sword (RightHand) + Shield (LeftArm) | 0.90m / 0.55m | No | 7 / 7 OK | **Y** |
| **C06** | Cultist | Bundle 2 — Mage | Zoner | Void Staff -> RightHand | 1.60m | Yes (5D/Super) | 7 / 7 OK | **Y** |
| **C09** | PicoChan | Bundle 2 — Archer | Zoner | Short Bow -> LeftHand | 1.05m | Yes (5B/5C/5D/Super) | 7 / 7 OK | **Y** |
| **C14** | Muryotaisu | Bundle 2 — TwoHanded | Heavy Swordsman | Greatsword -> RightHand | 1.55m | No | 7 / 7 OK | **Y** |
| **C08** | SanpakuChan | Bundle 3 — Swordsman | Mixup / Assassin | Katana -> RightHand | 0.90m | No | 7 / 7 OK | **Y** |
| **C12** | Fina | Bundle 3 — Crossbow | Zoner / Striker | Tactical Crossbow -> RightHand | 0.72m | Yes (5B/5C/5D/Super) | 7 / 7 OK | **Y** |
| **EX01** | Hugo | Bundle 3 — Spearman | Heavy Mid-Range | Battle Spear -> RightHand | 2.00m | No | 7 / 7 OK | **Y** |
| **SK08** | Torgrim | Bundle 3 — Hammer | Boss Bruiser | Colossal Mountain Hammer -> RightHand | 1.65m | No | 7 / 7 OK | **Y** |

---

## 2. Standardized 7-Move Frame Data & Role Specification

Each launch fighter uses the archetypally scaled 7-move framework:

| Slot | Input | Role | Target S / A / R | OnBlock | Base Dmg | Hit Level & Mechanics |
|---|---|---|:---:|:---:|:---:|---|
| **1** | `5A` (J / Square) | Light Jab / Poke | 5 / 3 / 8 | +1 | 280 | High/Mid · Gatlings into 5B, 5C, 2C, 5D, 236236C |
| **2** | `5B` (K / Triangle) | Medium Strike / Blade | 7 / 4 / 12 | -2 | 520 | Mid · Gatlings into 5C, 2C, 5D, 236236C |
| **3** | `5C` (L / R1) | Heavy Slash / Cleave | 11 / 4 / 17 | -4 | 820 | Mid · Gatlings into 5D, 236236C |
| **4** | `2C` (S+L / Down+R1) | Low Sweep / Quake | 10 / 3 / 22 | -8 | 700 | Low · Hard Knockdown on Hit |
| **5** | `j.B` (Air+K / Air+Tri) | Jumping Strike / Aerial | 7 / 4 / 14 | 0 | 520 | Overhead · Instant Air-to-Ground Starter |
| **6** | `5D` (Space / Circle) | Signature Drive Attack | 13 / 4 / 17 | -4 | 750 | Mid · Character Drive (Cancels into Super) |
| **7** | `236236C` (QCFx2+L) | Signature Finisher (Super) | 9 / 4 / 26 | -14 | 2200 | Mid · Spends 50% Heat · Super Flash Invuln |

*Formula Guarantee:* Every single move strictly satisfies the BBCF / Dustloop recovery identity:
`recovery = blockstun - (active - 1) - advantageOnBlock`

---

## 3. Dedicated Warrior Bundle Slicing Matrix

The 7 active attack clips for each fighter are sliced directly from `Take 001` inside each warrior pack's `Attack1.FBX`, with Humanoid retargeting, `m_ApplyRootMotion: 0`, and `keepOriginalPositionY: 0`:

| Fighter | Pack Folder | 5A Slice | 5B Slice | 5C Slice | 2C Slice | j.B Slice | 5D Slice | Super Slice |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **C01 Akio** | `Warrior Pack Bundle 1/Karate Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C02 Aika** | `Warrior Pack Bundle 1/Ninja Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C05 Akane** | `Warrior Pack Bundle 1/Sorceress Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **EX04 OfficerK** | `Warrior Pack Bundle 1/Brute Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C11 Casual1** | `Warrior Pack Bundle 2/Knight Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C06 Cultist** | `Warrior Pack Bundle 2/Mage Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C09 PicoChan** | `Warrior Pack Bundle 2/Archer Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C14 Muryotaisu** | `Warrior Pack Bundle 2/TwoHanded Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C08 SanpakuChan** | `Warrior Pack Bundle 3/Swordsman Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **C12 Fina** | `Warrior Pack Bundle 3/Crossbow Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **EX01 Hugo** | `Warrior Pack Bundle 3/Spearman Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |
| **SK08 Torgrim** | `Warrior Pack Bundle 3/Hammer Warrior` | f1–f16 | f17–f39 | f40–f72 | f73–f108 | f109–f130 | f131–f165 | f166–f214 |

---

## 4. Preserved Vaulted Characters (13 Fighters)

The following 13 fighters are preserved untouched in the archive / vault:

| Fighter ID | Fighter Name | Status | Preserved Pack / Style |
|---|---|---|---|
| **C04** | UnityChan | Vaulted Legacy | Fighter Pack Bundle |
| **C07** | StellarWitch | Vaulted Legacy | Human Spell Casting Animations |
| **C10** | Reika | Vaulted Legacy | Human Melee Animations |
| **C13** | SapphiArtchan | Vaulted Legacy | Human Spell Casting Animations |
| **SK02** | Galahad | Vaulted Legacy | Warrior Bundle 2 (Knight) |
| **SK03** | Vex | Vaulted Legacy | Warrior Bundle 3 / 1 |
| **SK05** | LordMorvath | Vaulted Legacy | Warrior Bundle 2 (Mage) |
| **SK06** | CommanderRen | Vaulted Legacy | Human Melee Animations |
| **EX02** | Granite | Vaulted Legacy | Warrior Bundle 3 (Hammer) |
| **EX03** | Viper | Vaulted Legacy | Warrior Bundle 3 (Sword) |
| **EX05** | Kira | Vaulted Legacy | Warrior Bundle 2 (Archer) |
| **EX06** | Sentry | Vaulted Legacy | Warrior Bundle 3 (Spear) |
| **EX07** | Lyra | Vaulted Legacy | Warrior Bundle 1 (Sorceress) |
