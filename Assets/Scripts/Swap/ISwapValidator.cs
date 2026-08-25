using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Model;

namespace Match3.Swap
{
    /// <summary>
    /// Rules for whether a player-initiated swap is legal and whether the
    /// board still has any legal move left at all.
    /// </summary>
    public interface ISwapValidator
    {
        bool AreAdjacent(Vector2Int a, Vector2Int b);
        bool HasAnyValidMove(BoardModel board, BoardConfig config);

        /// <summary>Every currently legal swap on the board, each with its best resulting match length. Used by move-selection (AI, hints), not by the core resolve loop.</summary>
        IReadOnlyList<EvaluatedMove> GetAllValidMoves(BoardModel board, BoardConfig config);
    }
}
