using UnityEngine;

namespace Match3.Battle.Data
{
    /// <summary>
    /// Formulas for deriving Main Stats (HP, Attack, Magic Attack, Slash
    /// Damage, Swordrain Damage, Max VHP) from a character's Primal Stats
    /// (Endurance, Strength, Intelligence). Shared/global — one asset for
    /// every character, so all characters scale the same way and stay
    /// comparable as Primal Stats change.
    ///
    /// The design doc specifies the stat hierarchy (Endurance -&gt; HP,
    /// Strength -&gt; Attack -&gt; Slash Damage, Intelligence -&gt; Magic Attack
    /// -&gt; Swordrain Damage) but leaves the exact formulas open. This uses
    /// a simple, standard RPG shape — a flat base plus a linear
    /// per-point scalar — since it's easy to reason about and rebalance
    /// from the Inspector without touching code. Dexterity has no Main
    /// Stat branch defined in the doc yet, so it isn't consumed here.
    /// </summary>
    [CreateAssetMenu(fileName = "StatDerivationConfig", menuName = "Match3/Battle/Stat Derivation Config")]
    public sealed class StatDerivationConfig : ScriptableObject
    {
        [Header("Max HP (from Endurance)")]
        [SerializeField] private float _baseHp = 50f;
        [SerializeField] private float _hpPerEndurance = 10f;

        [Header("Attack (from Strength)")]
        [SerializeField] private float _baseAttack = 5f;
        [SerializeField] private float _attackPerStrength = 2f;

        [Header("Magic Attack (from Intelligence)")]
        [SerializeField] private float _baseMagicAttack = 5f;
        [SerializeField] private float _magicAttackPerIntelligence = 2f;

        [Header("Slash Damage (from Attack)")]
        [Tooltip("Slash Damage = Attack * this%. 100 means Slash Damage equals Attack exactly.")]
        [SerializeField] private float _slashDamageFromAttackPercent = 100f;

        [Header("Swordrain Damage (from Magic Attack)")]
        [Tooltip("Swordrain Damage = Magic Attack * this%. 100 means Swordrain Damage equals Magic Attack exactly.")]
        [SerializeField] private float _swordrainDamageFromMagicAttackPercent = 100f;

        [Header("Max VHP (from Max HP)")]
        [Tooltip("Max VHP = Max HP * this%. Capped at 50 to match the design doc's Max VHP <= Max HP / 2 rule.")]
        [SerializeField] [Range(0f, 50f)] private float _maxVhpFromMaxHpPercent = 50f;

        public float CalculateMaxHp(int endurance)
        {
            return _baseHp + (endurance * _hpPerEndurance);
        }

        public float CalculateAttack(int strength)
        {
            return _baseAttack + (strength * _attackPerStrength);
        }

        public float CalculateMagicAttack(int intelligence)
        {
            return _baseMagicAttack + (intelligence * _magicAttackPerIntelligence);
        }

        public float CalculateSlashDamage(float attack)
        {
            return attack * (_slashDamageFromAttackPercent / 100f);
        }

        public float CalculateSwordrainDamage(float magicAttack)
        {
            return magicAttack * (_swordrainDamageFromMagicAttackPercent / 100f);
        }

        public float CalculateMaxVhp(float maxHp)
        {
            return maxHp * (_maxVhpFromMaxHpPercent / 100f);
        }
    }
}
