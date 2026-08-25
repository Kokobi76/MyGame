using UnityEngine;
using DG.Tweening;

namespace Match3.Battle.View
{
    /// <summary>
    /// A single flying attack object (a ranged Slash shot, one piece of
    /// Sword rain). Purely presentational — travels from a start to an
    /// end position; the caller decides when damage lands, typically via
    /// the returned <see cref="Tween"/>'s completion.
    /// </summary>
    public sealed class AttackProjectileView : MonoBehaviour
    {
        [SerializeField] private float _flightDuration = 0.35f;

        public Tween PlayFlight(Vector3 from, Vector3 to)
        {
            transform.position = from;
            return transform.DOMove(to, _flightDuration).SetEase(Ease.InQuad);
        }

        /// <summary>Called by the pool before a recycled instance is reused, so no stale tween lingers on it.</summary>
        public void ResetVisualState()
        {
            transform.DOKill();
        }
    }
}
