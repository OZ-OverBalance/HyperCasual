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
    [SerializeField] private RawImage RawImage_LocalCharacterPreview; 
    [SerializeField] private TMP_Text Text_LocalReadyState;
    [SerializeField] private GameObject Object_LocalHostBadge;
    [SerializeField] private GameObject Prefab_LocalPreviewRig;

    [Header("Control")]
    [SerializeField] private UIButton Button_Ready;
    [SerializeField] private UIButton Button_StartGame;
    [SerializeField] private TMP_Text Text_StatusMessage;

    [Header("Color Palette")]
    [SerializeField] private Button[] Button_ColorPalettes;

    private GameObject _spawnedLocalRig;
    private PlayerColor _previewPlayerColor;
    private RenderTexture _localPreviewRT;

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
            && RawImage_LocalCharacterPreview != null
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
        if (Button_ColorPalettes != null)
        {
            for (int i = 0; i < Button_ColorPalettes.Length; i++)
            {
                int colorIndex = i;
                Button_ColorPalettes[i].onClick.AddListener(() => HandleClickColorButton(colorIndex));
            }
        }
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

        if (Button_ColorPalettes != null)
        {
            foreach (var btn in Button_ColorPalettes)
            {
                if (btn != null) btn.onClick.RemoveAllListeners();
            }
        }
    }

    private void HandleClickColorButton(int colorIndex)
    {
        if (_isLocalReady)
        {
            Text_StatusMessage.text = "준비 완료 상태에서는 색상을 변경할 수 없습니다.";
            return;
        }

        if (_roomManager == null) return;

        _roomManager.RequestChangeColorServerRpc(colorIndex);
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

        if (_spawnedLocalRig != null)
        {
            Destroy(_spawnedLocalRig);
            _spawnedLocalRig = null;
        }

        if (_localPreviewRT != null)
        {
            _localPreviewRT.Release();
            Destroy(_localPreviewRT);
            _localPreviewRT = null;
        }

        _previewPlayerColor = null;
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

    public void SetLocalPlayer(string nickname, int colorIndex, bool isReady, bool isHost)
    {
        Text_LocalNickname.text = nickname;
        _isLocalReady = isReady;
        _isLocalHost = isHost;

        if (_spawnedLocalRig == null && Prefab_LocalPreviewRig != null)
        {
            _spawnedLocalRig = Instantiate(Prefab_LocalPreviewRig, new Vector3(0, -100f, 0), Quaternion.identity);
            _previewPlayerColor = _spawnedLocalRig.GetComponentInChildren<PlayerColor>();

            Camera previewCam = _spawnedLocalRig.GetComponentInChildren<Camera>();
            if (previewCam != null)
            {
                if (_localPreviewRT == null)
                {
                    _localPreviewRT = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                    _localPreviewRT.Create();
                }

                previewCam.targetTexture = _localPreviewRT;
                RawImage_LocalCharacterPreview.texture = _localPreviewRT;
                previewCam.enabled = true;
            }
        }

        if (_previewPlayerColor != null)
        {
            _previewPlayerColor.ApplyMaterial(colorIndex);
        }

        Object_LocalHostBadge.SetActive(isHost);
        Text_LocalReadyState.gameObject.SetActive(!isHost);

        RefreshLocalReadyState();
        RefreshButtonState(false);
    }

    public void AddPlayerSlot(ulong clientId, string nickname, int colorIndex, bool isReady, bool isHost)
    {
        if (_playerSlots.ContainsKey(clientId))
        {
            UpdatePlayerSlot(clientId, nickname, colorIndex, isReady, isHost);
            return;
        }

        WaitingRoomPlayerSlot playerSlot = Instantiate(Prefab_PlayerSlot, Transform_PlayerSlotRoot);
        playerSlot.InitializeSlot(clientId, nickname, colorIndex, isReady, isHost);
        _playerSlots.Add(clientId, playerSlot);
    }

    public void UpdatePlayerSlot(ulong clientId, string nickname, int colorIndex, bool isReady, bool isHost)
    {
        if (!_playerSlots.TryGetValue(clientId, out WaitingRoomPlayerSlot playerSlot))
        {
            AddPlayerSlot(clientId, nickname, colorIndex, isReady, isHost);
            return;
        }

        playerSlot.InitializeSlot(clientId, nickname, colorIndex, isReady, isHost);
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
        NetCodeRoomManager roomManager = NetCodeRoomManager.Instance;

        if (roomManager == null)
        {
            Text_StatusMessage.text = "대기실 정보 불러오는 중...";
            return;
        }

        Button_StartGame.SetInteractable(false);
        Text_StatusMessage.text = "게임 시작 중..";

        roomManager.RequestStartGameServerRpc();
    }

    private void HandleClickLeaveButton()
    {
        GameManager gameManager = GameManager.Inst;

        if (gameManager == null)
        {
            Text_StatusMessage.text = "게임 매니저를 찾을 수 없습니다..";
            return;
        }

        bool isChanged = gameManager.TryChangeGameState(GameState.Lobby);

        if (!isChanged)
        {
            Text_StatusMessage.text = "로비로 이동할 수 없습니다.";
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }

    private async UniTask BindRoomManagerAsync()
    {
        while (NetCodeRoomManager.Instance == null)
        {
            await UniTask.Yield();

            if (this == null)
            {
                return;
            }
        }

        if (this == null)
        {
            return;
        }

        _roomManager = NetCodeRoomManager.Instance;

        if (_roomManager == null)
        {
            return;
        }

        _roomManager.OnPlayerListChanged -= HandlePlayerListChanged;
        _roomManager.OnPlayerListChanged += HandlePlayerListChanged;

        if (this == null)
        {
            _roomManager.OnPlayerListChanged -= HandlePlayerListChanged;
            return;
        }

        RefreshPlayerList();
    }

    private void HandlePlayerListChanged()
    {
        if (this == null)
        {
            if (_roomManager != null)
            {
                _roomManager.OnPlayerListChanged -= HandlePlayerListChanged;
            }

            return;
        }

        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        if (this == null || _roomManager == null || NetworkManager.Singleton == null || Button_Ready == null || Button_StartGame == null || Image_LocalCharacter == null)
        {
            return;
        }

        // 아래에는 기존 코드 그대로

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        ulong hostClientId = NetworkManager.ServerClientId;

        ClearPlayerSlots();

        bool hasGuestPlayer = false;
        bool areAllGuestPlayersReady = true;

        HashSet<int> takenColors = new HashSet<int>();

        for (int i = 0; i < _roomManager.PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = _roomManager.PlayerList[i];
            takenColors.Add(playerData.ColorIndex);

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
                SetLocalPlayer(playerData.PlayerName.ToString(), playerData.ColorIndex, playerData.IsReady, isHost);
                continue;
            }

            AddPlayerSlot(playerData.ClientId, playerData.PlayerName.ToString(), playerData.ColorIndex, playerData.IsReady, isHost);
        }

        if (Button_ColorPalettes != null)
        {
            for (int i = 0; i < Button_ColorPalettes.Length; i++)
            {
                if (Button_ColorPalettes[i] != null)
                {
                    bool isTakenByOther = takenColors.Contains(i);
                    Button_ColorPalettes[i].interactable = !isTakenByOther;
                }
            }
        }

        bool canStartGame = hasGuestPlayer && areAllGuestPlayersReady;
        RefreshButtonState(canStartGame);
    }
}