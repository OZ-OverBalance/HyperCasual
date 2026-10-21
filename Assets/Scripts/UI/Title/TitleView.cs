using UnityEngine;

public sealed class TitleView : UIBase
{
    [Header("Buttons")]
    [SerializeField] private UIButton Button_Start;
    [SerializeField] private UIButton Button_Settings;
    [SerializeField] private UIButton Button_Exit;

    private TitleViewModel _viewModel;

    public override UILayer Layer => UILayer.Main;

    public void SetViewModel(TitleViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences()&& Button_Start != null&& Button_Settings != null&& Button_Exit != null;
    }

    protected override void InitializeUI()
    {
    }

    protected override void BindEvents()
    {
        Button_Start.BindOnClickButtonEvent(OnClickStart);
        Button_Exit.BindOnClickButtonEvent(OnClickExit);
        Button_Settings.BindOnClickButtonEvent(OnClickSetting);
    }

    protected override void UnbindEvents()
    {
        Button_Start.UnbindOnClickButtonEvent(OnClickStart);
        Button_Exit.UnbindOnClickButtonEvent(OnClickExit);
        Button_Settings.UnbindOnClickButtonEvent(OnClickSetting);
    }

    protected override void ReleaseUI()
    {
        _viewModel = null;
    }

    private void OnClickStart()
    {
        if (_viewModel == null)
        {
            Debug.LogError("TitleView - ViewModel 연결되지 않음");
            return;
        }

        if (!_viewModel.StartGame())
        {
            Debug.LogError("TitleView - Lobby 상태 전환 실패");
        }

        UIManager.Inst.CloseUI(UIType.SettingsPopup);
    }

    private void OnClickExit()
    {
        if (_viewModel == null)
        {
            return;
        }

        _viewModel.ExitGame();
    }

    private void OnClickSetting()
    {
        if (_viewModel == null)
        {
            Debug.LogError("TitleView - ViewModel 연결되지 않음");
            return;
        }

        _viewModel.OpenGameSetting();
    }
}