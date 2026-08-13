using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LocalMapTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private int testPlayerCount = 3; // 가상 유저 수
    [SerializeField] private Vector3 editMapSpawnPosition = Vector3.zero; // 편집용 맵 생성 위치

    [Header("세그먼트(제작 툴) 프리팹 설정")]
    [SerializeField] private AssetReferenceGameObject assetRef_SegmentPrefab; // 세그먼트 어드레서블 참조 (또는 GameObject로 받아도 됨)

    private RoundMapSetupResult currentSetupResult;
    private string myAssignedMapId; // 나에게 배정된 맵 ID
    private BaseMap currentEditMapInstance; // 현재 편집 중인 맵 오브젝트
    private GameObject currentSegmentInstance; // 동적 생성된 세그먼트(제작) 오브젝트
    private SegmentBuildManager currentBuildManager; // 세그먼트 내 빌드 매니저
    private FullLevelData finalFullLevelData; // 최종 모인 5개 맵 전체 데이터

    private void Update()
    {
        // [7번 키] 내 전용 맵 배정받고 + 세그먼트 스폰 (에디팅 페이즈 시작)
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TestStartEditingPhaseAsync().Forget();
        }

        // [8번 키] 편집 완료 후 데이터 추출 + 내 맵&세그먼트 파괴 + 5개 맵 데이터 합성
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
    /// [7번 키] 라운드 맵 배분 -> 나에게 맵 1개 할당 -> 단일 편집용 맵 및 세그먼트 스폰
    /// </summary>
    private async UniTaskVoid TestStartEditingPhaseAsync()
    {
        Debug.Log("<color=yellow>=== [7번] 맵 배정 및 제작 페이즈 시작 ===</color>");

        // 기존에 남아있던 테스트용 맵/세그먼트 정리
        ClearCurrentEditObjects();

        // 1. JSON 데이터로드 (MapData 등)
        GameDataManager.Inst.LoadData<MapData>();

        // 2. 인원수에 맞춰 맵 ID 할당
        currentSetupResult = MapManager.Inst.ProvideMapIdsForRound(testPlayerCount);

        if (currentSetupResult.PlayerMapIds.Count == 0)
        {
            Debug.LogError("배정된 맵 ID가 없습니다. MapData JSON을 확인하세요.");
            return;
        }

        // 첫 번째 맵 ID를 '나(Local Player)'의 맵으로 지정
        myAssignedMapId = currentSetupResult.PlayerMapIds[0];
        Debug.Log($"<color=cyan>[맵 배정 완료] 나에게 배정된 맵 ID: {myAssignedMapId}</color>");

        // 3. 나에게 할당된 기본 맵 스폰
        MapManager.Inst.ClearAllMaps();
        currentEditMapInstance = await MapManager.Inst.SpawnSingleEditMap(myAssignedMapId, editMapSpawnPosition);

        // 4. 💡 제작 단계 구동을 위한 'Segment' 프리팹 동적 스폰
        if (assetRef_SegmentPrefab != null)
        {
            var handle = Addressables.InstantiateAsync(assetRef_SegmentPrefab, editMapSpawnPosition, Quaternion.identity);
            currentSegmentInstance = await handle.ToUniTask();
        }

        // 5. 스폰된 세그먼트/맵에서 SegmentBuildManager 찾기
        if (currentSegmentInstance != null)
        {
            currentBuildManager = currentSegmentInstance.GetComponentInChildren<SegmentBuildManager>();
        }

        if (currentBuildManager == null && currentEditMapInstance != null)
        {
            currentBuildManager = currentEditMapInstance.GetComponentInChildren<SegmentBuildManager>();
        }

        Debug.Log($"<color=green>✔ 편집용 맵 및 세그먼트 스폰 완료! (BuildManager 탐색: {currentBuildManager != null})</color>");
    }

    /// <summary>
    /// [8번 키] 내가 배치한 함정 데이터 추출 -> 💡 편집용 맵 삭제 -> 5개 레벨 데이터 합성
    /// </summary>
    private void TestSaveAndCombineLevelData()
    {
        Debug.Log("<color=yellow>=== [8번] 내 맵 데이터 추출, 맵 파괴 및 5개 레벨 합성 ===</color>");

        if (currentEditMapInstance == null && currentBuildManager == null)
        {
            Debug.LogWarning("편집 중인 맵이 없습니다! 먼저 7번 키를 눌러 맵을 스폰하세요.");
            return;
        }

        // 1. SegmentBuildManager(또는 BaseMap)에서 내가 설치한 맵 데이터(CraftMapData) 추출
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

        // 2. 💡 [중복 방지 핵심] 데이터 추출이 끝났으므로 제작용 맵과 세그먼트 오브젝트 파괴!
        ClearCurrentEditObjects();

        // 3. 가상으로 전체 5개 맵 데이터 패킷(FullLevelData) 합성
        finalFullLevelData = new FullLevelData();

        // (1) 내 맵 데이터 추가
        if (myCraftData != null)
        {
            finalFullLevelData.allMapData.Add(myCraftData);
        }

        // (2) 나머지 가상 유저들의 맵 추가 (빈 함정 상태로 가상 생성)
        for (int i = 1; i < currentSetupResult.PlayerMapIds.Count; i++)
        {
            finalFullLevelData.allMapData.Add(new CraftMapData { mapId = currentSetupResult.PlayerMapIds[i] });
        }

        // (3) 모자란 사람 수를 채운 프리셋 맵 추가
        for (int i = 0; i < currentSetupResult.PresetMapIds.Count; i++)
        {
            finalFullLevelData.allMapData.Add(new CraftMapData { mapId = currentSetupResult.PresetMapIds[i] });
        }

        // 4. 5개 맵 순서를 랜덤하게 셔플 (경주할 때 순서가 무작위가 되도록)
        ShuffleList(finalFullLevelData.allMapData);

        string jsonLog = JsonUtility.ToJson(finalFullLevelData, true);
        Debug.Log($"<color=white>✔ 최종 5개 맵 수집 및 셔플 완료! (에디트 맵 삭제됨):\n{jsonLog}</color>");
    }

    /// <summary>
    /// [9번 키] 수집된 5개 맵 데이터를 이용해 월드에 이어붙여 최종 레벨 복원 스폰
    /// </summary>
    private async UniTaskVoid TestBuildFullLevelAsync()
    {
        Debug.Log("<color=yellow>=== [9번] 완성된 5개 맵 이어붙여 스폰 (경주 준비) ===</color>");

        if (finalFullLevelData == null || finalFullLevelData.allMapData.Count == 0)
        {
            Debug.LogWarning("합성된 맵 데이터가 없습니다! 먼저 8번 키를 눌러 데이터를 추출/합성하세요.");
            return;
        }

        // 1. 기존 잔여 맵 파괴 및 최종 맵 5개 비동기 스폰 & 이음새 연결
        await MapManager.Inst.ImportFullLevelDataAsync(finalFullLevelData);

        Debug.Log($"<color=green>✔ 총 {MapManager.Inst.MapCount}개의 맵 연결 스폰 완료!</color>");
        Debug.Log($"<color=green>시작점 좌표: {MapManager.Inst.CurrentSpawnPosition}</color>");
    }

    /// <summary>
    /// [0번 키] 플레이어 오브젝트를 시작 위치로 이동 (리스폰 테스트)
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

    /// <summary>
    /// 편집용으로 스폰했던 맵과 세그먼트를 씬에서 완전히 제거
    /// </summary>
    private void ClearCurrentEditObjects()
    {
        if (currentEditMapInstance != null)
        {
            Destroy(currentEditMapInstance.gameObject);
            currentEditMapInstance = null;
        }

        if (currentSegmentInstance != null)
        {
            // Addressables로 생성했을 경우 ReleaseInstance 사용 권장
            Addressables.ReleaseInstance(currentSegmentInstance);
            currentSegmentInstance = null;
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