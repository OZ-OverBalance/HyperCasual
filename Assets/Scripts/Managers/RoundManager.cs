using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class RoundManager
{
    private readonly GameManager _gameManager;
    private readonly HashSet<ulong> _arrivedPlayerIds = new();

    private int _currentRound;
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

    public bool TryStartRound()
    {
        if (_isRoundActive || _gameManager.CurrentState != GameState.WaitingRoom)
        {
            return false;
        }

        if (!_gameManager.TryChangeGameState(GameState.Build))
        {
            return false;
        }

        _currentRound++;
        _isRoundActive = true;
        _arrivedPlayerIds.Clear();
        _arrivedPlayerCount = 0;

        NetCodeScoreManager.Instance?.ResetRoundGoalRank();
        OnStartedRound?.Invoke(_currentRound);

        return true;
    }

    public bool TryStartNextRound()
    {
        if (_isRoundActive || _gameManager.CurrentState != GameState.Result)
        {
            return false;
        }

        if (!_gameManager.TryChangeGameState(GameState.Build))
        {
            return false;
        }

        _currentRound++;
        _isRoundActive = true;
        _arrivedPlayerIds.Clear();
        _arrivedPlayerCount = 0;

        NetCodeScoreManager.Instance?.ResetRoundGoalRank();
        OnStartedRound?.Invoke(_currentRound);

        return true;
    }

    public bool TryStartRun()
    {
        if (!_isRoundActive || _gameManager.CurrentState != GameState.Build)
        {
            return false;
        }

        _arrivedPlayerIds.Clear();
        _arrivedPlayerCount = 0;
        _gameManager.BuildPhaseManager?.SaveAndClearCurrentMap();

        return _gameManager.TryChangeGameState(GameState.Run);
    }

    public void OnPlayerArrived(ulong clientId)
    {
        if (!_isRoundActive || _gameManager.CurrentState != GameState.Run) return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && !networkManager.IsServer)
        {
            return;
        }

        if (!_arrivedPlayerIds.Add(clientId))
        {
            return;
        }

        _arrivedPlayerCount = _arrivedPlayerIds.Count;
        Debug.Log($"[RoundManager] 플레이어({clientId}) 도착! 현재 도착 인원: {_arrivedPlayerCount}");

        int totalPlayers = Unity.Netcode.NetworkManager.Singleton != null
            ? networkManager.ConnectedClientsList.Count
            : 1;

        if (_arrivedPlayerCount >= totalPlayers)
        {
            Debug.Log("[RoundManager] 모든 플레이어 도착 완료! 라운드를 종료합니다.");
            TryEndRound();
        }
    }

    public bool TryEndRound()
    {
        if (!_isRoundActive || _gameManager.CurrentState != GameState.Run)
        {
            return false;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && !networkManager.IsServer)
        {
            return false;
        }

        NetCodeScoreManager scoreManager = NetCodeScoreManager.Instance;
        if (scoreManager == null)
        {
            Debug.LogError("[RoundManager] NetCodeScoreManager가 없습니다.");
            return false;
        }

        // 결과 데이터를 먼저 확정해야 Result UI가 열릴 때 즉시 표시할 수 있음
        scoreManager.FinalizeRoundScores(_currentRound);

        return CompleteRound(_currentRound);
    }

    // 서버에서 전달받은 결과를 클라이언트의 로컬 라운드 상태에 반영
    public bool ApplyRoundResult(int roundIndex)
    {
        if (_gameManager.CurrentState == GameState.Result)
        {
            _currentRound = roundIndex;
            _isRoundActive = false;
            return true;
        }

        if (_gameManager.CurrentState != GameState.Run)
        {
            return false;
        }

        _currentRound = roundIndex;
        return CompleteRound(roundIndex);
    }

    public void StartRunPhase()
    {
        Debug.Log("[RoundManager] Run Phase 실행");


        if (!_gameManager.TryChangeGameState(GameState.Run))
        {
            return;
        }

        CameraManager.Inst?.ActivateFollowCamera();

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsServer)
        {
            NetCodeRoomManager.Instance?.SetPlayerObjSpawn();
        }
    }

    private bool CompleteRound(int roundIndex)
    {
        if (!_gameManager.TryChangeGameState(GameState.Result))
        {
            return false;
        }

        if (CameraManager.Inst != null)
        {
            CameraManager.Inst.StopSpectating();
        }

        _isRoundActive = false;
        _arrivedPlayerIds.Clear();
        _arrivedPlayerCount = 0;

        OnEndedRound?.Invoke(roundIndex);
        return true;
    }

    public void HandlePlayerDisconnect(ulong clientId)
    {
        if (!_isRoundActive || _gameManager.CurrentState != GameState.Run)
        {
            return;
        }

        _arrivedPlayerIds.Remove(clientId);
        _arrivedPlayerCount = _arrivedPlayerIds.Count;

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        int remainingPlayerCount = networkManager.ConnectedClientsList.Count;

        Debug.Log($"RoundManager - 플레이어({clientId}) 연결 해제. 도착 {_arrivedPlayerCount}/{remainingPlayerCount}");

        if (_arrivedPlayerCount >= remainingPlayerCount)
        {
            TryEndRound();
        }
    }

    public void ResetMatch()
    {
        _currentRound = 0;
        _isRoundActive = false;

        _arrivedPlayerIds.Clear();
        _arrivedPlayerCount = 0;
    }
}