using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;




public class NetCodeMapManager : NetworkBehaviour
{
    public static NetCodeMapManager Instance { get; private set; }

    private Dictionary<ulong, CraftMapData> _clientPlacedObjectsDic = new Dictionary<ulong, CraftMapData>();
    private List<CraftMapData> _presetMapDataList = new List<CraftMapData>();

    private int _nextInstanceId = 0;

    private HashSet<ulong> _isBuildComplete = new HashSet<ulong>();
    private HashSet<ulong> _arrivedPlayerIds = new HashSet<ulong>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterGoalIn(ulong clientId)
    {
        if (!IsServer) return;

        if (_arrivedPlayerIds.Contains(clientId)) return;

        _arrivedPlayerIds.Add(clientId);
        Debug.Log($"[MapManager] 플레이어 {clientId} 골인 (현재 골인 인원: {_arrivedPlayerIds.Count})");

        CheckRoundEndCondition();
    }

    private void CheckRoundEndCondition()
    {
        if (!IsServer) return;

        int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (_arrivedPlayerIds.Count >= totalPlayers)
        {
            Debug.Log("[MapManager] 모든 플레이어 골인 완료 라운드 정산 시작");
            SettleRoundScores(); 
        }
    }

    private void SettleRoundScores()
    {
        NetCodeScoreManager.Instance.CalculationRoundScore();
        _arrivedPlayerIds.Clear();
        MapManager.Inst.ClearAllMaps();
        PrintScoreLogForTestClientRpc();
    }

    [ClientRpc]
    private void PrintScoreLogForTestClientRpc()
    {
        NetCodeScoreManager.Instance.PrintScoreLog();
    }

    public void ServerStartRunPhase()
    {
        if (IsServer == false) return;

        FullLevelData fullLevelData = new FullLevelData();
        
        if (MapManager.Inst != null && MapManager.Inst.PersistentFullLevelData != null)
        {
            var baseMaps = MapManager.Inst.PersistentFullLevelData.allMapData;

            int playerIndex = 0;
            var submittedList = new List<CraftMapData>(_clientPlacedObjectsDic.Values);

            for (int i = 0; i < baseMaps.Count; i++)
            {
                if (playerIndex < submittedList.Count)
                {
                    fullLevelData.allMapData.Add(submittedList[playerIndex]);
                    playerIndex++;
                }
                else
                {
                    fullLevelData.allMapData.Add(baseMaps[i]);
                }
            }
        }
        else
        {
            foreach (CraftMapData data in _clientPlacedObjectsDic.Values)
            {
                fullLevelData.allMapData.Add(data);
            }
        }

        MapManager.Inst.ShuffleList(fullLevelData.allMapData);
        
        MapManager.Inst.ImportFullLevelDataForNetworkAsync(fullLevelData).Forget();

        StartRunPhaseClientRpc();

        NetCodeObstacleManager.Instance.TriggerRunStart();
    }

    [ClientRpc]
    public void StartRunPhaseClientRpc()
    {
        GameManager.Inst.RoundManager.StartRunPhase();
    }

    /// <summary>
    /// 클라이언트가 장애물 배치를 완료하고 서버로 전송할 때 호출하는 ServerRpc
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitClientMapDataServerRpc(NetworkPlacedObjectData[] placedObjects, string mapId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[NetCodeMapManager] 클라이언트({senderClientId})로부터 장애물 데이터 {placedObjects.Length}개 수신 완료");

        List<PlacedObjectData> userObjectList = new List<PlacedObjectData>();
        CraftMapData newMapData = new CraftMapData();

        newMapData.mapId = mapId;

        foreach (var data in placedObjects)
        {
            if (TryGenerateInstanceId(out int newInstanceId))
            {
                PlacedObjectData finalData = new PlacedObjectData()
                {
                    InstanceId = newInstanceId,
                    Id = data.Id,
                    GridPos = data.GridPos,
                    RotationStep = data.RotationStep,
                    RoundPlaced = data.RoundPlaced,
                    OwnerClientId = senderClientId,
                };

                userObjectList.Add(finalData);

                //SpawnNetworkObject(finalData).Forget();
            }
        }

        newMapData.placedSegements = userObjectList; 

        if (_clientPlacedObjectsDic.ContainsKey(senderClientId))
        {
            _clientPlacedObjectsDic[senderClientId] = newMapData;
        }
        else
        {
            _clientPlacedObjectsDic.Add(senderClientId, newMapData);
        }

        if(!_isBuildComplete.Contains(senderClientId))
        {
            _isBuildComplete.Add(senderClientId);
        }

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (_isBuildComplete.Count >= playerCount)
        {
            // 여기서 Run페이즈 실행하기
            ServerStartRunPhase();
            _isBuildComplete.Clear();
        }
    }

