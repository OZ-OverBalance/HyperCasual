using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class BuildInventoryView : UIBase
{
    [SerializeField] private List<BuildInventorySlot> List_InventorySlots;
    [SerializeField] private UIButton Button_Undo;
    [SerializeField] private TMP_Text Text_PlacementProgress;
    [SerializeField] private UIButton Button_Submit;

    private SegmentBuildManager _buildManager;

    public override UILayer Layer => UILayer.Content;

    protected override bool ValidateReferences()
    {
        if (!base.ValidateReferences())
        {
            return false;
        }

        if (List_InventorySlots == null || List_InventorySlots.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            if (List_InventorySlots[i] == null)
            {
                return false;
            }
        }

        if (Button_Undo == null)
        {
            return false;
        }

        if (Text_PlacementProgress == null || Button_Submit == null)
        {
            return false;
        }

        return true;
    }

    protected override void InitializeUI()
    {
        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            List_InventorySlots[i].Initialize(i);
        }
    }

    protected override void BindEvents()
    {
        BindSlotEvents();
        BindManagerEvents();

        Button_Undo.BindOnClickButtonEvent(HandleClickUndoButton);
        Button_Submit.BindOnClickButtonEvent(HandleClickSubmitButton);
    }

    protected override void UnbindEvents()
    {
        UnbindSlotEvents();
        UnbindManagerEvents();

        Button_Undo.UnbindOnClickButtonEvent(HandleClickUndoButton);
        Button_Submit.UnbindOnClickButtonEvent(HandleClickSubmitButton);
    }

    protected override void RefreshUI()
    {
        RefreshInventorySlots();
        RefreshSelectedSlot(_buildManager != null ? _buildManager.SelectedSlotIndex : -1);
        RefreshUndoButton();
        RefreshBuildStatus();
    }

    protected override void ReleaseUI()
    {
        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            List_InventorySlots[i].Release();
        }

        _buildManager = null;
    }

    public void SetBuildManager(SegmentBuildManager buildManager)
    {
        if (_buildManager == buildManager)
        {
            RefreshInventorySlots();
            return;
        }

        if (IsOpened)
        {
            UnbindManagerEvents();
        }

        _buildManager = buildManager;

        if (IsOpened)
        {
            BindManagerEvents();
        }

        RefreshInventorySlots();
        RefreshSelectedSlot(_buildManager != null ? _buildManager.SelectedSlotIndex : -1);
    }

    private void BindSlotEvents()
    {
        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            List_InventorySlots[i].OnSlotClicked -= HandleSlotClicked;
            List_InventorySlots[i].OnSlotClicked += HandleSlotClicked;
        }
    }

    private void UnbindSlotEvents()
    {
        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            List_InventorySlots[i].OnSlotClicked -= HandleSlotClicked;
        }
    }

    private void BindManagerEvents()
    {
        if (_buildManager == null)
        {
            return;
        }

        _buildManager.OnInventoryChanged -= HandleInventoryChanged;
        _buildManager.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        _buildManager.OnBuildCompleted -= HandleBuildStateChanged;
        _buildManager.OnBuildResumed -= HandleBuildStateChanged;

        _buildManager.OnInventoryChanged += HandleInventoryChanged;
        _buildManager.OnSelectedSlotChanged += HandleSelectedSlotChanged;
        _buildManager.OnBuildCompleted += HandleBuildStateChanged;
        _buildManager.OnBuildResumed += HandleBuildStateChanged;
    }

    private void UnbindManagerEvents()
    {
        if (_buildManager == null)
        {
            return;
        }

        _buildManager.OnInventoryChanged -= HandleInventoryChanged;
        _buildManager.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        _buildManager.OnBuildCompleted -= HandleBuildStateChanged;
        _buildManager.OnBuildResumed -= HandleBuildStateChanged;
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (_buildManager == null)
        {
            return;
        }

        _buildManager.SelectItemBySlotIndex(slotIndex);
    }

    private void HandleInventoryChanged()
    {
        RefreshInventorySlots();
        RefreshUndoButton();
        RefreshBuildStatus();
    }

    private void HandleSelectedSlotChanged(int slotIndex)
    {
        RefreshSelectedSlot(slotIndex);
    }

    private void HandleClickUndoButton()
    {
        if (_buildManager == null)
        {
            return;
        }

        _buildManager.TryRemoveLastPlacedObjectAndRefund();

        RefreshUndoButton();
    }

    private void HandleClickSubmitButton()
    {
        if (_buildManager == null || !_buildManager.CanCompleteBuild)
        {
            return;
        }

        _buildManager.CompleteBuild();
    }

    private void HandleBuildStateChanged()
    {
        RefreshBuildStatus();
        RefreshUndoButton();
    }

    private void RefreshUndoButton()
    {
        bool canUndo = _buildManager != null && !_buildManager.IsBuildLocked && _buildManager.GetPlacedObjects().Count > 0;
        Button_Undo.SetInteractable(canUndo);
    }

    private void RefreshInventorySlots()
    {
        IReadOnlyList<InventorySlot> inventorySlots = _buildManager != null ? _buildManager.InventorySlots : null;

        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            InventorySlot inventorySlot = null;

            if (inventorySlots != null && i < inventorySlots.Count)
            {
                inventorySlot = inventorySlots[i];
            }

            List_InventorySlots[i].Refresh(inventorySlot);
        }
    }

    private void RefreshSelectedSlot(int selectedSlotIndex)
    {
        for (int i = 0; i < List_InventorySlots.Count; i++)
        {
            List_InventorySlots[i].SetSelected(i == selectedSlotIndex);
        }
    }

    private void RefreshBuildStatus()
    {
        if (_buildManager == null)
        {
            Text_PlacementProgress.text = "설치 0/0";
            Button_Submit.SetInteractable(false);
            return;
        }

        int placedCount = _buildManager.PlacedItemCount;
        int requiredCount = _buildManager.RequiredPlacementCount;

        Text_PlacementProgress.text = $"설치 {placedCount}/{requiredCount}";

        bool canSubmit = !_buildManager.IsBuildLocked && _buildManager.CanCompleteBuild;

        Button_Submit.SetInteractable(canSubmit);
    }
}