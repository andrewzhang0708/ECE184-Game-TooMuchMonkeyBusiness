using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] hearts;
    [SerializeField] private bool hideEmptyHearts = true;
    [SerializeField, Range(0f, 1f)] private float emptyHeartAlpha = 0.25f;

    private int lastShownLives = -1;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        RefreshHealth(true);
    }

    private void Update()
    {
        RefreshHealth(false);
    }

    private void RefreshHealth(bool forceRefresh)
    {
        if (playerHealth == null || hearts == null)
        {
            return;
        }

        int lives = playerHealth.CurrentLives;

        if (!forceRefresh && lives == lastShownLives)
        {
            return;
        }

        lastShownLives = lives;

        for (int i = 0; i < hearts.Length; i++)
        {
            Image heart = hearts[i];

            if (heart == null)
            {
                continue;
            }

            bool filled = i < lives;

            if (hideEmptyHearts)
            {
                heart.enabled = filled;
                continue;
            }

            heart.enabled = true;

            Color color = heart.color;
            color.a = filled ? 1f : emptyHeartAlpha;
            heart.color = color;
        }
    }
}
