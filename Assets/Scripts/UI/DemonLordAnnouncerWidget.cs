using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Cutscenes;
using DemonIdol.Audio;

namespace DemonIdol.UI
{
    /// <summary>
    /// Interactive Demon Lord Announcer Widget for Demon Idol.
    /// Features:
    /// - High-resolution portrait banner of Lady Aurelia, Demon Lord of the Game.
    /// - Dynamic context-aware commentary for Mode Select, Character Select, and Combat.
    /// - Interactive tap-to-quip voice reactions.
    /// - One-click access to Game Introduction and Daily Routine MP4 cutscenes.
    /// - Fully self-building hierarchy for 100% scene independence.
    /// </summary>
    public class DemonLordAnnouncerWidget : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Image _frameBorder;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _dialogueText;
        [SerializeField] private Button _btnWatchIntro;
        [SerializeField] private Button _btnDailyRoutine;
        [SerializeField] private Button _btnPortraitInteraction;
        [SerializeField] private CanvasGroup _bubbleCanvasGroup;

        private Coroutine _typewriterRoutine;
        private string _targetDialogue = "";
        private bool _isBuilt = false;

        private void Awake()
        {
            EnsureWidgetHierarchy();
        }

        private void OnEnable()
        {
            EnsureWidgetHierarchy();
            LoadPortrait();
        }

