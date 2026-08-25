using System.Collections.Generic;
using UnityEngine;

public enum WheelDirection
{
    Clockwise,
    CounterClockwise
}

public class ObstacleFerrisWheel : MonoBehaviour
{
    [SerializeField] private GameObject Prefab_Platform;
    [SerializeField] private int PlatformCount = 3;
    [SerializeField] private float Radius = 3f;
    [SerializeField] private float RotationSpeed = 30f;
    [SerializeField] private WheelDirection Direction_Wheel = WheelDirection.Clockwise;

    private readonly List<Transform> _platforms = new();
    private float _currentAngle;

    // private bool _isActive;

    private void Awake()
    {
        SpawnPlatforms();
    }

    // private void OnEnable()
    // {
    //     HazardActivationSignal.OnActivateAllRequested += HandleActivateAll;
    // }

    // private void OnDisable()
    // {
    //     HazardActivationSignal.OnActivateAllRequested -= HandleActivateAll;
    // }

    // private void HandleActivateAll()
    // {
    //     _isActive = true;
    // }

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

    private void Update()
    {
        // if (!_isActive) return;

        float directionMultiplier = Direction_Wheel == WheelDirection.Clockwise ? -1f : 1f;
        _currentAngle += RotationSpeed * directionMultiplier * Time.deltaTime;

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