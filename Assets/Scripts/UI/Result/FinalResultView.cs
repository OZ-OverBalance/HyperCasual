using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public sealed class FinalResultView : UIBase
{
    [SerializeField] private TMP_Text Text_Title;
    [SerializeField] private Transform Transform_RankingSlotRoot;
    [SerializeField] private FinalResultRankingSlot Prefab_RankingSlot;

    [SerializeField] private UIButton Button_ReturnWaitingRoom;
    [SerializeField] private TMP_Text Text_StatusMessage;

    [SerializeField] private Color[] _playerColors;

    private readonly Dictionary<ulong, FinalResultRankingSlot> _rankingSlots = new();

    private NetCodeScoreManager _scoreManager;

    public override UILayer Layer => UILayer.Main;

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && Text_Title != null && Transform_RankingSlotRoot != null && Prefab_RankingSlot != null && Button_ReturnWaitingRoom != null && Text_StatusMessage != null;
    }

    protected override void BindEvents()
    {
        Button_ReturnWaitingRoom.BindOnClickButtonEvent(HandleClickReturnWaitingRoomButton);
    }

    protected override void UnbindEvents()
    {
        Button_ReturnWaitingRoom.UnbindOnClickButtonEvent(HandleClickReturnWaitingRoomButton);
    }

    protected override void RefreshUI()
    {
        _scoreManager = NetCodeScoreManager.Instance;

        Text_Title.text = "최종 순위";
        Text_StatusMessage.text = string.Empty;

        RefreshReturnButton();
        RebuildRankingSlots();
    }

    protected override void ReleaseUI()
    {
        ClearRankingSlots();
        _scoreManager = null;
    }

    private void HandleClickReturnWaitingRoomButton()
    {
        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;

        if (roomManager == null)
        {
            Text_StatusMessage.text = "대기실 정보를 찾을 수 없습니다.";
            return;
        }

        Button_ReturnWaitingRoom.SetInteractable(false);
        Text_StatusMessage.text = "대기실로 이동하고 있어요...";

        roomManager.RequestReturnToWaitingRoomServerRpc();
    }

    private void RefreshReturnButton()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        bool isHost = networkManager != null && networkManager.IsServer;

        Button_ReturnWaitingRoom.gameObject.SetActive(isHost);
        Button_ReturnWaitingRoom.SetInteractable(isHost);

        if (!isHost)
        {
            Text_StatusMessage.text = "방장이 대기실로 이동하기를 기다리고 있습니다.";
        }
    }

    private void RebuildRankingSlots()
    {
        ClearRankingSlots();

        if (_scoreManager == null)
        {
            Text_StatusMessage.text = "최종 점수 정보를 불러올 수 없습니다.";
            return;
        }

        IReadOnlyList<PlayerRoundResultData> results = _scoreManager.LatestRoundResults;

        for (int i = 0; i < results.Count; i++)
        {
            PlayerRoundResultData resultData = results[i];

            FinalResultRankingSlot rankingSlot = Instantiate(Prefab_RankingSlot, Transform_RankingSlotRoot);

            rankingSlot.InitializeSlot(resultData.ClientId, i + 1, GetPlayerNickname(resultData.ClientId), resultData.TotalScore, GetPlayerColor(resultData.ClientId));

            _rankingSlots[resultData.ClientId] = rankingSlot;
        }
    }

    private string GetPlayerNickname(ulong clientId)
    {
        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;

        if (roomManager == null)
        {
            return $"Player {clientId}";
        }

        for (int i = 0; i < roomManager.PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = roomManager.PlayerList[i];

            if (playerData.ClientId == clientId)
            {
                return playerData.PlayerName.ToString();
            }
        }

        return $"Player {clientId}";
    }

    private Color GetPlayerColor(ulong clientId)
    {
        if (_playerColors == null || _playerColors.Length == 0)
        {
            return Color.black;
        }

        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;

        if (roomManager == null)
        {
            return Color.black;
        }

        for (int i = 0; i < roomManager.PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = roomManager.PlayerList[i];

            if (playerData.ClientId != clientId)
            {
                continue;
            }

            int colorIndex = playerData.ColorIndex;

            if (colorIndex < 0 || colorIndex >= _playerColors.Length)
            {
                return Color.black;
            }

            return _playerColors[colorIndex];
        }

        return Color.black;
    }

    private void ClearRankingSlots()
    {
        foreach (FinalResultRankingSlot rankingSlot in _rankingSlots.Values)
        {
            if (rankingSlot == null)
            {
                continue;
            }

            rankingSlot.Release();
            Destroy(rankingSlot.gameObject);
        }

        _rankingSlots.Clear();
    }
}