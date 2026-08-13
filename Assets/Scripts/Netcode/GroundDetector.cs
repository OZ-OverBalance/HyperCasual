using System;
using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    // 지면 여부를 체크
    public event Action<bool> GroundTriggeredEvent;

    private void OnTriggerStay(Collider other)
    {
        GroundTriggeredEvent.Invoke(true);
    }

    private void OnTriggerExit(Collider other)
    {
        GroundTriggeredEvent.Invoke(false);
    }
}
