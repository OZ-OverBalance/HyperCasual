using System;
using UnityEngine;

public class GridInputHandler : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;
    [SerializeField] private Camera Camera_Build;
    [SerializeField] private LayerMask LayerMask_Buildable;

    public event Action<Vector3, bool> OnHoverChanged;

    public void SetCamera(Camera camera)
    {
        Camera_Build = camera;
    }

    private void Update()
    {
        bool hasHit = TryGetWorldPointUnderPointer(out var worldPos);
        OnHoverChanged?.Invoke(worldPos, hasHit);

        if (Input.GetMouseButtonDown(0) && hasHit)
        {
            Manager_Segment.TryPlaceAt(worldPos);
        }
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
}
