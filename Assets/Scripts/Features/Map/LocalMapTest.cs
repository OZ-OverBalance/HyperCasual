using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class LocalMapTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private int testPlayerCount = 3; // 가상 유저 수
    [SerializeField] private Vector3 editMapSpawnPosition = Vector3.zero; // 편집용 맵 생성 위치

    private RoundMapSetupResult currentSetupResult;
    private string myAssignedMapId; // 나에게 배정된 맵 ID
    private BaseMap currentEditMapInstance; // 현재 편집 중인 맵 오브젝트 (내부에 이미 빌더가 존재함)
    private SegmentBuildManager currentBuildManager; // BaseMap 내부의 빌드 매니저
    private FullLevelData finalFullLevelData; // 최종 모인 5개 맵 전체 데이터

    private void Update()
    {
        // [7번 키] 내 전용 맵 배정 및 편집 페이즈 시작
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TestStartEditingPhaseAsync().Forget();
        }

        // [8번 키] 편집 완료 후 데이터 추출 + 내 맵 파괴 + 5개 맵 데이터 합성
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            TestSaveAndCombineLevelData();
        }

        // [9번 키] 최종 5개 맵을 이어붙여 완성본 씬 스폰 (플레이 페이즈)
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TestBuildFullLevelAsync().Forget();
        }

        // [0번 키] 플레이어 출발지로 이동 (리스폰 테스트)
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TestRespawnPlayer();
        }
    }

    /// <summary>
    /// [7번 키] 라운드 맵 배분 -> 단일 편집용 BaseMap 스폰 (내부 빌더 활용)
    /// </summary>
    private async UniTaskVoid TestStartEditingPhaseAsync()
    {
        Debug.Log("<color=yellow>=== [7번] 맵 배정 및 제작 페이즈 시작 ===</color>");

        ClearCurrentEditObjects();

        // 1. JSON 데이터 로드
        GameDataManager.Inst.LoadData<MapData>();

        // 2. 인원수에 맞춰 맵 ID 할당
        currentSetupResult = MapManager.Inst.ProvideMapIdsForRound(testPlayerCount);

        if (currentSetupResult.PlayerMapIds.Count == 0)
        {
            Debug.LogError("배정된 맵 ID가 없습니다. MapData JSON을 확인하세요.");
            return;
        }

        myAssignedMapId = currentSetupResult.PlayerMapIds[0];
        Debug.Log($"<color=cyan>[맵 배정 완료] 나에게 배정된 맵 ID: {myAssignedMapId}</color>");

        // 3. 편집용 BaseMap 스폰 (이 프리팹 내부에 이미 SegmentBuildManager가 자식으로 포함되어 있음)
        MapManager.Inst.ClearAllMaps();
        currentEditMapInstance = await MapManager.Inst.SpawnSingleEditMap(myAssignedMapId, editMapSpawnPosition);

        // 4. BaseMap 자식 구조에서 SegmentBuildManager 및 GridInputHandler 탐색 (중복 스폰 제거)
        if (currentEditMapInstance != null)
        {
            currentBuildManager = currentEditMapInstance.GetComponentInChildren<SegmentBuildManager>();

            // 입력 카메라 연결
            var inputHandler = currentEditMapInstance.GetComponentInChildren<GridInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.SetCamera(Camera.main);
            }

            // 빌더 초기화 및 라운드 시작 (아이템 배치 허용)
            if (currentBuildManager != null)
            {
                currentBuildManager.StartNewRound(1, new List<InventorySlot>());
            }

            // 편집 영역 표시 켜기
            currentEditMapInstance.SetCraftAreaVisibility(true);
        }

        Debug.Log($"<color=green>✔ 편집용 맵 스폰 완료! (BuildManager 탐색 성공 여부: {currentBuildManager != null})</color>");
    }

    /// <summary>
    /// [8번 키] 내가 배치한 함정 데이터 추출 -> 편집용 맵 삭제 -> 5개 레벨 데이터 합성
    /// </summary>
    private void TestSaveAndCombineLevelData()
    {
        Debug.Log("<color=yellow>=== [8번] 내 맵 데이터 추출, 맵 파괴 및 5개 레벨 합성 ===</color>");

        if (currentEditMapInstance == null && currentBuildManager == null)
        {
            Debug.LogWarning("편집 중인 맵이 없습니다! 먼저 7번 키를 눌러 맵을 스폰하세요.");
            return;
        }

        // 1. BaseMap 내의 SegmentBuildManager에서 배치 데이터 추출
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

        int placedCount = myCraftData != null ? myCraftData.placedSegements.Count : 0;
        Debug.Log($"<color=cyan>[내 맵 저장 완료] 설치된 장애물 수: {placedCount}개</color>");

        // 2. 추출이 완료되었으므로 편집용 BaseMap 파괴
        ClearCurrentEditObjects();

        // 3. 5개 맵 전체 데이터 합성
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

        ShuffleList(finalFullLevelData.allMapData);

        string jsonLog = JsonUtility.ToJson(finalFullLevelData, true);
        Debug.Log($"<color=white>✔ 최종 5개 맵 수집 및 셔플 완료! (에디트 맵 삭제됨):\n{jsonLog}</color>");
    }

    /// <summary>
    /// [9번 키] 완성된 5개 맵 이어붙여 스폰 (플레이 페이즈)
    /// </summary>
    private async UniTaskVoid TestBuildFullLevelAsync()
    {
        Debug.Log("<color=yellow>=== [9번] 완성된 5개 맵 이어붙여 스폰 (경주 준비) ===</color>");

        if (finalFullLevelData == null || finalFullLevelData.allMapData.Count == 0)
        {
            Debug.LogWarning("합성된 맵 데이터가 없습니다! 먼저 8번 키를 눌러 데이터를 추출/합성하세요.");
            return;
        }

        await MapManager.Inst.ImportFullLevelDataAsync(finalFullLevelData);

        Debug.Log($"<color=green>✔ 총 {MapManager.Inst.MapCount}개의 맵 연결 스폰 완료!</color>");
        Debug.Log($"<color=green>시작점 좌표: {MapManager.Inst.CurrentSpawnPosition}</color>");
    }

    /// <summary>
    /// [0번 키] 플레이어 출발지로 이동
    /// </summary>
    private void TestRespawnPlayer()
    {
        Debug.Log("<color=yellow>=== [0번] 플레이어 스폰/리스폰 테스트 ===</color>");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("씬에 'Player' 태그를 가진 오브젝트가 없습니다!");
            return;
        }

        GameManager.Inst.RespawnPlayer(player);
        Debug.Log($"<color=cyan>플레이어가 {player.transform.position} 위치로 스폰/리스폰되었습니다.</color>");
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