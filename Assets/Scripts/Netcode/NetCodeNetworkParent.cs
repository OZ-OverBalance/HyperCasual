using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;

public class NetCodeNetworkParent : NetworkBehaviour
{
    [Header("스폰할 자식 프리팹 목록")]
    [SerializeField] private GameObject childPrefab;
    private GameObject _childInstance;
    private NetworkObject _childNetObj;

    private void Start()
    {
        SpawnAndAttachChildren();
    }

    public override void OnNetworkDespawn()
    {
        if(_childNetObj != null)
        {
            _childNetObj.Despawn();
        }

        Destroy(_childInstance);
    }

    private void SpawnAndAttachChildren()
    {

        if (childPrefab == null) return;
        NetworkObject parentNetObj = this.transform.GetComponent<NetworkObject>();
        GameObject childInstance;
        if (parentNetObj != null && parentNetObj.IsSpawned)
        {
            childInstance = Instantiate(childPrefab, transform.position, transform.rotation);
        }
        else
        {
            childInstance = Instantiate(childPrefab, transform.position, transform.rotation, transform);
        }
        _childInstance = childInstance;
        NetworkObject childNetObj = childInstance.GetComponent<NetworkObject>();

        if (childNetObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && parentNetObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer && !childNetObj.IsSpawned)
            {
                childNetObj.Spawn();
                childInstance.transform.SetParent(transform);
                _childNetObj = childNetObj;
            }
        }


        childInstance.transform.localPosition = Vector3.zero;
        childInstance.transform.localRotation = Quaternion.identity;

        BaseMap baseMap = GetComponentInParent<BaseMap>();
        if (baseMap != null)
        {
            baseMap.SetSegmentSpawner();
        }
    }
}