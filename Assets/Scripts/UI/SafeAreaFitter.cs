using UnityEngine;

namespace DemonIdol.UI
{
    /// <summary>
    /// Automatic Mobile Safe Area Fitter for iPhone Notch, Dynamic Island,
    /// and Android camera cutouts. Clamps anchors to Screen.safeArea with minimum padding.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Padding")]
        [Tooltip("Extra padding in points beyond native safe area")]
        [SerializeField] private float _extraHorizontalPadding = 8f;
        [SerializeField] private float _extraVerticalPadding = 8f;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int _lastScreenSize = new Vector2Int(0, 0);
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height ||
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Convert safe area rectangle from absolute pixels to normalized anchors
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x = Mathf.Clamp01((anchorMin.x + _extraHorizontalPadding) / Screen.width);
            anchorMin.y = Mathf.Clamp01((anchorMin.y + _extraVerticalPadding) / Screen.height);
            anchorMax.x = Mathf.Clamp01((anchorMax.x - _extraHorizontalPadding) / Screen.width);
            anchorMax.y = Mathf.Clamp01((anchorMax.y - _extraVerticalPadding) / Screen.height);

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
