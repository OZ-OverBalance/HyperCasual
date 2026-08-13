using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SegmentSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject AssetRef_SegmentPrefab;
    [SerializeField] private Camera Camera_Local;

    //테스트용 동적생성
    private void Start()
    {
        ShowBuildPhase();
    }

    public void ShowBuildPhase()
    {
        SpawnSegmentAsync().Forget();
    }

    private async UniTaskVoid SpawnSegmentAsync()
    {
        var handle = Addressables.InstantiateAsync(AssetRef_SegmentPrefab, transform);
        var segmentInstance = await handle.ToUniTask();

        var inputHandler = segmentInstance.GetComponent<GridInputHandler>();
        inputHandler.SetCamera(Camera_Local);
    }
}