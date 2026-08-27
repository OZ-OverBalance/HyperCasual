using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInputHandler : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;
    [SerializeField] private Camera Camera_Build;
    [SerializeField] private LayerMask LayerMask_Buildable;

    public event Action<Vector3, bool> OnHoverChanged;

    //public void SetCamera(Camera camera)
    //{
    //    Camera_Build = camera;
    //}

    private void Update()
    {
        if (Manager_Segment == null || Camera_Build == null)
        {
            return;
        }

        if (IsPointerOverUI())
        {
            OnHoverChanged?.Invoke(Vector3.zero, false);
            return;
        }

        bool hasHit = TryGetWorldPointUnderPointer(out Vector3 worldPosition);
        OnHoverChanged?.Invoke(worldPosition, hasHit);

        if (!Input.GetMouseButtonDown(0) || !hasHit)
        {
            return;
        }

        Manager_Segment.TryPlaceAt(worldPosition);
    }

    private bool TryGetWorldPointUnderPointer(out Vector3 worldPos)
    {
        var ray = Camera_Build.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit, 100f, LayerMask_Buildable, QueryTriggerInteraction.Collide))
        {
            worldPos = hit.point;
            return true;
        }

        worldPos = default;
        return false;
    }

    public void InitializeHandler(SegmentBuildManager segmentBuildManager, Camera buildCamera)
    {
        Manager_Segment = segmentBuildManager;
        Camera_Build = buildCamera;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
