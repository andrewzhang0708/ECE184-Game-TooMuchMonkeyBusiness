using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DoubleJumpPurchaseController : MonoBehaviour
{
    [Header("Purchase")]
    [SerializeField, Min(0)] private int price = 10;
    [SerializeField] private Button purchaseButton;

    [Header("Display")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Optional. This object is hidden after Double Jump has been purchased.")]
    [SerializeField] private GameObject hideWhenPurchased;
    [SerializeField] private string pricePrefix = "BUY DOUBLE JUMP - ";
    [SerializeField] private string coinSuffix = " COINS";
    [SerializeField] private string purchasedText = "PURCHASED";
    [SerializeField] private string notEnoughCoinsText = "NOT ENOUGH COINS";

    private void Awake()
    {
        if (purchaseButton == null)
        {
            purchaseButton = GetComponent<Button>();
        }

        purchaseButton?.onClick.AddListener(BuyDoubleJump);
        Refresh();
    }

    private void OnEnable()
    {
        CoinProgress.CoinsChanged += HandleCoinsChanged;
        PowerUpProgress.DoubleJumpChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        CoinProgress.CoinsChanged -= HandleCoinsChanged;
        PowerUpProgress.DoubleJumpChanged -= Refresh;
    }

    private void OnDestroy()
    {
        purchaseButton?.onClick.RemoveListener(BuyDoubleJump);
    }

    public void BuyDoubleJump()
    {
        if (PowerUpProgress.HasDoubleJump)
        {
            Refresh();
            return;
        }

        if (!CoinProgress.TrySpendSavedCoins(price))
        {
            if (statusText != null)
            {
                statusText.text = notEnoughCoinsText;
            }

            Debug.LogWarning(
                "Double Jump purchase failed. Saved coins: " +
                CoinProgress.SavedCoins + ", price: " + price + ".",
                this
            );
            RefreshButtonState();
            return;
        }

        PowerUpProgress.UnlockDoubleJump();
        Refresh();
    }

    public void Refresh()
    {
        bool purchased = PowerUpProgress.HasDoubleJump;

        GameObject purchaseDisplay = hideWhenPurchased != null
            ? hideWhenPurchased
            : purchaseButton != null
                ? purchaseButton.gameObject
                : null;
        if (purchaseDisplay != null)
        {
            purchaseDisplay.SetActive(!purchased);
        }

        if (priceText != null)
        {
            priceText.text = purchased
                ? purchasedText
                : pricePrefix + price + coinSuffix;
        }

        if (statusText != null)
        {
            statusText.text = purchased ? purchasedText : string.Empty;
        }

        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (purchaseButton != null)
        {
            purchaseButton.interactable = !PowerUpProgress.HasDoubleJump;
        }
    }

    private void HandleCoinsChanged(int totalCoins)
    {
        Refresh();
    }
}
