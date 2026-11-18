using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace DemonIdol.Cutscenes
{
    public enum VideoUsage
    {
        StoryIntro,
        StoryVictory,
        UltimateCinematic,
        TutorialDemo,
        General
    }

    public enum VideoAspectMode
    {
        FitLetterbox,
        BleedFill
    }

    [Serializable]
    public class VideoAssetDef
    {
        public string id;
        public VideoUsage usage = VideoUsage.General;
        public string character = "Kai";
        public string relativePath; // e.g. "Cutscenes/kai_intro.mp4"
        public VideoClip bundledClip; // Direct VideoClip reference if bundled
        public float prepareTimeoutSeconds = 3.5f;
        public VideoAspectMode aspectMode = VideoAspectMode.FitLetterbox;
        public bool allowSkip = true;
        public int authoredLockTicks = 20; // For ultimate cinematics
    }

    /// <summary>
    /// Cross-platform video resolver for native Android, iOS, and Editor/Desktop.
    /// Handles Android APK/JAR streaming assets paths without unsafe naked File.Exists checks.
    /// Automatically maps all in-game video requirements across story, ultimates, countdowns, and credits
    /// to the 4 bundled 1080p MP4 items with intelligent fallback mapping.
    /// </summary>
    public static class VideoPathResolver
    {
        public const string VIDEO_CYBERPUNK_INTRO = "Cutscenes/Cyberpunk_city_intro.mp4";
        public const string VIDEO_HERO_DEFEATING_BOSS = "Cinematics/Hero_defeating_skeleton_boss.mp4";
        public const string VIDEO_MATCH_COUNTDOWN = "Cinematics/MY_DEMON_IDOL_launch_countdown.mp4";
        public const string VIDEO_CREDITS_LOOP = "Cinematics/Ending_credits_video_loop.mp4";

        private static readonly Dictionary<string, VideoAssetDef> _registry = new Dictionary<string, VideoAssetDef>(StringComparer.OrdinalIgnoreCase);

        static VideoPathResolver()
        {
            RegisterDefaultAssets();
        }

        private static void RegisterDefaultAssets()
        {
            // 1. Story Intro & Cyberpunk City Cutscenes
            Register(new VideoAssetDef
            {
                id = "story_intro_ch1",
                usage = VideoUsage.StoryIntro,
                character = "Kai",
                relativePath = VIDEO_CYBERPUNK_INTRO,
                prepareTimeoutSeconds = 3.5f,
                aspectMode = VideoAspectMode.FitLetterbox,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "kai_intro.mp4",
                usage = VideoUsage.StoryIntro,
                character = "Kai",
                relativePath = VIDEO_CYBERPUNK_INTRO,
                prepareTimeoutSeconds = 3.5f,
                aspectMode = VideoAspectMode.FitLetterbox,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "Cyberpunk_city_intro",
                usage = VideoUsage.StoryIntro,
                character = "Kai",
                relativePath = VIDEO_CYBERPUNK_INTRO,
                prepareTimeoutSeconds = 3.5f,
                aspectMode = VideoAspectMode.FitLetterbox,
                allowSkip = true
            });

            // 2. Hero Defeating Skeleton Boss (Domain Super Ultimate & Story Victory)
            Register(new VideoAssetDef
            {
                id = "ultimate_astral_kai",
                usage = VideoUsage.UltimateCinematic,
                character = "Kai",
                relativePath = VIDEO_HERO_DEFEATING_BOSS,
                prepareTimeoutSeconds = 2.5f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true,
                authoredLockTicks = 20
            });
            Register(new VideoAssetDef
            {
                id = "kai_domain.mp4",
                usage = VideoUsage.UltimateCinematic,
                character = "Kai",
                relativePath = VIDEO_HERO_DEFEATING_BOSS,
                prepareTimeoutSeconds = 2.5f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true,
                authoredLockTicks = 20
            });
            Register(new VideoAssetDef
            {
                id = "story_victory_ch1",
                usage = VideoUsage.StoryVictory,
                character = "Kai",
                relativePath = VIDEO_HERO_DEFEATING_BOSS,
                prepareTimeoutSeconds = 3.5f,
                aspectMode = VideoAspectMode.FitLetterbox,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "kai_victory.mp4",
                usage = VideoUsage.StoryVictory,
                character = "Kai",
                relativePath = VIDEO_HERO_DEFEATING_BOSS,
                prepareTimeoutSeconds = 3.5f,
                aspectMode = VideoAspectMode.FitLetterbox,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "Hero_defeating_skeleton_boss",
                usage = VideoUsage.UltimateCinematic,
                character = "Kai",
                relativePath = VIDEO_HERO_DEFEATING_BOSS,
                prepareTimeoutSeconds = 2.5f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true,
                authoredLockTicks = 20
            });

            // 3. Match Launch & Round Intro Countdown
            Register(new VideoAssetDef
            {
                id = "match_launch_countdown",
                usage = VideoUsage.General,
                character = "All",
                relativePath = VIDEO_MATCH_COUNTDOWN,
                prepareTimeoutSeconds = 3.0f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "match_launch_countdown.mp4",
                usage = VideoUsage.General,
                character = "All",
                relativePath = VIDEO_MATCH_COUNTDOWN,
                prepareTimeoutSeconds = 3.0f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "MY_DEMON_IDOL_launch_countdown",
                usage = VideoUsage.General,
                character = "All",
                relativePath = VIDEO_MATCH_COUNTDOWN,
                prepareTimeoutSeconds = 3.0f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true
            });

            // 4. Ending Credits Video Loop
            Register(new VideoAssetDef
            {
                id = "credits_ending_loop",
                usage = VideoUsage.General,
                character = "All",
                relativePath = VIDEO_CREDITS_LOOP,
                prepareTimeoutSeconds = 3.0f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "credits_ending_loop.mp4",
                usage = VideoUsage.General,
                character = "All",
                relativePath = VIDEO_CREDITS_LOOP,
                prepareTimeoutSeconds = 3.0f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true
            });
            Register(new VideoAssetDef
            {
                id = "Ending_credits_video_loop",
                usage = VideoUsage.General,
                character = "All",
                relativePath = "Cinematics/Ending_credits_video_loop.mp4",
                prepareTimeoutSeconds = 3.0f,
                aspectMode = VideoAspectMode.BleedFill,
                allowSkip = true
            });
        }

        public static void Register(VideoAssetDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) return;
            _registry[def.id] = def;
        }

        public static VideoAssetDef GetDef(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _registry.TryGetValue(id, out var def);
            return def;
        }

        /// <summary>
        /// Resolves cross-platform VideoPlayer URL or returns clip if direct clip exists.
        /// Android-safe: constructs valid jar:file:// or standard path without throwing IO exceptions.
        /// If a specific video asset is not found on disk, intelligently falls back to one of the 4 bundled MP4s.
        /// </summary>
        public static bool Resolve(string assetIdOrPath, out VideoClip outClip, out string outUrl, out VideoAssetDef outDef)
        {
            outClip = null;
            outUrl = string.Empty;
            outDef = GetDef(assetIdOrPath);

            // Check if this is a manifest slot ID (e.g. S01_GAME_OPENING, C01_SUPER, etc.)
            if (!string.IsNullOrEmpty(assetIdOrPath))
            {
                var slot = DemonIdol.Combat.VideoBindings.GetSlotBinding(assetIdOrPath);
                if (slot != null)
                {
                    // Strict authoring rule: LIVE_SUPER_VFX, LIVE_POSE, UI_CREDITS do not resolve to video files
                    if (slot.safeFallback == DemonIdol.Combat.SafeFallbackType.LIVE_SUPER_VFX ||
                        slot.safeFallback == DemonIdol.Combat.SafeFallbackType.LIVE_POSE ||
                        slot.safeFallback == DemonIdol.Combat.SafeFallbackType.UI_CREDITS)
                    {
                        return false;
                    }

                    if (!string.IsNullOrEmpty(slot.relativePath) && PlatformFileExists(slot.relativePath, out outUrl))
                    {
                        outDef = new VideoAssetDef
                        {
                            id = slot.slotId,
                            relativePath = slot.relativePath,
                            authoredLockTicks = slot.authoredLockTicks,
                            allowSkip = true
                        };
                        return true;
                    }
                }
            }

            if (outDef != null && outDef.bundledClip != null)
            {
                outClip = outDef.bundledClip;
                return true;
            }

            string relativePath = outDef != null ? outDef.relativePath : assetIdOrPath;

            // 1. Check primary relative path
            if (!string.IsNullOrEmpty(relativePath) && PlatformFileExists(relativePath, out outUrl))
            {
                return true;
            }

            // 2. Intelligent fallback to the 4 bundled videos until all individual character assets are authored
            VideoUsage usage = outDef != null ? outDef.usage : InferUsageFromId(assetIdOrPath);
            string fallbackRelativePath = GetFallbackRelativePath(assetIdOrPath, usage);

            if (!string.IsNullOrEmpty(fallbackRelativePath) && PlatformFileExists(fallbackRelativePath, out outUrl))
            {
                if (outDef == null)
                {
                    outDef = new VideoAssetDef
                    {
                        id = assetIdOrPath ?? "fallback_video",
                        usage = usage,
                        relativePath = fallbackRelativePath,
                        aspectMode = (usage == VideoUsage.UltimateCinematic ? VideoAspectMode.BleedFill : VideoAspectMode.FitLetterbox),
                        authoredLockTicks = 20,
                        allowSkip = true
                    };
                }
                return true;
            }

            return false;
        }

        private static VideoUsage InferUsageFromId(string idOrPath)
        {
            if (string.IsNullOrEmpty(idOrPath)) return VideoUsage.General;
            string lower = idOrPath.ToLowerInvariant();
            if (lower.Contains("ultimate") || lower.Contains("domain") || lower.Contains("super")) return VideoUsage.UltimateCinematic;
            if (lower.Contains("victory") || lower.Contains("win")) return VideoUsage.StoryVictory;
            if (lower.Contains("intro") || lower.Contains("chapter")) return VideoUsage.StoryIntro;
            if (lower.Contains("tutorial") || lower.Contains("lesson")) return VideoUsage.TutorialDemo;
            return VideoUsage.General;
        }

        public static string GetFallbackRelativePath(string assetIdOrPath, VideoUsage usage)
        {
            string lower = (assetIdOrPath ?? string.Empty).ToLowerInvariant();

            if (usage == VideoUsage.UltimateCinematic || lower.Contains("ultimate") || lower.Contains("domain") || lower.Contains("boss"))
            {
                return VIDEO_HERO_DEFEATING_BOSS;
            }
            if (usage == VideoUsage.StoryIntro || lower.Contains("intro") || lower.Contains("chapter") || lower.Contains("city"))
            {
                return VIDEO_CYBERPUNK_INTRO;
            }
            if (usage == VideoUsage.StoryVictory || lower.Contains("victory") || lower.Contains("win"))
            {
                return VIDEO_HERO_DEFEATING_BOSS;
            }
            if (lower.Contains("credit") || lower.Contains("ending"))
            {
                return VIDEO_CREDITS_LOOP;
            }
            if (lower.Contains("countdown") || lower.Contains("launch") || lower.Contains("match"))
            {
                return VIDEO_MATCH_COUNTDOWN;
            }

            return VIDEO_CYBERPUNK_INTRO;
        }

        private static bool PlatformFileExists(string relativePath, out string outUrl)
        {
            outUrl = string.Empty;
            if (string.IsNullOrEmpty(relativePath)) return false;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var assetManager = currentActivity.Call<AndroidJavaObject>("getAssets"))
                using (var inputStream = assetManager.Call<AndroidJavaObject>("open", relativePath))
                {
                    if (inputStream != null)
                    {
                        outUrl = Path.Combine(Application.streamingAssetsPath, relativePath);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
#elif UNITY_IOS && !UNITY_EDITOR
            string iosPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (File.Exists(iosPath))
            {
                outUrl = "file://" + iosPath;
                return true;
            }
            return false;
#else
            // Editor / Standalone / Test Environment
            string basePath = Application.streamingAssetsPath;
            if (string.IsNullOrEmpty(basePath))
            {
                basePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets");
            }

            string fullPath = Path.Combine(basePath, relativePath);
            if (File.Exists(fullPath))
            {
                outUrl = fullPath;
                return true;
            }

            // Direct Assets folder check (e.g. Assets/videos/...)
            string directAssetPath = Path.Combine(Application.dataPath, relativePath);
            if (File.Exists(directAssetPath))
            {
                outUrl = "file://" + directAssetPath;
                return true;
            }

            string projAssetPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", relativePath);
            if (File.Exists(projAssetPath))
            {
                outUrl = "file://" + projAssetPath;
                return true;
            }

            // Also check direct workspace relative path
            string directPath = Path.Combine("Assets", "StreamingAssets", relativePath);
            if (File.Exists(directPath))
            {
                outUrl = Path.GetFullPath(directPath);
                return true;
            }

            return false;
#endif
        }
    }
}
