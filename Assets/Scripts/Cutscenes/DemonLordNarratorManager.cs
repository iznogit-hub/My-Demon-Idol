using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DemonIdol.UI;
using DemonIdol.Combat;

namespace DemonIdol.Cutscenes
{
    public enum DemonLordCutsceneKind
    {
        GameIntroduction,
        IdolCommentary,
        DailyRoutine,
        SoulDrain,
        DemonBeastBlast,
        GameOverDefeat,
        FloatingGhostCompanion
    }

    public enum NarratorContext
    {
        GameStart,
        ModeSelect,
        FighterSelect,
        DomainExpansion,
        MatchVictory,
        MatchDefeat,
        GameOver
    }

    public static class DemonLordAssetCatalog
    {
        // 1. Studio Splash & Brand Intros
        public const string INTRO_STUDIO_KLING_A = "videos/kling-4.0_generate_a_similar_character_intraoducing_the_game_called_my_demon_idol-0.mp4";
        public const string INTRO_STUDIO_KLING_SC = "videos/kling-4.0_generate_a_similar_character_intraoducing_the_game_called_my_demon_idol_sc-0.mp4";
        public const string INTRO_HAILUO = "videos/hailuo-03_generate_a_similar_character_intraoducing_the_game_called_my_demon_idol-0.mp4";
        public const string INTRO_KLING_A = INTRO_STUDIO_KLING_A;
        public const string INTRO_KLING_SC = INTRO_STUDIO_KLING_SC;

        // 2. Menus & Interstitials
        public const string TITLE_SCREEN_LOOP = "videos/Monster_floating_with_title_text_1db164ec_1080p.mp4";
        public const string GHOST_FLOATING_TITLE = TITLE_SCREEN_LOOP;
        public const string DAILY_ROUTINE = "videos/Demon_lord_idol_daily_routine_a9877cdb_1080p.mp4";
        public const string SPEAKING_TO_CAMERA = "videos/Gothic_idol_speaking_to_camera_0f99ebe4_1080p.mp4";
        public const string UMBRELLA_SPEAKING = "videos/Girl_holding_umbrella_speaking_70d09446_1080p.mp4";
        public const string TUTORIAL_HOW_TO_PLAY = ""; // Empty: no recorded combat video; runs in-engine procedural tutorial panel
        public const string DOJO_INTERSTITIAL = "videos/Monster_floating_on_game_screen_e0aa4105_1080p.mp4";
        public const string LOADING_SCREEN_LOOP = DOJO_INTERSTITIAL;
        public const string MATCH_LAUNCH_COUNTDOWN = "Cinematics/MY_DEMON_IDOL_launch_countdown.mp4";

        // 3. Story Mode Narrative Arc
        public const string STORY_CH0_PROLOGUE = "Cutscenes/Cyberpunk_city_intro.mp4";
        public const string SOUL_DRAIN_ACT1 = "videos/Gothic_girl_draining_souls_05ae5ffa_1080p.mp4";
        public const string SOUL_DRAIN_IDOL = "videos/Gothic_idol_draining_souls_bf6fba8b_1080p.mp4";
        public const string SOUL_DRAIN_ETHEREAL = "videos/Gothic_girl_draining_ethereal_souls_d076cd85_1080p.mp4";
        public const string SOUL_DRAIN_ACT4 = "videos/Gothic_girl_draining_souls_8948ff66_1080p.mp4";
        public const string SOUL_DRAIN_ACT5 = "videos/Gothic_idol_drains_ethereal_souls_eb055e9e_1080p.mp4";

        // 4. Climax Boss & Endings
        public const string DARK_ENERGY_BEAST = "videos/Demon_beast_unleashes_dark_energy_cb092431_1080p.mp4";
        public const string DARK_BLAST_BEAST = "videos/Demon_beast_unleashes_dark_blast_150f4633_1080p.mp4";
        public const string BOSS_DEFEAT_CLIMAX = "Cinematics/Hero_defeating_skeleton_boss.mp4";
        public const string FINALE_EPILOGUE_A = "videos/Girl_holding_umbrella_smiling_me…_41d91bb9_1080p.mp4";
        public const string FINALE_EPILOGUE_B = "videos/Girl_holding_umbrella_smiling_me…_49511e57_1080p.mp4";
        public const string CREDITS_LOOP = "Cinematics/Ending_credits_video_loop.mp4";

