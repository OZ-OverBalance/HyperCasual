using System;
using UnityEngine;

public enum LaunchDirection
{
    Up,
    Down,
    Left,
    Right
}

public class HazardLauncher : MonoBehaviour
{
    [SerializeField] private GameObject Prefab_Projectile;
    [SerializeField] private Transform Transform_FirePoint;
    [SerializeField] private LaunchDirection Direction_Fire = LaunchDirection.Right;
    [SerializeField] private float FireInterval = 2f;
    [SerializeField] private float ProjectileSpeed = 10f;
    [SerializeField] private float ProjectileLifetime = 5f;

    private static event Action OnActivateAllRequested;

    private float _fireTimer;
    private bool _isActive;

    private void OnEnable()
    {
        OnActivateAllRequested += HandleActiveAll;
    }

    private void OnDisable()
    {
        OnActivateAllRequested -= HandleActiveAll;
    }

    private void Update()
    {
        if (!_isActive) return;

        _fireTimer += Time.deltaTime;

        if (_fireTimer >= FireInterval)
        {
            _fireTimer = 0f;
            FireProjectile();
        }
    }

    public static void AcitvateAll()
    {
        OnActivateAllRequested?.Invoke();
    }

    private void HandleActiveAll()
    {
        _isActive = true;
        _fireTimer = 0f; 
    }

    private void FireProjectile()
    {
        if (Prefab_Projectile == null || Transform_FirePoint == null) return;

        Vector3 direction = GetDirectionVector();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion directionRotation = Quaternion.Euler(0f, 0f, angle);

        Quaternion finalRotation = directionRotation * Prefab_Projectile.transform.rotation;

        var projectileObj = UnityEngine.Object.Instantiate(Prefab_Projectile, Transform_FirePoint.position, finalRotation);

        if (projectileObj.TryGetComponent(out Projectile projectile))
        {
            projectile.Launch(direction, ProjectileSpeed, ProjectileLifetime);
        }
    }

    private Vector3 GetDirectionVector()
    {
        switch (Direction_Fire)
        {
            case LaunchDirection.Up: return transform.up;
            case LaunchDirection.Down: return -transform.up;
            case LaunchDirection.Left: return -transform.right;
            case LaunchDirection.Right: return transform.right;
            default: return transform.right;
        }
    }
}