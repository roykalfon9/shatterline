using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Shatterline
{
    /// <summary>Checks the current pointer position, avoiding the UI module's previous-frame state.</summary>
    public static class PointerInput
    {
        static readonly List<RaycastResult> hits = new List<RaycastResult>();
        static EventSystem lastEventSystem;
        static PointerEventData pointer;

        public static bool IsOverControl(Vector2 screenPosition)
        {
            var system = EventSystem.current;
            if (system == null) return false;
            if (lastEventSystem != system)
            {
                lastEventSystem = system;
                pointer = new PointerEventData(system);
            }
            pointer.Reset();
            pointer.position = screenPosition;
            hits.Clear();
            system.RaycastAll(pointer, hits);
            foreach (var hit in hits)
                if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                    return true;
            return false;
        }

        public static bool IsLaunchOverControl(InputAction launch)
        {
            var device = launch.activeControl?.device;
            if (device is Mouse mouse) return IsOverControl(mouse.position.ReadValue());
            if (device is Touchscreen touch) return IsOverControl(touch.primaryTouch.position.ReadValue());
            return false;
        }
    }
}
