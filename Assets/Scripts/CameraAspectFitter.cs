using UnityEngine;

namespace Shatterline
{
    /// <summary>
    /// Pillarboxes/letterboxes the camera's viewport rect so the 9:16 play area
    /// is always fully visible and undistorted, regardless of window/screen shape.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        [SerializeField] float targetAspect = 9f / 16f;

        Camera cam;
        int lastWidth, lastHeight;

        void Awake()
        {
            cam = GetComponent<Camera>();
            Apply();
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
                Apply();
        }

        void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            float windowAspect = (float)Screen.width / Screen.height;
            float scaleHeight = windowAspect / targetAspect;

            Rect rect = new Rect(0f, 0f, 1f, 1f);
            if (scaleHeight < 1f)
            {
                rect.width = 1f;
                rect.height = scaleHeight;
                rect.x = 0f;
                rect.y = (1f - scaleHeight) / 2f;
            }
            else
            {
                float scaleWidth = 1f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1f;
                rect.x = (1f - scaleWidth) / 2f;
                rect.y = 0f;
            }
            cam.rect = rect;
        }
    }
}
