#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Characters;

namespace DemonIdol.Characters.Editor
{
    [InitializeOnLoad]
    public static class MDI_RosterAssetCreator
    {
        static MDI_RosterAssetCreator()
        {
            EditorApplication.delayCall += AutoGenerateIfMissing;
        }

        public static void AutoGenerateIfMissing()
        {
            if (!File.Exists("Assets/MDI_Characters/Clans/Orc/ClanDefinition_Orc.asset"))
            {
                GenerateAllRosterDefinitions();
            }
        }

        private struct FighterSpec
        {
            public string id;
            public string displayName;
            public string clan;
            public FighterGender gender;
            public string folder;
            public string neonHex;
            public float neonIntensity;
            public string accessoryName;
            public HumanoidBoneTarget targetBone;
            public Color skinTint;
            public Color hairTint;
            public Color primaryTint;
            public Color secondaryTint;
            public Color accentTint;
        }

        private struct ClanSpec
        {
            public string clanId;
            public string clanName;
            public string stageName;
            public string folder;
            public Color primaryColor;
            public Color secondaryColor;
            public string neonHex;
            public string[] fighterIds;
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Generate All Roster Definitions", priority = 1)]
        public static void GenerateAllRosterDefinitions()
        {
            var clans = new ClanSpec[]
            {
                new ClanSpec { clanId = "CLAN_ORC", clanName = "Orc Clan", stageName = "Iron Sanctum", folder = "Orc",
                    primaryColor = new Color(0.7f, 0.25f, 0.1f), secondaryColor = new Color(0.2f, 0.15f, 0.1f), neonHex = "#FF5500",
                    fighterIds = new string[] { "ORC_ARIN", "ORC_BORAN", "ORC_KURO" } },

                new ClanSpec { clanId = "CLAN_MEDIEVAL", clanName = "Medieval Clan", stageName = "Lantern Gate", folder = "Medieval",
                    primaryColor = new Color(0.85f, 0.7f, 0.2f), secondaryColor = new Color(0.3f, 0.3f, 0.35f), neonHex = "#FFD700",
                    fighterIds = new string[] { "MED_LEON", "MED_VICTOR", "MED_CHIYO" } },

                new ClanSpec { clanId = "CLAN_OCCULT", clanName = "Occult Clan", stageName = "Whispering Grove", folder = "Occult",
                    primaryColor = new Color(0.5f, 0.1f, 0.7f), secondaryColor = new Color(0.15f, 0.05f, 0.25f), neonHex = "#9D00FF",
                    fighterIds = new string[] { "OCC_REIKA" } },

                new ClanSpec { clanId = "CLAN_NEON", clanName = "Neon Clan", stageName = "Neon Shrine", folder = "Neon",
                    primaryColor = new Color(0.0f, 0.7f, 0.9f), secondaryColor = new Color(0.1f, 0.1f, 0.2f), neonHex = "#00D4FF",
                    fighterIds = new string[] { "NEO_ZEV", "NEO_JIN", "NEO_YUKI" } },

                new ClanSpec { clanId = "CLAN_ASH", clanName = "Ash Clan", stageName = "Ashen Amphitheatre", folder = "Ash",
                    primaryColor = new Color(0.6f, 0.5f, 0.4f), secondaryColor = new Color(0.25f, 0.2f, 0.2f), neonHex = "#FF8C00",
                    fighterIds = new string[] { "ASH_SHIN", "ASH_MIRA", "ASH_VESPER" } },

                new ClanSpec { clanId = "CLAN_DEMON", clanName = "Demon Clan", stageName = "Hollow Throne", folder = "Demon",
                    primaryColor = new Color(0.8f, 0.05f, 0.15f), secondaryColor = new Color(0.1f, 0.02f, 0.05f), neonHex = "#DC143C",
                    fighterIds = new string[] { "DEM_DANTE", "DEM_KAEL", "DEM_NOX" } }
            };

            var fighters = new FighterSpec[]
            {
                // 1. Orc Clan
                new FighterSpec { id = "ORC_ARIN", displayName = "Arin the Iron Vow", clan = "Orc", gender = FighterGender.Male, folder = "Orc/Arin",
                    neonHex = "#FF5500", neonIntensity = 2.2f, accessoryName = "Heavy Riveted Iron Pauldron", targetBone = HumanoidBoneTarget.LeftShoulder,
                    skinTint = new Color(0.65f, 0.72f, 0.55f), hairTint = new Color(0.15f, 0.15f, 0.15f), primaryTint = new Color(0.35f, 0.35f, 0.38f),
                    secondaryTint = new Color(0.6f, 0.3f, 0.15f), accentTint = new Color(1f, 0.45f, 0.1f) },

                new FighterSpec { id = "ORC_BORAN", displayName = "Boran", clan = "Orc", gender = FighterGender.Male, folder = "Orc/Boran",
                    neonHex = "#FF1133", neonIntensity = 2.5f, accessoryName = "Spiked Berserker Tusk Mask", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.7f, 0.65f, 0.55f), hairTint = new Color(0.4f, 0.1f, 0.1f), primaryTint = new Color(0.5f, 0.15f, 0.15f),
                    secondaryTint = new Color(0.2f, 0.18f, 0.15f), accentTint = new Color(0.9f, 0.1f, 0.2f) },

                new FighterSpec { id = "ORC_KURO", displayName = "Kuro", clan = "Orc", gender = FighterGender.Male, folder = "Orc/Kuro",
                    neonHex = "#00FF66", neonIntensity = 2.0f, accessoryName = "Shrouded Assassin Cowl", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.55f, 0.65f, 0.6f), hairTint = new Color(0.1f, 0.12f, 0.1f), primaryTint = new Color(0.15f, 0.18f, 0.15f),
                    secondaryTint = new Color(0.25f, 0.3f, 0.25f), accentTint = new Color(0.1f, 0.9f, 0.4f) },

