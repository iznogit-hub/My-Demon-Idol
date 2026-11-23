using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DemonIdol.UI;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Resolution-scale and Post-Processing quality profile manager.
    /// Handles mobile-safe 60 FPS scaling and automatically disables expensive Bokeh DOF
    /// and chromatic aberration on Low / Mobile-Battery presets while preserving atmospheric fog
    /// and ACES tone mapping.
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

        public static bool IsDepthOfFieldEnabled(int qualityTier)
        {
            // Low / Mobile-battery disables expensive Bokeh DOF
            return qualityTier > 0;
        }

        public static bool IsChromaticAberrationEnabled(int qualityTier)
        {
            // Low quality disables chromatic aberration
            return qualityTier > 0 && !SettingsPanel.ReducedMotion;
        }

        public static void ApplyToVolume(Volume volume, int qualityTier)
        {
            if (volume == null || volume.profile == null) return;

            if (volume.profile.TryGet<DepthOfField>(out var dof))
            {
                dof.active = IsDepthOfFieldEnabled(qualityTier);
            }

            if (volume.profile.TryGet<ChromaticAberration>(out var chroma))
            {
                chroma.active = IsChromaticAberrationEnabled(qualityTier);
            }
        }
    }
}
