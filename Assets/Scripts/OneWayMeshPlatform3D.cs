using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class OneWayMeshPlatform3D : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Optional. Leave empty to find the Rigidbody on the object tagged Player.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Detection")]
    [Tooltip("How far beyond the platform bounds to start checking for the player.")]
    [SerializeField] private float horizontalMargin = 0.5f;
    [Tooltip("The player's feet must clear the top by this amount before collision returns.")]
    [SerializeField] private float reenableClearance = 0.05f;
    [Tooltip("Small upward movements below this speed do not disable the platform.")]
    [SerializeField] private float upwardSpeedThreshold = 0.01f;

    private MeshCollider platformCollider;
    private Collider[] playerColliders;
    private Bounds platformBounds;

    public bool IsPassingThrough { get; private set; }

    private void Awake()
    {
        platformCollider = GetComponent<MeshCollider>();

        if (playerRigidbody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerRigidbody = player.GetComponentInParent<Rigidbody>();
            }
        }

        if (playerRigidbody == null)
        {
            Debug.LogError(
                "OneWayMeshPlatform3D could not find the player's Rigidbody. " +
                "Assign it in the Inspector or tag the player as Player.",
                this
            );
            enabled = false;
            return;
        }

        playerColliders = playerRigidbody.GetComponentsInChildren<Collider>();
        RefreshPlatformBounds();
    }

    private void FixedUpdate()
    {
        RefreshPlatformBounds();

        if (!TryGetPlayerBounds(out Bounds playerBounds))
        {
            return;
        }

        bool isHorizontallyNear = IsHorizontallyNearPlatform(playerBounds);

        if (IsPassingThrough)
        {
            if (!isHorizontallyNear || IsPlayerAbovePlatform(playerBounds))
            {
                SetPassingThrough(false);
            }

            return;
        }

        bool isMovingUpThroughPlatform =
            playerRigidbody.linearVelocity.y > upwardSpeedThreshold &&
            !IsPlayerAbovePlatform(playerBounds);

        if (isHorizontallyNear && isMovingUpThroughPlatform)
        {
            SetPassingThrough(true);
        }
    }

    private void OnDisable()
    {
        SetPassingThrough(false);
    }

    private bool IsHorizontallyNearPlatform(Bounds playerBounds)
    {
        return playerBounds.max.x >= platformBounds.min.x - horizontalMargin &&
               playerBounds.min.x <= platformBounds.max.x + horizontalMargin &&
               playerBounds.max.z >= platformBounds.min.z - horizontalMargin &&
               playerBounds.min.z <= platformBounds.max.z + horizontalMargin;
    }

    private void RefreshPlatformBounds()
    {
        platformBounds = platformCollider.bounds;
    }

    private bool IsPlayerAbovePlatform(Bounds playerBounds)
    {
        return TryGetSurfaceHeight(playerBounds.center, out float surfaceHeight) &&
               playerBounds.min.y >= surfaceHeight + reenableClearance;
    }

    private bool TryGetSurfaceHeight(Vector3 playerCenter, out float surfaceHeight)
    {
        Vector3 rayOrigin = new Vector3(
            playerCenter.x,
            platformBounds.max.y + 1f,
            playerCenter.z
        );
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (platformCollider.Raycast(ray, out RaycastHit hit, platformBounds.size.y + 2f))
        {
            surfaceHeight = hit.point.y;
            return true;
        }

        surfaceHeight = 0f;
        return false;
    }

    private void SetPassingThrough(bool isPassingThrough)
    {
        if (IsPassingThrough == isPassingThrough || platformCollider == null)
        {
            return;
        }

        IsPassingThrough = isPassingThrough;

        if (playerColliders == null)
        {
            return;
        }

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];

            if (playerCollider != null)
            {
                Physics.IgnoreCollision(platformCollider, playerCollider, isPassingThrough);
            }
        }
    }

    private bool TryGetPlayerBounds(out Bounds playerBounds)
    {
        playerBounds = default;
        bool foundCollider = false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];

            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
            {
                continue;
            }

            if (!foundCollider)
            {
                playerBounds = playerCollider.bounds;
                foundCollider = true;
            }
            else
            {
                playerBounds.Encapsulate(playerCollider.bounds);
            }
        }

        return foundCollider;
    }
}
