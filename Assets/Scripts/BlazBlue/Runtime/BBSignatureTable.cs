#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Per-fighter signature on top of a shared archetype moveset.
    /// Drive, one special, and the super differ by timing, damage, and reach.
    /// ClipToken is a hook for CMU / Kevin Iglesias / warrior clips. It does not replace the shared asset.
    /// </summary>
    public static class BBSignatureTable
    {
        public struct Signature
        {
            public string driveName;
            public string specialId;
            public string superId;
            public float damageScale;
            public int startupDelta;
            public float reachScale;
            public int hitstopBonus;
            public string clipToken;
        }

        public static bool TryGet(string characterId, string moveId, out Signature signature)
        {
            signature = default;
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(moveId)) return false;
            if (!TryBase(characterId, out signature)) return false;
            bool signatureMove = moveId == "5D" || moveId == "2D" || moveId == "6D" || moveId == "j.D"
                || moveId == signature.specialId || moveId == signature.superId;
            return signatureMove;
        }

        public static bool TryBase(string characterId, out Signature signature)
        {
            switch (characterId)
            {
                case "C01": signature = Make("Iron Rush", "236A", "236236C", 1.05f, 0, 0.90f, 2, "MA Atk P 1"); break;
                case "C02": signature = Make("Resonance Nail", "236A", "236236C", 0.95f, -1, 1.05f, 1, "MA Atk K 3"); break;
                case "C04": signature = Make("Aero Hop", "236A", "236236C", 0.90f, -1, 1.25f, 1, "MA Atk K 2"); break;
                case "C05": signature = Make("Echoing Mandate", "214A", "236236C", 0.92f, 1, 1.20f, 2, "Spearman Attack1"); break;
                case "C06": signature = Make("Cursed Veil", "236A", "236236C", 1.10f, 2, 0.95f, 3, "KI Magic Omni"); break;
                case "C07": signature = Make("Star Field", "22C", "236236C", 1.00f, 0, 1.10f, 3, "KI Magic Direct 1H L"); break;
                case "C08": signature = Make("Sanpaku Cross", "623B", "236236C", 1.08f, 0, 0.80f, 2, "Ninja Attack1"); break;
                case "C09": signature = Make("Cyber Clinch", "623B", "236236C", 1.20f, 2, 1.15f, 4, "Heavy Punch"); break;
                case "C10": signature = Make("Veil Riposte", "214A", "236236C", 1.00f, 1, 0.85f, 2, "Knight shield"); break;
                case "C11": signature = Make("Casual Draw", "236A", "22C", 0.88f, 0, 1.18f, 1, "Female Punch"); break;
                case "C12": signature = Make("Hero Cleave", "623B", "236236C", 1.15f, 1, 1.08f, 4, "MA Atk K 1"); break;
                case "C13": signature = Make("Idol Beat", "22C", "214A", 0.84f, -1, 0.70f, 1, "KI spellcast bubble"); break;
                case "C14": signature = Make("Shrine Slash", "214A", "623B", 1.25f, 2, 1.12f, 5, "2Handed Attack1"); break;
                case "SK02": signature = Make("Aegis", "623B", "236236C", 1.02f, 2, 0.88f, 3, "Knight Attack1"); break;
                case "SK03": signature = Make("Dune Cleaver", "236A", "623B", 1.18f, 1, 1.00f, 4, "Crossbow Attack1"); break;
                case "SK05": signature = Make("Abyss Orbit", "236A", "22C", 1.12f, 2, 1.00f, 3, "Mage Attack1"); break;
                case "SK06": signature = Make("Bastion", "623B", "214A", 1.16f, 3, 1.10f, 4, "KI Polearm"); break;
                case "SK08": signature = Make("Storm Break", "22C", "623B", 1.22f, 2, 1.18f, 5, "Hammer Attack1"); break;
                case "EX01": signature = Make("Highland Swing", "214A", "236236C", 1.14f, 2, 0.98f, 4, "Brute Attack1"); break;
                case "EX02": signature = Make("Granite Drop", "623B", "22C", 1.28f, 3, 1.20f, 5, "Hammer Attack1"); break;
                case "EX03": signature = Make("Viper Edge", "236A", "214A", 1.00f, -1, 0.92f, 2, "Swordsman Attack1"); break;
                case "EX04": signature = Make("Discipline", "623B", "236A", 0.98f, 0, 0.62f, 2, "Karate Attack1"); break;
                case "EX05": signature = Make("Starfall", "236A", "22C", 0.86f, 0, 1.30f, 1, "Archer Attack1"); break;
                case "EX06": signature = Make("Phalanx", "236A", "623B", 0.94f, 1, 1.28f, 2, "Spearman Attack1"); break;
                case "EX07": signature = Make("Aether", "236A", "214A", 1.04f, 0, 1.02f, 3, "Sorceress Attack1"); break;
                default:
                    signature = default;
                    return false;
            }
            return true;
        }

        static Signature Make(string drive, string special, string superId, float damage, int startup, float reach, int hitstop, string clip)
        {
            return new Signature
            {
                driveName = drive,
                specialId = special,
                superId = superId,
                damageScale = damage,
                startupDelta = startup,
                reachScale = reach,
                hitstopBonus = hitstop,
                clipToken = clip
            };
        }

