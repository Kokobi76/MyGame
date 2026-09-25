using System;
using UnityEngine;

namespace Match3.Battle.Buffs
{
    /// <summary>Which Battle Stat a Normal Buff/Debuff modifies. Extend this as more stats need buffing.</summary>
    public enum ModifiableStat
    {
        SlashDamage,
        SwordrainDamage
    }

    /// <summary>The design doc's "Sign" column: +, -, *, / applied with either a flat number or a percentage of the running stat value.</summary>
    public enum ModifierOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>
    /// One stat modification a Buff/Debuff applies. When several
    /// modifiers target the same stat, they're applied in sequence, each
    /// transforming the result of the previous one — see
    /// <see cref="BuffManager.GetModifiedStat"/>.
    /// </summary>
    [Serializable]
    public sealed class StatModifier
    {
        [SerializeField] private ModifiableStat _stat;
        [SerializeField] private ModifierOperation _operation;
        [SerializeField] private float _value;
        [Tooltip("Checked: Value is a percentage of the running value at this point (e.g. 20 = +20%). Unchecked: Value is a flat number.")]
        [SerializeField] private bool _isPercentage;

        public ModifiableStat Stat => _stat;
        public ModifierOperation Operation => _operation;
        public float Value => _value;
        public bool IsPercentage => _isPercentage;

        /// <summary>Applies this modifier to a running value, returning the new running value.</summary>
        public float Apply(float runningValue)
        {
            float amount = _isPercentage ? runningValue * (_value / 100f) : _value;

            switch (_operation)
            {
                case ModifierOperation.Add:
                    return runningValue + amount;
                case ModifierOperation.Subtract:
                    return runningValue - amount;
                case ModifierOperation.Multiply:
                    float multiplier = _isPercentage ? (_value / 100f) : _value;
                    return runningValue * multiplier;
                case ModifierOperation.Divide:
                    float divisor = _isPercentage ? (_value / 100f) : _value;
                    return SafeDivide(runningValue, divisor);
                default:
                    return runningValue;
            }
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }
    }
}
