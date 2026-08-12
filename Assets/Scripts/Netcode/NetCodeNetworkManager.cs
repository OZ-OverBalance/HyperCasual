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

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("UGS 초기화 성공!"); // 이 로그가 찍히면 성공입니다.

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"로그인 성공! ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGS 초기화 실패: {e.Message}"); // 여기가 뜬다면 연결/로그인 문제
        }
    }

    private void OnDestroy()
    {
        _clientSideService.EndClientService();
        if (_netCodeNetworkManager != null && _netCodeNetworkManager.IsServer == true)
        {
            _serverSideService.EndServerService();
        }
    }

    /// <summary>
    /// [릴레이 연동] 호스트 시작 (조인 코드 반환)
    /// </summary>
    public async Task<string> StartAsHostWithRelay(int maxPlayers = 4)
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return null;
        }

        try
        {
            // 1. Relay 할당 및 조인 코드 발급 (호스트 제외 인원이므로 maxPlayers - 1)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 2. Transport에 호스트 데이터 주입
            var transport = _netCodeNetworkManager.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 3. 기존 서비스 초기화 및 호스트 구동
            _serverSideService.InitServerService();
            _clientSideService.InitClientService();
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

    /// <summary>
    /// [릴레이 연동] 클라이언트 참가 (조인 코드 입력)
    /// </summary>
    public async Task<bool> StartAsClientWithRelay(string joinCode)
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return false;
        }

        try
        {
            // 1. 조인 코드로 Relay 정보 가져오기
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 2. Transport에 클라이언트 데이터 주입
            var transport = _netCodeNetworkManager.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // 3. 기존 서비스 초기화 및 클라이언트 구동
            _clientSideService.InitClientService();
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

    // 로컬 테스트용 기존 함수
    public void StartAsHost()
    {
        if (_netCodeNetworkManager == null) return;
        _serverSideService.InitServerService();
        _clientSideService.InitClientService();
        _netCodeNetworkManager.StartHost();
    }

    public void StartAsClient()
    {
        if (_netCodeNetworkManager == null) return;
        _clientSideService.InitClientService();
        _netCodeNetworkManager.StartClient();
    }
}