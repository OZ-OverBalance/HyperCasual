using Unity.Netcode;
using UnityEngine;

public class NetCodeObstacleManager : NetworkBehaviour
{
    public static NetCodeObstacleManager Instance { get; private set; }

    [Header("Network Timing")]
    public NetworkVariable<double> GlobalStartTime = new NetworkVariable<double>(0f);
    [SerializeField] private float startDelay = 3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }

    public void TriggerRunStart()
    {
        if (IsServer)
        {
            GlobalStartTime.Value = NetworkManager.Singleton.ServerTime.Time + startDelay;
        }
    }

    public void TriggerRunEnd()
    {
        if(IsServer)
        {
            GlobalStartTime.Value = 0f;
        }
    }
}