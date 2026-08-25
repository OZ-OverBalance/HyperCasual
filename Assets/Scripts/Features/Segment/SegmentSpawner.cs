using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SegmentSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject AssetRef_SegmentPrefab;
    [SerializeField] private Vector2 Offset_SpawnPositionl;

    private Camera _camera_Local;
    private GameObject _segmentInstance;

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

    public async UniTask<SegmentBuildManager> ShowBuildPhaseAsync(int roundIndex)
    {
        ReleaseBuildPhase();

        if (AssetRef_SegmentPrefab == null || !AssetRef_SegmentPrefab.RuntimeKeyIsValid())
        {
            Debug.LogError("SegmentSpawner - Segment 프리팹 참조 없음");
            return null;
        }

        _segmentInstance = await Addressables.InstantiateAsync(AssetRef_SegmentPrefab, transform);
        if (_segmentInstance == null)
        {
            Debug.LogError("SegmentSpawner - Segment 프리팹 생성 실패");
            return null;
        }

        Vector3 spawnPosition = transform.position + new Vector3(Offset_SpawnPositionl.x, Offset_SpawnPositionl.y, 0f);

        _segmentInstance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        Camera cameraToUse = CameraManager.Inst.MainCamera;
        if (cameraToUse == null)
        {
            Debug.LogError("SegmentSpawner - 제작용 카메라 없음");
            ReleaseBuildPhase();
            return null;
        }

        if (!_segmentInstance.TryGetComponent(out SegmentBuildRuntimeBinder runtimeBinder))
        {
            Debug.LogError("SegmentSpawner - 프리팹 루트에 SegmentBuildRuntimeBinder 없음");
            ReleaseBuildPhase();
            return null;
        }

        SegmentBuildManager segmentBuildManager = runtimeBinder.BuildManager;

        if (segmentBuildManager == null)
        {
            Debug.LogError("SegmentSpawner - SegmentBuildManager 없음");
            ReleaseBuildPhase();
            return null;
        }

        runtimeBinder.InitializeRuntime(cameraToUse, roundIndex);

        return segmentBuildManager;
    }

    public async UniTask<SegmentBuildManager> ShowBuildPhaseForNetworkAsync(int roundIndex)
    {
        ReleaseBuildPhase();

        if (AssetRef_SegmentPrefab == null || !AssetRef_SegmentPrefab.RuntimeKeyIsValid())
        {
            Debug.LogError("SegmentSpawner - Segment 프리팹 참조 없음");
            return null;
        }

        _segmentInstance = await Addressables.InstantiateAsync(AssetRef_SegmentPrefab);
        if (_segmentInstance == null)
        {
            Debug.LogError("SegmentSpawner - Segment 프리팹 생성 실패");
            return null;
        }

        Vector3 spawnPosition = transform.position + new Vector3(Offset_SpawnPositionl.x, Offset_SpawnPositionl.y, 0f);

        _segmentInstance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        Camera cameraToUse = CameraManager.Inst.MainCamera;
        if (cameraToUse == null)
        {
            Debug.LogError("SegmentSpawner - 제작용 카메라 없음");
            ReleaseBuildPhase();
            return null;
        }

        if (!_segmentInstance.TryGetComponent(out SegmentBuildRuntimeBinder runtimeBinder))
        {
            Debug.LogError("SegmentSpawner - 프리팹 루트에 SegmentBuildRuntimeBinder 없음");
            ReleaseBuildPhase();
            return null;
        }

        SegmentBuildManager segmentBuildManager = runtimeBinder.BuildManager;

        if (segmentBuildManager == null)
        {
            Debug.LogError("SegmentSpawner - SegmentBuildManager 없음");
            ReleaseBuildPhase();
            return null;
        }

        runtimeBinder.InitializeRuntime(cameraToUse, roundIndex);

        var networObj = _segmentInstance.GetComponent<NetworkObject>();

        networObj.Spawn();

        networObj.transform.SetParent(transform);
        networObj.transform.localPosition = Vector3.zero;
        networObj.transform.localRotation = Quaternion.identity;

        return segmentBuildManager;
    }

    public void ReleaseBuildPhase()
    {
        if (_segmentInstance == null)
        {
            return;
        }

        Addressables.ReleaseInstance(_segmentInstance);
        _segmentInstance = null;
    }
}