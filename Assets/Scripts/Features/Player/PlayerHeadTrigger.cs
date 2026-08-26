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

            bool isNotJumpingUp = incomingPlayer.GetVelocity().y < 1.0f;
            bool isAbove = incomingPlayer.transform.position.y >= (transform.position.y - 0.3f);

            if (isNotJumpingUp && isAbove)
            {
                Vector3 correctedPos = incomingPlayer.transform.position;
                if (correctedPos.y < transform.position.y + 0.1f)
                {
                    correctedPos.y = transform.position.y + 0.1f;
                    incomingPlayer.transform.position = correctedPos;
                }

                ownerPlayer.GetStomped();
                incomingPlayer.BounceFromHead();
            }
        }
    }
}