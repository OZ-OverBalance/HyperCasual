using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Tilemaps;


// 빌드 페이즈 전체를 담당하는 매니저.
public class SegmentBuildManager : MonoBehaviour
{

    [Header("공용 그리드/타일맵 참조")]
    [SerializeField] private Grid Grid_Shared; 
    [SerializeField] private Tilemap Tilemap_PlayerPlacement;
    [SerializeField] private Transform Transform_PlacedObjectsRoot;

    [Header("설정")]
    [SerializeField] private SegmentConfig Data_Config;
    [SerializeField] private List<PlaceableObjectData> Catalog_AllItems; 
    [SerializeField] private KeyCode Key_Rotate = KeyCode.R;


    private readonly Dictionary<Vector2Int, string> _occupancy = new();
    private readonly Dictionary<string, PlaceableObjectData> _catalogById = new();
    private readonly List<PlacedObjectData> _placedObjects = new();
    private readonly PlayerInventory _inventory = new();
    private readonly HashSet<string> _loadedAddresses = new();

    private GameObjectManager ObjectManager { get { return GameManager.Inst.GameObjectManager;  } }

    private PlaceableObjectData _selectedItem;
    private int _selectedRotation;
    private int _currentRound = 1;

    private bool _isBuildLocked;
    public bool IsBuildLocked { get { return _isBuildLocked; } }

    public event Action<PlaceableObjectData> OnItemSelected;
    public event Action<PlacedObjectData> OnObjectPlaced;
    public event Action<int> OnObjectRemoved;
    public event Action<int> OnRotationChanged;

    //세그먼트 제작 상태를 알리는 이벤트
    public event Action OnBuildCompleted;
    public event Action OnBuildResumed;

    protected void Awake()
    {
        GameDataManager.Inst.LoadData<SegmentData>(); //임시
        BuildCatalogLookup();
        InitializeGrid();   
    }

    private void Update()
    {
        if (_selectedItem != null && Input.GetKeyDown(Key_Rotate))
        {
            RotateSelection();
        }
    }

    #region 초기화

    private void BuildCatalogLookup()
    {
        _catalogById.Clear();
        foreach (var data in Catalog_AllItems)
        {
            _catalogById[data.Id] = data;
        }
    }

    private void InitializeGrid()
    {
        _occupancy.Clear();
        MarkProtectedZone(Data_Config.EntryPos, Data_Config.ProtectedZoneSize);
        MarkProtectedZone(new Vector2Int(Data_Config.ExitPos.x - Data_Config.ProtectedZoneSize.x + 1, Data_Config.ExitPos.y), Data_Config.ProtectedZoneSize);
    }

    private void MarkProtectedZone(Vector2Int anchor, Vector2Int size)
    {
        int startY = anchor.y - (size.y / 2);

        for (int x = anchor.x; x < anchor.x + size.x; x++)
        {
            for (int y = startY; y < startY + size.y; y++)
            {
                if (x < 0 || y < 0 || x >= Data_Config.GridSize.x || y >= Data_Config.GridSize.y) continue;

                _occupancy[new Vector2Int(x, y)] = "PROTECTED";
            }
        }
    }

    #endregion

    #region 라운드 관리

    public void StartNewRound(int roundIndex, List<InventorySlot> newInventory)
    {
        _currentRound = roundIndex;
        _inventory.Slots = newInventory;
        _isBuildLocked = false;
        DeselectItem();
    }

    public void CompleteBuild()
    {
        if (_isBuildLocked) return;

        _isBuildLocked = true;
        DeselectItem();
        OnBuildCompleted?.Invoke();
    }

    public void ResumeBuild()
    {
        if (!_isBuildLocked) return;

        _isBuildLocked = false;
        OnBuildResumed?.Invoke();
    }

    public void ToggleBuildComplete()
    {
        if (_isBuildLocked)
        {
            ResumeBuild();
        }
        else
        {
            CompleteBuild();
        }
    }

    #endregion

    #region 선택 / 회전

    public void SelectItem(PlaceableObjectData data)
    {
        if (_isBuildLocked) return;
        if (!_inventory.CanPlace(data)) return;

        _selectedItem = data;
        _selectedRotation = 0; 
        OnItemSelected?.Invoke(_selectedItem);
    }

    private void DeselectItem()
    {
        _selectedItem = null;
        _selectedRotation = 0;
        OnItemSelected?.Invoke(null);
    }

    private void RotateSelection()
    {
        if (!_selectedItem.CanRotate) return;
        _selectedRotation = (_selectedRotation + 1) % 4;
        OnRotationChanged?.Invoke(_selectedRotation);
    }

    #endregion

    #region 배치

