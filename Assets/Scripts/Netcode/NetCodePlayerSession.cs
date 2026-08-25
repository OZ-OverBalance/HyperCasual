using Unity.Netcode;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerSession : NetworkBehaviour
{
    private PlayerColor _playerColor;
    private void Awake()
    {
        _playerColor = GetComponent<PlayerColor>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        RegisterAndSetupAsync().Forget();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetCodeRoomManager.Instance != null)
        {
            NetCodeRoomManager.Instance.RemovePlayer(OwnerClientId);
            NetCodeRoomManager.Instance.UnregisterPlayerObject(OwnerClientId);
        }
    }

    private async UniTaskVoid RegisterAndSetupAsync()
    {
        await UniTask.WaitUntil(() => NetCodeRoomManager.Instance != null);

        if (IsServer)
        {
            NetCodeRoomManager.Instance.RegisterPlayerObject(OwnerClientId, this.gameObject);
            ApplyPlayerColorFromServer();
        }

        if (IsOwner)
        {
            CameraManager.Inst.SetTargetCamera(OwnerClientId, this.gameObject);
        }
    }
    private void ApplyPlayerColorFromServer()
    {
        if (!IsServer || _playerColor == null) return;

        var roomManager = NetCodeRoomManager.Instance;
        if (roomManager == null) return;

        for (int i = 0; i < roomManager.PlayerList.Count; i++)
        {
            if (roomManager.PlayerList[i].ClientId == OwnerClientId)
            {
                _playerColor.SetColorServer(roomManager.PlayerList[i].ColorIndex);
                break;
            }
        }
    }
}