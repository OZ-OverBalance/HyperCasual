using System.Collections.Generic;
using UnityEngine;

public class MapManager : SingletonBase<MapManager>
{
    [SerializeField] private List<GameObject> Prefab_baseMap = new List<GameObject>();

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

        if (Prefab_baseMap == null || Prefab_baseMap.Count == 0)
        {
            return;
        }

        List<GameObject> pool = new List<GameObject>(Prefab_baseMap);
        ShuffleList(pool);

        Vector3 nextSpawnPos = firstMapSpawnPosition;

        for (int i = 0; i < mapCount; i++)
        {
            GameObject selectedPrefab = pool[i];

            GameObject mapObj = Instantiate(selectedPrefab, nextSpawnPos, Quaternion.identity, transform);
            BaseMap baseMap = mapObj.GetComponent<BaseMap>();

            if (baseMap != null)
            {
                activeMaps.Add(baseMap);

                float mapWidth = baseMap.ArrivePosition.x - baseMap.StartPosition.x;
                nextSpawnPos += new Vector3(mapWidth + mapDistanceOffset, 0, 0);
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
