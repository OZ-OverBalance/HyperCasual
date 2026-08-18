using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LocalMapTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private int testPlayerCount = 1;
    [SerializeField] private Vector2 editMapSpawnPosition = Vector2.zero;
    [SerializeField] private GameObject Prefab_Player;

    private RoundMapSetupResult currentSetupResult;
    private string myAssignedMapId;
    private BaseMap currentEditMapInstance;
    private SegmentBuildManager currentBuildManager;
    private FullLevelData finalFullLevelData;
    private int currentRound = 1;

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

        // 1. 라운드 맵 정보 가져오기
        currentSetupResult = MapManager.Inst.SetupRoundMaps(currentRound, testPlayerCount);
        myAssignedMapId = currentSetupResult.PlayerMapIds[0];

        // 2. 내 맵의 누적 데이터 가져오기
        CraftMapData myPreviousData = MapManager.Inst.GetPlayerCraftMapData(0);
        int prevPlacedCount = (myPreviousData != null && myPreviousData.placedSegements != null) ? myPreviousData.placedSegements.Count : 0;

        Debug.Log($"<color=cyan>=== [7번] Round {currentRound} 편집 시작 (배정 맵: {myAssignedMapId}, 이전 기물: {prevPlacedCount}개) ===</color>");

        // 3. 편집용 BaseMap 스폰
        MapManager.Inst.ClearAllMaps();
        currentEditMapInstance = await MapManager.Inst.SpawnSingleEditMap(myAssignedMapId, editMapSpawnPosition);

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
                // 💡 [핵심] 이전 라운드 기물들이 있으면 먼저 복원
                if (myPreviousData != null && myPreviousData.placedSegements.Count > 0)
                {
                    await currentBuildManager.LoadExistingPlacedDataAsync(myPreviousData.placedSegements);
                }

                // 이번 라운드 시작 (새 인벤토리 부여)
                currentBuildManager.StartNewRound(currentRound, new List<InventorySlot>());
            }
        }
    }

    /// <summary>
    /// [8번 키] 데이터 추출 ->
    /// </summary>
    private void TestSaveAndCombineLevelData()
    {
        CraftMapData myUpdatedCraftData = null;

        if (currentBuildManager != null)
        {
            myUpdatedCraftData = currentBuildManager.ExportCurrentCraftMapData(myAssignedMapId);
        }
        else if (currentEditMapInstance != null)
        {
            myUpdatedCraftData = new CraftMapData
            {
                mapId = myAssignedMapId,
                placedSegements = currentEditMapInstance.GetPlacedDataList(GameManager.Inst.GameObjectManager)
            };
        }

        if (myUpdatedCraftData != null)
        {
            // 💡 [핵심] 리스트를 새 객체로 깊은 복사하여 영구 보존
            CraftMapData savedData = new CraftMapData
            {
                mapId = myUpdatedCraftData.mapId,
                placedSegements = new List<PlacedObjectData>(myUpdatedCraftData.placedSegements)
            };

            MapManager.Inst.UpdatePlayerCraftMapData(0, savedData);
            Debug.Log($"<color=cyan>✔ [Test] Round {currentRound} 맵 데이터 영구 저장 완료! (기물 수: {savedData.placedSegements.Count}개)</color>");
        }
        else
        {
            Debug.LogError("<color=red>❌ [Test] 저장할 기물 데이터를 추출하지 못했습니다!</color>");
        }

        // 맵 및 편집 오브젝트 정리
        ClearCurrentEditObjects();
    }

    private void ClearCurrentEditObjects()
    {
        var objectManager = GameManager.Inst.GameObjectManager;

        if (currentEditMapInstance != null)
        {
            // 💡 맵에 붙은 기물 인스턴스들 먼저 정리
            if (objectManager != null)
            {
                currentEditMapInstance.ClearAllPlacedObjects(objectManager);
            }

            Destroy(currentEditMapInstance.gameObject);
            currentEditMapInstance = null;
        }

        // MapManager에 등록된 activeMaps도 함께 정리
        MapManager.Inst.ClearAllMaps();

        currentBuildManager = null;
    }

    /// <summary>
    /// [9번 키] 합성된 레벨 씬에 스폰 (플레이 페이즈)
    /// </summary>
    private async UniTaskVoid TestBuildFullLevelAsync()
    {
        // 누적된 5개 맵 전체 데이터 복원 스폰
        await MapManager.Inst.ImportFullLevelDataAsync(MapManager.Inst.PersistentFullLevelData);

        // 다음 라운드 준비
        currentRound++;
    }

    /// <summary>
    /// [0번 키] 플레이어 리스폰
    /// </summary>
    private void TestRespawnPlayer()
    {
        Vector3 spawnPos = MapManager.Inst.CurrentSpawnPosition;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // 1. 씬에 이미 플레이어가 있다면 위치만 리셋
        if (player != null)
        {
            GameManager.Inst.RespawnPlayer(player);
            Debug.Log($"<color=cyan>[Test] 기존 플레이어 시작 위치({spawnPos})로 리스폰 완료</color>");
            return;
        }

        // 2. 인스펙터 연결 확인
        if (Prefab_Player == null)
        {
            Debug.LogError("[Test] prefab_Player 필드에 플레이어 프리팹을 드래그해서 넣어주세요!");
            return;
        }

        // 3. ResourceManager 로드 없이 GameObjectManager로 즉시 생성
        var objectManager = GameManager.Inst.GameObjectManager;
        if (objectManager.TryCreateObject(Prefab_Player, spawnPos, Quaternion.identity, null, out GameObjectInstance playerInstance))
        {
            playerInstance.gameObject.tag = "Player";
            Debug.Log($"<color=green>✔ [Test] 플레이어 직접 스폰 성공! (ID: {playerInstance.InstanceId} / 위치: {spawnPos})</color>");
        }
        else
        {
            Debug.LogError("[Test] 플레이어 생성 실패! 프리팹에 'GameObjectInstance' 스크립트가 붙어있는지 확인하세요.");
        }
    }
}