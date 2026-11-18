using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Video;
using DemonIdol.Cutscenes;

namespace DemonIdol.Combat
{
    public enum SafeFallbackType
    {
        NEUTRAL_IMAGE,
        LIVE_POSE,
        LIVE_SUPER_VFX,
        UI_CREDITS
    }

    [Serializable]
    public class MoveVideoBinding
    {
        public string moveOrSuperId = "ultimate_astral_kai";
        public VideoClip directClip;
        public string relativePath = "Cinematics/Hero_defeating_skeleton_boss.mp4";
        public string alias = "Hero_defeating_skeleton_boss";
        public VideoAspectMode aspectMode = VideoAspectMode.BleedFill;
        public int authoredLockTicks = 20;
        public string fallbackRelativePath = "Cinematics/Hero_defeating_skeleton_boss.mp4";
    }

    [Serializable]
    public class CinematicSlotBinding
    {
        public string slotId;
        public string filename;
        public string relativePath;
        public float durationTargetSeconds;
        public string trigger;
        public string ownerOrCharacter;
        public bool isRequired;
        public string placeholderAlias;
        public SafeFallbackType safeFallback;
        public string captionTableKey;
        public string audioTrackKey;
        public VideoClip directClip;
        public int authoredLockTicks = 20;
    }

    /// <summary>
    /// Content Contract: VideoBindings ScriptableObject and Cinematic Manifest Engine.
    /// Governs the 38-slot MP4 cinematic inventory (8 shared + 30 per-character slots).
    /// Enforces:
    /// - Exactly 38 slots (25 required, 13 optional).
    /// - Strict placeholder mapping (PH_CLIP_1 exclusively for S06).
    /// - Safe path validation (no absolute, relative traversal, or remote URLs).
    /// - Byte-level deduplication checking across packaged streaming assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVideoBindings", menuName = "DemonIdol/Combat/VideoBindings")]
    public class VideoBindings : ScriptableObject
    {
        public const string PH_CLIP_1_PATH = "Cinematics/Hero_defeating_skeleton_boss.mp4";
        public const string PH_CLIP_2_PATH = "Cutscenes/Cyberpunk_city_intro.mp4";
        public const string PH_CLIP_3_PATH = "Cinematics/MY_DEMON_IDOL_launch_countdown.mp4";
        public const string PH_CLIP_4_PATH = "Cinematics/Ending_credits_video_loop.mp4";
        public const string NEUTRAL_IMAGE_PATH = "Assets/Alebardium/Bloodlines UI/Textures/Frame/Frame_background.png";

        [Header("Character Identity")]
        public string characterId = "Kai";

        [Header("Cinematic Move Bindings")]
        public List<MoveVideoBinding> bindings = new List<MoveVideoBinding>();

        [Header("Global Fallback")]
        public string defaultFallbackPath = PH_CLIP_1_PATH;

        private static Dictionary<string, CinematicSlotBinding> _manifestSlots;
        private static Dictionary<string, Dictionary<string, string>> _localizedCaptions;

        static VideoBindings()
        {
            InitializeManifest();
            InitializeCaptions();
        }

        public static IReadOnlyDictionary<string, CinematicSlotBinding> ManifestSlots
        {
            get
            {
                if (_manifestSlots == null || _manifestSlots.Count == 0)
                {
                    InitializeManifest();
                }
                return _manifestSlots;
            }
        }

        public static CinematicSlotBinding GetSlotBinding(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            ManifestSlots.TryGetValue(slotId, out var binding);
            return binding;
        }

        public static void SetSlotBindingPath(string slotId, string newRelativePath)
        {
            if (string.IsNullOrEmpty(slotId)) return;
            if (_manifestSlots != null && _manifestSlots.TryGetValue(slotId, out var binding))
            {
                binding.relativePath = newRelativePath;
            }
        }

        public static void ResetManifest()
        {
            InitializeManifest();
        }