        public void EnsureWidgetHierarchy()
        {
            if (_isBuilt && _portraitImage != null && _dialogueText != null) return;

            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();

            // Background Container (Sleek Sumi Dark Wash with Crimson Accent)
            Image bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            if (atlas != null) atlas.StyleOutlineFrame(bg, true, true);
            else bg.color = new Color(0.06f, 0.07f, 0.10f, 0.88f);

            // 1. Portrait Frame Holder
            Transform pHolder = transform.Find("Portrait_Holder");
            GameObject pHolderGo;
            if (pHolder == null)
            {
                pHolderGo = new GameObject("Portrait_Holder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pHolderGo.transform.SetParent(transform, false);
                RectTransform prt = pHolderGo.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0f, 0.5f);
                prt.anchorMax = new Vector2(0f, 0.5f);
                prt.pivot = new Vector2(0f, 0.5f);
                prt.anchoredPosition = new Vector2(10f, 0f);
                prt.sizeDelta = new Vector2(85f, 85f);
            }
            else
            {
                pHolderGo = pHolder.gameObject;
            }

            _frameBorder = pHolderGo.GetComponent<Image>();
            if (atlas != null && atlas.FrameOutlineRed != null)
            {
                _frameBorder.sprite = atlas.FrameOutlineRed;
                _frameBorder.type = Image.Type.Sliced;
                _frameBorder.color = Color.white;
            }
            else
            {
                _frameBorder.color = new Color(0.85f, 0.15f, 0.20f, 1f); // Torii Crimson
            }

            _btnPortraitInteraction = pHolderGo.GetComponent<Button>();
            if (_btnPortraitInteraction == null) _btnPortraitInteraction = pHolderGo.AddComponent<Button>();
            _btnPortraitInteraction.onClick.RemoveAllListeners();
            _btnPortraitInteraction.onClick.AddListener(OnTapPortrait);

            // Portrait Inner Image
            Transform pInner = pHolderGo.transform.Find("Image_Portrait");
            GameObject pInnerGo;
            if (pInner == null)
            {
                pInnerGo = new GameObject("Image_Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pInnerGo.transform.SetParent(pHolderGo.transform, false);
                RectTransform iprt = pInnerGo.GetComponent<RectTransform>();
                iprt.anchorMin = Vector2.zero;
                iprt.anchorMax = Vector2.one;
                iprt.sizeDelta = new Vector2(-6f, -6f);
                iprt.anchoredPosition = Vector2.zero;
            }
            else
            {
                pInnerGo = pInner.gameObject;
            }
            _portraitImage = pInnerGo.GetComponent<Image>();
            _portraitImage.preserveAspect = true;

            // 2. Title & Role Text
            Transform tTr = transform.Find("Text_Title");
            GameObject tGo;
            if (tTr == null)
            {
                tGo = new GameObject("Text_Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                tGo.transform.SetParent(transform, false);
                RectTransform trt = tGo.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 1f);
                trt.anchorMax = new Vector2(1f, 1f);
                trt.pivot = new Vector2(0f, 1f);
                trt.anchoredPosition = new Vector2(105f, -8f);
                trt.sizeDelta = new Vector2(-220f, 22f);
            }
            else
            {
                tGo = tTr.gameObject;
            }
            _titleText = tGo.GetComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 13;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = new Color(1f, 0.82f, 0.35f, 1f); // Pale Gold
            _titleText.text = "★ THE DEMON LORD // LADY AURELIA";

            // 3. Speech Bubble / Dialogue Text
            Transform dTr = transform.Find("Text_Dialogue");
            GameObject dGo;
            if (dTr == null)
            {
                dGo = new GameObject("Text_Dialogue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(CanvasGroup));
                dGo.transform.SetParent(transform, false);
                RectTransform drt = dGo.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0f, 0f);
                drt.anchorMax = new Vector2(1f, 1f);
                drt.pivot = new Vector2(0f, 0.5f);
                drt.anchoredPosition = new Vector2(105f, -12f);
                drt.sizeDelta = new Vector2(-220f, -32f);
            }
            else
            {
                dGo = dTr.gameObject;
            }
            _dialogueText = dGo.GetComponent<Text>();
            _dialogueText.font = font;
            _dialogueText.fontSize = 12;
            _dialogueText.fontStyle = FontStyle.Normal;
            _dialogueText.lineSpacing = 1.15f;
            _dialogueText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            _bubbleCanvasGroup = dGo.GetComponent<CanvasGroup>();

            // 4. Action Button: Watch Intro Cutscene
            Transform btnIntroTr = transform.Find("Btn_WatchIntro");
            GameObject btnIntroGo;
            if (btnIntroTr == null)
            {
                btnIntroGo = new GameObject("Btn_WatchIntro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnIntroGo.transform.SetParent(transform, false);
                RectTransform brt = btnIntroGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 0.5f);
                brt.anchorMax = new Vector2(1f, 0.5f);
                brt.pivot = new Vector2(1f, 0.5f);
                brt.anchoredPosition = new Vector2(-10f, 16f);
                brt.sizeDelta = new Vector2(100f, 30f);

                Image bImg = btnIntroGo.GetComponent<Image>();
                bImg.color = new Color(0.85f, 0.15f, 0.20f, 0.95f); // Crimson

                GameObject bTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                bTxtGo.transform.SetParent(btnIntroGo.transform, false);
                RectTransform btrt = bTxtGo.GetComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.sizeDelta = Vector2.zero;
                Text bTxt = bTxtGo.GetComponent<Text>();
                bTxt.font = font;
                bTxt.fontSize = 11;
                bTxt.fontStyle = FontStyle.Bold;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.color = Color.white;
                bTxt.text = "▶ INTRO";
            }
            else
            {
                btnIntroGo = btnIntroTr.gameObject;
            }
            _btnWatchIntro = btnIntroGo.GetComponent<Button>();
            if (atlas != null) atlas.StyleButton(_btnWatchIntro, 3, true);
            _btnWatchIntro.onClick.RemoveAllListeners();
            _btnWatchIntro.onClick.AddListener(OnClickWatchIntro);

            // 5. Action Button: Daily Routine Cutscene
            Transform btnRoutineTr = transform.Find("Btn_DailyRoutine");
            GameObject btnRoutineGo;
            if (btnRoutineTr == null)
            {
                btnRoutineGo = new GameObject("Btn_DailyRoutine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnRoutineGo.transform.SetParent(transform, false);
                RectTransform rrt = btnRoutineGo.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(1f, 0.5f);
                rrt.anchorMax = new Vector2(1f, 0.5f);
                rrt.pivot = new Vector2(1f, 0.5f);
                rrt.anchoredPosition = new Vector2(-10f, -18f);
                rrt.sizeDelta = new Vector2(100f, 26f);

                Image rImg = btnRoutineGo.GetComponent<Image>();
                rImg.color = new Color(0.20f, 0.22f, 0.32f, 0.95f); // Slate Blue

                GameObject rTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                rTxtGo.transform.SetParent(btnRoutineGo.transform, false);
                RectTransform rtrt = rTxtGo.GetComponent<RectTransform>();
                rtrt.anchorMin = Vector2.zero;
                rtrt.anchorMax = Vector2.one;
                rtrt.sizeDelta = Vector2.zero;
                Text rTxt = rTxtGo.GetComponent<Text>();
                rTxt.font = font;
                rTxt.fontSize = 10;
                rTxt.fontStyle = FontStyle.Normal;
                rTxt.alignment = TextAnchor.MiddleCenter;
                rTxt.color = Color.white;
                rTxt.text = "ROUTINE";
            }
            else
            {
                btnRoutineGo = btnRoutineTr.gameObject;
            }
            _btnDailyRoutine = btnRoutineGo.GetComponent<Button>();
            if (atlas != null) atlas.StyleButton(_btnDailyRoutine, 3, false);
            _btnDailyRoutine.onClick.RemoveAllListeners();
            _btnDailyRoutine.onClick.AddListener(OnClickDailyRoutine);

            _isBuilt = true;
            LoadPortrait();
        }

        private void LoadPortrait()
        {
            if (_portraitImage == null) return;
            var sprite = DemonLordNarratorManager.Instance.GetAnnouncerPortraitSprite();
            if (sprite != null)
            {
                _portraitImage.sprite = sprite;
                _portraitImage.color = Color.white;
            }
        }

        public void SetDialogue(string text, bool typewriter = true)
        {
            EnsureWidgetHierarchy();
            _targetDialogue = text ?? "";

            if (!gameObject.activeInHierarchy || !typewriter)
            {
                if (_dialogueText != null) _dialogueText.text = $"\"{_targetDialogue}\"";
                return;
            }

            if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = StartCoroutine(TypewriterRoutine(_targetDialogue));
        }

        private IEnumerator TypewriterRoutine(string message)
        {
            if (_dialogueText == null) yield break;

            _dialogueText.text = "\"";
            for (int i = 0; i < message.Length; i++)
            {
                _dialogueText.text = $"\"{message.Substring(0, i + 1)}\"";
                if (i % 2 == 0) yield return null;
            }
        }

        public void UpdateForMode(string modeName)
        {
            string speech = DemonLordNarratorManager.Instance.GetModeDialogue(modeName);
            SetDialogue(speech);
        }

        public void UpdateForFighter(string characterId)
        {
            string speech = DemonLordNarratorManager.Instance.GetFighterCommentary(characterId);
            SetDialogue(speech);
        }

        public void UpdateForContext(NarratorContext context)
        {
            string speech = DemonLordNarratorManager.Instance.GetRandomAnnouncerQuote(context);
            SetDialogue(speech);
        }

        private void OnTapPortrait()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            string quote = DemonLordNarratorManager.Instance.GetRandomAnnouncerQuote(NarratorContext.GameStart);
            SetDialogue(quote);

            // Subtle bounce animation on frame
            if (_frameBorder != null)
            {
                StartCoroutine(BounceRoutine(_frameBorder.transform));
            }
        }

        private IEnumerator BounceRoutine(Transform tr)
        {
            Vector3 orig = Vector3.one;
            tr.localScale = orig * 1.12f;
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                tr.localScale = Vector3.Lerp(orig * 1.12f, orig, elapsed / 0.2f);
                yield return null;
            }
            tr.localScale = orig;
        }

        private void OnClickWatchIntro()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            DemonLordNarratorManager.Instance.PlayGameIntroduction();
        }

        private void OnClickDailyRoutine()
        {
            CombatAudioDirector.Instance?.PlayButtonClick();
            DemonLordNarratorManager.Instance.PlayDailyRoutine();
        }
    }
}
