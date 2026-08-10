using System.Collections.Generic;
using UnityEngine;

public class MapManager : SingletonBase<MapManager>
{
    [SerializeField] private GameObject Prefab_baseMap;

    [SerializeField] private Vector3 firstMapSpawnPosition = Vector3.zero;
    [SerializeField] private float mapDistanceOffset = 2.0f;

    private List<BaseMap> activeMaps = new List<BaseMap>();
    public int MapCount => activeMaps.Count;

    protected override void Awake()
    {
        base.Awake();
    }

    public void GenerateFullLevel(int mapCount)
    {
        ClearAllMaps();

        Vector3 nextSpawnPos = firstMapSpawnPosition;

        for (int i = 0; i < mapCount; i++)
        {
            GameObject mapObj = Instantiate(Prefab_baseMap, nextSpawnPos, Quaternion.identity, transform);
            BaseMap baseMap = mapObj.GetComponent<BaseMap>();

            if (baseMap != null)
            {
                activeMaps.Add(baseMap);

                float mapWidth = baseMap.ArrivePosition.x - baseMap.StartPosition.x;
                nextSpawnPos += new Vector3(mapWidth + mapDistanceOffset, 0, 0);
            }
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
        foreach (var map in activeMaps)
        {
            if (map != null)
            {
                Destroy(map.gameObject);
            }
        }
        activeMaps.Clear();
    }
}
