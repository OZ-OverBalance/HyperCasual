using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum WheelDirection
{
    Clockwise,
    CounterClockwise
}

public class ObstacleFerrisWheel : ObstacleBase
{
    [SerializeField] private GameObject Prefab_Platform;
    [SerializeField] private int PlatformCount = 3;
    [SerializeField] private float Radius = 3f;
    [SerializeField] private float RotationSpeed = 1f;
    [SerializeField] private WheelDirection Direction_Wheel = WheelDirection.Clockwise;

    private readonly List<Transform> _platforms = new();
    private double _globalStartTime;
    private float _currentAngle;


    private void Awake()
    {
        SpawnPlatforms();
    }


    private void SpawnPlatforms()
    {
        int clampedCount = Mathf.Clamp(PlatformCount, 3, 5);
        var objectManager = GameManager.Inst.GameObjectManager;

        for (int i = 0; i < clampedCount; i++)
        {
            if (objectManager.TryCreateObject(Prefab_Platform, transform.position, Quaternion.identity, transform, out GameObjectInstance instance))
            {
                _platforms.Add(instance.transform);
            }
        }
    }

    protected override void OnObstacleStarted()
    {
        if (NetCodeObstacleManager.Instance != null)
        {
            _globalStartTime = NetCodeObstacleManager.Instance.GlobalStartTime.Value;
        }
    }

    protected override void OnObstacleUpdate()
    {
        double elapsedTime = NetworkManager.Singleton.ServerTime.Time - _globalStartTime; 
        if(elapsedTime < 0) elapsedTime = 0;

        float directionMultiplier = Direction_Wheel == WheelDirection.Clockwise ? -1f : 1f;
        _currentAngle = RotationSpeed * directionMultiplier * (float)elapsedTime;

        UpdatePlatformPositions();
    }

    private void UpdatePlatformPositions()
    {
        Vector3 center = transform.position;
        float angleStep = 360f / _platforms.Count;

        for (int i = 0; i < _platforms.Count; i++)
        {
            float angle = _currentAngle + (angleStep * i);
            float radians = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * Radius;

            _platforms[i].position = center + offset;
            _platforms[i].rotation = Quaternion.identity;
        }
    }
}