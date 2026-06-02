using TMPro;
using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    public static CoinCounter Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private string label = "Coins: ";

    public int CoinCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one CoinCounter should exist in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        UpdateHud();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddCoins(int amount)
    {
        CoinCount += amount;
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (coinText != null)
        {
            coinText.text = label + CoinCount;
        }
    }
}
