using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class TrampolineRope2D : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private Transform anchor;
    [SerializeField] private float ropeLength = 5f;
    [SerializeField] private float grabRadius = 1.2f;
    [SerializeField] private bool keepRopeTaut = true;
    [SerializeField] private LayerMask playerLayer = ~0;

    [Header("Rope Visual")]
    [SerializeField] private int visualSegments = 18;
    [SerializeField] private float visualSag = 0.25f;
    [SerializeField] private float visualBend = 0.12f;
    [SerializeField] private float visualBendSmoothing = 10f;
    [SerializeField] private float idleSwingGravity = 25f;
    [SerializeField] private float idleSwingDamping = 2.8f;
    [SerializeField] private float idleReleaseVelocityMultiplier = 1f;

    [Header("Swing")]
    [SerializeField] private float pumpForce = 24f;
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float minClimbLength = 0.6f;
    [SerializeField] private float releaseVelocityMultiplier = 1f;
    [SerializeField] private float maxSwingSpeed = 12f;

    [Header("2D Plane")]
    [SerializeField] private bool lockToRopeZ = true;
    [SerializeField] private float planeZ;

    private LineRenderer lineRenderer;
    private Rigidbody attachedBody;
    private PlayerController2D attachedController;
    private readonly Collider[] grabHits = new Collider[8];
    private float currentRopeLength;
    private float visualBendOffset;
    private float idleSwingAngle;
    private float idleSwingAngularVelocity;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = Mathf.Max(2, visualSegments);
        lineRenderer.useWorldSpace = true;

        if (anchor == null)
        {
            anchor = transform;
        }

        planeZ = lockToRopeZ ? anchor.position.z : planeZ;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (attachedBody == null)
        {
            if (keyboard.eKey.wasPressedThisFrame)
            {
                TryGrab();
            }

            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            Release(true);
        }
    }

    private void FixedUpdate()
    {
        if (attachedBody == null)
        {
            return;
        }

        KeepBodyOnPlane(attachedBody);
        ClimbRope();
        ConstrainToRopeLength(attachedBody);
        PumpSwing();
    }

    private void LateUpdate()
    {
        UpdateIdleSwing();
        DrawRope();
    }

    private void DrawRope()
    {
        int segmentCount = Mathf.Max(2, visualSegments);

        if (lineRenderer.positionCount != segmentCount)
        {
            lineRenderer.positionCount = segmentCount;
        }

        Vector3 start = anchor.position;
        Vector3 end = GetVisualRopeEnd();
        Vector3 ropeVector = end - start;
        Vector3 bendNormal = Vector3.Cross(Vector3.forward, ropeVector).normalized;

        float targetBend = GetTargetVisualBend(bendNormal);
        visualBendOffset = Mathf.Lerp(
            visualBendOffset,
            targetBend,
            1f - Mathf.Exp(-visualBendSmoothing * Time.deltaTime)
        );

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            float arc = Mathf.Sin(t * Mathf.PI);
            Vector3 position = Vector3.Lerp(start, end, t);
            position += Vector3.down * (visualSag * arc);
            position += bendNormal * (visualBendOffset * arc);
            position.z = planeZ;
            lineRenderer.SetPosition(i, position);
        }
    }

    private Vector3 GetVisualRopeEnd()
    {
        if (attachedBody == null)
        {
            Vector3 idleDirection = Quaternion.AngleAxis(idleSwingAngle * Mathf.Rad2Deg, Vector3.forward) * Vector3.down;
            Vector3 idleEnd = anchor.position + idleDirection * ropeLength;
            idleEnd.z = planeZ;
            return idleEnd;
        }

        Vector3 fromAnchor = attachedBody.position - anchor.position;
        fromAnchor.z = 0f;

        if (fromAnchor.sqrMagnitude <= 0.001f)
        {
            return anchor.position + Vector3.down * ropeLength;
        }

        Vector3 endPosition = anchor.position + fromAnchor.normalized * ropeLength;
        endPosition.z = planeZ;
        return endPosition;
    }

    private float GetTargetVisualBend(Vector3 bendNormal)
    {
        if (attachedBody == null)
        {
            return 0f;
        }

        float sidewaysSpeed = Vector3.Dot(attachedBody.linearVelocity, bendNormal);
        return Mathf.Clamp(-sidewaysSpeed * visualBend, -0.8f, 0.8f);
    }

    private void UpdateIdleSwing()
    {
        if (attachedBody != null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        float angularAcceleration = -idleSwingGravity / Mathf.Max(ropeLength, 0.01f) * Mathf.Sin(idleSwingAngle);
        idleSwingAngularVelocity += angularAcceleration * deltaTime;
        idleSwingAngularVelocity *= Mathf.Exp(-idleSwingDamping * deltaTime);
        idleSwingAngle += idleSwingAngularVelocity * deltaTime;

        if (Mathf.Abs(idleSwingAngle) < 0.001f && Mathf.Abs(idleSwingAngularVelocity) < 0.001f)
        {
            idleSwingAngle = 0f;
            idleSwingAngularVelocity = 0f;
        }
    }

    private void OnDisable()
    {
        Release(false);
    }

    private void TryGrab()
    {
        Vector3 ropeEnd = anchor.position + Vector3.down * ropeLength;
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            anchor.position,
            ropeEnd,
            grabRadius,
            grabHits,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Rigidbody candidate = grabHits[i].attachedRigidbody;

            if (candidate != null)
            {
                Attach(candidate);
                return;
            }
        }
    }

    private void Attach(Rigidbody body)
    {
        attachedBody = body;
        attachedController = attachedBody.GetComponent<PlayerController2D>();

        attachedController?.SetExternalMotionActive(true);
        KeepBodyOnPlane(attachedBody);
        SyncIdleSwingToBody(attachedBody);

        float grabDistance = Vector3.Distance(anchor.position, attachedBody.position);
        currentRopeLength = Mathf.Min(grabDistance, ropeLength);

        attachedBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void PumpSwing()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float input = 0f;

        if (keyboard.aKey.isPressed)
        {
            input -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            input += 1f;
        }

        if (Mathf.Approximately(input, 0f))
        {
            return;
        }

        Vector3 fromAnchor = attachedBody.position - anchor.position;
        Vector3 tangent = Vector3.Cross(Vector3.forward, fromAnchor).normalized;
        float distanceScale = Mathf.Clamp01(fromAnchor.magnitude / Mathf.Max(ropeLength, 0.01f));
        attachedBody.AddForce(tangent * input * pumpForce * distanceScale, ForceMode.Acceleration);
    }

    private void ClimbRope()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float input = 0f;

        if (keyboard.wKey.isPressed)
        {
            input -= 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            input += 1f;
        }

        if (Mathf.Approximately(input, 0f))
        {
            return;
        }

        float previousLength = currentRopeLength;
        currentRopeLength = Mathf.Clamp(
            currentRopeLength + input * climbSpeed * Time.fixedDeltaTime,
            minClimbLength,
            ropeLength
        );

        if (Mathf.Approximately(previousLength, currentRopeLength))
        {
            return;
        }

        MoveBodyToCurrentRopeLength(attachedBody);
    }

    private void MoveBodyToCurrentRopeLength(Rigidbody body)
    {
        Vector3 fromAnchor = body.position - anchor.position;
        fromAnchor.z = 0f;

        if (fromAnchor.sqrMagnitude <= 0.001f)
        {
            fromAnchor = Vector3.down;
        }

        Vector3 ropeDirection = fromAnchor.normalized;
        Vector3 position = anchor.position + ropeDirection * currentRopeLength;
        position.z = planeZ;
        body.position = position;

        Vector3 velocity = body.linearVelocity;
        velocity -= ropeDirection * Vector3.Dot(velocity, ropeDirection);
        velocity.z = 0f;
        body.linearVelocity = velocity;
    }

    private void Release(bool launch)
    {
        if (attachedBody == null)
        {
            return;
        }

        Rigidbody body = attachedBody;
        PlayerController2D controller = attachedController;
        Vector3 releaseVelocity = body.linearVelocity;
        CacheIdleSwingFromBody(body, releaseVelocity);

        attachedBody = null;
        attachedController = null;

        controller?.SetExternalMotionActive(false);

        if (launch)
        {
            releaseVelocity.x *= releaseVelocityMultiplier;
            releaseVelocity.y *= releaseVelocityMultiplier;
            releaseVelocity.z = 0f;
            body.linearVelocity = releaseVelocity;
        }
    }

    private void SyncIdleSwingToBody(Rigidbody body)
    {
        Vector3 fromAnchor = body.position - anchor.position;
        fromAnchor.z = 0f;

        if (fromAnchor.sqrMagnitude <= 0.001f)
        {
            idleSwingAngle = 0f;
            idleSwingAngularVelocity = 0f;
            return;
        }

        Vector3 direction = fromAnchor.normalized;
        idleSwingAngle = Mathf.Atan2(direction.x, -direction.y);
        idleSwingAngularVelocity = 0f;
    }

    private void CacheIdleSwingFromBody(Rigidbody body, Vector3 releaseVelocity)
    {
        Vector3 fromAnchor = body.position - anchor.position;
        fromAnchor.z = 0f;

        float radius = Mathf.Max(fromAnchor.magnitude, 0.01f);
        Vector3 direction = fromAnchor.normalized;
        Vector3 tangent = Vector3.Cross(Vector3.forward, direction).normalized;

        idleSwingAngle = Mathf.Atan2(direction.x, -direction.y);
        idleSwingAngularVelocity = Vector3.Dot(releaseVelocity, tangent) / radius * idleReleaseVelocityMultiplier;
    }

    private void ConstrainToRopeLength(Rigidbody body)
    {
        Vector3 fromAnchor = body.position - anchor.position;
        fromAnchor.z = 0f;

        float distance = fromAnchor.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 ropeDirection = fromAnchor / distance;

        if (keepRopeTaut || distance > currentRopeLength)
        {
            Vector3 constrainedPosition = anchor.position + ropeDirection * currentRopeLength;
            constrainedPosition.z = planeZ;
            body.position = constrainedPosition;
        }

        Vector3 velocity = body.linearVelocity;
        float radialSpeed = Vector3.Dot(velocity, ropeDirection);

        if (keepRopeTaut)
        {
            velocity -= ropeDirection * radialSpeed;
            velocity.z = 0f;
            body.linearVelocity = velocity;
            ClampTangentSpeed(body, ropeDirection);
            return;
        }

        if (radialSpeed > 0f)
        {
            velocity -= ropeDirection * radialSpeed;
            velocity.z = 0f;
            body.linearVelocity = velocity;
        }

        ClampTangentSpeed(body, ropeDirection);
    }

    private void ClampTangentSpeed(Rigidbody body, Vector3 ropeDirection)
    {
        if (maxSwingSpeed <= 0f)
        {
            return;
        }

        Vector3 velocity = body.linearVelocity;
        Vector3 tangentVelocity = velocity - ropeDirection * Vector3.Dot(velocity, ropeDirection);

        if (tangentVelocity.magnitude <= maxSwingSpeed)
        {
            return;
        }

        body.linearVelocity = tangentVelocity.normalized * maxSwingSpeed;
    }

    private void KeepBodyOnPlane(Rigidbody body)
    {
        if (!lockToRopeZ)
        {
            return;
        }

        Vector3 position = body.position;
        position.z = planeZ;
        body.position = position;

        Vector3 velocity = body.linearVelocity;
        velocity.z = 0f;
        body.linearVelocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        Transform anchorTransform = anchor != null ? anchor : transform;
        Vector3 grabCenter = anchorTransform.position + Vector3.down * ropeLength;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(anchorTransform.position, grabCenter);
        Gizmos.DrawWireSphere(anchorTransform.position, grabRadius);
        Gizmos.DrawWireSphere(grabCenter, grabRadius);
    }
}
