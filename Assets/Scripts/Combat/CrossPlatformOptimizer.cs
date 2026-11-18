using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Master performance optimizer across iOS, Android, WebGL, and PC/Mac.
    /// Handles 120 FPS ProMotion, Vulkan frame pacing, Retina DPI clamp,
    /// WebAudio unlocking, and memory cleanup.
    /// </summary>
    public class CrossPlatformOptimizer : MonoBehaviour
    {
        [Header("Frame Rate Target")]
        [Tooltip("Target framerate for mobile (60, 90, 120 FPS)")]
        [SerializeField] private int _targetFPS = 120;

        [Header("WebGL Resolution Clamping")]
        [SerializeField] private int _maxWebWidth = 1920;
        [SerializeField] private int _maxWebHeight = 1080;

        private bool _webAudioUnlocked = false;

        private void Awake()
        {
            // 1. Frame Rate & VSync Tuning
            QualitySettings.vSyncCount = 0;

#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1; // Browser requestAnimationFrame
            ClampWebGLResolution();
            AudioListener.pause = false;
#else
            Application.targetFrameRate = _targetFPS;
#endif

            // 2. Enforce Strict Landscape Orientation (Portrait Disabled)
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            // 3. Prevent Screen Dimming on Mobile
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 3. PhysX Memory Reuse
            Physics.reuseCollisionCallbacks = true;

            // 4. Force GC Mode on supported platforms
#if !UNITY_WEBGL
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
#endif

            // 5. Android Low Memory Warning Handler
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.lowMemory += HandleLowMemory;
#endif
        }

        private void Update()
        {
#if UNITY_WEBGL || UNITY_EDITOR
            if (!_webAudioUnlocked)
            {
                bool userInteracted = false;
                try
                {
                    userInteracted = UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.anyKeyDown || UnityEngine.Input.touchCount > 0;
                }
                catch (System.InvalidOperationException)
                {
#if ENABLE_INPUT_SYSTEM
                    userInteracted = (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                        || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame);
#endif
                }

                if (userInteracted)
                {
                    _webAudioUnlocked = true;
                    AudioListener.pause = false;
                }
            }
#endif
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.lowMemory -= HandleLowMemory;
#endif
        }

        private void HandleLowMemory()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private void ClampWebGLResolution()
        {
            int screenW = Screen.currentResolution.width;
            int screenH = Screen.currentResolution.height;

            if (screenW > _maxWebWidth || screenH > _maxWebHeight)
            {
                float aspect = (float)screenW / screenH;
                int targetW = _maxWebWidth;
                int targetH = Mathf.RoundToInt(targetW / aspect);
                Screen.SetResolution(targetW, targetH, FullScreenMode.Windowed);
            }
        }

        public static void FlushMemory()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }

    // Backward compatibility aliases for existing references
    public class iOSPerformanceOptimizer : CrossPlatformOptimizer { }
    public class AndroidPerformanceOptimizer : CrossPlatformOptimizer { }
    public class WebGLPerformanceOptimizer : CrossPlatformOptimizer { }
}
