using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class SpimSwingBridge : MonoBehaviour
{
    [Header("Normal Player")]
    [SerializeField] private PlayerController2D playerController;
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private GameObject normalVisualRoot;
    [SerializeField] private Animator normalAnimator;
    [SerializeField] private Transform normalHandGrabPoint;

    [Header("Spim Rig")]
    [SerializeField] private ChimpMovement chimpRig;
    [SerializeField] private Transform chimpRigRoot;
    [SerializeField] private GameObject chimpVisualRoot;
    [SerializeField] private Vector3 rigRotationOffset = new Vector3(0f, 90f, 0f);

    [Header("Switching")]
    [SerializeField] private bool hideNormalVisualWhileSwinging = true;
    [SerializeField] private bool hideChimpVisual = true;
    [SerializeField] private bool disableAnimatorWhileSwinging = true;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] private float failedGrabTimeout = 0.35f;
    [SerializeField] private float releaseVelocityMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawHandDistanceLine = true;
    [SerializeField] private bool warnIfHighlightedBarHasNoSwingable = true;

    private readonly List<HandSwingBar> highlightedBars = new List<HandSwingBar>();
    private readonly List<HandSwingBar> barsInRange = new List<HandSwingBar>();
    private bool rigActive;
    private bool connected;
    private float grabAttemptStartedAt;
    private bool savedPlayerKinematic;
    private bool savedAnimatorEnabled;
    private Transform originalRigParent;
    private Vector3 originalRigLocalPosition;
    private Quaternion originalRigLocalRotation;
    private Rigidbody[] rigBodies;
    private RigidbodyConstraints[] originalRigConstraints;

    private void Awake()
    {
        if (playerBody == null)
        {
            playerBody = GetComponent<Rigidbody>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController2D>();
        }

        if (chimpRigRoot == null && chimpRig != null)
        {
            chimpRigRoot = chimpRig.transform;
        }

        if (normalAnimator == null)
        {
            normalAnimator = GetComponentInChildren<Animator>();
        }

        if (chimpRigRoot != null)
        {
            originalRigParent = chimpRigRoot.parent;
            originalRigLocalPosition = chimpRigRoot.localPosition;
            originalRigLocalRotation = chimpRigRoot.localRotation;
            CacheRigBodies();
        }

        if (chimpRig != null)
        {
            chimpRig.SetUseInternalInput(false);
        }

        SetChimpVisualVisible(!hideChimpVisual);
        SetRigPhysicsActive(false);
    }

    private void Update()
    {
        RefreshBarOutlines();

        if (WasGrabPressedThisFrame())
        {
            if (rigActive)
            {
                ReleaseToNormalPlayer();
            }
            else
            {
                BeginRigGrabAttempt();
            }
        }

        if (rigActive && WasJumpPressedThisFrame())
        {
            ReleaseToNormalPlayer();
        }

        if (!rigActive || chimpRig == null)
        {
            return;
        }

        if (!connected && chimpRig.IsConnected())
        {
            connected = true;
            FreezeNormalPlayerForSwing();
        }

        if (!connected
            && Time.time - grabAttemptStartedAt > failedGrabTimeout
            && !chimpRig.IsConnected())
        {
            CancelRigGrabAttempt();
        }
    }

    private void LateUpdate()
    {
        DrawHandDistanceLine();

        if (!rigActive || !connected || chimpRig == null)
        {
            return;
        }

        transform.position = chimpRig.GetRigCenter();
    }

    private void BeginRigGrabAttempt()
    {
        if (chimpRig == null || chimpRigRoot == null)
        {
            Debug.LogWarning("SpimSwingBridge needs a ChimpMovement rig reference.", this);
            return;
        }

        WarnIfClosestBarCannotUseSpim();

        rigActive = true;
        connected = false;
        grabAttemptStartedAt = Time.time;

        chimpRigRoot.SetParent(null, true);
        chimpRigRoot.position = transform.position;
        Vector3 rigPosition = chimpRigRoot.position;
        rigPosition.z = transform.position.z;
        chimpRigRoot.position = rigPosition;
        chimpRigRoot.rotation = transform.rotation * Quaternion.Euler(rigRotationOffset);

        SetRigPhysicsActive(true);
        chimpRig.BeginGrabAttempt();
    }

    private void CancelRigGrabAttempt()
    {
        if (chimpRig != null)
        {
            chimpRig.CancelGrabOrRelease();
        }

        rigActive = false;
        connected = false;
        SetRigPhysicsActive(false);
        RestoreRigParent();
        ResetRigToPlayer();
        ClearBarOutlines();
        SetChimpVisualVisible(!hideChimpVisual);
    }

    private void FreezeNormalPlayerForSwing()
    {
        savedPlayerKinematic = playerBody.isKinematic;
        playerBody.linearVelocity = Vector3.zero;
        playerBody.angularVelocity = Vector3.zero;
        playerBody.isKinematic = true;

        if (playerController != null)
        {
            playerController.SetExternalMotionActive(true);
        }

        if (normalVisualRoot != null && hideNormalVisualWhileSwinging)
        {
            normalVisualRoot.SetActive(false);
        }

        SetChimpVisualVisible(true);

        if (normalAnimator != null && disableAnimatorWhileSwinging)
        {
            savedAnimatorEnabled = normalAnimator.enabled;
            normalAnimator.enabled = false;
        }
    }

    private void ReleaseToNormalPlayer()
    {
        Vector3 releasePosition = transform.position;
        Vector3 releaseVelocity = Vector3.zero;

        if (chimpRig != null)
        {
            releasePosition = chimpRig.GetRigCenter();
            releaseVelocity = chimpRig.GetAverageVelocity() * releaseVelocityMultiplier;
            chimpRig.CancelGrabOrRelease();
        }

        transform.position = releasePosition;
        playerBody.isKinematic = savedPlayerKinematic;
        releaseVelocity.z = 0f;
        playerBody.linearVelocity = releaseVelocity;

        if (playerController != null)
        {
            playerController.SetExternalMotionActive(false);
        }

        if (normalVisualRoot != null)
        {
            normalVisualRoot.SetActive(true);
        }

        if (normalAnimator != null && disableAnimatorWhileSwinging)
        {
            normalAnimator.enabled = savedAnimatorEnabled;
        }

        rigActive = false;
        connected = false;
        SetRigPhysicsActive(false);
        RestoreRigParent();
        ResetRigToPlayer();
        ClearBarOutlines();
        SetChimpVisualVisible(!hideChimpVisual);
    }

    private void OnDisable()
    {
        ClearBarOutlines();
    }

    private void RestoreRigParent()
    {
        if (chimpRigRoot == null)
        {
            return;
        }

        chimpRigRoot.SetParent(originalRigParent, false);
        chimpRigRoot.localPosition = originalRigLocalPosition;
        chimpRigRoot.localRotation = originalRigLocalRotation;
    }

    private void ResetRigToPlayer()
    {
        if (chimpRigRoot == null)
        {
            return;
        }

        chimpRigRoot.position = transform.position;
        chimpRigRoot.rotation = transform.rotation * Quaternion.Euler(rigRotationOffset);
    }

    private void SetRigPhysicsActive(bool active)
    {
        if (chimpRigRoot == null)
        {
            return;
        }

        if (rigBodies == null || rigBodies.Length == 0)
        {
            CacheRigBodies();
        }

        for (int i = 0; i < rigBodies.Length; i++)
        {
            Rigidbody body = rigBodies[i];

            if (body == null)
            {
                continue;
            }

            if (!active)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.detectCollisions = active;
            body.isKinematic = !active;

            if (active)
            {
                body.constraints = RigidbodyConstraints.FreezePositionZ
                    | RigidbodyConstraints.FreezeRotationX
                    | RigidbodyConstraints.FreezeRotationY;

                Vector3 position = body.position;
                position.z = transform.position.z;
                body.position = position;
            }
            else if (originalRigConstraints != null && i < originalRigConstraints.Length)
            {
                body.constraints = originalRigConstraints[i];
            }
        }

        Collider[] colliders = chimpRigRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = active;
        }
    }

    private void CacheRigBodies()
    {
        if (chimpRigRoot == null)
        {
            rigBodies = new Rigidbody[0];
            originalRigConstraints = new RigidbodyConstraints[0];
            return;
        }

        rigBodies = chimpRigRoot.GetComponentsInChildren<Rigidbody>(true);
        originalRigConstraints = new RigidbodyConstraints[rigBodies.Length];

        for (int i = 0; i < rigBodies.Length; i++)
        {
            originalRigConstraints[i] = rigBodies[i] != null
                ? rigBodies[i].constraints
                : RigidbodyConstraints.None;
        }
    }

    private void SetChimpVisualVisible(bool visible)
    {
        if (chimpVisualRoot == null)
        {
            return;
        }

        Renderer[] renderers = chimpVisualRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
    }

    private void RefreshBarOutlines()
    {
        if (rigActive)
        {
            ClearBarOutlines();
            return;
        }

        Vector3 grabPoint = GetNormalHandPosition();
        float maxDistanceSqr = maxGrabDistance * maxGrabDistance;
        IReadOnlyList<HandSwingBar> activeBars = HandSwingBar.Bars;

        barsInRange.Clear();

        for (int i = 0; i < activeBars.Count; i++)
        {
            HandSwingBar bar = activeBars[i];

            if (bar == null || GetPlanarDistanceSqr(grabPoint, bar.GrabPoint) > maxDistanceSqr)
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

    private void DrawHandDistanceLine()
    {
        if (!drawHandDistanceLine || rigActive)
        {
            return;
        }

        HandSwingBar closestBar = FindClosestHandSwingBar();

        if (closestBar == null)
        {
            return;
        }

        Vector3 grabPoint = GetNormalHandPosition();
        float distanceSqr = GetPlanarDistanceSqr(grabPoint, closestBar.GrabPoint);
        Color lineColor = distanceSqr <= maxGrabDistance * maxGrabDistance ? Color.green : Color.red;
        Vector3 barPoint = closestBar.GrabPoint;
        barPoint.z = grabPoint.z;
        Debug.DrawLine(grabPoint, barPoint, lineColor);
    }

    private void WarnIfClosestBarCannotUseSpim()
    {
        if (!warnIfHighlightedBarHasNoSwingable)
        {
            return;
        }

        HandSwingBar closestBar = FindClosestHandSwingBar();

        if (closestBar == null)
        {
            return;
        }

        float distanceSqr = GetPlanarDistanceSqr(GetNormalHandPosition(), closestBar.GrabPoint);

        if (distanceSqr > maxGrabDistance * maxGrabDistance)
        {
            return;
        }

        if (closestBar.GetComponentInParent<Swingable>() == null)
        {
            Debug.LogWarning(
                "Closest highlighted bar has HandSwingBar but no Swingable. Spim rig needs Swingable on the bar to actually connect.",
                closestBar
            );
        }
    }

    private HandSwingBar FindClosestHandSwingBar()
    {
        Vector3 grabPoint = GetNormalHandPosition();
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

        return closestBar;
    }

    private Vector3 GetNormalHandPosition()
    {
        return normalHandGrabPoint != null ? normalHandGrabPoint.position : transform.position;
    }

    private static float GetPlanarDistanceSqr(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float y = a.y - b.y;
        return x * x + y * y;
    }

    private static bool WasGrabPressedThisFrame()
    {
        return (Keyboard.current != null
                && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
    }

    private static bool WasJumpPressedThisFrame()
    {
        return (Keyboard.current != null
                && (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }
}
