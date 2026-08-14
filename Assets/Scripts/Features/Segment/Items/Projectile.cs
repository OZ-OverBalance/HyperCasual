using UnityEngine;

public enum ProjectileMotionType
{
    Linear,   
    Gravity  
}

public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileMotionType MotionType_Movement = ProjectileMotionType.Linear;

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
            Object.Destroy(gameObject);
        }
    }
}