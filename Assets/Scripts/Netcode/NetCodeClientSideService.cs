using Unity.Netcode;
using UnityEngine;

public class NetCodeClientSideService
{
    public ulong CurrentClientId { get; private set; }

    public void InitClientService()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        if(NetCodeRoomManager.Instance != null)
        {
            NetCodeRoomManager.Instance.PlayerList.OnListChanged += OnPlayerListChanged;
        }
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
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            string reason = NetworkManager.Singleton.DisconnectReason;

            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log($"서버 연결에 실패했습니다 : 실패 사유: " + reason);
            }
            else
            {
                Debug.Log($"서버 연결에 실패했습니다 : 서버 응답 없음 또는 네트워크 시간 초과(Timeout)");
            }

            // TODO: 타이틀 화면으로 돌아가기, 실패팝업 띄우기 등 처리
        }
        else
        {
            Debug.Log($"다른 클라이언트가 접속 해제했습니다! {clientId}");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            CurrentClientId = clientId;
            Debug.Log("서버 접속 성공! 내 클라이언트 ID: " + clientId);
            // TODO: 대기방 화면 UI 보여주기
        }
        else
        {
            Debug.Log($"다른 클라이언트가 접속했습니다! {clientId}");
        }
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
