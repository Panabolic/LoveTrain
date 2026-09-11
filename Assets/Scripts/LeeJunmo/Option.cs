using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Option : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject optionPanel;

    [Header("Volume Sliders (0 ~ 100)")]
    public Slider BGMSlider;
    public Slider SFXSlider;
    [SerializeField] private TextMeshProUGUI bgmVolumeValueText;
    [SerializeField] private TextMeshProUGUI sfxVolumeValueText;

    [Header("Screen Settings")]
    [SerializeField] private Button screenModePreviousButton;
    [SerializeField] private Button screenModeNextButton;
    [SerializeField] private TextMeshProUGUI screenModeValueText;
    [SerializeField] private Button windowResolutionPreviousButton;
    [SerializeField] private Button windowResolutionNextButton;
    [SerializeField] private TextMeshProUGUI windowResolutionValueText;

    [Header("Language")]
    [SerializeField] private Button languagePreviousButton;
    [SerializeField] private Button languageNextButton;
    [SerializeField] private TextMeshProUGUI languageValueText;
    private Text languageLegacyValueText;

    [Header("Function Buttons")]
    public Button restartButton;
    public Button quitButton;
    [SerializeField] private Button closeButton;

    [Header("Title Access")]
    [SerializeField] private Button titleOpenButton;

    private static bool optionInputBlocked;

    private static readonly string[] screenModeLabels =
    {
        "Windowed",
        "Fullscreen",
        "Borderless"
    };

    private static readonly string[] screenModeKeys = { "ui.screen.windowed", "ui.screen.fullscreen", "ui.screen.borderless" };

    private readonly List<ScreenResolutionOption> windowResolutionOptions = new List<ScreenResolutionOption>();
    private int screenModeIndex;
    private int windowResolutionIndex;

    public static bool IsOptionInputBlocked => optionInputBlocked;

    public static void SetOptionInputBlocked(bool blocked)
    {
        optionInputBlocked = blocked;
    }

    private void Awake()
    {
        HideOptionPanel();
    }

    void Start()
    {
        SetOptionInputBlocked(false);
        HideOptionPanel();
        ScreenDisplaySettings.ApplySavedPreferences();

        InitializeButtons();
        InitializeSliders();
        InitializeScreenControls();
        InitializeLanguageControls();
        InitializeTitleOpenButton();

        HideOptionPanel();
    }

    private void OnDestroy()
    {
        EnglishLocalization.LanguageChanged -= RefreshLanguageControls;
        if (languagePreviousButton != null) languagePreviousButton.onClick.RemoveListener(ToggleLanguage);
        if (languageNextButton != null) languageNextButton.onClick.RemoveListener(ToggleLanguage);
        if (BGMSlider != null) BGMSlider.onValueChanged.RemoveListener(UpdateBGMVolume);
        if (SFXSlider != null) SFXSlider.onValueChanged.RemoveListener(UpdateSFXVolume);

        if (screenModePreviousButton != null) screenModePreviousButton.onClick.RemoveListener(SelectPreviousScreenMode);
        if (screenModeNextButton != null) screenModeNextButton.onClick.RemoveListener(SelectNextScreenMode);
        if (windowResolutionPreviousButton != null) windowResolutionPreviousButton.onClick.RemoveListener(SelectPreviousWindowResolution);
        if (windowResolutionNextButton != null) windowResolutionNextButton.onClick.RemoveListener(SelectNextWindowResolution);
        if (titleOpenButton != null) titleOpenButton.onClick.RemoveListener(ToggleOptionPanel);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseOption);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleOptionPanel();
        }

        RefreshTitleOpenButtonVisibility();
    }

    public void ToggleOptionPanel()
    {
        if (GameManager.Instance == null) return;

        GameState currentState = GameManager.Instance.CurrentState;

        if (currentState == GameState.Pause)
        {
            CloseOption();
            return;
        }

        if (optionInputBlocked)
        {
            return;
        }

        if (CanOpenOptionFromState(currentState))
        {
            GameManager.Instance.PauseGame();
            RefreshScreenControlValues();
            RefreshLanguageControls();
            if (optionPanel != null) optionPanel.SetActive(true);
        }
    }

    private bool CanOpenOptionFromState(GameState state)
    {
        return state == GameState.Title
            || state == GameState.Playing
            || state == GameState.Boss
            || state == GameState.Start;
    }

    public void CloseOption()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Pause)
        {
            GameManager.Instance.ResumeGame();
            HideOptionPanel();
        }
    }

    private void HideOptionPanel()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }
    }

    private void InitializeButtons()
    {
        InitializeCloseButton();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    Time.timeScale = 1.0f;
                    GameManager.Instance.RestartGame();
                }
            });
        }

        if (quitButton != null && quitButton != closeButton)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });
        }
    }

    private void InitializeCloseButton()
    {
        if (closeButton == null && optionPanel != null)
        {
            closeButton = FindButtonInOptionPanel("CloseButton");
        }

        if (closeButton == null)
        {
            return;
        }

        closeButton.onClick.RemoveListener(CloseOption);
        closeButton.onClick.AddListener(CloseOption);
    }

    private void InitializeSliders()
    {
        ConfigureVolumeSlider(BGMSlider);
        ConfigureVolumeSlider(SFXSlider);

        if (SoundManager.Instance != null)
        {
            if (BGMSlider != null) BGMSlider.value = SoundManager.Instance.GetBGMVolume() * 100f;
            if (SFXSlider != null) SFXSlider.value = SoundManager.Instance.GetSFXVolume() * 100f;
        }
        else
        {
            if (BGMSlider != null) BGMSlider.value = 100;
            if (SFXSlider != null) SFXSlider.value = 100;
        }

        RefreshVolumeValueTexts();

        if (BGMSlider != null) BGMSlider.onValueChanged.AddListener(UpdateBGMVolume);
        if (SFXSlider != null) SFXSlider.onValueChanged.AddListener(UpdateSFXVolume);
    }

    private void ConfigureVolumeSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 100f;
    }

    private void InitializeTitleOpenButton()
    {
        if (titleOpenButton == null)
        {
            titleOpenButton = FindButtonInCanvas("TitleOptionButton");
        }

        if (titleOpenButton == null)
        {
            return;
        }

        titleOpenButton.onClick.RemoveListener(ToggleOptionPanel);
        titleOpenButton.onClick.AddListener(ToggleOptionPanel);
        RefreshTitleOpenButtonVisibility();
    }

    private void RefreshTitleOpenButtonVisibility()
    {
        if (titleOpenButton == null)
        {
            return;
        }

        bool isTitleState = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Title;
        titleOpenButton.gameObject.SetActive(isTitleState);
        titleOpenButton.interactable = isTitleState && !optionInputBlocked;
    }

    private Button FindButtonInCanvas(string buttonName)
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            return null;
        }

        Button[] buttons = parentCanvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == buttonName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void InitializeLanguageControls()
    {
        EnglishLocalization.LanguageChanged -= RefreshLanguageControls;
        EnglishLocalization.LanguageChanged += RefreshLanguageControls;
        if (optionPanel == null) return;
        if (languagePreviousButton == null) languagePreviousButton = FindButtonInOptionPanel("LanguagePrevButton");
        if (languageNextButton == null) languageNextButton = FindButtonInOptionPanel("LanguageNextButton");
        if (languageValueText == null) languageValueText = FindTMPTextInOptionPanel("LanguageValueText");
        if (languageValueText == null)
            foreach (Text label in optionPanel.GetComponentsInChildren<Text>(true))
                if (label.name == "LanguageValueText") languageLegacyValueText = label;
        if (languagePreviousButton != null)
        {
            languagePreviousButton.onClick.RemoveListener(ToggleLanguage);
            languagePreviousButton.onClick.AddListener(ToggleLanguage);
        }
        if (languageNextButton != null)
        {
            languageNextButton.onClick.RemoveListener(ToggleLanguage);
            languageNextButton.onClick.AddListener(ToggleLanguage);
        }
        RefreshLanguageControls();
    }

    private void ToggleLanguage() => EnglishLocalization.SetLanguage(!EnglishLocalization.IsEnglish);

    private void RefreshLanguageControls()
    {
        SetTMPValueText(languageValueText, EnglishLocalization.IsEnglish ? "English" : "한국어");
        if (languageLegacyValueText != null) languageLegacyValueText.text = EnglishLocalization.IsEnglish ? "English" : "한국어";
        RefreshScreenModeValue();
    }

    private void InitializeScreenControls()
    {
        if (optionPanel == null)
        {
            return;
        }

        FindScreenControlsInOptionPanel();

        if (!HasScreenControlReferences())
        {
            return;
        }

        screenModePreviousButton.onClick.RemoveListener(SelectPreviousScreenMode);
        screenModeNextButton.onClick.RemoveListener(SelectNextScreenMode);
        windowResolutionPreviousButton.onClick.RemoveListener(SelectPreviousWindowResolution);
        windowResolutionNextButton.onClick.RemoveListener(SelectNextWindowResolution);

        RefreshScreenControlValues();

        screenModePreviousButton.onClick.AddListener(SelectPreviousScreenMode);
        screenModeNextButton.onClick.AddListener(SelectNextScreenMode);
        windowResolutionPreviousButton.onClick.AddListener(SelectPreviousWindowResolution);
        windowResolutionNextButton.onClick.AddListener(SelectNextWindowResolution);
    }

    private void FindScreenControlsInOptionPanel()
    {
        if (screenModePreviousButton == null) screenModePreviousButton = FindButtonInOptionPanel("ScreenModePrevButton");
        if (screenModeNextButton == null) screenModeNextButton = FindButtonInOptionPanel("ScreenModeNextButton");
        if (screenModeValueText == null) screenModeValueText = FindTMPTextInOptionPanel("ScreenModeValueText");
        if (windowResolutionPreviousButton == null) windowResolutionPreviousButton = FindButtonInOptionPanel("WindowResolutionPrevButton");
        if (windowResolutionNextButton == null) windowResolutionNextButton = FindButtonInOptionPanel("WindowResolutionNextButton");
        if (windowResolutionValueText == null) windowResolutionValueText = FindTMPTextInOptionPanel("WindowResolutionValueText");
    }

    private Button FindButtonInOptionPanel(string buttonName)
    {
        Button[] buttons = optionPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == buttonName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private TextMeshProUGUI FindTMPTextInOptionPanel(string textName)
    {
        TextMeshProUGUI[] texts = optionPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == textName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private bool HasScreenControlReferences()
    {
        return screenModePreviousButton != null
            && screenModeNextButton != null
            && screenModeValueText != null
            && windowResolutionPreviousButton != null
            && windowResolutionNextButton != null
            && windowResolutionValueText != null;
    }

    private void SetTMPValueText(TextMeshProUGUI valueText, string value)
    {
        if (valueText == null)
        {
            return;
        }

        valueText.text = value;
    }

    private void SetVolumeValueText(TextMeshProUGUI valueText, float value)
    {
        int displayValue = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 100f));
        SetTMPValueText(valueText, $"{displayValue} %");
    }

    private void RefreshVolumeValueTexts()
    {
        if (BGMSlider != null) SetVolumeValueText(bgmVolumeValueText, BGMSlider.value);
        if (SFXSlider != null) SetVolumeValueText(sfxVolumeValueText, SFXSlider.value);
    }

    private void SetScreenModeValueText(string value)
    {
        SetTMPValueText(screenModeValueText, value);
    }

    private void SetWindowResolutionValueText(string value)
    {
        SetTMPValueText(windowResolutionValueText, value);
    }

    private void RefreshWindowResolutionOptions()
    {
        windowResolutionOptions.Clear();
        windowResolutionOptions.AddRange(ScreenDisplaySettings.GetWindowResolutionOptions());
        windowResolutionIndex = ScreenDisplaySettings.FindClosestResolutionIndex(
            windowResolutionOptions,
            ScreenDisplaySettings.GetSavedWindowResolution());
    }

    private void RefreshScreenControlValues()
    {
        if (!HasScreenControlReferences())
        {
            return;
        }

        screenModeIndex = (int)ScreenDisplaySettings.GetSavedDisplayMode();
        RefreshWindowResolutionOptions();
        RefreshScreenModeValue();
        RefreshWindowResolutionValue();
        RefreshWindowResolutionInteractable();
    }

    private void RefreshScreenModeValue()
    {
        screenModeIndex = Mathf.Clamp(screenModeIndex, 0, screenModeLabels.Length - 1);
        SetScreenModeValueText(EnglishLocalization.Get(screenModeKeys[screenModeIndex], screenModeLabels[screenModeIndex]));
    }

    private void RefreshWindowResolutionValue()
    {
        ScreenDisplayModeOption displayMode = (ScreenDisplayModeOption)Mathf.Clamp(
            screenModeIndex,
            0,
            screenModeLabels.Length - 1);

        if (displayMode != ScreenDisplayModeOption.Windowed)
        {
            SetWindowResolutionValueText(ScreenDisplaySettings.GetResolutionForDisplayMode(displayMode).Label);
            return;
        }

        if (windowResolutionOptions.Count == 0)
        {
            SetWindowResolutionValueText("N/A");
            return;
        }

        windowResolutionIndex = Mathf.Clamp(windowResolutionIndex, 0, windowResolutionOptions.Count - 1);
        SetWindowResolutionValueText(windowResolutionOptions[windowResolutionIndex].Label);
    }

    private void RefreshWindowResolutionInteractable()
    {
        bool canChangeResolution = ScreenDisplaySettings.GetSavedDisplayMode() == ScreenDisplayModeOption.Windowed
            && windowResolutionOptions.Count > 0;

        windowResolutionPreviousButton.interactable = canChangeResolution;
        windowResolutionNextButton.interactable = canChangeResolution;
    }

    private void SelectPreviousScreenMode()
    {
        ChangeScreenMode(-1);
    }

    private void SelectNextScreenMode()
    {
        ChangeScreenMode(1);
    }

    private void SelectPreviousWindowResolution()
    {
        ChangeWindowResolution(-1);
    }

    private void SelectNextWindowResolution()
    {
        ChangeWindowResolution(1);
    }

    private void ChangeScreenMode(int direction)
    {
        screenModeIndex = WrapIndex(screenModeIndex + direction, screenModeLabels.Length);
        ScreenDisplayModeOption mode = (ScreenDisplayModeOption)screenModeIndex;

        if (windowResolutionOptions.Count > 0)
        {
            int resolutionIndex = Mathf.Clamp(windowResolutionIndex, 0, windowResolutionOptions.Count - 1);
            ScreenDisplaySettings.SaveWindowResolution(windowResolutionOptions[resolutionIndex], saveImmediately: false);
        }

        ScreenDisplaySettings.ApplyDisplayMode(mode);
        RefreshScreenModeValue();
        RefreshWindowResolutionValue();
        RefreshWindowResolutionInteractable();
    }

    private void ChangeWindowResolution(int direction)
    {
        if (windowResolutionOptions.Count == 0)
        {
            return;
        }

        windowResolutionIndex = WrapIndex(windowResolutionIndex + direction, windowResolutionOptions.Count);
        ScreenResolutionOption selectedResolution = windowResolutionOptions[windowResolutionIndex];

        if (ScreenDisplaySettings.GetSavedDisplayMode() == ScreenDisplayModeOption.Windowed)
        {
            ScreenDisplaySettings.ApplyWindowResolution(selectedResolution);
            screenModeIndex = (int)ScreenDisplayModeOption.Windowed;
            RefreshScreenModeValue();
        }
        else
        {
            ScreenDisplaySettings.SaveWindowResolution(selectedResolution);
        }

        RefreshWindowResolutionValue();
        RefreshWindowResolutionInteractable();
    }

    private int WrapIndex(int value, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int result = value % count;
        if (result < 0)
        {
            result += count;
        }

        return result;
    }

    void UpdateBGMVolume(float value)
    {
        SetVolumeValueText(bgmVolumeValueText, value);

        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMVolume(value / 100f);
    }

    void UpdateSFXVolume(float value)
    {
        SetVolumeValueText(sfxVolumeValueText, value);

        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(value / 100f);
    }
}
