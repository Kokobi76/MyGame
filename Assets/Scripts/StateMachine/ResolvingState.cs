namespace Match3.StateMachine
{
    /// <summary>
    /// Matches are being removed, tiles are collapsing and the board is
    /// refilling. This loops internally (cascades) before the board
    /// returns to <see cref="IdleState"/>.
    /// </summary>
    public sealed class ResolvingState : IBoardState
    {
        public void Enter()
        {
        }

        public void Exit()
        {
        }
    }
}
