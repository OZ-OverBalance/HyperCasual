using Unity.Netcode;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

public class NetCodeRoomManager : NetworkBehaviour
{
    public static NetCodeRoomManager Instance { get; private set; }

    public NetworkList<NetCodeNetworkPlayerData> PlayerList;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PlayerList = new NetworkList<NetCodeNetworkPlayerData>();
    }
    public void AddPlayer(ulong clientId, string playerName)
    {
        if (!IsServer) return;

        PlayerList.Add(new NetCodeNetworkPlayerData
        {
            ClientId = clientId,
            PlayerName = playerName,
            IsReady = false
        });

        Debug.Log($"[Server] 플레이어 룸 추가 완료 - ID: {clientId}, 이름: {playerName}");
    }

    public void RemovePlayer(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                PlayerList.RemoveAt(i);
                Debug.Log($"[Server] 플레이어 룸 제거 - ID: {clientId}");
                break;
            }
        }
    }
    [ServerRpc]
    public void RegisterPlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId) return;
        }

        PlayerList.Add(new NetCodeNetworkPlayerData
        {
            ClientId = clientId,
            PlayerName = playerName,
            IsReady = false
        });

        Debug.Log($"[Server] 플레이어 룸 등록 완료 - ID: {clientId}, 이름: {playerName}");
    }

    [ServerRpc]
    public void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == senderId)
            {
                NetCodeNetworkPlayerData currentData = PlayerList[i];

                // 기존 데이터에서 레디 상태만 반전(또는 인자로 받은 값으로 설정)
                PlayerList[i] = new NetCodeNetworkPlayerData
                {
                    ClientId = currentData.ClientId,
                    PlayerName = currentData.PlayerName, // 기존 닉네임 유지
                    IsReady = !currentData.IsReady       // 레디 상태 토글
                };
                break;
            }
        }
    }
}
