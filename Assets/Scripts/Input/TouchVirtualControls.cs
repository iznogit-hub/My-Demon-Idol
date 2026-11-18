using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DemonIdol.Combat;
using DemonIdol.UI;

namespace DemonIdol.Input
{
    /// <summary>
    /// Master On-Screen Virtual Touch Combat Controls for Mobile and WebGL.
    /// Provides low-latency, frame-accurate floating D-Pad Joystick (Left)
    /// and dedicated Gatling action buttons (Right):
    /// [L] Light, [M] Medium, [H] Heavy, [D] Drive Skill, [S] Domain Super,
    /// [P] Parry, [B] Backdash, [Burst], [Barrier], [SS-Up], [SS-Down].
    /// Supports dynamic opacity, scaling, and left-handed layout flipping.
    /// </summary>
    public class TouchVirtualControls : MonoBehaviour
    {
        public static TouchVirtualControls Instance { get; private set; }

        [Header("Target Player Input")]
        [SerializeField] private MobileFighterInput _targetInput;

        // Button held states for frame-accurate chord & hold support
        private bool _isLightHeld = false;
        private bool _isMediumHeld = false;
        private bool _isHeavyHeld = false;
        private bool _isSkillHeld = false;
        private bool _isDriveHeld = false;
        private bool _isThrowHeld = false;
        private bool _isSuperHeld = false;
        private bool _isBurstHeld = false;
        private bool _isBarrierHeld = false;
        private bool _isParryHeld = false;
        private bool _isBackdashHeld = false;
        private bool _isSidestepUpHeld = false;
        private bool _isSidestepDownHeld = false;

        [Header("Virtual Joystick Elements (Left)")]
        [SerializeField] private RectTransform _joystickBackground;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private float _joystickRadius = 80f;

        [Header("Action Buttons (Right)")]
        [SerializeField] private Button _btnLight;
        [SerializeField] private Button _btnMedium;
        [SerializeField] private Button _btnHeavy;
        [SerializeField] private Button _btnDrive;
        [SerializeField] private Button _btnThrow;
        [SerializeField] private Button _btnSuper;
        [SerializeField] private Button _btnParry;
        [SerializeField] private Button _btnBackdash;
        [SerializeField] private Button _btnBurst;
        [SerializeField] private Button _btnBarrier;
        [SerializeField] private Button _btnSidestepUp;
        [SerializeField] private Button _btnSidestepDown;

        private Vector2 _joystickInput = Vector2.zero;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        private bool _defaultsCached = false;
        private Vector2 _defaultJoystickAnchorMin;
        private Vector2 _defaultJoystickAnchorMax;
        private Vector2 _defaultJoystickPos;
        private Vector3 _defaultJoystickScale = Vector3.one;

        private class ControlTransformDefault
        {
            public RectTransform rect;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 anchoredPosition;
            public Vector3 localScale;
        }
        private readonly List<ControlTransformDefault> _buttonDefaults = new List<ControlTransformDefault>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _canvas = GetComponentInParent<Canvas>();

            AutoDiscoverButtonsIfMissing();
            WireActionButtons();
        }

        private void Start()
        {
            FindTargetInputIfMissing();
            ApplySettings(
                SettingsPanel.TouchControlsOpacity,
                SettingsPanel.TouchControlsScale,
                SettingsPanel.LeftHandedMode
            );
        }

        public static void RefreshFromSettings()
        {
            if (Instance != null)
            {
                Instance.ApplySettings(
                    SettingsPanel.TouchControlsOpacity,
                    SettingsPanel.TouchControlsScale,
                    SettingsPanel.LeftHandedMode
                );
            }
        }

        public void BindFighter(FighterController fighter)
        {
            if (fighter != null)
            {
                _targetInput = fighter.GetComponent<MobileFighterInput>();
                if (_targetInput == null) _targetInput = fighter.gameObject.AddComponent<MobileFighterInput>();
            }
        }

        private void FindTargetInputIfMissing()
        {
            if (_targetInput == null)
            {
                var fighters = FindObjectsByType<FighterController>();
                foreach (var f in fighters)
                {
                    if (f.isPlayerControlled)
                    {
                        BindFighter(f);
                        break;
                    }
                }
            }
        }

        public void CacheControlDefaults()
        {
            if (_defaultsCached) return;

            if (_joystickBackground != null)
            {
                _defaultJoystickAnchorMin = _joystickBackground.anchorMin;
                _defaultJoystickAnchorMax = _joystickBackground.anchorMax;
                _defaultJoystickPos = _joystickBackground.anchoredPosition;
                _defaultJoystickScale = _joystickBackground.localScale;
            }

            Button[] buttons = new Button[] { 
                _btnLight, _btnMedium, _btnHeavy, _btnDrive, _btnThrow, _btnSuper, 
                _btnParry, _btnBackdash, _btnBurst, _btnBarrier, 
                _btnSidestepUp, _btnSidestepDown 
            };

            foreach (var b in buttons)
            {
                if (b != null)
                {
                    RectTransform rt = b.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        _buttonDefaults.Add(new ControlTransformDefault
                        {
                            rect = rt,
                            anchorMin = rt.anchorMin,
                            anchorMax = rt.anchorMax,
                            anchoredPosition = rt.anchoredPosition,
                            localScale = rt.localScale
                        });
                    }
                }
            }

