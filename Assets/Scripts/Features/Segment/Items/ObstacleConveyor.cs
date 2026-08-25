using Unity.Netcode;
using UnityEngine;

public enum ConveyorDirection
{
    Clockwise,
    CounterClockwise
}
public enum ConveyorFace
{
    Top,
    Bottom,
    Left,
    Right
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
        if (!other.TryGetComponent(out Rigidbody rb)) return;

        Vector3 pushDirection = GetPushDirection();
        Vector3 displacement = pushDirection * (BeltSpeed * Time.deltaTime);

        rb.MovePosition(rb.position + displacement);
    }

    private Vector3 GetPushDirection()
    {
        return Direction_Belt == ConveyorDirection.Clockwise ? Vector3.right : Vector3.left;
    }
}