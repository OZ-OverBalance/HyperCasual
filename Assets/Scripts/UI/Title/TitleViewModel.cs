public sealed class TitleViewModel
{
    private readonly GameManager _gameManager;

    public TitleViewModel(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    // 타이틀에서 로비 상태로 전환합니다.
    public bool StartGame()
    {
        if (_gameManager == null)
        {
            return false;
        }

        return _gameManager.TryChangeGameState(
            GameState.Lobby);
    }

    // 게임 종료를 요청합니다.
    public bool ExitGame()
    {
        if (_gameManager == null)
        {
            return false;
        }

        _gameManager.QuitGame();
        return true;
    }
}