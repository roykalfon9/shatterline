using UnityEngine;

namespace Shatterline
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] float restOffsetY = 0.35f;

        Rigidbody2D rb;
        Transform paddleReference;

        public bool HasLaunched { get; private set; }
        public float CurrentSpeed { get; private set; }
        public Vector2 Direction { get; private set; } = Vector2.up;

        float speedMultiplier = 1f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        public void SetPaddleReference(Transform paddle)
        {
            paddleReference = paddle;
        }

        public void ResetBall(float baseSpeed, float slowMultiplier)
        {
            HasLaunched = false;
            CurrentSpeed = baseSpeed;
            speedMultiplier = slowMultiplier;
            Direction = Vector2.up;
            rb.linearVelocity = Vector2.zero;
        }

        public void Launch()
        {
            LaunchInDirection(Vector2.up);
        }

        public void LaunchInDirection(Vector2 direction)
        {
            HasLaunched = true;
            Direction = direction.normalized;
            rb.linearVelocity = Direction * CurrentSpeed * speedMultiplier;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
            if (HasLaunched)
                rb.linearVelocity = Direction * CurrentSpeed * speedMultiplier;
        }

        void FixedUpdate()
        {
            if (!HasLaunched)
            {
                rb.MovePosition(new Vector2(paddleReference.position.x, paddleReference.position.y + restOffsetY));
                return;
            }

            if (rb.position.y < paddleReference.position.y && rb.linearVelocity.y < 0f)
            {
                GameManager.Instance.ReportBallLost(this);
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!HasLaunched)
                return;

            if (collision.collider.CompareTag("Paddle"))
            {
                HandlePaddleBounce(collision);
            }
            else
            {
                Direction = rb.linearVelocity.normalized;
                rb.linearVelocity = Direction * CurrentSpeed * speedMultiplier;
            }

            AudioManager.Instance.PlayBounce();
        }

        void HandlePaddleBounce(Collision2D collision)
        {
            Bounds paddleBounds = collision.collider.bounds;
            float offset = (rb.position.x - paddleBounds.center.x) / paddleBounds.extents.x;
            offset = Mathf.Clamp(offset, -1f, 1f);

            float angleDeg = offset * config.paddleMaxDeflectionDeg;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Direction = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)).normalized;

            CurrentSpeed = Mathf.Min(CurrentSpeed + config.speedStepPerHit, config.maxBallSpeed);
            rb.linearVelocity = Direction * CurrentSpeed * speedMultiplier;
        }
    }
}
