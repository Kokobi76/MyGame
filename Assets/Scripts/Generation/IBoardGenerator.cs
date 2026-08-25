using Match3.Data;
using Match3.Model;

namespace Match3.Generation
{
    /// <summary>
    /// Populates a <see cref="BoardModel"/> with tiles. Separated from
    /// <see cref="Match3.Collapse.ICollapseResolver"/> because generation
    /// must guarantee a match-free starting board, while mid-game refills
    /// deliberately allow cascades.
    /// </summary>
    public interface IBoardGenerator
    {
        /// <summary>Fills every cell of an empty board with no pre-existing matches.</summary>
        void GenerateInitialBoard(BoardModel board, BoardConfig config);

        /// <summary>Reorders the existing tiles in place, without changing their multiset, until no match remains.</summary>
        void ShuffleBoard(BoardModel board, BoardConfig config);
    }
}
