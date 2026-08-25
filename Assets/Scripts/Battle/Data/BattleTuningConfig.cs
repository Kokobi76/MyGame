using UnityEngine;

namespace Match3.Battle.Data
{
    /// <summary>
    /// Every tunable number in the Tile Resolve formulas, in one place so
    /// balancing never requires touching code. Percent fields are stored
    /// as plain numbers (5 = 5%), matching how the design doc writes
    /// them, and divided by 100 where they're applied.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleTuningConfig", menuName = "Match3/Battle/Battle Tuning Config")]
    public sealed class BattleTuningConfig : ScriptableObject
    {
        [Header("Heal / Resource (per matched tile)")]
        [Tooltip("HP tile: Current HP += this% of MaxHP, per tile matched.")]
        [SerializeField] private float _hpHealPercentPerTile = 0.3f;
        [Tooltip("VHP tile: Current VHP += this% of MaxVHP, per tile matched.")]
        [SerializeField] private float _vhpHealPercentPerTile = 0.6f;

        [Header("Damage (per matched tile)")]
        [Tooltip("Ranged Slash total damage = SlashDamage * n * this%. Melee Slash ignores this and deals SlashDamage * n directly.")]
        [SerializeField] private float _slashRangedDamagePercentPerTile = 5f;
        [Tooltip("Sword damage = SwordrainDamage * n * this%.")]
        [SerializeField] private float _swordDamagePercentPerTile = 5f;

        [Header("Sword-Triggered Slash Proc")]
        [Tooltip("Sword tiles also trigger a bonus Slash hit (sized by n Sword tiles matched) dealt at this% of its normal damage.")]
        [SerializeField] [Range(0f, 100f)] private float _swordTriggeredSlashDamagePercent = 50f;

        [Header("Shield")]
        [Tooltip("Shield Count needed to convert into 1 Shield Stack.")]
        [SerializeField] [Min(1)] private int _shieldCountPerStack = 2;

        [Header("Extra Turn")]
        [Tooltip("A matched run at or above this length grants the acting side another move.")]
        [SerializeField] private int _extraTurnMatchLength = 4;

        public float HpHealPercentPerTile => _hpHealPercentPerTile;
        public float VhpHealPercentPerTile => _vhpHealPercentPerTile;
        public float SlashRangedDamagePercentPerTile => _slashRangedDamagePercentPerTile;
        public float SwordDamagePercentPerTile => _swordDamagePercentPerTile;
        public float SwordTriggeredSlashDamagePercent => _swordTriggeredSlashDamagePercent;
        public int ShieldCountPerStack => _shieldCountPerStack;
        public int ExtraTurnMatchLength => _extraTurnMatchLength;
    }
}
