using System;
using UnityEngine;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master Sumi-e design tokens and motion timings for Demon Idol UI.
    /// Inspired by authentic Japanese ink wash painting (suibokuga) and torii vermilion accents,
    /// adapted with SF6-grade modern tactical readability.
    /// </summary>
    public static class SumiStyleTokens
    {
        // --- Color Palette (PROPOSED) ---
        public static readonly Color PaperWhite       = new Color(0.957f, 0.937f, 0.902f, 0.95f); // #F4EFE6
        public static readonly Color PaperWash        = new Color(0.957f, 0.937f, 0.902f, 0.92f);
        public static readonly Color CarbonInk        = new Color(0.102f, 0.102f, 0.102f, 1.0f);  // #1A1A1A
        public static readonly Color CarbonInkWash    = new Color(0.102f, 0.102f, 0.102f, 0.85f);
        public static readonly Color Vermilion        = new Color(0.761f, 0.231f, 0.133f, 1.0f);  // #C23B22
        public static readonly Color ToriiRed         = new Color(0.85f, 0.18f, 0.12f, 1.0f);
        public static readonly Color ParryGold        = new Color(0.831f, 0.686f, 0.216f, 1.0f);  // #D4AF37
        public static readonly Color IndigoVoid       = new Color(0.106f, 0.122f, 0.231f, 0.95f); // #1B1F3B
        public static readonly Color DisabledInk      = new Color(0.102f, 0.102f, 0.102f, 0.40f);

        // --- AMV Kinetic Primary Colors (JJK Overhaul) ---
        public static readonly Color KineticGreen     = new Color(0.000f, 1.000f, 0.231f, 1.0f); // #00FF3B
        public static readonly Color KineticBlue      = new Color(0.000f, 0.220f, 1.000f, 1.0f); // #0038FF
        public static readonly Color KineticRed       = new Color(1.000f, 0.118f, 0.176f, 1.0f); // #FF1E2D
        public static readonly Color KineticYellow    = new Color(1.000f, 0.902f, 0.000f, 1.0f); // #FFE600

        // --- Raimon College Unification Palette ---
        public static readonly Color ObsidianBlack    = new Color(0.071f, 0.078f, 0.102f, 1.0f); // #12141A
        public static readonly Color ObsidianCharcoal = new Color(0.078f, 0.086f, 0.118f, 1.0f); // #14161E
        public static readonly Color RaimonVermilion  = new Color(0.761f, 0.231f, 0.133f, 1.0f); // #C23B22
        public static readonly Color AntiqueGold      = new Color(0.831f, 0.686f, 0.216f, 1.0f); // #D4AF37
        public static readonly Color FogTeal          = new Color(0.039f, 0.078f, 0.071f, 1.0f); // #0A1412
        public static readonly Color FogIndigo        = new Color(0.047f, 0.059f, 0.110f, 1.0f); // #0C0F1C

        // Aliases for UI typography & branding
        public static readonly Color SumiOffWhite     = PaperWhite;
        public static readonly Color BrushGold        = ParryGold;
        public static readonly Color MidnightBlack    = CarbonInk;

        // --- Motion Timing in Seconds (PROPOSED) ---
        public const float PanelInDurationNormal    = 0.20f; // 200ms EaseOutCubic
        public const float PanelOutDurationNormal   = 0.14f; // 140ms EaseInCubic
        public const float ButtonPulseDurationNormal= 0.08f; // 80ms
        public const float InkWipeDurationNormal    = 0.15f; // 150ms
        public const float BannerRoundDurationNormal= 0.70f; // 700ms
        public const float BannerFightDurationNormal= 0.70f; // 700ms
        public const float BannerKODurationNormal   = 1.00f; // 1000ms

        // Reduced-Motion Timing (<=50ms fade, no aggressive shake/wipes)
        public const float ReducedMotionMaxDuration = 0.05f;
        public const float BannerDurationReduced    = 0.40f;

        // Debounce window to prevent rapid double-clicks on menu transitions
        public const float MenuDebounceSeconds      = 0.20f; // 200ms

        // Dynamic motion accessors respecting SettingsPanel.ReducedMotion
        public static bool IsReducedMotion => SettingsPanel.ReducedMotion;
        public static float PanelInDuration => SettingsPanel.ReducedMotion ? ReducedMotionMaxDuration : PanelInDurationNormal;
        public static float PanelOutDuration => SettingsPanel.ReducedMotion ? ReducedMotionMaxDuration : PanelOutDurationNormal;
        public static float ButtonPulseDuration => SettingsPanel.ReducedMotion ? 0.02f : ButtonPulseDurationNormal;
        public static float InkWipeDuration => SettingsPanel.ReducedMotion ? 0.04f : InkWipeDurationNormal;

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return (t * t * t + 1f);
        }

        public static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float GetPanelInDuration(bool reducedMotion) => reducedMotion ? ReducedMotionMaxDuration : PanelInDurationNormal;
        public static float GetPanelOutDuration(bool reducedMotion) => reducedMotion ? ReducedMotionMaxDuration : PanelOutDurationNormal;
        public static float GetBannerDuration(string bannerType, bool reducedMotion)
        {
            if (reducedMotion) return BannerDurationReduced;
            switch (bannerType)
            {
                case "KO": return BannerKODurationNormal;
                default: return BannerFightDurationNormal;
            }
        }
    }
}
