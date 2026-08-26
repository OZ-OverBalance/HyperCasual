using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : SingletonBase<CameraManager>
{
    [SerializeField] private CinemachineCamera cineCamera;
    [SerializeField] private CinemachineCamera cineMapCamera;
    [SerializeField] private Camera Camera_main;
    public Camera MainCamera => Camera_main;

    private ulong CurTargetId;

    private List<PlayerController> _spectateTargets = new List<PlayerController>();
    private int _spectateIndex = 0;
    private bool _isSpectating = false;

    private void Update()
    {
        if (!_isSpectating) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetMouseButtonDown(0))
        {
            SwitchSpectateTarget(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetMouseButtonDown(1))
        {
            SwitchSpectateTarget(-1);
        }
    }

    public void SetTargetCamera(ulong targetId, GameObject playerObj)
    {
        if (cineCamera == null || playerObj == null) return;

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

    public void ActivateFollowCamera()
    {
        if (cineCamera != null)
        {
            cineCamera.Priority = 20;
            if (cineMapCamera != null) cineMapCamera.Priority = 10;
        }
    }
    public void StartSpectating()
    {
        _isSpectating = true;
        _spectateIndex = 0;
        RefreshSpectateTargets();

        if (_spectateTargets.Count > 0)
        {
            SetTargetCamera(_spectateTargets[0].OwnerClientId, _spectateTargets[0].gameObject);
            Debug.Log($"[Spectator] 관전 시작: {_spectateTargets[0].name}");
        }
    }

    public void StopSpectating()
    {
        _isSpectating = false;
        _spectateTargets.Clear();
    }

    private void SwitchSpectateTarget(int direction)
    {
        RefreshSpectateTargets();
        if (_spectateTargets.Count <= 0) return;

        _spectateIndex = (_spectateIndex + direction + _spectateTargets.Count) % _spectateTargets.Count;
        PlayerController target = _spectateTargets[_spectateIndex];

        SetTargetCamera(target.OwnerClientId, target.gameObject);
        Debug.Log($"[Spectator] 대상 전환: {target.name} (ClientId: {target.OwnerClientId})");
    }

    private void RefreshSpectateTargets()
    {
        _spectateTargets.Clear();
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (!player.IsOwner && !player.IsDead && player.gameObject.activeInHierarchy)
            {
                _spectateTargets.Add(player);
            }
        }
    }
}