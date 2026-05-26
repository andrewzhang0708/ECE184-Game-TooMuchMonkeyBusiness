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

    [Header("Gravity")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [Tooltip("If enabled, the extra gravity is applied while ascending too. Keeps up/down force symmetric.")]
    [SerializeField] private bool applyGravityWhileAscending = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer = ~0;

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

    private bool isGrounded;
    private bool externalMotionActive;

    private Quaternion facingRightRotation;
    private Quaternion facingLeftRotation;

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

        if (jumpPressed && isGrounded)
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
        Move();
        ApplyExtraGravity();
        UpdateAnimator();
    }

    private void ApplyExtraGravity()
    {
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

        if (isGrounded)
        {
            velocity.x = horizontal * moveSpeed;
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

    private void Jump()
    {
        isGrounded = false;

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
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    public void SetExternalMotionActive(bool isActive)
    {
        externalMotionActive = isActive;
    }

    private void CheckGround()
    {
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

        isGrounded = false;

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
            return;
        }
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
