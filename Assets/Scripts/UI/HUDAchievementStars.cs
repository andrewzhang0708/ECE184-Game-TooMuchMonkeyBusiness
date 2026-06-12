using System;
using UnityEngine;

public class HUDAchievementStars : MonoBehaviour
{
    [Serializable]
    private struct AchievementStarSlot
    {
        [Tooltip("Must match the Achievement ID used by Cage or StarAchievement.")]
        public string achievementId;
        public GameObject emptyStar;
        public GameObject completedStar;
    }

    [SerializeField] private AchievementStarSlot[] starSlots;

    private void Awake()
    {
        RefreshAll();
    }

    private void OnEnable()
    {
        StarProgress.StarsChanged += HandleStarsChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        StarProgress.StarsChanged -= HandleStarsChanged;
    }

    public void RefreshAll()
    {
        if (starSlots == null)
        {
            return;
        }

        for (int i = 0; i < starSlots.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(starSlots[i].achievementId))
            {
                Debug.LogWarning(
                    "HUD achievement star slot " + (i + 1) +
                    " needs an Achievement ID.",
                    this
                );
            }

            RefreshSlot(starSlots[i]);
        }
    }

    private static void RefreshSlot(AchievementStarSlot slot)
    {
        bool isCompleted = StarProgress.HasAchievement(slot.achievementId);

        if (slot.emptyStar != null)
        {
            slot.emptyStar.SetActive(!isCompleted);
        }

        if (slot.completedStar != null)
        {
            slot.completedStar.SetActive(isCompleted);
        }
    }

    private void HandleStarsChanged(int totalStars)
    {
        RefreshAll();
    }
}
