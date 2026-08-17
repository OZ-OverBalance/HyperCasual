using System;
using Unity.Netcode;
using UnityEngine;

public class NetCodeRoomManager : NetworkBehaviour
{
    public static NetCodeRoomManager Instance { get; private set; }

    public event Action OnPlayerListChanged;

    public NetworkList<NetCodeNetworkPlayerData> PlayerList = new NetworkList<NetCodeNetworkPlayerData>();

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        
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

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        // 보낸 사람의 ID를 넷코드가 안전하게 추출해 줍니다.
        ulong clientId = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                return;
            }
        }


        // 중복 등록 방지 체크 후 리스트에 추가
        AddPlayer(clientId, playerName);
    }

    [ServerRpc(RequireOwnership = false)]
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PlayerList.OnListChanged += HandlePlayerListChanged;

        if (!IsClient)
        {
            return;
        }

        NetCodeNetworkManager networkManager = NetCodeNetworkManager.Inst;

        if (networkManager == null)
        {
            Debug.LogError("NetCodeRoomManager - NetCodeNetworkManager 없음");
            return;
        }

        RegisterPlayerServerRpc(networkManager.LocalPlayerName);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetCodeRoomManager.Instance != null)
        {
            NetCodeRoomManager.Instance.RemovePlayer(OwnerClientId);
        }

        base.OnNetworkDespawn();
    }

    private void HandlePlayerListChanged(NetworkListEvent<NetCodeNetworkPlayerData> changeEvent)
    {
        OnPlayerListChanged?.Invoke();
    }
}
