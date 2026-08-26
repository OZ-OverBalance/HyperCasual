using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Threading;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
public class PlayerController : NetworkBehaviour
{
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
    [SerializeField] private float slowSpeedMultiplier = 0.5f; // 슬로우 시 원래 속도의 50%
    [SerializeField] private float slowDuration = 2.0f;       // 슬로우 지속 시간
    [SerializeField] private float knockBackForce = 10f;       // 넉백 튕기는 힘

    private float currentSpeedMultiplier = 1.0f;
    private float slowTimer = 0f;

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
    private Vector3 currentCheckpoint;

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
    }



    public override void OnNetworkSpawn()
    {
        currentCheckpoint = spawnPoint;

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
        if(_stunCts != null)
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
    }

    private void Update()
    {
        if (!IsOwner)
        {
            SyncAnimationsFromNetwork();
            return;
        }

        if (!_canControl || isDeadNet.Value || IsPlayingLanding() || IsStunnedNet.Value == true)
        {
            horizontalInput = 0f;
            UpdateAnimation();
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");

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

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                currentSpeedMultiplier = 1.0f; 
            }
        }

        HandleRotation();
        UpdateAnimation();

        UpdateStateServerRpc(horizontalInput, isGrounded, isCrouching, isTouchingWall, slowTimer > 0f);
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
        if(!_canControl || isDeadNet.Value || IsStunnedNet.Value)
        {
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
        //RequestWallJumpServerRpc();

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
        if (!IsOwner) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("DeadZone"))
        {
            RequestDieServerRpc();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("SpawnPoint"))
        {
            SetCheckpoint(other.transform.position);
            Debug.Log($"[Checkpoint] SpawnPoint 도달! 갱신 위치: {other.transform.position}");
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("ArrivePortal"))
        {
            if (_hasArrived) return;
            _hasArrived = true;

            _canControl = false;
            rb.linearVelocity = Vector3.zero;

            RequestArriveServerRpc();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Slow"))
        {
            currentSpeedMultiplier = slowSpeedMultiplier;
            slowTimer = slowDuration; 
            Debug.Log("[Gimmick] 슬로우 구간 진입!");
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("KnockBack"))
        {
            Vector3 knockBackDir = -other.transform.forward;
            if (knockBackDir == Vector3.zero) knockBackDir = Vector3.back;

            rb.linearVelocity = new Vector3(knockBackDir.x * knockBackForce, knockBackForce * 0.7f, 0f);

            if (netAnim != null)
            {
                netAnim.SetTrigger("doKnockBack");
            }
            else if (anim != null)
            {
                anim.Play("Dodge_Backward", 0, 0f);
            }

            Debug.Log("[Gimmick] 넉백 피격!");
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

        if (GameManager.Inst != null)
        {
            Vector3? targetPos = (currentCheckpoint != Vector3.zero) ? currentCheckpoint : (Vector3?)null;
            GameManager.Inst.RespawnPlayer(gameObject, targetPos);
        }
        else
        {
            transform.position = (currentCheckpoint != Vector3.zero) ? currentCheckpoint : spawnPoint;
            rb.linearVelocity = Vector3.zero;
        }

        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        isDeadNet.Value = false;

        PlayLandingClientRpc();
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
            currentCheckpoint = newCheckpointPos;
            UpdateCheckpointServerRpc(newCheckpointPos);
        }
    }

    [ServerRpc]
    private void UpdateCheckpointServerRpc(Vector3 newCheckpointPos)
    {
        currentCheckpoint = newCheckpointPos;
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
        anim.SetBool("isSlowed", slowTimer > 0f);

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
        return stateInfo.IsName("Push_Ups") || stateInfo.IsName("Waving") || stateInfo.IsName("Cheering");
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
        if(newValue == true)
        {
            if (isDeadNet.Value) return;
            if (isCrouching) StopCookieRunCrouch();

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            anim.SetBool("isWallHanging", false);
            anim.SetBool("isCrouching", false);
            anim.SetBool("isStunned", true);

            if(IsServer)
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



}