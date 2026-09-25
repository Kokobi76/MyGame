using UnityEngine;

namespace Match3.Battle.Buffs
{
    /// <summary>
    /// How a buff's lifetime is measured. Turn/Cycle carry a count;
    /// Instant is shorthand for "1 Turn"; Permanence never expires on its
    /// own; Condition expires only when some external check says so
    /// (explicitly not Turn/Cycle based, per the design doc) — callers
    /// query <see cref="BuffInstance.HasConditionEnded"/> and clear it
    /// themselves once their own condition is satisfied.
    /// </summary>
    public enum BuffDurationType
    {
        Turn,
        Cycle,
        Instant,
        Permanence,
        Condition
    }

    /// <summary>A buff's configured lifetime — see <see cref="BuffDurationType"/> for what each shape means.</summary>
    public readonly struct BuffDuration
    {
        public BuffDurationType Type { get; }
        public int Count { get; }

        private BuffDuration(BuffDurationType type, int count)
        {
            Type = type;
            Count = count;
        }

        public static BuffDuration Turns(int count)
        {
            return new BuffDuration(BuffDurationType.Turn, Mathf.Max(count, 1));
        }

        public static BuffDuration Cycles(int count)
        {
            return new BuffDuration(BuffDurationType.Cycle, Mathf.Max(count, 1));
        }

        public static BuffDuration Instant()
        {
            return new BuffDuration(BuffDurationType.Instant, 1);
        }

        public static BuffDuration Permanent()
        {
            return new BuffDuration(BuffDurationType.Permanence, 0);
        }

        public static BuffDuration UntilCondition()
        {
            return new BuffDuration(BuffDurationType.Condition, 0);
        }
    }
}
