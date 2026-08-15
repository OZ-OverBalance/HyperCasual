using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetCodeNetworkManager : SingletonBase<NetCodeNetworkManager>
{
    [SerializeField] private NetworkManager _netCodeNetworkManager;

    private NetCodeClientSideService _clientSideService = new NetCodeClientSideService();
    private NetCodeServerSideService _serverSideService = new NetCodeServerSideService();

    public string LocalPlayerName { get; private set; }

    public event System.Action OnLocalClientConnected;
    public event System.Action<string> OnLocalClientDisconnected;


    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("UGS 초기화 성공!"); 

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"로그인 성공! ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGS 초기화 실패: {e.Message}"); 
        }

        if(_netCodeNetworkManager != null)
        {
            _netCodeNetworkManager.ConnectionApprovalCallback += ApprovalCheck;
        }
    }

    protected override void OnDestroy()
    {
        _clientSideService.EndClientService();

        if (_netCodeNetworkManager != null)
        {
            _netCodeNetworkManager.ConnectionApprovalCallback -= ApprovalCheck;

            if (_netCodeNetworkManager.IsServer)
            {
                _serverSideService.EndServerService();
            }
        }

        base.OnDestroy();
    }

    // [릴레이 연동] 호스트 시작 (조인 코드 반환)
    public async Task<string> StartAsHostWithRelay(int maxPlayers)
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return null;
        }

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = _netCodeNetworkManager.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );


            _serverSideService.InitServerService();
            _clientSideService.InitClientService();

            _netCodeNetworkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(LocalPlayerName);
            _netCodeNetworkManager.StartHost();



            Debug.Log($"[Relay] 호스트 시작 완료! 조인 코드: {joinCode}");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] 호스트 시작 실패: {e.Message}");
            return null;
        }
    }

    // [릴레이 연동] 클라이언트 참가 (조인 코드 입력)
    public async Task<bool> StartAsClientWithRelay(string joinCode)
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return false;
        }

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = _netCodeNetworkManager.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            _clientSideService.InitClientService();

            _netCodeNetworkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(LocalPlayerName);
            bool success = _netCodeNetworkManager.StartClient();

            Debug.Log($"[Relay] 클라이언트 참가 결과: {success}");
            return success;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] 클라이언트 참가 실패: {e.Message}");
            return false;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log("ApprovalCHeck실행됨");
        byte[] payload = request.Payload;
        string playerName = "Player";
        if (payload != null && payload.Length > 0)
        {
            playerName = System.Text.Encoding.UTF8.GetString(payload);
        }

        NetCodeRoomManager.Instance.PlayerList.Add(new NetCodeNetworkPlayerData
        {
            ClientId = request.ClientNetworkId,
            PlayerName = playerName,
            IsReady = false
        });

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Pending = false;
    }

    public void SetLocalPlayerName(string playerName)
    {
        LocalPlayerName = playerName;
    }

    public void NotifyLocalClientConnected()
    {
        OnLocalClientConnected?.Invoke();
    }

    public void NotifyLocalClientDisconnected(string reason)
    {
        OnLocalClientDisconnected?.Invoke(reason);
    }
}