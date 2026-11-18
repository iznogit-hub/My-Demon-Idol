using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DemonIdol.UI;
using DemonIdol.Audio;

namespace DemonIdol.Cutscenes
{
    public enum CutscenePlaybackState
    {
        Idle,
        Preparing,
        Ready,
        Entering,
        Playing,
        Exiting,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Master MP4 Cutscene and Cinematic Engine for Demon Idol.
    /// Features:
    /// - Robust async state machine with Token-guarded lifecycle to ignore stale callbacks.
    /// - Android/iOS cross-platform StreamingAssets URL and VideoClip resolution.
    /// - Deterministic presentation timeout to guarantee gameplay continuity on decode stalls.
    /// - Audio ducking / muting of background match BGM during story and ultimate cutaways.
    /// - Dynamic RenderTexture clearing to CarbonInk to eliminate black flashes and last-frame bleed.
    /// - Accessible one-shot skip button + mobile hold-to-skip (1.2s).
    /// - Background / phone call pause handling.
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        private static CutsceneManager _instance;
        public static CutsceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<CutsceneManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CutsceneManager");
                        _instance = go.AddComponent<CutsceneManager>();
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

        [Header("Components")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CanvasGroup _blackCurtainGroup;
        [SerializeField] private RawImage _displayImage;
        [SerializeField] private Image _stillDisplayImage;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private RenderTexture _renderTexture;

        [Header("Skip UI")]
        [SerializeField] private GameObject _skipPrompt;
        [SerializeField] private Image _skipFillBar;
        [SerializeField] private Button _btnInstantSkip;
        [SerializeField] private float _skipHoldDuration = 1.2f;

        [Header("Captions / Subtitles")]
        [SerializeField] private Text _captionText;
        [SerializeField] private GameObject _captionPanel;

        [Header("Settings & Timers (PROPOSED)")]
        [SerializeField] private float _fadeDuration = 0.35f;
        [SerializeField] private float _defaultPrepareTimeoutSeconds = 3.0f;

        public CutscenePlaybackState State { get; private set; } = CutscenePlaybackState.Idle;
        public bool IsPlaying => State == CutscenePlaybackState.Playing || State == CutscenePlaybackState.Entering;

        private Guid _activeRequestToken = Guid.Empty;
        private Action _onCompleteCallback;
        private Action _onFailedCallback;
        private float _skipTimer = 0f;
        private Coroutine _playbackRoutine;
        private Coroutine _timeoutRoutine;
        private bool _wasMusicMutedByCutscene = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureCutsceneUI();
        }

        public void EnsureCutsceneUI()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
                if (_videoPlayer == null) _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            }

            EnsureRenderTexture();

            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;

            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Cutscene_Canvas");
                canvasGo.transform.SetParent(transform, false);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 250;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = canvas.GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            Transform existingBacking = canvas.transform.Find("Image_SolidBlackBacking");
            if (existingBacking == null)
            {
                GameObject backGo = new GameObject("Image_SolidBlackBacking");
                backGo.transform.SetParent(canvas.transform, false);
                Image backImg = backGo.AddComponent<Image>();
                backImg.color = SumiStyleTokens.CarbonInk;
                RectTransform brt = backImg.rectTransform;
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.sizeDelta = Vector2.zero;
            }

            if (_displayImage == null)
            {
                Transform existingImg = canvas.transform.Find("RawImage_VideoDisplay");
                if (existingImg != null)
                {
                    _displayImage = existingImg.GetComponent<RawImage>();
                }
                else
                {
                    GameObject imgGo = new GameObject("RawImage_VideoDisplay");
                    imgGo.transform.SetParent(canvas.transform, false);
                    _displayImage = imgGo.AddComponent<RawImage>();
                    RectTransform rt = _displayImage.rectTransform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    _displayImage.color = Color.white;
                }
            }
            _displayImage.texture = _renderTexture;
            _displayImage.material = null; // Strict rule: Zero post-processing or overlay shaders on video pixels

