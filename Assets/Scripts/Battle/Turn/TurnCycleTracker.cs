using System;
using Match3.Battle.Model;

namespace Match3.Battle.Turn
{
    /// <summary>
    /// Tracks whose turn it is and how many turns/cycles have elapsed.
    /// Extra moves earned from 4+ matches are banked in a counter rather
    /// than a flag: earning 2 in one move grants 2 additional moves (not
    /// 1), and each still-qualifying follow-up move can add more on top,
    /// chaining for as long as the side keeps matching 4+.
    ///
    /// A Cycle completes when control finishes passing through both
    /// sides and returns to whichever side moves first — not on every
    /// individual move, so a side chaining several extra moves only
    /// closes out the cycle once its whole turn (banked moves included)
    /// actually ends.
    /// </summary>
    public sealed class TurnCycleTracker
    {
        private readonly BattleSide _firstSide;
        private int _extraMovesRemaining;

        public BattleSide CurrentSide { get; private set; }
        public int TurnCount { get; private set; }
        public int CycleCount { get; private set; }
        public int ExtraMovesRemaining => _extraMovesRemaining;

        public event Action<BattleSide> SideChanged;
        public event Action<int> CycleCompleted;

        public TurnCycleTracker(BattleSide firstSide)
        {
            _firstSide = firstSide;
            CurrentSide = firstSide;
        }

        /// <summary>
        /// Call once per completed move (a full swap-and-cascade resolve).
        /// Always advances <see cref="TurnCount"/>. <paramref name="extraMovesEarned"/>
        /// (0 if none) is added to the current side's banked extra-move
        /// count; if any remain banked after this move, one is spent and
        /// the same side goes again — otherwise the turn passes.
        /// </summary>
        public void CompleteMove(int extraMovesEarned)
        {
            TurnCount++;
            _extraMovesRemaining += Math.Max(extraMovesEarned, 0);

            if (_extraMovesRemaining > 0)
            {
                _extraMovesRemaining--;
                return;
            }

            AdvanceToOtherSide();
        }

        /// <summary>
        /// Passes the turn to the other side without counting it as a
        /// completed move, and clears any banked extra moves — used when
        /// a side forfeits by timing out, or a skill with a full-board
        /// destroy area forces an immediate turn change. <see cref="TurnCount"/>
        /// only increases for actual moves, per the design doc's
        /// definition of a Turn.
        /// </summary>
        public void ForceAdvanceTurn()
        {
            _extraMovesRemaining = 0;
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
