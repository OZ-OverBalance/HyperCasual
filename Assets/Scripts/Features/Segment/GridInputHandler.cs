using UnityEngine;

public class GridInputHandler : MonoBehaviour
{
    [SerializeField] private Camera Camera_Build;
    [SerializeField] private LayerMask LayerMask_Buildable;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Debug.Log("[GridInputHandler] 클릭 감지됨");

        if (TryGetWorldPointUnderPointer(out var worldPos))
        {
            Debug.Log("[GridInputHandler] 레이캐스트 성공, worldPos=" + worldPos);
            SegmentBuildManager.Inst.TryPlaceAt(worldPos);
        }
        else
        {
            Debug.Log("[GridInputHandler] 레이캐스트 실패 - Buildable 콜라이더에 안 닿음");
        }
    }

    private bool TryGetWorldPointUnderPointer(out Vector3 worldPos)
    {
        var ray = Camera_Build.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 3f);

        if (Physics.Raycast(ray, out var hit, 100f, LayerMask_Buildable, QueryTriggerInteraction.Collide))
        {
            worldPos = hit.point;
            return true;
        }

        worldPos = default;
        return false;
    }
}
