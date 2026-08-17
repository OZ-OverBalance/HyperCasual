using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class WaitingRoomView : UIBase
{
    [Header("Room Info")]
    [SerializeField] private TMP_Text Text_RoomCode;
    [SerializeField] private UIButton Button_CopyRoomCode;
    [SerializeField] private UIButton Button_Leave;

    [Header("Other Players")]
    [SerializeField] private Transform Transform_PlayerSlotRoot;
    [SerializeField] private WaitingRoomPlayerSlot Prefab_PlayerSlot;

    [Header("Local Player")]
    [SerializeField] private TMP_Text Text_LocalNickname;
    [SerializeField] private Image Image_LocalCharacter;
    [SerializeField] private TMP_Text Text_LocalReadyState;
    [SerializeField] private GameObject Object_LocalHostBadge;

    [Header("Control")]
    [SerializeField] private UIButton Button_Ready;
    [SerializeField] private UIButton Button_StartGame;
    [SerializeField] private TMP_Text Text_StatusMessage;

    private readonly Dictionary<ulong, WaitingRoomPlayerSlot> _playerSlots = new();

    private NetCodeRoomManager _roomManager;
    private string _roomCode;
    private bool _isLocalReady;
    private bool _isLocalHost;

    public override UILayer Layer => UILayer.Main;

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences()
            && Text_RoomCode != null
            && Button_CopyRoomCode != null
            && Button_Leave != null
            && Transform_PlayerSlotRoot != null
            && Prefab_PlayerSlot != null
            && Text_LocalNickname != null
            && Image_LocalCharacter != null
            && Text_LocalReadyState != null
            && Object_LocalHostBadge != null
            && Button_Ready != null
            && Button_StartGame != null
            && Text_StatusMessage != null;
    }

    protected override void InitializeUI()
    {
        _roomCode = string.Empty;
        _isLocalReady = false;
        _isLocalHost = false;
    }

    protected override void BindEvents()
    {
        Button_CopyRoomCode.BindOnClickButtonEvent(HandleClickCopyRoomCodeButton);
        Button_Ready.BindOnClickButtonEvent(HandleClickReadyButton);
        Button_StartGame.BindOnClickButtonEvent(HandleClickStartGameButton);
        Button_Leave.BindOnClickButtonEvent(HandleClickLeaveButton);
    }

    protected override void UnbindEvents()
    {
        Button_CopyRoomCode.UnbindOnClickButtonEvent(HandleClickCopyRoomCodeButton);
        Button_Ready.UnbindOnClickButtonEvent(HandleClickReadyButton);
        Button_StartGame.UnbindOnClickButtonEvent(HandleClickStartGameButton);
        Button_Leave.UnbindOnClickButtonEvent(HandleClickLeaveButton);

        if (_roomManager != null)
        {
            _roomManager.OnPlayerListChanged -= HandlePlayerListChanged;
        }
    }

    protected override void RefreshUI()
    {
        Text_RoomCode.text = string.IsNullOrWhiteSpace(_roomCode) ? "-" : $"{_roomCode}";
        Text_StatusMessage.text = string.Empty;

        Object_LocalHostBadge.SetActive(false);
        Button_StartGame.gameObject.SetActive(false);

        RefreshLocalReadyState();
        RefreshButtonState(false);

        BindRoomManagerAsync().Forget();
    }

    protected override void ReleaseUI()
    {
        if (_roomManager != null)
        {
            _roomManager.OnPlayerListChanged -= HandlePlayerListChanged;
        }

        ClearPlayerSlots();

        _roomManager = null;
        _roomCode = string.Empty;
        _isLocalReady = false;
        _isLocalHost = false;
    }

    public void SetRoomCode(string roomCode)
    {
        _roomCode = roomCode?.Trim();
        Text_RoomCode.text = string.IsNullOrWhiteSpace(_roomCode) ? "-" : $"{_roomCode}";
    }

    public void SetLocalPlayer(string nickname, Sprite characterSprite, bool isReady, bool isHost)
    {
        Text_LocalNickname.text = nickname;
        Image_LocalCharacter.sprite = characterSprite;
        Image_LocalCharacter.enabled = characterSprite != null;

        _isLocalReady = isReady;
        _isLocalHost = isHost;

        Object_LocalHostBadge.SetActive(isHost);
        Text_LocalReadyState.gameObject.SetActive(!isHost);

        RefreshLocalReadyState();
        RefreshButtonState(false);
    }

    public void AddPlayerSlot(ulong clientId, string nickname, Sprite characterSprite, bool isReady, bool isHost)
    {
        if (_playerSlots.ContainsKey(clientId))
        {
            UpdatePlayerSlot(clientId, nickname, characterSprite, isReady, isHost);
            return;
        }

        WaitingRoomPlayerSlot playerSlot = Instantiate(Prefab_PlayerSlot, Transform_PlayerSlotRoot);

        playerSlot.InitializeSlot(clientId, nickname, characterSprite, isReady, isHost);

        _playerSlots.Add(clientId, playerSlot);
    }

    public void UpdatePlayerSlot(ulong clientId, string nickname, Sprite characterSprite, bool isReady, bool isHost)
    {
        if (!_playerSlots.TryGetValue(clientId, out WaitingRoomPlayerSlot playerSlot))
        {
            AddPlayerSlot(clientId, nickname, characterSprite, isReady, isHost);
            return;
        }

        playerSlot.InitializeSlot(clientId, nickname, characterSprite, isReady, isHost);
    }

    public void RemovePlayerSlot(ulong clientId)
    {
        if (!_playerSlots.TryGetValue(clientId, out WaitingRoomPlayerSlot playerSlot))
        {
            return;
        }

        _playerSlots.Remove(clientId);
        Destroy(playerSlot.gameObject);
    }

    public void RefreshButtonState(bool areAllGuestPlayersReady)
    {
        Button_Ready.gameObject.SetActive(!_isLocalHost);
        Button_StartGame.gameObject.SetActive(_isLocalHost);
        Button_StartGame.SetInteractable(_isLocalHost && areAllGuestPlayersReady);
    }

    private void ClearPlayerSlots()
    {
        foreach (WaitingRoomPlayerSlot playerSlot in _playerSlots.Values)
        {
            if (playerSlot != null)
            {
                Destroy(playerSlot.gameObject);
            }
        }

        _playerSlots.Clear();
    }

    private void RefreshLocalReadyState()
    {
        Text_LocalReadyState.text = _isLocalReady ? "READY!" : "WAITING";

        Text_LocalReadyState.color = _isLocalReady ? new Color(0.45f, 1f, 0.45f) : Color.white;
    }

    private void HandleClickCopyRoomCodeButton()
    {
        if (string.IsNullOrWhiteSpace(_roomCode))
        {
            Text_StatusMessage.text = "복사할 방 코드가 없습니다.";
            return;
        }

        GUIUtility.systemCopyBuffer = _roomCode;
        Text_StatusMessage.text = "방 코드 복사 완료!";
    }

    private void HandleClickReadyButton()
    {
        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;

        if (roomManager == null)
        {
            Text_StatusMessage.text = "대기실 정보를 불러오는 중입니다.";
            return;
        }

        roomManager.ToggleReadyServerRpc();
    }

    private void HandleClickStartGameButton()
    {
        Text_StatusMessage.text = "게임 시작 기능을 연결해야 합니다.";
    }

    private void HandleClickLeaveButton()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        GameManager gameManager = GameManager.Inst;

        if (gameManager == null)
        {
            Text_StatusMessage.text = "게임 매니저를 찾을 수 없습니다.";
            return;
        }

        bool isChanged = gameManager.TryChangeGameState(GameState.Lobby);

        if (!isChanged)
        {
            Text_StatusMessage.text = "로비로 이동할 수 없습니다.";
        }
    }

    private async UniTask BindRoomManagerAsync()
    {
        while (NetCodeRoomManager.Instance == null)
        {
            if (this == null)
            {
                return;
            }

            await UniTask.Yield();
        }

        _roomManager = NetCodeRoomManager.Instance;

        _roomManager.OnPlayerListChanged -= HandlePlayerListChanged;
        _roomManager.OnPlayerListChanged += HandlePlayerListChanged;

        RefreshPlayerList();
    }

    private void HandlePlayerListChanged()
    {
        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        if (_roomManager == null || NetworkManager.Singleton == null)
        {
            return;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        ulong hostClientId = NetworkManager.ServerClientId;

        ClearPlayerSlots();

        bool hasGuestPlayer = false;
        bool areAllGuestPlayersReady = true;

        for (int i = 0; i < _roomManager.PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = _roomManager.PlayerList[i];

            bool isLocalPlayer = playerData.ClientId == localClientId;
            bool isHost = playerData.ClientId == hostClientId;

            if (!isHost)
            {
                hasGuestPlayer = true;

                if (!playerData.IsReady)
                {
                    areAllGuestPlayersReady = false;
                }
            }

            if (isLocalPlayer)
            {
                SetLocalPlayer(playerData.PlayerName.ToString(), Image_LocalCharacter.sprite, playerData.IsReady, isHost);
                continue;
            }

            AddPlayerSlot(playerData.ClientId, playerData.PlayerName.ToString(), null, playerData.IsReady, isHost);
        }

        bool canStartGame = hasGuestPlayer && areAllGuestPlayersReady;

        RefreshButtonState(canStartGame);
    }
}