using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Hip height and weapon reach used to build hurtboxes and attack boxes.
    /// Reach follows the warrior archetype, not a flat default.
    /// </summary>
    public static class BBRosterArchetype
    {
        public struct Info
        {
            public string id;
            public string driveName;
            public BBWarriorType warrior;
            public float hipHeight;
            public float weaponReach;
            public string archetypeKey;
        }

        public static Info Get(string id)
        {
            switch (id)
            {
                case "C01": return Make(id, "Iron Rush", BBWarriorType.Karate, 0.95f, 0.55f, "unarmed");
                case "C02": return Make(id, "Resonance Nail", BBWarriorType.Karate, 0.92f, 0.55f, "unarmed");
                case "C04": return Make(id, "Aero Hop", BBWarriorType.Karate, 0.90f, 0.55f, "unarmed");
                case "C05": return Make(id, "Echoing Mandate", BBWarriorType.Spearman, 0.88f, 1.15f, "spear");
                case "C06": return Make(id, "Cursed Veil", BBWarriorType.Mage, 1.00f, 0.90f, "mage");
                case "C07": return Make(id, "Star Field", BBWarriorType.Sorceress, 0.93f, 0.90f, "sorceress");
                case "C08": return Make(id, "Sanpaku Cross", BBWarriorType.Ninja, 0.90f, 0.70f, "ninja");
                case "C09": return Make(id, "Cyber Clinch", BBWarriorType.Hammer, 0.86f, 0.60f, "grappler");
                case "C10": return Make(id, "Veil Riposte", BBWarriorType.Knight, 0.94f, 0.85f, "sword");
                case "C11": return Make(id, "Casual Draw", BBWarriorType.Karate, 0.92f, 0.55f, "unarmed");
                case "C12": return Make(id, "Hero Cleave", BBWarriorType.Karate, 0.93f, 0.55f, "unarmed");
                case "C13": return Make(id, "Idol Beat", BBWarriorType.Sorceress, 0.90f, 0.90f, "sorceress");
                case "C14": return Make(id, "Shrine Slash", BBWarriorType.TwoHanded, 1.05f, 1.10f, "twohand");
                case "SK02": return Make(id, "Aegis", BBWarriorType.Knight, 1.05f, 0.85f, "sword");
                case "SK03": return Make(id, "Dune Cleaver", BBWarriorType.Ninja, 1.02f, 0.85f, "sword");
                case "SK05": return Make(id, "Abyss Orbit", BBWarriorType.Mage, 1.00f, 0.90f, "mage");
                case "SK06": return Make(id, "Bastion", BBWarriorType.Spearman, 1.08f, 1.15f, "spear");
                case "SK08": return Make(id, "Storm Break", BBWarriorType.Hammer, 1.12f, 1.10f, "hammer");
                case "EX01": return Make(id, "Highland Swing", BBWarriorType.Brute, 1.10f, 0.75f, "brute");
                case "EX02": return Make(id, "Granite Drop", BBWarriorType.Hammer, 1.15f, 1.10f, "hammer");
                case "EX03": return Make(id, "Viper Edge", BBWarriorType.Swordsman, 1.00f, 0.85f, "sword");
                case "EX04": return Make(id, "Discipline", BBWarriorType.Karate, 1.02f, 0.55f, "unarmed");
                case "EX05": return Make(id, "Starfall", BBWarriorType.Swordsman, 0.95f, 0.85f, "sword");
                case "EX06": return Make(id, "Phalanx", BBWarriorType.Spearman, 1.06f, 1.15f, "spear");
                case "EX07": return Make(id, "Aether", BBWarriorType.Sorceress, 0.96f, 0.90f, "sorceress");
                default: return Make(id ?? "C01", "Drive", BBWarriorType.Karate, 0.95f, 0.55f, "unarmed");
            }
        }

        static Info Make(string id, string drive, BBWarriorType warrior, float hip, float reach, string key)
        {
            return new Info
            {
                id = id,
                driveName = drive,
                warrior = warrior,
                hipHeight = hip,
                weaponReach = reach,
                archetypeKey = key
            };
        }
    }
}
