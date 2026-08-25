namespace Match3.StateMachine
{
    /// <summary>
    /// A phase in the board's lifecycle (Idle, Swapping, Resolving...).
    /// Enter/Exit are extension points for phase-specific side effects
    /// (e.g. showing a "processing" indicator); the actual step-by-step
    /// gameplay flow is driven by coroutines in
    /// <see cref="Match3.Gameplay.BoardController"/>, which calls
    /// <see cref="BoardStateMachine.TransitionTo"/> at each phase change.
    /// </summary>
    public interface IBoardState
    {
        void Enter();
        void Exit();
    }
}
