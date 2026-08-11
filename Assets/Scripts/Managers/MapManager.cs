using System.Collections.Generic;
using UnityEngine;

public class MapManager : SingletonBase<MapManager>
{
    [SerializeField] private List<GameObject> Prefab_baseMap = new List<GameObject>();
    [SerializeField] private List<GameObject> Prefab_craftable = new List<GameObject>();

    [SerializeField] private Vector3 firstMapSpawnPosition = Vector3.zero;
    [SerializeField] private float mapDistanceOffset = 34.0f;

    private List<BaseMap> activeMaps = new List<BaseMap>();
    public int MapCount => activeMaps.Count;
    public Vector3 CurrentSpawnPosition;

    protected override void Awake()
    {
        base.Awake();
    }

    // 맵 랜덤 배치용, 플레이어 수 필요
    public List<int> GenerateRandomMapIndices(int mapCount)
    {
        if (Prefab_baseMap ==null || Prefab_baseMap.Count < mapCount)
        {
            return null;
        }

        List<int> indexPool = new List<int>();
        for (int i = 0; i < Prefab_baseMap.Count; i++)
        {
            indexPool.Add(i);
        }

        ShuffleList(indexPool);

        return indexPool.GetRange(0, mapCount);
    }

    // 최종 맵 생성 멀티용
    public void BuildLevelFromIndices(List<int> mapIndices)
    {
        ClearAllMaps();

        if (mapIndices == null || mapIndices.Count == 0)
        {
            return;
        }

        Vector3 nextSpawnPos = firstMapSpawnPosition;

        for (int i = 0; i < mapIndices.Count; i++)
        {
            int prefabIndex = mapIndices[i];

            if (prefabIndex < 0 || prefabIndex >= Prefab_baseMap.Count)
            {
                continue;
            }

            GameObject selectedPrefab = Prefab_baseMap[prefabIndex];

            GameObject mapObj = Instantiate(selectedPrefab, nextSpawnPos, Quaternion.identity, transform);
            BaseMap baseMap = mapObj.GetComponent<BaseMap>();

            if (baseMap != null)
            {
                activeMaps.Add(baseMap);

                float mapWidth = baseMap.ArrivePosition.x - baseMap.StartPosition.x;
                nextSpawnPos += new Vector3(mapWidth + mapDistanceOffset, 0, 0);
            }
        }

        CurrentSpawnPosition = GetGlobalStartPosition();
    }

    // 최종 맵 생성 로컬용
    public void GenerateFullLevel(int mapCount)
    {
        List<int> randomIndices = GenerateRandomMapIndices(mapCount);
        if (randomIndices != null)
        {
            BuildLevelFromIndices(randomIndices);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public BaseMap GetMap(int index)
    {
        if (index >= 0 && index < activeMaps.Count)
        {
            return activeMaps[index];
        }
        return null;
    }

    public Vector3 GetGlobalStartPosition()
    {
        if (activeMaps.Count > 0 && activeMaps[0] != null)
        {
            return activeMaps[0].StartPosition;
        }
        return Vector3.zero;
    }

    public void ClearAllMaps()
    {
        var objectManager = GameManager.Inst.GameObjectManager;

        foreach (var map in activeMaps)
        {
            if (map != null)
            {
                if (objectManager != null)
                {
                    map.ClearAllPlacedObjects(objectManager);
                }
                Destroy(map.gameObject);
            }
        }
        activeMaps.Clear();
    }

    public FullLevelData ExportFullLevelData(List<int> currentMapIndices)
    {
        FullLevelData fullData = new FullLevelData();
        var objectManager = GameManager.Inst.GameObjectManager;

        for (int i = 0; i < activeMaps.Count; i++)
        {
            CraftMapData craftMapData = new CraftMapData();
            craftMapData.mapIndex = currentMapIndices[i];
            craftMapData.placedSegements = activeMaps[i].GetPlacedDataList(objectManager);

            fullData.allMapData.Add(craftMapData);
        }

        return fullData;
    }

    public void ImportFullLevelData(FullLevelData fullData)
    {
        List<int> mapIndices = new List<int>();
        foreach (var mapData in fullData.allMapData)
        {
            mapIndices.Add(mapData.mapIndex);
        }
        BuildLevelFromIndices(mapIndices);

        var objectManager = GameManager.Inst.GameObjectManager;

        for (int i = 0; i < fullData.allMapData.Count; i++)
        {
            activeMaps[i].LoadPlacedData(fullData.allMapData[i].placedSegements, Prefab_craftable, objectManager);
        }
    }

    public void SetRespawnPosition(Vector3 newPosition)
    {
        CurrentSpawnPosition = newPosition;
    }
}
