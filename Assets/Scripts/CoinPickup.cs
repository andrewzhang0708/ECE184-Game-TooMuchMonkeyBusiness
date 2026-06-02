using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [Tooltip("Optional. Assign a child visual if the collider is on a parent object.")]
    [SerializeField] private GameObject visual;

    private bool isCollected;

    private void Awake()
    {
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || other.GetComponentInParent<PlayerController2D>() == null)
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
