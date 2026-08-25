using Match3.Data;
using Match3.Model;

namespace Match3.Matching
{
    /// <summary>
    /// Scans a board for runs of same-type tiles that meet the minimum
    /// match length. Kept behind an interface so the algorithm can be
    /// swapped or mocked independently of everything that consumes it
    /// (swap validation, cascade resolution, deadlock detection).
    /// </summary>
    public interface IMatchFinder
    {
        MatchSearchResult FindMatches(BoardModel board, BoardConfig config);
    }
}
