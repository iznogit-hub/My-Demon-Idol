using System;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Resolution-scale proposals for the settings profiles. Custom uses the saved scale.
    /// </summary>
    public static class GraphicsProfiles
    {
        public const float LowScale = 0.70f;
        public const float BalancedScale = 0.85f;
        public const float HighScale = 1.00f;

        public static float ScaleForTier(int qualityTier, float customScale)
        {
            switch (qualityTier)
            {
                case 0: return LowScale;
                case 1: return BalancedScale;
                case 2: return HighScale;
                default:
                    if (customScale < 0.50f)
                        return HighScale;
                    if (customScale > 1.00f)
                        return HighScale;
                    return customScale;
            }
        }

        public static string ProfileName(int qualityTier)
        {
            switch (qualityTier)
            {
                case 0: return "Low";
                case 1: return "Balanced";
                case 2: return "High";
                default: return "Custom";
            }
        }
    }
}
