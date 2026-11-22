using UnityEngine;
using UnityEngine.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Reads the shared combo counter. Created at runtime if the scene has no widget.
    /// </summary>
    public class BBComboReadout : MonoBehaviour
    {
        Text _text;

        public static BBComboReadout Create(Transform parent)
        {
            var go = new GameObject("ComboReadout", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.2f, 1f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta = new Vector2(280f, 64f);
            var readout = go.AddComponent<BBComboReadout>();
            readout._text = text;
            return readout;
        }

        void Update()
        {
            if (_text == null) return;
            int combo = BBContactPresentation.ComboCount;
            _text.text = combo > 1 ? combo + " HITS" : "";
            if (BBSuperFlash.FreezeTicks > 0)
            {
                _text.text = "SUPER";
            }
        }
    }
}
