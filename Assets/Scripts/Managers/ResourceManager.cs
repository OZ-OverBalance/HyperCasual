using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class ResourceManager : SingletonBase<ResourceManager>
{
    private sealed class ResourceEntry
    {
        public Type AssetType { get; }
        public AsyncOperationHandle Handle { get; }
        public int ReferenceCount { get; set; }

        public ResourceEntry(Type assetType, AsyncOperationHandle handle)
        {
            AssetType = assetType;
            Handle = handle;
            ReferenceCount = 1;
        }
    }

    private readonly Dictionary<string, ResourceEntry> ResourceEntries = new();

    // Addressables 에셋 비동기 로드
    // 이미 로드 중이거나 완료된 에셋은 같은 Handle 재사용
    public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            Debug.LogError("ResourceManager - Address 가 비어 있음");
            return null;
        }

        if (ResourceEntries.TryGetValue(address, out ResourceEntry resourceEntry))
        {
            if (resourceEntry.AssetType != typeof(T))
            {
                Debug.LogError($"ResourceManager - {address}의 요청 타입 다름 등록 타입: {resourceEntry.AssetType.Name}, 요청 타입: {typeof(T).Name}");
                return null;
            }

            resourceEntry.ReferenceCount++;

            return await resourceEntry.Handle.Convert<T>().ToUniTask();
        }

        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(address);

        ResourceEntry newEntry = new(typeof(T), loadHandle);

        ResourceEntries.Add(address, newEntry);

        try
        {
            T loadedAsset = await loadHandle.ToUniTask();

            if (loadedAsset == null)
            {
                ReleaseFailedAsset(address, newEntry);
                return null;
            }

            return loadedAsset;
        }
        catch (Exception exception)
        {
            Debug.LogError($"ResourceManager - 에셋 로드 실패 : {address}\n{exception.Message}");

            ReleaseFailedAsset(address, newEntry);
            return null;
        }
    }

    // 에셋의 참조 횟수를 감소시키고, 더 이상 사용하지 않으면 해제합니다.
    public bool TryReleaseAsset(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        if (!ResourceEntries.TryGetValue(address, out ResourceEntry resourceEntry))
        {
            return false;
        }

        resourceEntry.ReferenceCount--;

        if (resourceEntry.ReferenceCount > 0)
        {
            return true;
        }

        if (resourceEntry.Handle.IsValid())
        {
            Addressables.Release(resourceEntry.Handle);
        }

        ResourceEntries.Remove(address);
        return true;
    }

    // 현재 캐싱된 모든 Addressables 에셋을 해제
    public void ReleaseAllAssets()
    {
        foreach (ResourceEntry resourceEntry in ResourceEntries.Values)
        {
            if (resourceEntry.Handle.IsValid())
            {
                Addressables.Release(resourceEntry.Handle);
            }
        }

        ResourceEntries.Clear();
    }

    protected override void OnDestroy()
    {
        if (Inst != this)
        {
            return;
        }

        ReleaseAllAssets();

        base.OnDestroy();
    }

    private void ReleaseFailedAsset(string address,ResourceEntry failedEntry)
    {
        if (ResourceEntries.TryGetValue(address, out ResourceEntry registeredEntry) && registeredEntry == failedEntry)
        {
            ResourceEntries.Remove(address);
        }

        if (failedEntry.Handle.IsValid())
        {
            Addressables.Release(failedEntry.Handle);
        }
    }
}