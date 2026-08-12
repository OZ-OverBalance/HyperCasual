using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class RoundMapSetupResult
{
    public List<string> PlayerMapIds = new List<string>();
    public List<string> PresetMapIds = new List<string>();
    public List<string> FullRoundMapIds = new List<string>();
}

public class MapManager : SingletonBase<MapManager>
{
    [SerializeField] private Vector3 firstMapSpawnPosition = Vector3.zero;
    [SerializeField] private float mapDistanceOffset = 34.0f;

    private List<BaseMap> activeMaps = new List<BaseMap>();
    public int MapCount => activeMaps.Count;
    public Vector3 CurrentSpawnPosition;

    protected override void Awake()
    {
        base.Awake();
    }

    // 플레이어 맵 제공
    public RoundMapSetupResult ProvideMapIdsForRound(int playerCount)
    {
        RoundMapSetupResult result = new RoundMapSetupResult();
        const int targetMapCount = 5;

        var allBaseMapIds = GameDataManager.Inst.GetAllDataId<MapData>();

        if (allBaseMapIds == null || allBaseMapIds.Count == 0)
        {
            return result;
        }

        List<string> pool = new List<string>(allBaseMapIds);
        ShuffleList(pool);

        int actualPlayerCount = Mathf.Min(playerCount, targetMapCount);
        for (int i = 0; i < actualPlayerCount; i++)
        {
            string selectedId = pool[i % pool.Count];
            result.PlayerMapIds.Add(selectedId);
        }

        int missingCount = targetMapCount - actualPlayerCount;
        if (missingCount > 0)
        {
            List<string> presetPool = new List<string>(allBaseMapIds);
            ShuffleList(presetPool);

            for (int i = 0; i < missingCount; i++)
            {
                string presetId = presetPool[i % presetPool.Count];
                result.PresetMapIds.Add(presetId);
            }
        }

        result.FullRoundMapIds.AddRange(result.PlayerMapIds);
        result.FullRoundMapIds.AddRange(result.PresetMapIds);
        ShuffleList(result.FullRoundMapIds);

        return result;
    }

    // 최종 맵 생성
    public async UniTask BuildLevelFromMapId(List<string> mapIds)
    {
        ClearAllMaps();

        if (mapIds == null || mapIds.Count == 0)
        {
            return;
        }

        Vector3 targetConnectWorldPosition = firstMapSpawnPosition;

        for (int i = 0; i < mapIds.Count; i++)
        {
            string mapId = mapIds[i];

            MapData mapData = GameDataManager.Inst.GetData<MapData>(mapId);
            if (mapData == null)
            {
                continue;
            }

            GameObject mapPrefab = await ResourceManager.Inst.LoadAsset<GameObject>(mapData.PrefabPath);
            if (mapPrefab == null)
            {
                continue;
            }

            GameObject mapObj = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity, transform);
            BaseMap baseMap = mapObj.GetComponent<BaseMap>();

            if (baseMap != null)
            {
                Vector3 localStartOffset = baseMap.StartPosition - mapObj.transform.position;
                mapObj.transform.position = targetConnectWorldPosition - localStartOffset;

                targetConnectWorldPosition = baseMap.ArrivePosition + new Vector3(1.0f, 0f, 0f);

                activeMaps.Add(baseMap);
            }
        }

        CurrentSpawnPosition = GetGlobalStartPosition();
    }

    // 장애물 복구
    public async UniTask ImportFullLevelDataAsync(FullLevelData fullData)
    {
        if (fullData == null || fullData.allMapData == null) return;

        List<string> mapIds = new List<string>();
        foreach (var mapData in fullData.allMapData)
        {
            mapIds.Add(mapData.mapIndex.ToString());
        }

        await BuildLevelFromMapId(mapIds);

        var objectManager = GameManager.Inst.GameObjectManager;

        for (int i = 0; i < fullData.allMapData.Count; i++)
        {
            if (i < activeMaps.Count)
            {
                await activeMaps[i].LoadPlacedData(fullData.allMapData[i].placedSegements, objectManager);
            }
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

    public void SetRespawnPosition(Vector3 newPosition)
    {
        CurrentSpawnPosition = newPosition;
    }
}
