using UnityEngine;

namespace Match3.Battle.Data
{
    public enum AttackType
    {
        Melee,
        Ranged
    }

    /// <summary>
    /// A character's Primal Stats and attack behavior. Player and Enemy
    /// each get their own asset. Main Stats (HP, Attack, Slash Damage,
    /// etc.) are no longer set directly here — they're derived from
    /// these Primal Stats via <see cref="StatDerivationConfig"/> when a
    /// <see cref="Match3.Battle.Model.CharacterState"/> is created, per
    /// the design doc's Primal Stats -&gt; Main Stats -&gt; Battle Stats
    /// hierarchy.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Match3/Battle/Character Config")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Character";
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;

        [Header("Primal Stats")]
        [Tooltip("Thể lực — feeds Max HP (and, through it, Max VHP).")]
        [SerializeField] [Min(0)] private int _endurance = 10;
        [Tooltip("Sức mạnh — feeds Attack, which feeds Slash Damage.")]
        [SerializeField] [Min(0)] private int _strength = 10;
        [Tooltip("Trí lực — feeds Magic Attack, which feeds Swordrain Damage.")]
        [SerializeField] [Min(0)] private int _intelligence = 10;
        [Tooltip("Nhanh nhẹn — reserved for future use; the design doc has not defined a Main Stat branch for it yet.")]
        [SerializeField] [Min(0)] private int _dexterity = 10;

        [Header("Resources")]
        [SerializeField] private float _maxMana = 100f;

        [Header("Attack")]
        [SerializeField] private AttackType _attackType = AttackType.Melee;
        [Tooltip("Number of projectiles fired by a Ranged Slash attack. Unused for Melee.")]
        [SerializeField] [Min(1)] private int _rangedObjectCount = 3;

        public string DisplayName => _displayName;
        public Sprite Sprite => _sprite;
        public Color Color => _color;
        public int Endurance => _endurance;
        public int Strength => _strength;
        public int Intelligence => _intelligence;
        public int Dexterity => _dexterity;
        public float MaxMana => _maxMana;
        public AttackType AttackType => _attackType;
        public int RangedObjectCount => _rangedObjectCount;
    }
}
