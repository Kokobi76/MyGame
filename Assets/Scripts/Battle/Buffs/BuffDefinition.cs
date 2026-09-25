using System.Collections.Generic;
using UnityEngine;

namespace Match3.Battle.Buffs
{
    public enum BuffCategory
    {
        /// <summary>Control Buff — a fixed, hardcoded effect (see <see cref="ControlBuffType"/>), not configurable stat modifiers.</summary>
        Effect,

        /// <summary>Normal Buff/Debuff — modifies Battle Stats via <see cref="StatModifier"/>s.</summary>
        Buff
    }

    public enum BuffTarget
    {
        Self,
        Opponent
    }

    /// <summary>
    /// What happens when this buff is applied again while an instance of
    /// it is already active on the same target.
    /// </summary>
    public enum BuffStackingBehavior
    {
        /// <summary>Adds the new application's duration to whatever remains.</summary>
        Stack,
        /// <summary>Replaces the remaining duration with the new application's.</summary>
        Override,
        /// <summary>Ignored entirely until the current instance ends.</summary>
        Waiting
    }

    /// <summary>
    /// The designer-facing "recipe" for one buff — everything needed to
    /// create a <see cref="BuffInstance"/> when it's applied. One asset
    /// per buff (e.g. "AttackBoost", "Stunned", "ShieldedFromDebuffs").
    /// </summary>
    [CreateAssetMenu(fileName = "BuffDefinition", menuName = "Match3/Battle/Buff Definition")]
    public sealed class BuffDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Buff";
        [SerializeField] private BuffCategory _category = BuffCategory.Buff;
        [SerializeField] private BuffTarget _target = BuffTarget.Self;
        [Tooltip("Positive (Buff) or Negative (Debuff/Control effect). Drives Invincible's \"clear all negative buffs\" and future UI coloring.")]
        [SerializeField] private bool _isNegative;

        [Header("Duration")]
        [SerializeField] private BuffDurationType _durationType = BuffDurationType.Instant;
        [Tooltip("Only used when Duration Type is Turn or Cycle.")]
        [SerializeField] [Min(1)] private int _durationCount = 1;

        [Header("Stacking")]
        [SerializeField] private BuffStackingBehavior _stacking = BuffStackingBehavior.Override;
        [SerializeField] private bool _hasStackLimit;
        [Tooltip("Only used when Has Stack Limit is checked.")]
        [SerializeField] [Min(1)] private int _maxStacks = 1;

        [Header("Effect (Control Buff) — only used when Category = Effect")]
        [SerializeField] private ControlBuffType _controlType;

        [Header("Stat Modifiers (Normal Buff) — only used when Category = Buff")]
        [SerializeField] private List<StatModifier> _statModifiers = new List<StatModifier>();

        public string DisplayName => _displayName;
        public BuffCategory Category => _category;
        public BuffTarget Target => _target;
        public bool IsNegative => _isNegative;
        public BuffStackingBehavior Stacking => _stacking;
        public bool HasStackLimit => _hasStackLimit;
        public int MaxStacks => _maxStacks;
        public ControlBuffType ControlType => _controlType;
        public IReadOnlyList<StatModifier> StatModifiers => _statModifiers;

        public BuffDuration GetDuration()
        {
            switch (_durationType)
            {
                case BuffDurationType.Turn:
                    return BuffDuration.Turns(_durationCount);
                case BuffDurationType.Cycle:
                    return BuffDuration.Cycles(_durationCount);
                case BuffDurationType.Permanence:
                    return BuffDuration.Permanent();
                case BuffDurationType.Condition:
                    return BuffDuration.UntilCondition();
                case BuffDurationType.Instant:
                default:
                    return BuffDuration.Instant();
            }
        }
    }
}
