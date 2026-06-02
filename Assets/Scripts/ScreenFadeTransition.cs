using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeTransition : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    public static ScreenFadeTransition Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private bool isTransitioning;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TryPlay(Action onBlackScreen)
    {
        if (isTransitioning)
        {
            return false;
        }

        StartCoroutine(Play(onBlackScreen));
        return true;
    }

    private IEnumerator Play(Action onBlackScreen)
    {
        isTransitioning = true;
        canvasGroup.blocksRaycasts = true;

        yield return FadeTo(1f);
        onBlackScreen?.Invoke();
        yield return FadeTo(0f);

        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
