using UnityEngine;

public enum SwingMode
{
    Oscillate,
    FullRotate
}

public class HazardSwing : MonoBehaviour
{
    [SerializeField] private SwingMode Mode_Swing = SwingMode.Oscillate;

    [Header("Oscillate 전용 - 왕복 폭")]
    [SerializeField] private float MaxSwingAngle = 45f;

    [Header("공통 - 속도")]
    [SerializeField] private float SwingSpeed = 1f;

    //private bool _isActive;
    private float _elapsedTime;

    //private void OnEnable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested += HandleActiveAll;
    //}

    //private void OnDisable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested -= HandleActiveAll;
    //}

    //private void HandleActiveAll()
    //{
    //    _isActive = true;
    //    _elapsedTime = 0f;
    //}

    private void Update()
    {
        //if (!_isActive) return;

        _elapsedTime += Time.deltaTime * SwingSpeed;

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

    private void UpdateOscillate()
    {
        float angle = Mathf.Sin(_elapsedTime) * MaxSwingAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateFullRotate()
    {
        float angle = _elapsedTime * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}