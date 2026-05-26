using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HorizontalBarSwing2D : MonoBehaviour
{
    private static readonly List<HorizontalBarSwing2D> ActiveBars = new List<HorizontalBarSwing2D>();

    [Header("Grab")]
    [SerializeField] private Transform anchor;
    [SerializeField] private float grabRadius = 1.35f;
    [SerializeField] private LayerMask playerLayer = ~0;

    [Header("Swing")]
    [SerializeField] private float defaultHangDistance = 1.8f;
    [SerializeField] private float minHangDistance = 0.8f;
    [SerializeField] private float maxHangDistance = 3.5f;
    [SerializeField] private float releaseVelocityMultiplier = 1f;
    [SerializeField] private float maxSwingSpeed = 13f;
    [SerializeField] private int handSnapFrames = 3;
    [SerializeField] private bool staticHang = true;
    [SerializeField] private bool alignStaticHangToHands = true;

    [Header("2D Plane")]
    [SerializeField] private bool lockToAnchorZ = true;
    [SerializeField] private float planeZ;

    [Header("Runtime Visual")]
    [SerializeField] private bool createSimpleBarVisual = true;
    [SerializeField] private float visualLength = 1.4f;
    [SerializeField] private float visualRadius = 0.12f;
    [SerializeField] private Color visualColor = new Color(0.95f, 0.55f, 0.18f);

    private Rigidbody attachedBody;
    private PlayerController2D attachedController;
    private MonkeyKickController attachedKickController;
    private bool previousAlignBodyToRope;
    private bool hasPreviousAlignBodyToRope;
    private bool previousUseGravity;
    private bool previousIsKinematic;
    private float currentHangDistance;
    private int attachedFrame = -1;
    private int remainingHandSnapFrames;

    private void Awake()
    {
        if (anchor == null)
        {
            anchor = transform;
        }

        planeZ = lockToAnchorZ ? anchor.position.z : planeZ;

        if (createSimpleBarVisual)
        {
            CreateSimpleBarVisual();
        }
    }

    private void OnEnable()
    {
        if (!ActiveBars.Contains(this))
        {
            ActiveBars.Add(this);
        }
    }

    private void OnDisable()
    {
        Release(false);
        ActiveBars.Remove(this);
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

        if (staticHang)
        {
            HoldStillUnderBar(attachedBody);
        }
        else
        {
            ConstrainToBar(attachedBody);
        }
    }

    private void LateUpdate()
    {
        if (attachedBody != null && staticHang && alignStaticHangToHands)
        {
            AlignGrabHandsToAnchor();
            return;
        }

        if (remainingHandSnapFrames > 0)
        {
            AlignGrabHandsToAnchor();
            remainingHandSnapFrames--;
        }
    }

    public static bool TryGrabClosest(Rigidbody body, out HorizontalBarSwing2D grabbedBar)
    {
        grabbedBar = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < ActiveBars.Count; i++)
        {
            HorizontalBarSwing2D bar = ActiveBars[i];

            if (bar == null || !bar.CanAttach(body, out float sqrDistance))
            {
                continue;
            }

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                grabbedBar = bar;
            }
        }

        return grabbedBar != null && grabbedBar.TryAttach(body);
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

        Vector3 offset = body.position - anchor.position;
        offset.z = 0f;
        sqrDistance = offset.sqrMagnitude;

        return sqrDistance <= grabRadius * grabRadius;
    }

    private void Attach(Rigidbody body)
    {
        attachedBody = body;
        attachedFrame = Time.frameCount;
        attachedController = attachedBody.GetComponent<PlayerController2D>();
        attachedKickController = attachedBody.GetComponent<MonkeyKickController>();
        previousUseGravity = attachedBody.useGravity;
        previousIsKinematic = attachedBody.isKinematic;

        attachedController?.SetExternalMotionActive(true);
        attachedKickController?.SetGrabbing(true, anchor);
        if (attachedKickController != null)
        {
            previousAlignBodyToRope = attachedKickController.alignBodyToRope;
            hasPreviousAlignBodyToRope = true;
            attachedKickController.alignBodyToRope = !staticHang && previousAlignBodyToRope;
            attachedKickController.allowKickInput = !staticHang;
        }

        KeepBodyOnPlane(attachedBody);
        currentHangDistance = Mathf.Clamp(defaultHangDistance, minHangDistance, maxHangDistance);
        MoveBodyToHangDirection(attachedBody, Vector3.down);

        if (staticHang)
        {
            attachedBody.useGravity = false;
            attachedBody.isKinematic = true;
        }

        attachedBody.linearVelocity = Vector3.zero;
        attachedKickController?.ForceGrabArmPose();

        if (!staticHang)
        {
            AlignGrabHandsToAnchor();
            remainingHandSnapFrames = Mathf.Max(1, handSnapFrames);
        }
        else
        {
            remainingHandSnapFrames = 0;
        }

        attachedBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
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

        attachedBody = null;
        attachedFrame = -1;
        attachedController = null;
        attachedKickController = null;
        remainingHandSnapFrames = 0;
        body.useGravity = previousUseGravity;
        body.isKinematic = previousIsKinematic;

        controller?.SetExternalMotionActive(false);
        kickController?.SetGrabbing(false, null);
        if (kickController != null)
        {
            if (hasPreviousAlignBodyToRope)
            {
                kickController.alignBodyToRope = previousAlignBodyToRope;
            }

            kickController.allowKickInput = true;
        }

        hasPreviousAlignBodyToRope = false;

        if (launch)
        {
            releaseVelocity.x *= releaseVelocityMultiplier;
            releaseVelocity.y *= releaseVelocityMultiplier;
            releaseVelocity.z = 0f;
            body.linearVelocity = releaseVelocity;
        }
    }

    private void ConstrainToBar(Rigidbody body)
    {
        Vector3 fromAnchor = body.position - anchor.position;
        fromAnchor.z = 0f;

        if (fromAnchor.sqrMagnitude <= 0.001f)
        {
            fromAnchor = Vector3.down;
        }

        Vector3 hangDirection = fromAnchor.normalized;
        MoveBodyToHangDirection(body, hangDirection);

        Vector3 velocity = body.linearVelocity;
        velocity -= hangDirection * Vector3.Dot(velocity, hangDirection);
        velocity.z = 0f;

        if (maxSwingSpeed > 0f && velocity.magnitude > maxSwingSpeed)
        {
            velocity = velocity.normalized * maxSwingSpeed;
        }

        body.linearVelocity = velocity;
    }

    private void HoldStillUnderBar(Rigidbody body)
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void AlignGrabHandsToAnchor()
    {
        if (attachedBody == null || attachedKickController == null)
        {
            return;
        }

        if (!attachedKickController.TryGetGrabHandPosition(out Vector3 handPosition))
        {
            return;
        }

        Vector3 correction = anchor.position - handPosition;
        correction.z = 0f;

        if (correction.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector3 position = attachedBody.position + correction;
        position.z = planeZ;
        attachedBody.position = position;

        if (!staticHang)
        {
            Vector3 fromAnchor = attachedBody.position - anchor.position;
            fromAnchor.z = 0f;

            if (fromAnchor.sqrMagnitude > 0.001f)
            {
                currentHangDistance = Mathf.Clamp(fromAnchor.magnitude, minHangDistance, maxHangDistance);
            }
        }
    }

    private void MoveBodyToHangDirection(Rigidbody body, Vector3 hangDirection)
    {
        Vector3 position = anchor.position + hangDirection * currentHangDistance;
        position.z = planeZ;
        body.position = position;
    }

    private void KeepBodyOnPlane(Rigidbody body)
    {
        if (!lockToAnchorZ)
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

    private void CreateSimpleBarVisual()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Horizontal Bar Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(visualRadius * 2f, visualLength * 0.5f, visualRadius * 2f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = visualColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform anchorTransform = anchor != null ? anchor : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(anchorTransform.position, grabRadius);
        Gizmos.DrawLine(
            anchorTransform.position + Vector3.back * visualLength * 0.5f,
            anchorTransform.position + Vector3.forward * visualLength * 0.5f
        );
    }
}
