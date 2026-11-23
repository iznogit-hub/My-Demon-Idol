using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue
{
    public enum BBArchetypeClass
    {
        Rushdown,
        Zoner,
        Grappler,
        Swordsman,
        AllRounder
    }

    /// <summary>
    /// Hip height, weapon reach, and fighting game archetype classification
    /// used to build hurtboxes, attack boxes, and archetype properties.
    /// Follows Docs/ANIM_PACKS.md 1-to-1.
    /// </summary>
    public static class BBRosterArchetype
    {
        public static readonly string[] LaunchRosterIds = new string[]
        {
            "C01", "C02", "C05", "C06", "C08", "C09", "C11", "C12", "C14", "EX01", "EX04", "SK08"
        };

        public const string LaunchBossId = "SK08";

        public static bool IsLaunchFighter(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < LaunchRosterIds.Length; i++)
            {
                if (string.Equals(LaunchRosterIds[i], id, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public struct Info
        {
            public string id;
            public string driveName;
            public BBWarriorType warrior;
            public float hipHeight;
            public float weaponReach;
            public string archetypeKey;
            public BBArchetypeClass archetypeClass;
        }

        public static Info Get(string id)
        {
            switch (id)
            {
                case "C01": return Make(id, "Iron Rush", BBWarriorType.Karate, 0.95f, 0.85f, "unarmed", BBArchetypeClass.Rushdown);
                case "C02": return Make(id, "Resonance Nail", BBWarriorType.Karate, 0.89f, 0.55f, "unarmed", BBArchetypeClass.Rushdown);
                case "C04": return Make(id, "Aero Hop", BBWarriorType.Karate, 0.88f, 0.55f, "unarmed", BBArchetypeClass.Rushdown);
                case "C05": return Make(id, "Echoing Mandate", BBWarriorType.Spearman, 0.91f, 1.15f, "spear", BBArchetypeClass.Swordsman);
                case "C06": return Make(id, "Cursed Veil", BBWarriorType.Mage, 0.95f, 0.90f, "mage", BBArchetypeClass.Zoner);
                case "C07": return Make(id, "Star Field", BBWarriorType.Sorceress, 0.88f, 0.90f, "sorceress", BBArchetypeClass.Zoner);
                case "C08": return Make(id, "Sanpaku Cross", BBWarriorType.Ninja, 0.90f, 0.70f, "ninja", BBArchetypeClass.Rushdown);
                case "C09": return Make(id, "Cyber Clinch", BBWarriorType.Hammer, 0.86f, 0.60f, "grappler", BBArchetypeClass.Grappler);
                case "C10": return Make(id, "Veil Riposte", BBWarriorType.Knight, 0.93f, 0.85f, "sword", BBArchetypeClass.AllRounder);
                case "C11": return Make(id, "Casual Draw", BBWarriorType.Karate, 0.92f, 0.55f, "unarmed", BBArchetypeClass.Rushdown);
                case "C12": return Make(id, "Hero Cleave", BBWarriorType.Karate, 0.91f, 0.65f, "unarmed", BBArchetypeClass.AllRounder);
                case "C13": return Make(id, "Idol Beat", BBWarriorType.Sorceress, 0.87f, 0.90f, "sorceress", BBArchetypeClass.Zoner);
                case "C14": return Make(id, "Shrine Slash", BBWarriorType.TwoHanded, 0.98f, 1.10f, "twohand", BBArchetypeClass.Swordsman);
                case "SK02": return Make(id, "Aegis", BBWarriorType.Knight, 0.98f, 0.85f, "sword", BBArchetypeClass.AllRounder);
                case "SK03": return Make(id, "Dune Cleaver", BBWarriorType.Ninja, 0.94f, 0.95f, "crossbow", BBArchetypeClass.Zoner);
                case "SK05": return Make(id, "Abyss Orbit", BBWarriorType.Mage, 0.96f, 0.90f, "mage", BBArchetypeClass.Zoner);
                case "SK06": return Make(id, "Bastion", BBWarriorType.Spearman, 0.98f, 1.15f, "spear", BBArchetypeClass.Swordsman);
                case "SK08": return Make(id, "Storm Break", BBWarriorType.Hammer, 0.88f, 1.05f, "hammer", BBArchetypeClass.Grappler);
                case "EX01": return Make(id, "Highland Swing", BBWarriorType.Brute, 1.00f, 0.75f, "brute", BBArchetypeClass.Grappler);
                case "EX02": return Make(id, "Granite Drop", BBWarriorType.Hammer, 0.99f, 1.10f, "hammer", BBArchetypeClass.Grappler);
                case "EX03": return Make(id, "Viper Edge", BBWarriorType.Swordsman, 0.95f, 0.85f, "sword", BBArchetypeClass.Swordsman);
                case "EX04": return Make(id, "Discipline", BBWarriorType.Karate, 0.96f, 0.65f, "unarmed", BBArchetypeClass.AllRounder);
                case "EX05": return Make(id, "Starfall", BBWarriorType.Swordsman, 0.92f, 0.95f, "archer", BBArchetypeClass.Zoner);
                case "EX06": return Make(id, "Phalanx", BBWarriorType.Spearman, 0.99f, 1.15f, "spear", BBArchetypeClass.Swordsman);
                case "EX07": return Make(id, "Aether", BBWarriorType.Sorceress, 0.90f, 0.90f, "sorceress", BBArchetypeClass.Zoner);
                default: return Make(id ?? "C01", "Drive", BBWarriorType.Karate, 0.95f, 0.70f, "unarmed", BBArchetypeClass.AllRounder);
            }
        }

        static Info Make(string id, string drive, BBWarriorType warrior, float hip, float reach, string key, BBArchetypeClass archClass)
        {
            return new Info
            {
                id = id,
                driveName = drive,
                warrior = warrior,
                hipHeight = hip,
                weaponReach = reach,
                archetypeKey = key,
                archetypeClass = archClass
            };
        }
    }
}
