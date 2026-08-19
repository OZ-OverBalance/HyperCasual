using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public sealed class BuildPhaseManager
{
    private readonly GameManager _gameManager;

    private BaseMap _currentEditMap;
    private SegmentBuildManager _segmentBuildManager;
    private SegmentSpawner _segmentSpawner;
    private string _assignedMapId;

    public BaseMap CurrentEditMap => _currentEditMap;

    public SegmentBuildManager SegmentBuildManager => _segmentBuildManager;
    public SegmentSpawner SegmentSpawner => _segmentSpawner;

    public string AssignedMapId => _assignedMapId;

    public BuildPhaseManager(GameManager gameManager)
    {
        _gameManager = gameManager;

        if (_gameManager.RoundManager != null)
        {
            _gameManager.RoundManager.OnStartedRound += HandleStartedRound;
        }
    }

    public void Release()
    {
        if (_gameManager.RoundManager != null)
        {
            _gameManager.RoundManager.OnStartedRound -= HandleStartedRound;
        }

        ClearEditMap();
    }

    private void HandleStartedRound(int roundIndex)
    {
        StartBuildPhaseAsync(roundIndex).Forget();
    }

    private async UniTask StartBuildPhaseAsync(int roundIndex)
    {
        if (MapManager.Inst == null || NetCodeRoomManager.Instance == null || NetworkManager.Singleton == null)
        {
            Debug.LogError("BuildPhaseManager - 필수 매니저 없음");
            return;
        }

        ClearEditMap();

        GameDataManager.Inst.LoadData<MapData>();
        GameDataManager.Inst.LoadData<SegmentData>();

        int playerCount = NetCodeRoomManager.Instance.PlayerList.Count;

        RoundMapSetupResult setupResult = MapManager.Inst.SetupRoundMaps(roundIndex, playerCount);

        int localPlayerIndex = GetLocalPlayerIndex();

        if (localPlayerIndex < 0 || localPlayerIndex >= setupResult.PlayerMapIds.Count)
        {
            Debug.LogError("BuildPhaseManager - 로컬 플레이어 맵 배정 실패");
            return;
        }

        _assignedMapId = setupResult.PlayerMapIds[localPlayerIndex];
        _currentEditMap = await MapManager.Inst.SpawnSingleEditMap(_assignedMapId, Vector3.zero);

        if (_currentEditMap == null)
        {
            Debug.LogError("BuildPhaseManager - 편집 맵 생성 실패");
            return;
        }

        _segmentSpawner = _currentEditMap.GetComponentInChildren<SegmentSpawner>(true);

        if (_segmentSpawner == null)
        {
            Debug.LogError("BuildPhaseManager - SegmentSpawner 없음");
            return;
        }

        CameraManager.Inst.SetTargetMap(_currentEditMap.CentorPoint);

        CraftMapData previousData = MapManager.Inst.GetPlayerCraftMapData(localPlayerIndex);
        if (previousData != null && previousData.placedSegements != null && previousData.placedSegements.Count > 0)
        {
            await _segmentBuildManager.LoadExistingPlacedDataAsync(previousData.placedSegements);
        }

        _segmentBuildManager.StartNewRound(roundIndex, new List<InventorySlot>());
        _segmentSpawner.ShowBuildPhase();

        Debug.Log($"BuildPhaseManager - 편집 맵 생성 완료 : {_assignedMapId}");
    }

    private int GetLocalPlayerIndex()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < NetCodeRoomManager.Instance.PlayerList.Count; i++)
        {
            if (NetCodeRoomManager.Instance.PlayerList[i].ClientId == localClientId)
            {
                return i;
            }
        }

        return -1;
    }

    private void ClearEditMap()
    {
        if (_currentEditMap != null)
        {
            Object.Destroy(_currentEditMap.gameObject);
        }

        _currentEditMap = null;
        _segmentBuildManager = null;
        _segmentSpawner = null;
        _assignedMapId = string.Empty;
    }

    public void SaveAndClearCurrentMap()
    {
        var localPlayerIndex = GetLocalPlayerIndex();
        if (_segmentBuildManager != null && localPlayerIndex >= 0)
        {
            CraftMapData updatedData = _segmentBuildManager.ExportCurrentCraftMapData(_assignedMapId);

            MapManager.Inst.UpdatePlayerCraftMapData(localPlayerIndex, updatedData);
            Debug.Log($"<color=cyan>[BuildPhaseManager] 맵 데이터 저장 완료 (설치 기물 수: {updatedData.placedSegements.Count}개)</color>");
        }

        ClearEditMap();
    }
}