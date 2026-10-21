using UnityEngine;

public class AudioListenerManager : SingletonBase<AudioListenerManager>
{
    private Transform TargetPlayer;

    protected override void Awake()
    {
        base.Awake();

        if (Inst != this)
        {
            return;
        }
    }

    private void LateUpdate()
    {
        if (TargetPlayer == null)
        {
            if (Camera.main != null)
            {
                transform.position = Camera.main.transform.position;
            }

            return;
        }

        transform.position = TargetPlayer.position;
    }

    public void SetTarget(Transform playerTransform)
    {
        TargetPlayer = playerTransform;
    }
}
