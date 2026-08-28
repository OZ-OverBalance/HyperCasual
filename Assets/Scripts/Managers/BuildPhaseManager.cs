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

        await UniTask.Yield();
        _currentEditMap.SetSegmentSpawner();
        _segmentSpawner = _currentEditMap.SegmentSpawner;

        if (_segmentSpawner == null)
        {
            Debug.LogError("BuildPhaseManager - 편집 맵에 SegmentSpawner 참조 없음");
            return;
        }

        _segmentBuildManager = await _segmentSpawner.ShowBuildPhaseAsync(roundIndex);

        if (_segmentBuildManager == null)
        {
            Debug.LogError("BuildPhaseManager - Segment 제작 시스템 생성 실패");
            return;
        }

        CameraManager.Inst.SetTargetMap(_currentEditMap.CentorPoint);

        CraftMapData previousData = MapManager.Inst.GetPlayerCraftMapData(localPlayerIndex);
        if (previousData != null && previousData.placedSegements != null && previousData.placedSegements.Count > 0)
        {
            await _segmentBuildManager.LoadExistingPlacedDataAsync(previousData.placedSegements);
        }

        List<InventorySlot> randomInventory = _segmentBuildManager.CreateRandomInventory(itemTypeCount: 8, countPerItem: 1);

        Debug.Log(
    $"[BuildPhaseManager] 생성된 인벤토리 슬롯 수: " +
    $"{randomInventory.Count}"
);

        _segmentBuildManager.StartNewRound(roundIndex, randomInventory);

        //_segmentSpawner.ShowBuildPhase();

        UIManager uiManager = UIManager.Inst;

        if (uiManager == null)
        {
            Debug.LogError("BuildPhaseManager - UIManager 없음");
            return;
        }

        BuildInventoryView inventoryView = await uiManager.ShowBuildInventoryUIAsync(_segmentBuildManager);

        if (inventoryView == null)
        {
            Debug.LogError("BuildPhaseManager - 제작 인벤토리 UI 생성 실패");
            return;
        }

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
        if (UIManager.Inst != null)
        {
            UIManager.Inst.CloseUI(UIType.BuildInventory);
        }

        var objManager = GameManager.Inst.GameObjectManager;

        if (_currentEditMap != null)
        {
            _currentEditMap.ClearAllPlacedObjects(objManager);
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

    public void SaveAndClearCurrentMap()
    {
        var localPlayerIndex = GetLocalPlayerIndex();
        if (_segmentBuildManager != null && localPlayerIndex >= 0)
        {
            CraftMapData myData = _segmentBuildManager.ExportCurrentCraftMapData(_assignedMapId);
            MapManager.Inst.UpdateLocalMapSaveData(myData);

            NetworkPlacedObjectData[] netDataArray = new NetworkPlacedObjectData[myData.placedSegements.Count];
            for (int i = 0; i < myData.placedSegements.Count; i++)
            {
                netDataArray[i] = new NetworkPlacedObjectData(myData.placedSegements[i]);
            }

            if (NetCodeMapManager.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetCodeMapManager.Instance.SubmitClientMapDataServerRpc(netDataArray, myData.mapId);
                Debug.Log($"[BuildPhaseManager] 서버로 기물 데이터 제출 완료 ({netDataArray.Length}개)");
            }
            else
            {
                // 싱글, 에디터 테스트 용
                MapManager.Inst.UpdatePlayerCraftMapData(localPlayerIndex, myData);
                Debug.Log($"[BuildPhaseManager] 로컬 MapManager 저장 완료 ({myData.placedSegements.Count}개)");
            }
        }

        ClearEditMap();
    }
}