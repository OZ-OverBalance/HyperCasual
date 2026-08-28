using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetCodeScoreManager : NetworkBehaviour
{
    public static NetCodeScoreManager Instance;

    private Dictionary<ulong, int> playerScoreDic = new Dictionary<ulong, int>();
    public Dictionary<ulong, int> PlayerScoreDic => playerScoreDic;

    private int cureentGoalRank = 1;
    private readonly int[] goalScores = { 5, 4, 3, 2, 1 };
    private int killScore = 1;

    public event Action<ulong, int> OnScoreChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public void ResetRoundGoalRank()
    {
        if (!IsServer) return;
        cureentGoalRank = 1;
    }

    // 포탈 도착용
    public void AddGoalScore(ulong playerId)
    {
        if (!IsServer) return;

        int scoreAdd = 1;

        int rankIndex = cureentGoalRank - 1;
        if (rankIndex >= 0 && rankIndex < goalScores.Length)
        {
            scoreAdd = goalScores[rankIndex];
        }

        AddScore(playerId, scoreAdd);

        cureentGoalRank++;
    }

    // 함정용 코드
    public void AddTrapKillScore(ulong ownerId, ulong deadPlayerId)
    {
        if (!IsServer) return;
        if (ownerId == deadPlayerId)
        {
            return;
        }

        AddScore(ownerId, killScore);
    }

    private void AddScore(ulong playerId, int amount)
    {
        if (!playerScoreDic.ContainsKey(playerId))
        {
            playerScoreDic[playerId] = 0;
        }

        playerScoreDic[playerId] += amount;
        OnScoreChanged?.Invoke(playerId, playerScoreDic[playerId]);
        UpdateScoreClientRpc(playerId, playerScoreDic[playerId]);
    }

    public int GetScore(ulong playerId)
    {
        return playerScoreDic.TryGetValue(playerId, out int score) ? score : 0;
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(ulong playerId, int newScore)
    {
        if (IsServer) return;

        playerScoreDic[playerId] = newScore;
        OnScoreChanged?.Invoke(playerId, newScore);
    }
}