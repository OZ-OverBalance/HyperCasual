using UnityEngine;

public sealed class GameObjectInstance : MonoBehaviour
{
    public int InstanceId { get; private set; } = -1;
    public bool IsRegistered => InstanceId > 0;

    // GameObjectManager가 발급한 고유 InstanceId 설정
    public bool TryInitializeInstance(int instanceId)
    {
        if (IsRegistered)
        {
            return false;
        }

        if (instanceId <= 0)
        {
            return false;
        }

        InstanceId = instanceId;

        return true;
    }
}