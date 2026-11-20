using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Combat;
using DemonIdol.Audio;
using Input = UnityEngine.Input;

namespace DemonIdol.UI
{
    using Input = UnityEngine.Input;
    /// <summary>
    /// AAA Pre-Match VS Splash Screen.
    /// Renders high-impact anime split-screen clash between P1 Challenger and P2 Opponent:
    /// - Diagonal neon energy slashes (Cyan P1 vs Crimson P2)
    /// - Dynamic character titles, archetypes, and domain expansion previews
    /// - Calligraphy Kanji + "VS" clash emblem with impact animation
    /// - Audio announcer callouts and stage indicator
    /// - Auto-proceeds to combat arena scene on sequence completion or keypress
    /// </summary>
    public class PreMatchVSOverlay : MonoBehaviour
    {
        public static PreMatchVSOverlay Instance { get; private set; }

        [Header("Root & Canvas")]
        public GameObject OverlayRoot;
        public CanvasGroup CanvasGroup;

        [Header("P1 Challenger UI (Cyan)")]
        public RectTransform P1Panel;
        public Text P1NameText;
        public Text P1TitleText;
        public Text P1ArchetypeText;
        public Text P1DomainText;
        public Image P1PortraitImage;

        [Header("P2 Opponent UI (Crimson)")]
        public RectTransform P2Panel;
        public Text P2NameText;
        public Text P2TitleText;
        public Text P2ArchetypeText;
        public Text P2DomainText;
        public Image P2PortraitImage;

        [Header("Center Clash Emblem")]
        public RectTransform CenterClashRoot;
        public Text VsEmblemText;
        public Text KanjiClashText;
        public Text StageBadgeText;
        public Text ModeBadgeText;

        [Header("Timing")]
        public float introDurationSeconds = 1.8f;

        private Coroutine _introRoutine;
        private Action _onCompleteCallback;
        private bool _isIntroActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (OverlayRoot != null) OverlayRoot.SetActive(false);
            if (CanvasGroup != null) CanvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (_isIntroActive)
            {
                // Allow instant skip on space, enter, or submit
                if (UnityEngine.Input.anyKeyDown || (UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    CompleteSequence();
                }
            }
        }

        public void PlayMatchIntro(string p1Id, string p2Id, string stageId, string mode, Action onComplete)
        {
            _onCompleteCallback = onComplete;
            _isIntroActive = true;

            PopulateFighterData(p1Id, p2Id, stageId, mode);

            if (OverlayRoot != null) OverlayRoot.SetActive(true);

            if (_introRoutine != null) StopCoroutine(_introRoutine);
            _introRoutine = StartCoroutine(AnimateIntroSequence());
        }

        private void PopulateFighterData(string p1Id, string p2Id, string stageId, string mode)
        {
            var p1Def = CharacterRegistry.GetCharacterById(p1Id);
            var p2Def = CharacterRegistry.GetCharacterById(p2Id);

            if (P1NameText != null) P1NameText.text = p1Def != null ? p1Def.displayName.ToUpper() : p1Id.ToUpper();
            if (P1TitleText != null) P1TitleText.text = p1Def != null ? p1Def.title.ToUpper() : "CHALLENGER";
            if (P1ArchetypeText != null) P1ArchetypeText.text = p1Def != null ? $"ARCHETYPE: {p1Def.archetype.ToUpper()}" : "";
            if (P1DomainText != null) P1DomainText.text = p1Def != null ? $"⚡ DOMAIN: {p1Def.domainName.ToUpper()}" : "";

            if (P2NameText != null) P2NameText.text = p2Def != null ? p2Def.displayName.ToUpper() : p2Id.ToUpper();
            if (P2TitleText != null) P2TitleText.text = p2Def != null ? p2Def.title.ToUpper() : "OPPONENT";
            if (P2ArchetypeText != null) P2ArchetypeText.text = p2Def != null ? $"ARCHETYPE: {p2Def.archetype.ToUpper()}" : "";
            if (P2DomainText != null) P2DomainText.text = p2Def != null ? $"⚡ DOMAIN: {p2Def.domainName.ToUpper()}" : "";

            // Portraits if assigned
            if (p1Def != null && p1Def.portraitSprite != null && P1PortraitImage != null)
            {
                P1PortraitImage.sprite = p1Def.portraitSprite;
                P1PortraitImage.color = Color.white;
            }
            if (p2Def != null && p2Def.portraitSprite != null && P2PortraitImage != null)
            {
                P2PortraitImage.sprite = p2Def.portraitSprite;
                P2PortraitImage.color = Color.white;
            }

            // Stage and Mode
            if (StageBadgeText != null) StageBadgeText.text = $"ARENA: {stageId.ToUpper()} // TOURNAMENT PLATFORM";
            if (ModeBadgeText != null) ModeBadgeText.text = $"[ {mode.ToUpper()} BATTLE ]";

            // Audio Callout
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayDomainActivation();
            }
        }

        private IEnumerator AnimateIntroSequence()
        {
            float elapsed = 0f;

            // Initial positions
            Vector2 p1Start = new Vector2(-400f, 0f);
            Vector2 p1End = Vector2.zero;
            Vector2 p2Start = new Vector2(400f, 0f);
            Vector2 p2End = Vector2.zero;

            Vector3 vsScaleStart = Vector3.one * 2.5f;
            Vector3 vsScaleEnd = Vector3.one;

            while (elapsed < introDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.35f);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (CanvasGroup != null) CanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.15f);

                if (P1Panel != null) P1Panel.anchoredPosition = Vector2.Lerp(p1Start, p1End, smoothT);
                if (P2Panel != null) P2Panel.anchoredPosition = Vector2.Lerp(p2Start, p2End, smoothT);

                if (CenterClashRoot != null)
                {
                    CenterClashRoot.localScale = Vector3.Lerp(vsScaleStart, vsScaleEnd, smoothT);
                }

                yield return null;
            }

            CompleteSequence();
        }

        private void CompleteSequence()
        {
            if (!_isIntroActive) return;
            _isIntroActive = false;

            if (_introRoutine != null)
            {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }

            var callback = _onCompleteCallback;
            _onCompleteCallback = null;
            callback?.Invoke();
        }
    }
}
