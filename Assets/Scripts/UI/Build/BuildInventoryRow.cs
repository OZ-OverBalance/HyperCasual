using UnityEngine;

public class BuildInventoryRow : MonoBehaviour
{
    [SerializeField] private RectTransform RectTransform_Content;

    public void AddSlot(BuildInventorySlot slot)
    {
        slot.transform.SetParent(RectTransform_Content, false);
    }
}