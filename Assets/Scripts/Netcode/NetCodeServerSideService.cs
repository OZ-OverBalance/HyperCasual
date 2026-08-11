using Unity.Netcode;
using UnityEngine;

public class NetCodeServerSideService
{
    public void InitServerService()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnServerClientConnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }
    }

    public void EndServerService()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;


        }
    }

    private void OnServerStarted()
    {
        Debug.Log("[server] 서버시작");
    }

    private void OnServerStopped(bool isHost)
    {
        if (isHost)
        {
            Debug.Log("[server] 호스트 서버 중지");
        }
        else
        {
            Debug.Log("[server] 전용(Dedicated) 서버가 중지되었습니다.");
        }
    }

    private void OnServerClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[서버] 플레이어 접속 감지! Client ID: {clientId}");
        }
    }
}
