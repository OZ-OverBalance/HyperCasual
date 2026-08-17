using System;

public sealed class JoinRoomPopupViewModel
{
    private string _nickname;
    private string _roomCode;

    public event Action<string, string> OnJoinRoomRequested;
    public event Action<string> OnValidationFailed;

    public void SetNickname(string nickname)
    {
        _nickname = nickname?.Trim();
    }

    public void SetRoomCode(string roomCode)
    {
        _roomCode = roomCode?.Trim();
    }

    public void RequestJoinRoom()
    {
        if (string.IsNullOrWhiteSpace(_nickname))
        {
            OnValidationFailed?.Invoke("닉네임을 입력해 주세요.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_roomCode))
        {
            OnValidationFailed?.Invoke("방 코드를 입력해 주세요.");
            return;
        }

        OnJoinRoomRequested?.Invoke(_nickname, _roomCode);
    }
}