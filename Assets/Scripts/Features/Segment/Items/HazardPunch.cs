using System;
using Unity.Netcode;
using UnityEngine;

public enum PunchState
{
    Idle,
    Extending,
    Holding,
    Retracting
}

public class HazardPunch : ObstacleBase
{
    [Header("참조")]
    [SerializeField] private Transform Transform_Fist;
    [SerializeField] private Collider Collider_Fist;

    [Header("레이어 전환")]
    [SerializeField] private LayerMask LayerMask_Knockback;
    [SerializeField] private LayerMask LayerMask_Ground;

    [Header("펀치 주기")]
    [SerializeField] private float IdleDuration = 1.5f;
    [SerializeField] private float ExtendDuration = 0.15f;
    [SerializeField] private float HoldDuration = 0.1f;
    [SerializeField] private float RetractDuration = 0.3f;

    [Header("거리")]
    [SerializeField] private float PunchDistance = 1.5f;
    [SerializeField] private Vector3 LocalPunchDirection = Vector3.up;

    [Header("스프링 감쇠")]
    [SerializeField] private float SpringStiffness = 400f;
    [SerializeField] private float SpringDamping = 12f;

    public event Action<float, float> OnExtensionChanged;

    private PunchState _state = PunchState.Idle;
    private double _elapsedTime;
    private double _globalStartTime;
    private double _stateStartTime;

    private float _springVelocity;
    private float _currentExtension;

    private Vector3 _restLocalPosition;

    private void Awake()
    {
        if (Transform_Fist != null)
        {
            _restLocalPosition = Transform_Fist.localPosition;
        }
    }

    protected override void OnObstacleStarted()
    {
        if (NetCodeObstacleManager.Instance != null)
        {
            _globalStartTime = NetCodeObstacleManager.Instance.GlobalStartTime.Value;
        }

        _state = PunchState.Idle;
        _stateStartTime = 0;
        UpdateFistColliderState();
    }

    protected override void OnObstacleUpdate()
    {
        _elapsedTime = NetworkManager.Singleton.ServerTime.Time - _globalStartTime;
        if (_elapsedTime < 0) _elapsedTime = 0;

        UpdateStateMachine();
        UpdateFistPosition();
    }

    private void UpdateStateMachine()
    {
        double timeSinceStateStart = _elapsedTime - _stateStartTime;

        switch (_state)
        {
            case PunchState.Idle:
                if (timeSinceStateStart >= IdleDuration) ChangeState(PunchState.Extending);
                break;
            case PunchState.Extending:
                if (timeSinceStateStart >= ExtendDuration) ChangeState(PunchState.Holding);
                break;
            case PunchState.Holding:
                if (timeSinceStateStart >= HoldDuration) ChangeState(PunchState.Retracting);
                break;
            case PunchState.Retracting:
                if (timeSinceStateStart >= RetractDuration) ChangeState(PunchState.Idle);
                break;
        }
    }

    private void ChangeState(PunchState newState)
    {
        _state = newState;
        _stateStartTime = _elapsedTime;

        UpdateFistColliderState();
    }

    private void UpdateFistColliderState()
    {
        if (Collider_Fist == null) return;

        bool isPunching = _state == PunchState.Extending || _state == PunchState.Holding;

        int targetLayer = isPunching ? GetLayerFromMask(LayerMask_Knockback) : GetLayerFromMask(LayerMask_Ground);
        Collider_Fist.gameObject.layer = targetLayer;
    }

    private int GetLayerFromMask(LayerMask mask)
    {
        int layerValue = mask.value;
        int layerIndex = 0;

        while (layerValue > 1)
        {
            layerValue >>= 1;
            layerIndex++;
        }

        return layerIndex;
    }

    private void UpdateFistPosition()
    {
        float target = (_state == PunchState.Extending || _state == PunchState.Holding) ? PunchDistance : 0f;

        float force = (target - _currentExtension) * SpringStiffness;
        _springVelocity += force * Time.deltaTime;
        _springVelocity *= Mathf.Clamp01(1f - SpringDamping * Time.deltaTime);
        _currentExtension += _springVelocity * Time.deltaTime;

        if (Transform_Fist != null)
        {
            Transform_Fist.localPosition = _restLocalPosition + LocalPunchDirection.normalized * _currentExtension;
        }

        OnExtensionChanged?.Invoke(_currentExtension, PunchDistance);
    }

    protected override void OnObstacleStopped()
    {
        _state = PunchState.Idle;
        _currentExtension = 0f;
        _springVelocity = 0f;

        if (Transform_Fist != null)
        {
            Transform_Fist.localPosition = _restLocalPosition;
        }

        UpdateFistColliderState();
    }
}