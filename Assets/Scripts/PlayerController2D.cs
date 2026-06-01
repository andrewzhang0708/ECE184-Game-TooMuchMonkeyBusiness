using UnityEngine;
using UnityEngine.InputSystem;

// https://poki.com/en/g/papa-louie-3?campaign=22729182886&adgroup=185500509270&extensionid=&targetid=dsa-1463903668522&location=9060098&matchtype=&network=g&device=c&devicemodel=&creative=760735981576&keyword=&placement=&target=&gad_source=1&gad_campaignid=22729182886&gbraid=0AAAAADlg9ZGIzckQ07fCJQi8eF98UJ-jz&gclid=Cj0KCQjwiJvQBhCYARIsAMjts3Lq3HTXpJoPvLrWaDdAa2BPRhk0rOlp-a_g7L5GwlqS6oO0YXudrL8aApogEALw_wcB

[RequireComponent(typeof(Rigidbody))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float airAcceleration = 18f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Rolling")]
    [SerializeField] private float minimumRollSlopeAngle = 3f;
    [SerializeField] private float rollSlopeAcceleration = 12f;
    [SerializeField] private float rollFlatDeceleration = 8f;
    [SerializeField] private float rollUphillDeceleration = 18f;
    [SerializeField] private float rollStopSpeed = 0.15f;
    [SerializeField] private float maxRollSpeed = 25f;

    [Header("Gravity")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [Tooltip("If enabled, the extra gravity is applied while ascending too. Keeps up/down force symmetric.")]
    [SerializeField] private bool applyGravityWhileAscending = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float slopeCheckDistance = 0.9f;
    [SerializeField] private float slopeProbeForwardDistance = 0.35f;
    [SerializeField] private float groundStickForce = 20f;
    [SerializeField] private float groundGraceDuration = 0.08f;
    [SerializeField] private float jumpGroundIgnoreDuration = 0.1f;

    [Header("Visual Facing")]
    [Tooltip("Drag MonkeyVisual here, not the Player root.")]
    [SerializeField] private Transform visualTransform;

    [Header("Animation")]
    [Tooltip("Drag the Animator on MonkeyVisual / monkey model here.")]
    [SerializeField] private Animator animator;

    [Header("Jump Audio")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 3f)] private float jumpVolume = 1f;

    private Rigidbody rb;
    private Collider[] ownColliders;
    private readonly Collider[] groundHits = new Collider[8];
    private readonly RaycastHit[] slopeHits = new RaycastHit[8];

    private const float VerticalWallAngle = 89.9f;

    private bool isGrounded;
    private bool isRolling;
    private bool externalMotionActive;
    private float rollSpeed;
    private float rollHorizontalDirection;
    private float lastGroundedTime = float.NegativeInfinity;
    private float ignoreGroundUntil;

    private Quaternion facingRightRotation;
    private Quaternion facingLeftRotation;
    private Quaternion rollingFacingRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ownColliders = GetComponentsInChildren<Collider>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 2.5D platformer:
        // Lock Z movement and all rotations.
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

        // IMPORTANT:
        // In the Inspector, rotate MonkeyVisual so that it is facing RIGHT at game start.
        // This script will remember that as the "right-facing" direction.
        if (visualTransform != null)
        {
            facingRightRotation = visualTransform.localRotation;
            facingLeftRotation = facingRightRotation * Quaternion.Euler(0f, 180f, 0f);

            Vector3 rollingEulerAngles = facingRightRotation.eulerAngles;
            rollingEulerAngles.y = 90f;
            rollingFacingRotation = Quaternion.Euler(rollingEulerAngles);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (externalMotionActive)
        {
            UpdateAnimator();
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame && HorizontalBarSwing2D.TryGrabClosest(rb, out _))
        {
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame && TrampolineRope2D.TryGrabClosest(rb, out _))
        {
            return;
        }

        CheckGround();

        bool jumpPressed = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
        bool rollPressed = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;

        if (rollPressed && !isRolling)
        {
            TryStartRolling();
        }
        else if (jumpPressed && isGrounded && !isRolling)
        {
            Jump();
        }
        else if (jumpPressed && !isGrounded)
        {
            // Debug.Log("Jump pressed but not grounded. isGrounded=" + isGrounded);
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (externalMotionActive)
        {
            return;
        }

        CheckGround();

        if (isRolling)
        {
            UpdateRolling();
        }
        else
        {
            Move();
        }

        ApplyExtraGravity();
        UpdateAnimator();
    }

    private void ApplyExtraGravity()
    {
        if (isGrounded)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;

        // Apply additional gravity while falling, and optionally while ascending.
        bool applyWhileAscending = applyGravityWhileAscending && velocity.y > 0f;

        if (velocity.y < 0f || applyWhileAscending)
        {
            velocity.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = velocity;
        }
    }

    private void Move()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float horizontal = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            // Do not change
            horizontal -= 2f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            // Do not change
            horizontal += 2f;
        }

        Vector3 velocity = rb.linearVelocity;
        bool isIdle = Mathf.Approximately(horizontal, 0f);

        if (isGrounded)
        {
            if (isIdle)
            {
                velocity.x = 0f;
                velocity.y = 0f;
            }
            else if (TryGetGroundNormal(horizontal, out Vector3 groundNormal))
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.right, groundNormal).normalized;
                Vector3 slopeVelocity = slopeDirection * (horizontal * moveSpeed);

                velocity.x = slopeVelocity.x;
                velocity.y = slopeVelocity.y;
            }
            else
            {
                velocity.x = horizontal * moveSpeed;
            }
        }
        else if (!Mathf.Approximately(horizontal, 0f))
        {
            float targetSpeed = horizontal * moveSpeed;
            velocity.x = Mathf.MoveTowards(
                velocity.x,
                targetSpeed,
                airAcceleration * Time.fixedDeltaTime
            );
        }

        velocity.z = 0f;
        rb.linearVelocity = velocity;

        if (isGrounded && isIdle && TryGetGroundNormal(out Vector3 idleGroundNormal))
        {
            Vector3 slopeGravity = Vector3.ProjectOnPlane(Physics.gravity, idleGroundNormal);
            rb.AddForce(-slopeGravity, ForceMode.Acceleration);
        }
        else if (isGrounded && groundStickForce > 0f)
        {
            rb.AddForce(Vector3.down * groundStickForce, ForceMode.Acceleration);
        }

        UpdateFacing(horizontal);
    }

    private void UpdateFacing(float horizontal)
    {
        if (Mathf.Approximately(horizontal, 0f))
        {
            return;
        }

        if (visualTransform == null)
        {
            return;
        }

        visualTransform.localRotation = horizontal > 0f
            ? facingRightRotation
            : facingLeftRotation;
    }

    private void TryStartRolling()
    {
        if (!isGrounded || !TryGetGroundNormal(out Vector3 groundNormal))
        {
            return;
        }

        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        if (slopeAngle < minimumRollSlopeAngle)
        {
            return;
        }

        Vector3 downhillDirection = Vector3.ProjectOnPlane(Physics.gravity, groundNormal).normalized;
        if (Mathf.Approximately(downhillDirection.x, 0f))
        {
            return;
        }

        isRolling = true;
        rollHorizontalDirection = Mathf.Sign(downhillDirection.x);
        rollSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, downhillDirection));
        UpdateRollingFacing();
    }

    private void UpdateRolling()
    {
        if (!isGrounded)
        {
            StopRolling(false);
            return;
        }

        if (!TryGetGroundNormal(rollHorizontalDirection, out Vector3 groundNormal))
        {
            StopRolling(false);
            return;
        }

        Vector3 rollDirection = Vector3.ProjectOnPlane(
            Vector3.right * rollHorizontalDirection,
            groundNormal
        ).normalized;

        Vector3 slopeGravity = Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
        float gravityAlongRoll = Vector3.Dot(slopeGravity, rollDirection);
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        float acceleration;

        if (slopeAngle < minimumRollSlopeAngle)
        {
            acceleration = -rollFlatDeceleration;
        }
        else if (gravityAlongRoll > 0f)
        {
            acceleration = rollSlopeAcceleration;
        }
        else
        {
            acceleration = -rollUphillDeceleration;
        }

        rollSpeed = Mathf.Clamp(
            rollSpeed + acceleration * Time.fixedDeltaTime,
            0f,
            Mathf.Max(rollStopSpeed, maxRollSpeed)
        );

        if (acceleration <= 0f && rollSpeed <= rollStopSpeed)
        {
            StopRolling(true);
            return;
        }

        Vector3 velocity = rollDirection * rollSpeed;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        if (groundStickForce > 0f)
        {
            rb.AddForce(Vector3.down * groundStickForce, ForceMode.Acceleration);
        }

        UpdateRollingFacing();
    }

    private void StopRolling(bool stopCompletely)
    {
        isRolling = false;
        rollSpeed = 0f;
        UpdateFacing(rollHorizontalDirection);

        if (!stopCompletely)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void UpdateRollingFacing()
    {
        if (visualTransform != null)
        {
            visualTransform.localRotation = rollingFacingRotation;
        }
    }

    private void Jump()
    {
        isGrounded = false;
        lastGroundedTime = float.NegativeInfinity;
        ignoreGroundUntil = Time.time + jumpGroundIgnoreDuration;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        float effectiveJumpForce = jumpForce;

        // If we're applying stronger gravity during ascent as well, scale the
        // jump impulse so the apex height remains approximately the same.
        if (applyGravityWhileAscending && fallMultiplier > 0f)
        {
            effectiveJumpForce = jumpForce * Mathf.Sqrt(fallMultiplier);
        }

        rb.AddForce(Vector3.up * effectiveJumpForce, ForceMode.Impulse);
        PlayJumpSound();
        // Debug.Log("Playing Sound");

        UpdateAnimator();
    }

    private void PlayJumpSound()
    {
        if (jumpClip == null)
        {
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(jumpClip, jumpVolume);
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsRolling", isRolling);
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("RollSpeed", rollSpeed);
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    public void SetExternalMotionActive(bool isActive)
    {
        externalMotionActive = isActive;

        if (isActive)
        {
            StopRolling(false);
        }
    }

    private void CheckGround()
    {
        if (Time.time < ignoreGroundUntil)
        {
            isGrounded = false;
            return;
        }

        // Debug.Log("Checking ground. Grounded=" + isGrounded);
        Vector3 checkPosition = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.55f;

        int hitCount = Physics.OverlapSphereNonAlloc(
            checkPosition,
            groundCheckRadius,
            groundHits,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = groundHits[i];

            if (hit == null || IsOwnCollider(hit))
            {
                continue;
            }

            int hitLayer = hit.gameObject.layer;
            if ((groundLayer.value & (1 << hitLayer)) == 0)
            {
                // Hit is not part of the configured groundLayer mask
                continue;
            }

            isGrounded = true;
            lastGroundedTime = Time.time;
            return;
        }

        isGrounded = Time.time - lastGroundedTime <= groundGraceDuration;
    }

    private bool TryGetGroundNormal(out Vector3 groundNormal)
    {
        return TryGetGroundNormal(0f, out groundNormal);
    }

    private bool TryGetGroundNormal(float horizontal, out Vector3 groundNormal)
    {
        Vector3 rayOrigin = groundCheck != null
            ? groundCheck.position + Vector3.up * groundCheckRadius
            : transform.position;

        if (!Mathf.Approximately(horizontal, 0f))
        {
            float direction = Mathf.Sign(horizontal);
            Vector3 forwardOrigin =
                rayOrigin +
                Vector3.right * (direction * slopeProbeForwardDistance) +
                Vector3.up * slopeProbeForwardDistance;

            if (TryRaycastGroundNormal(
                forwardOrigin,
                slopeCheckDistance + slopeProbeForwardDistance,
                out groundNormal
            ))
            {
                return true;
            }
        }

        return TryRaycastGroundNormal(rayOrigin, slopeCheckDistance, out groundNormal);
    }

    private bool TryRaycastGroundNormal(
        Vector3 rayOrigin,
        float rayDistance,
        out Vector3 groundNormal
    )
    {
        int hitCount = Physics.RaycastNonAlloc(
            rayOrigin,
            Vector3.down,
            slopeHits,
            rayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.PositiveInfinity;
        groundNormal = Vector3.up;
        bool foundGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = slopeHits[i];

            if (hit.collider == null || IsOwnCollider(hit.collider))
            {
                continue;
            }

            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle >= VerticalWallAngle || hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            groundNormal = hit.normal;
            foundGround = true;
        }

        return foundGround;
    }

    private bool IsOwnCollider(Collider targetCollider)
    {
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == targetCollider)
            {
                return true;
            }
        }

        return false;
    }
}
