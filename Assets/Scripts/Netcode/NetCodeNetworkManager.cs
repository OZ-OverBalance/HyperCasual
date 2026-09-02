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
    public string CurrentRoomCode { get; private set; }

    public event System.Action OnLocalClientConnected;
    public event System.Action<string> OnLocalClientDisconnected;


    protected override void Awake()
    {
        base.Awake();

        if (Inst != this || _netCodeNetworkManager == null)
        {
            return;
        }

        _netCodeNetworkManager.ConnectionApprovalCallback -= ApprovalCheck;
        _netCodeNetworkManager.ConnectionApprovalCallback += ApprovalCheck;
    }

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

            CurrentRoomCode = joinCode;

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
        CurrentRoomCode = joinCode?.Trim();

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
        GameState currentState = GameManager.Inst.CurrentState;

        if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            return;
        }

        if (currentState != GameState.WaitingRoom)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "Game is already start";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Reason = string.Empty;
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

        GameManager gameManager = GameManager.Inst;

        if (gameManager == null)
        {
            return;
        }

        gameManager.TryChangeGameState(GameState.Lobby);
    }
}