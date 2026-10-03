using UnityEngine;

namespace Shatterline
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField] RectTransform title;
        CanvasGroup group;

        void Awake() => group = GetComponent<CanvasGroup>();
        void OnEnable() => group.alpha = 0f;
        void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / .18f);
            if (title != null)
                title.localScale = Vector3.one * (1f + .008f * Mathf.Sin(Time.unscaledTime * 1.5f));
        }
        void OnDisable()
        {
            if (title != null) title.localScale = Vector3.one;
        }
    }
}
