using System;
using System.Collections.Generic;
using UnityEngine;

public static class StarProgress
{
    private const string SavedStarsKey = "TotalStars";
    private const string AchievementKeyPrefix = "StarAchievement.";
    private const string AchievementRegistryKey = "StarAchievement.Registry";
    private const char AchievementSeparator = '\n';

    public static event Action<int> StarsChanged;

    public static int TotalStars => PlayerPrefs.GetInt(SavedStarsKey, 0);

    public static bool HasAchievement(string achievementId)
    {
        return !string.IsNullOrWhiteSpace(achievementId) &&
            PlayerPrefs.GetInt(AchievementKeyPrefix + achievementId, 0) == 1;
    }

    public static bool AwardAchievement(string achievementId, int starReward = 1)
    {
        if (string.IsNullOrWhiteSpace(achievementId))
        {
            Debug.LogWarning("A star achievement needs a unique achievement ID.");
            return false;
        }

        if (HasAchievement(achievementId))
        {
            return false;
        }

        int newTotal = TotalStars + Mathf.Max(0, starReward);
        PlayerPrefs.SetInt(AchievementKeyPrefix + achievementId, 1);
        RegisterAchievement(achievementId);
        PlayerPrefs.SetInt(SavedStarsKey, newTotal);
        PlayerPrefs.Save();
        StarsChanged?.Invoke(newTotal);
        return true;
    }

    public static void ResetProgress()
    {
        foreach (string achievementId in GetRegisteredAchievements())
        {
            PlayerPrefs.DeleteKey(AchievementKeyPrefix + achievementId);
        }

        // Compatibility with progress saved before the registry was introduced.
        PlayerPrefs.DeleteKey(AchievementKeyPrefix + "RescueRio");
        PlayerPrefs.DeleteKey(AchievementRegistryKey);
        PlayerPrefs.DeleteKey(SavedStarsKey);
        PlayerPrefs.Save();
        StarsChanged?.Invoke(0);
    }

    private static void RegisterAchievement(string achievementId)
    {
        HashSet<string> achievements = GetRegisteredAchievements();
        if (!achievements.Add(achievementId))
        {
            return;
        }

        PlayerPrefs.SetString(
            AchievementRegistryKey,
            string.Join(AchievementSeparator.ToString(), achievements)
        );
    }

    private static HashSet<string> GetRegisteredAchievements()
    {
        HashSet<string> achievements = new HashSet<string>();
        string registry = PlayerPrefs.GetString(AchievementRegistryKey, string.Empty);

        foreach (string achievementId in registry.Split(AchievementSeparator))
        {
            if (!string.IsNullOrWhiteSpace(achievementId))
            {
                achievements.Add(achievementId);
            }
        }

        return achievements;
    }
}