    public void TryPlaceAt(Vector3 worldPos)
    {
        if (_isBuildLocked) return;
        if (_selectedItem == null) return;

        var cellPos = ToCell(worldPos);
        var rotatedOffsets = GetRotatedOffsets(_selectedItem.CellOffsets, _selectedRotation);

        if (!ValidatePlacement(cellPos, rotatedOffsets, _selectedItem, _selectedRotation)) return;

        PlaceObjectAsync(cellPos, rotatedOffsets).Forget();
    }

    private async UniTaskVoid PlaceObjectAsync(Vector2Int cellPos, List<Vector2Int> rotatedOffsets)
    {
        var itemToPlace = _selectedItem;

        var worldPos = GetCellCenterWorldPos(cellPos);
        var rotation = Quaternion.Euler(0f, 0f, _selectedRotation * 90f);

        var segementData = GameDataManager.Inst.GetData<SegmentData>(itemToPlace.Id);
        if (segementData == null)
        {
            return;
        }

        string address = segementData.PrefabPath;
        GameObject prefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(address);

        if (prefab == null)
        {
            Debug.LogError("[SegmentBuildManager] 프리팹 로드 실패: " + itemToPlace.Id);
            return;
        }

        _loadedAddresses.Add(address);

        if (!ObjectManager.TryCreateObject(prefab, worldPos, rotation, Transform_PlacedObjectsRoot, out GameObjectInstance instance))
        {
            Debug.LogError("[SegmentBuildManager] 오브젝트 생성 실패: " + itemToPlace.Id);
            return;
        }

        instance.gameObject.name = itemToPlace.Id;

        var placed = new PlacedObjectData
        {
            InstanceId = instance.InstanceId,
            Id = itemToPlace.Id,
            GridPos = cellPos,
            RotationStep = _selectedRotation,
            RoundPlaced = _currentRound
        };

        _placedObjects.Add(placed);
        MarkOccupancy(cellPos, rotatedOffsets, placed.InstanceId.ToString());

        BaseMap currentMap = GetComponentInParent<BaseMap>();
        if (currentMap != null)
        {
            currentMap.RegisterInstanceId(instance.InstanceId);
        }

        if (itemToPlace.TileAsset != null)
        {
            Tilemap_PlayerPlacement.SetTile((Vector3Int)cellPos, itemToPlace.TileAsset);
        }

        _inventory.ConsumeItem(itemToPlace);
        OnObjectPlaced?.Invoke(placed);

        if (!_inventory.CanPlace(itemToPlace))
        {
            DeselectItem();
        }
    }

    #endregion

    #region 삭제

    // 폭탄 방식 - 인벤토리 복구 없이 완전 소멸
    public void RemoveObject(int instanceId)
    {
        RemoveObjectInternal(instanceId, refundToInventory: false);
    }

    // 자유 수정 방식 - 배치 모드에서 이미 배치한 아이템도 재설치 가능 (인벤토리로 복구 됨) 
    public void RemoveObjectAndRefund(int instanceId)
    {
        RemoveObjectInternal(instanceId, refundToInventory: true);
    }

    // 위 두 방식을 게임룰에 따라 자유롭게 변경할 목적으로 구현한 공통 메서드 
    private void RemoveObjectInternal(int instanceId, bool refundToInventory)
    {
        if (_isBuildLocked) return;

        var target = FindPlacedObject(instanceId);
        if (target == null) return;

        if (!_catalogById.TryGetValue(target.Id, out var data))
        {
            Debug.LogError("[SegmentBuildManager] 카탈로그에 없는 Id: " + target.Id);
            return;
        }

        var rotatedOffsets = GetRotatedOffsets(data.CellOffsets, target.RotationStep);

        ClearOccupancy(target.GridPos, rotatedOffsets);
        Tilemap_PlayerPlacement.SetTile((Vector3Int)target.GridPos, null);

        ObjectManager.TryDestroyObject(instanceId);

        _placedObjects.Remove(target);

        if (refundToInventory)
        {
            _inventory.RefundItem(data);
        }

        OnObjectRemoved?.Invoke(instanceId);
    }

    private PlacedObjectData FindPlacedObject(int instanceId)
    {
        for (int i = 0; i < _placedObjects.Count; i++)
        {
            if (_placedObjects[i].InstanceId == instanceId) return _placedObjects[i];
        }
        return null;
    }

    #endregion

    #region 유효성 검증

    private bool ValidatePlacement(Vector2Int origin, List<Vector2Int> rotatedOffsets, PlaceableObjectData data, int rotationStep)
    {
        for (int i = 0; i < rotatedOffsets.Count; i++)
        {
            var cell = origin + rotatedOffsets[i];
            if (cell.x < 0 || cell.y < 0 || cell.x >= Data_Config.GridSize.x || cell.y >= Data_Config.GridSize.y)
            {
                return false;
            }
            if (_occupancy.ContainsKey(cell))
            {
                return false;
            }
        }

        if (data.RequiresSurfaceAttachment && !HasAdjacentSurface(origin, rotatedOffsets, rotationStep))
        {
            return false;
        }

        return true;
    }

