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
        bool mousePositionPending;
        bool mouseInitialized;
        bool touchActive;
        bool touchStartedOverUI;
        Vector2 previousTouchPosition;
        float pendingTouchDelta;
        float baseWidth;
        Coroutine widenRoutine;
        SpriteRenderer spriteRenderer;
        Color baseColor;
        float hitPulse;
        bool isSlow;

        public float HalfWidth => paddleCollider.bounds.extents.x;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
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

        void OnDisable()
        {
            moveAction.Disable();
            mouseXAction.Disable();
        }

        void Update()
        {
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, Time.deltaTime * 7f);
            Color tint = isSlow ? new Color(.7f, .4f, 1f) : baseColor;
            spriteRenderer.color = Color.Lerp(tint, Color.white, hitPulse);
            var game = GameManager.Instance;
            bool canMove = game != null && game.AcceptsPaddleInput;
            moveInput = canMove && !(moveAction.activeControl?.device is Touchscreen)
                ? moveAction.ReadValue<float>() : 0f;

            // Hold pointer changes until a physics tick consumes them. Update can
            // run more often than FixedUpdate, so a one-frame flag loses movement.
            float mouseX = mouseXAction.ReadValue<float>();
            if (mouseInitialized && !Mathf.Approximately(mouseX, lastMouseScreenX) && canMove)
            {
                var mouse = Mouse.current;
                if (mouse != null && !PointerInput.IsOverControl(mouse.position.ReadValue()))
                {
                    mouseScreenX = mouseX;
                    mousePositionPending = true;
                }
            }
            lastMouseScreenX = mouseX;
            mouseInitialized = true;

            var touch = Touchscreen.current?.primaryTouch;
            bool pressed = touch != null && touch.press.isPressed;
            if (pressed)
            {
                Vector2 position = touch.position.ReadValue();
                if (!touchActive)
                    touchStartedOverUI = !canMove || PointerInput.IsOverControl(position);
                else if (canMove && !touchStartedOverUI)
                {
                    // Position difference is a distance, not a velocity. Consume
                    // it once; never multiply by deltaTime or paddle speed.
                    float depth = Mathf.Abs(playCamera.transform.position.z - transform.position.z);
                    Vector3 before = playCamera.ScreenToWorldPoint(new Vector3(previousTouchPosition.x, previousTouchPosition.y, depth));
                    Vector3 after = playCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, depth));
                    pendingTouchDelta += after.x - before.x;
                }
                previousTouchPosition = position;
            }
            touchActive = pressed;
            if (!canMove)
            {
                mousePositionPending = false;
                pendingTouchDelta = 0f;
            }
        }

        void FixedUpdate()
        {
            if (GameManager.Instance == null || !GameManager.Instance.AcceptsPaddleInput) return;
            float newX = rb.position.x;
            if (mousePositionPending)
            {
                float depth = Mathf.Abs(playCamera.transform.position.z - transform.position.z);
                newX = playCamera.ScreenToWorldPoint(new Vector3(mouseScreenX, 0f, depth)).x;
                mousePositionPending = false;
            }
            else if (!touchActive)
                newX += moveInput * config.paddleSpeed * Time.fixedDeltaTime;

            newX += pendingTouchDelta;
            pendingTouchDelta = 0f;
            rb.MovePosition(new Vector2(ClampToScreen(newX), rb.position.y));
        }

        float ClampToScreen(float x)
        {
            Vector3 left = playCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f));
            Vector3 right = playCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f));
            float half = HalfWidth;
            return Mathf.Clamp(x, left.x + half, right.x - half);
        }

        public void ShowHitFeedback() => hitPulse = 1f;
        public void SetSlowVisual(bool active) => isSlow = active;

        public void ResetEffects()
        {
            if (widenRoutine != null) StopCoroutine(widenRoutine);
            widenRoutine = null;
            transform.localScale = new Vector3(baseWidth, transform.localScale.y, transform.localScale.z);
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
