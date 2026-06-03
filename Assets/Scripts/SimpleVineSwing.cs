using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleVineSwing : MonoBehaviour
{
    private static readonly List<SimpleVineSwing> ActiveVines = new List<SimpleVineSwing>();

    [Header("Grab")]
    [Tooltip("Point the player's hands should attach to. If empty, this object's position is used.")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float grabRadius = 2f;

    [Header("Swing")]
    [Tooltip("Continuous A/D force applied along the pendulum tangent while grabbed.")]
    [SerializeField] private float pumpForce = 35f;
    [SerializeField] private float releaseBoost = 1f;
    [SerializeField] private float maxReleaseSpeed = 18f;

    private Rigidbody vineBody;
    private HingeJoint hinge;
    private Rigidbody grabbedPlayerBody;
    private PlayerController2D grabbedPlayerController;
    private FixedJoint grabJoint;
    private float grabbedAtTime;

    private void Awake()
    {
        vineBody = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();
    }

    private void OnEnable()
    {
        if (!ActiveVines.Contains(this))
        {
            ActiveVines.Add(this);
        }
    }

    private void OnDisable()
    {
        if (grabbedPlayerBody != null)
        {
            Release();
        }

        ActiveVines.Remove(this);
    }

    private void Update()
    {
        if (grabbedPlayerBody == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool releasePressed =
            keyboard.eKey.wasPressedThisFrame ||
            keyboard.wKey.wasPressedThisFrame ||
            keyboard.upArrowKey.wasPressedThisFrame;

        if (releasePressed && Time.time - grabbedAtTime > 0.08f)
        {
            Release();
        }
    }

    private void FixedUpdate()
    {
        if (grabbedPlayerBody == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float input = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            input -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            input += 1f;
        }

        if (Mathf.Approximately(input, 0f))
        {
            return;
        }

        Vector3 forceDirection = GetTangentDirection() * input;
        vineBody.AddForceAtPosition(
            forceDirection * pumpForce,
            GetGrabPointWorld(),
            ForceMode.Acceleration
        );
    }

    public static bool TryGrabClosest(Rigidbody playerBody, out SimpleVineSwing grabbedVine)
    {
        grabbedVine = null;

        if (playerBody == null)
        {
            return false;
        }

        PlayerController2D playerController = playerBody.GetComponent<PlayerController2D>();
        if (playerController == null)
        {
            return false;
        }

        float bestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < ActiveVines.Count; i++)
        {
            SimpleVineSwing vine = ActiveVines[i];
            if (vine == null || vine.grabbedPlayerBody != null)
            {
                continue;
            }

            float grabRadiusSqr = vine.grabRadius * vine.grabRadius;
            float distanceSqr = (vine.GetGrabPointWorld() - playerBody.position).sqrMagnitude;

            if (distanceSqr > grabRadiusSqr || distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            grabbedVine = vine;
        }

        if (grabbedVine == null)
        {
            return false;
        }

        grabbedVine.Grab(playerBody, playerController);
        return true;
    }

    private void Grab(Rigidbody playerBody, PlayerController2D playerController)
    {
        grabbedPlayerBody = playerBody;
        grabbedPlayerController = playerController;
        grabbedAtTime = Time.time;

        Vector3 playerHandPoint = GetPlayerHandPointWorld(playerBody);
        Vector3 grabWorldPoint = GetGrabPointWorld();
        playerBody.position += grabWorldPoint - playerHandPoint;
        playerBody.linearVelocity = vineBody.GetPointVelocity(grabWorldPoint);

        grabJoint = playerBody.gameObject.AddComponent<FixedJoint>();
        grabJoint.connectedBody = vineBody;
        grabJoint.autoConfigureConnectedAnchor = false;
        grabJoint.anchor = playerBody.transform.InverseTransformPoint(GetPlayerHandPointWorld(playerBody));
        grabJoint.connectedAnchor = vineBody.transform.InverseTransformPoint(grabWorldPoint);
        grabJoint.enableCollision = false;
        grabJoint.breakForce = Mathf.Infinity;
        grabJoint.breakTorque = Mathf.Infinity;

        grabbedPlayerController.SetExternalMotionActive(true);
    }

    private void Release()
    {
        if (grabJoint != null)
        {
            Destroy(grabJoint);
        }

        if (grabbedPlayerBody != null)
        {
            Vector3 releaseVelocity = vineBody.GetPointVelocity(GetGrabPointWorld()) * releaseBoost;
            releaseVelocity.z = 0f;
            grabbedPlayerBody.linearVelocity = Vector3.ClampMagnitude(releaseVelocity, maxReleaseSpeed);
        }

        if (grabbedPlayerController != null)
        {
            grabbedPlayerController.SetExternalMotionActive(false);
        }

        grabbedPlayerBody = null;
        grabbedPlayerController = null;
        grabJoint = null;
    }

    private Vector3 GetGrabPointWorld()
    {
        return grabPoint != null ? grabPoint.position : transform.position;
    }

    private Vector3 GetHingePointWorld()
    {
        if (hinge != null)
        {
            return hinge.transform.TransformPoint(hinge.anchor);
        }

        return transform.position + Vector3.up;
    }

    private Vector3 GetTangentDirection()
    {
        Vector3 radius = GetGrabPointWorld() - GetHingePointWorld();
        radius.z = 0f;

        if (radius.sqrMagnitude <= 0.0001f)
        {
            return Vector3.right;
        }

        Vector3 tangent = Vector3.Cross(Vector3.forward, radius.normalized);
        tangent.z = 0f;
        return tangent.normalized;
    }

    private static Vector3 GetPlayerHandPointWorld(Rigidbody playerBody)
    {
        Collider[] playerColliders = playerBody.GetComponentsInChildren<Collider>();
        Bounds bounds = new Bounds(playerBody.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = playerCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(playerCollider.bounds);
            }
        }

        if (!hasBounds)
        {
            return playerBody.position + Vector3.up * 0.8f;
        }

        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetGrabPointWorld(), grabRadius);
    }
}