        public static void InitializeManifest(string csvText = null)
        {
            _manifestSlots = new Dictionary<string, CinematicSlotBinding>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(csvText))
            {
                // Try load from Resources
                try
                {
                    var textAsset = Resources.Load<TextAsset>("Content/Cinematics/vishaka-mp4-manifest");
                    if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
                    {
                        csvText = textAsset.text;
                    }
                }
                catch
                {
                    // Standalone test runner / non-native Unity fallback
                }

                if (string.IsNullOrEmpty(csvText))
                {
                    // Fallback to direct file read if running outside Unity runtime
                    string diskPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Content", "Cinematics", "vishaka-mp4-manifest.csv");
                    if (File.Exists(diskPath))
                    {
                        csvText = File.ReadAllText(diskPath);
                    }
                }
            }

            if (!string.IsNullOrEmpty(csvText))
            {
                ParseManifestCsv(csvText);
            }
            else
            {
                PopulateDefault38Slots();
            }
        }

        private static void ParseManifestCsv(string csv)
        {
            using (var reader = new StringReader(csv))
            {
                string header = reader.ReadLine(); // skip header
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var tokens = SplitCsvLine(line);
                    if (tokens.Count < 8) continue;

                    string slotId = tokens[0].Trim();
                    string filename = tokens[1].Trim();
                    float.TryParse(tokens[2].Trim(), out float durationTarget);
                    string trigger = tokens[3].Trim();
                    string owner = tokens[4].Trim();
                    bool isRequired = string.Equals(tokens[5].Trim(), "required", StringComparison.OrdinalIgnoreCase);
                    string placeholderAlias = tokens[6].Trim();
                    string safeFallbackRaw = tokens[7].Trim();

                    SafeFallbackType fallbackType = ParseSafeFallback(safeFallbackRaw);
                    string relPath = ResolvePlaceholderRelativePath(placeholderAlias, fallbackType);

                    var slot = new CinematicSlotBinding
                    {
                        slotId = slotId,
                        filename = filename,
                        relativePath = relPath,
                        durationTargetSeconds = durationTarget,
                        trigger = trigger,
                        ownerOrCharacter = owner,
                        isRequired = isRequired,
                        placeholderAlias = placeholderAlias,
                        safeFallback = fallbackType,
                        captionTableKey = $"caption_{slotId.ToLowerInvariant()}",
                        audioTrackKey = $"audio_{slotId.ToLowerInvariant()}",
                        authoredLockTicks = 20
                    };

                    _manifestSlots[slotId] = slot;
                }
            }
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var cur = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(cur.ToString());
                    cur.Clear();
                }
                else
                {
                    cur.Append(c);
                }
            }
            result.Add(cur.ToString());
            return result;
        }

        private static SafeFallbackType ParseSafeFallback(string raw)
        {
            if (raw.StartsWith("UI", StringComparison.OrdinalIgnoreCase)) return SafeFallbackType.UI_CREDITS;
            if (raw.StartsWith("live sumi-e", StringComparison.OrdinalIgnoreCase) || raw.Contains("VFX")) return SafeFallbackType.LIVE_SUPER_VFX;
            if (raw.StartsWith("live 3D", StringComparison.OrdinalIgnoreCase) || raw.Contains("pose")) return SafeFallbackType.LIVE_POSE;
            return SafeFallbackType.NEUTRAL_IMAGE;
        }

        private static string ResolvePlaceholderRelativePath(string placeholderAlias, SafeFallbackType fallbackType)
        {
            if (placeholderAlias.StartsWith("PH_CLIP_1", StringComparison.OrdinalIgnoreCase)) return PH_CLIP_1_PATH;
            if (placeholderAlias.StartsWith("PH_CLIP_2", StringComparison.OrdinalIgnoreCase)) return PH_CLIP_2_PATH;
            if (placeholderAlias.StartsWith("PH_CLIP_3", StringComparison.OrdinalIgnoreCase)) return PH_CLIP_3_PATH;
            if (placeholderAlias.StartsWith("PH_CLIP_4", StringComparison.OrdinalIgnoreCase)) return PH_CLIP_4_PATH;

            if (fallbackType == SafeFallbackType.NEUTRAL_IMAGE) return NEUTRAL_IMAGE_PATH;
            return string.Empty;
        }

        private static void PopulateDefault38Slots()
        {
            // Fallback hardcoded populator ensuring exactly 38 slots
            string[] characters = { "C01", "C02", "C03", "C04", "C05", "C06" };
            // Shared 8
            AddSlot("S01_GAME_OPENING", "shared/s01_game_opening.mp4", 60f, true, "PH_CLIP_2", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_2_PATH);
            AddSlot("S02_ACT1_TRANSITION", "shared/s02_act1_transition.mp4", 20f, true, "PH_CLIP_3", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_3_PATH);
            AddSlot("S03_ACT2_TRANSITION", "shared/s03_act2_transition.mp4", 20f, true, "PH_CLIP_3", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_3_PATH);
            AddSlot("S04_ACT3_TRANSITION", "shared/s04_act3_transition.mp4", 20f, true, "PH_CLIP_3", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_3_PATH);
            AddSlot("S05_FINAL_BOSS_INTRO", "shared/s05_final_boss_intro.mp4", 25f, true, "PH_CLIP_4", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_4_PATH);
            AddSlot("S06_FINAL_BOSS_DEFEAT", "shared/s06_final_boss_defeat.mp4", 20f, true, "PH_CLIP_1", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_1_PATH);
            AddSlot("S07_FINALE_EPILOGUE", "shared/s07_finale_epilogue.mp4", 45f, true, "PH_CLIP_2", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_2_PATH);
            AddSlot("S08_CREDITS", "shared/s08_credits.mp4", 90f, false, "none", SafeFallbackType.UI_CREDITS, NEUTRAL_IMAGE_PATH);

            for (int i = 0; i < characters.Length; i++)
            {
                string c = characters[i];
                AddSlot($"{c}_INTRO", $"chars/{c.ToLowerInvariant()}/{c.ToLowerInvariant()}_intro.mp4", 4f, false, "LIVE_INTRO_POSE", SafeFallbackType.LIVE_POSE, string.Empty);
                AddSlot($"{c}_SUPER", $"chars/{c.ToLowerInvariant()}/{c.ToLowerInvariant()}_super.mp4", 1.5f, true, "LIVE_SUPER_VFX", SafeFallbackType.LIVE_SUPER_VFX, string.Empty);
                AddSlot($"{c}_VICTORY", $"chars/{c.ToLowerInvariant()}/{c.ToLowerInvariant()}_victory.mp4", 4f, false, "LIVE_VICTORY_POSE", SafeFallbackType.LIVE_POSE, string.Empty);

                if (c == "C01")
                {
                    AddSlot($"{c}_STORY_OPEN", $"chars/{c.ToLowerInvariant()}/{c.ToLowerInvariant()}_story_open.mp4", 40f, true, "PH_CLIP_2", SafeFallbackType.NEUTRAL_IMAGE, PH_CLIP_2_PATH);
                }
                else
                {
                    AddSlot($"{c}_STORY_OPEN", $"chars/{c.ToLowerInvariant()}/{c.ToLowerInvariant()}_story_open.mp4", 40f, true, "NEUTRAL_IMAGE", SafeFallbackType.NEUTRAL_IMAGE, NEUTRAL_IMAGE_PATH);
                }

                AddSlot($"{c}_STORY_END", $"chars/{c.ToLowerInvariant()}/{c.ToLowerInvariant()}_story_end.mp4", 45f, true, "NEUTRAL_IMAGE", SafeFallbackType.NEUTRAL_IMAGE, NEUTRAL_IMAGE_PATH);
            }
        }

        private static void AddSlot(string id, string file, float dur, bool req, string alias, SafeFallbackType fallback, string path)
        {
            _manifestSlots[id] = new CinematicSlotBinding
            {
                slotId = id,
                filename = file,
                relativePath = path,
                durationTargetSeconds = dur,
                isRequired = req,
                placeholderAlias = alias,
                safeFallback = fallback,
                captionTableKey = $"caption_{id.ToLowerInvariant()}",
                authoredLockTicks = 20
            };
        }

        public static bool ValidateManifest(out List<string> errors, out int requiredCount, out int optionalCount)
        {
            errors = new List<string>();
            requiredCount = 0;
            optionalCount = 0;

            var slots = ManifestSlots;
            if (slots.Count != 38)
            {
                errors.Add($"Manifest must contain exactly 38 slots, found {slots.Count}.");
            }

            foreach (var kvp in slots)
            {
                var slot = kvp.Value;
                if (slot.isRequired) requiredCount++;
                else optionalCount++;

                // Validate safe paths
                if (!string.IsNullOrEmpty(slot.relativePath))
                {
                    if (slot.relativePath.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                        slot.relativePath.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
                        slot.relativePath.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                        slot.relativePath.Contains("..") ||
                        (slot.relativePath.Length > 2 && slot.relativePath[1] == ':') ||
                        (slot.relativePath.StartsWith("/") && !slot.relativePath.StartsWith("Assets/")))
                    {
                        errors.Add($"Slot '{slot.slotId}' has unsafe or untrusted path: '{slot.relativePath}'.");
                    }
                }

                // Enforce PH_CLIP_1 exclusivity
                if (string.Equals(slot.placeholderAlias, "PH_CLIP_1", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(slot.relativePath) && slot.relativePath.Contains("Hero_defeating_skeleton_boss")))
                {
                    if (!string.Equals(slot.slotId, "S06_FINAL_BOSS_DEFEAT", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"PH_CLIP_1 (Hero_defeating_skeleton_boss) is strictly restricted to S06_FINAL_BOSS_DEFEAT, but found assigned to '{slot.slotId}'.");
                    }
                }
            }

            if (requiredCount != 25)
            {
                errors.Add($"Manifest must contain exactly 25 required slots, found {requiredCount}.");
            }
            if (optionalCount != 13)
            {
                errors.Add($"Manifest must contain exactly 13 optional slots, found {optionalCount}.");
            }

            return errors.Count == 0;
        }

        public static bool ValidateDeduplication(string streamingAssetsDir, out List<string> errors)
        {
            errors = new List<string>();
            if (!Directory.Exists(streamingAssetsDir)) return true;

            var files = Directory.GetFiles(streamingAssetsDir, "*.mp4", SearchOption.AllDirectories);
            var hashToPaths = new Dictionary<string, string>();

            using (var sha = SHA256.Create())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string filePath = files[i];
                    try
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            byte[] hashBytes = sha.ComputeHash(stream);
                            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                            if (hashToPaths.TryGetValue(hash, out string existingPath))
                            {
                                errors.Add($"Deduplication check failed! Duplicate video payload detected: '{Path.GetFileName(filePath)}' matches '{Path.GetFileName(existingPath)}' (Hash: {hash}).");
                            }
                            else
                            {
                                hashToPaths[hash] = filePath;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to hash '{filePath}': {ex.Message}");
                    }
                }
            }

            return errors.Count == 0;
        }

        #region Captions & Localization

        private static void InitializeCaptions()
        {
            _localizedCaptions = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // C01 Kai
            AddCaption("C01_STORY_OPEN", "en", "The stage is empty, but the resonance remains. Kai steps forward, breaking the contract shackles.");
            AddCaption("C01_STORY_OPEN", "ja", "舞台は静まり返り、響きだけが残る。カイは一歩を踏み出し、契約の呪縛を断ち切る。");
            AddCaption("C01_STORY_END", "en", "With the crown shattered, Kai takes a final bow into the starlit void. The crowd goes wild.");
            AddCaption("C01_STORY_END", "ja", "冠が砕け散り、カイは星空の虚無へと最後の一礼を捧げる。観客の歓声が響き渡る。");

            // Shared transitions
            AddCaption("S01_GAME_OPENING", "en", "When the crowd's devotion overflows, the bound performer-spirits awaken as rogue Shikigami idols.");
            AddCaption("S01_GAME_OPENING", "ja", "観客の祈りが満ちるとき、縛られし魂は暴走する式神アイドルとして目覚める。");
            AddCaption("S02_ACT1_TRANSITION", "en", "ACT 1: THE EMPTY STAGE — The first binding cracks.");
            AddCaption("S02_ACT1_TRANSITION", "ja", "第一幕：空白の舞台 — 最初の封印が軋む。");
            AddCaption("S03_ACT2_TRANSITION", "en", "ACT 2: INK AND APPLAUSE — The crowd itself turns against the performers.");
            AddCaption("S03_ACT2_TRANSITION", "ja", "第二幕：墨と拍手 — 群衆の熱狂が刃となる。");
            AddCaption("S04_ACT3_TRANSITION", "en", "ACT 3: CURTAIN OF IRON — Face the final rogue Shikigami idol atop the Hollow Throne.");
            AddCaption("S04_ACT3_TRANSITION", "ja", "第三幕：鉄の帳 — 虚無の王座にて最後の式神と対峙せよ。");
            AddCaption("S05_FINAL_BOSS_INTRO", "en", "Behold Nox, the Hollow Crown! The embodiment of endless applause.");
            AddCaption("S05_FINAL_BOSS_DEFEAT", "en", "The Hollow Crown shatters into sumi ink. The performance is finished.");
            AddCaption("S07_FINALE_EPILOGUE", "en", "Silence returns to the arena. The contracts dissolve, setting the idols free.");
        }

        private static void AddCaption(string slotId, string locale, string text)
        {
            if (!_localizedCaptions.TryGetValue(slotId, out var localeMap))
            {
                localeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _localizedCaptions[slotId] = localeMap;
            }
            localeMap[locale] = text;
        }

        public static string GetCaption(string slotId, string locale = "en")
        {
            if (string.IsNullOrEmpty(slotId)) return "[AUTHOR TEXT PENDING]";
            if (_localizedCaptions != null && _localizedCaptions.TryGetValue(slotId, out var map))
            {
                if (map.TryGetValue(locale, out string localizedText))
                {
                    return localizedText;
                }
                if (map.TryGetValue("en", out string defaultText))
                {
                    return defaultText;
                }
            }
            return $"[AUTHOR TEXT PENDING] - {slotId}";
        }

        #endregion

        public MoveVideoBinding GetBinding(string moveId)
        {
            if (bindings == null || string.IsNullOrEmpty(moveId)) return null;

            for (int i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(bindings[i].moveOrSuperId, moveId, StringComparison.OrdinalIgnoreCase))
                {
                    return bindings[i];
                }
            }
            return null;
        }

        public bool ResolveMoveVideo(string moveId, out string resolvedUrl, out int lockTicks)
        {
            resolvedUrl = string.Empty;
            lockTicks = 20;

            var binding = GetBinding(moveId);
            string targetPath = binding != null ? binding.relativePath : defaultFallbackPath;

            if (binding != null)
            {
                lockTicks = binding.authoredLockTicks;
            }

            if (VideoPathResolver.Resolve(targetPath, out _, out resolvedUrl, out var def))
            {
                if (def != null && def.authoredLockTicks > 0)
                {
                    lockTicks = def.authoredLockTicks;
                }
                return true;
            }

            // Fallback to default
            return VideoPathResolver.Resolve(defaultFallbackPath, out _, out resolvedUrl, out _);
        }
    }
}
