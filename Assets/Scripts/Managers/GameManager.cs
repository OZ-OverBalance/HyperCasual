using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    private GameStateMachine _stateMachine;

    public GameState CurrentState => _stateMachine.CurrentState;
    public RoundManager RoundManager { get; private set; }
    public BuildPhaseManager BuildPhaseManager{ get; private set; }
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
        InitializeBuildPhaseManager();

        Application.targetFrameRate = 60;
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

        ReleaseBuildPhaseManager();
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

    private void InitializeBuildPhaseManager()
    {
        BuildPhaseManager = new BuildPhaseManager(this);
    }

    private void ReleaseBuildPhaseManager()
    {
        if (BuildPhaseManager == null)
        {
            return;
        }

        BuildPhaseManager.Release();
        BuildPhaseManager = null;
    }

    public bool TryChangeGameState(GameState nextState)
    {
        if (_stateMachine == null)
        {
            Debug.LogError("[GameManager] StateMachine이 초기화되지 않았습니다.");
            return false;
        }

        Debug.Log(
            $"[GameManager] 상태 변경 요청: {CurrentState} → {nextState}\n" +
            $"호출 경로:\n{System.Environment.StackTrace}");

        bool isChanged = _stateMachine.TryChangeState(nextState);

        Debug.Log(
            $"[GameManager] 상태 변경 결과: {isChanged}, " +
            $"현재 상태: {CurrentState}");

        return isChanged;
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

    public void RespawnPlayer(GameObject playerObj, Vector3? targetPosition = null)
    {
        if (playerObj == null) return;

        Vector3 finalSpawnPosition;

        // 1. 체크포인트 좌표가 전달된 경우 (사망 후 부활)
        if (targetPosition.HasValue)
        {
            finalSpawnPosition = targetPosition.Value;
        }
        // 2. 전달된 좌표가 없는 경우 (게임 시작/라운드 초기 스폰) -> MapManager 기본 스폰 위치 + 플레이어별 오프셋
        else
        {
            Vector3 baseSpawnPos = MapManager.Inst != null
                ? MapManager.Inst.CurrentSpawnPosition
                : Vector3.zero;

            float spawnOffset = 0f;

            if (playerObj.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
            {
                ulong clientId = netObj.OwnerClientId;
                var roomManager = NetCodeRoomManager.Instance;

                if (roomManager != null)
                {
                    for (int i = 0; i < roomManager.PlayerList.Count; i++)
                    {
                        if (roomManager.PlayerList[i].ClientId == clientId)
                        {
                            spawnOffset = i * 1.5f;
                            break;
                        }
                    }
                }
                else
                {
                    spawnOffset = clientId * 1.5f;
                }
            }

            finalSpawnPosition = baseSpawnPos + new Vector3(spawnOffset, 0f, 0f);
        }

        playerObj.transform.position = finalSpawnPosition;

        if (playerObj.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.linearVelocity = Vector3.zero;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}