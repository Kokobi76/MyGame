using System.Collections.Generic;
using Match3.Swap;

namespace Match3.Battle.AI
{
    public enum AiDifficulty
    {
        Random,
        Smart
    }

    /// <summary>
    /// Chooses which legal swap to play next, given the full set of
    /// currently legal moves (each with the length of the match it would
    /// produce). Used for both the Enemy's turn and an auto-playing
    /// Player's turn. Implementations don't touch the board directly —
    /// move validation and execution stay the core board's job via
    /// <see cref="Match3.Gameplay.BoardController.RequestSwap"/> and
    /// <see cref="Match3.Gameplay.BoardController.GetAllValidMoves"/>.
    /// </summary>
    public interface IMoveSelector
    {
        bool TryGetMove(IReadOnlyList<EvaluatedMove> validMoves, out SwapMove move);
    }
}
