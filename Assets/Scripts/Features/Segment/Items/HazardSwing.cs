using Unity.Netcode;
using UnityEngine;

public enum SwingMode
{
    Oscillate,
    FullRotate
}

public class HazardSwing : ObstacleBase
{
    [SerializeField] private SwingMode Mode_Swing = SwingMode.Oscillate;

    [Header("Oscillate 전용 - 왕복 폭")]
    [SerializeField] private float MaxSwingAngle = 45f;

    [Header("공통 - 속도")]
    [SerializeField] private float SwingSpeed = 1f;

    private Vector3 _startPosition;
    private double _elapsedTime;
    private double _globalStartTime;



    private void UpdateOscillate()
    {
        float angle = Mathf.Sin((float)_elapsedTime * SwingSpeed) * MaxSwingAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateFullRotate()
    {
        float angle = (float)_elapsedTime * SwingSpeed * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    protected override void OnObstacleStarted()
    {
        _startPosition = transform.position;

        if (NetCodeObstacleManager.Instance != null)
        {
            _globalStartTime = NetCodeObstacleManager.Instance.GlobalStartTime.Value;
        }
    }

    protected override void OnObstacleUpdate()
    {
        _elapsedTime = NetworkManager.Singleton.ServerTime.Time - _globalStartTime;
        if (_elapsedTime < 0) _elapsedTime = 0;

        switch (Mode_Swing)
        {
            case SwingMode.Oscillate:
                UpdateOscillate();
                break;
            case SwingMode.FullRotate:
                UpdateFullRotate();
                break;
        }
    }

    protected override void OnObstacleStopped()
    {
        transform.position = _startPosition;
    }
}