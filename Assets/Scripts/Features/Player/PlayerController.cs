using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
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
    [SerializeField] private float wallJumpHorizontalForce = 9f;  
    [SerializeField] private int maxWallClimbs = 1;

    [Header("CookieRun Style Crouch Tuning")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private float crouchHeightRatio = 0.35f;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector3 checkSize = new Vector3(0.5f, 0.15f, 0.5f);
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    private float horizontalInput;
    private bool isCrouching;
    private bool isGrounded;
    private bool isTouchingWall;
    private int remainingWallClimbs;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private Vector3 originalModelScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;

        if (characterModel == null && transform.Find("Model") != null)
        {
            characterModel = transform.Find("Model");
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

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

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
        rb.linearVelocity = new Vector3(horizontalInput * moveSpeed, rb.linearVelocity.y, 0f);
    }

    private void ExecuteJump()
    {
        if (isCrouching) StopCookieRunCrouch();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void ExecuteWallClimb()
    {
        if (isCrouching) StopCookieRunCrouch();

        float pushDirection = -transform.forward.x;

        rb.linearVelocity = new Vector3(pushDirection * wallJumpHorizontalForce, wallClimbForce, 0f);

        transform.rotation = Quaternion.Euler(0f, pushDirection > 0 ? 90f : -90f, 0f);

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

        if (characterModel != null)
        {
            characterModel.localScale = new Vector3(
                originalModelScale.x,
                originalModelScale.y * crouchHeightRatio,
                originalModelScale.z
            );
            characterModel.localPosition = new Vector3(0f, yOffset, 0f);
        }
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

            if (characterModel != null)
            {
                characterModel.localScale = originalModelScale;
                characterModel.localPosition = Vector3.zero;
            }
        }
    }

    private void HandleRotation()
    {
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
        float currentHeight = capsuleCollider.height;
        float bodyHeight = currentHeight * 0.85f;

        Vector3 wallCheckCenter = transform.position
            + Vector3.up * (currentHeight * 0.5f)
            + transform.forward * wallCheckForwardOffset;

        Vector3 fullBodyBoxSize = new Vector3(wallCheckSize.x, bodyHeight, wallCheckSize.z);

        isTouchingWall = Physics.OverlapBox(
            wallCheckCenter,
            fullBodyBoxSize * 0.5f,
            transform.rotation,
            wallLayer
        ).Length > 0;
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

        Vector3 wallCheckCenter = transform.position
            + Vector3.up * (currentHeight * 0.5f)
            + transform.forward * wallCheckForwardOffset;

        Vector3 fullBodyBoxSize = new Vector3(wallCheckSize.x, bodyHeight, wallCheckSize.z);

        Gizmos.color = isTouchingWall ? Color.cyan : Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(wallCheckCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, fullBodyBoxSize);
    }
}