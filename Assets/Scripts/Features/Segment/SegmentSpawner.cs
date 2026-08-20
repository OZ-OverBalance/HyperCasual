using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SegmentSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject AssetRef_SegmentPrefab;
    [SerializeField] private Vector2 Offset_SpawnPositionl;

    private Camera _camera_Local;
    private GameObject _spawnedSegmentObject;

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

    public async UniTask<SegmentBuildManager> SpawnSegmentAsync()
    {
        var handle = Addressables.InstantiateAsync(AssetRef_SegmentPrefab, transform);
        _spawnedSegmentObject = await handle.ToUniTask();

        _spawnedSegmentObject.transform.localPosition = new Vector3(Offset_SpawnPositionl.x, Offset_SpawnPositionl.y, 0f);

        var inputHandler = _spawnedSegmentObject.GetComponent<GridInputHandler>();

        Camera cameraToUse = _camera_Local;

        cameraToUse = CameraManager.Inst.MainCamera;
        
        inputHandler.SetCamera(cameraToUse);

        return _spawnedSegmentObject.GetComponentInChildren<SegmentBuildManager>(true); 
    }

    public void ClearSpawnedSegment()
    {
        if (_spawnedSegmentObject != null)
        {
            Addressables.ReleaseInstance(_spawnedSegmentObject);
            _spawnedSegmentObject = null;
        }
    }
}