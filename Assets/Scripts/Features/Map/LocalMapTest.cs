using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class LocalMapTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private int testPlayerCount = 1;
    [SerializeField] private Vector2 editMapSpawnPosition = Vector2.zero;

    private RoundMapSetupResult currentSetupResult;
    private string myAssignedMapId;
    private BaseMap currentEditMapInstance;
    private SegmentBuildManager currentBuildManager;
    private FullLevelData finalFullLevelData;

    private void Update()
    {
        // [7번 키] 편집 페이즈 시작 (맵 배정 및 스폰)
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TestStartEditingPhaseAsync().Forget();
        }

        // [8번 키] 데이터 추출 및 레벨 데이터 합성
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            TestSaveAndCombineLevelData();
        }

        // [9번 키] 5개 맵 스폰 및 복원 (플레이 페이즈)
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TestBuildFullLevelAsync().Forget();
        }

        // [0번 키] 플레이어 리스폰 테스트
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TestRespawnPlayer();
        }
    }

    /// <summary>
    /// [7번 키] 라운드 맵 배분 -> 단일 편집용 BaseMap 스폰
    /// </summary>
    private async UniTaskVoid TestStartEditingPhaseAsync()
    {
        ClearCurrentEditObjects();

        // 1. JSON 데이터 로드
        GameDataManager.Inst.LoadData<MapData>();

        // 2. 인원수에 맞춰 맵 ID 할당
        currentSetupResult = MapManager.Inst.ProvideMapIdsForRound(testPlayerCount);

        if (currentSetupResult.PlayerMapIds.Count == 0)
        {
            Debug.LogError("[Test] 배정된 맵 ID가 없습니다.");
            return;
        }

        myAssignedMapId = currentSetupResult.PlayerMapIds[0];

        // 3. 편집용 BaseMap 스폰
        MapManager.Inst.ClearAllMaps();
        currentEditMapInstance = await MapManager.Inst.SpawnSingleEditMap(myAssignedMapId, editMapSpawnPosition);

        // 4. 자식 컴포넌트 참조 및 설정
        if (currentEditMapInstance != null)
        {
            currentBuildManager = currentEditMapInstance.GetComponentInChildren<SegmentBuildManager>();

            var inputHandler = currentEditMapInstance.GetComponentInChildren<GridInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.SetCamera(Camera.main);
            }

            if (currentBuildManager != null)
            {
                currentBuildManager.StartNewRound(1, new List<InventorySlot>());
            }
        }
    }

    /// <summary>
    /// [8번 키] 데이터 추출 -> 편집용 맵 파괴 -> 5개 레벨 데이터 합성
    /// </summary>
    private void TestSaveAndCombineLevelData()
    {
        if (currentEditMapInstance == null && currentBuildManager == null)
        {
            Debug.LogWarning("[Test] 편집 중인 맵이 없습니다!");
            return;
        }

        // 1. 데이터 추출
        CraftMapData myCraftData = null;

        if (currentBuildManager != null)
        {
            myCraftData = currentBuildManager.ExportCurrentCraftMapData(myAssignedMapId);
        }
        else if (currentEditMapInstance != null)
        {
            myCraftData = new CraftMapData
            {
                mapId = myAssignedMapId,
                placedSegements = currentEditMapInstance.GetPlacedDataList(GameManager.Inst.GameObjectManager)
            };
        }

        // 2. 편집용 맵 삭제
        ClearCurrentEditObjects();

        // 3. 5개 맵 전체 레벨 데이터 합성
        finalFullLevelData = new FullLevelData();

        if (myCraftData != null)
        {
            finalFullLevelData.allMapData.Add(myCraftData);
        }

        for (int i = 1; i < currentSetupResult.PlayerMapIds.Count; i++)
        {
            finalFullLevelData.allMapData.Add(new CraftMapData { mapId = currentSetupResult.PlayerMapIds[i] });
        }

        for (int i = 0; i < currentSetupResult.PresetMapIds.Count; i++)
        {
            finalFullLevelData.allMapData.Add(new CraftMapData { mapId = currentSetupResult.PresetMapIds[i] });
        }

        // 4. 무작위 셔플
        ShuffleList(finalFullLevelData.allMapData);
    }

    /// <summary>
    /// [9번 키] 합성된 레벨 씬에 스폰 (플레이 페이즈)
    /// </summary>
    private async UniTaskVoid TestBuildFullLevelAsync()
    {
        if (finalFullLevelData == null || finalFullLevelData.allMapData.Count == 0)
        {
            Debug.LogWarning("[Test] 합성된 맵 데이터가 없습니다!");
            return;
        }

        await MapManager.Inst.ImportFullLevelDataAsync(finalFullLevelData);
    }

    /// <summary>
    /// [0번 키] 플레이어 리스폰
    /// </summary>
    private void TestRespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        GameManager.Inst.RespawnPlayer(player);
    }

    private void ClearCurrentEditObjects()
    {
        if (currentEditMapInstance != null)
        {
            Destroy(currentEditMapInstance.gameObject);
            currentEditMapInstance = null;
        }

        currentBuildManager = null;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}