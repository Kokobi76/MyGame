using UnityEngine;
using UnityEngine.Pool;

namespace Match3.Battle.View
{
    /// <summary>
    /// Pools <see cref="AttackProjectileView"/> instances the same way
    /// <see cref="Match3.View.TileViewPool"/> pools tiles: wraps Unity's
    /// built-in <see cref="ObjectPool{T}"/> instead of reimplementing one.
    /// </summary>
    public sealed class AttackProjectilePool
    {
        private readonly IObjectPool<AttackProjectileView> _pool;
        private readonly AttackProjectileView _prefab;
        private readonly Transform _parent;

        public AttackProjectilePool(AttackProjectileView prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<AttackProjectileView>(
                CreateProjectile,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyPooledProjectile,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public AttackProjectileView Get()
        {
            return _pool.Get();
        }

        public void Release(AttackProjectileView projectile)
        {
            _pool.Release(projectile);
        }

        private AttackProjectileView CreateProjectile()
        {
            return UnityEngine.Object.Instantiate(_prefab, _parent);
        }

        private void OnGetFromPool(AttackProjectileView projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(AttackProjectileView projectile)
        {
            projectile.ResetVisualState();
            projectile.gameObject.SetActive(false);
        }

        private void OnDestroyPooledProjectile(AttackProjectileView projectile)
        {
            UnityEngine.Object.Destroy(projectile.gameObject);
        }
    }
}
