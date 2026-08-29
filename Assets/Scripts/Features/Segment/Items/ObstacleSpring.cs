using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ObstacleSpring : NetworkTriggerBase
{
    [SerializeField] private float BounceForce = 15f;
    [SerializeField] private float ReboundCooldown = 0.3f;

    public event Action OnBounced;

    private float _lastBounceTime = -999f;

    private void Bounce(Rigidbody rb)
    {
        if (rb != null)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = BounceForce;
            rb.linearVelocity = velocity;
        }
    }

    protected override void OnPlayerTriggered(Collider other)
    {
       // if (Time.time - _lastBounceTime < ReboundCooldown) return;
       // _lastBounceTime = Time.time;


    }



    protected override void OnPlayerTriggeredForLocal(Collider other)
    {
        if (other.TryGetComponent<Rigidbody>(out var rb))
        {
            RequestSpringEffectServerRpc();
            Bounce(rb);
        }
        else
        {
            Debug.Log("몬가져옴...");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpringEffectServerRpc()
    {
        TriggerClientRpc();
    }

    [ClientRpc]
    protected override void TriggerClientRpc()
    {
        OnBounced?.Invoke();
    }
}