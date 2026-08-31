using UnityEngine;
using UnityEngine.Pool;

namespace Match3.Battle.View
{
    /// <summary>Pools <see cref="FloatingTextView"/> instances the same way the other view pools in this project work.</summary>
    public sealed class FloatingTextPool
    {
        private readonly IObjectPool<FloatingTextView> _pool;
        private readonly FloatingTextView _prefab;
        private readonly Transform _parent;

        public FloatingTextPool(FloatingTextView prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<FloatingTextView>(
                CreateText,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyPooledText,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public FloatingTextView Get()
        {
            return _pool.Get();
        }

        public void Release(FloatingTextView view)
        {
            _pool.Release(view);
        }

        private FloatingTextView CreateText()
        {
            return UnityEngine.Object.Instantiate(_prefab, _parent);
        }

        private void OnGetFromPool(FloatingTextView view)
        {
            view.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(FloatingTextView view)
        {
            view.ResetVisualState();
            view.gameObject.SetActive(false);
        }

        private void OnDestroyPooledText(FloatingTextView view)
        {
            UnityEngine.Object.Destroy(view.gameObject);
        }
    }
}
