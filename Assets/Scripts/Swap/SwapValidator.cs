using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Model;
using Match3.Matching;

namespace Match3.Swap
{
    /// <summary>
    /// Default <see cref="ISwapValidator"/> implementation. Depends on
    /// <see cref="IMatchFinder"/> to answer "would this swap create a
    /// match" by performing the swap on the model, checking, then
    /// swapping back — cheap for typical board sizes and always correct
    /// since it reuses the exact same matching rules as the rest of the
    /// game.
    /// </summary>
    public sealed class SwapValidator : ISwapValidator
    {
        private readonly IMatchFinder _matchFinder;

        public SwapValidator(IMatchFinder matchFinder)
        {
            _matchFinder = matchFinder;
        }

        public bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            int manhattanDistance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            return manhattanDistance == 1;
        }

        /// <summary>
        /// Checks every cell's right and up neighbor, which together
        /// cover every adjacent pair on the board exactly once.
        /// </summary>
        public bool HasAnyValidMove(BoardModel board, BoardConfig config)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int current = new Vector2Int(x, y);

                    if (TrySwapCreatesMatch(board, config, current, current + Vector2Int.right))
                    {
                        return true;
                    }
                    if (TrySwapCreatesMatch(board, config, current, current + Vector2Int.up))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Same scan pattern as <see cref="HasAnyValidMove"/>, collecting every hit (with its best match length) instead of stopping at the first.</summary>
        public IReadOnlyList<EvaluatedMove> GetAllValidMoves(BoardModel board, BoardConfig config)
        {
            List<EvaluatedMove> validMoves = new List<EvaluatedMove>();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int current = new Vector2Int(x, y);
                    Vector2Int rightNeighbor = current + Vector2Int.right;
                    Vector2Int upNeighbor = current + Vector2Int.up;

                    if (TryEvaluateSwap(board, config, current, rightNeighbor, out int rightLength))
                    {
                        validMoves.Add(new EvaluatedMove(new SwapMove(current, rightNeighbor), rightLength));
                    }
                    if (TryEvaluateSwap(board, config, current, upNeighbor, out int upLength))
                    {
                        validMoves.Add(new EvaluatedMove(new SwapMove(current, upNeighbor), upLength));
                    }
                }
            }

            return validMoves;
        }

        private bool TrySwapCreatesMatch(BoardModel board, BoardConfig config, Vector2Int a, Vector2Int b)
        {
            return TryEvaluateSwap(board, config, a, b, out _);
        }

        /// <summary>Swaps, checks, swaps back — reporting both whether a match resulted and the longest single group found, in one pass.</summary>
        private bool TryEvaluateSwap(BoardModel board, BoardConfig config, Vector2Int a, Vector2Int b, out int bestMatchLength)
        {
            bestMatchLength = 0;
            if (!board.IsWithinBounds(a) || !board.IsWithinBounds(b))
            {
                return false;
            }

            board.SwapTiles(a, b);
            MatchSearchResult matchResult = _matchFinder.FindMatches(board, config);
            board.SwapTiles(a, b);

            if (!matchResult.HasMatches)
            {
                return false;
            }

            foreach (MatchGroup group in matchResult.Groups)
            {
                if (group.Positions.Count > bestMatchLength)
                {
                    bestMatchLength = group.Positions.Count;
                }
            }
            return true;
        }
    }
}
