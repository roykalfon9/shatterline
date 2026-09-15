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
        float moveInput;
        float baseWidth;
        Coroutine widenRoutine;

        public float HalfWidth => paddleCollider.bounds.extents.x;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            baseWidth = transform.localScale.x;

            InputActionMap gameplay = controls.FindActionMap("Gameplay", true);
            moveAction = gameplay.FindAction("Move", true);
        }

        void OnEnable()
        {
            moveAction.Enable();
        }

        void Update()
        {
            moveInput = moveAction.ReadValue<float>();
        }

        void FixedUpdate()
        {
            float delta = moveInput * config.paddleSpeed * Time.fixedDeltaTime;
            float newX = ClampToScreen(rb.position.x + delta);
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
