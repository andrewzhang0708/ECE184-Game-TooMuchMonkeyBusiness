using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int startingLives = 3;

    [Header("Damage Source")]
    [Tooltip("Optional. If set, objects with this tag can damage the player.")]
    [SerializeField] private string enemyTag = "Enemy";
    [Tooltip("Optional. Leave as Nothing to ignore layer checks.")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1f;

    [Header("Control Lock")]
    [Tooltip("Disable player control scripts while invincible so knockback can carry the player away.")]
    [SerializeField] private bool disableControlsWhileInvincible = true;
    [SerializeField] private PlayerController2D playerController;
    [SerializeField] private BananaShooter bananaShooter;

    [Header("Knockback")]
    [SerializeField] private float knockbackHorizontalForce = 7f;
    [SerializeField] private float knockbackHorizontalDeceleration = 7f;
    [SerializeField] private float knockbackUpForce = 3f;

    [Header("Hit Gravity")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private bool applyGravityWhileAscending = true;

    [Header("Jump Stomp")]
    [SerializeField] private bool enableJumpStomp = true;
    [SerializeField] private float stompTopTolerance = 0.35f;
    [SerializeField] private float minimumStompDownwardVelocity = -0.05f;
    [SerializeField] private float stompFallMemoryDuration = 0.2f;
    [SerializeField] private float stompBounceVelocity = 7f;
    [SerializeField] private bool logStompDebug = true;

    [Header("Death")]
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private string startScreenSceneName = "StartScreen";

    private Rigidbody rb;
    private int currentLives;
    private float invincibleUntil;
    private Coroutine controlLockRoutine;
    private bool isDead;
    private float lastDownwardVelocity;
    private float lastDownwardTime = float.NegativeInfinity;

    public int CurrentLives => currentLives;
    public bool IsInvincible => Time.time < invincibleUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentLives = startingLives;

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController2D>();
        }

        if (bananaShooter == null)
        {
            bananaShooter = GetComponent<BananaShooter>();
        }
    }

    private void FixedUpdate()
    {
        UpdateStompFallMemory();

        if (!IsInvincible || currentLives <= 0 || isDead)
        {
            return;
        }

        if (playerController != null && playerController.enabled)
        {
            return;
        }

        UpdateKnockbackHorizontalDecay();
        ApplyExtraGravity();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTakeDamage(collision.collider, collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryTakeDamage(collision.collider, collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTakeDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTakeDamage(other);
    }

    private void TryTakeDamage(Collider damageCollider)
    {
        TryTakeDamage(damageCollider, null);
    }

    private void TryTakeDamage(Collider damageCollider, Collision collision)
    {
        if (IsInvincible || currentLives <= 0 || isDead)
        {
            return;
        }

        if (!IsEnemyCollider(damageCollider))
        {
            return;
        }

        EnemyStompStun enemyStun = damageCollider.GetComponentInParent<EnemyStompStun>();
        if (enemyStun != null && enemyStun.IsStunned)
        {
            TryStompEnemy(damageCollider, enemyStun, collision);
            return;
        }

        if (TryStompEnemy(damageCollider, enemyStun, collision))
        {
            return;
        }

        currentLives = Mathf.Max(0, currentLives - 1);
        invincibleUntil = Time.time + invincibilityDuration;

        ApplyKnockback(damageCollider.transform.position);
        LockControlsDuringInvincibility();

        Debug.Log("Player took damage. Lives left: " + currentLives);

        if (currentLives <= 0)
        {
            Debug.Log("Player has no lives left.");
            StartCoroutine(DeathRoutine());
        }
    }

    private bool IsEnemyCollider(Collider damageCollider)
    {
        if (damageCollider == null)
        {
            return false;
        }

        if (damageCollider.GetComponentInParent<SimpleHoppingEnemy>() != null)
        {
            return true;
        }

        if (damageCollider.GetComponentInParent<RollingSphereEnemy>() != null)
        {
            return true;
        }

        if (
            !string.IsNullOrEmpty(enemyTag) &&
            damageCollider.GetComponentInParent<EnemyStompStun>() != null &&
            damageCollider.GetComponentInParent<EnemyStompStun>().CompareTag(enemyTag)
        )
        {
            return true;
        }

        if (!string.IsNullOrEmpty(enemyTag) && damageCollider.CompareTag(enemyTag))
        {
            return true;
        }

        return (enemyLayer.value & (1 << damageCollider.gameObject.layer)) != 0;
    }

    private bool TryStompEnemy(Collider enemyCollider, EnemyStompStun enemyStun, Collision collision)
    {
        if (!enableJumpStomp || enemyStun == null)
        {
            return false;
        }

        if (!HasRecentStompFall())
        {
            LogStompDebug(
                "not falling. current y velocity: " +
                rb.linearVelocity.y.ToString("F2") +
                ", recent downward velocity: " +
                lastDownwardVelocity.ToString("F2"),
                enemyStun
            );
            return false;
        }

        if (!TryGetBounds(GetComponentsInChildren<Collider>(), out Bounds playerBounds))
        {
            LogStompDebug("no player bounds", enemyStun);
            return false;
        }

        if (!TryGetBounds(enemyStun.GetComponentsInChildren<Collider>(), out Bounds enemyBounds))
        {
            enemyBounds = enemyCollider.bounds;
        }

        bool hasTopContact = collision == null || HasTopStompContact(collision, enemyBounds);
        bool playerIsAboveEnemy =
            playerBounds.center.y > enemyBounds.center.y ||
            playerBounds.min.y >= enemyBounds.center.y - stompTopTolerance;
        if (!playerIsAboveEnemy)
        {
            LogStompDebug(
                "player not above enemy. player center y: " +
                playerBounds.center.y.ToString("F2") +
                ", enemy center y: " +
                enemyBounds.center.y.ToString("F2"),
                enemyStun
            );
            return false;
        }

        if (!hasTopContact)
        {
            LogStompDebug("contact not on upper half of enemy", enemyStun);
            return false;
        }

        bool overlapsEnemyX =
            playerBounds.max.x >= enemyBounds.min.x &&
            playerBounds.min.x <= enemyBounds.max.x;
        bool overlapsEnemyZ =
            playerBounds.max.z >= enemyBounds.min.z &&
            playerBounds.min.z <= enemyBounds.max.z;
        if (!overlapsEnemyX || !overlapsEnemyZ)
        {
            LogStompDebug("player bounds did not overlap enemy x/z bounds", enemyStun);
            return false;
        }

        enemyStun.Stomp();
        Debug.Log("Player stomped enemy: " + enemyStun.name, enemyStun);
        BounceAfterStomp();
        return true;
    }

    private bool HasTopStompContact(Collision collision, Bounds enemyBounds)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            bool contactOnUpperHalf = contact.point.y >= enemyBounds.center.y - stompTopTolerance;

            if (contactOnUpperHalf)
            {
                return true;
            }
        }

        return false;
    }

    private void LogStompDebug(string message, Object context)
    {
        if (logStompDebug)
        {
            Debug.Log("Stomp failed: " + message, context);
        }
    }

    private void UpdateStompFallMemory()
    {
        if (rb == null)
        {
            return;
        }

        float verticalVelocity = rb.linearVelocity.y;
        if (verticalVelocity <= minimumStompDownwardVelocity)
        {
            lastDownwardVelocity = verticalVelocity;
            lastDownwardTime = Time.time;
        }
    }

    private bool HasRecentStompFall()
    {
        if (rb.linearVelocity.y <= minimumStompDownwardVelocity)
        {
            return true;
        }

        return Time.time - lastDownwardTime <= stompFallMemoryDuration;
    }

    private static bool TryGetBounds(Collider[] colliders, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider targetCollider = colliders[i];
            if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = targetCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetCollider.bounds);
            }
        }

        return hasBounds;
    }

    private void BounceAfterStomp()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = stompBounceVelocity;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void ApplyKnockback(Vector3 damageSourcePosition)
    {
        float horizontalDirection = Mathf.Sign(transform.position.x - damageSourcePosition.x);

        if (Mathf.Approximately(horizontalDirection, 0f))
        {
            horizontalDirection = 1f;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontalDirection * knockbackHorizontalForce;
        velocity.y = knockbackUpForce;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void UpdateKnockbackHorizontalDecay()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = Mathf.MoveTowards(
            velocity.x,
            0f,
            knockbackHorizontalDeceleration * Time.fixedDeltaTime
        );
        rb.linearVelocity = velocity;
    }

    private void ApplyExtraGravity()
    {
        Vector3 velocity = rb.linearVelocity;
        bool applyWhileAscending = applyGravityWhileAscending && velocity.y > 0f;

        if (velocity.y < 0f || applyWhileAscending)
        {
            velocity.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = velocity;
        }
    }

    private void LockControlsDuringInvincibility()
    {
        if (!disableControlsWhileInvincible)
        {
            return;
        }

        if (controlLockRoutine != null)
        {
            StopCoroutine(controlLockRoutine);
        }

        controlLockRoutine = StartCoroutine(ControlLockRoutine(invincibilityDuration));
    }

    private IEnumerator ControlLockRoutine(float duration)
    {
        bool wasPlayerControllerEnabled = playerController != null && playerController.enabled;
        bool wasBananaShooterEnabled = bananaShooter != null && bananaShooter.enabled;

        SetControlScriptsEnabled(false);

        yield return new WaitForSeconds(duration);

        if (playerController != null)
        {
            playerController.enabled = wasPlayerControllerEnabled;
        }

        if (bananaShooter != null)
        {
            bananaShooter.enabled = wasBananaShooterEnabled;
        }

        controlLockRoutine = null;
    }

    private void SetControlScriptsEnabled(bool enabled)
    {
        if (playerController != null)
        {
            playerController.enabled = enabled;
        }

        if (bananaShooter != null)
        {
            bananaShooter.enabled = enabled;
        }
    }

    private IEnumerator DeathRoutine()
    {
        isDead = true;

        if (controlLockRoutine != null)
        {
            StopCoroutine(controlLockRoutine);
            controlLockRoutine = null;
        }

        SetControlScriptsEnabled(false);

        yield return new WaitForSecondsRealtime(deathDelay);

        Time.timeScale = 1f;
        MenuController.OpenLevelPanelOnNextStart();
        SceneManager.LoadScene(startScreenSceneName);
    }
}
