using System;
using System.Collections.Generic;
using UnityEngine;


// 실제로 그리드에 배치된 오브젝트 하나를 나타내는 데이터.
[Serializable]
public class PlacedObjectData
{
    public int InstanceId;
    public string Id;
    public Vector2Int GridPos;
    public int RotationStep;
    public int RoundPlaced;
}

//이번 라운드에 플레이어가 보유한 아이템 슬롯 하나.
[Serializable]
public class InventorySlot
{
    public PlaceableObjectData Data;
    public int RemainingCount;
}


// 라운드마다 새로 부여되는 플레이어 인벤토리.
public class PlayerInventory
{
    public List<InventorySlot> Slots = new();

    public bool CanPlace(PlaceableObjectData data)
    {
        var slot = FindSlot(data);
        return slot != null && slot.RemainingCount > 0;
    }

    public void ConsumeItem(PlaceableObjectData data)
    {
        var slot = FindSlot(data);
        if (slot != null) slot.RemainingCount--;
    }

    private InventorySlot FindSlot(PlaceableObjectData data)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].Data == data) return Slots[i];
        }
        return null;
    }
}