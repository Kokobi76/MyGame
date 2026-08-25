using UnityEngine;

namespace Match3.Swap
{
    /// <summary>A pair of adjacent grid cells that can legally be swapped.</summary>
    public readonly struct SwapMove
    {
        public Vector2Int From { get; }
        public Vector2Int To { get; }

        public SwapMove(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }
    }
}
