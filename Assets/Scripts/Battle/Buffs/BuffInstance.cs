using Match3.Battle.Model;

namespace Match3.Battle.Buffs
{
    /// <summary>
    /// One active application of a <see cref="BuffDefinition"/> on a
    /// character. Tracks remaining duration and handles what happens
    /// when the same definition is applied again (per its
    /// <see cref="BuffStackingBehavior"/>).
    /// </summary>
    public sealed class BuffInstance
    {
        private int _remainingCount;

        public BuffDefinition Definition { get; }
        public CharacterState Source { get; }
        public int StackCount { get; private set; }
        public bool IsExpired { get; private set; }

        public BuffInstance(BuffDefinition definition, CharacterState source)
        {
            Definition = definition;
            Source = source;
            _remainingCount = definition.GetDuration().Count;
            StackCount = 1;
        }

        /// <summary>Advances Turn/Instant duration by one. No-op for Cycle/Permanence/Condition.</summary>
        public void TickTurn()
        {
            BuffDurationType type = Definition.GetDuration().Type;
            if (type != BuffDurationType.Turn && type != BuffDurationType.Instant)
            {
                return;
            }
            DecrementAndCheckExpiry();
        }

        /// <summary>Advances Cycle duration by one. No-op for every other duration type.</summary>
        public void TickCycle()
        {
            if (Definition.GetDuration().Type != BuffDurationType.Cycle)
            {
                return;
            }
            DecrementAndCheckExpiry();
        }

        /// <summary>Marks a Condition-duration buff as ended. Callers own deciding when their condition is met.</summary>
        public void MarkConditionEnded()
        {
            IsExpired = true;
        }

        /// <summary>Handles the same <see cref="Definition"/> being applied again while this instance is still active.</summary>
        public void Reapply()
        {
            switch (Definition.Stacking)
            {
                case BuffStackingBehavior.Stack:
                    _remainingCount += Definition.GetDuration().Count;
                    if (!Definition.HasStackLimit || StackCount < Definition.MaxStacks)
                    {
                        StackCount++;
                    }
                    break;
                case BuffStackingBehavior.Override:
                    _remainingCount = Definition.GetDuration().Count;
                    break;
                case BuffStackingBehavior.Waiting:
                    // Ignored entirely until the current instance ends.
                    break;
            }
        }

        private void DecrementAndCheckExpiry()
        {
            _remainingCount--;
            if (_remainingCount <= 0)
            {
                IsExpired = true;
            }
        }
    }
}
