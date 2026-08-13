using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class UIFlowController : MonoBehaviour
{
    private GameManager _gameManager;
    private UIManager _uiManager;
    private bool _isChangingUI;

    private void Start()
    {
        InitializeFlowAsync().Forget();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private async UniTaskVoid InitializeFlowAsync()
    {
        _gameManager = GameManager.Inst;
        _uiManager = UIManager.Inst;

        if (_gameManager == null || _uiManager == null)
        {
            Debug.LogError("UIFlowController - 필수 매니저가 초기화되지 않음");
            return;
        }

        _gameManager.OnGameStateChanged += HandleGameStateChanged;

        if (_gameManager.CurrentState == GameState.None)
        {
            _gameManager.TryChangeGameState(GameState.Title);
        }
        else
        {
            await ChangeUIAsync(_gameManager.CurrentState);
        }
    }

    private void UnbindEvents()
    {
        if (_gameManager == null)
        {
            return;
        }

        _gameManager.OnGameStateChanged -= HandleGameStateChanged;

        _gameManager = null;
        _uiManager = null;
    }

    private void HandleGameStateChanged(GameState gameState)
    {
        ChangeUIAsync(gameState).Forget();
    }

    private async UniTask ChangeUIAsync(GameState gameState)
    {
        if (_isChangingUI || _uiManager == null)
        {
            return;
        }

        _isChangingUI = true;

        try
        {
            switch (gameState)
            {
                case GameState.Title:
                    await _uiManager.ShowTitleUIAsync();
                    break;

                case GameState.Lobby:
                    _uiManager.CloseUI(UIType.Title);
                    break;
            }
        }
        finally
        {
            _isChangingUI = false;
        }
    }
}