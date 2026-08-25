using UnityEngine;
using DG.Tweening;

namespace Match3.View
{
    /// <summary>
    /// Visual representation of a single tile. Purely presentational: it
    /// knows how to display a type and play its own move/spawn/removal
    /// tweens, but nothing about grid rules. Instances are recycled by
    /// <see cref="TileViewPool"/> rather than destroyed between uses.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public int TypeId { get; private set; }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(int typeId, Sprite sprite, Color color)
        {
            TypeId = typeId;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = color;
            transform.localScale = Vector3.one;
        }

        public Tween AnimateMoveTo(Vector3 targetPosition, float duration)
        {
            return transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad);
        }

        public Tween AnimateSpawn(float duration)
        {
            transform.localScale = Vector3.zero;
            return transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        }

        public Tween AnimateRemoval(float duration)
        {
            return transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }

        /// <summary>Called by the pool before a recycled instance is reused, so no stale tween lingers on it.</summary>
        public void ResetVisualState()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }
}
