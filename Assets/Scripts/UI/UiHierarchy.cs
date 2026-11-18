using UnityEngine;

namespace DemonIdol.UI
{
    /// <summary>
    /// Shared UI object setup. AddComponent runs Awake immediately, so a modal
    /// behaviour must not assume Image/RawImage has already created a RectTransform.
    /// </summary>
    public static class UiHierarchy
    {
        public static RectTransform RequireRectTransform(GameObject host)
        {
            if (host == null)
                return null;

            RectTransform rect = host.GetComponent<RectTransform>();
            if (rect == null)
                rect = host.AddComponent<RectTransform>();
            return rect;
        }

        /// <summary>
        /// Prefer an existing Canvas ancestor. Otherwise parent under a live Canvas
        /// so a RectTransform modal is not left under a plain Transform.
        /// </summary>
        public static Transform ResolveCanvasParent(Transform preferred)
        {
            if (preferred != null && preferred.GetComponentInParent<Canvas>() != null)
                return preferred;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                return canvas.transform;

            return preferred;
        }
    }
}
