using Unity.Netcode;
using UnityEngine;

public class NetCodeClientSideService
{
    public ulong CurrentClientId { get; private set; }
    public string ClientName;

    public void InitClientService()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public void EndClientService()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"다른 클라이언트가 접속 해제했습니다! {clientId}");
            return;
        }

        string reason = NetworkManager.Singleton.DisconnectReason;

        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log($"서버 연결이 해제되었습니다. 사유 : {reason}");
        }
        else
        {
            Debug.Log("서버 연결이 해제되었습니다.");
        }

        NetCodeNetworkManager networkManager = NetCodeNetworkManager.Inst;

        if (networkManager != null)
        {
            networkManager.NotifyLocalClientDisconnected(reason);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"다른 클라이언트가 접속했습니다! {clientId}");
            return;
        }

        CurrentClientId = clientId;

        Debug.Log($"서버 접속 성공! 내 클라이언트 ID : {clientId}");

        NetCodeNetworkManager networkManager = NetCodeNetworkManager.Inst;

        if (networkManager == null)
        {
            Debug.LogError("NetCodeClientSideService - NetCodeNetworkManager 없음");
            return;
        }

        networkManager.NotifyLocalClientConnected();
    }

    private void OnPlayerListChanged(NetworkListEvent<NetCodeNetworkPlayerData> changeEvent)
    {
        // TODO : 바뀐 플레이어 데이터를 id를 통해 접근해서 UI에 반영

        switch(changeEvent.Type)
        {
            case NetworkListEvent<NetCodeNetworkPlayerData>.EventType.Add:
                // 플레이어가 입장해서 데이터가 추가 됬을때
                break;
            case NetworkListEvent<NetCodeNetworkPlayerData>.EventType.Remove:
                // 플레이어가 퇴장해서 데이터가 사라졌을때
                break;
            case NetworkListEvent<NetCodeNetworkPlayerData>.EventType.Value:
                // 데이터의 내부 값이 수정되었을때
                break;
        }
    }
}
