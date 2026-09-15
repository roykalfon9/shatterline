using UnityEngine;

namespace Shatterline
{
    /// <summary>
    /// Shrinks this RectTransform to Screen.safeArea so UI content clears
    /// notches/cutouts on Android devices. Attach to a full-stretch child of
    /// the Canvas and parent all UI panels under it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        Rect lastSafeArea;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != lastSafeArea)
                Apply();
        }

        void Apply()
        {
            lastSafeArea = Screen.safeArea;

            Vector2 anchorMin = lastSafeArea.position;
            Vector2 anchorMax = lastSafeArea.position + lastSafeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }
    }
}
