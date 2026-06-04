using UnityEngine;

public class BirdFlight : MonoBehaviour
{
    private enum FlightState
    {
        Idle,
        Fly
    }

    [System.Serializable]
    private struct FlightStateSettings
    {
        public float horizontalSpeed;
        public float patrolDistance;
        public float verticalBobAmplitude;
        public float verticalBobFrequency;
        public float flapAngle;
        public float flapFrequency;
    }

    [Header("State")]
    [SerializeField] private FlightState startState = FlightState.Idle;
    [SerializeField] private FlightStateSettings idleState = new FlightStateSettings
    {
        horizontalSpeed = 0f,
        patrolDistance = 0f,
        verticalBobAmplitude = 0.15f,
        verticalBobFrequency = 1.25f,
        flapAngle = 8f,
        flapFrequency = 1.5f
    };
    [SerializeField] private FlightStateSettings flyState = new FlightStateSettings
    {
        horizontalSpeed = 3f,
        patrolDistance = 8f,
        verticalBobAmplitude = 0.35f,
        verticalBobFrequency = 1.25f,
        flapAngle = 35f,
        flapFrequency = 5f
    };

    [Header("Facing")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private bool faceMovementDirection = true;
    [SerializeField] private Vector3 turnRotationEuler = new Vector3(0f, 180f, 0f);

    [Header("Wings")]
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private Vector3 leftWingFlapAxis = Vector3.forward;
    [SerializeField] private Vector3 rightWingFlapAxis = Vector3.forward;
    [SerializeField] private bool mirrorRightWing = true;

    private Vector3 startPosition;
    private Quaternion startVisualRotation;
    private Quaternion leftWingStartRotation;
    private Quaternion rightWingStartRotation;
    private int direction = 1;
    private float bobOffset;
    private float flapOffset;
    private FlightState currentState;
    private bool movementEnabled = true;

    private void Awake()
    {
        startPosition = transform.position;
        currentState = startState;
        bobOffset = Random.value * Mathf.PI * 2f;
        flapOffset = Random.value * Mathf.PI * 2f;

        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        startVisualRotation = visualTransform.localRotation;

        if (leftWing != null)
        {
            leftWingStartRotation = leftWing.localRotation;
        }

        if (rightWing != null)
        {
            rightWingStartRotation = rightWing.localRotation;
        }

        UpdateFacing();
    }

    private void Update()
    {
        if (movementEnabled)
        {
            UpdateMovement();
        }

        UpdateWingFlap();
    }

    public void TriggerIdle()
    {
        SetState(FlightState.Idle);
    }

    public void TriggerFly()
    {
        SetState(FlightState.Fly);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    private void SetState(FlightState state)
    {
        if (currentState == state)
        {
            return;
        }

        currentState = state;
    }

    private void UpdateMovement()
    {
        FlightStateSettings settings = GetCurrentSettings();
        float halfDistance = Mathf.Max(0f, settings.patrolDistance) * 0.5f;
        float minX = startPosition.x - halfDistance;
        float maxX = startPosition.x + halfDistance;

        Vector3 position = transform.position;
        position.x += direction * settings.horizontalSpeed * Time.deltaTime;

        if (position.x >= maxX)
        {
            position.x = maxX;
            direction = -1;
            UpdateFacing();
        }
        else if (position.x <= minX)
        {
            position.x = minX;
            direction = 1;
            UpdateFacing();
        }

        position.y = startPosition.y +
            Mathf.Sin((Time.time * settings.verticalBobFrequency * Mathf.PI * 2f) + bobOffset) *
            settings.verticalBobAmplitude;
        position.z = startPosition.z;
        transform.position = position;
    }

    private FlightStateSettings GetCurrentSettings()
    {
        return currentState == FlightState.Fly ? flyState : idleState;
    }

    private void UpdateFacing()
    {
        if (!faceMovementDirection || visualTransform == null)
        {
            return;
        }

        visualTransform.localRotation = direction > 0
            ? startVisualRotation
            : startVisualRotation * Quaternion.Euler(turnRotationEuler);
    }

    private void UpdateWingFlap()
    {
        FlightStateSettings settings = GetCurrentSettings();
        float flap =
            Mathf.Sin((Time.time * settings.flapFrequency * Mathf.PI * 2f) + flapOffset) *
            settings.flapAngle;

        if (leftWing != null)
        {
            leftWing.localRotation =
                leftWingStartRotation *
                Quaternion.AngleAxis(flap, leftWingFlapAxis.normalized);
        }

        if (rightWing != null)
        {
            float rightFlap = mirrorRightWing ? -flap : flap;
            rightWing.localRotation =
                rightWingStartRotation *
                Quaternion.AngleAxis(rightFlap, rightWingFlapAxis.normalized);
        }
    }
}