            if (_stillDisplayImage == null)
            {
                Transform existingStill = canvas.transform.Find("Image_StillDisplay");
                if (existingStill != null)
                {
                    _stillDisplayImage = existingStill.GetComponent<Image>();
                }
                else
                {
                    GameObject stillGo = new GameObject("Image_StillDisplay");
                    stillGo.transform.SetParent(canvas.transform, false);
                    _stillDisplayImage = stillGo.AddComponent<Image>();
                    RectTransform srt = _stillDisplayImage.rectTransform;
                    srt.anchorMin = Vector2.zero;
                    srt.anchorMax = Vector2.one;
                    srt.sizeDelta = Vector2.zero;
                    srt.anchoredPosition = Vector2.zero;
                    _stillDisplayImage.color = Color.white;
                    _stillDisplayImage.preserveAspect = true;
                    _stillDisplayImage.gameObject.SetActive(false);
                }
            }

            if (_blackCurtainGroup == null)
            {
                Transform existingCurtain = canvas.transform.Find("BlackCurtain_Overlay");
                if (existingCurtain != null)
                {
                    _blackCurtainGroup = existingCurtain.GetComponent<CanvasGroup>();
                }
                else
                {
                    GameObject curtainGo = new GameObject("BlackCurtain_Overlay");
                    curtainGo.transform.SetParent(canvas.transform, false);
                    Image curtainImg = curtainGo.AddComponent<Image>();
                    curtainImg.color = Color.black;
                    RectTransform crt = curtainImg.rectTransform;
                    crt.anchorMin = Vector2.zero;
                    crt.anchorMax = Vector2.one;
                    crt.sizeDelta = Vector2.zero;
                    _blackCurtainGroup = curtainGo.AddComponent<CanvasGroup>();
                }
                _blackCurtainGroup.alpha = 0f;
                _blackCurtainGroup.blocksRaycasts = false;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (_btnInstantSkip == null)
            {
                Transform existingBtn = canvas.transform.Find("Btn_InstantSkip");
                if (existingBtn != null)
                {
                    _btnInstantSkip = existingBtn.GetComponent<Button>();
                }
                else
                {
                    GameObject btnGo = new GameObject("Btn_InstantSkip");
                    btnGo.transform.SetParent(canvas.transform, false);
                    _btnInstantSkip = btnGo.AddComponent<Button>();
                    Image btnImg = btnGo.AddComponent<Image>();
                    btnImg.color = SumiStyleTokens.CarbonInkWash;

                    RectTransform rt = btnGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-40f, -40f);
                    rt.sizeDelta = new Vector2(140f, 40f);

                    GameObject txtGo = new GameObject("Text");
                    txtGo.transform.SetParent(btnGo.transform, false);
                    Text t = txtGo.AddComponent<Text>();
                    t.text = "SKIP >>";
                    t.font = font;
                    t.fontSize = 18;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.color = SumiStyleTokens.PaperWhite;
                    RectTransform txtRt = txtGo.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                }
                _btnInstantSkip.onClick.RemoveListener(OnClickInstantSkip);
                _btnInstantSkip.onClick.AddListener(OnClickInstantSkip);
                _btnInstantSkip.gameObject.SetActive(false);
            }

