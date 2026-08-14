using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class BackupPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Jump Tuning")]
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float jumpCutMultiplier = 0.3f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Wall Climb Tuning (Full Body)")]
    [SerializeField] private float wallCheckForwardOffset = 0.4f;
    [SerializeField] private Vector3 wallCheckSize = new Vector3(0.5f, 1.0f, 0.5f);
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallClimbForce = 13f;
    [SerializeField] private float wallJumpHorizontalForce = 12f;
    [SerializeField] private float wallJumpControlLockTime = 0.15f;
    [SerializeField] private int maxWallClimbs = 1;

    [Header("CookieRun Style Crouch Tuning")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private float crouchHeightRatio = 0.35f;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector3 checkSize = new Vector3(0.5f, 0.15f, 0.5f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Spawn & Death")]
    [SerializeField] private Transform spawnPoint;
    private bool isDead = false;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Animator anim;

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
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        anim = GetComponentInChildren<Animator>();

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
    private void Start()
    {
        RespawnAtStart();
    }
    private void Update()
    {
        if (isDead || IsPlayingLanding()) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");


        if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)))
        {
            if (anim != null) anim.SetTrigger("doPushUps");
        }
        else if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)))
        {
            if (anim != null) anim.SetTrigger("doWaving");

            RotateToCamera();
        }
        else if (isGrounded && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
        {
            if (anim != null) anim.SetTrigger("doCheering");

            RotateToCamera();
        }
        else if (IsPlayingEmote() && (horizontalInput != 0 || Input.GetKeyDown(KeyCode.Space)))
        {
            if (anim != null) anim.Play("Idle_A");
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");

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
    }
    private void FixedUpdate()
    {
        CheckGround();
        CheckWall();
        ApplyMovement();

        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                ExecuteJump();
            }
            else if (!isGrounded && isTouchingWall && remainingWallClimbs > 0)
            {
                ExecuteWallClimb();
            }
        }
    }
    private void ApplyMovement()
    {
        if (isDead || IsPlayingLanding())
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
            rb.linearVelocity = new Vector3(horizontalInput * moveSpeed, rb.linearVelocity.y, 0f);
        }
    }

    private void ExecuteJump()
    {
        if (isCrouching) StopCookieRunCrouch();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);

        if (anim != null) anim.SetTrigger("doJump");

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

        if (anim != null) anim.SetTrigger("doWallJump");

        remainingWallClimbs--;
        jumpBufferTimer = 0f;
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
        if (wallJumpLockTimer > 0 || IsPlayingEmote()) return;

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
        if (collision.gameObject.layer == LayerMask.NameToLayer("DeadZone"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector3.zero;
        if (anim != null) anim.SetTrigger("doDie");

        Invoke(nameof(RespawnAtStart), 1.5f);
    }

    public void RespawnAtStart()
    {
        if (GameManager.Inst != null)
        {
            GameManager.Inst.RespawnPlayer(gameObject);
        }
        else if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            rb.linearVelocity = Vector3.zero;
        }

        transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        isDead = false;

        if (anim != null)
        {
            anim.ResetTrigger("doDie");
            anim.Play("Landing", 0, 0f);
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;

        anim.SetFloat("moveSpeed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isCrouching", isCrouching);

        bool isWallHangingState = !isGrounded && isTouchingWall;
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
}