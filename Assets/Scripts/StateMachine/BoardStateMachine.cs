using System;

namespace Match3.StateMachine
{
    /// <summary>
    /// Minimal state machine that tracks the board's current
    /// <see cref="IBoardState"/> and raises <see cref="StateChanged"/> on
    /// every transition, so unrelated systems (like input) can react
    /// without the machine knowing they exist.
    /// </summary>
    public sealed class BoardStateMachine
    {
        public IBoardState CurrentState { get; private set; }

        public event Action<IBoardState> StateChanged;

        public void Initialize(IBoardState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
            OnStateChanged(CurrentState);
        }

        public void TransitionTo(IBoardState nextState)
        {
            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
            OnStateChanged(CurrentState);
        }

        private void OnStateChanged(IBoardState state)
        {
            StateChanged?.Invoke(state);
        }
    }
}
