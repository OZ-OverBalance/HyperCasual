using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SegmentSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject AssetRef_SegmentPrefab;
    [SerializeField] private Vector2 Offset_SpawnPositionl;

    private Camera _camera_Local;

    //테스트용 동적생성
    //private void Start()
    //{
    //    ShowBuildPhase();
    //}

    // 플레이어 개별 카메라 구현 전까지 메인 카메라로 통합하기 위한 임시 장치
    public void SetLocalCamera(Camera camera)
    {
        _camera_Local = camera;
    }

    public void ShowBuildPhase()
    {
        SpawnSegmentAsync().Forget();
    }

    private async UniTaskVoid SpawnSegmentAsync()
    {
        var handle = Addressables.InstantiateAsync(AssetRef_SegmentPrefab, transform);
        var segmentInstance = await handle.ToUniTask();

        segmentInstance.transform.localPosition = new Vector3(Offset_SpawnPositionl.x, Offset_SpawnPositionl.y, 0f);

        var inputHandler = segmentInstance.GetComponent<GridInputHandler>();

        Camera cameraToUse = _camera_Local;

        cameraToUse = CameraManager.Inst.MainCamera;

        if (cameraToUse == null) return;

        inputHandler.SetCamera(cameraToUse);
    }
}