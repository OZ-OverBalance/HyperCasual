using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class ObstacleBomb : MonoBehaviour
{
    [SerializeField] private float FuseDuration = 0.6f; 
    [SerializeField] private float Lifetime = 1f;

    public float FuseDurationValue => FuseDuration;

    public event Action OnFuseStarted;
    public event Action OnDetonated;

    private void Start()
    {
        DetonateSequenceAsync().Forget();
    }

    private async UniTaskVoid DetonateSequenceAsync()
    {
        OnFuseStarted?.Invoke();

        await UniTask.Delay(TimeSpan.FromSeconds(FuseDuration));

        OnDetonated?.Invoke();

        Destroy(gameObject, Lifetime);
    }
}