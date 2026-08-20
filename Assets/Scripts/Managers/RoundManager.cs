using Cysharp.Threading.Tasks;
using System;

public sealed class RoundManager
{
    private readonly GameManager _gameManager;

    private int _currentRound;
    private bool _isRoundActive;

    public int CurrentRound => _currentRound;
    public bool IsRoundActive => _isRoundActive;

    public event Action<int> OnStartedRound;
    public event Action<int> OnEndedRound;

    public RoundManager(GameManager gameManager)
    {
        if (gameManager == null)
        {
            throw new ArgumentNullException(nameof(gameManager));
        }

        _gameManager = gameManager;
    }

// 대기실 > Build 상태 전환
public bool TryStartRound()
    {
        if (_isRoundActive)
        {
            return false;
        }

        if (_gameManager.CurrentState != GameState.WaitingRoom)
        {
            return false;
        }

        if (!_gameManager.TryChangeGameState(GameState.Build))
        {
            return false;
        }

        _currentRound++;
        _isRoundActive = true;

        OnStartedRound?.Invoke(_currentRound);

        return true;
    }

    // Build > Run 상태 전환
    public bool TryStartRun()
    {
        if (!_isRoundActive)
        {
            return false;
        }

        if (_gameManager.CurrentState != GameState.Build)
        {
            return false;
        }

        _gameManager.BuildPhaseManager?.SaveAndClearCurrentMap();

        return _gameManager.TryChangeGameState(GameState.Run);
    }

    // Run > Result 상태 전환
    public bool TryEndRound()
    {
        if (!_isRoundActive)
        {
            return false;
        }

        if (_gameManager.CurrentState != GameState.Run)
        {
            return false;
        }

        if (!_gameManager.TryChangeGameState(GameState.Result))
        {
            return false;
        }

        _isRoundActive = false;

        OnEndedRound?.Invoke(_currentRound);

        return true;
    }

    public void StartRunPhase()
    {
        StartRunPhaseAsync().Forget();
    }

    private async UniTaskVoid StartRunPhaseAsync()
    {
        if (_gameManager.BuildPhaseManager != null)
        {
            _gameManager.BuildPhaseManager.SaveAndClearCurrentMap();
        }

        if (MapManager.Inst != null)
        {
            await MapManager.Inst.GenerateRunPhaseLevel();
        }

        if (_gameManager.TryChangeGameState(GameState.Run))
        {
            //  TODO : 플레이어 리스폰, 카메라 시점 전환 필요
        }
    }
}