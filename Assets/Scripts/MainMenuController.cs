using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button continueButton;
    public Button settingButton;
    public Button creditButton;
    public Button quitButton;
    public Button settingCloseButton;
    public Button creditCloseButton;

    [Header("Panels")]
    public CanvasGroup settingPanelGroup;
    public CanvasGroup creditPanelGroup;
    public CanvasGroup mainMenuGroup;

    [Header("Behavior")]
    public bool continueEnabled = true;
    public string gameplaySceneName = string.Empty;
    [Range(0.05f, 1f)] public float panelFadeDuration = 0.2f;

    private Coroutine panelRoutine;

    private void Awake()
    {
        ValidateReferences();
        BindButtons();
        InitializePanels();
        ApplyContinueState();
    }

    private void ValidateReferences()
    {
        WarnIfMissing(startButton, nameof(startButton));
        WarnIfMissing(continueButton, nameof(continueButton));
        WarnIfMissing(settingButton, nameof(settingButton));
        WarnIfMissing(creditButton, nameof(creditButton));
        WarnIfMissing(quitButton, nameof(quitButton));
        WarnIfMissing(settingCloseButton, nameof(settingCloseButton));
        WarnIfMissing(creditCloseButton, nameof(creditCloseButton));
        WarnIfMissing(settingPanelGroup, nameof(settingPanelGroup));
        WarnIfMissing(creditPanelGroup, nameof(creditPanelGroup));
        WarnIfMissing(mainMenuGroup, nameof(mainMenuGroup));
    }

    private static void WarnIfMissing(Object target, string name)
    {
        if (target == null)
        {
            Debug.LogWarning($"MainMenuController missing reference: {name}");
        }
    }

    private void BindButtons()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (settingButton != null) settingButton.onClick.AddListener(OpenSettings);
        if (creditButton != null) creditButton.onClick.AddListener(OpenCredits);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (settingCloseButton != null) settingCloseButton.onClick.AddListener(ClosePanels);
        if (creditCloseButton != null) creditCloseButton.onClick.AddListener(ClosePanels);
    }

    private void InitializePanels()
    {
        SetPanelVisible(settingPanelGroup, false, true);
        SetPanelVisible(creditPanelGroup, false, true);
        SetMainMenuInteractable(true);
    }

    private void ApplyContinueState()
    {
        if (continueButton == null) return;

        continueButton.interactable = continueEnabled;
        var group = continueButton.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = continueEnabled ? 1f : 0.5f;
        }
    }

    public void OnStartClicked()
    {
        if (!string.IsNullOrWhiteSpace(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        Debug.Log("Start Game clicked");
    }

    public void OnContinueClicked()
    {
        if (!continueEnabled)
        {
            Debug.Log("Continue is disabled");
            return;
        }

        Debug.Log("Continue clicked");
    }

    public void OpenSettings()
    {
        StartPanelTransition(settingPanelGroup);
    }

    public void OpenCredits()
    {
        StartPanelTransition(creditPanelGroup);
    }

    public void ClosePanels()
    {
        StartPanelTransition(null);
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
#if UNITY_EDITOR
        // Keep editor session running; only log in editor.
#else
        Application.Quit();
#endif
    }

    private void StartPanelTransition(CanvasGroup targetPanel)
    {
        if (panelRoutine != null)
        {
            StopCoroutine(panelRoutine);
        }
        panelRoutine = StartCoroutine(AnimatePanels(targetPanel));
    }

    private IEnumerator AnimatePanels(CanvasGroup targetPanel)
    {
        SetMainMenuInteractable(targetPanel == null);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, panelFadeDuration);

        float settingStart = settingPanelGroup != null ? settingPanelGroup.alpha : 0f;
        float creditStart = creditPanelGroup != null ? creditPanelGroup.alpha : 0f;
        float settingEnd = targetPanel == settingPanelGroup ? 1f : 0f;
        float creditEnd = targetPanel == creditPanelGroup ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            if (settingPanelGroup != null)
            {
                SetPanelVisible(settingPanelGroup, true, false);
                settingPanelGroup.alpha = Mathf.Lerp(settingStart, settingEnd, eased);
            }

            if (creditPanelGroup != null)
            {
                SetPanelVisible(creditPanelGroup, true, false);
                creditPanelGroup.alpha = Mathf.Lerp(creditStart, creditEnd, eased);
            }

            yield return null;
        }

        if (settingPanelGroup != null)
        {
            settingPanelGroup.alpha = settingEnd;
            SetPanelVisible(settingPanelGroup, settingEnd > 0.001f, false);
        }

        if (creditPanelGroup != null)
        {
            creditPanelGroup.alpha = creditEnd;
            SetPanelVisible(creditPanelGroup, creditEnd > 0.001f, false);
        }

        panelRoutine = null;
    }

    private static void SetPanelVisible(CanvasGroup group, bool visible, bool forceAlpha)
    {
        if (group == null) return;

        if (forceAlpha) group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        group.gameObject.SetActive(visible);
    }

    private void SetMainMenuInteractable(bool interactable)
    {
        if (mainMenuGroup == null) return;

        mainMenuGroup.interactable = interactable;
        mainMenuGroup.blocksRaycasts = interactable;
        mainMenuGroup.alpha = interactable ? 1f : 0.65f;
    }
}
