using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : Singleton<FadeManager>
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    public void FadeIn(System.Action onComplete = null)
    {
        LogManager.LogDebugOnly("Starting fade in effect", LogType.FadeManager);
        StartCoroutine(FadeCoroutine(1f, 0f, onComplete));
    }

    public void FadeOut(System.Action onComplete = null)
    {
        LogManager.LogDebugOnly("Starting fade out effect", LogType.FadeManager);
        StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
    }

    public bool IsFadePanelVisible()
    {
        return fadePanel.color.a >= 1f;
    }

    public void Stay(float alpha)
    {
        SetAlpha(alpha);
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, System.Action onComplete)
    {
        LogManager.LogDebugOnly($"Fading from alpha {startAlpha} to {endAlpha} over {fadeDuration} seconds", LogType.FadeManager);

        fadePanel.gameObject.SetActive(true);
        SetAlpha(startAlpha);
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            SetAlpha(currentAlpha);
            yield return null;
        }

        SetAlpha(endAlpha);

        if (endAlpha == 0f)
            fadePanel.gameObject.SetActive(false);

        LogManager.LogDebugOnly($"Fade complete. Final alpha: {endAlpha}", LogType.FadeManager);

        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        Color panelColor = fadePanel.color;
        panelColor.a = alpha;
        fadePanel.color = panelColor;
    }
}
