using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Initial Area Bounds")]
    [Tooltip("Optional. Assign the two entries/exits that bound the starting area. Either side may be empty.")]
    [SerializeField] private Transform initialBoundary1;
    [SerializeField] private Transform initialBoundary2;

    [Header("Follow Offset")]
    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private float cameraZ = -10f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.0f;

    private Vector3 velocity;
    private Camera cameraComponent;
    private Transform currentBoundary1;
    private Transform currentBoundary2;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
    }

    private void Start()
    {
        SetInitialBoundsFromPlayerPosition();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = new Vector3(
            ClampCameraX(target.position.x + xOffset),
            target.position.y + yOffset,
            cameraZ
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // For 2.5D / side-scrolling platformer, keep camera looking straight forward.
        transform.rotation = Quaternion.identity;
    }

    public void SetAreaBounds(Transform boundary1, Transform boundary2)
    {
        currentBoundary1 = boundary1;
        currentBoundary2 = boundary2;

        Vector3 position = transform.position;
        position.x = ClampCameraX(position.x);
        transform.position = position;
        velocity.x = 0f;
    }

    private void SetInitialBoundsFromPlayerPosition()
    {
        if (initialBoundary1 != null || initialBoundary2 != null)
        {
            SetAreaBounds(initialBoundary1, initialBoundary2);
            return;
        }

        if (target == null)
        {
            SetAreaBounds(initialBoundary1, initialBoundary2);
            return;
        }

        Transform nearestLeftExit = null;
        Transform nearestRightExit = null;
        float nearestLeftDistance = float.PositiveInfinity;
        float nearestRightDistance = float.PositiveInfinity;
        float nearestZDistance = float.PositiveInfinity;
        float playerX = target.position.x;
        float playerZ = target.position.z;
        AreaTransitionTrigger[] exits = FindObjectsByType<AreaTransitionTrigger>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (AreaTransitionTrigger exit in exits)
        {
            float zDistance = Mathf.Abs(exit.transform.position.z - playerZ);
            if (zDistance < nearestZDistance)
            {
                nearestZDistance = zDistance;
            }
        }

        const float sameAreaZTolerance = 1f;
        foreach (AreaTransitionTrigger exit in exits)
        {
            Transform exitTransform = exit.transform;
            float zDistance = Mathf.Abs(exitTransform.position.z - playerZ);
            if (zDistance > nearestZDistance + sameAreaZTolerance)
            {
                continue;
            }

            float xDifference = exitTransform.position.x - playerX;
            if (xDifference <= 0f && -xDifference < nearestLeftDistance)
            {
                nearestLeftDistance = -xDifference;
                nearestLeftExit = exitTransform;
            }
            else if (xDifference > 0f && xDifference < nearestRightDistance)
            {
                nearestRightDistance = xDifference;
                nearestRightExit = exitTransform;
            }
        }

        initialBoundary1 = nearestLeftExit;
        initialBoundary2 = nearestRightExit;
        SetAreaBounds(initialBoundary1, initialBoundary2);
    }

    private float ClampCameraX(float cameraX)
    {
        if (cameraComponent == null)
        {
            return cameraX;
        }

        float halfWidth = cameraComponent.orthographicSize * cameraComponent.aspect;
        bool hasBoundary1 = currentBoundary1 != null;
        bool hasBoundary2 = currentBoundary2 != null;

        if (!hasBoundary1 && !hasBoundary2)
        {
            return cameraX;
        }

        if (!hasBoundary1)
        {
            return Mathf.Min(cameraX, currentBoundary2.position.x - halfWidth);
        }

        if (!hasBoundary2)
        {
            return Mathf.Max(cameraX, currentBoundary1.position.x + halfWidth);
        }

        float leftX = Mathf.Min(currentBoundary1.position.x, currentBoundary2.position.x);
        float rightX = Mathf.Max(currentBoundary1.position.x, currentBoundary2.position.x);
        float minCameraX = leftX + halfWidth;
        float maxCameraX = rightX - halfWidth;

        if (minCameraX > maxCameraX)
        {
            return (leftX + rightX) * 0.5f;
        }

        return Mathf.Clamp(cameraX, minCameraX, maxCameraX);
    }
}
