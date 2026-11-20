using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DemonIdol.Cutscenes;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master Credits Screen for Demon Idol.
    /// Displays production credits in an elegant Sumi-e dark brush layout with auto-scroll,
    /// recognizing game design, rollback engineering, character art, sound synthesis, and licensors.
    /// Features dynamic fallback generation for 100% scene independence.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CreditsUI : MonoBehaviour
    {
        public static CreditsUI Instance { get; private set; }

        public const string CREDITS_TEXT =
            "DEMON IDOL\n" +
            "TEKKEN × BLAZBLUE COMBAT REVOLUTION\n\n" +
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
            "GAME DIRECTION & SYSTEM ARCHITECTURE\n" +
            "Amber & DeepMind Antigravity Pair Programming Team\n\n" +
            "COMBAT SYSTEMS DESIGN\n" +
            "Tekken 3D Sidestep & Frame Traps Engine\n" +
            "BlazBlue Gatling Cancel & Astral Finish Matrix\n" +
            "Okizeme Ground Wakeup & Dynamic Tech Rolls\n\n" +
            "DETERMINISTIC ROLLBACK NETWORK\n" +
            "Photon Quantum 3.0 Engine (Exit Games)\n" +
            "Photon Realtime Cloud Relay\n\n" +
            "BACKEND & CLOUD PERSISTENCE\n" +
            "Firebase Authentication (Guest & Email/Password)\n" +
            "Cloud Firestore Player Profiles & ELO Matrix\n" +
            "Firebase Cloud Messaging (FCM)\n\n" +
            "VISUAL EFFECTS & GRAPHICS\n" +
            "Jean Moreno — Cartoon FX Remaster (CFXR)\n" +
            "Sumi-e Dynamic Brush & Ink Splatter Shaders\n" +
            "Flooded Grounds Environmental Shaders\n\n" +
            "CHARACTER ROSTER & 3D MODELS\n" +
            "Demon Idol Studio — 18-Fighter 6-Clan Roster & Standalone URP Anime Toon Shaders\n" +
            "Demon Idol 60Hz Homegrown Deterministic 2.5D Combat Engine\n\n" +
            "AUDIO SYNTHESIS & SOUND DESIGN\n" +
            "Demon Idol Procedural 44.1kHz ADSR Synthesizer\n" +
            "Flooded Grounds Ambient Soundscapes\n" +
            "Alebardium Bloodlines Audio Toolkit\n\n" +
            "LICENSES & THIRD-PARTY ATTRIBUTIONS\n" +
            "\"College Raimon\" Japanese campus environment assets by BuildVerse (Unity Asset Store EULA)\n" +
            "Motion capture: Open Motion Project by ACCAD/The Ohio State University, licensed under CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/)\n" +
            "Fleshy Fight Sounds by will_leamon (opengameart.org), OGA-BY 3.0\n" +
            "\"Low-Poly Katana\" (https://skfb.ly/oWS9S) by AF1Q, licensed under CC BY 4.0\n" +
            "\"Stylized Rock\" (https://skfb.ly/ouBTP) by Jaspreet Singh, licensed under CC BY 4.0\n" +
            "Sonniss #GameAudioGDC 2026 Sound Library (Sonniss GDC License v2.0)\n" +
            "KayKit & Quaternius Universal Animation Libraries (CC0)\n" +
            "Kenney Game Assets & Audio Suites (CC0 1.0)\n" +
            "Kronbits Particle Pack & Poly Haven 2K HDRIs (CC0)\n\n" +
            "MOBILE STORE COMPLIANCE & ACCESSIBILITY\n" +
            "Apple App Store Guideline 5.1.1(v) Compliant\n" +
            "Google Play Data Safety Certified\n" +
            "120 FPS iPhone ProMotion & Notch Dynamic Island Safe Area\n\n" +
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
            "THANK YOU FOR PLAYING DEMON IDOL!\n" +
            "© 2026 Famous Web Interactive Studio. All rights reserved.";

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Text _creditsText;
        [SerializeField] private RectTransform _contentRect;
        [SerializeField] private Button _btnClose;
        [SerializeField] private float _scrollSpeed = 40f;

        [Header("Video Background Loop")]
        [SerializeField] private VideoPlayer _bgVideoPlayer;
        [SerializeField] private RawImage _bgVideoRawImage;
        [SerializeField] private RenderTexture _bgRenderTexture;

        private Coroutine _scrollRoutine;
        private bool _isPaused = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
            gameObject.SetActive(false);
        }

        public void Open()
        {
            EnsureUIHierarchy();
            gameObject.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            if (_bgVideoPlayer != null)
            {
                _bgVideoPlayer.Play();
            }

            if (_contentRect != null)
            {
                _contentRect.anchoredPosition = new Vector2(0f, -250f);
            }

            if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
            _scrollRoutine = StartCoroutine(AutoScrollSequence());
        }

        public void Close()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_bgVideoPlayer != null && _bgVideoPlayer.isPlaying)
            {
                _bgVideoPlayer.Stop();
            }
            if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
            gameObject.SetActive(false);
        }

        private IEnumerator AutoScrollSequence()
        {
            while (true)
            {
                if (!_isPaused && _contentRect != null)
                {
                    _contentRect.anchoredPosition += new Vector2(0f, _scrollSpeed * Time.unscaledDeltaTime);
                }
                yield return null;
            }
        }

        public void EnsureUIHierarchy()
        {
            if (_canvasGroup != null) return;

            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform rt = UiHierarchy.RequireRectTransform(gameObject);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Background
            Image bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }
            if (atlas != null) atlas.StylePanel(bg, false);
            else bg.color = new Color(0.03f, 0.03f, 0.05f, 0.98f);

            // Video Background Loop (Atmospheric live loop behind credits text)
            if (_bgVideoRawImage == null && VideoPathResolver.Resolve("credits_ending_loop", out var clip, out var url, out _))
            {
                GameObject vGo = new GameObject("RawImage_VideoBg");
                vGo.transform.SetParent(transform, false);
                vGo.transform.SetSiblingIndex(0); // Behind text elements

                _bgVideoRawImage = vGo.AddComponent<RawImage>();
                RectTransform vRt = _bgVideoRawImage.rectTransform;
                vRt.anchorMin = Vector2.zero;
                vRt.anchorMax = Vector2.one;
                vRt.sizeDelta = Vector2.zero;
                _bgVideoRawImage.color = new Color(1f, 1f, 1f, 0.35f); // Semi-transparent ambient wash

                if (_bgVideoPlayer == null) _bgVideoPlayer = gameObject.AddComponent<VideoPlayer>();
                _bgVideoPlayer.playOnAwake = false;
                _bgVideoPlayer.isLooping = true;
                _bgVideoPlayer.renderMode = VideoRenderMode.RenderTexture;

                if (_bgRenderTexture == null)
                {
                    _bgRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                    _bgRenderTexture.name = "Credits_RenderTexture";
                    _bgRenderTexture.Create();
                }

                _bgVideoPlayer.targetTexture = _bgRenderTexture;
                _bgVideoRawImage.texture = _bgRenderTexture;

                if (clip != null)
                {
                    _bgVideoPlayer.source = VideoSource.VideoClip;
                    _bgVideoPlayer.clip = clip;
                }
                else if (!string.IsNullOrEmpty(url))
                {
                    _bgVideoPlayer.source = VideoSource.Url;
                    _bgVideoPlayer.url = url;
                }
            }

            // Mask / Scroll Container
            GameObject maskObj = new GameObject("Mask_Container", typeof(RectTransform), typeof(RectMask2D));
            maskObj.transform.SetParent(transform, false);
            RectTransform mrt = maskObj.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(740f, 520f);

            // Content Rect
            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(maskObj.transform, false);
            _contentRect = contentObj.GetComponent<RectTransform>();
            _contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            _contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            _contentRect.sizeDelta = new Vector2(700f, 1600f);
            _contentRect.anchoredPosition = new Vector2(0f, -250f);

            // Credits Text
            GameObject txtObj = new GameObject("Text_Credits", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(contentObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            _creditsText = txtObj.GetComponent<Text>();
            _creditsText.font = font;
            _creditsText.text = CREDITS_TEXT;
            _creditsText.fontSize = 14;
            _creditsText.alignment = TextAnchor.UpperCenter;
            _creditsText.color = SumiStyleTokens.SumiOffWhite;
            _creditsText.lineSpacing = 1.2f;

            // Header Banner
            GameObject hdrObj = new GameObject("Header_Credits", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hdrObj.transform.SetParent(transform, false);
            RectTransform hrt = hdrObj.GetComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(0f, 270f);
            hrt.sizeDelta = new Vector2(400f, 40f);
            Text hdrTxt = hdrObj.GetComponent<Text>();
            hdrTxt.font = font;
            hdrTxt.text = "CREDITS & LICENSES";
            hdrTxt.fontSize = 22;
            hdrTxt.fontStyle = FontStyle.Bold;
            hdrTxt.alignment = TextAnchor.MiddleCenter;
            hdrTxt.color = SumiStyleTokens.BrushGold;

            // Close Button
            GameObject btnObj = new GameObject("Btn_CloseCredits", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(transform, false);
            RectTransform brt = btnObj.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(340f, 270f);
            brt.sizeDelta = new Vector2(40f, 40f);
            Image btnImg = btnObj.GetComponent<Image>();
            _btnClose = btnObj.GetComponent<Button>();
            _btnClose.targetGraphic = btnImg;
            if (atlas != null) atlas.StyleButton(_btnClose, 3, false);
            else btnImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
            _btnClose.onClick.AddListener(Close);

            GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            closeTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform crt = closeTxtObj.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.sizeDelta = Vector2.zero;
            Text ctxt = closeTxtObj.GetComponent<Text>();
            ctxt.font = font;
            ctxt.text = "✕";
            ctxt.fontSize = 18;
            ctxt.alignment = TextAnchor.MiddleCenter;
            ctxt.color = Color.white;
        }
    }
}
