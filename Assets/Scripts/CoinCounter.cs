using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinCounter : MonoBehaviour
{
    public static CoinCounter Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private string label = "Coins: ";
    [SerializeField] private string startScreenSceneName = "StartScreen";

    public int CoinCount => CoinProgress.CurrentCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one CoinCounter should exist in the scene.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        if (SceneManager.GetActiveScene().name != startScreenSceneName)
        {
            CoinProgress.BeginRun();
        }

        UpdateHud();
    }

    private void OnEnable()
    {
        CoinProgress.CoinsChanged += HandleCoinsChanged;
        UpdateHud();
    }

    private void OnDisable()
    {
        CoinProgress.CoinsChanged -= HandleCoinsChanged;
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
        CoinProgress.AddCoins(amount);
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (coinText != null)
        {
            coinText.text = label + CoinCount;
        }
    }

    private void HandleCoinsChanged(int totalCoins)
    {
        UpdateHud();
    }
}
