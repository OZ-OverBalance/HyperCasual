using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FinalResultRankingSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text Text_Rank;
    [SerializeField] private Image Image_PlayerColor;
    [SerializeField] private TMP_Text Text_Nickname;
    [SerializeField] private TMP_Text Text_TotalScore;

    private ulong _clientId;

    public ulong ClientId => _clientId;

    public void InitializeSlot(ulong clientId, int rank, string nickname, int totalScore, Color playerColor)
    {
        _clientId = clientId;

        Text_Rank.text = $"{rank}위";
        Text_Nickname.text = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname;
        Text_TotalScore.text = $"{totalScore} 점";
        Image_PlayerColor.color = playerColor;
    }

    public void Release()
    {
        _clientId = 0;
    }
}