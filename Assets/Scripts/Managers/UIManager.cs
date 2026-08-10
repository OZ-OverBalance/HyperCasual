using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public sealed class UIManager : SingletonBase<UIManager>
{
    [Header("UI Layer Roots")]
    [SerializeField] private RectTransform _backgroundRoot;
    [SerializeField] private RectTransform _mainRoot;
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private RectTransform _popupRoot;
    [SerializeField] private RectTransform _veryFrontRoot;

    protected override void Awake()
    {
        base.Awake();

        if (Inst != this)
        {
            return;
        }

        ValidateLayerRoots();
    }

    // UILayer의 부모 RectTransform을 반환
    public bool TryGetLayerRoot(UILayer uiLayer, out RectTransform layerRoot)
    {
        layerRoot = GetLayerRoot(uiLayer);

        return layerRoot != null;
    }

    public RectTransform GetLayerRoot(UILayer uiLayer)
    {
        switch (uiLayer)
        {
            case UILayer.Background:
                return _backgroundRoot;

            case UILayer.Main:
                return _mainRoot;

            case UILayer.Content:
                return _contentRoot;

            case UILayer.Popup:
                return _popupRoot;

            case UILayer.VeryFront:
                return _veryFrontRoot;

            default:
                return null;
        }
    }

    private void ValidateLayerRoots()
    {
        if (_backgroundRoot == null)
        {
            Debug.LogError("UIManager - Background Root 연결되지 않음.");
        }

        if (_mainRoot == null)
        {
            Debug.LogError("UIManager - Main Root 연결되지 않음.");
        }

        if (_contentRoot == null)
        {
            Debug.LogError("UIManager - Content Root 연결되지 않음");
        }

        if (_popupRoot == null)
        {
            Debug.LogError("UIManager - Popup Root 연결되지 않음.");
        }

        if (_veryFrontRoot == null)
        {
            Debug.LogError("UIManager - VeryFront Root 연결되지 않음.");
        }
    }
}