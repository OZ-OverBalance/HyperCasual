using Unity.Netcode;
using UnityEngine;

public class PlayerSession : NetworkBehaviour
{

    public override void OnNetworkDespawn()
    {
        // 클라이언트가 나갈 때 서버 측에서 리스트에서 제거하도록 처리
        if (IsServer)
        {
            NetCodeRoomManager.Instance.RemovePlayer(OwnerClientId);
        }
    }
}