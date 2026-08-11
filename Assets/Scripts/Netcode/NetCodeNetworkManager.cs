using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetCodeNetworkManager : MonoBehaviour
{
    [SerializeField] private NetworkManager _netCodeNetworkManager;

    public static NetCodeNetworkManager Instance { get; set; }

    private NetCodeClientSideService _clientSideService = new NetCodeClientSideService();
    private NetCodeServerSideService _serverSideService = new NetCodeServerSideService();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {

    }

    private void OnDestroy()
    {
        _clientSideService.EndClientService();
        if(_netCodeNetworkManager.IsServer == true)
        {
            _serverSideService.EndServerService();
        }
    }

    public void StartAsHost()
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return;
        }

        _clientSideService.InitClientService();
        _netCodeNetworkManager.StartHost();
    }

    public void StartAsServer()
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return;
        }

        _serverSideService.InitServerService();
        _netCodeNetworkManager.StartServer();
    }

    public void StartAsClient()
    {
        if (_netCodeNetworkManager == null)
        {
            Debug.LogWarning("네트워크 매니저가 존재하지 않습니다");
            return;
        }

        _clientSideService.InitClientService();
        _netCodeNetworkManager.StartClient();
    }

}
