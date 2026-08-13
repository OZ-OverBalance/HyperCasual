using System.Collections.Generic;
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
            return true;
        }

        TryDestroyObject(createdInstance.InstanceId);

        Debug.LogError($"GameObjectManager - {prefab.name} 프리팹에 UIBase 없음");
        return false;
    }
}