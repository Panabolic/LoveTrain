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

    [Header("Screen Settings")]
    [SerializeField] private Button screenModePreviousButton;
    [SerializeField] private Button screenModeNextButton;
    [SerializeField] private TextMeshProUGUI screenModeValueText;
    [SerializeField] private Button windowResolutionPreviousButton;
    [SerializeField] private Button windowResolutionNextButton;
    [SerializeField] private TextMeshProUGUI windowResolutionValueText;
    [SerializeField] private bool createMissingScreenControls = true;

    [Header("Function Buttons")]
    public Button restartButton;
    public Button quitButton;

    private static readonly string[] screenModeLabels =
    {
        "Windowed",
        "Fullscreen",
        "Borderless"
    };

    private readonly List<ScreenResolutionOption> windowResolutionOptions = new List<ScreenResolutionOption>();
    private int screenModeIndex;
    private int windowResolutionIndex;

    private void Awake()
    {
        HideOptionPanel();
    }

    void Start()
    {
        HideOptionPanel();
        ScreenDisplaySettings.ApplySavedPreferences();

        InitializeButtons();
        InitializeSliders();
        InitializeScreenControls();

        HideOptionPanel();
    }

    private void OnDestroy()
    {
        if (BGMSlider != null) BGMSlider.onValueChanged.RemoveListener(UpdateBGMVolume);
        if (SFXSlider != null) SFXSlider.onValueChanged.RemoveListener(UpdateSFXVolume);

        if (screenModePreviousButton != null) screenModePreviousButton.onClick.RemoveListener(SelectPreviousScreenMode);
        if (screenModeNextButton != null) screenModeNextButton.onClick.RemoveListener(SelectNextScreenMode);
        if (windowResolutionPreviousButton != null) windowResolutionPreviousButton.onClick.RemoveListener(SelectPreviousWindowResolution);
        if (windowResolutionNextButton != null) windowResolutionNextButton.onClick.RemoveListener(SelectNextWindowResolution);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleOptionPanel();
        }
    }

    public void ToggleOptionPanel()
    {
        if (GameManager.Instance == null) return;

        GameState currentState = GameManager.Instance.CurrentState;

        if (currentState == GameState.Playing || currentState == GameState.Boss || currentState == GameState.Start)
        {
            GameManager.Instance.PauseGame();
            RefreshScreenControlValues();
            if (optionPanel != null) optionPanel.SetActive(true);
        }
        else if (currentState == GameState.Pause)
        {
            CloseOption();
        }
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

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });
        }
    }

    private void InitializeSliders()
    {
        if (BGMSlider != null) { BGMSlider.minValue = 0; BGMSlider.maxValue = 100; }
        if (SFXSlider != null) { SFXSlider.minValue = 0; SFXSlider.maxValue = 100; }

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

        if (BGMSlider != null) BGMSlider.onValueChanged.AddListener(UpdateBGMVolume);
        if (SFXSlider != null) SFXSlider.onValueChanged.AddListener(UpdateSFXVolume);
    }

    private void InitializeScreenControls()
    {
        if (optionPanel == null)
        {
            return;
        }

        FindScreenControlsInOptionPanel();

        if (createMissingScreenControls && !HasScreenControlReferences())
        {
            CreateScreenControls();
        }

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
        if (screenModeValueText == null) screenModeValueText = FindTextInOptionPanel("ScreenModeValueText");
        if (windowResolutionPreviousButton == null) windowResolutionPreviousButton = FindButtonInOptionPanel("WindowResolutionPrevButton");
        if (windowResolutionNextButton == null) windowResolutionNextButton = FindButtonInOptionPanel("WindowResolutionNextButton");
        if (windowResolutionValueText == null) windowResolutionValueText = FindTextInOptionPanel("WindowResolutionValueText");
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

    private TextMeshProUGUI FindTextInOptionPanel(string textName)
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

    private void CreateScreenControls()
    {
        RectTransform optionPanelRect = optionPanel.GetComponent<RectTransform>();
        if (optionPanelRect == null)
        {
            return;
        }

        RectTransform group = CreateRectObject("ScreenSettingsControls", optionPanelRect);
        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.anchoredPosition = new Vector2(0f, -35f);
        group.sizeDelta = new Vector2(390f, 76f);

        CreateSelectorRow(
            group,
            "Screen",
            "ScreenModePrevButton",
            "ScreenModeValueText",
            "ScreenModeNextButton",
            new Vector2(0f, 18f),
            out screenModePreviousButton,
            out screenModeValueText,
            out screenModeNextButton);

        CreateSelectorRow(
            group,
            "Resolution",
            "WindowResolutionPrevButton",
            "WindowResolutionValueText",
            "WindowResolutionNextButton",
            new Vector2(0f, -18f),
            out windowResolutionPreviousButton,
            out windowResolutionValueText,
            out windowResolutionNextButton);
    }

    private void CreateSelectorRow(
        RectTransform parent,
        string label,
        string previousButtonName,
        string valueTextName,
        string nextButtonName,
        Vector2 anchoredPosition,
        out Button previousButton,
        out TextMeshProUGUI valueText,
        out Button nextButton)
    {
        RectTransform row = CreateRectObject(label + "Row", parent);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = anchoredPosition;
        row.sizeDelta = new Vector2(370f, 30f);

        TextMeshProUGUI labelText = CreateText("Label", row, label, 14, TextAlignmentOptions.Right);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(94f, 0f);

        previousButton = CreateTextButton(previousButtonName, row, "<");
        RectTransform previousButtonRect = previousButton.GetComponent<RectTransform>();
        previousButtonRect.anchorMin = new Vector2(0f, 0.5f);
        previousButtonRect.anchorMax = new Vector2(0f, 0.5f);
        previousButtonRect.pivot = new Vector2(0f, 0.5f);
        previousButtonRect.anchoredPosition = new Vector2(112f, 0f);
        previousButtonRect.sizeDelta = new Vector2(34f, 28f);

        valueText = CreateValueText(valueTextName, row);
        RectTransform valueRect = valueText.transform.parent.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 0.5f);
        valueRect.anchorMax = new Vector2(0f, 0.5f);
        valueRect.pivot = new Vector2(0f, 0.5f);
        valueRect.anchoredPosition = new Vector2(150f, 0f);
        valueRect.sizeDelta = new Vector2(174f, 28f);

        nextButton = CreateTextButton(nextButtonName, row, ">");
        RectTransform nextButtonRect = nextButton.GetComponent<RectTransform>();
        nextButtonRect.anchorMin = new Vector2(0f, 0.5f);
        nextButtonRect.anchorMax = new Vector2(0f, 0.5f);
        nextButtonRect.pivot = new Vector2(0f, 0.5f);
        nextButtonRect.anchoredPosition = new Vector2(328f, 0f);
        nextButtonRect.sizeDelta = new Vector2(34f, 28f);
    }

    private Button CreateTextButton(string buttonName, RectTransform parent, string label)
    {
        RectTransform buttonRect = CreateRectObject(buttonName, parent);
        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.95f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        TextMeshProUGUI buttonText = CreateText("Text", buttonRect, label, 16, TextAlignmentOptions.Center);
        RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        return button;
    }

    private TextMeshProUGUI CreateValueText(string textName, RectTransform parent)
    {
        RectTransform valueBox = CreateRectObject(textName + "Box", parent);
        Image background = valueBox.gameObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.95f);

        TextMeshProUGUI valueText = CreateText(textName, valueBox, string.Empty, 14, TextAlignmentOptions.Center);
        RectTransform valueTextRect = valueText.GetComponent<RectTransform>();
        valueTextRect.anchorMin = Vector2.zero;
        valueTextRect.anchorMax = Vector2.one;
        valueTextRect.offsetMin = new Vector2(6f, 0f);
        valueTextRect.offsetMax = new Vector2(-6f, 0f);
        return valueText;
    }

    private RectTransform CreateRectObject(string objectName, RectTransform parent)
    {
        GameObject newObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = newObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private TextMeshProUGUI CreateText(string objectName, RectTransform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateRectObject(objectName, parent);
        TextMeshProUGUI uiText = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        uiText.fontSize = fontSize;
        uiText.color = Color.black;
        uiText.alignment = alignment;
        uiText.text = text;
        uiText.raycastTarget = false;
        return uiText;
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
        screenModeValueText.text = screenModeLabels[screenModeIndex];
    }

    private void RefreshWindowResolutionValue()
    {
        ScreenDisplayModeOption displayMode = (ScreenDisplayModeOption)Mathf.Clamp(
            screenModeIndex,
            0,
            screenModeLabels.Length - 1);

        if (displayMode != ScreenDisplayModeOption.Windowed)
        {
            windowResolutionValueText.text = ScreenDisplaySettings.GetResolutionForDisplayMode(displayMode).Label;
            return;
        }

        if (windowResolutionOptions.Count == 0)
        {
            windowResolutionValueText.text = "N/A";
            return;
        }

        windowResolutionIndex = Mathf.Clamp(windowResolutionIndex, 0, windowResolutionOptions.Count - 1);
        windowResolutionValueText.text = windowResolutionOptions[windowResolutionIndex].Label;
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
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMVolume(value / 100f);
    }

    void UpdateSFXVolume(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(value / 100f);
    }
}
