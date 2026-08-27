using UnityEngine;

public enum ProjectileMotionType
{
    Linear,   
    Gravity  
}

public class Projectile : MonoBehaviour, IPoolObject
{
    [SerializeField] private ProjectileMotionType MotionType_Movement = ProjectileMotionType.Linear;

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

    public void Launch(Vector3 direction, float speed, float lifetime)
    {
        _moveDirection = direction.normalized;
        _moveSpeed = speed;
        _remainingLifetime = lifetime;
        _isLaunched = true;

        if (MotionType_Movement == ProjectileMotionType.Gravity && _rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = _moveDirection * _moveSpeed;
        }
    }

    private void Update()
    {
        if (!_isLaunched) return;

        if (MotionType_Movement == ProjectileMotionType.Linear)
        {
            transform.position += _moveDirection * (_moveSpeed * Time.deltaTime);
        }

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
}