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

    [Header("배치 차단용 - 기본맵 고정 오브젝트 태그")]
    [SerializeField] private string Tag_FixedObstruction = "MapFixture";

    private List<int> placedSegmentInstanceId = new List<int>();
    private SegmentBuildManager _currentBuildManager;

    public Vector3 StartPosition => Transform_startPoint.position;
    public Vector3 ArrivePosition => Transform_arrivePoint.position;
    public Transform CentorPoint => Transform_centorPoint;
    public SegmentSpawner SegmentSpawner
    {
        get
        {
            if (Spawner_Segment == null)
            {
                SetSegmentSpawner();
            }
            return Spawner_Segment;
        }
    }
    public SegmentBuildManager CurrentBuildManager => _currentBuildManager;

    private void Awake()
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
        await UniTask.WaitUntil(() => SetSegmentSpawner() == true);

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


        await UniTask.WaitUntil(() => SetSegmentSpawner() == true);

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

    public bool SetSegmentSpawner()
    {

        if (Spawner_Segment != null)
        {
            return true;
        }

        SegmentSpawner spn = GetComponentInChildren<SegmentSpawner>(true);

        if (spn != null)
        {
            Spawner_Segment = spn;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool HasFixedObstructionAt(Vector3 worldPosition, Vector3 cellSize)
    {
        Collider[] overlaps = Physics.OverlapBox(worldPosition, cellSize * 0.4f, Quaternion.identity);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Transform current = overlaps[i].transform;

            while (current != null)
            {
                if (current.CompareTag(Tag_FixedObstruction))
                {
                    return true;
                }

                current = current.parent;
            }
        }

        return false;
    }

    public List<PlacedObjectData> ExtractPresetInitialObjects()
    {
        List<PlacedObjectData> presetObjects = new List<PlacedObjectData>();
        var instances = GetComponentsInChildren<GameObjectInstance>(true);
        int tempInstanceId = 10000;

        foreach (var inst in instances)
        {
            if (inst.gameObject == this.gameObject) continue;

            string targetId = !string.IsNullOrEmpty(inst.SegmentId)
                ? inst.SegmentId
                : inst.gameObject.name.Replace("(Clone)", "").Trim();

            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(inst.transform.localPosition.x),
                Mathf.RoundToInt(inst.transform.localPosition.y)
            );

            int rotStep = Mathf.RoundToInt(inst.transform.localEulerAngles.z / 90f) % 4;
            if (rotStep < 0) rotStep += 4;

            presetObjects.Add(new PlacedObjectData
            {
                InstanceId = tempInstanceId++,
                Id = targetId,
                GridPos = gridPos,
                RotationStep = rotStep,
                RoundPlaced = 1,
                OwnerClientId = ulong.MaxValue
            });
        }

        Debug.Log($"[BaseMap] {gameObject.name}에서 프리셋 장애물 {presetObjects.Count}개 추출 완료");
        return presetObjects;
    }

    public void ClearPresetStaticObjects()
    {
        var instances = GetComponentsInChildren<GameObjectInstance>(true);
        foreach (var inst in instances)
        {
            if (inst.gameObject == this.gameObject) continue;
            Destroy(inst.gameObject);
        }
    }
}
