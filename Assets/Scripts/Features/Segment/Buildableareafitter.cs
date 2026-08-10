using UnityEngine;

// Buildable 오브젝트의 위치나 크기를 손으로 설정하는데 생기는 실수를 막아주는 피터
public class BuildableAreaFitter : MonoBehaviour
{
    [SerializeField] private Grid Grid_Shared;
    [SerializeField] private SegmentConfig Data_Config;
    [SerializeField] private float ColliderDepth = 0.2f;

    [ContextMenu("그리드에 정확히 맞추기")]
    private void FitToGrid()
    {
        if (Grid_Shared == null || Data_Config == null)
        {
            Debug.LogWarning("[BuildableAreaFitter] Grid_Shared 또는 Data_Config가 비어있습니다.");
            return;
        }

        Vector3 min = Grid_Shared.CellToWorld(Vector3Int.zero);
        Vector3 max = Grid_Shared.CellToWorld(new Vector3Int(Data_Config.GridSize.x, Data_Config.GridSize.y, 0));

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        transform.position = center;
        transform.localScale = new Vector3(size.x, size.y, ColliderDepth);

        Debug.Log("[BuildableAreaFitter] 정렬 완료 - Position=" + center + ", Scale=" + transform.localScale);
    }
}