using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;

public class ObstacleCrumbling : NetworkTriggerBase
{
    [SerializeField] private bool CanRespawn = true;
    [SerializeField] private float DelayBeforeFall = 0.5f;
    [SerializeField] private float RespawnDelay = 3f;
    [SerializeField] private string Tag_Player = "Player";
    [SerializeField] private GameObject StonesRoot;

    //private bool _isActive;
    private bool _isTriggered;

    public event Action<float> OnShakeStarted;
    public event Action OnBroken;

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


    private async UniTaskVoid CrumbleSequenceAsync()
    {
        OnShakeStarted?.Invoke(DelayBeforeFall);

        await UniTask.Delay(TimeSpan.FromSeconds(DelayBeforeFall));

        SetSolid(false);
        OnBroken?.Invoke();

        if (!CanRespawn) return;

        await UniTask.Delay(TimeSpan.FromSeconds(RespawnDelay));

        SetSolid(true);

        if(IsServer)
        {
            SetTriggered(false);
        }
    }

    private void SetSolid(bool isSolid)
    {
        if (StonesRoot != null)
        {
            StonesRoot.SetActive(isSolid);
        }
    }

    protected override void OnPlayerTriggered(Collider other)
    {
        if (!IsServer) return;

        TriggerClientRpc();
    }

    [ClientRpc]
    protected override void TriggerClientRpc()
    {
        CrumbleSequenceAsync().Forget();
    }
}