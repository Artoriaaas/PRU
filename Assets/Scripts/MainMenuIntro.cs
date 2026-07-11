using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuIntro : MonoBehaviour
{
    public static bool hasSeenIntro = false;
    [Header("Intro UI")]
    public CanvasGroup introOverlayGroup;
    public Text introText;

    [Header("Menu Layers To Reveal")]
    public CanvasGroup[] menuLayers;

    [Header("Timing")]
    public float lineDisplayDuration = 2f;
    public float textFadeDuration = 0.35f;
    public float menuFadeInDuration = 0.6f;

    [TextArea(2, 4)]
    public string[] introLines =
    {
        "This game is inspired by historical events.",
        "Some events have been adapted for gameplay.",
        "Lead Dai Viet. Defend the homeland."
    };

    public bool IntroFinished { get; private set; }

    private void Awake()
    {
        ResetIntroOverlay();
        IntroFinished = false;
    }

    private void OnDisable()
    {
        ResetIntroOverlay();
    }

    private void Start()
    {
        IntroFinished = true;
        SetMenuLayersAlpha(1f, true);
        ResetIntroOverlay();
    }

    private void ResetIntroOverlay()
    {
        if (introOverlayGroup == null) return;

        introOverlayGroup.alpha = 0f;
        introOverlayGroup.interactable = false;
        introOverlayGroup.blocksRaycasts = false;
        introOverlayGroup.gameObject.SetActive(false);
    }

    private IEnumerator PlayIntro()
    {
        IntroFinished = false;
        SetMenuLayersAlpha(0f, false);

        introOverlayGroup.gameObject.SetActive(true);
        introOverlayGroup.alpha = 1f;
        introOverlayGroup.interactable = false;
        introOverlayGroup.blocksRaycasts = true;

        if (introText != null)
        {
            introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, 0f);
        }

        for (int i = 0; i < introLines.Length; i++)
        {
            if (introText != null)
            {
                introText.text = introLines[i];
                yield return FadeText(1f);
            }

            yield return new WaitForSecondsRealtime(lineDisplayDuration);

            if (introText != null && i < introLines.Length - 1)
            {
                yield return FadeText(0f);
            }
        }

        if (introText != null)
        {
            yield return FadeText(0f);
        }

        yield return FadeMenuLayers(1f);
        IntroFinished = true;
        hasSeenIntro = true;
        ResetIntroOverlay();
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        if (introText == null) yield break;

        Color c = introText.color;
        float start = c.a;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, textFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(start, targetAlpha, t);
            introText.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        introText.color = c;
    }

    private IEnumerator FadeMenuLayers(float targetAlpha)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, menuFadeInDuration);
        float[] starts = new float[menuLayers.Length];

        for (int i = 0; i < starts.Length; i++)
        {
            if (menuLayers[i] != null)
            {
                menuLayers[i].gameObject.SetActive(true);
                starts[i] = menuLayers[i].alpha;
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            for (int i = 0; i < menuLayers.Length; i++)
            {
                if (menuLayers[i] == null) continue;
                menuLayers[i].alpha = Mathf.Lerp(starts[i], targetAlpha, eased);
            }

            yield return null;
        }

        SetMenuLayersAlpha(targetAlpha, true);
    }

    private void SetMenuLayersAlpha(float alpha, bool interactable)
    {
        if (menuLayers == null) return;

        for (int i = 0; i < menuLayers.Length; i++)
        {
            if (menuLayers[i] == null) continue;
            menuLayers[i].alpha = alpha;
            menuLayers[i].interactable = interactable;
            menuLayers[i].blocksRaycasts = interactable;
            menuLayers[i].gameObject.SetActive(alpha > 0.001f || interactable);
        }
    }
}
