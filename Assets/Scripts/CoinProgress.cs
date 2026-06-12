using UnityEngine;

public static class CoinProgress
{
    private const string SavedCoinsKey = "TotalCoins";

    private static int runStartCoins;
    private static int runCoins;
    private static bool runActive;

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
    }

    public static void DiscardRun()
    {
        runActive = false;
        runCoins = 0;
    }
}
