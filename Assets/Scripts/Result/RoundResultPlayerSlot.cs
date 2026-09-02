using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoundResultPlayerSlot : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Image Image_PlayerColor;
    [SerializeField] private TMP_Text Text_Nickname;
    [SerializeField] private TMP_Text Text_TotalScore;

    [Header("Round Scores")]
    [SerializeField] private Transform Transform_ScoreBlockRoot;
    [SerializeField] private RoundScoreBlock Prefab_ScoreBlock;
    [SerializeField] private Color _previousRoundColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);

    private readonly Dictionary<int, RoundScoreBlock> _scoreBlocks = new();
    private ulong _clientId;

    public ulong ClientId => _clientId;

    public void InitializeSlot(ulong clientId, string nickname, int totalScore, Color playerColor, IReadOnlyList<PlayerRoundResultData> roundHistory, int currentRoundIndex)
    {
        _clientId = clientId;

        Text_Nickname.text = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname;

        Text_TotalScore.text = $"{totalScore} 점";
        Image_PlayerColor.color = playerColor;

        RebuildScoreBlocks(roundHistory, currentRoundIndex);
    }

    public void Release()
    {
        ClearScoreBlocks();
        _clientId = 0;
    }

    private void RebuildScoreBlocks(IReadOnlyList<PlayerRoundResultData> roundHistory, int currentRoundIndex)
    {
        ClearScoreBlocks();

        if (roundHistory == null)
        {
            return;
        }

        for (int i = 0; i < roundHistory.Count; i++)
        {
            PlayerRoundResultData resultData = roundHistory[i];

            if (resultData.RoundScore <= 0)
            {
                continue;
            }

            RoundScoreBlock scoreBlock = Instantiate(Prefab_ScoreBlock, Transform_ScoreBlockRoot);

            Color blockColor = resultData.RoundIndex == currentRoundIndex ? GetRoundColor(resultData.RoundIndex) : _previousRoundColor;

            scoreBlock.Refresh(resultData.RoundScore, blockColor);

            _scoreBlocks[resultData.RoundIndex] = scoreBlock;
        }
    }

    private void ClearScoreBlocks()
    {
        foreach (RoundScoreBlock scoreBlock in _scoreBlocks.Values)
        {
            if (scoreBlock != null)
            {
                Destroy(scoreBlock.gameObject);
            }
        }

        _scoreBlocks.Clear();
    }

    private Color GetRoundColor(int roundIndex)
    {
        switch ((roundIndex - 1) % 4)
        {
            case 0:
                return new Color(0.32f, 0.72f, 1f);

            case 1:
                return new Color(0.35f, 0.9f, 0.55f);

            case 2:
                return new Color(1f, 0.68f, 0.25f);

            default:
                return new Color(0.75f, 0.55f, 1f);
        }
    }
}
