using TMPro;
using UnityEngine;

public sealed class InGameHUDView : UIBase
{

    [SerializeField] private TMP_Text _textPhase;
    [SerializeField] private TMP_Text _textTimer;


    [SerializeField] private GameObject _panelSubmissionStatus;
    [SerializeField] private TMP_Text _textSubmissionCount;

    private NetCodeMapManager _mapManager;

    private GameState _displayedState;
    private bool _hasDisplayedState;
    private bool _isRunTimerBound;

    public override UILayer Layer => UILayer.Main;

    private void Update()
    {
        if (!IsOpened)
        {
            return;
        }

        RefreshPhaseIfChanged();

        if (_displayedState == GameState.Run && !_isRunTimerBound)
        {
            TryBindRunTimer();
        }
    }

    protected override bool ValidateReferences()
    {
        return base.ValidateReferences() && _textPhase != null && _textTimer != null;
    }

    protected override void InitializeUI()
    {
        _textPhase.text = string.Empty;
        _textTimer.text = "--:--";

        if (_panelSubmissionStatus != null)
        {
            _panelSubmissionStatus.SetActive(false);
        }
    }

    protected override void RefreshUI()
    {
        _hasDisplayedState = false;
        RefreshPhaseIfChanged();
    }

    protected override void UnbindEvents()
    {
        UnbindRunTimer();
        _hasDisplayedState = false;
    }

    protected override void ReleaseUI()
    {
        UnbindRunTimer();
        _hasDisplayedState = false;
    }

    private void RefreshPhaseIfChanged()
    {
        if (GameManager.Inst == null)
        {
            return;
        }

        GameState currentState = GameManager.Inst.CurrentState;

        if (_hasDisplayedState && _displayedState == currentState)
        {
            return;
        }

        _displayedState = currentState;
        _hasDisplayedState = true;

        switch (currentState)
        {
            case GameState.Build:
                ShowBuildPhase();
                break;

            case GameState.Run:
                ShowRunPhase();
                break;

            default:
                HidePhaseInformation();
                break;
        }
    }

    private void ShowBuildPhase()
    {
        UnbindRunTimer();

        _textPhase.text = "BUILD PHASE";

        _textTimer.text = "--:--";

        if (_panelSubmissionStatus != null)
        {
            _panelSubmissionStatus.SetActive(true);
        }
    }

    private void ShowRunPhase()
    {
        _textPhase.text = "RUN PHASE";

        if (_panelSubmissionStatus != null)
        {
            _panelSubmissionStatus.SetActive(false);
        }

        if (!TryBindRunTimer())
        {
            _textTimer.text = "--:--";
        }
    }

    private void HidePhaseInformation()
    {
        UnbindRunTimer();

        _textPhase.text = string.Empty;
        _textTimer.text = "--:--";

        if (_panelSubmissionStatus != null)
        {
            _panelSubmissionStatus.SetActive(false);
        }
    }

    private bool TryBindRunTimer()
    {
        if (_isRunTimerBound)
        {
            return true;
        }

        NetCodeMapManager mapManager = NetCodeMapManager.Instance;

        if (mapManager == null || !mapManager.IsSpawned)
        {
            return false;
        }

        _mapManager = mapManager;
        _mapManager.remainingTime.OnValueChanged += HandleRunRemainingTimeChanged;
        _isRunTimerBound = true;

        SetRemainingTime(_mapManager.remainingTime.Value);
        return true;
    }

    private void UnbindRunTimer()
    {
        if (_mapManager != null && _isRunTimerBound)
        {
            _mapManager.remainingTime.OnValueChanged -= HandleRunRemainingTimeChanged;
        }

        _mapManager = null;
        _isRunTimerBound = false;
    }

    private void HandleRunRemainingTimeChanged(int previousValue, int currentValue)
    {
        SetRemainingTime(currentValue);
    }

    public void SetRemainingTime(int remainingSeconds)
    {
        remainingSeconds = Mathf.Max(0, remainingSeconds);

        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;

        _textTimer.text = $"{minutes:00}:{seconds:00}";
    }

    // 빌드 제출 현황 연결 예정
    public void SetSubmissionProgress(int submittedPlayerCount, int totalPlayerCount)
    {
        if (_textSubmissionCount == null)
        {
            return;
        }

        totalPlayerCount = Mathf.Max(0, totalPlayerCount);
        submittedPlayerCount = Mathf.Clamp(submittedPlayerCount, 0, totalPlayerCount);

        _textSubmissionCount.text = $"{submittedPlayerCount} / {totalPlayerCount}";
    }
}