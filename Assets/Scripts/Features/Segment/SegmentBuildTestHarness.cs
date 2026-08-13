using System.Collections.Generic;
using UnityEngine;

// UI로 확인할 방법이 없어서 테스트용으로 만든 임시 스크립트 
public class SegmentBuildTestHarness : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;

    [Header("테스트용 아이템 (1/2/3키에 대응)")]
    [SerializeField] private List<PlaceableObjectData> Catalog_TestItems;

    [Header("테스트용 인벤토리 수량")]
    [SerializeField] private int TestItemCount = 99;

    private readonly List<int> _placedInstanceIds = new();

    private void Start()
    {
        GrantTestInventory();
        Manager_Segment.OnObjectPlaced += HandleObjectPlaced;
        Manager_Segment.OnObjectRemoved += HandleObjectRemoved;
    }

    private void OnDisable()
    {
        if (Manager_Segment != null)
        {
            Manager_Segment.OnObjectPlaced -= HandleObjectPlaced;
            Manager_Segment.OnObjectRemoved -= HandleObjectRemoved;
        }
    }

    private void Update()
    {
        CheckSelectionInput();
        CheckDeleteInput();
        CheckCompleteInput();
    }

    private void GrantTestInventory()
    {
        var slots = new List<InventorySlot>();

        for (int i = 0; i < Catalog_TestItems.Count; i++)
        {
            slots.Add(new InventorySlot
            {
                Data = Catalog_TestItems[i],
                RemainingCount = TestItemCount
            });
        }

        Manager_Segment.StartNewRound(1, slots);
    }

    private void CheckSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySelectByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TrySelectByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TrySelectByIndex(2);
        }
    }

    private void TrySelectByIndex(int index)
    {
        if (index < 0 || index >= Catalog_TestItems.Count) return;

        Manager_Segment.SelectItem(Catalog_TestItems[index]);
        Debug.Log("[TestHarness] 선택됨: " + Catalog_TestItems[index].Id);
    }

    private void CheckDeleteInput()
    {
        if (!Input.GetKeyDown(KeyCode.Delete)) return;
        if (_placedInstanceIds.Count == 0) return;
        int lastIndex = _placedInstanceIds.Count - 1;
        Manager_Segment.RemoveObject(_placedInstanceIds[lastIndex]);
    }

    private void CheckCompleteInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Manager_Segment.ToggleBuildComplete();
            Debug.Log("[TestHarness] 빌드 상태 전환" + Manager_Segment.IsBuildLocked);
        }
    }

    private void HandleObjectPlaced(PlacedObjectData placed)
    {
        _placedInstanceIds.Add(placed.InstanceId);
        Debug.Log("[TestHarness] 배치됨: " + placed.InstanceId + " at " + placed.GridPos);
    }

    private void HandleObjectRemoved(int instanceId)
    {
        _placedInstanceIds.Remove(instanceId);
        Debug.Log("[TestHarness] 삭제됨: " + instanceId);
    }
}