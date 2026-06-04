using UnityEngine;
using UnityEngine.InputSystem;

public class BananaShooter : MonoBehaviour
{
    [Header("References")]
    public GameObject bananaTemplate;
    public Transform firePoint;

    [Tooltip("Usually drag Player2 or MonkeyVisual here. The banana fires toward this object's facing direction.")]
    public Transform facingReference;

    [Header("Shooting")]
    public float shootSpeed = 18f;
    public float cooldown = 0.2f;
    public float projectileLifetime = 4f;
    public float spawnOffset = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootClip;
    [Range(0f, 3f)] public float shootVolume = 1f;

    [Header("Arc / Gravity")]
    public bool useGravity = true;

    [Tooltip("Extra downward acceleration. 0 = normal Unity gravity only. Try 15-40 for faster falling.")]
    public float extraDownwardAcceleration = 25f;

    [Tooltip("Optional upward angle at launch. 0 = perfectly horizontal. Try 5-15 if it drops too soon.")]
    public float upwardLaunchBoost = 2f;

    [Header("Direction")]
    public bool useFacingReference = true;
    public PlayerController2D playerController;
    [Tooltip("Local axis on facingReference that points out of the character. Try (1,0,0) if Forward does not match the model.")]
    public Vector3 localFacingAxis = Vector3.forward;
    public bool forceWorldXDirection = true;

    [Header("Projectile Hit Ignore")]
    public string[] ignoredHitTags = { "Collectible" };
    public LayerMask ignoredHitLayers;

    [Header("Enemy Defeat")]
    public string enemyTag = "Enemy";
    public float enemyDefeatUpVelocity = 5f;
    public float enemyDefeatHorizontalVelocity = 1.5f;
    public float enemyDefeatFallMultiplier = 2.5f;
    public float enemyDestroyDelay = 4f;

    private float nextShootTime = 0f;

    void Reset()
    {
        bananaTemplate = gameObject;
        firePoint = transform;
        facingReference = transform.root;
        playerController = GetComponentInParent<PlayerController2D>();
    }

    void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController2D>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && Time.time >= nextShootTime)
        {
            Shoot();
            Debug.Log("Banana shot!");
            nextShootTime = Time.time + cooldown;
        }
    }

    void Shoot()
    {
        if (bananaTemplate == null)
        {
            Debug.LogWarning("BananaShooter: bananaTemplate is not assigned.");
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = bananaTemplate.transform.rotation;
        Vector3 shootDir = GetShootDirection();

        GameObject projectile = Instantiate(
            bananaTemplate,
            spawnPosition + shootDir * spawnOffset,
            spawnRotation
        );

        projectile.name = "Banana Projectile";
        projectile.transform.SetParent(null, true);

        BananaShooter shooterOnProjectile = projectile.GetComponent<BananaShooter>();
        if (shooterOnProjectile != null)
        {
            Destroy(shooterOnProjectile);
        }

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
        }

        Collider col = projectile.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = projectile.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }

        rb.useGravity = useGravity;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.linearVelocity = shootDir * shootSpeed;

        BananaProjectileGravity gravity = projectile.GetComponent<BananaProjectileGravity>();
        if (gravity == null)
        {
            gravity = projectile.AddComponent<BananaProjectileGravity>();
        }
        gravity.extraDownwardAcceleration = extraDownwardAcceleration;

        BananaProjectileHitDestroy hitDestroy = projectile.GetComponent<BananaProjectileHitDestroy>();
        if (hitDestroy == null)
        {
            hitDestroy = projectile.AddComponent<BananaProjectileHitDestroy>();
        }
        hitDestroy.ownerRoot = transform.root;
        hitDestroy.ignoredTags = ignoredHitTags;
        hitDestroy.ignoredLayers = ignoredHitLayers;
        hitDestroy.enemyTag = enemyTag;
        hitDestroy.enemyDefeatUpVelocity = enemyDefeatUpVelocity;
        hitDestroy.enemyDefeatHorizontalVelocity = enemyDefeatHorizontalVelocity;
        hitDestroy.enemyDefeatFallMultiplier = enemyDefeatFallMultiplier;
        hitDestroy.enemyDestroyDelay = enemyDestroyDelay;

        PlayShootSound();

        Destroy(projectile, projectileLifetime);
    }

    void PlayShootSound()
    {
        if (shootClip == null)
        {
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(shootClip, shootVolume);
            return;
        }

        Vector3 soundPosition = firePoint != null ? firePoint.position : transform.position;
        AudioSource.PlayClipAtPoint(shootClip, soundPosition, shootVolume);
    }

    Vector3 GetShootDirection()
    {
        Vector3 dir;

        if (useFacingReference && facingReference != null)
        {
            // For your 3D platformer, this usually means "the way the monkey/player is facing."
            Vector3 facingAxis = localFacingAxis.sqrMagnitude > 0f
                ? localFacingAxis.normalized
                : Vector3.forward;
            dir = facingReference.TransformDirection(facingAxis);
        }
        else
        {
            // Fallback: use this shooter's forward direction.
            dir = transform.forward;
        }

        // Make the launch mostly horizontal, then add a small upward component.
        dir.y = 0f;

        if (forceWorldXDirection)
        {
            if (playerController != null)
            {
                return playerController.IsFacingRight ? Vector3.right : Vector3.left;
            }

            float xDirection = dir.x;

            if (Mathf.Approximately(xDirection, 0f) && facingReference != null)
            {
                xDirection = facingReference.forward.x;
            }

            if (Mathf.Approximately(xDirection, 0f) && facingReference != null)
            {
                xDirection = facingReference.right.x;
            }

            dir = xDirection >= 0f ? Vector3.right : Vector3.left;
        }

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
            dir.y = 0f;
        }

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector3.forward;
        }

        dir = dir.normalized;
        dir += Vector3.up * upwardLaunchBoost / shootSpeed;

        return dir.normalized;
    }
}

