using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ObstacleCrumbling : MonoBehaviour
{
    [SerializeField] private bool CanRespawn = true;
    [SerializeField] private float DelayBeforeFall = 0.5f;
    [SerializeField] private float RespawnDelay = 3f;
    [SerializeField] private string Tag_Player = "Player";
    [SerializeField] private GameObject StonesRoot;

    //private bool _isActive;
    private bool _isTriggered;

    //private void OnEnable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested += HandleActivateAll;
    //}

    //private void OnDisable()
    //{
    //    HazardActivationSignal.OnActivateAllRequested -= HandleActivateAll;
    //}

    //private void HandleActivateAll()
    //{
    //    _isActive = true;
    //}

    private void OnTriggerEnter(Collider other)
    {
        //if (!_isActive) return;
        if (_isTriggered) return;
        if (!other.CompareTag(Tag_Player)) return;

        _isTriggered = true;
        CrumbleSequenceAsync().Forget();
    }

    private async UniTaskVoid CrumbleSequenceAsync()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(DelayBeforeFall));

        SetSolid(false);

        if (!CanRespawn) return;

        await UniTask.Delay(TimeSpan.FromSeconds(RespawnDelay));

        SetSolid(true);
        _isTriggered = false;
    }

    private void SetSolid(bool isSolid)
    {
        if (StonesRoot != null)
        {
            StonesRoot.SetActive(isSolid);
        }
    }
}