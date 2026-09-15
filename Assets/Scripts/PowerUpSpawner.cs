using UnityEngine;

namespace Shatterline
{
    public enum PowerUpType
    {
        WidenPaddle,
        SlowBall,
        MultiBall
    }

    public class PowerUpSpawner : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] PaddleController paddle;
        [SerializeField] Camera playCamera;

        [Header("Pool")]
        [SerializeField] PowerUpCapsule capsulePrefab;
        [SerializeField] int poolSize = 6;
        [SerializeField] float fallSpeed = 3f;

        [Header("Sprites")]
        [SerializeField] Sprite widenPaddleSprite;
        [SerializeField] Sprite slowBallSprite;
        [SerializeField] Sprite multiBallSprite;

        [Header("Widen Paddle")]
        [SerializeField] float widenMultiplier = 1.5f;
        [SerializeField] float widenDuration = 8f;

        [Header("Slow Ball")]
        [SerializeField] float slowMultiplier = 0.65f;
        [SerializeField] float slowDuration = 8f;

        ObjectPool<PowerUpCapsule> pool;

        void Awake()
        {
            pool = new ObjectPool<PowerUpCapsule>(capsulePrefab, poolSize, transform);
        }

        public void TryDrop(Vector3 position)
        {
            if (Random.value > config.powerUpDropChance)
                return;

            var type = (PowerUpType)Random.Range(0, 3);
            Sprite sprite = SpriteFor(type);

            PowerUpCapsule capsule = pool.Get();
            capsule.Begin(position, sprite, fallSpeed, type, this, playCamera);
        }

        Sprite SpriteFor(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.WidenPaddle: return widenPaddleSprite;
                case PowerUpType.SlowBall: return slowBallSprite;
                default: return multiBallSprite;
            }
        }

        public void ReturnCapsule(PowerUpCapsule capsule)
        {
            pool.Return(capsule);
        }

        public void ApplyEffect(PowerUpType type)
        {
            AudioManager.Instance.PlayPowerUp();
            switch (type)
            {
                case PowerUpType.WidenPaddle:
                    paddle.ApplyWidenPaddle(widenMultiplier, widenDuration);
                    break;
                case PowerUpType.SlowBall:
                    GameManager.Instance.ApplySlowBall(slowMultiplier, slowDuration);
                    break;
                case PowerUpType.MultiBall:
                    GameManager.Instance.SpawnMultiBalls();
                    break;
            }
        }
    }
}
