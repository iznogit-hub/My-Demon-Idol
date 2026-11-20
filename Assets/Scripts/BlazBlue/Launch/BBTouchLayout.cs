using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// On-screen A B C D and a stick. Writes the existing BBInputSampler virtual masks.
    /// </summary>
    public class BBTouchLayout : MonoBehaviour
    {
        public BBInputSampler target;

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
            }
            else
            {
                target.virtualHeldMask = (byte)(target.virtualHeldMask & ~bit);
                target.virtualReleasedMask |= bit;
            }
        }

        public void SetStick(Vector2 stick)
        {
            if (target == null) return;
            target.virtualStickInput = stick;
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
            CreateButton(layout, root.transform, "A", BBButton.A, new Vector2(-80f, 80f));
            CreateButton(layout, root.transform, "B", BBButton.B, new Vector2(-80f, 170f));
            CreateButton(layout, root.transform, "C", BBButton.C, new Vector2(-170f, 80f));
            CreateButton(layout, root.transform, "D", BBButton.D, new Vector2(-170f, 170f));
            return layout;
        }

        static void CreateButton(BBTouchLayout layout, Transform parent, string label, BBButton button, Vector2 corner)
        {
            var go = new GameObject("Touch_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = corner;
            rt.sizeDelta = new Vector2(72f, 72f);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.14f, 0.72f);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
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

        static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => call());
            trigger.triggers.Add(entry);
        }
    }
}
