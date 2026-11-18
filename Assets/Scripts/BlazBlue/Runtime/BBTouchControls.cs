using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// BlazBlue touch diamond. Built at runtime so the Classic overlay prefab stays untouched.
    /// Reference layout is 1920x1080, buttons anchored from the bottom-right.
    /// </summary>
    public class BBTouchControls : MonoBehaviour
    {
        BBInputSampler _sampler;

        public static BBTouchControls Ensure(Transform canvas, BBInputSampler sampler)
        {
            if (SettingsPanel.ControlScheme != ControlScheme.BlazBlue4Button) return null;
            Transform existing = canvas.Find("TouchControls_BB");
            BBTouchControls ctrl = existing != null ? existing.GetComponent<BBTouchControls>() : null;
            if (ctrl == null)
            {
                GameObject root = new GameObject("TouchControls_BB", typeof(RectTransform));
                root.transform.SetParent(canvas, false);
                RectTransform rt = root.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                ctrl = root.AddComponent<BBTouchControls>();
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ctrl.Button(root.transform, "A", new Vector2(-470f, 150f), 150f, font, s => s.touchA = true, s => s.touchA = false);
                ctrl.Button(root.transform, "B", new Vector2(-430f, 330f), 150f, font, s => s.touchB = true, s => s.touchB = false);
                ctrl.Button(root.transform, "C", new Vector2(-250f, 300f), 150f, font, s => s.touchC = true, s => s.touchC = false);
                ctrl.Button(root.transform, "D", new Vector2(-230f, 120f), 170f, font, s => s.touchD = true, s => s.touchD = false);
                ctrl.Button(root.transform, "THROW", new Vector2(-470f, 500f), 110f, font, s => s.touchThrow = true, s => s.touchThrow = false);
                ctrl.Button(root.transform, "BARRIER", new Vector2(-250f, 480f), 110f, font, s => s.touchBarrier = true, s => s.touchBarrier = false);
                ctrl.Button(root.transform, "RC", new Vector2(-430f, 640f), 96f, font, s => s.touchRapid = true, s => s.touchRapid = false);
                ctrl.Button(root.transform, "BURST", new Vector2(-250f, 640f), 96f, font, s => s.touchBurst = true, s => s.touchBurst = false);
            }
            ctrl._sampler = sampler;
            return ctrl;
        }

        void Button(Transform parent, string label, Vector2 pos, float size, Font font, System.Action<BBInputSampler> down, System.Action<BBInputSampler> up)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            go.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.2f, 0.9f);
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;
            EventTrigger trigger = go.AddComponent<EventTrigger>();
            EventTrigger.Entry press = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            press.callback.AddListener(_ => { if (_sampler != null) down(_sampler); });
            trigger.triggers.Add(press);
            EventTrigger.Entry release = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            release.callback.AddListener(_ => { if (_sampler != null) up(_sampler); });
            trigger.triggers.Add(release);
        }
    }
}