public class BananaProjectileGravity : MonoBehaviour
{
    public float extraDownwardAcceleration = 25f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        rb.AddForce(Vector3.down * extraDownwardAcceleration, ForceMode.Acceleration);
    }
}

public class BananaProjectileHitDestroy : MonoBehaviour
{
    public Transform ownerRoot;
    public string[] ignoredTags;
    public LayerMask ignoredLayers;
    public string enemyTag = "Enemy";
    public float enemyDefeatUpVelocity = 5f;
    public float enemyDefeatHorizontalVelocity = 1.5f;
    public float enemyDefeatFallMultiplier = 2.5f;
    public float enemyDestroyDelay = 4f;

    private bool hasHit;

    private void OnCollisionEnter(Collision collision)
    {
        TryDestroy(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDestroy(other);
    }

    private void TryDestroy(Collider other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        if (ShouldIgnore(other))
        {
            return;
        }

        Transform enemyRoot = FindTaggedRoot(other.transform, enemyTag);
        if (enemyRoot != null)
        {
            hasHit = true;
            DefeatedEnemyFall.Defeat(
                enemyRoot.gameObject,
                transform.position,
                enemyDefeatUpVelocity,
                enemyDefeatHorizontalVelocity,
                enemyDefeatFallMultiplier,
                enemyDestroyDelay
            );
            Destroy(gameObject);
            return;
        }

        hasHit = true;
        Destroy(gameObject);
    }

    private Transform FindTaggedRoot(Transform target, string tagToFind)
    {
        if (target == null || string.IsNullOrEmpty(tagToFind))
        {
            return null;
        }

        Transform current = target;
        while (current != null)
        {
            if (current.CompareTag(tagToFind))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private bool ShouldIgnore(Collider other)
    {
        if ((ignoredLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            return true;
        }

        if (ignoredTags == null)
        {
            return false;
        }

        for (int i = 0; i < ignoredTags.Length; i++)
        {
            string ignoredTag = ignoredTags[i];
            if (!string.IsNullOrEmpty(ignoredTag) && other.CompareTag(ignoredTag))
            {
                return true;
            }
        }

        return false;
    }
}

public class DefeatedEnemyFall : MonoBehaviour
{
    private Rigidbody rb;
    private float fallMultiplier = 2.5f;

    public static void Defeat(
        GameObject enemyRoot,
        Vector3 hitPosition,
        float upVelocity,
        float horizontalVelocity,
        float fallMultiplier,
        float destroyDelay
    )
    {
        if (enemyRoot == null || enemyRoot.GetComponent<DefeatedEnemyFall>() != null)
        {
            return;
        }

        EnemyStompStun stompStun = enemyRoot.GetComponentInChildren<EnemyStompStun>();
        if (stompStun != null)
        {
            stompStun.CancelStunForDefeat();
        }

        MonoBehaviour[] scripts = enemyRoot.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null && !(scripts[i] is DefeatedEnemyFall))
            {
                scripts[i].enabled = false;
            }
        }

        Collider[] colliders = enemyRoot.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        AudioSource[] audioSources = enemyRoot.GetComponentsInChildren<AudioSource>();
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                audioSources[i].Stop();
                audioSources[i].enabled = false;
            }
        }

        Rigidbody enemyRigidbody = enemyRoot.GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = enemyRoot.AddComponent<Rigidbody>();
        }

        enemyRigidbody.isKinematic = false;
        enemyRigidbody.useGravity = true;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

        float horizontalDirection = Mathf.Sign(enemyRoot.transform.position.x - hitPosition.x);
        if (Mathf.Approximately(horizontalDirection, 0f))
        {
            horizontalDirection = 1f;
        }

        enemyRigidbody.linearVelocity = new Vector3(
            horizontalDirection * horizontalVelocity,
            upVelocity,
            0f
        );

        DefeatedEnemyFall fall = enemyRoot.AddComponent<DefeatedEnemyFall>();
        fall.rb = enemyRigidbody;
        fall.fallMultiplier = fallMultiplier;

        Destroy(enemyRoot, destroyDelay);
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < 0f)
        {
            velocity.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = velocity;
        }
    }
}
