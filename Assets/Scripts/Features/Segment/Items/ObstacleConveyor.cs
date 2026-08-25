using UnityEngine;

public enum ConveyorDirection
{
    Clockwise,     
    CounterClockwise 
}

public class ObstacleConveyor : MonoBehaviour
{
    [SerializeField] private ConveyorDirection Direction_Belt = ConveyorDirection.Clockwise;
    [SerializeField] private float BeltSpeed = 3f;
    [SerializeField] private string Tag_Player = "Player";

    //private bool _isActive;

    //private void OnEnable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested += HandleActivateAll;
    //}

    //private void OnDisable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested -= HandleActivateAll;
    //}

    //private void HandleActivateAll()
    //{
    //    _isActive = true;
    //}

    private void OnTriggerStay(Collider other)
    {
        //if (!_isActive) return;
        if (!other.CompareTag(Tag_Player)) return;

        if (other.TryGetComponent(out CharacterController controller))
        {
            Vector3 pushDirection = GetPushDirection();
            controller.Move(pushDirection * (BeltSpeed * Time.deltaTime));
        }
    }

    private Vector3 GetPushDirection()
    {
        return Direction_Belt == ConveyorDirection.Clockwise ? Vector3.right : Vector3.left;
    }
}