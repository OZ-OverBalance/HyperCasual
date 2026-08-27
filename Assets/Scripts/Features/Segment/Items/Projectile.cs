using UnityEngine;

public enum ProjectileMotionType
{
    Linear,
    Gravity
}

public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileMotionType MotionType_Movement = ProjectileMotionType.Linear;
    [SerializeField] private LayerMask LayerMask_BlockedBy;

    private Vector3 _moveDirection;
    private float _moveSpeed;
    private float _remainingLifetime;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        TryGetComponent(out _rigidbody);
    }

    public void Launch(Vector3 direction, float speed, float lifetime)
    {
        _moveDirection = direction.normalized;
        _moveSpeed = speed;
        _remainingLifetime = lifetime;

        if (MotionType_Movement == ProjectileMotionType.Gravity && _rigidbody != null)
        {
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = _moveDirection * _moveSpeed;
        }
    }

    private void Update()
    {
        if (MotionType_Movement == ProjectileMotionType.Linear)
        {
            transform.position += _moveDirection * (_moveSpeed * Time.deltaTime);
        }

        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, LayerMask_BlockedBy))
        {
            Destroy(gameObject);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}