                // 2. Medieval Clan
                new FighterSpec { id = "MED_LEON", displayName = "Leon", clan = "Medieval", gender = FighterGender.Male, folder = "Medieval/Leon",
                    neonHex = "#FFD700", neonIntensity = 2.0f, accessoryName = "Winged Knight Sallet Crest", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.95f, 0.85f, 0.78f), hairTint = new Color(0.85f, 0.75f, 0.45f), primaryTint = new Color(0.85f, 0.88f, 0.92f),
                    secondaryTint = new Color(0.15f, 0.25f, 0.55f), accentTint = new Color(1.0f, 0.85f, 0.2f) },

                new FighterSpec { id = "MED_VICTOR", displayName = "Victor", clan = "Medieval", gender = FighterGender.Male, folder = "Medieval/Victor",
                    neonHex = "#A0C0D0", neonIntensity = 2.0f, accessoryName = "Reinforced Plate Gorget & Spikes", targetBone = HumanoidBoneTarget.Neck,
                    skinTint = new Color(0.9f, 0.82f, 0.75f), hairTint = new Color(0.35f, 0.3f, 0.25f), primaryTint = new Color(0.45f, 0.48f, 0.52f),
                    secondaryTint = new Color(0.25f, 0.2f, 0.2f), accentTint = new Color(0.7f, 0.8f, 0.9f) },

                new FighterSpec { id = "MED_CHIYO", displayName = "Chiyo", clan = "Medieval", gender = FighterGender.Female, folder = "Medieval/Chiyo",
                    neonHex = "#FF69B4", neonIntensity = 2.2f, accessoryName = "Kunai Sash & Flowing Crimson Scarf", targetBone = HumanoidBoneTarget.Neck,
                    skinTint = new Color(0.98f, 0.88f, 0.82f), hairTint = new Color(0.12f, 0.1f, 0.15f), primaryTint = new Color(0.2f, 0.18f, 0.22f),
                    secondaryTint = new Color(0.7f, 0.15f, 0.25f), accentTint = new Color(1.0f, 0.45f, 0.65f) },

                // 3. Occult Clan
                new FighterSpec { id = "OCC_REIKA", displayName = "Reika the Veilweaver", clan = "Occult", gender = FighterGender.Female, folder = "Occult/Reika",
                    neonHex = "#00FFFF", neonIntensity = 2.5f, accessoryName = "Mystic Veil & Prayer Bead Halo", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.98f, 0.9f, 0.88f), hairTint = new Color(0.4f, 0.2f, 0.6f), primaryTint = new Color(0.25f, 0.18f, 0.35f),
                    secondaryTint = new Color(0.15f, 0.45f, 0.55f), accentTint = new Color(0.2f, 0.95f, 1.0f) },

                // 4. Neon Clan
                new FighterSpec { id = "NEO_ZEV", displayName = "Zev the Stormbound", clan = "Neon", gender = FighterGender.Male, folder = "Neon/Zev",
                    neonHex = "#00D4FF", neonIntensity = 2.5f, accessoryName = "High-Voltage Cyber Conduit Collar", targetBone = HumanoidBoneTarget.Neck,
                    skinTint = new Color(0.92f, 0.85f, 0.8f), hairTint = new Color(0.2f, 0.4f, 0.85f), primaryTint = new Color(0.12f, 0.14f, 0.18f),
                    secondaryTint = new Color(0.25f, 0.3f, 0.4f), accentTint = new Color(0.1f, 0.8f, 1.0f) },

