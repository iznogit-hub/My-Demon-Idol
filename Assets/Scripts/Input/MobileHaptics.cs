using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DemonIdol.Combat
{
    public enum HapticImpact
    {
        Light,      // Quick jab / light strike
        Medium,     // Medium attack / guard block
        Heavy,      // Heavy void cleave / launcher / wall splat
        Parry,      // Crisp notification success
        CounterHit  // Heavy sharp impact
    }

    /// <summary>
    /// High-fidelity mobile haptic engine.
    /// Provides iOS CoreHaptics / UIImpactFeedbackGenerator integration for iPhone,
    /// with graceful Android/Editor fallbacks. Zero garbage generation.
    /// </summary>
    public static class MobileHaptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _TriggerImpactHaptic(int style);
        [DllImport("__Internal")]
        private static extern void _TriggerNotificationHaptic(int type);
#endif

        public static void Trigger(HapticImpact impact)
        {
            if (!DemonIdol.UI.SettingsPanel.HapticsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            switch (impact)
            {
                case HapticImpact.Light:
                    _TriggerImpactHaptic(0); // UIImpactFeedbackStyleLight
                    break;
                case HapticImpact.Medium:
                    _TriggerImpactHaptic(1); // UIImpactFeedbackStyleMedium
                    break;
                case HapticImpact.Heavy:
                case HapticImpact.CounterHit:
                    _TriggerImpactHaptic(2); // UIImpactFeedbackStyleHeavy
                    break;
                case HapticImpact.Parry:
                    _TriggerNotificationHaptic(0); // UINotificationFeedbackTypeSuccess
                    break;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android vibration fallback
            switch (impact)
            {
                case HapticImpact.Light:
                    Handheld.Vibrate();
                    break;
                case HapticImpact.Heavy:
                case HapticImpact.CounterHit:
                    Handheld.Vibrate();
                    break;
            }
#else
            // Unity Editor debug log (or silent)
#endif
        }
    }
}
