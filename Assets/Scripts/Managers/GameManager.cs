using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    private GameStateMachine _stateMachine;

    public GameState CurrentState => _stateMachine.CurrentState;
    public RoundManager RoundManager { get; private set; }
    public GameObjectManager GameObjectManager { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    protected override void Awake()
    {
        base.Awake();

        if (Inst != this)
        {
            return;
        }

        InitializeStateMachine();
        InitializeRoundManager();
        InitializeGameObjectManager();
    }

    private void InitializeRoundManager()
    {
        RoundManager = new RoundManager(this);
    }

    private void InitializeGameObjectManager()
    {
        GameObjectManager = new GameObjectManager();
    }

    protected override void OnDestroy()
    {
        if (Inst != this)
        {
            return;
        }

        ReleaseGameObjectManager();
        ReleaseRoundManager();
        ReleaseStateMachine();

        base.OnDestroy();
    }

    private void ReleaseRoundManager()
    {
        RoundManager = null;
    }

    private void ReleaseGameObjectManager()
    {
        if (GameObjectManager == null)
        {
            return;
        }

        GameObjectManager.DestroyAllObjects();
        GameObjectManager = null;
    }

    public bool TryChangeGameState(GameState nextState)
    {
        if (_stateMachine == null)
        {
            return false;
        }

        return _stateMachine.TryChangeState(nextState);
    }

    public bool CanChangeGameState(GameState nextState)
    {
        if (_stateMachine == null)
        {
            return false;
        }

        return _stateMachine.CanChangeState(nextState);
    }

    private void InitializeStateMachine()
    {
        IReadOnlyList<GameStateTransition> transitions = DefaultGameFlowProvider.CreateTransitions();

        _stateMachine = new GameStateMachine(GameState.None, transitions);

        _stateMachine.OnStateChanged += HandleGameStateChanged;
    }

    private void ReleaseStateMachine()
    {
        if (_stateMachine == null)
        {
            return;
        }

        _stateMachine.OnStateChanged -= HandleGameStateChanged;
        _stateMachine = null;
    }

    private void HandleGameStateChanged(GameState gameState)
    {
        OnGameStateChanged?.Invoke(gameState);
    }

    public void RespawnPlayer(GameObject playerObj)
    {
        if (playerObj == null)
        {
            return;
        }

        Vector3 respawnPosition = MapManager.Inst.CurrentSpawnPosition;

        playerObj.transform.position = respawnPosition;

        if (playerObj.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.linearVelocity = Vector3.zero;
        }
    }
}