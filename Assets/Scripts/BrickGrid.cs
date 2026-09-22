using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public class BrickGrid : MonoBehaviour
    {
        [SerializeField] Brick brickPrefab;
        [SerializeField] Sprite[] brickSprites;
        [SerializeField] PowerUpSpawner powerUpSpawner;
        [SerializeField] ParticleSystem breakParticlePrefab;
        [SerializeField] int breakParticlePoolSize = 12;
        [SerializeField] Camera playCamera;
        [SerializeField] float topMarginViewport = 0.1f;
        [SerializeField] float sideMarginViewport = 0.05f;
        [SerializeField] float rowSpan = 0.55f;
        [SerializeField] float brickGap = 0.08f;
        [SerializeField] float brickHeight = 0.4f;

        readonly List<Brick> activeBricks = new List<Brick>();
        ObjectPool<ParticleSystem> particlePool;

        public int RemainingBreakable { get; private set; }

        void Awake()
        {
            particlePool = new ObjectPool<ParticleSystem>(breakParticlePrefab, breakParticlePoolSize, transform);
        }

        public void BuildLevel(LevelData level)
        {
            foreach (var brick in activeBricks)
                Destroy(brick.gameObject);
            activeBricks.Clear();

            Vector3 topLeft = playCamera.ViewportToWorldPoint(new Vector3(sideMarginViewport, 1f - topMarginViewport, 0f));
            Vector3 topRight = playCamera.ViewportToWorldPoint(new Vector3(1f - sideMarginViewport, 1f - topMarginViewport, 0f));
            float playWidth = topRight.x - topLeft.x;
            int columns = Mathf.Max(1, level.ColumnCount);
            float cellWidth = playWidth / columns;
            float brickWidth = cellWidth - brickGap;

            RemainingBreakable = 0;

            for (int r = 0; r < level.RowCount; r++)
            {
                BrickRow row = level.rows[r];
                int scoreRowIndex = level.ScoringRowIndex(r);

                for (int c = 0; c < row.hp.Length; c++)
                {
                    int hp = row.hp[c];
                    if (hp <= 0)
                        continue;

                    Vector3 pos = new Vector3(
                        topLeft.x + cellWidth * (c + 0.5f),
                        topLeft.y - rowSpan * r,
                        0f);

                    Sprite sprite = brickSprites[(scoreRowIndex - 1 + brickSprites.Length) % brickSprites.Length];

                    Brick brick = Instantiate(brickPrefab, pos, Quaternion.identity, transform);
                    // transform.localScale multiplies the sprite's own pixels-per-unit
                    // size, it isn't an absolute world size - divide by the sprite's
                    // native bounds to land on the actual desired width/height.
                    Vector2 nativeSize = sprite.bounds.size;
                    brick.transform.localScale = new Vector3(brickWidth / nativeSize.x, brickHeight / nativeSize.y, 1f);

                    brick.Initialize(hp, scoreRowIndex * 10, sprite, this);
                    brick.gameObject.SetActive(true);

                    activeBricks.Add(brick);
                    RemainingBreakable++;
                }
            }
        }

        public void NotifyBrickBroken(Brick brick)
        {
            RemainingBreakable--;
            powerUpSpawner.TryDrop(brick.transform.position);

            if (RemainingBreakable <= 0)
                GameManager.Instance.ReportLevelClear();
        }

        public void PlayBreakEffect(Vector3 position, Color color)
        {
            ParticleSystem ps = particlePool.Get();
            ps.transform.position = position;
            var main = ps.main;
            main.startColor = color;
            ps.Play();
            StartCoroutine(ReturnAfterPlay(ps));
        }

        IEnumerator ReturnAfterPlay(ParticleSystem ps)
        {
            yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
            ps.Stop();
            particlePool.Return(ps);
        }
    }
}
