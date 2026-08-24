using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildInventorySlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIButton Button_Select;
    [SerializeField] private Image Image_Icon;
    [SerializeField] private Image Image_SelectedFrame;
    [SerializeField] private TMP_Text Text_Count;
    [SerializeField] private GameObject Object_DisabledCover;

    private InventorySlot _inventorySlot;
    private int _slotIndex = -1;

    public event Action<int> OnSlotClicked;

    private void OnEnable()
    {
        if (Button_Select != null)
        {
            Button_Select.BindOnClickButtonEvent(HandleClickSlot);
        }
    }

    private void OnDisable()
    {
        if (Button_Select != null)
        {
            Button_Select.UnbindOnClickButtonEvent(HandleClickSlot);
        }
    }

    public void Initialize(int slotIndex)
    {
        _slotIndex = slotIndex;
        SetSelected(false);
    }

    public void Refresh(InventorySlot inventorySlot)
    {
        _inventorySlot = inventorySlot;

        bool hasData = inventorySlot != null && inventorySlot.Data != null;

        gameObject.SetActive(hasData);

        if (!hasData)
        {
            return;
        }

        Image_Icon.sprite = inventorySlot.Data.Icon_Thumbnail;
        Image_Icon.enabled = inventorySlot.Data.Icon_Thumbnail != null;

        Text_Count.text = inventorySlot.RemainingCount.ToString();

        bool isAvailable = inventorySlot.RemainingCount > 0;

        Button_Select.SetInteractable(isAvailable);
        Object_DisabledCover.SetActive(!isAvailable);

        if (!isAvailable)
        {
            SetSelected(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (Image_SelectedFrame != null)
        {
            Image_SelectedFrame.gameObject.SetActive(isSelected);
        }
    }

    public void Release()
    {
        _inventorySlot = null;
        _slotIndex = -1;
        OnSlotClicked = null;
    }

    private void HandleClickSlot()
    {
        if (_inventorySlot == null || _inventorySlot.Data == null || _inventorySlot.RemainingCount <= 0)
        {
            return;
        }

        OnSlotClicked?.Invoke(_slotIndex);
    }
}