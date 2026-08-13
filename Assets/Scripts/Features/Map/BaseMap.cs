using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BaseMap : MonoBehaviour
{
    [Header("타일맵")]
    [SerializeField] private Grid Grid;
    [SerializeField] private Tilemap Tilemap_Ground;
    [SerializeField] private Tilemap Tilemap_CraftArea;

    [Header("포인트 좌표")]
    [SerializeField] private Transform Transform_startPoint;
    [SerializeField] private Transform Transform_arrivePoint;
    [SerializeField] private Transform Transform_spawnPoint;

    private List<int> placedSegmentInstanceId = new List<int>();
    public Vector3 StartPosition => Transform_startPoint.position;
    public Vector3 ArrivePosition => Transform_arrivePoint.position;

    private void Start()
    {
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return Grid.WorldToCell(worldPosition);
    }

    public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
    {
        return Grid.GetCellCenterWorld(cellPosition);
    }

    public bool CanBuild(Vector3 worldPosition, GameObjectManager objectManager)
    {
        Vector3Int cellPos = WorldToCell(worldPosition);

        if (Tilemap_Ground != null && Tilemap_Ground.HasTile(cellPos))
        {
            return false;
        }

        if (Tilemap_CraftArea != null && !Tilemap_CraftArea.HasTile(cellPos))
        {
            return false;
        }

        foreach (int instanceId in placedSegmentInstanceId)
        {
            if (objectManager.TryGetObject(instanceId, out GameObjectInstance instance))
            {
                if (WorldToCell(instance.transform.position) == cellPos)
                {
                    return false;

                }
            }
        }

        return true;
    }

    // 설치할 때 호출
    public void RegisterInstanceId(int instanceId)
    {
        if (!placedSegmentInstanceId.Contains(instanceId))
        {
            placedSegmentInstanceId.Add(instanceId);
        }
    }

    public void ClearAllPlacedObjects(GameObjectManager objectManager)
    {
        foreach (var instanceId in placedSegmentInstanceId)
        {
            objectManager.TryDestroyObject(instanceId);
        }
        placedSegmentInstanceId.Clear();
    }

    public void SetCraftAreaVisibility(bool isVisible)
    {
        if (Tilemap_CraftArea != null)
        {
            var renderer = Tilemap_CraftArea.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = isVisible;
            }
        }
    }

    public List<PlacedObjectData> GetPlacedDataList(GameObjectManager objectManager)
    {
        List<PlacedObjectData> dataList = new List<PlacedObjectData>();

        foreach (var instanceId in placedSegmentInstanceId)
        {
            if (objectManager.TryGetObject(instanceId, out GameObjectInstance instance))
            {
                Vector2Int cellPos2D = (Vector2Int)WorldToCell(instance.transform.position);

                dataList.Add(new PlacedObjectData
                {
                    InstanceId = instance.InstanceId,
                    Id = instance.gameObject.name,
                    GridPos = cellPos2D,
                    RotationStep = Mathf.RoundToInt(instance.transform.eulerAngles.z / 90f) % 4,
                });
            }
        }

        return dataList;
    }

    public async UniTask LoadPlacedData(List<PlacedObjectData> dataList, GameObjectManager objectManager)
    {
        ClearAllPlacedObjects(objectManager);

        if (dataList == null || dataList.Count == 0)
        {
            return;
        }

        foreach (var data in dataList)
        {
            var segmentData = GameDataManager.Inst.GetData<SegmentData>(data.Id);
            if (segmentData == null)
            {
                continue;
            }

            var prefabpath = segmentData.PrefabPath;
            GameObject prefab = await ResourceManager.Inst.LoadAsset<GameObject>(prefabpath);

            if (prefab != null)
            {
                Vector3Int cellPos3D = new Vector3Int(data.GridPos.x, data.GridPos.y, 0);
                Vector3 worldPos = GetCellCenterWorld(cellPos3D);
                Quaternion rotation = Quaternion.Euler(0f, 0f, data.RotationStep * 90f);

                if (objectManager.TryCreateObject(prefab, worldPos, rotation, transform, out GameObjectInstance createdInstance))
                {
                    createdInstance.gameObject.name = data.Id;
                    RegisterInstanceId(createdInstance.InstanceId);
                }
            }
        }
    }
}
