using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
public class PlayerController : NetworkBehaviour
{
    public static readonly List<PlayerController> AllPlayers = new List<PlayerController>();
    [Header("이동")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("점프")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float jumpCutMultiplier = 0.3f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("벽 점프")]
    [SerializeField] private float wallCheckForwardOffset = 0.4f;
    [SerializeField] private Vector3 wallCheckSize = new Vector3(0.5f, 1.0f, 0.5f);
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallClimbForce = 13f;
    [SerializeField] private float wallJumpHorizontalForce = 12f;
    [SerializeField] private float wallJumpControlLockTime = 0.15f;
    [SerializeField] private int maxWallClimbs = 1;

    [Header("슬라이딩")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private float crouchHeightRatio = 0.35f;

    [Header("그라운드 체크")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector3 checkSize = new Vector3(0.5f, 0.15f, 0.5f);
    [SerializeField] private LayerMask groundLayer;

    [Header("스폰 및 죽음")]
    [SerializeField] private Vector3 spawnPoint = Vector3.zero;

    [Header("밟기 및 공중 점프")]
    [SerializeField] private float headBounceForce = 14f;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private int maxAirJumps = 1;

    [Header("낙하 중력 강화")]
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    
    [Header("기믹 효과 설정")]
    [SerializeField] private float slowSpeedMultiplier = 0.5f; 
    [SerializeField] private float knockBackForce = 10f;
    [SerializeField] private float knockBackDuration = 0.4f;

    [Header("리스폰 무적 설정")]
    [SerializeField] private float respawnInvincibleDuration = 2.0f;
    [SerializeField] private float blinkInterval = 0.15f;

    [Header("이펙트 & 파티클 연출")]
    [SerializeField] private GameObject stunEffectObj;          
    [SerializeField] private ParticleSystem jumpParticle;          
    [SerializeField] private ParticleSystem landParticle;         
    [SerializeField] private ParticleSystem slideParticle;         
    [SerializeField] private ParticleSystem headBounceParticle;    
    [SerializeField] private ParticleSystem wallJumpParticle;

    private bool _isInvincible = false;
    private Renderer[] _renderers;
    private CancellationTokenSource _invincibleCts;

    public bool IsInvincible
    {
        get { return _isInvincible; }
    }

    public bool HasArrived
    {
        get { return _hasArrived; }
    }



    private float currentSpeedMultiplier = 1.0f;
    private bool _isOnSlowArea = false;
    private float knockBackTimer = 0f;

    // 네트워크 동기화 변수들
    public NetworkVariable<bool> IsStunnedNet = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isDeadNet = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isCrouchingNet = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isGroundedNet = new NetworkVariable<bool>(true);
    private NetworkVariable<bool> isTouchingWallNet = new NetworkVariable<bool>(false);
    private NetworkVariable<float> horizontalInputNet = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> isSlowedNet = new NetworkVariable<bool>(false);

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Animator anim;
    private NetworkAnimator netAnim;
    public NetworkVariable<Vector3> CurrentCheckpointNet = new NetworkVariable<Vector3>(
          Vector3.zero,
          NetworkVariableReadPermission.Everyone,
          NetworkVariableWritePermission.Server
      );

    private float horizontalInput;
    private bool isCrouching;
    private bool isGrounded;
    private bool isTouchingWall;
    private int remainingWallClimbs;
    private float wallJumpLockTimer;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private Vector3 originalModelScale;

    private int remainingAirJumps;
    private float stunTimer;

    private CancellationTokenSource _respawnCts;
    private CancellationTokenSource _stunCts;

    private bool _canControl = false;
    private bool _hasArrived = false;
    private bool _wasGroundedLastFrame = false;

    public bool IsDead
    {
        get { return isDeadNet.Value; }
    }

    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }

    private void Awake()
    {
        _respawnCts = new CancellationTokenSource();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        anim = GetComponentInChildren<Animator>();
        netAnim = GetComponent<NetworkAnimator>();

        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;

        if (characterModel == null && anim != null)
        {
            characterModel = anim.transform;
        }

        if (characterModel != null)
        {
            originalModelScale = characterModel.localScale;
        }
        else
        {
            originalModelScale = Vector3.one;
        }

        _renderers = GetComponentsInChildren<Renderer>();
    }



    public override void OnNetworkSpawn()
    {
        if (!AllPlayers.Contains(this))
        {
            AllPlayers.Add(this);
        }

        if (IsServer)
        {
            CurrentCheckpointNet.Value = Vector3.zero; 
        }

        if (!IsOwner)
        {
            rb.isKinematic = true;
        }

        if (IsOwner)
        {
            if (GameManager.Inst != null)
            {
                GameManager.Inst.RespawnPlayer(gameObject);
            }
            else
            {
                transform.position = spawnPoint;
            }
        }

        IsStunnedNet.OnValueChanged += OnStunStateChanged;

        if (GameManager.Inst != null)
        {
            GameManager.Inst.OnGameStateChanged += HandleGameStateChanged;
            UpdateControlState(GameManager.Inst.CurrentState);
        }
    }

    public override void OnNetworkDespawn()
    {
        AllPlayers.Remove(this);

        if (_stunCts != null)
        {
            _stunCts?.Cancel();
            _stunCts?.Dispose();
            _stunCts = null;
        }

        if (_respawnCts != null)
        {
            _respawnCts.Cancel();
            _respawnCts.Dispose();
            _respawnCts = null;
        }

        IsStunnedNet.OnValueChanged -= OnStunStateChanged;

        if (GameManager.Inst != null)
        {
            GameManager.Inst.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (_invincibleCts != null)
        {
            _invincibleCts.Cancel();
            _invincibleCts.Dispose();
            _invincibleCts = null;
        }
    }

    private void Update()
    {
        if (knockBackTimer > 0f)
        {
            knockBackTimer -= Time.deltaTime;
        }

        if (!IsOwner)
        {
            SyncAnimationsFromNetwork();
            return;
        }

        if (!_canControl || isDeadNet.Value || IsPlayingLanding() || IsStunnedNet.Value == true || knockBackTimer > 0f)
        {
            horizontalInput = 0f;
            UpdateAnimation();
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        currentSpeedMultiplier = _isOnSlowArea ? slowSpeedMultiplier : 1.0f;

        if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)))
        {
            OnEmoteButtonPressed(1);
        }
        else if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)))
        {
            RotateToCamera();
            OnEmoteButtonPressed(2);
        }
        else if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
        {
            RotateToCamera();
            OnEmoteButtonPressed(3);
        }
        else if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)))
        {
            RotateToCamera();
            OnEmoteButtonPressed(4);
        }
        else if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)))
        {
            RotateToCamera();
            OnEmoteButtonPressed(5);
        }
        else if (IsPlayingEmote() && (horizontalInput != 0 || Input.GetKeyDown(KeyCode.Space)))
        {
            Debug.Log("cancelEmoteeeeeeeeeeee");
            if (netAnim != null) netAnim.SetTrigger("cancelEmote");
        }

        if (wallJumpLockTimer > 0)
        {
            wallJumpLockTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, 0f);
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            remainingWallClimbs = maxWallClimbs;
            remainingAirJumps = maxAirJumps;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        bool isCrouchKeyPressed = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        if (isCrouchKeyPressed && isGrounded && !isCrouching)
        {
            StartCookieRunCrouch();
        }
        else if (!isCrouchKeyPressed && isCrouching)
        {
            StopCookieRunCrouch();
        }

        HandleRotation();
        UpdateAnimation();

        UpdateStateServerRpc(horizontalInput, isGrounded, isCrouching, isTouchingWall, _isOnSlowArea);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (!_canControl)
        {
            ApplyMovement();
            return;
        }

        CheckGround();
        CheckWall();
        ApplyMovement();
        ApplyExtraFallGravity();

        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                ExecuteJump();
            }
            else if (!isGrounded && remainingAirJumps > 0) 
            {
                ExecuteAirJump();
            }
            else if (!isGrounded && isTouchingWall && remainingWallClimbs > 0)
            {
                ExecuteWallClimb();
            }
        }
    }

    private void ApplyExtraFallGravity()
    {
        if (!isGrounded && rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector3.up * (Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
        }
    }

    private void ApplyMovement()
    {
        if (!_canControl || isDeadNet.Value || IsStunnedNet.Value || knockBackTimer > 0f) 
        {
            if (knockBackTimer > 0f) return;

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (IsPlayingEmote() && horizontalInput == 0)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (wallJumpLockTimer <= 0)
        {
            float currentMoveSpeed = moveSpeed * currentSpeedMultiplier;
            rb.linearVelocity = new Vector3(horizontalInput * currentMoveSpeed, rb.linearVelocity.y, 0f);
        }
    }
    private void HandleGameStateChanged(GameState newState)
    {
        UpdateControlState(newState);
    }
    private void UpdateControlState(GameState state)
    {
        _canControl = (state == GameState.Run);

        if (!_canControl)
        {
            horizontalInput = 0f;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }

    private void ExecuteJump()
    {
        if (isCrouching) StopCookieRunCrouch();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);

        if (netAnim != null) netAnim.SetTrigger("doJump");

        PlayJumpEffectRpc();

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void ExecuteWallClimb()
    {
        if (isCrouching) StopCookieRunCrouch();

        float pushDirection = transform.forward.x > 0 ? -1f : 1f;

        rb.linearVelocity = new Vector3(pushDirection * wallJumpHorizontalForce, wallClimbForce, 0f);
        transform.rotation = Quaternion.Euler(0f, pushDirection > 0 ? 90f : -90f, 0f);

        wallJumpLockTimer = wallJumpControlLockTime;

        if (netAnim != null) netAnim.SetTrigger("doWallJump");
        PlayWallJumpEffectRpc();

        remainingWallClimbs--;
        jumpBufferTimer = 0f;
    }

    [ServerRpc]
    private void RequestWallJumpServerRpc()
    {
        if (anim != null) anim.SetTrigger("doWallJump");
    }

    private void StartCookieRunCrouch()
    {
        if (isCrouching || !isGrounded) return;
        isCrouching = true;

        if (slideParticle != null) slideParticle.Play();
        float newHeight = originalColliderHeight * crouchHeightRatio;
        float yOffset = -(originalColliderHeight - newHeight) * 0.5f;

        capsuleCollider.height = newHeight;
        capsuleCollider.center = new Vector3(
            originalColliderCenter.x,
            originalColliderCenter.y + yOffset,
            originalColliderCenter.z
        );
    }

    private void StopCookieRunCrouch()
    {
        if (!isCrouching) return;

        float newHeight = originalColliderHeight * crouchHeightRatio;
        Vector3 rayOrigin = transform.position + Vector3.up * newHeight;
        bool headBlocked = Physics.Raycast(rayOrigin, Vector3.up, originalColliderHeight - newHeight, groundLayer);

        if (!headBlocked)
        {
            isCrouching = false;
            if (slideParticle != null) slideParticle.Stop();
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }
    }

    private void HandleRotation()
    {
        if (wallJumpLockTimer > 0 || horizontalInput == 0) return;

        if (horizontalInput > 0)
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        else if (horizontalInput < 0)
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
    }

    private void CheckGround()
    {
        if (groundCheckPoint == null) return;

        isGrounded = Physics.OverlapBox(
            groundCheckPoint.position,
            checkSize * 0.5f,
            Quaternion.identity,
            groundLayer
        ).Length > 0;

        if (isGrounded && !_wasGroundedLastFrame && rb.linearVelocity.y <= 0.1f)
        {
            PlayLandEffectRpc();
        }
        _wasGroundedLastFrame = isGrounded;
    }

    private void CheckWall()
    {
        Vector3 checkDirection = transform.forward;
        if (horizontalInput != 0)
        {
            checkDirection = new Vector3(horizontalInput, 0f, 0f).normalized;
        }

        float currentHeight = capsuleCollider.height;
        float bodyHeight = currentHeight * 0.85f;

        Vector3 wallCheckCenter = transform.position
            + Vector3.up * (currentHeight * 0.5f)
            + checkDirection * wallCheckForwardOffset;

        Vector3 fullBodyBoxSize = new Vector3(wallCheckSize.x, bodyHeight, wallCheckSize.z);

        isTouchingWall = Physics.OverlapBox(
            wallCheckCenter,
            fullBodyBoxSize * 0.5f,
            Quaternion.identity,
            wallLayer
        ).Length > 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner || _isInvincible) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("DeadZone"))
        {
            RequestDieServerRpc();

            var instance = collision.gameObject.GetComponentInParent<GameObjectInstance>();

            if (instance != null)
            {
                ulong trapOwnerId = instance.OwnerClientId;

                if (trapOwnerId != ulong.MaxValue)
                {
                    Debug.Log($"[Kill Trigger] 사망자: {OwnerClientId} | 함정 소유자: {trapOwnerId}");

                    ScoreManager.Inst.AddTrapKillScore(trapOwnerId, OwnerClientId);
                }
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("SpawnPoint"))
        {
            SetCheckpoint(other.transform.position);
            Debug.Log($"[Checkpoint] SpawnPoint 도달! 갱신 위치: {other.transform.position}");
            return;
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("ArrivePortal"))
        {
            if (_hasArrived) return;
            _hasArrived = true;

            _canControl = false;
            rb.linearVelocity = Vector3.zero;

            if (CameraManager.Inst != null)
            {
                CameraManager.Inst.StartSpectating();
            }

            RequestArriveServerRpc();
            return;
        }

        if (_isInvincible) return; 

        if (other.gameObject.layer == LayerMask.NameToLayer("Slow"))
        {
            _isOnSlowArea = true;
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("KnockBack"))
        {
            Vector3 knockBackDir = -other.transform.forward;
            if (knockBackDir == Vector3.zero) knockBackDir = Vector3.back;

            float upwardForce = knockBackForce * 0.6f;
            float horizontalForce = knockBackDir.x * knockBackForce * 2.5f;

            rb.linearVelocity = new Vector3(horizontalForce, upwardForce, 0f);
            knockBackTimer = knockBackDuration;

            if (netAnim != null)
            {
                netAnim.SetTrigger("doKnockBack");
            }
            else if (anim != null)
            {
                anim.Play("Dodge_Backward", 0, 0f);
            }

            Debug.Log("[Gimmick] 수평 넉백 발동!");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Slow"))
        {
            _isOnSlowArea = false;
        }
    }

    [ServerRpc]
    private void RequestArriveServerRpc(ServerRpcParams rpcParams = default)
    {
        SetPlayerActiveClientRpc(false);

        if (GameManager.Inst != null && GameManager.Inst.RoundManager != null)
        {
            GameManager.Inst.RoundManager.OnPlayerArrived(rpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    public void SetPlayerActiveClientRpc(bool isActive)
    {
        if (characterModel != null)
        {
            characterModel.gameObject.SetActive(isActive);
        }
        else if (anim != null)
        {
            anim.gameObject.SetActive(isActive);
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = isActive;
        }

        if (isActive)
        {
            _hasArrived = false;
        }
    }
    [ServerRpc]
    private void RequestDieServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isDeadNet.Value) return;
        isDeadNet.Value = true;

        stunTimer = 0f;

        if (isCrouching)
        {
            StopCookieRunCrouch();
        }

        rb.linearVelocity = Vector3.zero;

        PlayDeathClientRpc();

        RespawnAsync(_respawnCts.Token).Forget();
    }

    [ClientRpc]
    private void PlayDeathClientRpc()
    {
        if (anim != null)
        {
            anim.SetBool("isCrouching", false);
            anim.SetBool("isWallHanging", false);
            anim.Play("Death_A", 0, 0f);
        }
    }

    private async UniTaskVoid RespawnAsync(CancellationToken token)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(1.5f), cancellationToken: token);

        SetPlayerActiveClientRpc(true);

        Vector3 checkpointPos = CurrentCheckpointNet.Value;
        Vector3 finalPos = (checkpointPos != Vector3.zero) ? checkpointPos : spawnPoint;

        rb.linearVelocity = Vector3.zero;
        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        TeleportPlayerClientRpc(finalPos);

        isDeadNet.Value = false;

        PlayLandingClientRpc();
        StartInvincibleClientRpc();
    }
    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 targetPosition)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = targetPosition;
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(targetPosition, Quaternion.Euler(0f, 90f, 0f), transform.localScale);
        }
    }

    [ClientRpc]
    private void PlayLandingClientRpc()
    {
        if (anim != null)
        {
            anim.ResetTrigger("doDie");
            anim.Play("Landing", 0, 0f);
        }
    }
    public void SetCheckpoint(Vector3 newCheckpointPos)
    {
        if (IsOwner)
        {
            UpdateCheckpointServerRpc(newCheckpointPos);
        }
    }

    [ServerRpc]
    private void UpdateCheckpointServerRpc(Vector3 newCheckpointPos)
    {
        CurrentCheckpointNet.Value = newCheckpointPos;
        Debug.Log($"[Server] 플레이어({OwnerClientId}) 체크포인트 저장 완료: {newCheckpointPos}");
    }

    [ServerRpc]
    private void UpdateStateServerRpc(float hInput, bool grounded, bool crouching, bool wall, bool isSlowed)
    {
        horizontalInputNet.Value = hInput;
        isGroundedNet.Value = grounded;
        isCrouchingNet.Value = crouching;
        isTouchingWallNet.Value = wall;
        isSlowedNet.Value = isSlowed;
    }


    private void UpdateAnimation()
    {
        if (anim == null) return;

        if (isDeadNet.Value || IsStunnedNet.Value) return;

        anim.SetFloat("moveSpeed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isCrouching", isCrouching);
        anim.SetBool("isSlowed", _isOnSlowArea);

        bool isWallHangingState = !isGrounded && isTouchingWall;
        anim.SetBool("isWallHanging", isWallHangingState);
    }

    private void SyncAnimationsFromNetwork()
    {
        if (anim == null) return;

        anim.SetFloat("moveSpeed", Mathf.Abs(horizontalInputNet.Value));
        anim.SetBool("isGrounded", isGroundedNet.Value);
        anim.SetBool("isCrouching", isCrouchingNet.Value);
        anim.SetBool("isSlowed", isSlowedNet.Value);

        bool isWallHangingState = !isGroundedNet.Value && isTouchingWallNet.Value;
        anim.SetBool("isWallHanging", isWallHangingState);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheckPoint.position, checkSize);
        }

        float currentHeight = capsuleCollider != null ? capsuleCollider.height : 2.0f;
        float bodyHeight = currentHeight * 0.85f;

        Vector3 checkDirection = transform.forward;
        if (horizontalInput != 0)
        {
            checkDirection = new Vector3(horizontalInput, 0f, 0f).normalized;
        }

        Vector3 wallCheckCenter = transform.position
            + Vector3.up * (currentHeight * 0.5f)
            + checkDirection * wallCheckForwardOffset;

        Vector3 fullBodyBoxSize = new Vector3(wallCheckSize.x, bodyHeight, wallCheckSize.z);

        Gizmos.color = isTouchingWall ? Color.cyan : Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(wallCheckCenter, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, fullBodyBoxSize);
    }

    private bool IsPlayingEmote()
    {
        if (anim == null) return false;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Push_Ups") ||
               stateInfo.IsName("Waving") ||
               stateInfo.IsName("Cheering") ||
               stateInfo.IsName("HipHop Dance") ||
               stateInfo.IsName("Dance5");
    }

    private bool IsPlayingLanding()
    {
        if (anim == null) return false;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Landing");
    }

    private void RotateToCamera()
    {
        if (Camera.main != null)
        {
            Vector3 lookTarget = Camera.main.transform.position;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);
        }
    }

    public void OnEmoteButtonPressed(int emoteId)
    {
        RequestEmoteServerRpc(emoteId);
    }

    [ServerRpc]
    private void RequestEmoteServerRpc(int emoteId)
    {
        ExecuteEmoteClientRpc(emoteId);
    }

    [ClientRpc]
    private void ExecuteEmoteClientRpc(int emoteId)
    {
        netAnim.ResetTrigger("cancelEmote");
        netAnim.ResetTrigger("doPushUp");
        netAnim.ResetTrigger("doWaving");
        netAnim.ResetTrigger("doCheering");
        netAnim.ResetTrigger("doDance1"); 
        netAnim.ResetTrigger("doDance2"); 

        if (emoteId == 1)
        {
            netAnim.SetTrigger("doPushUp");
        }
        else if (emoteId == 2)
        {
            netAnim.SetTrigger("doWaving");
        }
        else if (emoteId == 3)
        {
            netAnim.SetTrigger("doCheering");
        }
        else if (emoteId == 4)
        {
            netAnim.SetTrigger("doDance1");
        }
        else if (emoteId == 5) 
        {
            netAnim.SetTrigger("doDance2");
        }
    }

    public void BounceFromHead()
    {
        if (!IsOwner) return;

        if (isCrouching) StopCookieRunCrouch();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, headBounceForce, 0f);
        remainingAirJumps = maxAirJumps;

        if (netAnim != null) netAnim.SetTrigger("doJump");

    }

    private void ExecuteAirJump()
    {
        if (isCrouching) StopCookieRunCrouch(); 

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
        remainingAirJumps--;
        jumpBufferTimer = 0f;

        if (netAnim != null) netAnim.SetTrigger("doJump");
    }

    public void GetStomped()
    {
        if (_isInvincible) return;
        RequestStunServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission =RpcInvokePermission.Everyone)]
    public void RequestStunServerRpc()
    {
        if (isDeadNet.Value) return;

        if (IsStunnedNet.Value)
        {
            _stunCts?.Cancel();
            _stunCts?.Dispose();
        }

        IsStunnedNet.Value = true;
        _stunCts = new CancellationTokenSource();

        StunTimerAsync(stunDuration, _stunCts.Token).Forget();
    }


    private void OnStunStateChanged(bool preValue, bool newValue)
    {
        if (stunEffectObj != null)
        {
            stunEffectObj.SetActive(newValue);
        }

        if (newValue == true)
        {
            if (isDeadNet.Value) return;
            if (isCrouching) StopCookieRunCrouch();

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            anim.SetBool("isWallHanging", false);
            anim.SetBool("isCrouching", false);
            anim.SetBool("isStunned", true);

            if (IsServer)
            {
                netAnim.SetTrigger("doStun");
            }
        }
        else
        {
            anim.SetBool("isStunned", false);
        }
    }
    private async UniTaskVoid StunTimerAsync(float duration, CancellationToken cancellationToken)
    {
            await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);

            IsStunnedNet.Value = false;
    }
    [ClientRpc]
    private void StartInvincibleClientRpc()
    {
        _isInvincible = true;

        if (_invincibleCts != null)
        {
            _invincibleCts.Cancel();
            _invincibleCts.Dispose();
        }
        _invincibleCts = new CancellationTokenSource();

        BlinkEffectAsync(respawnInvincibleDuration, _invincibleCts.Token).Forget();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayJumpEffectRpc()
    {
        if (jumpParticle != null) jumpParticle.Play();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayLandEffectRpc()
    {
        if (landParticle != null) landParticle.Play();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayHeadBounceEffectRpc()
    {
        if (headBounceParticle != null) headBounceParticle.Play();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayWallJumpEffectRpc()
    {
        if (wallJumpParticle != null) wallJumpParticle.Play();
    }
    private async UniTaskVoid BlinkEffectAsync(float duration, CancellationToken token)
    {
        float timer = 0f;
        bool visible = true;

        while (timer < duration)
        {
            visible = !visible;
            SetRenderersVisibility(visible);

            await UniTask.Delay(System.TimeSpan.FromSeconds(blinkInterval), cancellationToken: token);
            timer += blinkInterval;
        }

        SetRenderersVisibility(true);
        _isInvincible = false;
    }

    private void SetRenderersVisibility(bool isVisible)
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = isVisible;
            }
        }
    }
    public void ResetPlayerForNextRound(Vector3 startSpawnPos)
    {
        if (!IsServer) return;

        isDeadNet.Value = false;
        IsStunnedNet.Value = false;
        CurrentCheckpointNet.Value = Vector3.zero;

        if (_stunCts != null)
        {
            _stunCts.Cancel();
            _stunCts.Dispose();
            _stunCts = null;
        }

        if (_respawnCts != null)
        {
            _respawnCts.Cancel();
            _respawnCts.Dispose();
            _respawnCts = null;
        }

        if (_invincibleCts != null)
        {
            _invincibleCts.Cancel();
            _invincibleCts.Dispose();
            _invincibleCts = null;
        }

        ResetPlayerClientRpc(startSpawnPos);
    }

    [ClientRpc]
    private void ResetPlayerClientRpc(Vector3 startSpawnPos)
    {
        SetPlayerActiveClientRpc(true);
        SetRenderersVisibility(true);

        _hasArrived = false;
        _isInvincible = false;
        _isOnSlowArea = false;
        isCrouching = false;
        knockBackTimer = 0f;
        wallJumpLockTimer = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        remainingAirJumps = maxAirJumps;
        remainingWallClimbs = maxWallClimbs;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = startSpawnPos;
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(startSpawnPos, Quaternion.Euler(0f, 90f, 0f), transform.localScale);
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
            anim.Play("Idle_A", 0, 0f);
        }

        if (IsOwner && CameraManager.Inst != null)
        {
            CameraManager.Inst.StopSpectating();
            CameraManager.Inst.SetTargetCamera(OwnerClientId, gameObject);
        }
        if (stunEffectObj != null) stunEffectObj.SetActive(false);
        if (slideParticle != null) slideParticle.Stop();
    }
}