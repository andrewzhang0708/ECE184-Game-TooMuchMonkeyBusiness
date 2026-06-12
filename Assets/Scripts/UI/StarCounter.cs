using TMPro;
using UnityEngine;

public class StarCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text starText;
    [SerializeField] private string label = "x";

    private void Awake()
    {
        if (starText == null)
        {
            starText = GetComponent<TMP_Text>();
        }

        Refresh(StarProgress.TotalStars);
    }

    private void OnEnable()
    {
        StarProgress.StarsChanged += Refresh;
        Refresh(StarProgress.TotalStars);
    }

    private void OnDisable()
    {
        StarProgress.StarsChanged -= Refresh;
    }

    private void Refresh(int totalStars)
    {
        if (starText != null)
        {
            starText.text = label + totalStars;
        }
    }
}
