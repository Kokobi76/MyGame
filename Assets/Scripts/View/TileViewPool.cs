using UnityEngine;
using UnityEngine.Pool;

namespace Match3.View
{
    /// <summary>
    /// Thin wrapper around Unity's built-in <see cref="ObjectPool{T}"/>
    /// (UnityEngine.Pool) for <see cref="TileView"/> instances. Unity 6
    /// ships this pooling implementation out of the box, so this class
    /// only wires up the create/get/release/destroy callbacks instead of
    /// reimplementing a pool from scratch.
    /// </summary>
    public sealed class TileViewPool
    {
        private readonly IObjectPool<TileView> _pool;
        private readonly TileView _prefab;
        private readonly Transform _parent;

        public TileViewPool(TileView prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<TileView>(
                CreateTileView,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyPooledTileView,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public TileView Get()
        {
            return _pool.Get();
        }

        public void Release(TileView tileView)
        {
            _pool.Release(tileView);
        }

        private TileView CreateTileView()
        {
            return UnityEngine.Object.Instantiate(_prefab, _parent);
        }

        private void OnGetFromPool(TileView tileView)
        {
            tileView.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(TileView tileView)
        {
            tileView.ResetVisualState();
            tileView.gameObject.SetActive(false);
        }

        private void OnDestroyPooledTileView(TileView tileView)
        {
            UnityEngine.Object.Destroy(tileView.gameObject);
        }
    }
}
