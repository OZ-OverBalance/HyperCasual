using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetCodeScoreManager : NetworkBehaviour
{
    [SerializeField] private int _winningScore = 15;

    public static NetCodeScoreManager Instance { get; private set; }

    private readonly Dictionary<ulong, int> _playerScoreDic = new();
    private readonly Dictionary<ulong, int> _roundScoreDic = new();
    private readonly Dictionary<ulong, List<PlayerRoundResultData>> _roundHistoryByPlayer = new();
    private readonly List<PlayerRoundResultData> _latestRoundResults = new();

    private int _currentGoalRank = 1;
    private readonly int[] _goalScores = { 5, 4, 3, 2, 1 };
    private bool _hasGoalPlayer;
    private int _killScore = 1;
    private int _latestRoundIndex;

    public IReadOnlyDictionary<ulong, int> PlayerScoreDic => _playerScoreDic;
    public IReadOnlyDictionary<ulong, int> RoundScoreDic => _roundScoreDic;
    public IReadOnlyList<PlayerRoundResultData> LatestRoundResults => _latestRoundResults;
    public int LatestRoundIndex => _latestRoundIndex;

    public event Action<ulong, int> OnScoreChanged;
    public event Action<int> OnRoundResultUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;

        base.OnNetworkDespawn();
    }

    public void ResetRoundGoalRank()
    {
        if (!IsServer) return;

        _currentGoalRank = 1;
        _hasGoalPlayer = false;
        _roundScoreDic.Clear();
    }


    // 포탈 도착용
    public void AddGoalScore(ulong playerId)
    {
        if (!IsServer) return;

        int scoreToAdd = 1;
        int rankIndex = _currentGoalRank - 1;

        if (rankIndex >= 0 && rankIndex < _goalScores.Length)
        {
            scoreToAdd = _goalScores[rankIndex];
        }

        AddRoundScore(playerId, scoreToAdd);

        _currentGoalRank++;
        _hasGoalPlayer = true;

        Debug.Log($"[NetCodeScoreManager] {playerId} 도착, 획득 점수: {scoreToAdd}");
    }

    // 함정용 코드
    public void AddTrapKillScore(ulong ownerId, ulong deadPlayerId)
    {
        if (!IsServer || ownerId == deadPlayerId) return;

        AddRoundScore(ownerId, _killScore);
    }

    public int GetScore(ulong playerId)
    {
        return _playerScoreDic.TryGetValue(playerId, out int score) ? score : 0;
    }

    public IReadOnlyList<PlayerRoundResultData> GetRoundHistory(ulong playerId)
    {
        if (_roundHistoryByPlayer.TryGetValue(playerId, out List<PlayerRoundResultData> history))
        {
            return history;
        }

        return Array.Empty<PlayerRoundResultData>();
    }

    public void FinalizeRoundScores(int roundIndex)
    {
        if (!IsServer)
        {
            return;
        }

        List<ulong> playerIds = GetConnectedPlayerIds();
        PlayerRoundResultData[] resultArray = new PlayerRoundResultData[playerIds.Count];

        for (int i = 0; i < playerIds.Count; i++)
        {
            ulong playerId = playerIds[i];
            int roundScore = GetAwardedRoundScore(playerId);

            AddTotalScore(playerId, roundScore);

            resultArray[i] = new PlayerRoundResultData(
                playerId,
                roundIndex,
                roundScore,
                GetScore(playerId));
        }

        Array.Sort(resultArray, CompareResultDescending);

        ApplyRoundResults(roundIndex, resultArray);
        PublishRoundResultsClientRpc(roundIndex, resultArray);

        _roundScoreDic.Clear();
        _currentGoalRank = 1;
        _hasGoalPlayer = false;
    }

    // 기존 호출부가 남아 있을 때를 위한 호환용 메서드
    public void CalculationRoundScore()
    {
        int roundIndex = GameManager.Inst != null && GameManager.Inst.RoundManager != null ? GameManager.Inst.RoundManager.CurrentRound : 0;
        FinalizeRoundScores(roundIndex);
    }
    public void PrintScoreLog()
    {
        Debug.Log("========== [현재 전체 점수판 현황] ==========");

        foreach (KeyValuePair<ulong, int> scorePair in _playerScoreDic)
        {
            Debug.Log($"Client ID: {scorePair.Key} | 점수: {scorePair.Value}점");
        }

        Debug.Log("=============================================");
    }

    public bool HasWinner()
    {
        foreach (KeyValuePair<ulong, int> scorePair in _playerScoreDic)
        {
            if (scorePair.Value >= _winningScore)
            {
                return true;
            }
        }

        return false;
    }

    public void ResetMatchScores()
    {
        _playerScoreDic.Clear();
        _roundScoreDic.Clear();
        _roundHistoryByPlayer.Clear();
        _latestRoundResults.Clear();

        _currentGoalRank = 1;
        _hasGoalPlayer = false;
        _latestRoundIndex = 0;
    }

    private void AddRoundScore(ulong playerId, int amount)
    {
        if (!_roundScoreDic.ContainsKey(playerId))
        {
            _roundScoreDic[playerId] = 0;
        }

        _roundScoreDic[playerId] += amount;
    }

    private void AddTotalScore(ulong playerId, int amount)
    {
        if (!_playerScoreDic.ContainsKey(playerId))
        {
            _playerScoreDic[playerId] = 0;
        }

        _playerScoreDic[playerId] += amount;
        OnScoreChanged?.Invoke(playerId, _playerScoreDic[playerId]);
    }

    private int GetAwardedRoundScore(ulong playerId)
    {
        if (!_hasGoalPlayer)
        {
            return 0;
        }

        return _roundScoreDic.TryGetValue(playerId, out int score) ? score : 0;
    }

    private List<ulong> GetConnectedPlayerIds()
    {
        List<ulong> playerIds = new();

        if (NetworkManager != null)
        {
            for (int i = 0; i < NetworkManager.ConnectedClientsList.Count; i++)
            {
                playerIds.Add(NetworkManager.ConnectedClientsList[i].ClientId);
            }
        }

        if (playerIds.Count == 0)
        {
            foreach (ulong playerId in _roundScoreDic.Keys)
            {
                playerIds.Add(playerId);
            }
        }

        return playerIds;
    }

    private void ApplyRoundResults(int roundIndex, PlayerRoundResultData[] resultArray)
    {
        _latestRoundIndex = roundIndex;
        _latestRoundResults.Clear();

        for (int i = 0; i < resultArray.Length; i++)
        {
            PlayerRoundResultData resultData = resultArray[i];

            _latestRoundResults.Add(resultData);
            _playerScoreDic[resultData.ClientId] = resultData.TotalScore;

            if (!_roundHistoryByPlayer.TryGetValue(
                    resultData.ClientId,
                    out List<PlayerRoundResultData> history))
            {
                history = new List<PlayerRoundResultData>();
                _roundHistoryByPlayer.Add(resultData.ClientId, history);
            }

            ReplaceOrAddHistory(history, resultData);

            if (!IsServer)
            {
                OnScoreChanged?.Invoke(resultData.ClientId, resultData.TotalScore);
            }
        }

        OnRoundResultUpdated?.Invoke(roundIndex);
    }

    private void ReplaceOrAddHistory(List<PlayerRoundResultData> history, PlayerRoundResultData resultData)
    {
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].RoundIndex == resultData.RoundIndex)
            {
                history[i] = resultData;
                return;
            }
        }

        history.Add(resultData);
        history.Sort(CompareRoundAscending);
    }

    private int CompareResultDescending(PlayerRoundResultData left, PlayerRoundResultData right)
    {
        int totalCompare = right.TotalScore.CompareTo(left.TotalScore);

        if (totalCompare != 0)
        {
            return totalCompare;
        }

        return left.ClientId.CompareTo(right.ClientId);
    }

    private int CompareRoundAscending(PlayerRoundResultData left, PlayerRoundResultData right)
    {
        return left.RoundIndex.CompareTo(right.RoundIndex);
    }

    [ClientRpc]
    private void PublishRoundResultsClientRpc(
         int roundIndex,
         PlayerRoundResultData[] resultArray)
    {
        if (IsServer)
        {
            return;
        }

        ApplyRoundResults(roundIndex, resultArray);

        GameManager gameManager = GameManager.Inst;

        if (gameManager != null && gameManager.RoundManager != null)
        {
            gameManager.RoundManager.ApplyRoundResult(roundIndex);
        }
    }
}