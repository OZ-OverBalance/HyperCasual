using TMPro;
using UnityEngine;

public sealed class LobbyView : UIBase
{
    [SerializeField] private TMP_InputField InputField_Nickname;
    [SerializeField] private TMP_InputField InputField_RoomCode;

    [SerializeField] private UIButton Button_CreateRoom;
    [SerializeField] private UIButton Button_JoinRoom;
    [SerializeField] private UIButton Button_Back;

    [SerializeField] private TMP_Text Text_ValidationMessage;

    private LobbyViewModel _viewModel;

    public override UILayer Layer => UILayer.Main;

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && InputField_Nickname != null && InputField_RoomCode != null && Button_CreateRoom != null && Button_JoinRoom != null && Button_Back != null && Text_ValidationMessage != null;
    }

    protected override void InitializeUI()
    {
        _viewModel = new LobbyViewModel(GameManager.Inst);

        InputField_Nickname.characterLimit = 12;
        InputField_RoomCode.characterLimit = 10;

        // [TODO] 네트워크 연결 전 비활성화
        Button_CreateRoom.SetInteractable(false);
        Button_JoinRoom.SetInteractable(false);
    }

    protected override void BindEvents()
    {
        InputField_Nickname.onValueChanged.AddListener(HandleNicknameValueChanged);
        InputField_RoomCode.onValueChanged.AddListener(HandleRoomCodeValueChanged);

        Button_CreateRoom.BindOnClickButtonEvent(HandleClickCreateRoomButton);
        Button_JoinRoom.BindOnClickButtonEvent(HandleClickJoinRoomButton);
        Button_Back.BindOnClickButtonEvent(HandleClickBackButton);

        _viewModel.OnCreateRoomRequested += HandleCreateRoomRequested;
        _viewModel.OnJoinRoomRequested += HandleJoinRoomRequested;
        _viewModel.OnValidationFailed += HandleValidationFailed;
    }

    protected override void UnbindEvents()
    {
        InputField_Nickname.onValueChanged.RemoveListener(HandleNicknameValueChanged);
        InputField_RoomCode.onValueChanged.RemoveListener(HandleRoomCodeValueChanged);

        Button_CreateRoom.UnbindOnClickButtonEvent(HandleClickCreateRoomButton);
        Button_JoinRoom.UnbindOnClickButtonEvent(HandleClickJoinRoomButton);
        Button_Back.UnbindOnClickButtonEvent(HandleClickBackButton);

        if (_viewModel == null)
        {
            return;
        }

        _viewModel.OnCreateRoomRequested -= HandleCreateRoomRequested;
        _viewModel.OnJoinRoomRequested -= HandleJoinRoomRequested;
        _viewModel.OnValidationFailed -= HandleValidationFailed;
    }

    protected override void RefreshUI()
    {
        Text_ValidationMessage.text = string.Empty;

        _viewModel.SetNickname(InputField_Nickname.text);
        _viewModel.SetRoomCode(InputField_RoomCode.text);
    }

    protected override void ReleaseUI()
    {
        _viewModel = null;
    }

    private void HandleNicknameValueChanged(string nickname)
    {
        Text_ValidationMessage.text = string.Empty;
        _viewModel?.SetNickname(nickname);
    }

    private void HandleRoomCodeValueChanged(string roomCode)
    {
        Text_ValidationMessage.text = string.Empty;
        _viewModel?.SetRoomCode(roomCode);
    }

    private void HandleClickCreateRoomButton()
    {
        _viewModel?.RequestCreateRoom();
    }

    private void HandleClickJoinRoomButton()
    {
        _viewModel?.RequestJoinRoom();
    }

    private void HandleClickBackButton()
    {
        _viewModel?.ReturnToTitle();
    }

    private void HandleCreateRoomRequested(string nickname)
    {
        // [TODO] 네트워크 병합 후 방 생성 함수 연결
        Debug.Log($"LobbyView - 방 생성 요청 : {nickname}");
    }

    private void HandleJoinRoomRequested(
        string nickname,
        string roomCode)
    {
        // [TODO] 네트워크 병합 후 방 참가 함수 연결
        Debug.Log($"LobbyView - 방 참가 요청 : {nickname}, {roomCode}");
    }

    private void HandleValidationFailed(string message)
    {
        Text_ValidationMessage.text = message;
    }
}