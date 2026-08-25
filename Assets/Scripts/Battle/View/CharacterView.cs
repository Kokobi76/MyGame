using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Match3.Utilities;

namespace Match3.Battle.View
{
    /// <summary>
    /// Visual representation of one character in the scene. Displays the
    /// character's sprite, remembers its resting position, exposes the
    /// points ranged/Sword projectiles should spawn from, and plays the
    /// melee lunge-and-return tween. Everything about the character's
    /// stats lives in <see cref="Match3.Battle.Model.CharacterState"/> —
    /// this class is purely presentational.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Attack Origins (optional — defaults to this transform)")]
        [Tooltip("Where ranged Slash projectiles spawn from. Leave empty to use this character's own position.")]
        [SerializeField] private Transform _projectileOrigin;
        [Tooltip("Where Sword projectiles spawn from (the design doc's \"designated position\"). Leave empty to use this character's own position.")]
        [SerializeField] private Transform _swordSpawnPoint;

        [Header("Melee")]
        [SerializeField] private float _meleeLungeDuration = 0.2f;
        [Tooltip("How far along the path to the target the lunge travels before stopping (0-1). Less than 1 so the two characters don't fully overlap.")]
        [SerializeField] [Range(0.1f, 1f)] private float _meleeApproachFraction = 0.7f;

        public Vector3 HomePosition { get; private set; }
        public Vector3 ProjectileOrigin => _projectileOrigin != null ? _projectileOrigin.position : transform.position;
        public Vector3 SwordSpawnPosition => _swordSpawnPoint != null ? _swordSpawnPoint.position : transform.position;
        public Vector3 TargetPoint => transform.position;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            HomePosition = transform.position;
        }

        /// <summary>Sets this character's visual appearance from its <see cref="Match3.Battle.Data.CharacterConfig"/>.</summary>
        public void SetAppearance(Sprite sprite, Color color)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = color;
        }

        /// <summary>Lunges toward <paramref name="targetPosition"/>, invokes <paramref name="onImpact"/> the moment it arrives, then returns home.</summary>
        public IEnumerator PlayMeleeAttack(Vector3 targetPosition, Action onImpact)
        {
            Vector3 approachPosition = Vector3.Lerp(HomePosition, targetPosition, _meleeApproachFraction);

            yield return transform.DOMove(approachPosition, _meleeLungeDuration).SetEase(Ease.OutQuad).AsCoroutine();
            onImpact?.Invoke();
            yield return transform.DOMove(HomePosition, _meleeLungeDuration).SetEase(Ease.InQuad).AsCoroutine();
        }
    }
}
