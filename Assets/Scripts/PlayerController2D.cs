using UnityEngine;
using UnityEngine.InputSystem;

// https://poki.com/en/g/papa-louie-3?campaign=22729182886&adgroup=185500509270&extensionid=&targetid=dsa-1463903668522&location=9060098&matchtype=&network=g&device=c&devicemodel=&creative=760735981576&keyword=&placement=&target=&gad_source=1&gad_campaignid=22729182886&gbraid=0AAAAADlg9ZGIzckQ07fCJQi8eF98UJ-jz&gclid=Cj0KCQjwiJvQBhCYARIsAMjts3Lq3HTXpJoPvLrWaDdAa2BPRhk0rOlp-a_g7L5GwlqS6oO0YXudrL8aApogEALw_wcB

[RequireComponent(typeof(Rigidbody))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float airAcceleration = 18f;
    [SerializeField] private float airDeceleration = 10f;
    [SerializeField] private float jumpForce = 8f;
    [Tooltip("Multiplier applied to upward velocity when the jump key is released early.")]
    [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.45f;
    [Tooltip("Time used to smoothly reduce upward velocity after releasing the jump key.")]
    [SerializeField, Min(0.01f)] private float jumpCutDuration = 0.08f;

    [Header("Rolling")]
    [SerializeField] private bool enableRolling;
    [SerializeField] private float minimumRollSlopeAngle = 3f;
    [SerializeField] private float rollSlopeAcceleration = 12f;
    [SerializeField] private float rollFlatDeceleration = 8f;
    [SerializeField] private float rollUphillDeceleration = 18f;
    [SerializeField] private float rollStopSpeed = 0.15f;
    [SerializeField] private float maxRollSpeed = 25f;
    [SerializeField] private float rollVisualDegreesPerSpeed = 120f;
    [SerializeField] private float rollWallCheckDistance = 0.15f;

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
    [SerializeField] private bool logGroundColliderChanges;
    [SerializeField] private bool logGroundCheckDebug = true;
    [SerializeField] private float groundCheckDebugInterval = 0.25f;
    [SerializeField] private float groundCheckDebugRayDistance = 20f;

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
    private readonly RaycastHit[] wallHits = new RaycastHit[8];

    private const float VerticalWallAngle = 89.9f;

    private bool isGrounded;
    private bool isRolling;
    private bool externalMotionActive;
    private bool isCuttingJumpShort;
    private float jumpCutElapsed;
    private float jumpCutStartVelocity;
    private float jumpCutTargetVelocity;
    private float rollSpeed;
    private float rollHorizontalDirection;
    private float rollVisualAngle;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastGroundContactTime = float.NegativeInfinity;
    private float nextGroundCheckDebugTime;
    private float ignoreGroundUntil;
    private Collider lastLoggedGroundCollider;

    private Quaternion facingRightRotation;
    private Quaternion facingLeftRotation;
    private Quaternion rollingFacingRotation;
    private Animator cachedAnimator;
    private bool animatorHasIsGrounded;
    private bool animatorHasIsRolling;
    private bool animatorHasSpeed;
    private bool animatorHasRollSpeed;
    private bool animatorHasVerticalSpeed;

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

        if (keyboard.eKey.wasPressedThisFrame && SimpleVineSwing.TryGrabClosest(rb, out _))
        {
            return;
        }

        CheckGround();

        bool jumpPressed = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
        bool jumpReleased = keyboard.wKey.wasReleasedThisFrame || keyboard.upArrowKey.wasReleasedThisFrame;
        bool jumpHeld = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        bool rollPressed = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;

        if (jumpReleased && !jumpHeld)
        {
            CutJumpShort();
        }

        if (enableRolling && rollPressed && !isRolling)
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

        UpdateJumpCut();
        ApplyExtraGravity();
        UpdateAnimator();
    }

    private void ApplyExtraGravity()
    {
        if (isGrounded && HasRecentGroundContact())
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
        Vector3 movementGroundNormal = Vector3.up;
        bool hasGroundContact = HasRecentGroundContact();
        bool canUseGroundMovement = isGrounded && hasGroundContact;
        bool hasGroundNormal =
            canUseGroundMovement &&
            TryGetGroundNormal(horizontal, out movementGroundNormal);
        rb.useGravity = !hasGroundNormal;

        if (canUseGroundMovement)
        {
            if (isIdle)
            {
                velocity.x = 0f;
                velocity.y = 0f;
            }
            else if (hasGroundNormal)
            {
                Vector3 slopeDirection =
                    Vector3.ProjectOnPlane(Vector3.right, movementGroundNormal).normalized;
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
        else
        {
            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0f,
                airDeceleration * Time.fixedDeltaTime
            );
        }

        velocity.z = 0f;
        rb.linearVelocity = velocity;

        if (canUseGroundMovement && !isIdle && groundStickForce > 0f)
        {
            Vector3 stickDirection = hasGroundNormal
                ? -movementGroundNormal
                : Vector3.down;

            rb.AddForce(stickDirection * groundStickForce, ForceMode.Acceleration);
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
        rb.useGravity = true;
        rollHorizontalDirection = Mathf.Sign(downhillDirection.x);
        rollSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, downhillDirection));
        rollVisualAngle = 0f;
        UpdateRollingFacing();
    }

    private void UpdateRolling()
    {
        rb.useGravity = true;

        if (IsRollingIntoWall())
        {
            StopRolling(true);
            return;
        }

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

        rollVisualAngle -=
            rollHorizontalDirection *
            rollSpeed *
            rollVisualDegreesPerSpeed *
            Time.fixedDeltaTime;
        UpdateRollingFacing();
    }

    private bool IsRollingIntoWall()
    {
        if (Mathf.Approximately(rollHorizontalDirection, 0f))
        {
            return false;
        }

        Bounds playerBounds = GetPlayerBounds();
        float radius = Mathf.Min(playerBounds.extents.x, playerBounds.extents.y);
        radius = Mathf.Max(radius, 0.05f);
        float halfHeight = Mathf.Max(playerBounds.extents.y, radius);
        Vector3 center = playerBounds.center;
        Vector3 top = center + Vector3.up * (halfHeight - radius);
        Vector3 bottom = center - Vector3.up * (halfHeight - radius);
        Vector3 direction = Vector3.right * rollHorizontalDirection;

        int hitCount = Physics.CapsuleCastNonAlloc(
            top,
            bottom,
            radius,
            direction,
            wallHits,
            rollWallCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = wallHits[i];

            if (
                hit.collider == null ||
                IsOwnCollider(hit.collider) ||
                IsIgnoredOneWayPlatform(hit.collider)
            )
            {
                continue;
            }

            float wallAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (wallAngle >= VerticalWallAngle)
            {
                return true;
            }
        }

        return false;
    }

    private Bounds GetPlayerBounds()
    {
        Bounds playerBounds = new Bounds(transform.position, Vector3.zero);
        bool foundCollider = false;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider ownCollider = ownColliders[i];

            if (ownCollider == null || !ownCollider.enabled || ownCollider.isTrigger)
            {
                continue;
            }

            if (!foundCollider)
            {
                playerBounds = ownCollider.bounds;
                foundCollider = true;
            }
            else
            {
                playerBounds.Encapsulate(ownCollider.bounds);
            }
        }

        return playerBounds;
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
            Quaternion rollRotation = Quaternion.AngleAxis(rollVisualAngle, Vector3.forward);
            visualTransform.localRotation = rollRotation * rollingFacingRotation;
        }
    }

    private void Jump()
    {
        rb.useGravity = true;
        isGrounded = false;
        isCuttingJumpShort = false;
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

    private void CutJumpShort()
    {
        Vector3 velocity = rb.linearVelocity;

        if (isGrounded || velocity.y <= 0f)
        {
            return;
        }

        isCuttingJumpShort = true;
        jumpCutElapsed = 0f;
        jumpCutStartVelocity = velocity.y;
        jumpCutTargetVelocity = velocity.y * jumpCutMultiplier;
    }

    private void UpdateJumpCut()
    {
        if (!isCuttingJumpShort)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        if (isGrounded || velocity.y <= 0f)
        {
            isCuttingJumpShort = false;
            return;
        }

        jumpCutElapsed += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(jumpCutElapsed / jumpCutDuration);
        float smoothedTarget = Mathf.Lerp(
            jumpCutStartVelocity,
            jumpCutTargetVelocity,
            progress
        );

        velocity.y = Mathf.Min(velocity.y, smoothedTarget);
        rb.linearVelocity = velocity;

        if (progress >= 1f)
        {
            isCuttingJumpShort = false;
        }
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

        CacheAnimatorParameters();

        if (animatorHasIsGrounded)
        {
            animator.SetBool("IsGrounded", isGrounded && HasRecentGroundContact());
        }

        if (animatorHasIsRolling)
        {
            animator.SetBool("IsRolling", isRolling);
        }

        if (animatorHasSpeed)
        {
            animator.SetFloat("Speed", GetRunningAnimationSpeed());
        }

        if (animatorHasRollSpeed)
        {
            animator.SetFloat("RollSpeed", rollSpeed);
        }

        if (animatorHasVerticalSpeed)
        {
            animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        }
    }

    private void CacheAnimatorParameters()
    {
        if (cachedAnimator == animator)
        {
            return;
        }

        cachedAnimator = animator;
        animatorHasIsGrounded = HasAnimatorParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        animatorHasIsRolling = HasAnimatorParameter("IsRolling", AnimatorControllerParameterType.Bool);
        animatorHasSpeed = HasAnimatorParameter("Speed", AnimatorControllerParameterType.Float);
        animatorHasRollSpeed = HasAnimatorParameter("RollSpeed", AnimatorControllerParameterType.Float);
        animatorHasVerticalSpeed = HasAnimatorParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private float GetRunningAnimationSpeed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !isGrounded || !HasRecentGroundContact() || isRolling)
        {
            return 0f;
        }

        bool isMovingLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool isMovingRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

        return isMovingLeft != isMovingRight
            ? Mathf.Abs(rb.linearVelocity.x)
            : 0f;
    }

    public void SetExternalMotionActive(bool isActive)
    {
        externalMotionActive = isActive;

        if (isActive)
        {
            rb.useGravity = true;
            isCuttingJumpShort = false;
            StopRolling(false);
        }
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }

    private void CheckGround()
    {
        if (Time.time < ignoreGroundUntil)
        {
            isGrounded = false;
            lastGroundContactTime = float.NegativeInfinity;
            LogGroundCheckDebug(false, groundCheck != null
                ? groundCheck.position
                : transform.position + Vector3.down * 0.55f);
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

            if (hit == null || IsOwnCollider(hit) || IsIgnoredOneWayPlatform(hit))
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
            LogGroundCollider(hit);
            LogGroundCheckDebug(true, checkPosition);
            return;
        }

        isGrounded = Time.time - lastGroundedTime <= groundGraceDuration;
        LogGroundCheckDebug(isGrounded, checkPosition);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time < ignoreGroundUntil)
        {
            return;
        }

        Collider hitCollider = collision.collider;

        if (
            hitCollider == null ||
            !IsGroundLayer(hitCollider) ||
            IsIgnoredOneWayPlatform(hitCollider)
        )
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            float slopeAngle = Vector3.Angle(contact.normal, Vector3.up);

            if (slopeAngle < VerticalWallAngle)
            {
                isGrounded = true;
                lastGroundedTime = Time.time;
                lastGroundContactTime = Time.time;
                LogGroundCollider(hitCollider);
                return;
            }
        }
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

            if (
                hit.collider == null ||
                IsOwnCollider(hit.collider) ||
                IsIgnoredOneWayPlatform(hit.collider)
            )
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

    private bool IsIgnoredOneWayPlatform(Collider targetCollider)
    {
        OneWayMeshPlatform3D oneWayPlatform =
            targetCollider.GetComponentInParent<OneWayMeshPlatform3D>();

        return oneWayPlatform != null && oneWayPlatform.IsPassingThrough;
    }

    private bool IsGroundLayer(Collider targetCollider)
    {
        int hitLayer = targetCollider.gameObject.layer;
        return (groundLayer.value & (1 << hitLayer)) != 0;
    }

    private bool HasRecentGroundContact()
    {
        return Time.time - lastGroundContactTime <= groundGraceDuration;
    }

    private void LogGroundCheckDebug(bool grounded, Vector3 checkPosition)
    {
        if (!logGroundCheckDebug || Time.time < nextGroundCheckDebugTime)
        {
            return;
        }

        nextGroundCheckDebugTime = Time.time + groundCheckDebugInterval;

        if (grounded)
        {
            Debug.Log("GroundCheck: T", this);
            return;
        }

        if (TryGetGroundCheckDistance(checkPosition, out float distanceToGround, out Collider groundCollider))
        {
            Debug.Log(
                "GroundCheck: F | distance to ground: " +
                distanceToGround.ToString("F3") +
                " | ground: " +
                groundCollider.name,
                groundCollider
            );
            return;
        }

        Debug.Log(
            "GroundCheck: F | no ground within " +
            groundCheckDebugRayDistance.ToString("F1") +
            " units",
            this
        );
    }

    private bool TryGetGroundCheckDistance(
        Vector3 checkPosition,
        out float distanceToGround,
        out Collider groundCollider
    )
    {
        int hitCount = Physics.RaycastNonAlloc(
            checkPosition,
            Vector3.down,
            slopeHits,
            groundCheckDebugRayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.PositiveInfinity;
        groundCollider = null;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = slopeHits[i];

            if (
                hit.collider == null ||
                IsOwnCollider(hit.collider) ||
                IsIgnoredOneWayPlatform(hit.collider)
            )
            {
                continue;
            }

            if (hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            groundCollider = hit.collider;
        }

        if (groundCollider == null)
        {
            distanceToGround = 0f;
            return false;
        }

        distanceToGround = Mathf.Max(0f, closestDistance - groundCheckRadius);
        return true;
    }

    private void LogGroundCollider(Collider groundCollider)
    {
        if (!logGroundColliderChanges || groundCollider == lastLoggedGroundCollider)
        {
            return;
        }

        lastLoggedGroundCollider = groundCollider;
        Debug.Log(
            "Standing on collider: " +
            groundCollider.name +
            " | Layer: " +
            LayerMask.LayerToName(groundCollider.gameObject.layer) +
            " | Path: " +
            GetHierarchyPath(groundCollider.transform),
            groundCollider
        );
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}
