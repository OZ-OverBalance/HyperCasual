using Cysharp.Threading.Tasks;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
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
        Debug.Log($"UIFlowController - 게임 상태 변경 수신 : {gameState}");
        ChangeUIAsync(gameState).Forget();
    }

    private async UniTask ChangeUIAsync(GameState gameState)
    {
        if (_uiManager == null)
        {
            return;
        }

        while (_isChangingUI)
        {
            await UniTask.Yield();
        }

        _isChangingUI = true;

        bool shouldShowLoading = gameState == GameState.Lobby || gameState == GameState.WaitingRoom;

        if (shouldShowLoading)
        {
            await _uiManager.ShowLoadingUIAsync(GetLoadingMessage(gameState));
        }

        try
        {
            switch (gameState)
            {
                case GameState.Title:
                    _uiManager.CloseUI(UIType.Lobby);
                    await _uiManager.ShowTitleUIAsync();
                    break;

                case GameState.Lobby:
                    _uiManager.CloseUI(UIType.Title);
                    _uiManager.CloseUI(UIType.WaitingRoom);
                    _uiManager.CloseUI(UIType.JoinRoomPopup);

                    await _uiManager.ShowLobbyUIAsync();
                    break;

                case GameState.WaitingRoom:
                    await ShowWaitingRoomUIAsync();
                    break;

                case GameState.Build:
                    _uiManager.CloseUI(UIType.WaitingRoom);
                    _uiManager.CloseUI(UIType.JoinRoomPopup);
                    _uiManager.CloseUI(UIType.Lobby);
                    break;
            }
        }
        finally
        {
            if (shouldShowLoading)
            {
                await _uiManager.HideLoadingUIAsync();
            }

            _isChangingUI = false;
        }
    }

    private string GetLoadingMessage(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Lobby:
                return "로비로 이동하고 있어요...";

            case GameState.WaitingRoom:
                return "대기실로 이동하고 있어요...";

            default:
                return "LOADING...";
        }
    }

    private async UniTask ShowWaitingRoomUIAsync()
    {
        UIManager uiManager = UIManager.Inst;

        if (uiManager == null)
        {
            Debug.LogError("UIFlowController - UIManager 없음");
            return;
        }

        uiManager.CloseUI(UIType.Title);
        uiManager.CloseUI(UIType.Lobby);
        uiManager.CloseUI(UIType.JoinRoomPopup);

        WaitingRoomView waitingRoomView = await uiManager.ShowUIAsync<WaitingRoomView>(UIType.WaitingRoom);

        if (waitingRoomView == null)
        {
            Debug.LogError("UIFlowController - WaitingRoom UI 표시 실패");
            return;
        }

        NetCodeNetworkManager networkManager = NetCodeNetworkManager.Inst;

        if (networkManager != null)
        {
            waitingRoomView.SetRoomCode(networkManager.CurrentRoomCode);
        }
    }
}