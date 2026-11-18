using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    [Serializable]
    public class FighterProfile
    {
        public string id;
        public string displayName;
        public string title;
        public string archetype;
        public string description;
        public string prefabPath;

        // Stats (0-100)
        public int speed;
        public int power;
        public int reach;
        public int defense;

        public string domainName;
        public string domainDescription;
        public Color themeColor;
    }

    /// <summary>
    /// Central catalog of the 14 approved fighters in Demon Idol.
    /// Maps Jujutsu Kaisen & Bleach aesthetic anime sorcerers across all game modes.
    /// </summary>
    public static class FighterRosterCatalog
    {
        public static readonly FighterProfile[] Roster = new FighterProfile[]
        {
            // 01. Akio (Megumi Fushiguro homage)
            new FighterProfile
            {
                id = "C01",
                displayName = "AKIO",
                title = "Shadow Chimera Prodigy",
                archetype = "RUSHDOWN / MIXUP",
                description = "Highschool prodigy summoning shikigami shadows, setting up unseeable pincer strikes and shadow puddle mixups.",
                prefabPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                speed = 88,
                power = 82,
                reach = 75,
                defense = 80,
                domainName = "CHIMERA SHADOW GARDEN",
                domainDescription = "Floods the stage with liquid darkness, spawning twin shikigami clones delivering inescapable high-low crossups.",
                themeColor = new Color(0.545f, 0.361f, 0.965f, 1f) // Shadow Violet #8B5CF6
            },
            // 02. Aika (Nobara Kugisaki homage)
            new FighterProfile
            {
                id = "C02",
                displayName = "AIKA",
                title = "Straw Doll Resonance",
                archetype = "ZONER / NEEDLE NAILS",
                description = "Spirited sorceress controlling horizontal space with cursed iron nails and remote hairpin explosive detonations.",
                prefabPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                speed = 84,
                power = 86,
                reach = 85,
                defense = 72,
                domainName = "RESONANCE HAIRPIN REQUIEM",
                domainDescription = "Drives giant nails into the stage floor, triggering linked cursed energy detonations across all distances.",
                themeColor = new Color(0.957f, 0.247f, 0.369f, 1f) // Rose Red #F43F5E
            },
            // 03. Ren (Male Hero Protagonist Counterpart)
            new FighterProfile
            {
                id = "C03",
                displayName = "REN",
                title = "Occult Vanguard Prodigy",
                archetype = "MALE PROTAGONIST / RUSHDOWN",
                description = "Charismatic male lead protagonist wielding dark occult martial arts, high-impact counter strikes, and vortex pressure.",
                prefabPath = "Assets/Prefabs/Characters/C12M_Ren.prefab",
                speed = 95,
                power = 92,
                reach = 76,
                defense = 80,
                domainName = "MALEVOLENT OCCULT DOMAIN",
                domainDescription = "Concentrates cursed spark into instantaneous double-impact shockwaves, shattering defensive guards.",
                themeColor = new Color(1f, 0.165f, 0.427f, 1f) // Neo-Pink #FF2A6D
            },
            // 04. UnityChan (Satoru Gojo homage)
            new FighterProfile
            {
                id = "C04",
                displayName = "UNITYCHAN",
                title = "Limitless Six Eyes",
                archetype = "SPACE DISTORTION / AERIAL",
                description = "Special-grade sorceress commanding infinity barrier defense, gravitational teleport pulls, and aerial divekicks.",
                prefabPath = "Assets/Prefabs/Characters/C04_UnityChan.prefab",
                speed = 95,
                power = 95,
                reach = 90,
                defense = 90,
                domainName = "INFINITE VOID",
                domainDescription = "Floods opponent consciousness with infinite cosmic information, immobilizing them for complete combo punish.",
                themeColor = new Color(0.024f, 0.714f, 0.831f, 1f) // Limitless Cyan #06B6D4
            },
            // 05. Akane (Toge Inumaki homage)
            new FighterProfile
            {
                id = "C05",
                displayName = "AKANE",
                title = "Cursed Speech Singer",
                archetype = "ASSISTANT / ACOUSTIC SHOCK",
                description = "Mysterious acoustic support sorceress using throat-sigil cursed speech words to freeze opponents and break enemy startup.",
                prefabPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                speed = 86,
                power = 76,
                reach = 88,
                defense = 74,
                domainName = "ECHOING MANDATE",
                domainDescription = "Resonates absolute vocal commandments across spacetime, forcing immediate block collapse and wall bounce.",
                themeColor = new Color(0.659f, 0.333f, 0.969f, 1f) // Cursed Plum #A855F7
            },
            // 06. Cultist (Suguru Geto homage)
            new FighterProfile
            {
                id = "C06",
                displayName = "CULTIST",
                title = "Curse Spirit Manipulator",
                archetype = "PUPPET / SWARM SUMMONER",
                description = "Dark occult summoner manipulating swarming cursed spirits, mid-screen trap orbs, and multi-angle vortex pressure.",
                prefabPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                speed = 78,
                power = 92,
                reach = 82,
                defense = 85,
                domainName = "ALL-ENVELOPING UZUMAKI",
                domainDescription = "Condenses thousands of low-grade curses into a concentrated beam of catastrophic occult energy.",
                themeColor = new Color(0.063f, 0.725f, 0.506f, 1f) // Malevolent Emerald #10B981
            },
            // 07. StellarWitch (Shoko Ieiri homage)
            new FighterProfile
            {
                id = "C07",
                displayName = "STELLARWITCH",
                title = "Reverse Cursed Physician",
                archetype = "ETHEREAL ZONER / TRAP",
                description = "Occult physician using reversed cursed energy blades and floating glyphs to control the neutral frame and punish whiffs.",
                prefabPath = "Assets/Prefabs/Characters/C07_StellarWitch.prefab",
                speed = 80,
                power = 75,
                reach = 92,
                defense = 80,
                domainName = "ASTRAL RENEWAL MATRIX",
                domainDescription = "Erects a golden alchemical field reversing damage momentum and emitting tracking energy shards.",
                themeColor = new Color(0.204f, 0.827f, 0.600f, 1f) // Celestial Mint #34D399
            },
            // 08. SanpakuChan (Mei Mei homage)
            new FighterProfile
            {
                id = "C08",
                displayName = "SANPAKUCHAN",
                title = "The Black Bird Striker",
                archetype = "ASSISTANT / BIRD STRIKE",
                description = "Mercenary battle-maiden assistant wielding heavy mid-range strikes, raven shikigami dive bombs, and high-priority counter pokes.",
                prefabPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                speed = 85,
                power = 94,
                reach = 88,
                defense = 76,
                domainName = "BIRD STRIKE EXECUTION",
                domainDescription = "Sacrifices shikigami life in a supersonic death-pact missile that inflicts massive barrier chip and knockdown.",
                themeColor = new Color(0.576f, 0.200f, 0.918f, 1f) // Amethyst Glaze #9333EA
            },
            // 09. PicoChan (Kokichi Muta / Mechamaru homage)
            new FighterProfile
            {
                id = "C09",
                displayName = "PICOCHAN",
                title = "Ultimate Mech Puppet",
                archetype = "ARMORED ARTILLERY / ZONING",
                description = "Miniature armored construct with built-in cursed beam cannons, rocket thrust dashes, and heavy super armor.",
                prefabPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                speed = 65,
                power = 96,
                reach = 95,
                defense = 92,
                domainName = "PICO CANNON BARRAGE",
                domainDescription = "Locks mechanical targeting relays, unloading a screen-wide volley of thermal cursed rockets.",
                themeColor = new Color(0.518f, 0.800f, 0.086f, 1f) // Electric Lime #84CC16
            },
            // 10. Reika (Hero Alt Counterpart)
            new FighterProfile
            {
                id = "C10",
                displayName = "REIKA",
                title = "Occult Shadow Blade",
                archetype = "HERO ALT / SWIFT DUALIST",
                description = "Tactical combat operative weaving lightning-fast shadow blades, evasive sidesteps, and lethal combo confirms.",
                prefabPath = "Assets/Prefabs/Characters/C12B_Reika.prefab",
                speed = 93,
                power = 85,
                reach = 84,
                defense = 74,
                domainName = "PHANTOM SHADOW SLASHES",
                domainDescription = "Unleashes multi-angle phantom shadow strikes, freezing opponent hurtboxes into brittle ice crystals.",
                themeColor = new Color(0.95f, 0.35f, 0.65f, 1f) // Rose Sakura
            },
            // 11. Casual1 (Kinji Hakari homage)
            new FighterProfile
            {
                id = "C11",
                displayName = "CASUAL1",
                title = "Restless Fever Gambler",
                archetype = "RUSHDOWN / FEVER OVERDRIVE",
                description = "Street brawler gambling with slot-machine overdrive surges, immense frame advantage, and relentless forward momentum.",
                prefabPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                speed = 90,
                power = 88,
                reach = 72,
                defense = 82,
                domainName = "IDLE DEATH GAMBLE",
                domainDescription = "Triggers a jackpot fever mode granting infinite super meter and automatic hit-confirm cancels for 30 ticks.",
                themeColor = new Color(0.925f, 0.282f, 0.600f, 1f) // Neon Fuchsia #EC4899
            },
            // 12. Fina (Lead Hero Protagonist)
            new FighterProfile
            {
                id = "C12",
                displayName = "FINA",
                title = "The Lead Protagonist",
                archetype = "NEO-DEMON IDOL / LEAD HERO",
                description = "The central heroine of Demon Idol. Commanding supreme anime combat flow, cursed domain strikes, and unstoppable rushdown.",
                prefabPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                speed = 98,
                power = 92,
                reach = 82,
                defense = 84,
                domainName = "NEO-IDOL CATACLYSM",
                domainDescription = "Manifests a supreme domain of shattered obsidian and radiant pink sakura energy, crushing defensive barriers.",
                themeColor = new Color(1f, 0.165f, 0.427f, 1f) // Neo-Pink #FF2A6D
            },
            // 13. SapphiArtchan (Takada-chan homage)
            new FighterProfile
            {
                id = "C13",
                displayName = "SAPPHIARTCHAN",
                title = "The Cursed Pop Idol",
                archetype = "ILLUSION / FLASHY MIXUP",
                description = "Pop sensation dazzling foes with illusionary spotlights, sparkling heart bursts, and deceptive rhythm mixups.",
                prefabPath = "Assets/Prefabs/Characters/C13_SapphiArtchan.prefab",
                speed = 88,
                power = 78,
                reach = 80,
                defense = 70,
                domainName = "INFINITE IDOL ENCORE",
                domainDescription = "Summons a glowing concert stage blinding the opponent with neon pyrotechnics and crowd cheers.",
                themeColor = new Color(0.753f, 0.518f, 0.988f, 1f) // Pastel Violet #C084FC
            },
            // 14. Muryotaisu (Ryomen Sukuna homage)
            new FighterProfile
            {
                id = "C14",
                displayName = "MURYOTAISU",
                title = "The King of Curses",
                archetype = "ASSISTANT / BOSS VOID SUPPORT",
                description = "Malevolent demonic support overlord slicing through dimensional space with invisible flying slashes and devastating fiery arrows.",
                prefabPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                speed = 96,
                power = 99,
                reach = 92,
                defense = 94,
                domainName = "MALEVOLENT SHRINE",
                domainDescription = "Opens a barrier-free domain relentlessly shredding everything within 20 meters with thousands of continuous slashes.",
                themeColor = new Color(0.937f, 0.267f, 0.267f, 1f) // Blood Crimson #EF4444
            }
        };

        public static FighterProfile GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return Roster[0];

            string normalized = id.Trim();

            // Name & Canonical ID mapping for the 14 characters
            if (normalized.Equals("akio", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C01", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M01", StringComparison.OrdinalIgnoreCase) || normalized.Equals("kai", StringComparison.OrdinalIgnoreCase)) return Roster[0];
            if (normalized.Equals("aika", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C02", StringComparison.OrdinalIgnoreCase) || normalized.Equals("F01", StringComparison.OrdinalIgnoreCase)) return Roster[1];
            if (normalized.Equals("ren", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C12M", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C03", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M02", StringComparison.OrdinalIgnoreCase) || normalized.Equals("malehero", StringComparison.OrdinalIgnoreCase)) return Roster[2];
            if (normalized.Equals("unitychan", StringComparison.OrdinalIgnoreCase) || normalized.Equals("unitychan_dynamic", StringComparison.OrdinalIgnoreCase) || normalized.Equals("gojo", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C04", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M03", StringComparison.OrdinalIgnoreCase) || normalized.Equals("zev", StringComparison.OrdinalIgnoreCase)) return Roster[3];
            if (normalized.Equals("akane", StringComparison.OrdinalIgnoreCase) || normalized.Equals("anggirl", StringComparison.OrdinalIgnoreCase) || normalized.Equals("inumaki", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C05", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M06", StringComparison.OrdinalIgnoreCase) || normalized.Equals("kuro", StringComparison.OrdinalIgnoreCase)) return Roster[4];
            if (normalized.Equals("cultist", StringComparison.OrdinalIgnoreCase) || normalized.Equals("geto", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C06", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M11", StringComparison.OrdinalIgnoreCase) || normalized.Equals("victor", StringComparison.OrdinalIgnoreCase)) return Roster[5];
            if (normalized.Equals("stellarwitch", StringComparison.OrdinalIgnoreCase) || normalized.Equals("shoko", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C07", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M12", StringComparison.OrdinalIgnoreCase) || normalized.Equals("kael", StringComparison.OrdinalIgnoreCase)) return Roster[6];
            if (normalized.Equals("sanpakuchan", StringComparison.OrdinalIgnoreCase) || normalized.Equals("meimei", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C08", StringComparison.OrdinalIgnoreCase) || normalized.Equals("F02", StringComparison.OrdinalIgnoreCase) || normalized.Equals("mira", StringComparison.OrdinalIgnoreCase)) return Roster[7];
            if (normalized.Equals("picochan", StringComparison.OrdinalIgnoreCase) || normalized.Equals("mechamaru", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C09", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M08", StringComparison.OrdinalIgnoreCase) || normalized.Equals("boran", StringComparison.OrdinalIgnoreCase)) return Roster[8];
            if (normalized.Equals("reika", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C12B", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C10", StringComparison.OrdinalIgnoreCase) || normalized.Equals("kiki", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M09", StringComparison.OrdinalIgnoreCase)) return Roster[9];
            if (normalized.Equals("casual1", StringComparison.OrdinalIgnoreCase) || normalized.Equals("hakari", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C11", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M07", StringComparison.OrdinalIgnoreCase) || normalized.Equals("shin", StringComparison.OrdinalIgnoreCase)) return Roster[10];
            if (normalized.Equals("fina", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C12", StringComparison.OrdinalIgnoreCase) || normalized.Equals("underwear1", StringComparison.OrdinalIgnoreCase) || normalized.Equals("maki", StringComparison.OrdinalIgnoreCase) || normalized.Equals("hero", StringComparison.OrdinalIgnoreCase) || normalized.Equals("protagonist", StringComparison.OrdinalIgnoreCase)) return Roster[11];
            if (normalized.Equals("sapphiartchan", StringComparison.OrdinalIgnoreCase) || normalized.Equals("takada", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C13", StringComparison.OrdinalIgnoreCase) || normalized.Equals("F05", StringComparison.OrdinalIgnoreCase) || normalized.Equals("chiyo", StringComparison.OrdinalIgnoreCase)) return Roster[12];
            if (normalized.Equals("muryotaisu", StringComparison.OrdinalIgnoreCase) || normalized.Equals("sukuna", StringComparison.OrdinalIgnoreCase) || normalized.Equals("C14", StringComparison.OrdinalIgnoreCase) || normalized.Equals("M04", StringComparison.OrdinalIgnoreCase) || normalized.Equals("dante", StringComparison.OrdinalIgnoreCase) || normalized.Equals("F03", StringComparison.OrdinalIgnoreCase) || normalized.Equals("nox", StringComparison.OrdinalIgnoreCase)) return Roster[13];

            foreach (var f in Roster)
            {
                if (f.id.Equals(normalized, StringComparison.OrdinalIgnoreCase)) return f;
            }
            return Roster[0];
        }
    }
}
