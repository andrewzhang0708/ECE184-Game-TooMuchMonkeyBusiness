using System.Collections;
using UnityEngine;

public class SpringBoard : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Transform topPlatform;
    [SerializeField] private Transform springVisual;

    [Header("Movement")]
    [SerializeField] private float platformUpDistance = 0.8f;
    [SerializeField] private float riseTime = 0.12f;
    [SerializeField] private float fallTime = 0.25f;

    [Header("Bounce")]
    [SerializeField] private float bounceForce = 14f;

    [Header("Reset")]
    [SerializeField] private float resetDelay = 1f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip triggerSound;

    private Vector3 platformStartPosition;
    private Vector3 platformUpPosition;
    private bool isTriggered;

    private void Start()
    {
        platformStartPosition = topPlatform.localPosition;
        platformUpPosition = platformStartPosition + Vector3.up * platformUpDistance;

        topPlatform.gameObject.SetActive(true);
        springVisual.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider other)
    {
        if (isTriggered)
        {
            return;
        }

        Rigidbody playerRigidbody = other.attachedRigidbody;

        if (playerRigidbody == null || !playerRigidbody.CompareTag("Player"))
        {
            return;
        }

        isTriggered = true;
        springVisual.gameObject.SetActive(true);
        PlayTriggerSound();
        BouncePlayer(playerRigidbody);
        StartCoroutine(SpringRoutine());
    }

    private void PlayTriggerSound()
    {
        if (audioSource != null && triggerSound != null)
        {
            audioSource.PlayOneShot(triggerSound);
        }
    }

    private IEnumerator SpringRoutine()
    {
        yield return MovePlatform(platformStartPosition, platformUpPosition, riseTime);
        yield return new WaitForSeconds(resetDelay);
        yield return MovePlatform(platformUpPosition, platformStartPosition, fallTime);

        springVisual.gameObject.SetActive(false);
        isTriggered = false;
    }

    private IEnumerator MovePlatform(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            topPlatform.localPosition = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            topPlatform.localPosition = Vector3.Lerp(from, to, timer / duration);
            yield return null;
        }

        topPlatform.localPosition = to;
    }

    private void BouncePlayer(Rigidbody playerRigidbody)
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.y = 0f;
        playerRigidbody.linearVelocity = velocity;

        playerRigidbody.AddForce(
            Vector3.up * bounceForce,
            ForceMode.VelocityChange
        );
    }
}
