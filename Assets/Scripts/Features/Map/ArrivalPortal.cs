using Unity.Netcode;
using UnityEngine;

public class ArrivalPortal : NetworkBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Player"))
        {
            return;
        }

        if (IsServer && NetCodeScoreManager.Instance != null)
        {
            if (collider.TryGetComponent<NetworkObject>(out var netObj))
            {
                NetCodeScoreManager.Instance.AddGoalScore(netObj.OwnerClientId);
                Debug.Log("[ArrivalPortal] 플레이어 골인 확인");
            }
        }
    }
}
