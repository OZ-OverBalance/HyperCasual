using System;

public class GameManager : SingletonBase<GameManager>
{
    private GameState _currentState = GameState.None;

    public GameState CurrentState => _currentState;

    public event Action<GameState> OnGameStateChanged;

    public void ChangeGameState(GameState gameState)
    {
        if (_currentState == gameState)
        {
            return;
        }

        _currentState = gameState;
        OnGameStateChanged?.Invoke(_currentState);
    }
}