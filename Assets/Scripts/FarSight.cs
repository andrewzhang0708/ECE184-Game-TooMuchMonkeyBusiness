using UnityEngine;

public class FarSight : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Optional. Leave empty to use the camera tagged MainCamera.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [Tooltip("0 stays fixed. 1 moves with the camera. Smaller values look farther away.")]
    [SerializeField, Range(0f, 1f)] private float horizontalFollowRatio = 0.25f;
    [Tooltip("Set to 0 to keep the background height fixed.")]
    [SerializeField, Range(0f, 1f)] private float verticalFollowRatio = 0f;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError(
                "FarSight could not find the camera. Assign Camera Transform in the Inspector " +
                "or tag the camera as MainCamera.",
                this
            );
            enabled = false;
            return;
        }

        startPosition = transform.position;
        cameraStartPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraMovement = cameraTransform.position - cameraStartPosition;

        transform.position = new Vector3(
            startPosition.x + cameraMovement.x * horizontalFollowRatio,
            startPosition.y + cameraMovement.y * verticalFollowRatio,
            startPosition.z
        );
    }
}
