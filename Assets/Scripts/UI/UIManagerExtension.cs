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
            Debug.LogError($"UIManagerExtension - {uiType}의 View 타입이 {typeof(TView).Name}와 일치하지 않음");
        }

        return null;
    }
}