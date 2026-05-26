using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHandGrabSwing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController2D playerController;
    [SerializeField] private Transform handGrabPoint;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private bool setAnimatorSwingingParameter = true;
    [SerializeField] private string swingingParameterName = "IsSwinging";

    [Header("Grab")]
    [SerializeField] private float maxGrabDistance = 1.25f;
    [SerializeField] private float snapSpeedLimit = 16f;
    [SerializeField] private bool allowEKeyGrab = true;

    [Header("Debug")]
    [SerializeField] private bool logHandDistanceToClosestBar = true;
    [SerializeField] private bool drawHandDistanceLine = true;

    [Header("Swing Motor")]
    [SerializeField] private float hingeForce = 180f;
    [SerializeField] private float targetVelocity = 220f;
    [SerializeField] private float startupTangentSpeed = 2f;
    [SerializeField] private float startupAngularSpeed = 3f;
    [SerializeField] private bool useAssistTorque = true;
    [SerializeField] private float assistTorqueAcceleration = 45f;
    [SerializeField] private int defaultStartupDirection = 1;

    [Header("Release")]
    [SerializeField] private float releaseBoost = 1f;
    [SerializeField] private float jumpReleaseImpulse = 5f;

    private readonly List<HandSwingBar> nearbyBars = new List<HandSwingBar>();
    private readonly List<HandSwingBar> highlightedBars = new List<HandSwingBar>();
    private readonly List<HandSwingBar> barsInRange = new List<HandSwingBar>();

    private Rigidbody rb;
    private HingeJoint swingJoint;
    private RigidbodyConstraints savedConstraints;
    private bool savedUseGravity;
    private bool savedIsKinematic;
    private bool isSwinging;

    public bool IsSwinging => isSwinging;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        RefreshBarOutlines();
        LogHandDistanceToClosestBar();

        if (isSwinging && WasJumpPressedThisFrame())
        {
            Release(true);
            return;
        }

        if (!WasGrabPressedThisFrame())
        {
            return;
        }

        if (isSwinging)
        {
            Release(false);
            return;
        }

        TryGrabClosest();
    }

    private void FixedUpdate()
    {
        if (!isSwinging || swingJoint == null)
        {
            return;
        }

        float swingInput = ReadSwingInput();
        bool hasInput = Mathf.Abs(swingInput) > 0.01f;

        JointMotor motor = swingJoint.motor;
        motor.force = hasInput ? hingeForce : 0f;
        motor.targetVelocity = swingInput * targetVelocity;

        swingJoint.motor = motor;
        swingJoint.useMotor = hasInput;

        if (useAssistTorque && hasInput)
        {
            rb.AddTorque(Vector3.forward * swingInput * assistTorqueAcceleration, ForceMode.Acceleration);
            rb.WakeUp();
        }

        if (logHandDistanceToClosestBar)
        {
            Vector3 worldAnchor = transform.TransformPoint(swingJoint.anchor);
            Debug.Log(
                $"Swing input: {swingInput:F2}, velocity: {rb.linearVelocity}, jointAngle: {swingJoint.angle:F2}, jointVelocity: {swingJoint.velocity:F2}, worldAnchor: {worldAnchor}, connectedAnchor: {swingJoint.connectedAnchor}, useGravity: {rb.useGravity}, isKinematic: {rb.isKinematic}, constraints: {rb.constraints}, angularVelocity: {rb.angularVelocity}",
                this
            );
        }
    }

    public bool TryGrabClosest()
    {
        HandSwingBar closestBar = FindClosestBar();

        if (closestBar == null)
        {
            return false;
        }

        Grab(closestBar);
        return true;
    }

    private void OnDisable()
    {
        ClearBarOutlines();
    }

    private void Grab(HandSwingBar bar)
    {
        if (isSwinging)
        {
            return;
        }

        Vector3 grabPoint = GetHandPosition();
        Vector3 offsetToBar = bar.GrabPoint - grabPoint;
        offsetToBar.z = 0f;

        if (offsetToBar.sqrMagnitude > 0f)
        {
            transform.position += offsetToBar;
            Physics.SyncTransforms();
        }

        savedConstraints = rb.constraints;
        savedUseGravity = rb.useGravity;
        savedIsKinematic = rb.isKinematic;

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY;
        rb.angularVelocity = Vector3.zero;

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, snapSpeedLimit);
        ClearBarOutlines();

        Vector3 handPosition = GetHandPosition();
        Vector3 connectedAnchor = bar.GrabPoint;
        connectedAnchor.z = handPosition.z;

        swingJoint = gameObject.AddComponent<HingeJoint>();
        swingJoint.autoConfigureConnectedAnchor = false;
        swingJoint.connectedBody = null;
        swingJoint.connectedAnchor = connectedAnchor;
        swingJoint.anchor = transform.InverseTransformPoint(handPosition);
        swingJoint.axis = transform.InverseTransformDirection(Vector3.forward);
        swingJoint.useLimits = false;
        swingJoint.useSpring = false;
        swingJoint.useMotor = false;
        swingJoint.enablePreprocessing = false;

        AddStartupSwingVelocity(connectedAnchor);
        rb.WakeUp();

        isSwinging = true;
        SetAnimatorSwinging(true);

        if (playerController != null)
        {
            playerController.SetExternalMotionActive(true);
        }
    }

    private void Release(bool jumpRelease)
    {
        if (!isSwinging)
        {
            return;
        }

        if (swingJoint != null)
        {
            Destroy(swingJoint);
            swingJoint = null;
        }

        rb.constraints = savedConstraints;
        rb.useGravity = savedUseGravity;
        rb.isKinematic = savedIsKinematic;

        Vector3 releaseVelocity = rb.linearVelocity * releaseBoost;

        if (jumpRelease)
        {
            releaseVelocity.y = Mathf.Max(releaseVelocity.y, jumpReleaseImpulse);
        }

        releaseVelocity.z = 0f;
        rb.linearVelocity = releaseVelocity;

        isSwinging = false;
        SetAnimatorSwinging(false);

        if (playerController != null)
        {
            playerController.SetExternalMotionActive(false);
        }
    }

    private void SetAnimatorSwinging(bool swinging)
    {
        if (!setAnimatorSwingingParameter || animator == null || string.IsNullOrEmpty(swingingParameterName))
        {
            return;
        }

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter parameter = animator.GetParameter(i);

            if (parameter.type == AnimatorControllerParameterType.Bool
                && parameter.name == swingingParameterName)
            {
                animator.SetBool(swingingParameterName, swinging);
                return;
            }
        }
    }

    private void AddStartupSwingVelocity(Vector3 anchor)
    {
        Vector3 fromAnchor = rb.position - anchor;
        fromAnchor.z = 0f;

        if (fromAnchor.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 tangent = Vector3.Cross(Vector3.forward, fromAnchor.normalized);
        float input = ReadSwingInput();

        if (Mathf.Abs(input) < 0.01f)
        {
            input = Mathf.Sign(rb.linearVelocity.x);
        }

        if (Mathf.Abs(input) < 0.01f)
        {
            input = defaultStartupDirection >= 0 ? 1f : -1f;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity += tangent * input * startupTangentSpeed;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        Vector3 angularVelocity = rb.angularVelocity;
        angularVelocity.z += input * startupAngularSpeed;
        rb.angularVelocity = angularVelocity;
    }

    private HandSwingBar FindClosestBar()
    {
        Vector3 grabPoint = GetHandPosition();
        HandSwingBar closestBar = null;
        float closestDistanceSqr = maxGrabDistance * maxGrabDistance;

        IReadOnlyList<HandSwingBar> activeBars = HandSwingBar.Bars;

        for (int i = 0; i < activeBars.Count; i++)
        {
            HandSwingBar bar = activeBars[i];

            if (bar == null)
            {
                continue;
            }

            float distanceSqr = GetPlanarDistanceSqr(grabPoint, bar.GrabPoint);

            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestBar = bar;
            }
        }

        if (closestBar != null)
        {
            return closestBar;
        }

        for (int i = nearbyBars.Count - 1; i >= 0; i--)
        {
            HandSwingBar bar = nearbyBars[i];

            if (bar == null)
            {
                nearbyBars.RemoveAt(i);
                continue;
            }

            float distanceSqr = GetPlanarDistanceSqr(grabPoint, bar.GrabPoint);

            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestBar = bar;
            }
        }

        return closestBar;
    }

    private void LogHandDistanceToClosestBar()
    {
        if (!logHandDistanceToClosestBar)
        {
            return;
        }

        Vector3 grabPoint = GetHandPosition();
        HandSwingBar closestBar = null;
        float closestDistanceSqr = float.PositiveInfinity;
        IReadOnlyList<HandSwingBar> activeBars = HandSwingBar.Bars;

        for (int i = 0; i < activeBars.Count; i++)
        {
            HandSwingBar bar = activeBars[i];

            if (bar == null)
            {
                continue;
            }

            float distanceSqr = GetPlanarDistanceSqr(grabPoint, bar.GrabPoint);

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestBar = bar;
            }
        }

        if (closestBar == null)
        {
            // Debug.Log("Hand grab distance: no HandSwingBar active");
            return;
        }

        float distance = Mathf.Sqrt(closestDistanceSqr);
        // Debug.Log($"Hand grab distance to closest bar: {distance:F3} / max {maxGrabDistance:F3}", this);
    }

    private void LateUpdate()
    {
        DrawHandDistanceLine();
    }

    private void DrawHandDistanceLine()
    {
        if (!drawHandDistanceLine)
        {
            return;
        }

        Vector3 grabPoint = GetHandPosition();
        HandSwingBar closestBar = null;
        float closestDistanceSqr = float.PositiveInfinity;
        IReadOnlyList<HandSwingBar> activeBars = HandSwingBar.Bars;

        for (int i = 0; i < activeBars.Count; i++)
        {
            HandSwingBar bar = activeBars[i];

            if (bar == null)
            {
                continue;
            }

            float distanceSqr = GetPlanarDistanceSqr(grabPoint, bar.GrabPoint);

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestBar = bar;
            }
        }

        if (closestBar == null)
        {
            return;
        }

        Color lineColor = closestDistanceSqr <= maxGrabDistance * maxGrabDistance
            ? Color.green
            : Color.red;

        Vector3 barPointOnPlayerPlane = closestBar.GrabPoint;
        barPointOnPlayerPlane.z = grabPoint.z;
        Debug.DrawLine(grabPoint, barPointOnPlayerPlane, lineColor);
    }

    private void RefreshBarOutlines()
    {
        if (isSwinging)
        {
            ClearBarOutlines();
            return;
        }

        Vector3 grabPoint = GetHandPosition();
        float maxDistanceSqr = maxGrabDistance * maxGrabDistance;
        IReadOnlyList<HandSwingBar> activeBars = HandSwingBar.Bars;

        barsInRange.Clear();

        for (int i = 0; i < activeBars.Count; i++)
        {
            HandSwingBar bar = activeBars[i];

            if (bar == null)
            {
                continue;
            }

            if (GetPlanarDistanceSqr(grabPoint, bar.GrabPoint) > maxDistanceSqr)
            {
                continue;
            }

            barsInRange.Add(bar);
        }

        for (int i = highlightedBars.Count - 1; i >= 0; i--)
        {
            HandSwingBar bar = highlightedBars[i];

            if (bar == null || !barsInRange.Contains(bar))
            {
                if (bar != null)
                {
                    bar.SetHighlighted(false);
                }

                highlightedBars.RemoveAt(i);
            }
        }

        for (int i = 0; i < barsInRange.Count; i++)
        {
            HandSwingBar bar = barsInRange[i];

            if (highlightedBars.Contains(bar))
            {
                continue;
            }

            bar.SetHighlighted(true);
            highlightedBars.Add(bar);
        }
    }

    private void ClearBarOutlines()
    {
        for (int i = highlightedBars.Count - 1; i >= 0; i--)
        {
            HandSwingBar bar = highlightedBars[i];

            if (bar != null)
            {
                bar.SetHighlighted(false);
            }
        }

        highlightedBars.Clear();
    }

    private Vector3 GetHandPosition()
    {
        return handGrabPoint != null ? handGrabPoint.position : transform.position;
    }

    private static float GetPlanarDistanceSqr(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float y = a.y - b.y;
        return x * x + y * y;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandSwingBar bar = other.GetComponentInParent<HandSwingBar>();

        if (bar != null && !nearbyBars.Contains(bar))
        {
            nearbyBars.Add(bar);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HandSwingBar bar = other.GetComponentInParent<HandSwingBar>();

        if (bar != null)
        {
            nearbyBars.Remove(bar);
        }
    }

    private bool WasGrabPressedThisFrame()
    {
        return (Keyboard.current != null
                && (Keyboard.current.spaceKey.wasPressedThisFrame
                    || (allowEKeyGrab && Keyboard.current.eKey.wasPressedThisFrame)))
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
    }

    private static bool WasJumpPressedThisFrame()
    {
        return (Keyboard.current != null
                && (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

    private static float ReadSwingInput()
    {
        float keyboardInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                keyboardInput -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                keyboardInput += 1f;
            }
        }

        float gamepadInput = Gamepad.current != null ? Gamepad.current.leftStick.x.ReadValue() : 0f;

        return Mathf.Abs(gamepadInput) > Mathf.Abs(keyboardInput) ? gamepadInput : keyboardInput;
    }
}
