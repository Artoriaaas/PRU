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

    [Header("Transitions")]
    public CanvasGroup fadeOverlay;
    public Text messageText;
    public float startFadeDuration = 0.6f;
    public float messageDuration = 2f;

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Behavior")]
    public bool continueEnabled = true;
    public bool hasSaveData = false;
    public string gameplaySceneName = string.Empty;
    public MainMenuIntro menuIntro;

    [Range(0.05f, 1f)] public float panelFadeDuration = 0.2f;

    private Coroutine panelRoutine;
    private Coroutine messageRoutine;
    private bool menuReady;

    private void Awake()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            gameplaySceneName = "MapScene";
        }

        ValidateReferences();
        BindButtons();
        InitializePanels();
        ApplyContinueState();
        ApplyButtonSounds();
        HideMessage();

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
            UpdateFadeOverlayRaycast(false);
        }

        menuReady = menuIntro == null || menuIntro.introOverlayGroup == null;
        if (mainMenuGroup != null && menuIntro != null)
        {
            mainMenuGroup.interactable = false;
            mainMenuGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!menuReady && menuIntro != null && menuIntro.IntroFinished)
        {
            menuReady = true;
            EnableMenuAfterIntro();
        }
    }

    private void EnableMenuAfterIntro()
    {
        SetMainMenuInteractable(true);

        if (menuIntro != null && menuIntro.menuLayers != null)
        {
            for (int i = 0; i < menuIntro.menuLayers.Length; i++)
            {
                var layer = menuIntro.menuLayers[i];
                if (layer == null) continue;
                layer.alpha = 1f;
                layer.interactable = true;
                layer.blocksRaycasts = true;
                layer.gameObject.SetActive(true);
            }
        }
    }

    private void UpdateFadeOverlayRaycast(bool block)
    {
        if (fadeOverlay == null) return;

        fadeOverlay.blocksRaycasts = block;
        fadeOverlay.interactable = block;

        var image = fadeOverlay.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = block;
        }
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

    private void ApplyButtonSounds()
    {
        var effects = FindObjectsOfType<UIButtonEffect>(true);
        for (int i = 0; i < effects.Length; i++)
        {
            if (hoverSound != null) effects[i].hoverSound = hoverSound;
            if (clickSound != null) effects[i].clickSound = clickSound;
        }
    }

    private void InitializePanels()
    {
        SetPanelVisible(settingPanelGroup, false, true);
        SetPanelVisible(creditPanelGroup, false, true);
        if (menuIntro == null)
        {
            SetMainMenuInteractable(true);
        }
    }

    private void ApplyContinueState()
    {
        if (continueButton == null) return;

        // Compute hasSaveData dynamically from PlayerPrefs
        int mapProgress = PlayerPrefs.GetInt("MapProgress", 0);
        int tutorialStep = PlayerPrefs.GetInt("TutorialStep", 0);
        int mapProgression = PlayerPrefs.GetInt("MapProgression", 0);
        hasSaveData = mapProgress > 0 || tutorialStep > 0 || mapProgression > 0;

        continueEnabled = hasSaveData;

        continueButton.interactable = continueEnabled;
        var group = continueButton.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = continueEnabled ? 1f : 0.5f;
        }
    }

    public void OnStartClicked()
    {
        if (!menuReady) return;
        PlayerPrefs.DeleteKey("TutorialStep");
        PlayerPrefs.DeleteKey("MapProgression");
        PlayerPrefs.DeleteKey("MapProgress");
        
        string[] castles = { "Trại Yên", "Thiên Trường", "Thăng Long" };
        foreach (var c in castles)
        {
            PlayerPrefs.DeleteKey("DialogueBefore_" + c);
            PlayerPrefs.DeleteKey("DialogueAfter_" + c + "_Pending");
        }
        
        PlayerPrefs.Save();
        SkillManager.ResetSkillsStatic();
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        SetMainMenuInteractable(false);

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            UpdateFadeOverlayRaycast(true);
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, startFadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            fadeOverlay.alpha = 1f;
        }

        if (!string.IsNullOrWhiteSpace(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
            yield break;
        }

        Debug.Log("Gameplay Scene Not Assigned");

        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, startFadeDuration * 0.5f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            fadeOverlay.alpha = 0f;
            UpdateFadeOverlayRaycast(false);
        }

        SetMainMenuInteractable(true);
    }

    public void OnContinueClicked()
    {
        if (!menuReady) return;

        Debug.Log("Continue Clicked");

        if (!hasSaveData)
        {
            ShowMessage("No Save Data");
            return;
        }

        StartCoroutine(StartGameRoutine());
    }

    public void OpenSettings()
    {
        if (!menuReady) return;
        StartPanelTransition(settingPanelGroup);
    }

    public void OpenCredits()
    {
        if (!menuReady) return;
        StartPanelTransition(creditPanelGroup);
    }

    public void ClosePanels()
    {
        StartPanelTransition(null);
    }

    private void OnQuitClicked()
    {
        if (!menuReady) return;

        Debug.Log("Quit Clicked");
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }

    private void ShowMessage(string text)
    {
        if (messageText == null)
        {
            Debug.Log(text);
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }
        messageRoutine = StartCoroutine(ShowMessageRoutine(text));
    }

    private void HideMessage()
    {
        if (messageText == null) return;
        messageText.gameObject.SetActive(false);
    }

    private IEnumerator ShowMessageRoutine(string text)
    {
        messageText.gameObject.SetActive(true);
        messageText.text = text;
        Color c = messageText.color;
        c.a = 1f;
        messageText.color = c;

        yield return new WaitForSecondsRealtime(messageDuration);

        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / duration);
            messageText.color = c;
            yield return null;
        }

        messageText.gameObject.SetActive(false);
        messageRoutine = null;
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
