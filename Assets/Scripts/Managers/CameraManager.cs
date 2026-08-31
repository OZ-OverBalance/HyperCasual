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
    private PlayerController _currentTargetPlayer;

    private List<PlayerController> _spectateTargets = new List<PlayerController>();
    private int _spectateIndex = 0;
    private bool _isSpectating = false;

    private void Update()
    {
        if (!_isSpectating) return;

        if (_currentTargetPlayer == null || _currentTargetPlayer.HasArrived || _currentTargetPlayer.IsDead || !_currentTargetPlayer.gameObject.activeInHierarchy)
        {
            SwitchSpectateTarget(0);
            return;
        }

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

    public void SetTargetMap(Transform mapCenterTransform, float orthoSize = 18f)
    {
        if (cineMapCamera == null || mapCenterTransform == null) return;

        cineMapCamera.Follow = mapCenterTransform;
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
        SwitchSpectateTarget(0);
    }

    public void StopSpectating()
    {
        _isSpectating = false;
        _currentTargetPlayer = null;
        _spectateTargets.Clear();
    }

    private void SwitchSpectateTarget(int direction)
    {
        RefreshSpectateTargets();
        if (_spectateTargets.Count <= 0) return;

        _spectateIndex = (_spectateIndex + direction + _spectateTargets.Count) % _spectateTargets.Count;

        _currentTargetPlayer = _spectateTargets[_spectateIndex];

        SetTargetCamera(_currentTargetPlayer.OwnerClientId, _currentTargetPlayer.gameObject);
        Debug.Log($"[Spectator] 대상 전환: {_currentTargetPlayer.name} (ClientId: {_currentTargetPlayer.OwnerClientId})");
    }

    private void RefreshSpectateTargets()
    {
        _spectateTargets.Clear();
        for (int i = 0; i < PlayerController.AllPlayers.Count; i++)
        {
            PlayerController player = PlayerController.AllPlayers[i];
            if (player == null) continue;

            if (!player.IsOwner && !player.HasArrived && !player.IsDead && player.gameObject.activeInHierarchy)
            {
                _spectateTargets.Add(player);
            }
        }

        if (_spectateTargets.Count == 0)
        {
            _currentTargetPlayer = null;
            if (cineMapCamera != null)
            {
                cineMapCamera.Priority = 30;
            }
            else if (cineCamera != null)
            {
                cineCamera.Follow = null;
            }
            return;
        }
        else
        {
            if (cineMapCamera != null) cineMapCamera.Priority = 10;
            if (cineCamera != null) cineCamera.Priority = 20;
        }

        if (_spectateIndex >= _spectateTargets.Count)
        {
            _spectateIndex = 0;
        }
    }
}