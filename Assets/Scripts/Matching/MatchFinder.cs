using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Model;

namespace Match3.Matching
{
    /// <summary>
    /// Default <see cref="IMatchFinder"/> implementation. Scans every row
    /// and every column once, looking for runs of identical TypeIds using
    /// a single shared run-length scan (<see cref="FindLineMatches"/>) so
    /// the horizontal and vertical cases do not duplicate logic.
    /// </summary>
    public sealed class MatchFinder : IMatchFinder
    {
        public MatchSearchResult FindMatches(BoardModel board, BoardConfig config)
        {
            List<MatchGroup> groups = new List<MatchGroup>();
            groups.AddRange(FindLineMatches(board, config, isHorizontal: true));
            groups.AddRange(FindLineMatches(board, config, isHorizontal: false));
            return new MatchSearchResult(groups);
        }

        /// <summary>
        /// Walks every line (row when horizontal, column when vertical)
        /// and records each run whose length reaches the configured
        /// minimum. An extra sentinel step past the last cell flushes the
        /// final run without special-casing the loop boundary.
        /// </summary>
        private List<MatchGroup> FindLineMatches(BoardModel board, BoardConfig config, bool isHorizontal)
        {
            List<MatchGroup> matches = new List<MatchGroup>();
            int outerCount = isHorizontal ? board.Height : board.Width;
            int innerCount = isHorizontal ? board.Width : board.Height;

            for (int outer = 0; outer < outerCount; outer++)
            {
                int runStart = 0;
                int runTypeId = int.MinValue;
                int runLength = 0;

                for (int inner = 0; inner <= innerCount; inner++)
                {
                    Tile tile = inner < innerCount ? GetTileAt(board, isHorizontal, outer, inner) : null;
                    bool continuesRun = tile != null && tile.TypeId == runTypeId;

                    if (continuesRun)
                    {
                        runLength++;
                        continue;
                    }

                    if (runLength >= config.MinMatchLength)
                    {
                        matches.Add(BuildMatchGroup(isHorizontal, outer, runStart, runLength, runTypeId));
                    }

                    runStart = inner;
                    runTypeId = tile?.TypeId ?? int.MinValue;
                    runLength = tile != null ? 1 : 0;
                }
            }

            return matches;
        }

        private static Tile GetTileAt(BoardModel board, bool isHorizontal, int outer, int inner)
        {
            return isHorizontal ? board.GetTile(inner, outer) : board.GetTile(outer, inner);
        }

        private static MatchGroup BuildMatchGroup(bool isHorizontal, int outer, int runStart, int runLength, int typeId)
        {
            List<Vector2Int> positions = new List<Vector2Int>(runLength);
            for (int i = 0; i < runLength; i++)
            {
                int inner = runStart + i;
                positions.Add(isHorizontal ? new Vector2Int(inner, outer) : new Vector2Int(outer, inner));
            }

            MatchOrientation orientation = isHorizontal ? MatchOrientation.Horizontal : MatchOrientation.Vertical;
            return new MatchGroup(positions, typeId, orientation);
        }
    }
}
