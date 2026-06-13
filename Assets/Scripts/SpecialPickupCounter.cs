using TMPro;
using UnityEngine;

public class SpecialPickupCounter : MonoBehaviour
{
    public static SpecialPickupCounter Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private string label = "";
    [SerializeField] private string separator = "/";

    [Header("Reward")]
    [SerializeField, Min(1)] private int requiredCount = 5;
    [Tooltip("Use a unique ID for each level, for example CollectSpecials.Level1.")]
    [SerializeField] private string achievementId = "CollectSpecials.Level1";
    [SerializeField, Min(0)] private int starReward = 1;

    private int currentCount;

    public int CurrentCount => currentCount;
    public int RequiredCount => requiredCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "Only one SpecialPickupCounter should exist in a scene.",
                this
            );
            enabled = false;
            return;
        }

        Instance = this;

        if (countText == null)
        {
            countText = GetComponent<TMP_Text>();
        }

        currentCount = StarProgress.HasAchievement(achievementId)
            ? requiredCount
            : 0;
        UpdateHud();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddSpecial(int amount = 1)
    {
        if (StarProgress.HasAchievement(achievementId))
        {
            currentCount = requiredCount;
            UpdateHud();
            return;
        }

        currentCount = Mathf.Clamp(
            currentCount + Mathf.Max(0, amount),
            0,
            requiredCount
        );
        UpdateHud();

        if (currentCount < requiredCount)
        {
            return;
        }

        if (StarProgress.AwardAchievement(achievementId, starReward))
        {
            Debug.Log(
                "Collected " + requiredCount +
                " special pickups. Awarded " + starReward +
                " star(s). Total stars: " + StarProgress.TotalStars,
                this
            );
        }
    }

    private void UpdateHud()
    {
        if (countText != null)
        {
            countText.text =
                label + currentCount + separator + requiredCount;
        }
    }
}
