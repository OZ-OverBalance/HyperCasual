using UnityEngine;

public class CheckpointFlag : MonoBehaviour
{
    private bool isActivated = false;

    private void OnTriggerEnter(Collider collider)
    {
        if (isActivated || !collider.CompareTag("Player"))
        {
            return;
        }

        isActivated = true;

        if (MapManager.Inst != null)
        {
            MapManager.Inst.SetRespawnPosition(transform.position);
        }
    }

    public void ResetFlag()
    {
        isActivated = false;
    }
}
