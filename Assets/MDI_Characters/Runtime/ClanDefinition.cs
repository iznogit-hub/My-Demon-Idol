using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Characters
{
    [CreateAssetMenu(fileName = "ClanDefinition", menuName = "Demon Idol/MDI/Clan Definition")]
    public class ClanDefinition : ScriptableObject
    {
        [Header("Clan Identity")]
        public string clanId = "CLAN_ORC";
        public string clanName = "Orc Clan";
        public string stageName = "Iron Sanctum";

        [Header("Theme Colors")]
        public Color primaryColor = new Color(0.6f, 0.2f, 0.1f, 1f);
        public Color secondaryColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [ColorUsage(true, true)]
        public Color neonColor = new Color(1f, 0.33f, 0f, 1f);

        [Header("Fighters")]
        public List<FighterDefinition> fighters = new List<FighterDefinition>();
    }
}
