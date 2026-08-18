using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public sealed class JoinRoomPopupView : UIBase
{
    [SerializeField] private TMP_InputField InputField_RoomCode;
    [SerializeField] private UIButton Button_JoinRoom;
    [SerializeField] private UIButton Button_Cancel;
    [SerializeField] private TMP_Text Text_ValidationMessage;

    private JoinRoomPopupViewModel _viewModel;
    private string _nickname;

    public override UILayer Layer => UILayer.Popup;

    public void SetNickname(string nickname)
    {
        _nickname = nickname?.Trim();
        _viewModel?.SetNickname(_nickname);
    }

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && InputField_RoomCode != null && Button_JoinRoom != null && Button_Cancel != null && Text_ValidationMessage != null;
    }

    protected override void InitializeUI()
    {
        _viewModel = new JoinRoomPopupViewModel();

        InputField_RoomCode.characterLimit = 10;
    }

    protected override void BindEvents()
    {
        InputField_RoomCode.onValueChanged.AddListener(HandleRoomCodeValueChanged);

        Button_JoinRoom.BindOnClickButtonEvent(HandleClickJoinRoomButton);
        Button_Cancel.BindOnClickButtonEvent(HandleClickCancelButton);

        _viewModel.OnJoinRoomRequested += HandleJoinRoomRequested;
        _viewModel.OnValidationFailed += HandleValidationFailed;
    }

    protected override void UnbindEvents()
    {
        InputField_RoomCode.onValueChanged.RemoveListener(HandleRoomCodeValueChanged);

        Button_JoinRoom.UnbindOnClickButtonEvent(HandleClickJoinRoomButton);
        Button_Cancel.UnbindOnClickButtonEvent(HandleClickCancelButton);

        if (_viewModel == null)
        {
            return;
        }

        _viewModel.OnJoinRoomRequested -= HandleJoinRoomRequested;
        _viewModel.OnValidationFailed -= HandleValidationFailed;
    }

    protected override void RefreshUI()
    {
        InputField_RoomCode.text = string.Empty;
        Text_ValidationMessage.text = string.Empty;

        _viewModel.SetNickname(_nickname);
        _viewModel.SetRoomCode(string.Empty);
    }

    protected override void ReleaseUI()
    {
        _viewModel = null;
        _nickname = string.Empty;
    }

    private void HandleRoomCodeValueChanged(string roomCode)
    {
        Text_ValidationMessage.text = string.Empty;
        _viewModel?.SetRoomCode(roomCode);
    }

    private void HandleClickJoinRoomButton()
    {
        _viewModel?.RequestJoinRoom();
    }

    private void HandleClickCancelButton()
    {
        Close();
    }

    private void HandleJoinRoomRequested(string nickname, string roomCode)
    {
        JoinRoomAsync(nickname, roomCode).Forget();
    }

    private async UniTask JoinRoomAsync(string nickname, string roomCode)
    {
        NetCodeNetworkManager networkManager = NetCodeNetworkManager.Inst;

        if (networkManager == null)
        {
            HandleValidationFailed("네트워크 매니저 찾을 수 없음");
            return;
        }

        Button_JoinRoom.SetInteractable(false);
        networkManager.SetLocalPlayerName(nickname);

        bool isStarted = await networkManager.StartAsClientWithRelay(roomCode);

        Button_JoinRoom.SetInteractable(true);

        if (!isStarted)
        {
            HandleValidationFailed("방 참가에 실패했습니다.");
            return;
        }

        Close();
        ChangeToWaitingRoom();
    }

    private void HandleValidationFailed(string message)
    {
        Text_ValidationMessage.text = message;
    }

    private void ChangeToWaitingRoom()
    {
        GameManager gameManager = GameManager.Inst;

        if (gameManager == null)
        {
            HandleValidationFailed("게임 매니저를 찾을 수 없음.");

            return;
        }

        gameManager.TryChangeGameState(GameState.WaitingRoom);
    }
}