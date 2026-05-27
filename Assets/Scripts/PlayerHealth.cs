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
    [SerializeField] private ChimpMovement chimpMovement;
    [SerializeField] private PlayerHandGrabSwing handGrabSwing;
    [SerializeField] private BananaShooter bananaShooter;

    [Header("Knockback")]
    [SerializeField] private float knockbackHorizontalForce = 7f;
    [SerializeField] private float knockbackUpForce = 3f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private string startScreenSceneName = "StartScreen";

    private Rigidbody rb;
    private int currentLives;
    private float invincibleUntil;
    private Coroutine controlLockRoutine;
    private bool isDead;

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

        if (chimpMovement == null)
        {
            chimpMovement = GetComponent<ChimpMovement>();
        }

        if (handGrabSwing == null)
        {
            handGrabSwing = GetComponent<PlayerHandGrabSwing>();
        }

        if (bananaShooter == null)
        {
            bananaShooter = GetComponent<BananaShooter>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTakeDamage(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryTakeDamage(collision.collider);
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
        if (IsInvincible || currentLives <= 0 || isDead)
        {
            return;
        }

        if (!IsEnemyCollider(damageCollider))
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

        if (!string.IsNullOrEmpty(enemyTag) && damageCollider.gameObject.tag == enemyTag)
        {
            return true;
        }

        return (enemyLayer.value & (1 << damageCollider.gameObject.layer)) != 0;
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
        bool wasChimpMovementEnabled = chimpMovement != null && chimpMovement.enabled;
        bool wasHandGrabSwingEnabled = handGrabSwing != null && handGrabSwing.enabled;
        bool wasBananaShooterEnabled = bananaShooter != null && bananaShooter.enabled;

        SetControlScriptsEnabled(false);

        yield return new WaitForSeconds(duration);

        if (playerController != null)
        {
            playerController.enabled = wasPlayerControllerEnabled;
        }

        if (chimpMovement != null)
        {
            chimpMovement.enabled = wasChimpMovementEnabled;
        }

        if (handGrabSwing != null)
        {
            handGrabSwing.enabled = wasHandGrabSwingEnabled;
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

        if (chimpMovement != null)
        {
            chimpMovement.enabled = enabled;
        }

        if (handGrabSwing != null)
        {
            handGrabSwing.enabled = enabled;
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
