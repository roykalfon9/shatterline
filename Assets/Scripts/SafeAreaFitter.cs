using UnityEngine;

namespace Shatterline
{
    /// <summary>
    /// Fits UI to the intersection of the play camera and the device safe area.
    /// Attach to a full-stretch child of
    /// the Canvas and parent all UI panels under it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        Camera playCamera;
        Rect lastArea;
        Vector2Int lastScreen;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            playCamera = Camera.main;
        }

        // CameraAspectFitter updates first, so both world and UI use the same viewport.
        void LateUpdate()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            Rect viewport = playCamera != null ? playCamera.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
            Rect area = Rect.MinMaxRect(Mathf.Max(safe.xMin, viewport.xMin), Mathf.Max(safe.yMin, viewport.yMin),
                Mathf.Min(safe.xMax, viewport.xMax), Mathf.Min(safe.yMax, viewport.yMax));
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == lastArea && screen == lastScreen) return;
            lastArea = area;
            lastScreen = screen;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }
    }
}