        // 5. Game Over & Defeat
        public const string GAME_OVER_GHOST = "videos/Monster_ghost_floating_game_over_597068c8_1080p.mp4";
        public const string GAME_OVER_GHOST_ALT = "videos/Monster_ghost_floating_game_over_ec32629e_1080p.mp4";

        // 6. Character Victory / Super Cinematics (13 clips in array: 9 Gothic Idol finishes + 4 umbrella variants)
        public static readonly string[] CharacterFinishClips = new[]
        {
            "videos/Gothic_idol_holding_pink_umbrella_0ad3aff9_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_18c7c79c_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_64c5641c_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_6bd7c0ea_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_9c6d334f_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_d1adfb38_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_d4cb549a_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_d52ec1a3_1080p.mp4",
            "videos/Gothic_idol_holding_pink_umbrella_f7b5d561_1080p.mp4",
            "videos/Gothic_character_holding_pink_um…_2c9b6843_1080p.mp4",
            "videos/Gothic_character_holding_pink_um…_81213e6e_1080p.mp4",
            "videos/Girl_holding_pink_umbrella_7d9bb4e5_1080p.mp4",
            "videos/Girl_holding_pink_umbrella_af61a0f1_1080p.mp4"
        };

        // High-definition artwork & 2K stills
        public const string ANNOUNCER_PORTRAIT = "videos/Demon_Lord_Announcer_Portrait.jpg";
        public const string SHADOW_DEMON_ART = "videos/Ghost_transforms_into_shadow_demon_848862a8_2K.jpg";
        public const string SHADOW_DEMON_ALT_ART = "videos/Ghost_transforms_into_shadow_demon_ee5a22e6_2K.jpg";
        public const string GHOST_FLOATING_ART = "videos/Monster_ghost_floating_near_idol_6fdc636f_2K.jpg";
    }

    /// <summary>
    /// Master Narrator and Demon Lord Cinematic Director for Demon Idol.
    /// Manages the full media library of Demon Lord videos (intro, daily routine, soul drain,
    /// dark blasts, game over) and high-resolution announcer portraits across UI, combat, and cutscenes.
    /// </summary>
    public class DemonLordNarratorManager : MonoBehaviour
    {
        private static DemonLordNarratorManager _instance;
        public static DemonLordNarratorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DemonLordNarratorManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DemonLordNarratorManager");
                        _instance = go.AddComponent<DemonLordNarratorManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private Texture2D _announcerPortraitTexture;
        private Sprite _announcerPortraitSprite;
        private Texture2D _shadowDemonTexture;
        private Sprite _shadowDemonSprite;
        private Texture2D _shadowDemonAltTexture;
        private Sprite _shadowDemonAltSprite;
        private Texture2D _ghostFloatingTexture;
        private Sprite _ghostFloatingSprite;

        public event Action<string> OnDialogueSpoken;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        #region Media Path Resolution

        /// <summary>
        /// Resolves a relative path to a playable file URL.
        /// </summary>
        public static string ResolveMediaUrl(string relativePath)
        {
            string path = Path.Combine(Application.dataPath, relativePath);
            if (!File.Exists(path))
            {
                string cand = Path.GetFullPath(Path.Combine("Assets", relativePath));
                if (File.Exists(cand)) path = cand;
            }

            if (!path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return "file://" + path;
            }
            return path;
        }

        public static bool MediaExists(string relativePath)
        {
            string path = Path.Combine(Application.dataPath, relativePath);
            if (File.Exists(path)) return true;
            string cand = Path.GetFullPath(Path.Combine("Assets", relativePath));
            return File.Exists(cand);
        }

