using System.Collections.Generic;
using UnityEngine;

public class SegmentGhostPreview : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;
    [SerializeField] private GridInputHandler InputHandler_Grid;
    [SerializeField] private Transform Transform_GhostRoot;
    [SerializeField] private Shader Shader_Ghost;
    [SerializeField] private Color Color_Valid = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color Color_Invalid = new Color(1f, 0.3f, 0.3f, 1f);

    private readonly List<GameObject> _ghostCells = new();
    private Material _ghostMaterial;

    private PlaceableObjectData _currentItem;
    private int _currentRotation;

    private void Awake()
    {
        _ghostMaterial = new Material(Shader_Ghost);
    }

    private void OnEnable()
    {
        Manager_Segment.OnItemSelected += HandleItemSelected;
        Manager_Segment.OnRotationChanged += HandleRotationChanged;
        InputHandler_Grid.OnHoverChanged += HandleHoverChanged;
    }

    private void OnDisable()
    {
        Manager_Segment.OnItemSelected -= HandleItemSelected;
        Manager_Segment.OnRotationChanged -= HandleRotationChanged;
        InputHandler_Grid.OnHoverChanged -= HandleHoverChanged;
    }

    private void HandleItemSelected(PlaceableObjectData data)
    {
        _currentItem = data;
        _currentRotation = 0;
        RebuildGhostCells();
    }

    private void HandleRotationChanged(int rotationStep)
    {
        _currentRotation = rotationStep;
    }

    private void HandleHoverChanged(Vector3 worldPos, bool hasHit)
    {
        bool shouldShow = _currentItem != null && hasHit;
        SetGhostActive(shouldShow);
        if (!shouldShow) return;

        var hoveredCell = Manager_Segment.ToCell(worldPos);
        var rotatedOffsets = Manager_Segment.GetRotatedOffsets(_currentItem.CellOffsets, _currentRotation);

        for (int i = 0; i < _ghostCells.Count && i < rotatedOffsets.Count; i++)
        {
            var cell = hoveredCell + rotatedOffsets[i];
            _ghostCells[i].transform.position = Manager_Segment.GetCellCenterWorldPos(cell);
        }

        bool isValid = Manager_Segment.IsPlacementValid(worldPos, _currentItem, _currentRotation);
        _ghostMaterial.color = isValid ? Color_Valid : Color_Invalid;
    }

    private void RebuildGhostCells()
    {
        ClearGhostCells();

        if (_currentItem == null) return;

        for (int i = 0; i < _currentItem.CellOffsets.Count; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(Transform_GhostRoot);
            cube.transform.localScale = Vector3.one * 0.9f;
            cube.GetComponent<Renderer>().sharedMaterial = _ghostMaterial;
            Destroy(cube.GetComponent<Collider>());
            _ghostCells.Add(cube);
        }
    }

    private void ClearGhostCells()
    {
        for (int i = 0; i < _ghostCells.Count; i++)
        {
            Destroy(_ghostCells[i]);
        }
        _ghostCells.Clear();
    }

    private void SetGhostActive(bool active)
    {
        for (int i = 0; i < _ghostCells.Count; i++)
        {
            _ghostCells[i].SetActive(active);
        }
    }
}