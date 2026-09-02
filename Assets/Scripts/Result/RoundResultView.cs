using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public sealed class RoundResultView : UIBase
{
    [Header("Result")]
    [SerializeField] private TMP_Text Text_Title;
    [SerializeField] private TMP_Text Text_Round;
    [SerializeField] private Transform Transform_PlayerRows;
    [SerializeField] private RoundResultPlayerSlot Prefab_PlayerSlot;

    [Header("Control")]
    [SerializeField] private UIButton Button_NextRound;
    [SerializeField] private TMP_Text Text_StatusMessage;

    [Header("Player Colors")]
    [SerializeField]
    private Color[] _playerColors =
    {
        new Color(0.25f, 0.7f, 1f),
        new Color(1f, 0.45f, 0.3f),
        new Color(0.35f, 0.9f, 0.5f),
        new Color(1f, 0.8f, 0.25f),
        new Color(0.7f, 0.45f, 1f)
    };

    private readonly Dictionary<ulong, RoundResultPlayerSlot> _playerSlots = new();
    private NetCodeScoreManager _scoreManager;

    public override UILayer Layer => UILayer.Popup;

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && Text_Title != null && Text_Round != null && Transform_PlayerRows != null && Prefab_PlayerSlot != null && Button_NextRound != null && Text_StatusMessage != null;
    }

    protected override void BindEvents()
    {
        Button_NextRound.BindOnClickButtonEvent(HandleClickNextRoundButton);

        _scoreManager = NetCodeScoreManager.Instance;

        if (_scoreManager != null)
        {
            _scoreManager.OnRoundResultUpdated -= HandleRoundResultUpdated;
            _scoreManager.OnRoundResultUpdated += HandleRoundResultUpdated;
        }
    }

    protected override void UnbindEvents()
    {
        Button_NextRound.UnbindOnClickButtonEvent(HandleClickNextRoundButton);

        if (_scoreManager != null)
        {
            _scoreManager.OnRoundResultUpdated -= HandleRoundResultUpdated;
        }
    }

    protected override void RefreshUI()
    {
        _scoreManager = NetCodeScoreManager.Instance;

        Text_Title.text = "ROUND RESULT";
        Text_StatusMessage.text = string.Empty;

        RefreshNextRoundButton();
        RebuildPlayerSlots();
    }

    protected override void ReleaseUI()
    {
        ClearPlayerSlots();
        _scoreManager = null;
    }

    private void HandleRoundResultUpdated(int roundIndex)
    {
        if (!IsOpened)
        {
            return;
        }

        RebuildPlayerSlots();
    }

    private void HandleClickNextRoundButton()
    {
        if (_scoreManager == null)
        {
            Text_StatusMessage.text = "점수 정보를 불러오는 중입니다.";
            return;
        }

        Button_NextRound.SetInteractable(false);
        Text_StatusMessage.text = "다음 라운드를 준비하고 있어요...";

        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;
        if (roomManager == null)
        {
            Button_NextRound.SetInteractable(true);
            Text_StatusMessage.text = "대기실 정보를 찾을 수 없습니다.";
            return;
        }

        roomManager.RequestStartNextRoundServerRpc();
    }

    private void RefreshNextRoundButton()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        bool isHost = networkManager != null && networkManager.IsServer;

        Button_NextRound.gameObject.SetActive(isHost);
        Button_NextRound.SetInteractable(isHost);

        if (!isHost)
        {
            Text_StatusMessage.text = "방장이 다음 라운드를 준비하고 있어요...";
        }
    }

    private void RebuildPlayerSlots()
    {
        ClearPlayerSlots();

        if (_scoreManager == null)
        {
            Text_Round.text = "ROUND -";
            return;
        }

        int roundIndex = _scoreManager.LatestRoundIndex;
        Text_Round.text = $"ROUND {roundIndex}";

        IReadOnlyList<PlayerRoundResultData> latestResults = _scoreManager.LatestRoundResults;

        for (int i = 0; i < latestResults.Count; i++)
        {
            PlayerRoundResultData resultData = latestResults[i];
            RoundResultPlayerSlot playerSlot = Instantiate(Prefab_PlayerSlot, Transform_PlayerRows);

            string nickname = GetPlayerNickname(resultData.ClientId);
            Color playerColor = GetPlayerColor(i);
            IReadOnlyList<PlayerRoundResultData> history = _scoreManager.GetRoundHistory(resultData.ClientId);

            playerSlot.InitializeSlot(resultData.ClientId, nickname, resultData.TotalScore, playerColor, history);

            _playerSlots.Add(resultData.ClientId, playerSlot);
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

    private Color GetPlayerColor(int playerIndex)
    {
        if (_playerColors == null || _playerColors.Length == 0)
        {
            return Color.white;
        }

        return _playerColors[playerIndex % _playerColors.Length];
    }

    private void ClearPlayerSlots()
    {
        foreach (RoundResultPlayerSlot playerSlot in _playerSlots.Values)
        {
            if (playerSlot != null)
            {
                playerSlot.Release();
                Destroy(playerSlot.gameObject);
            }
        }

        _playerSlots.Clear();
    }
}
