using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class TrampolineRope2D : MonoBehaviour
{
    private static readonly List<TrampolineRope2D> ActiveRopes = new List<TrampolineRope2D>();

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
    [SerializeField] private bool useKickControllerForSwing = true;
    [SerializeField] private float pumpForce = 24f;
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float minClimbLength = 0.6f;
    [SerializeField] private float releaseVelocityMultiplier = 1f;
    [SerializeField] private float maxSwingSpeed = 12f;

    [Header("Hand Grab Alignment")]
    [SerializeField] private bool alignHandsToRopeOnGrab = true;
    [SerializeField] private int handAlignFrames = 4;

    [Header("2D Plane")]
    [SerializeField] private bool lockToRopeZ = true;
    [SerializeField] private float planeZ;

    private LineRenderer lineRenderer;
    private Rigidbody attachedBody;
    private PlayerController2D attachedController;
    private MonkeyKickController attachedKickController;
    private float currentRopeLength;
    private float visualBendOffset;
    private float idleSwingAngle;
    private float idleSwingAngularVelocity;
    private int attachedFrame = -1;
    private int remainingHandAlignFrames;

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

    private void OnEnable()
    {
        if (!ActiveRopes.Contains(this))
        {
            ActiveRopes.Add(this);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (attachedBody != null && Time.frameCount > attachedFrame && keyboard.eKey.wasPressedThisFrame)
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

        if (!useKickControllerForSwing || attachedKickController == null)
        {
            PumpSwing();
        }
    }

    private void LateUpdate()
    {
        AlignHandsToRopeWhileGrabbing();
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
        ActiveRopes.Remove(this);
    }

    public static bool TryGrabClosest(Rigidbody body, out TrampolineRope2D grabbedRope)
    {
        grabbedRope = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < ActiveRopes.Count; i++)
        {
            TrampolineRope2D rope = ActiveRopes[i];

            if (rope == null || !rope.CanAttach(body, out float sqrDistance))
            {
                continue;
            }

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                grabbedRope = rope;
            }
        }

        return grabbedRope != null && grabbedRope.TryAttach(body);
    }

    public bool TryAttach(Rigidbody body)
    {
        if (!CanAttach(body, out _))
        {
            return false;
        }

        Attach(body);
        return true;
    }

    private bool CanAttach(Rigidbody body, out float sqrDistance)
    {
        sqrDistance = float.PositiveInfinity;

        if (body == null || attachedBody != null)
        {
            return false;
        }

        if (((1 << body.gameObject.layer) & playerLayer.value) == 0)
        {
            return false;
        }

        Vector3 closestPoint = GetClosestPointOnRope(body.position);
        Vector3 offset = body.position - closestPoint;
        offset.z = 0f;
        sqrDistance = offset.sqrMagnitude;

        return sqrDistance <= grabRadius * grabRadius;
    }

    private Vector3 GetClosestPointOnRope(Vector3 position)
    {
        Vector3 start = anchor.position;
        Vector3 end = GetVisualRopeEnd();
        Vector3 ropeVector = end - start;
        ropeVector.z = 0f;

        if (ropeVector.sqrMagnitude <= 0.001f)
        {
            return start;
        }

        Vector3 fromStart = position - start;
        fromStart.z = 0f;
        float t = Mathf.Clamp01(Vector3.Dot(fromStart, ropeVector) / ropeVector.sqrMagnitude);
        Vector3 closestPoint = start + ropeVector * t;
        closestPoint.z = planeZ;
        return closestPoint;
    }

    private void AlignHandsToRopeWhileGrabbing()
    {
        if (!alignHandsToRopeOnGrab || remainingHandAlignFrames <= 0)
        {
            return;
        }

        remainingHandAlignFrames--;

        if (attachedBody == null || attachedKickController == null)
        {
            return;
        }

        if (!attachedKickController.TryGetGrabHandPosition(out Vector3 handPosition))
        {
            return;
        }

        if (!TryGetPointOnRopeAtY(handPosition.y, out Vector3 ropePoint))
        {
            return;
        }

        Vector3 correction = ropePoint - handPosition;
        correction.z = 0f;

        if (correction.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector3 position = attachedBody.position + correction;
        position.z = planeZ;
        attachedBody.position = position;

        Vector3 fromAnchor = attachedBody.position - anchor.position;
        fromAnchor.z = 0f;

        if (fromAnchor.sqrMagnitude > 0.001f)
        {
            currentRopeLength = Mathf.Clamp(fromAnchor.magnitude, minClimbLength, ropeLength);
        }
    }

    private bool TryGetPointOnRopeAtY(float y, out Vector3 point)
    {
        Vector3 start = anchor.position;
        Vector3 end = GetVisualRopeEnd();
        start.z = planeZ;
        end.z = planeZ;

        float yRange = end.y - start.y;

        if (Mathf.Abs(yRange) <= 0.001f)
        {
            point = GetClosestPointOnRope(new Vector3(anchor.position.x, y, planeZ));
            return true;
        }

        float t = Mathf.Clamp01((y - start.y) / yRange);
        point = Vector3.Lerp(start, end, t);
        point.z = planeZ;
        return true;
    }

    private void Attach(Rigidbody body)
    {
        attachedBody = body;
        attachedFrame = Time.frameCount;
        attachedController = attachedBody.GetComponent<PlayerController2D>();
        attachedKickController = attachedBody.GetComponent<MonkeyKickController>();

        attachedController?.SetExternalMotionActive(true);
        attachedKickController?.SetGrabbing(true, anchor);
        KeepBodyOnPlane(attachedBody);
        SyncIdleSwingToBody(attachedBody);

        Vector3 fromAnchor = attachedBody.position - anchor.position;
        fromAnchor.z = 0f;
        float grabDistance = fromAnchor.magnitude;
        currentRopeLength = Mathf.Clamp(grabDistance, minClimbLength, ropeLength);
        MoveBodyToCurrentRopeLength(attachedBody);
        remainingHandAlignFrames = alignHandsToRopeOnGrab ? Mathf.Max(1, handAlignFrames) : 0;

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
        MonkeyKickController kickController = attachedKickController;
        Vector3 releaseVelocity = body.linearVelocity;
        CacheIdleSwingFromBody(body, releaseVelocity);

        attachedBody = null;
        attachedFrame = -1;
        attachedController = null;
        attachedKickController = null;
        remainingHandAlignFrames = 0;

        controller?.SetExternalMotionActive(false);
        kickController?.SetGrabbing(false, null);

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
