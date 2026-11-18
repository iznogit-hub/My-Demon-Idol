using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemonIdol.UI
{
    /// <summary>
    /// Global Loading Screen & Real-time Feedback UI for Demon Idol.
    /// Manages animated Sumi-e loading spinners, dynamic progress bar (0% - 100%),
    /// rotating high-tier combat tips (Tekken KBD, BlazBlue Bursts/Gatlings),
    /// and asynchronous scene/network transition smoothing.
    /// Features dynamic fallback generation for 100% scene independence.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LoadingFeedbackUI : MonoBehaviour
    {
        public static LoadingFeedbackUI Instance { get; private set; }

        private static readonly string[] CombatTips = new string[]
        {
            "TEKKEN KBD: Korean Backdash cancels b, n, b with d/b to reset recovery early and retreat safely.",
            "BLAZBLUE GATLING: Confirm Light into Medium, Heavy, and Drive on hit to unleash high-damage chains.",
            "BARRIER BLOCK: Hold the Barrier button while guarding to negate chip damage and push the attacker back.",
            "PSYCHIC BURST: Tap Burst while trapped in hitstun to blast the attacker away and reset the neutral game.",
            "DOMAIN EXPANSION: Reach 100% Super Meter to freeze combat ticks for 20 frames and execute your Astral Finish.",
            "OKIZEME WAKEUP: Grounded fighters are immune to High/Mid/Throw. Only Low stomps can strike OTG.",
            "TECH ROLL: Tap Sidestep while grounded to execute an invincible 14-tick recovery roll away from danger.",
            "3D SIDESTEP: Double-tap Up or Down to sidestep linear projectile and strike trajectories into the Z-plane.",
            "TACTICAL THROW: Press Light and Medium simultaneously on the same tick to grab an opponent through their guard."
        };

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text _percentText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _tipText;
        [SerializeField] private RectTransform _spinnerTransform;

        [Header("Spinner Animation")]
        [SerializeField] private float _spinnerRotationSpeed = 240f;

        private Coroutine _loadingRoutine;
        private Coroutine _tipRotationRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
            HideInstant();
        }

        private void Update()
        {
            if (_spinnerTransform != null && _canvasGroup != null && _canvasGroup.alpha > 0.01f)
            {
                _spinnerTransform.Rotate(0f, 0f, -_spinnerRotationSpeed * Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// Displays the loading screen for a specified duration with simulated or real progress.
        /// </summary>
        public void ShowLoading(float duration, string status = "SYNCHRONIZING COMBAT TICK ENGINE...", Action onComplete = null)
        {
            EnsureUIHierarchy();
            gameObject.SetActive(true);

            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);
            if (_tipRotationRoutine != null) StopCoroutine(_tipRotationRoutine);

            _loadingRoutine = StartCoroutine(LoadingSequence(duration, status, onComplete));
            _tipRotationRoutine = StartCoroutine(TipRotationSequence());
        }

        /// <summary>
        /// Updates the real-time progress bar (0.0 to 1.0).
        /// </summary>
        public void SetProgress(float progress, string status = null)
        {
            EnsureUIHierarchy();
            gameObject.SetActive(true);

            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_progressBar != null) _progressBar.value = Mathf.Clamp01(progress);
            if (_percentText != null) _percentText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
            if (!string.IsNullOrEmpty(status) && _statusText != null) _statusText.text = status;
        }

        public void HideLoading(float fadeDuration = 0.35f, Action onHidden = null)
        {
            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);
            if (_tipRotationRoutine != null) StopCoroutine(_tipRotationRoutine);

            StartCoroutine(FadeOut(fadeDuration, onHidden));
        }

        public void HideInstant()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator LoadingSequence(float duration, string status, Action onComplete)
        {
            if (_statusText != null) _statusText.text = status;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                if (_progressBar != null) _progressBar.value = progress;
                if (_percentText != null) _percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
                yield return null;
            }

            if (_progressBar != null) _progressBar.value = 1f;
            if (_percentText != null) _percentText.text = "100%";

            yield return new WaitForSecondsRealtime(0.15f);
            onComplete?.Invoke();
            HideLoading();
        }

        private IEnumerator TipRotationSequence()
        {
            int tipIdx = UnityEngine.Random.Range(0, CombatTips.Length);
            while (true)
            {
                if (_tipText != null)
                {
                    _tipText.text = CombatTips[tipIdx % CombatTips.Length];
                }
                tipIdx++;
                yield return new WaitForSecondsRealtime(4.0f);
            }
        }

        private IEnumerator FadeOut(float duration, Action onHidden)
        {
            if (_canvasGroup == null)
            {
                gameObject.SetActive(false);
                onHidden?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            onHidden?.Invoke();
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

            // Background Image
            Image bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }
            if (atlas != null) atlas.StylePanel(bg, false);
            else bg.color = new Color(0.04f, 0.04f, 0.06f, 0.98f);

            RectTransform rootRt = UiHierarchy.RequireRectTransform(gameObject);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // Rotating Spinner
            if (_spinnerTransform == null)
            {
                GameObject spinObj = new GameObject("Spinner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                spinObj.transform.SetParent(transform, false);
                _spinnerTransform = spinObj.GetComponent<RectTransform>();
                _spinnerTransform.anchoredPosition = new Vector2(0f, 60f);
                _spinnerTransform.sizeDelta = new Vector2(70f, 70f);

                Image spinImg = spinObj.GetComponent<Image>();
                if (atlas != null && atlas.CheckRound1 != null)
                {
                    spinImg.sprite = atlas.CheckRound1;
                    spinImg.color = Color.white;
                }
                else
                {
                    spinImg.color = SumiStyleTokens.ToriiRed;
                }
            }

            // Status Caption
            if (_statusText == null)
            {
                GameObject stObj = new GameObject("Text_Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                stObj.transform.SetParent(transform, false);
                RectTransform srt = stObj.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(0f, -10f);
                srt.sizeDelta = new Vector2(600f, 35f);
                _statusText = stObj.GetComponent<Text>();
                _statusText.font = font;
                _statusText.text = "SYNCHRONIZING COMBAT TICK ENGINE...";
                _statusText.fontSize = 15;
                _statusText.fontStyle = FontStyle.Bold;
                _statusText.alignment = TextAnchor.MiddleCenter;
                _statusText.color = SumiStyleTokens.SumiOffWhite;
            }

            // Progress Slider
            if (_progressBar == null)
            {
                GameObject sliderObj = new GameObject("Slider_Progress", typeof(RectTransform), typeof(Slider));
                sliderObj.transform.SetParent(transform, false);
                RectTransform srt = sliderObj.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(0f, -60f);
                srt.sizeDelta = new Vector2(420f, 18f);

                _progressBar = sliderObj.GetComponent<Slider>();
                _progressBar.minValue = 0f;
                _progressBar.maxValue = 1f;

                // Background
                GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgObj.transform.SetParent(sliderObj.transform, false);
                RectTransform bgrt = bgObj.GetComponent<RectTransform>();
                bgrt.anchorMin = Vector2.zero;
                bgrt.anchorMax = Vector2.one;
                bgrt.sizeDelta = Vector2.zero;
                Image sliderBg = bgObj.GetComponent<Image>();
                if (atlas != null && atlas.PBRectEmpty != null)
                {
                    sliderBg.sprite = atlas.PBRectEmpty;
                    sliderBg.type = Image.Type.Sliced;
                    sliderBg.color = Color.white;
                }
                else
                {
                    sliderBg.color = new Color(0.15f, 0.15f, 0.18f, 0.8f);
                }

                // Fill Area
                GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
                fillArea.transform.SetParent(sliderObj.transform, false);
                RectTransform fart = fillArea.GetComponent<RectTransform>();
                fart.anchorMin = Vector2.zero;
                fart.anchorMax = Vector2.one;
                fart.sizeDelta = Vector2.zero;

                GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fill.transform.SetParent(fillArea.transform, false);
                RectTransform frt = fill.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.sizeDelta = Vector2.zero;
                Image fillImg = fill.GetComponent<Image>();
                if (atlas != null && atlas.PBRectFull_V5 != null)
                {
                    fillImg.sprite = atlas.PBRectFull_V5;
                    fillImg.type = Image.Type.Sliced;
                    fillImg.color = Color.white;
                }
                else
                {
                    fillImg.color = SumiStyleTokens.BrushGold;
                }

                _progressBar.fillRect = frt;
            }

            // Percent Text
            if (_percentText == null)
            {
                GameObject pctObj = new GameObject("Text_Percent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                pctObj.transform.SetParent(transform, false);
                RectTransform prt = pctObj.GetComponent<RectTransform>();
                prt.anchoredPosition = new Vector2(0f, -90f);
                prt.sizeDelta = new Vector2(200f, 25f);
                _percentText = pctObj.GetComponent<Text>();
                _percentText.font = font;
                _percentText.text = "0%";
                _percentText.fontSize = 14;
                _percentText.alignment = TextAnchor.MiddleCenter;
                _percentText.color = SumiStyleTokens.BrushGold;
            }

            // Combat Tip Text
            if (_tipText == null)
            {
                GameObject tipObj = new GameObject("Text_CombatTip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                tipObj.transform.SetParent(transform, false);
                RectTransform trt = tipObj.GetComponent<RectTransform>();
                trt.anchoredPosition = new Vector2(0f, -145f);
                trt.sizeDelta = new Vector2(650f, 60f);
                _tipText = tipObj.GetComponent<Text>();
                _tipText.font = font;
                _tipText.text = CombatTips[0];
                _tipText.fontSize = 13;
                _tipText.fontStyle = FontStyle.Italic;
                _tipText.alignment = TextAnchor.MiddleCenter;
                _tipText.color = new Color(0.8f, 0.8f, 0.85f, 0.9f);
            }
        }
    }
}
