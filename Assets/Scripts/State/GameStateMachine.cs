using System;
using System.Collections.Generic;

public sealed class GameStateMachine
{
    private readonly IReadOnlyList<GameStateTransition> _transitions;
    private GameState _currentState;

    public GameState CurrentState => _currentState;

    public event Action<GameState> OnStateChanged;

    public GameStateMachine(GameState initialState, IReadOnlyList<GameStateTransition> transitions)
    {
        _currentState = initialState;

        if (transitions == null)
        {
            throw new ArgumentNullException(nameof(transitions));
        }

        _transitions = transitions;
    }

    public bool TryChangeState(GameState nextState)
    {
        if (_currentState == nextState)
        {
            return false;
        }

        if (!CanChangeState(nextState))
        {
            return false;
        }

        _currentState = nextState;
        OnStateChanged?.Invoke(_currentState);

        return true;
    }

    public bool CanChangeState(GameState nextState)
    {
        for (int i = 0; i < _transitions.Count; i++)
        {
            GameStateTransition transition = _transitions[i];

            if (transition.CanTransition(_currentState, nextState))
            {
                return true;
            }
        }

        return false;
    }
}