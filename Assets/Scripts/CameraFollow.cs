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
        SetAreaBounds(initialBoundary1, initialBoundary2);
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
