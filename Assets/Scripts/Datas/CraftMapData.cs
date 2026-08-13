using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedSegmentData
{
    public string segmentId;
    public Vector3Int cellPosition;
    public int rotationStep;
}

[System.Serializable]
public class CraftMapData
{
    public string mapId;
    public List<PlacedSegmentData> placedSegements = new List<PlacedSegmentData>();
}

[System.Serializable]
public class FullLevelData
{
    public List<CraftMapData> allMapData = new List<CraftMapData>();
}
