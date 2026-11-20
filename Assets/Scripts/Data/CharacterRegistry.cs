using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Content Registry for CharacterDefinitions.
    /// Provides the authoritative 14-fighter roster for Demon Idol (Jujutsu Kaisen & Bleach anime sorcerers),
    /// dynamically discovering authored CharacterDefinitions and providing fallbacks.
    /// </summary>
    public static class CharacterRegistry
    {
        private static List<CharacterDefinition> _cachedCharacters;

        private static readonly Dictionary<string, string> _idAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // C01 - Akio (Megumi homage)
            { "Akio", "C01" },
            { "C01", "C01" },
            { "M01", "C01" },
            { "Kai", "C01" },

            // C02 - Aika (Nobara homage)
            { "Aika", "C02" },
            { "C02", "C02" },
            { "F01", "C02" },
            { "Nobara", "C02" },

            // C03 - Ren (Male Hero Protagonist Counterpart)
            { "Ren", "C03" },
            { "C12M", "C03" },
            { "MaleHero", "C03" },
            { "C03", "C03" },
            { "M02", "C03" },
            { "Yuji", "C03" },

            // C04 - UnityChan (Gojo homage)
            { "UnityChan", "C04" },
            { "unitychan_dynamic", "C04" },
            { "Gojo", "C04" },
            { "C04", "C04" },
            { "M03", "C04" },
            { "Zev", "C04" },

            // C05 - Akane (Inumaki homage)
            { "Akane", "C05" },
            { "angGirl", "C05" },
            { "Inumaki", "C05" },
            { "C05", "C05" },
            { "M06", "C05" },
            { "Kuro", "C05" },

            // C06 - Cultist (Geto homage)
            { "Cultist", "C06" },
            { "Geto", "C06" },
            { "C06", "C06" },
            { "M11", "C06" },
            { "Victor", "C06" },

            // C07 - StellarWitch (Shoko homage)
            { "StellarWitch", "C07" },
            { "Shoko", "C07" },
            { "C07", "C07" },
            { "M12", "C07" },
            { "Kael", "C07" },

            // C08 - SanpakuChan (Mei Mei homage)
            { "SanpakuChan", "C08" },
            { "MeiMei", "C08" },
            { "C08", "C08" },
            { "F02", "C08" },
            { "Mira", "C08" },

            // C09 - PicoChan (Mechamaru homage)
            { "PicoChan", "C09" },
            { "Mechamaru", "C09" },
            { "C09", "C09" },
            { "M08", "C09" },
            { "Boran", "C09" },

            // C10 - Reika (Hero Alt Counterpart)
            { "Reika", "C10" },
            { "C12B", "C10" },
            { "C10", "C10" },
            { "M09", "C10" },
            { "F04", "C10" },
            { "Kiki", "C10" },

            // C11 - Casual1 (Hakari homage)
            { "Casual1", "C11" },
            { "Hakari", "C11" },
            { "C11", "C11" },
            { "M07", "C11" },
            { "Shin", "C11" },

            // C12 - Fina (Lead Hero Protagonist)
            { "Fina", "C12" },
            { "C12", "C12" },
            { "Protagonist", "C12" },
            { "Hero", "C12" },
            { "Underwear1", "C12" },
            { "Maki", "C12" },
            { "M10", "C12" },
            { "Jin", "C12" },
            { "F06", "C12" },
            { "Vesper", "C12" },

            // C13 - SapphiArtchan (Takada-chan homage)
            { "SapphiArtchan", "C13" },
            { "SapphiArt", "C13" },
            { "Takada", "C13" },
            { "C13", "C13" },
            { "F05", "C13" },
            { "Chiyo", "C13" },

            // C14 - Muryotaisu (Sukuna homage)
            { "Muryotaisu", "C14" },
            { "Sukuna", "C14" },
            { "C14", "C14" },
            { "M04", "C14" },
            { "Dante", "C14" },
            { "F03", "C14" },
            { "Nox", "C14" },
            { "M05", "C14" },
            // Synty Sidekicks (8)
            { "SK01", "SK01" }, { "Valen", "SK01" },
            { "SK02", "SK02" }, { "Galahad", "SK02" },
            { "SK03", "SK03" }, { "Vex", "SK03" },
            { "SK04", "SK04" },
            { "SK05", "SK05" }, { "LordMorvath", "SK05" }, { "Morvath", "SK05" },
            { "SK06", "SK06" }, { "CommanderRen", "SK06" },
            { "SK07", "SK07" }, { "Malakar", "SK07" },
            { "SK08", "SK08" }, { "Torgrim", "SK08" },

            // External Dota Heroes (8)
            { "EX01", "EX01" }, { "Hugo", "EX01" },
            { "EX02", "EX02" }, { "Granite", "EX02" },
            { "EX03", "EX03" }, { "Viper", "EX03" },
            { "EX04", "EX04" }, { "OfficerK", "EX04" },
            { "EX05", "EX05" }, { "Kira", "EX05" },
            { "EX06", "EX06" }, { "Sentry", "EX06" },
            { "EX07", "EX07" }, { "Lyra", "EX07" },
            { "EX08", "EX08" }, { "Garrick", "EX08" },
        };

        public static string ResolveCanonicalId(string idOrAlias)
        {
            if (string.IsNullOrEmpty(idOrAlias)) return idOrAlias;
            if (_idAliases.TryGetValue(idOrAlias, out var canonical))
            {
                return canonical;
            }
            return idOrAlias;
        }

        public static string MapAuthoritativeIdToLegacy(string authId)
        {
            if (string.IsNullOrEmpty(authId)) return authId;
            return authId;
        }

        public static string GetCharacterShortName(string charId)
        {
            var def = GetCharacterById(charId);
            if (def != null && !string.IsNullOrEmpty(def.displayName))
            {
                string shortPart = def.displayName.Split(' ')[0];
                if (!string.IsNullOrEmpty(shortPart)) return shortPart;
            }
            return GetCharacterNameShort(charId);
        }

        public static string ResolveDefaultDisplayName(string charId)
        {
            return GetCharacterNameShort(charId);
        }

        public static string GetCharacterNameShort(string charId)
        {
            string canon = ResolveCanonicalId(charId);
            return canon switch
            {
                "C01" => "Akio",
                "C02" => "Aika",
                "C03" => "Ren",
                "C04" => "UnityChan",
                "C05" => "Akane",
                "C06" => "Cultist",
                "C07" => "StellarWitch",
                "C08" => "SanpakuChan",
                "C09" => "PicoChan",
                "C10" => "Reika",
                "C11" => "Casual1",
                "C12" => "Fina",
                "C13" => "SapphiArtchan",
                "C14" => "Muryotaisu",
                "SK01" => "Valen",
                "SK02" => "Galahad",
                "SK03" => "Vex",
                "SK04" => "Jin",
                "SK05" => "LordMorvath",
                "SK06" => "CommanderRen",
                "SK07" => "Malakar",
                "SK08" => "Torgrim",
                "EX01" => "Hugo",
                "EX02" => "Granite",
                "EX03" => "Viper",
                "EX04" => "OfficerK",
                "EX05" => "Kira",
                "EX06" => "Sentry",
                "EX07" => "Lyra",
                "EX08" => "Garrick",

                _ => charId ?? "Fighter"
            };
        }

        public struct RosterEntry
        {
            public string id;
            public string name;
            public string title;
            public string archetype;
            public string desc;
            public float speed;
            public float power;
            public float reach;
            public float defense;
            public string domain;
            public string domainDesc;
            public string prefabPath;
            public string signatureStageId;
        }

        private static readonly RosterEntry[] _canonical14List = new RosterEntry[]
        {
            new RosterEntry
            {
                id = "C01", name = "Akio", title = "Shadow Chimera Prodigy", archetype = "Ten Shadows / Rushdown",
                desc = "Highschool prodigy summoning shikigami shadows, setting up unseeable pincer strikes and shadow puddle mixups.",
                speed = 88f, power = 82f, reach = 75f, defense = 80f,
                domain = "Chimera Shadow Garden",
                domainDesc = "Floods the stage with liquid darkness, spawning twin shikigami clones delivering inescapable high-low crossups.",
                prefabPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                signatureStageId = "ST01"
            },
            new RosterEntry
            {
                id = "C02", name = "Aika", title = "Straw Doll Resonance", archetype = "Zoner / Needle Nails",
                desc = "Spirited sorceress controlling horizontal space with cursed iron nails and remote hairpin explosive detonations.",
                speed = 84f, power = 86f, reach = 85f, defense = 72f,
                domain = "Resonance Hairpin Requiem",
                domainDesc = "Drives giant nails into the stage floor, triggering linked cursed energy detonations across all distances.",
                prefabPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                signatureStageId = "ST02"
            },
            new RosterEntry
            {
                id = "C03", name = "Ren", title = "Occult Vanguard Prodigy", archetype = "Male Protagonist / Rushdown",
                desc = "Charismatic male lead protagonist wielding dark occult martial arts, high-impact counter strikes, and vortex pressure.",
                speed = 95f, power = 92f, reach = 76f, defense = 80f,
                domain = "Malevolent Occult Domain",
                domainDesc = "Concentrates cursed spark into instantaneous double-impact shockwaves, shattering defensive guards.",
                prefabPath = "Assets/Prefabs/Characters/C12M_Ren.prefab",
                signatureStageId = "ST03"
            },
            new RosterEntry
            {
                id = "C04", name = "UnityChan", title = "Limitless Six Eyes", archetype = "Space Distortion / Aerial",
                desc = "Special-grade sorceress commanding infinity barrier defense, gravitational teleport pulls, and aerial divekicks.",
                speed = 95f, power = 95f, reach = 90f, defense = 90f,
                domain = "Infinite Void",
                domainDesc = "Floods opponent consciousness with infinite cosmic information, immobilizing them for complete combo punish.",
                prefabPath = "Assets/Prefabs/Characters/C04_UnityChan.prefab",
                signatureStageId = "ST04"
            },
            new RosterEntry
            {
                id = "C05", name = "Akane", title = "Cursed Speech Singer", archetype = "Assistant / Acoustic Shock",
                desc = "Mysterious acoustic support sorceress using throat-sigil cursed speech words to freeze opponents and break enemy startup.",
                speed = 86f, power = 76f, reach = 88f, defense = 74f,
                domain = "Echoing Mandate",
                domainDesc = "Resonates absolute vocal commandments across spacetime, forcing immediate block collapse and wall bounce.",
                prefabPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                signatureStageId = "ST05"
            },
            new RosterEntry
            {
                id = "C06", name = "Cultist", title = "Curse Spirit Manipulator", archetype = "Puppet / Swarm Summoner",
                desc = "Dark occult summoner manipulating swarming cursed spirits, mid-screen trap orbs, and multi-angle vortex pressure.",
                speed = 78f, power = 92f, reach = 82f, defense = 85f,
                domain = "All-Enveloping Uzumaki",
                domainDesc = "Condenses thousands of low-grade curses into a concentrated beam of catastrophic occult energy.",
                prefabPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                signatureStageId = "ST06"
            },
            new RosterEntry
            {
                id = "C07", name = "StellarWitch", title = "Reverse Cursed Physician", archetype = "Ethereal Zoner / Trap",
                desc = "Occult physician using reversed cursed energy blades and floating glyphs to control the neutral frame and punish whiffs.",
                speed = 80f, power = 75f, reach = 92f, defense = 80f,
                domain = "Astral Renewal Matrix",
                domainDesc = "Erects a golden alchemical field reversing damage momentum and emitting tracking energy shards.",
                prefabPath = "Assets/Prefabs/Characters/C07_StellarWitch.prefab",
                signatureStageId = "ST07"
            },
            new RosterEntry
            {
                id = "C08", name = "SanpakuChan", title = "The Black Bird Striker", archetype = "Assistant / Heavy Pokes",
                desc = "Mercenary battle-maiden assistant wielding heavy mid-range strikes, raven shikigami dive bombs, and high-priority counter pokes.",
                speed = 85f, power = 94f, reach = 88f, defense = 76f,
                domain = "Bird Strike Execution",
                domainDesc = "Sacrifices shikigami life in a supersonic death-pact missile that inflicts massive barrier chip and knockdown.",
                prefabPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                signatureStageId = "ST08"
            },
            new RosterEntry
            {
                id = "C09", name = "PicoChan", title = "Ultimate Mech Puppet", archetype = "Armored Artillery / Zoning",
                desc = "Miniature armored construct with built-in cursed beam cannons, rocket thrust dashes, and heavy super armor.",
                speed = 65f, power = 96f, reach = 95f, defense = 92f,
                domain = "Pico Cannon Barrage",
                domainDesc = "Locks mechanical targeting relays, unloading a screen-wide volley of thermal cursed rockets.",
                prefabPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                signatureStageId = "ST09"
            },
            new RosterEntry
            {
                id = "C10", name = "Reika", title = "Occult Shadow Blade", archetype = "Hero Alt / Swift Dualist",
                desc = "Tactical combat operative weaving lightning-fast shadow blades, evasive sidesteps, and lethal combo confirms.",
                speed = 93f, power = 85f, reach = 84f, defense = 74f,
                domain = "Phantom Shadow Slashes",
                domainDesc = "Drops temperature to absolute zero, freezing opponent hurtboxes into brittle ice crystals.",
                prefabPath = "Assets/Prefabs/Characters/C12B_Reika.prefab",
                signatureStageId = "ST10"
            },
            new RosterEntry
            {
                id = "C11", name = "Casual1", title = "Restless Fever Gambler", archetype = "Rushdown / Fever Overdrive",
                desc = "Street brawler gambling with slot-machine overdrive surges, immense frame advantage, and relentless forward momentum.",
                speed = 90f, power = 88f, reach = 72f, defense = 82f,
                domain = "Idle Death Gamble",
                domainDesc = "Triggers a jackpot fever mode granting infinite super meter and automatic hit-confirm cancels for 30 ticks.",
                prefabPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                signatureStageId = "ST11"
            },
            new RosterEntry
            {
                id = "C12", name = "Fina", title = "The Lead Protagonist", archetype = "Neo-Demon Idol / Lead Hero",
                desc = "The central heroine of Demon Idol. Commanding supreme anime combat flow, cursed domain strikes, and unstoppable rushdown.",
                speed = 98f, power = 92f, reach = 82f, defense = 84f,
                domain = "Neo-Idol Cataclysm",
                domainDesc = "Manifests a supreme domain of shattered obsidian and radiant pink sakura energy, crushing defensive barriers.",
                prefabPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                signatureStageId = "ST12"
            },
            new RosterEntry
            {
                id = "C13", name = "SapphiArtchan", title = "The Cursed Pop Idol", archetype = "Illusion / Flashy Mixup",
                desc = "Pop sensation dazzling foes with illusionary spotlights, sparkling heart bursts, and deceptive rhythm mixups.",
                speed = 88f, power = 78f, reach = 80f, defense = 70f,
                domain = "Infinite Idol Encore",
                domainDesc = "Summons a glowing concert stage blinding the opponent with neon pyrotechnics and crowd cheers.",
                prefabPath = "Assets/Prefabs/Characters/C13_SapphiArtchan.prefab",
                signatureStageId = "ST01"
            },
            new RosterEntry
            {
                id = "C14", name = "Muryotaisu", title = "The King of Curses", archetype = "Assistant / Boss Void Support",
                desc = "Malevolent demonic support overlord slicing through dimensional space with invisible flying slashes and devastating fiery arrows.",
                speed = 96f, power = 99f, reach = 92f, defense = 94f,
                domain = "Malevolent Shrine",
                domainDesc = "Opens a barrier-free domain relentlessly shredding everything within 20 meters with thousands of continuous slashes.",
                prefabPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                signatureStageId = "ST06"
            },
            // Synty Sidekicks (8)
            new RosterEntry
            {
                id = "SK01", name = "Valen", title = "Shadow Scout", archetype = "Ninja / Dual Blades",
                desc = "Stealthy shinobi darting through shadows, delivering high-speed dual ninjato crossups and evasive smoke teleports.",
                speed = 92f, power = 78f, reach = 75f, defense = 70f,
                domain = "Shadow Eclipse Mirage",
                domainDesc = "Shrouds the battlefield in black mist, blinding the target while shadow reflections strike from all angles.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK01_Valen.prefab",
                signatureStageId = "ST04"
            },
            new RosterEntry
            {
                id = "SK02", name = "Galahad", title = "Sun Templar", archetype = "Knight / Sword & Shield",
                desc = "Heavily armored righteous paladin deflecting heavy blows with an iron barrier shield and retaliating with holy thrusts.",
                speed = 74f, power = 88f, reach = 82f, defense = 95f,
                domain = "Radiant Aegis Cathedral",
                domainDesc = "Erects a golden sanctuary nullifying knockback and empowering broadsword slashes with solar fire.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK02_Galahad.prefab",
                signatureStageId = "ST04"
            },
            new RosterEntry
            {
                id = "SK03", name = "Vex", title = "Dune Raider", archetype = "Brute / Heavy Scimitar",
                desc = "Hardened desert marauder overwhelming foes with relentless curved scimitar swings and low ankle sweeping kicks.",
                speed = 82f, power = 90f, reach = 80f, defense = 84f,
                domain = "Desert Vortex Cataclysm",
                domainDesc = "Summons a swirling sandstorm vortex pulling the opponent inward and locking their aerial recovery.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK03_Vex.prefab",
                signatureStageId = "ST01"
            },
            new RosterEntry
            {
                id = "SK04", name = "Jin", title = "Zen Duelist", archetype = "Karate / Unarmed Kata",
                desc = "Disciplined martial master chaining lightning palm strikes, precise parries, and aerial roundhouse juggles.",
                speed = 90f, power = 84f, reach = 70f, defense = 80f,
                domain = "Tranquil Void Horizon",
                domainDesc = "Manifests a reflective water domain where every physical counter lands with double hitstun.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK04_Jin.prefab",
                signatureStageId = "ST03"
            },
            new RosterEntry
            {
                id = "SK05", name = "Lord Morvath", title = "Void Warlock", archetype = "Sorceress / Dark Magic",
                desc = "Malevolent warlock weaving gravitational singularities and corrosive dark orb traps across the screen.",
                speed = 76f, power = 94f, reach = 88f, defense = 75f,
                domain = "Abyssal Singularity Rift",
                domainDesc = "Rips open a gravitational void that continuously drains opponent super meter and crushes defensive barriers.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK05_LordMorvath.prefab",
                signatureStageId = "ST04"
            },
            new RosterEntry
            {
                id = "SK06", name = "Commander Ren", title = "Iron Vanguard", archetype = "Two-Handed / Heavy Greatsword",
                desc = "Veteran battle commander sweeping entire swathes of the arena with a colossal greatsword and armor-piercing charges.",
                speed = 72f, power = 95f, reach = 90f, defense = 92f,
                domain = "Fortress Bastion Protocol",
                domainDesc = "Locks down the arena within iron palisades, granting temporary super armor on heavy startup frames.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK06_CommanderRen.prefab",
                signatureStageId = "ST02"
            },
            new RosterEntry
            {
                id = "SK07", name = "Malakar", title = "Archon Seer", archetype = "Mage / Arcane Staff",
                desc = "Ancient celestial scholar conjuring radiant stasis beams and delayed arcane minefields from afar.",
                speed = 78f, power = 92f, reach = 92f, defense = 76f,
                domain = "Chrono Astral Stasis",
                domainDesc = "Freezes temporal flow around the target, dramatically extending juggle windows and combo potential.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK07_Malakar.prefab",
                signatureStageId = "ST01"
            },
            new RosterEntry
            {
                id = "SK08", name = "Torgrim", title = "Storm Breaker", archetype = "Hammer / Heavy Bruiser",
                desc = "Mountain titan wielding an apocalyptic warhammer, creating devastating ground shocks and unblockable wall bounces.",
                speed = 68f, power = 98f, reach = 88f, defense = 90f,
                domain = "Thunderstruck Mjolnir Wrath",
                domainDesc = "Calls down lightning pillars across the arena floor, electrifying the ground and triggering bound tornadoes.",
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK08_Torgrim.prefab",
                signatureStageId = "ST02"
            },
            // External Dota Heroes (8)
            new RosterEntry
            {
                id = "EX01", name = "Hugo", title = "Raging Berserker", archetype = "Two-Handed / Dual Blades",
                desc = "Bloodthirsty warlord delivering unstoppable leaping overhead strikes and furious dual-blade cross slashes.",
                speed = 84f, power = 94f, reach = 88f, defense = 78f,
                domain = "Bloodfrenzy Rampage",
                domainDesc = "Enters a frenzied state where every hit dealt leeches health and boosts movement speed.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX01_Hugo.prefab",
                signatureStageId = "ST05"
            },
            new RosterEntry
            {
                id = "EX02", name = "Granite", title = "Sanctuary Golem", archetype = "Brute / Stone Colossus",
                desc = "Impenetrable ancient guardian pulverizing opponents with colossal granite fists and seismic fissures.",
                speed = 62f, power = 99f, reach = 86f, defense = 99f,
                domain = "Tectonic Earthshaker",
                domainDesc = "Shatters the ground plane into levitating stone boulders that rain down upon the defender.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX02_Granite.prefab",
                signatureStageId = "ST04"
            },
            new RosterEntry
            {
                id = "EX03", name = "Viper", title = "Shadow Operative", archetype = "Ninja / Dual Daggers",
                desc = "Lethal assassin moving with hyper-velocity, applying corrosive venom that steadily drains enemy life and meter.",
                speed = 95f, power = 80f, reach = 72f, defense = 68f,
                domain = "Venomous Miasma Ambush",
                domainDesc = "Engulfs the stage in toxic green smog, causing continuous poison damage and reducing enemy dash speed.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX03_Viper.prefab",
                signatureStageId = "ST05"
            },
            new RosterEntry
            {
                id = "EX04", name = "Officer K", title = "Security Enforcer", archetype = "Karate / Tactical Brawler",
                desc = "Elite tactical combatant combining devastating martial arts counter-throws with high-impact riot kicks.",
                speed = 86f, power = 85f, reach = 76f, defense = 85f,
                domain = "Total Lockdown Containment",
                domainDesc = "Deploys perimeter laser barriers that violently rebound foes into juggles when wall-splatted.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX04_OfficerK.prefab",
                signatureStageId = "ST06"
            },
            new RosterEntry
            {
                id = "EX05", name = "Kira", title = "Apex Huntress", archetype = "Archer / Energy Bow",
                desc = "Futuristic cyber-ranger controlling full screen length with piercing plasma arrows and anti-air volley spreads.",
                speed = 91f, power = 86f, reach = 95f, defense = 72f,
                domain = "Starfall Piercing Volley",
                domainDesc = "Illuminates target with tracking crosshairs before raining down a continuous barrage of plasma bolts.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX05_Kira.prefab",
                signatureStageId = "ST06"
            },
            new RosterEntry
            {
                id = "EX06", name = "Sentry", title = "Phalanx Sentinel", archetype = "Spearman / Halberd Pike",
                desc = "Disciplined spear specialist keeping attackers at bay with immense mid-range pokes and anti-air sweeps.",
                speed = 75f, power = 88f, reach = 98f, defense = 90f,
                domain = "Impenetrable Spear Wall",
                domainDesc = "Surrounds the fighter with animated spirit halberds automatically intercepting incoming projectiles.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX06_Sentry.prefab",
                signatureStageId = "ST02"
            },
            new RosterEntry
            {
                id = "EX07", name = "Lyra", title = "Arcane Valkyrie", archetype = "Sorceress / Mystic Focus",
                desc = "Hovering ethereal champion firing homing mystic shards and performing graceful aerial dive slashes.",
                speed = 88f, power = 89f, reach = 84f, defense = 78f,
                domain = "Celestial Radiance Sanctum",
                domainDesc = "Floods the arena with blinding holy light, restoring user health while damaging any enemy in proximity.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX07_Lyra.prefab",
                signatureStageId = "ST03"
            },
            new RosterEntry
            {
                id = "EX08", name = "Garrick", title = "Highland Marauder", archetype = "Hammer / Mountain Cleaver",
                desc = "Fierce northern berserker leaping across the screen to crush guards with colossal two-handed cleavers.",
                speed = 76f, power = 96f, reach = 86f, defense = 88f,
                domain = "Glacial Avalanche Slam",
                domainDesc = "Creates a catastrophic ice eruption freezing the enemy solid on impact for a guaranteed full combo.",
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX08_Garrick.prefab",
                signatureStageId = "ST03"
            }
        };

        private static readonly Dictionary<string, CharacterDefinition> _fallback14Characters = new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<CharacterDefinition> GetAuthoritative14Roster()
        {
            var roster14 = new List<CharacterDefinition>();
            for (int i = 0; i < _canonical14List.Length; i++)
            {
                var def = GetCharacterById(_canonical14List[i].id);
                if (def != null && !roster14.Exists(r => r.id == def.id))
                {
                    roster14.Add(def);
                }
            }
            return roster14;
        }

        public static IReadOnlyList<CharacterDefinition> GetAuthoritative18Roster()
        {
            return GetAuthoritative14Roster();
        }

        public static IReadOnlyList<CharacterDefinition> GetAuthoritative30Roster()
        {
            var roster30 = new List<CharacterDefinition>();
            for (int i = 0; i < _canonical14List.Length; i++)
            {
                var def = GetCharacterById(_canonical14List[i].id);
                if (def != null && !roster30.Exists(r => r.id == def.id))
                {
                    roster30.Add(def);
                }
            }
            return roster30;
        }

        public static IReadOnlyList<CharacterDefinition> GetAllCharacters()
        {
            if (_cachedCharacters == null || _cachedCharacters.Count == 0)
            {
                ReloadRegistry();
            }
            return _cachedCharacters;
        }

        public static CharacterDefinition GetCharacterById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string canonicalId = ResolveCanonicalId(id);

            var list = GetAllCharacters();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].id, id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(list[i].id, canonicalId, StringComparison.OrdinalIgnoreCase))
                {
                    return list[i];
                }
            }

            // Fallback for 14-fighter roster
            if (_fallback14Characters.TryGetValue(id, out var extDef)) return extDef;
            if (_fallback14Characters.TryGetValue(canonicalId, out var extDefCanon)) return extDefCanon;

            for (int i = 0; i < _canonical14List.Length; i++)
            {
                var entry = _canonical14List[i];
                if (string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.id, canonicalId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.name, id, StringComparison.OrdinalIgnoreCase))
                {
                    var created = CreateEntryDefinition(entry);
                    _fallback14Characters[id] = created;
                    _fallback14Characters[entry.id] = created;
                    return created;
                }
            }

            return null;
        }

        private static CharacterDefinition CreateEntryDefinition(RosterEntry entry)
        {
            var def = CreateDefaultDefinition(
                entry.id,
                entry.name,
                entry.title,
                entry.archetype,
                entry.desc,
                entry.speed,
                entry.power,
                entry.reach,
                entry.defense,
                entry.domain,
                entry.domainDesc,
                entry.signatureStageId
            );
            def.moveSet = CreateDefaultMoveSet(entry.id);

            GameObject prefab = null;
#if UNITY_EDITOR
            try
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
            }
            catch { }
