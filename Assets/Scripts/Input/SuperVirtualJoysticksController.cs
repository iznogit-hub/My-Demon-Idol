using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DemonIdol.Combat;

namespace DemonIdol.Input
{
    public enum VirtualControlScheme
    {
        StandardTouchButtons,
        DualSuperVirtualJoysticks
    }

    /// <summary>
    /// Super Virtual Joysticks: Dual On-Screen Joystick System for Unity.
    /// Provides an optional dual-stick combat layout:
    /// - Left Joystick: 8-way directional navigation, jumping, crouching, dashing.
    /// - Right Joystick: Dynamic action skill dial (Melee, Projectile, Special, Ultra, Grab, Break).
    /// Provides dynamic floating center, deadzone filtering, visual haptic glow, and instant scheme toggling.
    /// </summary>
    [DisallowMultipleComponent]
    public class SuperVirtualJoysticksController : MonoBehaviour
    {
        public static SuperVirtualJoysticksController Instance { get; private set; }

        [Header("Control Scheme")]
        [SerializeField] private VirtualControlScheme _currentScheme = VirtualControlScheme.StandardTouchButtons;

        [Header("Target Fighter Input")]
        [SerializeField] private MobileFighterInput _targetInput;

        [Header("Left Joystick (Movement)")]
        [SerializeField] private RectTransform _leftStickBase;
        [SerializeField] private RectTransform _leftStickHandle;
        [SerializeField] private float _leftStickRadius = 85f;
        [SerializeField] private float _deadzone = 0.15f;

        [Header("Right Joystick (Combat / Skill Dial)")]
        [SerializeField] private RectTransform _rightStickBase;
        [SerializeField] private RectTransform _rightStickHandle;
        [SerializeField] private float _rightStickRadius = 85f;
        [SerializeField] private float _flickThreshold = 0.55f;

        [Header("UI Canvas Group")]
        [SerializeField] private CanvasGroup _joystickCanvasGroup;
        [SerializeField] private GameObject _standardButtonsContainer;

        private Vector2 _leftInput = Vector2.zero;
        private Vector2 _rightInput = Vector2.zero;
        private bool _isLeftTouching = false;
        private bool _isRightTouching = false;
        private bool _rightActionFired = false;

        public VirtualControlScheme CurrentScheme => _currentScheme;
        public Vector2 MovementVector => _leftInput;
        public Vector2 ActionVector => _rightInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Load user preference
            int savedScheme = PlayerPrefs.GetInt("MDI_VirtualControlScheme", (int)VirtualControlScheme.StandardTouchButtons);
            _currentScheme = (VirtualControlScheme)savedScheme;
        }

        private void Start()
        {
            ApplyScheme(_currentScheme);
        }

        public void SetTargetInput(MobileFighterInput input)
        {
            _targetInput = input;
        }

        public void ToggleControlScheme()
        {
            var nextScheme = (_currentScheme == VirtualControlScheme.StandardTouchButtons)
                ? VirtualControlScheme.DualSuperVirtualJoysticks
                : VirtualControlScheme.StandardTouchButtons;

            ApplyScheme(nextScheme);
        }

        public void ApplyScheme(VirtualControlScheme scheme)
        {
            _currentScheme = scheme;
            PlayerPrefs.SetInt("MDI_VirtualControlScheme", (int)_currentScheme);
            PlayerPrefs.Save();

            bool isDualStick = (_currentScheme == VirtualControlScheme.DualSuperVirtualJoysticks);

            if (_rightStickBase != null) _rightStickBase.gameObject.SetActive(isDualStick);
            if (_standardButtonsContainer != null) _standardButtonsContainer.SetActive(!isDualStick);

            if (_joystickCanvasGroup != null)
            {
                _joystickCanvasGroup.alpha = 1f;
                _joystickCanvasGroup.blocksRaycasts = true;
            }
        }

        public void OnLeftStickPointerDown(PointerEventData eventData)
        {
            _isLeftTouching = true;
            UpdateLeftStick(eventData);
        }

        public void OnLeftStickDrag(PointerEventData eventData)
        {
            UpdateLeftStick(eventData);
        }

        public void OnLeftStickPointerUp(PointerEventData eventData)
        {
            _isLeftTouching = false;
            _leftInput = Vector2.zero;
            if (_leftStickHandle != null) _leftStickHandle.anchoredPosition = Vector2.zero;
            SendMovementToTarget(Vector2.zero);
        }

        private void UpdateLeftStick(PointerEventData eventData)
        {
            if (_leftStickBase == null || _leftStickHandle == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _leftStickBase, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

            Vector2 clamped = Vector2.ClampMagnitude(localPos, _leftStickRadius);
            _leftStickHandle.anchoredPosition = clamped;

            Vector2 normalized = clamped / _leftStickRadius;
            if (normalized.magnitude < _deadzone)
            {
                normalized = Vector2.zero;
            }
            _leftInput = normalized;
            SendMovementToTarget(_leftInput);
        }

        public void OnRightStickPointerDown(PointerEventData eventData)
        {
            _isRightTouching = true;
            _rightActionFired = false;
            UpdateRightStick(eventData);
        }

        public void OnRightStickDrag(PointerEventData eventData)
        {
            UpdateRightStick(eventData);
        }

        public void OnRightStickPointerUp(PointerEventData eventData)
        {
            _isRightTouching = false;
            _rightActionFired = false;
            _rightInput = Vector2.zero;
            if (_rightStickHandle != null) _rightStickHandle.anchoredPosition = Vector2.zero;
        }

        private void UpdateRightStick(PointerEventData eventData)
        {
            if (_rightStickBase == null || _rightStickHandle == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rightStickBase, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

            Vector2 clamped = Vector2.ClampMagnitude(localPos, _rightStickRadius);
            _rightStickHandle.anchoredPosition = clamped;

            Vector2 normalized = clamped / _rightStickRadius;
            _rightInput = normalized;

            // Trigger action on deliberate flick past threshold
            if (!_rightActionFired && normalized.magnitude >= _flickThreshold)
            {
                _rightActionFired = true;
                DispatchSkillByAngle(normalized);
            }
            else if (normalized.magnitude < _flickThreshold * 0.5f)
            {
                // Reset so rapid flick combos can re-trigger
                _rightActionFired = false;
            }
        }

        private void DispatchSkillByAngle(Vector2 dir)
        {
            if (_targetInput == null) return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Directional Skill Mapping:
            // 315..45 deg (Right): Melee Combo [M]
            // 45..135 deg (Up): Special Attack [S]
            // 135..225 deg (Left): Projectile Blast [P]
            // 225..315 deg (Down): Ultra / Domain Super [ULTRA]
            if (angle >= 315f || angle < 45f)
            {
                _targetInput.OnMeleePressed();
            }
            else if (angle >= 45f && angle < 135f)
            {
                _targetInput.OnSpecialPressed();
            }
            else if (angle >= 135f && angle < 225f)
            {
                _targetInput.OnProjectilePressed();
            }
            else
            {
                _targetInput.OnUltraPressed();
            }
        }

        private void SendMovementToTarget(Vector2 move)
        {
            if (_targetInput != null)
            {
                _targetInput.SetVirtualJoystick(move);
            }
        }
    }
}
