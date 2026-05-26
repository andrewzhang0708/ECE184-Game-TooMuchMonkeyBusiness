using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SpimSwingBridge : MonoBehaviour
{
    [Header("Normal Player")]
    [SerializeField] private PlayerController2D playerController;
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private GameObject normalVisualRoot;
    [SerializeField] private Animator normalAnimator;

    [Header("Spim Rig")]
    [SerializeField] private ChimpMovement chimpRig;
    [SerializeField] private Transform chimpRigRoot;
    [SerializeField] private GameObject chimpVisualRoot;

    [Header("Switching")]
    [SerializeField] private bool hideNormalVisualWhileSwinging = true;
    [SerializeField] private bool hideChimpVisual = true;
    [SerializeField] private bool disableAnimatorWhileSwinging = true;
    [SerializeField] private float failedGrabTimeout = 0.35f;
    [SerializeField] private float releaseVelocityMultiplier = 1f;

    private bool rigActive;
    private bool connected;
    private float grabAttemptStartedAt;
    private bool savedPlayerKinematic;
    private bool savedAnimatorEnabled;
    private Transform originalRigParent;
    private Vector3 originalRigLocalPosition;
    private Quaternion originalRigLocalRotation;

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

        rigActive = true;
        connected = false;
        grabAttemptStartedAt = Time.time;

        chimpRigRoot.SetParent(null, true);
        chimpRigRoot.position = transform.position;
        chimpRigRoot.rotation = transform.rotation;

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

    private void SetRigPhysicsActive(bool active)
    {
        if (chimpRigRoot == null)
        {
            return;
        }

        Rigidbody[] bodies = chimpRigRoot.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].detectCollisions = active;
            bodies[i].isKinematic = !active;

            if (!active)
            {
                bodies[i].linearVelocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
            }
        }

        Collider[] colliders = chimpRigRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = active;
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
