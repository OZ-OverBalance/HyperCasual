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
        if (Tilemap_CraftArea != null)
        {
            TilemapRenderer renderer = Tilemap_CraftArea.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
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

    public List<PlacedSegementData> GetPlacedDataList(GameObjectManager objectManager)
    {
        List<PlacedSegementData> dataList = new List<PlacedSegementData>();

        foreach (var instanceId in placedSegmentInstanceId)
        {
            if (objectManager.TryGetObject(instanceId, out GameObjectInstance instance))
            {
                // TODO: GameObjectInstance 또는 기물 컴포넌트에서 PrefabId를 추출하여 할당
                dataList.Add(new PlacedSegementData
                {
                    placedSegementId = 0,
                    cellPosition = WorldToCell(instance.transform.position)
                });
            }
        }

        return dataList;
    }

    // TODO : 장애물 코드에 맞게 수정
    public async UniTask LoadPlacedData(List<PlacedSegementData> dataList, GameObjectManager objectManager)
    {
        ClearAllPlacedObjects(objectManager);

        foreach (var data in dataList)
        {
            //GameObject prefab = await ResourceManager.Inst.LoadAsset<GameObject>();

            //if (prefab != null)
            //{
            //    Vector3 worldPos = GetCellCenterWorld(data.cellPosition);

            //    if (objectManager.TryCreateObject(prefab, worldPos, Quaternion.identity, transform, out GameObjectInstance createdInstance))
            //    {
            //        RegisterInstanceId(createdInstance.InstanceId);
            //    }
            //}
        }
    }
}
