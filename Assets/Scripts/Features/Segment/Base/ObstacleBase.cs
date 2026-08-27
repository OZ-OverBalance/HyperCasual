using Unity.Netcode;
using UnityEngine;

public abstract class ObstacleBase : NetworkBehaviour
{
    protected bool HasStarted { get; private set; } = false;

    protected virtual void Update()
    {
        if (NetCodeObstacleManager.Instance == null) return;

        double startTime = NetCodeObstacleManager.Instance.GlobalStartTime.Value;

        if (startTime <= 0)
        {
            if (HasStarted)
            {
                HasStarted = false;
                OnObstacleStopped(); // 멈출 때 필요한 정리 작업 (선택사항)
            }
            return;
        }

        if (!HasStarted)
        {
            if (NetworkManager.Singleton.ServerTime.Time >= startTime)
            {
                HasStarted = true;
                OnObstacleStarted();
            }
        }
        else
        {
            OnObstacleUpdate();
        }
    }

    /// <summary>
    /// NetCodeObstacleManager에서 시작타이머가 시작된 초기에 딱 한번 실행할 메서드
    /// </summary>
    protected virtual void OnObstacleStarted() { }

    /// <summary>
    /// 시작타이머가 시작된 이후, 매 Update문 마다 실행할 메서드
    /// </summary>
    protected virtual void OnObstacleUpdate() { }

    /// <summary>
    /// 중간에 장애물시간을 멈추거나 페이즈가 끝났을때 실행할 메서드 
    /// 필요시 오버라이드하여 사용
    /// </summary>
    protected virtual void OnObstacleStopped() { }
}