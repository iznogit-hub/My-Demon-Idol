using UnityEngine;
using UnityEngine.EventSystems;

namespace DemonIdol.Input
{
    /// <summary>
    /// Attach directly to the Joystick Background RectTransform.
    /// Dispatches touch events to the TouchVirtualControls master controller.
    /// </summary>
    public class VirtualJoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private TouchVirtualControls _controller;

        public void BindController(TouchVirtualControls ctrl)
        {
            _controller = ctrl;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_controller != null) _controller.OnJoystickPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_controller != null) _controller.OnJoystickDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_controller != null) _controller.OnJoystickPointerUp(eventData);
        }
    }
}
