using UnityEngine;

public sealed class SegmentBuildRuntimeBinder : MonoBehaviour
{
    [SerializeField] private SegmentBuildManager Manager_Segment;
    [SerializeField] private GridInputHandler InputHandler_Grid;
    [SerializeField] private SegmentBuildTestHarness Harness_Test;

    public SegmentBuildManager BuildManager
    {
        get
        {
            return Manager_Segment;
        }
    }

    public void InitializeRuntime(Camera buildCamera, int roundIndex)
    {
        if (Manager_Segment == null)
        {
            Debug.LogError("SegmentBuildRuntimeBinder - SegmentBuildManager 참조 없음");
            return;
        }

        if (InputHandler_Grid == null)
        {
            Debug.LogError("SegmentBuildRuntimeBinder - GridInputHandler 참조 없음");
            return;
        }

        if (buildCamera == null)
        {
            Debug.LogError("SegmentBuildRuntimeBinder - 제작 카메라 없음");
            return;
        }

        InputHandler_Grid.InitializeHandler(Manager_Segment, buildCamera);

        if (Harness_Test == null)
        {
            Debug.LogError("SegmentBuildRuntimeBinder - SegmentBuildTestHarness 참조 없음");
            return;
        }

        Harness_Test.InitializeHarness(Manager_Segment, roundIndex);
    }
}