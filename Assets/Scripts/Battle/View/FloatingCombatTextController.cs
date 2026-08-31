using UnityEngine;

namespace Match3.Battle.View
{
    public enum FloatingTextKind
    {
        Damage,
        HpHeal,
        VhpHeal,
        ManaGain,
        ShieldGain,
        ShieldBlock
    }

    /// <summary>
    /// Spawns pooled floating combat text (damage taken, HP/VHP healed,
    /// Mana gained, Shield gained or consumed-to-block) at a world
    /// position, each kind with its own color and label format. Purely
    /// cosmetic and non-blocking — callers fire-and-forget this; nothing
    /// in gameplay waits on it.
    /// </summary>
    public sealed class FloatingCombatTextController : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private FloatingTextView _textPrefab;
        [SerializeField] private Transform _textContainer;
        [SerializeField] private int _poolDefaultCapacity = 16;
        [SerializeField] private int _poolMaxSize = 64;

        [Header("Per-Kind Color")]
        [SerializeField] private Color _damageColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _hpHealColor = new Color(0.3f, 0.85f, 0.3f);
        [SerializeField] private Color _vhpHealColor = new Color(0.4f, 0.75f, 1f);
        [SerializeField] private Color _manaGainColor = new Color(0.65f, 0.45f, 1f);
        [SerializeField] private Color _shieldGainColor = new Color(0.95f, 0.85f, 0.2f);
        [SerializeField] private Color _shieldBlockColor = new Color(0.7f, 0.7f, 0.7f);

        private FloatingTextPool _pool;

        private void Awake()
        {
            EnsureTextContainer();
            _pool = new FloatingTextPool(_textPrefab, _textContainer, _poolDefaultCapacity, _poolMaxSize);
        }

        public void Spawn(Vector3 worldPosition, FloatingTextKind kind, int amount)
        {
            FloatingTextView view = _pool.Get();
            view.transform.position = worldPosition;
            view.Play(BuildLabel(kind, amount), GetColor(kind), () => _pool.Release(view));
        }

        private static string BuildLabel(FloatingTextKind kind, int amount)
        {
            switch (kind)
            {
                case FloatingTextKind.Damage:
                    return $"-{amount}";
                case FloatingTextKind.HpHeal:
                    return $"+{amount}";
                case FloatingTextKind.VhpHeal:
                    return $"+{amount} VHP";
                case FloatingTextKind.ManaGain:
                    return $"+{amount} MP";
                case FloatingTextKind.ShieldGain:
                    return $"+{amount} Shield";
                case FloatingTextKind.ShieldBlock:
                    return $"-{amount} Shield";
                default:
                    return amount.ToString();
            }
        }

        private Color GetColor(FloatingTextKind kind)
        {
            switch (kind)
            {
                case FloatingTextKind.Damage:
                    return _damageColor;
                case FloatingTextKind.HpHeal:
                    return _hpHealColor;
                case FloatingTextKind.VhpHeal:
                    return _vhpHealColor;
                case FloatingTextKind.ManaGain:
                    return _manaGainColor;
                case FloatingTextKind.ShieldGain:
                    return _shieldGainColor;
                case FloatingTextKind.ShieldBlock:
                    return _shieldBlockColor;
                default:
                    return Color.white;
            }
        }

        private void EnsureTextContainer()
        {
            if (_textContainer != null)
            {
                return;
            }

            GameObject containerObject = new GameObject("FloatingTexts");
            containerObject.transform.SetParent(transform);
            containerObject.transform.localPosition = Vector3.zero;
            _textContainer = containerObject.transform;
        }
    }
}
