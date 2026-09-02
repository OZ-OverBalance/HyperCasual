using System.Collections.Generic;

public static class DefaultGameFlowProvider
{
    public static IReadOnlyList<GameStateTransition> CreateTransitions()
    {
        // [TODO] GameDataManager 구현 후 JSON 기반 GameFlowData 공급 방식으로 교체
        List<GameStateTransition> transitions = new List<GameStateTransition>
            {
                new GameStateTransition(GameState.None, GameState.Title),
                new GameStateTransition(GameState.Title, GameState.Lobby),
                new GameStateTransition(GameState.Lobby, GameState.Title),
                new GameStateTransition(GameState.Lobby, GameState.WaitingRoom),
                new GameStateTransition(GameState.WaitingRoom, GameState.Build),
                new GameStateTransition(GameState.WaitingRoom, GameState.Lobby),
                new GameStateTransition(GameState.Build, GameState.Run),
                new GameStateTransition(GameState.Run, GameState.Result),
                new GameStateTransition(GameState.Result, GameState.Build),
                new GameStateTransition(GameState.Result, GameState.WaitingRoom)
            };

        return transitions;
    }
}