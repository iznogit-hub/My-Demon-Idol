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
        public Text P1DriveText;
        public Text P2DriveText;
        public Text CountdownText;

        [Header("Timing")]
        public float introDurationSeconds = 1.8f;

        [Header("Diagonal Blade-Split AMV Visuals")]
        public Image diagonalSlashLine;
        public Image inkShatterWipeOverlay;

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

            // Dynamically ensure diagonal blade slash if not serialized
            if (diagonalSlashLine == null)
            {
                var slashObj = new GameObject("Dynamic_DiagonalSlash", typeof(RectTransform), typeof(Image));
                slashObj.transform.SetParent(transform, false);
                slashObj.transform.rotation = Quaternion.Euler(0f, 0f, 25f);
                var rt = slashObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(8f, 1600f);
                diagonalSlashLine = slashObj.GetComponent<Image>();
                diagonalSlashLine.color = SumiStyleTokens.Vermilion;
                diagonalSlashLine.raycastTarget = false;
                diagonalSlashLine.gameObject.SetActive(false);
            }
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

        public void PopulateFighterData(string p1Id, string p2Id, string stageId, string mode)
        {
            var p1Def = CharacterRegistry.GetCharacterById(p1Id);
            var p2Def = CharacterRegistry.GetCharacterById(p2Id);

            if (P1NameText != null) P1NameText.text = p1Def != null ? p1Def.displayName.ToUpper() : p1Id.ToUpper();
            if (P1TitleText != null) P1TitleText.text = p1Def != null ? p1Def.title.ToUpper() : "CHALLENGER";
            if (P1ArchetypeText != null) P1ArchetypeText.text = p1Def != null ? $"ARCHETYPE: {p1Def.archetype.ToUpper()}" : "";
            if (P1DomainText != null) P1DomainText.text = p1Def != null ? $"⚡ DOMAIN: {p1Def.domainName.ToUpper()}" : "";
            ApplyCard(p1Id, P1PortraitImage, P1DriveText, P1NameText);
            ApplyCard(p2Id, P2PortraitImage, P2DriveText, P2NameText);

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
            if (StageBadgeText != null) StageBadgeText.text = "COLLEGE RAIMON // " + (string.IsNullOrEmpty(stageId) ? "ST07" : stageId.ToUpper());
            if (ModeBadgeText != null) ModeBadgeText.text = $"[ {mode.ToUpper()} BATTLE ]";

            // Audio Callout
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayDomainActivation();
            }
        }

        private void SetGroupActive(bool stage, bool namesDrives, bool roundBanner, bool fightBanner)
        {
            if (StageBadgeText != null) StageBadgeText.gameObject.SetActive(stage);
            if (ModeBadgeText != null) ModeBadgeText.gameObject.SetActive(stage);

            if (P1Panel != null) P1Panel.gameObject.SetActive(namesDrives);
            if (P2Panel != null) P2Panel.gameObject.SetActive(namesDrives);
            if (P1NameText != null) P1NameText.gameObject.SetActive(namesDrives);
            if (P1DriveText != null) P1DriveText.gameObject.SetActive(namesDrives);
            if (P2NameText != null) P2NameText.gameObject.SetActive(namesDrives);
            if (P2DriveText != null) P2DriveText.gameObject.SetActive(namesDrives);

            if (CountdownText != null)
            {
                CountdownText.gameObject.SetActive(roundBanner || fightBanner);
                if (roundBanner)
                {
                    CountdownText.text = "ROUND 1";
                    CountdownText.fontSize = 76;
                    CountdownText.color = new Color(1.0f, 0.90f, 0.35f, 1f);
                    CountdownText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                }
                else if (fightBanner)
                {
                    CountdownText.text = "FIGHT";
                    CountdownText.fontSize = 88;
                    CountdownText.color = new Color(1.0f, 0.22f, 0.22f, 1f);
                    CountdownText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                }
            }
        }

        private IEnumerator AnimateIntroSequence()
        {
            if (CanvasGroup != null) CanvasGroup.alpha = 1f;

            // STEP 1: Stage name (1s), centered and cleared before next
            SetGroupActive(stage: true, namesDrives: false, roundBanner: false, fightBanner: false);
            if (StageBadgeText != null)
            {
                StageBadgeText.rectTransform.anchoredPosition = new Vector2(0f, 30f);
                StageBadgeText.fontSize = 52;
            }
            if (ModeBadgeText != null)
            {
                ModeBadgeText.rectTransform.anchoredPosition = new Vector2(0f, -30f);
                ModeBadgeText.fontSize = 28;
            }
            yield return new WaitForSecondsRealtime(1.0f);

            // Clear stage name before next
            SetGroupActive(stage: false, namesDrives: false, roundBanner: false, fightBanner: false);
            yield return new WaitForSecondsRealtime(0.05f);

            // STEP 2: Diagonal Blade-Split Clash (P1 left, P2 right, glowing slash in center)
            SetGroupActive(stage: false, namesDrives: true, roundBanner: false, fightBanner: false);
            if (diagonalSlashLine != null)
            {
                diagonalSlashLine.gameObject.SetActive(true);
                diagonalSlashLine.color = SumiStyleTokens.Vermilion;
            }
            if (P1NameText != null)
            {
                P1NameText.rectTransform.anchoredPosition = new Vector2(-460f, 380f);
                P1NameText.fontSize = 40;
            }
            if (P1DriveText != null)
            {
                P1DriveText.rectTransform.anchoredPosition = new Vector2(-460f, 330f);
                P1DriveText.fontSize = 26;
            }
            if (P2NameText != null)
            {
                P2NameText.rectTransform.anchoredPosition = new Vector2(460f, 380f);
                P2NameText.fontSize = 40;
            }
            if (P2DriveText != null)
            {
                P2DriveText.rectTransform.anchoredPosition = new Vector2(460f, 330f);
                P2DriveText.fontSize = 26;
            }
            yield return new WaitForSecondsRealtime(1.5f);

            // Clear names and drives before next
            SetGroupActive(stage: false, namesDrives: false, roundBanner: false, fightBanner: false);
            if (diagonalSlashLine != null) diagonalSlashLine.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.05f);

            // STEP 3: "ROUND 1" (1.0s), centered and cleared before next
            SetGroupActive(stage: false, namesDrives: false, roundBanner: true, fightBanner: false);
            yield return new WaitForSecondsRealtime(1.0f);

            // Clear ROUND 1 before next
            SetGroupActive(stage: false, namesDrives: false, roundBanner: false, fightBanner: false);
            yield return new WaitForSecondsRealtime(0.05f);

            // STEP 4: "FIGHT" once (0.8s), centered, then cleared
            SetGroupActive(stage: false, namesDrives: false, roundBanner: false, fightBanner: true);
            yield return new WaitForSecondsRealtime(0.8f);

            // STEP 5: Nothing may cover the fighters after FIGHT
            SetGroupActive(stage: false, namesDrives: false, roundBanner: false, fightBanner: false);
            if (CanvasGroup != null) CanvasGroup.alpha = 0f;
            if (OverlayRoot != null) OverlayRoot.SetActive(false);

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

            // Ensure all overlay text is hidden so nothing covers fighters
            SetGroupActive(stage: false, namesDrives: false, roundBanner: false, fightBanner: false);
            if (CanvasGroup != null) CanvasGroup.alpha = 0f;
            if (OverlayRoot != null) OverlayRoot.SetActive(false);
            gameObject.SetActive(false);

            var callback = _onCompleteCallback;
            _onCompleteCallback = null;
            callback?.Invoke();
        }

        public void SetCountdown(string value)
        {
            if (CountdownText != null) CountdownText.text = value;
        }

        public static PreMatchVSOverlay CreateRuntime()
        {
            if (Instance != null) return Instance;
            var canvasGo = new GameObject("VSCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var host = new GameObject("PreMatchVSOverlay");
            host.transform.SetParent(canvasGo.transform, false);
            var overlay = host.AddComponent<PreMatchVSOverlay>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overlay.P1NameText = MakeText(host.transform, "P1Name", font, new Vector2(-460f, 380f));
            overlay.P2NameText = MakeText(host.transform, "P2Name", font, new Vector2(460f, 380f));
            overlay.P1DriveText = MakeText(host.transform, "P1Drive", font, new Vector2(-460f, 330f));
            overlay.P2DriveText = MakeText(host.transform, "P2Drive", font, new Vector2(460f, 330f));
            overlay.StageBadgeText = MakeText(host.transform, "Stage", font, new Vector2(0f, 30f));
            overlay.StageBadgeText.rectTransform.sizeDelta = new Vector2(1000f, 60f);
            overlay.ModeBadgeText = MakeText(host.transform, "Mode", font, new Vector2(0f, -30f));
            overlay.ModeBadgeText.rectTransform.sizeDelta = new Vector2(600f, 40f);
            overlay.CountdownText = MakeText(host.transform, "Countdown", font, new Vector2(0f, 0f));

            // Initially inactive until triggered in order
            overlay.SetGroupActive(false, false, false, false);
            return overlay;
        }

        static void ApplyCard(string id, Image portrait, Text drive, Text name)
        {
            var card = DemonIdol.BlazBlue.BBPresentationLibrary.Find(id);
            DemonIdol.BlazBlue.BBSignatureTable.TryBase(id, out var signature);
            if (drive != null) drive.text = "DRIVE: " + (card != null && !string.IsNullOrEmpty(card.driveName) ? card.driveName : signature.driveName);
            if (name != null && card != null && !string.IsNullOrEmpty(card.displayName)) name.text = card.displayName.ToUpper();
            if (portrait == null) return;
            if (card != null && card.portrait != null)
            {
                portrait.sprite = card.portrait;
                portrait.color = Color.white;
            }
            else if (card != null)
            {
                portrait.sprite = null;
                portrait.color = card.placeholderColor;
            }
        }

        static Text MakeText(Transform parent, string label, Font font, Vector2 pos)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(520f, 60f);
            return text;
        }
    }
}
