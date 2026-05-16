using UnityEngine;
using UnityEngine.InputSystem;

// https://poki.com/en/g/papa-louie-3?campaign=22729182886&adgroup=185500509270&extensionid=&targetid=dsa-1463903668522&location=9060098&matchtype=&network=g&device=c&devicemodel=&creative=760735981576&keyword=&placement=&target=&gad_source=1&gad_campaignid=22729182886&gbraid=0AAAAADlg9ZGIzckQ07fCJQi8eF98UJ-jz&gclid=Cj0KCQjwiJvQBhCYARIsAMjts3Lq3HTXpJoPvLrWaDdAa2BPRhk0rOlp-a_g7L5GwlqS6oO0YXudrL8aApogEALw_wcB

[RequireComponent(typeof(Rigidbody))]
public class PlayerController2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float airAcceleration = 18f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody rb;
    private Collider[] ownColliders;
    private readonly Collider[] groundHits = new Collider[8];
    private bool isGrounded;
    private bool externalMotionActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ownColliders = GetComponentsInChildren<Collider>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
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

        if (keyboard.wKey.wasPressedThisFrame && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        if (externalMotionActive)
        {
            return;
        }

        CheckGround();

        Move();
    }

    private void Move()
    {
        float horizontal = 0f;
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.aKey.isPressed)
        {
            // Debug.Log("A key is pressed");
            // Debug.Log("Horizontal before: " + horizontal);
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontal += 1f;
        }

        Vector3 velocity = rb.linearVelocity;

        if (isGrounded)
        {
            velocity.x = horizontal * moveSpeed;
        }
        else if (!Mathf.Approximately(horizontal, 0f))
        {
            float targetSpeed = horizontal * moveSpeed;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, airAcceleration * Time.fixedDeltaTime);
        }

        velocity.z = 0f;
        rb.linearVelocity = velocity;

        if (!Mathf.Approximately(horizontal, 0f))
        {
            transform.forward = horizontal > 0f ? Vector3.right : Vector3.left;
        }
    }

    private void Jump()
    {
        isGrounded = false;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void SetExternalMotionActive(bool isActive)
    {
        externalMotionActive = isActive;
    }

    private void CheckGround()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.55f;
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
            if (groundHits[i] != null && !IsOwnCollider(groundHits[i]))
            {
                isGrounded = true;
                return;
            }
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
