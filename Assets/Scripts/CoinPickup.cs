using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [Tooltip("Optional. Assign a child visual if the collider is on a parent object.")]
    [SerializeField] private GameObject visual;

    [Header("Pickup Effect")]
    [SerializeField] private CoinPickupEffect pickupEffectPrefab;
    [SerializeField] private float effectHeadOffset = 0.2f;

    [Header("Pickup Audio")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField, Range(0f, 3f)] private float pickupVolume = 1f;

    private bool isCollected;

    private void Awake()
    {
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
        if (isCollected || player == null)
        {
            return;
        }

        isCollected = true;

        if (CoinCounter.Instance != null)
        {
            CoinCounter.Instance.AddCoins(value);
        }
        else
        {
            Debug.LogWarning("No CoinCounter exists in the scene.", this);
        }

        CoinPickupEffect.Spawn(
            pickupEffectPrefab,
            other,
            effectHeadOffset
        );

        if (pickupClip != null)
        {
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, pickupVolume);
        }

        if (visual != null)
        {
            visual.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