            _defaultsCached = true;
        }

        public void ApplySettings(float opacity, float scale, bool leftHanded)
        {
            CacheControlDefaults();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = Mathf.Clamp(opacity, 0.2f, 1.0f);

            float clampedScale = Mathf.Clamp(scale, 0.65f, 1.5f);

            // Joystick layout & scaling
            if (_joystickBackground != null)
            {
                _joystickBackground.localScale = _defaultJoystickScale * clampedScale;
                if (leftHanded)
                {
                    // Mirror joystick to bottom right
                    _joystickBackground.anchorMin = new Vector2(1f - _defaultJoystickAnchorMax.x, _defaultJoystickAnchorMin.y);
                    _joystickBackground.anchorMax = new Vector2(1f - _defaultJoystickAnchorMin.x, _defaultJoystickAnchorMax.y);
                    _joystickBackground.anchoredPosition = new Vector2(-Mathf.Abs(_defaultJoystickPos.x), _defaultJoystickPos.y);
                }
                else
                {
                    _joystickBackground.anchorMin = _defaultJoystickAnchorMin;
                    _joystickBackground.anchorMax = _defaultJoystickAnchorMax;
                    _joystickBackground.anchoredPosition = _defaultJoystickPos;
                }
            }

            // Action buttons layout & scaling
            foreach (var item in _buttonDefaults)
            {
                if (item.rect != null)
                {
                    item.rect.localScale = item.localScale * clampedScale;
                    if (leftHanded)
                    {
                        // Mirror buttons to bottom left
                        item.rect.anchorMin = new Vector2(1f - item.anchorMax.x, item.anchorMin.y);
                        item.rect.anchorMax = new Vector2(1f - item.anchorMin.x, item.anchorMin.y);
                        item.rect.anchoredPosition = new Vector2(Mathf.Abs(item.anchoredPosition.x), item.anchoredPosition.y);
                    }
                    else
                    {
                        item.rect.anchorMin = item.anchorMin;
                        item.rect.anchorMax = item.anchorMax;
                        item.rect.anchoredPosition = item.anchoredPosition;
                    }
                }
            }
        }

        private void WireActionButtons()
        {
            if (_btnLight != null)
                WireButtonUpDown(_btnLight.gameObject, isDown => { _isLightHeld = isDown; if (isDown) _targetInput?.OnLightAttackPressed(); });
            if (_btnMedium != null)
                WireButtonUpDown(_btnMedium.gameObject, isDown => { _isMediumHeld = isDown; if (isDown) _targetInput?.OnMediumAttackPressed(); });
            if (_btnHeavy != null)
                WireButtonUpDown(_btnHeavy.gameObject, isDown => { _isHeavyHeld = isDown; if (isDown) _targetInput?.OnHeavyAttackPressed(); });
            if (_btnDrive != null)
                WireButtonUpDown(_btnDrive.gameObject, isDown => { _isDriveHeld = isDown; if (isDown) _targetInput?.OnDriveSkillPressed(); });
            if (_btnThrow != null)
                WireButtonUpDown(_btnThrow.gameObject, isDown => { _isThrowHeld = isDown; if (isDown) _targetInput?.OnThrowPressed(); });
            if (_btnSuper != null)
                WireButtonUpDown(_btnSuper.gameObject, isDown => { _isSuperHeld = isDown; if (isDown) _targetInput?.OnDomainSuperPressed(); });
            if (_btnParry != null)
                WireButtonUpDown(_btnParry.gameObject, isDown => { _isParryHeld = isDown; if (isDown) _targetInput?.OnParryPressed(); });
            if (_btnBackdash != null)
                WireButtonUpDown(_btnBackdash.gameObject, isDown => { _isBackdashHeld = isDown; if (isDown) _targetInput?.OnBackdashPressed(); });
            if (_btnBurst != null)
                WireButtonUpDown(_btnBurst.gameObject, isDown => { _isBurstHeld = isDown; if (isDown) _targetInput?.OnBurstPressed(); });
            if (_btnBarrier != null)
                WireButtonUpDown(_btnBarrier.gameObject, isDown => {
                    _isBarrierHeld = isDown;
                    if (isDown) _targetInput?.OnBarrierHoldStart();
                    else _targetInput?.OnBarrierHoldEnd();
                });
            if (_btnSidestepUp != null)
                WireButtonUpDown(_btnSidestepUp.gameObject, isDown => { _isSidestepUpHeld = isDown; if (isDown) _targetInput?.OnSidestepUpPressed(); });
            if (_btnSidestepDown != null)
                WireButtonUpDown(_btnSidestepDown.gameObject, isDown => { _isSidestepDownHeld = isDown; if (isDown) _targetInput?.OnSidestepDownPressed(); });
        }

        private void WireButtonUpDown(GameObject go, Action<bool> onStateChanged)
        {
            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();

            EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((data) =>
            {
                onStateChanged?.Invoke(true);
            });
            trigger.triggers.Add(down);

            EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((data) =>
            {
                onStateChanged?.Invoke(false);
            });
            trigger.triggers.Add(up);
        }

        private void AutoDiscoverButtonsIfMissing()
        {
            if (_btnThrow == null) _btnThrow = FindButtonRecursive(gameObject, "Btn_Throw", "Button_Throw", "ThrowButton", "Btn_Grab", "Btn_Touch_Throw");
            if (_btnLight == null) _btnLight = FindButtonRecursive(gameObject, "Btn_Light", "Button_Light", "Btn_Projectile", "Btn_Touch_Light", "Btn_Touch_P");
            if (_btnMedium == null) _btnMedium = FindButtonRecursive(gameObject, "Btn_Medium", "Button_Medium");
            if (_btnHeavy == null) _btnHeavy = FindButtonRecursive(gameObject, "Btn_Heavy", "Button_Heavy", "Btn_Melee", "Btn_Touch_Heavy", "Btn_Touch_M");
            if (_btnDrive == null) _btnDrive = FindButtonRecursive(gameObject, "Btn_Drive", "Button_Drive", "Btn_Special", "Btn_Touch_Drive", "Btn_Touch_S");
            if (_btnSuper == null) _btnSuper = FindButtonRecursive(gameObject, "Btn_Super", "Button_Super", "Btn_Ultra", "Btn_Touch_Super", "Btn_Touch_Ultra");
            if (_btnParry == null) _btnParry = FindButtonRecursive(gameObject, "Btn_Parry", "Button_Parry");
            if (_btnBackdash == null) _btnBackdash = FindButtonRecursive(gameObject, "Btn_Backdash", "Button_Backdash");
            if (_btnBurst == null) _btnBurst = FindButtonRecursive(gameObject, "Btn_Burst", "Button_Burst", "Btn_Break", "Btn_Touch_Burst", "Btn_Touch_Break");
            if (_btnBarrier == null) _btnBarrier = FindButtonRecursive(gameObject, "Btn_Barrier", "Button_Barrier");
            if (_btnSidestepUp == null) _btnSidestepUp = FindButtonRecursive(gameObject, "Btn_SidestepUp", "Button_SidestepUp");
            if (_btnSidestepDown == null) _btnSidestepDown = FindButtonRecursive(gameObject, "Btn_SidestepDown", "Button_SidestepDown");
        }

        private Button FindButtonRecursive(GameObject root, params string[] candidateNames)
        {
            if (root == null) return null;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                foreach (var name in candidateNames)
                {
                    if (string.Equals(b.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return b;
                    }
                }
            }
            return null;
        }

        private void WirePointerDown(GameObject go, Action onDown)
        {
            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener((data) =>
            {
                FindTargetInputIfMissing();
                onDown?.Invoke();
            });
            trigger.triggers.Add(entry);
        }

        private void WirePointerUp(GameObject go, Action onUp)
        {
            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerUp;
            entry.callback.AddListener((data) =>
            {
                FindTargetInputIfMissing();
                onUp?.Invoke();
            });
            trigger.triggers.Add(entry);
        }

        #region Joystick Pointer Callbacks

        public void OnJoystickPointerDown(BaseEventData eventData)
        {
            OnJoystickDrag(eventData);
        }

        public void OnJoystickDrag(BaseEventData eventData)
        {
            PointerEventData pointerData = eventData as PointerEventData;
            if (pointerData == null || _joystickBackground == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickBackground,
                pointerData.position,
                pointerData.pressEventCamera,
                out localPoint
            );

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _joystickRadius);
            _joystickInput = clamped / _joystickRadius;

            if (_joystickHandle != null)
            {
                _joystickHandle.anchoredPosition = clamped;
            }

            if (_targetInput != null)
            {
                _targetInput.SetVirtualJoystick(_joystickInput);
            }
        }

        public void OnJoystickPointerUp(BaseEventData eventData)
        {
            _joystickInput = Vector2.zero;
            if (_joystickHandle != null)
            {
                _joystickHandle.anchoredPosition = Vector2.zero;
            }

            if (_targetInput != null)
            {
                _targetInput.SetVirtualJoystick(Vector2.zero);
            }
        }

        #endregion

        #region Runtime Builder for UFE Scene

        /// <summary>
        /// Programmatically ensures the on-screen touch controls overlay exists on the given Canvas,
        /// wired and ready for mobile touchscreen or mouse input in the UFE combat scene.
        /// </summary>
        public static TouchVirtualControls EnsureRuntimeTouchControls(Transform canvasParent)
        {
            if (Instance != null) return Instance;

            Transform existing = canvasParent.Find("TouchControlsOverlay");
            if (existing != null)
            {
                var existingCtrl = existing.GetComponent<TouchVirtualControls>();
                if (existingCtrl != null) return existingCtrl;
            }

            GameObject touchOverlay = new GameObject("TouchControlsOverlay", typeof(RectTransform));
            touchOverlay.transform.SetParent(canvasParent, false);
            RectTransform rootRt = touchOverlay.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            TouchVirtualControls touchCtrl = touchOverlay.AddComponent<TouchVirtualControls>();

            // Virtual Joystick (Left)
            GameObject joyBg = CreateRuntimeUIElement("Joystick_Background", touchOverlay.transform, 
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(160f, 160f), new Vector2(180f, 180f), 
                new Color(0.1f, 0.12f, 0.18f, 0.65f));
            touchCtrl._joystickBackground = joyBg.GetComponent<RectTransform>();

            GameObject joyHandle = CreateRuntimeUIElement("Joystick_Handle", joyBg.transform, 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f), 
                new Color(0.85f, 0.2f, 0.2f, 0.9f));
            touchCtrl._joystickHandle = joyHandle.GetComponent<RectTransform>();

            VirtualJoystickZone joyZone = joyBg.AddComponent<VirtualJoystickZone>();
            joyZone.BindController(touchCtrl);

            // Action Buttons (Right)
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 3-Button Master Controls Layout: P (Projectile), M (Melee 3-Hit), S (Special), ULTRA (P+M+S)
            Color neonPink = new Color(1.0f, 0.16f, 0.43f, 0.95f);
            Color meleeRed = new Color(0.85f, 0.2f, 0.2f, 0.95f);
            Color projCyan = new Color(0.1f, 0.82f, 0.95f, 0.95f);
            Color specGold = new Color(0.95f, 0.75f, 0.2f, 0.95f);
            Color slate = new Color(0.18f, 0.2f, 0.28f, 0.85f);
            Color techOrange = new Color(0.95f, 0.5f, 0.15f, 0.85f);

            // [P] Projectile Button (Fast 1.8s recharge)
            touchCtrl._btnLight = CreateRuntimeActionButton("Btn_Touch_Light", touchOverlay.transform, "P\n(PROJ)", new Vector2(-235f, 95f), new Vector2(85f, 85f), projCyan, font);
            // [M] Melee Button (3-Hit Target Combo: M1->M2->M3)
            touchCtrl._btnHeavy = CreateRuntimeActionButton("Btn_Touch_Heavy", touchOverlay.transform, "M\n(MELEE)", new Vector2(-125f, 185f), new Vector2(95f, 95f), meleeRed, font);
            // [S] Special Button (Signature Skill / Node Teleport)
            touchCtrl._btnDrive = CreateRuntimeActionButton("Btn_Touch_Drive", touchOverlay.transform, "S\n(SPECIAL)", new Vector2(-215f, 215f), new Vector2(85f, 85f), specGold, font);
            // [ULTRA] Macro Button (P+M+S Chord - Frame 1 Invincible Reversal)
            touchCtrl._btnSuper = CreateRuntimeActionButton("Btn_Touch_Super", touchOverlay.transform, "★ ULTRA ★\n(P+M+S)", new Vector2(-125f, 310f), new Vector2(110f, 65f), neonPink, font);
            // [GRAB] Command Grab (66 Close Proximity)
            touchCtrl._btnThrow = CreateRuntimeActionButton("Btn_Touch_Throw", touchOverlay.transform, "GRAB\n[66]", new Vector2(-325f, 95f), new Vector2(80f, 65f), slate, font);
            // [BREAK] Grab Break Tech (P+M)
            touchCtrl._btnBurst = CreateRuntimeActionButton("Btn_Touch_Burst", touchOverlay.transform, "BREAK\n[P+M]", new Vector2(-325f, 185f), new Vector2(80f, 65f), techOrange, font);

            touchCtrl.CacheControlDefaults();
            touchCtrl.AutoDiscoverButtonsIfMissing();
            touchCtrl.WireActionButtons();
            touchCtrl.ApplySettings(
                SettingsPanel.TouchControlsOpacity,
                SettingsPanel.TouchControlsScale,
                SettingsPanel.LeftHandedMode
            );

            return touchCtrl;
        }

        private static GameObject CreateRuntimeUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static Button CreateRuntimeActionButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, Color color, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = color;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            Text txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        #endregion
    }
}
