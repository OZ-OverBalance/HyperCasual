using System.Collections.Generic;
using UnityEngine;

public class SegmentGhostPreview : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;
    [SerializeField] private GridInputHandler InputHandler_Grid;
    [SerializeField] private Shader Shader_Ghost;
    [SerializeField] private Color Color_Valid = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color Color_Invalid = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private GameObject Prefab_ArrowIndicator; 

    private readonly List<GameObject> _indicatorCells = new();
    private GameObject _silhouetteInstance;
    private Renderer[] _silhouetteRenderers;
    private GameObject _arrowInstance;
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
        RebuildIndicatorCells();
        RebuildSilhouette();
        RebuildArrow();
    }

    private void HandleRotationChanged(int rotationStep)
    {
        _currentRotation = rotationStep;

        if (_silhouetteInstance != null)
        {
            _silhouetteInstance.transform.localRotation = Quaternion.Euler(0f, 0f, rotationStep * 90f);
        }

        UpdateArrowRotation();
    }

    private void HandleHoverChanged(Vector3 worldPos, bool hasHit)
    {
        bool shouldShow = _currentItem != null && hasHit;
        SetVisible(shouldShow);
        if (!shouldShow) return;

        var hoveredCell = Manager_Segment.ToCell(worldPos);
        var rotatedOffsets = Manager_Segment.GetRotatedOffsets(_currentItem.CellOffsets, _currentRotation);

        for (int i = 0; i < _indicatorCells.Count && i < rotatedOffsets.Count; i++)
        {
            var cell = hoveredCell + rotatedOffsets[i];
            _indicatorCells[i].transform.position = Manager_Segment.GetCellCenterWorldPos(cell);
        }

        if (_silhouetteInstance != null)
        {
            _silhouetteInstance.transform.position = Manager_Segment.GetCellCenterWorldPos(hoveredCell);
        }

        UpdateArrowPosition(hoveredCell);

        bool isValid = Manager_Segment.IsPlacementValid(worldPos, _currentItem, _currentRotation);
        Color tint = isValid ? Color_Valid : Color_Invalid;
        _ghostMaterial.color = tint;
    }

    private void RebuildArrow()
    {
        ClearArrow();

        if (_currentItem == null || !_currentItem.ShowDirectionArrow) return;
        if (Prefab_ArrowIndicator == null) return;

        _arrowInstance = Instantiate(Prefab_ArrowIndicator, transform);
        UpdateArrowRotation();
    }

    private void ClearArrow()
    {
        if (_arrowInstance != null)
        {
            Destroy(_arrowInstance);
            _arrowInstance = null;
        }
    }

    private void UpdateArrowPosition(Vector2Int hoveredCell)
    {
        if (_arrowInstance == null) return;

        var rotatedOrigin = Manager_Segment.GetRotatedOffsets(
            new List<Vector2Int> { _currentItem.ArrowOriginCell }, _currentRotation)[0];

        var arrowCell = hoveredCell + rotatedOrigin;
        _arrowInstance.transform.position = Manager_Segment.GetCellCenterWorldPos(arrowCell);
    }

    private void UpdateArrowRotation()
    {
        if (_arrowInstance == null || _currentItem == null) return;

        var rotatedDirection = Manager_Segment.GetRotatedOffsets(
            new List<Vector2Int> { _currentItem.ArrowLocalDirection }, _currentRotation)[0];

        float angle = Mathf.Atan2(rotatedDirection.y, rotatedDirection.x) * Mathf.Rad2Deg;
        _arrowInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void RebuildIndicatorCells()
    {
        ClearIndicatorCells();

        if (_currentItem == null) return;

        for (int i = 0; i < _currentItem.CellOffsets.Count; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localScale = Vector3.one * 0.9f;
            cube.GetComponent<Renderer>().sharedMaterial = _ghostMaterial;
            Destroy(cube.GetComponent<Collider>());
            _indicatorCells.Add(cube);
        }
    }

    private void ClearIndicatorCells()
    {
        for (int i = 0; i < _indicatorCells.Count; i++)
        {
            Destroy(_indicatorCells[i]);
        }
        _indicatorCells.Clear();
    }

    private void RebuildSilhouette()
    {
        ClearSilhouette();

        if (_currentItem == null || _currentItem.AssetRef_Prefab == null) return;

        var loadedPrefab = _currentItem.AssetRef_Prefab.Asset as GameObject;
        if (loadedPrefab == null) return;

        _silhouetteInstance = Instantiate(loadedPrefab, transform);
        _silhouetteInstance.name = "GhostSilhouette";

        StripFunctionalComponents(_silhouetteInstance);

        _silhouetteRenderers = _silhouetteInstance.GetComponentsInChildren<Renderer>();
        ApplyGhostMaterialToSilhouette();
    }

    private void StripFunctionalComponents(GameObject instance)
    {
        var colliders = instance.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        var rigidbodies = instance.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Destroy(rigidbodies[i]);
        }

        var networkObjects = instance.GetComponentsInChildren<Unity.Netcode.NetworkObject>();
        for (int i = 0; i < networkObjects.Length; i++)
        {
            Destroy(networkObjects[i]);
        }
    }

    private void ApplyGhostMaterialToSilhouette()
    {
        if (_silhouetteRenderers == null) return;

        for (int i = 0; i < _silhouetteRenderers.Length; i++)
        {
            var materials = new Material[_silhouetteRenderers[i].sharedMaterials.Length];
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = _ghostMaterial;
            }
            _silhouetteRenderers[i].materials = materials;
        }
    }

    private void ClearSilhouette()
    {
        if (_silhouetteInstance != null)
        {
            Destroy(_silhouetteInstance);
            _silhouetteInstance = null;
        }
    }

    private void SetVisible(bool visible)
    {
        for (int i = 0; i < _indicatorCells.Count; i++)
        {
            _indicatorCells[i].SetActive(visible);
        }

        if (_silhouetteInstance != null)
        {
            _silhouetteInstance.SetActive(visible);
        }

        if (_arrowInstance != null)
        {
            _arrowInstance.SetActive(visible);
        }
    }
}