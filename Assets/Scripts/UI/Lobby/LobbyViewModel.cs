using System;
using UnityEngine;

public sealed class LobbyViewModel
{
    private const int MinNicknameLength = 2;
    private const int MaxNicknameLength = 12;

    private readonly GameManager _gameManager;

    private string _nickname;

    public event Action<string> OnCreateRoomRequested;
    public event Action<string> OnJoinRoomRequested;
    public event Action<string> OnValidationFailed;

    public LobbyViewModel(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void SetNickname(string nickname)
    {
        _nickname = nickname?.Trim();
    }

    public void RequestCreateRoom()
    {
        if (!ValidateNickname())
        {
            return;
        }

        OnCreateRoomRequested?.Invoke(_nickname);
    }

    public void RequestJoinRoom()
    {
        if (!ValidateNickname())
        {
            return;
        }

        OnJoinRoomRequested?.Invoke(_nickname);
    }

    public void ReturnToTitle()
    {
        if (_gameManager == null)
        {
            Debug.LogError("LobbyViewModel - GameManager가 초기화되지 않음");
            return;
        }

        if (!_gameManager.TryChangeGameState(GameState.Title))
        {
            Debug.LogWarning("LobbyViewModel - Title 상태로 변경할 수 없음");
        }
    }

    private bool ValidateNickname()
    {
        if (string.IsNullOrWhiteSpace(_nickname))
        {
            OnValidationFailed?.Invoke("닉네임을 입력해 주세요.");
            return false;
        }

        if (_nickname.Length < MinNicknameLength || _nickname.Length > MaxNicknameLength)
        {
            OnValidationFailed?.Invoke($"닉네임은 {MinNicknameLength}~{MaxNicknameLength}자만 가능합니다.");
            return false;
        }

        return true;
    }

    public void ChangeToWaitingRoom()
    {
        if (_gameManager == null)
        {
            return;
        }

        _gameManager.TryChangeGameState(GameState.WaitingRoom);
    }
}