using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleHandIK : MonoBehaviour
{
    private enum HandRaiseState
    {
        Idle,
        Raising,
        Holding,
        Lowering
    }

    public Animator animator;
    [Tooltip("Used to choose which hand raises based on the player's facing direction.")]
    public PlayerController2D playerController;

    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform leftElbowHint;
    public Transform rightHandTarget;
    public Transform rightElbowHint;

    [Header("IK Settings")]
    public float blendSpeed = 8f;
    [Min(0)] public int holdFrames = 30;
    [Tooltip("Swap the hand assigned to each facing direction.")]
    public bool reversed;
    [Tooltip("Keep each IK target on the corresponding hand's current Z plane.")]
    public bool lockTargetDepth;
    [Tooltip("Apply the target's rotation to the hand.")]
    public bool useTargetRotation;
    [Tooltip("Use the animated elbow position when no elbow hint is assigned.")]
    public bool useAnimatedElbowFallback = true;

    private HandRaiseState state;
    private float currentWeight;
    private int remainingHoldFrames;
    private bool activeHandIsRight;

    private void Awake()
    {
        FindReferences();
    }

    private void Reset()
    {
        FindReferences();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (
            state == HandRaiseState.Idle &&
            keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame
        )
        {
            StartRaise();
        }

        UpdateRaiseSequence();
    }

    private void StartRaise()
    {
        bool isFacingRight = playerController != null && playerController.IsFacingRight;
        activeHandIsRight = isFacingRight ^ reversed;
        state = HandRaiseState.Raising;
    }

    private void UpdateRaiseSequence()
    {
        switch (state)
        {
            case HandRaiseState.Raising:
                currentWeight = MoveWeightTowards(1f);
                if (Mathf.Approximately(currentWeight, 1f))
                {
                    remainingHoldFrames = holdFrames;
                    state = remainingHoldFrames > 0
                        ? HandRaiseState.Holding
                        : HandRaiseState.Lowering;
                }
                break;

            case HandRaiseState.Holding:
                remainingHoldFrames--;
                if (remainingHoldFrames <= 0)
                {
                    state = HandRaiseState.Lowering;
                }
                break;

            case HandRaiseState.Lowering:
                currentWeight = MoveWeightTowards(0f);
                if (Mathf.Approximately(currentWeight, 0f))
                {
                    state = HandRaiseState.Idle;
                }
                break;
        }
    }

    private float MoveWeightTowards(float targetWeight)
    {
        if (blendSpeed <= 0f)
        {
            return targetWeight;
        }

        return Mathf.MoveTowards(
            currentWeight,
            targetWeight,
            blendSpeed * Time.deltaTime
        );
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        float leftWeight = activeHandIsRight ? 0f : currentWeight;
        float rightWeight = activeHandIsRight ? currentWeight : 0f;

        ApplyHandIK(
            AvatarIKGoal.LeftHand,
            AvatarIKHint.LeftElbow,
            HumanBodyBones.LeftHand,
            HumanBodyBones.LeftLowerArm,
            leftHandTarget,
            leftElbowHint,
            leftWeight
        );

        ApplyHandIK(
            AvatarIKGoal.RightHand,
            AvatarIKHint.RightElbow,
            HumanBodyBones.RightHand,
            HumanBodyBones.RightLowerArm,
            rightHandTarget,
            rightElbowHint,
            rightWeight
        );
    }

    private void ApplyHandIK(
        AvatarIKGoal handGoal,
        AvatarIKHint elbowGoal,
        HumanBodyBones handBoneType,
        HumanBodyBones lowerArmBoneType,
        Transform handTarget,
        Transform elbowHint,
        float weight
    )
    {
        if (handTarget == null)
        {
            animator.SetIKPositionWeight(handGoal, 0f);
            animator.SetIKRotationWeight(handGoal, 0f);
            animator.SetIKHintPositionWeight(elbowGoal, 0f);
            return;
        }

        Transform handBone = animator.GetBoneTransform(handBoneType);
        Vector3 targetPosition = handTarget.position;

        if (lockTargetDepth && handBone != null)
        {
            targetPosition.z = handBone.position.z;
        }

        animator.SetIKPositionWeight(handGoal, weight);
        animator.SetIKPosition(handGoal, targetPosition);

        float rotationWeight = useTargetRotation ? weight : 0f;
        animator.SetIKRotationWeight(handGoal, rotationWeight);
        if (useTargetRotation)
        {
            animator.SetIKRotation(handGoal, handTarget.rotation);
        }

        bool hasElbowHint = TryGetElbowHintPosition(
            lowerArmBoneType,
            elbowHint,
            out Vector3 elbowHintPosition
        );

        animator.SetIKHintPositionWeight(
            elbowGoal,
            hasElbowHint ? weight : 0f
        );

        if (hasElbowHint)
        {
            animator.SetIKHintPosition(elbowGoal, elbowHintPosition);
        }
    }

    private bool TryGetElbowHintPosition(
        HumanBodyBones lowerArmBoneType,
        Transform elbowHint,
        out Vector3 hintPosition
    )
    {
        Transform lowerArm = animator.GetBoneTransform(lowerArmBoneType);

        if (elbowHint != null)
        {
            hintPosition = elbowHint.position;

            if (lockTargetDepth && lowerArm != null)
            {
                hintPosition.z = lowerArm.position.z;
            }

            return true;
        }

        if (useAnimatedElbowFallback && lowerArm != null)
        {
            hintPosition = lowerArm.position;
            return true;
        }

        hintPosition = Vector3.zero;
        return false;
    }

    private void FindReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController2D>();
        }
    }
}
