using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Tilemaps;

// 배치 가능한 오브젝트 타입 분류
public enum PlaceableType
{
    Terrain,
    Obstacle,
    Hazard
}

// 배치 될 아이템 오브젝트의 정적 데이터 지금은 그레이 박스 테스트 용 임시 에셋 기준으로 테스트
[CreateAssetMenu(menuName = "Game/PlaceableObjectData", fileName = "PlaceableObjectData")]
public class PlaceableObjectData : ScriptableObject
{
    [Header("식별")]
    public string Id;

    [Header("에셋 참조")]
    public AssetReferenceGameObject AssetRef_Prefab;
    public TileBase TileAsset; 
    public Sprite Icon_Thumbnail;

    [Header("배치 규칙")]
    public Vector2Int Footprint = Vector2Int.one;
    public PlaceableType Type;
    public bool CanRotate = true;
}

// 개인 그리드 규격을 설정하는 공용 설정 에셋
[CreateAssetMenu(menuName = "Game/SegmentConfig", fileName = "SegmentConfig")]
public class SegmentConfig : ScriptableObject
{
    [Header("임시값 - 그레이박스 테스트 후 조정 예정")]
    public Vector2Int GridSize = new Vector2Int(12, 6);
    //public float CellSize = 1f;

    [Header("고정 확정값")]
    public Vector2Int EntryPos = new Vector2Int(0, 3);
    public Vector2Int ExitPos = new Vector2Int(11, 3);
    public int ProtectedZoneWidth = 2;
}