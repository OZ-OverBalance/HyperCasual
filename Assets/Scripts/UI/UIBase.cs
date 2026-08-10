using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIBase : MonoBehaviour
{
    [Header("UI Base")]
    [SerializeField] private CanvasGroup _canvasGroup;

    private bool _isInitialized;
    private bool _isOpened;

    public abstract UILayer Layer { get; }

    public bool IsInitialized => _isInitialized;
    public bool IsOpened => _isOpened;

    /// <summary>
    /// UI를 최초 한 번 초기화합니다.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (!ValidateReferences())
        {
            Debug.LogError($"[UIBase] {name}의 필수 참조가 연결되지 않았습니다.");
            return;
        }

        _isInitialized = true;

        InitializeUI();
        CloseImmediately();
    }

    /// <summary>
    /// UI를 활성화하고 이벤트를 연결합니다.
    /// </summary>
    public void Open()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        if (!_isInitialized || _isOpened)
        {
            return;
        }

        _isOpened = true;
        gameObject.SetActive(true);

        SetInteraction(true);
        BindEvents();
        RefreshUI();
        PlayOpenAnimation();
    }

    /// <summary>
    /// UI 이벤트를 해제하고 비활성화합니다.
    /// </summary>
    public void Close()
    {
        if (!_isOpened)
        {
            return;
        }

        _isOpened = false;

        SetInteraction(false);
        UnbindEvents();
        PlayCloseAnimation();
    }

    /// <summary>
    /// UI가 완전히 제거되기 전 내부 상태를 정리합니다.
    /// </summary>
    public void Release()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_isOpened)
        {
            _isOpened = false;
            UnbindEvents();
        }

        ReleaseUI();

        _isInitialized = false;
    }

    /// <summary>
    /// 닫기 연출이 끝난 시점에 UI를 비활성화합니다.
    /// </summary>
    public void CompleteClose()
    {
        if (_isOpened)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    protected virtual bool ValidateReferences()
    {
        return _canvasGroup != null;
    }

    protected virtual void InitializeUI()
    {
    }

    protected virtual void BindEvents()
    {
    }

    protected virtual void UnbindEvents()
    {
    }

    protected virtual void RefreshUI()
    {
    }

    protected virtual void PlayOpenAnimation()
    {
    }

    protected virtual void PlayCloseAnimation()
    {
        CompleteClose();
    }

    protected virtual void ReleaseUI()
    {
    }

    private void CloseImmediately()
    {
        _isOpened = false;

        SetInteraction(false);
        gameObject.SetActive(false);
    }

    private void SetInteraction(bool isEnabled)
    {
        _canvasGroup.alpha = isEnabled ? 1f : 0f;
        _canvasGroup.interactable = isEnabled;
        _canvasGroup.blocksRaycasts = isEnabled;
    }
}