#endif
            if (prefab == null)
            {
                try { prefab = Resources.Load<GameObject>($"Prefabs/Fighters/{entry.id}_{entry.name}"); } catch { }
            }
            def.modelPrefab = prefab;

#if UNITY_EDITOR
            try
            {
                def.animatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");
            }
            catch { }
#endif
            return def;
        }

        public static void RegisterCharacter(CharacterDefinition def)
        {
            if (ReferenceEquals(def, null) || string.IsNullOrEmpty(def.id)) return;
            if (def.moveSet == null)
            {
                def.moveSet = CreateDefaultMoveSet(def.id);
            }
            if (_cachedCharacters == null) _cachedCharacters = new List<CharacterDefinition>();

            for (int i = 0; i < _cachedCharacters.Count; i++)
            {
                if (string.Equals(_cachedCharacters[i].id, def.id, StringComparison.OrdinalIgnoreCase))
                {
                    _cachedCharacters[i] = def;
                    return;
                }
            }
            _cachedCharacters.Add(def);
        }

        public static bool UnregisterCharacter(string id)
        {
            if (_cachedCharacters == null || string.IsNullOrEmpty(id)) return false;
            string canonicalId = ResolveCanonicalId(id);

            for (int i = _cachedCharacters.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_cachedCharacters[i].id, id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_cachedCharacters[i].id, canonicalId, StringComparison.OrdinalIgnoreCase))
                {
                    _cachedCharacters.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public static void ReorderCharacters(IList<string> orderedIds)
        {
            if (_cachedCharacters == null || orderedIds == null) return;
            var reordered = new List<CharacterDefinition>();

            for (int i = 0; i < orderedIds.Count; i++)
            {
                string targetId = orderedIds[i];
                var found = _cachedCharacters.Find(c => !ReferenceEquals(c, null) && (string.Equals(c.id, targetId, StringComparison.OrdinalIgnoreCase) || string.Equals(ResolveCanonicalId(c.id), ResolveCanonicalId(targetId), StringComparison.OrdinalIgnoreCase)));
                if (!ReferenceEquals(found, null) && !reordered.Exists(r => string.Equals(r.id, found.id, StringComparison.OrdinalIgnoreCase)))
                {
                    reordered.Add(found);
                }
            }

            for (int i = 0; i < _cachedCharacters.Count; i++)
            {
                var candidate = _cachedCharacters[i];
                if (!ReferenceEquals(candidate, null) && !reordered.Exists(r => string.Equals(r.id, candidate.id, StringComparison.OrdinalIgnoreCase)))
                {
                    reordered.Add(candidate);
                }
            }

            _cachedCharacters = reordered;
        }

        public static void ClearRegistry()
        {
            if (_cachedCharacters != null)
            {
                _cachedCharacters.Clear();
            }
        }

        public static void ReloadRegistry()
        {
            _cachedCharacters = new List<CharacterDefinition>();

            // 1. Authoritative 14-Fighter Lineup
            for (int i = 0; i < _canonical14List.Length; i++)
            {
                var entry = _canonical14List[i];
                EnsureRosterFighter(
                    entry.id, entry.name, entry.title, entry.archetype, entry.desc,
                    entry.speed, entry.power, entry.reach, entry.defense,
                    entry.domain, entry.domainDesc, entry.prefabPath, entry.signatureStageId
                );
            }

            // 2. Discover any authored CharacterDefinition assets in Resources or Assets
            try
            {
                var loaded = Resources.LoadAll<CharacterDefinition>("");
                if (loaded != null && loaded.Length > 0)
                {
                    for (int i = 0; i < loaded.Length; i++)
                    {
                        if (loaded[i] != null)
                        {
                            string canonId = ResolveCanonicalId(loaded[i].id);
                            var existing = GetCharacterById(canonId);
                            if (existing != null)
                            {
                                if (loaded[i].modelPrefab != null) existing.modelPrefab = loaded[i].modelPrefab;
                                if (loaded[i].portraitSprite != null) existing.portraitSprite = loaded[i].portraitSprite;
                                if (loaded[i].bannerSprite != null) existing.bannerSprite = loaded[i].bannerSprite;
                                if (loaded[i].moveSet != null) existing.moveSet = loaded[i].moveSet;
                                if (loaded[i].animatorController != null) existing.animatorController = loaded[i].animatorController;
                            }
                            else
                            {
                                if (loaded[i].moveSet == null)
                                {
                                    loaded[i].moveSet = CreateDefaultMoveSet(loaded[i].id);
                                }
                                _cachedCharacters.Add(loaded[i]);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Standalone test runner fallback
            }
        }

        private static void EnsureRosterFighter(
            string id, string name, string title, string archetype, string desc,
            float speed, float power, float reach, float defense, string domain, string domainDesc,
            string prefabFallbackPath, string signatureStageId = "ST01")
        {
            for (int i = 0; i < _cachedCharacters.Count; i++)
            {
                if (string.Equals(_cachedCharacters[i].id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            var def = CreateDefaultDefinition(id, name, title, archetype, desc, speed, power, reach, defense, domain, domainDesc, signatureStageId);
            def.moveSet = CreateDefaultMoveSet(id);

            GameObject prefab = null;
#if UNITY_EDITOR
            try
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabFallbackPath);
            }
            catch { }
#endif
            if (prefab == null)
            {
                try { prefab = Resources.Load<GameObject>(prefabFallbackPath); } catch { }
            }
            def.modelPrefab = prefab;

#if UNITY_EDITOR
            try
            {
                def.animatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");
            }
            catch { }
#endif

            _cachedCharacters.Add(def);
        }

        public static MoveSet CreateDefaultMoveSet(string charId = "Default")
        {
            string canonId = ResolveCanonicalId(charId);
            string shortName = GetCharacterNameShort(canonId);
            MoveSet authored = Resources.Load<MoveSet>($"Content/MoveSets/{canonId}_{shortName}_MoveSet")
                ?? Resources.Load<MoveSet>($"Content/Characters/{canonId}_{shortName}_MoveSet");
            if (authored != null) return authored;

#if UNITY_EDITOR
            try
            {
                authored = UnityEditor.AssetDatabase.LoadAssetAtPath<MoveSet>($"Assets/CombatData/MoveSets/{canonId}_{shortName}_MoveSet.asset");
                if (authored != null) return authored;
            }
            catch { }
#endif

            var ms = MoveSet.CreateSafeInstance<MoveSet>();
            ms.characterId = charId;
            ms.moveSetName = $"{charId} Tactical Set";

            // Default Attack Data assets
            var lightAtk = LoadAttackDataAsset("Kai_LightAttack");
            var medAtk = LoadAttackDataAsset("Kai_MediumAttack");
            var heavyAtk = LoadAttackDataAsset("Kai_HeavyAttack");
            var driveAtk = LoadAttackDataAsset("Kai_DriveSkill");
            var jumpAtk = LoadAttackDataAsset("Kai_JumpAttack");
            var superAtk = LoadAttackDataAsset("Kai_DomainSuper");
            var throwAtk = LoadAttackDataAsset("Kai_Throw");
            var lowAtk = LoadAttackDataAsset("Kai_LowAttack");

            ms.standingL = lightAtk;
            ms.lightAttack = lightAtk;
            ms.crouchingL = lowAtk;
            ms.lowSweepAttack = lowAtk;

            ms.mediumAttack = medAtk;

            ms.standingH = heavyAtk;
            ms.heavyAttack = heavyAtk;
            ms.forwardH = heavyAtk;
            ms.crouchingH = lowAtk;

            ms.neutralS = driveAtk;
            ms.driveSkill = driveAtk;
            ms.forwardS = driveAtk;
            ms.downS = driveAtk;

            ms.airL = jumpAtk;
            ms.airH = jumpAtk;
            ms.airS = driveAtk;

            ms.tacticalThrow = throwAtk;
            ms.domainSuper = superAtk;
            ms.defensiveBurst = driveAtk;

            return ms;
        }

        private static AttackData LoadAttackDataAsset(string assetName)
        {
            AttackData data = Resources.Load<AttackData>($"CombatData/{assetName}");
#if UNITY_EDITOR
            if (data == null)
            {
                try
                {
                    data = UnityEditor.AssetDatabase.LoadAssetAtPath<AttackData>($"Assets/CombatData/{assetName}.asset");
                }
                catch { }
            }
#endif
            return data;
        }

        public static CharacterDefinition CreateDefaultDefinition(
            string id, string name, string title, string archetype, string desc,
            float speed, float power, float reach, float defense,
            string domain, string domainDesc, string signatureStageId = "ST01")
        {
            var def = ScriptableObject.CreateInstance<CharacterDefinition>();
            def.id = id;
            def.displayName = name;
            def.title = title;
            def.archetype = archetype;
            def.description = desc;
            def.speed = speed;
            def.power = power;
            def.reach = reach;
            def.defense = defense;
            def.domainName = domain;
            def.domainDescription = domainDesc;
            def.signatureStageId = signatureStageId;
            return def;
        }
    }
}
