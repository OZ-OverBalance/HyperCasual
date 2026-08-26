using UnityEngine;

public class PlayerHeadTrigger : MonoBehaviour
{
    [SerializeField] private PlayerController ownerPlayer;

    private void Awake()
    {
        if (ownerPlayer == null)
        {
            ownerPlayer = GetComponentInParent<PlayerController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController incomingPlayer = other.GetComponent<PlayerController>();
        if (incomingPlayer == null)
        {
            incomingPlayer = other.GetComponentInParent<PlayerController>();
        }

        if (incomingPlayer != null)
        {
            if (incomingPlayer == ownerPlayer || incomingPlayer.IsDead)
            {
                return;
            }

            bool isFalling = incomingPlayer.GetVelocity().y < -0.1f;

            bool isAbove = incomingPlayer.transform.position.y > transform.position.y;

            if (isFalling && isAbove)
            {
                ownerPlayer.GetStomped();
                incomingPlayer.BounceFromHead();
            }
        }
    }
}