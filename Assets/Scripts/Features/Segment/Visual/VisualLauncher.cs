using UnityEngine;

public class VisualLauncher : MonoBehaviour
{
    [SerializeField] private HazardLauncher Launcher_Logic;
    [SerializeField] private Transform Transform_Warmup;
    [SerializeField] private float WarmupScaleMultiplier = 1.2f;
    [SerializeField] private float WarmupDuration = 0.2f;
    [SerializeField] private AnimationCurve WarmupCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f);

    [Header("발사 연기")]
    [SerializeField] private ParticleSystem ParticleSystem_Smoke;

    private Vector3 _originalScale;
    private float _warmupTimer;
    private bool _isWarmingUp;

    private void Awake()
    {
        if (Transform_Warmup != null)
        {
            _originalScale = Transform_Warmup.localScale;
        }
    }

    private void OnEnable()
    {
        if (Launcher_Logic != null)
        {
            Launcher_Logic.OnFireWarmupStart += HandleWarmupStart;
        }
    }

    private void OnDisable()
    {
        if (Launcher_Logic != null)
        {
            Launcher_Logic.OnFireWarmupStart -= HandleWarmupStart;
        }
    }

    private void HandleWarmupStart()
    {
        _isWarmingUp = true;
        _warmupTimer = 0f;

        if (ParticleSystem_Smoke != null)
        {
            ParticleSystem_Smoke.Play();
        }
    }

    private void Update()
    {
        if (!_isWarmingUp || Transform_Warmup == null) return;

        _warmupTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_warmupTimer / WarmupDuration);

        float pulse = WarmupCurve.Evaluate(progress);
        float scaleFactor = 1f + (WarmupScaleMultiplier - 1f) * pulse;

        Transform_Warmup.localScale = _originalScale * scaleFactor;

        if (progress >= 1f)
        {
            _isWarmingUp = false;
            Transform_Warmup.localScale = _originalScale;
        }
    }
}