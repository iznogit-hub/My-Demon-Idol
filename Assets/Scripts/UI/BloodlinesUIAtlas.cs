using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DemonIdol.UI
{
    /// <summary>
    /// Central runtime loader for Alebardium "Bloodlines UI" asset-pack sprites.
    /// All textures live under Assets/Alebardium/Bloodlines UI/Textures/ and are
    /// loaded via Resources.Load fallback or direct Sprite construction from
    /// imported Texture2D assets referenced at edit-time through SerializedFields,
    /// with a runtime-cache so each sprite is built exactly once.
    ///
    /// Usage:  BloodlinesUIAtlas.Instance.ButtonRedDefault  (returns Sprite)
    ///         BloodlinesUIAtlas.FrameBackground             (static shorthand)
    /// </summary>
    public class BloodlinesUIAtlas : MonoBehaviour
    {
        public static BloodlinesUIAtlas Instance { get; private set; }

        // ──── Texture paths relative to Assets/Alebardium/Bloodlines UI/Textures/ ────
        // These are loaded by scanning the imported textures at runtime.

        #region Cached Sprites

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        // ── Buttons ──
        public Sprite Btn1_RedDefault   => Load("Button/Button1/Status_Red_Default");
        public Sprite Btn1_RedHover     => Load("Button/Button1/Status_Red_Hover");
        public Sprite Btn1_GrayDefault  => Load("Button/Button1/Status_Grey_Default");
        public Sprite Btn1_GrayHover    => Load("Button/Button1/Status_Grey_Hover");
        public Sprite Btn1_Pressed      => Load("Button/Button1/Status_Pressed");
        public Sprite Btn1_Disabled     => Load("Button/Button1/Status_Disable");

        public Sprite Btn2_RedDefault   => Load("Button/Button2/Button2_Status_Red_Default");
        public Sprite Btn2_RedHover     => Load("Button/Button2/Button2_Status_Red_Hover");
        public Sprite Btn2_GrayDefault  => Load("Button/Button2/Button2_Status_Grey_Default");
        public Sprite Btn2_GrayHover    => Load("Button/Button2/Button2_Status_Grey_Hover");
        public Sprite Btn2_Pressed      => Load("Button/Button2/Button2_Status_Pressed");
        public Sprite Btn2_Disabled     => Load("Button/Button2/Button2_Status_Disable");

        public Sprite Btn3_RedDefault   => Load("Button/Button3/Button3_Status_Red_Default");
        public Sprite Btn3_RedHover     => Load("Button/Button3/Button3_Status_Red_Hover");
        public Sprite Btn3_GrayDefault  => Load("Button/Button3/Button3_Status_Grey_Default");
        public Sprite Btn3_GrayHover    => Load("Button/Button3/Button3_Status_Grey_Hover");
        public Sprite Btn3_Pressed      => Load("Button/Button3/Button3_Status_Pressed");
        public Sprite Btn3_Disabled     => Load("Button/Button3/Button3_Status_Disable");

        // ── Frames ──
        public Sprite FrameBg           => Load("Frame/Frame_background");
        public Sprite FrameOutline      => Load("Frame/Frame_outline");
        public Sprite FrameOutlineRed   => Load("Frame/Frame_outline_red");
        public Sprite FrameOutlineV2    => Load("Frame/Frame_outline_v2");
        public Sprite FrameMainMenu     => Load("Frame/Frame_main_menu");
        public Sprite FrameMainMenuRed  => Load("Frame/Frame_main_menu_red");

        // ── Notice ──
        public Sprite NoticeV1          => Load("Notice/Frame_notice_v1");
        public Sprite NoticeV2          => Load("Notice/Frame_notice_v2");

        // ── Layout ──
        public Sprite LayoutHealth      => Load("Layout/HEALTH");
        public Sprite LayoutMenu        => Load("Layout/MENU");
        public Sprite LayoutProgressBar => Load("Layout/PROGRESS_BAR");
        public Sprite LayoutSettings    => Load("Layout/SETTINGS");
        public Sprite LayoutToggle      => Load("Layout/TOGGLE_SWITCH");
        public Sprite LayoutOnOff       => Load("Layout/ON_OFF");
        public Sprite LayoutCheckbox    => Load("Layout/CHECKBOX");
        public Sprite LayoutColors      => Load("Layout/Colors");

        // ── Progress Bar (Rectangle) ──
        public Sprite PBRectEmpty       => Load("Progress_Bar/Rectangle/Progress_Bar_Rectangle_empty");
        public Sprite PBRectFull_V1     => Load("Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v1");
        public Sprite PBRectFull_V2     => Load("Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v2");
        public Sprite PBRectFull_V3     => Load("Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v3");
        public Sprite PBRectFull_V4     => Load("Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v4");
        public Sprite PBRectFull_V5     => Load("Progress_Bar/Rectangle/Progress_Bar_Rectangle_full_v5");

        // ── Progress Bar (Round) ──
        public Sprite PBRoundEmpty_V1V4 => Load("Progress_Bar/Round/Progress_Bar_Round_empty_v1_v4");
        public Sprite PBRoundEmpty_V2V3 => Load("Progress_Bar/Round/Progress_Bar_Round_empty_v2_v3");
        public Sprite PBRoundFull_V1    => Load("Progress_Bar/Round/Progress_Bar_Round_full_v1");
        public Sprite PBRoundFull_V2    => Load("Progress_Bar/Round/Progress_Bar_Round_full_v2");
        public Sprite PBRoundFull_V3    => Load("Progress_Bar/Round/Progress_Bar_Round_full_v3");
        public Sprite PBRoundFull_V4    => Load("Progress_Bar/Round/Progress_Bar_Round_full_v4");

        // ── Slider ──
        public Sprite SliderBg          => Load("Slider/Slider_Background");
        public Sprite SliderFill        => Load("Slider/Slider_Fill");
        public Sprite SliderHandle      => Load("Slider/Slider_Handle");

        // ── Toggle (Checkmarks) ──
        public Sprite CheckRound1       => Load("Toggle/Icon Checkmark 1 (Round)");
        public Sprite CheckRound2       => Load("Toggle/Icon Checkmark 2 (Round)");
        public Sprite CheckRound3       => Load("Toggle/Icon Checkmark 3 (Round)");
        public Sprite CheckRect1        => Load("Toggle/Icon Checkmark 1 (Rect)");
        public Sprite CheckRect2        => Load("Toggle/Icon Checkmark 2 (Rect)");
        public Sprite CheckRect3        => Load("Toggle/Icon Checkmark 3 (Rect)");

        // ── Input Field ──
        public Sprite InputFrameGray    => Load("Input Field/Frame_input");
        public Sprite InputFrameRed     => Load("Input Field/Frame_input_red");
        public Sprite InputCross        => Load("Input Field/Cross");

        // ── Icons ──
        public Sprite Icon1             => Load("Icon/Icons");
        public Sprite Icon16            => Load("Icon/Icons-16");
        public Sprite Icon17            => Load("Icon/Icons-17");
        public Sprite Icon18            => Load("Icon/Icons-18");
        public Sprite Icon19            => Load("Icon/Icons_19");

        #endregion

        // ──── Static Shorthands ────
        public static Sprite FrameBackground   => Instance?.FrameBg;
        public static Sprite ButtonRed         => Instance?.Btn1_RedDefault;
        public static Sprite ButtonGray        => Instance?.Btn1_GrayDefault;
        public static Sprite ButtonPressed     => Instance?.Btn1_Pressed;
        public static Sprite HealthBar         => Instance?.LayoutHealth;
        public static Sprite ProgressBar       => Instance?.LayoutProgressBar;
        public static Sprite MenuFrame         => Instance?.LayoutMenu;
        public static Sprite SettingsFrame     => Instance?.LayoutSettings;

        // ──── Font Cache ────
        private Font _bloodlinesFont;
        public Font BloodlinesFont
        {
            get
            {
                if (_bloodlinesFont == null)
                {
                    // Try Bloodlines pack fonts first
                    _bloodlinesFont = Resources.Load<Font>("Fonts/MedievalSharp-Regular");
                    if (_bloodlinesFont == null)
                    {
                        // Direct path load
                        var allFonts = Resources.FindObjectsOfTypeAll<Font>();
                        foreach (var f in allFonts)
                        {
                            if (f.name.Contains("MedievalSharp") || f.name.Contains("ManufacturingConsent"))
                            {
                                _bloodlinesFont = f;
                                break;
                            }
                        }
                    }
                    if (_bloodlinesFont == null)
                    {
                        _bloodlinesFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                    if (_bloodlinesFont == null)
                    {
                        _bloodlinesFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }
                return _bloodlinesFont;
            }
        }

        // ──── Audio Cache ────
        private AudioClip _clickSfx;
        private AudioClip _hoverSfx;

        public AudioClip ClickSfx
        {
            get
            {
                if (_clickSfx == null)
                {
                    var allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
                    foreach (var c in allClips)
                    {
                        if (c.name.Contains("Click Button SFX")) { _clickSfx = c; break; }
                    }
                }
                return _clickSfx;
            }
        }

        public AudioClip HoverSfx
        {
            get
            {
                if (_hoverSfx == null)
                {
                    var allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
                    foreach (var c in allClips)
                    {
                        if (c.name.Contains("Hover Button SFX")) { _hoverSfx = c; break; }
                    }
                }
                return _hoverSfx;
            }
        }

        // ──── Lifecycle ────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Call this from any manager's Awake/Start to ensure the atlas singleton exists.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance != null) return;
            var existing = FindAnyObjectByType<BloodlinesUIAtlas>(FindObjectsInactive.Include);
            if (existing != null) { Instance = existing; return; }
            var go = new GameObject("[BloodlinesUIAtlas]", typeof(BloodlinesUIAtlas));
            if (Application.isPlaying) DontDestroyOnLoad(go);
        }

        // ──── Sprite Loader ────

        private Sprite Load(string subPath)
        {
            // Procedural Native UI Mode: Bloodlines external asset loading disabled
            return null;
        }

        // ──── Helper: Apply Bloodlines button styling to any Button ────

        /// <summary>
        /// Apply full Bloodlines visual styling to a Unity UI Button.
        /// Uses the specified button style (1=ornate, 2=medium, 3=minimal) and color (red/gray).
        /// </summary>
        public void StyleButton(Button btn, int style = 1, bool red = true)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img == null) return;

            Sprite normal, hover, pressed, disabled;

            switch (style)
            {
                case 2:
                    normal   = red ? Btn2_RedDefault : Btn2_GrayDefault;
                    hover    = red ? Btn2_RedHover   : Btn2_GrayHover;
                    pressed  = Btn2_Pressed;
                    disabled = Btn2_Disabled;
                    break;
                case 3:
                    normal   = red ? Btn3_RedDefault : Btn3_GrayDefault;
                    hover    = red ? Btn3_RedHover   : Btn3_GrayHover;
                    pressed  = Btn3_Pressed;
                    disabled = Btn3_Disabled;
                    break;
                default:
                    normal   = red ? Btn1_RedDefault : Btn1_GrayDefault;
                    hover    = red ? Btn1_RedHover   : Btn1_GrayHover;
                    pressed  = Btn1_Pressed;
                    disabled = Btn1_Disabled;
                    break;
            }

            if (normal != null)
            {
                img.sprite = normal;
                img.type = Image.Type.Sliced;
                img.color = Color.white; // Let the sprite's own colors show through

                var ss = new SpriteState();
                ss.highlightedSprite = hover;
                ss.pressedSprite     = pressed;
                ss.disabledSprite    = disabled;
                btn.spriteState = ss;
                btn.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            }

            // Wire hover SFX
            var trigger = btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
        }

        /// <summary>
        /// Apply Bloodlines frame background to an Image.
        /// </summary>
        public void StylePanel(Image img, bool red = false)
        {
            if (img == null) return;
            Sprite s = red ? FrameMainMenuRed : FrameMainMenu;
            if (s != null)
            {
                img.sprite = s;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        /// <summary>
        /// Apply Bloodlines progress bar styling (health/super/burst).
        /// </summary>
        public void StyleProgressBar(Image bgImg, Image fillImg, int variant = 1)
        {
            if (bgImg != null && PBRectEmpty != null)
            {
                bgImg.sprite = PBRectEmpty;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            if (fillImg != null)
            {
                Sprite fill = variant switch
                {
                    2 => PBRectFull_V2,
                    3 => PBRectFull_V3,
                    4 => PBRectFull_V4,
                    5 => PBRectFull_V5,
                    _ => PBRectFull_V1,
                };
                if (fill != null)
                {
                    fillImg.sprite = fill;
                    fillImg.type = Image.Type.Sliced;
                    fillImg.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Apply Bloodlines outline frame to an Image.
        /// </summary>
        public void StyleOutlineFrame(Image img, bool red = false, bool v2 = false)
        {
            if (img == null) return;
            Sprite s = v2 ? FrameOutlineV2 : (red ? FrameOutlineRed : FrameOutline);
            if (s != null)
            {
                img.sprite = s;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        /// <summary>
        /// Apply Bloodlines notice/popup frame to an Image.
        /// </summary>
        public void StyleNotice(Image img, int variant = 1)
        {
            if (img == null) return;
            Sprite s = variant == 2 ? NoticeV2 : NoticeV1;
            if (s != null)
            {
                img.sprite = s;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        /// <summary>
        /// Apply Bloodlines input field frame to an Image.
        /// </summary>
        public void StyleInputField(Image img, bool red = false)
        {
            if (img == null) return;
            Sprite s = red ? InputFrameRed : InputFrameGray;
            if (s != null)
            {
                img.sprite = s;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }
    }
}
