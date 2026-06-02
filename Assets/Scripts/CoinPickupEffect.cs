using UnityEngine;

public class CoinPickupEffect : MonoBehaviour
{
    [Header("Arc")]
    [SerializeField, Min(0.01f)] private float lifetime = 1f;
    [SerializeField] private float initialVerticalSpeed = 4f;
    [SerializeField, Min(0f)] private float downwardAcceleration = 8f;

    private Vector3 startPosition;
    private Collider followCollider;
    private float followHeightOffset;
    private float elapsed;

    public static CoinPickupEffect Spawn(
        CoinPickupEffect prefab,
        Collider playerCollider,
        float heightOffset
    )
    {
        if (prefab == null || playerCollider == null)
        {
            return null;
        }

        Bounds playerBounds = playerCollider.bounds;
        Vector3 spawnPosition = new Vector3(
            playerBounds.center.x,
            playerBounds.max.y + heightOffset,
            playerBounds.center.z
        );
        CoinPickupEffect effect = Instantiate(
            prefab,
            spawnPosition,
            prefab.transform.rotation
        );
        effect.followCollider = playerCollider;
        effect.followHeightOffset = heightOffset;
        return effect;
    }

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float arcHeight =
            initialVerticalSpeed * elapsed -
            0.5f * downwardAcceleration * elapsed * elapsed;
        Vector3 position = startPosition + Vector3.up * arcHeight;
        if (followCollider != null)
        {
            Bounds playerBounds = followCollider.bounds;
            position = new Vector3(
                playerBounds.center.x,
                playerBounds.max.y + followHeightOffset + arcHeight,
                playerBounds.center.z
            );
        }

        transform.position = position;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
