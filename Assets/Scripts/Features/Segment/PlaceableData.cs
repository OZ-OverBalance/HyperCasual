using System.Collections.Generic;
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
    public List<Vector2Int> CellOffsets = new() { Vector2Int.zero };
    public PlaceableType Type;
    public bool CanRotate = true;
    public bool RequiresSurfaceAttachment;

    [Header("방향 화살표 표시")]
    public bool ShowDirectionArrow;
    public Vector2Int ArrowLocalDirection = Vector2Int.right; 
    public Vector2Int ArrowOriginCell = Vector2Int.zero;
}