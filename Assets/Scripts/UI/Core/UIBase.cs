using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIBase : GameObjectInstance
{
    [Header("UI Base")]
    [SerializeField] private CanvasGroup _canvasGroup;

    private bool _isInitialized;
    private bool _isOpened;

    public abstract UILayer Layer { get; }

    public bool IsInitialized => _isInitialized;
    public bool IsOpened => _isOpened;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (!ValidateReferences())
        {
            Debug.LogError($"UIBase - {name} 필수 참조가 연결되지 않음");
            return;
        }

        _isInitialized = true;

        InitializeUI();
        CloseImmediately();
    }

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