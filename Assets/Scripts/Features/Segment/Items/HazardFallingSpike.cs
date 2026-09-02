using Unity.Netcode;
using UnityEngine;

public enum FallingSpikeState
{
    Idle,
    Falling,
    Grounded,
    Rising
}

public class HazardFallingSpike : ObstacleBase
{
    [Header("참조")]
    [SerializeField] private Transform Transform_Spike;
    [SerializeField] private Collider Collider_Spike;

    [Header("주기")]
    [SerializeField] private float IdleDuration = 1.5f;
    [SerializeField] private float GroundedDuration = 0.3f;
    [SerializeField] private float RiseDuration = 1.0f;

    [Header("낙하")]
    [SerializeField] private float FallDistance = 3f;
    [SerializeField] private float FallAcceleration = 40f;

    [Header("바닥 감지")]
    [SerializeField] private LayerMask LayerMask_Ground;
    [SerializeField] private float RaycastStartOffset = 0.1f;

    [Header("기둥")]
    [SerializeField] private Transform Transform_PillarRoot;

    private FallingSpikeState _state = FallingSpikeState.Idle;
    private double _elapsedTime;
    private double _globalStartTime;
    private double _stateStartTime;

    private float _fallVelocity;
    private float _currentDrop;
    private float _actualFallDistance;

    private Vector3 _restLocalPosition;

    private void Awake()
    {
        if (Transform_Spike != null)
        {
            _restLocalPosition = Transform_Spike.localPosition;
        }

        if (Transform_PillarRoot != null)
        {
            Transform_PillarRoot.gameObject.SetActive(false); 
        }
    }

    protected override void OnObstacleStarted()
    {
        if (NetCodeObstacleManager.Instance != null)
        {
            _globalStartTime = NetCodeObstacleManager.Instance.GlobalStartTime.Value;
        }

        _state = FallingSpikeState.Idle;
        _stateStartTime = 0;
        _currentDrop = 0f;
        _fallVelocity = 0f;
        _actualFallDistance = FallDistance;

        UpdateSpikeColliderState();
    }

    protected override void OnObstacleUpdate()
    {
        _elapsedTime = NetworkManager.Singleton.ServerTime.Time - _globalStartTime;
        if (_elapsedTime < 0) _elapsedTime = 0;

        UpdateStateMachine();
        UpdateSpikePosition();
    }

    private void UpdateStateMachine()
    {
        double timeSinceStateStart = _elapsedTime - _stateStartTime;

        switch (_state)
        {
            case FallingSpikeState.Idle:
                if (timeSinceStateStart >= IdleDuration) ChangeState(FallingSpikeState.Falling);
                break;

            case FallingSpikeState.Falling:
                if (_currentDrop >= _actualFallDistance) ChangeState(FallingSpikeState.Grounded);
                break;

            case FallingSpikeState.Grounded:
                if (timeSinceStateStart >= GroundedDuration) ChangeState(FallingSpikeState.Rising);
                break;

            case FallingSpikeState.Rising:
                if (timeSinceStateStart >= RiseDuration) ChangeState(FallingSpikeState.Idle);
                break;
        }
    }

    private void ChangeState(FallingSpikeState newState)
    {
        _state = newState;
        _stateStartTime = _elapsedTime;

        if (newState == FallingSpikeState.Falling)
        {
            _fallVelocity = 0f;
            _actualFallDistance = CalculateActualFallDistance();
        }

        UpdateSpikeColliderState();
    }

    private float CalculateActualFallDistance()
    {
        if (Transform_Spike == null) return FallDistance;

        Vector3 rayOrigin = Transform_Spike.position + Vector3.up * RaycastStartOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, FallDistance + RaycastStartOffset, LayerMask_Ground))
        {
            float groundDistance = hit.distance - RaycastStartOffset;
            return Mathf.Min(FallDistance, Mathf.Max(0f, groundDistance));
        }

        return FallDistance;
    }

    private void UpdateSpikePosition()
    {
        if (_state == FallingSpikeState.Falling)
        {
            _fallVelocity += FallAcceleration * Time.deltaTime;
            _currentDrop += _fallVelocity * Time.deltaTime;
            _currentDrop = Mathf.Min(_currentDrop, _actualFallDistance);
        }
        else if (_state == FallingSpikeState.Rising)
        {
            double timeSinceStateStart = _elapsedTime - _stateStartTime;
            float progress = Mathf.Clamp01((float)(timeSinceStateStart / RiseDuration));
            _currentDrop = Mathf.Lerp(_actualFallDistance, 0f, progress);
        }
        else if (_state == FallingSpikeState.Idle)
        {
            _currentDrop = 0f;
        }
        else if (_state == FallingSpikeState.Grounded)
        {
            _currentDrop = _actualFallDistance;
        }

        if (Transform_Spike != null)
        {
            Transform_Spike.localPosition = _restLocalPosition + Vector3.down * _currentDrop;
        }

        UpdatePillar();
    }

    private void UpdateSpikeColliderState()
    {
        if (Collider_Spike == null) return;

        bool isDangerous = _state == FallingSpikeState.Falling || _state == FallingSpikeState.Grounded;
        Collider_Spike.enabled = isDangerous;
    }

    private void UpdatePillar()
    {
        if (Transform_PillarRoot == null) return;
        if (FallDistance <= 0f) return;

        if (_state == FallingSpikeState.Idle)
        {
            Transform_PillarRoot.gameObject.SetActive(false);
            return;
        }

        Transform_PillarRoot.gameObject.SetActive(true);

        Vector3 scale = Transform_PillarRoot.localScale;
        scale.y = _currentDrop / FallDistance;
        Transform_PillarRoot.localScale = scale;
    }

    protected override void OnObstacleStopped()
    {
        _state = FallingSpikeState.Idle;
        _currentDrop = 0f;
        _fallVelocity = 0f;

        if (Transform_Spike != null)
        {
            Transform_Spike.localPosition = _restLocalPosition;
        }

        UpdateSpikeColliderState();
    }
}