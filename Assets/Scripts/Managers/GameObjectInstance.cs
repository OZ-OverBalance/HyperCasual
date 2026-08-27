using UnityEngine;

public class GameObjectInstance : MonoBehaviour
{
    public int InstanceId { get; private set; } = -1;
    public bool IsRegistered => InstanceId > 0;
    public ulong OwnerClientId { get; private set; }

    // GameObjectManager가 발급한 고유 InstanceId 설정
    public bool TryInitializeInstance(int instanceId)
    {
        if (IsRegistered || instanceId <= 0)
        {
            return false;
        }

        InstanceId = instanceId;
        return true;
    }

    public void SetOwnerClientId(ulong ownerClientId)
    {
        OwnerClientId = ownerClientId;
    }
}