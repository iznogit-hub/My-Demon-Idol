using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemonIdol.UI;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.UI
{
    /// <summary>
    /// Sumi-e styled Mobile Touch Overlay supporting:
    /// - Mode A: Dynamic Floating Virtual Joystick (Left hand, CarbonInk brush ring & Vermilion puck)
    /// - Mode B: Pocket Hitbox / Leverless D-Pad (Left hand, Japanese arcade button layout with bottom Thumb Up)
    /// - Mode C: Action Buttons A, B, C, D + Barrier (A+B) + Super / Distortion Macro
    /// - Multi-touch tracking via pointer IDs
    /// - Responsive SafeAreaFitter integration
    /// - showTouchOverlayInEditor toggle for testing in Unity Game View
    /// </summary>
    [DisallowMultipleComponent]
    public class BBTouchOverlay : MonoBehaviour
    {
        public static bool showTouchOverlayInEditor = false;

        public enum LeftHandMode
        {
            Joystick,
            PocketHitbox
        }

        [Header("Target Fighter Sampler")]
        public BBInputSampler targetSampler;

        [Header("Controller Mode")]
        public LeftHandMode currentMode = LeftHandMode.Joystick;

        [Header("UI Containers")]
        [SerializeField] private GameObject _joystickContainer;
        [SerializeField] private GameObject _hitboxContainer;
        [SerializeField] private GameObject _actionContainer;
        [SerializeField] private Text _modeToggleLabel;

        // Multi-touch tracking for Pocket Hitbox
        private bool _hitboxLeft = false;
        private bool _hitboxDown = false;
        private bool _hitboxRight = false;
        private bool _hitboxUp = false;

        // Button held state bitmask
        private ushort _heldButtonMask = 0;

        // Macro super execution state
        private bool _superMacroActive = false;
        private int _superMacroTicks = 0;
        private float _macroFallbackTimer = 0f;
        private float _samplerSearchTimer = 0f;
        private CombatTickEngine _subscribedTickEngine = null;

        private static Sprite s_CircleSprite;
        private static Sprite s_RingSprite;

        public static bool ShouldShowOverlay()
        {
#if UNITY_EDITOR
            if (showTouchOverlayInEditor) return true;
            return false;
#else
            return Application.isMobilePlatform;
#endif
        }

        private void OnEnable()
        {
            SubscribeTickEngine();
        }

        private void OnDisable()
        {
            UnsubscribeTickEngine();
        }

        private void SubscribeTickEngine()
        {
            if (CombatTickEngine.Instance != null && _subscribedTickEngine == null)
            {
                _subscribedTickEngine = CombatTickEngine.Instance;
                _subscribedTickEngine.OnTick -= HandleSimulationTick;
                _subscribedTickEngine.OnTick += HandleSimulationTick;
            }
        }

        private void UnsubscribeTickEngine()
        {
            if (_subscribedTickEngine != null)
            {
                _subscribedTickEngine.OnTick -= HandleSimulationTick;
                _subscribedTickEngine = null;
            }
        }

        private void Awake()
        {
            if (targetSampler == null)
            {
                var samplers = FindObjectsByType<BBInputSampler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var s in samplers)
                {
                    if (s.playerIndex == 0)
                    {
                        targetSampler = s;
                        break;
                    }
                }
            }

            RefreshModeDisplay();
        }

        private void Update()
        {
            if (targetSampler == null)
            {
                _samplerSearchTimer -= Time.unscaledDeltaTime;
                if (_samplerSearchTimer <= 0f)
                {
                    _samplerSearchTimer = 0.5f;
                    var samplers = FindObjectsByType<BBInputSampler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (var s in samplers)
                    {
                        if (s.playerIndex == 0)
                        {
                            targetSampler = s;
                            break;
                        }
                    }
                }
            }

            if (_subscribedTickEngine == null)
            {
                SubscribeTickEngine();
            }

            if (currentMode == LeftHandMode.PocketHitbox)
            {
                UpdateHitboxDirections();
            }

            // Fallback tick progression only if CombatTickEngine is absent (e.g. standalone tests)
            if (_superMacroActive && _subscribedTickEngine == null)
            {
                _macroFallbackTimer += Time.unscaledDeltaTime;
                if (_macroFallbackTimer >= 1f / 60f)
                {
                    _macroFallbackTimer = 0f;
                    StepSuperMacro();
                }
            }
        }

        private void HandleSimulationTick(int tick)
        {
            if (_superMacroActive)
            {
                StepSuperMacro();
            }
        }

        /// <summary>
        /// Steps the 236236C super macro by 1 tick using facing-relative direction injection.
        /// </summary>
        public void StepSuperMacro()
        {
            if (!_superMacroActive || targetSampler == null) return;

            int facing = 1;
            var brain = targetSampler.GetComponent<BBFighterBrain>();
            if (brain != null && brain.fighter != null)
            {
                facing = brain.fighter.FacingDirection >= 0 ? 1 : -1;
            }

            _superMacroTicks++;
            switch (_superMacroTicks)
            {
                case 1: targetSampler.SetTouchInput(BBInputFrame.FacingToWorldNumpad(2, facing), 0); break; // 2 (down)
                case 2: targetSampler.SetTouchInput(BBInputFrame.FacingToWorldNumpad(3, facing), 0); break; // 3 (down-forward)
                case 3: targetSampler.SetTouchInput(BBInputFrame.FacingToWorldNumpad(6, facing), 0); break; // 6 (forward)
                case 4: targetSampler.SetTouchInput(BBInputFrame.FacingToWorldNumpad(2, facing), 0); break; // 2 (down)
                case 5: targetSampler.SetTouchInput(BBInputFrame.FacingToWorldNumpad(3, facing), 0); break; // 3 (down-forward)
                case 6: targetSampler.SetTouchInput(BBInputFrame.FacingToWorldNumpad(6, facing), (ushort)BBButton.C); break; // 6 + C
                default:
                    _superMacroActive = false;
                    _superMacroTicks = 0;
                    if (currentMode == LeftHandMode.PocketHitbox) UpdateHitboxDirections();
                    else targetSampler.SetTouchInput(5, _heldButtonMask);
                    break;
            }
        }

        public void Bind(BBInputSampler sampler)
        {
            targetSampler = sampler;
        }

        public void ToggleMode()
        {
            currentMode = (currentMode == LeftHandMode.Joystick) ? LeftHandMode.PocketHitbox : LeftHandMode.Joystick;
            if (targetSampler != null)
            {
                targetSampler.virtualStickInput = Vector2.zero;
            }
            _hitboxLeft = _hitboxDown = _hitboxRight = _hitboxUp = false;
            RefreshModeDisplay();
            MobileHaptics.Trigger(HapticImpact.Medium);
        }

        public void RefreshModeDisplay()
        {
            if (_joystickContainer != null)
            {
                _joystickContainer.SetActive(currentMode == LeftHandMode.Joystick);
            }
            if (_hitboxContainer != null)
            {
                _hitboxContainer.SetActive(currentMode == LeftHandMode.PocketHitbox);
            }
            if (_modeToggleLabel != null)
            {
                _modeToggleLabel.text = (currentMode == LeftHandMode.Joystick) ? "LEVERLESS" : "JOYSTICK";
            }
        }

        public void SetHitboxDirection(sbyte dir, bool pressed)
        {
            switch (dir)
            {
                case 4: _hitboxLeft = pressed; break;
                case 2: _hitboxDown = pressed; break;
                case 6: _hitboxRight = pressed; break;
                case 8: _hitboxUp = pressed; break;
            }

            if (pressed) MobileHaptics.Trigger(HapticImpact.Light);
            UpdateHitboxDirections();
        }

        private void UpdateHitboxDirections()
        {
            if (targetSampler == null) return;

            Vector2 cleaned = BBInputSampler.CleanSOCD(_hitboxUp, _hitboxDown, _hitboxLeft, _hitboxRight);
            targetSampler.virtualStickInput = cleaned;

            sbyte numpad = 5;
            if (cleaned.x < -0.5f && cleaned.y > 0.5f) numpad = 7;
            else if (cleaned.x > 0.5f && cleaned.y > 0.5f) numpad = 9;
            else if (cleaned.x < -0.5f && cleaned.y < -0.5f) numpad = 1;
            else if (cleaned.x > 0.5f && cleaned.y < -0.5f) numpad = 3;
            else if (cleaned.y > 0.5f) numpad = 8;
            else if (cleaned.y < -0.5f) numpad = 2;
            else if (cleaned.x < -0.5f) numpad = 4;
            else if (cleaned.x > 0.5f) numpad = 6;

            targetSampler.SetTouchInput(numpad, _heldButtonMask);
        }

        public void SetJoystickStick(Vector2 stick)
        {
            if (targetSampler == null) return;
            targetSampler.virtualStickInput = stick;
        }

        public void SetButton(BBButton button, bool pressed)
        {
            ushort bit = (ushort)button;
            if (pressed)
            {
                _heldButtonMask |= bit;
                if (targetSampler != null)
                {
                    targetSampler.virtualHeldMask |= (byte)bit;
                    targetSampler.virtualPressedMask |= (byte)bit;
                }
                MobileHaptics.Trigger(HapticImpact.Light);
            }
            else
            {
                _heldButtonMask = (ushort)(_heldButtonMask & ~bit);
                if (targetSampler != null)
                {
                    targetSampler.virtualHeldMask = (byte)(targetSampler.virtualHeldMask & ~(byte)bit);
                    targetSampler.virtualReleasedMask |= (byte)bit;
                }
            }
        }

        public void TriggerBarrierMacro(bool pressed)
        {
            // A + B Barrier Guard
            SetButton(BBButton.A, pressed);
            SetButton(BBButton.B, pressed);
            if (pressed) MobileHaptics.Trigger(HapticImpact.Medium);
        }

        public void TriggerSuperMacro()
        {
            // 236236C Distortion Drive Macro
            _superMacroActive = true;
            _superMacroTicks = 0;
            _macroFallbackTimer = 0f;
            MobileHaptics.Trigger(HapticImpact.Heavy);
        }

        public static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null) return s_CircleSprite;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float radius = center - 2f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - d + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            s_CircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_CircleSprite;
        }

        public static Sprite GetRingSprite()
        {
            if (s_RingSprite != null) return s_RingSprite;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float outerRadius = center - 2f;
            float innerRadius = outerRadius - 12f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alphaOuter = Mathf.Clamp01(outerRadius - d + 1f);
                    float alphaInner = Mathf.Clamp01(d - innerRadius + 1f);
                    float alpha = Mathf.Min(alphaOuter, alphaInner);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            s_RingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_RingSprite;
        }

        public static BBTouchOverlay Create(Transform parent, BBInputSampler sampler)
        {
            var root = new GameObject("BBTouchOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var safeFitter = root.AddComponent<SafeAreaFitter>();

            var overlay = root.AddComponent<BBTouchOverlay>();
            overlay.Bind(sampler);

            // Container 1: Joystick
            overlay._joystickContainer = CreateJoystickUI(overlay, root.transform);

            // Container 2: Pocket Hitbox
            overlay._hitboxContainer = CreateHitboxUI(overlay, root.transform);

            // Container 3: Action Buttons (A, B, C, D, Barrier, Super)
            overlay._actionContainer = CreateActionButtonsUI(overlay, root.transform);

            // Mode Toggle Button (Top Center-Left)
            CreateModeToggleButton(overlay, root.transform);

            overlay.RefreshModeDisplay();
            return overlay;
        }

        private static GameObject CreateJoystickUI(BBTouchOverlay overlay, Transform parent)
        {
            var stickRoot = new GameObject("JoystickZone", typeof(RectTransform), typeof(Image));
            stickRoot.transform.SetParent(parent, false);
            var rt = stickRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bgImg = stickRoot.GetComponent<Image>();
            bgImg.color = Color.clear; // Transparent hit test area

            // Visual Anchor Base Ring
            var ringGo = new GameObject("BaseRing", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(stickRoot.transform, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = new Vector2(0f, 0f);
            ringRt.anchorMax = new Vector2(0f, 0f);
            ringRt.pivot = new Vector2(0.5f, 0.5f);
            ringRt.anchoredPosition = new Vector2(160f, 160f);
            ringRt.sizeDelta = new Vector2(180f, 180f);

            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = GetRingSprite();
            ringImg.color = SumiStyleTokens.CarbonInkWash;

            // Visual Puck Knob
            var puckGo = new GameObject("PuckKnob", typeof(RectTransform), typeof(Image));
            puckGo.transform.SetParent(ringGo.transform, false);
            var puckRt = puckGo.GetComponent<RectTransform>();
            puckRt.anchorMin = new Vector2(0.5f, 0.5f);
            puckRt.anchorMax = new Vector2(0.5f, 0.5f);
            puckRt.pivot = new Vector2(0.5f, 0.5f);
            puckRt.anchoredPosition = Vector2.zero;
            puckRt.sizeDelta = new Vector2(72f, 72f);

            var puckImg = puckGo.GetComponent<Image>();
            puckImg.sprite = GetCircleSprite();
            puckImg.color = SumiStyleTokens.Vermilion;

            var zone = stickRoot.AddComponent<BBDynamicJoystickZone>();
            zone.overlay = overlay;
            zone.ringTransform = ringRt;
            zone.puckTransform = puckRt;

            return stickRoot;
        }

        private static GameObject CreateHitboxUI(BBTouchOverlay overlay, Transform parent)
        {
            var hitboxRoot = new GameObject("PocketHitboxZone", typeof(RectTransform));
            hitboxRoot.transform.SetParent(parent, false);
            var rt = hitboxRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Vector2 basePos = new Vector2(160f, 130f);

            // Left (4), Down (2), Right (6) in curved Japanese arcade arch
            CreateHitboxArcadeButton(overlay, hitboxRoot.transform, "Hitbox_Left", 4, basePos + new Vector2(-75f, 15f), "◀", 64f, SumiStyleTokens.CarbonInk);
            CreateHitboxArcadeButton(overlay, hitboxRoot.transform, "Hitbox_Down", 2, basePos + new Vector2(0f, 40f), "▼", 64f, SumiStyleTokens.CarbonInk);
            CreateHitboxArcadeButton(overlay, hitboxRoot.transform, "Hitbox_Right", 6, basePos + new Vector2(75f, 25f), "▶", 64f, SumiStyleTokens.CarbonInk);

            // Thumb Up (8) (Jump button located at bottom center)
            CreateHitboxArcadeButton(overlay, hitboxRoot.transform, "Hitbox_Up", 8, basePos + new Vector2(0f, -48f), "▲ JUMP", 82f, SumiStyleTokens.Vermilion);

            return hitboxRoot;
        }

        private static void CreateHitboxArcadeButton(BBTouchOverlay overlay, Transform parent, string name, sbyte direction, Vector2 pos, string label, float diameter, Color color)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(diameter, diameter);

            var img = btnGo.GetComponent<Image>();
            img.sprite = GetCircleSprite();
            img.color = color;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = diameter > 70f ? 15 : 20;
            txt.fontStyle = FontStyle.Bold;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var handler = btnGo.AddComponent<BBPointerButtonHandler>();
            handler.OnPointerDownAction = () => overlay.SetHitboxDirection(direction, true);
            handler.OnPointerUpAction = () => overlay.SetHitboxDirection(direction, false);
        }

        private static GameObject CreateActionButtonsUI(BBTouchOverlay overlay, Transform parent)
        {
            var actionRoot = new GameObject("ActionButtonsZone", typeof(RectTransform));
            actionRoot.transform.SetParent(parent, false);
            var rt = actionRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Ergonomic 4-button arc layout (Right-hand thumb arc)
            Vector2 origin = new Vector2(-120f, 120f);

            // A: Light Jab (5A) - Bottom Left
            CreateActionButton(overlay, actionRoot.transform, "Btn_A", "5A", origin + new Vector2(-110f, -25f), 78f, SumiStyleTokens.Vermilion,
                () => overlay.SetButton(BBButton.A, true), () => overlay.SetButton(BBButton.A, false));

            // B: Medium Strike (5B) - Top Left
            CreateActionButton(overlay, actionRoot.transform, "Btn_B", "5B", origin + new Vector2(-60f, 65f), 78f, new Color(0.12f, 0.40f, 0.90f, 0.92f),
                () => overlay.SetButton(BBButton.B, true), () => overlay.SetButton(BBButton.B, false));

            // C: Heavy Strike (5C) - Top Right
            CreateActionButton(overlay, actionRoot.transform, "Btn_C", "5C", origin + new Vector2(30f, 75f), 82f, SumiStyleTokens.ParryGold,
                () => overlay.SetButton(BBButton.C, true), () => overlay.SetButton(BBButton.C, false));

            // D: Drive Signature (5D) - Far Right
            CreateActionButton(overlay, actionRoot.transform, "Btn_D", "5D", origin + new Vector2(85f, -10f), 82f, new Color(0.58f, 0.20f, 0.85f, 0.92f),
                () => overlay.SetButton(BBButton.D, true), () => overlay.SetButton(BBButton.D, false));

            // Barrier Macro (A+B) - Defensive Shield (Under A & B)
            CreateActionButton(overlay, actionRoot.transform, "Btn_Barrier", "BARRIER\n(A+B)", origin + new Vector2(-155f, 85f), 60f, new Color(0.06f, 0.65f, 0.91f, 0.88f),
                () => overlay.TriggerBarrierMacro(true), () => overlay.TriggerBarrierMacro(false), 10);

            // Super Macro (236236C / Distortion) - Crimson Finisher Macro (Above C & D)
            CreateActionButton(overlay, actionRoot.transform, "Btn_Super", "SUPER\n236236C", origin + new Vector2(-10f, 175f), 68f, new Color(0.88f, 0.12f, 0.18f, 0.92f),
                () => overlay.TriggerSuperMacro(), null, 11);

            return actionRoot;
        }

        private static void CreateActionButton(BBTouchOverlay overlay, Transform parent, string name, string label, Vector2 pos, float diameter, Color color,
            Action onDown, Action onUp, int fontSize = 24)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(diameter, diameter);

            var img = btnGo.GetComponent<Image>();
            img.sprite = GetCircleSprite();
            img.color = color;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var handler = btnGo.AddComponent<BBPointerButtonHandler>();
            handler.OnPointerDownAction = onDown;
            handler.OnPointerUpAction = onUp;
        }

        private static void CreateModeToggleButton(BBTouchOverlay overlay, Transform parent)
        {
            var toggleGo = new GameObject("ModeToggleBtn", typeof(RectTransform), typeof(Image));
            toggleGo.transform.SetParent(parent, false);
            var rt = toggleGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -24f);
            rt.sizeDelta = new Vector2(120f, 38f);

            var img = toggleGo.GetComponent<Image>();
            img.color = SumiStyleTokens.CarbonInkWash;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(toggleGo.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.text = "LEVERLESS";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = SumiStyleTokens.PaperWhite;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            overlay._modeToggleLabel = txt;

            var handler = toggleGo.AddComponent<BBPointerButtonHandler>();
            handler.OnPointerDownAction = () => overlay.ToggleMode();
        }
    }

    /// <summary>
    /// Multi-touch pointer tracker for individual arcade buttons.
    /// Tracks pointerDown and pointerUp with unique pointer IDs.
    /// </summary>
    public class BBPointerButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action OnPointerDownAction;
        public Action OnPointerUpAction;

        private int _activePointerId = -1;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId == -1)
            {
                _activePointerId = eventData.pointerId;
                OnPointerDownAction?.Invoke();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId)
            {
                _activePointerId = -1;
                OnPointerUpAction?.Invoke();
            }
        }
    }

    /// <summary>
    /// Dynamic capacitive thumbstick responder with Sumi-e brush puck and ring.
    /// Re-centers on pointer down, clamps movement within maxRadius, and outputs analog vector.
    /// </summary>
    public class BBDynamicJoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public BBTouchOverlay overlay;
        public RectTransform ringTransform;
        public RectTransform puckTransform;

        public float maxRadius = 75f;
        public float deadzone = 12f;

        private Vector2 _centerPos;
        private int _activePointerId = -1;
        private bool _isDragging = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isDragging) return;

            _isDragging = true;
            _activePointerId = eventData.pointerId;

            // Re-center ring to touch position dynamically
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out _centerPos
            );

            if (ringTransform != null)
            {
                ringTransform.anchoredPosition = _centerPos;
            }

            UpdatePuckAndStick(eventData.position, eventData.pressEventCamera);
            MobileHaptics.Trigger(HapticImpact.Light);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _activePointerId) return;
            UpdatePuckAndStick(eventData.position, eventData.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            _isDragging = false;
            _activePointerId = -1;

            if (puckTransform != null)
            {
                puckTransform.anchoredPosition = Vector2.zero;
            }

            if (overlay != null)
            {
                overlay.SetJoystickStick(Vector2.zero);
            }
        }

        private void UpdatePuckAndStick(Vector2 screenPos, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                cam,
                out Vector2 localPos
            );

            Vector2 delta = localPos - _centerPos;
            float dist = delta.magnitude;

            if (dist < deadzone)
            {
                if (puckTransform != null) puckTransform.anchoredPosition = Vector2.zero;
                if (overlay != null) overlay.SetJoystickStick(Vector2.zero);
                return;
            }

            Vector2 dir = delta / dist;
            float clampedDist = Mathf.Min(dist, maxRadius);

            if (puckTransform != null)
            {
                puckTransform.anchoredPosition = dir * clampedDist;
            }

            float normalized = (clampedDist - deadzone) / (maxRadius - deadzone);
            if (overlay != null)
            {
                overlay.SetJoystickStick(dir * normalized);
            }
        }
    }
}
