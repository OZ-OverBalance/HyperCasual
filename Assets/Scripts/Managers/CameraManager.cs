using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : SingletonBase<CameraManager>
{
    [SerializeField] private CinemachineCamera cineCamera;

    private ulong CurTargetId;

    public void SetTargetCamera(ulong targetId, GameObject playerObj)
    {
        if (cineCamera == null) return;
        if (playerObj == null) return;

        CurTargetId = targetId;
        cineCamera.Follow = playerObj.transform;
    }
}
