using UnityEngine;

public enum StretchAxis
{
    X,
    Y,
    Z
}

public class VisualPunch : MonoBehaviour
{
    [SerializeField] private HazardPunch Punch_Logic;
    [SerializeField] private Transform Transform_Spring;
    [SerializeField] private StretchAxis Axis_Stretch = StretchAxis.Y;

    private Vector3 _restSpringScale;

    private void Awake()
    {
        if (Transform_Spring != null)
        {
            _restSpringScale = Transform_Spring.localScale;
        }
    }

    private void OnEnable()
    {
        if (Punch_Logic != null)
        {
            Punch_Logic.OnExtensionChanged += HandleExtensionChanged;
        }
    }

    private void OnDisable()
    {
        if (Punch_Logic != null)
        {
            Punch_Logic.OnExtensionChanged -= HandleExtensionChanged;
        }
    }

    private void HandleExtensionChanged(float currentExtension, float maxDistance)
    {
        if (Transform_Spring == null) return;

        float stretchRatio = 1f + (currentExtension / Mathf.Max(maxDistance, 0.01f));
        Vector3 scale = _restSpringScale;

        switch (Axis_Stretch)
        {
            case StretchAxis.X:
                scale.x = _restSpringScale.x * stretchRatio;
                break;
            case StretchAxis.Y:
                scale.y = _restSpringScale.y * stretchRatio;
                break;
            case StretchAxis.Z:
                scale.z = _restSpringScale.z * stretchRatio;
                break;
        }

        Transform_Spring.localScale = scale;
    }
}