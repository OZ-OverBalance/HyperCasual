using Cysharp.Threading.Tasks;
using UnityEngine;

public static class UIManagerExtension
{
    // UI를 열고 요청한 View 타입 반환
    public static async UniTask<TView> ShowUIAsync<TView>(this UIManager uiManager, UIType uiType) where TView : UIBase
    {
        if (uiManager == null)
        {
            return null;
        }

        UIBase uiBase = await uiManager.ShowUIAsync(uiType);

        if (uiBase is TView targetView)
        {
            return targetView;
        }

        if (uiBase != null)
        {
            Debug.LogError($"UIManagerExtension - {uiType} View 타입이 {typeof(TView).Name}와 일치하지 않음");
        }

        return null;
    }

    public static async UniTask<TitleView> ShowTitleUIAsync(this UIManager uiManager)
    {
        if (uiManager == null || GameManager.Inst == null)
        {
            return null;
        }

        TitleView titleView = await uiManager.ShowUIAsync<TitleView>(UIType.Title);

        if (titleView == null)
        {
            return null;
        }

        TitleViewModel viewModel = new TitleViewModel(GameManager.Inst);
        titleView.SetViewModel(viewModel);

        return titleView;
    }

    public static async UniTask<LobbyView> ShowLobbyUIAsync(this UIManager uiManager)
    {
        if (uiManager == null || GameManager.Inst == null)
        {
            return null;
        }

        LobbyView lobbyView = await uiManager.ShowUIAsync<LobbyView>(UIType.Lobby);

        return lobbyView;
    }

    public static async UniTask<JoinRoomPopupView> ShowJoinRoomPopupUIAsync(this UIManager uiManager, string nickname)
    {
        if (uiManager == null)
        {
            return null;
        }

        JoinRoomPopupView popupView = await uiManager.ShowUIAsync<JoinRoomPopupView>(UIType.JoinRoomPopup);

        if (popupView == null)
        {
            return null;
        }

        popupView.SetNickname(nickname);

        return popupView;
    }

    public static async UniTask<BuildInventoryView> ShowBuildInventoryUIAsync(this UIManager uiManager, SegmentBuildManager buildManager)
    {
        if (uiManager == null)
        {
            Debug.LogError("UIManagerExtension - UIManager 없음");
            return null;
        }

        if (buildManager == null)
        {
            Debug.LogError("UIManagerExtension - SegmentBuildManager 없음");
            return null;
        }

        BuildInventoryView inventoryView = await uiManager.ShowUIAsync<BuildInventoryView>(UIType.BuildInventory);

        if (inventoryView == null)
        {
            Debug.LogError("UIManagerExtension - BuildInventoryView 생성 실패");
            return null;
        }

        inventoryView.SetBuildManager(buildManager);

        return inventoryView;
    }

    public static async UniTask<LoadingView> ShowLoadingUIAsync(this UIManager uiManager, string message = null)
    {
        if (uiManager == null)
        {
            Debug.LogError("UIManagerExtension - UIManager 없음");
            return null;
        }

        LoadingView loadingView = await uiManager.ShowUIAsync<LoadingView>(UIType.Loading);

        if (loadingView == null)
        {
            Debug.LogError("UIManagerExtension - LoadingView 생성 실패");
            return null;
        }

        loadingView.AddLoadingRequest(message);
        return loadingView;
    }

    public static async UniTask<bool> HideLoadingUIAsync(this UIManager uiManager)
    {
        if (uiManager == null)
        {
            return false;
        }

        if (!uiManager.TryGetUI(UIType.Loading, out LoadingView loadingView))
        {
            return false;
        }

        bool canClose = loadingView.RemoveLoadingRequest();

        if (!canClose)
        {
            return true;
        }

        await loadingView.WaitForMinimumPlaybackAsync();

        if (loadingView.HasLoadingRequests)
        {
            return true;
        }

        return uiManager.CloseUI(UIType.Loading);
    }

    public static bool ForceHideLoadingUI(this UIManager uiManager)
    {
        if (uiManager == null)
        {
            return false;
        }

        if (!uiManager.TryGetUI(UIType.Loading, out LoadingView loadingView))
        {
            return false;
        }

        loadingView.ClearLoadingRequests();
        return uiManager.CloseUI(UIType.Loading);
    }

    public static async UniTask<RoundResultView> ShowRoundResultUIAsync(this UIManager uiManager)
    {
        if (uiManager == null)
        {
            return null;
        }

        return await uiManager.ShowUIAsync<RoundResultView>(UIType.ResultPopup);
    }
}