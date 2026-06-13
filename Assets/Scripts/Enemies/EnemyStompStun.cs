using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyStompStun : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundSnapSearchDistance = 2f;
    [SerializeField] private float groundSkin = 0.02f;

    [Header("Fall")]
    [SerializeField] private float fallMultiplier = 8.95f;

    [Header("Stun")]
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private ParticleSystem stunParticleEffect;
    [Tooltip("Extra height above the enemy bounds for the automatically generated star effect.")]
    [SerializeField] private float stunEffectHeightOffset = 0.35f;
    [Tooltip("Additional local XYZ offset for the automatically generated star effect.")]
    [SerializeField] private Vector3 stunEffectPositionOffset;
    [SerializeField, Min(0.01f)] private float stunEffectSize = 1f;
    [SerializeField] private AudioSource stunAudioSource;
    [SerializeField] private AudioClip stunSoundEffect;
    [SerializeField, Range(0f, 3f)] private float stunVolume = 1f;

    private Rigidbody rb;
    private Collider[] colliders;
    private MonoBehaviour[] scripts;
    private AudioSource[] audioSources;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private Coroutine stompRoutine;
    private bool isStunned;
    private bool isResolvingStun;
    private bool hadRigidbody;
    private bool hasSavedRigidbodyState;
    private bool savedIsKinematic;
    private bool savedUseGravity;
    private RigidbodyConstraints savedConstraints;

    public bool IsStunned => isStunned;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        scripts = GetComponentsInChildren<MonoBehaviour>();
        audioSources = GetComponentsInChildren<AudioSource>();

        if (stunAudioSource == null)
        {
            stunAudioSource = GetComponent<AudioSource>();
        }

        if (stunParticleEffect == null)
        {
            stunParticleEffect = StunStarParticleFactory.Create(
                transform,
                GetAutomaticStunEffectPosition() + stunEffectPositionOffset,
                stunEffectSize
            );
        }
        else
        {
            StunStarParticleFactory.ApplyStarRenderer(stunParticleEffect);
        }
    }

    public void Stomp()
    {
        if (stompRoutine != null)
        {
            StopCoroutine(stompRoutine);
        }

        stompRoutine = StartCoroutine(StompRoutine());
    }

    public void CancelStunForDefeat()
    {
        if (stompRoutine != null)
        {
            StopCoroutine(stompRoutine);
            stompRoutine = null;
        }

        if (stunParticleEffect != null)
        {
            stunParticleEffect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        isStunned = false;
        isResolvingStun = false;
    }

    private IEnumerator StompRoutine()
    {
        isResolvingStun = true;
        isStunned = true;

        SetScriptsEnabled(false);
        StopAndDisableAudioSources();
        EnsureRigidbody();

        if (stunParticleEffect != null)
        {
            stunParticleEffect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
            stunParticleEffect.Play();
        }

        PlayStunSound();

        while (!IsGrounded())
        {
            ApplyExtraFallGravity();
            yield return new WaitForFixedUpdate();
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
        SnapAboveGround();
        rb.isKinematic = true;
        rb.useGravity = false;

        yield return new WaitForSeconds(stunDuration);

        if (stunParticleEffect != null)
        {
            stunParticleEffect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        SetScriptsEnabled(true);
        SetAudioSourcesEnabled(true);
        RestoreRigidbody();
        isStunned = false;
        isResolvingStun = false;
        stompRoutine = null;
    }

    private void EnsureRigidbody()
    {
        if (!hasSavedRigidbodyState)
        {
            hadRigidbody = rb != null;
        }

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        if (!hasSavedRigidbodyState)
        {
            savedIsKinematic = rb.isKinematic;
            savedUseGravity = rb.useGravity;
            savedConstraints = rb.constraints;
            hasSavedRigidbodyState = true;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints =
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation;
    }

    private void RestoreRigidbody()
    {
        if (rb == null)
        {
            return;
        }

        rb.isKinematic = savedIsKinematic;
        rb.useGravity = savedUseGravity;
        rb.constraints = savedConstraints;
        hasSavedRigidbodyState = false;

        if (!hadRigidbody)
        {
            Destroy(rb);
            rb = null;
        }
    }

    private void SetScriptsEnabled(bool enabled)
    {
        if (scripts == null)
        {
            return;
        }

        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour script = scripts[i];
            if (script != null && script != this)
            {
                script.enabled = enabled;
            }
        }
    }

    private void StopAndDisableAudioSources()
    {
        if (audioSources == null)
        {
            return;
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source == null)
            {
                continue;
            }

            source.Stop();

            if (source != stunAudioSource)
            {
                source.enabled = false;
            }
        }
    }

    private void SetAudioSourcesEnabled(bool enabled)
    {
        if (audioSources == null)
        {
            return;
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && source != stunAudioSource)
            {
                source.enabled = enabled;
            }
        }
    }

    private bool IsGrounded()
    {
        if (!TryGetColliderBounds(out Bounds bounds))
        {
            return true;
        }

        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = groundHits[i].collider;
            if (hitCollider != null && !IsOwnCollider(hitCollider))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetColliderBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        if (colliders == null)
        {
            return false;
        }

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

    private Vector3 GetAutomaticStunEffectPosition()
    {
        if (!TryGetColliderBounds(out Bounds bounds))
        {
            return Vector3.up * (1f + stunEffectHeightOffset);
        }

        Vector3 worldPosition = new Vector3(
            bounds.center.x,
            bounds.max.y + stunEffectHeightOffset,
            bounds.center.z
        );
        return transform.InverseTransformPoint(worldPosition);
    }

    private bool IsOwnCollider(Collider targetCollider)
    {
        if (colliders == null)
        {
            return false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == targetCollider)
            {
                return true;
            }
        }

        return false;
    }

    private void SnapAboveGround()
    {
        if (!TryGetColliderBounds(out Bounds bounds))
        {
            return;
        }

        Vector3 origin = bounds.center + Vector3.up * Mathf.Max(0.1f, groundSnapSearchDistance * 0.5f);
        float rayDistance = groundSnapSearchDistance + bounds.extents.y;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            rayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        float highestGroundY = float.NegativeInfinity;
        bool foundGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = groundHits[i].collider;
            if (hitCollider == null || IsOwnCollider(hitCollider))
            {
                continue;
            }

            if (groundHits[i].point.y > highestGroundY)
            {
                highestGroundY = groundHits[i].point.y;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            return;
        }

        float targetBottomY = highestGroundY + groundSkin;
        float liftAmount = targetBottomY - bounds.min.y;
        if (liftAmount > 0f)
        {
            transform.position += Vector3.up * liftAmount;
        }
    }

    private void ApplyExtraFallGravity()
    {
        if (rb == null || rb.linearVelocity.y >= 0f)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
    }

    private void PlayStunSound()
    {
        if (stunSoundEffect == null)
        {
            return;
        }

        if (stunAudioSource != null)
        {
            stunAudioSource.enabled = true;
            stunAudioSource.PlayOneShot(stunSoundEffect, stunVolume);
            return;
        }

        AudioGainFilter.PlayClipAtPoint(
            stunSoundEffect,
            transform.position,
            stunVolume,
            1f
        );
    }
}
