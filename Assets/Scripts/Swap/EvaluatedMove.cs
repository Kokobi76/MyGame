namespace Match3.Swap
{
    /// <summary>A legal swap, plus the length of the best (longest) match it would produce.</summary>
    public readonly struct EvaluatedMove
    {
        public SwapMove Move { get; }
        public int BestMatchLength { get; }

        public EvaluatedMove(SwapMove move, int bestMatchLength)
        {
            Move = move;
            BestMatchLength = bestMatchLength;
        }
    }
}
