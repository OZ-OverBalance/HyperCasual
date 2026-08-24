using UnityEngine;

public class TestSaveButton : MonoBehaviour
{
    [SerializeField] private UIButton Button_Save;

    private void OnEnable()
    {
        Button_Save.BindOnClickButtonEvent(OnClickCompleteBuild);
    }

    private void OnClickCompleteBuild()
    {
        GameManager.Inst.BuildPhaseManager.SegmentBuildManager.CompleteBuild();
        GameManager.Inst.BuildPhaseManager.SaveAndClearCurrentMap();
        //GameManager.Inst.RoundManager.TryStartRun();
    }

    private void NoneCode()
    {

    }
}
