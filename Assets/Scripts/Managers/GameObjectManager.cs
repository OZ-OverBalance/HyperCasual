using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class GameObjectManager
{
    private readonly Dictionary<int, GameObjectInstance> _objectInstances;

    private int _nextInstanceId;

    public int ObjectCount => _objectInstances.Count;

    public GameObjectManager()
    {
        _objectInstances = new Dictionary<int, GameObjectInstance>();
        _nextInstanceId = 0;
    }

    // 프리팹 생성, 고유 InstanceId 발급 후 등록
    public bool TryCreateObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, out GameObjectInstance createdInstance)
    {
        createdInstance = null;

        if (prefab == null)
        {
            return false;
        }

        GameObject createdObject = Object.Instantiate(prefab, position, rotation, parent);

        if (!createdObject.TryGetComponent(out GameObjectInstance gameObjectInstance))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (!TryGenerateInstanceId(out int instanceId))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (!gameObjectInstance.TryInitializeInstance(instanceId))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (!_objectInstances.TryAdd(instanceId, gameObjectInstance))
        {
            Object.Destroy(createdObject);
            return false;
        }

        createdInstance = gameObjectInstance;

        return true;
    }

    /// <summary>
    /// 기존 TryCreateObject메서드에 동기화를 위한 로직을 추가한 Network전용 메서드
    /// </summary>
    public bool TryCreateObjectForNetwork(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, out GameObjectInstance createdInstance)
    {
        createdInstance = null;

        if (prefab == null)
        {
            return false;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return false;
        }

        GameObject createdObject = Object.Instantiate(prefab, position, rotation, parent);


        if (!createdObject.TryGetComponent(out GameObjectInstance gameObjectInstance))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (!TryGenerateInstanceId(out int instanceId))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (!gameObjectInstance.TryInitializeInstance(instanceId))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (!_objectInstances.TryAdd(instanceId, gameObjectInstance))
        {
            Object.Destroy(createdObject);
            return false;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (createdObject.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (!netObj.IsSpawned)
                {
                    netObj.Spawn(); 
                }

                var mapNetObj = parent.GetComponent<NetworkObject>();

                if (mapNetObj != null)
                {
                    netObj.TrySetParent(mapNetObj, worldPositionStays: false);
                }
                else
                {
                    Debug.Log("[GameObjectManager] 부모의 NetworkObject가 없어요ㅠㅠㅠㅠ(장애물설치)");
                }
            }
            else
            {
                Debug.LogWarning($"[ObjectManager] 프리팹({prefab.name})에 NetworkObject 컴포넌트가 없습니다!");
            }
        }

        createdInstance = gameObjectInstance;
        return true;
    }

    // InstanceId 로 등록된 객체 가져오기
    public bool TryGetObject(int instanceId, out GameObjectInstance gameObjectInstance)
    {
        gameObjectInstance = null;

        if (instanceId <= 0)
        {
            return false;
        }

        if (!_objectInstances.TryGetValue(instanceId, out GameObjectInstance registeredInstance))
        {
            return false;
        }

        if (registeredInstance == null)
        {
            _objectInstances.Remove(instanceId);
            return false;
        }

        gameObjectInstance = registeredInstance;

        return true;
    }

    // InstanceId 로 관리 목록에서 제거 후 파괴
    public bool TryDestroyObject(int instanceId)
    {
        if (!TryGetObject(instanceId, out GameObjectInstance gameObjectInstance))
        {
            return false;
        }

        _objectInstances.Remove(instanceId);
        Object.Destroy(gameObjectInstance.gameObject);

        return true;
    }

    // 현재 등록된 모든 객체 제거
    public void DestroyAllObjects()
    {
        foreach (GameObjectInstance gameObjectInstance in _objectInstances.Values)
        {
            if (gameObjectInstance != null)
            {
                Object.Destroy(gameObjectInstance.gameObject);
            }
        }

        _objectInstances.Clear();
    }

    private bool TryGenerateInstanceId(out int instanceId)
    {
        instanceId = -1;

        if (_nextInstanceId == int.MaxValue)
        {
            return false;
        }

        _nextInstanceId++;
        instanceId = _nextInstanceId;

        return true;
    }

    public bool TryCreateUIObject(GameObject prefab, Transform parent, out UIBase createdUI)
    {
        createdUI = null;

        if (!TryCreateObject(prefab, Vector3.zero, Quaternion.identity, parent, out GameObjectInstance createdInstance))
        {
            return false;
        }

        createdUI = createdInstance as UIBase;

        if (createdUI != null)
        {
            if (createdUI.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            return true;
        }

        TryDestroyObject(createdInstance.InstanceId);

        Debug.LogError($"GameObjectManager - {prefab.name} 프리팹에 UIBase 없음");
        return false;
    }
}