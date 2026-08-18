using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public sealed class LobbyView : UIBase
{
    [SerializeField] private TMP_InputField InputField_Nickname;

    [SerializeField] private UIButton Button_CreateRoom;
    [SerializeField] private UIButton Button_JoinRoom;
    [SerializeField] private UIButton Button_Back;

    [SerializeField] private TMP_Text Text_ValidationMessage;

    private LobbyViewModel _viewModel;

    public override UILayer Layer => UILayer.Main;

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && InputField_Nickname != null && Button_CreateRoom != null && Button_JoinRoom != null && Button_Back != null && Text_ValidationMessage != null;
    }

    protected override void InitializeUI()
    {
        _viewModel = new LobbyViewModel(GameManager.Inst);

        InputField_Nickname.characterLimit = 12;

        Button_CreateRoom.SetInteractable(true);
        Button_JoinRoom.SetInteractable(true);
    }

    protected override void BindEvents()
    {
        InputField_Nickname.onValueChanged.AddListener(HandleNicknameValueChanged);

        Button_CreateRoom.BindOnClickButtonEvent(HandleClickCreateRoomButton);
        Button_JoinRoom.BindOnClickButtonEvent(HandleClickJoinRoomButton);
        Button_Back.BindOnClickButtonEvent(HandleClickBackButton);

        _viewModel.OnCreateRoomRequested += HandleCreateRoomRequested;
        _viewModel.OnValidationFailed += HandleValidationFailed;
    }

    protected override void UnbindEvents()
    {
        InputField_Nickname.onValueChanged.RemoveListener(HandleNicknameValueChanged);

        Button_CreateRoom.UnbindOnClickButtonEvent(HandleClickCreateRoomButton);
        Button_JoinRoom.UnbindOnClickButtonEvent(HandleClickJoinRoomButton);
        Button_Back.UnbindOnClickButtonEvent(HandleClickBackButton);

        if (_viewModel == null)
        {
            return;
        }

        _viewModel.OnCreateRoomRequested -= HandleCreateRoomRequested;
        _viewModel.OnValidationFailed -= HandleValidationFailed;
    }

    protected override void RefreshUI()
    {
        Text_ValidationMessage.text = string.Empty;

        _viewModel.SetNickname(InputField_Nickname.text);
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

    private void HandleClickCreateRoomButton()
    {
        _viewModel?.RequestCreateRoom();
    }

    private void HandleClickJoinRoomButton()
    {
        string nickname = InputField_Nickname.text?.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            HandleValidationFailed("닉네임을 입력해 주세요.");
            return;
        }

        UIManager.Inst.ShowJoinRoomPopupUIAsync(nickname).Forget();
    }

    private void HandleClickBackButton()
    {
        _viewModel?.ReturnToTitle();
    }

    private void HandleCreateRoomRequested(string nickname)
    {
        CreateRoomAsync(nickname).Forget();
    }

    private async UniTask CreateRoomAsync(string nickname)
    {
        NetCodeNetworkManager networkManager = NetCodeNetworkManager.Inst;

        if (networkManager == null)
        {
            HandleValidationFailed("네트워크 매니저를 찾을 수 없음");
            return;
        }

        networkManager.SetLocalPlayerName(nickname);

        string joinCode = await networkManager.StartAsHostWithRelay(4);

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            HandleValidationFailed("방 생성 실패");
            return;
        }

        Text_ValidationMessage.text = $"방 코드 : {joinCode}";

        GUIUtility.systemCopyBuffer = joinCode;

        _viewModel?.ChangeToWaitingRoom();
    }

    private void HandleValidationFailed(string message)
    {
        Text_ValidationMessage.text = message;
    }
}