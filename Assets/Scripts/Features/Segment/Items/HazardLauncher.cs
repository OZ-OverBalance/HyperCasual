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

        var projectileObj = Instantiate(Prefab_Projectile, Transform_FirePoint.position, Quaternion.identity);

        if (projectileObj.TryGetComponent(out Projectile projectile))
        {
            projectile.Launch(GetDirectionVector(), ProjectileSpeed, ProjectileLifetime);
        }
    }

    private Vector3 GetDirectionVector()
    {
        switch (Direction_Fire)
        {
            case LaunchDirection.Up: return Vector3.up;
            case LaunchDirection.Down: return Vector3.down;
            case LaunchDirection.Left: return Vector3.left;
            case LaunchDirection.Right: return Vector3.right;
            default: return Vector3.right;
        }
    }
}