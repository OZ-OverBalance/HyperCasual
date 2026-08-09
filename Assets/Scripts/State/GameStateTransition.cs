public sealed class GameStateTransition
{
    public GameState FromState { get; }
    public GameState ToState { get; }

    public GameStateTransition(GameState fromState, GameState toState)
    {
        FromState = fromState;
        ToState = toState;
    }

    public bool CanTransition(GameState fromState, GameState toState)
    {
        return FromState == fromState && ToState == toState;
    }
}