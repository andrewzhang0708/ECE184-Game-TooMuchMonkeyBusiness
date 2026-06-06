using System.Collections.Generic;
using UnityEngine;

public class BirdCarry : MonoBehaviour
{
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
    [SerializeField, Min(0f)] private float maxRightDistance = 4f;
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

    private readonly HashSet<Transform> riders = new HashSet<Transform>();

    private Vector3 startPosition;
    private Vector3 previousPosition;
    private Quaternion startVisualRotation;
    private Quaternion leftWingStartRotation;
    private Quaternion rightWingStartRotation;
    private int direction = 1;
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
        previousPosition = transform.position;

        UpdateMovement();
        MoveRiders(transform.position - previousPosition);
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

        riders.Remove(player.transform);

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
            riders.Remove(player.transform);
            return;
        }

        riders.Add(player.transform);
        TriggerFly();
    }

    private void TriggerFly()
    {
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
            float minX = startPosition.x - halfDistance;
            float maxX = startPosition.x + maxRightDistance;

            position.x += direction * settings.horizontalSpeed * Time.deltaTime;

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
                UpdateFacing();
            }
            else if (position.x <= minX)
            {
                position.x = minX;
                direction = 1;
                UpdateFacing();
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

        foreach (Transform rider in riders)
        {
            if (rider != null)
            {
                rider.position += delta;
            }
        }
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
