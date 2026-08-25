using System.Collections.Generic;
using UnityEngine;

namespace Match3.Matching
{
    /// <summary>
    /// The full outcome of one <see cref="IMatchFinder.FindMatches"/> pass:
    /// every individual matched run, plus a flattened, de-duplicated set
    /// of positions (an L or T shaped match shares a corner tile between
    /// two groups, so callers that only care about "which cells clear"
    /// should use <see cref="AllMatchedPositions"/>).
    /// </summary>
    public sealed class MatchSearchResult
    {
        public IReadOnlyList<MatchGroup> Groups { get; }
        public IReadOnlyCollection<Vector2Int> AllMatchedPositions { get; }
        public bool HasMatches => Groups.Count > 0;

        public MatchSearchResult(IReadOnlyList<MatchGroup> groups)
        {
            Groups = groups;
            AllMatchedPositions = BuildAllMatchedPositions(groups);
        }

        private static HashSet<Vector2Int> BuildAllMatchedPositions(IReadOnlyList<MatchGroup> groups)
        {
            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
            foreach (MatchGroup group in groups)
            {
                foreach (Vector2Int position in group.Positions)
                {
                    positions.Add(position);
                }
            }
            return positions;
        }
    }
}
