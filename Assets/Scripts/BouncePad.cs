using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] private float bounceVelocity = 18f;
    [SerializeField] private bool onlyBounceWhenFalling = true;

    [Header("Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private string bounceTriggerName = "Bounce";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bounceSound;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 0.1f;

    private float lastBounceTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        TryBounce(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBounce(other);
    }

    private void TryBounce(Collider other)
    {
        if (Time.time - lastBounceTime < cooldown)
        {
            return;
        }

        Rigidbody playerRb = other.attachedRigidbody;

        if (playerRb == null)
        {
            return;
        }

        if (!playerRb.CompareTag("Player"))
        {
            return;
        }

        if (onlyBounceWhenFalling && playerRb.linearVelocity.y > 0f)
        {
            return;
        }

        Vector3 velocity = playerRb.linearVelocity;
        velocity.y = bounceVelocity;
        playerRb.linearVelocity = velocity;

        lastBounceTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(bounceTriggerName);
        }

        if (audioSource != null && bounceSound != null)
        {
            audioSource.PlayOneShot(bounceSound);
        }
    }
}