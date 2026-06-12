using UnityEngine;

public class StarAchievement : MonoBehaviour
{
    [SerializeField] private string achievementId;
    [SerializeField, Min(0)] private int starReward = 1;

    public bool IsCompleted => StarProgress.HasAchievement(achievementId);

    public void CompleteAchievement()
    {
        if (StarProgress.AwardAchievement(achievementId, starReward))
        {
            Debug.Log(
                "Completed achievement " + achievementId +
                ". Total stars: " + StarProgress.TotalStars,
                this
            );
        }
    }
}
