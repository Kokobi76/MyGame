using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Model;
using Match3.Matching;

namespace Match3.Generation
{
    /// <summary>
    /// Default <see cref="IBoardGenerator"/> implementation.
    /// </summary>
    public sealed class BoardGenerator : IBoardGenerator
    {
        private const int MaxShuffleAttempts = 50;

        private readonly TileTypeRandomizer _randomizer;
        private readonly IMatchFinder _matchFinder;

        public BoardGenerator(TileTypeRandomizer randomizer, IMatchFinder matchFinder)
        {
            _randomizer = randomizer;
            _matchFinder = matchFinder;
        }

        /// <summary>
        /// Fills the board left-to-right, bottom-to-top. For every cell it
        /// excludes whichever type would immediately complete a 3-run with
        /// the two neighbors to the left and/or below, which guarantees a
        /// match-free board in a single O(width*height) pass with no
        /// retries needed.
        /// </summary>
        public void GenerateInitialBoard(BoardModel board, BoardConfig config)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    HashSet<int> excludedTypeIds = GetExcludedTypeIds(board, x, y);
                    int typeId = _randomizer.GetRandomTypeId(config, excludedTypeIds);
                    board.SetTile(x, y, new Tile(typeId));
                }
            }
        }

        /// <summary>
        /// Repeatedly reshuffles the existing tile types (Fisher-Yates)
        /// until a match-free arrangement is found, up to
        /// <see cref="MaxShuffleAttempts"/>. Falls back to a full,
        /// guaranteed-valid regeneration if it never converges, which in
        /// practice only happens with extremely low tile-type variety.
        /// </summary>
        public void ShuffleBoard(BoardModel board, BoardConfig config)
        {
            List<Vector2Int> filledPositions = GetFilledPositions(board);
            List<int> typeIds = GetTypeIds(board, filledPositions);

            for (int attempt = 0; attempt < MaxShuffleAttempts; attempt++)
            {
                ShuffleInPlace(typeIds);
                ApplyTypeIds(board, filledPositions, typeIds);

                if (!_matchFinder.FindMatches(board, config).HasMatches)
                {
                    return;
                }
            }

            GenerateInitialBoard(board, config);
        }

        private static HashSet<int> GetExcludedTypeIds(BoardModel board, int x, int y)
        {
            HashSet<int> excludedTypeIds = new HashSet<int>();

            if (x >= 2)
            {
                Tile left1 = board.GetTile(x - 1, y);
                Tile left2 = board.GetTile(x - 2, y);
                if (left1 != null && left2 != null && left1.TypeId == left2.TypeId)
                {
                    excludedTypeIds.Add(left1.TypeId);
                }
            }

            if (y >= 2)
            {
                Tile down1 = board.GetTile(x, y - 1);
                Tile down2 = board.GetTile(x, y - 2);
                if (down1 != null && down2 != null && down1.TypeId == down2.TypeId)
                {
                    excludedTypeIds.Add(down1.TypeId);
                }
            }

            return excludedTypeIds;
        }

        private static List<Vector2Int> GetFilledPositions(BoardModel board)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!board.IsEmpty(position))
                    {
                        positions.Add(position);
                    }
                }
            }
            return positions;
        }

        private static List<int> GetTypeIds(BoardModel board, List<Vector2Int> positions)
        {
            List<int> typeIds = new List<int>(positions.Count);
            foreach (Vector2Int position in positions)
            {
                typeIds.Add(board.GetTile(position).TypeId);
            }
            return typeIds;
        }

        private static void ApplyTypeIds(BoardModel board, List<Vector2Int> positions, List<int> typeIds)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                board.SetTile(positions[i], new Tile(typeIds[i]));
            }
        }

        private static void ShuffleInPlace(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }
    }
}
