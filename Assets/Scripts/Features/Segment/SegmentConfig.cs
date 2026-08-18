using UnityEngine;

// 개인 그리드 규격을 설정하는 공용 설정 에셋
[CreateAssetMenu(menuName = "Game/SegmentConfig", fileName = "SegmentConfig")]
public class SegmentConfig : ScriptableObject
{
    [Header("임시값 - 그레이박스 테스트 후 조정 예정")]
    public Vector2Int GridSize = new Vector2Int(20, 20);

    [Header("고정 확정값")]
    public Vector2Int EntryPos = new Vector2Int(0, 3);
    public Vector2Int ExitPos = new Vector2Int(19, 3);
    public Vector2Int ProtectedZoneSize = new Vector2Int(4, 3);
}