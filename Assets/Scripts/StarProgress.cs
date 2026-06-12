using System;
using UnityEngine;

public static class StarProgress
{
    private const string SavedStarsKey = "TotalStars";
    private const string AchievementKeyPrefix = "StarAchievement.";

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
        PlayerPrefs.SetInt(SavedStarsKey, newTotal);
        PlayerPrefs.Save();
        StarsChanged?.Invoke(newTotal);
        return true;
    }
}
