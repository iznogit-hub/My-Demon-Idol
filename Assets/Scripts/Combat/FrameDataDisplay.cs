using System;
using UnityEngine;
using UnityEngine.UI;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Real-time fighting game Frame Data & Advantage Display.
    /// Shows Active Move Name, Startup, Active, Recovery frames,
    /// and Frame Advantage on Hit / Block (+3, -6, etc.).
    /// </summary>
    public class FrameDataDisplay : MonoBehaviour
    {
        public static FrameDataDisplay Instance { get; private set; }

        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;

        private GameObject _canvasGo;
        private Text _p1FrameText;
        private Text _p2FrameText;
        private Text _advantageText;

        public bool isVisible = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void Start()
        {
            FindFightersIfMissing();
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
        }

        private void OnDestroy()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        public void BindFighters(FighterController p1, FighterController p2)
        {
            _fighter1 = p1;
            _fighter2 = p2;
        }

        private void FindFightersIfMissing()
        {
            if (_fighter1 == null || _fighter2 == null)
            {
                var fighters = FindObjectsByType<FighterController>();
                if (fighters != null && fighters.Length >= 2)
                {
                    Array.Sort(fighters, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                    if (_fighter1 == null) _fighter1 = fighters[0];
                    if (_fighter2 == null) _fighter2 = fighters[1];
                }
            }
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("FrameDataDisplay_Canvas");
            _canvasGo.transform.SetParent(transform);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95; // Under popups, above gameplay

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Container Panel
            var panelGo = new GameObject("FramePanel");
            panelGo.transform.SetParent(_canvasGo.transform, false);
            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 30);
            rect.sizeDelta = new Vector2(720, 90);

            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            // P1 Frame Text
            var p1Go = new GameObject("P1_Text");
            p1Go.transform.SetParent(panelGo.transform, false);
            var p1Rect = p1Go.AddComponent<RectTransform>();
            p1Rect.anchorMin = new Vector2(0f, 0f);
            p1Rect.anchorMax = new Vector2(0.45f, 1f);
            p1Rect.offsetMin = new Vector2(10, 5);
            p1Rect.offsetMax = new Vector2(-5, -5);
            _p1FrameText = p1Go.AddComponent<Text>();
            _p1FrameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _p1FrameText.fontSize = 18;
            _p1FrameText.color = new Color(0.2f, 0.9f, 1f);
            _p1FrameText.alignment = TextAnchor.MiddleLeft;
            _p1FrameText.text = "P1: NEUTRAL";

            // Advantage Text (Center)
            var advGo = new GameObject("Advantage_Text");
            advGo.transform.SetParent(panelGo.transform, false);
            var advRect = advGo.AddComponent<RectTransform>();
            advRect.anchorMin = new Vector2(0.45f, 0f);
            advRect.anchorMax = new Vector2(0.55f, 1f);
            advRect.offsetMin = Vector2.zero;
            advRect.offsetMax = Vector2.zero;
            _advantageText = advGo.AddComponent<Text>();
            _advantageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _advantageText.fontSize = 20;
            _advantageText.fontStyle = FontStyle.Bold;
            _advantageText.color = Color.yellow;
            _advantageText.alignment = TextAnchor.MiddleCenter;
            _advantageText.text = "±0";

            // P2 Frame Text
            var p2Go = new GameObject("P2_Text");
            p2Go.transform.SetParent(panelGo.transform, false);
            var p2Rect = p2Go.AddComponent<RectTransform>();
            p2Rect.anchorMin = new Vector2(0.55f, 0f);
            p2Rect.anchorMax = new Vector2(1f, 1f);
            p2Rect.offsetMin = new Vector2(5, 5);
            p2Rect.offsetMax = new Vector2(-10, -5);
            _p2FrameText = p2Go.AddComponent<Text>();
            _p2FrameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _p2FrameText.fontSize = 18;
            _p2FrameText.color = new Color(1f, 0.4f, 0.4f);
            _p2FrameText.alignment = TextAnchor.MiddleRight;
            _p2FrameText.text = "P2: NEUTRAL";

            _canvasGo.SetActive(isVisible);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (_canvasGo != null) _canvasGo.SetActive(visible);
        }

        public void ToggleVisibility()
        {
            SetVisible(!isVisible);
        }

        private void HandleCombatTick(int tick)
        {
            if (!isVisible || _canvasGo == null || !_canvasGo.activeSelf) return;

            FindFightersIfMissing();
            UpdateFighterFrameData(_fighter1, _p1FrameText, "P1");
            UpdateFighterFrameData(_fighter2, _p2FrameText, "P2");

            UpdateAdvantageMetric();
        }

        private void UpdateFighterFrameData(FighterController fc, Text label, string prefix)
        {
            if (fc == null || label == null) return;

            string stateName = fc.State.ToString().ToUpperInvariant();
            if (fc.CurrentAttack != null)
            {
                var atk = fc.CurrentAttack;
                label.text = $"{prefix}: {atk.attackName}\n" +
                             $"[i{atk.startupFrames}] Act:{atk.activeFrames} Rec:{atk.recoveryFrames} ({stateName})";
            }
            else
            {
                label.text = $"{prefix}: {stateName}\n" +
                             $"Ticks Left: {fc.RemainingTicks}";
            }
        }

        private void UpdateAdvantageMetric()
        {
            if (_fighter1 == null || _fighter2 == null || _advantageText == null) return;

            // Frame Advantage calculation:
            // Advantage = P2 Remaining Stun - P1 Remaining Recovery
            int adv = _fighter2.RemainingTicks - _fighter1.RemainingTicks;
            if (_fighter1.State == FighterState.AttackRecovery && _fighter2.State == FighterState.Blockstun)
            {
                adv = _fighter2.RemainingTicks - _fighter1.RemainingTicks;
            }
            else if (_fighter2.State == FighterState.AttackRecovery && _fighter1.State == FighterState.Blockstun)
            {
                adv = -(_fighter1.RemainingTicks - _fighter2.RemainingTicks);
            }

            if (adv > 0)
            {
                _advantageText.text = $"+{adv}";
                _advantageText.color = new Color(0.2f, 1f, 0.3f); // Green / Plus
            }
            else if (adv < 0)
            {
                _advantageText.text = $"{adv}";
                _advantageText.color = new Color(1f, 0.3f, 0.3f); // Red / Minus
            }
            else
            {
                _advantageText.text = "±0";
                _advantageText.color = Color.yellow;
            }
        }
    }
}
