using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalMapTestController : MonoBehaviour
{
    [Header("테스트 가상 인원 설정")]
    [SerializeField] private int testPlayerCount = 3; // 가상 플레이어 수 (3명이면 유저 맵 3개 + 프리셋 2개 = 총 5개 생성)

    private RoundMapSetupResult lastMapSetup;
    private FullLevelData lastExportedData;

    private void Update()
    {
        // [7번 키] 데이터 로드 + 5개 맵 구조 배분 및 비동기 동적 생성 테스트
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TestBuildLevelAsync().Forget();
        }

        // [8번 키] 깃발 위치 기반 리스폰 및 이동 테스트
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            TestPlayerRespawn();
        }

        // [9번 키] 데이터 추출 (Export) 테스트
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TestExportData();
        }

        // [10번 키] 추출한 데이터 기반 복원 (Import) 비동기 테스트
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TestImportDataAsync().Forget();
        }
    }

    /// <summary>
    /// [1번] 엑셀 데이터 로드 -> 맵 5개 제공 배분 -> Addressables 비동기 스폰
    /// </summary>
    private async UniTaskVoid TestBuildLevelAsync()
    {
        Debug.Log("<color=yellow>=== [1] 로컬 맵 제공 및 동적 로드 테스트 시작 ===</color>");

        // 1. JSON 데이터 로드 (GameDataManager 테스트)
        GameDataManager.Inst.LoadData<MapData>();

        // 2. 플레이어 인원수 기반 맵 5개 배분 (ProvideMapIdsForRound)
        lastMapSetup = MapManager.Inst.ProvideMapIdsForRound(testPlayerCount);

        Debug.Log($"<color=cyan>[맵 배분 결과] 유저 맵: {lastMapSetup.PlayerMapIds.Count}개, 프리셋 맵: {lastMapSetup.PresetMapIds.Count}개 | 총: {lastMapSetup.FullRoundMapIds.Count}개</color>");
        Debug.Log($"선택된 맵 ID 순서: {string.Join(", ", lastMapSetup.FullRoundMapIds)}");

        // 3. Addressables + UniTask 기반 비동기 맵 스폰
        await MapManager.Inst.BuildLevelFromMapId(lastMapSetup.FullRoundMapIds);

        Debug.Log($"<color=green>✔ 맵 5개 생성 및 연결 완료! 기본 리스폰 위치: {MapManager.Inst.CurrentSpawnPosition}</color>");
    }

    /// <summary>
    /// [2번] GameManager의 리스폰 작동 테스트
    /// </summary>
    private void TestPlayerRespawn()
    {
        Debug.Log("<color=yellow>=== [2] 리스폰 테스트 ===</color>");

        GameObject dummyPlayer = GameObject.FindGameObjectWithTag("Player");

        if (dummyPlayer == null)
        {
            Debug.LogWarning("씬에 'Player' 태그를 가진 오브젝트가 없습니다. 하이어라키의 테스트 플레이어 태그를 'Player'로 설정해주세요.");
            return;
        }

        // GameManager의 리스폰 호출
        GameManager.Inst.RespawnPlayer(dummyPlayer);
        Debug.Log($"<color=cyan>플레이어가 리스폰 위치({MapManager.Inst.CurrentSpawnPosition})로 이동되었습니다.</color>");
    }

    /// <summary>
    /// [3번] 맵 세부 데이터 추출 (Export)
    /// </summary>
    private void TestExportData()
    {
        Debug.Log("<color=yellow>=== [3] 맵 데이터 Export 테스트 ===</color>");

        if (MapManager.Inst.MapCount == 0)
        {
            Debug.LogWarning("먼저 1번 키를 눌러 맵을 생성해주세요!");
            return;
        }

        // 임의의 인덱스 목록 생성 후 데이터 추출
        System.Collections.Generic.List<int> mockIndices = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };
        lastExportedData = MapManager.Inst.ExportFullLevelData(mockIndices);

        string jsonOutput = JsonUtility.ToJson(lastExportedData, true);
        Debug.Log($"<color=white>추출된 FullLevelData (JSON):\n{jsonOutput}</color>");
    }

    /// <summary>
    /// [4번] 추출된 데이터 기반으로 전체 레벨 복원 (Import)
    /// </summary>
    private async UniTaskVoid TestImportDataAsync()
    {
        Debug.Log("<color=yellow>=== [4] ImportFullLevelDataAsync 복원 테스트 ===</color>");

        if (lastExportedData == null)
        {
            Debug.LogWarning("저장된 LevelData가 없습니다! 먼저 3번 키를 눌러 데이터를 추출하세요.");
            return;
        }

        // 저장된 맵 및 기물 데이터 기반 복원 로드
        await MapManager.Inst.ImportFullLevelDataAsync(lastExportedData);
        Debug.Log("<color=green>✔ 저장된 데이터 기반 레벨 복원 완료!</color>");
    }
}