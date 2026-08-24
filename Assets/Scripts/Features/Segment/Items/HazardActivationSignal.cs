using System;

// 장애물을 공통으로 구독하는 스크립트. 장애물이 추가될 때 공통으로 이 이벤트를 구독하여 유지보수 보강
public static class HazardActivationSignal
{
    public static event Action OnActivateAllRequested;

    public static void ActivateAll()
    {
        OnActivateAllRequested?.Invoke();
    }
}