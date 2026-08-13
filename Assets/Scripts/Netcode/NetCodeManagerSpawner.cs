using Unity.Netcode;
using UnityEngine;

public class NetCodeManagerSpawner : MonoBehaviour
{
    [Header("스폰할 룸 매니저 프리팹")]
    [SerializeField] private GameObject netCodeRoomManagerPrefab;

    private void Start()
    {
        // NetworkManager가 이미 존재하고 서버가 시작된 상태일 수 있으므로 체크
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            // 만약 스크립트가 켜질 때 이미 서버가 켜져 있는 상태라면 바로 스폰 실행
            if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening)
            {
                SpawnRoomManager();
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
        // 호스트(서버) 권한일 때만 스폰 실행
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnRoomManager();
        }
    }

    private void SpawnRoomManager()
    {
        // 중복 스폰 방지 (이미 씬에 스폰된 매니저가 존재한다면 무시)
        if (NetCodeRoomManager.Instance != null) return;

        if (netCodeRoomManagerPrefab != null)
        {
            // 1. 프리팹 생성
            GameObject roomManagerInstance = Instantiate(netCodeRoomManagerPrefab);

            // 2. 네트워크 오브젝트 컴포넌트를 가져와서 스폰!
            NetworkObject netObj = roomManagerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();

                // 3. 씬이 넘어가거나 로비/게임 간 이동할 때 파괴되지 않도록 유지
                DontDestroyOnLoad(roomManagerInstance);

                Debug.Log("[NetCodeManagerSpawner] NetCodeRoomManager 동적 스폰 완료!");
            }
            else
            {
                Debug.LogError("[NetCodeManagerSpawner] 생성한 프리팹에 NetworkObject 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[NetCodeManagerSpawner] NetCodeRoomManager 프리팹이 연결되지 않았습니다!");
        }
    }
}