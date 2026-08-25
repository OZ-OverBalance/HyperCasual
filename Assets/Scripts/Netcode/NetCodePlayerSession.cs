using Unity.Netcode;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerSession : NetworkBehaviour
{

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
        }

        if (IsOwner)
        {
            CameraManager.Inst.SetTargetCamera(OwnerClientId, this.gameObject);
        }
    }
}