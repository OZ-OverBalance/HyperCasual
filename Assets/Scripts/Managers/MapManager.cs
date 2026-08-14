using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RoundMapSetupResult
{
    public List<string> PlayerMapIds = new List<string>();
    public List<string> PresetMapIds = new List<string>();
}

public class MapManager : SingletonBase<MapManager>
{
    [SerializeField] private Vector3 firstMapSpawnPosition = Vector3.zero;

    private List<BaseMap> activeMaps = new List<BaseMap>();
    public int MapCount => activeMaps.Count;
    public Vector3 CurrentSpawnPosition;

    protected override void Awake()
    {
        base.Awake();
        GameDataManager.Inst.LoadData<MapData>();
        GameDataManager.Inst.LoadData<SegmentData>();
    }

    // 플레이어 맵 제공
    public RoundMapSetupResult ProvideMapIdsForRound(int playerCount)
    {
        RoundMapSetupResult result = new RoundMapSetupResult();
        const int targetMapCount = 5;

        var allMapData = GameDataManager.Inst.GetAllData<MapData>();

        if (allMapData == null || allMapData.Count == 0)
        {
            return result;
        }

        List<string> presetPool = new List<string>();
        foreach (var mapData in allMapData)
        {
            if (mapData.Id.StartsWith("Map_Preset"))
            {
                presetPool.Add(mapData.Id);
            }
        }

        int actualPlayerCount = Mathf.Min(playerCount, targetMapCount);
        for (int i = 0; i < actualPlayerCount; i++)
        {
            result.PlayerMapIds.Add("Map_Basic_01");
        }

        int missingCount = targetMapCount - actualPlayerCount;
        if (missingCount > 0 && presetPool.Count > 0)
        {
            ShuffleList(presetPool);

            for (int i = 0; i < missingCount; i++)
            {
                string presetId = presetPool[i % presetPool.Count];
                result.PresetMapIds.Add(presetId);
            }
        }

        return result;
    }

    // 개인 맵 생성
    public async UniTask<BaseMap> SpawnSingleEditMap(string mapId, Vector3 spawnPos)
    {
        var mapData = GameDataManager.Inst.GetData<MapData>(mapId);
        if (mapData == null)
        {
            return null;
        }

        GameObject mapPrefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(mapData.PrefabPath);
        if (mapPrefab == null)
        {
            return null;
        }

        var mapObj = Instantiate(mapPrefab, spawnPos, Quaternion.identity, transform);
        return mapObj.GetComponent<BaseMap>();
    }

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

            GameObject mapPrefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(mapData.PrefabPath);
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

        await SpawnPortalOnLastMapAsync();
    }

    private async UniTask SpawnPortalOnLastMapAsync()
    {
        if (activeMaps.Count == 0) return;

        BaseMap lastMap = activeMaps[activeMaps.Count - 1];
        Vector3 portalSpawnPos = lastMap.ArrivePosition + new Vector3(0f, 1.0f, 0f);

        var portalData = GameDataManager.Inst.GetData<MapData>("Map_Portal_01");
        if (portalData == null)
        {
            return;
        }

        GameObject portalPrefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(portalData.PrefabPath);
        if (portalPrefab == null)
        {
            return;
        }

        var objectManager = GameManager.Inst.GameObjectManager;
        if (objectManager.TryCreateObject(portalPrefab, portalSpawnPos, Quaternion.identity, lastMap.transform, out GameObjectInstance portalInstance))
        {
            portalInstance.gameObject.name = portalData.Id;
            lastMap.RegisterInstanceId(portalInstance.InstanceId);
        }
    }

    // 최종 맵, 장애물 복구
    public async UniTask ImportFullLevelDataAsync(FullLevelData fullData)
    {
        if (fullData == null || fullData.allMapData == null) return;

        List<string> mapIds = new List<string>();
        foreach (var mapData in fullData.allMapData)
        {
            mapIds.Add(mapData.mapId);
        }

        await BuildLevelFromMapId(mapIds);

        var objectManager = GameManager.Inst.GameObjectManager;

        for (int i = 0; i < fullData.allMapData.Count; i++)
        {
            if (i < activeMaps.Count)
            {
                var targetCraftData = fullData.allMapData[i];

                if (targetCraftData.placedSegements != null && targetCraftData.placedSegements.Count > 0)
                {
                    await activeMaps[i].LoadPlacedData(targetCraftData.placedSegements, objectManager);
                }
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

    public FullLevelData ExportFullLevelData(List<string> currentMapIds)
    {
        FullLevelData fullData = new FullLevelData();
        var objectManager = GameManager.Inst.GameObjectManager;

        for (int i = 0; i < activeMaps.Count; i++)
        {
            CraftMapData craftMapData = new CraftMapData()
            {
                mapId = currentMapIds[i],
                placedSegements = activeMaps[i].GetPlacedDataList(objectManager)
            };

            fullData.allMapData.Add(craftMapData);
        }

        return fullData;
    }

    public void SetRespawnPosition(Vector3 newPosition)
    {
        CurrentSpawnPosition = newPosition;
    }
}