        #endregion

        #region Artwork & Portrait Accessors

        public Sprite GetAnnouncerPortraitSprite()
        {
            if (_announcerPortraitSprite != null) return _announcerPortraitSprite;
            var tex = GetAnnouncerPortraitTexture();
            if (tex != null)
            {
                _announcerPortraitSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return _announcerPortraitSprite;
        }

        public Texture2D GetAnnouncerPortraitTexture()
        {
            if (_announcerPortraitTexture != null) return _announcerPortraitTexture;
            _announcerPortraitTexture = LoadTexture(DemonLordAssetCatalog.ANNOUNCER_PORTRAIT);
            return _announcerPortraitTexture;
        }

        public Sprite GetShadowDemonSprite()
        {
            if (_shadowDemonSprite != null) return _shadowDemonSprite;
            if (_shadowDemonTexture == null) _shadowDemonTexture = LoadTexture(DemonLordAssetCatalog.SHADOW_DEMON_ART);
            if (_shadowDemonTexture != null)
            {
                _shadowDemonSprite = Sprite.Create(_shadowDemonTexture, new Rect(0, 0, _shadowDemonTexture.width, _shadowDemonTexture.height), new Vector2(0.5f, 0.5f));
            }
            return _shadowDemonSprite;
        }

        public Sprite GetShadowDemonAltSprite()
        {
            if (_shadowDemonAltSprite != null) return _shadowDemonAltSprite;
            if (_shadowDemonAltTexture == null) _shadowDemonAltTexture = LoadTexture(DemonLordAssetCatalog.SHADOW_DEMON_ALT_ART);
            if (_shadowDemonAltTexture != null)
            {
                _shadowDemonAltSprite = Sprite.Create(_shadowDemonAltTexture, new Rect(0, 0, _shadowDemonAltTexture.width, _shadowDemonAltTexture.height), new Vector2(0.5f, 0.5f));
            }
            return _shadowDemonAltSprite;
        }

        public Sprite GetGhostCompanionSprite()
        {
            if (_ghostFloatingSprite != null) return _ghostFloatingSprite;
            if (_ghostFloatingTexture == null) _ghostFloatingTexture = LoadTexture(DemonLordAssetCatalog.GHOST_FLOATING_ART);
            if (_ghostFloatingTexture != null)
            {
                _ghostFloatingSprite = Sprite.Create(_ghostFloatingTexture, new Rect(0, 0, _ghostFloatingTexture.width, _ghostFloatingTexture.height), new Vector2(0.5f, 0.5f));
            }
            return _ghostFloatingSprite;
        }

        private static Texture2D LoadTexture(string relativePath)
        {
            try
            {
                string path = Path.Combine(Application.dataPath, relativePath);
                if (!File.Exists(path))
                {
                    string cand = Path.GetFullPath(Path.Combine("Assets", relativePath));
                    if (File.Exists(cand)) path = cand;
                }

                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return tex;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DemonLordNarratorManager] Failed to load texture at '{relativePath}': {ex.Message}");
            }
            return null;
        }

        #endregion

        #region Cutscene Playback

        public void PlayGameIntroduction(Action onComplete = null)
        {
            string url = ResolveMediaUrl(DemonLordAssetCatalog.INTRO_HAILUO);
            string caption = "I am Lady Aurelia, Demon Lord of this realm. Welcome... to My Demon Idol!";
            OnDialogueSpoken?.Invoke(caption);
            CutsceneManager.Instance.PlayCutsceneFromUrl(url, onComplete, () =>
            {
                // Fallback to Kling variant if primary fails
                string altUrl = ResolveMediaUrl(DemonLordAssetCatalog.INTRO_KLING_A);
                CutsceneManager.Instance.PlayCutsceneFromUrl(altUrl, onComplete, onComplete, caption);
            }, caption);
        }

