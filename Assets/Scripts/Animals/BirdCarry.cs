using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BirdCarry : MonoBehaviour
{
    private enum FlightDirection
    {
        Right,
        Left
    }

    private enum CarryState
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

    [Header("Idle State")]
    [SerializeField] private FlightStateSettings idleState = new FlightStateSettings
    {
        horizontalSpeed = 0f,
        patrolDistance = 0f,
        verticalBobAmplitude = 0.15f,
        verticalBobFrequency = 1.25f,
        flapAngle = 0f,
        flapFrequency = 0f
    };

    [Header("Fly State")]
    [SerializeField] private FlightStateSettings flyState = new FlightStateSettings
    {
        horizontalSpeed = 3f,
        patrolDistance = 8f,
        verticalBobAmplitude = 1.32f,
        verticalBobFrequency = 1.25f,
        flapAngle = 35f,
        flapFrequency = 5f
    };

    [Header("Carry")]
    [SerializeField] private FlightDirection initialFlightDirection = FlightDirection.Right;
    [FormerlySerializedAs("maxRightDistance")]
    [SerializeField, Min(0f)] private float maxFlightDistance = 4f;
    [SerializeField] private float topContactMinimumNormalY = 0.5f;
    [SerializeField] private bool moveRidersWithBird = true;

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

    private readonly HashSet<PlayerController2D> riders =
        new HashSet<PlayerController2D>();

    private Vector3 startPosition;
    private Vector3 previousPosition;
    private Quaternion startVisualRotation;
    private Quaternion leftWingStartRotation;
    private Quaternion rightWingStartRotation;
    private int direction = 1;
    private int initialDirection = 1;
    private int facingDirection = 1;
    private float bobOffset;
    private float flapOffset;
    private CarryState currentState = CarryState.Idle;
    private bool stopAtStartWhenReached;

    private void Awake()
    {
        startPosition = transform.position;
        previousPosition = startPosition;
        bobOffset = Random.value * Mathf.PI * 2f;
        flapOffset = Random.value * Mathf.PI * 2f;
        initialDirection = initialFlightDirection == FlightDirection.Right ? 1 : -1;
        direction = initialDirection;
        facingDirection = initialDirection;

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

    private void FixedUpdate()
    {
        previousPosition = transform.position;

        UpdateMovement();
        Vector3 movementDelta = transform.position - previousPosition;
        UpdateFacingFromMovement(movementDelta.x);
        MoveRiders(movementDelta);
    }

    private void Update()
    {
        UpdateWingFlap();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryUpdateRider(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryUpdateRider(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerController2D player = collision.collider.GetComponentInParent<PlayerController2D>();

        if (player == null)
        {
            return;
        }

        riders.Remove(player);

        if (riders.Count == 0 && currentState == CarryState.Fly)
        {
            stopAtStartWhenReached = true;
        }
    }

    private void TryUpdateRider(Collision collision)
    {
        PlayerController2D player = collision.collider.GetComponentInParent<PlayerController2D>();

        if (player == null)
        {
            return;
        }

        bool isStandingOnBird = false;
        bool playerIsAboveBird = player.transform.position.y > transform.position.y;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (
                playerIsAboveBird &&
                contact.point.y >= transform.position.y &&
                Mathf.Abs(contact.normal.y) >= topContactMinimumNormalY
            )
            {
                isStandingOnBird = true;
                break;
            }
        }

        if (!isStandingOnBird)
        {
            riders.Remove(player);
            return;
        }

        riders.Add(player);
        TriggerFly();
    }

    private void TriggerFly()
    {
        if (currentState == CarryState.Idle)
        {
            initialDirection = initialFlightDirection == FlightDirection.Right ? 1 : -1;
            direction = initialDirection;
            facingDirection = initialDirection;
            UpdateFacing();
        }

        currentState = CarryState.Fly;
        stopAtStartWhenReached = false;
    }

    private void UpdateMovement()
    {
        FlightStateSettings settings = GetCurrentSettings();
        Vector3 position = transform.position;
        float previousX = position.x;

        if (currentState == CarryState.Fly)
        {
            float halfDistance = Mathf.Max(0f, settings.patrolDistance) * 0.5f;
            float minX = initialDirection > 0
                ? startPosition.x - halfDistance
                : startPosition.x - maxFlightDistance;
            float maxX = initialDirection > 0
                ? startPosition.x + maxFlightDistance
                : startPosition.x + halfDistance;

            position.x += direction * settings.horizontalSpeed * Time.fixedDeltaTime;

            if (stopAtStartWhenReached && CrossedStart(previousX, position.x))
            {
                position.x = startPosition.x;
                currentState = CarryState.Idle;
                stopAtStartWhenReached = false;
            }
            else if (position.x >= maxX)
            {
                position.x = maxX;
                direction = -1;
            }
            else if (position.x <= minX)
            {
                position.x = minX;
                direction = 1;
            }
        }

        position.y = startPosition.y +
            Mathf.Sin((Time.time * settings.verticalBobFrequency * Mathf.PI * 2f) + bobOffset) *
            settings.verticalBobAmplitude;
        position.z = startPosition.z;
        transform.position = position;
    }

    private bool CrossedStart(float previousX, float nextX)
    {
        if (Mathf.Approximately(previousX, startPosition.x) && !HasRider())
        {
            return true;
        }

        float previousOffset = previousX - startPosition.x;
        float nextOffset = nextX - startPosition.x;
        return previousOffset < 0f && nextOffset >= 0f ||
            previousOffset > 0f && nextOffset <= 0f;
    }

    private bool HasRider()
    {
        riders.RemoveWhere(rider => rider == null);
        return riders.Count > 0;
    }

    private FlightStateSettings GetCurrentSettings()
    {
        return currentState == CarryState.Fly ? flyState : idleState;
    }

    private void MoveRiders(Vector3 delta)
    {
        if (!moveRidersWithBird || delta == Vector3.zero)
        {
            return;
        }

        foreach (PlayerController2D rider in riders)
        {
            if (rider == null)
            {
                continue;
            }

            Rigidbody riderBody = rider.GetComponent<Rigidbody>();
            if (riderBody != null)
            {
                riderBody.position += delta;
            }
            else
            {
                rider.transform.position += delta;
            }
        }
    }

    private void UpdateFacing()
    {
        if (!faceMovementDirection || visualTransform == null)
        {
            return;
        }

        visualTransform.localRotation = facingDirection == initialDirection
            ? startVisualRotation
            : startVisualRotation * Quaternion.Euler(turnRotationEuler);
    }

    private void UpdateFacingFromMovement(float horizontalMovement)
    {
        if (Mathf.Approximately(horizontalMovement, 0f))
        {
            return;
        }

        int newFacingDirection = horizontalMovement > 0f ? 1 : -1;
        if (newFacingDirection == facingDirection)
        {
            return;
        }

        facingDirection = newFacingDirection;
        UpdateFacing();
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
