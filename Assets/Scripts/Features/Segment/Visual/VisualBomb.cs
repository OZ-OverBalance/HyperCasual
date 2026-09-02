using UnityEngine;

public class VisualBomb : MonoBehaviour
{
    [SerializeField] private ObstacleBomb Bomb_Logic;
    [SerializeField] private ParticleSystem ParticleSystem_Explosion;

    private void OnEnable()
    {
        if (Bomb_Logic != null)
        {
            Bomb_Logic.OnDetonated += HandleDetonated;
        }
    }

    private void OnDisable()
    {
        if (Bomb_Logic != null)
        {
            Bomb_Logic.OnDetonated -= HandleDetonated;
        }
    }

    private void HandleDetonated()
    {
        if (ParticleSystem_Explosion != null)
        {
            ParticleSystem_Explosion.Play();
        }
    }
}