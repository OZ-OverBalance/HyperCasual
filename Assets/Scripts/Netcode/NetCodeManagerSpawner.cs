using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class NetCodeManagerSpawner : MonoBehaviour
{
    [Header("스폰할 룸 매니저 프리팹")]
    [SerializeField] private GameObject _netCodeRoomManagerPrefab;
    [SerializeField] private GameObject _netCodeMapManagerPrefab;
    [SerializeField] private GameObject _netCodeObstacleManagerPrefab;

    private bool _isSpawnedManagers = false;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening)
            {
                TrySpawnManagers();
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
            TrySpawnManagers();
        }
    }
    private void TrySpawnManagers()
    {
        if (_isSpawnedManagers) return;
        _isSpawnedManagers = true;

        SpawnManagerAsync(_netCodeRoomManagerPrefab);
        SpawnManagerAsync(_netCodeMapManagerPrefab);
        SpawnManagerAsync(_netCodeObstacleManagerPrefab);
    }

    private async void SpawnManagerAsync(GameObject prefab)
    {

        if (prefab != null)
        {
            await UniTask.WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
            GameObject ManagerInstance = Instantiate(prefab);

            NetworkObject netObj = ManagerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();

                DontDestroyOnLoad(ManagerInstance);

                Debug.Log($"[NetCodeManagerSpawner] {ManagerInstance.name} 동적 스폰 완료!");
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