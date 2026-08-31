using UnityEngine;

public class VisualSwing : MonoBehaviour
{
    [SerializeField] private ParticleSystem ParticleSystem_Hit;
    [SerializeField] private float HitCooldown = 0.2f; 

    private float _cooldownTimer;

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[VisualSwing] OnTriggerEnter: " + other.gameObject.name);
        TryPlayHitEffect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPlayHitEffect(other);
    }

    private void TryPlayHitEffect(Collider other)
    {
        if (_cooldownTimer > 0f) return;
        if (ParticleSystem_Hit == null)
        {
            Debug.Log("[VisualSwing] ParticleSystem_Hit이 연결 안 됨");
            return;
        }

        _cooldownTimer = HitCooldown;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Debug.Log("[VisualSwing] 타격 이펙트 재생, hitPoint=" + hitPoint);

        ParticleSystem_Hit.transform.position = hitPoint;
        ParticleSystem_Hit.Play();
    }
}