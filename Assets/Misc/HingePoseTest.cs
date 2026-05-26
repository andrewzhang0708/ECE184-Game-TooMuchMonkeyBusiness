using UnityEngine;
using UnityEngine.InputSystem;

public class HingePoseTest : MonoBehaviour
{
    [Header("Joint")]
    public HingeJoint legJoint;

    [Header("Target Angles")]
    public float neutralAngle = 0f;
    public float stretchBackAngle = -45f;   // A
    public float tuckForwardAngle = 45f;    // D

    [Header("Spring Settings")]
    public float springStrength = 200f;
    public float damper = 20f;

    [Header("Motion Feel")]
    public float angleChangeSpeed = 300f;

    private float currentTargetAngle;

    void Start()
    {
        currentTargetAngle = neutralAngle;

        if (legJoint == null)
        {
            Debug.LogWarning("HingePoseTest: legJoint is not assigned.");
            return;
        }

        legJoint.useSpring = true;

        JointSpring spring = legJoint.spring;
        spring.spring = springStrength;
        spring.damper = damper;
        spring.targetPosition = neutralAngle;
        legJoint.spring = spring;
    }

    void Update()
    {
        if (legJoint == null) return;

        float desiredAngle = neutralAngle;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
            {
                desiredAngle = stretchBackAngle;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                desiredAngle = tuckForwardAngle;
            }
        }

        currentTargetAngle = Mathf.MoveTowards(
            currentTargetAngle,
            desiredAngle,
            angleChangeSpeed * Time.deltaTime
        );

        JointSpring spring = legJoint.spring;
        spring.spring = springStrength;
        spring.damper = damper;
        spring.targetPosition = currentTargetAngle;
        legJoint.spring = spring;
    }
}