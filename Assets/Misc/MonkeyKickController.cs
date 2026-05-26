using UnityEngine;
using UnityEngine.InputSystem;

public class MonkeyKickController : MonoBehaviour
{
    private static readonly int IsHangingHash = Animator.StringToHash("IsHanging");
    private static readonly int VerticalHangStateHash = Animator.StringToHash("VerticalHang");

    [Header("Physics")]
    public Rigidbody playerRb;

    [Tooltip("Whether the monkey is currently grabbing a rope/object.")]
    public bool isGrabbing = false;

    [Tooltip("Optional. The grab anchor / rope pivot. Used to calculate swing tangent.")]
    public Transform grabAnchor;

    [Header("Kick Force")]
    public float kickImpulse = 8f;
    public float kickCooldown = 0.18f;
    public bool allowKickInput = true;

    [Header("Body Visual")]
    public Transform bodyVisual;
    public bool alignBodyToRope = true;
    public float bodyAlignSpeed = 14f;

    [Header("Animator")]
    public Animator animator;
    public bool useAnimatorHangPose = true;
    public bool snapHangAnimation = true;
    public bool rebindAnimatorOnRelease = true;

    [Header("Leg Joints")]
    public Transform leftHip;
    public Transform rightHip;
    public Transform leftKnee;
    public Transform rightKnee;

    [Header("Arm Hang Pose")]
    public Transform leftShoulder;
    public Transform rightShoulder;
    public Transform leftElbow;
    public Transform rightElbow;
    public Transform leftWrist;
    public Transform rightWrist;
    public bool poseArmsOnGrab = true;
    public float armPoseBlendSpeed = 18f;
    public float handSpread = 0.18f;

    [Header("Leg Swing Visual")]
    public float hipKickAngle = 35f;
    public float kneeKickAngle = 25f;
    public float kickOutSpeed = 18f;
    public float returnSpeed = 10f;

    private float lastKickTime = -999f;
    private float visualKickAmount = 0f;
    private float visualKickTarget = 0f;
    private int kickDirection = 0;
    private int pendingKickDirection = 0;

    private Quaternion leftHipDefault;
    private Quaternion rightHipDefault;
    private Quaternion leftKneeDefault;
    private Quaternion rightKneeDefault;
    private Quaternion leftShoulderDefault;
    private Quaternion rightShoulderDefault;
    private Quaternion leftElbowDefault;
    private Quaternion rightElbowDefault;
    private Quaternion bodyVisualDefault;
    private float armGrabPoseAmount = 0f;

    void Start()
    {
        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && useAnimatorHangPose)
        {
            animator.SetBool(IsHangingHash, false);
        }

        AutoAssignArmJoints();

