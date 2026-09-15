using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shatterline
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PaddleController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] InputActionAsset controls;
        [SerializeField] Collider2D paddleCollider;
        [SerializeField] Camera playCamera;

        Rigidbody2D rb;
        InputAction moveAction;
        InputAction mouseXAction;
        float moveInput;
        float mouseScreenX;
        float lastMouseScreenX;
        bool mouseActiveThisFrame;
        bool mouseInitialized;
        float baseWidth;
        Coroutine widenRoutine;

        public float HalfWidth => paddleCollider.bounds.extents.x;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            baseWidth = transform.localScale.x;

            InputActionMap gameplay = controls.FindActionMap("Gameplay", true);
            moveAction = gameplay.FindAction("Move", true);
            mouseXAction = gameplay.FindAction("MouseX", true);
        }

        void OnEnable()
        {
            moveAction.Enable();
            mouseXAction.Enable();
        }

        void Update()
        {
            moveInput = moveAction.ReadValue<float>();

            // Mouse is absolute cursor-tracking (GDD §4), not a relative drag
            // like touch: only treat it as "active" while it's actually moving,
            // so keyboard/gamepad control still works on a desktop with an idle
            // mouse plugged in.
            mouseActiveThisFrame = false;
            if (mouseXAction.controls.Count > 0)
            {
                mouseScreenX = mouseXAction.ReadValue<float>();
                if (!mouseInitialized)
                {
                    lastMouseScreenX = mouseScreenX;
                    mouseInitialized = true;
                }
                else if (!Mathf.Approximately(mouseScreenX, lastMouseScreenX))
                {
                    mouseActiveThisFrame = true;
                }
                lastMouseScreenX = mouseScreenX;
            }
        }

        void FixedUpdate()
        {
            float newX;
            if (mouseActiveThisFrame)
            {
                float depth = Mathf.Abs(playCamera.transform.position.z - transform.position.z);
                Vector3 world = playCamera.ScreenToWorldPoint(new Vector3(mouseScreenX, 0f, depth));
                newX = ClampToScreen(world.x);
            }
            else
            {
                float delta = moveInput * config.paddleSpeed * Time.fixedDeltaTime;
                newX = ClampToScreen(rb.position.x + delta);
            }
            rb.MovePosition(new Vector2(newX, rb.position.y));
        }

        float ClampToScreen(float x)
        {
            Vector3 left = playCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f));
            Vector3 right = playCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f));
            float half = HalfWidth;
            return Mathf.Clamp(x, left.x + half, right.x - half);
        }

        public void ApplyWidenPaddle(float multiplier, float duration)
        {
            if (widenRoutine != null)
                StopCoroutine(widenRoutine);
            widenRoutine = StartCoroutine(WidenRoutine(multiplier, duration));
        }

        IEnumerator WidenRoutine(float multiplier, float duration)
        {
            transform.localScale = new Vector3(baseWidth * multiplier, transform.localScale.y, transform.localScale.z);
            yield return new WaitForSeconds(duration);
            transform.localScale = new Vector3(baseWidth, transform.localScale.y, transform.localScale.z);
            widenRoutine = null;
        }
    }
}
