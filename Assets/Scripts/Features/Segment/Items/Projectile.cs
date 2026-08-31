using UnityEngine;

public enum ProjectileMotionType
{
    Linear,
    Gravity
}

public class Projectile : MonoBehaviour, IPoolObject
{
    [SerializeField] private ProjectileMotionType MotionType_Movement = ProjectileMotionType.Linear;
    [SerializeField] private LayerMask LayerMask_BlockedBy;
    [SerializeField] private string Tag_MapFixture = "MapFixture";
    [SerializeField] private string Tag_Player = "Player";

    private Vector3 _moveDirection;
    private float _moveSpeed;
    private float _remainingLifetime;
    private Rigidbody _rigidbody;
    private HazardLauncher _ownerLauncher;
    private bool _isLaunched = false;

    private void Awake()
    {
        TryGetComponent(out _rigidbody);
    }

    public void InitPool(HazardLauncher launcher)
    {
        _ownerLauncher = launcher;
    }

    public void Launch(Vector3 direction, float speed, float lifetime, Collider ignoreCollider = null)
    {
        _moveDirection = direction.normalized;
        _moveSpeed = speed;
        _remainingLifetime = lifetime;
        _isLaunched = true;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = false;
        }

        if (MotionType_Movement == ProjectileMotionType.Gravity && _rigidbody != null)
        {
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = _moveDirection * _moveSpeed;
        }

        if (ignoreCollider != null && TryGetComponent(out Collider myCollider))
        {
            Physics.IgnoreCollision(myCollider, ignoreCollider, true);
        }
    }

    private void FixedUpdate()
    {
        if (!_isLaunched) return;

        if (MotionType_Movement == ProjectileMotionType.Linear && _rigidbody != null)
        {
            Vector3 displacement = _moveDirection * (_moveSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(_rigidbody.position + displacement);
        }
    }

    private void Update()
    {
        if (!_isLaunched) return;

        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f)
        {
            ReturnSelfToPool();
        }
    }

    private void ReturnSelfToPool()
    {
        _isLaunched = false;

        if (_ownerLauncher != null)
        {
            _ownerLauncher.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSpawn()
    {
        _isLaunched = false;

        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void OnDespawn()
    {
        _isLaunched = false;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        bool isBlockedByLayer = IsInLayerMask(collision.gameObject.layer, LayerMask_BlockedBy);
        bool isBlockedByTag = HasTagInParent(collision.transform, Tag_MapFixture);
        bool hitPlayer = collision.gameObject.CompareTag(Tag_Player);

        if (isBlockedByLayer || isBlockedByTag || hitPlayer)
        {
            ReturnSelfToPool();
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private bool HasTagInParent(Transform target, string tag)
    {
        Transform current = target;

        while (current != null)
        {
            if (current.CompareTag(tag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}