using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIManager : SingletonBase<UIManager>
{
    [Header("UI Layer Roots")]
    [SerializeField] private RectTransform BackgroundRoot;
    [SerializeField] private RectTransform MainRoot;
    [SerializeField] private RectTransform ContentRoot;
    [SerializeField] private RectTransform PopupRoot;
    [SerializeField] private RectTransform VeryFrontRoot;

    [Header("UI Configs")]
    [SerializeField] private List<UIConfig> _uiConfigs;

    private readonly Dictionary<UIType, UIConfig> _uiConfigByType = new();
    private readonly Dictionary<UIType, UIBase> _uiByType = new();
    private readonly HashSet<UIType> _loadingUITypes = new();
    private readonly Stack<UIType> _popupStack = new();

    protected override void Awake()
    {
        base.Awake();

        if (Inst != this)
        {
            return;
        }

        ValidateLayerRoots();
        InitializeUIConfigs();
    }

    protected override void OnDestroy()
    {
        if (Inst != this)
        {
            return;
        }

        ReleaseAllUI();

        _uiConfigByType.Clear();

        base.OnDestroy();
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
                return BackgroundRoot;

            case UILayer.Main:
                return MainRoot;

            case UILayer.Content:
                return ContentRoot;

            case UILayer.Popup:
                return PopupRoot;

            case UILayer.VeryFront:
                return VeryFrontRoot;

            default:
                return null;
        }
    }

    private void ValidateLayerRoots()
    {
        if (BackgroundRoot == null)
        {
            Debug.LogError("UIManager - Background Root 연결되지 않음.");
        }

        if (MainRoot == null)
        {
            Debug.LogError("UIManager - Main Root 연결되지 않음.");
        }

        if (ContentRoot == null)
        {
            Debug.LogError("UIManager - Content Root 연결되지 않음");
        }

        if (PopupRoot == null)
        {
            Debug.LogError("UIManager - Popup Root 연결되지 않음.");
        }

        if (VeryFrontRoot == null)
        {
            Debug.LogError("UIManager - VeryFront Root 연결되지 않음.");
        }
    }

    private void InitializeUIConfigs()
    {
        _uiConfigByType.Clear();

        if (_uiConfigs == null)
        {
            Debug.LogError("UIManager - UIConfig 목록 없음");
            return;
        }

        foreach (UIConfig uiConfig in _uiConfigs)
        {
            if (!ValidateUIConfig(uiConfig))
            {
                continue;
            }

            if (!_uiConfigByType.TryAdd(uiConfig.UIType, uiConfig))
            {
                Debug.LogError($"UIManager - {uiConfig.UIType} 설정 중복");
            }
        }
    }

    private bool ValidateUIConfig(UIConfig uiConfig)
    {
        if (uiConfig == null)
        {
            Debug.LogError("UIManager - 비어 있는 UIConfig 가 있음");
            return false;
        }

        if (uiConfig.UIType == UIType.None)
        {
            Debug.LogError("UIManager - UIType = None");
            return false;
        }

        if (string.IsNullOrWhiteSpace(uiConfig.Address))
        {
            Debug.LogError($"UIManager - {uiConfig.UIType} Address 가 비어 있음");
            return false;
        }

        return true;
    }

    private bool TryGetUIConfig(UIType uiType, out UIConfig uiConfig)
    {
        uiConfig = null;

        if (uiType == UIType.None)
        {
            return false;
        }

        if (!_uiConfigByType.TryGetValue(uiType, out uiConfig))
        {
            Debug.LogError($"UIManager - {uiType} UIConfig 가 없음");
            return false;
        }

        return true;
    }

    // UI 비동기 생성 or 이미 생성된 UI를 다시 열기
    public async UniTask<UIBase> ShowUIAsync(UIType uiType)
    {
        if (uiType == UIType.None)
        {
            return null;
        }

        if (_uiByType.TryGetValue(uiType, out UIBase cachedUI))
        {
            OpenUI(uiType, cachedUI);
            return cachedUI;
        }

        if (_loadingUITypes.Contains(uiType))
        {
            await UniTask.WaitUntil(() => !_loadingUITypes.Contains(uiType));

            if (_uiByType.TryGetValue(uiType, out cachedUI))
            {
                OpenUI(uiType, cachedUI);
                return cachedUI;
            }

            return null;
        }

        if (!TryGetUIConfig(uiType, out UIConfig uiConfig))
        {
            return null;
        }

        if (!TryGetManagers(out ResourceManager resourceManager, out GameObjectManager gameObjectManager))
        {
            return null;
        }

        _loadingUITypes.Add(uiType);

        try
        {
            GameObject prefab = await resourceManager.LoadAssetAsync<GameObject>(uiConfig.Address);

            if (prefab == null)
            {
                return null;
            }

            if (!prefab.TryGetComponent(out UIBase prefabUI))
            {
                Debug.LogError($"UIManager - {uiType} 프리팹에 UIBase 없음");

                resourceManager.TryReleaseAsset(uiConfig.Address);
                return null;
            }

            if (!TryGetLayerRoot(prefabUI.Layer, out RectTransform layerRoot))
            {
                Debug.LogError($"UIManager - {prefabUI.Layer} Root 찾을 수 없음");

                resourceManager.TryReleaseAsset(uiConfig.Address);
                return null;
            }

            if (!gameObjectManager.TryCreateUIObject(prefab, layerRoot, out UIBase createdUI))
            {
                resourceManager.TryReleaseAsset(uiConfig.Address);
                return null;
            }

            if (!_uiByType.TryAdd(uiType, createdUI))
            {
                gameObjectManager.TryDestroyObject(createdUI.InstanceId);

                resourceManager.TryReleaseAsset(uiConfig.Address);
                return null;
            }

            createdUI.Initialize();

            if (!createdUI.IsInitialized)
            {
                _uiByType.Remove(uiType);

                gameObjectManager.TryDestroyObject(createdUI.InstanceId);

                resourceManager.TryReleaseAsset(uiConfig.Address);
                return null;
            }

            OpenUI(uiType, createdUI);
            return createdUI;
        }
        finally
        {
            _loadingUITypes.Remove(uiType);
        }
    }

    private void OpenUI(UIType uiType, UIBase targetUI)
    {
        if (targetUI == null)
        {
            return;
        }

        targetUI.Open();

        if (targetUI.Layer != UILayer.Popup)
        {
            return;
        }

        RemovePopupType(uiType);
        _popupStack.Push(uiType);
    }

    public bool CloseUI(UIType uiType)
    {
        if (!_uiByType.TryGetValue(uiType, out UIBase targetUI))
        {
            return false;
        }

        targetUI.Close();
        RemovePopupType(uiType);

        return true;
    }

    public bool CloseTopPopup()
    {
        while (_popupStack.Count > 0)
        {
            UIType uiType = _popupStack.Pop();

            if (!_uiByType.TryGetValue(uiType, out UIBase popupUI))
            {
                continue;
            }

            if (!popupUI.IsOpened)
            {
                continue;
            }

            popupUI.Close();
            return true;
        }

        return false;
    }

    private bool TryGetManagers(out ResourceManager resourceManager, out GameObjectManager gameObjectManager)
    {
        resourceManager = ResourceManager.Inst;
        gameObjectManager = null;

        if (resourceManager == null)
        {
            Debug.LogError("UIManager - ResourceManager 초기화되지 않음");
            return false;
        }

        if (GameManager.Inst == null || GameManager.Inst.GameObjectManager == null)
        {
            Debug.LogError("UIManager - GameObjectManager 초기화되지 않음");
            return false;
        }

        gameObjectManager = GameManager.Inst.GameObjectManager;
        return true;
    }

    private void RemovePopupType(UIType targetType)
    {
        if (_popupStack.Count == 0)
        {
            return;
        }

        Stack<UIType> temporaryStack = new();

        while (_popupStack.Count > 0)
        {
            UIType uiType = _popupStack.Pop();

            if (uiType != targetType)
            {
                temporaryStack.Push(uiType);
            }
        }

        while (temporaryStack.Count > 0)
        {
            _popupStack.Push(temporaryStack.Pop());
        }
    }

    // UI 인스턴스를 제거 및 Addressables 에셋 참조 해제
    public bool ReleaseUI(UIType uiType)
    {
        if (!_uiByType.TryGetValue(uiType, out UIBase targetUI))
        {
            return false;
        }

        if (!TryGetUIConfig(uiType, out UIConfig uiConfig))
        {
            return false;
        }

        _uiByType.Remove(uiType);
        RemovePopupType(uiType);

        targetUI.Release();

        ReleaseUIObject(targetUI);

        if (ResourceManager.Inst != null)
        {
            ResourceManager.Inst.TryReleaseAsset(uiConfig.Address);
        }

        return true;
    }

    private void ReleaseUIObject(UIBase targetUI)
    {
        if (targetUI == null)
        {
            return;
        }

        if (GameManager.Inst != null && GameManager.Inst.GameObjectManager != null)
        {
            if (GameManager.Inst.GameObjectManager.TryDestroyObject(targetUI.InstanceId))
            {
                return;
            }
        }

        Destroy(targetUI.gameObject);
    }

    public void ReleaseAllUI()
    {
        if (_uiByType.Count == 0)
        {
            _popupStack.Clear();
            _loadingUITypes.Clear();
            return;
        }

        List<UIType> uiTypes = new List<UIType>(_uiByType.Keys);

        for (int i = 0; i < uiTypes.Count; i++)
        {
            ReleaseUI(uiTypes[i]);
        }

        _popupStack.Clear();
        _loadingUITypes.Clear();
    }
}