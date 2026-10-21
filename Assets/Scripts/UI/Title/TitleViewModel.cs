using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class TitleViewModel
{
    private readonly GameManager _gameManager;

    public TitleViewModel(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    // 타이틀에서 로비 상태로 전환합니다.
    public bool StartGame()
    {
        if (_gameManager == null)
        {
            return false;
        }

        return _gameManager.TryChangeGameState(
            GameState.Lobby);
    }

    // 게임 종료를 요청합니다.
    public bool ExitGame()
    {
        if (_gameManager == null)
        {
            return false;
        }

        _gameManager.QuitGame();
        return true;
    }

    // 설정 UI켜기
    public void OpenGameSetting()
    {
        OpenGameSettingAsync().Forget();
    }

    private async UniTaskVoid OpenGameSettingAsync()
    {
        if (UIManager.Inst == null)
        {
            Debug.LogError("TitleViewModel - UIManager 없음");
            return;
        }

        SettingView settingView = await UIManager.Inst.ShowUIAsync<SettingView>(UIType.SettingsPopup);

        if (settingView == null)
        {
            Debug.LogError("TitleViewModel - SettingsPopup UI 없음");
        }
    }
}