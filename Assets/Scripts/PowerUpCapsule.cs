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
        }

        void Update()
        {
            transform.position += Vector3.down * speed * Time.deltaTime;

            float bottomY = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0f)).y;
            if (transform.position.y < bottomY)
                owner.ReturnCapsule(this);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Paddle"))
                return;

            owner.ApplyEffect(type);
            owner.ReturnCapsule(this);
        }
    }
}
