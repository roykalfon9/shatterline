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
        MaterialPropertyBlock properties;
        static readonly int DamageId = Shader.PropertyToID("_Damage");

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            properties = new MaterialPropertyBlock();
        }

        public void Initialize(int startingHp, int brickScoreValue, Sprite sprite, BrickGrid grid, Color tint)
        {
            hp = startingHp;
            scoreValue = brickScoreValue;
            owner = grid;
            spriteRenderer.sprite = sprite;
            baseColor = tint;
            properties.SetFloat(DamageId, 0f);
            spriteRenderer.SetPropertyBlock(properties);
            spriteRenderer.color = baseColor;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (hp <= 0 || !collision.collider.CompareTag("Ball"))
                return;

            hp--;
            if (hp <= 0)
                Break();
            else
                Flash();
        }

        void Flash()
        {
            AudioManager.Instance.PlayToughHit();
            properties.SetFloat(DamageId, 1f);
            spriteRenderer.SetPropertyBlock(properties);
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
            owner.PlayBreakEffect(transform.position, baseColor);
            GameManager.Instance.AddScore(scoreValue);
            owner.NotifyBrickBroken(this);
            gameObject.SetActive(false);
        }
    }
}
