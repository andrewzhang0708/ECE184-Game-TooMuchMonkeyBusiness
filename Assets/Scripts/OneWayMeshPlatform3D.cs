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
    private float platformTop;

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
        if (platformCollider.enabled)
        {
            RefreshPlatformBounds();
        }

        if (!TryGetPlayerBounds(out Bounds playerBounds))
        {
            return;
        }

        bool isHorizontallyNear = IsHorizontallyNearPlatform(playerBounds);

        if (!platformCollider.enabled)
        {
            if (!isHorizontallyNear || playerBounds.min.y >= platformTop + reenableClearance)
            {
                platformCollider.enabled = true;
                RefreshPlatformBounds();
            }

            return;
        }

        bool isBelowPlatform = playerBounds.max.y < platformTop;
        bool isMovingUpThroughPlatform =
            playerRigidbody.linearVelocity.y > upwardSpeedThreshold &&
            playerBounds.min.y <= platformTop + reenableClearance;

        if (isHorizontallyNear && (isBelowPlatform || isMovingUpThroughPlatform))
        {
            platformCollider.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }
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
        platformTop = platformBounds.max.y;
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