                new FighterSpec { id = "NEO_JIN", displayName = "Jin", clan = "Neon", gender = FighterGender.Male, folder = "Neon/Jin",
                    neonHex = "#00FF88", neonIntensity = 2.4f, accessoryName = "Tactical HUD Visor Plate", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.94f, 0.86f, 0.8f), hairTint = new Color(0.12f, 0.12f, 0.14f), primaryTint = new Color(0.15f, 0.2f, 0.18f),
                    secondaryTint = new Color(0.2f, 0.25f, 0.22f), accentTint = new Color(0.15f, 1.0f, 0.5f) },

                new FighterSpec { id = "NEO_YUKI", displayName = "Yuki", clan = "Neon", gender = FighterGender.Female, folder = "Neon/Yuki",
                    neonHex = "#FF007F", neonIntensity = 2.6f, accessoryName = "Exposed Carbon Shoulder Reactor Core", targetBone = HumanoidBoneTarget.LeftShoulder,
                    skinTint = new Color(0.98f, 0.9f, 0.88f), hairTint = new Color(0.95f, 0.4f, 0.7f), primaryTint = new Color(0.18f, 0.15f, 0.2f),
                    secondaryTint = new Color(0.85f, 0.85f, 0.9f), accentTint = new Color(1.0f, 0.1f, 0.6f) },

                // 5. Ash Clan
                new FighterSpec { id = "ASH_SHIN", displayName = "Shin", clan = "Ash", gender = FighterGender.Male, folder = "Ash/Shin",
                    neonHex = "#FF8C00", neonIntensity = 2.2f, accessoryName = "Prayer Bead Rosary & Straw Hat", targetBone = HumanoidBoneTarget.Neck,
                    skinTint = new Color(0.92f, 0.84f, 0.76f), hairTint = new Color(0.2f, 0.15f, 0.12f), primaryTint = new Color(0.45f, 0.38f, 0.3f),
                    secondaryTint = new Color(0.65f, 0.45f, 0.25f), accentTint = new Color(1.0f, 0.55f, 0.1f) },

                new FighterSpec { id = "ASH_MIRA", displayName = "Mira the Ash Singer", clan = "Ash", gender = FighterGender.Female, folder = "Ash/Mira",
                    neonHex = "#E06666", neonIntensity = 2.1f, accessoryName = "Flowing Silk Ribbon Battle Sash", targetBone = HumanoidBoneTarget.Hips,
                    skinTint = new Color(0.98f, 0.9f, 0.86f), hairTint = new Color(0.7f, 0.45f, 0.45f), primaryTint = new Color(0.4f, 0.32f, 0.32f),
                    secondaryTint = new Color(0.6f, 0.25f, 0.25f), accentTint = new Color(0.95f, 0.45f, 0.45f) },

                new FighterSpec { id = "ASH_VESPER", displayName = "Vesper", clan = "Ash", gender = FighterGender.Female, folder = "Ash/Vesper",
                    neonHex = "#F0F8FF", neonIntensity = 2.0f, accessoryName = "Twin Crossed Scabbards & Dagger Holster", targetBone = HumanoidBoneTarget.Spine,
                    skinTint = new Color(0.96f, 0.88f, 0.84f), hairTint = new Color(0.25f, 0.25f, 0.28f), primaryTint = new Color(0.25f, 0.24f, 0.26f),
                    secondaryTint = new Color(0.5f, 0.5f, 0.55f), accentTint = new Color(0.9f, 0.95f, 1.0f) },

                // 6. Demon Clan
                new FighterSpec { id = "DEM_DANTE", displayName = "Dante", clan = "Demon", gender = FighterGender.Male, folder = "Demon/Dante",
                    neonHex = "#DC143C", neonIntensity = 2.4f, accessoryName = "Demonic Spine Mantle & Claws", targetBone = HumanoidBoneTarget.Spine,
                    skinTint = new Color(0.92f, 0.85f, 0.82f), hairTint = new Color(0.95f, 0.95f, 0.98f), primaryTint = new Color(0.55f, 0.08f, 0.12f),
                    secondaryTint = new Color(0.18f, 0.12f, 0.15f), accentTint = new Color(0.95f, 0.1f, 0.2f) },

                new FighterSpec { id = "DEM_KAEL", displayName = "Kael", clan = "Demon", gender = FighterGender.Male, folder = "Demon/Kael",
                    neonHex = "#FF4500", neonIntensity = 2.5f, accessoryName = "Obsidian Curved Horns & Wing Carapace", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.85f, 0.75f, 0.72f), hairTint = new Color(0.18f, 0.1f, 0.1f), primaryTint = new Color(0.15f, 0.1f, 0.12f),
                    secondaryTint = new Color(0.5f, 0.15f, 0.08f), accentTint = new Color(1.0f, 0.35f, 0.05f) },

                new FighterSpec { id = "DEM_NOX", displayName = "Nox the Hollow Crown", clan = "Demon", gender = FighterGender.Female, folder = "Demon/Nox",
                    neonHex = "#8A2BE2", neonIntensity = 2.6f, accessoryName = "Floating Fractured Horn Crown", targetBone = HumanoidBoneTarget.Head,
                    skinTint = new Color(0.98f, 0.9f, 0.92f), hairTint = new Color(0.15f, 0.12f, 0.2f), primaryTint = new Color(0.15f, 0.1f, 0.2f),
                    secondaryTint = new Color(0.35f, 0.12f, 0.45f), accentTint = new Color(0.7f, 0.2f, 1.0f) }
            };

            var createdFighters = new Dictionary<string, FighterDefinition>();

            // 1. Create FighterDefinitions
            foreach (var fSpec in fighters)
            {
                string folderPath = $"Assets/MDI_Characters/Clans/{fSpec.folder}";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/FighterDefinition_{fSpec.id}.asset";
                FighterDefinition fDef = AssetDatabase.LoadAssetAtPath<FighterDefinition>(assetPath);
                if (fDef == null)
                {
                    fDef = ScriptableObject.CreateInstance<FighterDefinition>();
                    AssetDatabase.CreateAsset(fDef, assetPath);
                }

                fDef.id = fSpec.id;
                fDef.displayName = fSpec.displayName;
                fDef.clan = fSpec.clan;
                fDef.gender = fSpec.gender;
                ColorUtility.TryParseHtmlString(fSpec.neonHex, out Color neonCol);
                fDef.emissiveNeonColor = neonCol;
                fDef.emissiveIntensity = fSpec.neonIntensity;
                fDef.skinTint = fSpec.skinTint;
                fDef.hairTint = fSpec.hairTint;
                fDef.primaryTint = fSpec.primaryTint;
                fDef.secondaryTint = fSpec.secondaryTint;
                fDef.accentTint = fSpec.accentTint;

                // Configure default accessory slot
                fDef.accessories.Clear();
                fDef.accessories.Add(new AccessoryAttachment
                {
                    accessoryName = fSpec.accessoryName,
                    targetBone = fSpec.targetBone,
                    localPosition = Vector3.zero,
                    localEulerAngles = Vector3.zero,
                    localScale = Vector3.one
                });

                EditorUtility.SetDirty(fDef);
                createdFighters[fSpec.id] = fDef;
            }

            // 2. Create ClanDefinitions
            foreach (var cSpec in clans)
            {
                string folderPath = $"Assets/MDI_Characters/Clans/{cSpec.folder}";
                string assetPath = $"{folderPath}/ClanDefinition_{cSpec.folder}.asset";

                ClanDefinition cDef = AssetDatabase.LoadAssetAtPath<ClanDefinition>(assetPath);
                if (cDef == null)
                {
                    cDef = ScriptableObject.CreateInstance<ClanDefinition>();
                    AssetDatabase.CreateAsset(cDef, assetPath);
                }

                cDef.clanId = cSpec.clanId;
                cDef.clanName = cSpec.clanName;
                cDef.stageName = cSpec.stageName;
                cDef.primaryColor = cSpec.primaryColor;
                cDef.secondaryColor = cSpec.secondaryColor;
                ColorUtility.TryParseHtmlString(cSpec.neonHex, out Color clanNeon);
                cDef.neonColor = clanNeon;

                cDef.fighters.Clear();
                foreach (string fId in cSpec.fighterIds)
                {
                    if (createdFighters.TryGetValue(fId, out FighterDefinition f))
                    {
                        cDef.fighters.Add(f);
                    }
                }

                EditorUtility.SetDirty(cDef);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#4CAF50><b>[MDI Roster]</b></color> Successfully generated 6 ClanDefinitions and 18 FighterDefinitions with distinct neon palettes and accessories!");
        }
    }
}
#endif
