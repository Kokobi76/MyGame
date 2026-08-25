using System.Collections.Generic;
using Match3.Swap;

namespace Match3.Battle.AI
{
    /// <summary>
    /// The "Pick Smart" AI difficulty: prioritizes a random move among
    /// those that reach the configured extra-turn match length; falls
    /// back to a random move among all legal moves (already guaranteed
    /// at least a 3-match) when none qualify. The preferred length is
    /// injected rather than hardcoded so it always matches whatever
    /// <see cref="Match3.Battle.Data.BattleTuningConfig.ExtraTurnMatchLength"/>
    /// is currently set to.
    /// </summary>
    public sealed class SmartMoveSelector : IMoveSelector
    {
        private readonly int _preferredMinMatchLength;

        public SmartMoveSelector(int preferredMinMatchLength)
        {
            _preferredMinMatchLength = preferredMinMatchLength;
        }

        public bool TryGetMove(IReadOnlyList<EvaluatedMove> validMoves, out SwapMove move)
        {
            if (validMoves == null || validMoves.Count == 0)
            {
                move = default;
                return false;
            }

            IReadOnlyList<EvaluatedMove> preferredMoves = FilterByMinLength(validMoves, _preferredMinMatchLength);
            IReadOnlyList<EvaluatedMove> candidates = preferredMoves.Count > 0 ? preferredMoves : validMoves;

            move = candidates[UnityEngine.Random.Range(0, candidates.Count)].Move;
            return true;
        }

        private static List<EvaluatedMove> FilterByMinLength(IReadOnlyList<EvaluatedMove> moves, int minLength)
        {
            List<EvaluatedMove> filtered = new List<EvaluatedMove>();
            foreach (EvaluatedMove move in moves)
            {
                if (move.BestMatchLength >= minLength)
                {
                    filtered.Add(move);
                }
            }
            return filtered;
        }
    }
}
