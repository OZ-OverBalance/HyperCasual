using System.Collections.Generic;
using UnityEngine;

// UI로 확인할 방법이 없어서 테스트용으로 만든 임시 스크립트
public class SegmentBuildTestHarness : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;

    [Header("전체 배치 가능 아이템 풀 - 라운드마다 이 중 N개를 랜덤으로 뽑음")]
    [SerializeField] private List<PlaceableObjectData> Catalog_TestItems;

    [Header("라운드당 랜덤으로 주어질 아이템 개수")]
    [SerializeField] private int ItemsPerRound = 3;

    [Header("테스트용 인벤토리 수량")]
    [SerializeField] private int TestItemCount = 99;

    [Header("입력 키")]
    [SerializeField] private KeyCode Key_CycleSelect = KeyCode.Tab;
    [SerializeField] private KeyCode Key_NewRound = KeyCode.N;
    [SerializeField] private KeyCode Key_DeletePermanent = KeyCode.Delete;
    [SerializeField] private KeyCode Key_DeleteRefund = KeyCode.Backspace;

    private readonly List<int> _placedInstanceIds = new();
    private List<PlaceableObjectData> _currentRoundItems = new();
    private int _currentTestIndex = -1;
    private int _currentRoundIndex = 1;

    private void Start()
    {
        DrawNewRoundItems();
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
        CheckNewRoundInput();
    }

    private void DrawNewRoundItems()
    {
        _currentRoundItems = PickRandomItems(Catalog_TestItems, ItemsPerRound);
        _currentTestIndex = -1;

        var slots = new List<InventorySlot>();
        for (int i = 0; i < _currentRoundItems.Count; i++)
        {
            slots.Add(new InventorySlot
            {
                Data = _currentRoundItems[i],
                RemainingCount = TestItemCount
            });
        }

        Manager_Segment.StartNewRound(_currentRoundIndex, slots);
        LogRoundItems();
    }

    private List<PlaceableObjectData> PickRandomItems(List<PlaceableObjectData> pool, int count)
    {
        var shuffled = new List<PlaceableObjectData>(pool);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            PlaceableObjectData temp = shuffled[i];
            shuffled[i] = shuffled[swapIndex];
            shuffled[swapIndex] = temp;
        }

        int pickCount = Mathf.Min(count, shuffled.Count);
        var result = new List<PlaceableObjectData>();
        for (int i = 0; i < pickCount; i++)
        {
            result.Add(shuffled[i]);
        }
        return result;
    }

    private void LogRoundItems()
    {
        string itemNames = "";
        for (int i = 0; i < _currentRoundItems.Count; i++)
        {
            itemNames += _currentRoundItems[i].Id;
            if (i < _currentRoundItems.Count - 1)
            {
                itemNames += ", ";
            }
        }
        Debug.Log("[TestHarness] 라운드 " + _currentRoundIndex + " 아이템 뽑힘: " + itemNames);
    }

    private void CheckSelectionInput()
    {
        if (!Input.GetKeyDown(Key_CycleSelect)) return;
        if (_currentRoundItems.Count == 0) return;

        _currentTestIndex = (_currentTestIndex + 1) % _currentRoundItems.Count;
        TrySelectByIndex(_currentTestIndex);
    }

    private void TrySelectByIndex(int index)
    {
        if (index < 0 || index >= _currentRoundItems.Count) return;
        Manager_Segment.SelectItem(_currentRoundItems[index]);
        Debug.Log("[TestHarness] 선택됨 (" + (index + 1) + "/" + _currentRoundItems.Count +  "): " + _currentRoundItems[index].Id);
    }

    private void CheckDeleteInput()
    {
        if (_placedInstanceIds.Count == 0) return;
        int lastIndex = _placedInstanceIds.Count - 1;

        if (Input.GetKeyDown(Key_DeletePermanent))
        {
            Manager_Segment.RemoveObject(_placedInstanceIds[lastIndex]);
            Debug.Log("[TestHarness] 완전 삭제됨");
        }
        else if (Input.GetKeyDown(Key_DeleteRefund))
        {
            Manager_Segment.RemoveObjectAndRefund(_placedInstanceIds[lastIndex]);
            Debug.Log("[TestHarness] 환불 삭제됨 (인벤토리 복구)");
        }
    }

    private void CheckCompleteInput()
    {
        if (!Input.GetKeyDown(KeyCode.KeypadEnter)) return;

        Manager_Segment.ToggleBuildComplete();
        Debug.Log("[TestHarness] 빌드 상태 전환, 잠김여부=" + Manager_Segment.IsBuildLocked);
    }

    private void CheckNewRoundInput()
    {
        if (!Input.GetKeyDown(Key_NewRound)) return;

        _currentRoundIndex++;
        DrawNewRoundItems();
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