    private bool HasAdjacentSurface(Vector2Int origin, List<Vector2Int> rotatedOffsets, int rotationStep)
    {
        Vector2Int floorDirection = RotateOffset(Vector2Int.down, rotationStep);

        for (int i = 0; i < rotatedOffsets.Count; i++)
        {
            var cell = origin + rotatedOffsets[i];
            var floorNeighbor = cell + floorDirection;

            if (IsPartOfOwnFootprint(floorNeighbor, origin, rotatedOffsets)) continue;

            if (_occupancy.TryGetValue(floorNeighbor, out var occupant) && occupant != "PROTECTED")
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPartOfOwnFootprint(Vector2Int cell, Vector2Int origin, List<Vector2Int> rotatedOffsets)
    {
        for (int i = 0; i < rotatedOffsets.Count; i++)
        {
            if (origin + rotatedOffsets[i] == cell) return true;
        }
        return false;
    }

    private static readonly Vector2Int[] AdjacentDirections =
    {
    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public bool IsPlacementValid(Vector3 worldPos, PlaceableObjectData data, int rotationStep)
    {
        var cellPos = ToCell(worldPos);
        var rotatedOffsets = GetRotatedOffsets(data.CellOffsets, rotationStep);
        return ValidatePlacement(cellPos, rotatedOffsets, data, rotationStep);
    }

    // 경로를 완전히 봉쇄하는 것을 게임 룰 상 허용하기로 결정됨에 따라 메서드 주석 처리 / 나중에 필요하게 될 경우 다시 사용할 예정
    //private bool HasValidPathAfterPlacement(Vector2Int tempOrigin, List<Vector2Int> tempOffsets)
    //{
    //    var visited = new HashSet<Vector2Int> { Data_Config.EntryPos };
    //    var queue = new Queue<Vector2Int>();
    //    queue.Enqueue(Data_Config.EntryPos);

    //    while (queue.Count > 0)
    //    {
    //        var current = queue.Dequeue();
    //        if (current == Data_Config.ExitPos) return true;

    //        foreach (var dir in NeighborDirections)
    //        {
    //            var next = current + dir;
    //            if (visited.Contains(next)) continue;
    //            if (next.x < 0 || next.y < 0 || next.x >= Data_Config.GridSize.x || next.y >= Data_Config.GridSize.y) continue;
    //            if (IsBlocked(next, tempOrigin, tempOffsets)) continue;

    //            visited.Add(next);
    //            queue.Enqueue(next);
    //        }
    //    }

    //    return false;
    //}

    //private static readonly Vector2Int[] NeighborDirections =
    //{
    //    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    //};

    //private bool IsBlocked(Vector2Int cell, Vector2Int tempOrigin, List<Vector2Int> tempOffsets)
    //{
    //    if (_occupancy.TryGetValue(cell, out var occupant) && occupant != "PROTECTED")
    //    {
    //        return true;
    //    }

    //    for (int i = 0; i < tempOffsets.Count; i++)
    //    {
    //        if (tempOrigin + tempOffsets[i] == cell) return true;
    //    }

    //    return false;
    //}

    #endregion

    #region 점유 관련 헬퍼

    private void MarkOccupancy(Vector2Int origin, List<Vector2Int> rotatedOffsets, string instanceId)
    {
        for (int i = 0; i < rotatedOffsets.Count; i++)
        {
            _occupancy[origin + rotatedOffsets[i]] = instanceId;
        }
    }

    private void ClearOccupancy(Vector2Int origin, List<Vector2Int> rotatedOffsets)
    {
        for (int i = 0; i < rotatedOffsets.Count; i++)
        {
            _occupancy.Remove(origin + rotatedOffsets[i]);
        }
    }

    #endregion

    #region 좌표 변환 헬퍼

    public Vector2Int ToCell(Vector3 worldPos)
    {
        return (Vector2Int)Grid_Shared.WorldToCell(worldPos);
    }

    public Vector3 GetCellCenterWorldPos(Vector2Int cellPos)
    {
        Vector3 cellSize = Grid_Shared.cellSize;
        Vector3 halfOffset = new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
        return Grid_Shared.CellToWorld((Vector3Int)cellPos) + halfOffset;
    }

    private Vector2Int RotateOffset(Vector2Int offset, int rotationStep)
    {
        switch (rotationStep % 4)
        {
            case 1: return new Vector2Int(-offset.y, offset.x);
            case 2: return new Vector2Int(-offset.x, -offset.y);
            case 3: return new Vector2Int(offset.y, -offset.x);
            default: return offset;
        }
    }

    public List<Vector2Int> GetRotatedOffsets(List<Vector2Int> cellOffsets, int rotationStep)
    {
        var result = new List<Vector2Int>();
        for (int i = 0; i < cellOffsets.Count; i++)
        {
            result.Add(RotateOffset(cellOffsets[i], rotationStep));
        }
        return result;
    }

    public IReadOnlyList<PlacedObjectData> GetPlacedObjects()
    {
        return _placedObjects;
    }

    #endregion

    #region 디버그 (Gizmo)

    private void OnDrawGizmos()
    {
        if (Grid_Shared == null || Data_Config == null) return;

        DrawGridBounds();

        if (_occupancy == null) return;

        foreach (var pair in _occupancy)
        {
            Vector3 worldPos = GetCellCenterWorldPos(pair.Key);
            Gizmos.color = pair.Value == "PROTECTED" ? Color.yellow : Color.red;
            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
        }
    }

    private void DrawGridBounds()
    {
        Gizmos.color = Color.cyan;

        Vector3 min = Grid_Shared.CellToWorld(Vector3Int.zero);
        Vector3 max = Grid_Shared.CellToWorld(new Vector3Int(Data_Config.GridSize.x, Data_Config.GridSize.y, 0));
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        Gizmos.DrawWireCube(center, size);

        for (int x = 0; x <= Data_Config.GridSize.x; x++)
        {
            Vector3 lineStart = Grid_Shared.CellToWorld(new Vector3Int(x, 0, 0));
            Vector3 lineEnd = Grid_Shared.CellToWorld(new Vector3Int(x, Data_Config.GridSize.y, 0));
            Gizmos.DrawLine(lineStart, lineEnd);
        }

        for (int y = 0; y <= Data_Config.GridSize.y; y++)
        {
            Vector3 lineStart = Grid_Shared.CellToWorld(new Vector3Int(0, y, 0));
            Vector3 lineEnd = Grid_Shared.CellToWorld(new Vector3Int(Data_Config.GridSize.x, y, 0));
            Gizmos.DrawLine(lineStart, lineEnd);
        }
    }

    #endregion

    public CraftMapData ExportCurrentCraftMapData(string currentMapId)
    {
        CraftMapData mapData = new CraftMapData
        {
            mapId = currentMapId,
            placedSegements = new List<PlacedObjectData>(_placedObjects)
        };

        return mapData;
    }

    public async UniTask LoadExistingPlacedDataAsync(List<PlacedObjectData> existingData)
    {
        if (existingData == null || existingData.Count == 0) return;

        if (_catalogById.Count == 0)
        {
            BuildCatalogLookup();
        }

        BaseMap currentMap = GetComponentInParent<BaseMap>();
        Transform parentRoot = Transform_PlacedObjectsRoot != null ? Transform_PlacedObjectsRoot : transform;

        _placedObjects.Clear();

        foreach (var data in existingData)
        {
            var segmentData = GameDataManager.Inst.GetData<SegmentData>(data.Id);
            if (segmentData == null)
            {
                continue;
            }

            GameObject prefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(segmentData.PrefabPath);
            if (prefab == null)
            {
                continue;
            }

            _loadedAddresses.Add(segmentData.PrefabPath);

            Vector3 worldPos = GetCellCenterWorldPos(data.GridPos);
            Quaternion rotation = Quaternion.Euler(0f, 0f, data.RotationStep * 90f);

            if (ObjectManager.TryCreateObject(prefab, worldPos, rotation, Transform_PlacedObjectsRoot, out GameObjectInstance instance))
            {
                instance.gameObject.name = data.Id;

                if (_catalogById.TryGetValue(data.Id, out var catalogData))
                {
                    var rotatedOffsets = GetRotatedOffsets(catalogData.CellOffsets, data.RotationStep);
                    MarkOccupancy(data.GridPos, rotatedOffsets, instance.InstanceId.ToString());

                    if (catalogData.TileAsset != null && Tilemap_PlayerPlacement != null)
                    {
                        Tilemap_PlayerPlacement.SetTile((Vector3Int)data.GridPos, catalogData.TileAsset);
                    }
                }
                else
                {
                    _occupancy[data.GridPos] = instance.InstanceId.ToString();
                }

                if (currentMap != null)
                {
                    currentMap.RegisterInstanceId(instance.InstanceId);
                }

                _placedObjects.Add(new PlacedObjectData
                {
                    InstanceId = instance.InstanceId,
                    Id = data.Id,
                    GridPos = data.GridPos,
                    RotationStep = data.RotationStep,
                    RoundPlaced = data.RoundPlaced
                });
            }
            else
            {
                Debug.LogError($"[SegmentBuildManager] 오브젝트 스폰 실패: {data.Id}");
            }
        }
    }
}