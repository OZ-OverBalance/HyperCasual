using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
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

    public void Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            ClearPresetStaticObjects();
        }
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
        if (dataList == null || dataList.Count == 0) return;

        SetSegmentSpawner();
        if (Spawner_Segment != null)
        {
            _currentBuildManager = await Spawner_Segment.ShowBuildPhaseAsync(roundIndex);
            if (_currentBuildManager != null)
            {
                _currentBuildManager.CompleteBuild();
                await _currentBuildManager.LoadExistingPlacedDataForNetworkAsync(dataList);
                return;
            }
        }

        if (!NetworkManager.Singleton.IsServer) return;

        foreach (var data in dataList)
        {
            var segmentData = GameDataManager.Inst.GetData<SegmentData>(data.Id);
            if (segmentData == null)
            {
                Debug.LogError($"[BaseMap] SegmentData 누락: {data.Id}");
                continue;
            }

            GameObject prefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(segmentData.PrefabPath);
            if (prefab == null) continue;

            Vector3 localOffset = new Vector3(data.GridPos.x / 100f, data.GridPos.y / 100f, 0f);
            Vector3 spawnWorldPos = this.transform.TransformPoint(localOffset);
            Quaternion rotation = this.transform.rotation * Quaternion.Euler(0f, 0f, data.RotationStep * 90f);

            GameObject spawnedObj = Instantiate(prefab, spawnWorldPos, rotation, this.transform);

            if (spawnedObj.TryGetComponent<GameObjectInstance>(out var instance))
            {
                instance.SetOwnerClientId(data.OwnerClientId);
                if (data.InstanceId > 0)
                {
                    instance.TryInitializeInstance(data.InstanceId);
                }
            }

            if (spawnedObj.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (!netObj.IsSpawned)
                {
                    netObj.Spawn();
                }
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


            Vector3 localPos = this.transform.InverseTransformPoint(inst.transform.position);
            Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(localPos.x * 100f),
            Mathf.RoundToInt(localPos.y * 100f)
            );

            int rotStep = Mathf.RoundToInt((inst.transform.eulerAngles.z - this.transform.eulerAngles.z) / 90f) % 4;
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
        for (int i = instances.Length - 1; i >= 0; i--)
        {
            var inst = instances[i];
            if (inst == null || inst.gameObject == this.gameObject) continue;

            string objName = inst.gameObject.name;

            if (inst.gameObject.CompareTag(Tag_FixedObstruction) ||
                objName.Contains("DeadZone") ||
                objName.Contains("StartPoint") ||
                objName.Contains("ArrivePoint") ||
                objName.Contains("SpawnPoint") ||
                objName.Contains("Centor") ||
                objName.Contains("dirt_with_grass") ||
                objName.Contains("flag_A_red"))
            {
                continue;
            }

            if (inst.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                continue;
            }

            Destroy(inst.gameObject);
        }

        var obstacles = GetComponentsInChildren<ObstacleBase>(true);
        for (int i = obstacles.Length - 1; i >= 0; i--)
        {
            var obs = obstacles[i];
            if (obs == null || obs.gameObject == this.gameObject) continue;

            if (obs.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                continue;
            }

            Destroy(obs.gameObject);
        }
    }

    public Vector2Int LocalToCell2D(Vector3 localPosition)
    {
        Vector3 cellSize = Grid != null ? Grid.cellSize : Vector3.one;

        return new Vector2Int(
            Mathf.FloorToInt(localPosition.x / cellSize.x),
            Mathf.FloorToInt(localPosition.y / cellSize.y)
        );
    }
}
