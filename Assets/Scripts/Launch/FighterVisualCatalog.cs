using System;
using System.Collections.Generic;
using System.IO;
using DemonIdol.Combat;

namespace DemonIdol.Launch
{
    public sealed class FighterVisualLook
    {
        public string CharacterId;
        public string DisplayName;
        public string Read;
        public string PrefabAssetPath;
        public string[] ResourceParts;
    }

    /// <summary>
    /// Authoritative visual catalog for the 14 playable Demon Idol fighters.
    /// Maps character identities (C01-C14) to their authentic anime sorcerer prefabs.
    /// </summary>
    public static class FighterVisualCatalog
    {
        private static readonly FighterVisualLook[] Looks =
        {
            // 01. Akio
            new FighterVisualLook
            {
                CharacterId = "C01",
                DisplayName = "Akio",
                Read = "Shadow prodigy in highschool uniform with purple neon and spiked hair.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 02. Aika
            new FighterVisualLook
            {
                CharacterId = "C02",
                DisplayName = "Aika",
                Read = "Straw doll sorceress in highschool uniform with rose red neon and needle pouch.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 03. Ren
            new FighterVisualLook
            {
                CharacterId = "C03",
                DisplayName = "Ren",
                Read = "Divergent brawler with amber neon and close-quarters combat wraps.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C03_Ren.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 04. UnityChan
            new FighterVisualLook
            {
                CharacterId = "C04",
                DisplayName = "UnityChan",
                Read = "Limitless special grade sorceress with cyan neon and infinity spatial field.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C04_UnityChan.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 05. Akane
            new FighterVisualLook
            {
                CharacterId = "C05",
                DisplayName = "Akane",
                Read = "Cursed speech acoustic singer with high zipped collar and purple neon.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 06. Cultist
            new FighterVisualLook
            {
                CharacterId = "C06",
                DisplayName = "Cultist",
                Read = "Occult curse manipulator with dark robes and malevolent emerald neon.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 07. StellarWitch
            new FighterVisualLook
            {
                CharacterId = "C07",
                DisplayName = "StellarWitch",
                Read = "Reverse cursed physician with mint celestial aura and astral alchemy robes.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C07_StellarWitch.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 08. SanpakuChan
            new FighterVisualLook
            {
                CharacterId = "C08",
                DisplayName = "SanpakuChan",
                Read = "Black bird mercenary with amethyst glaze neon and braided silver hair.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 09. PicoChan
            new FighterVisualLook
            {
                CharacterId = "C09",
                DisplayName = "PicoChan",
                Read = "Armored puppet construct with electric lime neon and shoulder cannon relays.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 10. Kiki / Reika
            new FighterVisualLook
            {
                CharacterId = "C10",
                DisplayName = "Reika",
                Read = "Freezing shinigami swordsman with glacial azure neon and katana sheaths.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C10_Reika.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 11. Casual1
            new FighterVisualLook
            {
                CharacterId = "C11",
                DisplayName = "Casual1",
                Read = "Restless fever gambler with streetwear jacket and neon fuchsia aura.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 12. Fina
            new FighterVisualLook
            {
                CharacterId = "C12",
                DisplayName = "Fina",
                Read = "Lead hero protagonist commanding anime combat flow, cursed domain strikes, and unstoppable rushdown.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 13. SapphiArtchan
            new FighterVisualLook
            {
                CharacterId = "C13",
                DisplayName = "SapphiArtchan",
                Read = "Cursed pop idol in gothic midnight dress with pastel violet neon.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C13_SapphiArtchan.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            },
            // 14. Muryotaisu
            new FighterVisualLook
            {
                CharacterId = "C14",
                DisplayName = "Muryotaisu",
                Read = "Ancient calamity king of curses with blood crimson robes and malevolent shrine neon.",
                PrefabAssetPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                ResourceParts = new[] { "Head", "Spine", "Accessory" }
            }
        };

        public static IReadOnlyList<FighterVisualLook> All => Looks;

        public static FighterVisualLook Find(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;

            string canonical = CharacterRegistry.ResolveCanonicalId(characterId);

            for (int i = 0; i < Looks.Length; i++)
            {
                if (string.Equals(Looks[i].CharacterId, characterId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Looks[i].CharacterId, canonical, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Looks[i].DisplayName, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    return Looks[i];
                }
            }
            return null;
        }

        public static bool AllReferencedFilesExist(string repositoryRoot, out string missing)
        {
            for (int i = 0; i < Looks.Length; i++)
            {
                string full = Path.Combine(repositoryRoot, Looks[i].PrefabAssetPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                {
                    missing = Looks[i].PrefabAssetPath;
                    return false;
                }
            }

            missing = null;
            return true;
        }
    }
}
