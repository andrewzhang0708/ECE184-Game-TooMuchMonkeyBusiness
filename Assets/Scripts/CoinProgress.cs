using System;
using UnityEngine;

public static class CoinProgress
{
    private const string SavedCoinsKey = "TotalCoins";

    private static int runStartCoins;
    private static int runCoins;
    private static bool runActive;

    public static event Action<int> CoinsChanged;

    public static int SavedCoins => PlayerPrefs.GetInt(SavedCoinsKey, 0);
    public static int CurrentCoins => runActive ? runStartCoins + runCoins : SavedCoins;

    public static void BeginRun()
    {
        runStartCoins = SavedCoins;
        runCoins = 0;
        runActive = true;
    }

    public static void AddCoins(int amount)
    {
        if (!runActive)
        {
            BeginRun();
        }

        runCoins = Mathf.Max(0, runCoins + amount);
        CoinsChanged?.Invoke(CurrentCoins);
    }

    public static void CommitRun()
    {
        if (!runActive)
        {
            return;
        }

        PlayerPrefs.SetInt(SavedCoinsKey, runStartCoins + runCoins);
        PlayerPrefs.Save();
        runActive = false;
        runCoins = 0;
        CoinsChanged?.Invoke(SavedCoins);
    }

    public static void DiscardRun()
    {
        runActive = false;
        runCoins = 0;
    }

    public static bool TrySpendSavedCoins(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        int savedCoins = SavedCoins;

        if (runActive || savedCoins < safeAmount)
        {
            return false;
        }

        PlayerPrefs.SetInt(SavedCoinsKey, savedCoins - safeAmount);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(SavedCoins);
        return true;
    }

    public static void ResetProgress()
    {
        runStartCoins = 0;
        runCoins = 0;
        runActive = false;
        PlayerPrefs.DeleteKey(SavedCoinsKey);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(0);
    }
}
