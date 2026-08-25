using UnityEngine;

namespace Match3.Battle.Data
{
    public enum AttackType
    {
        Melee,
        Ranged
    }

    /// <summary>
    /// A character's starting stats and attack behavior. Player and Enemy
    /// each get their own asset, so the two sides can be balanced
    /// independently. All values are plain Inspector fields (not
    /// hardcoded) per the design doc's "must be easy to rebalance" rule.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Match3/Battle/Character Config")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Character";
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;

        [Header("Core Stats")]
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private float _swordrainDamage = 10f;
        [SerializeField] private float _slashDamage = 10f;
        [SerializeField] private float _maxMana = 100f;

        [Header("VHP")]
        [Tooltip("Automatically clamped to at most MaxHP / 2.")]
        [SerializeField] private float _maxVhp = 50f;

        [Header("Attack")]
        [SerializeField] private AttackType _attackType = AttackType.Melee;
        [Tooltip("Number of projectiles fired by a Ranged Slash attack. Unused for Melee.")]
        [SerializeField] [Min(1)] private int _rangedObjectCount = 3;

        public string DisplayName => _displayName;
        public Sprite Sprite => _sprite;
        public Color Color => _color;
        public float MaxHp => _maxHp;
        public float SwordrainDamage => _swordrainDamage;
        public float SlashDamage => _slashDamage;
        public float MaxMana => _maxMana;
        public float MaxVhp => _maxVhp;
        public AttackType AttackType => _attackType;
        public int RangedObjectCount => _rangedObjectCount;

        private void OnValidate()
        {
            float maxAllowedVhp = _maxHp * 0.5f;
            if (_maxVhp > maxAllowedVhp)
            {
                _maxVhp = maxAllowedVhp;
            }
        }
    }
}
