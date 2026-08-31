using UnityEngine;
using Cysharp.Threading.Tasks;

public class VisualSpring : MonoBehaviour
{
    [SerializeField] private ObstacleSpring Spring_Logic;
    [SerializeField] private Transform Transform_SpringAndPad;
    [SerializeField] private float CompressDistance = 0.2f;
    [SerializeField] private float BounceDuration = 0.25f;

    private Vector3 _restLocalPosition;

    private void Awake()
    {
        if (Transform_SpringAndPad != null)
        {
            _restLocalPosition = Transform_SpringAndPad.localPosition;
        }
    }

    private void OnEnable()
    {
        if (Spring_Logic != null)
        {
            Spring_Logic.OnBounced += HandleBounced;
        }
    }

    private void OnDisable()
    {
        if (Spring_Logic != null)
        {
            Spring_Logic.OnBounced -= HandleBounced;
        }
    }

    private void HandleBounced()
    {
        Debug.Log("바운스감지됨ㅇㅇㅇㅇㅇ");
        BounceAnimationAsync().Forget();
    }

    private async UniTaskVoid BounceAnimationAsync()
    {
        if (Transform_SpringAndPad == null) return;

        float elapsed = 0f;
        float halfDuration = BounceDuration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            float y = Mathf.Lerp(_restLocalPosition.y, _restLocalPosition.y - CompressDistance, progress);
            Transform_SpringAndPad.localPosition = new Vector3(_restLocalPosition.x, y, _restLocalPosition.z);
            await UniTask.Yield();
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            float y = Mathf.Lerp(_restLocalPosition.y - CompressDistance, _restLocalPosition.y, progress);
            Transform_SpringAndPad.localPosition = new Vector3(_restLocalPosition.x, y, _restLocalPosition.z);
            await UniTask.Yield();
        }

        Transform_SpringAndPad.localPosition = _restLocalPosition;
    }
}