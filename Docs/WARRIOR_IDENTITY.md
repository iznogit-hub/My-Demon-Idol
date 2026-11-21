# Warrior Identity & Combat Specification (All 25 Fighters)

**Project:** *My Demon Idol* (BlazBlue Combat System)  
**Specification:** Unified Warrior Archetype, Animation Set, Weapon & Feel Mapping  
**Branch:** `main`  

---

## 1. Roster Identity & Archetype Matrix

| # | ID | Fighter Name | Archetype | Primary Pack | Secondary Pack (Fallback) | Weapon in Hand | Fight Feel |
|:---:|:---:|---|---|---|---|:---:|---|
| 1 | **C01** | **Akio** | Striker | Martial Art Animations | Fighter Pack Bundle | None | Fast boxer, straight punches, rapid counter-jabs, rushdown |
| 2 | **C02** | **Aika** | Kicker | Martial Art Animations | Fighter Pack Bundle | None | Acrobatic kicking specialist, rapid front snaps, spinning roundhouses, high-pressure corner carry |
| 3 | **C04** | **UnityChan** | Striker | Fighter Pack Bundle | Martial Art Animations | None | Energetic speed striker, fast flurries, double palm strikes, peppy forward momentum |
| 4 | **C05** | **Akane** | Polearm | Warrior Bundle 3 *(Spearman)* | Human Melee Animations | Polearm | Disciplined spear lancer, long-range thrusts, sweeping perimeter denials, lethal mid-range control |
| 5 | **C06** | **Cultist** | Caster | Human Spell Casting Animations | Warrior Bundle 1 *(Sorceress)* | None | Dark ritualistic warlock, dual-hand channelings, ground glyphs, ominous wide-burst hexes |
| 6 | **C07** | **StellarWitch** | Caster | Human Spell Casting Animations | Martial Art Animations | Staff | Graceful astral spellcaster, one-handed celestial summons, omnidirectional starbursts, defensive traps |
| 7 | **C08** | **SanpakuChan** | Ninja | Warrior Bundle 1 *(Ninja)* | Martial Art Animations | Dagger | Lethal shadow assassin, low evasive dashes, reverse-grip daggers, lightning-quick crossups |
| 8 | **C09** | **PicoChan** | Grappler | Fighter Pack Bundle *(Heavy)* | Martial Art Animations | None | Powerhouse cybernetic brawler, crushing hooks, seismic body tackles, devastating throws |
| 9 | **C10** | **Reika** | Sword | Human Melee Animations | Warrior Bundle 2 *(Knight)* | Sword | Aristocratic rapier duelist, pinpoint thrusts, elegant counter-parries, calculated spacing |
| 10 | **C11** | **Casual1** | Striker | Fighter Pack Bundle | Martial Art Animations | None | Relaxed street brawler, fluid rhythm, slipping jabs, deceptive counter-punches |
| 11 | **C12** | **Fina** | Kicker | Martial Art Animations | Fighter Pack Bundle | None | Charismatic heroic striker, explosive high kicks, lunging front chops, neon-charged impact |
| 12 | **C13** | **SapphiArtchan** | Caster | Human Spell Casting Animations | Martial Art Animations | Staff | Vibrant idol enchantress, musical incantations, cheerful spell pirouettes, zoning rhythm |
| 13 | **C14** | **Muryotaisu** | Greatsword | Warrior Bundle 2 *(2Handed)* | Human Melee Animations | Greatsword | Stoic nodachi master, devastating heavy arcs, armor-crushing sweeps, immense poise |
| 14 | **SK02** | **Galahad** | Sword | Warrior Bundle 2 *(Knight)* | Human Melee Animations | Sword / Shield | Noble holy knight, resolute shield advances, clean sword cleaves, stalwart defense |
| 15 | **SK03** | **Vex** | Ninja | Warrior Bundle 3 *(Swordsman)* | Warrior Bundle 1 *(Ninja)* | Sword | Treacherous curved-blade assassin, low slicing lunges, dirty strikes, unpredictable tempo |
| 16 | **SK05** | **LordMorvath** | Caster | Warrior Bundle 2 *(Mage)* | Human Spell Casting Animations | Staff | Tyrannical death magus, wide spectral sweeps, dark spell invocations, overwhelming pressure |
| 17 | **SK06** | **CommanderRen** | Sword | Human Melee Animations | Warrior Bundle 3 *(Swordsman)* | Sword | Battle-hardened military commander, efficient saber slices, forceful guard breakers, forward drive |
| 18 | **SK08** | **Torgrim** | Greatsword | Warrior Bundle 3 *(Hammer/2H)* | Dwarf Animations | Hammer | Relentless mountain warrior, earth-shaking overhead smashes, wide horizontal sweeps, brutal momentum |
| 19 | **EX01** | **Hugo** | Brute | Warrior Bundle 1 *(Brute)* | GAMWILL Bionic Gorilla | None | Colossal cybernetic gorilla, earth-splitting double fist slams, charging bulldozers, monstrous reach |
| 20 | **EX02** | **Granite** | Brute | Warrior Bundle 3 *(Hammer)* | Warrior Bundle 1 *(Brute)* | Hammer | Unstoppable stone behemoth, slow windup crushing blows, ground shockwaves, impenetrable bulk |
| 21 | **EX03** | **Viper** | Sword | Warrior Bundle 3 *(Swordsman)* | Warrior Bundle 1 | Sword | Predatory mercenary, biting diagonal slashes, evasive feints, venomous advancing thrusts |
| 22 | **EX04** | **OfficerK** | Striker | Warrior Bundle 1 *(Karate)* | Fighter Pack Bundle | None | Clinical riot police officer, strict karate straight punches, low sweep takedowns, crowd suppression |
| 23 | **EX05** | **Kira** | Sword | Warrior Bundle 2 *(Knight/Sword)* | Warrior Bundle 3 | Sword | Lethal iaido swordsman, lightning fast draw cuts, razor-sharp step-throughs, razor focus |
| 24 | **EX06** | **Sentry** | Polearm | Warrior Bundle 3 *(Spearman)* | Human Melee Animations | Polearm | Unyielding mechanical automaton, piston-driven spear thrusts, defensive barrier pokes, robotic precision |
| 25 | **EX07** | **Lyra** | Caster | Warrior Bundle 1 *(Sorceress)* | Human Spell Casting Animations | Staff | Mystic sorceress of dawn, graceful hovering casts, swirling light spheres, dazzling arcane detonations |

---

## 2. Animation & Weapon Discipline Guidelines

1. **Weapon Consistency**:
   - Fighters equipped with weapons (`sword`, `greatsword`, `polearm`, `dagger`, `hammer`, `staff`) exclusively execute weapon strikes and weapon-drawn poses for all weapon-bearing normal, special, and drive commands.
   - Unarmed fighters (`striker`, `kicker`, `grappler`, `brute`) strictly execute authentic martial art punches, palm strikes, roundhouse kicks, knees, elbows, and grappling tackles.
   - Casters (`caster`) channel elemental, celestial, or necromantic energy through designated casting animations (directional beams, omnidirectional bursts, incantations, ground callings).

2. **Grounding & Rooting Rules**:
   - Root Transform Y is baked for all grounded animations, preserving ground contact (feet $\le$ 0.03m from floor at all times).
   - Root Transform XZ is baked for stationary strikes, allowing BBMatchSim simulation physics to handle pushback and trajectory; lunges only use controlled displacement.
