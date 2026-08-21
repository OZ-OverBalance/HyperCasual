using Unity.Netcode;
using UnityEngine;

public class NetCodeManagerSpawner : MonoBehaviour
{
    [Header("스폰할 룸 매니저 프리팹")]
    [SerializeField] private GameObject _netCodeRoomManagerPrefab;
    [SerializeField] private GameObject _netCodeMapManagerPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening)
            {
                SpawnManager(_netCodeRoomManagerPrefab);
                SpawnManager(_netCodeMapManagerPrefab);
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnManager(_netCodeRoomManagerPrefab);
            SpawnManager(_netCodeMapManagerPrefab);
        }
    }

    private void SpawnManager(GameObject prefab)
    {

        if (prefab != null)
        {
            GameObject ManagerInstance = Instantiate(prefab);

            NetworkObject netObj = ManagerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();

                DontDestroyOnLoad(ManagerInstance);

                Debug.Log("[NetCodeManagerSpawner] NetCodeManager 동적 스폰 완료!");
            }
            else
            {
                Debug.LogError("[NetCodeManagerSpawner] 생성한 프리팹에 NetworkObject 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[NetCodeManagerSpawner] NetCodeManager 프리팹이 연결되지 않았습니다!");
        }
    }
}