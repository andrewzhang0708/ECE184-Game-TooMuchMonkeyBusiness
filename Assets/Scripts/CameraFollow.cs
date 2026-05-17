using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Follow Offset")]
    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private float cameraZ = -10f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.0f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = new Vector3(
            target.position.x + xOffset,
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
}