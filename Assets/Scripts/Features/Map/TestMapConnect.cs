using UnityEngine;

public class TestMapConnect : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private int playerCount = 4; // 테스트할 맵 연결 개수

    private System.Collections.Generic.List<int> currentMapIndices;

    private void Update()
    {
        // [1번 키] 맵 랜덤 동적 생성 및 이어붙이기 검증
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestMapGenerationAndConnection();
        }

        // [2번 키] 생성된 맵들의 이음새(Arrive -> Next Start) 좌표 콘솔 출력
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ValidateMapConnections();
        }

        // [3번 키] 전체 맵 초기화 (삭제)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            MapManager.Inst.ClearAllMaps();
            Debug.Log("<color=red>모든 맵이 제거되었습니다.</color>");
        }
    }

    private void TestMapGenerationAndConnection()
    {
        Debug.Log("<color=yellow>=== [1] 맵 랜더링 및 연결 테스트 시작 ===</color>");

        // 1. Host 기준 랜덤 인덱스 생성
        currentMapIndices = MapManager.Inst.GenerateRandomMapIndices(playerCount);

        if (currentMapIndices == null || currentMapIndices.Count == 0)
        {
            Debug.LogError("맵 인덱스 생성 실패! MapManager의 Prefab_baseMap 개수가 부족한지 확인하세요.");
            return;
        }

        // 2. 인덱스 기반 맵 동적 생성
        MapManager.Inst.BuildLevelFromIndices(currentMapIndices);
        Debug.Log($"<color=cyan>맵 {playerCount}개 생성 완료! (선택된 맵 인덱스: {string.Join(", ", currentMapIndices)})</color>");

        // 3. 이음새 오차 자동 검증 실행
        ValidateMapConnections();
    }

    private void ValidateMapConnections()
    {
        int mapCount = MapManager.Inst.MapCount;
        if (mapCount == 0)
        {
            Debug.LogWarning("생성된 맵이 없습니다. 먼저 1번 키를 눌러주세요.");
            return;
        }

        Debug.Log($"<color=green>=== [2] 맵 이음새 좌표 오차 검증 (총 {mapCount}개 맵) ===</color>");

        for (int i = 0; i < mapCount; i++)
        {
            BaseMap currentMap = MapManager.Inst.GetMap(i);
            Debug.Log($"[Map {i}] StartPos: {currentMap.StartPosition} | ArrivePos: {currentMap.ArrivePosition}");

            // 다음 맵과의 이음새 간격 확인
            if (i < mapCount - 1)
            {
                BaseMap nextMap = MapManager.Inst.GetMap(i + 1);
                float distance = Vector3.Distance(currentMap.ArrivePosition, nextMap.StartPosition);
                Debug.Log($"  └─► [Map {i} -> Map {i + 1} 연결 거리]: <color=orange>{distance:F2} unit</color>");
            }
        }
    }

    // Scene 창에 맵 연결선을 시각적으로 그려주는 기즈모 연출
    private void OnDrawGizmos()
    {
        if (MapManager.Inst == null || MapManager.Inst.MapCount == 0) return;

        for (int i = 0; i < MapManager.Inst.MapCount; i++)
        {
            BaseMap map = MapManager.Inst.GetMap(i);
            if (map == null) continue;

            // StartPoint : 파란색 구체
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(map.StartPosition, 0.4f);

            // ArrivePoint : 빨간색 구체
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(map.ArrivePosition, 0.4f);

            // 맵 내부 연결선 (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(map.StartPosition, map.ArrivePosition);

            // 다음 맵과의 연결선 (초록색 점선/선)
            if (i < MapManager.Inst.MapCount - 1)
            {
                BaseMap nextMap = MapManager.Inst.GetMap(i + 1);
                if (nextMap != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(map.ArrivePosition, nextMap.StartPosition);
                }
            }
        }
    }
}