        public void PlayDailyRoutine(Action onComplete = null)
        {
            string url = ResolveMediaUrl(DemonLordAssetCatalog.DAILY_ROUTINE);
            string caption = "A glimpse behind the curtain... even the Demon Lord has her daily ceremonies.";
            OnDialogueSpoken?.Invoke(caption);
            CutsceneManager.Instance.PlayCutsceneFromUrl(url, onComplete, onComplete, caption);
        }

        public void PlayCommentary(Action onComplete = null)
        {
            string url = ResolveMediaUrl(DemonLordAssetCatalog.SPEAKING_TO_CAMERA);
            string caption = "Sharpen your technique, mortal. The arena accepts only the finest performers.";
            OnDialogueSpoken?.Invoke(caption);
            CutsceneManager.Instance.PlayCutsceneFromUrl(url, onComplete, () =>
            {
                string altUrl = ResolveMediaUrl(DemonLordAssetCatalog.UMBRELLA_SPEAKING);
                CutsceneManager.Instance.PlayCutsceneFromUrl(altUrl, onComplete, onComplete, caption);
            }, caption);
        }

        public void PlaySoulDrain(Action onComplete = null)
        {
            string url = ResolveMediaUrl(DemonLordAssetCatalog.SOUL_DRAIN_IDOL);
            string caption = "DOMAIN EXPANSION: SOUL HARVEST! Your ethereal essence belongs to the idol.";
            OnDialogueSpoken?.Invoke(caption);
            CutsceneManager.Instance.PlayCutsceneFromUrl(url, onComplete, () =>
            {
                string altUrl = ResolveMediaUrl(DemonLordAssetCatalog.SOUL_DRAIN_ETHEREAL);
                CutsceneManager.Instance.PlayCutsceneFromUrl(altUrl, onComplete, onComplete, caption);
            }, caption);
        }

        public void PlayDemonBeastBlast(Action onComplete = null)
        {
            string url = ResolveMediaUrl(DemonLordAssetCatalog.DARK_BLAST_BEAST);
            string caption = "AWAKENING: DARK ENERGY BLAST! Consume the battlefield in eternal midnight.";
            OnDialogueSpoken?.Invoke(caption);
            CutsceneManager.Instance.PlayCutsceneFromUrl(url, onComplete, () =>
            {
                string altUrl = ResolveMediaUrl(DemonLordAssetCatalog.DARK_ENERGY_BEAST);
                CutsceneManager.Instance.PlayCutsceneFromUrl(altUrl, onComplete, onComplete, caption);
            }, caption);
        }

        public void PlayGameOver(Action onComplete = null)
        {
            string url = ResolveMediaUrl(DemonLordAssetCatalog.GAME_OVER_GHOST);
            string caption = "GAME OVER. Fallen so soon? In My Demon Idol, defeat is only the overture to your damnation...";
            OnDialogueSpoken?.Invoke(caption);
            CutsceneManager.Instance.PlayCutsceneFromUrl(url, onComplete, () =>
            {
                string altUrl = ResolveMediaUrl(DemonLordAssetCatalog.GAME_OVER_GHOST_ALT);
                CutsceneManager.Instance.PlayCutsceneFromUrl(altUrl, onComplete, onComplete, caption);
            }, caption);
        }

        public void PlayCutscene(DemonLordCutsceneKind kind, Action onComplete = null)
        {
            switch (kind)
            {
                case DemonLordCutsceneKind.GameIntroduction:
                    PlayGameIntroduction(onComplete);
                    break;
                case DemonLordCutsceneKind.DailyRoutine:
                    PlayDailyRoutine(onComplete);
                    break;
                case DemonLordCutsceneKind.IdolCommentary:
                    PlayCommentary(onComplete);
                    break;
                case DemonLordCutsceneKind.SoulDrain:
                    PlaySoulDrain(onComplete);
                    break;
                case DemonLordCutsceneKind.DemonBeastBlast:
                    PlayDemonBeastBlast(onComplete);
                    break;
                case DemonLordCutsceneKind.GameOverDefeat:
                    PlayGameOver(onComplete);
                    break;
                default:
                    PlayCommentary(onComplete);
                    break;
            }
        }

