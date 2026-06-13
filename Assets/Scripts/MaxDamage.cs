using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MaxDamage : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] private float upwardVelocity = 12f;
    [SerializeField, Min(0f)] private float deathDelay = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField, Range(0f, 10f)] private float hitVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float playbackSpeed = 1f;

    private void OnTriggerEnter(Collider other)
    {
        ApplyFatalDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ApplyFatalDamage(collision.collider);
    }

    private void ApplyFatalDamage(Collider other)
    {
        if (other == null)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        Rigidbody playerRigidbody = playerHealth.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.linearVelocity = Vector3.up * upwardVelocity;
        }

        if (hitClip != null)
        {
            GameObject audioObject = new GameObject("Max Damage Audio");
            audioObject.transform.position = playerHealth.transform.position;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.pitch = playbackSpeed;
            source.spatialBlend = 0f;

            AudioGainFilter gainFilter = audioObject.AddComponent<AudioGainFilter>();
            gainFilter.Gain = Mathf.Max(1f, hitVolume);

            AudioCategoryVolume category =
                audioObject.AddComponent<AudioCategoryVolume>();
            category.IsMusic = false;

            source.PlayOneShot(hitClip, Mathf.Min(hitVolume, 1f));
            Destroy(audioObject, hitClip.length / playbackSpeed + 0.1f);
        }

        playerHealth.TakeFatalDamage(deathDelay);
    }
}
