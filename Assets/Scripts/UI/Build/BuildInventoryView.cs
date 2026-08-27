using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildInventoryView : UIBase
{
    [SerializeField] private UIButton Button_Undo;
    [SerializeField] private TMP_Text Text_PlacementProgress;
    [SerializeField] private UIButton Button_Submit;

    [SerializeField] private RectTransform RectTransform_ToggleArrow;
    [SerializeField] private RectTransform Panel_Inventory;
    [SerializeField] private UIButton Button_Toggle;
    [SerializeField] private float _slideDuration = 0.25f;

    [Header("인벤토리 슬롯")]
    [SerializeField] private BuildInventorySlot Prefab_InventorySlot;
    [SerializeField] private RectTransform RectTransform_SlotContent;
    [SerializeField] private RectTransform RectTransform_SlotViewport;
    [SerializeField] private ScrollRect ScrollRect_Inventory;
    [SerializeField] private Scrollbar Scrollbar_Vertical;

    private readonly Dictionary<int, BuildInventorySlot> _inventorySlots = new();

    private Vector2 _openedPosition;
    private Vector2 _closedPosition;
    private Tween _panelTween;
    private bool _isPanelOpened = true;
    private Vector3 _toggleArrowDefaultScale;
    private SegmentBuildManager _buildManager;

    public override UILayer Layer => UILayer.Content;

    protected override bool ValidateReferences()
    {
        if (!base.ValidateReferences())
        {
            return false;
        }

        if (Button_Undo == null || Button_Submit == null || Button_Toggle == null || Text_PlacementProgress == null)
        {
            return false;
        }

        if (Prefab_InventorySlot == null || RectTransform_SlotContent == null || RectTransform_SlotViewport == null || ScrollRect_Inventory == null)
        {
            return false;
        }

        if (Panel_Inventory == null || RectTransform_ToggleArrow == null)
        {
            return false;
        }

        return true;
    }

    protected override void InitializeUI()
    {
        InitializePanel();
    }

    protected override void BindEvents()
    {
        BindManagerEvents();

        Button_Undo.BindOnClickButtonEvent(HandleClickUndoButton);
        Button_Submit.BindOnClickButtonEvent(HandleClickSubmitButton);
        Button_Toggle.BindOnClickButtonEvent(HandleClickToggleButton);
    }

    protected override void UnbindEvents()
    {
        UnbindManagerEvents();

        Button_Undo.UnbindOnClickButtonEvent(HandleClickUndoButton);
        Button_Submit.UnbindOnClickButtonEvent(HandleClickSubmitButton);
        Button_Toggle.UnbindOnClickButtonEvent(HandleClickToggleButton);
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
        _panelTween?.Kill();
        _panelTween = null;

        ClearInventorySlots();

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

        RebuildInventorySlots();
        RefreshSelectedSlot(_buildManager != null ? _buildManager.SelectedSlotIndex : -1);
        RefreshUndoButton();
        RefreshBuildStatus();
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
        SetPanelOpened(false);
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
        GameManager.Inst.BuildPhaseManager.SaveAndClearCurrentMap();
    }

    private void HandleClickToggleButton()
    {
        SetPanelOpened(!_isPanelOpened);
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

        int inventoryCount = inventorySlots != null ? inventorySlots.Count : 0;

        if (_inventorySlots.Count != inventoryCount)
        {
            RebuildInventorySlots();
            return;
        }

        for (int i = 0; i < inventoryCount; i++)
        {
            if (_inventorySlots.TryGetValue(i, out BuildInventorySlot inventorySlot))
            {
                inventorySlot.Refresh(inventorySlots[i]);
            }
        }
    }

    private void RefreshSelectedSlot(int selectedSlotIndex)
    {
        foreach (KeyValuePair<int, BuildInventorySlot> pair in _inventorySlots)
        {
            pair.Value.SetSelected(pair.Key == selectedSlotIndex);
        }
    }

    private void RefreshBuildStatus()
    {
        if (_buildManager == null)
        {
            Button_Submit.SetInteractable(false);
            return;
        }

        int placedCount = _buildManager.PlacedItemCount;
        int requiredCount = _buildManager.RequiredPlacementCount;

        Text_PlacementProgress.text = $"설치 {placedCount}/{requiredCount}";

        bool canSubmit = !_buildManager.IsBuildLocked && _buildManager.CanCompleteBuild;

        Button_Submit.SetInteractable(canSubmit);
    }

    private void InitializePanel()
    {
        Canvas.ForceUpdateCanvases();

        _openedPosition = Panel_Inventory.anchoredPosition;
        _toggleArrowDefaultScale = RectTransform_ToggleArrow.localScale;

        float panelWidth = Panel_Inventory.rect.width;

        _closedPosition = _openedPosition + Vector2.left * panelWidth;

        SetPanelOpened(true);
    }

    private void SetPanelOpened(bool isOpened)
    {
        _isPanelOpened = isOpened;
        RefreshToggleArrow();

        _panelTween?.Kill();

        Vector2 targetPosition = isOpened ? _openedPosition : _closedPosition;

        _panelTween = Panel_Inventory.DOAnchorPos(targetPosition, _slideDuration).SetEase(Ease.OutCubic);
    }

    private void RefreshToggleArrow()
    {
        float direction = _isPanelOpened ? 1f : -1f;

        RectTransform_ToggleArrow.localScale = new Vector3(_toggleArrowDefaultScale.x * direction, _toggleArrowDefaultScale.y, _toggleArrowDefaultScale.z);
    }

    private void RebuildInventorySlots()
    {
        ClearInventorySlots();

        if (_buildManager == null)
        {
            RefreshScrollState();
            return;
        }

        IReadOnlyList<InventorySlot> inventorySlots = _buildManager.InventorySlots;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            BuildInventorySlot inventorySlot = Instantiate(Prefab_InventorySlot, RectTransform_SlotContent);

            inventorySlot.Initialize(i);
            inventorySlot.OnSlotClicked += HandleSlotClicked;
            inventorySlot.Refresh(inventorySlots[i]);

            _inventorySlots.Add(i, inventorySlot);
        }

        RefreshScrollState();
    }

    private void ClearInventorySlots()
    {
        foreach (BuildInventorySlot inventorySlot in _inventorySlots.Values)
        {
            if (inventorySlot == null)
            {
                continue;
            }

            inventorySlot.OnSlotClicked -= HandleSlotClicked;
            inventorySlot.Release();
            Destroy(inventorySlot.gameObject);
        }

        _inventorySlots.Clear();
    }

    private void RefreshScrollState()
    {
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform_SlotContent);

        float contentHeight = LayoutUtility.GetPreferredHeight(RectTransform_SlotContent);
        float viewportHeight = RectTransform_SlotViewport.rect.height;

        bool canScroll = contentHeight > viewportHeight + 0.5f;

        ScrollRect_Inventory.vertical = canScroll;

        if (Scrollbar_Vertical != null)
        {
            Scrollbar_Vertical.gameObject.SetActive(canScroll);
        }

        if (!canScroll)
        {
            ScrollRect_Inventory.verticalNormalizedPosition = 1f;
            RectTransform_SlotContent.anchoredPosition = Vector2.zero;
        }
    }
}