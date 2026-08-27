using System;
using System.Collections.Generic;
using Unity.Netcode;
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
    public ulong OwnerClientId;
}

//이번 라운드에 플레이어가 보유한 아이템 슬롯 하나.
[Serializable]
public class InventorySlot
{
    public PlaceableObjectData Data;
    public int RemainingCount;

    public bool IsAvailable
    {
        get
        {
            return Data != null && RemainingCount > 0;
        }
    }
}


// 라운드마다 새로 부여되는 플레이어 인벤토리.
public class PlayerInventory
{
    private readonly List<InventorySlot> _slots = new();

    public IReadOnlyList<InventorySlot> Slots
    {
        get
        {
            return _slots;
        }
    }

    public void SetSlots(List<InventorySlot> slots)
    {
        _slots.Clear();

        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot == null || slot.Data == null)
            {
                continue;
            }

            _slots.Add(slot);
        }
    }

    public bool CanPlace(PlaceableObjectData data)
    {
        var slot = FindSlot(data);
        return slot != null && slot.IsAvailable;
    }

    public bool ConsumeItem(PlaceableObjectData data)
    {
        var slot = FindSlot(data);
        if (slot == null || slot.RemainingCount <= 0)
        {
            return false;
        }

        slot.RemainingCount--;
        return true;
    }

    public bool RefundItem(PlaceableObjectData data)
    {
        var slot = FindSlot(data);
        if (slot == null)
        {
            return false;
        }

        slot.RemainingCount++;
        return true;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count)
        {
            return null;
        }

        return _slots[index];
    }

    public int GetSlotIndex(PlaceableObjectData data)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Data == data)
            {
                return i;
            }
        }

        return -1;
    }

    private InventorySlot FindSlot(PlaceableObjectData data)
    {
        int slotIndex = GetSlotIndex(data);

        return GetSlot(slotIndex);
    }
}

[Serializable]
public struct NetworkPlacedObjectData : INetworkSerializable
{
    public int InstanceId;       // 서버가 발급해 채워줄 ID, 로컬에서 지정한 instanceid는 덮어씌워짐
    public string Id;         
    public Vector2Int GridPos;   
    public int RotationStep;    
    public int RoundPlaced;
    public ulong OwnerClientId;

    public NetworkPlacedObjectData(PlacedObjectData data)
    {
        InstanceId = data.InstanceId;
        Id = data.Id;   
        GridPos = data.GridPos;
        RotationStep = data.RotationStep;
        RoundPlaced = data.RoundPlaced;
        OwnerClientId = data.OwnerClientId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref InstanceId);
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref GridPos);
        serializer.SerializeValue(ref RotationStep);
        serializer.SerializeValue(ref RoundPlaced);
        serializer.SerializeValue(ref OwnerClientId);
    }
}