using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public sealed class BuildPhaseManager
{
    private const int FirstRoundIndex = 1;
    private const int InventoryItemTypeCount = 5;
    private const int InventoryCountPerItem = 1;

    private readonly GameManager _gameManager;

    private BaseMap _currentEditMap;
    private SegmentBuildManager _segmentBuildManager;
    private SegmentSpawner _segmentSpawner;

    private string _assignedMapId;
    private bool _hasInitializedGameData;

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
        _hasInitializedGameData = false;
    }

    public void SaveAndClearCurrentMap()
    {
        try
        {
            if (_segmentBuildManager == null)
            {
                Debug.LogWarning("BuildPhaseManager - 저장할 제작 데이터 없음");
                return;
            }

            if (string.IsNullOrWhiteSpace(_assignedMapId))
            {
                Debug.LogError("BuildPhaseManager - 배정된 MapId 없음");
                return;
            }

            CraftMapData craftMapData = _segmentBuildManager.ExportCurrentCraftMapData(_assignedMapId);

            if (craftMapData == null || craftMapData.placedSegements == null)
            {
                Debug.LogError("BuildPhaseManager - 맵 데이터 내보내기 실패");
                return;
            }

            if (CanSubmitMapDataToServer())
            {
                SubmitMapDataToServer(craftMapData);
            }
            else
            {
                SaveMapDataLocally(craftMapData);
            }
        }
        finally
        {
            ClearEditMap();
        }
    }

    private void HandleStartedRound(int roundIndex)
    {
        StartBuildPhaseAsync(roundIndex).Forget();
    }

    private async UniTask StartBuildPhaseAsync(int roundIndex)
    {
        UIManager uiManager = UIManager.Inst;

        if (uiManager != null)
        {
            await uiManager.ShowLoadingUIAsync("맵을 준비하고 있어요...");
        }

        try
        {
            await PrepareBuildPhaseAsync(roundIndex);
        }
        finally
        {
            if (uiManager != null)
            {
                await uiManager.HideLoadingUIAsync();
            }
        }
    }

    private async UniTask PrepareBuildPhaseAsync(int roundIndex)
    {
        if (!ValidateRequiredManagers())
        {
            return;
        }

        bool isFirstBuildPhase = roundIndex == FirstRoundIndex;

        ClearEditMap();
        InitializeGameDataIfNeeded();

        int localPlayerIndex = GetLocalPlayerIndex();

        if (localPlayerIndex < 0)
        {
            Debug.LogError("BuildPhaseManager - 로컬 플레이어 인덱스 확인 실패");
            return;
        }

        int playerCount = NetCodeRoomManager.Instance.PlayerList.Count;

        RoundMapSetupResult setupResult = await MapManager.Inst.SetupRoundMaps(roundIndex, playerCount);

        // 최초 빌드 페이즈 - 기본 맵을 플레이어별로 최초 배정
        if (isFirstBuildPhase)
        {
            if (!TryAssignInitialMap(setupResult, localPlayerIndex))
            {
                return;
            }
        }

        // 다음 라운드 빌드 페이즈 - 누적된 제작 맵 중 서버가 배정한 맵 사용. PlayerMapIds는 서버에서 동일한 순서로 동기화되어 전달되어야 함
        else
        {
            if (!TryAssignAccumatedBuildMap(MapManager.Inst.CurrentBuildData))
            {
                return;
            }
        }

        // 모든 빌드 페이즈 공통 - 배정된 맵 생성, Segment 제작 시스템 생성, 카메라 타겟 지정
        if (!await CreateAssignedMapAsync(roundIndex))
        {
            return;
        }

        // 다음 라운드 빌드 페이즈에서만 실행 - 배정받은 누적 맵의 기존 장치 복원, MapId로 찾음
        if (!isFirstBuildPhase)
        {
            await RestoreAssignedMapDataAsync();
        }

        // 모든 빌드 페이즈 공통 - 장치 5개 지급, 인벤토리 UI 표시
        InitializeRoundInventory(roundIndex);

        if (!await ShowBuildInventoryAsync())
        {
            return;
        }

        Debug.Log($"BuildPhaseManager - Round {roundIndex} / 편집 맵 준비 완료 : {_assignedMapId}");
    }

    private bool ValidateRequiredManagers()
    {
        if (MapManager.Inst == null)
        {
            Debug.LogError("BuildPhaseManager - MapManager 없음");
            return false;
        }

        if (GameDataManager.Inst == null)
        {
            Debug.LogError("BuildPhaseManager - GameDataManager 없음");
            return false;
        }

        if (NetCodeRoomManager.Instance == null)
        {
            Debug.LogError("BuildPhaseManager - NetCodeRoomManager 없음");

            return false;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("BuildPhaseManager - NetworkManager 없음");
            return false;
        }

        if (CameraManager.Inst == null)
        {
            Debug.LogError("BuildPhaseManager - CameraManager 없음");
            return false;
        }

        return true;
    }

    private void InitializeGameDataIfNeeded()
    {
        if (_hasInitializedGameData)
        {
            return;
        }

        GameDataManager.Inst.LoadData<MapData>();
        GameDataManager.Inst.LoadData<SegmentData>();

        _hasInitializedGameData = true;

        Debug.Log("BuildPhaseManager - 최초 게임 데이터 초기화 완료");
    }

    private bool TryAssignInitialMap(RoundMapSetupResult setupResult, int localPlayerIndex)
    {
        if (!TryGetAssignedMapId(setupResult, localPlayerIndex, out string mapId))
        {
            Debug.LogError("BuildPhaseManager - 최초 기본 맵 배정 실패");
            return false;
        }

        _assignedMapId = mapId;

        Debug.Log($"BuildPhaseManager - 최초 기본 맵 배정 : {_assignedMapId}");

        return true;
    }

    private bool TryAssignAccumulatedMap(RoundMapSetupResult setupResult, int localPlayerIndex)
    {
        // 실제 무작위 추첨은 서버에서 수행 > SetupRoundMaps()가 반환한 동기화된 배정 결과만 사용
        if (!TryGetAssignedMapId(setupResult,localPlayerIndex, out string mapId))
        {
            Debug.LogError("BuildPhaseManager - 누적 맵 배정 실패");
            return false;
        }

        _assignedMapId = mapId;

        Debug.Log($"BuildPhaseManager - 누적 맵 배정 결과 : {_assignedMapId}");
        return true;
    }

    private bool TryAssignAccumatedBuildMap(CraftMapData buildMapData)
    {
        if (!TryGetAssignedBuildMapId(buildMapData, out string mapId))
        {
            Debug.LogError("BuildPhaseManager - 누적 맵 배정 실패");
            return false;
        }

        _assignedMapId = mapId;

        Debug.Log($"BuildPhaseManager - 누적 맵 배정 결과 : {_assignedMapId}");
        return true;
    }

    private bool TryGetAssignedBuildMapId(CraftMapData buildMapData, out string mapId)
    {
        mapId = string.Empty;

        if (buildMapData == null || buildMapData.mapId == null)
        {
            return false;
        }

        mapId = buildMapData.mapId;
        return !string.IsNullOrWhiteSpace(mapId);
    }

    private bool TryGetAssignedMapId(RoundMapSetupResult setupResult, int localPlayerIndex, out string mapId)
    {
        mapId = string.Empty;

        if (setupResult == null || setupResult.PlayerMapIds == null)
        {
            return false;
        }

        if (localPlayerIndex < 0 ||localPlayerIndex >= setupResult.PlayerMapIds.Count)
        {
            return false;
        }

        mapId = setupResult.PlayerMapIds[localPlayerIndex];
        return !string.IsNullOrWhiteSpace(mapId);
    }

    private async UniTask<bool> CreateAssignedMapAsync(int roundIndex)
    {
        _currentEditMap = await MapManager.Inst.SpawnSingleEditMap(_assignedMapId, Vector3.zero);

        if (_currentEditMap == null)
        {
            Debug.LogError("BuildPhaseManager - 편집 맵 생성 실패");
            return false;
        }

        await UniTask.Yield();

        _currentEditMap.SetSegmentSpawner();
        _segmentSpawner = _currentEditMap.SegmentSpawner;

        if (_segmentSpawner == null)
        {
            Debug.LogError("BuildPhaseManager - egmentSpawner 참조 없음");
            return false;
        }

        _segmentBuildManager = await _segmentSpawner.ShowBuildPhaseAsync(roundIndex);

        if (_segmentBuildManager == null)
        {
            Debug.LogError("BuildPhaseManager - 제작 시스템 생성 실패");
            return false;
        }

        CameraManager.Inst.SetTargetMap(_currentEditMap.CentorPoint);
        return true;
    }

    private async UniTask RestoreAssignedMapDataAsync()
    {
        CraftMapData assignedMapData = MapManager.Inst.CurrentBuildData;

        if (assignedMapData == null || assignedMapData.placedSegements == null || assignedMapData.placedSegements.Count == 0)
        {
            Debug.Log($"BuildPhaseManager - {_assignedMapId}에 복원할 기존 장치 없음");
            return;
        }

        await _segmentBuildManager.LoadExistingPlacedDataAsync(assignedMapData.placedSegements);

        Debug.Log($"BuildPhaseManager - {_assignedMapId}의 {assignedMapData.placedSegements.Count}개 복원");
    }

    private CraftMapData GetCraftMapDataByMapId(string mapId)
    {
        FullLevelData fullLevelData = MapManager.Inst.PersistentFullLevelData;
        if (fullLevelData == null || fullLevelData.allMapData == null)
            {
                return null;
            }

        for (int i = 0; i < fullLevelData.allMapData.Count; i++)
        {
            CraftMapData mapData = fullLevelData.allMapData[i];

            if (mapData != null && mapData.mapId == mapId)
            {
                return mapData;
            }
        }

        return null;
    }

    private int GetAssignedMapDataIndex()
    {
        FullLevelData fullLevelData = MapManager.Inst.PersistentFullLevelData;

        if (fullLevelData == null || fullLevelData.allMapData == null)
        {
            return -1;
        }

        for (int i = 0; i < fullLevelData.allMapData.Count; i++)
        {
            CraftMapData mapData = fullLevelData.allMapData[i];

            if (mapData != null && mapData.mapId == _assignedMapId)
            {
                return i;
            }
        }

        return -1;
    }

    private void InitializeRoundInventory(int roundIndex)
    {
        List<InventorySlot> randomInventory = _segmentBuildManager.CreateRandomInventory(itemTypeCount: InventoryItemTypeCount, countPerItem: InventoryCountPerItem);

        _segmentBuildManager.StartNewRound(roundIndex, randomInventory);
    }

    private async UniTask<bool> ShowBuildInventoryAsync()
    {
        UIManager uiManager = UIManager.Inst;

        if (uiManager == null)
        {
            Debug.LogError("BuildPhaseManager - UIManager 없음");
            return false;
        }

        BuildInventoryView inventoryView = await uiManager.ShowBuildInventoryUIAsync(_segmentBuildManager);

        if (inventoryView == null)
        {
            Debug.LogError("BuildPhaseManager - 제작 인벤토리 UI 생성 실패");                                                                                                                                                                       
            return false;
        }

        return true;
    }

    private int GetLocalPlayerIndex()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < NetCodeRoomManager.Instance.PlayerList.Count; i++)
        {
            NetCodeNetworkPlayerData playerData = NetCodeRoomManager.Instance.PlayerList[i];

            if (playerData.ClientId == localClientId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool CanSubmitMapDataToServer()
    {
        return NetCodeMapManager.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void SubmitMapDataToServer(CraftMapData craftMapData)
    {
        NetworkPlacedObjectData[] networkData = new NetworkPlacedObjectData[craftMapData.placedSegements.Count];

        for (int i = 0; i < craftMapData.placedSegements.Count; i++)
        {
            networkData[i] = new NetworkPlacedObjectData(craftMapData.placedSegements[i]);
        }

        NetCodeMapManager.Instance.SubmitClientMapDataServerRpc(networkData, craftMapData.mapId);

        Debug.Log($"BuildPhaseManager - 서버 맵 데이터 제출 완료 ({_assignedMapId}, {networkData.Length}개)");
    }

    private void SaveMapDataLocally(CraftMapData craftMapData)
    {
        int assignedMapDataIndex = GetAssignedMapDataIndex();

        if (assignedMapDataIndex < 0)
        {
            Debug.LogError("BuildPhaseManager - 저장할 배정 맵을 찾을 수 없음");
            return;
        }

        // UpdatePlayerCraftMapData 기존 메서드명 사용 > 전달하는 값은 플레이어 인덱스가 아닌 MapId로 찾은 실제 누적 맵 데이터 인덱스
        MapManager.Inst.UpdatePlayerCraftMapData(assignedMapDataIndex, craftMapData);
        Debug.Log($"BuildPhaseManager - 로컬 맵 저장 완료 ({_assignedMapId}, {craftMapData.placedSegements.Count}개)");
    }

    private void ClearEditMap()
    {
        if (UIManager.Inst != null)
        {
            UIManager.Inst.CloseUI(UIType.BuildInventory);
        }

        GameObjectManager objectManager = _gameManager.GameObjectManager;

        if (_currentEditMap != null && objectManager != null)
        {
            _currentEditMap.ClearAllPlacedObjects(objectManager);
        }

        if (_segmentSpawner != null)
        {
            _segmentSpawner.ReleaseBuildPhase();
        }

        if (_currentEditMap != null)
        {
            Object.Destroy(_currentEditMap.gameObject);
        }

        _currentEditMap = null;
        _segmentBuildManager = null;
        _segmentSpawner = null;
        _assignedMapId = string.Empty;
    }
}