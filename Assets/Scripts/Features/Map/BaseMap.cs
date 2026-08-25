using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BaseMap : MonoBehaviour
{
    [SerializeField] private SegmentSpawner Spawner_Segment;

    [Header("타일맵")]
    [SerializeField] private Grid Grid;

    [Header("포인트 좌표")]
    [SerializeField] private Transform Transform_startPoint;
    [SerializeField] private Transform Transform_arrivePoint;
    [SerializeField] private Transform Transform_spawnPoint;
    [SerializeField] private Transform Transform_centorPoint;

    [Header("가림막")]
    [SerializeField] private GameObject GameObject_Cover;

    private List<int> placedSegmentInstanceId = new List<int>();
    private SegmentBuildManager _currentBuildManager;

    public Vector3 StartPosition => Transform_startPoint.position;
    public Vector3 ArrivePosition => Transform_arrivePoint.position;
    public Transform CentorPoint => Transform_centorPoint;
    public SegmentSpawner SegmentSpawner => Spawner_Segment;
    public SegmentBuildManager CurrentBuildManager => _currentBuildManager;

    private void Awake()
    {
        if (GameObject_Cover != null)
        {
            GameObject_Cover.SetActive(false);
        }
    }

    public void SetCover(bool isVisible)
    {
        if (GameObject_Cover == null)
        {
            return;
        }

        GameObject_Cover.SetActive(isVisible);
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

        int currentRound = GameManager.Inst.RoundManager.CurrentRound;

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
                    RoundPlaced = currentRound
                });
            }
        }

        return dataList;
    }

    public async UniTask LoadPlacedData(List<PlacedObjectData> dataList, GameObjectManager objectManager, int roundIndex)
    {
        if (Spawner_Segment == null)
        {
            Debug.LogWarning("[BaseMap] Spqwner_Segment 없음");
            return;
        }

        _currentBuildManager = await Spawner_Segment.ShowBuildPhaseAsync(roundIndex);

        if (_currentBuildManager != null)
        {
            _currentBuildManager.CompleteBuild();

            if (dataList != null && dataList.Count > 0)
            {
                await _currentBuildManager.LoadExistingPlacedDataAsync(dataList);
            } 
        }
    }

    public async UniTask LoadPlacedDataForNetwork(List<PlacedObjectData> dataList, GameObjectManager objectManager, int roundIndex)
    {
        if (Spawner_Segment == null)
        {
            Debug.LogWarning("[BaseMap] Spqwner_Segment 없음");
            return;
        }

        _currentBuildManager = await Spawner_Segment.ShowBuildPhaseAsync(roundIndex);

        if (_currentBuildManager != null)
        {
            _currentBuildManager.CompleteBuild();

            if (dataList != null && dataList.Count > 0)
            {
                await _currentBuildManager.LoadExistingPlacedDataForNetworkAsync(dataList);
            }
        }
    }
}
