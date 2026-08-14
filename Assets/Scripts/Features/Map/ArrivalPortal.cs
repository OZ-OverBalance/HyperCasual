using UnityEngine;

public class ArrivalPortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Player"))
        {
            return;
        }

        if (collider.TryGetComponent(out GameObjectInstance instance))
        {
            var gameObjectManager = GameManager.Inst.GameObjectManager;

            gameObjectManager.TryDestroyObject(instance.InstanceId);
        }
    }
}
