using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class NetworkTriggerBase : ObstacleBase
{
    [SerializeField] private bool triggerOnlyOnce = false;
    private bool _hasTriggered = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (TryGetComponent(out Collider col))
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!HasStarted) return;

        if (triggerOnlyOnce && _hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce)
            {
                _hasTriggered = true;
            }

            OnPlayerTriggered(other);
        }
    }

    /// <summary>
    /// 게임이 시작되고, 플레이어가 Collider안에 들어왔을때 실행할 메서드
    /// </summary>
    protected abstract void OnPlayerTriggered(Collider other);

    /// <summary>
    /// 트리거가 발동했을 때 모든 클라이언트에게 이펙트나 사운드 등을 동기화하는 가상 RPC
    /// 자식 클래스에서 필요에 따라 오버라이드하여 사용
    /// </summary>
    [ClientRpc]
    protected virtual void TriggerClientRpc(ulong playerId)
    {

    }
}