        #endregion

        #region Announcer Dialogue & Commentary Catalog

        public string GetRandomAnnouncerQuote(NarratorContext context)
        {
            string[] quotes = context switch
            {
                NarratorContext.GameStart => new[]
                {
                    "I am the Demon Lord. Are you prepared to offer your soul to My Demon Idol?",
                    "Welcome, mortal challenger. Step into the arena and witness our concert.",
                    "Let the blood and rhythm guide your strikes into the abyss."
                },
                NarratorContext.ModeSelect => new[]
                {
                    "Choose your trial wisely. Every mode tests a different fragment of your spirit.",
                    "The souls of the arena murmur in anticipation of your next decision.",
                    "Step forward. Prove that your rhythm transcends mere mortality."
                },
                NarratorContext.DomainExpansion => new[]
                {
                    "Domain Expansion! Witness the soul harvest of the Demon Lord!",
                    "The spatial barrier fractures! All souls within shall feed the crimson flame!",
                    "Territory conquered! In this space, every strike strikes the soul directly!"
                },
                NarratorContext.MatchVictory => new[]
                {
                    "Magnificent performance! Your rhythm echoes through the deep halls of the abyss.",
                    "A decisive victory! The audience of spirits roars in your favor!",
                    "Flawless execution. You wear your victory like a dark crown."
                },
                NarratorContext.MatchDefeat => new[]
                {
                    "Fallen so soon? In My Demon Idol, defeat is only the overture to your damnation.",
                    "Your soul wavered. Return to the dojo before challenging true darkness again.",
                    "A momentary collapse. Rise and reclaim your tempo, mortal."
                },
                NarratorContext.GameOver => new[]
                {
                    "GAME OVER. The shadows claim another wandering challenger.",
                    "Your performance has concluded... or will you dare to restart the song?",
                    "The abyss remembers your effort. Press continue to defy your fate."
                },
                _ => new[]
                {
                    "The concert never truly ends in My Demon Idol.",
                    "Rhythm, speed, and sovereign power dictate all combat here."
                }
            };

            int pick = UnityEngine.Random.Range(0, quotes.Length);
            return quotes[pick];
        }

        public string GetModeDialogue(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return GetRandomAnnouncerQuote(NarratorContext.ModeSelect);
            string m = mode.ToLowerInvariant();
            if (m.Contains("arcade"))
                return "Arcade Mode! Battle your way through my elite champions and reach my personal stage.";
            if (m.Contains("story"))
                return "Story Mode! Follow the path of the Fallen Idol and uncover the ancient pact.";
            if (m.Contains("pvp"))
                return "Online PvP! Synchronize your soul across the network and conquer distant challengers.";
            if (m.Contains("versus") || m.Contains("local"))
                return "Local Versus! Side by side, settle who possesses the true rhythm of sovereignty.";
            if (m.Contains("training") || m.Contains("lesson"))
                return "Training Dojo! Study frame data and hit confirms before facing true nightmares.";
            return GetRandomAnnouncerQuote(NarratorContext.ModeSelect);
        }

        public string GetFighterCommentary(string characterId)
        {
            return characterId switch
            {
                "C01" => "Kai the Fool... reckless and swift as a shadow. Will his dual daggers cut deep enough?",
                "C02" => "Reika the Veilweaver... weaving spiritual silken threads to ensnare her prey. A dancer after my own heart.",
                "C03" => "Arin the Iron Vow... heavy armor and unbreakable defense. A moving fortress on the battlefield.",
                "C04" => "Mira the Ash Singer... born from dying embers, her melancholy song burns hotter than hellfire.",
                "C05" => "Zev the Stormbound... thunder coils within his iron fists. A tempest waiting to erupt.",
                "C06" => "Nox the Hollow Crown... sovereign of phantoms. Even the shadows bow to his royal decree.",
                _ => "A formidable warrior steps into the circle. Show me what your soul is made of!"
            };
        }

        #endregion
    }
}
