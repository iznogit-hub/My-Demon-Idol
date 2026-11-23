using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemonIdol.UI;
using DemonIdol.Combat;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// On-screen A B C D, virtual D-pad, dynamic floating thumbstick, and hold-to-pause controls.
    /// Fully calibrated for mobile phone landscape viewports with safe area margins and multi-touch haptics.
    /// Writes the existing BBInputSampler virtual masks and stick vectors.
    /// </summary>
    public class BBTouchLayout : MonoBehaviour
    {
        public BBInputSampler target;

        private bool _isHoldingPause = false;
        private float _pauseHoldTimer = 0f;
        private const float PAUSE_HOLD_DURATION = 0.8f;
        private Text _pauseLabelText;

        // D-pad input tracking
        private bool _padUp = false;
        private bool _padDown = false;
        private bool _padLeft = false;
        private bool _padRight = false;

        public static bool ShouldShowTouchControls(bool isMobilePlatform, bool simulatorMobile, bool hasTouchHardware)
        {
            return isMobilePlatform || simulatorMobile || hasTouchHardware;
        }

        public static bool ShouldShowTouchControls()
        {
            bool simulatorMobile = false;
#if UNITY_EDITOR
            simulatorMobile = UnityEngine.Device.Application.isMobilePlatform
                || UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld;
#endif
            bool touchHardware = SystemInfo.deviceType == DeviceType.Handheld;
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null) touchHardware = true;
#endif
            return ShouldShowTouchControls(Application.isMobilePlatform, simulatorMobile, touchHardware);
        }

        private void OnEnable()
        {
            SubscribeHaptics();
        }

        private void OnDisable()
        {
            UnsubscribeHaptics();
        }

        private void SubscribeHaptics()
        {
            if (BBMatchSim.Instance != null)
            {
                BBMatchSim.Instance.OnHitConfirmed -= HandleHitHaptic;
                BBMatchSim.Instance.OnHitConfirmed += HandleHitHaptic;
                BBMatchSim.Instance.OnBurstExecuted -= HandleBurstHaptic;
                BBMatchSim.Instance.OnBurstExecuted += HandleBurstHaptic;
                BBMatchSim.Instance.OnRapidCanceled -= HandleRapidHaptic;
                BBMatchSim.Instance.OnRapidCanceled += HandleRapidHaptic;
            }
        }

        private void UnsubscribeHaptics()
        {
            if (BBMatchSim.Instance != null)
            {
                BBMatchSim.Instance.OnHitConfirmed -= HandleHitHaptic;
                BBMatchSim.Instance.OnBurstExecuted -= HandleBurstHaptic;
                BBMatchSim.Instance.OnRapidCanceled -= HandleRapidHaptic;
            }
        }

        private void HandleHitHaptic(int attackerIdx, int defenderIdx, float damage, Vector3 hitPos, bool isCounter, bool isHeavy)
        {
            if (isCounter) MobileHaptics.Trigger(HapticImpact.CounterHit);
            else if (isHeavy) MobileHaptics.Trigger(HapticImpact.Heavy);
            else MobileHaptics.Trigger(HapticImpact.Medium);
        }

        private void HandleBurstHaptic(int playerIndex, BBBurstType burstType, Vector3 pos)
        {
            MobileHaptics.Trigger(HapticImpact.CounterHit);
        }

        private void HandleRapidHaptic(int playerIndex, Vector3 pos)
        {
            MobileHaptics.Trigger(HapticImpact.Parry);
        }

        public void Bind(BBInputSampler sampler)
        {
            target = sampler;
        }

        public void SetButton(BBButton button, bool held)
        {
            if (target == null) return;
            byte bit = (byte)button;
            if (held)
            {
                target.virtualHeldMask |= bit;
                target.virtualPressedMask |= bit;
                MobileHaptics.Trigger(HapticImpact.Light);
            }
            else
            {
                target.virtualHeldMask = (byte)(target.virtualHeldMask & ~bit);
                target.virtualReleasedMask |= bit;
            }
        }

        public void SetDirection(int x, int y, bool held)
        {
            if (x < 0) _padLeft = held;
            if (x > 0) _padRight = held;
            if (y < 0) _padDown = held;
            if (y > 0) _padUp = held;

            float stickX = (_padRight ? 1f : 0f) - (_padLeft ? 1f : 0f);
            float stickY = (_padUp ? 1f : 0f) - (_padDown ? 1f : 0f);

            SetStick(new Vector2(stickX, stickY));
            if (held) MobileHaptics.Trigger(HapticImpact.Light);
        }

        public void SetStick(Vector2 stick)
        {
            if (target == null) return;
            target.virtualStickInput = stick;
        }

        public void SetPauseHeld(bool held)
        {
            _isHoldingPause = held;
            if (!held)
            {
                _pauseHoldTimer = 0f;
                if (_pauseLabelText != null) _pauseLabelText.text = "PAUSE";
            }
            else
            {
                MobileHaptics.Trigger(HapticImpact.Light);
            }
        }

        private void Update()
        {
            if (BBMatchSim.Instance != null && !_isHoldingPause)
            {
                // Ensure active haptics connection if sim spawned after layout
                SubscribeHaptics();
            }

            if (_isHoldingPause)
            {
                _pauseHoldTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(_pauseHoldTimer / PAUSE_HOLD_DURATION);
                if (_pauseLabelText != null)
                {
                    _pauseLabelText.text = $"PAUSE {(int)(progress * 100)}%";
                }

                if (_pauseHoldTimer >= PAUSE_HOLD_DURATION)
                {
                    _isHoldingPause = false;
                    _pauseHoldTimer = 0f;
                    if (_pauseLabelText != null) _pauseLabelText.text = "PAUSED";
                    TriggerPause();
                }
            }
        }

        private void TriggerPause()
        {
            MobileHaptics.Trigger(HapticImpact.Medium);
            if (BattleHUDController.Instance != null)
            {
                BattleHUDController.Instance.ToggleInGamePause();
            }
            else if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.ToggleLocalPause(true);
            }
            else
            {
                Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
            }
        }

        public static BBTouchLayout Create(Transform parent, BBInputSampler sampler)
        {
            var root = new GameObject("BBTouchLayout", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var layout = root.AddComponent<BBTouchLayout>();
            layout.Bind(sampler);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Safe Area & Screen Margins
            Rect safe = Screen.safeArea;
            float safeLeft = Mathf.Max(24f, safe.xMin);
            float safeRight = Mathf.Max(24f, Screen.width - safe.xMax);
            float safeBottom = Mathf.Max(24f, safe.yMin);

            // 1. Dynamic Floating Thumbstick Zone (Left half of screen)
            CreateFloatingJoystickZone(layout, root.transform);

            // 2. Virtual D-Pad (Left side visual anchor)
            Vector2 dpadOrigin = new Vector2(safeLeft + 80f, safeBottom + 80f);
            CreateDPad(layout, root.transform, dpadOrigin);

            // 3. Action Buttons A, B, C, D (Right side, minimum 76x76 touch target)
            Vector2 rightOrigin = new Vector2(-safeRight - 40f, safeBottom + 40f);
            CreateButton(layout, root.transform, "A", BBButton.A, new Vector2(rightOrigin.x, rightOrigin.y), new Color(0.85f, 0.20f, 0.25f, 0.85f));
            CreateButton(layout, root.transform, "B", BBButton.B, new Vector2(rightOrigin.x, rightOrigin.y + 90f), new Color(0.20f, 0.50f, 0.90f, 0.85f));
            CreateButton(layout, root.transform, "C", BBButton.C, new Vector2(rightOrigin.x - 90f, rightOrigin.y), new Color(0.95f, 0.75f, 0.20f, 0.85f));
            CreateButton(layout, root.transform, "D", BBButton.D, new Vector2(rightOrigin.x - 90f, rightOrigin.y + 90f), new Color(0.65f, 0.25f, 0.85f, 0.85f));

            // 4. Hold-to-Pause Button (Top right, safe under status bar)
            CreateHoldPauseButton(layout, root.transform, new Vector2(-safeRight - 20f, -40f));

            return layout;
        }

        static void CreateFloatingJoystickZone(BBTouchLayout layout, Transform parent)
        {
            var zoneGo = new GameObject("FloatingJoystickZone", typeof(RectTransform), typeof(Image));
            zoneGo.transform.SetParent(parent, false);
            var rt = zoneGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.45f, 0.8f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = zoneGo.GetComponent<Image>();
            img.color = Color.clear; // Invisible raycast target for gestures

            var zone = zoneGo.AddComponent<BBFloatingJoystickZone>();
            zone.layout = layout;
        }

        static void CreateDPad(BBTouchLayout layout, Transform parent, Vector2 origin)
        {
            var dpadGo = new GameObject("VirtualDPad", typeof(RectTransform));
            dpadGo.transform.SetParent(parent, false);
            var rt = dpadGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = origin;
            rt.sizeDelta = new Vector2(180f, 180f);

            CreateDPadDirection(layout, dpadGo.transform, "UP", 0, 1, new Vector2(0f, 55f), "▲");
            CreateDPadDirection(layout, dpadGo.transform, "DOWN", 0, -1, new Vector2(0f, -55f), "▼");
            CreateDPadDirection(layout, dpadGo.transform, "LEFT", -1, 0, new Vector2(-55f, 0f), "◀");
            CreateDPadDirection(layout, dpadGo.transform, "RIGHT", 1, 0, new Vector2(55f, 0f), "▶");
        }

        static void CreateDPadDirection(BBTouchLayout layout, Transform parent, string name, int x, int y, Vector2 pos, string arrow)
        {
            var go = new GameObject("Pad_" + name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(56f, 56f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.20f, 0.80f);

            var textGo = new GameObject("Arrow", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = arrow;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var trigger = go.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerDown, () => layout.SetDirection(x, y, true));
            Add(trigger, EventTriggerType.PointerUp, () => layout.SetDirection(x, y, false));
        }

        static void CreateButton(BBTouchLayout layout, Transform parent, string label, BBButton button, Vector2 corner, Color btnColor)
        {
            var go = new GameObject("Touch_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = corner;
            rt.sizeDelta = new Vector2(76f, 76f);
            var image = go.GetComponent<Image>();
            image.color = btnColor;
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 28;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var trigger = go.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerDown, () => layout.SetButton(button, true));
            Add(trigger, EventTriggerType.PointerUp, () => layout.SetButton(button, false));
        }

        static void CreateHoldPauseButton(BBTouchLayout layout, Transform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("Touch_HoldPause", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(100f, 44f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.24f, 0.85f);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = "PAUSE";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            layout._pauseLabelText = text;

            var trigger = go.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerDown, () => layout.SetPauseHeld(true));
            Add(trigger, EventTriggerType.PointerUp, () => layout.SetPauseHeld(false));
        }

        static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => call());
            trigger.triggers.Add(entry);
        }
    }

    /// <summary>
    /// Dynamic capacitive thumbstick gesture responder for mobile touchscreens.
    /// Re-centers touch origin dynamically and maps continuous analog vectors into BBInputSampler.
    /// </summary>
    public class BBFloatingJoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public BBTouchLayout layout;
        public float maxRadius = 75f;
        public float deadzone = 12f;

        private Vector2 _origin;
        private bool _isDragging = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _origin = eventData.position;
            UpdateStick(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            UpdateStick(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            if (layout != null) layout.SetStick(Vector2.zero);
        }

        private void UpdateStick(Vector2 currentPos)
        {
            Vector2 delta = currentPos - _origin;
            float dist = delta.magnitude;

            if (dist < deadzone)
            {
                if (layout != null) layout.SetStick(Vector2.zero);
                return;
            }

            Vector2 dir = delta / dist;
            float clampedDist = Mathf.Min(dist, maxRadius);
            float normalized = (clampedDist - deadzone) / (maxRadius - deadzone);

            if (layout != null)
            {
                layout.SetStick(dir * normalized);
            }
        }
    }
}
