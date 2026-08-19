using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : SingletonBase<CameraManager>
{
    [SerializeField] private CinemachineCamera cineCamera;
    [SerializeField] private CinemachineCamera cineMapCamera;
    [SerializeField] private Camera Camera_main;
    public Camera MainCamera => Camera_main;

    private ulong CurTargetId;

    public void SetTargetCamera(ulong targetId, GameObject playerObj)
    {
        if (cineCamera == null) return;
        if (playerObj == null) return;

        CurTargetId = targetId;
        cineCamera.Follow = playerObj.transform;

        cineCamera.Priority = 20;
        if (cineMapCamera != null)
        {
            cineMapCamera.Priority = 10;
        }
    }

    public void SetTargetMap(Transform mapCentorTransform, float orthoSize = 15f)
    {
        if (cineMapCamera == null || mapCentorTransform == null) return;

        cineMapCamera.Follow = mapCentorTransform;
        cineMapCamera.Lens.OrthographicSize = orthoSize;

        cineMapCamera.Priority = 20;
        if (cineCamera != null)
        {
            cineCamera.Priority = 10;
        }
    }
}
