using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BaseMap : MonoBehaviour
{
    [Header("타일맵")]
    [SerializeField] private Grid Grid;
    [SerializeField] private Tilemap Tilemap_Ground;
    [SerializeField] private Tilemap Tilemap_CraftArea;

    [Header("포인트 좌표")]
    [SerializeField] private Transform Transform_startPoint;
    [SerializeField] private Transform Transform_arrivePoint;
    [SerializeField] private Transform Transform_spawnPoint;

    private List<GameObject> placedSegment = new List<GameObject>();
    public Vector3 StartPosition => Transform_startPoint.position;
    public Vector3 ArrivePosition => Transform_arrivePoint.position;

    private void Start()
    {
        if (Tilemap_CraftArea != null)
        {
            TilemapRenderer renderer = Tilemap_CraftArea.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return Grid.WorldToCell(worldPosition);
    }

    public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
    {
        return Grid.GetCellCenterWorld(cellPosition);
    }

    public bool CanBuild(Vector3 worldPosition)
    {
        Vector3Int cellPos = WorldToCell(worldPosition);

        if (Tilemap_Ground != null && Tilemap_Ground.HasTile(cellPos))
        {
            return false;
        }

        if (Tilemap_CraftArea != null && !Tilemap_CraftArea.HasTile(cellPos))
        {
            return false;
        }

        foreach (var obj in placedSegment)
        {
            if ( obj != null && WorldToCell(obj.transform.position) == cellPos)
            {
                return false;
            }
        }

        return true;
    }

    public void RegisterObject(GameObject obj)
    {
        placedSegment.Add(obj);
    }

    public void ClearAllPlacedObjects()
    {
        foreach (var obj in placedSegment)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        placedSegment.Clear();
    }

    public void SetCraftAreaVisibility(bool isVisible)
    {
        if (Tilemap_CraftArea != null)
        {
            var renderer = Tilemap_CraftArea.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = isVisible;
            }
        }
    }
}
