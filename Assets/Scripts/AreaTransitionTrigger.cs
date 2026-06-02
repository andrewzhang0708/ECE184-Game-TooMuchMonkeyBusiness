using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaTransitionTrigger : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private AreaTransitionTrigger nextLevelExit1;
    [SerializeField] private AreaTransitionTrigger nextLevelExit2;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField, Min(0f)] private float transitionCooldown = 0.25f;

    private static float nextAllowedTransitionTime;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextAllowedTransitionTime)
        {
            return;
        }

        PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
        if (player == null || destination == null)
        {
            return;
        }

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            return;
        }

        nextAllowedTransitionTime = Time.time + transitionCooldown;

        ScreenFadeTransition fadeTransition = ScreenFadeTransition.Instance;
        if (fadeTransition != null)
        {
            fadeTransition.TryPlay(() => Teleport(playerRigidbody));
            return;
        }

        Teleport(playerRigidbody);
    }

    private void Teleport(Rigidbody playerRigidbody)
    {
        if (cameraFollow != null)
        {
            Transform boundary1 = nextLevelExit1 != null
                ? nextLevelExit1.transform
                : null;
            Transform boundary2 = nextLevelExit2 != null
                ? nextLevelExit2.transform
                : null;

            cameraFollow.SetAreaBounds(boundary1, boundary2);
        }

        playerRigidbody.position = destination.position;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }
}
