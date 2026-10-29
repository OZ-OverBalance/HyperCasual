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
    private bool _isBuildDataBound;
    private bool _isRunTimerBound;

    public override UILayer Layer => UILayer.Main;

    private void Update()
    {
        if (!IsOpened)
        {
            return;
        }

        RefreshPhaseIfChanged();

        if (_displayedState == GameState.Build && !_isBuildDataBound)
        {
            TryBindBuildData();
        }

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
        UnbindBuildData();
        UnbindRunTimer();

        _hasDisplayedState = false;
    }

    protected override void ReleaseUI()
    {
        UnbindBuildData();
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

        SetSubmissionProgress(0, 0);
        TryBindBuildData();
    }

    private void ShowRunPhase()
    {
        UnbindBuildData();

        _textPhase.text = "RUN PHASE";
        _textTimer.text = "--:--";

        if (_panelSubmissionStatus != null)
        {
            _panelSubmissionStatus.SetActive(false);
        }

        TryBindRunTimer();
    }

    private void HidePhaseInformation()
    {
        UnbindBuildData();
        UnbindRunTimer();

        _textPhase.text = string.Empty;
        _textTimer.text = "--:--";

        if (_panelSubmissionStatus != null)
        {
            _panelSubmissionStatus.SetActive(false);
        }
    }

    private bool TryBindBuildData()
    {
        if (_isBuildDataBound)
        {
            return true;
        }

        NetCodeMapManager mapManager = NetCodeMapManager.Instance;

        if (mapManager == null || !mapManager.IsSpawned)
        {
            return false;
        }

        _mapManager = mapManager;
        _mapManager.buildRemainingTime.OnValueChanged += HandleBuildRemainingTimeChanged;
        _mapManager.submittedBuildCount.OnValueChanged += HandleSubmissionCountChanged;
        _mapManager.buildPlayerCount.OnValueChanged += HandleBuildPlayerCountChanged;

        _isBuildDataBound = true;

        SetRemainingTime(_mapManager.buildRemainingTime.Value);
        RefreshSubmissionProgress();

        return true;
    }

    private void UnbindBuildData()
    {
        if (_mapManager != null && _isBuildDataBound)
        {
            _mapManager.buildRemainingTime.OnValueChanged -= HandleBuildRemainingTimeChanged;
            _mapManager.submittedBuildCount.OnValueChanged -= HandleSubmissionCountChanged;
            _mapManager.buildPlayerCount.OnValueChanged -= HandleBuildPlayerCountChanged;
        }

        _isBuildDataBound = false;

        if (!_isRunTimerBound)
        {
            _mapManager = null;
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

        _isRunTimerBound = false;

        if (!_isBuildDataBound)
        {
            _mapManager = null;
        }
    }

    private void HandleBuildRemainingTimeChanged(int previousValue, int currentValue)
    {
        SetRemainingTime(currentValue);
    }

    private void HandleRunRemainingTimeChanged(int previousValue, int currentValue)
    {
        SetRemainingTime(currentValue);
    }

    private void HandleSubmissionCountChanged(int previousValue, int currentValue)
    {
        RefreshSubmissionProgress();
    }

    private void HandleBuildPlayerCountChanged(int previousValue, int currentValue)
    {
        RefreshSubmissionProgress();
    }

    private void RefreshSubmissionProgress()
    {
        if (_mapManager == null)
        {
            return;
        }

        SetSubmissionProgress(_mapManager.submittedBuildCount.Value, _mapManager.buildPlayerCount.Value);
    }

    public void SetRemainingTime(int remainingSeconds)
    {
        remainingSeconds = Mathf.Max(0, remainingSeconds);

        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;

        _textTimer.text = $"{minutes:00}:{seconds:00}";
    }

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