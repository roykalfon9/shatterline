using UnityEngine;

namespace Shatterline
{
    /// <summary>
    /// Minimal falling-capsule behaviour, pooled by PowerUpSpawner.
    /// Not part of the GDD's top-level script table; it's an implementation
    /// detail PowerUpSpawner needs to have something to pool.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PowerUpCapsule : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        float speed;
        PowerUpType type;
        PowerUpSpawner owner;
        Camera cam;
        bool consumed;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Begin(Vector3 position, Sprite sprite, float fallSpeed, PowerUpType powerUpType, PowerUpSpawner spawner, Camera camera)
        {
            transform.position = position;
            spriteRenderer.sprite = sprite;
            speed = fallSpeed;
            type = powerUpType;
            owner = spawner;
            cam = camera;
            consumed = false;
        }

        void Update()
        {
            transform.position += Vector3.down * speed * Time.deltaTime;

            float bottomY = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0f)).y;
            if (transform.position.y < bottomY)
                Consume(() => { });
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Paddle"))
                return;

            // The paddle has two overlapping colliders (a solid one for ball
            // bounces, a slightly larger trigger one for catching power-ups),
            // so a falling capsule can overlap both and fire this callback
            // twice for the same catch. Guard against double-applying the
            // effect (Multi-Ball spawning balls twice, etc.).
            Consume(() => owner.ApplyEffect(type));
        }

        void Consume(System.Action beforeReturn)
        {
            if (consumed)
                return;
            consumed = true;

            beforeReturn();
            owner.ReturnCapsule(this);
        }
    }
}
