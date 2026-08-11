using Unity.Netcode;
using UnityEngine;

public class PlayerSession : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string myName = PlayerPrefs.GetString("MyPlayerName", "Player_" + Random.Range(100, 999));

            // 서버의 룸 매니저에 내 닉네임 등록 요청
            if (NetCodeRoomManager.Instance != null)
            {
                NetCodeRoomManager.Instance.RegisterPlayerNameServerRpc(myName);
            }
            else
            {
                Debug.LogWarning("[PlayerSession] ServerRoomManager를 찾지 못했습니다.");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        // 클라이언트가 나갈 때 서버 측에서 리스트에서 제거하도록 처리
        if (IsServer)
        {
            NetCodeRoomManager.Instance.RemovePlayer(OwnerClientId);
        }
    }
}