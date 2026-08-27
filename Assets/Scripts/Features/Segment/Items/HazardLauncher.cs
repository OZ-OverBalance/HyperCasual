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
    [SerializeField] private bool AlignRotationToDirection = true;

    [SerializeField] private float WarmupDuration = 0.2f; 

    private bool _hasWarmedUp;

    public event Action OnFireWarmupStart;
    public event Action OnPunchWarmupStart;
    public event Action OnFired;

    private float _fireTimer;
    //private bool _isActive;

    //private void OnEnable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested += HandleActiveAll;
    //}

    //private void OnDisable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested -= HandleActiveAll;
    //}

    //private void HandleActiveAll()
    //{
    //    _isActive = true;
    //    _fireTimer = 0f;
    //}

    private void Update()
    {
        //if (!_isActive) return;

        _fireTimer += Time.deltaTime;

        if (_fireTimer >= FireInterval - WarmupDuration && !_hasWarmedUp)
        {
            _hasWarmedUp = true;
            OnFireWarmupStart?.Invoke();
        }

        if (_fireTimer >= FireInterval)
        {
            _fireTimer = 0f;
            _hasWarmedUp = false;
            FireProjectile();
        }
    }

    private void FireProjectile()
    {
        if (Prefab_Projectile == null || Transform_FirePoint == null) return;

        Vector3 direction = GetDirectionVector();
        Quaternion finalRotation;

        if (AlignRotationToDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion directionRotation = Quaternion.Euler(0f, 0f, angle);
            finalRotation = directionRotation * Prefab_Projectile.transform.rotation;
        }
        else
        {
            finalRotation = Prefab_Projectile.transform.rotation;
        }

        var projectileObj = Instantiate(Prefab_Projectile, Transform_FirePoint.position, finalRotation);

        if (projectileObj.TryGetComponent(out Projectile projectile))
        {
            projectile.Launch(direction, ProjectileSpeed, ProjectileLifetime);
        }

        OnFired?.Invoke();
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