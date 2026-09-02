using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SegmentGhostPreview : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;
    [SerializeField] private GridInputHandler InputHandler_Grid;
    [SerializeField] private Shader Shader_Ghost;
    [SerializeField] private Color Color_Valid = new Color(0.3f, 1f, 0.3f, 0.6f);
    [SerializeField] private Color Color_Invalid = new Color(1f, 0.3f, 0.3f, 0.6f);
    [SerializeField] private GameObject Prefab_ArrowIndicator;

    private GameObject _silhouetteInstance;
    private Renderer[] _silhouetteRenderers;
    private GameObject _arrowInstance;
    private Material _silhouetteMaterial;

    private PlaceableObjectData _currentItem;
    private int _currentRotation;
    private Vector2Int? _lastHoveredCell;

    private void Awake()
    {
        _silhouetteMaterial = new Material(Shader_Ghost);
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
        _lastHoveredCell = hoveredCell;

        if (_silhouetteInstance != null)
        {
            _silhouetteInstance.transform.position = Manager_Segment.GetCellCenterWorldPos(hoveredCell);
        }

        UpdateArrowPosition(hoveredCell);

        bool isValid = Manager_Segment.IsPlacementValid(worldPos, _currentItem, _currentRotation);
        _silhouetteMaterial.color = isValid ? Color_Valid : Color_Invalid; // 실루엣 자체가 색상 판정을 겸함
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

    private void RebuildSilhouette()
    {
        ClearSilhouette();

        if (_currentItem == null || _currentItem.AssetRef_Prefab == null) return;

        LoadSilhouetteAsync(_currentItem).Forget();
    }

    private async UniTaskVoid LoadSilhouetteAsync(PlaceableObjectData data)
    {
        GameObject prefab = await ResourceManager.Inst.LoadAssetAsync<GameObject>(data.AssetRef_Prefab.AssetGUID);

        if (prefab == null) return;
        if (_currentItem != data) return;

        bool wasPrefabActive = prefab.activeSelf;
        prefab.SetActive(false);

        _silhouetteInstance = Instantiate(prefab, transform);

        prefab.SetActive(wasPrefabActive);

        _silhouetteInstance.name = "GhostSilhouette";

        StripFunctionalComponents(_silhouetteInstance);

        _silhouetteRenderers = _silhouetteInstance.GetComponentsInChildren<Renderer>(true);
        ApplyGhostMaterialToSilhouette();

        _silhouetteInstance.transform.localRotation = Quaternion.Euler(0f, 0f, _currentRotation * 90f);

        if (_lastHoveredCell.HasValue)
        {
            _silhouetteInstance.transform.position = Manager_Segment.GetCellCenterWorldPos(_lastHoveredCell.Value);
        }

        _silhouetteInstance.SetActive(true);
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

        var behaviours = instance.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            Destroy(behaviours[i]);
        }

        var particles = instance.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop();
            Destroy(particles[i]);
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
                materials[j] = _silhouetteMaterial;
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