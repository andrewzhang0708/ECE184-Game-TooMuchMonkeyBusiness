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

            RefreshButtonState();
            return;
        }

        PowerUpProgress.UnlockDoubleJump();
        Refresh();
    }

    public void Refresh()
    {
        bool purchased = PowerUpProgress.HasDoubleJump;

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
            purchaseButton.interactable =
                !PowerUpProgress.HasDoubleJump &&
                CoinProgress.SavedCoins >= price;
        }
    }

    private void HandleCoinsChanged(int totalCoins)
    {
        Refresh();
    }
}
