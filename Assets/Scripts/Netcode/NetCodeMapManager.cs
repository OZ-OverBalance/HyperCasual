using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetCodeMapManager : NetworkBehaviour
{
    public static NetCodeMapManager Instance { get; private set; }

    private Dictionary<ulong, List<NetworkPlacedObjectData>> _clientPlacedObjectsDic = new Dictionary<ulong, List<NetworkPlacedObjectData>>();

    private int _nextInstanceId = 0;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 클라이언트가 장애물 배치를 완료하고 서버로 전송할 때 호출하는 ServerRpc
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitClientMapDataServerRpc(NetworkPlacedObjectData[] placedObjects, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[NetCodeMapManager] 클라이언트({senderClientId})로부터 장애물 데이터 {placedObjects.Length}개 수신 완료");

        List<NetworkPlacedObjectData> userObjectList = new List<NetworkPlacedObjectData>();

        foreach (var data in placedObjects)
        {
            if (TryGenerateInstanceId(out int newInstanceId))
            {
                NetworkPlacedObjectData finalData = data;
                finalData.InstanceId = newInstanceId;

                userObjectList.Add(finalData);
            }
        }

        if (_clientPlacedObjectsDic.ContainsKey(senderClientId))
        {
            _clientPlacedObjectsDic[senderClientId] = userObjectList;
        }
        else
        {
            _clientPlacedObjectsDic.Add(senderClientId, userObjectList);
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
}