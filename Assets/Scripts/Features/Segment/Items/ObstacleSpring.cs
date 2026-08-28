using System;
using Unity.Netcode;
using UnityEngine;

public class ObstacleSpring : NetworkTriggerBase
{
    [SerializeField] private float BounceForce = 15f;
    [SerializeField] private float ReboundCooldown = 0.3f;

    public event Action OnBounced;

    private float _lastBounceTime = -999f;

    private void Bounce(Collider playerCollider)
    {
        if (playerCollider.TryGetComponent(out Rigidbody rb))
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = BounceForce;
            rb.linearVelocity = velocity;
        }

        OnBounced?.Invoke();
    }

    protected override void OnPlayerTriggered(Collider other)
    {
        if (!IsServer) return;

        if (Time.time - _lastBounceTime < ReboundCooldown) return;
        _lastBounceTime = Time.time;

        if (other.TryGetComponent(out NetworkObject playerNetObj))
        {
            TriggerClientRpc(playerNetObj.NetworkObjectId);
        }
    }

    [ClientRpc]
    protected override void TriggerClientRpc()
    {

    }

    [ClientRpc]
    private void TriggerClientRpc(ulong playerNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerObj))
        {
            Bounce(playerObj.GetComponent<Collider>());
        }
    }
}