        if (bodyVisual != null) bodyVisualDefault = bodyVisual.localRotation;
        if (leftHip != null) leftHipDefault = leftHip.localRotation;
        if (rightHip != null) rightHipDefault = rightHip.localRotation;
        if (leftKnee != null) leftKneeDefault = leftKnee.localRotation;
        if (rightKnee != null) rightKneeDefault = rightKnee.localRotation;
        if (leftShoulder != null) leftShoulderDefault = leftShoulder.localRotation;
        if (rightShoulder != null) rightShoulderDefault = rightShoulder.localRotation;
        if (leftElbow != null) leftElbowDefault = leftElbow.localRotation;
        if (rightElbow != null) rightElbowDefault = rightElbow.localRotation;
    }

    void Update()
    {
        HandleKickInput();
        UpdateBodyVisual();
        UpdateArmGrabPose();
        UpdateLegVisuals();
    }

    void FixedUpdate()
    {
        ApplyPendingKick();
    }

    void HandleKickInput()
    {
        if (!allowKickInput) return;

        if (!isGrabbing) return;

        if (Time.time - lastKickTime < kickCooldown) return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null) return;

        if (keyboard.aKey.isPressed)
        {
            Kick(-1);
        }
        else if (keyboard.dKey.isPressed)
        {
            Kick(1);
        }
    }

    void Kick(int direction)
    {
        lastKickTime = Time.time;
        kickDirection = direction;
        visualKickTarget = 1f;
        pendingKickDirection = direction;
    }

    void ApplyPendingKick()
    {
        if (pendingKickDirection == 0 || !isGrabbing || !allowKickInput)
        {
            pendingKickDirection = 0;
            return;
        }

        Vector3 forceDir = GetKickDirection(pendingKickDirection);

        if (playerRb != null && forceDir.sqrMagnitude > 0.001f)
        {
            playerRb.AddForce(forceDir * kickImpulse, ForceMode.Impulse);
        }

        pendingKickDirection = 0;
    }

    Vector3 GetKickDirection(int direction)
    {
        // If we know the grab anchor, push along the swing tangent.
        if (grabAnchor != null)
        {
            Vector3 radial = playerRb != null
                ? playerRb.position - grabAnchor.position
                : transform.position - grabAnchor.position;
            radial.z = 0f;

            // Prevent weird zero vector.
            if (radial.sqrMagnitude > 0.001f)
            {
                radial.Normalize();

                // Tangent direction around the anchor.
                // For a typical 3D side-view game on the X-Y plane, use Vector3.forward.
                Vector3 tangent = Vector3.Cross(Vector3.forward, radial).normalized;

                return tangent * direction;
            }
        }

        // Fallback: push along world X.
        return Vector3.right * direction;
    }

    void UpdateBodyVisual()
    {
        if (bodyVisual == null || !alignBodyToRope)
        {
            return;
        }

        Quaternion targetRotation = bodyVisualDefault;

        if (isGrabbing && grabAnchor != null)
        {
            Vector3 radial = playerRb != null
                ? playerRb.position - grabAnchor.position
                : transform.position - grabAnchor.position;
            radial.z = 0f;

            if (radial.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(radial.y, radial.x) * Mathf.Rad2Deg + 90f;
                targetRotation = bodyVisualDefault * Quaternion.Euler(0f, 0f, angle);
            }
        }

        bodyVisual.localRotation = Quaternion.Slerp(
            bodyVisual.localRotation,
            targetRotation,
            1f - Mathf.Exp(-bodyAlignSpeed * Time.deltaTime)
        );
    }

    void UpdateLegVisuals()
    {
        float visualSpeed = visualKickTarget > visualKickAmount ? kickOutSpeed : returnSpeed;
        visualKickAmount = Mathf.MoveTowards(
            visualKickAmount,
            visualKickTarget,
            Time.deltaTime * visualSpeed
        );

        if (Mathf.Approximately(visualKickAmount, 1f))
        {
            visualKickTarget = 0f;
        }

        float amount = visualKickAmount;

        if (leftHip != null)
        {
            Quaternion kickRot = Quaternion.Euler(hipKickAngle * kickDirection * amount, 0f, 0f);
            leftHip.localRotation = leftHipDefault * kickRot;
        }

        if (rightHip != null)
        {
            Quaternion kickRot = Quaternion.Euler(hipKickAngle * kickDirection * amount, 0f, 0f);
            rightHip.localRotation = rightHipDefault * kickRot;
        }

        if (leftKnee != null)
        {
            Quaternion kickRot = Quaternion.Euler(-kneeKickAngle * kickDirection * amount, 0f, 0f);
            leftKnee.localRotation = leftKneeDefault * kickRot;
        }

        if (rightKnee != null)
        {
            Quaternion kickRot = Quaternion.Euler(-kneeKickAngle * kickDirection * amount, 0f, 0f);
            rightKnee.localRotation = rightKneeDefault * kickRot;
        }
    }

    public void SetGrabbing(bool grabbing, Transform anchor)
    {
        isGrabbing = grabbing;
        grabAnchor = anchor;

        if (animator != null && useAnimatorHangPose)
        {
            animator.SetBool(IsHangingHash, grabbing);

            if (grabbing && snapHangAnimation)
            {
                animator.CrossFadeInFixedTime(VerticalHangStateHash, 0f, 0, 0f);
                animator.Update(0f);
            }
            else if (!grabbing && rebindAnimatorOnRelease)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetBool(IsHangingHash, false);
            }
        }

        if (!grabbing)
        {
            visualKickAmount = 0f;
            visualKickTarget = 0f;
            kickDirection = 0;
            pendingKickDirection = 0;
            armGrabPoseAmount = 0f;
            ResetArms();
            ResetLegs();
        }
    }

    public bool TryGetGrabHandPosition(out Vector3 handPosition)
    {
        if (leftWrist != null && rightWrist != null)
        {
            handPosition = (leftWrist.position + rightWrist.position) * 0.5f;
            return true;
        }

        if (leftWrist != null)
        {
            handPosition = leftWrist.position;
            return true;
        }

        if (rightWrist != null)
        {
            handPosition = rightWrist.position;
            return true;
        }

        handPosition = transform.position;
        return false;
    }

    public void ForceGrabArmPose()
    {
        if (useAnimatorHangPose && animator != null)
        {
            return;
        }

        if (!poseArmsOnGrab || grabAnchor == null)
        {
            return;
        }

        armGrabPoseAmount = 1f;
        Vector3 right = bodyVisual != null ? bodyVisual.right : transform.right;
        PoseArmTowardGrab(leftShoulder, leftElbow, leftWrist, grabAnchor.position - right * handSpread * 0.5f);
        PoseArmTowardGrab(rightShoulder, rightElbow, rightWrist, grabAnchor.position + right * handSpread * 0.5f);
    }

    void AutoAssignArmJoints()
    {
        Transform searchRoot = bodyVisual != null ? bodyVisual : transform;

        if (leftShoulder == null) leftShoulder = FindDeepChild(searchRoot, "L_shoulder_J");
        if (rightShoulder == null) rightShoulder = FindDeepChild(searchRoot, "R_shoulder_J");
        if (leftElbow == null) leftElbow = FindDeepChild(searchRoot, "L_elbow_J");
        if (rightElbow == null) rightElbow = FindDeepChild(searchRoot, "R_elbow_J");
        if (leftWrist == null) leftWrist = FindDeepChild(searchRoot, "L_wrist_J");
        if (rightWrist == null) rightWrist = FindDeepChild(searchRoot, "R_wrist_J");
    }

    Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    void UpdateArmGrabPose()
    {
        if (useAnimatorHangPose && animator != null)
        {
            return;
        }

        if (!poseArmsOnGrab)
        {
            return;
        }

        float targetAmount = isGrabbing && grabAnchor != null ? 1f : 0f;
        armGrabPoseAmount = Mathf.MoveTowards(
            armGrabPoseAmount,
            targetAmount,
            Time.deltaTime * armPoseBlendSpeed
        );

        if (armGrabPoseAmount <= 0f)
        {
            ResetArms();
            return;
        }

        Vector3 right = bodyVisual != null ? bodyVisual.right : transform.right;
        PoseArmTowardGrab(leftShoulder, leftElbow, leftWrist, grabAnchor.position - right * handSpread * 0.5f);
        PoseArmTowardGrab(rightShoulder, rightElbow, rightWrist, grabAnchor.position + right * handSpread * 0.5f);
    }

    void PoseArmTowardGrab(Transform shoulder, Transform elbow, Transform wrist, Vector3 handTarget)
    {
        if (shoulder == null || elbow == null || wrist == null)
        {
            return;
        }

        Vector3 shoulderToHand = handTarget - shoulder.position;

        if (shoulderToHand.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 elbowTarget = Vector3.Lerp(shoulder.position, handTarget, 0.5f);
        RotateJointToward(shoulder, elbow.position - shoulder.position, elbowTarget - shoulder.position);
        RotateJointToward(elbow, wrist.position - elbow.position, handTarget - elbow.position);
    }

    void RotateJointToward(Transform joint, Vector3 currentDirection, Vector3 targetDirection)
    {
        if (joint == null || currentDirection.sqrMagnitude <= 0.001f || targetDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.FromToRotation(currentDirection, targetDirection) * joint.rotation;
        joint.rotation = Quaternion.Slerp(joint.rotation, targetRotation, armGrabPoseAmount);
    }

    void ResetLegs()
    {
        if (leftHip != null) leftHip.localRotation = leftHipDefault;
        if (rightHip != null) rightHip.localRotation = rightHipDefault;
        if (leftKnee != null) leftKnee.localRotation = leftKneeDefault;
        if (rightKnee != null) rightKnee.localRotation = rightKneeDefault;
    }

    void ResetArms()
    {
        if (leftShoulder != null) leftShoulder.localRotation = leftShoulderDefault;
        if (rightShoulder != null) rightShoulder.localRotation = rightShoulderDefault;
        if (leftElbow != null) leftElbow.localRotation = leftElbowDefault;
        if (rightElbow != null) rightElbow.localRotation = rightElbowDefault;
    }
}