            if (_captionPanel == null)
            {
                Transform capTrans = canvas.transform.Find("Panel_Captions");
                if (capTrans != null)
                {
                    _captionPanel = capTrans.gameObject;
                    _captionText = _captionPanel.GetComponentInChildren<Text>();
                }
                else
                {
                    _captionPanel = new GameObject("Panel_Captions");
                    _captionPanel.transform.SetParent(canvas.transform, false);
                    Image capBg = _captionPanel.AddComponent<Image>();
                    capBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

                    RectTransform capRt = _captionPanel.GetComponent<RectTransform>();
                    capRt.anchorMin = new Vector2(0.15f, 0.05f);
                    capRt.anchorMax = new Vector2(0.85f, 0.16f);
                    capRt.sizeDelta = Vector2.zero;

                    GameObject txtGo = new GameObject("Text_Caption");
                    txtGo.transform.SetParent(_captionPanel.transform, false);
                    _captionText = txtGo.AddComponent<Text>();
                    _captionText.font = font;
                    _captionText.fontSize = 22;
                    _captionText.alignment = TextAnchor.MiddleCenter;
                    _captionText.color = SumiStyleTokens.PaperWhite;
                    RectTransform txtRt = txtGo.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;

                    _captionPanel.SetActive(false);
                }
            }
        }

        private void EnsureRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                _renderTexture.name = "Cutscene_RenderTexture";
                _renderTexture.Create();
            }
        }

        private void ClearRenderTexture()
        {
            if (_renderTexture == null) return;
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            GL.Clear(true, true, SumiStyleTokens.CarbonInk);
            RenderTexture.active = active;
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (State != CutscenePlaybackState.Playing && State != CutscenePlaybackState.Ready) return;

            // Touch / Mouse Hold to Skip on Mobile
            bool isInteracting = false;
            try
            {
                isInteracting = UnityEngine.Input.GetMouseButton(0) || (UnityEngine.Input.touchCount > 0);
            }
            catch (System.InvalidOperationException)
            {
#if ENABLE_INPUT_SYSTEM
                isInteracting = UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed;
#endif
            }

            if (isInteracting)
            {
                _skipTimer += Time.unscaledDeltaTime;
                if (_skipPrompt != null) _skipPrompt.SetActive(true);
                if (_skipFillBar != null) _skipFillBar.fillAmount = Mathf.Clamp01(_skipTimer / _skipHoldDuration);

                if (_skipTimer >= _skipHoldDuration)
                {
                    SkipCutscene();
                }
            }
            else
            {
                _skipTimer = Mathf.MoveTowards(_skipTimer, 0f, Time.unscaledDeltaTime * 2.5f);
                if (_skipFillBar != null) _skipFillBar.fillAmount = Mathf.Clamp01(_skipTimer / _skipHoldDuration);
                if (_skipTimer <= 0.05f && _skipPrompt != null) _skipPrompt.SetActive(false);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!IsPlaying || _videoPlayer == null) return;

            if (pauseStatus)
            {
                if (_videoPlayer.isPlaying) _videoPlayer.Pause();
                if (_audioSource != null && _audioSource.isPlaying) _audioSource.Pause();
            }
            else
            {
                if (_videoPlayer.isPaused) _videoPlayer.Play();
                if (_audioSource != null) _audioSource.UnPause();
            }
        }

        #region Public Playback API

        public void PlaySuperOverlay(string characterId, bool isConnected, Action onComplete = null)
        {
            // Authoring Rule: Sim first decides CONNECTED vs whiff or block.
            // Only CONNECTED may start the SUPER overlay. Whiff or block plays only live VFX, never a hit film.
            if (!isConnected)
            {
                onComplete?.Invoke();
                return;
            }

            // All six super slots use LIVE_SUPER_VFX (no placeholder clip).
            // Bounded overlay: picture-in-picture or band overlay, gone by sim tick 20 (fade included).
            StartCoroutine(BoundedSuperOverlayRoutine(characterId, onComplete));
        }

        private IEnumerator BoundedSuperOverlayRoutine(string characterId, Action onComplete)
        {
            int maxTicks = 20;
            int ticks = 0;
            while (ticks < maxTicks)
            {
                ticks++;
                yield return null;
            }
            onComplete?.Invoke();
        }

        public void PlayManifestSlot(string slotId, Action onComplete = null, Action onFailed = null, string locale = "en")
        {
            var slot = DemonIdol.Combat.VideoBindings.GetSlotBinding(slotId);
            string caption = DemonIdol.Combat.VideoBindings.GetCaption(slotId, locale);

            if (slot == null)
            {
                // Fallback to ID-based resolver
                PlayCutsceneById(slotId, onComplete, onFailed, caption);
                return;
            }

            // If slot uses live VFX, live pose, or UI credits: do not decode video
            if (slot.safeFallback == DemonIdol.Combat.SafeFallbackType.LIVE_SUPER_VFX)
            {
                PlaySuperOverlay(slot.ownerOrCharacter, true, onComplete);
                return;
            }

            if (slot.safeFallback == DemonIdol.Combat.SafeFallbackType.LIVE_POSE ||
                slot.safeFallback == DemonIdol.Combat.SafeFallbackType.UI_CREDITS)
            {
                onComplete?.Invoke();
                return;
            }

            // If slot uses NEUTRAL_IMAGE or video file
            if (VideoPathResolver.Resolve(slot.relativePath, out var clip, out var url, out var def))
            {
                float timeout = def != null ? def.prepareTimeoutSeconds : _defaultPrepareTimeoutSeconds;
                PlayCutsceneInternal(clip, url, timeout, onComplete, onFailed, caption);
            }
            else
            {
                // Fallback to neutral image presentation + caption
                ShowNeutralImageFallback(caption, onComplete);
            }
        }

        public void ShowNeutralImageFallback(string caption, Action onComplete)
        {
            StartCoroutine(NeutralImageFallbackRoutine(caption, onComplete));
        }

        private IEnumerator NeutralImageFallbackRoutine(string caption, Action onComplete)
        {
            EnsureCutsceneUI();
            ClearRenderTexture();

            if (_captionText != null) _captionText.text = caption;
            if (_captionPanel != null) _captionPanel.SetActive(true);
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(true);

            State = CutscenePlaybackState.Playing;
            yield return StartCoroutine(FadeCanvas(0f, 1f, 0.2f));

            float elapsed = 0f;
            while (elapsed < 1.5f && State == CutscenePlaybackState.Playing)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return StartCoroutine(FadeCanvas(1f, 0f, 0.2f));
            if (_captionPanel != null) _captionPanel.SetActive(false);
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(false);
            State = CutscenePlaybackState.Completed;
            onComplete?.Invoke();
        }

        public void PlayCutscene(VideoClip clip, Action onComplete = null, Action onFailed = null, string caption = null)
        {
            PlayCutsceneInternal(clip, null, _defaultPrepareTimeoutSeconds, onComplete, onFailed, caption);
        }

        public void PlayCutsceneFromUrl(string url, Action onComplete = null, Action onFailed = null, string caption = null)
        {
            PlayCutsceneInternal(null, url, _defaultPrepareTimeoutSeconds, onComplete, onFailed, caption);
        }

        public void PlayCutsceneById(string assetId, Action onComplete = null, Action onFailed = null, string caption = null)
        {
            if (VideoPathResolver.Resolve(assetId, out var clip, out var url, out var def))
            {
                float timeout = def != null ? def.prepareTimeoutSeconds : _defaultPrepareTimeoutSeconds;
                PlayCutsceneInternal(clip, url, timeout, onComplete, onFailed, caption);
            }
            else
            {
                Debug.LogWarning($"[CutsceneManager] Asset ID '{assetId}' could not be resolved. Triggering graceful fallback.");
                if (onFailed != null) onFailed.Invoke();
                else onComplete?.Invoke();
            }
        }

        #endregion

        #region Playback State Machine

        private void PlayCutsceneInternal(VideoClip clip, string url, float prepareTimeout, Action onComplete, Action onFailed, string caption)
        {
            EnsureCutsceneUI();

            // Cancel any in-flight playback and generate fresh request token
            CancelActiveCutscene();
            Guid token = Guid.NewGuid();
            _activeRequestToken = token;

            _onCompleteCallback = onComplete;
            _onFailedCallback = onFailed;
            _skipTimer = 0f;

            if (_playbackRoutine != null) StopCoroutine(_playbackRoutine);
            _playbackRoutine = StartCoroutine(PlaybackFlowRoutine(token, clip, url, prepareTimeout, caption));
        }

        private IEnumerator PlaybackFlowRoutine(Guid token, VideoClip clip, string url, float timeoutSeconds, string caption)
        {
            State = CutscenePlaybackState.Preparing;
            ClearRenderTexture();

            if (_videoPlayer == null)
            {
                FinishWithFailure(token, "VideoPlayer component is missing.");
                yield break;
            }

            // Route audio
            if (_audioSource != null)
            {
                _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                _videoPlayer.SetTargetAudioSource(0, _audioSource);
            }

            // Assign Source
            if (clip != null)
            {
                _videoPlayer.source = VideoSource.VideoClip;
                _videoPlayer.clip = clip;
            }
            else if (!string.IsNullOrEmpty(url))
            {
                _videoPlayer.source = VideoSource.Url;
                _videoPlayer.url = url;
            }
            else
            {
                FinishWithFailure(token, "No valid clip or URL provided.");
                yield break;
            }

            // Start Prepare with deterministic timeout
            bool prepareCompleted = false;
            bool prepareError = false;
            string errorMessage = string.Empty;

            VideoPlayer.EventHandler onPrepared = (vp) => { prepareCompleted = true; };
            VideoPlayer.ErrorEventHandler onError = (vp, msg) => { prepareError = true; errorMessage = msg; };

            _videoPlayer.prepareCompleted += onPrepared;
            _videoPlayer.errorReceived += onError;

            _videoPlayer.Prepare();

            float elapsed = 0f;
            while (!prepareCompleted && !prepareError && elapsed < timeoutSeconds)
            {
                if (_activeRequestToken != token)
                {
                    // Stale callback guard: request was superseded
                    _videoPlayer.prepareCompleted -= onPrepared;
                    _videoPlayer.errorReceived -= onError;
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _videoPlayer.prepareCompleted -= onPrepared;
            _videoPlayer.errorReceived -= onError;

            if (_activeRequestToken != token) yield break;

            if (prepareError || !prepareCompleted)
            {
                FinishWithFailure(token, $"Prepare failed or timed out: {errorMessage}");
                yield break;
            }

            // Ready state
            State = CutscenePlaybackState.Ready;
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(true);

            // Duck combat music to prevent dual-soundtrack cacophony (§4.3)
            DuckCombatMusic(true);

            // Display captions if enabled
            if (SettingsPanel.CaptionsEnabled && !string.IsNullOrEmpty(caption))
            {
                if (_captionText != null) _captionText.text = caption;
                if (_captionPanel != null) _captionPanel.SetActive(true);
            }

            // Cinematic Fade In from Black (softens AI video cuts, guarantees 0 post-processing on video pixels)
            State = CutscenePlaybackState.Entering;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
            if (_stillDisplayImage != null) _stillDisplayImage.gameObject.SetActive(false);
            if (_displayImage != null) _displayImage.gameObject.SetActive(true);

            _videoPlayer.Play();
            yield return StartCoroutine(FadeBlackCurtain(1f, 0f, _fadeDuration));

            if (_activeRequestToken != token) yield break;

            // Start Playback
            State = CutscenePlaybackState.Playing;

            // Wait until completion or skip
            while (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                if (_activeRequestToken != token) yield break;
                yield return null;
            }

            // Clean exit sequence
            FinishWithSuccess(token);
        }

        public void OnClickInstantSkip()
        {
            SkipCutscene();
        }

        public void SkipCutscene()
        {
            if (State != CutscenePlaybackState.Playing && State != CutscenePlaybackState.Ready) return;
            State = CutscenePlaybackState.Exiting;
            StartCoroutine(ExitRoutine(_activeRequestToken, CutscenePlaybackState.Completed));
        }

        private void FinishWithSuccess(Guid token)
        {
            if (_activeRequestToken != token) return;
            State = CutscenePlaybackState.Exiting;
            StartCoroutine(ExitRoutine(token, CutscenePlaybackState.Completed));
        }

        private void FinishWithFailure(Guid token, string reason)
        {
            if (_activeRequestToken != token) return;
            Debug.LogWarning($"[CutsceneManager] Playback aborted ({reason}). Graceful fallback invoked.");
            State = CutscenePlaybackState.Failed;
            StartCoroutine(ExitRoutine(token, CutscenePlaybackState.Failed));
        }

        private IEnumerator ExitRoutine(Guid token, CutscenePlaybackState finalState)
        {
            // Cinematic Fade Out to Black (softens transitions back to match/UI)
            yield return StartCoroutine(FadeBlackCurtain(0f, 1f, _fadeDuration));

            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Stop();
            }

            if (_captionPanel != null) _captionPanel.SetActive(false);
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(false);
            if (_skipPrompt != null) _skipPrompt.SetActive(false);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_blackCurtainGroup != null) _blackCurtainGroup.alpha = 0f;

            // Restore Music
            DuckCombatMusic(false);

            ClearRenderTexture();

            if (_activeRequestToken != token) yield break;

            State = finalState;
            _activeRequestToken = Guid.Empty;

            Action complete = _onCompleteCallback;
            Action failed = _onFailedCallback;
            _onCompleteCallback = null;
            _onFailedCallback = null;

            if (finalState == CutscenePlaybackState.Failed)
            {
                failed?.Invoke();
            }
            else
            {
                complete?.Invoke();
            }
        }

        public void PlayStoryStill(Sprite stillSprite, string caption, float displayDuration, Action onComplete)
        {
            StartCoroutine(StoryStillRoutine(stillSprite, caption, displayDuration, onComplete));
        }

        private IEnumerator StoryStillRoutine(Sprite stillSprite, string caption, float displayDuration, Action onComplete)
        {
            EnsureCutsceneUI();
            CancelActiveCutscene();
            Guid token = Guid.NewGuid();
            _activeRequestToken = token;
            State = CutscenePlaybackState.Playing;

            if (_displayImage != null) _displayImage.gameObject.SetActive(false);
            if (_stillDisplayImage != null)
            {
                _stillDisplayImage.sprite = stillSprite;
                _stillDisplayImage.gameObject.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }

            if (SettingsPanel.CaptionsEnabled && !string.IsNullOrEmpty(caption))
            {
                if (_captionText != null) _captionText.text = caption;
                if (_captionPanel != null) _captionPanel.SetActive(true);
            }
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(true);

            // Fade in narrative still from black
            yield return StartCoroutine(FadeBlackCurtain(1f, 0f, _fadeDuration));

            float elapsed = 0f;
            while (elapsed < displayDuration && State == CutscenePlaybackState.Playing)
            {
                if (_activeRequestToken != token) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out still to black
            yield return StartCoroutine(FadeBlackCurtain(0f, 1f, _fadeDuration));

            if (_stillDisplayImage != null) _stillDisplayImage.gameObject.SetActive(false);
            if (_captionPanel != null) _captionPanel.SetActive(false);
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(false);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_blackCurtainGroup != null) _blackCurtainGroup.alpha = 0f;

            State = CutscenePlaybackState.Completed;
            _activeRequestToken = Guid.Empty;
            onComplete?.Invoke();
        }

        private IEnumerator FadeBlackCurtain(float startAlpha, float targetAlpha, float duration)
        {
            if (_blackCurtainGroup == null) yield break;

            _blackCurtainGroup.blocksRaycasts = (targetAlpha > 0.5f);
            float actualDuration = SettingsPanel.ReducedMotion ? SumiStyleTokens.ReducedMotionMaxDuration : duration;

            float elapsed = 0f;
            while (elapsed < actualDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / actualDuration);
                _blackCurtainGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _blackCurtainGroup.alpha = targetAlpha;
        }

        public void CancelActiveCutscene()
        {
            _activeRequestToken = Guid.Empty;
            if (_playbackRoutine != null) StopCoroutine(_playbackRoutine);
            if (_videoPlayer != null && _videoPlayer.isPlaying) _videoPlayer.Stop();
            DuckCombatMusic(false);
            ClearRenderTexture();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_captionPanel != null) _captionPanel.SetActive(false);
            if (_btnInstantSkip != null) _btnInstantSkip.gameObject.SetActive(false);
            State = CutscenePlaybackState.Idle;
        }

        private void DuckCombatMusic(bool duck)
        {
            if (CombatAudioDirector.Instance != null)
            {
                if (duck)
                {
                    CombatAudioDirector.Instance.SetMusicVolume(0f);
                    _wasMusicMutedByCutscene = true;
                }
                else if (_wasMusicMutedByCutscene)
                {
                    CombatAudioDirector.Instance.SetMusicVolume(1f);
                    _wasMusicMutedByCutscene = false;
                }
            }
        }

        private IEnumerator FadeCanvas(float startAlpha, float targetAlpha, float duration)
        {
            if (_canvasGroup == null) yield break;

            _canvasGroup.blocksRaycasts = (targetAlpha > 0.5f);
            bool reducedMotion = SettingsPanel.ReducedMotion;
            float actualDuration = reducedMotion ? SumiStyleTokens.ReducedMotionMaxDuration : duration;

            float elapsed = 0f;
            while (elapsed < actualDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / actualDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        #endregion
    }
}