    /// <summary>
    /// 서버에 저장된 맵 데이터를 섞어서 클라이언트에게 보내주는 메서드
    /// </summary>
    public void RequestDistributeMaps()
    {
        if (IsServer == false) return;

        List<CraftMapData> maps = new List<CraftMapData>();
        foreach(CraftMapData data  in _clientPlacedObjectsDic.Values)
        {
            maps.Add(data);
        }

        GameUtil.UtilShuffleList(maps);

        int index = 0;
        foreach(var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (index >= maps.Count) break;

            ulong targetClientId = client.ClientId;
            NetworkPlacedObjectData[] assignedMap = new NetworkPlacedObjectData[maps[index].placedSegements.Count];

            for (int i = 0; i < maps[index].placedSegements.Count; i++)
            {
                assignedMap[i] = new NetworkPlacedObjectData(maps[index].placedSegements[i]);
            }

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { targetClientId }
                }
            };

            ReceiveAssignedMapClientRpc(assignedMap, maps[index].mapId, clientRpcParams);

            index++;
        }
    }

    /// <summary>
    /// 서버에서 받은 데이터를 저장하는 rpc
    /// </summary>
    [ClientRpc]
    private void ReceiveAssignedMapClientRpc(NetworkPlacedObjectData[] placedObjects, string mapId, ClientRpcParams clientRpcParams = default)
    {
        if (mapId == null) return;

        List<PlacedObjectData> userObjectList = new List<PlacedObjectData>();
        CraftMapData newMapData = new CraftMapData();

        newMapData.mapId = mapId;

        foreach (var data in placedObjects)
        {
            PlacedObjectData finalData = new PlacedObjectData()
            {
                InstanceId = data.InstanceId,
                Id = data.Id,
                GridPos = data.GridPos,
                RotationStep = data.RotationStep,
                RoundPlaced = data.RoundPlaced,
                OwnerClientId = NetworkManager.Singleton.LocalClientId
            };

            userObjectList.Add(finalData);
        }

        newMapData.placedSegements = userObjectList;

        MapManager.Inst.SetCurrentBuildData(newMapData);

        Debug.Log($"데이터 수신성공 {newMapData.mapId} , 설치 오브젝트 {newMapData.placedSegements.Count}개");
    }

    /// <summary>
    /// 배치된 오브젝트 데이터를 받아서 스폰시키고 동기화 시키는 메서드
    /// </summary>
    private async UniTaskVoid SpawnNetworkObject(NetworkPlacedObjectData data)
    {
        var obstacleData = GameDataManager.Inst.GetData<SegmentData>(data.Id); 
        if (obstacleData == null)
        {
            Debug.LogWarning($"[NetCodeMapManager] 스폰 실패: 데이터를 찾을 수 없음 (ID: {data.Id})");
            return;
        }

        GameObject prefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(obstacleData.PrefabPath);
        if (prefab == null) return;

        Vector3 worldPos = new Vector3(data.GridPos.x, data.GridPos.y, 0);
        Quaternion worldRot = Quaternion.Euler(0, 0, data.RotationStep * 90f);

        GameObject spawnedObj = Instantiate(prefab, worldPos, worldRot);


        if (spawnedObj.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError("[NetCodeMapManager] 스폰 대상 프리팹에 NetworkObject 컴포넌트가 없습니다!");
            Destroy(spawnedObj);
        }
    }

    /// <summary>
    /// 서버에서 고유 인스턴스 아이디를 발급하는 메서드 
    /// </summary>
    private bool TryGenerateInstanceId(out int instanceId)
    {
        instanceId = -1;

        if (_nextInstanceId == int.MaxValue)
        {
            return false;
        }

        _nextInstanceId++;
        instanceId = _nextInstanceId;
        return true;
    }

    private void ResetCompleteHashset()
    {
        _isBuildComplete.Clear();
    }
}