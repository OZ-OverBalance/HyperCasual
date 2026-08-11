using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedSegementData
{
    public int placedSegementId;
    public Vector3Int cellPosition;
}

[System.Serializable]
public class MapData
{
    public int mapIndex;
    public List<PlacedSegementData> placedSegements = new List<PlacedSegementData>();
}

[System.Serializable]
public class FullLevelData
{
    public List<MapData> allMapData = new List<MapData>();
}
