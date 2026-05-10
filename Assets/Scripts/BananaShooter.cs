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

    [Header("Arc / Gravity")]
    public bool useGravity = true;

    [Tooltip("Extra downward acceleration. 0 = normal Unity gravity only. Try 15-40 for faster falling.")]
    public float extraDownwardAcceleration = 25f;

    [Tooltip("Optional upward angle at launch. 0 = perfectly horizontal. Try 5-15 if it drops too soon.")]
    public float upwardLaunchBoost = 2f;

    [Header("Direction")]
    public bool useFacingReference = true;

    private float nextShootTime = 0f;

    void Reset()
    {
        bananaTemplate = gameObject;
        firePoint = transform;
        facingReference = transform.root;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.cKey.wasPressedThisFrame && Time.time >= nextShootTime)
        {
            Shoot();
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

        Transform spawnTransform = firePoint != null ? firePoint : bananaTemplate.transform;
        Vector3 shootDir = GetShootDirection(spawnTransform);

        GameObject projectile = Instantiate(
            bananaTemplate,
            spawnTransform.position + shootDir * spawnOffset,
            spawnTransform.rotation
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

        Destroy(projectile, projectileLifetime);
    }

    Vector3 GetShootDirection(Transform spawnTransform)
    {
        Vector3 dir;

        if (useFacingReference && facingReference != null)
        {
            // For your 3D platformer, this usually means "the way the monkey/player is facing."
            dir = facingReference.forward;
        }
        else
        {
            // Fallback: use the banana/firePoint's forward direction.
            dir = spawnTransform.forward;
        }

        // Make the launch mostly horizontal, then add a small upward component.
        dir.y = 0f;

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