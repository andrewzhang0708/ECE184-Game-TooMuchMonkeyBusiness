using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RollingSphereEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private int startDirection = 1;
    [SerializeField] private bool lockToXAxis = true;

    [Header("Collision")]
    [Tooltip("Which layers count as walls. Ground contacts are ignored unless their collision normal is wall-like.")]
    [SerializeField] private LayerMask wallLayers = ~0;
    [Tooltip("Higher values require a more vertical wall before bouncing. Ground is usually near 0.")]
    [SerializeField, Range(0f, 1f)] private float minWallNormalX = 0.65f;

    [Header("Visual Roll")]
    [Tooltip("Drag the child SpriteRenderer transform here. Leave empty to auto-find one in children.")]
    [SerializeField] private Transform rollingVisual;
    [Tooltip("2D sprites usually roll around Z when moving along X.")]
    [SerializeField] private Vector3 visualRollAxis = Vector3.forward;
    [SerializeField] private float visualRadius = 0f;
    [SerializeField] private float visualRollMultiplier = 1f;

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private int direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        direction = startDirection >= 0 ? 1 : -1;

        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = false;

        if (rollingVisual == null)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            rollingVisual = spriteRenderer != null ? spriteRenderer.transform : null;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (lockToXAxis)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationY;
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = direction * moveSpeed;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        RollVisual();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryBounce(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryBounce(collision);
    }

    private void TryBounce(Collision collision)
    {
        if (!IsInWallLayer(collision.gameObject.layer))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            if (!IsWallBlockingCurrentDirection(contact.normal))
            {
                continue;
            }

            direction *= -1;
            return;
        }
    }

    private bool IsInWallLayer(int layer)
    {
        return (wallLayers.value & (1 << layer)) != 0;
    }

    private bool IsWallBlockingCurrentDirection(Vector3 normal)
    {
        if (Mathf.Abs(normal.x) < minWallNormalX)
        {
            return false;
        }

        return (direction > 0 && normal.x < 0f)
            || (direction < 0 && normal.x > 0f);
    }

    private void RollVisual()
    {
        if (rollingVisual == null)
        {
            return;
        }

        float radius = visualRadius > 0f
            ? visualRadius
            : sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        if (radius <= 0f)
        {
            return;
        }

        float degreesPerSecond = moveSpeed / radius * Mathf.Rad2Deg * visualRollMultiplier;
        rollingVisual.Rotate(visualRollAxis, -direction * degreesPerSecond * Time.fixedDeltaTime, Space.Self);
    }
}
