using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Data
{
    [CreateAssetMenu(fileName = "MDI_RosterDatabase", menuName = "DemonIdol/Data/MDI_RosterDatabase")]
    public class MDIRosterDatabase : ScriptableObject
    {
        private static MDIRosterDatabase s_Instance;
        public static MDIRosterDatabase Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<MDIRosterDatabase>("MDI_RosterDatabase");
                }
                return s_Instance;
            }
            set => s_Instance = value;
        }

        [Header("Full 25 Character Roster")]
        public CharacterDefinition[] characters = new CharacterDefinition[0];

        [Header("Stages")]
        public StageDefinition st07Stage;
        public StageDefinition[] additionalStages = new StageDefinition[0];

        [Header("Global Runtime Controllers")]
        public RuntimeAnimatorController defaultAnimatorController;

        [Header("Contact Sparks & VFX")]
        public GameObject hitSparkLightPrefab;
        public GameObject hitSparkHeavyPrefab;
        public GameObject hitSparkSuperPrefab;
        public GameObject guardSparkPrefab;

        public CharacterDefinition GetCharacterById(string id)
        {
            if (string.IsNullOrEmpty(id) || characters == null) return null;
            for (int i = 0; i < characters.Length; i++)
            {
                var def = characters[i];
                if (def != null && (string.Equals(def.id, id, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(def.displayName, id, StringComparison.OrdinalIgnoreCase)))
                {
                    return def;
                }
            }
            return null;
        }

        public StageDefinition GetStageById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (st07Stage != null && string.Equals(st07Stage.id, id, StringComparison.OrdinalIgnoreCase))
            {
                return st07Stage;
            }
            if (additionalStages != null)
            {
                for (int i = 0; i < additionalStages.Length; i++)
                {
                    if (additionalStages[i] != null && string.Equals(additionalStages[i].id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        return additionalStages[i];
                    }
                }
            }
            return null;
        }
    }
}
