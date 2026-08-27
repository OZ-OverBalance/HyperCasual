using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum LaunchDirection
{
    Up,
    Down,
    Left,
    Right
}

public class HazardLauncher : ObstacleBase
{
    [SerializeField] private GameObject Prefab_Projectile;
    [SerializeField] private Transform Transform_FirePoint;
    [SerializeField] private LaunchDirection Direction_Fire = LaunchDirection.Right;
    [SerializeField] private float FireInterval = 2f;
    [SerializeField] private float ProjectileSpeed = 10f;
    [SerializeField] private float ProjectileLifetime = 5f;
    [SerializeField] private bool AlignRotationToDirection = true;
    [SerializeField] private int InitialPoolSize = 4;

    [SerializeField] private float WarmupDuration = 0.2f; 

    private bool _hasWarmedUp;

    public event Action OnFireWarmupStart;
    public event Action OnPunchWarmupStart;
    public event Action OnFired;

    private float _fireTimer;
    private Queue<GameObject> _projectilePool = new Queue<GameObject>();
    private Transform _poolContainer;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializePool();
    }

    private void InitializePool()
    {
        GameObject containerObj = new GameObject("ProjectilePool_Container");
        containerObj.transform.SetParent(transform);
        _poolContainer = containerObj.transform;

        if (Prefab_Projectile == null) return;

        for (int i = 0; i < InitialPoolSize; i++)
        {
            GameObject obj = Instantiate(Prefab_Projectile, _poolContainer);
            obj.SetActive(false);
            SetupProjectilePoolReference(obj);
            _projectilePool.Enqueue(obj);
        }
    }

    private Quaternion CalculateRotation(Vector3 direction)
    {
        if (AlignRotationToDirection && Prefab_Projectile != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion directionRotation = Quaternion.Euler(0f, 0f, angle);
            return directionRotation * Prefab_Projectile.transform.rotation;
        }
        return Prefab_Projectile != null ? Prefab_Projectile.transform.rotation : Quaternion.identity;
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

    /// <summary>
    /// 서버를 포함한 모든 클라이언트가 각자의 로컬 풀에서 투사체를 발사하는 RPC
    /// </summary>
    [ClientRpc]
    private void FireClientRpc(Vector3 spawnPos, Quaternion rotation, Vector3 direction) 
    {
        if (Prefab_Projectile == null) return;

        var projectileObj = GetPooledProjectile(spawnPos, rotation);

        if (projectileObj.TryGetComponent(out Projectile projectile))
        {
            projectile.Launch(direction, ProjectileSpeed, ProjectileLifetime);
        }

        // 서버쪽에서 이펙트나 사운드 실행시점을 조절해야한다면 여기에 메서드 추가

    }

    private void SetupProjectilePoolReference(GameObject obj)
    {
        if (obj.TryGetComponent(out Projectile projectile))
        {
            projectile.InitPool(this);
        }
    }

    public GameObject GetPooledProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (_projectilePool.Count > 0)
        {
            obj = _projectilePool.Dequeue();
        }
        else
        {
            obj = Instantiate(Prefab_Projectile, _poolContainer);
            SetupProjectilePoolReference(obj);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        if (obj.TryGetComponent(out IPoolObject poolObj))
        {
            poolObj.OnSpawn();
        }

        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj.TryGetComponent(out IPoolObject poolObj))
        {
            poolObj.OnDespawn();
        }

        obj.SetActive(false);
        _projectilePool.Enqueue(obj);
    }

    protected override void OnObstacleStarted()
    {
        if (IsServer)
        {
            _fireTimer = 0f;
        }
    }

    protected override void OnObstacleUpdate()
    {
        if (!IsServer) return;

        _fireTimer += Time.deltaTime;

        if (_fireTimer >= FireInterval)
        {
            _fireTimer = 0f;

            Vector3 spawnPos = Transform_FirePoint.position;
            Vector3 direction = GetDirectionVector();
            Quaternion finalRotation = CalculateRotation(direction);

            FireClientRpc(spawnPos, finalRotation, direction);

            // 로컬에서 이펙트,사운드를 따로 재생해도 되면 여기에 메서드 추가

        }
    }
}