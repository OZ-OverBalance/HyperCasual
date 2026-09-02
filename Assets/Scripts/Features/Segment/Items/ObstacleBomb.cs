using System;
using UnityEngine;

public class ObstacleBomb : MonoBehaviour
{
    [SerializeField] private float Lifetime = 1f;

    public event Action OnDetonated;

    private void Start()
    {
        OnDetonated?.Invoke();
        Destroy(gameObject, Lifetime);
    }
}