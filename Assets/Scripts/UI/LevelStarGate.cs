using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelStarGate : MonoBehaviour
{
    [Header("Requirement")]
    [SerializeField, Min(0)] private int requiredStars;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private string requirementPrefix = "x";

    [Header("Locked State")]
    [Tooltip("The cloud covering this level button. The requirement star may remain its child.")]
    [SerializeField] private GameObject lockedCloud;
    [SerializeField] private Button levelButton;

    [Header("Optional Unlock Animation")]
    [SerializeField] private Animator cloudAnimator;
    [SerializeField] private string unlockTrigger = "Open";
    [SerializeField] private bool hideCloudWithoutAnimator = true;

    private bool isUnlocked;

    private void Awake()
    {
        if (levelButton == null)
        {
            levelButton = GetComponentInChildren<Button>(true);
        }

        if (
            hideCloudWithoutAnimator &&
            lockedCloud != null &&
            levelButton != null &&
            levelButton.transform.IsChildOf(lockedCloud.transform)
        )
        {
            Debug.LogWarning(
                "The level button is inside the locked cloud. Move the button outside the cloud " +
                "or disable Hide Cloud Without Animator so unlocking does not hide the button.",
                this
            );
        }

        Refresh();
    }

    private void OnEnable()
    {
        StarProgress.StarsChanged += HandleStarsChanged;
        Refresh();
    }

    private void OnDisable()
    {
        StarProgress.StarsChanged -= HandleStarsChanged;
    }

    public void Refresh()
    {
        if (requirementText != null)
        {
            requirementText.text = requirementPrefix + requiredStars;
        }

        bool shouldUnlock = StarProgress.TotalStars >= requiredStars;

        if (levelButton != null)
        {
            levelButton.interactable = shouldUnlock;
        }

        if (!shouldUnlock)
        {
            isUnlocked = false;

            if (lockedCloud != null)
            {
                lockedCloud.SetActive(true);
            }

            if (cloudAnimator != null)
            {
                cloudAnimator.Rebind();
                cloudAnimator.Update(0f);
            }

            return;
        }

        if (isUnlocked)
        {
            return;
        }

        isUnlocked = true;

        if (cloudAnimator != null && !string.IsNullOrEmpty(unlockTrigger))
        {
            cloudAnimator.SetTrigger(unlockTrigger);
        }
        else if (hideCloudWithoutAnimator && lockedCloud != null)
        {
            lockedCloud.SetActive(false);
        }
    }

    private void HandleStarsChanged(int totalStars)
    {
        Refresh();
    }
}
