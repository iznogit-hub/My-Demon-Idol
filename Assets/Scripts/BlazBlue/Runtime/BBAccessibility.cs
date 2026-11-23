using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure runtime accessibility contract for BlazBlue combat and presentation systems.
    /// Decouples simulation and presentation from the UI layer (SettingsPanel).
    /// </summary>
    public static class BBAccessibility
    {
        public static Func<bool> ReducedGoreQuery;
        public static Func<bool> ReducedFlashingQuery;
        public static Func<bool> HighContrastHitboxesQuery;

        public static bool ReducedGore => ReducedGoreQuery != null ? ReducedGoreQuery() : PlayerPrefs.GetInt("MDI_ReducedGore", 0) == 1;
        public static bool ReducedFlashing => ReducedFlashingQuery != null ? ReducedFlashingQuery() : PlayerPrefs.GetInt("MDI_ReducedFlashing", 0) == 1;
        public static bool HighContrastHitboxes => HighContrastHitboxesQuery != null ? HighContrastHitboxesQuery() : PlayerPrefs.GetInt("MDI_HighContrastHitboxes", 0) == 1;
    }
}
