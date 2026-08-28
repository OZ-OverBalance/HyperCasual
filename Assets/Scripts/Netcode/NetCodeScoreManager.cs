using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetCodeScoreManager : NetworkBehaviour
{
    public static NetCodeScoreManager Instance;

    private Dictionary<ulong, int> playerScoreDic = new Dictionary<ulong, int>();
    public Dictionary<ulong, int> PlayerScoreDic => playerScoreDic;

    private Dictionary<ulong, int> roundScoreDic = new Dictionary<ulong, int>();
    public Dictionary<ulong, int> RoundScoreDic => roundScoreDic;

    private int cureentGoalRank = 1;
    private readonly int[] goalScores = { 5, 4, 3, 2, 1 };
    private bool isGoalAnyone = false;
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
        isGoalAnyone = false;
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

        AddRoundScore(playerId, scoreAdd);

        cureentGoalRank++;
        if (isGoalAnyone == false) isGoalAnyone = true;
        Debug.Log("[NetCodeScoreManager 도착확인");
    }

    // 함정용 코드
    public void AddTrapKillScore(ulong ownerId, ulong deadPlayerId)
    {
        if (!IsServer) return;
        if (ownerId == deadPlayerId)
        {
            return;
        }

        AddRoundScore(ownerId, killScore);
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

    private void AddRoundScore(ulong playerId, int amount)
    {
        if (!roundScoreDic.ContainsKey(playerId))
        {
            roundScoreDic[playerId] = 0;
        }

        roundScoreDic[playerId] += amount;
    }

    public int GetScore(ulong playerId)
    {
        return playerScoreDic.TryGetValue(playerId, out int score) ? score : 0;
    }

    /// <summary>
    /// 한 라운드가 끝나고 점수를 정산하는 메서드
    /// </summary>
    public void CalculationRoundScore()
    {
        if (!IsServer) return;

        if(isGoalAnyone == true)
        {
            foreach (var data in roundScoreDic)
            {
                AddScore(data.Key, data.Value);
            }
        }
    }

    public void PrintScoreLog()
    {
        Debug.Log("========== [현재 전체 점수판 현황] ==========");

        foreach (var kvp in playerScoreDic)
        {
            ulong clientId = kvp.Key;
            int score = kvp.Value;
            Debug.Log($"Client ID: {clientId} | 점수: {score}점");
        }

        Debug.Log("=============================================");
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(ulong playerId, int newScore)
    {
        if (IsServer) return;

        playerScoreDic[playerId] = newScore;
        OnScoreChanged?.Invoke(playerId, newScore);
    }
}