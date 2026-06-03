using UnityEngine;

public class CameraBasedEnemyActivator : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("How far outside the visible camera viewport the enemy can be before activating.")]
    [SerializeField] private Vector2 viewportPadding = new Vector2(0.25f, 0.25f);

    [Header("Activation")]
    [Tooltip("If empty, all MonoBehaviours on this GameObject except this activator will be toggled.")]
    [SerializeField] private Behaviour[] behavioursToActivate;
    [SerializeField] private Animator[] animatorsToActivate;
    [SerializeField] private Rigidbody[] rigidbodiesToWake;
    [SerializeField] private bool startInactive = true;
    [SerializeField] private bool deactivateWhenFarAway;
    [SerializeField] private bool logActivationChanges;

    private bool isActivated;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (behavioursToActivate == null || behavioursToActivate.Length == 0)
        {
            behavioursToActivate = GetComponents<Behaviour>();
        }

        if (animatorsToActivate == null || animatorsToActivate.Length == 0)
        {
            animatorsToActivate = GetComponentsInChildren<Animator>();
        }

        if (rigidbodiesToWake == null || rigidbodiesToWake.Length == 0)
        {
            rigidbodiesToWake = GetComponentsInChildren<Rigidbody>();
        }

        if (startInactive)
        {
            SetActivated(false);
        }
    }

    private void Update()
    {
        bool shouldBeActive = IsNearCameraView();

        if (shouldBeActive && !isActivated)
        {
            SetActivated(true);
            return;
        }

        if (deactivateWhenFarAway && !shouldBeActive && isActivated)
        {
            SetActivated(false);
        }
    }

    private bool IsNearCameraView()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

        if (cameraToUse == null)
        {
            return false;
        }

        Vector3 viewportPoint = cameraToUse.WorldToViewportPoint(transform.position);

        if (viewportPoint.z < 0f)
        {
            return false;
        }

        return viewportPoint.x >= -viewportPadding.x &&
            viewportPoint.x <= 1f + viewportPadding.x &&
            viewportPoint.y >= -viewportPadding.y &&
            viewportPoint.y <= 1f + viewportPadding.y;
    }

    private void SetActivated(bool activated)
    {
        isActivated = activated;

        for (int i = 0; i < behavioursToActivate.Length; i++)
        {
            Behaviour behaviour = behavioursToActivate[i];

            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            behaviour.enabled = activated;
        }

        for (int i = 0; i < animatorsToActivate.Length; i++)
        {
            Animator animator = animatorsToActivate[i];

            if (animator == null)
            {
                continue;
            }

            animator.enabled = activated;
        }

        if (activated)
        {
            WakeRigidbodies();
        }
        else
        {
            SleepRigidbodies();
        }

        if (logActivationChanges)
        {
            Debug.Log(
                name + " camera activation: " + (activated ? "active" : "inactive"),
                this
            );
        }
    }

    private void WakeRigidbodies()
    {
        for (int i = 0; i < rigidbodiesToWake.Length; i++)
        {
            Rigidbody body = rigidbodiesToWake[i];

            if (body == null)
            {
                continue;
            }

            body.WakeUp();
        }
    }

    private void SleepRigidbodies()
    {
        for (int i = 0; i < rigidbodiesToWake.Length; i++)
        {
            Rigidbody body = rigidbodiesToWake[i];

            if (body == null)
            {
                continue;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

        if (cameraToUse == null)
        {
            return;
        }

        bool isNearCamera = false;

        if (cameraToUse != null)
        {
            Vector3 viewportPoint = cameraToUse.WorldToViewportPoint(transform.position);
            isNearCamera = viewportPoint.z >= 0f &&
                viewportPoint.x >= -viewportPadding.x &&
                viewportPoint.x <= 1f + viewportPadding.x &&
                viewportPoint.y >= -viewportPadding.y &&
                viewportPoint.y <= 1f + viewportPadding.y;
        }

        Gizmos.color = isNearCamera ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}
