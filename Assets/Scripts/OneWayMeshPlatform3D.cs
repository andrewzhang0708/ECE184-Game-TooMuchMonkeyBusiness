using UnityEngine;

public class OneWayMeshPlatform3D : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Optional. Leave empty to find the Rigidbody on the object tagged Player.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Detection")]
    [Tooltip("How far outside the platform bounds the player can trigger pass-through.")]
    [SerializeField, Min(0f)] private float horizontalMargin = 0.5f;
    [Tooltip("Small velocity range treated as stationary.")]
    [SerializeField, Min(0f)] private float verticalVelocityEpsilon = 0.01f;
    [Tooltip("Extra height the player's feet must clear before collision is restored.")]
    [SerializeField, Min(0f)] private float reenableClearance = 0.05f;

    private Collider[] platformColliders;
    private Collider[] playerColliders;

    public bool IsPassingThrough { get; private set; }

    private void Awake()
    {
        platformColliders = GetComponentsInChildren<Collider>();

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

        if (!HasUsablePlatformCollider())
        {
            Debug.LogError(
                "OneWayMeshPlatform3D needs at least one enabled, non-trigger Collider.",
                this
            );
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!TryGetPlayerBounds(out Bounds playerBounds))
        {
            return;
        }

        float verticalVelocity = playerRigidbody.linearVelocity.y;
        bool isHorizontallyNear = IsHorizontallyNearPlatform(playerBounds);

        if (verticalVelocity > verticalVelocityEpsilon)
        {
            // Rising players must be able to pass through from below.
            if (isHorizontallyNear)
            {
                SetPassingThrough(true);
            }
            else if (IsPassingThrough && !IsInsidePlatform(playerBounds))
            {
                SetPassingThrough(false);
            }

            return;
        }

        if (!IsPassingThrough)
        {
            return;
        }

        // Never restore collision while the player still overlaps the platform.
        // If the player exits below it, collision stays ignored through the next
        // jump and is restored only after the player clears the top surface.
        if (IsInsidePlatform(playerBounds))
        {
            return;
        }

        if (!isHorizontallyNear)
        {
            SetPassingThrough(false);
            return;
        }

        if (IsCompletelyAbovePlatform(playerBounds))
        {
            SetPassingThrough(false);
        }
    }

    private void OnDisable()
    {
        SetPassingThrough(false);
    }

    private bool IsInsidePlatform(Bounds playerBounds)
    {
        for (int i = 0; i < platformColliders.Length; i++)
        {
            Collider platformPart = platformColliders[i];

            if (IsUsablePlatformCollider(platformPart) &&
                playerBounds.Intersects(platformPart.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsHorizontallyNearPlatform(Bounds playerBounds)
    {
        if (!TryGetPlatformBounds(out Bounds platformBounds))
        {
            return false;
        }

        return playerBounds.max.x >= platformBounds.min.x - horizontalMargin &&
               playerBounds.min.x <= platformBounds.max.x + horizontalMargin &&
               playerBounds.max.z >= platformBounds.min.z - horizontalMargin &&
               playerBounds.min.z <= platformBounds.max.z + horizontalMargin;
    }

    private bool IsCompletelyAbovePlatform(Bounds playerBounds)
    {
        if (!IsHorizontallyOverPlatform(playerBounds))
        {
            return false;
        }

        if (!TryGetSurfaceHeight(playerBounds.center, out float surfaceHeight))
        {
            return false;
        }

        return playerBounds.min.y >= surfaceHeight + reenableClearance;
    }

    private bool IsHorizontallyOverPlatform(Bounds playerBounds)
    {
        if (!TryGetPlatformBounds(out Bounds platformBounds))
        {
            return false;
        }

        return playerBounds.max.x >= platformBounds.min.x &&
               playerBounds.min.x <= platformBounds.max.x &&
               playerBounds.max.z >= platformBounds.min.z &&
               playerBounds.min.z <= platformBounds.max.z;
    }

    private bool TryGetSurfaceHeight(Vector3 playerCenter, out float surfaceHeight)
    {
        if (!TryGetPlatformBounds(out Bounds platformBounds))
        {
            surfaceHeight = 0f;
            return false;
        }

        Vector3 rayOrigin = new Vector3(
            playerCenter.x,
            platformBounds.max.y + 1f,
            playerCenter.z
        );
        Ray ray = new Ray(rayOrigin, Vector3.down);
        float rayDistance = platformBounds.size.y + 2f;
        bool foundSurface = false;
        surfaceHeight = float.NegativeInfinity;

        for (int i = 0; i < platformColliders.Length; i++)
        {
            Collider platformPart = platformColliders[i];

            if (!IsUsablePlatformCollider(platformPart) ||
                !platformPart.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                continue;
            }

            surfaceHeight = Mathf.Max(surfaceHeight, hit.point.y);
            foundSurface = true;
        }

        if (!foundSurface)
        {
            surfaceHeight = 0f;
        }

        return foundSurface;
    }

    private void SetPassingThrough(bool isPassingThrough)
    {
        if (IsPassingThrough == isPassingThrough || platformColliders == null)
        {
            return;
        }

        IsPassingThrough = isPassingThrough;

        if (playerColliders == null)
        {
            return;
        }

        for (int platformIndex = 0; platformIndex < platformColliders.Length; platformIndex++)
        {
            Collider platformPart = platformColliders[platformIndex];

            if (!IsUsablePlatformCollider(platformPart))
            {
                continue;
            }

            for (int playerIndex = 0; playerIndex < playerColliders.Length; playerIndex++)
            {
                Collider playerCollider = playerColliders[playerIndex];

                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(
                        platformPart,
                        playerCollider,
                        isPassingThrough
                    );
                }
            }
        }
    }

    private bool HasUsablePlatformCollider()
    {
        if (platformColliders == null)
        {
            return false;
        }

        for (int i = 0; i < platformColliders.Length; i++)
        {
            if (IsUsablePlatformCollider(platformColliders[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetPlatformBounds(out Bounds platformBounds)
    {
        platformBounds = default;
        bool foundCollider = false;

        for (int i = 0; i < platformColliders.Length; i++)
        {
            Collider platformPart = platformColliders[i];

            if (!IsUsablePlatformCollider(platformPart))
            {
                continue;
            }

            if (!foundCollider)
            {
                platformBounds = platformPart.bounds;
                foundCollider = true;
            }
            else
            {
                platformBounds.Encapsulate(platformPart.bounds);
            }
        }

        return foundCollider;
    }

    private static bool IsUsablePlatformCollider(Collider targetCollider)
    {
        return targetCollider != null &&
               targetCollider.enabled &&
               !targetCollider.isTrigger;
    }

    private bool TryGetPlayerBounds(out Bounds playerBounds)
    {
        playerBounds = default;
        bool foundCollider = false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];

            if (
                playerCollider == null ||
                !playerCollider.enabled ||
                playerCollider.isTrigger
            )
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
