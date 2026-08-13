using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SegmentSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject AssetRef_SegmentPrefab;
    [SerializeField] private Camera Camera_Local;
    [SerializeField] private Vector2 Offset_SpawnPositionl;

    //테스트용 동적생성
    private void Start()
    {
        ShowBuildPhase();

        if (Camera_Local == null)
        {
            Camera_Local = Camera.main;
        }
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
        inputHandler.SetCamera(Camera_Local);
    }
}