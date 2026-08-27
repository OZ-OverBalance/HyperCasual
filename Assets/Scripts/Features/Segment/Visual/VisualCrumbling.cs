using UnityEngine;

public class VisualCrumbling : MonoBehaviour
{
    [SerializeField] private ObstacleCrumbling Crumbling_Logic;
    [SerializeField] private GameObject StonesRoot; 
    [SerializeField] private float ShakeIntensity = 0.05f;
    [SerializeField] private ParticleSystem ParticleSystem_Break;

    private void OnEnable()
    {
        if (Crumbling_Logic != null)
        {
            Crumbling_Logic.OnShakeStarted += HandleShakeStarted;
            Crumbling_Logic.OnBroken += HandleBroken;
        }
    }

    private void OnDisable()
    {
        if (Crumbling_Logic != null)
        {
            Crumbling_Logic.OnShakeStarted -= HandleShakeStarted;
            Crumbling_Logic.OnBroken -= HandleBroken;
        }
    }

    private void HandleShakeStarted(float duration)
    {
        ShakeAsync(duration).Forget();
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid ShakeAsync(float duration)
    {
        if (StonesRoot == null) return;

        Vector3 originalLocalPos = StonesRoot.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / duration;
            float currentIntensity = ShakeIntensity * progress;

            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f) * currentIntensity;

            StonesRoot.transform.localPosition = originalLocalPos + shakeOffset;

            await Cysharp.Threading.Tasks.UniTask.Yield();
        }

        StonesRoot.transform.localPosition = originalLocalPos;
    }

    private void HandleBroken()
    {
        if (ParticleSystem_Break != null)
        {
            ParticleSystem_Break.Play();
        }
    }
}