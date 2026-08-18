using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftMapData
{
    public string mapId;
    public List<PlacedObjectData> placedSegements = new List<PlacedObjectData>();
}

[System.Serializable]
public class FullLevelData
{
    public List<CraftMapData> allMapData = new List<CraftMapData>();
}
