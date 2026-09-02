using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using Unity.Netcode;

public sealed class RoundManager
{
    private readonly GameManager _gameManager;

    private int _currentRound = 0;
    private bool _isRoundActive;
    private int _arrivedPlayerCount = 0;

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

    public void OnPlayerArrived(ulong clientId)
    {
        if (!_isRoundActive || _gameManager.CurrentState != GameState.Run) return;

        _arrivedPlayerCount++;
        UnityEngine.Debug.Log($"[RoundManager] 플레이어({clientId}) 도착! 현재 도착 인원: {_arrivedPlayerCount}");

        int totalPlayers = Unity.Netcode.NetworkManager.Singleton != null
            ? Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count
            : 1;

        if (_arrivedPlayerCount >= totalPlayers)
        {
            UnityEngine.Debug.Log("[RoundManager] 모든 플레이어 도착 완료! 라운드를 종료합니다.");
            NetCodeRoomManager.Instance.EndRoundClientRpc();
        }
    }
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

        _arrivedPlayerCount = 0; 

        _gameManager.BuildPhaseManager?.SaveAndClearCurrentMap();

        return _gameManager.TryChangeGameState(GameState.Run);
    }

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

        if(CameraManager.Inst != null)
        {
            CameraManager.Inst.StopSpectating();
        }

        _isRoundActive = false;

        OnEndedRound?.Invoke(_currentRound);

        TryStartNextRoundBuild();

        return true;
    }

    public void StartRunPhase()
    {
        StartRunPhaseAsync();
    }

    private void StartRunPhaseAsync()
    {
        UnityEngine.Debug.Log("[RoundManager] runphase 실행");

        if (_gameManager.BuildPhaseManager != null)
        {
           // _gameManager.BuildPhaseManager.SaveAndClearCurrentMap();
        }

        if (MapManager.Inst != null)
        {
            //await MapManager.Inst.GenerateRunPhaseLevel();
        }

        if (_gameManager.TryChangeGameState(GameState.Run))
        {
            CameraManager.Inst.ActivateFollowCamera();
        }
    }

    public bool TryStartNextRoundBuild()
    {
        if (_isRoundActive) return false;

        if (_gameManager.CurrentState != GameState.Result)
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
}