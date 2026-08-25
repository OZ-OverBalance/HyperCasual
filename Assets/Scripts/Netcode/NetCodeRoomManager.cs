using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetCodeRoomManager : NetworkBehaviour
{
    public static NetCodeRoomManager Instance { get; private set; }

    public event Action OnPlayerListChanged;

    public NetworkList<NetCodeNetworkPlayerData> PlayerList = new NetworkList<NetCodeNetworkPlayerData>();
    
    private Dictionary<ulong,GameObject> playerObjDictionary = new Dictionary<ulong, GameObject>();

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        
    }

    private int GetAvailableColorIndex()
    {
        HashSet<int> usedColors = new HashSet<int>();
        for (int i = 0; i < PlayerList.Count; i++)
        {
            usedColors.Add(PlayerList[i].ColorIndex);
        }

        for (int i = 0; i < 10; i++)
        {
            if (!usedColors.Contains(i)) return i;
        }
        return 0;
    }

    public void AddPlayer(ulong clientId, string playerName)
    {
        if (!IsServer) return;

        PlayerList.Add(new NetCodeNetworkPlayerData
        {
            ClientId = clientId,
            PlayerName = playerName,
            IsReady = false,
            ColorIndex = GetAvailableColorIndex()
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

    public void RegisterPlayerObject(ulong clientId, GameObject playerObj)
    {
        if (!playerObjDictionary.ContainsKey(clientId))
        {
            playerObjDictionary.Add(clientId, playerObj);
            Debug.Log($"[RoomManager] 플레이어 오브젝트 딕셔너리 등록 완료 - ID: {clientId}");
        }
    }

    public void UnregisterPlayerObject(ulong clientId)
    {
        if (playerObjDictionary.ContainsKey(clientId))
        {
            playerObjDictionary.Remove(clientId);
            Debug.Log($"[RoomManager] 플레이어 오브젝트 딕셔너리 제거 - ID: {clientId}");
        }
    }

    public GameObject GetPlayerObject(ulong clientId)
    {
        if (playerObjDictionary.TryGetValue(clientId, out GameObject playerObj))
        {
            return playerObj;
        }
        return null;
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
                    IsReady = !currentData.IsReady,       // 레디 상태 토글
                    ColorIndex = currentData.ColorIndex
                };
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestChangeColorServerRpc(int newColorIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId != senderClientId && PlayerList[i].ColorIndex == newColorIndex)
            {
                return; 
            }
        }

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == senderClientId)
            {
                NetCodeNetworkPlayerData currentData = PlayerList[i];
                PlayerList[i] = new NetCodeNetworkPlayerData
                {
                    ClientId = currentData.ClientId,
                    PlayerName = currentData.PlayerName,
                    IsReady = currentData.IsReady,
                    ColorIndex = newColorIndex
                };
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (senderClientId != NetworkManager.ServerClientId)
        {
            Debug.LogWarning("NetCodeRoomManager - 방장만 게임 시작 가능");
            return;
        }

        if (!CanStartGame())
        {
            Debug.LogWarning("NetCodeRoomManager - 준비되지 않은 플레이어가 있음");
            return;
        }

        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        GameManager gameManager = GameManager.Inst;

        if (gameManager == null || gameManager.RoundManager == null)
        {
            Debug.LogError("NetCodeRoomManager - 게임 또는 라운드 매니저 없음");
            return;
        }

        if (!gameManager.RoundManager.TryStartRound())
        {
            Debug.LogWarning("NetCodeRoomManager - 라운드 시작 실패");
        }
    }

    private bool CanStartGame()
    {
        bool hasGuestPlayer = false;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = PlayerList[i];

            bool isHost = playerData.ClientId == NetworkManager.ServerClientId;

            if (isHost)
            {
                continue;
            }

            hasGuestPlayer = true;

            if (!playerData.IsReady)
            {
                return false;
            }
        }

        return hasGuestPlayer;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PlayerList.OnListChanged -= HandlePlayerListChanged;
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
        PlayerList.OnListChanged -= HandlePlayerListChanged;

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnNetworkDespawn();
    }

    private void HandlePlayerListChanged(NetworkListEvent<NetCodeNetworkPlayerData> changeEvent)
    {
        OnPlayerListChanged?.Invoke();
    }
}
