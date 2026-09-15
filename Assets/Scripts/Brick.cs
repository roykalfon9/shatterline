using System.Collections;
using UnityEngine;

namespace Shatterline
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Brick : MonoBehaviour
    {
        [SerializeField] float flashDuration = 0.1f;

        SpriteRenderer spriteRenderer;
        int hp;
        int scoreValue;
        BrickGrid owner;
        Coroutine flashRoutine;
        Color baseColor;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(int startingHp, int brickScoreValue, Sprite sprite, BrickGrid grid)
        {
            hp = startingHp;
            scoreValue = brickScoreValue;
            owner = grid;
            spriteRenderer.sprite = sprite;
            baseColor = Color.white;
            spriteRenderer.color = baseColor;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.collider.CompareTag("Ball"))
                return;

            hp--;
            if (hp <= 0)
                Break();
            else
                Flash();
        }

        void Flash()
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            spriteRenderer.color = Color.white * 1.6f;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = baseColor;
            flashRoutine = null;
        }

        void Break()
        {
            AudioManager.Instance.PlayBreak();
            owner.PlayBreakEffect(transform.position, spriteRenderer.color);
            GameManager.Instance.AddScore(scoreValue);
            owner.NotifyBrickBroken(this);
            gameObject.SetActive(false);
        }
    }
}
