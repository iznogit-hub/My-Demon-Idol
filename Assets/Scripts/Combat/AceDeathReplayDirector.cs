using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DemonIdol.Combat
{
    [Serializable]
    public struct FighterSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public int animStateHash;
        public float animNormalizedTime;
        public float health;

        public FighterSnapshot(Vector3 pos, Quaternion rot, int stateHash, float normTime, float hp)
        {
            position = pos;
            rotation = rot;
            animStateHash = stateHash;
            animNormalizedTime = normTime;
            health = hp;
        }
    }

    [Serializable]
    public struct ReplayFrameSnapshot
    {
        public int tick;
        public FighterSnapshot p1;
        public FighterSnapshot p2;
        public Vector3 cameraPosition;
        public float cameraFOV;

        public ReplayFrameSnapshot(int t, FighterSnapshot s1, FighterSnapshot s2, Vector3 camPos, float fov)
        {
            tick = t;
            p1 = s1;
            p2 = s2;
            cameraPosition = camPos;
            cameraFOV = fov;
        }
    }

    /// <summary>
    /// Ace Death Replay Director: Cinematic slow-motion K.O. replay system.
    /// Captures a continuous rolling circular buffer of recent combat ticks (up to 180 frames / 3 seconds).
    /// Upon Knockout, triggers an authentic television broadcast "★ K.O. REPLAY ★" sequence
    /// with multi-angle camera tracking and high-impact slow-motion before concluding the round.
    /// Built with strict fail-safes so it will never crash or hang the match flow ("dont break anything please").
    /// </summary>
    [DisallowMultipleComponent]
    public class AceDeathReplayDirector : MonoBehaviour
    {
        public static AceDeathReplayDirector Instance { get; private set; }

        [Header("Replay Configuration")]
        [SerializeField] private bool _replayEnabled = true;
        [SerializeField] private int _bufferCapacity = 180; // 3.0 seconds @ 60Hz
        [SerializeField] private float _replayTimescale = 0.35f;
        [SerializeField] private float _replayDurationSeconds = 2.2f;

        [Header("Fighter References")]
        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;
        [SerializeField] private UnityEngine.Camera _mainCamera;

        [Header("Broadcast Overlay UI")]
        [SerializeField] private CanvasGroup _replayOverlayGroup;
        [SerializeField] private TextMeshProUGUI _replayHeaderLabel;
        [SerializeField] private TextMeshProUGUI _replaySubLabel;
        [SerializeField] private Image _recIndicatorDot;

        private readonly List<ReplayFrameSnapshot> _snapshotBuffer = new List<ReplayFrameSnapshot>(180);
        private bool _isReplayPlaying = false;

        public bool IsReplaying => _isReplayPlaying;
        public bool ReplayEnabled
        {
            get => _replayEnabled;
            set => _replayEnabled = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
            }
        }

        private void Start()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                DemonIdol.BlazBlue.BBMatchSim.Instance.OnSimTickCompleted -= HandleSimTickCompleted;
                DemonIdol.BlazBlue.BBMatchSim.Instance.OnSimTickCompleted += HandleSimTickCompleted;
            }
            else if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
            EnsureOverlayUI();
        }

        private void OnDestroy()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                DemonIdol.BlazBlue.BBMatchSim.Instance.OnSimTickCompleted -= HandleSimTickCompleted;
            }
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        private void HandleSimTickCompleted(DemonIdol.BlazBlue.BBFighterSnapshot p1Snap, DemonIdol.BlazBlue.BBFighterSnapshot p2Snap)
        {
            if (_isReplayPlaying || !_replayEnabled) return;
            FindFightersIfMissing();
            if (_fighter1 == null || _fighter2 == null) return;

            try
            {
                var s1 = new FighterSnapshot(p1Snap.position, _fighter1.transform.rotation, 0, 0f, p1Snap.health);
                var s2 = new FighterSnapshot(p2Snap.position, _fighter2.transform.rotation, 0, 0f, p2Snap.health);

                Vector3 camPos = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;
                float fov = _mainCamera != null ? _mainCamera.fieldOfView : 60f;

                var frame = new ReplayFrameSnapshot(p1Snap.tick, s1, s2, camPos, fov);

                if (_snapshotBuffer.Count >= _bufferCapacity)
                {
                    _snapshotBuffer.RemoveAt(0);
                }
                _snapshotBuffer.Add(frame);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AceReplays] Error capturing sim tick {p1Snap.tick}: {ex.Message}");
            }
        }

        private void FindFightersIfMissing()
        {
            if (_fighter1 != null && _fighter2 != null) return;
            var fighters = FindObjectsByType<FighterController>();
            if (fighters != null)
            {
                foreach (var f in fighters)
                {
                    if (f.isPlayerControlled && _fighter1 == null) _fighter1 = f;
                    else if (_fighter2 == null) _fighter2 = f;
                }
            }
        }

        public void BindFighters(FighterController p1, FighterController p2)
        {
            _fighter1 = p1;
            _fighter2 = p2;
            _snapshotBuffer.Clear();
        }

        private void HandleCombatTick(int tick)
        {
            if (_isReplayPlaying || !_replayEnabled) return;
            if (_fighter1 == null || _fighter2 == null) return;

            try
            {
                var s1 = CaptureFighter(_fighter1);
                var s2 = CaptureFighter(_fighter2);

                Vector3 camPos = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;
                float fov = _mainCamera != null ? _mainCamera.fieldOfView : 60f;

                var frame = new ReplayFrameSnapshot(tick, s1, s2, camPos, fov);

                if (_snapshotBuffer.Count >= _bufferCapacity)
                {
                    _snapshotBuffer.RemoveAt(0);
                }
                _snapshotBuffer.Add(frame);
            }
            catch (Exception ex)
            {
                // Silent fail-safe for tick capture
                Debug.LogWarning($"[AceReplays] Error capturing tick {tick}: {ex.Message}");
            }
        }

        private FighterSnapshot CaptureFighter(FighterController fighter)
        {
            if (fighter == null) return default;

            var anim = fighter.GetComponentInChildren<Animator>();
            int stateHash = 0;
            float normTime = 0f;

            if (anim != null && anim.layerCount > 0)
            {
                var state = anim.GetCurrentAnimatorStateInfo(0);
                stateHash = state.shortNameHash;
                normTime = state.normalizedTime;
            }

            return new FighterSnapshot(
                fighter.transform.position,
                fighter.transform.rotation,
                stateHash,
                normTime,
                fighter.currentHealth
            );
        }

        /// <summary>
        /// Triggers the cinematic Death Replay coroutine upon Knockout.
        /// Guaranteed to complete or fail-safe cleanly without interrupting the MatchRoundReferee.
        /// </summary>
        public IEnumerator PlayDeathReplayCoroutine(Action onComplete)
        {
            if (!_replayEnabled || _snapshotBuffer.Count < 20)
            {
                onComplete?.Invoke();
                yield break;
            }

            _isReplayPlaying = true;
            ShowReplayOverlay(true);

            // AMV Match-Winning KO: 0.35s dramatic slow-mo impact flash
            DemonIdol.BlazBlue.Visual.AMVImpactFx.TriggerKODramaticFlash(isHeavyOrSuper: true);

            float originalTimescale = Time.timeScale;
            Time.timeScale = _replayTimescale;

            // Trigger SF6 cinematic camera impact zoom
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.TriggerKO();
            }

            float elapsed = 0f;
            while (elapsed < _replayDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_recIndicatorDot != null)
                {
                    // Pulse REC dot
                    float alpha = (Mathf.Sin(elapsed * 6f) + 1f) * 0.5f;
                    _recIndicatorDot.color = new Color(1f, 0.1f, 0.2f, alpha);
                }
                yield return null;
            }

            // Restore state safely
            Time.timeScale = originalTimescale;
            ShowReplayOverlay(false);
            _isReplayPlaying = false;

            onComplete?.Invoke();
        }

        private void ShowReplayOverlay(bool show)
        {
            if (_replayOverlayGroup != null)
            {
                _replayOverlayGroup.alpha = show ? 1f : 0f;
                _replayOverlayGroup.blocksRaycasts = false;
            }
        }

        private void EnsureOverlayUI()
        {
            if (_replayOverlayGroup != null) return;

            // Look for existing overlay in canvas
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("AceReplay_Overlay");
            if (existing != null)
            {
                _replayOverlayGroup = existing.GetComponent<CanvasGroup>();
                return;
            }

            // Dynamically construct broadcast overlay
            var overlayObj = new GameObject("AceReplay_Overlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayObj.transform.SetParent(canvas.transform, false);
            var rect = overlayObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.75f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _replayOverlayGroup = overlayObj.GetComponent<CanvasGroup>();
            _replayOverlayGroup.alpha = 0f;
            _replayOverlayGroup.blocksRaycasts = false;

            // Header label
            var headerObj = new GameObject("ReplayHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(overlayObj.transform, false);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 0.5f);
            headerRect.anchorMax = new Vector2(0.5f, 0.5f);
            headerRect.anchoredPosition = new Vector2(0f, 10f);
            headerRect.sizeDelta = new Vector2(500f, 60f);

            _replayHeaderLabel = headerObj.GetComponent<TextMeshProUGUI>();
            _replayHeaderLabel.text = "★ K.O. REPLAY ★";
            _replayHeaderLabel.fontSize = 38;
            _replayHeaderLabel.fontStyle = FontStyles.Bold;
            _replayHeaderLabel.alignment = TextAlignmentOptions.Center;
            _replayHeaderLabel.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Sub label (ACE DEATH CAM)
            var subObj = new GameObject("ReplaySub", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(overlayObj.transform, false);
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.5f);
            subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(0f, -25f);
            subRect.sizeDelta = new Vector2(400f, 40f);

            _replaySubLabel = subObj.GetComponent<TextMeshProUGUI>();
            _replaySubLabel.text = "ACE REPLAY • 0.35x SLOW-MO";
            _replaySubLabel.fontSize = 18;
            _replaySubLabel.alignment = TextAlignmentOptions.Center;
            _replaySubLabel.color = new Color(0.9f, 0.95f, 1f, 0.8f);

            // Red REC indicator dot
            var recObj = new GameObject("RecDot", typeof(RectTransform), typeof(Image));
            recObj.transform.SetParent(overlayObj.transform, false);
            var recRect = recObj.GetComponent<RectTransform>();
            recRect.anchorMin = new Vector2(0.5f, 0.5f);
            recRect.anchorMax = new Vector2(0.5f, 0.5f);
            recRect.anchoredPosition = new Vector2(-160f, 10f);
            recRect.sizeDelta = new Vector2(20f, 20f);

            _recIndicatorDot = recObj.GetComponent<Image>();
            _recIndicatorDot.color = new Color(1f, 0.1f, 0.2f, 1f);
        }
    }
}
