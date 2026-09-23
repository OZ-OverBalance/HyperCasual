using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class FinalResultView : UIBase
{
    [SerializeField] private TMP_Text Text_Title;
    [SerializeField] private Transform Transform_RankingSlotRoot;
    [SerializeField] private FinalResultRankingSlot Prefab_RankingSlot;

    [SerializeField] private UIButton Button_ReturnWaitingRoom;
    [SerializeField] private TMP_Text Text_StatusMessage;

    [SerializeField] private Color[] _playerColors;

    [SerializeField] private RawImage RawImage_Podium;
    [SerializeField] private FinalResultPodiumRig Prefab_PodiumRig;

    private FinalResultPodiumRig _podiumRigInstance;
    private RenderTexture _podiumRenderTexture;

    private readonly Dictionary<ulong, FinalResultRankingSlot> _rankingSlots = new();

    private NetCodeScoreManager _scoreManager;

    public override UILayer Layer => UILayer.Main;

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && Text_Title != null && Transform_RankingSlotRoot != null && Prefab_RankingSlot != null && Button_ReturnWaitingRoom != null && Text_StatusMessage != null && RawImage_Podium != null && Prefab_PodiumRig != null;
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
        SetupPodiumPreview();
    }

    protected override void ReleaseUI()
    {
        ClearRankingSlots();
        ReleasePodiumPreview();
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

    private void SetupPodiumPreview()
    {
        ReleasePodiumPreview();

        if (Prefab_PodiumRig == null || RawImage_Podium == null)
        {
            return;
        }

        _podiumRigInstance = Instantiate(Prefab_PodiumRig, new Vector3(1000f, 1000f, 1000f), Quaternion.identity);

        _podiumRenderTexture = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);

        _podiumRenderTexture.name = "FinalResultPodiumRenderTexture";
        _podiumRenderTexture.Create();

        _podiumRigInstance.PreviewCamera.targetTexture = _podiumRenderTexture;
        RawImage_Podium.texture = _podiumRenderTexture;

        SpawnPodiumCharacters();
    }

    private void ReleasePodiumPreview()
    {
        if (RawImage_Podium != null)
        {
            RawImage_Podium.texture = null;
        }

        if (_podiumRigInstance != null)
        {
            if (_podiumRigInstance.PreviewCamera != null)
            {
                _podiumRigInstance.PreviewCamera.targetTexture = null;
            }

            Destroy(_podiumRigInstance.gameObject);
            _podiumRigInstance = null;
        }

        if (_podiumRenderTexture != null)
        {
            if (_podiumRenderTexture.IsCreated())
            {
                _podiumRenderTexture.Release();
            }

            Destroy(_podiumRenderTexture);
            _podiumRenderTexture = null;
        }
    }

    private void SpawnPodiumCharacters()
    {
        if (_podiumRigInstance == null || _scoreManager == null)
        {
            return;
        }

        IReadOnlyList<PlayerRoundResultData> results = _scoreManager.LatestRoundResults;

        int podiumPlayerCount = Mathf.Min(3, results.Count);

        for (int i = 0; i < podiumPlayerCount; i++)
        {
            PlayerRoundResultData resultData = results[i];
            int colorIndex = GetPlayerColorIndex(resultData.ClientId);

            _podiumRigInstance.SpawnCharacter(i, colorIndex);
        }
    }

    private int GetPlayerColorIndex(ulong clientId)
    {
        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;

        if (roomManager == null)
        {
            return 0;
        }

        for (int i = 0; i < roomManager.PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = roomManager.PlayerList[i];

            if (playerData.ClientId == clientId)
            {
                return Mathf.Max(0, playerData.ColorIndex);
            }
        }

        return 0;
    }
}