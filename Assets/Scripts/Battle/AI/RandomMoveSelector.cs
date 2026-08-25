using System.Collections.Generic;
using Match3.Swap;

namespace Match3.Battle.AI
{
    /// <summary>
    /// Picks uniformly at random among every currently legal swap. This
    /// is the "Pick Random" AI difficulty from the design doc, and is
    /// also always used for the Player's auto-play moves per spec.
    /// </summary>
    public sealed class RandomMoveSelector : IMoveSelector
    {
        public bool TryGetMove(IReadOnlyList<EvaluatedMove> validMoves, out SwapMove move)
        {
            if (validMoves == null || validMoves.Count == 0)
            {
                move = default;
                return false;
            }

            move = validMoves[UnityEngine.Random.Range(0, validMoves.Count)].Move;
            return true;
        }
    }
}
