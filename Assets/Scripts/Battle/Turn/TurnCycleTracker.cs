using System;
using Match3.Battle.Model;

namespace Match3.Battle.Turn
{
    /// <summary>
    /// Tracks whose turn it is and how many turns/cycles have elapsed.
    /// A Cycle completes when control finishes passing through both
    /// sides and returns to whichever side moves first — not on every
    /// individual move, so a side chaining several extra moves (from
    /// 4+ matches) only closes out the cycle once its whole turn ends.
    /// </summary>
    public sealed class TurnCycleTracker
    {
        private readonly BattleSide _firstSide;

        public BattleSide CurrentSide { get; private set; }
        public int TurnCount { get; private set; }
        public int CycleCount { get; private set; }

        public event Action<BattleSide> SideChanged;
        public event Action<int> CycleCompleted;

        public TurnCycleTracker(BattleSide firstSide)
        {
            _firstSide = firstSide;
            CurrentSide = firstSide;
        }

        /// <summary>
        /// Call once per completed move (a full swap-and-cascade resolve).
        /// Always advances <see cref="TurnCount"/>. Passes the turn to the
        /// other side unless <paramref name="earnedExtraMove"/> is true,
        /// in which case the same side goes again.
        /// </summary>
        public void CompleteMove(bool earnedExtraMove)
        {
            TurnCount++;

            if (!earnedExtraMove)
            {
                AdvanceToOtherSide();
            }
        }

        /// <summary>
        /// Passes the turn to the other side without counting it as a
        /// completed move — used when a side forfeits by timing out.
        /// <see cref="TurnCount"/> only increases for actual moves, per
        /// the design doc's definition of a Turn.
        /// </summary>
        public void ForceAdvanceTurn()
        {
            AdvanceToOtherSide();
        }

        private void AdvanceToOtherSide()
        {
            bool isLastSideOfCycle = CurrentSide != _firstSide;
            CurrentSide = CurrentSide.GetOpposite();
            OnSideChanged(CurrentSide);

            if (isLastSideOfCycle)
            {
                CycleCount++;
                OnCycleCompleted(CycleCount);
            }
        }

        private void OnSideChanged(BattleSide side)
        {
            SideChanged?.Invoke(side);
        }

        private void OnCycleCompleted(int cycleCount)
        {
            CycleCompleted?.Invoke(cycleCount);
        }
    }
}