#if UNITY_EDITOR
        public static UnityEngine.AnimationClip ResolveClip(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            // Martial Art Animations (Sample)
            if (token == "MA Atk P 1") return FindSubClip("Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx", "Atk_P_1");
            if (token == "MA Atk P 2") return FindSubClip("Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx", "Atk_P_2");
            if (token == "MA Atk P 3") return FindSubClip("Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx", "Atk_P_3");
            if (token == "MA Atk K 1") return FindSubClip("Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx", "Atk_K_1");
            if (token == "MA Atk K 2") return FindSubClip("Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx", "Atk_K_2");
            if (token == "MA Atk K 3") return FindSubClip("Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx", "Atk_K_3");

            // Kevin Iglesias Human Melee & Spellcasting
            if (token == "KI 1H Attack") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx", "HumanF@Attack1H01_R");
            if (token == "KI 1H Left") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx", "HumanF@Attack1H01_L");
            if (token == "KI 2H") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx", "HumanF@Attack2H01");
            if (token == "KI Polearm") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@AttackPolearm01.fbx", "HumanF@AttackPolearm01");
            if (token == "Knight shield") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx", "HumanF@AttackShield01");
            if (token == "KI spellcast") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx", "HumanF@MagicAttackDirect1H01_R");
            if (token == "KI Magic Direct 1H L") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx", "HumanF@MagicAttackDirect1H01_L");
            if (token == "KI spellcast bubble") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", "HumanF@MagicAttackDirect2H01");
            if (token == "KI Magic Omni") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx", "HumanF@MagicAttackOmni01");
            if (token == "KI Magic Call") return FindSubClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx", "HumanF@MagicAttackCall1H01_L");

            // Fighter Pack Bundle
            if (token == "Heavy Punch") return FindSubClip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX", "Punch");
            if (token == "Female Punch") return FindSubClip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX", "Punch");
            if (token == "Male Punch") return FindSubClip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Male Fighter Mecanim Animation Pack FREE/Animations/Male@Punch.FBX", "Punch");
            if (token == "Berserker Punch") return FindSubClip("Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX", "Punch");

            // ExplosiveLLC Warrior Bundles (1, 2, 3)
            if (token.EndsWith(" Attack1"))
            {
                string warrior = token.Split(' ')[0];
                string[] guids = UnityEditor.AssetDatabase.FindAssets(warrior + "@Attack1 t:Model", new[] { "Assets/ExplosiveLLC" });
                if (guids.Length > 0)
                {
                    string p = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    return FindSubClip(p, "Attack1");
                }
            }
            return null;
        }

        static UnityEngine.AnimationClip FindSubClip(string fbxPath, string name)
        {
            var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (objs == null) return null;
            foreach (var o in objs)
            {
                if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    if (string.IsNullOrEmpty(name) || c.name == name) return c;
                }
            }
            foreach (var o in objs) if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            return null;
        }
#endif
    }
}
