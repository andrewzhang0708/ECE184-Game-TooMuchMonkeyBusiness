using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpecialPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [Tooltip("Optional. Assign a child visual if the collider is on a parent object.")]
    [SerializeField] private GameObject visual;

    [Header("Pickup Effect")]
    [SerializeField] private CoinPickupEffect pickupEffectPrefab;
    [SerializeField] private float effectHeadOffset = 0.2f;

    [Header("Pickup Audio")]
    [SerializeField] private AudioClip pickupClip;
    [Tooltip("Values above 1 use additional gain and may cause distortion.")]
    [SerializeField, Range(0f, 10f)] private float pickupVolume = 1f;

    private bool isCollected;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
        if (isCollected || player == null)
        {
            return;
        }

        if (SpecialPickupCounter.Instance == null)
        {
            Debug.LogWarning(
                "No SpecialPickupCounter exists in the scene.",
                this
            );
            return;
        }

        isCollected = true;
        SpecialPickupCounter.Instance.AddSpecial(value);

        CoinPickupEffect.Spawn(
            pickupEffectPrefab,
            other,
            effectHeadOffset
        );

        if (pickupClip != null)
        {
            AudioGainFilter.PlayClipAtPoint(
                pickupClip,
                transform.position,
                pickupVolume
            );
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
