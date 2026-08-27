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

    private int _nextInstanceId = 0;

    private HashSet<ulong> _isBuildComplete = new HashSet<ulong>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public void ServerStartRunPhase()
    {
        if (IsServer == false) return;

        FullLevelData fullLevelData = new FullLevelData();
        
        foreach(CraftMapData data in _clientPlacedObjectsDic.Values)
        {
            fullLevelData.allMapData.Add(data);
        }

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

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        _isBuildComplete.Add(senderClientId);

        if(_isBuildComplete.Count >= playerCount)
        {
            // 여기서 Run페이즈 실행하기
            ServerStartRunPhase();
        }
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