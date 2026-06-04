using UnityEngine;

public class Popup : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Transform player;
    [SerializeField] private float radius = 6f;

    [Header("Popup")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private float speed = 4f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popupClip;
    [SerializeField, Range(0f, 3f)] private float popupVolume = 1f;

    private Vector3 hiddenPosition;
    private Vector3 shownPosition;
    private bool isTriggered;
    private bool hasPlayedSound;

    private void Awake()
    {
        shownPosition = transform.position;
        hiddenPosition = shownPosition + Vector3.down * distance;
        transform.position = hiddenPosition;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!isTriggered && IsPlayerCloseOnX())
        {
            isTriggered = true;
            PlayPopupSound();
        }

        if (!isTriggered)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            shownPosition,
            speed * Time.deltaTime
        );
    }

    private bool IsPlayerCloseOnX()
    {
        if (player == null)
        {
            return false;
        }

        return Mathf.Abs(player.position.x - shownPosition.x) <= radius;
    }

    private void PlayPopupSound()
    {
        if (hasPlayedSound || popupClip == null)
        {
            return;
        }

        hasPlayedSound = true;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(popupClip, popupVolume);
            return;
        }

        AudioSource.PlayClipAtPoint(popupClip, transform.position, popupVolume);
    }
}
