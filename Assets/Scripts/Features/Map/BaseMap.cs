using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BaseMap : MonoBehaviour
{
    [Header("타일맵")]
    [SerializeField] private Grid Grid;

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

    public Vector2Int WorldToCell2D(Vector3 worldPosition)
    {
        Vector3Int cell3D = Grid.WorldToCell(worldPosition);
        return new Vector2Int(cell3D.x, cell3D.y);
    }

    public Vector3 GetCellCenterWorld2D(Vector2Int cellPosition)
    {
        Vector3Int cell3D = new Vector3Int(cellPosition.x, cellPosition.y, 0);
        return Grid.GetCellCenterWorld(cell3D);
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

    public List<PlacedObjectData> GetPlacedDataList(GameObjectManager objectManager)
    {
        List<PlacedObjectData> dataList = new List<PlacedObjectData>();

        foreach (var instanceId in placedSegmentInstanceId)
        {
            if (objectManager.TryGetObject(instanceId, out GameObjectInstance instance))
            {
                Vector2Int cellPos2D = WorldToCell2D(instance.transform.position);

                dataList.Add(new PlacedObjectData
                {
                    InstanceId = instance.InstanceId,
                    Id = instance.gameObject.name,
                    GridPos = cellPos2D,
                    RotationStep = Mathf.RoundToInt(instance.transform.eulerAngles.z / 90f) % 4,
                    RoundPlaced = 1 // TODO : 나중에 바뀌면 고치기
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

            var prefabPath = segmentData.PrefabPath;
            GameObject prefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(prefabPath);

            if (prefab != null)
            {
                Vector3 worldPos = GetCellCenterWorld2D(data.GridPos);
                Quaternion rotation = Quaternion.Euler(0f, 0f, data.RotationStep * 90f);

                if (objectManager.TryCreateObject(prefab, worldPos, rotation, transform, out GameObjectInstance createdInstance))
                {
                    createdInstance.gameObject.name = data.Id;
                    RegisterInstanceId(createdInstance.InstanceId);
                }
                else
                {
                    ResourceManager.Inst.TryReleaseAsset(prefabPath);
                }
            }
        }
    }
}
