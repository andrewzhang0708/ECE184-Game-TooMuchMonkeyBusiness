using UnityEngine;
using UnityEngine.InputSystem;

public class MonkeyLowerBodyPose : MonoBehaviour
{
    [Header("Lower Body Bones")]
    public Transform pelvis;
    public Transform waist;
    public Transform leftHip;
    public Transform rightHip;
    public Transform leftKnee;
    public Transform rightKnee;

    [Header("Pose Strength")]
    [Tooltip("-1 = A / stretch backward, 0 = neutral, 1 = D / tuck forward")]
    [Range(-1f, 1f)]
    public float debugPose = 0f;

    public bool useKeyboardInput = true;
    public float poseBlendSpeed = 8f;

    [Header("Axis Selection")]
    [Tooltip("Try X first. If the legs twist sideways, change to Z or Y.")]
    public RotationAxis rotationAxis = RotationAxis.X;

    [Header("Waist / Pelvis Angles")]
    public float waistStretchAngle = -20f;
    public float waistTuckAngle = 25f;
    public float pelvisStretchAngle = -10f;
    public float pelvisTuckAngle = 15f;

    [Header("Hip Angles")]
    public float hipStretchAngle = -35f;
    public float hipTuckAngle = 55f;

    [Header("Knee Angles")]
    public float kneeStretchAngle = 10f;
    public float kneeTuckAngle = 65f;

    private float currentPose = 0f;

    private Quaternion pelvisDefault;
    private Quaternion waistDefault;
    private Quaternion leftHipDefault;
    private Quaternion rightHipDefault;
    private Quaternion leftKneeDefault;
    private Quaternion rightKneeDefault;

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    void Start()
    {
        if (pelvis != null) pelvisDefault = pelvis.localRotation;
        if (waist != null) waistDefault = waist.localRotation;
        if (leftHip != null) leftHipDefault = leftHip.localRotation;
        if (rightHip != null) rightHipDefault = rightHip.localRotation;
        if (leftKnee != null) leftKneeDefault = leftKnee.localRotation;
        if (rightKnee != null) rightKneeDefault = rightKnee.localRotation;
    }

    void LateUpdate()
    {
        float targetPose = debugPose;

        if (useKeyboardInput)
        {
            targetPose = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed)
                {
                    targetPose = -1f;
                }
                else if (Keyboard.current.dKey.isPressed)
                {
                    targetPose = 1f;
                }
            }
        }

        currentPose = Mathf.MoveTowards(
            currentPose,
            targetPose,
            poseBlendSpeed * Time.deltaTime
        );

        ApplyPose(currentPose);
    }

    void ApplyPose(float pose)
    {
        float waistAngle = GetAngle(pose, waistStretchAngle, waistTuckAngle);
        float pelvisAngle = GetAngle(pose, pelvisStretchAngle, pelvisTuckAngle);
        float hipAngle = GetAngle(pose, hipStretchAngle, hipTuckAngle);
        float kneeAngle = GetAngle(pose, kneeStretchAngle, kneeTuckAngle);

        if (waist != null)
        {
            waist.localRotation = waistDefault * AxisRotation(waistAngle);
        }

        if (pelvis != null)
        {
            pelvis.localRotation = pelvisDefault * AxisRotation(pelvisAngle);
        }

        if (leftHip != null)
        {
            leftHip.localRotation = leftHipDefault * AxisRotation(hipAngle);
        }

        if (rightHip != null)
        {
            rightHip.localRotation = rightHipDefault * AxisRotation(hipAngle);
        }

        if (leftKnee != null)
        {
            leftKnee.localRotation = leftKneeDefault * AxisRotation(kneeAngle);
        }

        if (rightKnee != null)
        {
            rightKnee.localRotation = rightKneeDefault * AxisRotation(kneeAngle);
        }
    }

    float GetAngle(float pose, float stretchAngle, float tuckAngle)
    {
        if (pose < 0f)
        {
            return Mathf.Lerp(0f, stretchAngle, -pose);
        }

        return Mathf.Lerp(0f, tuckAngle, pose);
    }

    Quaternion AxisRotation(float angle)
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Quaternion.Euler(angle, 0f, 0f);
            case RotationAxis.Y:
                return Quaternion.Euler(0f, angle, 0f);
            case RotationAxis.Z:
                return Quaternion.Euler(0f, 0f, angle);
            default:
                return Quaternion.identity;
        }
    }

    public void SetPose(float pose)
    {
        debugPose = Mathf.Clamp(pose, -1f, 1f);
    }
}